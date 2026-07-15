# Story 18.2 — Define `IUserRepository` and Canonicalize `AccountDAL`

**Epic:** 18 — `_users` and `configuration` Consolidation (SQL Migration Foundation)
**Story ID:** 18.2
**Status:** ready-for-dev
**Date added:** 2026-07-14
**Depends on:** 18.1 (`_users` Operation Catalog)
**Source requirements:** epics.md §Epic 18 Story 18.2; project-context.md §2.2

---

## User Story

As a developer,
I want a single `IUserRepository` interface over all `_users` CRUD operations,
So that every application-layer caller depends on the interface and a SQL migration (or ASP.NET Identity swap) requires changing only `AccountDAL`.

---

## Acceptance Criteria

**AC-1 — AccountDAL contains all in-scope _users operations**
Given the existing `AccountDAL` in `mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs`
When this story is complete
Then `AccountDAL` contains methods for every in-scope operation from the Story 18.1 catalog, including at minimum: `GetUserAsync`, `PutUserAsync`, `DeleteUserAsync`, `GetAllUsersAsync`, and any password or session methods present in the catalog

**AC-2 — AccountDAL uses consistent URL construction**
Given the existing `GetCouchDbUserAsync` (line 47) uses `$"{dbConfig.url}/_users/{HtmlEncode(userDocId)}"` (no `Get_Prefix_DB_Url` — correct for `_users` since `_users` is not tenant-prefixed)
When this story is complete
Then all `AccountDAL` methods use the same `{dbConfig.url}/_users/...` pattern — no mixed patterns; `_users` never takes a tenant prefix and must never use `Get_Prefix_DB_Url`

**AC-3 — IUserRepository interface extracted**
Given the full operation set is in `AccountDAL`
When the interface is extracted
Then `IUserRepository` is defined in `mmria.common/SharedLibraries/Account/IUserRepository.cs` with async method signatures matching every `AccountDAL` method; `AccountDAL` declares `public class AccountDAL : IUserRepository`

**AC-4 — AccountManager updated to depend on interface**
Given `AccountManager.cs` currently takes `AccountDAL` as a concrete constructor parameter (line 22, 25)
When this story is complete
Then `AccountManager` takes `IUserRepository` instead; internal `_dal` field type is `IUserRepository`

**AC-5 — ManageUsersDAL _users calls assessed**
Given `ManageUsersDAL` contains 5 `_users` calls (GET, GET-check, PUT, DELETE, `_all_docs`) that duplicate `AccountDAL` operations
When the developer evaluates them
Then a decision is recorded in the catalog: these 5 methods (`GetUserAsync`, `CheckUserAsync`, `PutUserAsync`, `DeleteUserAsync`, `GetAllUsersAsync`) are generic user CRUD — they belong behind `IUserRepository`; `ManageUsersDAL` is updated to inject `IUserRepository` and delegate to it; the 5 direct `_users` HTTP calls in `ManageUsersDAL` are replaced with repository method calls

**AC-6 — DI registration updated**
Given `IUserRepository` is defined
When DI registration is updated in `mmria-server/Program.cs`
Then `services.AddScoped<IUserRepository, AccountDAL>()` is present; any existing `AddScoped<AccountDAL>()` and `AddScoped<AccountManager>()` registrations are updated to reflect the new dependency chain

**AC-7 — Build succeeds with no controller changes**
Given no controllers are changed in this story
When the build runs
Then `mmria-server` and `mmria.common` build with zero errors

---

## Dev Notes — Implementation

### Files to change

| File | Change |
|------|--------|
| `mmria.common/SharedLibraries/Account/DAL/AccountDAL.cs` | **UPDATE** — add missing operations from catalog; ensure consistent URL construction; implement `IUserRepository` |
| `mmria.common/SharedLibraries/Account/IUserRepository.cs` | **CREATE** — interface with async signatures |
| `mmria.common/SharedLibraries/Account/Manager/AccountManager.cs` | **UPDATE** — change `AccountDAL` constructor param to `IUserRepository` |
| `mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs` | **UPDATE** — inject `IUserRepository`; replace 5 direct `_users` calls with delegation |
| `mmria-server/Program.cs` | **UPDATE** — register `IUserRepository` as `AccountDAL`; update dependent registrations |

---

### Current state of AccountDAL (verified 2026-07-14)

`AccountDAL.cs` currently has one `_users` call:
- Line 47: `GetCouchDbUserAsync` — GET user by doc ID (`org.couchdb.user:{name}`)

It also has `AuthenticateWithSessionAsync` (uses `/_session`, not `/_users`) — keep as-is; `_session` is out of scope for `IUserRepository`.

Methods to add (from ManageUsersDAL equivalents, to be the canonical implementations):
- `GetUserAsync(string user_id, DBConfigurationDetail dbConfig)` — GET by user_id
- `CheckUserAsync(string user_id, DBConfigurationDetail dbConfig)` — GET with not-found guard
- `PutUserAsync(user user, DBConfigurationDetail dbConfig)` — PUT create/update
- `DeleteUserAsync(string user_id, string rev, DBConfigurationDetail dbConfig)` — DELETE
- `GetAllUsersAsync(int skip, int take, DBConfigurationDetail dbConfig)` — `_all_docs` paginated

> **Note:** `GetCouchDbUserAsync` takes a `userName` string and builds the full doc ID internally (`org.couchdb.user:{name}`). `GetUserAsync` takes the full `user_id` (already includes `org.couchdb.user:` prefix). These are slightly different overloads — keep both; they serve different callers.

---

### URL construction rule for _users

`_users` is a global CouchDB database — it is **never** tenant-prefixed. Always construct URLs as:

```csharp
// Correct — _users is never prefixed
string url = $"{dbConfig.url}/_users/{userDocId}";

// Wrong — never use Get_Prefix_DB_Url for _users
// string url = dbConfig.Get_Prefix_DB_Url($"_users/{userDocId}");
```

---

### ManageUsersDAL delegation pattern

```csharp
// Before (ManageUsersDAL line 41):
string request_string = db_config.url + "/_users/" + user_id;
string responseFromServer = await _httpClient.ExecuteAsync("GET", request_string, ...);

// After:
return await _userRepository.GetUserAsync(user_id, db_config);
```

`ManageUsersDAL` will still own `BulkUpsertUserRoleJurisdictionsAsync`, `GetAllUserRoleJurisdictionsAsync`, `GetUserRoleJurisdictionAsync`, and other `jurisdiction/` database calls — those are NOT `_users` operations and are not affected.

---

### IUserRepository interface template

```csharp
namespace mmria.common.SharedLibraries.Account;

public interface IUserRepository
{
    Task<user?> GetCouchDbUserAsync(string userName, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<user> GetUserAsync(string userId, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<user> CheckUserAsync(string userId, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<mmria.common.model.couchdb.document_put_response> PutUserAsync(user user, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<System.Dynamic.ExpandoObject> DeleteUserAsync(string userId, string rev, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    Task<mmria.common.model.couchdb.get_response_header<user>> GetAllUsersAsync(int skip, int take, mmria.common.couchdb.DBConfigurationDetail dbConfig);
    // Add any additional methods discovered in the 18.1 catalog
}
```

Verify exact type names and namespaces against existing AccountDAL before finalizing.
