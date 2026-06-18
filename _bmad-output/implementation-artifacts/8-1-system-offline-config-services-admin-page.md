# Story 8.1: System Offline Config — Document, mmria-services, Controller, and Admin Page

Status: not-started

## Story

As an installation administrator,
I want a dedicated admin page where I can configure warn and offline dates and messages,
so that I can schedule a planned outage and control the messaging users see at each stage.

## Acceptance Criteria

1. A `SystemOfflineConfig` model class exists in `mmria.common.metadata` with properties: `string _id = "system-offline-config"`, `string _rev`, `string warn_date`, `string warn_message`, `string offline_date`, `string offline_modal_message`, `string offline_page_message`, `string data_type = "system_offline_config"`.
2. A GET endpoint on mmria-services returns the `system-offline-config` document from the CDC instance `metadata` CouchDB database. If the document does not exist, it returns an empty `SystemOfflineConfig` (all fields null/default). Follows the same pattern as `broadcastMessageController` in mmria-services.
3. A POST/PUT endpoint on mmria-services writes the `system-offline-config` document to the CDC instance `metadata` database. Uses `CouchDbRevisionHelper` for revision handling — same pattern as `broadcastMessageController.ReplicateMessage`.
4. A mmria-server controller at route `/system-offline` with actions:
   - `Index` (GET) — serves the admin Razor view; restricted to `installation_admin` role.
   - `GetConfig` (GET) — calls mmria-services GET endpoint; returns JSON config to the client.
   - `SaveConfig` (POST) — reads request body as `SystemOfflineConfig`; sanitizes (strips `_rev`, audit fields from client payload); calls mmria-services POST endpoint; returns save result.
5. An additional mmria-server GET endpoint at `/api/system-offline/status` (no role restriction, auth required) returns the current `warn_date`, `offline_date`, `warn_message`, `offline_modal_message`, `offline_page_message` — used by the periodic check (Story 8.4) and the post-login check (Story 8.3).
6. The admin Razor view (`Views/system_offline/Index.cshtml`) presents: two datetime inputs (`warn_date`, `offline_date`) and three `<textarea>` inputs (`warn_message`, `offline_modal_message`, `offline_page_message`), pre-populated on load via `GetConfig`. A Save button POSTs to `SaveConfig`. Follows the layout and styling of `Views/broadcast_message/Index.cshtml`.
7. A link to `/system-offline` appears in the installation admin navigation section alongside the broadcast-message link. Restricted to `installation_admin` role.
8. Build succeeds with zero errors.

## Tasks / Subtasks

- [ ] Add `SystemOfflineConfig` model to `mmria.common.metadata` (AC: #1)
  - [ ] New file: `nccdphp-drh-mmria-common/mmria.common/metadata/SystemOfflineConfig.cs`
  - [ ] Properties: `_id` (read-only `"system-offline-config"`), `_rev`, `warn_date`, `warn_message`, `offline_date`, `offline_modal_message`, `offline_page_message`, `data_type` (read-only `"system_offline_config"`)
- [ ] Add mmria-services endpoint (AC: #2, #3)
  - [ ] New controller: `nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs`
  - [ ] Route: `api/[controller]/[action]`; `[Authorize(AuthenticationSchemes = "BasicAuthentication")]`
  - [ ] `GetSystemOfflineConfig` (HttpGet): reads `{metadata_db_url}/system-offline-config`, returns `SystemOfflineConfig` (empty if 404)
  - [ ] `SaveSystemOfflineConfig` (HttpPost): writes to `{metadata_db_url}/system-offline-config` using `_couchDbHttpClient`; handles `_rev` via `CouchDbRevisionHelper`
  - [ ] Follow the same `ConfigDB` / `_couchDbHttpClient` injection pattern as `broadcastMessageController` in mmria-services
- [ ] Add mmria-server controller (AC: #4, #5)
  - [ ] New file: `source-code/mmria/mmria-server/Controllers/system_offlineController.cs`
  - [ ] Route: `[Route("system-offline/{action=Index}")]`
  - [ ] `Index` action: `[Authorize(Roles = "installation_admin")]`; returns `View()`
  - [ ] `GetConfig` (HttpGet): calls mmria-services `GetSystemOfflineConfig`; returns result as JSON
  - [ ] `SaveConfig` (HttpPost): reads body as `SystemOfflineConfig`; sanitizes (discard `_rev` from client, populate server-side); calls mmria-services `SaveSystemOfflineConfig`; returns result
  - [ ] Follow the `broadcast_messageController` pattern for calling mmria-services (ActorSystem message or direct HTTP call — check existing pattern)
  - [ ] `/api/system-offline/status` endpoint: `[Authorize]`; no role restriction; returns current config JSON (calls mmria-services or uses cached value)
- [ ] Add admin Razor view (AC: #6)
  - [ ] New file: `source-code/mmria/mmria-server/Views/system_offline/Index.cshtml`
  - [ ] Two `<input type="datetime-local">` for `warn_date` and `offline_date`
  - [ ] Three `<textarea>` for `warn_message`, `offline_modal_message`, `offline_page_message`
  - [ ] On load: fetch `/system-offline/GetConfig`, populate form
  - [ ] Save button: POST to `/system-offline/SaveConfig`, show success/error feedback
  - [ ] Match layout of `Views/broadcast_message/Index.cshtml`
- [ ] Add navigation link (AC: #7)
  - [ ] Locate the nav section where the broadcast-message link appears
  - [ ] Add a link to `/system-offline` with label "System Offline" in the same section
  - [ ] Restrict to `installation_admin` role using the existing conditional rendering pattern
- [ ] Build and verify (AC: #8)
  - [ ] Run `build-server` task and `build-services` task — zero errors
  - [ ] Navigate to `/system-offline` as installation_admin — confirm form loads
  - [ ] Save a config — confirm document appears in CDC instance `metadata` DB

## Dev Notes

**Model location:** Follow `mmria.common.metadata.BroadcastMessage` pattern. New file in `nccdphp-drh-mmria-common/mmria.common/metadata/SystemOfflineConfig.cs`.

**mmria-services pattern:** Study `nccdphp-drh-mmria-services/mmria.services/Controllers/broadcastMessageController.cs`:
- `ConfigDB` (type `mmria.common.couchdb.ConfigurationSet`) is injected
- `_couchDbHttpClient` (type `mmria.common.getset.CouchDbHttpClient`) is injected
- CDC instance metadata DB URL: `$"{ConfigDB.name_value["cdc_url"]}/metadata/system-offline-config"` (confirm the exact key for the CDC CouchDB URL from existing usages in broadcastMessageController)
- Revision handling: use `CouchDbRevisionHelper.DescribeRevisionHandling` and `CouchDbRevisionHelper.ResolveServerOwnedRevision`

**mmria-server → mmria-services call pattern:** Study how `broadcast_messageController.cs` in mmria-server calls mmria-services (look for HTTP client or ActorSystem message pattern). Follow the same mechanism.

**Sanitization in SaveConfig:** Discard any client-supplied `_rev`, `data_type`. Never trust revision or type from the request body — read them server-side from the existing document via GetConfig before writing.

**`/api/system-offline/status` endpoint:** This is the polling endpoint for Story 8.4 and the login check for Stories 8.2 and 8.3. It should return the same shape as the full config but is accessible to any authenticated user (no admin role required). Consider caching the config in-memory after the first load (similar to `vital_sign_range` pattern) to avoid a mmria-services round-trip on every 2-minute poll from all users.

**Navigation link location:** Search Views for `broadcast-message` link to find the exact partial/layout where it lives.

### Project Structure Notes

- New files: `SystemOfflineConfig.cs` (common), `systemOfflineController.cs` (mmria-services), `system_offlineController.cs` (mmria-server), `Views/system_offline/Index.cshtml`
- Modified files: navigation partial (add link)
- No new NuGet packages

### References

- [Source: nccdphp-drh-mmria-services/mmria.services/Controllers/broadcastMessageController.cs]
- [Source: source-code/mmria/mmria-server/Controllers/broadcast_messageController.cs]
- [Source: nccdphp-drh-mmria-common/mmria.common/metadata/BroadcastMessage.cs]
- [Source: prd-mmria-2026-06-12/prd.md#FR-8.1, FR-8.7]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
- `nccdphp-drh-mmria-common/mmria.common/metadata/SystemOfflineConfig.cs` (new)
- `nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs` (new)
- `source-code/mmria/mmria-server/Controllers/system_offlineController.cs` (new)
- `source-code/mmria/mmria-server/Views/system_offline/Index.cshtml` (new)
- Navigation partial containing broadcast-message link (modified)
