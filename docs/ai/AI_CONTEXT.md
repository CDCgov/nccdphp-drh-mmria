# AI Context Pack (Single File)
This document is the **source of truth** for Copilot/AI-assisted changes in this repo.

## Hard constraints (read first)
- **Preserve routes**: Any change to controllers, APIs, or server-side code must **not** change route names, route templates, `[Route]` attributes, HTTP method attributes, or conventional routing behavior **unless explicitly asked**. This is to preserve app functionality.
- **Refactor-only default**: When refactoring, **minimize enhancements**. Do not add new features, change behavior, alter outputs, or “improve” UX/performance beyond what is necessary to achieve the refactor goal—unless explicitly asked. Prefer small, mechanical moves that preserve existing behavior.
- **SharedLibraries-first**: Prefer implementing new/changed server-side logic in `SharedLibraries/` rather than directly in the MVC app.
- **Enforced feature-based layering (Option A)**: Organize shared server-side code by **feature**, and within each feature use **/Model**, **/Manager**, **/DAL**:
  - `/Model`: shared data contracts used by Manager and/or DAL
  - `/Manager`: business logic + orchestration
  - `/DAL`: *all* CouchDB/data access calls (and nothing else)
- **No CouchDB calls in Controllers** (new or modified code): Controllers call Managers. Managers call DAL.
- **Jurisdiction isolation**: All data operations must be **jurisdiction-scoped** and must **never** cross jurisdictions unless explicitly designed and approved.
- **Async end-to-end**: Avoid blocking `.Result`/`.Wait()`;
---

## Overview
This repository contains a **.NET 9 MVC** application ("MMRIA Server") with controllers and Razor views, plus an extended service ("MMRIA Sir Services") that handles additional calls.

The system is primarily single-tenant in deployment, but logically multi-tenant: tenants are called **jurisdictions**. Each jurisdiction has its **own CouchDB database set** (including a configuration DB and additional DBs per jurisdiction). The app also uses **Akka.NET** in parts of the server-side system and serves significant front-end JavaScript from `wwwroot/`.

### High-level components
- **MMRIA Server (.NET 9 MVC)**
  - Controllers + Razor Views
  - Should call shared **Managers** (preferred), not CouchDB directly
  - Uses/hosts Akka.NET where applicable
  - Serves static assets from `wwwroot/`
- **MMRIA Sir Services**
  - Handles additional calls separate from the MVC app
  - Communicates with MMRIA Server via: TODO (HTTP / internal SDK / messaging)
- **CouchDB**
  - Jurisdiction-scoped DB topology and naming rules
  - Mango queries and/or design-doc views may be used

---

## Architecture (enforced)
### SharedLibraries priority
All new or modified server-side code should be prioritized into the `SharedLibraries/` folder whenever feasible. Controllers should remain thin wrappers that call into shared feature `/Manager` code.

## Enforced structure (Option A): Feature-based /Model, /Manager, /DAL
All new or modified server-side code should be prioritized into `SharedLibraries/` and organized by **feature**, with `/Model`, `/Manager`, and `/DAL` under each feature folder.

### Required layout
Location: `SharedLibraries/Features/<FeatureName>/`

Each feature contains:
- `Model/`    (contracts used by Manager/DAL)
- `Manager/`  (business logic + orchestration)
- `DAL/`      (all CouchDB/data access for the feature)

Example:
- `SharedLibraries/Features/OfflineMode/Model/`
- `SharedLibraries/Features/OfflineMode/Manager/`
- `SharedLibraries/Features/OfflineMode/DAL/`

### Layer rules (strict)
- Controllers call **feature Managers**.
- Managers contain all business logic and orchestrate DAL calls.
- DAL contains **all CouchDB calls** and nothing else.
- Models used by a feature’s Manager or DAL must live under that feature’s `Model/`.
- **No CouchDB calls in Controllers** (new or modified code).

### Cross-feature reuse rules
- ❌ Do not call another feature’s `DAL/` directly.
- ✅ Shared utilities and truly cross-cutting models go in `SharedLibraries/Common/`:
  - `SharedLibraries/Common/Model/` (shared contracts)
  - `SharedLibraries/Common/` (shared helpers/utilities)
- If Feature A needs something from Feature B, prefer exposing it via Feature B’s **Manager** (or a shared service), not by reaching into Feature B’s DAL.

### Migration policy
- When modifying legacy code, move it into the appropriate feature folder.
- If the “feature home” is unclear, choose the best fit and document the rationale in the PR description.

### Controllers (MVC)
- Controllers must not call CouchDB directly.
- Controllers must not contain business logic.
- Controllers call feature Managers.
- Controllers must preserve routing contract unless explicitly asked to change it.
- Pass `CancellationToken` through.

### Akka.NET rules
- No `.Result`, `.Wait()`, or blocking I/O inside actors.
- Prefer async + `PipeTo` patterns.
- Prefer Tell + correlation over Ask on hot paths.
- If actors need data, call Managers/DAL via DI (no ad-hoc HTTP or duplicated Couch logic).

### MMRIA Server ↔ MMRIA Sir Services
- Use `IHttpClientFactory` for HTTP calls.
- Use explicit request/response DTOs.
- Propagate correlation ID + jurisdiction explicitly.

### Migration policy (incremental)
- No new direct CouchDB calls in controllers.
- If you touch a controller action, move Couch logic behind feature Manager + DAL.
- Refactor endpoint-by-endpoint (strangler fig), not big bang.

## Routing contract (do not break)
- Routes are part of the contract.
- Do not rename controller routes, action routes, route templates, or routing attributes unless explicitly requested.
- Refactors must preserve routing behavior to avoid breaking application functionality.

---

## Tenancy (Jurisdictions)
### Terminology
- **Jurisdiction**: tenant-like isolation boundary.
- **Jurisdiction Context**: resolved identity + routing info needed to safely access the correct DBs.

### Non-negotiable rules
1. All data operations MUST be jurisdiction-scoped.
2. Never hardcode DB names in controllers/managers/DAL/actors.
3. Resolve jurisdiction once per request; pass context down.
4. No cross-jurisdiction access unless explicitly designed and approved.

### Jurisdiction resolution (TODO)
Document how jurisdiction is derived:
- Route value? Subdomain? Header? Auth claim? Session?
- Source precedence
- Validation rules
- Error handling when missing/invalid

### DB topology + naming (TODO)
Each jurisdiction has:
- A config database
- Additional domain databases (examples): cases, users, audit, attachments, etc.

DB naming must be implemented in one place (e.g., `JurisdictionDbNamer`):
- Example (replace with your actual convention):
  - `{jurisdictionId}_config`
  - `{jurisdictionId}_cases`
  - `{jurisdictionId}_audit`

### Passing jurisdiction through the system
- MVC obtains context via middleware/filter or `IJurisdictionAccessor`
- Managers accept context explicitly OR read it from accessor
- Cross-service calls include jurisdiction (header/param) and validate on receiver

### Testing requirements
- Unit tests for DB naming rules
- Integration tests that ensure no cross-jurisdiction access

---

## CouchDB guidance
### Golden rules
- No direct CouchDB calls from controllers (new/modified code).
- Use DAL for all CouchDB access.
- Avoid N sequential calls in loops; batch where possible.
- Prefer `_bulk_docs` for multiple writes.
- Avoid accidental unindexed scans; ensure indexes exist for frequent Mango queries.

### Connection management
- Centralize CouchDB client configuration (auth, base URL, serialization).
- Reuse configured clients; do not create ad-hoc clients throughout code.
- Centralize retry/backoff and timeout policy.

### Error handling & logging
- Normalize CouchDB errors into meaningful outcomes:
  - missing doc → NotFound (if applicable)
  - conflict → 409 with actionable message
  - forbidden/unauthorized → 401/403
  - timeout/unavailable → 503 (with retry policy as appropriate)
- Log fields (recommended):
  - jurisdictionId, dbName, operation, docId (if applicable), latency, status/exception, correlationId

---

## Patterns (what to generate)
### Controller pattern
- Get jurisdiction context (or use accessor)
- Validate input
- Call feature Manager
- Return IActionResult / view result
- Pass CancellationToken
- **Do not change routes unless explicitly asked**
- **Refactor-only default**: preserve behavior unless explicitly asked to change it

### Manager pattern
- Public use-case entry points
- Orchestrate DAL calls
- Apply business rules and validation
- Return DTOs/models suitable for controllers/views

### DAL pattern
- Provide domain-meaningful methods (avoid “god query runner”)
- Encapsulate:
  - DB selection by jurisdiction
  - query definitions (Mango/view) + indexes
  - bulk operations
  - mapping to/from Models

### JavaScript in wwwroot
- Keep feature modules organized
- Avoid duplicate utility functions
- Prefer consistent naming and structure:
  - `wwwroot/js/shared/`
  - `wwwroot/js/features/<feature>/`
  - `wwwroot/js/pages/<page>/`

---

## Checklist (Copilot must follow)
### Non-negotiables
- ✅ Preserve existing route names/templates and routing behavior unless explicitly requested.
- ✅ Refactor-only default: minimize enhancements; preserve existing behavior unless explicitly asked to change it.
- ✅ Prefer changes in `SharedLibraries/`.
- ✅ Organize new/modified SharedLibraries code under `SharedLibraries/Features/<FeatureName>/`.
- ✅ Each feature must have `Model/`, `Manager/`, and `DAL/` folders.
- ✅ Models used by Manager/DAL go in that feature’s `/Model`.
- ✅ Business logic goes in that feature’s `/Manager`.
- ✅ CouchDB calls go only in that feature’s `/DAL`.
- ✅ Resolve jurisdiction once; pass context down.
- ✅ Async end-to-end; pass CancellationToken.
- ❌ No new CouchDB calls in controllers.
- ❌ Do not hardcode DB names.
- ❌ Do not cross jurisdictions.
- ❌ Do not call another feature’s `DAL/` directly.

### CouchDB efficiency
- ✅ Minimize round-trips; batch reads/writes where possible.
- ✅ Prefer `_bulk_docs` for multiple writes.
- ✅ Avoid unindexed queries/scans.
- ❌ Don’t do N sequential Couch calls in loops.

### Akka.NET safety
- ✅ No `.Result` / `.Wait()` in actors.
- ✅ Use async + `PipeTo`.
- ❌ Avoid Ask in hot paths unless justified.

### Cross-service calls
- ✅ Use `IHttpClientFactory`.
- ✅ Propagate jurisdiction + correlation IDs.

---

## Golden paths (fill these in)
Replace TODOs with real files that represent “best examples”.
- Best thin controller: TODO
- Best manager: TODO
- Best DAL (CouchDB query): TODO
- Best bulk docs usage: TODO
- Best jurisdiction resolution: TODO
- Best Akka actor (async + PipeTo): TODO
- Best modular JS: TODO

---

## Performance (what “efficient” means)
Efficiency is:
- Correct jurisdiction isolation (no wrong-db calls)
- Reduced CouchDB round-trips and payload sizes
- Stable concurrency under load (no mailbox runaway, no threadpool starvation)
- Low latency on key endpoints

Measure and track:
- CouchDB calls per endpoint (count, time, payload size)
- Query types and indexing coverage
- Actors: mailbox depth, message processing time, dispatcher saturation
- MVC/view render time and response size
- JS file count/size (and long client tasks if UI feels slow)

---

## Copilot prompt template
Use this when requesting a change:

“Follow AI_CONTEXT.md. Preserve all existing routes and route templates. Minimize enhancements during refactors (behavior-preserving unless explicitly requested). Implement changes in SharedLibraries under `SharedLibraries/Features/<FeatureName>/` using /Model, /Manager, /DAL. Move business logic into Manager and all CouchDB calls into DAL. Any models used by Manager/DAL go in Model. Keep controllers thin, async end-to-end, and pass CancellationToken. All data access must be jurisdiction-scoped and must not cross jurisdictions.”
