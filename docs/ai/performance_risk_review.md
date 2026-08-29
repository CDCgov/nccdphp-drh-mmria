# mmria – Production Performance & Resource Risk Review

> Scope: .NET application code in `nccdphp-drh-mmria` and `nccdphp-drh-mmria-utilities`.
> Target environment: OpenShift, ~72 tenant `mmria-server` pods + 72 tenant CouchDB pods, plus `mmria.services` workers.
> Evidence-based; cites concrete files, methods and line ranges. Hypotheses are labelled.

---

## 1. Executive summary

The mmria codebase has several systemic patterns that will become severe under multi-tenant scale on OpenShift:

1. **Sync-over-async blocking is widespread**, including inside per-request paths (authorization on every request via `_design/.../by_user_id` lookups), inside Akka actor message handlers, and at startup. Each blocking call burns a thread-pool thread for the duration of an HTTP round-trip to CouchDB. With 72 tenants and authenticated traffic, this is the most likely root cause of "the pod stops responding" symptoms.
2. **Compatibility shims construct `new HttpClient()` per call** (`SimpleHttpClientFactory`) and several utility paths still create `new HttpClient()` in the method body. This breaks the IHttpClientFactory pooling guarantee and risks socket exhaustion / SNAT churn on OpenShift.
3. **Unbounded CouchDB reads dominate report and case-view paths.** Examples: `report/_all_docs?include_docs=true` for the public aggregate-report endpoint, `mmrds/_design/sortable/_view/by_date_created?skip=0&take=250000` for "existing record IDs", `_find` queries with `limit = 1_000_000` and `268_435_456`. These pull the entire tenant dataset into the pod heap, then enumerate it again with LINQ in memory.
4. **Per-tenant fan-out without parallelism limits.** `SessionSummary`, `JurisdictionSummary`, `Process_DB_Synchronization_Set` and `c_document_sync_all` start one task per tenant/row with no `MaxDegreeOfParallelism` (only one of them, `c_document_sync_all`, sets a limit). With 72 tenants this materialises 72 concurrent CouchDB queries from a single pod and 72 in-memory result graphs.
5. **Static mutable globals in `mmria.server.Program`** (`Last_Change_Sequence`, `DateOfLastChange_Sequence_Call`, `Change_Sequence_Call_Count`) are used by background actors. In a multi-tenant pod they are shared across tenants — both a correctness bug and a memory/contention concern.
6. **Heavy `JObject` / `ExpandoObject` / Newtonsoft round-trips on every case** in the rebuild and CDC-sync paths — full deserialize → mutate → serialize → bulk POST cycle per record. At 72 tenants × tens of thousands of cases this is the biggest GC pressure in the repo.
7. **Chatty `Console.WriteLine` debug logging in hot paths** (controllers, case-view, log retrieval, exporters). Console writes are synchronous and serialised by the runtime; under load they become a contention bottleneck and inflate stdout volume that OpenShift collects.
8. **Dangerous patterns specific to the IJE batch supervisor**: a `while(DateTime.Now < next_date) { /* do nothing */ }` busy-spin and `.Result` on a "load all batches" call inside an actor's pre-start.

---

## Status snapshot (remediation progress)

| Issue | Status | Notes |
|---|---|---|
| A — `SimpleHttpClientFactory` per-call `new HttpClient()` | **DONE** | Self-contained shim now returns `HttpClient` over a single shared `SocketsHttpHandler` (PooledConnectionLifetime 5min, IdleTimeout 2min, MaxConnectionsPerServer 64). Not wired to DI per prior regression history. |
| B — Per-request authorization sync-over-async CouchDB calls | **DONE** | New `AuthorizationRoleCache` (5s TTL, `ConcurrentDictionary`); `GetActiveUserRoleJurisdictions` and `get_user_jurisdiction_set` split into wrapper + loader. |
| C — Aggregate report endpoint reads every report doc into memory | **DONE** | `ExecuteForJsonDocumentAsync` streams via `JsonDocument.ParseAsync`; `AggregateReportManager.GetReportsAsync` pre-filters `year_of_death != 9999` and `year_of_case_review` numeric *before* `Convert(...)` allocations. |
| D — `CaseViewManager` 250k / 268M-row checks | **DONE** | Phase 1: `RecordIdExistsAsync` Mango `_find limit:1` replaces 25k-row HashSet load. Phase 2: `GetDuplicateCaseViewAsync` `take` capped at 1000 (was Int32-near-max), `by_last_name` view scoped via `startkey`/`endkey`. Phase 3: audit `_find limit` lowered from `1_000_000` to `10_000` in `_auditController` and `AuditRecoverUtilController`. |
| E — Per-tenant fan-out without bounded parallelism | **DONE** | `JurisdictionSummary` and `SessionSummary` now build factory lists and run via `Parallel.ForEachAsync` with `MaxDegreeOfParallelism = 6`. `Process_DB_Synchronization_Set` and `Synchronize_Deleted_Case_Records` per-row `Task.Run` fire-and-forget loops replaced with awaited `Parallel.ForEachAsync` capped at 4 — exceptions now propagate to the actor. Validated by [PerformanceFixesTests](../../../nccdphp-drh-mmria-utilities/mmria-server.tests/Tests/Quarantine/PerformanceFixesTests.cs) (Epic 45 Story 45.2: file quarantined due to `BulkDocumentPayloadBuilder` duplication across `mmria-server` and `mmria.services` assemblies; behavior it validates is unchanged — restore file after upstream extern-alias fix). |
| F — `BatchSupervisor` busy-spin + `.Result` in ctor | **DONE** | `ReceiveActor` + `IWithStash`; `PreStart` sends `InitializeBatchList` self-message; `Initializing` state stashes; busy-wait replaced with `await Task.Delay(CvsServerRetryDelayMs)` plus diagnostic logs. |
| G — Mutable static state in `mmria.server.Program` shared across tenants | **DONE** | New `TenantChangeSequenceState` (per-tenant). `Program` exposes `ConcurrentDictionary<string, TenantChangeSequenceState>` keyed by `{url}|{prefix}`. `Process_DB_Synchronization_Set`, `Synchronize_Deleted_Case_Records`, and `c_db_setup` all read/write per-tenant state. |
| H — CouchDB sync deserialises every case to `JObject` then reserialises | **DONE** | Extracted `BulkDocumentPayloadBuilder` (System.Text.Json `JsonDocument` + `Utf8JsonWriter`). `bulk_write_chunk_async` no longer materialises a `JArray`/`JObject` graph per chunk; `hydrate_existing_revisions_async` replaced with `fetch_existing_revisions_async` returning a plain id→rev `Dictionary`. Payload concatenated directly from the original document JSON strings; `_rev` is rewritten only when needed (replace, insert, or strip). Legacy hydration semantics preserved (docs absent from the existing-rev map keep their original `_rev`). 7 new tests in `PerformanceFixesTests.cs` (13/13 passing). mmria.services copy ported in lockstep: parallel `BulkDocumentPayloadBuilder` under `nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/`, `bulk_write_chunk_async` no longer parses to `JArray`/`JObject`. |
| I — Inline `using var httpClient = new HttpClient()` (TAMUGeocode) | Not started | |
| J — Startup blocks on `.Result` / `GetAwaiter().GetResult()` | Not started | |
| K — `MMRIAServicesDAL.GetCaseView` 100k-row Skip(0).Take(100000) | Not started | |
| L — Fire-and-forget `Task.Run` for log persistence | Not started | |
| M — Excessive `Console.WriteLine` in request paths | Not started | |
| N — Per-row inner-loop allocations in exporter | Not started | |
| O — Mango `_find` queries with `limit = 1_000_000` | **DONE (subsumed by D Phase 3)** | The two `_auditController` / `AuditRecoverUtilController` callsites called out in O were the same ones lowered to 10,000 in D. The `overdose_measureController` call (already at 10,000) is unchanged. |
| P — `c_db_setup` / `Rebuild_Export_Queue` midnight wipes | Not started | |
| Q — `migrate.C_Get_Set_Value.get_value` dynamic walk per field | Not started | |
| R — `Process_DB_Synchronization_Set` / `Synchronize_Deleted_Case_Records` load all `_all_docs` into HashSets | **DONE** | `_all_docs` body now streamed via `JsonDocument`; `c_all_docs` POCO graph no longer materialised. `de_id` and `report` HashSets scoped in inner blocks so peak live HashSet count drops from 3 to 2. HashSet pre-sized from `total_rows`. Validated by `PopulateIdSet_*` tests. |

**Validation:** `mmria-server.csproj` and `mmria.services.csproj` build green after each change. `PerformanceFixesTests` (6 tests) cover the bounded-fan-out and `_all_docs` streaming patterns from E and R.

---

## 2. Top high-risk issues

### Issue A — `SimpleHttpClientFactory` returns a brand-new `HttpClient` every call  ✅ DONE
- **File / method:** [nccdphp-drh-mmria-common/mmria.common/SimpleHttpClientFactory.cs](nccdphp-drh-mmria-common/mmria.common/SimpleHttpClientFactory.cs#L8-L14)
- **Pattern:**
  ```csharp
  public HttpClient CreateClient(string name) { return new HttpClient(); }
  ```
- **Used by:** every `CreateCompatibilityCouchDbHttpClient()` shim — see [authorization.pmss.cs](source-code/mmria/mmria-server/util/authorization.pmss.cs#L209-L213), [authorization_user.pmss.cs](source-code/mmria/mmria-server/util/authorization_user.pmss.cs#L131-L134), [authorization_case.pmss.cs](source-code/mmria/mmria-server/util/authorization_case.pmss.cs#L144-L147), [authorization_case.cs](nccdphp-drh-mmria-common/mmria.common/utils/authorization_case.cs#L233-L237), and at startup in both [mmria-server/Program.cs](source-code/mmria/mmria-server/Program.cs#L189-L200) and [mmria.services/Program.cs](nccdphp-drh-mmria-services/mmria.services/Program.cs#L191-L201).
- **Why dangerous:** `new HttpClient()` per call holds a `HttpMessageHandler` + connection pool. Each disposal leaves sockets in `TIME_WAIT` for ~2 minutes. The actual `CouchDbHttpClient` is correctly factory-based, but every "compatibility" path circumvents that.
- **Impact:** sockets/connections, memory (handler graph), latency (TLS re-handshake), DNS lookups.
- **Why worse at 72 pods:** 72 pods × multiple compat-handler call-sites × every authenticated request → SNAT port exhaustion on the OpenShift node and unbounded CouchDB connection churn.
- **Severity:** Critical
- **Confidence:** High
- **Remediation:** Delete `SimpleHttpClientFactory`, require an injected `IHttpClientFactory` everywhere. Replace each `CreateCompatibilityCouchDbHttpClient()` callsite with the DI-resolved client. Where a static helper truly needs a client, accept it as a parameter (which most already do — the compat factory is the fallback that must die).

---

### Issue B — Per-request authorization does sync-over-async CouchDB calls  ✅ DONE
- **File / method:** [nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Other/authorization.cs `GetActiveUserRoleJurisdictions`](nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Other/authorization.cs#L235-L260) and [mmria.common/utils/authorization_case.cs `get_user_jurisdiction_set`](nccdphp-drh-mmria-common/mmria.common/utils/authorization_case.cs#L202-L223), plus [authorization.pmss.cs](source-code/mmria/mmria-server/util/authorization.pmss.cs#L225-L240).
- **Pattern:**
  ```csharp
  jurisdicion_result_string = couchDbHttpClient.ExecuteAsync(
      "GET", jurisdicion_view_url, ..., "application/json").GetAwaiter().GetResult();
  ```
  followed by `Newtonsoft.Json.JsonConvert.DeserializeObject<...>(jurisdicion_result_string)`.
- **Why dangerous:** these helpers are called from authorization filters / controllers per request. `.GetAwaiter().GetResult()` blocks an ASP.NET thread-pool thread for an entire CouchDB round-trip. Combined with Issue A (each call may spin a fresh `HttpClient`) this is the canonical thread-pool starvation pattern.
- **Impact:** thread pool, latency, request queueing → 503/timeouts; CPU when the runtime grows the pool.
- **Why worse at 72 pods:** every concurrent request on every pod consumes a worker thread until CouchDB responds. Slow tenants cascade.
- **Severity:** Critical
- **Confidence:** High
- **Remediation:** Make these methods `async Task<...>`; propagate `await` up to the controller. Cache the per-user role set for the session (it already lives in the session document) instead of re-reading the view on every request.

---

### Issue C — Aggregate report endpoint reads every report doc into memory  ✅ DONE
- **File / method:** [aggregate_reportController.Get](source-code/mmria/mmria-server/Controllers/api/aggregate_reportController.cs#L46-L50) → [AggregateReportManager.GetReportsAsync](nccdphp-drh-mmria-common/mmria.common/SharedLibraries/AggregateReport/Manager/AggregateReportManager.cs#L24-L70)
- **Pattern:**
  ```csharp
  string request_string = dbConfig.Get_Prefix_DB_Url("report/_all_docs?include_docs=true");
  string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, ...);
  using (JsonDocument doc = JsonDocument.Parse(responseFromServer)) { ... foreach row Convert(...) ... }
  ```
  No `limit`, no `skip`, no view filter.
- **Why dangerous:** materialises the **entire `report` database** as a single string in memory, then reparses to `JsonDocument`, then converts every row to a strongly-typed `c_report_object`. Memory peak is ~3× the document set (raw response + JsonDocument + result list).
- **Impact:** memory peak, GC (Gen2 / LOH for the response string), CPU, CouchDB read amplification.
- **Why worse at 72 pods:** an attacker or routine dashboard can easily fan this out to many tenants. Each call by a single pod drives one tenant's CouchDB to read its entire `report` DB.
- **Severity:** Critical
- **Confidence:** High
- **Remediation:** Use a dedicated CouchDB view emitting only the filtered fields (`year_of_death != 9999 && year_of_case_review present`), paginate (`limit`/`startkey`), and stream the response (`HttpCompletionOption.ResponseHeadersRead` + `Utf8JsonReader`) instead of `ReadAsStringAsync`. Project only the fields the caller needs.

---

### Issue D — `CaseViewManager` pulls up to 250,000 / 268,435,456 rows for "duplicate" / "existing IDs" checks  ✅ DONE
- **File / method:** [CaseViewManager.GetExistingRecordIdsAsync](nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseView/CaseViewManager.cs#L1379-L1399) and [GetDuplicateCaseViewAsync](nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseView/CaseViewManager.cs#L1497-L1605); also `_audit/Get find_url` with `limit = 1_000_000` ([_auditController.cs](source-code/mmria/mmria-server/Controllers/_auditController.cs#L93-L101) and [AuditRecoverUtilController.cs](source-code/mmria/mmria-server/Controllers/api/AuditRecoverUtilController.cs#L41-L48)).
- **Pattern:**
  ```csharp
  string request_string = db_config.Get_Prefix_DB_Url(
      "mmrds/_design/sortable/_view/by_date_created?skip=0&take=250000");
  ...
  // duplicate check
  int take = 268_435_256
  ```
  followed by a `case_view_response.rows.Where(...).ToList()` and **a follow-up `GetCaseByIdAsync` per row** inside `IsDuplicateCaseAsync`.
- **Why dangerous:** the duplicate path can do `O(n)` Couch GETs per duplicate check (n = matching rows). The "existing record IDs" path holds an unbounded `HashSet<string>` per call.
- **Impact:** memory, CPU, CouchDB read amplification (the per-row GET fan-out).
- **Why worse at 72 pods:** any tenant with many cases multiplies this; concurrent writers all hit it during save.
- **Severity:** Critical
- **Confidence:** High
- **Remediation:** Replace duplicate detection with a Mango query / view keyed on `(last_name, first_name, year_of_death, month_of_death, day_of_death, state_of_death)` so CouchDB returns at most O(1) matches without `include_docs`. Replace `GetExistingRecordIdsAsync` with a paged or count-only query (or a view that emits just `record_id`). For audit, use a `limit` sized to the page actually being rendered.

---

### Issue E — Per-tenant fan-out without bounded parallelism  ✅ DONE
- **File / method:** [JurisdictionSummary.execute](source-code/mmria/mmria-server/util/JurisdictionSummary.cs#L150-L213) and [SessionSummary.execute](source-code/mmria/mmria-server/util/SessionSummary.cs#L102-L132); also [Process_DB_Synchronization_Set.cs](source-code/mmria/mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs#L91-L135) which does `Task.Run(...)` per change-sequence row with **no awaiting and no concurrency limit** (the `Task.WhenAll` is commented out).
- **Pattern:**
  ```csharp
  foreach (var config in ConfigDB.detail_list) {
      record_count_task_list.Add(GetCaseCount(..., config.Value, ...));
  }
  await Task.WhenAll(record_count_task_list);
  ```
  ```csharp
  foreach (KeyValuePair<...> kvp in response_results) {
      System.Threading.Tasks.Task.Run(new Action(async () => { ... await _couchDbHttpClient.ExecuteAsync(...) ... }));
  }
  ```
- **Why dangerous:** no `SemaphoreSlim` / `Parallel.ForEachAsync(MaxDegreeOfParallelism=N)`. Fires N concurrent requests where N = tenant count or change-sequence size. The `Task.Run` fire-and-forget pattern in `Process_DB_Synchronization_Set` also detaches errors from the actor lifecycle and can pile up on the thread pool.
- **Impact:** CPU spikes, thread pool, CouchDB connection saturation, memory churn for response strings.
- **Why worse at 72 pods:** with 72 detail_list entries the summary pages spawn 72 concurrent CouchDB calls per request. If multiple admins refresh, the `central` CouchDB sees `requests × 72`.
- **Severity:** High
- **Confidence:** High
- **Remediation:** Use `Parallel.ForEachAsync` with a small `MaxDegreeOfParallelism` (e.g. 4–8) for both summary classes. In `Process_DB_Synchronization_Set`, replace the fire-and-forget `Task.Run` with an awaited bounded loop and stop swallowing exceptions silently.

---

### Issue F — `BatchSupervisor` busy-waits on wall-clock and `.Result`s a full doc list at construction  ✅ DONE
- **File / method:** [Actors/BatchSupervisor.cs](nccdphp-drh-mmria-services/mmria.services/Actors/BatchSupervisor.cs#L34-L80)
- **Pattern:**
  ```csharp
  var alldocs = _mmriaServicesManager.GetBatchSet(...).Result;          // .Result inside ctor
  foreach(var row in alldocs.rows) batch_id_list.Add(row.id, row.doc.Status);

  var next_date = DateTime.Now.AddMilliseconds(40 * 1000);
  while(DateTime.Now < next_date) { /* do nothing */ }                 // 100% CPU spin for 40s
  ```
- **Why dangerous:** the busy-wait pegs one CPU core for 40 s when the CVS server is down. The `.Result` at construction blocks the actor thread for an entire `vital_import/_all_docs?include_docs=true` round-trip, on every actor restart.
- **Impact:** CPU pegging, thread pool, OpenShift CPU throttling (will cause noisy-neighbor effect for sibling tenants on the node).
- **Why worse at 72 pods:** a downstream CVS outage can spin one core per pod simultaneously.
- **Severity:** Critical
- **Confidence:** High
- **Remediation:** Replace busy-wait with `await Task.Delay(40_000, cancellationToken)`. Move the initial load into an `async` PreStart override and `await` it; or schedule a self-message to load asynchronously after start.

---

### Issue G — Mutable static state in `mmria.server.Program` shared across tenants  ✅ DONE
- **File / method:** [Program.cs](source-code/mmria/mmria-server/Program.cs#L40-L42) — `public static int Change_Sequence_Call_Count`, `public static string Last_Change_Sequence`, `public static List<DateTime> DateOfLastChange_Sequence_Call` (used in [Process_DB_Synchronization_Set.cs](source-code/mmria/mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs#L37-L82)).
- **Pattern:** the per-tenant Quartz sync job reads/writes a single global "last change sequence". In multi-tenant mode (`overridableConfigSets[i]` per tenant) all 72 supervisors share this value.
- **Why dangerous:** functionally wrong (one tenant's sequence overwrites another's, causing unnecessary full re-syncs) **and** a contention point. Re-syncs amplify into full `_all_docs` reads on `mmrds`, `de_id`, `report` for *the wrong tenant*.
- **Impact:** CouchDB load, memory, correctness.
- **Why worse at 72 pods:** still bad even on one pod with 72 tenants; on each pod restart all sequences reset to `null`, triggering full re-sync of all tenants simultaneously.
- **Severity:** High
- **Confidence:** High (verified by grep – the only public mutable statics found in the server)
- **Remediation:** Move `Last_Change_Sequence` / counters into the per-tenant actor state (`QuartzSupervisor` per-tenant scope) or a `ConcurrentDictionary<tenantPrefix, sequenceState>`.

---

### Issue H — CouchDB sync path deserialises every case to `JObject` then reserialises to bulk POST  ⏸ DEFERRED
- **File / method:** [util/c_document_sync_all.cs `bulk_write_chunk_async`](source-code/mmria/mmria-server/util/c_document_sync_all.cs#L1170-L1220) and the parallel build at [process_batch_bulk_async](source-code/mmria/mmria-server/util/c_document_sync_all.cs#L1273-L1330); same pattern in [populate-cdc-instance/c_document_sync_all.cs](nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/c_document_sync_all.cs#L1010-L1100).
- **Pattern:**
  ```csharp
  var docs = new JArray(document_json_list.Select(JObject.Parse));   // parse N times
  ... hydrate_existing_revisions_async(...) ...
  var payload = new JObject { ["docs"] = docs }.ToString(Formatting.None); // serialize again
  ```
  Inside `process_batch_bulk_async`, every row is sent through `c_sync_document.build_documents_async()` which `JsonConvert.DeserializeObject<ExpandoObject>` and `JsonConvert.SerializeObject` per case (see [c_convert_to_report_object.cs](nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/c_convert_to_report_object.cs#L150-L210) and [c_generate_frequency_summary_report.cs](nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/c_generate_frequency_summary_report.cs#L210-L233)).
- **Why dangerous:** Every case is parsed → walked dynamically (`ExpandoObject` + `gs.get_value` reflection-style path lookups, see [c_convert_to_dqr_detail.cs](nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/c_convert_to_dqr_detail.cs#L200-L265)) → reserialised. Newtonsoft `JObject`/`JArray` are LOH-friendly only for tiny docs; mmria cases are large.
- **Impact:** CPU (parse/serialise), GC (Gen2/LOH), memory peaks during rebuild.
- **Why worse at 72 pods:** the rebuild path runs per tenant. Concurrent tenant rebuilds (e.g. after a deploy) run this loop in 72 pods at once.
- **Severity:** High
- **Confidence:** High
- **Remediation:** Stream the bulk batch directly: when reading `_all_docs?include_docs=true` use `Utf8JsonReader` to copy the `doc` element straight into the outgoing `_bulk_docs` payload `StringBuilder` (or a pooled `ArrayBufferWriter<byte>`), avoiding `JObject.Parse` entirely. For report transformations, codegen typed POCOs (you already have `case-version/.../mmria_case.set.*.cs` switch tables — wire them in) instead of `gs.get_value(expando, "path/with/slashes")` walking.

---

### Issue I — Inline `using var httpClient = new HttpClient()` in third-party callouts
- **File / method:** [Utilities/TAMUGeocode.cs](nccdphp-drh-mmria-services/mmria.services/Utilities/TAMUGeocode.cs#L20-L60) — both the sync (`.Result`) and async overloads do `using var httpClient = new HttpClient()` per call.
- **Why dangerous:** classic HttpClient socket-leak antipattern, **and** the sync overload uses `.Result` against an external HTTPS endpoint.
- **Impact:** sockets, latency, thread pool (sync overload).
- **Why worse at 72 pods:** geocoding spikes during batch import — each pod opens fresh TLS to the same host concurrently.
- **Severity:** High
- **Confidence:** High
- **Remediation:** Inject `IHttpClientFactory`, use a named client `"tamu-geocode"`, drop the sync overload (or have it call `GetAwaiter().GetResult()` only on the async path that uses the factory client — preferably remove entirely).

---

### Issue J — Startup blocks on async with `.Result` / `GetAwaiter().GetResult()`
- **File / method:** [mmria-server/Program.cs](source-code/mmria/mmria-server/Program.cs#L189-L201) `LoadRequiredOverridableConfigurationsAsync(...).Result`; [mmria.services/Program.cs](nccdphp-drh-mmria-services/mmria.services/Program.cs#L191-L235) `LoadRequiredConfigurationSetsAsync(...).GetAwaiter().GetResult()` and `schedulerFactory.GetScheduler().Result`; [Process_Central_Pull_list.cs](source-code/mmria/mmria-server/model/actor/quartz/Process_Central_Pull_list.cs#L295-L330) — multiple `PostCommand(...).GetAwaiter().GetResult()` inside actor message handler; [Exporter/export_all_generate_name_map.cs](nccdphp-drh-mmria-services/mmria.services/Utilities/Exporter/export_all_generate_name_map.cs#L48) `_couchDbHttpClient.ExecuteAsync(...).GetAwaiter().GetResult()`; [CouchDbHttpClient.cs](nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs#L356-L370) exposes a sync `Execute(...)` wrapper that does `GetAwaiter().GetResult()` — encouraging the pattern.
- **Why dangerous:** Startup blocking is acceptable, but the same wrapper is used inside actor handlers and exporters at runtime. `GetAwaiter().GetResult()` inside an `async` Akka message handler yields no benefit and risks deadlock under SynchronizationContext changes.
- **Impact:** thread pool, latency.
- **Severity:** High
- **Confidence:** High
- **Remediation:** Make actor handlers fully async. Remove the public `CouchDbHttpClient.Execute` sync wrapper or mark it `[Obsolete]` and forbid via analyzer (`Microsoft.VisualStudio.Threading.Analyzers VSTHRD002`).

---

### Issue K — `MMRIAServicesDAL.GetCaseView` returns up to 100 000 rows and `Skip(0).Take(100000)`
- **File / method:** [MMRIAServicesDAL.cs](nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs#L50-L75)
- **Pattern:**
  ```csharp
  result.total_rows = result.rows.Count;
  result.rows = result.rows.Skip(0).Take(100000).ToList();
  ```
- **Why dangerous:** `Skip(0).Take(100000).ToList()` is a no-op cap that materialises the whole list again. The outer call also does `GetBatchSet -> /vital_import/_all_docs?include_docs=true` with no limit ([same file](nccdphp-drh-mmria-common/mmria.common/SharedLibraries/MMRIAServices/DAL/MMRIAServicesDAL.cs#L107-L125)).
- **Impact:** memory, CPU, GC.
- **Severity:** High
- **Confidence:** High
- **Remediation:** Page on the CouchDB side with `limit` / `startkey`. Drop the `Skip(0).Take(N)` re-allocation.

---

### Issue L — Fire-and-forget `Task.Run` for log persistence in a controller
- **File / method:** [loggerController.cs `~line 431`](source-code/mmria/mmria-server/Controllers/loggerController.cs#L431-L460)
- **Pattern:** controller returns immediately; spawns `_ = Task.Run(async () => { foreach (logEntry in batch) await SaveLog(...) })`.
- **Why dangerous:** unbounded background work tied to no cancellation, no back-pressure. A misbehaving client posting huge batches enqueues unbounded thread-pool work; failures swallowed to `Console.WriteLine`.
- **Impact:** thread pool, CouchDB write amplification, memory.
- **Why worse at 72 pods:** offline-client log flush pattern can hit many pods at once; without back-pressure CouchDB write queue grows.
- **Severity:** High
- **Confidence:** High
- **Remediation:** Use `Channel<LogEntry>` with a single `BackgroundService` consumer per pod, bounded capacity, drop or 429 when full; bulk-POST entries via `_bulk_docs` rather than one PUT per entry.

---

### Issue M — Excessive `Console.WriteLine` in request paths
- **File / method:** sample evidence in [CaseViewManager.cs](nccdphp-drh-mmria-common/mmria.common/SharedLibraries/CaseView/CaseViewManager.cs#L1320-L1370) (logs every doc per row), [aggregate_reportController.Get](source-code/mmria/mmria-server/Controllers/api/aggregate_reportController.cs#L48), [api/caseController.cs](source-code/mmria/mmria-server/Controllers/api/caseController.cs#L262), [overdose_measureController.cs](source-code/mmria/mmria-server/Controllers/api/overdose_measureController.cs#L75-L84), [CouchDbHttpClient.SendForResponseAsync](nccdphp-drh-mmria-common/mmria.common/getset/CouchDbHttpClient.cs#L497-L505) on every non-2xx.
- **Why dangerous:** `Console.Out` is process-global and serialised by a lock. Under load this becomes a hidden contention point and inflates OpenShift log volume (and cost).
- **Impact:** CPU, contention, log-pipeline cost.
- **Severity:** Medium
- **Confidence:** High
- **Remediation:** Switch to `ILogger<T>` with appropriate levels; gate verbose payload logs at `LogLevel.Debug` and use `IsEnabled` checks.

---

### Issue N — Per-row inner-loop allocations in the exporter
- **File / method:** [Utilities/Exporter/exporter.cs ~lines 930–995](nccdphp-drh-mmria-services/mmria.services/Utilities/Exporter/exporter.cs#L930-L995) and [mmrds_exporter.cs Execute](nccdphp-drh-mmria-services/mmria.services/Utilities/Exporter/mmrds_exporter.cs#L78-L210)
- **Pattern:** for each case row, builds `Dictionary<,>` instances per path map, calls `gs.get_grid_value` / `get_multiform_grid_value` (which walk the dynamic case tree by string path). `mmrds_exporter` rebuilds the entire path map *per call*. Several `StreamWriter` instances are allocated up front but the cleanup path is not in the snippets; need to verify they are disposed in `finally`.
- **Why dangerous:** the path-map computation depends only on the metadata version — recomputing per export is wasted CPU. Per-row dictionary allocations dominate Gen0 collections.
- **Impact:** CPU, GC, IO if writers are not disposed (hypothesis pending verification).
- **Severity:** High (CPU/GC), Medium (writer disposal — needs confirmation)
- **Confidence:** Medium
- **Remediation:** Cache the path/name maps keyed by metadata version (e.g. `ConcurrentDictionary<string, ExportSchema>`) inside a singleton service. Replace `Dictionary<,>` per row with reused buffers cleared per row. Ensure all `qualitativeStreamWriter[i]` are disposed via `try/finally` or `await using`.

---

### Issue O — Mango `_find` queries with `limit = 1_000_000`  ✅ DONE (audit callsites lowered to 10,000 under Issue D Phase 3)
- **File / method:** [_auditController.cs](source-code/mmria/mmria-server/Controllers/_auditController.cs#L93-L101), [api/AuditRecoverUtilController.cs](source-code/mmria/mmria-server/Controllers/api/AuditRecoverUtilController.cs#L41-L48), [overdose_measureController.cs](source-code/mmria/mmria-server/Controllers/api/overdose_measureController.cs#L67-L84) (`limit = 10000`).
- **Why dangerous:** `_find` with no real bound returns whatever the index has. For a single case audit history this is fine for small cases but pathological for hot cases with thousands of edits, and the response is fully materialised.
- **Impact:** memory, CouchDB load.
- **Severity:** Medium
- **Confidence:** High
- **Remediation:** Use a real page size (e.g. 200) plus `bookmark` for paging.

---

### Issue P — `c_db_setup` / `Rebuild_Export_Queue` recursively delete and recreate directories per tenant on schedule
- **File / method:** [util/c_db_setup.cs](source-code/mmria/mmria-server/util/c_db_setup.cs#L255-L270) and [model/actor/quartz/Rebuild_Export_Queue.cs](source-code/mmria/mmria-server/model/actor/quartz/Rebuild_Export_Queue.cs#L48-L80)
- **Pattern:** at midnight each tenant deletes its export directory recursively and recreates it, then drops and re-creates the `export_queue` CouchDB.
- **Why dangerous:** at the synchronised midnight tick, 72 tenants delete their `export_queue` DB and trigger CouchDB compaction work. Filesystem deletes on PVCs can be slow.
- **Impact:** disk IOPS, CouchDB load (DELETE + PUT + security doc), CPU spike at the same wall-clock minute on 72 pods.
- **Severity:** Medium
- **Confidence:** High
- **Remediation:** Stagger by tenant (`StartAt(midnight + tenantHash % 60 minutes)`). Consider draining the queue rather than dropping the database.

---

### Issue Q — `migrate.C_Get_Set_Value.get_value(ExpandoObject, "path/with/slashes")` walks dynamic dictionaries on every field
- **File / method:** used in [BatchItemProcessingService.cs](nccdphp-drh-mmria-services/mmria.services/Services/BatchItemProcessingService.cs#L1190-L1220) and across `populate-cdc-instance/*` (e.g. [c_convert_to_dqr_detail.cs](nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/c_convert_to_dqr_detail.cs#L200-L260)).
- **Why dangerous:** strongly-typed code already exists ([case-version/mmria/v260120/mmria_case.get.sg.cs / .mg.cs](nccdphp-drh-mmria-common/mmria.common/case-version/mmria/v260120/mmria_case.get.sg.cs#L100-L130)) but the hot paths still pay the dynamic-walk + boxing cost per field per case. `string.Split` and `ToLowerInvariant` on every field add allocations.
- **Impact:** CPU, GC.
- **Severity:** Medium
- **Confidence:** High
- **Remediation:** Wire the codegen typed accessors into the rebuild and IJE-import paths. Failing that, cache split paths and avoid `ToLower()` (use `StringComparison.OrdinalIgnoreCase`).

---

### Issue R — `Synchronize_Deleted_Case_Records` and `Process_DB_Synchronization_Set` load all `_all_docs` for `mmrds`, `de_id`, `report` into `HashSet`s  ✅ DONE
- **File / method:** [Process_DB_Synchronization_Set.cs](source-code/mmria/mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs#L138-L170)
- **Pattern:** `GET /{prefix}mmrds/_all_docs` then `JsonConvert.DeserializeObject<c_all_docs>` then `foreach add to HashSet<string>`. Repeated for `de_id` and `report`.
- **Why dangerous:** three full id-listings per tenant per scheduled tick, in memory simultaneously.
- **Impact:** memory peak, CPU, CouchDB load.
- **Why worse at 72 pods:** runs on a Quartz schedule per tenant.
- **Severity:** High
- **Confidence:** High
- **Remediation:** Use the `_changes` feed with `since=last_seq` (already partly tracked via `Last_Change_Sequence` — but see Issue G) instead of full `_all_docs`. If a reconciliation pass is needed, page it.

---

## 3. Patterns observed but lower priority

- **`Newtonsoft.Json` and `System.Text.Json` mixed throughout** — both their reflection caches are warmed; the dual cost is real but minor compared to issues above. Standardising on `System.Text.Json` with source-generated `JsonSerializerContext` would cut allocations.
- **`new System.Text.RegularExpressions.Regex(...)` constructed inline** in [authorization_user.pmss.cs](source-code/mmria/mmria-server/util/authorization_user.pmss.cs#L120-L130) (and Replication). Convert to static `[GeneratedRegex]` partials.
- **`PagedCaseIdLoader.GetCaseIdsAsync`** uses `skip+limit` paging on a Couch view ([source](nccdphp-drh-mmria-services/mmria.services/Utilities/PagedCaseIdLoader.cs#L1-L65)). For very large datasets, switch to `startkey`+`startkey_docid` paging — `skip` is O(skip) on the view server.

---

## 4. Cross-cutting summary

### Most likely causes of pod memory growth
1. **Aggregate report endpoint** loading entire `report` DB as a single `string` then a `JsonDocument` then a `List<c_report_object>` (Issue C).
2. **`CaseViewManager.GetExistingRecordIdsAsync` / `GetDuplicateCaseViewAsync`** holding 250k–268M-row response lists (Issue D).
3. **CouchDB sync rebuild** keeping `JObject` graphs for every case in two `ConcurrentBag<string>`s (`de_id_documents`, `report_documents`) for the whole batch before bulk write (Issue H).
4. **Static `Program.DateOfLastChange_Sequence_Call` / `Last_Change_Sequence`** plus `Process_DB_Synchronization_Set` building three full id `HashSet`s per tenant (Issues G + R).
5. **`SimpleHttpClientFactory` leaking handlers** (Issue A).

### Most likely causes of CPU spikes
1. **`BatchSupervisor` busy-spin** when CVS is unreachable (Issue F).
2. **Per-tenant fan-out** with no concurrency limit on summary pages and change processing (Issue E).
3. **JObject parse/serialise per case** in rebuild/CDC paths (Issue H).
4. **`gs.get_value(expando, "path/with/slashes")` dynamic walks** per field (Issue Q).
5. **Synchronized midnight directory wipes + DB recreates** across 72 tenants (Issue P).
6. **`Console.WriteLine` lock contention** under load (Issue M).

### Most likely causes of CouchDB amplification
1. **`report/_all_docs?include_docs=true`** every aggregate-report call (Issue C).
2. **`mmrds/_design/sortable/_view/by_date_created?take=250000`** for ID listings, plus per-row GETs in duplicate detection (Issue D).
3. **Three full `_all_docs` reads per tenant** in `Process_DB_Synchronization_Set` (Issue R).
4. **Synchronized midnight DELETE/PUT/security on `export_queue` for 72 tenants** (Issue P).
5. **Per-request `jurisdiction/_design/sortable/_view/by_user_id`** lookups in the authorization helpers, made per request rather than cached on the session (Issue B).
6. **Vital-import scheduler** loading `vital_import/_all_docs?include_docs=true` at every actor restart (Issues F + K).

---

## 5. Suggested triage order

1. Issue F (`BatchSupervisor` busy-spin + `.Result`) — small, surgical fix, immediate CPU win.
2. Issue G (statics in `Program`) — correctness bug, simple refactor.
3. Issue A (kill `SimpleHttpClientFactory`) — touches many files but mechanical.
4. Issues B + C + D — these are the request-path hotspots that dominate p95 latency and pod memory.
5. Issues E, H, R — scheduled/background path improvements; impact across all 72 tenants.
6. Issues I, J, K, L, M, N, O, P, Q — clean up.
