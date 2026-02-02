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

## Enforced structure: Feature-based /Model, /Manager, /DAL
All new or modified server-side code should be prioritized into `SharedLibraries/` and organized by **feature**, with `/Model`, `/Manager`, and `/DAL` under each feature folder.

**CRITICAL: NO generic Model/Manager/DAL folders at SharedLibraries root level. ALL code must be feature-scoped.**

### Required layout
Location: `SharedLibraries/<FeatureName>/`

Each feature contains:
- `Model/`    (contracts used by Manager/DAL)
- `Manager/`  (business logic + orchestration)
- `DAL/`      (all CouchDB/data access for the feature)

Examples:
- `SharedLibraries/OfflineCase/Model/`
- `SharedLibraries/OfflineCase/Manager/`
- `SharedLibraries/OfflineCase/DAL/`
- `SharedLibraries/Case/Model/`
- `SharedLibraries/Case/Manager/`
- `SharedLibraries/Case/DAL/`

### Layer rules (strict)
- Controllers call **feature Managers**.
- Managers contain all business logic and orchestrate DAL calls.
- DAL contains **all CouchDB calls** and nothing else.
- Models used by a feature’s Manager or DAL must live under that feature’s `Model/`.
- **No CouchDB calls in Controllers** (new or modified code).

### Cross-feature reuse rules
- ❌ Do not call another feature's `DAL/` directly.
- ❌ Do NOT create generic `SharedLibraries/Model/`, `SharedLibraries/Manager/`, or `SharedLibraries/DAL/` folders.
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
**CRITICAL: No blocking calls in actors**

Blocking async operations in actors causes deadlocks and `System.NotSupportedException: There is no active ActorContext`.

**Required patterns:**
- ✅ Use `ReceiveAsync<T>` for async message handlers
- ✅ Use direct `await` for all async operations
- ✅ Use async + `PipeTo` patterns for complex flows
- ✅ Prefer Tell + correlation over Ask on hot paths
- ✅ If actors need data, call Managers/DAL via DI

**Strictly forbidden:**
- ❌ **NEVER** use `.Result` in actors
- ❌ **NEVER** use `.Wait()` in actors
- ❌ **NEVER** use `.GetAwaiter().GetResult()` in actors
- ❌ Do NOT use blocking I/O

**Example:**
```csharp
ReceiveAsync<MessageType>(async message =>
{
    var result = await _service.DoSomethingAsync();
    Sender.Tell(new ResponseMessage { Data = result });
});

// Constructor async init (fire-and-forget): InitializeStateAsync();
private async void InitializeStateAsync() { var data = await _service.GetDataAsync(); }
```

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

## Security Best Practices

**Critical rules:**
- ❌ No SSNs/PII in string variables (heap inspection risk). Use inline: `if (set.Contains(item.Substring(start, 9).Trim()))`
- ❌ No `System.Random` for IDs/tokens/keys. Use: `RandomNumberGenerator.GetInt32(min, max)`
- ❌ No untrusted input in file paths. Use: `Path.GetFileName(userInput)` to sanitize
- ✅ Remove PII from logs/errors (log line numbers, not values)

```csharp
// BAD: var ssn = item.Substring(start, 9); if (set.Contains(ssn)) { log(ssn); }
// GOOD: if (set.Contains(item.Substring(start, 9).Trim())) { log($"Line {n}"); }

// BAD: var id = new Random().Next(1000, 9999);
// GOOD: var id = RandomNumberGenerator.GetInt32(1000, 10000);

// BAD: var path = Path.Combine(baseDir, userFileName);
// GOOD: var path = Path.Combine(baseDir, Path.GetFileName(userFileName));
```

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

### cURL is DEPRECATED - Use CouchDbHttpClient
**Do NOT use `cURL` class in new/modified code.**

```csharp
// OLD: var curl = new cURL("GET", null, url, null, user, pass); string r = curl.execute();
// NEW: string r = await _couchDbHttpClient.ExecuteAsync("GET", url, null, user, pass);
```

**Rules:**
- ✅ Use `CouchDbHttpClient` via DI; ✅ `async Task<T>` methods; ✅ direct `await`
- ❌ No `cURL` class; ❌ No `.Result`/`.Wait()`/`.GetAwaiter().GetResult()`

**Why:** IHttpClientFactory prevents socket exhaustion, proper pooling, better testability.

---

## Patterns (what to generate)
### Controller pattern
- Get jurisdiction context (or use accessor)
- Validate input
- Call feature Manager
- Return IActionResult / view result
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
- ✅ Organize new/modified SharedLibraries code under `SharedLibraries/<FeatureName>/` (e.g., `OfflineCase`, `Case`, `Session`).
- ❌ Do NOT create generic `SharedLibraries/Model/`, `SharedLibraries/Manager/`, or `SharedLibraries/DAL/` folders.
- ✅ Each feature must have `Model/`, `Manager/`, and `DAL/` folders within the feature folder.
- ✅ Models used by Manager/DAL go in that feature’s `/Model`.
- ✅ Business logic goes in that feature’s `/Manager`.
- ✅ CouchDB calls go only in that feature’s `/DAL`.
- ✅ Resolve jurisdiction once; pass context down.
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
- ✅ Use `ReceiveAsync<T>` for async message handlers.
- ✅ Use async + `PipeTo`.
- ✅ Direct `await` for all async operations in actors.
- ❌ **NEVER** use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` in actors.
- ❌ Avoid Ask in hot paths unless justified.

### Security
- ✅ No sensitive data (SSN, PII) stored in string variables.
- ✅ Use `System.Security.Cryptography.RandomNumberGenerator` for secure random values.
- ✅ Sanitize file paths with `Path.GetFileName()` before using external input.
- ✅ Remove PII from error messages and logs.
- ❌ Do NOT use `System.Random` for IDs, tokens, or keys.
- ❌ Do NOT use untrusted input directly in file paths.
- ❌ Do NOT use `cURL` class (deprecated).

### Cross-service calls
- ✅ Use `IHttpClientFactory`.
- ✅ Propagate jurisdiction + correlation IDs.

---


## Performance
- Correct jurisdiction isolation
- Minimize CouchDB round-trips; batch operations
- Stable actor concurrency (no mailbox runaway)
- Track: DB calls/latency, query indexing, actor mailbox depth, render time

---

## Copilot prompt template
"Follow AI_CONTEXT.md: Preserve routes. Feature-based SharedLibraries/<Feature>/{Model,Manager,DAL}. Use CouchDbHttpClient (not cURL). ReceiveAsync+await in actors (never .Result/.Wait()). Security: no PII in strings, use RandomNumberGenerator, sanitize paths with GetFileName(). Jurisdiction-scoped data access."
