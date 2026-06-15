# Story 3.1: Config-Driven OMB Expiration Date

Status: ready-for-dev

## Story

As a developer,
I want the OMB expiration date read from the CouchDB configuration document at render time,
so that the next date change can be applied by running the update script — no code deployment required.

## Acceptance Criteria

1. The CouchDB config document in `database-scripts/` contains a flat `omb_expiration_date` string key under `string_keys.shared` with default value `"05/31/2026"`.
2. The relevant controller action(s) set a TempData/ViewBag entry using: `configuration.GetString("omb_expiration_date", host_prefix) ?? "05/31/2026"` — no helper class, no new service.
3. `Views/Shared/_BurdenStatement.cshtml` renders the OMB date from the TempData/ViewBag entry — the hardcoded string `Exp. Date 05/31/2026` is removed.
4. The `omb_expiration_label.prompt` value in `metadata.json` (in `database-scripts/`) is updated to match the new config-driven value — this patch is applied via the production update script.
5. If the `omb_expiration_date` key is absent from the config document, the hardcoded default `"05/31/2026"` renders correctly.

## Tasks / Subtasks

- [ ] Add `omb_expiration_date` key to CouchDB config document (AC: #1)
  - [ ] Locate the config document source in `source-code/mmria/mmria-server/database-scripts/`
  - [ ] Add `"omb_expiration_date": "05/31/2026"` under `string_keys.shared`
- [ ] Resolve OI-5: identify controller(s) serving the Home page and Committee Decisions form (AC: #2)
  - [ ] Search for the controller action(s) that serve these two surfaces
  - [ ] Confirm whether a single controller action serves both or separate actions are needed
- [ ] Add `GetString` call to controller(s) (AC: #2)
  - [ ] In the identified action(s): `var ombDate = configuration.GetString("omb_expiration_date", host_prefix) ?? "05/31/2026";`
  - [ ] Set as TempData or ViewBag entry following the existing pattern for similar config values (e.g., `metadata_version`, `is_offline_mode_enabled`)
  - [ ] No helper class, no new service — inline only
- [ ] Update `_BurdenStatement.cshtml` (AC: #3)
  - [ ] File: `Views/Shared/_BurdenStatement.cshtml`
  - [ ] Replace hardcoded `Exp. Date 05/31/2026` with the TempData/ViewBag value
  - [ ] Follow the existing path that provides data to this partial — do not introduce a new data-passing mechanism
- [ ] Update `metadata.json` `omb_expiration_label.prompt` (AC: #4)
  - [ ] Locate `omb_expiration_label` in `database-scripts/metadata.json` (or equivalent metadata source)
  - [ ] Update `prompt` value to match the config-driven value
  - [ ] Ensure this is deployable via the production update script
- [ ] Build and verify (AC: #2, #3, #5)
  - [ ] Run `build-server` task — zero errors
  - [ ] Load Home page — confirm OMB date renders from config
  - [ ] Load Committee Decisions form — confirm OMB date renders from config
  - [ ] Temporarily remove key from config doc, reload — confirm default `"05/31/2026"` renders

## Dev Notes

**Files to modify:**
- `source-code/mmria/mmria-server/database-scripts/` — add `omb_expiration_date` key
- Controller(s) serving Home page and Committee Decisions form (OI-5 — identify during implementation)
- `Views/Shared/_BurdenStatement.cshtml`
- `database-scripts/metadata.json` — update `omb_expiration_label.prompt`

**Controller pattern to follow** (architecture §3.2):
```csharp
var ombDate = configuration.GetString("omb_expiration_date", host_prefix) ?? "05/31/2026";
TempData["omb_expiration_date"] = ombDate;  // or ViewBag.OmbDate = ombDate;
```
Follow the exact TempData/ViewBag naming convention already used in the controller for other config values.

**`_BurdenStatement.cshtml` current content:**
```html
<p class="m-0">Exp. Date 05/31/2026</p>
```
Replace `05/31/2026` with the TempData/ViewBag value. Follow the existing pattern for how this partial already receives data from its parent layout/controller.

**`metadata.json` note:** The `omb_expiration_label` field's `prompt` value (`"Exp. Date 05/31/2026"`) is loaded into CouchDB and rendered by the form renderer on the Committee Decisions form. When the OMB date changes in production, the developer updates `omb_expiration_date` in the config doc AND patches this `prompt` value — both via the same production update script run. No client-side render-time substitution is needed for this field.

**OI-5 open item:** Developer identifies which controller action(s) serve the OMB date surfaces. Confirm before writing the controller code.

**Config access pattern** (architecture §2.2):
```csharp
var configuration = tenantRuntime.RequireConfiguration();
configuration.GetString("omb_expiration_date", host_prefix)
```

### Project Structure Notes

- Server-side changes: controller(s) + Razor partial
- No new C# files
- No new NuGet packages

### References

- [Source: architecture-mmria-v4.1.md#3.1 — CouchDB config document additions]
- [Source: architecture-mmria-v4.1.md#3.2 — Controller pattern]
- [Source: architecture-mmria-v4.1.md#3.3 — OMB expiration date render surface]
- [Source: architecture-mmria-v4.1.md#3.5 — Developer update workflow]
- [Source: prd-mmria-2026-06-12/prd.md#FR-3.1]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
