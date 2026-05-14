# Deployment Save Compatibility Matrix and Safeguards

## Summary
- From the case save perspective, `development` is not a hard break for every stale `origin/main` case page.
- The real risk is mixed old-client/new-server editing on the same case.
- A stale page can usually keep saving until that case has been touched by a refreshed `development` client that writes `checked_out_by_tab_id`.
- The top-level save contract stayed compatible in `source-code/mmria/mmria-server/Controllers/api/caseController.cs`.
- The new server now enforces lock ownership from the stored case document in `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs`.
- `origin/main` case pages do not load `tab-id.js`, while `development` does. That is the core deployment mismatch.

## Save Matrix
| Scenario | Stored case state at save time | Old page save outcome | Production data risk |
|---|---|---|---|
| User was already editing on `origin/main`, deploy happens, no refreshed client touches the same case | No `checked_out_by_tab_id` on stored doc | Save likely succeeds | Medium: old page still uses the old save queue/client behavior |
| Same user has a stale old page, then refreshes or reopens the same case in a new client tab or window | Stored doc now has `checked_out_by_tab_id` | Old page save likely blocked by same-user/different-tab enforcement | High: user can type on stale page and lose unsaved edits when save is rejected |
| User opened case before deploy but had not entered edit mode yet, then clicks `Enable Edit` on stale old page after deploy | Stored doc still has no tab id | Save likely succeeds and creates a legacy or tabless lock | Medium-high: new single-tab protections are partially bypassed during rollout window |
| User on stale old page clicks `Save & Finish` or navigates away after same case was touched by refreshed client | Stored doc has `checked_out_by_tab_id` | Close or unlock save can be blocked | High: lingering lock until timeout or manual release |
| Another user legitimately holds the case lock | Stored doc locked by another user within lock window | Save blocked | Expected behavior, not a deployment-specific regression |
| Case is offline in another tab or user flow that now writes offline ownership fields | Stored doc has offline or tab ownership fields | Old page save can be blocked by offline ownership checks | Medium-high for affected users |

## Recommended Rollout
- Preferred rollout: treat this as a drain-and-deploy release, not a fully transparent hot swap for active editors.
- `T-30 min`: notify users to finish case edits and avoid opening new cases near deployment.
- `T-10 min`: instruct users to click `Save & Finish`, return to summary, and refresh after deployment.
- `T-5 min`: verify there are no active edit locks, or at least no critical users still inside cases.
- Deploy only after the active editor count is effectively zero.
- `T+0`: tell users to hard refresh or reopen the case page before resuming editing.
- `T+15 min`: monitor logs for:
  - `Case is locked by another tab for this user`
  - `Case is offline in another tab for this user`
  - `(409) Conflict`
  - `save failed for:`
- If zero-downtime is required and active editors cannot be drained, assume some stale pages will save and some will fail depending on whether the case gets upgraded to tab-aware lock state mid-session.

## Safer Code Changes
- Highest-value change: add a runtime app-version mismatch check so old pages detect that the server version changed and block editing until refresh.
- Add cache-busted script URLs in the case layouts so a normal refresh reliably fetches the new JS bundle instead of reusing old script URLs.
- Add a deployment drain mode on the server:
  - block new checkouts or entering edit mode shortly before deployment
  - still allow current users to save-and-finish or release locks
  - then deploy once active edits drain out
- Return structured save error codes from `/api/case` for lock ownership failures instead of only free-text messages.
- Add a stale-client-specific response when the server sees a save with no tab id against a stored case that already has `checked_out_by_tab_id`.
- Not recommended: broad server-side compatibility shims that silently accept missing-tab saves after a case has a stored tab owner, because that weakens the single-tab protections.

## Assumptions
- `origin/main` is current production and `development` is the next deployment target.
- Normal authenticated requests do not appear to receive a `tab_id` claim automatically from auth, so stale `origin/main` saves should be treated as having no tab id.
- The main schema delta relevant to this deployment is lock and offline ownership fields, not a broad case-content shape break.
- This document is specifically about case saves during a live deployment window, not offline sync or delete behavior.
