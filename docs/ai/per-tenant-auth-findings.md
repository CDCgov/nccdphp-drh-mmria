# Per-Tenant Authentication (SAMS + Password) — Findings

**Story:** 41.1 (investigation)
**Baseline commit:** `f526e65ec339b0b936eddf8fc6a98e681021bccd`
**Author:** Amelia (dev) on behalf of Winston (architect)
**Date:** 2026-08-21

---

## Executive Summary

**Per-tenant SAMS on/off is already fully supported by the existing code — Story 41.2 is a configuration-only change with zero required code changes.**

In multi-tenant mode each tenant loads its own `OverridableConfiguration` CouchDB document, `AccountController` is instantiated fresh per HTTP request with a scoped `RequestTenantRuntime`, and every SAMS decision (`AutoLogin`, `Login` GET/POST, `Logout`, `HomeController`, `policyValuesController`, `Profile`) reads `sams:is_enabled` through that per-request, per-tenant configuration object. Flipping `sams:is_enabled` in a single tenant's `configuration-master` document is sufficient to switch that one tenant to password auth while others keep using SAMS.

Two follow-up notes for 41.2 planning:

1. The `SignIn` OIDC action does not itself guard on `use_sams`. This is not a bug today (nothing routes to `SignIn` for a password tenant), but 41.2 should decide whether to add a defensive guard.
2. `OverridableConfiguration.GetSAMSConfigurationDetail(prefix)` reads SAMS credential keys **without** a fallback to `shared`. It is only invoked by the OIDC controller constructor, which is only instantiated when `/Account/SignIn*` is routed — i.e., only for SAMS tenants — so password-only tenants do not need SAMS credential keys populated.

---

## Investigation Notes (Local Test vs Code Analysis)

The story allows a local multi-tenant HTTP test for AC #1. At the time of investigation, CouchDB (port 5984) was up but no mmria-server multi-tenant HTTP listener was detected (`Get-NetTCPConnection -State Listen -LocalPort 5984,44300..44304` showed only 5984). Per the story's fallback direction, evidence for AC #1 comes from **code analysis of `OverridableConfiguration.GetBoolean` plus the multi-tenant configuration loader** rather than a live HTTP round-trip. All citations are file:line against the baseline commit.

---

## 1. Current State — How `use_sams` Is Resolved (AC #1, AC #5)

### 1.1 `AccountController` lifecycle

ASP.NET Core instantiates MVC controllers **once per request** via the default `IControllerActivator`. `RequestTenantRuntime` is explicitly registered as scoped and built from the request's `Host` header:

- [source-code/mmria/mmria-server/Program.cs](source-code/mmria/mmria-server/Program.cs#L589-L604) — `builder.Services.AddScoped<RequestTenantRuntime>` that reads `accessor.HttpContext?.Request.Host.GetPrefix()` and calls `tenantCatalog.TryResolveConfiguration(hostPrefix)` **on every request**.
- [source-code/mmria/mmria-server/util/RequestTenantRuntime.cs](source-code/mmria/mmria-server/util/RequestTenantRuntime.cs#L26) — `EffectiveHostPrefix` is captured at construction time from the request host.
- [source-code/mmria/mmria-server/Controllers/AccountController.cs](source-code/mmria/mmria-server/Controllers/AccountController.cs#L49-L72) — constructor takes the scoped `RequestTenantRuntime`, sets `host_prefix = tenantRuntime.EffectiveHostPrefix;` and computes `use_sams = _configuration.GetBoolean("sams:is_enabled", host_prefix);` in the constructor body.

**Verdict:** no singleton or cached state; `use_sams` is recomputed on every request for the tenant that owns that request. There is no risk of cross-tenant leakage via a stale field.

### 1.2 `OverridableConfiguration.GetBoolean(key, prefix)` semantics

[nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs](nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs#L146-L159):

```csharp
public bool? GetBoolean(string key, string prefix)
{
    if (prefix.Equals("shared", StringComparison.OrdinalIgnoreCase)) return GetSharedBoolean(key);

    if (boolean_keys.ContainsKey(prefix))
    {
        if (boolean_keys[prefix].ContainsKey(key))
        {
            return boolean_keys[prefix][key];   // per-prefix override
        }
    }

    return GetSharedBoolean(key);               // fallback to "shared"
}
```

Behavior:

1. `prefix == "shared"` → read `boolean_keys["shared"][key]`.
2. Otherwise, if `boolean_keys[prefix]` has an entry for `key`, return that per-prefix override.
3. Otherwise, fall back to `boolean_keys["shared"][key]`.

**Per-prefix override IS supported by design and does not require code changes.**

### 1.3 How the tenant document is chosen in multi-tenant mode

Each tenant has its **own** `OverridableConfiguration` document loaded from its own CouchDB database:

- [nccdphp-drh-mmria-common/.../MultiTenantConfigurationLoader.cs](nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/MultiTenantConfigurationLoader.cs#L188-L204) — `LoadRequiredOverridableConfigurationsAsync` iterates `tenants[]` and loads one document per tenant, tagging each with `_id = "{tenant}_{sharedConfigId}"`.
- [source-code/mmria/mmria-server/util/TenantCatalog.cs](source-code/mmria/mmria-server/util/TenantCatalog.cs#L36-L58) — `TryResolveConfiguration(hostPrefix)` picks the config whose `_id` starts with `"{hostPrefix}_"` (see `MatchesTenantConfiguration` on [line 240-250](source-code/mmria/mmria-server/util/TenantCatalog.cs#L240-L250)).

**Consequence:** in multi-tenant mode, each tenant already reads from a physically distinct document. The `boolean_keys.shared.sams:is_enabled` value in `tenant1`'s document is independent of the value in `tenant2`'s document. Per-tenant override is achieved simply by editing that value in the per-tenant document — no need to rely on the `boolean_keys[prefix]` layer inside a single doc (though that layer still works if used).

### 1.4 Every call site that reads `sams:is_enabled` uses the per-request configuration

| Call site | File / Line | Uses per-request tenant? |
|---|---|---|
| `AccountController.use_sams` | [AccountController.cs](source-code/mmria/mmria-server/Controllers/AccountController.cs#L72) | Yes — scoped `RequestTenantRuntime` |
| `HomeController.Index` `ViewBag.sams_is_enabled` | [HomeController.cs](source-code/mmria/mmria-server/Controllers/HomeController.cs#L90) | Yes — same pattern |
| `policyValuesController.sams_is_enabled` | [policyValuesController.cs](source-code/mmria/mmria-server/Controllers/api/policyValuesController.cs#L44) | Yes — same pattern |
| `_config.cs` seed value | [_config.cs](source-code/mmria/mmria-server/Controllers/_config.cs#L240-L248) | Startup seed only; writes to `boolean_keys["shared"]` |

---

## 2. Per-Tenant Override Mechanism (AC #3) — Exact CouchDB Field Path

Each tenant's `configuration-master` document (`_id = "{tenant}_{shared_config_id}"`, e.g. `tenant1_shared_config`) lives in **that tenant's own CouchDB database**. The document schema is defined on [configuration.cs lines 127-165](nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs#L127-L165).

### Recommended approach — edit `boolean_keys.shared.sams:is_enabled` in the tenant's own document

This is the simplest and matches how the existing seed code writes the value ([_config.cs line 248](source-code/mmria/mmria-server/Controllers/_config.cs#L248)).

Example `configuration-master` document for a **password-only** tenant (`tenant1`):

```json
{
  "_id": "tenant1_shared_config",
  "_rev": "…",
  "data_type": "configuration-master",
  "boolean_keys": {
    "shared": {
      "sams:is_enabled": false,
      "is_offline_mode_enabled": false,
      "is_offline_logging_enabled": false,
      "is_schedule_enabled ": true,
      "multi_tenant_db_rebuild": true,
      "is_db_check_enabled": false,
      "is_environment_based": true,
      "is_development": false,
      "use_development_settings": false
    }
  },
  "string_keys": {
    "shared": {
      "couchdb_url": "…",
      "db_prefix": "tenant1_",
      "timer_user_name": "…",
      "timer_value": "…",
      "vitals_url": "…",
      "sams:endpoint_authorization": "…",
      "sams:logout_url": "…"
      /* no sams:client_id / sams:client_secret / sams:callback_url / sams:activity_name needed */
    }
  },
  "integer_keys": { "shared": { "session_idle_timeout_minutes": 70 /* … */ } }
}
```

Example for a **SAMS** tenant (`tenant2`) — same doc in `tenant2`'s DB with:

```json
"boolean_keys": { "shared": { "sams:is_enabled": true, /* … */ } },
"string_keys":  {
  "shared": {
    "sams:client_id":       "…",
    "sams:client_secret":   "…",
    "sams:callback_url":    "https://tenant2.example.com/Account/SignInCallback",
    "sams:activity_name":   "…",
    "sams:endpoint_authorization": "…",
    "sams:endpoint_token":         "…",
    "sams:endpoint_user_info":     "…",
    "sams:logout_url":             "…"
    /* … */
  }
}
```

### Alternative approach — per-prefix override inside a single tenant document

If a deployment prefers to keep a single "central" configuration document and layer overrides on top, `GetBoolean` also supports:

```json
"boolean_keys": {
  "shared":  { "sams:is_enabled": true },
  "tenant1": { "sams:is_enabled": false }   /* wins for GetBoolean("sams:is_enabled", "tenant1") */
}
```

This works because of the fallback logic in `GetBoolean` (see §1.2). It is not required for the multi-tenant deployment because each tenant already has its own document, but it is available.

---

## 3. SAMS Credentials — Per-Tenant, Not Globally Shared (AC #2)

The story hypothesized SAMS credentials were shared globally. **The code says otherwise.** Credential fields are read per-prefix with no fallback:

[configuration.cs lines 419-427](nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs#L419-L427):

```csharp
public SAMSConfigurationDetail GetSAMSConfigurationDetail(string prefix)
{
    SAMSConfigurationDetail result = new();
    result.client_id     = string_keys[prefix]["sams:client_id"];
    result.client_secret = string_keys[prefix]["sams:client_secret"];
    result.callback_url  = string_keys[prefix]["sams:callback_url"];
    result.activity_name = string_keys[prefix]["sams:activity_name"];
    return result;
}
```

This is invoked in the OIDC controller constructor:

- [AccountController.OIDC.cs line 102](source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs#L102) — `sams_config = configuration.GetSAMSConfigurationDetail(host_prefix);`

The endpoint URLs (`sams:endpoint_authorization`, `sams:endpoint_token`, `sams:endpoint_user_info`, `sams:endpoint_token_validation`, `sams:endpoint_user_info_sys`, `sams:logout_url`) are read via `GetString(...)` which **does** fall back to the tenant document's `shared` section ([configuration.cs lines 178-190](nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs#L178-L190)).

**Practical implication for 41.2:** In multi-tenant mode each tenant document's `string_keys.shared` carries its own SAMS credential set. This is fine — CDC's SAMS instance is the same across tenants, so each tenant document can simply hold the same client_id/client_secret. Nothing to change in code; nothing to change in schema. Password-only tenants can omit the SAMS credential keys because their controller path never calls `GetSAMSConfigurationDetail`.

---

## 4. Login.cshtml Verification (AC #4)

`Views/Account/Login.cshtml` **does not** reference `ViewBag.sams_is_enabled` (grep confirms zero matches for `sams` in [Login.cshtml](source-code/mmria/mmria-server/Views/Account/Login.cshtml)).

The SAMS/password branch is enforced **at the controller layer, not in the view.** Both the GET and POST `Login` actions short-circuit to `SignIn` before rendering the view when `use_sams == true`:

- [AccountController.cs GET Login line 137-138](source-code/mmria/mmria-server/Controllers/AccountController.cs#L137-L138) — `if (use_sams.HasValue && use_sams.Value) return RedirectToAction("SignIn");`
- [AccountController.cs POST Login line 155-158](source-code/mmria/mmria-server/Controllers/AccountController.cs#L155-L158) — same guard.

Consequence: SAMS tenants never render `Login.cshtml`; password tenants always render the plain password form. **No view changes needed for a mixed-tenant server.**

(For completeness: `ViewBag.sams_is_enabled` **is** used in `Views/Account/SignInCallback.cshtml` line 28 and `Views/Account/Profile.cshtml` line 23. Both are set from the per-request `use_sams`, so they are also correctly per-tenant.)

---

## 5. Cross-Tenant Path Audit (AC #5)

All findings below assume the standard case: a single mmria-server process serving multiple tenants distinguished by host header.

### `AutoLogin`

[AccountController.cs lines 92-107](source-code/mmria/mmria-server/Controllers/AccountController.cs#L92-L107)

```csharp
if (use_sams.HasValue && use_sams.Value)
    return RedirectToAction("SignIn", new { returnUrl });
// else → /Account/Login
```

Uses the per-request `use_sams`. **Verdict: per-tenant correct.**

### `Login` (GET and POST)

[AccountController.cs lines 131-158](source-code/mmria/mmria-server/Controllers/AccountController.cs#L131-L158)

Both actions redirect to `SignIn` when `use_sams == true` before doing anything else. **Verdict: per-tenant correct.**

### OIDC `SignIn` (SAMS initiation)

[AccountController.OIDC.cs lines 109-160](source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs#L109-L160)

**Does not check `use_sams` itself.** It reads per-tenant SAMS config, builds the redirect URL, and returns it. Today no code path leads to `SignIn` for a password-only tenant (all callers go through `AutoLogin` or `Login`, which do check). But this is a latent surface: a user hand-typing `/Account/SignIn` on a password-only tenant would either (a) trigger the SAMS flow, or (b) 500 because `GetSAMSConfigurationDetail(host_prefix)` would throw `KeyNotFoundException` on the missing per-tenant SAMS credential keys.

**Verdict: not broken today, but a defensive guard is a low-cost hardening candidate for 41.2 or a follow-up.**

### OIDC `SignInCallback`

[AccountController.OIDC.cs lines 163+](source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs#L163)

Same story as `SignIn` — reads per-tenant SAMS config; no `use_sams` guard. This endpoint is invoked by SAMS with `?code=…&state=…` after a real OIDC round-trip. If a bad actor hits this endpoint on a password tenant, the constructor throws before the action runs (500). Not exploitable across tenants because each request's `RequestTenantRuntime` is bound to that request's host.

**Verdict: not broken today; same optional hardening as `SignIn`.**

### `AppOffline`

[AccountController.cs lines 109-118](source-code/mmria/mmria-server/Controllers/AccountController.cs#L109-L118)

Does not touch `use_sams`. Just checks the tenant's offline config and either renders the offline view or redirects to `AutoLogin`. `AutoLogin` then handles the SAMS/password branch. **Verdict: per-tenant correct.**

### `Logout`

[AccountController.cs lines 405-430](source-code/mmria/mmria-server/Controllers/AccountController.cs#L405-L430)

When `use_sams == true`, redirects to `_configuration.GetSharedString("sams:logout_url")`; otherwise to `AutoLogin`. Uses per-request `use_sams`. **Verdict: per-tenant correct.**

Note: `sams:logout_url` is read via `GetSharedString` — this is a bug-free shared value today, but in mixed-tenant scenarios the URL is the CDC SAMS logout endpoint (the same for everyone). Fine as-is.

### `HomeController.Index`

[HomeController.cs line 90](source-code/mmria/mmria-server/Controllers/HomeController.cs#L90) — reads per-request `configuration.GetBoolean("sams:is_enabled", host_prefix)` into `ViewBag.sams_is_enabled`. **Verdict: per-tenant correct.**

### `policyValuesController`

[policyValuesController.cs line 44](source-code/mmria/mmria-server/Controllers/api/policyValuesController.cs#L44) — same per-request pattern. **Verdict: per-tenant correct.**

---

## 6. Minimal Change List for Story 41.2

**Nothing in the code layer is required for the basic feature.** Story 41.2 is a configuration + documentation task:

### Required (config-only)

1. In each SAMS-mode tenant's CouchDB `configuration-master` document (`_id = "{tenant}_{shared_config_id}"`), ensure `boolean_keys.shared.sams:is_enabled = true` and populate the SAMS credential keys in `string_keys.shared`.
2. In each password-mode tenant's CouchDB `configuration-master` document, set `boolean_keys.shared.sams:is_enabled = false`. The SAMS credential keys may be omitted.
3. Document the exact JSON snippet and the CouchDB update procedure (e.g., `curl -X PUT` against the tenant's `configuration-master` doc) in the operations runbook.

### Optional (recommended hardening, small-scope code changes)

4. Add a `use_sams` guard at the top of `AccountController.SignIn` and `AccountController.SignInCallback` (OIDC) that redirects to `Account/Login` when `use_sams == false`. This prevents a hand-typed `/Account/SignIn` from throwing `KeyNotFoundException` on a password-only tenant. Two-line change in each action.
   - Requires computing `use_sams` in the OIDC controller constructor (currently only the main controller does this).
5. Guard `GetSAMSConfigurationDetail(prefix)` against missing keys and return `null`/a partially-populated object instead of throwing, so the guard in (4) actually gets a chance to run. Alternatively, restructure the OIDC controller so `GetSAMSConfigurationDetail` is called inside the action rather than in the constructor.

### Not required

- No Login.cshtml changes.
- No changes to `OverridableConfiguration`, `TenantCatalog`, or `RequestTenantRuntime`.
- No changes to DI registration or controller lifetime.

### Testing recommendations for 41.2

- Bring up local multi-tenant with two tenants configured differently (one SAMS-enabled, one password) and verify:
  - `/Account/Login` on the SAMS tenant redirects to `SignIn`; on the password tenant renders the password form.
  - `/api/policy_values` returns `sams_is_enabled: true` and `false` respectively.
  - `Home` `ViewBag.sams_is_enabled` differs per tenant.
- Optional: add an integration test that flips `boolean_keys.shared.sams:is_enabled` on one tenant's config-master doc and confirms behavior changes on next request without a server restart. (Note: this only works if the `TenantCatalog` re-reads the doc — verify. See open questions below.)

---

## 7. Open Questions / Risks

1. **Runtime refresh of tenant config — RESOLVED (41.2): server restart required.**
   Traced `TenantCatalog.UpsertOverridableConfiguration` and `UpsertConfigurationSet` — both methods exist on the catalog ([TenantCatalog.cs L130-L200](source-code/mmria/mmria-server/util/TenantCatalog.cs#L130)) but have **zero production callers** (`grep -R UpsertOverridableConfiguration source-code/**/*.cs` finds only the definition; the only other match is in test setup under `mmria-server.tests/`). `LoadRequiredOverridableConfigurationsAsync` is invoked exactly once, at process startup in [Program.cs L210](source-code/mmria/mmria-server/Program.cs#L210). There is no CouchDB `_changes` listener, no admin endpoint, and no periodic reload wired to the catalog. **Conclusion: flipping `boolean_keys.shared.sams:is_enabled` in a tenant's `configuration-master` doc requires a mmria-server process restart to take effect for that tenant.** The `Upsert*` methods appear to be forward-looking infrastructure for a future hot-reload feature.
   *Operations runbook implication:* whenever this key is changed, restart the mmria-server pod(s). Kubernetes rolling restart is sufficient. Coordinate with the tenant so active password/SAMS sessions are not surprised mid-flow (login redirects will change on the next `/Account/Login` after restart).
2. **`sams:logout_url` is read shared, not per-tenant.** If any tenant ever needs a distinct logout URL, that becomes a code change. Not needed today (same CDC SAMS instance).
3. **`AmbiguousMatchException` risk.** `mmria.server.Controllers.AccountController` and `mmria.common.Controllers.AccountController` both have controller name `Account`. They partition cleanly today (Login/Logout/AutoLogin in one, SignIn/SignInCallback in the other), but adding an action with the same name to both would cause a startup or first-request failure. Out of scope for 41.1 but worth noting in code comments during 41.2 hardening.
4. **Live verification of AC #1 was not performed in 41.1.** Evidence is entirely code-based (per fallback direction in the story). 41.2 also could not run a live HTTP round-trip because the local multi-tenant mmria-server process was not up (only CouchDB port 5984 was listening; no listeners on 44300-44304). AC #8 build validation passes and the config-layer procedure below is complete; live verification is deferred to the first target-environment deploy.

---

## 8. Operator Procedure — Switch a Tenant Between SAMS and Password Auth (added by 41.2)

**Prerequisite:** decide which tenant(s) will be password-mode and which will be SAMS-mode. Default recommendation: `tenant1` is password (demo / training), all other tenants (`tenant2`, `tenant3`, `tenant4`, `tenant5`, `cdc`) are SAMS.

### 8.1 Preferred: use the admin UI (`Configuration` page)

1. Sign in to the target tenant as an admin user.
2. Navigate to the Configuration page (writes to the tenant's `configuration-master` document via [MultiTenantSetupController](../../source-code/mmria/mmria-server/Controllers/MultiTenantSetupController.cs)).
3. Set `sams:is_enabled` in the `shared` section to `false` (password) or `true` (SAMS).
4. Save.
5. **Restart the mmria-server process for the change to take effect** (see §7 #1).

### 8.2 Fallback: direct CouchDB upsert against the tenant DB

Use this only if the admin UI is unavailable and you have authenticated access to the tenant's CouchDB.

For password-mode `tenant1` (`_id = tenant1_shared_config` in `tenant1`'s couch DB):

```powershell
$url  = "http://tenant1-couchdb.local:6984/configuration/tenant1_shared_config"
$auth = "Basic " + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("mmrds:mmrds"))
$doc  = Invoke-RestMethod -Uri $url -Headers @{Authorization=$auth}
$doc.boolean_keys.shared.'sams:is_enabled' = $false
$body = $doc | ConvertTo-Json -Depth 20 -Compress
Invoke-RestMethod -Method Put -Uri $url -Headers @{Authorization=$auth; 'Content-Type'='application/json'} -Body $body
```

For a SAMS-mode tenant, set the same field to `$true` and confirm `string_keys.shared` carries the SAMS credential keys (`sams:client_id`, `sams:client_secret`, `sams:callback_url`, `sams:activity_name`).

**Do not hand-edit CouchDB in production without a change ticket.** Prefer the admin UI in prod; the direct-upsert form is intended for local and staging environments.

### 8.3 Verification checklist (per tenant, after mmria-server restart)

- [ ] `GET /api/policy_values` on the tenant returns `sams_is_enabled: false` (password) or `true` (SAMS).
- [ ] `GET /Account/Login` on a password tenant renders the password form; on a SAMS tenant redirects to `/Account/SignIn`.
- [ ] Hand-typed `GET /Account/SignIn` on a password tenant redirects cleanly to `/Account/Login` (no 500). This is the AC #7 guard added in 41.2.
- [ ] Log in on tenant A, then hit tenant B in the same browser — session must not carry across tenants.

---

## Appendix — Key File:Line References

| Topic | Reference |
|---|---|
| `OverridableConfiguration.GetBoolean` semantics | [configuration.cs L146-L159](nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs#L146-L159) |
| `GetSAMSConfigurationDetail` (per-prefix, no fallback) | [configuration.cs L419-L427](nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs#L419-L427) |
| `GetString` (per-prefix with shared fallback) | [configuration.cs L178-L190](nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/configuration.cs#L178-L190) |
| Multi-tenant document loading | [MultiTenantConfigurationLoader.cs L162-L204](nccdphp-drh-mmria-common/mmria.common/couchdb/configuration/MultiTenantConfigurationLoader.cs#L162-L204) |
| Tenant → config document matching | [TenantCatalog.cs L36-L58, L240-L250](source-code/mmria/mmria-server/util/TenantCatalog.cs#L36-L58) |
| `RequestTenantRuntime` scoped registration | [Program.cs L589-L604](source-code/mmria/mmria-server/Program.cs#L589-L604) |
| `AccountController` constructor & `use_sams` | [AccountController.cs L49-L72](source-code/mmria/mmria-server/Controllers/AccountController.cs#L49-L72) |
| `AutoLogin` | [AccountController.cs L92-L107](source-code/mmria/mmria-server/Controllers/AccountController.cs#L92-L107) |
| `Login` GET/POST branch on `use_sams` | [AccountController.cs L131-L158](source-code/mmria/mmria-server/Controllers/AccountController.cs#L131-L158) |
| `Logout` branch on `use_sams` | [AccountController.cs L405-L430](source-code/mmria/mmria-server/Controllers/AccountController.cs#L405-L430) |
| OIDC `SignIn` / `SignInCallback` | [AccountController.OIDC.cs L109, L163](source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs#L109) |
| Config seed writes `sams:is_enabled` | [_config.cs L240, L248](source-code/mmria/mmria-server/Controllers/_config.cs#L240-L248) |
