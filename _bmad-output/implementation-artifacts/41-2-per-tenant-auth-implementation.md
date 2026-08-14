# Story 41.2: Per-Tenant Auth — Implementation

Status: backlog

## Story

As a system administrator,
I want each tenant on the multi-tenant server to independently use either SAMS or password authentication,
so that a demo/training tenant can accept password logins while all other tenants remain SAMS-only.

## Acceptance Criteria

1. A tenant configured with `sams:is_enabled: false` in its per-tenant CouchDB config document presents the password login form to users, regardless of the server-level `sams.is_enabled` setting.
2. A tenant configured with `sams:is_enabled: true` (or inheriting the global `true`) routes users to SAMS authentication. Its login flow is unchanged from current behavior.
3. Both auth paths work correctly on the same running server instance with no interference between tenants.
4. The SAMS connection credentials (`client_id`, `client_secret`, `endpoint_*`) remain global — all SAMS tenants use the same SAMS instance.
5. All code paths that read `use_sams` (`AutoLogin`, `SignIn`, `Login GET`, `Logout`, `AppOffline`) correctly use the per-tenant resolved value, not a cached global value.
6. `dotnet build mmria-server.csproj` — zero errors.
7. Tested in the local multi-tenant environment: `tenant1` set to password auth, `tenant2` (or `cdc`) set to SAMS auth — both login flows work correctly from the same running server.

> **Pre-condition:** Story 41.1 (investigation) must be complete. The findings document at `docs/ai/per-tenant-auth-findings.md` defines the exact implementation scope. The tasks below reflect the expected minimal scope based on the architecture analysis; revise after reading Story 41.1 findings.

## Tasks / Subtasks

- [ ] Add per-tenant `sams:is_enabled` override to the demo/training tenant's CouchDB config document (AC: #1)
  - [ ] Set the field identified in Story 41.1 in the tenant's config document via the existing production update script or admin UI
  - [ ] Verify `_configuration.GetBoolean("sams:is_enabled", host_prefix)` resolves `false` for this tenant
- [ ] Verify or fix `AutoLogin` action (AC: #5)
  - [ ] `AutoLogin` reads `use_sams` — confirm it uses the per-tenant resolved value (should already be correct since `use_sams` is set in constructor from per-tenant config)
- [ ] Verify or fix `Login GET` action (AC: #1, #4, #5)
  - [ ] `GET /Account/Login` must check `use_sams` and redirect to `SignIn` only when the current tenant is SAMS-enabled
  - [ ] If the per-tenant value is already resolved in the constructor, no code change is needed — just verify
- [ ] Verify or fix `AppOffline` action (AC: #5)
  - [ ] `AppOffline` should not force a SAMS redirect for a password tenant — confirm it uses the per-tenant `use_sams`
- [ ] Any code changes identified by Story 41.1 findings document (AC: #2, #3, #5)
  - [ ] Implement only the minimal changes listed in `docs/ai/per-tenant-auth-findings.md`
- [ ] Build and test in local multi-tenant environment (AC: #6, #7)
  - [ ] `dotnet build mmria-server.csproj` — zero errors
  - [ ] Login to a password tenant: verify password form shown, login succeeds
  - [ ] Login to a SAMS tenant: verify SAMS redirect occurs, login succeeds
  - [ ] Verify no session bleed between tenants

## Dev Notes

**Key insight from architecture analysis:** `use_sams = _configuration.GetBoolean("sams:is_enabled", host_prefix)` already reads per-tenant from `OverridableConfiguration`. If the per-tenant CouchDB config document supports this key (to be confirmed in Story 41.1), the implementation may be configuration-only with no code changes — just set the key in the tenant config doc.

**SAMS credentials stay global** — `client_id`, `client_secret`, and endpoint URLs in `appsettings.json` are not per-tenant. All SAMS tenants authenticate against the same SAMS instance.

**Primary files (if code changes are needed):**
- `source-code/mmria/mmria-server/Controllers/AccountController.cs`
- `source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs`
- Per-tenant CouchDB config document (via db-redeploy script)

**Depends on:** Story 41.1 complete and `docs/ai/per-tenant-auth-findings.md` written.
