# MMRIA V4.1 — Story Index

Start a new chat thread for each story. Use the prompt shown to invoke the dev agent.

---

## Epic 1: Case Narrative Editor Fidelity

| Story | File | Status |
|---|---|---|
| 1.1 Fix Save-Path HTML Stripping | `1-1-fix-save-path-html-stripping.md` | ready-for-dev |
| 1.2 Fix Paste Handler Cursor Integrity | `1-2-fix-paste-handler-cursor-integrity.md` | ready-for-dev |
| 1.3 Update Case Narrative Instruction Text | `1-3-update-case-narrative-instruction-text.md` | ready-for-dev |

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

| Story | File | Status |
|---|---|---|
| 2.1 Add Vitals Range Config — CouchDB and Server Loading | `2-1-vitals-range-config-couchdb-and-server-loading.md` | ready-for-dev |
| 2.2 On-Blur Vitals Validation and Invalid Entry Modal | `2-2-on-blur-vitals-validation-and-modal.md` | ready-for-dev |
| 2.3 Display-Time Exclusion — Print, PDF, Date Fix | `2-3-display-time-exclusion-print-pdf-date-fix.md` | ready-for-dev |
| 2.4 Display-Time Exclusion — Graph and Table Views | `2-4-display-time-exclusion-graph-and-table.md` | ready-for-dev |
| 2.5 Historical Data Detection and Record Indicators | `2-5-historical-data-detection-and-indicators.md` | ready-for-dev |

> ⚠️ **Epic 2 sequencing:** Story 2.1 must be completed before 2.2–2.5. Stories 2.2–2.5 can be worked in any order after 2.1.

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

---

## Epic 3: System Configuration & Print Cleanup

| Story | File | Status |
|---|---|---|
| 3.1 Config-Driven OMB Expiration Date | `3-1-config-driven-omb-expiration-date.md` | ready-for-dev |
| 3.2 Config-Driven MMRIA Version Number | `3-2-config-driven-mmria-version.md` | ready-for-dev |
| 3.3 Remove Core Elements Only Print Option | `3-3-remove-core-elements-print-option.md` | ready-for-dev |

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

## Open Items — Resolve Before Affected Story

| OI | Affects | What to resolve |
|---|---|---|
| OI-3 | Story 1.1 | Confirm `textarea_control_strip_html_attributes()` at `case/index.js:~4356` is the only stripping site on the narrative save path |
| OI-4 | Story 2.2 | Confirm exact HTML `name` attributes on vitals inputs rendered by `chart.js`; confirm Oxygen Saturation field exists |
| OI-5 | Stories 3.1, 3.2 | Identify controller action(s) serving Home page, Committee Decisions form, and footer |
| OI-dev-B | Story 2.5 | Confirm the function/event in `case/index.js` that signals transition into case edit mode |
| OI-dev-C | Story 2.5 | Confirm the DOM target in `chart.js` for the per-record red text indicator |
