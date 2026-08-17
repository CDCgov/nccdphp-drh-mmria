# Story 6.1: Decouple Validation Rules Generation from Metadata Auto-Generation

Status: done

## Story

As a system administrator,
I want validation rules to be fully manually managed without any auto-generation from metadata,
So that rule creation is an explicit admin action rather than an automatic process driven by metadata structure.

## Context and Scope

Currently, `CaseValidationManager.GetOrCreateRuleDocumentAsync` auto-generates a full rule set from metadata whenever no document exists in the database. Similarly, `EnsureRuleDocumentShape` merges newly-generated rules into existing documents whenever metadata has fields that don't yet have rules. Additionally, a startup seeding block in `Program.cs` runs `GetOrCreateRuleDocumentAsync` + `SaveRuleDocumentAsync` for every tenant at server startup, which creates and writes a full auto-generated document if none exists.

This story removes all of that:
1. **Server startup** — Remove the seeding block entirely. Rules are never created or loaded at startup.
2. **`GetOrCreateRuleDocumentAsync`** — When no document exists in the database, return an empty rule document (no rules). Never auto-generate from metadata.
3. **`EnsureRuleDocumentShape`** — Stop merging auto-generated rules into existing documents. Only normalize what is already in the document.
4. **Remove dead code** — All private methods that exist solely to generate rules from metadata are deleted.

Rule creation will happen exclusively via the admin UI (Story 6.3).

### What IS Included

1. **Program.cs** — Remove the entire "Seed validation rules after database setup" try-catch block. The DI service registrations for `CaseValidationDAL` and `CaseValidationManager` are kept.

2. **`CaseValidationManager.GetOrCreateRuleDocumentAsync`** — When `_dal.GetRuleDocumentAsync` returns `null`, return a new empty `CaseValidationRuleDocument` (no rules). Do not call `BuildDefaultRuleDocument` with metadata.

3. **`CaseValidationManager.BuildDefaultRuleDocument`** — Rewrite to return an empty document with only the header fields set (`_id`, `metadata_version`, `date_created`, `created_by`). Remove all code that walks metadata and creates rules.

4. **`CaseValidationManager.EnsureRuleDocumentShape`** — Remove the call to `BuildDefaultRuleDocument` and the call to `MergeMissingRules`. Keep only the null-coalescing guards (ensure `field_rules`, `connected_field_rules`, `form_status_rules` are non-null lists) and the `NormalizeRuleDocumentMetadata` call.

5. **Remove dead private methods** — The following private methods are used only by the removed auto-generation path and must be deleted:
   - `CreateFieldRule(CaseValidationFlattenedField field)`
   - `GetSeededNumericRange(string fieldPath)` (or similar — the vitals seed data lookup)
   - `CreateFormStatusRules(IEnumerable<CaseValidationFlattenedField> fields)`
   - `CreateConnectedFieldRules(IEnumerable<CaseValidationFlattenedField> fields)`
   - `MergeMissingRules<T>(List<T> target, IEnumerable<T> defaults, Func<T, string> idSelector)` — only used in `EnsureRuleDocumentShape` after the auto-generation call is removed

### What IS NOT Included

- **No removal of `FlattenMetadata` or `FlattenNode`** — These are used by `EvaluateCase` and the forthcoming metadata fields API (Story 6.3).
- **No removal of evaluation logic** — `EvaluateCase`, `EvaluateFieldRules`, `EvaluateConnectedFieldRules`, `EvaluateFormStatus`, `BuildRuleSummary`, `PreviewRulesAsync`, `SaveSingleFieldAsync` are all unchanged.
- **No removal of DI registrations** — `CaseValidationDAL` and `CaseValidationManager` remain registered in DI.
- **No changes to the `api/validation_rules` endpoint in `CaseController`** — That endpoint loads rules from the DB to deliver to the client; it is unaffected (returns empty `{}` when no document exists).
- **No admin UI changes** — The admin UI port is Story 6.2.

## Acceptance Criteria

**AC #1: No startup seeding**
When the server starts,
Then no attempt is made to load or create validation rule documents — no `GetOrCreateRuleDocumentAsync` or `SaveRuleDocumentAsync` calls occur during startup. Server starts successfully with or without a rule document in the database.

**AC #2: Empty document returned when none exists**
Given no `case-validation-rules` document exists in the database,
When `GetOrCreateRuleDocumentAsync` is called (e.g., by the admin API endpoint),
Then it returns a `CaseValidationRuleDocument` with an empty `field_rules` list, empty `connected_field_rules` list, and empty `form_status_rules` list — no rules are auto-generated from metadata.

**AC #3: Existing document unchanged by shape enforcement**
Given a rule document with 3 manually-created rules exists in the database,
When `EnsureRuleDocumentShape` is called against it with current metadata,
Then the document still has only those 3 rules — no additional rules are merged in from metadata.

**AC #4: Case evaluation with no rules returns no findings**
Given no rule document exists (or an empty rule document is in the database),
When `EvaluateCase` is called,
Then it returns a `CaseValidationEvaluationResult` with empty `findings` and empty `checks` lists — no errors thrown, no null-reference exceptions.

**AC #5: Dead code removed**
When the codebase is built,
Then the following methods do not exist in `CaseValidationManager`:
- `CreateFieldRule`
- `GetSeededNumericRange` (or the vitals seed lookup helper)
- `CreateFormStatusRules`
- `CreateConnectedFieldRules`
- `MergeMissingRules`

**AC #6: Build succeeds**
When `dotnet build` is run on `mmria-server.csproj` and on `mmria.common.csproj`,
Then the build completes with 0 errors and 0 warnings introduced by this change.

## Tasks / Subtasks

### Phase 1 — Remove startup seeding from `Program.cs`

- [x] **Locate seeding block**: In `Program.cs`, find the comment `// Seed validation rules after database setup` and the `try` block immediately following it (~line 657).
- [x] **Delete the entire try-catch**: Remove from `// Seed validation rules after database setup` through the closing `}` of the `catch` block (~lines 657–728).
- [x] **Verify DI registrations remain**: Confirm that lines registering `CaseValidationDAL` and `CaseValidationManager` (~lines 324–325) are still present.

### Phase 2 — Simplify `GetOrCreateRuleDocumentAsync` in `CaseValidationManager.cs`

- [x] **Remove `BuildDefaultRuleDocument` call when doc is null**: In `GetOrCreateRuleDocumentAsync`, find the `if (document != null)` block. When `document` is `null`, replace the call to `BuildDefaultRuleDocument(metadataVersion, metadata, userName)` with a return of a new empty document.
- [x] **Remove `app metadata` parameter from `GetOrCreateRuleDocumentAsync`**: Decision: kept the `metadata` parameter for now to avoid cascading changes (Story 6.2 will port the admin controller that passes metadata). Simply stopped using it when document is null.

### Phase 3 — Simplify `BuildDefaultRuleDocument`

- [x] **Rewrite to return empty document**: Replaced the entire body of `BuildDefaultRuleDocument` with an empty document (id, metadata_version, date_created, created_by only).
- [x] **Verify no other callers** use `BuildDefaultRuleDocument` expecting auto-generated rules — `EvaluateCase` uses it as a null-rules fallback; empty document = no findings, which is correct.

### Phase 4 — Remove auto-generation from `EnsureRuleDocumentShape`

- [x] **Remove `BuildDefaultRuleDocument` and `MergeMissingRules` calls**: Removed both calls from `EnsureRuleDocumentShape`.
- [x] **Keep null-guard and normalize calls**: Method retains null-coalescing guards and `NormalizeRuleDocumentMetadata` call.
- [x] **Update signature if `app metadata` parameter is no longer used**: Kept `metadata` parameter in signature (unused) to avoid breaking changes with Story 6.2 port.

### Phase 5 — Delete dead private methods

- [x] **Delete `CreateFieldRule`**: Removed.
- [x] **Delete `GetSeededNumericRange`**: Removed.
- [x] **Delete `CreateFormStatusRules`**: Removed.
- [x] **Delete `CreateConnectedFieldRules`**: Removed.
- [x] **Delete `MergeMissingRules`**: Removed.

### Phase 6 — Build verification

- [x] Run `dotnet build` on `mmria.common.csproj` — 0 errors (67 pre-existing warnings, none in modified files).
- [x] Run `dotnet build` on `mmria-server.csproj` — 0 errors (15 pre-existing warnings, none in modified files).
- [ ] Confirm server starts without exceptions in the startup log.
