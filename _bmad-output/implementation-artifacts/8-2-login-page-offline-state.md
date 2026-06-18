---
baseline_commit: e0a5e5ac5673bdf0a11bc5dbf1ef800abbfec27c
---

# Story 8.2: Login Page Offline State

Status: done

## Story

As a user,
I want the login page to show only an offline message when the system has passed its offline date,
so that I am clearly informed the system is unavailable and am not presented with a non-functional login form.

## Acceptance Criteria

1. The login page's controller (or Razor Page) fetches the current `SystemOfflineConfig` from the `/api/system-offline/status` endpoint (or reads it directly from cache/service) before rendering.
2. When `DateTime.UtcNow >= offline_date` (and `offline_date` is set/non-empty), the login form fields (username, password, submit button) are hidden or not rendered.
3. When the system is in offline state, the `offline_page_message` text is displayed prominently on the login page. Display uses white text as specified. No other changes to the login page layout.
4. When `offline_date` is null/empty, or `DateTime.UtcNow < offline_date`, the login page renders normally — no offline message, full form.
5. The offline check is server-side (done during page render), not client-side, so the login form is never visible even for users who disable JavaScript.

## Tasks / Subtasks

- [x] Locate login page controller/Razor Page (AC: #1–#5)
  - [x] Find the action/method that renders the login page (search for `[AllowAnonymous]` + login view)
  - [x] Identify where the view model is constructed
- [x] Fetch system offline config at login render time (AC: #1)
  - [x] Inject or call the same mechanism used in `/api/system-offline/status` to read the current `SystemOfflineConfig`
  - [x] Pass `offline_date`, `offline_page_message` to the login view model (or ViewData/ViewBag)
- [x] Conditionally hide login form (AC: #2, #4)
  - [x] In the login Razor view: wrap form fields in `@if (!Model.IsOffline)` (or equivalent ViewData flag)
  - [x] `IsOffline` = server evaluates `!string.IsNullOrWhiteSpace(offlineDate) && DateTime.UtcNow >= DateTime.Parse(offlineDate)`
- [x] Show offline message in white text (AC: #3)
  - [x] Add a conditional block: `@if (Model.IsOffline)` → render `<p>` or `<div>` with `offline_page_message` styled with white text
  - [x] Position the message in the same area as the login form
- [x] Build and verify (AC: #1–#5)
  - [x] Run `build-server` — zero errors

## Dev Notes

**Login page location:** Search for `AllowAnonymous` + login-related route (e.g., `/account/login`, `/login`). The login view is likely in `Views/account/` or similar. Identify the controller action that passes the ViewModel to the login view.

**Config access pattern:** Since the login page must work for unauthenticated users, the server-side read cannot use the `/api/system-offline/status` endpoint (which requires auth). Instead, the controller should read the config directly from the service layer or in-memory cache — same source as `/api/system-offline/status`, bypassing auth. This is the same pattern as reading config for anonymous pages.

**Date comparison:** Store `offline_date` as ISO 8601 UTC string. Parse with `DateTime.TryParse(offline_date, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)`. Compare `DateTime.UtcNow >= dt`. If parsing fails, treat as not-offline.

**White text styling:** `style="color: white;"` inline on the message container. Keep markup minimal — a single `<div>` or `<p>` with the message.

**ViewData approach (if no ViewModel):** If the login page does not have a typed ViewModel, use `ViewData["IsOffline"]` (bool) and `ViewData["OfflinePageMessage"]` (string).

### Project Structure Notes

- Modified files: login controller action (add offline config read), login Razor view (add offline conditional)
- No new files
- No new NuGet packages

### References

- [Source: prd-mmria-2026-06-12/prd.md#FR-8.2, FR-8.4]
- Depends on Story 8.1 (`/api/system-offline/status` endpoint and cached config)

## Dev Agent Record

### Agent Model Used
Claude Sonnet 4.6

### Debug Log References
- Build error CS0708 on first attempt: helper methods were accidentally placed inside `IsLocalExtension` static class at end of `AccountController.cs` rather than inside `AccountController`. Fixed by moving methods into the main class and removing from the static class.

### Completion Notes List
- `AccountController.Login` GET action changed from `IActionResult` to `async Task<IActionResult>`.
- `LoadSystemOfflineConfigAsync()` added to `AccountController` — calls `mmria-services` `/api/systemOffline/GetSystemOfflineConfig` using `vitals_url` and `vital_service_key` from `_configuration`, same pattern as `system_offlineController`.
- `IsSystemOffline()` static helper parses ISO 8601 date with `RoundtripKind` and compares to `DateTime.UtcNow`.
- `ViewData["IsOffline"]` (bool) and `ViewData["OfflinePageMessage"]` (string) passed to view.
- `Login.cshtml`: added `@{bool isOffline = ...}` guard at top of form section; `@if (isOffline)` renders white-text message div; `else` renders the original login form unchanged.
- Build: zero errors.
- Manual verification steps (set past/future offline_date via admin page) are left for human tester per story AC.

### File List
- [source-code/mmria/mmria-server/Controllers/AccountController.cs](source-code/mmria/mmria-server/Controllers/AccountController.cs) (modified — Login GET action made async; `LoadSystemOfflineConfigAsync` and `IsSystemOffline` helpers added)
- [source-code/mmria/mmria-server/Views/Account/Login.cshtml](source-code/mmria/mmria-server/Views/Account/Login.cshtml) (modified — offline conditional added around login form; offline message rendered with white text)
