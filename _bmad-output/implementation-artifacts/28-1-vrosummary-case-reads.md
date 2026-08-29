# Story 28.1 — `VROSummary.cs` Case Reads Through `ICaseRepository`

**Epic:** 28 — mmria-server Non-DAL Remnants
**Story ID:** 28.1
**Status:** done
**Date added:** 2026-07-17
**Depends on:** Epic 17 story 17.2 (ICaseRepository), Epic 24 story 24.3 (GetCasesPagedAsync)
**Source requirements:** epics.md §Epic 28 Story 28.1; project-context.md §2.2

---

## User Story

As a developer,
I want `VROSummary.cs` to read case documents through `ICaseRepository` instead of constructing `mmrds` URLs directly,
So that the VRO summary actor has the same SQL migration seam as all other case-data consumers.

---

## Acceptance Criteria

**AC-1 — Per-document case GET replaced**
Given `VROSummary.cs` at approximately line 188 calls `_couchDbHttpClient.ExecuteAsync("GET", $"{db_config.url}/{db_config.prefix}mmrds/{id}", ...)` inside a `foreach` loop over `id_list`
When this story is complete
Then that call is replaced with `ICaseRepository.GetCaseDocumentJsonAsync(id, db_config)` (or equivalent method returning raw JSON); `ICaseRepository` is injected into `VROSummary` via constructor injection

**AC-2 — Case count GET replaced**
Given `VROSummary.cs` at approximately line 341 calls `_couchDbHttpClient.ExecuteAsync("GET", request_string, ...)` to read a case document in the `GetUserCount`/`GetCaseCount` methods
When this story is complete
Then that call is replaced with the corresponding `ICaseRepository` method; `ICaseRepository` is passed to or injected into the method that owns that call

**AC-3 — `_all_docs` ID-list call replaced**
Given `VROSummary.cs` `GetIdList()` at approximately line 502 calls `_couchDbHttpClient.ExecuteAsync("GET", $"{db_config.url}/{db_config.prefix}mmrds/_all_docs", ...)` to build the case ID set
When this story is complete
Then that call is replaced with `ICaseRepository.GetCasesPagedAsync(null, int.MaxValue, db_config)` or equivalent; the resulting ID set is assembled from the returned document IDs as before; the `_design` document filter (`if(_id.IndexOf("_design") > -1) continue`) is preserved

**AC-4 — `_couchDbHttpClient` removed from `VROSummary` if no other calls remain**
Given `VROSummary.cs` currently injects `CouchDbHttpClient` alongside `IUserRepository` and `IJurisdictionRepository`
When this story is complete
Then if all three CouchDB call sites are replaced, `CouchDbHttpClient _couchDbHttpClient` is removed from the constructor and field; all callers that instantiate `VROSummary` are updated accordingly

**AC-5 — Build passes with zero errors**
Given the changes above
When the build runs
Then `mmria-server` builds with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/util/VROSummary.cs` | **UPDATE** — inject `ICaseRepository`; replace 3 direct calls; remove `_couchDbHttpClient` if no calls remain |
| Caller(s) that instantiate `VROSummary` | **UPDATE** — pass `ICaseRepository` instance from DI scope; remove `CouchDbHttpClient` arg if removed from ctor |

**Context:** `VROSummary.cs` already has `IUserRepository` and `IJurisdictionRepository` injected (those were wired in Epics 18 and 19). The `_couchDbHttpClient` field survived because the mmrds calls were not covered in those epics.

**`ICaseRepository` methods to use:**
- AC-1 / AC-2: `GetCaseDocumentJsonAsync(id, dbConfig)` → raw JSON string
- AC-3: `GetCasesPagedAsync(null, int.MaxValue, dbConfig)` — returns `CasePage` with `IReadOnlyList<JObject> documents`; extract `_id` from each document to rebuild the ID set; filter out `_design/*` entries as the original code does

**Finding callers:** Search for `new VROSummary(` to locate instantiation sites that pass `CouchDbHttpClient`.
