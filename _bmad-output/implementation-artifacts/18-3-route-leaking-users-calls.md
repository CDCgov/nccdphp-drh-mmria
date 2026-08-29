# Story 18.3 — Route Leaking `_users` Calls Through `IUserRepository`

**Epic:** 18 — `_users` and `configuration` Consolidation (SQL Migration Foundation)
**Story ID:** 18.3
**Status:** done
**Date added:** 2026-07-14
**Depends on:** 18.2 (`IUserRepository` defined and `AccountDAL` canonicalized)
**Source requirements:** epics.md §Epic 18 Story 18.3; project-context.md §2.2

---

## User Story

As a developer,
I want all out-of-DAL `_users` calls in controllers and utility files to delegate to `IUserRepository`,
So that no file outside `AccountDAL` constructs a `/_users/` URL directly.

---

## Acceptance Criteria

**AC-1 — AccountController.OIDC.cs calls replaced**
Given `AccountController.OIDC.cs` line 255 (GET user by email — OIDC lookup) and line 301 (PUT user — OIDC provisioning)
When this story is complete
Then both calls are replaced with the corresponding `IUserRepository` methods; `IUserRepository` is injected into `AccountController` via constructor injection; only the CouchDB URL construction is moved — OIDC token handling, cookie management, and claims extraction remain in the controller

**AC-2 — passwordChangeController.cs call replaced**
Given `passwordChangeController.cs` line 137 — `db_config.url + "/_users/org.couchdb.user:" + userName` (used for GET then PUT in a password change workflow)
When this story is complete
Then the call is replaced with the corresponding `IUserRepository` methods (`GetUserAsync` / `CheckUserAsync` for GET, `PutUserAsync` for PUT); `IUserRepository` is injected via constructor injection

**AC-3 — JurisdictionSummary.cs call replaced**
Given `JurisdictionSummary.cs` (util class at `mmria-server/util/`) line 268 — `$"{p_config_detail.url}/_users/_all_docs?include_docs=true&skip=1"` (GET all users for summary statistics)
When this story is complete
Then the call is replaced with `IUserRepository.GetAllUsersAsync`; `IUserRepository` is injected via the class's constructor or existing dependency resolution mechanism

**AC-4 — VROSummary.cs call replaced**
Given `VROSummary.cs` (util class at `mmria-server/util/`) line 265 — identical pattern to JurisdictionSummary.cs
When this story is complete
Then the call is replaced with `IUserRepository.GetAllUsersAsync`; `IUserRepository` is injected

**AC-5 — No OIDC logic moved**
Given `AccountController.OIDC.cs` OIDC-specific logic (token validation, `ExternalLoginSignInAsync`, claims extraction, cookie construction)
When this story is complete
Then none of that logic has moved — only the `_users` URL construction and `ExecuteAsync` calls are removed

**AC-6 — DI registrations updated**
Given each file now requires `IUserRepository` as a constructor dependency
When DI registration is updated in `mmria-server/Program.cs`
Then all four classes have their DI registrations updated to satisfy `IUserRepository`; no other changes are made

**AC-7 — Build succeeds**
Given the changes are complete
When `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` runs
Then the build succeeds with exit code 0

---

## Dev Notes — Implementation

### Files to change

| File | Change |
|------|--------|
| `mmria-server/Controllers/AccountController.OIDC.cs` | **UPDATE** — inject `IUserRepository`; replace lines 255 and 301 |
| `mmria-server/Controllers/api/passwordChangeController.cs` | **UPDATE** — inject `IUserRepository`; replace line 137 |
| `mmria-server/util/JurisdictionSummary.cs` | **UPDATE** — inject `IUserRepository`; replace line 268 |
| `mmria-server/util/VROSummary.cs` | **UPDATE** — inject `IUserRepository`; replace line 265 |
| `mmria-server/Program.cs` | **UPDATE** — add `IUserRepository` to DI registrations for all four classes |

---

### Call sites inventory (verified 2026-07-14)

#### AccountController.OIDC.cs

| Line | HTTP Verb | Current URL | Replace with |
|------|-----------|-------------|-------------|
| 255 | GET | `$"{config_couchdb_url}/_users/{Uri.EscapeDataString("org.couchdb.user:" + email.ToLower())}"` | `GetCouchDbUserAsync(email, dbConfig)` or `GetUserAsync("org.couchdb.user:" + email.ToLower(), dbConfig)` |
| 301 | PUT | `$"{config_couchdb_url}/_users/{Uri.EscapeDataString(user._id)}"` | `PutUserAsync(user, dbConfig)` |

> **Note:** This controller uses `config_couchdb_url` (a raw URL string from config) rather than a `DBConfigurationDetail`. Verify whether `IUserRepository` methods can accept a raw URL or whether a `DBConfigurationDetail` is available in scope. If only a raw URL is available, the method signature may need to accommodate it, or a `DBConfigurationDetail` may need to be resolved from the existing config object.

#### passwordChangeController.cs

| Line | HTTP Verb | Current URL | Replace with |
|------|-----------|-------------|-------------|
| 137 | GET (then PUT) | `db_config.url + "/_users/org.couchdb.user:" + userName` | GET: `CheckUserAsync("org.couchdb.user:" + userName, db_config)`; PUT: `PutUserAsync(user, db_config)` after modifying the user object |

> Read the surrounding context at line 137 to confirm whether this is a GET followed by a PUT (read-then-update pattern for password change) or just a GET.

#### JurisdictionSummary.cs and VROSummary.cs

| File | Line | Operation | Current URL | Replace with |
|------|------|-----------|-------------|-------------|
| `JurisdictionSummary.cs` | 268 | GET all users (for count) | `$"{p_config_detail.url}/_users/_all_docs?include_docs=true&skip=1"` | `GetAllUsersAsync(skip: 1, take: int.MaxValue, p_config_detail)` or equivalent |
| `VROSummary.cs` | 265 | GET all users (for count) | `$"{p_config_detail.url}/_users/_all_docs?include_docs=true&skip=1"` | same as above |

> **Note on `skip=1`:** The current URL hardcodes `skip=1` — this skips the admin user doc at the top of the `_all_docs` response. Verify that `GetAllUsersAsync` in `IUserRepository` accepts a `skip` parameter. If `GetAllUsersAsync(int skip, int take, ...)` is the signature from Story 18.2, pass `skip: 1` and a large `take` value. Confirm what value the callers use for the response (total_rows count vs. actual docs).

---

### Architecture note on JurisdictionSummary / VROSummary

These utility classes (`mmria-server/util/`) are not controllers — they're summary-computation helpers. Verify how they receive their dependencies today (constructor injection, or passed as parameters). Follow the same pattern as the existing `CouchDbHttpClient` injection in these classes when adding `IUserRepository`.

---

## Dev Agent Record

### Completion Notes

- All five files updated per story spec; `IUserRepository` is now the sole path for `_users` CouchDB operations in these classes.
- `AccountController.OIDC.cs`: Added `IUserRepository _userRepository` field + constructor param. Replaced GET with `GetCouchDbUserAsync(email.ToLower(), db_config)` and PUT with `PutUserAsync(user, db_config)`. Restored `config_couchdb_url / config_timer_*` variables that are still needed for the session PUT later in the same method.
- `passwordChangeController.cs`: Added `IUserRepository _userRepository`. Replaced GET + explicit serialize + PUT block with `CheckUserAsync` + `PutUserAsync`. Null guard changed from `user_object == null` to `string.IsNullOrWhiteSpace(user_object._id)` (CheckUserAsync never returns null). Removed now-unused `object_string` local.
- `JurisdictionSummary.cs`: Added `IUserRepository _userRepository` to constructor; added `IUserRepository userRepository` parameter to `GetUserCount`; replaced 3-line URL/HTTP/deserialize block with `await userRepository.GetAllUsersAsync(1, int.MaxValue, p_config_detail)`; updated all three `GetUserCount` call sites in `execute()`.
- `VROSummary.cs` (`#if IS_PMSS_ENHANCED`): Same pattern as JurisdictionSummary. Also fixed pre-existing bug where `GetJurisdictions` was called without the required `couchDbHttpClient` parameter.
- `jurisdictionSummaryController.cs`: Added `IUserRepository _userRepository` field + constructor param; passes it to both `JurisdictionSummary` constructor calls.
- `vro_exportController.cs` (`#if IS_PMSS_ENHANCED`): Added both `CouchDbHttpClient _couchDbHttpClient` and `IUserRepository _userRepository` fields + constructor params; passes both to `VROSummary` constructor (also fixes pre-existing missing `couchDbHttpClient` arg).
- `Program.cs`: No changes needed — `IUserRepository` was already registered as scoped (backed by `AccountDAL`) from Story 18.2.
- Build verified: `dotnet build mmria-server.csproj -c Release` → **Build succeeded**, no errors.

### File List

- `source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs` — modified
- `source-code/mmria/mmria-server/Controllers/api/passwordChangeController.cs` — modified
- `source-code/mmria/mmria-server/util/JurisdictionSummary.cs` — modified
- `source-code/mmria/mmria-server/util/VROSummary.cs` — modified
- `source-code/mmria/mmria-server/Controllers/jurisdictionSummaryController.cs` — modified
- `source-code/mmria/mmria-server/Controllers/vro_exportController.cs` — modified
