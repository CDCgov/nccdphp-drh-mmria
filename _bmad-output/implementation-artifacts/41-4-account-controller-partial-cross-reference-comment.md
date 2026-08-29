# Story 41.4: AccountController Partial Cross-Reference Comment

Status: backlog

> **Origin:** Follow-up surfaced by Story 41.2 (per-tenant auth implementation). See [per-tenant-auth-findings.md](../../docs/ai/per-tenant-auth-findings.md) §7 #3. Low-effort documentation hardening to prevent a latent `AmbiguousMatchException` from surfacing during future edits.

## Story

As a developer maintaining the auth stack,
I want a clear cross-reference comment at the top of each `AccountController` partial file,
so that adding a new action to one partial without noticing the other cannot silently introduce an `AmbiguousMatchException` at startup or first request.

## Acceptance Criteria

1. [source-code/mmria/mmria-server/Controllers/AccountController.cs](../../source-code/mmria/mmria-server/Controllers/AccountController.cs) and [source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs](../../source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs) each carry a short (≤ 5 line) comment at the top of the class declaration that:
   - Names the other partial file
   - Lists which actions live in which partial (a two-line table is fine)
   - Notes that adding an action with a name that collides across the two partials will cause `AmbiguousMatchException`
2. The comment references the namespace mismatch: `mmria.server.Controllers.AccountController` (main) vs `mmria.common.Controllers.AccountController` (OIDC). These are effectively partials of the same MVC controller name (`Account`) because ASP.NET routes on controller-name suffix stripping, not full-type identity.
3. No functional code changes. `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` — zero errors.

## Tasks / Subtasks

- [ ] Add cross-reference comment to [AccountController.cs](../../source-code/mmria/mmria-server/Controllers/AccountController.cs)
  - [ ] Above the `public sealed partial class AccountController : Controller` declaration
  - [ ] Include the other partial's path and the action inventory
- [ ] Add matching cross-reference comment to [AccountController.OIDC.cs](../../source-code/mmria/mmria-server/Controllers/AccountController.OIDC.cs)
  - [ ] Same format; symmetric content
  - [ ] Explicitly call out the namespace mismatch as a hazard for future refactors
- [ ] Build verification
  - [ ] `dotnet build source-code/mmria/mmria-server/mmria-server.csproj` — zero errors

## Dev Notes

**Suggested comment shape** (adapt to house style):

```csharp
// This class is one of two partial-like halves of the /Account controller route.
// The other half lives in AccountController.OIDC.cs under namespace
// mmria.common.Controllers (this file is mmria.server.Controllers).
// Both partials share the MVC controller name "Account" — adding an action here
// with the same name as one in the OIDC partial will throw AmbiguousMatchException.
// Current split:
//   this file           → AutoLogin, Login (GET/POST), Logout, AppOffline
//   AccountController.OIDC.cs → SignIn, SignInCallback
```

**Why not merge the two partials?** Out of scope for this story. The current split predates 41.x and touching it risks unrelated regression. This story only adds the sign so future edits notice the hazard.

**Baseline reference:** [per-tenant-auth-findings.md](../../docs/ai/per-tenant-auth-findings.md) §7 #3.

**Depends on:** none. 41.2 is done and this is opportunistic hardening.
