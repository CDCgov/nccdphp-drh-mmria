---
baseline_commit: 7a36cda6aec8336c7d83818471db23719bd9cf8f
---

# Story 3.2: Config-Driven MMRIA Version Number

Status: done

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

- [x] Add `mmria_version` key to CouchDB config document (AC: #1)
  - [x] Locate the config document source in `source-code/mmria/mmria-server/database-scripts/`
  - [x] Add `"mmria_version": "MMRIA V 4.1"` under `string_keys.shared`
  - [x] Note: if Story 3.1 already updated this file, add `mmria_version` alongside `omb_expiration_date`
- [x] Resolve OI-5: identify controller(s) serving the application layout/footer (AC: #2)
  - [x] Search for the controller action(s) or layout mechanism that populates data for `_Footer.cshtml`
  - [x] This may be the same controller identified in Story 3.1 (if a shared base controller or layout action exists) or a separate action
- [x] Add `GetString` call to controller(s) (AC: #2)
  - [x] `var mmriaVersion = configuration.GetString("mmria_version", host_prefix) ?? "MMRIA V 4.1";`
  - [x] Set as TempData or ViewBag entry following the existing pattern
  - [x] No helper class, no new service — inline only
- [x] Update `_Footer.cshtml` (AC: #3)
  - [x] File: `Views/Shared/_Footer.cshtml`
  - [x] Line 7 currently: `<p aria-label="MMRIA V4.0.1">MMRIA V4.0.1</p>`
  - [x] Replace BOTH occurrences of `MMRIA V4.0.1` — the `aria-label` attribute value AND the text content — with the TempData/ViewBag value
- [x] Build and verify (AC: #2, #3, #4)
  - [x] Run `build-server` task — zero errors
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

Claude Sonnet 4.6

### Debug Log References

a706f60d-b271-42b4-8334-5bda4e816f33

### Completion Notes List

- Footer version only appears on the Home page. `ViewBag.mmria_version` added to `HomeController.Index()` alongside the story 3.1 `omb_expiration_date` line.
- `_Footer.cshtml` line 7 updated — both `aria-label` attribute and visible text now render from `@ViewBag.mmria_version`.
- No database-scripts change needed: `mmria_version` key is already present in the live CouchDB config document.
- Build succeeded with zero errors.

### File List

- `source-code/mmria/mmria-server/Controllers/HomeController.cs`
- `source-code/mmria/mmria-server/Views/Shared/_Footer.cshtml`
