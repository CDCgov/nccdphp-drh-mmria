# Story 26.3 — Export Queue and Broadcast Controllers

**Epic:** 26 — Controller API Direct-Call Remediation
**Story ID:** 26.3
**Status:** ready-for-dev
**Date added:** 2026-07-17
**Depends on:** Epic 23 story 23.4 (IExportQueueRepository), Epic 20 (IMetadataRepository)
**Source requirements:** epics.md §Epic 26 Story 26.3; project-context.md §2.2

---

## User Story

As a developer,
I want export queue and broadcast message controllers to call repository interfaces instead of constructing CouchDB URLs directly,
So that these controller-layer call sites have SQL migration seams.

---

## Acceptance Criteria

**AC-1 — `mmria.services/Controllers/ExportQueueController.cs` call replaced**
Given `ExportQueueController.cs` in mmria.services at approximately line 112 calls `_couchDbHttpClient.ExecuteAsync` to read from the export queue database
When this story is complete
Then that call is replaced with the appropriate `IExportQueueRepository` method; `IExportQueueRepository` is already injected into this controller from Story 24.10

**AC-2 — `mmria.services/Controllers/broadcastMessageController.cs` write replaced**
Given `broadcastMessageController.cs` in mmria.services at approximately line 138 calls `_couchDbHttpClient.ExecuteAsync("PUT", "{url}/{prefix}metadata/broadcast-message-list", ...)` to save the broadcast message list
When this story is complete
Then that PUT call is replaced with `await _metadataRepository.SaveBroadcastMessageListAsync(payload, p_config_detail)` (using the `IMetadataRepository` method that accepts a serialized payload or the typed `BroadcastMessageList`); `IMetadataRepository` is injected via the controller constructor; the per-tenant iteration, audit-field merging, and `existing` document fetch logic stay unchanged

**AC-3 — `source-code/mmria-server/Controllers/broadcast_messageController.cs` confirmed not a CouchDB call — no change**
Given `broadcast_messageController.cs` in mmria-server at approximately line 182 calls `_couchDbHttpClient.ExecuteAsync("POST", "{vitals_url}/api/broadcastMessage/ReplicateMessage", ...)` — a service-to-service POST to the mmria.services HTTP endpoint, not a direct CouchDB write
When this story begins
Then the developer confirms this is a service endpoint call and takes no action; a comment is added inline if absent: `// Service endpoint call — not a direct CouchDB write. No repository routing needed.`

**AC-4 — `mmria-server/Controllers/api/ije_messageController.cs` confirmed not a CouchDB call — no change**
Given `ije_messageController.cs` constructs URLs from `vitals_url` (Delete action: `.../VitalNotification`; Post action: `vitals_url`) and calls `_couchDbHttpClient.ExecuteAsync` as an HTTP transport to mmria.services endpoints
When this story begins
Then the developer confirms both actions target service endpoints (not CouchDB) and takes no action; a comment is added to each call site if absent: `// Service endpoint call — not a direct CouchDB write.`

**AC-5 — `queueController.cs` write assessed**
Given `queueController.cs` in mmria-server at approximately line 78 calls `_couchDbHttpClient.ExecuteAsync("PUT", db_config.url + "/queue/" + queue_item.queue_id, ...)` writing to a `queue` database without a tenant prefix (Pattern A, global database)
When this story begins
Then the developer reads the controller and confirms the target database; if the `queue` database has no existing repository interface: a `IQueueRepository` interface is created in `mmria.common/SharedLibraries/Queue/`, `QueueDAL` is implemented using `{db_config.url}/queue/...` (no prefix, global database), DI registration is added, and the controller call is replaced; the story completion notes document the decision

**AC-6 — Build passes with zero errors**
Given the changes above
When the build runs
Then `mmria-server` and `mmria.services` both build with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `nccdphp-drh-mmria-services/mmria.services/Controllers/ExportQueueController.cs` | **UPDATE** — confirm existing `IExportQueueRepository` injection from 24.10; replace remaining direct call |
| `nccdphp-drh-mmria-services/mmria.services/Controllers/broadcastMessageController.cs` | **UPDATE** — inject `IMetadataRepository`; replace PUT with `SaveBroadcastMessageListAsync` |
| `source-code/mmria/mmria-server/Controllers/broadcast_messageController.cs` | **NO CHANGE** — service endpoint call; add comment |
| `source-code/mmria/mmria-server/Controllers/api/ije_messageController.cs` | **NO CHANGE** — service endpoint calls; add comments |
| `source-code/mmria/mmria-server/Controllers/api/queueController.cs` | **UPDATE or CREATE** — assess `queue` DB; create `IQueueRepository` if needed |

**`IMetadataRepository.SaveBroadcastMessageListAsync` signature:**
```csharp
Task<document_put_response> SaveBroadcastMessageListAsync(string json, DBConfigurationDetail dbConfig);
```
The existing code serializes the `BroadcastMessageList` payload before calling PUT. Pass the serialized JSON string to `SaveBroadcastMessageListAsync`. The `existing` document fetch (needed to preserve `_rev` for the optimistic update) uses `IMetadataRepository.GetBroadcastMessageListAsync(p_config_detail)` — this method also exists.

**`queue` database note:**
The `queue` database at `db_config.url + "/queue/"` uses no tenant prefix. This is a per-deployment global queue, not per-tenant. If `IQueueRepository` needs to be created:
- Interface location: `mmria.common/SharedLibraries/Queue/IQueueRepository.cs`
- DAL: `mmria.common/SharedLibraries/Queue/DAL/QueueDAL.cs`
- URL pattern: `{db_config.url}/queue/{id}` (no prefix — different from Pattern B)
- DI: `services.AddScoped<IQueueRepository, QueueDAL>()` in `mmria-server/Program.cs`
