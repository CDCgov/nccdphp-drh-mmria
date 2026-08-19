# Story 29.5: Online Save-Then-Retry-on-Collision (Path A)

Status: backlog

## Story

As an abstractor,
I want the "Generate Record ID" flow to POST the case with a single locally-generated candidate and automatically try a fresh suffix if the server reports a collision,
so that the create-case flow does not do 20 pre-flight round trips in the common case and does not surface a hard failure when a rare race condition occurs.

## Acceptance Criteria

1. **Online path no longer pre-flights `/api/record_id`.** In online mode, `add_new_case()` in `index.mmria.js` generates one candidate `STATE-YEAR-NNNN` locally and issues a single POST to `/api/case`. No calls to `/api/record_id` on the online branch.
2. **Collision response triggers regeneration and retry.** When the response body contains `error_code === "record_id_conflict"`, `add_new_case()` regenerates the 4-digit suffix (same `generateCandidate` closure as Story 29.2), reassigns `home_record.record_id`, and re-POSTs to `/api/case`.
3. **Retry cap prevents infinite loop.** After 5 consecutive `record_id_conflict` responses, the loop exits, `alert("Unable to generate a unique Record ID after multiple attempts. Please try again.")` fires, and no case is created. Cap is a named constant `MAX_UNIQUE_RETRIES = 5` at the top of the loop.
4. **Non-collision errors surface immediately.** Any `error_code` other than `record_id_conflict` (or an absent `error_code` on a failed response) bypasses the retry loop; the response is surfaced via the existing `save_case_and_wait` failure path.
5. **PMSS variant kept consistent.** `index.pmss.js` uses the server-authoritative `/api/case_view/next-pmss-number` endpoint (already the source of truth). Diff review confirms no pre-flight loop remains and no changes are required beyond keeping the removal of the Story 29.2 `Get_Record_Id_List` wrapper intact.
6. **Offline branch untouched.** The offline branch of `add_new_case()` still works as-is in this story. Story 29.6 handles the offline behavior change; 29.5 and 29.6 do not conflict because they modify different branches of the same conditional.
7. **Build passes and smoke test succeeds.** Zero build errors. A case creates successfully with one round trip in the local multi-tenant environment. A DevTools instrumentation forcing `record_id_conflict` twice confirms the retry loop advances and eventually succeeds.

## Tasks / Subtasks

- [ ] Remove Story 29.2's pre-flight loop from the online branch of `add_new_case()` in `index.mmria.js` (AC: #1)
- [ ] Wrap the POST in a retry loop keyed on `error_code === "record_id_conflict"` (AC: #2, #3)
- [ ] Preserve the existing user-facing exhaustion message and `__handled` error marker (AC: #3)
- [ ] Ensure other error codes / missing codes short-circuit to normal failure (AC: #4)
- [ ] Diff-review `index.pmss.js` and confirm no pre-flight remains (AC: #5)
- [ ] Keep the offline branch of `add_new_case()` untouched (AC: #6)
- [ ] Add a `[Obsolete("no shipped callers after Story 29.5; retain for Story 29.3 cleanup pass")]` note (or code comment) on `record_idController.Get` (AC: none — housekeeping)
- [ ] Build + smoke test (AC: #7)

## Dev Notes

**Primary file:** `source-code/mmria/mmria-server/wwwroot/scripts/case/index.mmria.js` — the online branch of `add_new_case()`.

**Depends on Story 29.4** — this story reads `response.error_code`, which is added by 29.4.

**Detection pattern (client):**

```javascript
const resp = await save_case_and_wait_return_response(...); // whatever the accessor is
if (resp && resp.error_code === "record_id_conflict") { /* regenerate + retry */ }
```

Reuse the existing `save_case_and_wait` return path — do not introduce a parallel save function. If `save_case_and_wait` currently discards the response body, extend it to surface `error_code` (backward compatible — existing callers ignore it).

**Loop shape:**

```javascript
const MAX_UNIQUE_RETRIES = 5;
let attempts = 0;
let candidate = generateCandidate();
while (true) {
    result.home_record.record_id = candidate.toUpperCase();
    const resp = await try_save_case(result); // wraps save_case_and_wait
    if (!resp || resp.error_code !== "record_id_conflict") break; // success or non-collision error
    attempts++;
    if (attempts >= MAX_UNIQUE_RETRIES) {
        alert("Unable to generate a unique Record ID after multiple attempts. Please try again.");
        const err = new Error("Unable to generate a unique Record ID after multiple attempts.");
        err.__handled = true;
        throw err;
    }
    candidate = generateCandidate();
}
```

**PMSS:** the `/api/case_view/next-pmss-number` endpoint is already server-authoritative, so no analogous retry loop is needed. Just confirm Story 29.2's `Get_Record_Id_List` wrapper removal stayed in place.

**`record_idController` deprecation:** do not delete in this story. Add an `[Obsolete]` marker and a `// no shipped callers after Story 29.5` comment. Story 29.3 (or a follow-up) removes it once the sprint is confident nothing else calls it.
