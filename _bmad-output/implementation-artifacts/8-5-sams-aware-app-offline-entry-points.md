---
implemented: 2026-07-18
---

# Story 8.5: SAMS-Aware App Offline Entry Points

Status: done

## Story

As a user whose only login path is SAMS,
I want to see a clear offline page instead of being redirected to the SAMS identity provider,
so that I understand the system is unavailable without being bounced to an external login service.

## Acceptance Criteria

1. When `sams_is_enabled = true` and `now >= offline_date`, `GET /Account/SignIn` redirects to `/Account/AppOffline` instead of building the SAMS OAuth redirect URL.
2. When `sams_is_enabled = true` and `now >= offline_date`, `POST /Account/Logout` redirects to `/Account/AppOffline` instead of `sams:logout_url`.
3. `GET /Account/Login` checks offline state first (before the SAMS guard); if offline, redirects to `/Account/AppOffline`; if not offline and SAMS enabled, redirects to `/Account/SignIn`.
4. A dedicated `/Account/AppOffline` action (`[AllowAnonymous]`) performs a server-side offline state check. If offline, renders the `AppOffline` view with `offline_page_message`. If no longer offline, redirects immediately to `/Account/AutoLogin`.
5. The `AppOffline.cshtml` view displays `offline_page_message` in the standard purple-panel layout, includes the CDC conditions-of-use footer, and requires no authentication.
6. The `/Account/AppOffline` page calls `GET /api/account/offline-status` (anonymous) every 30 seconds and immediately on page load. When `is_offline` becomes false, it redirects to `/Account/AutoLogin`.
7. A new `GET /api/account/offline-status` (`[AllowAnonymous]`, route `~/api/account/offline-status`) returns `{ "is_offline": bool }` for the current tenant by calling `LoadSystemOfflineConfigAsync`.

## Tasks / Subtasks

- [x] Add offline guard to `GET /Account/SignIn` in `AccountController.OIDC.cs` (AC: #1)
  - [x] Make `SignIn` async; inline offline config fetch before building SAMS URL
  - [x] If `affectsThisTenant && isOffline` → `return RedirectToAction("AppOffline")`
  - [x] Wrap in try/catch — if check fails, proceed with normal SAMS redirect
- [x] Add offline guard to `POST /Account/Logout` in `AccountController.cs` (AC: #2)
  - [x] Before `return Redirect(sams:logout_url)`, call `LoadSystemOfflineConfigAsync`
  - [x] If offline for this tenant → `return RedirectToAction("AppOffline")`
- [x] Reorder guards in `GET /Account/Login` (AC: #3)
  - [x] Offline check runs first → redirect to AppOffline
  - [x] SAMS check runs second (no `!offlineForThisTenant` condition needed)
  - [x] Remove stale `ViewData["IsOffline"]` / `ViewData["OfflinePageMessage"]` inline path
- [x] Add `AppOffline` action to `AccountController.cs` (AC: #4)
  - [x] `[AllowAnonymous]` GET action
  - [x] If not offline → `return RedirectToAction("AutoLogin")`
  - [x] Otherwise set `ViewData["OfflinePageMessage"]` and return View
- [x] Add `OfflineStatus` API action to `AccountController.cs` (AC: #7)
  - [x] `[AllowAnonymous]`, `[HttpGet]`, `[Route("~/api/account/offline-status")]`
  - [x] Returns `Json(new { is_offline = isOffline })`
- [x] Create `Views/Account/AppOffline.cshtml` (AC: #5, #6)
  - [x] Matches Login page shell (same head assets, `_Header`, `_Footer` partials)
  - [x] Purple-panel div displaying `ViewData["OfflinePageMessage"]`
  - [x] CDC conditions-of-use block
  - [x] JavaScript: immediate fetch of `/api/account/offline-status` on load; 30-second `setInterval` fallback; redirect to `/Account/AutoLogin` when `is_offline: false`
- [x] Update `Login` POST offline block (AC: #3)
  - [x] Replace inline `ViewData` return with `return RedirectToAction("AppOffline")`

## Files Changed

| File | Change |
|---|---|
| `Controllers/AccountController.cs` | Added `AppOffline` action, `OfflineStatus` API, reordered Login GET guards, updated Login POST offline block, added Logout offline guard |
| `Controllers/AccountController.OIDC.cs` | Made `SignIn` async; added inline offline check before SAMS redirect |
| `Views/Account/AppOffline.cshtml` | New file — dedicated offline landing page with polling JS |

## Dev Notes

**Config key lookup:** `LoadSystemOfflineConfigAsync` calls the vitals service at `{vitals_url}/api/systemOffline/GetSystemOfflineConfig`. The OIDC partial inlines this fetch directly (it does not share `_systemOfflineManager` with the password partial).

**Bug fixed in parallel (22.2-B2):** `_config.cs` was reading `mmria_settings:sams:is_enabled` (wrong key) instead of `mmria_settings:sams_is_enabled`. This caused `use_sams` to always be `false`, masking the missing SAMS guard on `Login` GET (22.2-B3). Both were fixed as part of this work.
