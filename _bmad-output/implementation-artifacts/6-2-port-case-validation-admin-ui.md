# Story 6.2: Port Case Validation Admin UI from Branch

Status: draft

## Story

As a form designer,
I want a Case Validation Rule Manager page accessible from the home page,
So that I can view, filter, search, and manage the case validation rules document without editing CouchDB directly.

## Context and Scope

The `v4.1-case-data-validation-mode` branch contains a complete case validation admin UI that was developed as a proof of concept. This story ports all of that work to the current `v4.1-bmad-epic-1` branch. No new features are added — this is a clean port.

**Dependency on Story 6.1:** Story 6.1 removes startup auto-seeding. Story 6.2 should be completed after Story 6.1 because the ported `case_validationController.GetCurrentRules()` endpoint calls `GetOrCreateRuleDocumentAsync`, which will now return an empty document when none exists rather than auto-generating one.

### Files to Port

All files are cherry-picked from `v4.1-case-data-validation-mode`. The exact source content of each file on that branch is authoritative — port as-is unless a conflict with the current branch requires a minimal merge.

#### New Files (do not exist on current branch)

| File | Notes |
|---|---|
| `source-code/mmria/mmria-server/Controllers/case_validation_metadataController.cs` | MVC controller for the admin page |
| `source-code/mmria/mmria-server/Controllers/api/case_validationController.cs` | REST API controller (6 endpoints) |
| `source-code/mmria/mmria-server/Views/case_validation_metadata/Index.cshtml` | Admin UI Razor view (784 lines) |
| `source-code/mmria/mmria-server/wwwroot/scripts/case/case-validation.js` | Admin UI + case validation JS (1779 lines) |

#### Modified Files (merge onto current branch)

| File | Change |
|---|---|
| `source-code/mmria/mmria-server/Views/Home/Index.cshtml` | Add link under Form Designer section |
| `source-code/mmria/mmria-server/database-scripts/case_design_sortable.json` | Minor map function refactors (remove `&& doc.committee_review` null guards in views — unrelated to validation UI but included on the branch) |

#### Files Already Ported (in Story 4.0 — do NOT re-port)

- `CaseValidationManager.cs` — already on current branch; do not overwrite
- `CaseValidationDAL.cs` — already on current branch
- `CaseValidationModels.cs` — already on current branch

### API Endpoints Delivered

The ported `case_validationController` exposes:

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/case-validation/rules/current` | abstractor, data_analyst, form_designer | Returns current rule document |
| GET | `/api/case-validation/rules/current/summary` | form_designer | Returns rule count summary |
| GET | `/api/case-validation/rules/current/export` | form_designer | Returns rule document as JSON file download |
| PUT | `/api/case-validation/rules/{metadata_version}` | form_designer | Saves full rule document |
| POST | `/api/case-validation/rules/preview` | form_designer | Preview rules against a case |
| POST | `/api/case-validation/field` | abstractor | Quick-edit a single field value in a case |

### Home Page Link

The link is added under the **Form Designer** section in `Views/Home/Index.cshtml`, inside the existing "Metadata Management" `<ul>`, after the version manager link:

```html
<li><a href="/case_validation_metadata">Open case validation rule manager</a></li>
```

### What IS NOT Included

- **Add Rule modal** — That is Story 6.3.
- **No changes to CaseValidationManager** — The manager is already on the current branch from Story 4.0. The ported API controller uses it as-is.
- **No new DI registrations** — `CaseValidationDAL` and `CaseValidationManager` are already registered.

## Acceptance Criteria

**AC #1: Admin page accessible**
When a user with the `form_designer` role navigates to `/case_validation_metadata`,
Then the Case Validation Rule Manager page loads without error and displays the rule list (or an empty state if no rules exist).

**AC #2: Home page link present**
When a `form_designer` logs in and views the home page,
Then the Form Designer card contains a link labeled "Open case validation rule manager" that navigates to `/case_validation_metadata`.

**AC #3: Non-form-designer cannot access**
When a user without the `form_designer` role navigates to `/case_validation_metadata`,
Then they receive a 403 Forbidden response (the controller uses `[Authorize(Roles = "form_designer")]`).

**AC #4: GET current rules returns document**
When a user with appropriate role calls `GET /api/case-validation/rules/current`,
Then the endpoint returns the rule document from the database. If no document exists, it returns an empty rule document (no auto-generation — per Story 6.1).

**AC #5: PUT saves updated document**
When a `form_designer` calls `PUT /api/case-validation/rules/{metadata_version}` with a valid rule document body,
Then the document is saved to CouchDB and the endpoint returns `{ ok: true }`.

**AC #6: Export downloads JSON**
When a `form_designer` calls `GET /api/case-validation/rules/current/export`,
Then the response is a JSON file download with `Content-Disposition: attachment; filename="case-validation-rules-{version}.json"`.

**AC #7: Build succeeds**
When `dotnet build` is run on `mmria-server.csproj`,
Then the build completes with 0 errors and 0 warnings introduced by this change.

**AC #8: Design doc changes applied**
When `database-scripts/case_design_sortable.json` is applied,
Then the map functions match the versions from `v4.1-case-data-validation-mode` (null-guard removal for `doc.committee_review.pregnancy_relatedness`).

## Tasks / Subtasks

### Phase 1 — Cherry-pick or copy new files from branch

> The recommended approach is: `git checkout v4.1-case-data-validation-mode -- <file>` for each new file, then selectively revert or merge any conflicts with current branch state.

- [ ] **Port `case_validation_metadataController.cs`**:
  ```
  git checkout v4.1-case-data-validation-mode -- source-code/mmria/mmria-server/Controllers/case_validation_metadataController.cs
  ```
  Verify it compiles — the controller references `tenantRuntime.RequireConfiguration()` and `tenantRuntime.EffectiveHostPrefix`, which are already available on the current branch.

- [ ] **Port `case_validationController.cs`**:
  ```
  git checkout v4.1-case-data-validation-mode -- source-code/mmria/mmria-server/Controllers/api/case_validationController.cs
  ```
  Verify all referenced types and methods exist: `CaseValidationManager`, `MetadataVersionManager`, `RequestTenantRuntime`, `JsonRequestBodyReader`, `CaseValidationRulePreviewRequest`, `CaseValidationFieldUpdateRequest`. These should all be present from Story 4.0 and existing infrastructure.

- [ ] **Port `Views/case_validation_metadata/Index.cshtml`**:
  ```
  git checkout v4.1-case-data-validation-mode -- source-code/mmria/mmria-server/Views/case_validation_metadata/Index.cshtml
  ```
  Verify the view builds (check for any `@using` directives or Razor helpers that need to be available).

- [ ] **Port `wwwroot/scripts/case/case-validation.js`**:
  ```
  git checkout v4.1-case-data-validation-mode -- source-code/mmria/mmria-server/wwwroot/scripts/case/case-validation.js
  ```
  This file is referenced by `<script src="/scripts/case/case-validation.js">` in `Index.cshtml`. Verify it doesn't conflict with any existing JS that the case editor depends on (it's a new file — the current branch does not have it).

### Phase 2 — Merge modified files

- [ ] **Add home page link in `Views/Home/Index.cshtml`**:
  Locate the Form Designer section. Inside the "Metadata Management" `<ul>` (which already has `/editor`, `/form-designer`, `/version-manager`), add after the version-manager line:
  ```html
  <li><a href="/case_validation_metadata">Open case validation rule manager</a></li>
  ```

- [ ] **Update `database-scripts/case_design_sortable.json`**:
  ```
  git checkout v4.1-case-data-validation-mode -- source-code/mmria/mmria-server/database-scripts/case_design_sortable.json
  ```
  Verify the file applies cleanly. These changes remove `doc.committee_review &&` null-guards from map functions — not related to validation UI but part of the branch diff.

### Phase 3 — Verify no conflicts with existing Story 4.0 code

- [ ] Confirm `CaseValidationManager` on the current branch is NOT overwritten by this port — only the controller and views are new additions.
- [ ] Confirm `CaseController.cs` is not changed — it already delivers `TempData["validation_rules"]` to the case editor via a different endpoint path.
- [ ] Confirm the new `case_validationController` route (`/api/case-validation/...`) does not conflict with the existing `api/validation_rules` endpoint.

### Phase 4 — Build and smoke test

- [ ] Run `dotnet build mmria-server.csproj` — 0 errors.
- [ ] Start the server, log in as `form_designer`.
- [ ] Navigate to the home page — confirm link is present.
- [ ] Navigate to `/case_validation_metadata` — confirm page loads.
- [ ] Call `GET /api/case-validation/rules/current` — confirm it returns a valid JSON response.
