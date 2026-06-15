# Story 3.2: Config-Driven MMRIA Version Number

Status: ready-for-dev

## Story

As a developer,
I want the MMRIA version number read from the CouchDB configuration document at render time,
so that the next version change can be applied by running the update script — no code deployment required.

## Acceptance Criteria

1. The CouchDB config document in `database-scripts/` contains a flat `mmria_version` string key under `string_keys.shared` with default value `"MMRIA V 4.1"`.
2. The relevant controller action(s) set a TempData/ViewBag entry using: `configuration.GetString("mmria_version", host_prefix) ?? "MMRIA V 4.1"` — no helper class, no new service.
3. `Views/Shared/_Footer.cshtml` renders the version from the TempData/ViewBag entry — both occurrences of the hardcoded `MMRIA V4.0.1` string (aria-label and text content) are replaced.
4. If the `mmria_version` key is absent from the config document, the hardcoded default `"MMRIA V 4.1"` renders correctly.

## Tasks / Subtasks

- [ ] Add `mmria_version` key to CouchDB config document (AC: #1)
  - [ ] Locate the config document source in `source-code/mmria/mmria-server/database-scripts/`
  - [ ] Add `"mmria_version": "MMRIA V 4.1"` under `string_keys.shared`
  - [ ] Note: if Story 3.1 already updated this file, add `mmria_version` alongside `omb_expiration_date`
- [ ] Resolve OI-5: identify controller(s) serving the application layout/footer (AC: #2)
  - [ ] Search for the controller action(s) or layout mechanism that populates data for `_Footer.cshtml`
  - [ ] This may be the same controller identified in Story 3.1 (if a shared base controller or layout action exists) or a separate action
- [ ] Add `GetString` call to controller(s) (AC: #2)
  - [ ] `var mmriaVersion = configuration.GetString("mmria_version", host_prefix) ?? "MMRIA V 4.1";`
  - [ ] Set as TempData or ViewBag entry following the existing pattern
  - [ ] No helper class, no new service — inline only
- [ ] Update `_Footer.cshtml` (AC: #3)
  - [ ] File: `Views/Shared/_Footer.cshtml`
  - [ ] Line 7 currently: `<p aria-label="MMRIA V4.0.1">MMRIA V4.0.1</p>`
  - [ ] Replace BOTH occurrences of `MMRIA V4.0.1` — the `aria-label` attribute value AND the text content — with the TempData/ViewBag value
- [ ] Build and verify (AC: #2, #3, #4)
  - [ ] Run `build-server` task — zero errors
  - [ ] Load any page with the footer — confirm version renders from config
  - [ ] Inspect DOM: confirm both `aria-label` and visible text show the config value
  - [ ] Temporarily remove key from config doc, reload — confirm default `"MMRIA V 4.1"` renders

## Dev Notes

**Files to modify:**
- `source-code/mmria/mmria-server/database-scripts/` — add `mmria_version` key
- Controller(s) serving the layout with `_Footer.cshtml` (OI-5 — identify during implementation)
- `Views/Shared/_Footer.cshtml`

**`_Footer.cshtml` current line 7:**
```html
<p aria-label="MMRIA V4.0.1">MMRIA V4.0.1</p>
```
Both hardcoded strings must be replaced — the `aria-label` attribute and the visible text content.

**Controller pattern to follow** (architecture §3.2):
```csharp
var mmriaVersion = configuration.GetString("mmria_version", host_prefix) ?? "MMRIA V 4.1";
TempData["mmria_version"] = mmriaVersion;  // or ViewBag.MmriaVersion = mmriaVersion;
```

**OI-5 coordination with Story 3.1:** If Story 3.1 has been completed and OI-5 was resolved there, the controller and layout context are already identified. Check Story 3.1 dev agent completion notes before starting this story. The `mmria_version` line may be addable to the same controller location.

**Config access pattern** (architecture §2.2):
```csharp
var configuration = tenantRuntime.RequireConfiguration();
configuration.GetString("mmria_version", host_prefix)
```

**No metadata.json patch needed** for the version — unlike the OMB date, there is no corresponding metadata field for the version string.

### Project Structure Notes

- Server-side changes: controller(s) + Razor partial
- No new C# files
- No new NuGet packages

### References

- [Source: architecture-mmria-v4.1.md#3.1 — CouchDB config document additions]
- [Source: architecture-mmria-v4.1.md#3.2 — Controller pattern]
- [Source: architecture-mmria-v4.1.md#3.4 — MMRIA version render surface (_Footer.cshtml)]
- [Source: prd-mmria-2026-06-12/prd.md#FR-3.2]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
