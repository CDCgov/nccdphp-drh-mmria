var case_validation_state = {
  rules: null,
  rules_loaded: false,
  rules_loading: false,
  filter: 'findings',
  fields: [],
  rows: []
};

const case_validation_focus_key = 'mmria_case_validation_focus';

function case_validation_render(p_result, p_metadata, p_data, p_ui) {
  if (!case_validation_state.rules_loaded && !case_validation_state.rules_loading) {
    case_validation_load_rules();
  }

  const rules = case_validation_state.rules;
  case_validation_state.fields = case_validation_flatten_metadata(p_metadata);
  case_validation_state.rows = rules
    ? case_validation_evaluate(p_data, p_metadata, rules, case_validation_state.fields)
    : [];

  const findings = case_validation_state.rows.filter(r => r.is_finding === true);
  const visibleRows = case_validation_get_visible_rows(case_validation_state.rows, case_validation_state.filter);
  const counts = case_validation_filter_counts(case_validation_state.rows);

  p_result.push('<section id="case_validation_id">');
  p_result.push(case_validation_style_html());
  case_validation_render_header(p_result, p_metadata, p_data);
  p_result.push('<div class="container-fluid case-validation-view px-0">');
  p_result.push('<div class="row no-gutters align-items-center mb-3">');
  p_result.push('<div class="col">');
  p_result.push('<h2 class="h4 mb-1">Validation Results</h2>');
  p_result.push('<p class="mb-0"><strong>');
  p_result.push(findings.length);
  p_result.push('</strong> warning');
  p_result.push(findings.length === 1 ? '' : 's');
  p_result.push('</p>');
  p_result.push('</div>');
  p_result.push('<div class="col-auto">');
  p_result.push('<button type="button" class="btn btn-outline-primary" onclick="case_validation_reload_rules()">Validate Case</button>');
  p_result.push('</div>');
  p_result.push('</div>');

  p_result.push('<div class="btn-group mb-3" role="group" aria-label="Validation filters">');
  case_validation_filter_button(p_result, 'findings', 'Findings', counts.findings);
  case_validation_filter_button(p_result, 'all', 'All Fields', counts.all);
  case_validation_filter_button(p_result, 'form-status', 'Form Status', counts.form_status);
  case_validation_filter_button(p_result, 'range', 'Ranges', counts.range);
  case_validation_filter_button(p_result, 'connected-field', 'Connected Fields', counts.connected_field);
  p_result.push('</div>');

  if (case_validation_state.rules_loading) {
    p_result.push('<div class="alert alert-info">Loading validation metadata...</div>');
  } else if (!rules) {
    p_result.push('<div class="alert alert-warning">Validation metadata is not available.</div>');
  } else if (visibleRows.length === 0) {
    p_result.push('<div class="alert alert-success">No validation rows match the current filter.</div>');
  } else {
    const grouped = case_validation_group_rows(visibleRows);
    Object.keys(grouped).forEach(formPrompt => {
      p_result.push('<div class="case-validation-group mb-4">');
      p_result.push('<h2 class="h5 mb-2">');
      p_result.push(case_validation_escape_html(formPrompt));
      p_result.push('</h2>');
      p_result.push('<div class="list-group">');
      grouped[formPrompt].forEach(row => case_validation_render_row(p_result, row));
      p_result.push('</div>');
      p_result.push('</div>');
    });
  }

  p_result.push(case_validation_quick_edit_modal_html());
  p_result.push('</div>');
  p_result.push('</section>');
}

function case_validation_filter_button(p_result, filter, label, count) {
  const active = case_validation_state.filter === filter ? ' active' : '';
  p_result.push(`<button type="button" class="btn btn-outline-secondary${active}" aria-label="${case_validation_escape_attr(label)}" onclick="case_validation_set_filter('${filter}')">${label} <span class="badge badge-light ml-1" aria-hidden="true">${count || 0}</span></button>`);
}

function case_validation_style_html() {
  return `<style>
    .case-validation-view {
      max-width: 100%;
      overflow-x: hidden;
    }

    .case-validation-header .case-validation-header-main {
      column-gap: 0;
      row-gap: .75rem;
    }

    .case-validation-header .case-validation-header-actions {
      gap: .75rem;
    }

    .case-validation-header .case-validation-header-actions {
      min-width: 0;
    }

    .case-validation-header .case-validation-lock-message {
      overflow-wrap: anywhere;
    }

    .case-validation-header .case-validation-header-actions .btn {
      margin-left: 0 !important;
    }

    .case-validation-view .btn-group[aria-label="Validation filters"] {
      display: flex;
      flex-wrap: wrap;
      gap: .25rem;
    }

    .case-validation-view .btn-group[aria-label="Validation filters"] > .btn {
      border-radius: .25rem !important;
      margin-left: 0 !important;
    }

    .case-validation-view .case-validation-finding {
      background: #fff9e8;
      border-color: #ead39a;
      border-left: 4px solid #c28a13;
      color: #1f2933;
    }

    .case-validation-view .case-validation-finding .text-muted {
      color: #52616b !important;
    }

    .case-validation-view .case-validation-field-context {
      color: #415162;
      font-size: .875rem;
      overflow-wrap: anywhere;
      word-break: break-word;
    }

    .case-validation-view .case-validation-actions {
      display: flex;
      flex-wrap: wrap;
      gap: .5rem;
      justify-content: flex-end;
    }

    .case-validation-view .case-validation-actions .btn {
      margin-right: 0 !important;
    }

    @media (max-width: 767.98px) {
      .case-validation-header .case-validation-header-actions {
        justify-content: flex-start !important;
      }

      .case-validation-view .case-validation-actions {
        justify-content: flex-start;
      }
    }
  </style>`;
}

function case_validation_render_header(p_result, p_metadata, p_data) {
  if (!p_data || !p_data.home_record) {
    return;
  }

  const editState = case_validation_case_edit_state(p_data);
  const caseFolder = case_validation_case_folder_text(p_data);
  const caseStatus = case_validation_case_status_text(p_data);
  const dateCreated = case_validation_date_created_text(p_data);
  const lastServerSave = case_validation_last_server_save_text(p_data);
  const hostState = p_data.host_state && !case_validation_is_null_or_undefined(p_data.host_state)
    ? `<p class="construct__info mb-0">Reporting state: <span>${case_validation_escape_html(p_data.host_state)}</span></p>`
    : '';

  p_result.push('<div data-header="case-validation" class="construct__header case-validation-header">');
  p_result.push('<div class="construct__header-main case-validation-header-main position-relative row no-gutters align-items-start">');
  p_result.push('<div class="col-12 col-md-5 position-static">');
  p_result.push('<p class="construct__title h1 text-primary single-form-title" tabindex="-1">');
  p_result.push(case_validation_escape_html(case_validation_case_title_text(p_data)));
  p_result.push('</p>');
  p_result.push(`<p><button type="button" onclick="show_audit_click('${case_validation_escape_js(p_data._id || '')}')"${editState.audit_button_disabled}>View Audit Log</button></p>`);
  p_result.push(`<p class="construct__info mb-0"><strong>Case Folder:</strong> ${case_validation_escape_html(caseFolder)}`);
  if (p_data.home_record.record_id) {
    p_result.push(` <strong>Record ID:</strong> ${case_validation_escape_html(p_data.home_record.record_id)}`);
  }
  p_result.push('</p>');
  p_result.push('<p class="construct__subtitle">Case Validation</p>');
  p_result.push(hostState);
  if (caseStatus) {
    p_result.push(`<p class="construct__info mb-0">Case Status: <span>${case_validation_escape_html(caseStatus)}</span></p>`);
  }
  if (dateCreated) {
    p_result.push(`<p class="construct__info mb-0">Date created: <span>${case_validation_escape_html(dateCreated)}</span></p>`);
  }
  if (lastServerSave) {
    p_result.push(`<p class="construct__info mb-0">Last server save: <span id="last_updated_span">${case_validation_escape_html(lastServerSave)}</span></p>`);
  }
  p_result.push('</div>');

  p_result.push('<div class="construct__controller col-12 col-md-7 row no-gutters justify-content-md-end mt-3 mt-md-0">');
  p_result.push('<div class="row no-gutters align-items-center justify-content-md-end case-validation-header-actions">');
  p_result.push('<span class="spinner-container spinner-inline mr-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>');
  if (editState.show_edit_controls) {
    p_result.push(editState.currently_locked_by_html);
    p_result.push(`<input type="button" class="btn btn-primary" value="Enable Edit" onclick="init_inline_loader(function() { enable_edit_click() })" ${editState.enable_edit_disable_attribute} />`);
    p_result.push(`<input type="button" class="btn btn-primary" value="Save & Finish" onclick="save_and_finish_click()" ${editState.save_and_finish_disable_attribute} />`);
  }
  p_result.push('</div>');
  p_result.push('</div>');
  p_result.push('</div>');
  p_result.push('</div>');
}

function case_validation_case_edit_state(p_data) {
  const isProcessingOfflineCases = case_validation_local_storage_value('process_offline_cases') === 'true';
  const isOfflineMode = case_validation_local_storage_value('is_offline') === 'true' || isProcessingOfflineCases;
  const caseIsLocked = typeof is_case_locked === 'function' ? is_case_locked(p_data) : false;
  const isDataAnalystMode = typeof g_is_data_analyst_mode !== 'undefined' && g_is_data_analyst_mode === true;
  const result = {
    audit_button_disabled: isOfflineMode ? ' disabled' : '',
    case_is_locked: caseIsLocked,
    show_edit_controls: !(isDataAnalystMode || caseIsLocked),
    currently_locked_by_html: '',
    enable_edit_disable_attribute: '',
    save_and_finish_disable_attribute: ' disabled="disabled" '
  };

  if (!result.show_edit_controls) {
    return result;
  }

  const isCheckedOutExpired = typeof is_checked_out_expired === 'function' ? is_checked_out_expired(p_data) : true;
  const currentUser = typeof g_user_name === 'undefined' || g_user_name == null ? '' : String(g_user_name);
  const lastCheckedOutBy = p_data.last_checked_out_by == null ? '' : String(p_data.last_checked_out_by);
  const isOfflineCase = p_data.is_offline === true || p_data.is_offline === 'true';
  const mmriaTabId = typeof get_mmria_tab_id === 'function' ? get_mmria_tab_id() : null;

  if (g_data_is_checked_out) {
    result.enable_edit_disable_attribute = ' disabled="disabled" ';
    result.save_and_finish_disable_attribute = '';
    result.currently_locked_by_html = case_validation_lock_message_html('Currently Locked By', currentUser);
  }

  if (!isCheckedOutExpired &&
      lastCheckedOutBy === currentUser &&
      (p_data.checked_out_by_tab_id == null ||
        p_data.checked_out_by_tab_id === '' ||
        (mmriaTabId != null && p_data.checked_out_by_tab_id === mmriaTabId)) &&
      !isOfflineCase) {
    result.enable_edit_disable_attribute = ' disabled ';
    result.currently_locked_by_html = '';
  }

  if (!isCheckedOutExpired && lastCheckedOutBy !== currentUser && !isOfflineCase) {
    result.enable_edit_disable_attribute = '';
    result.save_and_finish_disable_attribute = ' disabled="disabled" ';
    result.currently_locked_by_html = case_validation_lock_message_html('Currently Locked By', lastCheckedOutBy);
  }

  if (isOfflineCase && p_data.offline_by !== null && p_data.offline_by !== currentUser) {
    result.enable_edit_disable_attribute = ' disabled ';
    result.save_and_finish_disable_attribute = ' disabled="disabled" ';
    result.currently_locked_by_html = case_validation_lock_message_html('Currently Offline By', p_data.offline_by);
  }

  if (isProcessingOfflineCases) {
    result.enable_edit_disable_attribute = ' disabled ';
    result.currently_locked_by_html = '';
  }

  return result;
}

function case_validation_lock_message_html(label, value) {
  if (!value) {
    return '';
  }

  return `<i class="case-validation-lock-message">(${case_validation_escape_html(label)}: <b>${case_validation_escape_html(value)}</b>)</i>`;
}

function case_validation_case_title_text(p_data) {
  const homeRecord = p_data.home_record || {};
  const lastName = case_validation_limit_text(homeRecord.last_name || '', 20);
  const firstName = case_validation_limit_text(homeRecord.first_name || '', 20);
  return [lastName, firstName].filter(Boolean).join(', ') || 'Case Validation';
}

function case_validation_case_folder_text(p_data) {
  const jurisdictionId = p_data.home_record && p_data.home_record.jurisdiction_id;
  return jurisdictionId === '/' ? 'Top Folder' : (jurisdictionId || '');
}

function case_validation_case_status_text(p_data) {
  const currentValue = p_data &&
    p_data.home_record &&
    p_data.home_record.case_status &&
    p_data.home_record.case_status.overall_case_status;

  if (case_validation_is_null_or_undefined(currentValue) || currentValue === '') {
    return '';
  }

  let label = String(currentValue);
  try {
    if (typeof get_metadata_value_node_by_mmria_path === 'function' && typeof g_metadata !== 'undefined') {
      const lookup = get_metadata_value_node_by_mmria_path(g_metadata, '/home_record/case_status/overall_case_status', '');
      if (lookup && Array.isArray(lookup.values)) {
        const found = lookup.values.find(item => String(item.value) === String(currentValue));
        if (found && found.display) {
          label = found.display;
        }
      }
    }
  } catch (_ex) { }

  return label;
}

function case_validation_date_created_text(p_data) {
  if (case_validation_is_null_or_undefined(p_data.date_created) || p_data.date_created === '') {
    return '';
  }

  return `${p_data.created_by || ''} ${case_validation_datetime_display_text(p_data.date_created)}`.trim();
}

function case_validation_last_server_save_text(p_data) {
  if (case_validation_is_null_or_undefined(p_data.date_last_updated) || p_data.date_last_updated === '') {
    return '';
  }

  return `${p_data.last_updated_by || ''} ${case_validation_datetime_display_text(p_data.date_last_updated)}`.trim();
}

function case_validation_datetime_display_text(value) {
  try {
    if (typeof convert_datetime_to_local_display_value === 'function') {
      return convert_datetime_to_local_display_value(value);
    }
  } catch (_ex) { }

  return String(value || '');
}

function case_validation_local_storage_value(key) {
  try {
    return window.localStorage.getItem(key);
  } catch (_ex) {
    return null;
  }
}

function case_validation_limit_text(value, maxLength) {
  if (typeof set_character_limit === 'function') {
    return set_character_limit(value, maxLength);
  }

  const text = String(value == null ? '' : value);
  if (text.length <= maxLength) {
    return text;
  }

  return `${text.substring(0, maxLength)}...`;
}

function case_validation_is_null_or_undefined(value) {
  if (typeof isNullOrUndefined === 'function') {
    return isNullOrUndefined(value);
  }

  return value === null || value === undefined;
}

function case_validation_set_filter(filter) {
  case_validation_state.filter = filter || 'findings';
  g_render();
}

async function case_validation_load_rules() {
  case_validation_state.rules_loading = true;
  try {
    const response = await fetch('/api/case-validation/rules/current', {
      method: 'GET',
      headers: { 'Accept': 'application/json' }
    });
    if (response.ok) {
      case_validation_state.rules = await response.json();
      case_validation_state.rules_loaded = true;
    }
  } catch (ex) {
    console.log(ex);
  } finally {
    case_validation_state.rules_loading = false;
    if (g_ui && g_ui.url_state && g_ui.url_state.selected_id === 'case_validation') {
      g_render();
    }
  }
}

function case_validation_reload_rules() {
  case_validation_state.rules = null;
  case_validation_state.rules_loaded = false;
  case_validation_load_rules();
}

function case_validation_get_visible_rows(rows, filter) {
  if (filter === 'all') {
    return rows.filter(r => r.category === 'field');
  }

  if (filter === 'findings') {
    return rows.filter(r => r.is_finding === true);
  }

  return rows.filter(r => r.category === filter);
}

function case_validation_filter_counts(rows) {
  return {
    findings: rows.filter(r => r.is_finding === true).length,
    all: rows.filter(r => r.category === 'field').length,
    form_status: rows.filter(r => r.category === 'form-status').length,
    range: rows.filter(r => r.category === 'range').length,
    connected_field: rows.filter(r => r.category === 'connected-field').length
  };
}

function case_validation_group_rows(rows) {
  return rows.reduce((acc, row) => {
    const key = row.form_prompt || row.form_path || 'Case';
    if (!acc[key]) {
      acc[key] = [];
    }
    acc[key].push(row);
    return acc;
  }, {});
}

function case_validation_render_row(p_result, row) {
  const findingClass = row.is_finding ? ' case-validation-finding' : '';
  const badge = row.is_finding ? case_validation_escape_html(row.category || 'warning') : (row.category === 'field' ? 'field' : 'OK');
  const value = row.value == null || row.value === '' ? '(blank)' : row.value;
  const expected = row.expected ? `<div><strong>Expected:</strong> ${case_validation_escape_html(row.expected)}</div>` : '';
  const problemField = row.is_finding ? case_validation_problem_field_html(row) : '';
  const comparedField = row.is_finding ? case_validation_related_field_html(row) : '';
  const reviewParts = [row.validation_level, row.confidence, row.review_status].filter(Boolean).map(case_validation_escape_html);
  const review = reviewParts.length > 0 ? `<div class="small text-muted">${reviewParts.join(' | ')}</div>` : '';
  const rationale = row.rationale ? `<div class="small"><strong>Why:</strong> ${case_validation_escape_html(row.rationale)}</div>` : '';
  const explanation = row.explanation ? `<div class="small text-muted">${case_validation_escape_html(row.explanation)}</div>` : '';
  const quickEditDisabled = !(row.can_quick_edit === true && g_data_is_checked_out === true);
  const quickEditTitle = quickEditDisabled ? 'Quick Edit requires edit mode and a supported scalar field.' : 'Quick Edit';

  p_result.push(`<div class="list-group-item${findingClass}" data-validation-field="${case_validation_escape_attr(row.field_path || '')}">`);
  p_result.push('<div class="row no-gutters align-items-start">');
  p_result.push('<div class="col">');
  p_result.push('<div class="d-flex align-items-center mb-1">');
  p_result.push(`<span class="badge badge-secondary mr-2">${badge}</span>`);
  p_result.push(`<strong>${case_validation_escape_html(row.prompt || row.field_path || '')}</strong>`);
  p_result.push('</div>');
  p_result.push(`<div>${case_validation_escape_html(row.message || row.subject || '')}</div>`);
  p_result.push(problemField);
  p_result.push(comparedField);
  p_result.push(`<div><strong>Value:</strong> ${case_validation_escape_html(value)}</div>`);
  p_result.push(expected);
  p_result.push(rationale);
  p_result.push(explanation);
  p_result.push(review);
  p_result.push('</div>');
  p_result.push('<div class="col-12 col-md-auto ml-md-3 mt-2 mt-md-0 case-validation-actions">');
  p_result.push(`<button type="button" class="btn btn-sm btn-primary mr-2" onclick="case_validation_open_field('${case_validation_escape_js(row.form_path || '')}', '${case_validation_escape_js(row.field_path || '')}')">Open Field</button>`);
  p_result.push(`<button type="button" class="btn btn-sm btn-primary" title="${case_validation_escape_attr(quickEditTitle)}" ${quickEditDisabled ? 'disabled' : ''} onclick="case_validation_open_quick_edit('${case_validation_escape_js(row.field_path || '')}')">Quick Edit</button>`);
  p_result.push('</div>');
  p_result.push('</div>');
  p_result.push('</div>');
}

function case_validation_problem_field_html(row) {
  const text = case_validation_field_locator_text(row);
  return text ? `<div class="case-validation-field-context"><strong>Problem field:</strong> ${case_validation_escape_html(text)}</div>` : '';
}

function case_validation_related_field_html(row) {
  if (!row.related_field_path && !row.related_prompt) {
    return '';
  }

  const related = {
    form_prompt: row.form_prompt,
    prompt: row.related_prompt,
    field_path: row.related_field_path,
    array_indexes: row.related_array_indexes,
    occurrence_index: row.related_occurrence_index
  };
  let text = case_validation_field_locator_text(related);
  if (row.related_value != null && row.related_value !== '') {
    text += `; value ${row.related_value}`;
  }

  return text ? `<div class="case-validation-field-context"><strong>Compared with:</strong> ${case_validation_escape_html(text)}</div>` : '';
}

function case_validation_field_locator_text(row) {
  const parts = [];
  case_validation_add_distinct_locator_part(parts, row.form_prompt);
  String(row.subject || '').split('/').forEach(part => case_validation_add_distinct_locator_part(parts, part));
  case_validation_add_distinct_locator_part(parts, row.prompt);

  const label = parts.join(' / ');
  const details = [];
  if (row.field_path) {
    details.push(row.field_path);
  }

  const occurrence = case_validation_occurrence_text(row.array_indexes, row.occurrence_index);
  if (occurrence) {
    details.push(occurrence);
  }

  if (label && details.length > 0) {
    return `${label} (${details.join(' | ')})`;
  }

  return label || details.join(' | ');
}

function case_validation_add_distinct_locator_part(parts, value) {
  const text = String(value || '').trim();
  const normalized = case_validation_normalize_subject(text);
  if (!normalized || parts.some(part => case_validation_normalize_subject(part) === normalized)) {
    return;
  }

  parts.push(text);
}

function case_validation_occurrence_text(arrayIndexes, occurrenceIndex) {
  if (Array.isArray(arrayIndexes) && arrayIndexes.length > 0) {
    return `occurrence ${arrayIndexes.map(index => Number(index) + 1).join('.')}`;
  }

  if (occurrenceIndex != null && Number(occurrenceIndex) > 0) {
    return `occurrence ${Number(occurrenceIndex) + 1}`;
  }

  return '';
}

function case_validation_flatten_metadata(metadata) {
  const result = [];
  if (!metadata || !Array.isArray(metadata.children)) {
    return result;
  }

  const lookup = case_validation_build_lookup_map(metadata);
  metadata.children.forEach((form, index) => {
    if (!form || (form.type || '').toLowerCase() !== 'form') {
      return;
    }

    case_validation_flatten_node({
      node: form,
      form_path: form.name,
      form_prompt: form.prompt,
      field_path: form.name,
      metadata_path: `g_metadata.children[${index}]`,
      ancestry: [form.prompt],
      is_multiform: case_validation_is_multi(form),
      is_grid: false,
      lookup,
      result
    });
  });

  return result;
}

function case_validation_flatten_node(ctx) {
  const node = ctx.node;
  const type = (node.type || '').toLowerCase();
  const isGrid = ctx.is_grid || type === 'grid';
  const isScalar = case_validation_is_scalar_type(type);
  const isMultiform = ctx.is_multiform || case_validation_is_multi(node);
  const subject = case_validation_build_subject(node, ctx.ancestry);
  const values = case_validation_resolve_values(node, ctx.lookup).map(v => ({ value: String(v.value), display: v.display || String(v.value) }));
  const field = {
    form_path: ctx.form_path,
    form_prompt: ctx.form_prompt,
    field_path: ctx.field_path,
    metadata_path: ctx.metadata_path,
    prompt: node.prompt,
    name: node.name,
    type: node.type,
    data_type: node.data_type || node.list_item_data_type,
    cardinality: node.cardinality,
    subject,
    path_reference: node.path_reference,
    values,
    tags: node.tags || [],
    min_value: node.min_value,
    max_value: node.max_value,
    max_length: node.max_length,
    regex_pattern: node.regex_pattern,
    validation_description: node.validation_description,
    is_multiform: isMultiform,
    is_grid: isGrid,
    is_required: node.is_required === true,
    is_hidden: node.is_hidden === true,
    is_read_only: node.is_read_only === true,
    is_scalar: isScalar,
    can_quick_edit: isScalar && !isMultiform && !isGrid && node.is_read_only !== true && node.is_hidden !== true && node.is_multiselect !== true
  };

  result_push(ctx.result, field);

  if (!Array.isArray(node.children)) {
    return;
  }

  node.children.forEach((child, index) => {
    const ancestry = ctx.ancestry.slice();
    if (node.prompt && type !== 'form') {
      ancestry.push(node.prompt);
    }

    case_validation_flatten_node({
      node: child,
      form_path: ctx.form_path,
      form_prompt: ctx.form_prompt,
      field_path: `${ctx.field_path}/${child.name}`,
      metadata_path: `${ctx.metadata_path}.children[${index}]`,
      ancestry,
      is_multiform: isMultiform,
      is_grid: isGrid,
      lookup: ctx.lookup,
      result: ctx.result
    });
  });
}

function case_validation_build_lookup_map(metadata) {
  const result = {};
  (metadata.lookup || []).forEach(item => {
    if (item && item.name) {
      result[`lookup/${item.name}`] = Array.isArray(item.values) ? item.values : [];
    }
  });
  return result;
}

function case_validation_resolve_values(node, lookup) {
  if (Array.isArray(node.values) && node.values.length > 0) {
    return node.values;
  }

  if (node.path_reference && lookup && Array.isArray(lookup[node.path_reference])) {
    return lookup[node.path_reference];
  }

  return [];
}

function result_push(result, item) {
  result.push(item);
}

function case_validation_evaluate(data, metadata, rules, fields) {
  const rows = [];
  const fieldMap = fields.reduce((acc, field) => {
    acc[field.field_path] = field;
    return acc;
  }, {});

  fields.filter(f => f.is_scalar).forEach(field => {
    case_validation_get_values(data, field.field_path).forEach(valueCtx => {
      rows.push({
        id: `field:${field.field_path}:${rows.length}`,
        category: 'field',
        severity: 'ok',
        form_path: field.form_path,
        form_prompt: field.form_prompt,
        field_path: field.field_path,
        metadata_path: field.metadata_path,
        prompt: field.prompt,
        subject: field.subject,
        value: case_validation_value_to_text(valueCtx.value, field),
        expected: case_validation_expected_text_for_field(field),
        message: field.subject,
        is_finding: false,
        validation_level: 'metadata',
        confidence: 'high',
        review_status: 'generated',
        source: 'metadata',
        rationale: field.validation_description || '',
        explanation: field.validation_description || '',
        can_quick_edit: field.can_quick_edit
      });
    });
  });

  if (rules.enabled === false) {
    return rows;
  }

  case_validation_evaluate_form_status(data, rules, fieldMap, rows);
  case_validation_evaluate_field_rules(data, rules, fieldMap, rows);
  case_validation_evaluate_connected_rules(data, rules, fieldMap, rows);
  return rows;
}

function case_validation_evaluate_form_status(data, rules, fieldMap, rows) {
  (rules.form_status_rules || []).filter(r => r.enabled !== false).forEach(rule => {
    const statusField = fieldMap[rule.status_field_path] || {};
    const statusValue = case_validation_get_values(data, rule.status_field_path)[0]?.value;
    const statusText = case_validation_value_to_text(statusValue, statusField);
    const statusKind = case_validation_normalize_status(statusValue, statusText);
    const meaningfulCount = case_validation_count_meaningful(case_validation_get_path_value(data, rule.form_path), `${rule.form_path}/`, [rule.status_field_path]);
    const dataPresentThreshold = Number(rule.data_present_min_meaningful_fields || 1);
    const completedThreshold = Number(rule.completed_min_meaningful_fields || 2);
    let expected = 'Status should match the meaningful data present in the form.';
    let message = `${rule.form_prompt || rule.form_path} status matches the current form data.`;
    let isFinding = false;

    if (meaningfulCount >= dataPresentThreshold && ['not-started', 'not-applicable', 'not-available'].indexOf(statusKind) > -1) {
      isFinding = true;
      expected = 'In Progress or Completed';
      message = rule.message || `${rule.form_prompt} status does not match data present in the form.`;
    }

    if (statusKind === 'completed' && meaningfulCount < completedThreshold) {
      isFinding = true;
      expected = `At least ${completedThreshold} meaningful fields`;
      message = `${rule.form_prompt} is marked Completed but has little meaningful data.`;
    }

    rows.push({
      id: `${rule.id}:form-status`,
      rule_id: rule.id,
      category: 'form-status',
      severity: isFinding ? (rule.severity || 'warning') : 'ok',
      form_path: rule.form_path,
      form_prompt: rule.form_prompt,
      field_path: rule.status_field_path,
      metadata_path: statusField.metadata_path,
      prompt: rule.status_field_prompt || statusField.prompt,
      subject: 'form status',
      value: statusText,
      expected,
      message,
      validation_level: rule.validation_level,
      confidence: rule.confidence,
      review_status: rule.review_status,
      source: rule.source,
      rationale: rule.rationale,
      admin_notes: rule.admin_notes,
      explanation: rule.explanation || case_validation_explain_rule(rule),
      is_finding: isFinding,
      can_quick_edit: statusField.can_quick_edit
    });
  });
}

function case_validation_evaluate_field_rules(data, rules, fieldMap, rows) {
  (rules.field_rules || []).filter(r => r.enabled !== false).forEach(rule => {
    const field = fieldMap[rule.field_path];
    if (!field) {
      return;
    }

    case_validation_get_values(data, rule.field_path).forEach((valueCtx, index) => {
      const issue = case_validation_is_blank_value(valueCtx.value) ? null : case_validation_field_rule_issue(rule, valueCtx.value);
      rows.push({
        id: `${rule.id}:${index}`,
        rule_id: rule.id,
        category: 'range',
        severity: issue ? (rule.severity || 'warning') : 'ok',
        form_path: rule.form_path,
        form_prompt: rule.form_prompt,
        field_path: rule.field_path,
        metadata_path: rule.metadata_path,
        prompt: rule.prompt,
        subject: rule.subject,
        array_indexes: valueCtx.array_indexes,
        occurrence_index: valueCtx.occurrence_index,
        value: case_validation_value_to_text(valueCtx.value, field),
        expected: issue ? issue.expected : case_validation_rule_expected_text(rule),
        message: issue ? (issue.message || rule.message || `${rule.prompt} is outside expected values.`) : (rule.rationale || rule.message || rule.subject),
        validation_level: rule.validation_level,
        confidence: rule.confidence,
        review_status: rule.review_status,
        source: rule.source,
        rationale: rule.rationale,
        admin_notes: rule.admin_notes,
        explanation: rule.explanation || case_validation_explain_rule(rule),
        is_finding: !!issue,
        can_quick_edit: field.can_quick_edit
      });
    });
  });
}

function case_validation_evaluate_connected_rules(data, rules, fieldMap, rows) {
  (rules.connected_field_rules || []).filter(r => r.enabled !== false).forEach(rule => {
    const field = fieldMap[rule.field_path] || { can_quick_edit: false };
    const relatedField = fieldMap[rule.related_field_path] || null;
    const pairs = case_validation_get_connected_value_pairs(data, rule);
    pairs.forEach((pair, index) => {
      const valueCtx = pair.valueCtx;
      const relatedCtx = pair.relatedCtx;
      const relatedValue = relatedCtx?.value;
      const issue = case_validation_connected_rule_issue(rule, valueCtx.value, relatedValue);
      rows.push({
        id: `${rule.id}:${index}`,
        rule_id: rule.id,
        category: 'connected-field',
        severity: issue ? (rule.severity || 'warning') : 'ok',
        form_path: rule.form_path,
        form_prompt: rule.form_prompt,
        field_path: rule.field_path,
        related_field_path: rule.related_field_path,
        metadata_path: rule.metadata_path,
        prompt: rule.prompt,
        related_prompt: rule.related_prompt,
        subject: rule.subject,
        array_indexes: valueCtx.array_indexes,
        occurrence_index: valueCtx.occurrence_index,
        related_array_indexes: relatedCtx?.array_indexes,
        related_occurrence_index: relatedCtx?.occurrence_index,
        value: case_validation_value_to_text(valueCtx.value, field),
        related_value: case_validation_value_to_text(relatedValue, relatedField),
        expected: issue ? issue.expected : case_validation_connected_expected_text(rule),
        message: issue ? (issue.message || rule.message) : (rule.rationale || rule.message),
        validation_level: rule.validation_level,
        confidence: rule.confidence,
        review_status: rule.review_status,
        source: rule.source,
        rationale: rule.rationale,
        admin_notes: rule.admin_notes,
        explanation: rule.explanation || case_validation_explain_rule(rule),
        is_finding: !!issue,
        can_quick_edit: field.can_quick_edit
      });
    });
  });
}

function case_validation_get_connected_value_pairs(data, rule) {
  if (rule.require_same_container) {
    const sharedPath = case_validation_shared_container_path(rule.field_path, rule.related_field_path);
    if (sharedPath) {
      const containers = case_validation_get_values(data, sharedPath);
      return containers.flatMap(container => {
        const valueSuffix = case_validation_remove_path_prefix(rule.field_path, sharedPath);
        const relatedSuffix = case_validation_remove_path_prefix(rule.related_field_path, sharedPath);
        return case_validation_build_indexed_pairs(
          case_validation_get_relative_values(container, valueSuffix),
          case_validation_get_relative_values(container, relatedSuffix)
        );
      });
    }
  }

  return case_validation_build_indexed_pairs(
    case_validation_get_values(data, rule.field_path),
    case_validation_get_values(data, rule.related_field_path)
  );
}

function case_validation_build_indexed_pairs(values, relatedValues) {
  const valueList = values && values.length ? values : [{ value: null }];
  const relatedList = relatedValues && relatedValues.length ? relatedValues : [{ value: null }];
  const count = Math.max(valueList.length, relatedList.length);
  const result = [];
  for (let i = 0; i < count; i += 1) {
    result.push({
      valueCtx: valueList.length === 1 ? valueList[0] : valueList[Math.min(i, valueList.length - 1)],
      relatedCtx: relatedList.length === 1 ? relatedList[0] : relatedList[Math.min(i, relatedList.length - 1)]
    });
  }
  return result;
}

function case_validation_get_relative_values(containerCtx, suffix) {
  if (!containerCtx || containerCtx.value == null) {
    return [{ value: null }];
  }

  if (!suffix) {
    return [containerCtx];
  }

  return case_validation_get_values(containerCtx.value, suffix);
}

function case_validation_shared_container_path(leftPath, rightPath) {
  const left = case_validation_split_path(leftPath);
  const right = case_validation_split_path(rightPath);
  const shared = [];
  for (let i = 0; i < left.length && i < right.length; i += 1) {
    if (String(left[i]).toLowerCase() !== String(right[i]).toLowerCase()) {
      break;
    }
    shared.push(left[i]);
  }
  return shared.join('/');
}

function case_validation_remove_path_prefix(path, prefix) {
  if (!path || !prefix) {
    return path || '';
  }
  if (String(path).toLowerCase() === String(prefix).toLowerCase()) {
    return '';
  }
  return String(path).toLowerCase().indexOf(String(prefix).toLowerCase() + '/') === 0
    ? String(path).substring(String(prefix).length + 1)
    : path;
}

function case_validation_split_path(path) {
  return String(path || '').split('/').filter(Boolean);
}

function case_validation_make_finding(rule, field, value, expected, message, category) {
  return {
    id: `${rule.id}:${category}:${Date.now()}:${Math.random()}`,
    category,
    severity: rule.severity || 'warning',
    form_path: rule.form_path,
    form_prompt: rule.form_prompt,
    field_path: rule.status_field_path || rule.field_path,
    metadata_path: field.metadata_path,
    prompt: rule.status_field_prompt || rule.prompt,
    subject: 'form status',
    value,
    expected,
    message,
    is_finding: true,
    can_quick_edit: field.can_quick_edit
  };
}

function case_validation_field_rule_issue(rule, value) {
  const text = value == null ? '' : String(value);
  if (Array.isArray(rule.allowed_values) && rule.allowed_values.length > 0 && rule.allowed_values.map(String).indexOf(text) === -1) {
    return { expected: 'Accepted list value', message: `${rule.prompt} is not an accepted list value.` };
  }

  const numberValue = Number(value);
  if (!Number.isNaN(numberValue)) {
    if (rule.min_value != null && numberValue < Number(rule.min_value)) {
      return { expected: `>= ${rule.min_value}` };
    }

    if (rule.max_value != null && numberValue > Number(rule.max_value)) {
      return { expected: `<= ${rule.max_value}` };
    }
  }

  if (rule.max_length != null && text.length > Number(rule.max_length)) {
    return { expected: `Length <= ${rule.max_length}` };
  }

  if (rule.regex_pattern) {
    try {
      if (!(new RegExp(rule.regex_pattern)).test(text)) {
        return { expected: `Pattern ${rule.regex_pattern}` };
      }
    } catch (_ex) {
      return null;
    }
  }

  return null;
}

function case_validation_rule_expected_text(rule) {
  const parts = [];
  if (rule.min_value != null && rule.min_value !== '') {
    parts.push(`>= ${rule.min_value}`);
  }
  if (rule.max_value != null && rule.max_value !== '') {
    parts.push(`<= ${rule.max_value}`);
  }
  if (rule.max_length != null && rule.max_length !== '') {
    parts.push(`length <= ${rule.max_length}`);
  }
  if (rule.regex_pattern) {
    parts.push(`pattern ${rule.regex_pattern}`);
  }
  if (Array.isArray(rule.allowed_values) && rule.allowed_values.length > 0) {
    parts.push('accepted list value');
  }
  return parts.join(', ');
}

function case_validation_explain_rule(rule) {
  if (!rule) {
    return '';
  }

  const field = rule.prompt || rule.field_path || rule.form_prompt || 'This rule';
  const level = rule.validation_level || 'metadata';
  const confidence = rule.confidence || 'medium';
  if (rule.category === 'connected-field') {
    const related = rule.related_prompt || rule.related_field_path || 'a related field';
    const expected = case_validation_connected_expected_text(rule) || 'consistent related values';
    return `${field} is compared with ${related} as ${level} validation with ${confidence} confidence; expected ${expected}.`;
  }

  if (rule.category === 'form-status') {
    return `${field} is checked as form-completeness validation with ${confidence} confidence; status should match meaningful data present in the form.`;
  }

  const expected = case_validation_rule_expected_text(rule) || 'the configured validation metadata';
  return `${field} is checked as ${level} validation with ${confidence} confidence; expected ${expected}.`;
}

function case_validation_connected_rule_issue(rule, value, related) {
  if (rule.rule_type === 'conditional_other_requires_specify') {
    if (case_validation_has_trigger_value(value, rule) && case_validation_is_blank_value(related)) {
      return { expected: `${rule.related_prompt} is entered when Other is selected`, message: rule.message };
    }
    return null;
  }

  if (case_validation_is_blank_value(value) || case_validation_is_blank_value(related)) {
    return null;
  }

  const lhs = Number(value);
  const rhs = Number(related);
  if (rule.rule_type === 'numeric_less_than_or_equal' && !Number.isNaN(lhs) && !Number.isNaN(rhs) && lhs > rhs) {
    return { expected: `${rule.prompt} <= ${rule.related_prompt}`, message: rule.message };
  }

  if (rule.rule_type === 'numeric_greater_than_or_equal' && !Number.isNaN(lhs) && !Number.isNaN(rhs) && lhs < rhs) {
    return { expected: `${rule.prompt} >= ${rule.related_prompt}`, message: rule.message };
  }

  if (rule.rule_type === 'numeric_max' && !Number.isNaN(lhs) && rule.max_difference != null && lhs > Number(rule.max_difference)) {
    return { expected: `Value <= ${rule.max_difference}`, message: rule.message };
  }

  const leftDate = case_validation_parse_date(value);
  const rightDate = case_validation_parse_date(related);
  if (rule.rule_type === 'date_less_than_or_equal' && leftDate && rightDate && leftDate.getTime() > rightDate.getTime()) {
    return { expected: `${rule.prompt} on or before ${rule.related_prompt}`, message: rule.message };
  }

  if (rule.rule_type === 'datetime_less_than_or_equal') {
    const leftDateTime = case_validation_parse_date_time(value);
    const rightDateTime = case_validation_parse_date_time(related);
    if (leftDateTime && rightDateTime &&
      (leftDateTime.date.getTime() > rightDateTime.date.getTime() ||
        (leftDateTime.date.getTime() === rightDateTime.date.getTime() &&
          leftDateTime.timeMs != null &&
          rightDateTime.timeMs != null &&
          leftDateTime.timeMs > rightDateTime.timeMs))) {
      return { expected: `${rule.prompt} on or before ${rule.related_prompt}`, message: rule.message };
    }
  }

  if (rule.rule_type === 'date_greater_than_or_equal' && leftDate && rightDate && leftDate.getTime() < rightDate.getTime()) {
    return { expected: `${rule.prompt} on or after ${rule.related_prompt}`, message: rule.message };
  }

  if (rule.rule_type === 'date_equal' && leftDate && rightDate && leftDate.getTime() !== rightDate.getTime()) {
    return { expected: `${rule.prompt} matches ${rule.related_prompt}`, message: rule.message };
  }

  return null;
}

function case_validation_connected_expected_text(rule) {
  if (rule.rule_type === 'numeric_less_than_or_equal') {
    return `${rule.prompt} <= ${rule.related_prompt}`;
  }
  if (rule.rule_type === 'numeric_greater_than_or_equal') {
    return `${rule.prompt} >= ${rule.related_prompt}`;
  }
  if (rule.rule_type === 'numeric_max' && rule.max_difference != null) {
    return `Value <= ${rule.max_difference}`;
  }
  if (rule.rule_type === 'date_less_than_or_equal') {
    return `${rule.prompt} on or before ${rule.related_prompt}`;
  }
  if (rule.rule_type === 'datetime_less_than_or_equal') {
    return `${rule.prompt} on or before ${rule.related_prompt}`;
  }
  if (rule.rule_type === 'date_greater_than_or_equal') {
    return `${rule.prompt} on or after ${rule.related_prompt}`;
  }
  if (rule.rule_type === 'date_equal') {
    return `${rule.prompt} matches ${rule.related_prompt}`;
  }
  if (rule.rule_type === 'conditional_other_requires_specify') {
    return `${rule.related_prompt} is entered when Other is selected`;
  }
  return rule.comparison || '';
}

function case_validation_parse_date(value) {
  if (case_validation_is_blank_value(value)) {
    return null;
  }

  if (typeof value === 'object') {
    const month = Number(value.month);
    const day = Number(value.day);
    const year = Number(value.year);
    if (!Number.isInteger(month) || !Number.isInteger(day) || !Number.isInteger(year) ||
      month < 1 || month > 12 || day < 1 || day > 31 || year < 1 ||
      case_validation_is_blank_value(value.month) || case_validation_is_blank_value(value.day) || case_validation_is_blank_value(value.year)) {
      return null;
    }

    return case_validation_date_from_parts(year, month, day);
  }

  const text = String(value).trim();
  const isoDateMatch = text.match(/^(\d{4})-(\d{1,2})-(\d{1,2})(?:$|[T\s])/);
  if (isoDateMatch) {
    return case_validation_date_from_parts(Number(isoDateMatch[1]), Number(isoDateMatch[2]), Number(isoDateMatch[3]));
  }

  const slashDateMatch = text.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})(?:$|\s)/);
  if (slashDateMatch) {
    return case_validation_date_from_parts(Number(slashDateMatch[3]), Number(slashDateMatch[1]), Number(slashDateMatch[2]));
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return null;
  }

  return new Date(Date.UTC(parsed.getFullYear(), parsed.getMonth(), parsed.getDate()));
}

function case_validation_date_from_parts(year, month, day) {
  if (!Number.isInteger(month) || !Number.isInteger(day) || !Number.isInteger(year) ||
    month < 1 || month > 12 || day < 1 || day > 31 || year < 1) {
    return null;
  }

  const parsed = new Date(Date.UTC(year, month - 1, day));
  return parsed.getUTCFullYear() === year && parsed.getUTCMonth() === month - 1 && parsed.getUTCDate() === day ? parsed : null;
}

function case_validation_parse_date_time(value) {
  const date = case_validation_parse_date(value);
  if (!date) {
    return null;
  }

  let timeMs = null;
  if (value && typeof value === 'object' && !Array.isArray(value)) {
    Object.keys(value).some(key => {
      if (String(key).toLowerCase().indexOf('time') > -1) {
        const parsed = case_validation_parse_time_ms(value[key]);
        if (parsed != null) {
          timeMs = parsed;
          return true;
        }
      }
      return false;
    });
  } else {
    timeMs = case_validation_parse_time_ms(value);
  }

  return { date, timeMs };
}

function case_validation_parse_time_ms(value) {
  if (case_validation_is_blank_value(value)) {
    return null;
  }

  const text = String(value).trim();
  const timeMatch = text.match(/^(\d{1,2}):(\d{2})(?::(\d{2}))?\s*(AM|PM)?$/i);
  if (timeMatch) {
    let hour = Number(timeMatch[1]);
    const minute = Number(timeMatch[2]);
    const second = Number(timeMatch[3] || 0);
    const ampm = (timeMatch[4] || '').toUpperCase();
    if (ampm === 'PM' && hour < 12) {
      hour += 12;
    }
    if (ampm === 'AM' && hour === 12) {
      hour = 0;
    }
    if (hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59 && second >= 0 && second <= 59) {
      return ((hour * 60 + minute) * 60 + second) * 1000;
    }
  }

  const parsed = new Date(text);
  return Number.isNaN(parsed.getTime()) ? null : ((parsed.getHours() * 60 + parsed.getMinutes()) * 60 + parsed.getSeconds()) * 1000;
}

function case_validation_has_trigger_value(value, rule) {
  if (case_validation_is_blank_value(value)) {
    return false;
  }

  if (Array.isArray(value)) {
    return value.some(item => case_validation_has_trigger_value(item, rule));
  }

  return case_validation_matches_trigger(value, rule.trigger_values) ||
    case_validation_matches_trigger(value, rule.trigger_displays);
}

function case_validation_matches_trigger(value, triggers) {
  if (!Array.isArray(triggers)) {
    return false;
  }

  const text = String(value == null ? '' : value).trim();
  return triggers.some(trigger => {
    const candidate = String(trigger == null ? '' : trigger).trim();
    if (!candidate) {
      return false;
    }
    if (text.toLowerCase() === candidate.toLowerCase()) {
      return true;
    }
    const lhs = Number(text);
    const rhs = Number(candidate);
    return !Number.isNaN(lhs) && !Number.isNaN(rhs) && Math.abs(lhs - rhs) < 0.000001;
  });
}

function case_validation_get_values(root, path) {
  const result = [];
  case_validation_collect_values(root, path.split('/'), 0, result, []);
  return result.length > 0 ? result : [{ value: null }];
}

function case_validation_collect_values(current, parts, index, result, arrayIndexes) {
  if (current == null) {
    return;
  }

  if (Array.isArray(current)) {
    current.forEach((item, arrayIndex) => {
      case_validation_collect_values(item, parts, index, result, (arrayIndexes || []).concat(arrayIndex));
    });
    return;
  }

  if (index >= parts.length) {
    result.push({ value: current, array_indexes: arrayIndexes || [], occurrence_index: result.length });
    return;
  }

  case_validation_collect_values(current[parts[index]], parts, index + 1, result, arrayIndexes || []);
}

function case_validation_get_path_value(root, path) {
  if (!root || !path) {
    return null;
  }

  return path.split('/').reduce((current, part) => current == null ? null : current[part], root);
}

function case_validation_count_meaningful(value, pathPrefix, ignoredPaths) {
  if (case_validation_is_blank_value(value)) {
    return 0;
  }

  const ignored = ignoredPaths || [];
  if (Array.isArray(value)) {
    return value.reduce((sum, item) => sum + case_validation_count_meaningful(item, pathPrefix, ignored), 0);
  }

  if (typeof value === 'object') {
    return Object.keys(value).reduce((sum, key) => {
      const path = pathPrefix ? `${String(pathPrefix).replace(/\/$/, '')}/${key}` : key;
      if (ignored.indexOf(path) > -1) {
        return sum;
      }
      return sum + case_validation_count_meaningful(value[key], path, ignored);
    }, 0);
  }

  return 1;
}

function case_validation_is_blank_value(value) {
  if (value == null) {
    return true;
  }

  if (Array.isArray(value)) {
    return value.length === 0;
  }

  if (typeof value === 'object') {
    return Object.keys(value).length === 0;
  }

  const text = String(value).trim().toLowerCase();
  return ['', '9999', '9998', '8888', '7777', '6666', '9999.0', '9998.0', '8888.0', '7777.0', '6666.0', '(select value)', 'select value'].indexOf(text) > -1;
}

function case_validation_expected_text_for_field(field) {
  const parts = [];
  if (field.min_value != null && field.min_value !== '') {
    parts.push(`>= ${field.min_value}`);
  }
  if (field.max_value != null && field.max_value !== '') {
    parts.push(`<= ${field.max_value}`);
  }
  if (field.max_length != null && field.max_length !== '') {
    parts.push(`length <= ${field.max_length}`);
  }
  if (field.values && field.values.length > 0) {
    parts.push('accepted list value');
  }
  return parts.join(', ');
}

function case_validation_value_to_text(value, field) {
  if (case_validation_is_blank_value(value)) {
    return '';
  }

  if (field && field.values && field.values.length > 0) {
    const found = field.values.find(v => String(v.value) === String(value));
    if (found) {
      return found.display || found.value;
    }
  }

  if (typeof value === 'object') {
    if (value.year || value.month || value.day) {
      return [value.month, value.day, value.year].filter(v => !case_validation_is_blank_value(v)).join('/');
    }

    return JSON.stringify(value);
  }

  return String(value);
}

function case_validation_normalize_status(value, display) {
  const raw = value == null ? '' : String(value).trim();
  if (raw === '0') return 'not-started';
  if (raw === '1') return 'in-progress';
  if (raw === '2') return 'completed';
  if (raw === '3') return 'not-available';
  if (raw === '4') return 'not-applicable';
  return case_validation_normalize_subject(display || raw).replace(/\s/g, '-');
}

function case_validation_open_field(formPath, fieldPath) {
  try {
    window.sessionStorage.setItem(case_validation_focus_key, JSON.stringify({ field_path: fieldPath }));
  } catch (_ex) { }
  window.location.hash = '#/' + g_ui.url_state.path_array[0] + '/' + formPath;
}

function case_validation_apply_pending_focus() {
  let focus = null;
  try {
    const raw = window.sessionStorage.getItem(case_validation_focus_key);
    if (raw) {
      focus = JSON.parse(raw);
      window.sessionStorage.removeItem(case_validation_focus_key);
    }
  } catch (_ex) {
    focus = null;
  }

  if (!focus || !focus.field_path) {
    return;
  }

  window.setTimeout(() => {
    const objectPath = 'g_data.' + focus.field_path.replace(/\//g, '.');
    const baseId = convert_object_path_to_jquery_id(objectPath);
    const target = document.getElementById(baseId + '_control') || document.getElementById(baseId);
    if (!target) {
      return;
    }

    target.classList.add('case-validation-focus');
    target.style.outline = '3px solid #8a1c7c';
    target.style.outlineOffset = '2px';
    target.scrollIntoView({ behavior: 'smooth', block: 'center' });
    if (typeof target.focus === 'function') {
      target.focus();
    }

    window.setTimeout(() => {
      target.style.outline = '';
      target.style.outlineOffset = '';
      target.classList.remove('case-validation-focus');
    }, 4500);
  }, 250);
}

function case_validation_open_quick_edit(fieldPath) {
  const field = case_validation_state.fields.find(f => f.field_path === fieldPath);
  if (!field || field.can_quick_edit !== true || g_data_is_checked_out !== true) {
    return;
  }

  const value = case_validation_get_values(g_data, fieldPath)[0]?.value;
  const html = case_validation_quick_edit_input_html(field, value);
  $('#case_validation_quick_edit_field_path').val(field.field_path);
  $('#case_validation_quick_edit_metadata_path').val(field.metadata_path);
  $('#case_validation_quick_edit_title').text(field.prompt || field.field_path);
  $('#case_validation_quick_edit_body').html(html);
  $('#case_validation_quick_edit_error').text('').hide();
  $('#case_validation_quick_edit_modal').modal('show');
}

function case_validation_quick_edit_input_html(field, value) {
  const safeValue = case_validation_escape_attr(value == null ? '' : String(value));
  if ((field.type || '').toLowerCase() === 'list' && field.values && field.values.length > 0) {
    const options = field.values.map(v => {
      const selected = String(v.value) === String(value) ? ' selected' : '';
      return `<option value="${case_validation_escape_attr(v.value)}"${selected}>${case_validation_escape_html(v.display || v.value)}</option>`;
    }).join('');
    return `<label for="case_validation_quick_edit_value">Value</label><select id="case_validation_quick_edit_value" class="form-control">${options}</select>`;
  }

  const type = (field.type || '').toLowerCase() === 'number' ? 'number' : ((field.type || '').toLowerCase() === 'date' ? 'date' : 'text');
  return `<label for="case_validation_quick_edit_value">Value</label><input id="case_validation_quick_edit_value" class="form-control" type="${type}" value="${safeValue}" />`;
}

function case_validation_quick_edit_modal_html() {
  return `
    <div id="case_validation_quick_edit_modal" class="modal fade" tabindex="-1" role="dialog" aria-labelledby="case_validation_quick_edit_title" aria-hidden="true">
      <div class="modal-dialog" role="document">
        <div class="modal-content">
          <div class="modal-header">
            <h5 id="case_validation_quick_edit_title" class="modal-title">Quick Edit</h5>
            <button type="button" class="close" data-dismiss="modal" aria-label="Close">
              <span aria-hidden="true">&times;</span>
            </button>
          </div>
          <div class="modal-body">
            <input type="hidden" id="case_validation_quick_edit_field_path" />
            <input type="hidden" id="case_validation_quick_edit_metadata_path" />
            <div id="case_validation_quick_edit_body"></div>
            <div id="case_validation_quick_edit_error" class="text-danger mt-2" style="display:none;"></div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-outline-secondary" data-dismiss="modal">Cancel</button>
            <button type="button" class="btn btn-primary" onclick="case_validation_save_quick_edit()">Save</button>
          </div>
        </div>
      </div>
    </div>`;
}

async function case_validation_save_quick_edit() {
  const fieldPath = $('#case_validation_quick_edit_field_path').val();
  const metadataPath = $('#case_validation_quick_edit_metadata_path').val();
  const field = case_validation_state.fields.find(f => f.field_path === fieldPath);
  const value = $('#case_validation_quick_edit_value').val();

  if (!field || g_data_is_checked_out !== true) {
    return;
  }

  try {
    const response = await fetch('/api/case-validation/field', {
      method: 'POST',
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json; charset=utf-8'
      },
      body: JSON.stringify({
        case_id: g_data._id,
        field_path: fieldPath,
        metadata_path: metadataPath,
        value: case_validation_cast_value_for_field(value, field),
        tab_id: typeof get_mmria_tab_id === 'function' ? get_mmria_tab_id() : g_data.checked_out_by_tab_id
      })
    });

    const result = await response.json();
    if (!response.ok || result.ok !== true) {
      $('#case_validation_quick_edit_error').text(result.error_description || result.message || 'Unable to save the field.').show();
      return;
    }

    g_data._rev = result.rev || g_data._rev;
    case_validation_set_path_value(g_data, fieldPath, case_validation_cast_value_for_field(value, field));
    case_validation_close_quick_edit_modal(function () {
      if (typeof set_local_case === 'function') {
        set_local_case(g_data, function () { g_render(); });
      } else {
        g_render();
      }
    });
  } catch (ex) {
    $('#case_validation_quick_edit_error').text(ex.message || ex).show();
  }
}

function case_validation_close_quick_edit_modal(onClosed) {
  const modal = $('#case_validation_quick_edit_modal');
  let done = false;
  const finish = function () {
    if (done) {
      return;
    }

    done = true;
    modal.off('hidden.bs.modal', finish);
    case_validation_cleanup_modal_backdrop();
    if (typeof onClosed === 'function') {
      onClosed();
    }
  };

  if (modal.length > 0 && modal.hasClass('show')) {
    modal.one('hidden.bs.modal', finish);
    modal.modal('hide');
    window.setTimeout(finish, 600);
  } else {
    finish();
  }
}

function case_validation_cleanup_modal_backdrop() {
  if ($('.modal.show').length > 0) {
    return;
  }

  $('.modal-backdrop').remove();
  $('body').removeClass('modal-open').css('padding-right', '');
}

function case_validation_cast_value_for_field(value, field) {
  const type = (field.type || '').toLowerCase();
  const dataType = (field.data_type || '').toLowerCase();
  if (value == null || value === '') {
    return null;
  }

  if (type === 'number' || (type === 'list' && (dataType === 'number' || dataType === 'double'))) {
    return Number(value);
  }

  if (type === 'boolean') {
    return value === true || value === 'true' || value === '1';
  }

  return value;
}

function case_validation_set_path_value(root, path, value) {
  const parts = path.split('/');
  let current = root;
  for (let i = 0; i < parts.length - 1; i++) {
    if (current[parts[i]] == null) {
      current[parts[i]] = {};
    }
    current = current[parts[i]];
  }
  current[parts[parts.length - 1]] = value;
}

function case_validation_is_scalar_type(type) {
  return ['string', 'textarea', 'number', 'list', 'date', 'datetime', 'time', 'boolean', 'jurisdiction', 'hidden'].indexOf(type) > -1;
}

function case_validation_is_multi(node) {
  return node && (node.cardinality === '*' || node.cardinality === '+');
}

function case_validation_build_subject(node, ancestry) {
  const parts = (ancestry || []).slice();
  if (node && node.prompt) {
    parts.push(node.prompt);
  }
  if (node && Array.isArray(node.tags)) {
    node.tags.forEach(t => parts.push(t));
  }
  return parts.filter(Boolean).join(' / ');
}

function case_validation_normalize_subject(value) {
  return String(value || '').toLowerCase().replace(/[^a-z0-9]+/g, ' ').replace(/\s+/g, ' ').trim();
}

function case_validation_escape_html(value) {
  return String(value == null ? '' : value)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function case_validation_escape_attr(value) {
  return case_validation_escape_html(value);
}

function case_validation_escape_js(value) {
  return String(value == null ? '' : value).replace(/\\/g, '\\\\').replace(/'/g, "\\'");
}
