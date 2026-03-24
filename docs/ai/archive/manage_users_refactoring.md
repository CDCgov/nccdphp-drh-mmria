# Manage Users Refactoring — Controller → Manager/DAL

- Status: Historical
- Scope: Earlier Manage Users controller-to-manager extraction notes and test ideas.
- When to use: Use only when tracing the original Manage Users migration work.
- Last verified: 2026-03-24
- Related docs: [Controller to SharedLibraries Migration Matrix](../controller_sharedlibraries_migration_matrix.md)

> Historical note: This file is preserved for migration history. It is not the canonical guide for new refactors

What Was Done

Three controller actions from `userController` and one from `user_role_jurisdictionController` were extracted into `ManageUsersManager` and `ManageUsersDAL` in the common project. The controllers now delegate to the manager; business logic and data access live in the common layer.

---

## Controllers Modified

### `userController` (`source-code/mmria/mmria-server/Controllers/api/userController.cs`)

| Original Action | HTTP | Route | Became Manager Method |
|---|---|---|---|
| `Post(user user)` | POST | `api/user` | `SaveUserAsync` |
| `Delete(user_id, rev)` | DELETE | `api/user` | `DeleteUserAsync` (auth check stays in controller) |
| `CheckUser(string id)` | GET | `api/user/check-user/{id}` | `CheckUserAsync` (now returns `IActionResult` 200/404) |

### `user_role_jurisdictionController` (`source-code/mmria/mmria-server/Controllers/api/user_role_jurisdictionController.cs`)

| Original Action | HTTP | Route | Became Manager Method |
|---|---|---|---|
| `PostBulk(List<user_role_jurisdiction>)` | POST | `api/user_role_jurisdiction/bulk` | `SaveUserRoleJurisdictionsAsync` (auth loop stays in controller) |

---

## Manager Created

**`ManageUsersManager`** (`nccdphp-drh-mmria-common/mmria.common/SharedLibraries/ManageUsers/Manager/ManageUsersManager.cs`)

| Method | Returns | Description |
|---|---|---|
| `CheckUserAsync(string user_id, DBConfigurationDetail db_config)` | `Task<bool>` | Returns `true` if user exists, `false` if not found |
| `SaveUserAsync(user user, DBConfigurationDetail db_config)` | `Task<document_put_response>` | Create or update user. Rejects duplicates on create (when `_rev` is null). Applies `app_prefix_list` logic. |
| `DeleteUserAsync(string user_id, string rev, DBConfigurationDetail db_config)` | `Task<ExpandoObject>` | Hard delete or prefix removal depending on how many prefixes the user has. |
| `SaveUserRoleJurisdictionsAsync(List<user_role_jurisdiction>, DBConfigurationDetail db_config)` | `Task<List<document_put_response>>` | Bulk create/update `user_role_jurisdiction` records via CouchDB `_bulk_docs`. |

---

## DAL Created

**`ManageUsersDAL`** (`nccdphp-drh-mmria-common/mmria.common/SharedLibraries/ManageUsers/DAL/ManageUsersDAL.cs`)

| Method | Returns | Description |
|---|---|---|
| `GetUserAsync(string user_id, DBConfigurationDetail db_config)` | `Task<user>` | GET `/_users/{id}`. Returns null if not found. Used internally by `DeleteUserAsync`. |
| `CheckUserAsync(string user_id, DBConfigurationDetail db_config)` | `Task<user>` | GET `/_users/{id}`. Returns empty `user` (never null) if not found or on error. Used for existence checks. |
| `PutUserAsync(user user, DBConfigurationDetail db_config)` | `Task<document_put_response>` | PUT `/_users/{id}`. Creates or updates a user document. |
| `DeleteUserAsync(string user_id, string rev, DBConfigurationDetail db_config)` | `Task<ExpandoObject>` | DELETE `/_users/{id}?rev={rev}`. Hard delete. |
| `BulkUpsertUserRoleJurisdictionsAsync(List<user_role_jurisdiction>, DBConfigurationDetail db_config)` | `Task<List<document_put_response>>` | POST `/{prefix}jurisdiction/_bulk_docs`. Bulk create/update role assignments. |

---

## Also Modified

**`authorization.cs`** (`nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Other/authorization.cs`)

Added a new pure-function overload (no `ClaimsPrincipal` dependency, directly testable):

```csharp
is_authorized_to_handle_jurisdiction_id(
    HashSet<(string jurisdiction_id, ResourceRightEnum ResourceRight)> jurisdiction_hashset,
    ResourceRightEnum p_resource_action,
    user_role_jurisdiction p_user_role_jurisdiction
) → bool
```

The server's `authorization_user.cs` overload 2 now delegates to this.

---

## Test Coverage Needed

| Manager Method | Test Cases Needed |
|---|---|
| `CheckUserAsync` | ✅ Exists → true, ✅ Not found → false, ✅ Duplicate detected before create |
| `SaveUserAsync` — create | Succeeds with valid new user; rejects duplicate (no `_rev`); applies `__no_prefix__` when prefix is empty; applies prefix when prefix is set |
| `SaveUserAsync` — update | Succeeds with valid `_rev`; skips duplicate check on update |
| `DeleteUserAsync` — hard delete | Deletes when user has only one prefix; returns `ok` response |
| `DeleteUserAsync` — prefix removal | Removes prefix only when user has multiple prefixes; user doc still exists after |
| `SaveUserRoleJurisdictionsAsync` | Bulk creates roles; returns correct `_rev` values; handles partial failures |
| `authorization.is_authorized_to_handle_jurisdiction_id` (pure overload) | Authorized when jurisdiction matches; denied when jurisdiction does not match; `installation_admin` bypasses jurisdiction check (top `/` folder) |



