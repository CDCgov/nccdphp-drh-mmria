# Story 27.1 — Activate Export Utility Repository Wiring

**Epic:** 27 — Services Utility Repository Activation
**Story ID:** 27.1
**Status:** ready-for-dev
**Date added:** 2026-07-17
**Depends on:** Epic 24 stories 24.10, 24.11; Story 26.3 (IExportQueueRepository confirmed wired in services)
**Source requirements:** epics.md §Epic 27 Story 27.1; project-context.md §2.2

---

## User Story

As a developer,
I want the export-utility classes in `mmria.services` to receive real repository instances from their supervising actors instead of relying on null fallbacks,
So that the export pipeline's database access routes fully through repository interfaces at runtime rather than falling back to direct HTTP calls.

---

## Acceptance Criteria

**AC-1 — `exporter.cs` receives real `IExportQueueRepository` at runtime**
Given `exporter.cs` was given a null-fallback `IExportQueueRepository?` constructor parameter in Story 24.10
When this story is complete
Then the actor or supervisor that instantiates `exporter.cs` passes a real `IExportQueueRepository` instance resolved from DI; the null-fallback branch (`_couchDbHttpClient.ExecuteAsync`) is no longer exercised during export queue processing

**AC-2 — `mmrds_exporter.cs` receives real `IExportQueueRepository` at runtime**
Given `mmrds_exporter.cs` was given a null-fallback constructor param in Story 24.10
When this story is complete
Then its instantiation site passes a real `IExportQueueRepository` from the supervisor's DI scope; null-fallback not exercised at runtime

**AC-3 — `core_element_exporter.cs` (services) receives real `IExportQueueRepository`**
Given `core_element_exporter.cs` in mmria.services was given a null-fallback param in Story 24.10
When this story is complete
Then its instantiation site passes a real `IExportQueueRepository`; null-fallback not exercised at runtime

**AC-4 — `IReportRepository` wired where applicable**
Given export jobs may also write report data via utility helpers
When this story begins
Then the developer confirms whether `IReportRepository` null-fallbacks exist in any of the three utility files; if they exist, those are activated with real instances from the supervisor's scope; if no such fallbacks exist, this AC is marked not-applicable

**AC-5 — Null-fallback code paths removed**
Given the null-fallback branches in each utility file are inconsistent with the required-repository pattern
When this story is complete
Then the null-fallback code IS removed — repositories are required, not optional; instantiation sites pass real repo instances; if a caller does not have the repo injected, thread it through from the supervisor's DI scope

**AC-6 — Build passes with zero errors**
Given the changes above
When the build runs
Then `mmria.services` builds with zero errors

---

## Dev Notes — Files to Change

| File | Change |
|------|--------|
| `nccdphp-drh-mmria-services/mmria.services/Utilities/Exporter/exporter.cs` | **ACTIVATE** — trace instantiation site; pass real `IExportQueueRepository` from supervisor |
| `nccdphp-drh-mmria-services/mmria.services/Utilities/Exporter/mmrds_exporter.cs` | **ACTIVATE** — same |
| `nccdphp-drh-mmria-services/mmria.services/Utilities/CoreElementExport/core_element_exporter.cs` | **ACTIVATE** — same |
| Supervisor/actor that instantiates the above | **UPDATE** — resolve repos from DI scope; pass to utility constructors |

**Tracing the instantiation chain:**
1. Start from `ExportQueueController.cs` or the Akka actor that handles export queue processing
2. Follow the chain: supervisor → `Process_Export_Queue` actor → exporter utilities
3. The supervisor already has `IExportQueueRepository` from Story 24.10 DI registration (`Program.cs` in mmria.services)
4. Pass the resolved repo instance through the constructor chain to each utility class

**Verification:** After wiring, trigger a test CVS export job in the multi-tenant test environment. Confirm in logs that the direct HTTP fallback branch is not reached (add a temporary `Console.WriteLine` if needed to confirm the repo path is taken, then remove before committing).
