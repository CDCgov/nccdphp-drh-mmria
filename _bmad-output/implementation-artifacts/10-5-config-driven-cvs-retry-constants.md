---
status: done
---

# Story 10.5 — Config-Driven CVS Retry Constants

**Epic:** 10 — CVS PDF Export Tool Reliability
**Story ID:** 10.5
**Status:** not-started
**Date added:** 2026-07-06

---

## User Story

As a system administrator,
I want the CVS retry attempt count and delay interval to be configurable via the CouchDB configuration document,
So that these values can be tuned per environment without a code deployment.

---

## Acceptance Criteria

**AC-1 — CouchDB config document seeds the new integer keys**
Given the CouchDB configuration document (seeded via `database-scripts/`)
When the document is applied
Then `integer_keys.shared` contains:
```json
"CVS_MAX_ATTEMPTS": 10,
"CVS_RETRY_DELAY_SECONDS": 60
```
And these values serve as the system-wide defaults

**AC-2 — Controller reads the values and sets TempData**
Given `CvsController.Index()` executes
When the view is served
Then the action reads both values from configuration:
```csharp
TempData["CVS_MAX_ATTEMPTS"] = configuration.GetInteger("CVS_MAX_ATTEMPTS", host_prefix) ?? 10;
TempData["CVS_RETRY_DELAY_SECONDS"] = configuration.GetInteger("CVS_RETRY_DELAY_SECONDS", host_prefix) ?? 60;
```
And the fallback defaults (10 and 60) are used if the keys are absent from the config document

**AC-3 — View emits the values as `window` globals before `cvs/index.js` loads**
Given `Views/cvs/Index.cshtml` renders
When the `<head>` is emitted
Then a Razor `@{ }` block reads the TempData values:
```csharp
var cvs_max_attempts = TempData["CVS_MAX_ATTEMPTS"] ?? 10;
var cvs_retry_delay_seconds = TempData["CVS_RETRY_DELAY_SECONDS"] ?? 60;
```
And an inline `<script>` block placed **before** the `cvs/index.js` `<script src>` tag emits:
```javascript
window.CVS_MAX_ATTEMPTS = @cvs_max_attempts;
window.CVS_RETRY_DELAY_SECONDS = @cvs_retry_delay_seconds;
```

**AC-4 — `cvs/index.js` reads from `window` globals with fallback**
Given `cvs/index.js` loads
When the module-level constants are evaluated
Then the hardcoded values are replaced with:
```javascript
const CVS_MAX_ATTEMPTS = window.CVS_MAX_ATTEMPTS ?? 10;
const CVS_RETRY_DELAY_SECONDS = window.CVS_RETRY_DELAY_SECONDS ?? 60;
```
And all retry logic in the module continues to reference `CVS_MAX_ATTEMPTS` and `CVS_RETRY_DELAY_SECONDS` without any other change

**AC-5 — No dedicated helper class is introduced**
Given the implementation
When reviewed
Then no new helper class, service, or utility method is created for these two values
And the `configuration.GetInteger(key, host_prefix) ?? default` pattern is used inline in `CvsController.Index()` exactly as it is used in `CaseController.cs` (e.g., `case_edit_auto_save_freq`)

**AC-6 — Tenant-level override is supported automatically**
Given a tenant's config document has `integer_keys.{tenant_prefix}.CVS_MAX_ATTEMPTS = 5`
When `configuration.GetInteger("CVS_MAX_ATTEMPTS", host_prefix)` is called
Then the tenant-specific value `5` is returned (existing `GetInteger` lookup order — tenant prefix first, then shared)
And no additional code is required to support this

---

## Dev Notes — Implementation

### Pattern reference

Follow `CaseController.cs` lines 74 and 50–51 exactly:

```csharp
// CaseController.cs (reference — do not modify)
TempData["case_edit_auto_save_freq"] = configuration.GetInteger("case_edit_auto_save_freq", host_prefix) ?? 2;
```

and `Views/Case/Index.cshtml` lines 52, 65–66:

```csharp
@{
    var case_edit_auto_save_freq = TempData["case_edit_auto_save_freq"];
}
<script>
    window.case_edit_inactivity_config = {
        ...
        auto_save_freq: @case_edit_auto_save_freq,
        ...
    };
</script>
```

### File 1: `source-code/mmria/mmria-server/Controllers/cvsController.cs`

In `Index(...)`, before `return View(model)`, add:

```csharp
TempData["CVS_MAX_ATTEMPTS"] = configuration.GetInteger("CVS_MAX_ATTEMPTS", host_prefix) ?? 10;
TempData["CVS_RETRY_DELAY_SECONDS"] = configuration.GetInteger("CVS_RETRY_DELAY_SECONDS", host_prefix) ?? 60;
```

### File 2: `source-code/mmria/mmria-server/Views/cvs/Index.cshtml`

In the existing `@{ ... }` block at the top of the file, add:

```csharp
var cvs_max_attempts = TempData["CVS_MAX_ATTEMPTS"] ?? 10;
var cvs_retry_delay_seconds = TempData["CVS_RETRY_DELAY_SECONDS"] ?? 60;
```

Then add an inline `<script>` block in `<head>` **immediately before** the `<script src="../scripts/cvs/index.js" ...>` tag:

```html
<script>
    window.CVS_MAX_ATTEMPTS = @cvs_max_attempts;
    window.CVS_RETRY_DELAY_SECONDS = @cvs_retry_delay_seconds;
</script>
```

### File 3: `source-code/mmria/mmria-server/wwwroot/scripts/cvs/index.js`

Replace the two hardcoded constants (lines 10–11 of the branch diff, near the top of the file):

```javascript
// BEFORE
const CVS_MAX_ATTEMPTS = 10;
const CVS_RETRY_DELAY_SECONDS = 30;

// AFTER
const CVS_MAX_ATTEMPTS = window.CVS_MAX_ATTEMPTS ?? 10;
const CVS_RETRY_DELAY_SECONDS = window.CVS_RETRY_DELAY_SECONDS ?? 60;
```

> Note: the default for `CVS_RETRY_DELAY_SECONDS` changes from 30 (branch value) to 60 (PRD-specified default). If the CouchDB config key is absent the fallback will be 60 seconds. The window global emitted by the view will be 60 unless overridden in the DB.

### File 4: `source-code/mmria/mmria-server/database-scripts/` (config document)

Add both keys to `integer_keys.shared` in the configuration document. Follow the same approach used for all other integer config keys. Developer locates the correct document and update path by grepping for an existing `integer_keys.shared` key (e.g., `case_edit_auto_save_freq` or `session_idle_timeout_minutes`) in the database-scripts seeding path.

```json
"integer_keys": {
  "shared": {
    ...existing keys...,
    "CVS_MAX_ATTEMPTS": 10,
    "CVS_RETRY_DELAY_SECONDS": 60
  }
}
```

### Files Changed

| File | Change |
|------|--------|
| `source-code/mmria/mmria-server/Controllers/cvsController.cs` | Add two `TempData` assignments in `Index()` before `return View(model)` |
| `source-code/mmria/mmria-server/Views/cvs/Index.cshtml` | Add two TempData reads in `@{ }` block; add inline `<script>` with `window.CVS_MAX_ATTEMPTS` and `window.CVS_RETRY_DELAY_SECONDS` before `cvs/index.js` script tag |
| `source-code/mmria/mmria-server/wwwroot/scripts/cvs/index.js` | Replace two hardcoded `const` values with `window.X ?? default` pattern |
| `source-code/mmria/mmria-server/database-scripts/` (config doc) | Add `CVS_MAX_ATTEMPTS: 10` and `CVS_RETRY_DELAY_SECONDS: 60` to `integer_keys.shared` |

### Sequencing

Depends on Story 10.3 — the two constants being made configurable are defined there. Story 10.5 must be merged after 10.3.
