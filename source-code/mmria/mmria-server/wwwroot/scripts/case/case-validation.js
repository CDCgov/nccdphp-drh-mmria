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
  p_result.push('<div class="container-fluid case-validation-view px-0">');
  p_result.push('<div class="row no-gutters align-items-center mb-3">');
  p_result.push('<div class="col">');
  p_result.push('<h1 class="h3 mb-1">Case Validation</h1>');
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
  const findingClass = row.is_finding ? ' list-group-item-warning' : '';
  const badge = row.is_finding ? case_validation_escape_html(row.category || 'warning') : (row.category === 'field' ? 'field' : 'OK');
  const value = row.value == null || row.value === '' ? '(blank)' : row.value;
  const expected = row.expected ? `<div><strong>Expected:</strong> ${case_validation_escape_html(row.expected)}</div>` : '';
  const review = row.review_status ? `<div class="small text-muted">${case_validation_escape_html(row.validation_level || '')}${row.validation_level && row.review_status ? ' | ' : ''}${case_validation_escape_html(row.review_status || '')}</div>` : '';
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
  p_result.push(`<div><strong>Value:</strong> ${case_validation_escape_html(value)}</div>`);
  p_result.push(expected);
  p_result.push(review);
  p_result.push('</div>');
  p_result.push('<div class="col-auto ml-3">');
  p_result.push(`<button type="button" class="btn btn-sm btn-outline-primary mr-2" onclick="case_validation_open_field('${case_validation_escape_js(row.form_path || '')}', '${case_validation_escape_js(row.field_path || '')}')">Open Field</button>`);
  p_result.push(`<button type="button" class="btn btn-sm btn-outline-secondary" title="${case_validation_escape_attr(quickEditTitle)}" ${quickEditDisabled ? 'disabled' : ''} onclick="case_validation_open_quick_edit('${case_validation_escape_js(row.field_path || '')}')">Quick Edit</button>`);
  p_result.push('</div>');
  p_result.push('</div>');
  p_result.push('</div>');
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
      review_status: rule.review_status,
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
        value: case_validation_value_to_text(valueCtx.value, field),
        expected: issue ? issue.expected : case_validation_rule_expected_text(rule),
        message: issue ? (issue.message || rule.message || `${rule.prompt} is outside expected values.`) : (rule.rationale || rule.message || rule.subject),
        validation_level: rule.validation_level,
        review_status: rule.review_status,
        is_finding: !!issue,
        can_quick_edit: field.can_quick_edit
      });
    });
  });
}

function case_validation_evaluate_connected_rules(data, rules, fieldMap, rows) {
  (rules.connected_field_rules || []).filter(r => r.enabled !== false).forEach(rule => {
    const field = fieldMap[rule.field_path];
    if (!field) {
      return;
    }

    const values = case_validation_get_values(data, rule.field_path);
    const relatedValues = case_validation_get_values(data, rule.related_field_path);
    values.forEach((valueCtx, index) => {
      const relatedValue = relatedValues.length === 1 ? relatedValues[0]?.value : relatedValues[Math.min(index, relatedValues.length - 1)]?.value;
      const issue = case_validation_connected_rule_issue(rule, valueCtx.value, relatedValue);
      rows.push({
        id: `${rule.id}:${index}`,
        rule_id: rule.id,
        category: 'connected-field',
        severity: issue ? (rule.severity || 'warning') : 'ok',
        form_path: rule.form_path,
        form_prompt: rule.form_prompt,
        field_path: rule.field_path,
        metadata_path: rule.metadata_path,
        prompt: rule.prompt,
        subject: rule.subject,
        value: case_validation_value_to_text(valueCtx.value, field),
        expected: issue ? issue.expected : case_validation_connected_expected_text(rule),
        message: issue ? (issue.message || rule.message) : (rule.rationale || rule.message),
        validation_level: rule.validation_level,
        review_status: rule.review_status,
        is_finding: !!issue,
        can_quick_edit: field.can_quick_edit
      });
    });
  });
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

function case_validation_connected_rule_issue(rule, value, related) {
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

  if (rule.rule_type === 'date_greater_than_or_equal' && leftDate && rightDate && leftDate.getTime() < rightDate.getTime()) {
    return { expected: `${rule.prompt} on or after ${rule.related_prompt}`, message: rule.message };
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
  if (rule.rule_type === 'date_greater_than_or_equal') {
    return `${rule.prompt} on or after ${rule.related_prompt}`;
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

    const parsed = new Date(Date.UTC(year, month - 1, day));
    return parsed.getUTCFullYear() === year && parsed.getUTCMonth() === month - 1 && parsed.getUTCDate() === day ? parsed : null;
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return null;
  }

  return new Date(Date.UTC(parsed.getFullYear(), parsed.getMonth(), parsed.getDate()));
}

function case_validation_get_values(root, path) {
  const result = [];
  case_validation_collect_values(root, path.split('/'), 0, result);
  return result.length > 0 ? result : [{ value: null }];
}

function case_validation_collect_values(current, parts, index, result) {
  if (current == null) {
    return;
  }

  if (Array.isArray(current)) {
    current.forEach((item, arrayIndex) => {
      case_validation_collect_values(item, parts, index, result, arrayIndex);
    });
    return;
  }

  if (index >= parts.length) {
    result.push({ value: current });
    return;
  }

  case_validation_collect_values(current[parts[index]], parts, index + 1, result);
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
    if (typeof set_local_case === 'function') {
      set_local_case(g_data, function () { g_render(); });
    } else {
      g_render();
    }

    $('#case_validation_quick_edit_modal').modal('hide');
  } catch (ex) {
    $('#case_validation_quick_edit_error').text(ex.message || ex).show();
  }
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
