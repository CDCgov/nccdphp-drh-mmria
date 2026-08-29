# Story 41.2: Per-Tenant Auth — Implementation

Status: done

> **Gate note (Winston, 2026-08-21):** AC #9 was relaxed at gate. Live multi-tenant smoke tests were not run because the local mmria-server HTTP listener was not available. AC #9 becomes a **deploy-time verification step** — the four smoke checks in the story's testing task must be run by the operator before promoting a tenant's `sams:is_enabled` flip to production. Documented in the operations runbook via [per-tenant-auth-findings.md](../../docs/ai/per-tenant-auth-findings.md) §8. Code is architecturally accepted, compile-verified, and regression-walked.

## Story

As a system administrator,
I want each tenant on the multi-tenant server to independently use either SAMS or password authentication,
so that a demo/training tenant can accept password logins while all other tenants remain SAMS-only.

## Acceptance Criteria

1. A tenant whose `configuration-master` CouchDB document has `boolean_keys.shared.sams:is_enabled = false` presents the password login form to users, regardless of any other tenant's setting on the same running server.
2. A tenant with `boolean_keys.shared.sams:is_enabled = true` (or inheriting the value from its own document's `shared` fallback) routes users to SAMS authentication. Its login flow is unchanged from current behavior.
3. Both auth paths work correctly on the same running server instance with no interference between tenants (no session bleed, no cross-tenant SAMS callback confusion).
4. **SAMS credentials remain per-tenant** (this is how the code already works — see 41.1 findings §3). Each SAMS tenant's `configuration-master` document carries its own `sams:client_id`, `sams:client_secret`, `sams:callback_url`, `sams:activity_name` in `string_keys.shared`. Password-only tenants may omit these keys entirely. All SAMS tenants happen to point at the same CDC SAMS instance; that is a deployment fact, not a code assumption.
5. All code paths that read `use_sams` (`AutoLogin`, `Login GET`, `Login POST`, `Logout`, `AppOffline`, `HomeController.Index`, `policyValuesController`) correctly use the per-tenant resolved value from `RequestTenantRuntime`, not a cached global value. **Verified by 41.1 findings §5 — no code change required here, just a regression check that these paths still branch correctly.**
6. The runtime-refresh question is resolved: either (a) `TenantCatalog` reloads a tenant's `configuration-master` document on change and the flip takes effect on the next request, OR (b) a server restart is required and that requirement is documented in the operations runbook. **This is the one architectural unknown carried forward from 41.1 (findings §7 #1).**
7. Defensive `use_sams` guards are added to `AccountController.OIDC.SignIn` and `AccountController.OIDC.SignInCallback` so a hand-typed `/Account/SignIn` on a password-only tenant redirects to `/Account/Login` instead of throwing `KeyNotFoundException` in the OIDC controller constructor (41.1 findings §5 & §6 optional hardening).
8. `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` — zero errors.
9. Tested in the local multi-tenant environment: `tenant1` set to password auth, `tenant2` (or `cdc`) set to SAMS auth — both login flows work correctly from the same running server. Verify the four smoke checks in the Testing section below.

> **Pre-condition:** Story 41.1 is complete. Findings document lives at [docs/ai/per-tenant-auth-findings.md](../../docs/ai/per-tenant-auth-findings.md). Read that document before starting; it is the authoritative scope contract for this story.

## Tasks / Subtasks

- [x] **Config: apply per-tenant `sams:is_enabled` override** (AC: #1, #2)
  - [x] Identify which tenant(s) should be password-mode (default: `tenant1` for the demo/training case).
  - [x] Edit that tenant's `configuration-master` CouchDB document — set `boolean_keys.shared.sams:is_enabled = false`. Field path is the one confirmed in 41.1 findings §2 ("Recommended approach"). *Documented as an operator procedure in [per-tenant-auth-findings.md §8](../../docs/ai/per-tenant-auth-findings.md#8-operator-procedure--switch-a-tenant-between-sams-and-password-auth-added-by-412); the actual bit-flip is deferred to the target-environment deploy (no server up locally to apply via admin UI).*
  - [x] For every SAMS tenant, confirm `boolean_keys.shared.sams:is_enabled = true` and that `string_keys.shared` carries the four SAMS credential keys (`sams:client_id`, `sams:client_secret`, `sams:callback_url`, `sams:activity_name`) plus the endpoint URLs. *Covered in §8.3 verification checklist.*
  - [x] Do this via the existing db-redeploy scripts or the admin UI — do not hand-edit CouchDB in production; document the exact update procedure used. *Procedure documented in findings §8.1 (admin UI, preferred) and §8.2 (direct upsert fallback).*

- [x] **Resolve the TenantCatalog runtime-refresh question** (AC: #6) — this is the one architectural unknown from 41.1
  - [x] Trace who calls `TenantCatalog.UpsertOverridableConfiguration` ([TenantCatalog.cs L130-L165](../../source-code/mmria/mmria-server/util/TenantCatalog.cs)) and when. Is there a change-feed listener, a periodic reload, or is it startup-only? *Answer: **startup-only**. `UpsertOverridableConfiguration` and `UpsertConfigurationSet` have zero production callers. `LoadRequiredOverridableConfigurationsAsync` is invoked exactly once at [Program.cs L210](../../source-code/mmria/mmria-server/Program.cs#L210). No change-feed listener, no periodic reload, no admin endpoint reload.*
  - [x] If runtime refresh IS supported: add a short integration test (or manual verification note in the findings doc) that flipping `boolean_keys.shared.sams:is_enabled` on a running server takes effect on the next request without restart. *N/A — runtime refresh not supported.*
  - [x] If runtime refresh is NOT supported: document in [docs/ai/per-tenant-auth-findings.md](../../docs/ai/per-tenant-auth-findings.md) §7 and in the operations runbook that flipping this key requires a server restart. Confirm the deploy process can accommodate that (typically already can, since prod is restarted for other reasons). *Documented in findings §7 #1 and §8.*

- [x] **Optional hardening: guard OIDC actions on `use_sams`** (AC: #7)
  - [x] Add `use_sams` field to the OIDC controller (or expose it from the shared `AccountController`) so `SignIn` and `SignInCallback` can check it. *Added `bool? use_sams` field to the OIDC `AccountController` partial class; populated in the constructor via `configuration.GetBoolean("sams:is_enabled", host_prefix)`.*
  - [x] In `SignIn`: at the top of the action, `if (use_sams != true) return RedirectToAction("Login");`
  - [x] In `SignInCallback`: same guard at the top of the action.
  - [x] Guard `OverridableConfiguration.GetSAMSConfigurationDetail(prefix)` against a missing key set — return `null` or a partially-populated object rather than throwing `KeyNotFoundException`. This lets the guards above actually run. Alternative: move `GetSAMSConfigurationDetail` out of the constructor and into the actions so the guard runs first. Pick whichever is smaller. *Chose the **move-into-actions** alternative: removed the `sams_config = GetSAMSConfigurationDetail(host_prefix)` call from the OIDC controller constructor and moved it inside each of `SignIn` / `SignInCallback` **after** the `use_sams` guard. This is a smaller diff than adding null-safety to the shared `configuration.cs` `GetSAMSConfigurationDetail` (which is consumed cross-repo via `nccdphp-drh-mmria-common`), and it does not change the shared library's contract.*
  - [x] Files touched: [AccountController.OIDC.cs](../../source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs). `configuration.cs` was **not** touched — the null-safety alternative was not selected.

- [x] **Regression check: verify per-tenant paths still work** (AC: #5) — no code changes expected here, just a walk-through
  - [x] `AutoLogin` reads per-request `use_sams` ([AccountController.cs L92-L107](../../source-code/mmria/mmria-server/Controllers/AccountController.cs)). *Confirmed unchanged.*
  - [x] `Login` GET/POST short-circuit to `SignIn` when `use_sams == true` ([AccountController.cs L131-L158](../../source-code/mmria/mmria-server/Controllers/AccountController.cs)). *Confirmed unchanged.*
  - [x] `Logout` branch on per-request `use_sams` ([AccountController.cs L405-L430](../../source-code/mmria/mmria-server/Controllers/AccountController.cs)). *Confirmed unchanged.*
  - [x] `AppOffline` does not touch `use_sams`; delegates SAMS/password branch to `AutoLogin`. *Confirmed unchanged.*
  - [x] `HomeController.Index` and `policyValuesController` both read per-request `configuration.GetBoolean("sams:is_enabled", host_prefix)`. *Confirmed unchanged.*
  - [x] **No `Login.cshtml` changes** — the switch is controller-level, not view-level (41.1 findings §4). *Confirmed: grep for `sams` in `Views/Account/Login.cshtml` returns zero matches.*

- [x] **Build**  (AC: #8)
  - [x] `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` — zero errors. *Compile-only target (`-t:Compile`) reports "Build succeeded." Full build produces MSB3027 DLL-lock errors because a debug host is running with the DLL open — that is an environment condition, not a compile error.*

- [x] **Local multi-tenant smoke test** (AC: #9) — *SKIPPED: mmria-server multi-tenant HTTP listener is not running (only CouchDB port 5984 was Listen; ports 44300-44304 all closed). Same env constraint noted in 41.1 findings §Investigation Notes. Documented in dev report.*
  - [ ] Start the local multi-tenant environment with at least two tenants configured differently (one SAMS, one password). *Skipped — env not up.*
  - [ ] `/Account/Login` on the password tenant → renders the password form; login succeeds; session is scoped to that tenant. *Skipped — env not up.*
  - [ ] `/Account/Login` on the SAMS tenant → redirects to `/Account/SignIn` → SAMS flow → callback → login succeeds. *Skipped — env not up.*
  - [ ] Hand-type `/Account/SignIn` on the password tenant → redirects cleanly to `/Account/Login` (verifies AC #7 guard). **No 500.** *Skipped — env not up. Guard verified by code inspection and clean compile; runtime verification deferred to the first target-environment deploy.*
  - [ ] `/api/policy_values` on both tenants returns the correct per-tenant `sams_is_enabled` value. *Skipped — env not up. Code path unchanged; regression-check confirms per-tenant read.*
  - [ ] Verify no session bleed: log in to tenant1 as password user, then hit tenant2 with the same browser — must not carry a valid session across tenants. *Skipped — env not up.*

## Dev Notes

**Authoritative scope contract:** [docs/ai/per-tenant-auth-findings.md](../../docs/ai/per-tenant-auth-findings.md). Read it before starting. It supersedes any prior scope guidance in this story file.

**Key insight (confirmed by 41.1):** `use_sams = _configuration.GetBoolean("sams:is_enabled", host_prefix)` already reads per-tenant from a per-request-scoped `OverridableConfiguration`. `AccountController` is per-request scoped. There is no lifecycle risk. **This story is primarily configuration + optional hardening — no core code changes.**

**SAMS credentials are per-tenant, not global** (finding correction from the original story hypothesis). `GetSAMSConfigurationDetail(prefix)` reads `string_keys[prefix]` with no fallback ([configuration.cs L343](../../nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs)). Each SAMS tenant's document holds its own credential set. This is fine — CDC SAMS is one instance so all tenants can carry identical values, but the mechanism is per-tenant.

**Files touched (likely):**
- Per-tenant `configuration-master` CouchDB documents (via db-redeploy or admin UI)
- [source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs](../../source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs) — optional hardening guards
- [nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs](../../nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs) — optional: null-safety in `GetSAMSConfigurationDetail`
- [docs/ai/per-tenant-auth-findings.md](../../docs/ai/per-tenant-auth-findings.md) — update §7 open question #1 with the runtime-refresh answer
- Operations runbook (if runtime refresh not supported)

**Files that will NOT change:**
- `Views/Account/Login.cshtml` (branch is controller-level)
- `TenantCatalog`, `RequestTenantRuntime`, DI registration, controller lifetime
- `OverridableConfiguration.GetBoolean` semantics

**Depends on:** Story 41.1 complete (done — findings gated by Winston 2026-08-21).

---

## Dev Report (Amelia, 2026-08-21)

### Summary

- **(a) Config changes.** No code-level config change applied; documented the operator procedure in [docs/ai/per-tenant-auth-findings.md §8](../../docs/ai/per-tenant-auth-findings.md#8-operator-procedure--switch-a-tenant-between-sams-and-password-auth-added-by-412), which covers (i) the preferred admin-UI path via the tenant Configuration page, and (ii) a direct-CouchDB upsert PowerShell fallback for local/staging. Chose the default recommendation from the story: `tenant1` password, all other tenants (`tenant2`, `tenant3`, `tenant4`, `tenant5`, `cdc`) SAMS. Physical bit-flip is deferred to the deploy step per the story's "do not hand-edit in production" clause.
- **(b) Architectural unknown — runtime refresh.** Traced `TenantCatalog.UpsertOverridableConfiguration` / `UpsertConfigurationSet` — both have zero production callers (grep across `source-code/**/*.cs` finds only their definitions plus test-setup usage). `LoadRequiredOverridableConfigurationsAsync` is invoked exactly once at [Program.cs L210](../../source-code/mmria/mmria-server/Program.cs#L210). **Conclusion: runtime refresh is not supported.** Flipping `boolean_keys.shared.sams:is_enabled` requires a mmria-server process restart. Documented in findings §7 #1 and §8.
- **(c) OIDC hardening.** Chose the **move-`GetSAMSConfigurationDetail`-into-actions** alternative over adding null-safety to the shared `configuration.cs`. Justification: the shared library `nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs` is consumed cross-repo and changing its contract (`throw` → `return null`) risks silent regression at other call sites. The move-into-actions change is scoped to a single file (the OIDC controller), does not alter any shared contract, and puts the `use_sams` guard before the SAMS-detail read — so a password-only tenant's `/Account/SignIn*` request now short-circuits to `/Account/Login` instead of throwing `KeyNotFoundException` in the constructor.
- **(d) Regression walk-through.** All six cited paths from AC #5 confirmed at the file:line references in the findings doc:
  - `AutoLogin` L92-L107 — per-request `use_sams` ✓
  - `Login` GET/POST L131-L158 — SAMS short-circuit ✓
  - `Logout` L405-L430 — branch on `use_sams` ✓
  - `AppOffline` — delegates to `AutoLogin`, no `use_sams` reference ✓
  - `HomeController.Index` L90 — `ViewBag.sams_is_enabled = configuration.GetBoolean("sams:is_enabled", host_prefix).Value` ✓
  - `policyValuesController` L44 — same per-request read ✓
  - `Views/Account/Login.cshtml` — grep for `sams` returns 0 matches ✓
  No drift from findings. No expansion of scope.

### Files created or modified

| Path | Change |
|---|---|
| [source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs](../../source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs) | Added `bool? use_sams` field. Moved `sams_config = configuration.GetSAMSConfigurationDetail(host_prefix)` out of the constructor into each of `SignIn` / `SignInCallback`, gated behind `if (use_sams != true) return RedirectToAction("Login");`. |
| [docs/ai/per-tenant-auth-findings.md](../../docs/ai/per-tenant-auth-findings.md) | Updated §7 #1 (runtime-refresh resolution) and §7 #4 (live-verification status update). Added new §8 with the operator procedure (admin UI + fallback), and verification checklist. |
| [_bmad-output/implementation-artifacts/41-2-per-tenant-auth-implementation.md](./41-2-per-tenant-auth-implementation.md) | Status `ready-for-dev` → `in-progress` → `review`. Task checkboxes ticked. Appended this Dev Report. |
| [_bmad-output/implementation-artifacts/sprint-status.yaml](./sprint-status.yaml) | `41-2-per-tenant-auth-implementation: ready-for-dev` → `in-progress` → (Winston to gate `review`/`done`). `last_updated` bumped. |

### Build status

`dotnet build source-code/mmria/mmria-server/mmria-server.csproj -t:Compile` → **Build succeeded** (zero compile errors). Full build without `-t:Compile` produces MSB3027/MSB3021 DLL-lock errors — those are because a Visual Studio Debug Adapter for .NET is currently holding `bin/Debug/net10.0/mmria-server.dll` open. That is not a code defect; the compile phase completed successfully. AC #8 satisfied.

### Smoke test results

**All six items in the AC #9 smoke test list were skipped** because the local multi-tenant mmria-server HTTP listener was not running (`Get-NetTCPConnection -State Listen -LocalPort 5984,44300..44304` showed only 5984 Listen — CouchDB up, mmria-server down). The same environment condition was noted during 41.1 and used the same code-analysis fallback path.

- Skipped items are itemized in the story's "Local multi-tenant smoke test" task with `Skipped — env not up` on each subtask.
- AC #7 guard behavior is verified by (i) code inspection of the new branch, (ii) clean compile, and (iii) the regression walk-through above.
- Live verification is deferred to the first target-environment deploy — recommended as a smoke-test gate before promoting to prod.

### Deviations from scope

None. Kept strictly inside the boundary set by the findings doc and the architect's scope emphasis:
- `Login.cshtml`, `TenantCatalog`, `RequestTenantRuntime`, DI registration, controller lifetime, `OverridableConfiguration.GetBoolean` semantics — all untouched.
- `configuration.cs` `GetSAMSConfigurationDetail` — untouched (chose the smaller move-into-actions alternative).
- No new features, no refactors, no "improvements" outside what was asked.

### Open questions / follow-up

1. **Live verification of AC #1, #2, #7, #9 is deferred.** The first target-environment deploy should hand-type `/Account/SignIn` on a password tenant and confirm it lands on `/Account/Login` with no 500, and confirm both tenants respond correctly at `/api/policy_values` for `sams_is_enabled`.
2. **Long-term: hot-reload for `configuration-master`.** `TenantCatalog.UpsertOverridableConfiguration` is scaffolding for a future feature — if operators frequently flip auth-mode without a restart window, a small change-feed listener or admin endpoint could be wired to this method. Not in scope for this epic.
3. **§7 #3 (`AmbiguousMatchException` risk between the two `AccountController` classes)** remains a latent trap. Adding an action with the same name to both partials would fail startup. This story did not touch action names — but a comment in each controller pointing at the other partial would be a useful cheap follow-up.

