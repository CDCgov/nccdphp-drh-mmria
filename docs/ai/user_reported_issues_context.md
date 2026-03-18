# User Reported Issues Context

Purpose: track recurring end-user problems, the relevant code areas, and high-level technical understanding so future investigation starts from known context instead of re-discovering the same areas.

## Issue 1: Tab/browser crash while working in graph-heavy case sections

### User report
- The user said `ER Visits and Hospitalizations / vital signs` is a major pain point.
- She reported the tab crashed while using that section.
- She said this has been a consistent problem over the years, not a one-time event.

### High-level case graph architecture
- Case-entry graphs are rendered on the case page with D3 v5 and C3.
- The case views load the chart libraries and the shared case chart renderer from:
  - `source-code/mmria/mmria-server/Views/Case/Index.cshtml`
  - `source-code/mmria/mmria-server/Views/abstractorDeidentifiedCase/Index.cshtml`
  - `source-code/mmria/mmria-server/Views/AnalystCase/Index.cshtml`
  - `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js`
- The chart renderer is metadata-driven. Graph controls are defined as `type: "chart"` in `database-scripts/metadata.json`, positioned by `database-scripts/default-ui-specification.json`, and mapped in `database-scripts/mmria-check-code.js` / `database-scripts/validator.js`.

### Case forms that use graphs
- There appear to be two graph-heavy case-entry areas:
  - `prenatal`
  - `er_visit_and_hospital_medical_records`
- In the current case metadata, there are seven case-entry graphs total:
  - `prenatal/blood_pressure_graph`
  - `prenatal/weight_gain_graph`
  - `prenatal/hematocrit_graph`
  - `er_visit_and_hospital_medical_records/temperature_graph`
  - `er_visit_and_hospital_medical_records/pulse_graph`
  - `er_visit_and_hospital_medical_records/respiration_graph`
  - `er_visit_and_hospital_medical_records/blood_pressure_graph`
- Other chart usage in the repo is mostly reporting/aggregate-report functionality, not case data-entry forms.

### ER Visits and Hospitalizations / vital signs
- The ER/Hospital section uses a `vital_signs` grid with a `datetime` X axis.
- The graph definitions are:
  - `temperature_graph` -> `er_visit_and_hospital_medical_records/vital_signs/date_and_time` vs `temperature`
  - `pulse_graph` -> `.../date_and_time` vs `pulse`
  - `respiration_graph` -> `.../date_and_time` vs `respiration`
  - `blood_pressure_graph` -> `.../date_and_time` vs `bp_systolic` and `bp_diastolic`
- This is the graph-heaviest case-entry section: one vital-signs grid feeds four separate charts.

### Prenatal graphs
- The Prenatal section uses a `routine_monitoring` grid with a `date` X axis.
- The graph definitions are:
  - `blood_pressure_graph` -> `prenatal/routine_monitoring/date_and_time` vs `systolic_bp` and `diastolic`
  - `weight_gain_graph` -> `.../date_and_time` vs `weight`
  - `hematocrit_graph` -> `.../date_and_time` vs `blood_hematocrit`

### How graph updates work
- Field updates on the case page eventually call:
  - `window.setTimeout(function() { update_charts(p_dictionary_path) }, 0);`
  - in `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`
- `update_charts(...)` in `chart.js` looks up charts that depend on the changed dictionary path and re-renders them.
- Re-rendering is not lightweight:
  - it rebuilds the graph markup
  - re-runs `c3.generate(...)`
  - rescans the full source array for X and Y values
  - recalculates axis ranges
- The chart renderer uses dynamic path evaluation helpers (`eval(...)` via converted dictionary/object paths) to walk the underlying case arrays.
- Graphs can also be switched to a table view, but the default graph mode is a live chart render.

### Relevant implementation details
- The chart renderer keeps global dependency maps:
  - `g_charts`
  - `g_chart_data`
  - `chart_function_params_map`
- Those maps are cleared when major case UI reloads occur, but regular field edits still rely on the shared global chart/update mechanism.
- The chart renderer has custom axis-floor/increment logic for each chart type:
  - blood pressure
  - weight gain
  - hematocrit
  - temperature
  - pulse
  - respiration
- ER/Hospital graphs use `datetime` labels and rotate the X-axis tick text on render, which adds more DOM work than a simple field edit.

### Why this area is a credible pain point
- `ER Visits and Hospitalizations / vital signs` is one of the most UI-intensive data-entry sections in the case workflow.
- A single change inside the `vital_signs` grid can trigger graph updates that:
  - traverse the full grid
  - rebuild one or more charts
  - re-run D3/C3 rendering in the live case page
- Compared with normal text/list fields, this section has more client-side work per edit and more opportunities for rendering or browser-tab stress.
- The graph stack is based on older D3/C3 client libraries, which is consistent with a long-standing pain point rather than a recent regression.

### Current conclusion
- There is not yet proof that the graphs are the direct root cause of the tab crash.
- There is enough code-level evidence to treat graph-heavy sections, especially `ER Visits and Hospitalizations / vital signs`, as a serious candidate area for browser-tab instability, UI slowness, or save interruption symptoms.

### Additional findings: chart implementation patterns that could contribute to memory growth

#### 1. `g_charts` is used as both a `Map` and a plain object bag
- In the case page, `g_charts` is created as a real `Map`.
- In the chart renderer, live chart instances are assigned with bracket syntax:
  - `g_charts['chart_<id>'] = c3.generate(...)`
- Elsewhere, cleanup calls only use:
  - `g_charts.clear()`

That is a bad pattern because `Map.clear()` only clears actual map entries. It does **not** remove arbitrary object properties added with bracket syntax.

Implication:
- dependency-tracking entries are cleared
- but any chart instances stored as `g_charts['chart_...']` can still remain attached to the `Map` object
- those lingering references can prevent garbage collection in a long-lived tab

This is the strongest concrete memory-leak candidate found so far in the chart implementation.

#### 2. Charts are re-rendered by replacing DOM, but old chart instances are not explicitly destroyed
- `update_charts(...)` rebuilds chart markup and then does:
  - `document.getElementById(chart_data.div_id).outerHTML = ...`
- It then immediately runs a fresh `c3.generate(...)` through `eval(...)`.
- There is no explicit cleanup step before replacement:
  - no `chart.destroy()`
  - no removal of old chart instance references

Implication:
- the DOM node is replaced
- a new C3 chart is created
- the old chart object is not explicitly disposed

Even if some objects are eventually garbage-collected, this is still a risky pattern for repeated rerenders. If C3/D3 internals keep event listeners, resize hooks, or internal references alive, memory can accumulate over time.

#### 3. Graph/table toggling follows the same replace-without-destroy pattern
- Switching a chart to table view replaces the graph markup with `outerHTML`.
- Switching back to graph view calls `chart_render(...)` and creates a new chart again.
- There is no explicit destroy/unregister step during graph-table transitions either.

This may not be the user's main workflow, but it is another place where chart objects can be recreated without clear teardown.

#### 4. Chart updates recompute data by rescanning the full source arrays
- For each rerender, the graph code:
  - re-evaluates the source array with `eval(...)`
  - rebuilds X-axis data
  - rebuilds Y-axis data
  - recalculates min/max/tick values
  - regenerates the chart
- In `ER Visits and Hospitalizations / vital signs`, four graphs share the same grid.

This looks more like CPU/main-thread pressure than a pure leak, but repeated full-array rescans are still likely to amplify browser instability in long data-entry sessions.

#### 5. The chart implementation stores large render inputs in global maps
- `chart_function_params_map` stores render parameters for each chart
- `g_chart_data` stores chart metadata and render context for each chart
- Those maps are cleared on major case/page changes, which is good
- But during normal use they are still part of the global long-lived page state

By itself this does not prove a leak, but it means chart-related state is intentionally retained for live rerender support.

### Refined conclusion for Issue 1
- The chart code contains at least one **strong** memory-retention risk:
  - `Map` misuse for live chart instances (`g_charts['...']` on a `Map`)
- It also contains a **strong** lifecycle smell:
  - recreating charts via `outerHTML` replacement without explicit chart destruction
- And it contains a **strong** performance risk:
  - repeated full-grid rescans and chart regeneration in `ER Visits and Hospitalizations / vital signs`

Taken together, these patterns make the graph-heavy sections a credible long-standing source of:
- browser memory growth
- main-thread slowdown
- UI instability
- tab crashes under extended use

### Proposed two-step fix plan

#### Step 1: internal lifecycle and memory cleanup with no intended user-visible change
Goal:
- reduce memory-retention risk
- clean up chart lifecycle correctly
- preserve the current chart UI and user workflow exactly

Recommended changes:
- Keep `g_charts` only for dependency tracking (`dictionary path -> set of chart ids`).
- Add a separate `g_chart_instances` `Map` for live C3 chart objects.
- Stop assigning live chart instances with bracket syntax like `g_charts['chart_x'] = ...`.
- Before re-rendering a chart, explicitly destroy any existing chart instance for that chart id.
- Before switching from graph to table, explicitly destroy the chart instance for that chart.
- Before clearing chart state on case navigation / reload / case change, destroy all live chart instances and then clear:
  - `g_chart_instances`
  - `g_charts`
  - `g_chart_data`
  - `chart_function_params_map`
- Reduce the amount of chart render state held in globals where possible. In particular, avoid retaining large temporary render arrays or other data that can be recomputed.

Expected outcome:
- same charts
- same graph/table toggle
- same labels/layout/behavior
- lower risk of stale chart objects and retained references building up over time

This is the safer first step because it targets memory/lifecycle issues without changing what the user sees.

#### Step 2: reduce rerender churn in graph-heavy sections
Goal:
- reduce CPU/main-thread work in sections like `ER Visits and Hospitalizations / vital signs`
- improve responsiveness and lower the chance of browser-tab degradation under extended use

Recommended investigation/changes:
- Avoid full chart recreation when only data changed and the existing chart can be updated in place.
- Reduce repeated full-array rescans where the chart inputs have not materially changed.
- Consider coalescing multiple rapid field edits into a single chart update pass.
- Avoid rerendering unrelated charts when only one dependent series changed.
- Profile the `vital_signs` grid specifically, because four charts share the same source data.

Expected outcome:
- same visible charts
- less redraw work per edit
- better performance in graph-heavy sections

This should be treated as a second step because it changes how chart updates are performed internally and therefore carries more regression risk than the lifecycle cleanup.

### Additional findings after Step 1 and Step 2

The memory/lifecycle cleanup and conservative rerender-coalescing work appear to have addressed one class of chart problems, but they did **not** eliminate the browser's layout-cost warnings in large ER/Hospital vital-sign cases.

Observed browser warnings during ER/Hospital testing with roughly 100 `vital_signs` rows:
- repeated `[Violation] 'focus' handler took <N>ms`
- repeated `[Violation] Forced reflow while executing JavaScript took <N>ms`

This points to a remaining **layout/reflow** problem, not just retained chart instances.

#### Likely remaining contributors

1. The case page still re-renders the edited control on every field update
- In `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`, field saves still:
  - update `g_data`
  - write the full case back to `localStorage`
  - rebuild the affected control with `page_render(...)`
  - replace the DOM with `outerHTML` / `replaceWith(...)`
  - reinitialize jQuery widgets like `datetimepicker(...)` and `numeric(...)`
- In a very large ER/Hospital record, even replacing only one control can still trigger expensive style/layout recalculation because the page uses a large, absolutely positioned form layout.

2. The chart renderer still forces synchronous layout reads after DOM writes
- In `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js`, chart render logic still:
  - regenerates the graph DOM
  - then immediately calls `getBoundingClientRect().width` on the chart SVG to position appended text
- That pattern can force the browser to flush layout immediately.

3. Chart `onrendered` still mutates many tick labels after C3 has rendered
- Each graph render still rotates X-axis tick labels in `onrendered`.
- With `datetime` X axes and many points, that means a large batch of post-render DOM mutations.
- In ER/Hospital vital signs, this happens across four charts tied to the same grid.

4. `focus` warnings are likely secondary effects of large synchronous UI work
- The repo does not have a single obvious custom `focus` event handler for the vital-sign inputs.
- The more plausible explanation is:
  - user focuses or tabs into a field
  - the browser/plugin performs focus-related work while the page is already under heavy layout pressure
  - DevTools reports the slow focus event as a violation
- This is consistent with the case page using dynamic widget initialization and occasional explicit `.focus()` calls after DOM replacement.

#### Refined conclusion
- The remaining ER/Hospital pain is now most credibly a **main-thread layout/reflow cost** problem.
- The chart memory fix was still worthwhile, but it does not fully address the large-record browser warnings.
- The next likely high-value area is not more chart-instance cleanup. It is reducing:
  - synchronous DOM replacement
  - immediate layout reads after writes
  - repeated post-render chart label work

#### Important repro refinement
- The issue can still appear even when the user does **very little**:
  - open a large ER/Hospital record
  - enter edit mode
- That means the problem is not limited to repeated incremental edits or long typing sessions.

This shifts suspicion toward the **initial edit-mode render path**:
- `g_render()` rebuilds the current case form into `form_content_id`
- `apply_tool_tips()` then initializes widgets across the rendered form
- chart rendering runs and performs post-render DOM work

In a large ER/Hospital record with ~100 `vital_signs` rows, that likely means:
- a very large HTML render pass
- initialization of many numeric/date/time controls
- immediate chart rendering for four graphs
- synchronous layout reads/mutations during or right after focus changes

This makes the next likely optimization target:
- **initial edit-mode render/setup cost**, not just per-field chart rerendering

#### Additional observation: browser became slow even on refresh
- In testing with a large ER/Hospital record, the browser took roughly 5-10 seconds to respond even to `F5` / refresh.

Why this matters:
- That strongly suggests the browser main thread was already saturated by synchronous client work.
- This is a stronger indicator of front-end render/setup pressure than of a save, lock, or server round-trip issue.

Refined interpretation:
- the page is doing enough synchronous work during/after edit-mode entry that even basic browser interaction can lag
- this points toward:
  - full-form render cost
  - large widget initialization cost
  - chart render/layout cost
- it is less consistent with:
  - network quality
  - backend save latency
  - a purely lock-related issue

#### Further refinement: active form only, but ER form itself is heavy
- The case page does **not** appear to render every case form at once.
- In `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/app.mmria.js`, the renderer selects the current form from `url_state` and renders only that form.

That means the problem is more specifically:
- opening one heavy `er_visit_and_hospital_medical_records` record is expensive by itself
- not that the entire case is being rendered and then hidden

Likely reasons the ER form is still expensive even before edit mode:
- the ER record renderer still walks and renders all child controls in the form
- the `vital_signs` grid can contain many rows, so one record can still produce a very large DOM subtree
- the four chart controls for the ER form are rendered in view mode as part of the form
- after render, `apply_tool_tips()` still initializes date/time/number widgets across the rendered form, even when the form is not yet in edit mode

Implication:
- view mode is not lightweight enough for very large ER records
- edit mode makes it worse, but the underlying cost begins earlier on form open

### Latest low-risk mitigation pass

Two conservative changes were made without changing the intended visible behavior:

1. View mode no longer initializes edit-only widgets across the case form
- In `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`, `apply_tool_tips()` now:
  - always applies tooltip behavior
  - skips `datetimepicker(...)` and `numeric(...)` initialization unless the case is actually in edit mode
  - scopes edit-widget initialization to `#form_content_id` instead of the whole page
- Rationale:
  - view mode should still show the form and charts
  - but it should not pay the edit-widget setup cost until editing actually begins

2. Initial chart render does less synchronous layout work
- In `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js`:
  - the empty `svg.append('text')` block that immediately measured `getBoundingClientRect().width` was removed
  - X-axis tick rotation was moved out of the synchronous `onrendered` path into `requestAnimationFrame(...)`
- Rationale:
  - reduce forced layout work during initial chart paint
  - preserve visible chart output as closely as possible

Expected effect:
- better view-mode open performance for large ER records
- lower chance that simply opening the ER form causes immediate focus/reflow violations
- edit mode may still be expensive, but less front-end work should happen before the user actually starts editing

### Latest test outcome after the low-risk mitigation pass

Observed result:
- the page became **much more responsive**
- switching between ER records still triggers browser warnings
- editing a large ER record still triggers warnings
- however, the page no longer appears to bog down severely, and refresh is responsive again

Latest warning pattern:
- repeated `[Violation] 'hashchange' handler took <N>ms`
- repeated `[Violation] Forced reflow while executing JavaScript took <N>ms`

Interpretation:
- this is a meaningful improvement over the earlier state where the browser could take many seconds even to respond to refresh
- the remaining warnings now appear more tied to:
  - record-to-record navigation on the case page
  - full ER form rerender on hash change
  - residual layout work during render
- this is less severe than the earlier “browser appears pinned” behavior

Likely remaining hotspot
- `window_on_hash_change(...)` in `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`
- On record navigation it still performs a substantial workflow:
  - URL-state parsing
  - sort/check logic
  - cleanup of chart state
  - case fetch / local cache handling
  - `g_render()`
- `get_specific_case(...)` then finishes by calling `g_render()`, which rebuilds the active form again

Practical conclusion
- The recent mitigation likely addressed the worst front-end setup cost.
- The remaining issue now looks more like:
  - case navigation/render cost for heavy ER records
  - not the prior severe view-mode/edit-mode widget initialization problem

### Further finding: the ER record-open cost is structural, not mainly driven by vital-sign count

Recent testing showed:
- clicking into an `ER Visits and Hospitalizations` record triggers the warnings even when the selected record has **no** vital signs
- the warnings get worse with more data, but they are not dependent on having many `vital_signs` rows

That strongly suggests the remaining bottleneck is the **ER form open path itself**.

Why the ER form is a structural hotspot:
- clicking a record triggers `window_on_hash_change(...)`
- that path loads the target case and then calls `g_render()`
- `g_render()` rebuilds the active form DOM from metadata
- the ER form itself is large and complex, with:
  - many groups and controls
  - one `vital_signs` grid
  - additional grids such as `birth_attendant`
  - several date/time-related controls and calculated groups
  - four chart controls that are rendered even in view mode

Important detail:
- the four ER charts are metadata-defined controls in the ER form
- they are rendered even when `vital_signs` is empty
- so “zero vital signs” does **not** mean “no chart/render cost”

Refined conclusion:
- the remaining issue is best understood as:
  - **ER record navigation/render cost**
- not simply:
  - “large chart data sets are slow”

This means future optimization should likely focus on:
- reducing work on record-open / hashchange for the ER form
- reducing initial chart work when the series is empty
- reducing the cost of building the ER form DOM itself

### Best next diagnostic targets
- Determine whether the crash tends to happen:
  - while typing into the `vital_signs` grid
  - when adding many rows
  - when switching between graph and table
  - when scrolling or moving between sections after graph updates
- Compare whether the same user sees similar instability in:
  - `prenatal/routine_monitoring`
  - `er_visit_and_hospital_medical_records/vital_signs`
- If this becomes a code investigation, start with:
  - `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js`
  - `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`
  - `source-code/mmria/mmria-server/database-scripts/metadata.json`
  - `source-code/mmria/mmria-server/database-scripts/default-ui-specification.json`

## Issue 2: Leaving a case idle in edit mode, then getting a conflict and being dropped out of edit state

### User report
- The user said that if she leaves a case open in an edited state for a while, she can come back and hit a conflict problem.
- She described the timing as roughly an hour or two, not overnight.
- She said the case is no longer in edit mode afterward and she has to leave the case and come back in.
- This description is specifically about older behavior before the recent 2026 save/lock work.

### Most likely older-code problem
The strongest historical candidate is the older edit-lock model:
- the case lock window was effectively treated as a fixed `case_lock_minutes` period from the original checkout
- the default lock window in server startup is `120` minutes
- autosave existed, but older save behavior did not slide the lock timestamp forward on each successful save

This matters because an open case could stay active in the browser while its stored `date_last_checked_out` aged toward the lock timeout anyway.

### Relevant older behavior

#### 1. Server-side edit lock did not slide on save
- In the older `SaveCaseAsync(...)` path, the stored lock fields were checked, but the server did not refresh `date_last_checked_out` when a checked-out case was saved.
- That changed in commit `7b77e5832` (`Implement sliding edit lock functionality and update case lock handling in tests`).
- Current code now does:
  - if the incoming case still has `date_last_checked_out`, set it to `DateTime.UtcNow` before saving
- That is a major fix for long-lived edit sessions.

#### 2. Client-side lock age was and still is based on a hardcoded 120-minute window
- The case page still uses client helpers:
  - `is_case_checked_out(...)`
  - `is_checked_out_expired(...)`
- Those helpers use a hardcoded `120` minute threshold in `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`.
- They do not read the server-configured `case_lock_minutes`.
- In older behavior, this meant the page could decide a checkout was expired based entirely on the local `date_last_checked_out` value.

#### 3. The client does not fully resync the local checkout timestamp after each save
- On successful save, the current client updates:
  - `_rev`
  - `last_updated_by`
- But it does not update local `g_data.date_last_checked_out` to the newly slid server value.
- It still recalculates `g_data_is_checked_out = is_case_checked_out(g_data)` using the local case object.
- This creates a remaining risk:
  - the server can consider the lock still fresh because it was slid on save
  - the client can still be reasoning from the original checkout timestamp
- That client/server drift is a plausible explanation for “the case is no longer in edit state” symptoms after long-open sessions.

#### 4. Older save queue behavior was less protective than current code
- Recent save-queue work improved how manual/awaited saves interact with background autosaves:
  - awaited actions are now treated as priority
  - the queue is explicitly FIFO
  - unstable-network retries were added for non-blocking saves
- Relevant commits:
  - `67f17e564` (`Implement save queue enhancements with retry logic and error handling`)
  - `49ba42c53` (`Enhance case controller with cache control headers and improve save queue management in scripts`)
- This is probably not the primary cause of the idle-lock issue, but it would have made older “come back after idle and try to save” stories more fragile under slow or unstable networks.

#### 5. Same-user multi-tab conflicts were historically weaker/less clear
- Recent work added/enforced `checked_out_by_tab_id` and stronger same-user/different-tab checks.
- Before that wave, some same-user conflicts were harder to distinguish from generic save problems.
- This is a secondary candidate if the user sometimes had multiple case tabs open without realizing it.

### What the older symptom likely looked like
A plausible older sequence is:
1. User clicks `Enable Edit` and starts editing.
2. The case remains open for a long time.
3. Autosave may continue, but the server does not slide `date_last_checked_out`.
4. The effective lock ages out around the configured window.
5. One of two things happens:
   - another user can obtain the lock, leading to a real conflict on the original user's next save, or
   - the original page's local checkout state drifts stale enough that the UI stops treating the case as actively checked out
6. The user sees a conflict/save problem and loses edit state.
7. Leaving the case and reopening it reloads fresh data and lock state, which is why “leave and come back” may appear to fix it.

### What recent work likely fixed
- **Major improvement:** sliding edit lock on save in `CaseManager.SaveCaseAsync(...)`
  - This is the most important fix for “open for 1-2 hours” edit sessions.
- **Major improvement:** stronger tab-id enforcement and unload cleanup
  - Helps distinguish real same-user tab conflicts from ambiguous lock behavior.
- **Major improvement:** better save queue handling
  - Helps manual user actions avoid being blocked behind autosaves under poor network conditions.
- **Moderate improvement:** session-timeout redirect handling is better understood/documented
  - But session timeout is less likely to explain a 1-2 hour issue in production if tenants are really using 720-minute sessions.

### What may still not be fully fixed
- The client still hardcodes `120` minutes when deciding whether a case is checked out.
- The client still does not update its own `date_last_checked_out` after a successful save to match the server's sliding lock behavior.
- Because of those two facts, there may still be UI-state drift in very long edit sessions even though the server-side lock logic is much better now.

### Current conclusion
- The recent server-side sliding lock change appears to address the biggest historical lock-aging gap.
- The issue is likely **much better** than it was in the older code.
- It is not clear that it is **fully** solved, because the client-side notion of checkout age still appears stale/hardcoded.

### Additional changes worth considering
- Update the client after successful save so that, when the case is still checked out, local `g_data.date_last_checked_out` is refreshed to “now”.
- Replace the hardcoded `120` minute client lock window with a value sourced from shared/server config.
- Add logging/telemetry when `g_data_is_checked_out` flips from `true` to `false` while the user is still on the case page.
- Continue to distinguish:
  - true lock conflicts
  - same-user/different-tab conflicts
  - session expiration
  - unstable network / save queue issues

### Best next diagnostic targets
- Ask whether the user had the same case open in another tab or browser.
- Ask whether the problem appeared after she had left the case untouched for around two hours specifically.
- Ask whether anyone else was editing the same case.
- Ask whether the problem happened after a visible autosave/save message or after manually clicking save.
- If reproducing in code, start with:
  - `nccdphp-drh-mmria-common/mmria.common/SharedLibraries/Case/Manager/CaseManager.cs`
  - `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`
  - `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/form.mmria.js`

## Issue 3: Problems later in the day after working in the app or on a case for a long time

### User report
- The user said she tends to have the most problems toward the end of the day.
- She reported good internet and said she does not use multiple tabs.
- This makes true network instability and same-user multi-tab conflicts less likely as the primary explanation.
- The report is broader than a single save error. It sounds more like long-lived-session instability, degraded behavior, or browser-tab stress after extended use.

### High-level conclusion
The code does not point to one obvious server-side `.NET` problem that would reliably make the application unstable after many hours.

The stronger explanation is a **long-lived browser-tab stress pattern**:
- the case page keeps large case state in memory
- edit mode starts recurring timer-driven work
- saves clone and serialize the entire case repeatedly
- successful saves also write the entire case back into browser `localStorage`
- graph-heavy sections can trigger expensive chart re-renders on top of that

This means end-of-day instability is more plausibly a **client-side accumulation problem** than a single clean server crash condition.

### Strongest candidate: long-lived edit mode keeps the browser busy all day

#### 1. Edit mode starts recurring timers that continue for the life of the case page
- The case page starts a one-second save processor for the lifetime of the page:
  - `g_process_save_interval = window.setInterval(process_save_case, 1000);`
- Once the user enters edit mode, autosave also starts:
  - `g_autosave_interval = window.setInterval(autosave, 10000);`
- The autosave function checks every 10 seconds and, once the case has been unchanged for about 3 minutes, it performs another save.

This means a case can sit open in edit mode for hours while the page continues to run timer-driven logic the entire time.

#### 2. Saves operate on the full case object, not just changed fields
- Every queued save clones the full case object through `mmria_safe_clone(...)`.
- On modern browsers this may use `structuredClone(...)`; otherwise it falls back to `JSON.parse(JSON.stringify(...))`.
- The save request sent to `/api/case` contains the full case payload plus change stack metadata.
- After successful save, the client often writes the full case back into `localStorage` with `set_local_case(...)`.

For a large case, one save can therefore involve:
- full-object clone in the queue
- full JSON request serialization for `fetch`
- full JSON serialization again for `localStorage`

That is not necessarily a bug by itself, but it is a credible source of browser CPU and memory pressure over a long day.

#### 3. Autosave saves even when the user is simply leaving the case open
- The autosave trigger is based on `date_last_updated`, not on a separate “dirty” flag.
- In practice, once a case is in edit mode, autosave can continue saving the case every few minutes for as long as the page remains open.
- That keeps the session alive, but it also means the page continues doing save work indefinitely.

This is important because the user described problems that happen later in the day, not immediately after entering edit mode.

### Strong candidate: older save/autosave plumbing was historically fragile

The commit history suggests the save model has been incrementally hardened over time rather than being stable for years:
- autosave was first added in 2019
- autosave nesting was reworked in 2024
- save-queue behavior was adjusted again in late 2024
- major save queue and retry hardening landed in March 2026

Relevant commits:
- `37e9b32a4` `Interim check-in setting up autosave`
- `04f9c4804` `Removed nesting in autosave.`
- `1e701a404` `removed discard adding to save queue.`
- `67f17e564` `Implement save queue enhancements with retry logic and error handling`
- `49ba42c53` `Enhance case controller with cache control headers and improve save queue management in scripts`

The current code now does several things that older code did not do as well:
- prune redundant non-blocking saves for the same case
- prioritize awaited/manual saves ahead of background autosaves
- retry some unstable-network failures instead of immediately failing the queue

This means some long-session save pain may already have improved substantially in recent code, even if users experienced it for years in the older code base.

### Strong candidate when combined with Issue 1: graph-heavy sections increase browser work
- `ER Visits and Hospitalizations / vital signs` already stands out as a graph-heavy section.
- Those charts are re-rendered through older D3/C3 code.
- Field changes can trigger full chart rebuild work on top of save queue activity and general case-page rendering.

This does not explain every long-day issue, but it is a plausible amplifier:
- long case session
- repeated autosaves
- large case object
- graph-heavy section

That combination is consistent with browser-tab degradation or crashes even when the internet is good.

### Moderate candidate: historical lock-state drift could make long sessions feel unstable
- Older code did not slide `date_last_checked_out` on save.
- That was improved in March 2026.
- The client still uses a hardcoded 120-minute checkout window and still reasons from local `date_last_checked_out`.

This means that some “later in the day” problems may have looked like save instability when they were really lock-state drift or conflict behavior after long-open sessions.

This is less likely to explain an outright browser crash, but it is still relevant to “things get weird later in the day” reports.

### Lower-confidence candidate: session timeout / auth handling
Session timeout is a weaker fit for this specific report, for two reasons:
- production is typically configured around 720 minutes (12 hours)
- once a case is in edit mode, autosave should keep making authenticated requests and extend the session

Still, there are some architectural caveats:
- the older session warning UI is effectively disabled on the case page (`set_session_warning_interval()` is commented out)
- JavaScript session-expiry handling is not globally centralized across all API calls

So session expiration can still create abrupt behavior in some flows, but it does **not** look like the strongest explanation for “end of day problems while editing a case.”

### Lower-confidence candidate: hidden .NET/server-side long-duration instability
From static code review, there is not a strong app-specific `.NET` pattern that obviously says:
- “after many hours this controller/manager becomes unstable”
- or “the server accumulates timer/state per user case page and breaks later”

That does not prove server-side issues never happen, but the code evidence for this specific user story is much weaker than the client-side/browser-tab accumulation theory.

### Why the user's details matter
The user said:
- good internet
- no multiple tabs
- more trouble later in the day

That combination makes these explanations **less** likely:
- simple network failure
- same-user multi-tab lock conflict
- immediate one-time bad save

And it makes these explanations **more** likely:
- long-lived case page doing repeated work
- browser memory/CPU pressure
- older autosave/save-queue behavior
- graph-heavy sections amplifying main-thread work

### Current conclusion
If this issue is real and recurring, the most credible explanation is:
1. long-lived case tab
2. repeated full-case clone/serialize/save behavior
3. possible graph re-render churn in heavy sections
4. historically weaker autosave/save queue behavior in older code

This is a better fit than blaming internet quality or assuming a single clean server-side crash.

### Additional changes worth considering
- Make autosave depend on a true unsaved-change signal instead of only elapsed time since `date_last_updated`.
- Reduce full-case cloning/serialization in the save queue where possible.
- Add telemetry/logging for:
  - autosave frequency
  - save queue length
  - time spent cloning/serializing
  - chart render duration
  - browser-side save failures by section
- Continue to harden the client-side checkout timestamp handling so long-open sessions stay aligned with server lock state.
- Consider special profiling of:
  - `ER Visits and Hospitalizations / vital signs`
  - very large cases
  - users who stay in one case tab for hours

### Best next diagnostic targets
- Ask whether end-of-day problems happen more often in graph-heavy sections than in simple text sections.
- Ask whether the browser becomes slow before the error/crash, or whether it fails suddenly.
- Ask whether the case had been left open in edit mode for a long time.
- Ask whether she saw signs of autosave activity before the issue.
- If reproducing in code, start with:
  - `source-code/mmria/mmria-server/wwwroot/scripts/case/index.js`
  - `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/chart.js`
  - `source-code/mmria/mmria-server/wwwroot/scripts/editor/page_renderer/form.mmria.js`
  - `source-code/mmria/mmria-server/CustomAuthHandler.cs`
