# Case View/Edit Playwright Testing Context

- Status: Active
- Scope: Durable notes for writing Playwright coverage against `/Case#/summary` and `/Case#/{index}/{form_name}`.
- When to use: Read this before adding or changing Playwright tests that create, open, edit, save, or validate case-page form fields.
- Last verified: 2026-04-27
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Case Summary Rendering Context](./case_summary_rendering_context.md), [Strongly Typed Case Generator Workflow](./strongly_typed_case_generator.md)

## Why this doc exists

The case page is one of the most contract-sensitive surfaces in the app:

- hash-based routing drives which form is displayed
- field controls are metadata-rendered, not hand-authored
- some controls update `g_data` only on `blur`
- save/edit lock behavior depends on checkout state, session state, and tab state
- several field types have special renderers and non-obvious selectors

Playwright tests are much more reliable when they follow those implementation details instead of treating the case page as a generic form.

## Route model

Summary page:

- `/Case#/summary`

Opened case pages:

- `/Case#/{case_list_index}/home_record`
- `/Case#/{case_list_index}/{form_name}`

Quick edit:

- `/Case#/{case_list_index}/field_search/{search_text}`

Notes:

- The case page uses hash-based navigation, so most form switches do not trigger a full page load.
- `#selected_form` drives navigation by setting `window.location.hash = "/" + value`.
- The `value` of each `#selected_form option` is `{case_list_index}/{form_name}`.
- Tests should wait on the hash change and the case-page spinner state, not on a full navigation event.

## Create-case flow

The summary page `Add New Case` button is:

- `#add-new-case`

The create screen fields are:

- `#new_first_name`
- `#new_middle_name`
- `#new_last_name`
- `#new_month_of_death`
- `#new_day_of_death`
- `#new_year_of_death`
- `#new_state_of_death`

The create submit button text is:

- `Generate Record ID & Continue`

Duplicate protection:

- Clicking submit first posts to `/api/isDuplicateCase`.
- If the server thinks the case duplicates an existing one, a modal dialog titled `Duplicate Name Found` appears and creation stops.
- If the case is not a duplicate, `#add_new_confirm_dialog` opens with a `Generate Record ID?` confirmation.

Important test implication:

- Use unique first/last name seed values for creation so reruns do not get blocked by duplicate detection.
- Keep create-time month/day/year aligned with the walkthrough field plan because the UI explicitly warns that the year of death is not editable after record-id generation.

## Existing-case opening flow

The summary list search box is:

- `#search_text_box`

Filter action:

- `Apply Filters`

Useful row contract:

- summary rows render as `tr[path="{case_id}"]`

Useful link contract:

- the open-case link inside the row points to `#/{index}/home_record`

Practical pattern:

1. Go to `/Case#/summary`
2. Fill `#search_text_box` with the record id
3. Click `Apply Filters`
4. Find `tr[path]` containing the record id text
5. Capture the `path` attribute as the real case id
6. Click the row link to open the case

## Edit-mode and save behavior

Normal case-form footer buttons:

- `input[value="Enable Edit"]`
- `input[value="Save & Continue"]`
- `input[value="Save & Finish"]`

Observed behavior:

- `Enable Edit` reloads the case, applies checkout metadata, saves it, and only then flips `g_data_is_checked_out = true`.
- `Save & Continue` posts the case but keeps the edit session active.
- `Save & Finish` posts the case and clears checkout fields, which is useful for destructive tests so they do not leave the case locked.

Practical pattern:

- If `window.g_data_is_checked_out !== true`, click `Enable Edit` and wait for the `/api/case` POST plus the browser state flip.
- After each form edit pass, click `Save & Continue`.
- At the very end, click `Save & Finish` so the walkthrough releases the lock.

## Metadata endpoints to trust

Use live versioned APIs, not stale local version markers:

- `GET /api/version/release-version`
- `GET /api/version/{releaseVersion}/metadata`

The case page itself fetches metadata this way. Use the same endpoints in tests.

## Stable selector conventions

### Form picker

- `#selected_form`

### Summary rows

- `tr[path="{case_id}"]`

### Case data object

- `window.g_data`
- `window.g_data._id`
- `window.g_data_is_checked_out`

### Path-based control ids

The case renderer converts object paths like `g_data.home_record.first_name` into ids by replacing `.`, `[` and `]` with `_`.

Example:

- object path: `g_data.home_record.first_name`
- control id: `g_data_home_record_first_name_control`

Useful derived forms:

- string / number / textarea / html_area / select / jurisdiction:
  - `#g_data_<path>_control`
- boolean:
  - wrapper id: `#g_data_<path>`
  - checkbox: `#g_data_<path> input[type="checkbox"]`
- datetime:
  - `#g_data_<path>-date`
  - `#g_data_<path>-time`
- radio groups:
  - wrapper id: `#g_data_<path>`
  - radios live inside that wrapper
- checkbox groups:
  - wrapper id: `#g_data_<path>`
  - checkboxes live inside that wrapper
- rich-text narrative:
  - `#case_narrative_editor`

### Metadata path breadcrumbs in the DOM

Many rendered containers carry:

- `mpath="/metadata/path"`

This is useful when the control id is special-cased or when debugging missing selectors.

## Field-type handling notes

### Text and number fields

- Most update on `blur`.
- `g_set_data_object_from_path(...)` stores the input string value for normal Playwright entry flows.
- Number fields are still frequently stored as strings in `g_data` during UI edits, so API assertions should normalize carefully.

### Textareas

- Normal textareas use the standard `_control` id.
- `case_opening_overview` is special and renders through Trumbowyg at `#case_narrative_editor`.

### Rich-text narrative

- The rich-text editor stores sanitized HTML, not plain text.
- The renderer normalizes editor HTML before putting it in `g_data`.
- Keep test input simple: paragraphs, emphasis, lists, and plain inline styles are safer than complex pasted markup.

### HTML area fields

- `html_area` fields expect valid document-like markup and are parsed before save.
- Safe test content looks like `<html><p>...</p></html>`.

### Dates

- Date inputs display `MM/DD/YYYY`.
- Browser-side storage normalizes to `YYYY-M-D`.
- Invalid dates clear the field and add an item to `#validation_summary_list`.

### Datetimes

- Datetime controls are split into separate date and time inputs.
- On blur, the page combines them into an ISO string and stores `toISOString()`.
- Final persistence assertions should compare normalized ISO strings, not the raw date/time display text.

### Times

- Time values are stored as trimmed strings like `13:14:15`.

### Single-select lists

- Normal selects use `_control`.
- Prefer non-placeholder values and avoid `9999`, `8888`, `7777`, `Other`, and mutually special options when the test intent is general walkthrough coverage.

### Radio groups

- Metadata `type = list` with a radio-style control renders a wrapper div containing radio inputs.

### Checkbox groups

- Metadata `type = list` with `is_multiselect = true` or checkbox control style renders a wrapper div containing checkbox inputs.
- Some groups have other-specify or mutually-exclusive behavior. General walkthrough tests should prefer a single safe option and avoid intentionally exercising those special branches unless that is the test’s purpose.

## Validation summary behavior

The form renderer includes:

- `#validation_summary`
- `#validation_summary_list`

The page adds entries here for invalid date/datetime conditions and other broken-rule flows. For walkthrough tests:

- enter valid values so the summary stays empty
- treat unexpected validation summary content as a signal that the field plan or selector strategy needs adjustment

## Special controls to skip in general walkthrough tests

Skip these unless the test is specifically about that behavior:

- `home_record/case_status/overall_case_status`
- any field path ending in:
  - `/get_coordinates`
  - `/cmd_get_coordinates`
  - `/get_coordinates_clear`
  - `/cmd_get_coordinates_clear`
  - `/view_community_vital_signs_button`

Why:

- these controls trigger side effects, dialogs, external lookups, or lock-state behavior that deserve dedicated tests

## Grids and multiforms

First-pass walkthrough tests should not try to cover grid row creation or multiform row add/edit/delete flows.

Reason:

- those flows are their own interaction model
- many controls appear only after adding a row or selecting a row
- the resulting selectors and save patterns are different enough that combining them with scalar-form walkthroughs makes the test brittle

Recommended strategy:

- visit all normal forms
- exercise visible scalar, non-grid, non-multiform fields
- write separate tests for:
  - adding a multiform row
  - editing an existing multiform row
  - adding grid rows
  - deleting rows

## Why API validation is stronger than DOM-only checks

DOM assertions alone can miss:

- values that render but do not actually update `g_data`
- values that update `g_data` locally but do not persist through `/api/case`
- date/datetime normalization differences
- rich-text sanitization changes

Recommended pattern:

1. Edit a control and assert the visible control state
2. Assert the browser-side `window.g_data` value changed as expected
3. After the walkthrough, fetch `/api/case?case_id=...` and compare saved JSON to the deterministic field plan

## Known brittleness areas for future tests

- grids and repeating forms
- address validation and geography context buttons
- quick edit route behavior
- offline mode and tab-conflict modals
- dependent list confirmation dialogs
- other-specify list clearing dialogs
- mutually-exclusive checkbox groups
- rich-text whitespace and sanitization normalization
- fields that are hidden until parent values change
- attachment routes, if present in PMSS-enhanced builds

## Suggested Playwright test shape

For durable case-page tests, prefer:

1. Fetch live release-version metadata
2. Open or create a case through the UI
3. Ensure edit mode is active
4. Read the currently reachable `#selected_form` options from the DOM
5. Walk each reachable form
6. For each form, edit visible scalar fields using a deterministic field plan
7. Save after each form
8. Save and finish at the end
9. Validate the saved case JSON through `/api/case?case_id=...`

That pattern keeps the test aligned with the actual navigation and persistence model of the app.
