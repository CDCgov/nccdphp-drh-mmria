# MMRIA V4.1 — Story Index

Start a new chat thread for each story. Use the prompt shown to invoke the dev agent.

---

## Epic 1: Case Narrative Editor Fidelity

| Story                                      | File                                            | Status       |
| ------------------------------------------ | ----------------------------------------------- | ------------ |
| 1.1 Fix Save-Path HTML Stripping           | `1-1-fix-save-path-html-stripping.md`           | verification |
| 1.2 Fix Paste Handler Cursor Integrity     | `1-2-fix-paste-handler-cursor-integrity.md`     | verification |
| 1.3 Update Case Narrative Instruction Text | `1-3-update-case-narrative-instruction-text.md` | verification |

**Story 1.1 prompt:**

```
dev this story _bmad-output/implementation-artifacts/1-1-fix-save-path-html-stripping.md
```

**Story 1.2 prompt:**

```
dev this story _bmad-output/implementation-artifacts/1-2-fix-paste-handler-cursor-integrity.md
```

**Story 1.3 prompt:**

```
dev this story _bmad-output/implementation-artifacts/1-3-update-case-narrative-instruction-text.md
```

---

## Epic 2: Vitals Field Validation

| Story                                                    | File                                                    | Status       |
| -------------------------------------------------------- | ------------------------------------------------------- | ------------ |
| 2.1 Add Vitals Range Config — CouchDB and Server Loading | `2-1-vitals-range-config-couchdb-and-server-loading.md` | verification |
| 2.2 On-Blur Vitals Validation and Invalid Entry Modal    | `2-2-on-blur-vitals-validation-and-modal.md`            | verification |
| 2.3 Display-Time Exclusion — Print, PDF, Date Fix        | `2-3-display-time-exclusion-print-pdf-date-fix.md`      | verification |
| 2.4 Display-Time Exclusion — Graph and Table Views       | `2-4-display-time-exclusion-graph-and-table.md`         | verification |
| 2.5 Historical Data Detection and Record Indicators      | `2-5-historical-data-detection-and-indicators.md`       | verification |
| 2.6 Vitals Validation Bug Fixes — Data Retention and Prenatal Coverage | `2-6-vitals-validation-bug-fixes.md` | draft |

> ⚠️ **Epic 2 sequencing:** Story 2.1 must be completed before 2.2–2.5. Stories 2.2–2.5 can be worked in any order after 2.1. Story 2.6 depends on 2.2 and 2.4 being complete.

**Story 2.1 prompt:**

```
dev this story _bmad-output/implementation-artifacts/2-1-vitals-range-config-couchdb-and-server-loading.md
```

**Story 2.2 prompt:**

```
dev this story _bmad-output/implementation-artifacts/2-2-on-blur-vitals-validation-and-modal.md
```

**Story 2.3 prompt:**

```
dev this story _bmad-output/implementation-artifacts/2-3-display-time-exclusion-print-pdf-date-fix.md
```

**Story 2.4 prompt:**

```
dev this story _bmad-output/implementation-artifacts/2-4-display-time-exclusion-graph-and-table.md
```

**Story 2.5 prompt:**

```
dev this story _bmad-output/implementation-artifacts/2-5-historical-data-detection-and-indicators.md
```

**Story 2.6 prompt:**

```
dev this story _bmad-output/implementation-artifacts/2-6-vitals-validation-bug-fixes.md
```

---

## Epic 3: System Configuration & Print Cleanup

| Story                                      | File                                       | Status       |
| ------------------------------------------ | ------------------------------------------ | ------------ |
| 3.1 Config-Driven OMB Expiration Date      | `3-1-config-driven-omb-expiration-date.md` | verification |
| 3.2 Config-Driven MMRIA Version Number     | `3-2-config-driven-mmria-version.md`       | verification |
| 3.3 Remove Core Elements Only Print Option | `3-3-remove-core-elements-print-option.md` | verification |

> ℹ️ Stories 3.1, 3.2, and 3.3 are independent — any order.
> ℹ️ Both 3.1 and 3.2 touch the same CouchDB config document in `database-scripts/`. If worked simultaneously, coordinate on that file.

**Story 3.1 prompt:**

```
dev this story _bmad-output/implementation-artifacts/3-1-config-driven-omb-expiration-date.md
```

**Story 3.2 prompt:**

```
dev this story _bmad-output/implementation-artifacts/3-2-config-driven-mmria-version.md
```

**Story 3.3 prompt:**

```
dev this story _bmad-output/implementation-artifacts/3-3-remove-core-elements-print-option.md
```

---

## Epic 4: Vitals Validation Refinements _(2026-06-16 refinement session)_

| Story                                                             | File                                    | Status      |
| ----------------------------------------------------------------- | --------------------------------------- | ----------- |
| 4.0 Validation Engine Foundation — Port, Seed, Replace Callsites  | `4-0-validation-engine-foundation.md`   | not-started |
| 4.1 Remove Prior FR-2.6 Behavior + Print/View/PDF Validation Gate | `4-1-print-view-pdf-validation-gate.md` | not-started |
| 4.2 Print/PDF Out-of-Range Comment Appending                      | `4-2-print-pdf-comment-appending.md`    | not-started |
| 4.3 OMB Block Right-Alignment — Home Page                         | `4-3-omb-block-right-alignment.md`      | not-started |

> ⚠️ **Epic 4 sequencing:** Story 4.0 must be completed before 4.1 and 5.1. Story 2.5 must be verified before 4.1 ships (4.1 removes the behavior 2.5 implemented). Stories 4.2 and 4.3 are independent.

**Story 4.0 prompt:**

```
create story 4.0: Port the CaseValidationManager field_rules path from branch v4.1-case-data-validation-mode (manual port, vitals field_rules only — exclude connected_field_rules, form_status_rules, admin UI). Seed the 6 confirmed vitals rules at server startup (temperature, heart_rate, respiration_rate, systolic_bp, diastolic_bp, oxygen_saturation) using GetSeededNumericRange with severity: hard and review_status: reviewed. Expose rules to the client via a new API endpoint and set window.mmria_validation_rules in Views/Case/Index.cshtml (replacing window.mmria_vital_sign_range). Update the three mmria_vitals_is_out_of_range() functions (chart.js, print_version_renderer.js, pdf-version/index.js) to read from window.mmria_validation_rules by field_path instead of window.mmria_vital_sign_range by field name. Update pass-through calls in case/index.js, print-version/index.js, pdf-version/index.js. Delete VitalSignRangeHelper.cs and remove the two TempData vitals range lines from CaseController.cs. Add evaluation context flag: active-input (hard enforced on blur) vs historical (hard downgraded to warning at load time). No gate logic, no panel UI — engine and callsite wiring only. Callsite reference: docs/ai/callsite-map is in conversation context. Save to _bmad-output/implementation-artifacts/4-0-validation-engine-foundation.md
```

**Story 4.1 prompt:**

```
create story 4.1: Remove prior FR-2.6 behavior (edit-mode modal, form-navigation modal, red text indicator) and implement the print/View/PDF validation gate per FR-2.6 in _bmad-output/planning-artifacts/prds/prd-mmria-2026-06-12/prd.md. Gate branches on severity: hard violations block entirely (no proceed path); soft/warning violations require UI acknowledgment only (no case-doc persistence). Historical out-of-range vitals surface as warnings. Depends on Story 4.0 — engine and window.mmria_validation_rules are already wired; this story adds only the gate logic at the print/PDF call sites in case/index.js. Save to _bmad-output/implementation-artifacts/4-1-print-view-pdf-validation-gate.md
```

**Story 4.2 prompt:**

```
create story 4.2: Update print and PDF display-time exclusion to append out-of-range notice to the Comment(s) column per FR-2.4 in _bmad-output/planning-artifacts/prds/prd-mmria-2026-06-12/prd.md. Save to _bmad-output/implementation-artifacts/4-2-print-pdf-comment-appending.md
```

**Story 4.3 prompt:**

```
create story 4.3: Right-align the OMB block on the Home page per FR-3.4 in _bmad-output/planning-artifacts/prds/prd-mmria-2026-06-12/prd.md. Save to _bmad-output/implementation-artifacts/4-3-omb-block-right-alignment.md
```

---

## Epic 5: Validation Errors Panel _(2026-06-16 refinement session)_

| Story                                                         | File                             | Status      |
| ------------------------------------------------------------- | -------------------------------- | ----------- |
| 5.1 Validation Errors Panel — Button, Modal, Field Navigation | `5-1-validation-errors-panel.md` | not-started |

> ℹ️ **Story 5.1 — OI-PRD-4 resolved. Hold lifted. Depends on Story 4.0.** The validation architecture is finalized: dedicated version-scoped `case-validation-rules` CouchDB document; `severity: hard` for active-input blur validation; `severity: warning` for historical data surfaced at case load time. Engine and `window.mmria_validation_rules` are wired in Story 4.0 — this story builds the case-worker–facing panel on top. See FR-6 in the PRD for full panel spec including error/warning bifurcation and stored-value message format.

**Story 5.1 prompt:**

```
create story 5.1: Implement the Validation Errors panel — button visibility (errors + warnings count) in edit mode, modal with bifurcated Errors (red) / Warnings (amber) sections and separate counts, field navigation, and load-time historical warning detection per FR-6.1, FR-6.2, FR-6.3 in _bmad-output/planning-artifacts/prds/prd-mmria-2026-06-12/prd.md. Depends on Story 4.0 — engine already ported, window.mmria_validation_rules already wired; do not re-port the engine. Warning rows must include stored value in message format 'Value [X] is outside expected range [min]–[max]'. Load-time historical scan runs in historical evaluation context (hard severity downgraded to warning). Save to _bmad-output/implementation-artifacts/5-1-validation-errors-panel.md
```

---

## Epic 6: Case Validation Rules Management _(2026-06-17 refinement session)_

| Story                                                                  | File                                          | Status |
| ---------------------------------------------------------------------- | --------------------------------------------- | ------ |
| 6.1 Decouple Validation Rules Generation from Metadata Auto-Generation | `6-1-decouple-validation-rules-generation.md` | draft  |
| 6.2 Port Case Validation Admin UI from Branch                          | `6-2-port-case-validation-admin-ui.md`        | done   |
| 6.3 Add/Edit Rule Modal with Cascading Metadata Dropdowns              | `6-3-add-edit-rule-modal.md`                  | done   |

> ⚠️ **Epic 6 sequencing:** Story 6.1 must be completed before 6.2. Story 6.2 must be completed before 6.3. All three must be done in order.

**Story 6.1 prompt:**

```
dev this story _bmad-output/implementation-artifacts/6-1-decouple-validation-rules-generation.md
```

**Story 6.2 prompt:**

```
dev this story _bmad-output/implementation-artifacts/6-2-port-case-validation-admin-ui.md
```

**Story 6.3 prompt:**

```
dev this story _bmad-output/implementation-artifacts/6-3-add-edit-rule-modal.md
```

---

## Epic 7: Admin Action Audit Logging _(2026-06-17 session)_

| Story                                             | File                                             | Status       |
| ------------------------------------------------- | ------------------------------------------------ | ------------ |
| 7.1 Audit Logging — Year of Death and Maiden Name | `7-1-audit-logging-year-of-death-maiden-name.md` | verification |
| 7.2 Audit Logging — Case Lifecycle Actions        | `7-2-audit-logging-case-lifecycle-actions.md`    | verification |

> ℹ️ Stories 7.1 and 7.2 are independent — can be worked in any order.

**Story 7.1 prompt:**

```
dev this story _bmad-output/implementation-artifacts/7-1-audit-logging-year-of-death-maiden-name.md
```

**Story 7.2 prompt:**

```
dev this story _bmad-output/implementation-artifacts/7-2-audit-logging-case-lifecycle-actions.md
```

---

## Epic 8: System Going Offline _(2026-06-17 session)_

| Story                                                                        | File                                               | Status       |
| ---------------------------------------------------------------------------- | -------------------------------------------------- | ------------ |
| 8.1 System Offline Config — Document, mmria-services, Controller, Admin Page | `8-1-system-offline-config-services-admin-page.md` | not-started  |
| 8.2 Login Page Offline State                                                 | `8-2-login-page-offline-state.md`                  | verification |
| 8.3 Warning and Going Offline Modals                                         | `8-3-warning-and-offline-modals.md`                | not-started  |
| 8.4 Periodic Offline Status Check                                            | `8-4-periodic-status-check.md`                     | not-started  |

> ⚠️ **Epic 8 sequencing:** Story 8.1 must be completed before 8.2, 8.3, and 8.4. Stories 8.2, 8.3, and 8.4 can be worked in any order after 8.1. Story 8.4 depends on Story 8.3 (`system-offline-check.js` module and modal handlers must exist first).

**Story 8.1 prompt:**

```
dev this story _bmad-output/implementation-artifacts/8-1-system-offline-config-services-admin-page.md
```

**Story 8.2 prompt:**

```
dev this story _bmad-output/implementation-artifacts/8-2-login-page-offline-state.md
```

**Story 8.3 prompt:**

```
dev this story _bmad-output/implementation-artifacts/8-3-warning-and-offline-modals.md
```

**Story 8.4 prompt:**

```
dev this story _bmad-output/implementation-artifacts/8-4-periodic-status-check.md
```

---

---

## Standalone Bug Fixes

| Story                                                   | File                                             | Status        |
| ------------------------------------------------------- | ------------------------------------------------ | ------------- |
| 9.1 Fix Data Summary Checks Field Filter for ALL Toggle | `9-1-fix-data-summary-checks-field-filter.md`    | done          |
| 9.2 Fix Manage Users Export Ignores Active Filter       | `9-2-fix-manage-users-export-respects-filter.md` | ready-for-dev |

**Story 9.1 prompt:**

```
dev this story _bmad-output/implementation-artifacts/9-1-fix-data-summary-checks-field-filter.md
```

**Story 9.2 prompt:**

```
dev this story _bmad-output/implementation-artifacts/9-2-fix-manage-users-export-respects-filter.md
```

---

## Epic 10: CVS PDF Export Tool Reliability _(2026-07-06)_

| Story                                                         | File                                                   | Status |
| ------------------------------------------------------------- | ------------------------------------------------------ | ------ |
| 10.1 Fix BatchSupervisor Busy-Wait CPU Spin                   | `10-1-fix-cvs-batch-supervisor-busy-wait.md`           | done   |
| 10.2 Server-Side CVS Error Hardening                          | `10-2-server-side-cvs-error-hardening.md`              | review |
| 10.3 Client-Side CVS Retry Mechanism with Countdown           | `10-3-client-side-cvs-retry-mechanism.md`              | review |
| 10.4 CVS Parent-Page Button State via BroadcastChannel        | `10-4-cvs-parent-page-broadcast-channel-status.md`     | review |
| 10.5 Config-Driven CVS Retry Constants                        | `10-5-config-driven-cvs-retry-constants.md`            | done        |

> ℹ️ Stories 10.1 and 10.2 are independent of each other and of 10.3/10.4.
> ⚠️ Story 10.4 depends on Story 10.3 — `post_cvs_status` and the BroadcastChannel message schema are defined in 10.3.
> ⚠️ Story 10.5 depends on Story 10.3 — the constants being made configurable are defined there.

**Story 10.1 prompt:**

```
dev this story _bmad-output/implementation-artifacts/10-1-fix-cvs-batch-supervisor-busy-wait.md
```

**Story 10.2 prompt:**

```
dev this story _bmad-output/implementation-artifacts/10-2-server-side-cvs-error-hardening.md
```

**Story 10.3 prompt:**

```
dev this story _bmad-output/implementation-artifacts/10-3-client-side-cvs-retry-mechanism.md
```

**Story 10.4 prompt:**

```
dev this story _bmad-output/implementation-artifacts/10-4-cvs-parent-page-broadcast-channel-status.md
```

**Story 10.5 prompt:**

```
dev this story _bmad-output/implementation-artifacts/10-5-config-driven-cvs-retry-constants.md
```

---

## Epic 11 — Vitals Import Integer Type Fix

| Story | File | Status |
|-------|------|--------|
| 11.1 — Vitals Import: Store Integer-Coded Fields as JSON Numbers | [11-1-vitals-import-integer-type-fix.md](11-1-vitals-import-integer-type-fix.md) | verification |

**Sequencing:** Independent — can be worked immediately.

**Dev prompt:**

```
dev this story _bmad-output/implementation-artifacts/11-1-vitals-import-integer-type-fix.md
```

---

## Epic 12 — Data Migration Tool Modernization

| Story | File | Status |
|-------|------|--------|
| 12.1 — Data Migration Environment Configuration Parity | [12-1-data-migration-environment-config.md](12-1-data-migration-environment-config.md) | not-started |
| 12.2 — Vitals Retrospective Type Correction Migration | [12-2-vitals-type-correction-migration.md](12-2-vitals-type-correction-migration.md) | not-started |

**Sequencing:** 12.1 must be completed before 12.2. Both are independent of Epic 11.

**Dev prompts:**

```
dev this story _bmad-output/implementation-artifacts/12-1-data-migration-environment-config.md
```

```
dev this story _bmad-output/implementation-artifacts/12-2-vitals-type-correction-migration.md
```

---

## Open Items — Resolve Before Affected Story

| OI       | Affects               | What to resolve                                                                                                                                                                                                                                                                                                                                                                                        |
| -------- | --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| OI-3     | Story 1.1             | **Resolved** — Implementation complete; going to formal verification.                                                                                                                                                                                                                                                                                                                                  |
| OI-4     | Story 2.2             | **Resolved** — Vitals input `name` attributes confirmed during Story 2.2 implementation.                                                                                                                                                                                                                                                                                                               |
| OI-5     | Stories 3.1, 3.2      | **Resolved** — Controller actions confirmed during Story 3.1/3.2 implementation.                                                                                                                                                                                                                                                                                                                       |
| OI-5-CVS | Stories 10.3, 10.5    | **Resolved** — Constants are config-driven via `integer_keys.shared` in the CouchDB config document (Story 10.5). Defaults: 10 attempts, 60-second delay (changed from 30s branch value per FR-11.3).                                                                                                                                                                             |
| OI-dev-B | Story 2.5             | **Resolved** — Edit-mode hook confirmed during Story 2.5 implementation.                                                                                                                                                                                                                                                                                                                               |
| OI-dev-C | Story 2.5             | **Resolved** — Chart.js DOM target confirmed during Story 2.5 implementation.                                                                                                                                                                                                                                                                                                                          |
| OI-PRD-4 | Stories 4.0, 4.1, 5.1 | **Resolved** — Dedicated version-scoped `case-validation-rules` CouchDB document; `severity: hard` for active-input blur; `severity: warning` for historical load-time scan; soft-acknowledgment print gate (UI-only, no persist); POC `CaseValidationManager` field_rules path ported in Story 4.0 (vitals-scoped). FR-2.1, FR-2.3, FR-2.6, FR-6 updated in PRD. Stories 4.0, 4.1, and 5.1 unblocked. |
