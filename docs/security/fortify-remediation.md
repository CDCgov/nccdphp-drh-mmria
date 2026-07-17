## Scan: 2026-07-17 — mmria services @ 3f892bab1400cac59388b9a5a633ef243006de3b (SSC app version 12317)

- Source issue: CDCgov/nccdphp-drh-mmria#485
- Workflow reference provided in issue: https://github.com/cdcent/nccdphp-od-devops/actions/runs/29609902098

## Finding 1 — Mass Assignment: Request Parameters Bound via Input Formatter at nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:46

**SSC Issue ID:** 2236287
**Rule GUID:** 7AA165DE-F8D9-471F-A1E4-562BB552146D
**Verdict:** Not applicable / false positive

### Evidence
- Source (request binding): `SaveSystemOfflineConfig([FromBody] SaveSystemOfflineConfigRequest request)` in `/nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:46-47`.
- Propagation control (allow-listed DTO): `/nccdphp-drh-mmria-services/mmria.services/Models/SaveSystemOfflineConfigRequest.cs:11-22` defines only permitted client fields and excludes server-managed properties (`_id`, `_rev`, `data_type`).
- Sink guard (server-owned write model): `/nccdphp-drh-mmria-services/mmria.services/Controllers/systemOfflineController.cs:68-80` constructs a new `SystemOfflineConfig` payload from explicit field mapping and forces `_rev` from server state (`existing?._rev`), not client input.

### Verdict rationale
The Fortify path is neutralized in the current codebase because the endpoint does not bind directly to the persisted document type and does not pass client payload directly to persistence. Instead, it bind-limits input to a dedicated request DTO and then rebuilds a server-owned payload through explicit per-field assignment before serialization and save. This eliminates mass-assignment of protected document identity/revision fields.

### SWA Summary
False Positive: request binding is limited to a dedicated allow-listed DTO and the persisted document is reconstructed server-side with explicit field assignment, so protected fields are not mass-assignable from client input.
