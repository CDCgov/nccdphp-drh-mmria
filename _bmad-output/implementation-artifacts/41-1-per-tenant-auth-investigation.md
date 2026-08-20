# Story 41.1: Per-Tenant Auth — Investigation and Configuration Analysis

Status: ready-for-dev

## Story

As a developer,
I want to understand exactly what is needed to enable SAMS auth on some tenants and password auth on others within the same server instance,
so that the implementation story (41.2) has no unresolved architectural questions.

## Acceptance Criteria

1. Document whether `OverridableConfiguration.GetBoolean("sams:is_enabled", host_prefix)` already supports a per-tenant override that differs from the global `appsettings.json` `sams.is_enabled` value. Confirm with a local test: set `sams:is_enabled` to `false` for `tenant1` in its CouchDB config document while the global setting is `true`, and verify `AccountController.use_sams` resolves to `false` for `tenant1` requests.
2. Document whether the SAMS connection details (`client_id`, `client_secret`, `endpoint_*` URLs) need to be per-tenant or whether shared global values work for all SAMS tenants (expected: shared is correct since all tenants use the same SAMS instance).
3. Identify the exact CouchDB config document field path that controls the per-tenant SAMS setting (e.g., `sams:is_enabled` under the per-tenant config document `string_keys` or as a top-level key). Confirm it is readable via `GetBoolean`.
4. Verify the Login page (`Views/Account/Login.cshtml`) correctly renders either the SAMS redirect button or the password form based on `ViewBag.sams_is_enabled` — confirm no additional view changes are needed for a mixed-tenant server.
5. Identify any other code paths (OIDC callback, `SignIn` action, `AutoLogin`, `AppOffline`) that need guarding to prevent cross-tenant auth leakage when one tenant is SAMS and another is password.
6. Produce a written findings document (committed to `docs/ai/` or similar) summarizing the above with: current state, what works already, and the minimal change list for Story 41.2.

## Tasks / Subtasks

- [ ] Test `OverridableConfiguration` per-tenant override of `sams:is_enabled` (AC: #1)
  - [ ] In the local multi-tenant environment, add `sams:is_enabled: false` to the `tenant1` CouchDB config document (whichever field path `GetBoolean("sams:is_enabled", "tenant1")` reads)
  - [ ] With global `sams.is_enabled: true` in appsettings, make a request to a `tenant1` route and verify `use_sams` resolves to `false` (add a debug log or breakpoint in `AccountController` constructor)
- [ ] Confirm SAMS credential sharing (AC: #2)
  - [ ] Verify that `endpoint_authorization`, `client_id`, `client_secret` are read globally (from `_configuration` without `host_prefix`) — confirm the OIDC flow uses the same global SAMS instance for all tenants
- [ ] Find the exact config key path (AC: #3)
  - [ ] Search for `GetBoolean("sams:is_enabled"` in `AccountController.cs` — the key is `"sams:is_enabled"` and `host_prefix` is the tenant prefix
  - [ ] Confirm that the per-tenant CouchDB config document supports overriding this key
- [ ] Review Login.cshtml for SAMS/password branch rendering (AC: #4)
  - [ ] Confirm `ViewBag.sams_is_enabled` is the correct switch in the view
  - [ ] Verify the view correctly shows the password form when `sams_is_enabled = false` and SAMS redirect when `true`
- [ ] Review cross-tenant auth paths (AC: #5)
  - [ ] `AutoLogin`: redirects to `SignIn` if `use_sams` — verify this is per-tenant
  - [ ] `SignIn` (OIDC initiation): verify it checks `use_sams` before building the SAMS redirect
  - [ ] `AppOffline`: verify it does not force SAMS redirect for a password tenant
- [ ] Write findings document (AC: #6)
  - [ ] Create `docs/ai/per-tenant-auth-findings.md`
  - [ ] Include: what already works, what needs changing, exact field path for per-tenant config, code paths requiring changes in Story 41.2

## Dev Notes

**Current auth resolution in `AccountController`:**
```csharp
use_sams = _configuration.GetBoolean("sams:is_enabled", host_prefix);
```
`_configuration` is `OverridableConfiguration` — already per-tenant by design. `host_prefix` is the tenant identifier. This is the key insight: if `OverridableConfiguration` already supports per-tenant override of this key, the entire feature may be a configuration-only change with minor view verification.

**Field path to test:** The per-tenant CouchDB config document stores overridable values. The key format used in `GetBoolean` calls is `"section:key"` with the tenant's `host_prefix`. Determine the exact JSON structure that places a `sams:is_enabled` value in the per-tenant config doc.

**What to look for in `OverridableConfiguration.GetBoolean`:** Whether it checks the per-tenant config doc before the global `OverridableConfiguration`, and whether the `"sams:is_enabled"` key is in the overridable key set.

**Output:** The findings document from this story is the primary input to Story 41.2 (implementation). Story 41.2 cannot begin until this investigation is complete.
