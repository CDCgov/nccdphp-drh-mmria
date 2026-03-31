'use strict';

var g_metadata = null;
var g_user_name = null;
var g_is_data_analyst_mode = null;
var g_data_is_checked_out = false;
var g_data = null;
var g_source_db = null;
var g_jurisdiction_list = [];
var g_user_role_jurisdiction_list = [];
var g_jurisdiction_tree = [];
var g_metadata_path = [];
var g_validator_map = [];
var g_event_map = [];
var g_validation_description_map = [];
var g_selected_index = null;
var g_selected_delete_index = null;
var g_couchdb_url = null;
var g_localDB = null;
var g_remoteDB = null;
var g_metadata_summary = [];
var default_object = null;
var g_change_stack = [];
var g_default_ui_specification = null;
var g_use_position_information = true;
var g_look_up = {};
var g_release_version = null;
var g_autosave_interval = null;
var g_value_to_display_lookup = {};
var g_name_to_value_lookup = {};
var g_display_to_value_lookup = {};
var g_value_to_index_number_lookup = {};
var g_name_to_value_lookup = {};
var g_is_confirm_for_case_lock = false;
var g_target_case_status = null;
var g_previous_case_status = null;
var g_other_specify_lookup = {};
var g_record_id_list = new Set();
var g_form_access_list = new Map();
var role_set = new Set();
const g_charts = new Map();
const g_chart_instances = new Map();
const g_chart_data = new Map();
const g_duplicate_path_set = new Set();
var g_case_narrative_is_updated = false;
var g_case_narrative_is_updated_date = null;
var g_case_narrative_original_value = null;
var g_case_navigation_save_in_progress = false;
var g_case_hash_restore_in_progress = false;

var g_is_committee_member_view = false;

// Track which case IDs are pending cleanup to prevent re-adding in both modes
const g_case_cleanup_pending = new Set();

var g_pinned_case_set = null;

var g_pinned_case_count = 0;
var g_is_jurisdiction_admin = false;

let save_start_time, save_end_time;

const g_is_version_based = false;

const g_cvs_api_request_data = new Map();

const g_dependent_parent_to_child = new Map();
const g_dependent_child_to_parent = new Map();
const g_dependent_child_metadata = new Map();

function clear_case_chart_state()
{
  if(typeof clear_chart_state === 'function')
  {
    clear_chart_state();
    return;
  }

  if(typeof chart_function_params_map !== 'undefined' && chart_function_params_map != null)
  {
    chart_function_params_map.clear();
  }

  g_charts.clear();
  g_chart_instances.clear();
  g_chart_data.clear();
}

if (typeof window.check_edit_inactivity !== 'function')
{
  window.check_edit_inactivity = function ()
  {
    return false;
  };
}

const  disable_on_selected_item_list = new Map()/*
disable_on_selected_item_list.set("tracking/admin_info/steve_transfer", new Map());
disable_on_selected_item_list.get("tracking/admin_info/steve_transfer").set(1, new Set());
//disable_on_selected_item_list.get("tracking/admin_info/steve_transfer").get(1).set("");


const event = new Event("build");

// Listen for the event.
elem.addEventListener(
  "build",
  (e) => {
    /* … * /
},
false,
);

// Dispatch the event.
elem.dispatchEvent(event);


const event = new Event("steve_transfer_changed");


// drop down = elem

*/


function calc_index_is_disabled(p_path, p_value)
{
    const dictionary = disable_on_selected_item_list.get("tracking/admin_info/steve_transfer");
    var result = false;
    var compare = false;

    for(const [k, v] of dictionary)
    {
        if(p_path.indexOf(p_path) == 0)
        {
            compare = true;
            break;
        }

    }

    if(!compare)
        return result;


    if(p_value == 1)
    {
       result = true;
    }
    
    return result;
}

function perform_index_enable_disable
(
    p_value, 
    p_dictionary_path, 
    p_form_index, 
    p_grid_index
)
{
    if(p_value == 1)
    {
       $mmria.set_disable(p_dictionary_path, p_form_index, p_grid_index) 
    }
    else
    {
        $mmria.set_enable(p_dictionary_path, p_form_index, p_grid_index)
    }
}

var is_faulted = false;

const peg_parser = peg.generate(`
start = blank_space html_start_tag  (blank_space ( balanced_tag / single_tag ) blank_space)* blank_space html_end_tag blank_space
html_start_tag = '<html>'
html_end_tag = '</html>'


single_tag = horizontal_line_tag / soft_return_tag
horizontal_line_tag = '<hr>'
soft_return_tag = '<br>'

balanced_tag = paragraph_tag / table_tag / unordered_list_tag / ordered_list_tag

in_line_able_tag = span_tag / bold_tag / underline_tag / italic_tag


span_in_line_able_tag =  bold_tag / underline_tag / italic_tag

paragraph_tag = paragraph_start_tag (inner_text / in_line_able_tag)+ paragraph_end_tag
paragraph_start_tag = '<p>' / '<p ' + style_attribute + '>'
paragraph_end_tag = '</p>'


span_tag = span_start_tag (inner_text / span_in_line_able_tag)+ span_end_tag
span_start_tag = '<span>' / '<span ' + style_attribute + '>'
span_end_tag = '</span>'

bold_tag = bold_start_tag inner_text bold_end_tag
bold_start_tag = '<b>'
bold_end_tag = '</b>'

underline_tag = underline_start_tag inner_text underline_end_tag
underline_start_tag = '<u>'
underline_end_tag = '</u>'

italic_tag = italic_start_tag inner_text italic_end_tag
italic_start_tag = '<i>'
italic_end_tag = '</i>'


unordered_list_tag = unordered_list_start_tag (blank_space list_item_tag)* blank_space unordered_list_end_tag
unordered_list_start_tag = '<ul>'
unordered_list_end_tag = '</ul>'


ordered_list_tag = ordered_list_start_tag  (blank_space list_item_tag)* blank_space ordered_list_end_tag
ordered_list_start_tag = '<ol>'
ordered_list_end_tag = '</ol>'

list_item_tag = list_item_start_tag (inner_text / in_line_able_tag)* list_item_end_tag
list_item_start_tag = '<li>'
list_item_end_tag = '</li>'

table_tag = table_start_tag (blank_space table_row_tag)* blank_space table_end_tag
table_start_tag = '<table>'  / '<table ' + table_attribue_list + '>' 
table_end_tag = '</table>'


table_row_tag = table_row_start_tag (blank_space table_header_tag / blank_space table_detail_tag)* blank_space table_row_end_tag
table_row_start_tag = '<tr>' / '<tr ' + table_attribue_list + '>' 
table_row_end_tag = '</tr>'

table_header_tag = table_header_start_tag (blank_space inner_text / blank_space in_line_able_tag)* blank_space table_header_end_tag
table_header_start_tag = '<th>' / '<th ' + table_attribue_list + '>' 
table_header_end_tag = '</th>'

table_detail_tag = table_detail_start_tag (blank_space inner_text / blank_space in_line_able_tag)* blank_space table_detail_end_tag
table_detail_start_tag = '<td>' / '<td ' + table_attribue_list + '>' 
table_detail_end_tag = '</td>'


style_attribute =  'style="' + name_value_list + '"'

name_value_list = name_value_pair / (name_value_pair + ';')+
name_value_pair = color_name + ':' + color_value
/ color_name + ': ' + color_value
/ backgroud_color_name + ':' + color_value 
/ backgroud_color_name + ': ' + color_value
/ text_align_name + ':' + align_attribute_value
/ text_align_name + ': ' + align_attribute_value
/ vertical_align_name + ':' + vertical_align_value
/ vertical_align_name + ': ' + vertical_align_value
/ font_family_name + ':' + font_family_value
/ font_family_name + ': ' + font_family_value
/ font_size_name + ':' + font_size_value
/ font_size_name + ': ' + font_size_value
/ width_attribute_name + ':' + width_attribute_value
/ width_attribute_name + ': ' + width_attribute_value
/ height_attribute_name + ':' + height_attribute_value
/ height_attribute_name + ': ' + height_attribute_value


font_family_name = 'font-family'
font_family_value = 'Times New Roman' /  'Calibri' / 'Ariel' / 'Helvetica' / 'Times' / 'serif' / 'sans-serif' / 'monospace'

/* font_family_value = [ a-zA-Z0-9,]* */

font_size_name = 'font-size'
font_size_value = '9pt' / '11pt' / '12pt' / '14pt' / '16pt' / '18pt'

color_name = 'color'
backgroud_color_name = 'background-color'

color_value = color_hex_value / color_name_value
color_hex_value = '#' + [a-fA-F0-9][a-fA-F0-9][a-fA-F0-9][a-fA-F0-9][a-fA-F0-9][a-fA-F0-9]
color_name_value = 'black' / 'red' / 'yellow' / 'green' / 'purple' / 'orange'


vertical_align_name = 'vertical-align'
vertical_align_value = 'baseline' /'text-top' / 'text-bottom' / 'super' / 'sub'
text_align_name = 'text-align'


table_attribue_list = table_attribue / (table_attribue + one_or_more_blank_space)+ 

table_attribue = valign_attribute_name + '=' + valign_attribute_value
/ align_attribute_name + '=' + align_attribute_value
/ width_attribute_name + '=' + width_attribute_value
/ height_attribute_name + '=' + height_attribute_value
/ col_span_attribute_name + '=' + col_span_attribute_value
/ row_span_attribute_name + '=' + row_span_attribute_value
/ border_attribute_name + '=' + border_attribute_value
/ style_attribute

valign_attribute_name = 'valign'
valign_attribute_value = 'top' / 'middle' / 'bottom' / 'baseline'

align_attribute_name = 'align'
align_attribute_value = 'left' / 'center' / 'right' / 'justify' / 'char'

width_attribute_name = 'width' 
width_attribute_value = one_or_more_digits + 'px'

height_attribute_name = 'height'
height_attribute_value = one_or_more_digits + 'px'

col_span_attribute_name = 'colspan'
col_span_attribute_value = one_or_more_digits

row_span_attribute_name = 'rowspan'
row_span_attribute_value = one_or_more_digits

border_attribute_name = 'border'
border_attribute_value = one_or_more_digits


//one_or_more_spaces = [ ]+
one_or_more_digits = [0-9]+

inner_text = basic_text / entity_text

entity_text = '&amp;' / '&lt;'
basic_text = [\\] a-zA-Z0-9\\.\\n\\[\\+\\*\\(\\)"'!@#$%^,>:;\\?=_-]+
blank_space "Blank space" = [ \\t\\n\\r]*
one_or_more_blank_space "One or more blank space" = [ \\t\\n\\r]+

`);


const save_queue = {
  is_active: false,
  active_item: null,
  item_list: [],
  process_timeout_id: null,
  process_timeout_due_ms: 0,
};

const CASE_SAVE_REQUEST_TIMEOUT_MS = 60000;

function mmria_safe_clone(p_obj)
{
  if(p_obj == null) return p_obj;

  try
  {
    if(typeof window !== 'undefined' && typeof window.structuredClone === 'function')
    {
      return window.structuredClone(p_obj);
    }
  }
  catch(_ex)
  {
    // fall back
  }

  return JSON.parse(JSON.stringify(p_obj));
}

function mmria_get_lock_release_tab_id(p_case)
{
  let current_tab_id = null;

  try
  {
    if (typeof get_mmria_tab_id === 'function')
    {
      current_tab_id = get_mmria_tab_id();
    }
  }
  catch (_ex)
  {
    current_tab_id = null;
  }

  if
  (
    (current_tab_id == null || current_tab_id === '') &&
    p_case != null &&
    p_case.checked_out_by_tab_id != null &&
    p_case.checked_out_by_tab_id !== ''
  )
  {
    current_tab_id = p_case.checked_out_by_tab_id;
  }

  return current_tab_id;
}

function mmria_get_save_retry_delay_ms(p_attempt_count)
{
  // attempt_count starts at 1
  if(p_attempt_count <= 1) return 2000;
  if(p_attempt_count == 2) return 5000;
  if(p_attempt_count == 3) return 10000;
  if(p_attempt_count == 4) return 30000;
  return 60000;
}

function mmria_get_narrative_save_snapshot(p_data)
{
  const is_relevant =
  (
    !g_is_pmss_enhanced &&
    p_data != null &&
    g_data != null &&
    p_data._id === g_data._id &&
    g_case_narrative_is_updated === true
  );

  return {
    is_updated: is_relevant,
    original_value: is_relevant ? g_case_narrative_original_value : null,
    new_value:
      (is_relevant && p_data.case_narrative)
        ? p_data.case_narrative.case_opening_overview
        : null,
    updated_date_iso:
      (is_relevant && g_case_narrative_is_updated_date != null)
        ? new Date(g_case_narrative_is_updated_date).toISOString()
        : null
  };
}

function mmria_get_save_queue_item_policy(p_options)
{
  const options = p_options || {};
  const is_awaited = options.isAwaited === true;

  return {
    intent: options.intent || (is_awaited ? 'awaited_save' : 'background_save'),
    isAwaited: is_awaited,
    retryMode: options.retryMode || (is_awaited ? 'fail-fast' : 'background-retry'),
    timeout_ms: Math.max(1, Number(options.timeout_ms) || CASE_SAVE_REQUEST_TIMEOUT_MS)
  };
}

function get_new_save_queue_item
(
  p_data,
  p_call_back,
  p_note,
  p_options
)
{
  const policy = mmria_get_save_queue_item_policy(p_options);
  const cloned_data = mmria_safe_clone(p_data);

  if
  (
    cloned_data != null &&
    (cloned_data.host_state == null || cloned_data.host_state === '')
  )
  {
    cloned_data.host_state = window.location.host.split('-')[0];
  }

  return {
    id: $mmria.get_new_guid(),
    date_created: new Date(),
    date_completed: null,
    data: cloned_data, 
    change_stack_items: mmria_safe_clone(Array.isArray(g_change_stack) ? g_change_stack : []),
    narrative_snapshot: mmria_get_narrative_save_snapshot(cloned_data),
    continuation_callback: p_call_back,
    note: p_note,
    is_data_analyst_mode: g_is_data_analyst_mode,
    user_name_snapshot: g_user_name,
    metadata_version_snapshot: g_release_version,
    intent: policy.intent,
    isAwaited: policy.isAwaited,
    retryMode: policy.retryMode,
    timeout_ms: policy.timeout_ms,
    post_rev: null,
    attempt_count: 0,
    next_attempt_ms: 0,
    last_error_dialog_shown_ms: 0,
    completion: null
  };
}

function mmria_clear_scheduled_save_queue_processing()
{
  if(save_queue.process_timeout_id != null)
  {
    window.clearTimeout(save_queue.process_timeout_id);
    save_queue.process_timeout_id = null;
    save_queue.process_timeout_due_ms = 0;
  }
}

function mmria_schedule_save_queue_processing(p_delay_ms)
{
  const delay_ms = Math.max(0, Number(p_delay_ms) || 0);
  const due_ms = Date.now() + delay_ms;

  if
  (
    save_queue.process_timeout_id != null &&
    save_queue.process_timeout_due_ms <= due_ms
  )
  {
    return;
  }

  mmria_clear_scheduled_save_queue_processing();

  save_queue.process_timeout_due_ms = due_ms;
  save_queue.process_timeout_id = window.setTimeout(async function ()
  {
    save_queue.process_timeout_id = null;
    save_queue.process_timeout_due_ms = 0;
    await process_save_case();
  }, delay_ms);
}

function mmria_dequeue_save_queue_item(p_item)
{
  if(save_queue.item_list.length > 0 && save_queue.item_list[0] === p_item)
  {
    save_queue.item_list.shift();
    return;
  }

  const idx = save_queue.item_list.findIndex(x => x && p_item && x.id === p_item.id);
  if(idx >= 0)
  {
    save_queue.item_list.splice(idx, 1);
  }
}

function mmria_is_retryable_transport_error(p_err)
{
  if(p_err == null) return false;

  if(p_err.name === 'AbortError') return true;
  if(p_err.status === 0) return true;

  const message =
    ((typeof p_err.message === 'string' && p_err.message) || '') +
    ' ' +
    ((typeof p_err.responseText === 'string' && p_err.responseText) || '');
  const normalized = message.toLowerCase();

  return (
    p_err instanceof TypeError ||
    normalized.indexOf('network') > -1 ||
    normalized.indexOf('offline') > -1 ||
    normalized.indexOf('timed out') > -1 ||
    normalized.indexOf('timeout') > -1 ||
    normalized.indexOf('failed to fetch') > -1
  );
}

function mmria_safe_to_json(p_value)
{
  try
  {
    return JSON.stringify(p_value);
  }
  catch(_ex)
  {
    return null;
  }
}

function mmria_is_change_stack_prefix_match(p_live_items, p_snapshot_items)
{
  if(!Array.isArray(p_snapshot_items) || p_snapshot_items.length === 0)
  {
    return true;
  }

  if(!Array.isArray(p_live_items) || p_live_items.length < p_snapshot_items.length)
  {
    return false;
  }

  for(let i = 0; i < p_snapshot_items.length; i++)
  {
    if(mmria_safe_to_json(p_live_items[i]) !== mmria_safe_to_json(p_snapshot_items[i]))
    {
      return false;
    }
  }

  return true;
}

function mmria_reconcile_live_narrative_state_after_success(p_item)
{
  if
  (
    g_is_pmss_enhanced ||
    !p_item ||
    !p_item.narrative_snapshot ||
    p_item.narrative_snapshot.is_updated !== true
  )
  {
    return;
  }

  g_case_narrative_original_value = p_item.narrative_snapshot.new_value;

  if(g_case_narrative_is_updated !== true)
  {
    return;
  }

  const current_dirty_date_iso =
    g_case_narrative_is_updated_date != null
      ? new Date(g_case_narrative_is_updated_date).toISOString()
      : null;
  const live_value =
    g_data && g_data.case_narrative
      ? g_data.case_narrative.case_opening_overview
      : null;

  const matched_saved_snapshot =
    current_dirty_date_iso === p_item.narrative_snapshot.updated_date_iso &&
    mmria_safe_to_json(live_value) === mmria_safe_to_json(p_item.narrative_snapshot.new_value);

  if(matched_saved_snapshot)
  {
    g_case_narrative_is_updated = false;
    g_case_narrative_is_updated_date = null;
    return;
  }

  console.warn(
    'Narrative save completed, but newer live narrative edits remain dirty. Keeping narrative dirty state intact.'
  );
}

function mmria_reconcile_live_save_state_after_success(p_item)
{
  if(!p_item || !p_item.data || !g_data || g_data._id !== p_item.data._id)
  {
    return;
  }

  const snapshot_items = Array.isArray(p_item.change_stack_items)
    ? p_item.change_stack_items
    : [];

  if(snapshot_items.length > 0)
  {
    if(mmria_is_change_stack_prefix_match(g_change_stack, snapshot_items))
    {
      g_change_stack.splice(0, snapshot_items.length);
    }
    else
    {
      console.warn(
        'Save completed, but live change stack no longer matched the queued snapshot. Leaving unmatched edits intact.'
      );
    }
  }

  mmria_reconcile_live_narrative_state_after_success(p_item);
}

function mmria_rebase_queued_items_to_new_rev(p_case_id, p_new_rev)
{
  if(!p_case_id || !p_new_rev) return;

  for(let i = 0; i < save_queue.item_list.length; i++)
  {
    const item = save_queue.item_list[i];
    if(item && item.data && item.data._id === p_case_id)
    {
      item.data._rev = p_new_rev;
      item.post_rev = p_new_rev;
    }
  }
}

function mmria_build_narrative_change_stack_item(p_item)
{
  if
  (
    !p_item ||
    !p_item.narrative_snapshot ||
    p_item.narrative_snapshot.is_updated !== true
  )
  {
    return null;
  }

  return {
    _id: p_item.data._id,
    _rev: p_item.data._rev,
    object_path: "g_data.case_narrative.case_opening_overview",
    metadata_path: "/case_narrative/case_opening_overview",
    old_value: p_item.narrative_snapshot.original_value,
    new_value: p_item.narrative_snapshot.new_value,
    dictionary_path: "/case_narrative/case_opening_overview",
    metadata_type: "textarea",
    prompt: 'Case Narrative',
    date_created:
      p_item.narrative_snapshot.updated_date_iso || new Date().toISOString(),
    user_name: p_item.user_name_snapshot
  };
}

function mmria_create_save_case_request_from_queue_item(p_item)
{
  const change_stack_items = mmria_safe_clone(
    Array.isArray(p_item.change_stack_items) ? p_item.change_stack_items : []
  );
  const narrative_change_item = mmria_build_narrative_change_stack_item(p_item);

  if(narrative_change_item != null)
  {
    change_stack_items.push(narrative_change_item);
  }

  return {
    Change_Stack: {
      _id: $mmria.get_new_guid(),
      case_id: p_item.data._id,
      case_rev: p_item.data._rev,
      date_created: new Date().toISOString(),
      user_name: p_item.user_name_snapshot,
      items: change_stack_items,
      metadata_version: p_item.metadata_version_snapshot,
      note: (p_item.note != null) ? p_item.note : ""
    },
    Case_Data: p_item.data
  };
}

async function mmria_fetch_case_save_response(p_save_case_request, p_timeout_ms)
{
  const controller = (typeof AbortController !== 'undefined')
    ? new AbortController()
    : null;
  const timeout_ms = Math.max(1, Number(p_timeout_ms) || CASE_SAVE_REQUEST_TIMEOUT_MS);
  let timeout_id = null;

  try
  {
    if(controller != null)
    {
      timeout_id = window.setTimeout(function ()
      {
        controller.abort();
      }, timeout_ms);
    }

    return await fetch(location.protocol + '//' + location.host + '/api/case', {
      method: "post",
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json; charset=utf-8',
        'dataType': 'json',
      },
      body: JSON.stringify(p_save_case_request),
      signal: controller != null ? controller.signal : undefined
    });
  }
  catch(err)
  {
    if(controller != null && err && err.name === 'AbortError')
    {
      throw {
        name: 'AbortError',
        status: 0,
        message: `Save request timed out after ${timeout_ms}ms`,
        responseText: `Save request timed out after ${timeout_ms}ms`
      };
    }

    throw err;
  }
  finally
  {
    if(timeout_id != null)
    {
      window.clearTimeout(timeout_id);
    }
  }
}

function mmria_has_awaited_save_for_case(p_case_id)
{
  if(!p_case_id) return false;

  const active_item = save_queue.active_item;
  if
  (
    active_item &&
    active_item.isAwaited === true &&
    active_item.data &&
    active_item.data._id === p_case_id
  )
  {
    return true;
  }

  return save_queue.item_list.some(item =>
    item &&
    item.isAwaited === true &&
    item.data &&
    item.data._id === p_case_id
  );
}

function mmria_prune_nonblocking_save_queue_items_for_case(p_case_id)
{
  if(!p_case_id) return;
  if(!save_queue || !Array.isArray(save_queue.item_list)) return;

  const active_item_id =
    save_queue.active_item && save_queue.active_item.id
      ? save_queue.active_item.id
      : null;

  // Drop older fire-and-forget saves for the same case.
  // This prevents a backlog of redundant posts (e.g., autosave or console load tests)
  // from blocking user/navigation saves that use callbacks/completions.
  for(let i = save_queue.item_list.length - 1; i >= 0; i--)
  {
    const item = save_queue.item_list[i];
    if(!item || !item.data) continue;
    if(item.data._id !== p_case_id) continue;
    if(active_item_id != null && item.id === active_item_id) continue;

    if(item.isAwaited !== true)
    {
      save_queue.item_list.splice(i, 1);
    }
  }
}

function mmria_enqueue_save_queue_item(p_queue_item)
{
  if(!p_queue_item) return;

  const case_id = p_queue_item.data && p_queue_item.data._id;
  mmria_prune_nonblocking_save_queue_items_for_case(case_id);

  let protected_prefix_length = 0;
  if(save_queue.active_item != null)
  {
    const active_index = save_queue.item_list.findIndex(item =>
      item &&
      item.id === save_queue.active_item.id
    );

    if(active_index >= 0)
    {
      protected_prefix_length = active_index + 1;
    }
  }

  if(p_queue_item.isAwaited === true)
  {
    // Insert ahead of background saves, but do NOT reorder awaited saves
    // relative to each other.
    let insert_index = protected_prefix_length;
    for(; insert_index < save_queue.item_list.length; insert_index++)
    {
      const it = save_queue.item_list[insert_index];
      if(!it || it.isAwaited !== true) break;
    }
    save_queue.item_list.splice(insert_index, 0, p_queue_item);
  }
  else
  {
    save_queue.item_list.push(p_queue_item);
  }
}

async function g_set_data_object_from_path
(
  p_object_path,
  p_metadata_path,
  p_dictionary_path,
  value,
  p_form_index,
  p_grid_index,
  p_date_object,
  p_time_object
) 
{

  //save_start_time = performance.now();
  var is_search_result = false;
  var search_text = null;

  if 
  (
    g_ui.url_state.selected_id &&
    g_ui.url_state.selected_id == 'field_search'
  ) 
  {
    is_search_result = true;
    search_text = g_ui.url_state.path_array[2].replace(/%20/g, ' ');
  }

  var current_value = $mmria.get_object_value_by_full_path(g_data, p_object_path);

  if (g_validator_map[p_metadata_path]) 
  {
    if (g_validator_map[p_metadata_path](value)) 
    {
        var metadata = $mmria.get_object_value_by_full_path(g_metadata, p_metadata_path);

        if (metadata.type.toLowerCase() == 'boolean') 
        {
            $mmria.set_object_value_by_full_path(g_data, p_object_path, value);
        } 
        else 
        {
            $mmria.set_object_value_by_full_path(g_data, p_object_path, value.trim());
        }
      g_data.date_last_updated = new Date();

      //g_data.last_updated_by = g_uid;

      g_change_stack.push({
        _id: g_data._id,
        _rev: g_data._rev,
        object_path: p_object_path,
        metadata_path: p_metadata_path,
        old_value: Array.isArray (current_value)?JSON.stringify(current_value) : current_value,
        new_value: value,
        dictionary_path: p_dictionary_path,
        metadata_type: metadata.type,
        prompt: metadata.prompt,
        date_created: new Date().toISOString(),
        user_name: g_user_name,
        form_index: p_form_index,
        grid_index: p_grid_index
      });

      window.setTimeout(async ()=> { await autorecalculate(p_dictionary_path) });

        set_local_case
        (
            g_data, 
            function () 
            {
                var post_html_call_back = [];

                document.getElementById
                (
                    convert_object_path_to_jquery_id(p_object_path)
                ).innerHTML = page_render(
                metadata,
                $mmria.get_object_value_by_full_path(g_data, p_object_path),
                g_ui,
                p_metadata_path,
                p_object_path,
                '',
                false,
                post_html_call_back
                ).join('');
                if (post_html_call_back.length > 0) 
                {
                    eval(post_html_call_back.join('\n'));
                }

                apply_validation();
            }
        );
    } 
    else 
    {
        // do nothing for now
      //g_ui.broken_rules[p_object_path] = true;
      //console.log("didn't pass validation");
    }
  } 
  else 
  {
    var metadata = $mmria.get_object_value_by_full_path(g_metadata, p_metadata_path);
    var current_value = $mmria.get_object_value_by_full_path(g_data, p_object_path);
    var valid_date_or_datetime = true;
    var entered_date_or_datetime_value = value;

    if(metadata === null)
      return;

    if(metadata.type.toLowerCase() == 'html_area')
    {
        try
        {
            peg_parser.parse(value);
            document.getElementById("ii-validation").value  = "passed html validation";
            $mmria.set_object_value_by_full_path(g_data, p_object_path, value);
        }
        catch(e)
        {
            const el = document.getElementById(convert_object_path_to_jquery_id(p_object_path) + '_control');
            let from = e.location.start.offset-5;
            
            if(from < 0)
            {
                from = 0;
            }

            let end = 10;
            if(from + end > el.value.length)
            {
                end = el.value.length - from;
            }

            const el_value = el.value.substr(from, end).replace(/</g, "&lt;");

            document.getElementById("ii-validation").innerHTML = `
            <p>Line: ${e.location.start.line} Column: ${e.location.start.column} expected: ${e.expected[0].type} ${e.expected[0].text}</p>
            <p style="color:#990000"> -> ${el_value}</p>
            <p>${e.message}</p>
            
            `;

            el.setSelectionRange(from, from + end);
            el.focus();

            return;
        }

        
    }
    else if 
    (
      metadata.type.toLowerCase() == 'list' &&
      metadata['is_multiselect'] &&
      metadata.is_multiselect == true
    ) 
    {
      let item = $mmria.get_object_value_by_full_path(g_data, p_object_path);
      

      if
      (
        metadata.data_type=='number' && 
        !isNaN(parseInt(value))
      )
      {
        const number_value = parseInt(value);

        item = list_create_number_set(item);
        
        if 
        (

           item.has(number_value)
        ) 
        {
            item.delete(number_value);
        } 
        else 
        {
            item.add(number_value);
        }

      }
      else
      {
        item = new Set(item);
        if (item.has(value)) 
        {
            item.delete(value);
        } 
        else 
        {
            item.add(value);
        }
      }

      const new_list = Array.from(item);
      $mmria.set_object_value_by_full_path(g_data, p_object_path, new_list);
      
    } 
    else if (metadata.type.toLowerCase() == 'boolean') 
    {
      $mmria.set_object_value_by_full_path(g_data, p_object_path, value);
    }
    else if (metadata.type.toLowerCase() == 'date') 
    {
      if (!is_valid_date(value)) 
      {
        valid_date_or_datetime = false;
        $mmria.set_object_value_by_full_path(g_data, p_object_path, "");
      }
      else
      {
          if(value!= null && value!="")
          {
            let save_datetime = new Date(value);
            $mmria.set_object_value_by_full_path(g_data, p_object_path,
                $mmria.escape_string_value(convert_date_to_storage_format(save_datetime))
            );
          }
          else
          {
            $mmria.set_object_value_by_full_path(g_data, p_object_path, "");
          }

      }
    } 
    else if (metadata.type.toLowerCase() == 'datetime') 
    {
      if (!is_valid_datetime(value)) 
      {
        valid_date_or_datetime = false;
        $mmria.set_object_value_by_full_path(g_data, p_object_path, "");
      }
      else
      {

        if(value!= null && value!="")
        {
            let save_datetime = new Date(value);
            $mmria.set_object_value_by_full_path(g_data, p_object_path,
                $mmria.escape_string_value(save_datetime.toISOString())
            );
        }
        else
        {
            $mmria.set_object_value_by_full_path(g_data, p_object_path, "");
        }
      }
    }
    else if(typeof value == "number")
    {
        $mmria.set_object_value_by_full_path(g_data, p_object_path, value);
    } 
    else 
    {
      try
      {
        const normalizedValue = value.trim().replace(/\\/g, '/');
        $mmria.set_object_value_by_full_path(g_data, p_object_path, normalizedValue);
      }
      catch(e)
      {
          const err = {
              status: 500,
              responseText : `unable to save field: ${p_dictionary_path}\n${e}`
          };
          $mmria.field_save_error_dialog_show(err, `unable to save field: ${p_dictionary_path} `);
      }

    }

    g_change_stack.push({
        _id: g_data._id,
        _rev: g_data._rev,
      object_path: p_object_path,
      metadata_path: p_metadata_path,
      old_value: Array.isArray (current_value)?JSON.stringify(current_value) : current_value,
      new_value: value,
      dictionary_path: p_dictionary_path,
      metadata_type: metadata.type,
      prompt: metadata.prompt,
      date_created: new Date().toISOString(),
      user_name: g_user_name,
      form_index: p_form_index,
      grid_index: p_grid_index
    });

    g_data.date_last_updated = new Date();
    //g_data.last_updated_by = g_uid;

    if(!g_is_pmss_enhanced)
    {
        window.setTimeout(async ()=> { await autorecalculate(p_dictionary_path, p_form_index, p_grid_index) });
    }
    

if
(
    metadata.type.toLowerCase() == 'date' &&
    valid_date_or_datetime
)
{
    set_local_case
    (
        g_data,
        function () 
        {
            gui_remove_broken_rule_click(convert_object_path_to_jquery_id(p_object_path));
        }
    );
}
else if
(
    metadata.type.toLowerCase() == 'datetime' &&
    valid_date_or_datetime
)
{
    set_local_case
    (
        g_data,
        function () 
        {

            const new_value = $mmria.get_object_value_by_full_path(g_data, p_object_path);

            let date_part_display_value = "";
            let time_part_display_value = '00:00:00';
            if(new_value != null && new_value != "")
            {
                /*
                 do nothing
                 
                */
            }
            else
            {
                document.getElementById(convert_object_path_to_jquery_id(p_object_path) + '-time').value = time_part_display_value;
            }

            gui_remove_broken_rule_click(convert_object_path_to_jquery_id(p_object_path));
        }
    );
}
else
    set_local_case
    (
        g_data, 
        function () 
        {
            var post_html_call_back = [];

            let ctx = {
                form_index: p_form_index,
                grid_index: p_grid_index,
                is_valid_date_or_datetime: valid_date_or_datetime,
                entered_date_or_datetime_value: entered_date_or_datetime_value,
            };

            if (is_search_result) 
            {
                let new_context = get_seach_text_context
                (
                [],
                post_html_call_back,
                metadata,
                $mmria.get_object_value_by_full_path(g_data, p_object_path),
                p_dictionary_path,
                p_metadata_path,
                p_object_path,
                search_text,
                false,
                ctx.form_index,
                ctx.grid_index,
                valid_date_or_datetime,
                entered_date_or_datetime_value
                );

                render_search_text(new_context);

                var new_html = new_context.result.join('');
                let result = $('#' + convert_object_path_to_jquery_id(p_object_path));

                if (result.length) 
                {
                result[0].outerHTML = new_html;
                } 
                else 
                {
                result.replaceWith(new_html);
                }
                //$("#" + convert_object_path_to_jquery_id(p_object_path))[0].outerHTML = new_html;
            }
            else if (metadata.type.toLowerCase() == 'textarea') 
            {
                var new_html = page_render(
                metadata,
                $mmria.get_object_value_by_full_path(g_data, p_object_path),
                g_ui,
                p_metadata_path,
                p_object_path,
                p_dictionary_path,
                false,
                post_html_call_back,
                null,
                ctx
                ).join('');

                $(
                '#' + convert_object_path_to_jquery_id(p_object_path)
                )[0].outerHTML = new_html;
            } 
            else 
            {
                var new_html = page_render(
                metadata,
                $mmria.get_object_value_by_full_path(g_data, p_object_path),
                g_ui,
                p_metadata_path,
                p_object_path,
                p_dictionary_path,
                false,
                post_html_call_back,
                null,
                ctx
                ).join('');

                $('#' + convert_object_path_to_jquery_id(p_object_path)).replaceWith(
                new_html
                );
                //$("#" + convert_object_path_to_jquery_id(p_object_path))[0].outerHTML = new_html;
            }

            switch (metadata.type.toLowerCase()) 
            {
                case 'time':
                $(
                    '#' + convert_object_path_to_jquery_id(p_object_path) + ' input'
                ).datetimepicker({
                    format: 'HH:mm:ss',
                    defaultDate: '',
                    keepInvalid: true,
                    useCurrent: false,
                    icons: {
                    time: 'x24 fill-p cdc-icon-clock_01',
                    date: 'x24 fill-p cdc-icon-calendar_01',
                    up: 'x24 fill-p cdc-icon-chevron-circle-up',
                    down: 'x24 fill-p cdc-icon-chevron-circle-down',
                    previous: 'x24 fill-p fill-p cdc-icon-chevron-circle-left-light',
                    next: 'x24 fill-p cdc-icon-chevron-circle-right-light',
                    },
                });
                break;

                case 'date':
                  $(`#${convert_object_path_to_jquery_id(p_object_path)} input`).datetimepicker({
                    format: 'MM/DD/YYYY',
                    keepInvalid: true,
                    useCurrent: false,
                    useStrict: true,
                    icons: {
                      time: "x24 cdc-icon-clock_01",
                      date: "x24 cdc-icon-calendar_01",
                      up: "x24 cdc-icon-chevron-double-right",
                      down: "x24 cdc-icon-chevron-double-right",
                      previous: 'x16 cdc-icon-chevron-double-right',
                      next: 'x16 cdc-icon-chevron-double-right'
                    }
                  });
                  
                  // flatpickr("#" + convert_object_path_to_jquery_id(p_object_path) + " input.date", {
                  //     utc: true,
                  //     enableTime: false,
                  //     defaultDate: value,
                  //     onChange: function(selectedDates, p_value, instance) {
                  //         g_set_data_object_from_path(p_object_path, p_metadata_path, p_dictionary_path, p_value);
                  //     }
                  // });

                  break;

                case 'datetime':
                  $(`#${convert_object_path_to_jquery_id(p_object_path)}-date`).datetimepicker({
                    format: 'MM/DD/YYYY',
                    keepInvalid: true,
                    useCurrent: false,
                    useStrict: true,
                    icons: {
                      up: "x16 cdc-icon-chevron-circle-up-light",
                      down: "x16 cdc-icon-chevron-circle-down-light",
                      previous: 'x16 cdc-icon-chevron-double-right',
                      next: 'x16 cdc-icon-chevron-double-right'
                    }
                  });

                  $(`#${convert_object_path_to_jquery_id(p_object_path)}-time`).datetimepicker({
                    format: 'HH:mm:ss',
                    keepInvalid: true,
                    useCurrent: false,
                    icons: {
                      up: "x16 cdc-icon-chevron-circle-up-light",
                      down: "x16 cdc-icon-chevron-circle-down-light",
                      previous: 'x16 cdc-icon-chevron-double-right',
                      next: 'x16 cdc-icon-chevron-double-right'
                    }
                  });


                 if (!isNullOrUndefined(p_date_object)) 
                 {
                  post_html_call_back.push(
                    `$('#${convert_object_path_to_jquery_id(
                        p_object_path
                    )}-time').focus();`
                    );
                 }
                 
                break;

                case 'date':

                break;

                case 'number':
                //$("#" + convert_object_path_to_jquery_id(p_object_path) + " input.number").numeric();
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number'
                ).numeric();
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number0'
                ).numeric({ decimal: false });
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number1'
                ).numeric({ decimalPlaces: 1 });
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number2'
                ).numeric({ decimalPlaces: 2 });
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number3'
                ).numeric({ decimalPlaces: 3 });
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number4'
                ).numeric({ decimalPlaces: 4 });
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number5'
                ).numeric({ decimalPlaces: 5 });
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number'
                ).attr('size', '15');
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number0'
                ).attr('size', '15');
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number1'
                ).attr('size', '15');
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number2'
                ).attr('size', '15');
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number3'
                ).attr('size', '15');
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number4'
                ).attr('size', '15');
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number5'
                ).attr('size', '15');

                break;

                case 'list':
                if 
                (
                    metadata.control_style != null &&
                    metadata.control_style == 'radio'
                ) 
                {
                    //console("bubba");
                    post_html_call_back.push
                    (
                        `$('#${convert_object_path_to_jquery_id(
                            p_object_path
                        )}${value}').focus()`
                    );
                }
                break;
            }

            if (post_html_call_back.length > 0) 
            {
                eval(post_html_call_back.join('\n'));
            }

            apply_validation();


        }
    );
  }

  window.setTimeout(function() { update_charts(p_dictionary_path) }, 0);
 //console.log('test');
}

function handle_paste_truncation(event, maxLength) {
    var element = event.target || event.srcElement;
    if (!element || !element.setAttribute) return;
    
    var clipboardData = event.clipboardData || window.clipboardData;
    if (!clipboardData) return;
    
    var pastedText = clipboardData.getData("text") || "";
    var currentValue = element.value || "";
    var selectionStart = element.selectionStart || 0;
    var selectionEnd = element.selectionEnd || 0;
    var beforeSelection = currentValue.substring(0, selectionStart);
    var afterSelection = currentValue.substring(selectionEnd);
    var wouldBeValue = beforeSelection + pastedText + afterSelection;
    
    if (wouldBeValue.length >= maxLength) {
        element.setAttribute("data-paste-truncated", "true");
    } else {
        if (element.removeAttribute) element.removeAttribute("data-paste-truncated");
    }
}

function g_add_grid_item(p_object_path, p_metadata_path, p_dictionary_path) 
{
  
  let metadata = $mmria.get_object_value_by_full_path(g_metadata, p_metadata_path);
  let new_line_item = create_default_object(metadata, {}, true);
  let grid = $mmria.get_object_value_by_full_path(g_data, p_object_path);

  grid.push(new_line_item[metadata.name][0]);
  set_local_case(g_data, function () {
    let post_html_call_back = [];
    let render_result = page_render(
      metadata,
      $mmria.get_object_value_by_full_path(g_data, p_object_path),
      g_ui,
      p_metadata_path,
      p_object_path,
      p_dictionary_path,
      false,
      post_html_call_back
    ).join('');

    let element = document.getElementById(p_metadata_path);
    element.outerHTML = render_result;

    apply_tool_tips();

    let jump_value = 9999;
    
    post_html_call_back.push
    (
      `document.getElementById("${p_metadata_path}").children[1].scrollTop = ${jump_value};
      set_focus_on_first_grid_item("${p_metadata_path}");`
    );

    if (post_html_call_back.length > 0) 
    {
      eval(post_html_call_back.join('\n'));
    }
  });
  

}


function set_focus_on_first_grid_item(p_metadata_path)
{

    var element = document.getElementById(p_metadata_path);
    let li_list = element.querySelectorAll("ul li");
    var lastchild = li_list[li_list.length-1];
    lastchild.querySelector("input, select, textarea, button").focus();
}

function g_delete_grid_item
(
	p_object_path,
	p_metadata_path,
    p_dictionary_path,
    p_metadata_prompt,
    p_data_length,
	p_index
) 
{
    var record_number = new Number(p_index) + new Number(1);

    const modal = build_delete_grid_dialog(record_number, p_object_path, p_metadata_path, p_dictionary_path, p_index, p_metadata_prompt, p_data_length);
    const box = $("#content");

    box.append(modal[0]);
    $(`#case_modal_${p_index}`).modal("show");
    $(`#case_modal_${p_index} .modal-footer .modal-cancel`).focus();
	
}

function g_delete_grid_item_action
    (
        p_object_path,
        p_metadata_path,
        p_dictionary_path,
        p_index
    )
{
	var metadata = $mmria.get_object_value_by_full_path(g_metadata, p_metadata_path);
	var index = p_object_path
		.match(new RegExp("\\[\\d+\\]$"))[0]
		.replace("[", "")
		.replace("]", "");
	var object_string = p_object_path.replace(new RegExp("(\\[\\d+\\]$)"), "");
    var object_path = $mmria.get_object_value_by_full_path(g_data, object_string);

	object_path.splice(index, 1);

	set_local_case(g_data, function () {
		var post_html_call_back = [];

		var render_result = page_render(
			metadata,
			object_path,
			g_ui,
			p_metadata_path,
			object_string,
			p_dictionary_path,
			false,
			post_html_call_back
		).join("");
		var element = document.getElementById(p_metadata_path);
		element.outerHTML = render_result;
		if (post_html_call_back.length > 0) {
			eval(post_html_call_back.join("\n"));
		}
    });


}

async function g_duplicate_record_item(p_object_path, p_metadata_path, p_index) 
{
    const metadata = $mmria.get_object_value_by_full_path(g_metadata, p_metadata_path);
    var object_string = p_object_path.replace(new RegExp("(\\[\\d+\\]$)"), "");

    const original = $mmria.get_object_value_by_full_path(g_data, object_string)[p_index];

    let clone = {};

    clone_multiform_object
    (
        metadata, 
        clone, 
        false,
        original,
        metadata.name
    )

    const multiform_path = p_object_path.substring(0, p_object_path.indexOf("["));
    var form_array = $mmria.get_object_value_by_full_path(g_data, multiform_path);     
    form_array.push(clone[metadata.name]);
    
    g_apply_sort(metadata, form_array, p_metadata_path, multiform_path, "/" + metadata.name);
    
    try
    {
        await save_case_and_wait(g_data, null, "duplicate_multiform");

        var post_html_call_back = [];
        document.getElementById(metadata.name + '_id').innerHTML = page_render
        (
            metadata,
            form_array,
            g_ui,
            p_metadata_path,
            multiform_path,
            "/" +metadata.name,
            false,
            post_html_call_back
        ).join('');
        if (post_html_call_back.length > 0) 
        {
            eval(post_html_call_back.join('\n'));
        }

        $mmria.duplicate_multiform_dialog_click();
    }
    catch (_ex)
    {
        // Existing save queue error handling/modal flow already handles the failure path.
    }

}


function g_delete_record_item(p_object_path, p_metadata_path, p_index) 
{
		var metadata = $mmria.get_object_value_by_full_path(g_metadata, p_metadata_path);
		var index = p_object_path
			.match(new RegExp("\\[\\d+\\]$"))[0]
			.replace("[", "")
			.replace("]", "");
		var object_string = p_object_path.replace(new RegExp("(\\[\\d+\\]$)"), "");
        var object_path = $mmria.get_object_value_by_full_path(g_data, object_string);

		object_path.splice(index, 1);
		set_local_case(g_data, function () {
			var post_html_call_back = [];
			document.getElementById(metadata.name + "_id").innerHTML = page_render(
				metadata,
				object_path,
				g_ui,
				p_metadata_path,
				object_string,
				"/" + metadata.name,
				false,
				post_html_call_back
			).join("");
			if (post_html_call_back.length > 0) {
				eval(post_html_call_back.join("\n"));
			}
		});
}




var $$ = {
  is_id: function (value) {
    // 2016-06-12T13:49:24.759Z
    if (value) {
      var test = value.match(/^\d+-\d+-\d+T\d+:\d+:\d+.\d+Z$/);
      return test ? true : false;
    } else {
      return false;
    }
  },
};

$(function () 
{
    /*
    if (window.IsDuplicate()) 
    {

      //alert("This is duplicate window\n\n Closing...");

      //document.getElementById('form_content_id').innerHTML = "It looks like you may have opened the view/edit case data in another browser tab.<br/> To ensure proper handling please use one broswer tab for editing a case.";

      //window.close();
 
      //return;
    }*/
  

  $(document).keydown(function (evt) 
  {
    if (evt.keyCode == 90 && evt.ctrlKey) 
    {
      evt.preventDefault();
      undo_click();
    }
  });

  window.setTimeout(()=> { $mmria.get_cvs_api_server_info(()=>{},()=>{}); }, 0);

  $('#profile_form2').on('submit', navigation_away);
  document.addEventListener('click', handle_case_page_link_navigation, false);

  if (window.location.pathname == '/analyst-case') 
  {
    g_is_data_analyst_mode = 'da';
  }


  // https://pure-essence.net/2010/02/14/jquery-session-timeout-countdown/
  // create the warning window and set autoOpen to false
  var sessionTimeoutWarningDialog = $('#sessionTimeoutWarningDiv');

  $('#sessionTimeoutOkButton').click(function () {
    // close dialog

    clearInterval(timer);
    running = false;
    $('#sessionTimeoutWarningDiv').dialog('close');
    profile.update_session_timer();
  });

  //$(sessionTimeoutWarningDialog).html(initialSessionTimeoutMessage);
  $(sessionTimeoutWarningDialog).dialog({
    title: 'Session Expiration Warning',
    autoOpen: false, // set this to false so we can manually open it
    closeOnEscape: false,
    draggable: false,
    width: 600,
    minHeight: 50,
    backgroundColor: 0xadc71a, // rgb(173, 199, 26),
    modal: true,
    beforeclose: function () {
      // bind to beforeclose so if the user clicks on the "X" or escape to close the dialog, it will work too
      // stop the timer
      clearInterval(timer);

      // stop countdown
      running = false;
    },
    buttons: {
      OK: function () {
        // close dialog

        clearInterval(timer);
        running = false;
        $(this).dialog('close');
        profile.update_session_timer();
      },
    },
    resizable: false,
    open: function () {
      // scrollbar fix for IE
      $('#sessionTimeoutWarningDiv').css('display', 'block');
      $('body').css('overflow', 'hidden');
      $('#sessionTimeoutExpiredId').hide();
      $('#sessionTimeoutPendingId').css('display', 'block');
    },
    close: function () {
      // reset overflow
      $('body').css('overflow', 'auto');
      clearInterval(timer);
      running = false;
      $(this).dialog('close');
      profile.update_session_timer();
    },
  });
  // end of dialog

  // start the idle timer
  //$.idleTimer(idleTime);

  // bind to idleTimer's idle.idleTimer event
  $(document).bind('sessionWarning', function () {
    $(sessionTimeoutWarningDialog).show();
    // if the user is idle and a countdown isn't already running
    //if($.data(document,'idleTimer') === 'idle' && !running)
    if (!running) {
      var counter = redirectAfter;
      running = true;

      // intialisze timer
      $('#' + sessionTimeoutCountdownId).html(redirectAfter);
      // open dialog
      $(sessionTimeoutWarningDialog).dialog('open');

      // create a timer that runs every second
      timer = setInterval(function () {
        counter -= 1;

        // if the counter is 0, redirect the user
        if (counter === 0) {
          //$(sessionTimeoutWarningDialog).html(expiredMessage);
          $('#sessionTimeoutExpiredId').show();
          $('#sessionTimeoutPendingId').css('display', 'none');
          $(sessionTimeoutWarningDialog).dialog('disable');
          //window.location = redirectTo;
          running = false;
          clearInterval(timer);
          clearInterval(session_warning_interval_id);
          profile.logout();
        } else {
          $('#' + sessionTimeoutCountdownId).html(counter);
        }
      }, 1000);
    }
  });

  //set_session_warning_interval();

  $.datetimepicker.setLocale('en');

  window.setTimeout(load_and_set_data, 0);
});


async function Get_Record_Id_List(p_call_back) 
{
    // Check if we're in offline mode
    const isOfflineMode = window.OfflineStatus.isOffline();
    
    if (isOfflineMode) {
        // In offline mode, use cached offline case data
        try {
            const recordIdSet = window.OfflineSessionManager.loadOfflineRecordIds(g_ui);
            recordIdSet.forEach(id => g_record_id_list.add(id));
            
            if (p_call_back != null) {
                p_call_back();
            }
        } catch (error) {
            offlineLog.error('CaseIndex', 'Error loading offline record IDs:', error);
            // Still call callback even if there's an error
            if (p_call_back != null) {
                p_call_back();
            }
        }
        return;
    }
    
    // Online mode - make API call as usual
    try {
        const url = `${location.protocol}//${location.host}/api/case_view/record-id-list`;

        const response = await $.ajax
        ({
            url: url,
        });

        if(response!= null)
        {
            for(var i = 0; i < response.length; i++)
            {
                let item = response[i];
                g_record_id_list.add(item.toUpperCase());
            }

            if(p_call_back!= null)
            {
                p_call_back();
            }
        }
    } catch (error) {
        console.error('Error fetching record ID list:', error);
        // Still call callback even if API call fails
        if (p_call_back != null) {
            p_call_back();
        }
    }

}

async function load_and_set_data() 
{
    const metadata_url = `${location.protocol}//${location.host}/api/jurisdiction_tree`;

    // Start all HTTP calls in parallel using native fetch
    const jurisdictionTreePromise = fetch(metadata_url).then(r => r.json());
    
    const formAccessPromise = get_form_access_list();
    
    const myUserPromise = fetch(`${location.protocol}//${location.host}/api/user/my-user`)
        .then(r => r.json())
        .catch(error => {
            console.error('Error loading user info:', error);
            return { name: 'offline-user' };
        });
    
    let duplicatePathPromise = null;
    if(!g_is_pmss_enhanced) {
        duplicatePathPromise = fetch(`${location.protocol}//${location.host}/Case/GetDuplicateMultiFormList`)
            .then(r => r.json())
            .catch(error => {
                console.error('Error loading duplicate path set (continuing without it):', error);
                return { field_list: [] };
            });
    }
    
    const myRolesPromise = fetch(`${location.protocol}//${location.host}/api/user_role_jurisdiction_view/my-roles`)
        .then(r => r.json())
        .catch(error => {
            console.error('Error loading user roles:', error);
            return { rows: [] };
        });

    // Wait for all calls to complete in parallel
    const [jurisdiction_tree, form_access_response, my_user_response, duplicate_path_set_response, my_role_list_response] = 
        await Promise.all([
            jurisdictionTreePromise,
            formAccessPromise,
            myUserPromise,
            duplicatePathPromise,
            myRolesPromise
        ].filter(p => p !== null));

    // Process form access response
    for(const item of form_access_response.access_list)
    {
        g_form_access_list.set(item.form_path.substr(1), item);
    }

    g_jurisdiction_tree = jurisdiction_tree;

    // Process user response
    g_user_name = my_user_response.name || my_user_response.user_name || 'offline-user';

    // Process duplicate path set response (if applicable)
    if(!g_is_pmss_enhanced && duplicate_path_set_response)
    {
        for(const i of duplicate_path_set_response.field_list)
        {
            g_duplicate_path_set.add(i);
        }
    }

    // Process roles response
    g_user_role_jurisdiction_list = [];
    for (let i in my_role_list_response.rows) 
    {
        let value = my_role_list_response.rows[i].value;
        role_set.add(value.role_name);
        if(value.role_name=="abstractor")
        {
            g_user_role_jurisdiction_list.push(value.jurisdiction_id);
        }
        else if(value.role_name=="jurisdiction_admin")
        {
            g_is_jurisdiction_admin = true;
        }
    }
    
    // Ensure at least one role is set for offline mode
    if (role_set.size === 0) {
        offlineLog.warn('CaseIndex', 'No roles found, adding default abstractor role for offline mode');
        role_set.add('abstractor');
    }

    if
    (
        g_user_role_jurisdiction_list.length == 0 &&
        my_role_list_response.rows.length == 1 &&
        my_role_list_response.rows[0].value.role_name == "vro"
    )
    {
        const value = my_role_list_response.rows[0].value;
        g_user_role_jurisdiction_list.push(value.jurisdiction_id);

        g_ui.case_view_request.status = "STEVE: Pending Vro Investigation";
        g_ui.case_view_request.jurisdiction = value.jurisdiction_id.substr(1);
    }

    if(location.href.endsWith("/CaseVRO"))
    {
        g_ui.case_view_request.status = "STEVE: Pending Vro Investigation";
    }

    create_jurisdiction_list(g_jurisdiction_tree);

    $('#landing_page').hide();
    $('#logout_page').hide();
    $('#footer').hide();
    $('#root').removeClass('header');

    const release_version = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/version/release-version`,
    });
    
    
    g_release_version = release_version;

    const default_ui_specification = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/version/${g_release_version}/ui_specification`,
    });
  
    g_default_ui_specification = default_ui_specification;
    

    document.getElementById('form_content_id').innerHTML = '<h4>Fetching data from database.</h4><h5>Please wait a few moments...</h5>';

    const metadata_response = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/version/${g_release_version}/metadata`,
    });

    g_metadata = metadata_response;
    metadata_summary(g_metadata_summary, g_metadata, 'g_metadata', 0, 0);
    default_object = create_default_object(g_metadata, {});

    build_other_specify_lookup(g_other_specify_lookup, g_metadata);

    set_list_lookup
    (
      g_display_to_value_lookup,
      g_value_to_display_lookup,
      g_value_to_index_number_lookup,
      g_metadata,
      '',
      'g_metadata'
    );


    for (let i in g_metadata.lookup) 
    {
      const child = g_metadata.lookup[i];

      g_look_up['lookup/' + child.name] = child.values;
    }

    // Check if we're in offline mode vs online mode
    const isOfflineMode = window.OfflineStatus.isOffline();
    
    if (isOfflineMode) {
        offlineLog.log('CaseIndex', '📴 Running in offline mode - loading from cache only');
        // Don't trigger new caching when already offline, just use what's cached
    } else {
        // Only cache metadata if we're preparing for offline mode (this should be triggered from the offline mode UI)
        // Removed automatic caching on page load since it should happen when entering offline mode
    }

    

    g_ui.url_state = url_monitor.get_url_state(window.location.href);
    
    // Set up the hash change handler
    window.onhashchange = window_on_hash_change;
    window.onbeforeunload = navigation_away;

    // Load the case set - hash changes will be handled naturally by browser navigation
    await get_case_set();
}
  

function create_jurisdiction_list(p_case_folder) 
{
  for (var i = 0; i < g_user_role_jurisdiction_list.length; i++) 
  {
    var jurisdiction_regex = new RegExp('^' + g_user_role_jurisdiction_list[i]);
    var match = p_case_folder.name.match(jurisdiction_regex);

    if (match) 
    {
      g_jurisdiction_list.push(p_case_folder.name);
      break;
    }
  }

  if (p_case_folder.children != null) 
  {
    for (var i = 0; i < p_case_folder.children.length; i++) 
    {
      var child = p_case_folder.children[i];

      create_jurisdiction_list(child);
    }
  }
}

var update_session_timer_interval_id = null;

async function apply_filter_click() 
{
    g_ui.case_view_request.page=1;
    await get_case_set();
}

async function get_case_set(p_call_back) 
{
    // DEBUG: Log get_case_set invocation
    //console.log(`[GET-CASE-SET-DEBUG] Entered get_case_set | p_call_back=${typeof p_call_back} | stack:`, new Error().stack.split('\n').slice(1, 3).join(' | '));
    
    // Check if we're in offline mode - if so, load cached cases
    const isOffline = window.OfflineStatus.isOffline();
    const isProcessingOfflineCases = window.OfflineStatus.isProcessingOfflineCases();
    
    if (is_offline_mode_enabled==true && isProcessingOfflineCases) {
        const offlineSessionId = localStorage.getItem('offline_session_id') || '';
        const offlineSessionData = await window.OfflineCaseManager.getCasesBySession(offlineSessionId);
        g_ui.offline_session_data = offlineSessionData;
        g_ui.offline_ids_not_changed = g_ui.offline_session_data.offline_ids.filter(id => !g_ui.offline_session_data.case_documents.some(change => change.documentId === id && change.syncState !== 5)); // 5 = no changes
    }else{

        g_ui.offline_ids_not_changed=[];
        g_ui.offline_session_data = null;
    }

    if (is_offline_mode_enabled==true && isOffline) {
        try {
            const offlineResult = await window.OfflineSessionManager.loadOfflineCases(
                ensure_offline_initialization,
                () => {
                    if (typeof window.OfflineCaseManager !== 'undefined' && window.OfflineCaseManager.updateOfflineCaseIndexMap) {
                        window.OfflineCaseManager.updateOfflineCaseIndexMap();
                    } else if (typeof update_offline_case_index_map === 'function') {
                        update_offline_case_index_map();
                    } else {
                        offlineLog.warn('CaseIndex', 'update_offline_case_index_map not available');
                    }
                }
            );
            
            g_ui.offline_mode_case_view_list = offlineResult.offline_mode_case_view_list;
            g_ui.case_view_list = offlineResult.case_view_list;
            g_ui.case_view_request.total_rows = offlineResult.total_rows;
        } catch (error) {
            offlineLog.error('CaseIndex', '❌ Error loading offline cases:', error);
            g_ui.case_view_list = [];
            g_ui.case_view_request.total_rows = 0;
        }
        
        // In offline mode, we need to render the navigation too
        if (p_call_back) {
            p_call_back();
        } else {
          
            
            if (!g_metadata || !g_metadata.children || g_form_access_list.size === 0 || role_set.size === 0) {
                offlineLog.error('CaseIndex', `❌ Missing required data for navigation rendering!\n  - Missing metadata: ${!g_metadata || !g_metadata.children}\n  - Missing form access: ${g_form_access_list.size === 0}\n  - Missing roles: ${role_set.size === 0}`);
            } 
            
            // Ensure default_object exists
            if (!default_object) {             
                default_object = {};
            }

            // Render navigation for offline mode
            var post_html_call_back = [];

            document.getElementById('navbar').innerHTML = navigation_render
            (
                g_metadata,
                0,
                g_ui
            ).join('');
            document.getElementById('form_content_id').innerHTML =
            '<h4>Fetching data from database.</h4><h5>Please wait a few moments...</h5>';
            document.getElementById('form_content_id').innerHTML = page_render(
                g_metadata,
                default_object,
                g_ui,
                'g_metadata',
                'default_object',
                '',
                false,
                post_html_call_back,
                null,
                null
            ).join('');
            
            if (post_html_call_back.length > 0) 
            {
                const codeToEval = post_html_call_back.join('\n');
                offlineLog.log('CaseIndex', `OFFLINE: About to evaluate post_html_call_back code:\n${codeToEval}\nCode length: ${codeToEval.length}`);
                
                try {
                    eval(codeToEval);
                } catch (error) {
                    offlineLog.error('CaseIndex', `OFFLINE: Error evaluating post_html_call_back: ${error}\nCode that failed:\n${codeToEval}`);
                }
            }
        }

        return;
    }

    //var url = `${location.protocol}//${location.host}/api/pinned_cases`;
    
  var case_view_url =
    location.protocol +
    '//' +
    location.host +
    '/api/case_view' +
    g_ui.case_view_request.get_query_string();


    // Start both HTTP calls in parallel to minimize round-trip latency
    let offlineSessionPromise = null;
    if(is_offline_mode_enabled==true)
    {
        const invalidStateDetected = localStorage.getItem('offline_mode_invalid_state_detected') || 'false';
        if(invalidStateDetected !=='true')
        {
            offlineSessionPromise = fetch(`/api/OfflineCase/active-user-session`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                },
            });
        }
    }

    // Wait for both calls to complete (filter out null if offline mode disabled)
    const [case_view_response] = await Promise.all(
        [
            $.ajax({ url: case_view_url }),
            offlineSessionPromise
        ].filter(p => p !== null)
    );

    //window.OfflineModals.closeLoadingSpinner(); 

    g_ui.case_view_request.total_rows = case_view_response.total_rows;
    
    // Use pinned_case_set from case_view_response (eliminates separate HTTP call)
    if(g_is_data_analyst_mode == null || g_is_data_analyst_mode !="da")
    {
        g_pinned_case_set = case_view_response.pinned_case_set || null;
    }

    // Create a map of case_view_response data by ID for quick lookup
    const fresh_case_data_map = new Map();
    for (const item of case_view_response.rows) {
        fresh_case_data_map.set(item.id, item);
    }

    // Build the final list prioritizing fresh data
    const new_list = [];
    const new_list_id_set = new Set();

    // First, add pinned cases (but use fresh data if available)
    for(const i in g_ui.case_view_list)
    {
        const item = g_ui.case_view_list[i];
        if (app_is_item_pinned(item.id) != 0 && !new_list_id_set.has(item.id)) 
        { 
            // Use fresh data if available, otherwise fall back to cached data
            const fresh_item = fresh_case_data_map.get(item.id);
            new_list.push(fresh_item || item); 
            new_list_id_set.add(item.id); 
        }
    }

    // Then add pinned cases from fresh data that weren't in the old list
    for(const item of case_view_response.rows)
    {
        if (app_is_item_pinned(item.id) != 0 && !new_list_id_set.has(item.id)) 
        { 
            new_list.push(item); 
            new_list_id_set.add(item.id);
        }
    }

    // Finally, add all non-pinned cases from fresh data
    for (const item of case_view_response.rows) 
    {
        if(!new_list_id_set.has(item.id))
            new_list.push(item);
    }

    g_ui.case_view_list = new_list;

    g_ui.offline_case_view_list_by_user = [];
    g_ui.process_offline_case_view_list_by_user = [];
    const offlineRestoreProcessingState = localStorage.getItem('process_offline_cases') || 'false';
    const offlineRestoreModeState = localStorage.getItem('is_offline') || 'false';
    
    // Process offline session result if it was fetched
    if(offlineSessionPromise)
    {        
        const processOfflineCases = localStorage.getItem('process_offline_cases') || 'false';
        const offlineSessionId = localStorage.getItem('offline_session_id');

        if(offlineRestoreModeState !== 'true' && offlineRestoreProcessingState !== 'true'){
            g_ui.offline_case_view_list_by_user = g_ui.case_view_list.filter(x=> x.value.offline_by == g_user_name && x.value.is_offline == true);
        }
        
        const response = await offlineSessionPromise;
        
        if (response.ok) {
            const result = await response.json();
                if(result && result.error !=="no active sessions"){
                    g_ui.process_offline_case_view_list_by_user = result;                          
                    // Check if offline_session_id is not set and set it from the response
                    if (!offlineSessionId || offlineSessionId === 'null' || offlineSessionId === '') {
                        offlineLog.log('CaseIndex', 'Setting offline_session_id from response:', result._id);
                        localStorage.setItem('offline_session_id', result._id);
                    }

                    if(g_ui.process_offline_case_view_list_by_user.offline_state === 0){
                        localStorage.setItem('abandon_offline_session', 'true');
                        //localStorage.setItem('offline_session_id', g_ui.process_offline_case_view_list_by_user._id)
                    }else if(g_ui.process_offline_case_view_list_by_user.offline_state === 1){
                        localStorage.setItem('process_offline_cases', 'true');
                        offlineLog.log('CaseIndex', 'User is processing offline cases');
                        // Fix race condition: Populate offline_ids_not_changed here as well
                        // This ensures it's set even on first load when process_offline_cases wasn't true yet
                        if (result.offline_ids && result.case_documents) {
                            g_ui.offline_ids_not_changed = result.offline_ids.filter(id => 
                                !result.case_documents.some(change => change.documentId === id && change.syncState !== 5)
                            );                          
                        }
                    }

                    const allDocumentsSynced = g_ui.process_offline_case_view_list_by_user.case_documents.every(doc => doc.syncState !== 0);

                    if (allDocumentsSynced && g_ui.process_offline_case_view_list_by_user.offline_state === 1) {
                        if (window.OfflineModals) {
                            window.OfflineModals.showLoadingSpinner(); 
                        }
                        await finish_online_processing_mode();
                        if (window.OfflineModals) {
                            window.OfflineModals.showLoadingSpinner(); 
                        }
                        // Exit immediately - do not render, do not call callback
                        // Page reload in finish_online_processing_mode will handle cleanup
                        return;
                    }
                }
                else{                 
                    localStorage.removeItem('abandon_offline_session');
                    localStorage.removeItem('offline_bypass_unlock_case_beacon');            
                    localStorage.removeItem('offline_session_id');            
                    
                }
            } 
        //}
    }
    if (window.OfflineModals) {
        window.OfflineModals.closeLoadingSpinner(); 
    }

    if (
        offlineRestoreModeState !== 'true' &&
        offlineRestoreProcessingState !== 'true' &&
        typeof window.restore_pending_go_offline_softlocks === 'function' &&
        window.__mmria_pending_go_offline_restore_running !== true
    ) {
        window.__mmria_pending_go_offline_restore_running = true;
        try {
            const restoreResult = await window.restore_pending_go_offline_softlocks();
            if (restoreResult && restoreResult.didRestore) {
                window.__mmria_pending_go_offline_restore_running = false;
                return get_case_set(p_call_back);
            }
        } finally {
            window.__mmria_pending_go_offline_restore_running = false;
        }
    }
    
    if (p_call_back) 
    {
        p_call_back();
    } 
    else 
    {
        var post_html_call_back = [];

        document.getElementById('navbar').innerHTML = navigation_render
        (
            g_metadata,
            0,
            g_ui
            ).join('');
            document.getElementById('form_content_id').innerHTML =
            '<h4>Fetching data from database.</h4><h5>Please wait a few moments...</h5>';
            document.getElementById('form_content_id').innerHTML = page_render(
            g_metadata,
            default_object,
            g_ui,
            'g_metadata',
            'default_object',
            '',
            false,
            post_html_call_back,
            null,
            null
        ).join('');

        if (post_html_call_back.length > 0) 
        {
            const codeToEval = post_html_call_back.join('\n');
            try {
                eval(codeToEval);
            } catch (error) {
                console.error('Error evaluating post_html_call_back:', error);
                console.error('Code that failed:', codeToEval);
            }
        }
        var section_list = document.getElementsByTagName('section');
        for (var i = 0; i < section_list.length; i++) 
        {
            var section = section_list[i];

            if (section.id == 'app_summary') 
            {
                section.style.display = 'block';
            } 
            else 
            {
                section.style.display = 'block';
            }
        }
    }
}

async function window_on_hash_change(e) 
{
  if (g_case_hash_restore_in_progress === true)
  {
    g_case_hash_restore_in_progress = false;
    g_ui.url_state = url_monitor.get_url_state(window.location.href);
    if (g_data)
    {
      g_render();
    }
    return;
  }

  // Detect when leaving /Case route and release locks EARLY (before navigation completes)
  
  if (is_offline_mode_enabled === true) {
    if (e.oldURL && e.newURL) {
        
      const newUrlLower = e.newURL.toLowerCase()
      
        const stillOnCase = newUrlLower.includes('/case');
           
        const isOfflineMode = localStorage.getItem('is_offline') === 'true';
        const isProcessingOfflineCases = localStorage.getItem('process_offline_cases') === 'true';
       
       

        // User is navigating from case view back to summary - release locks NOW while we have time
    
        if (!stillOnCase&&!isOfflineMode &&
            !isProcessingOfflineCases &&
            g_ui && 
            g_ui.offline_case_view_list_by_user && 
            g_ui.offline_case_view_list_by_user.length > 0 && 
            typeof window.OfflineSyncManager !== 'undefined' &&
            window.OfflineSyncManager.releaseCaseLocks) 
        {
        //   offlineLog.log('CaseIndex', 'Navigating away from case view - releasing offline case locks for', g_ui.offline_case_view_list_by_user.length, 'cases');
          
        //   // We have time to complete async operations during hash navigation
        //   try {
        //     await window.OfflineSyncManager.releaseCaseLocks();
        //     offlineLog.log('CaseIndex', '✓ Offline case locks released successfully via hash change');
        //   } catch (error) {
        //     offlineLog.error('CaseIndex', 'Error releasing offline case locks during navigation:', error);
        //   }
        }
      
    }
  }

  if (g_data) 
  {
    
    if (e.isTrusted) 
    {
      var new_url = e.newURL || window.location.href;
      g_ui.url_state = url_monitor.get_url_state(new_url);

      if 
      (
        g_ui.url_state.path_array &&
        g_ui.url_state.path_array.length > 0 &&
        parseInt(g_ui.url_state.path_array[0]) >= 0
      ) 
      {

        g_apply_sort(g_metadata, g_data, "","", "");
        var case_id = g_data._id;
        
        // Get the case index and add safety checks
        const caseIndex = parseInt(g_ui.url_state.path_array[0]);
        const isProcessingOfflineCases = window.OfflineStatus.isProcessingOfflineCases();
        const isOffline = window.OfflineStatus.isOffline();
        
        let targetCaseId;
        
        if (isProcessingOfflineCases || isOffline) {
            // Use offline navigation manager for offline modes
            const navigationResult = window.OfflineNavigationManager.getTargetCaseIdForHashChange(caseIndex, case_id, g_ui);
            
            if (navigationResult.error) {
                alert(navigationResult.error);
                window.location.hash = '#/summary';
                return;
            }
            
            targetCaseId = navigationResult.targetCaseId;
        } else {      
           
            if (!g_ui.case_view_list || caseIndex >= g_ui.case_view_list.length || caseIndex < 0) {               
                return;
            }
            
            targetCaseId = g_ui.case_view_list[caseIndex].id;
        }

        if(targetCaseId != case_id)
        {
            const previous_case_id = case_id;
            const previous_url = e.oldURL || window.location.href;
            
            // Mark for cleanup before saving
            g_case_cleanup_pending.add(previous_case_id);
            
            g_ui.broken_rules = {};
            clear_case_chart_state();
            if(g_data_is_checked_out)
            {
                try
                {
                    await run_case_save_busy_indicator_flow(async function()
                    {
                        await save_case_and_wait(g_data, null, "hash_change_case_switch");
                        clear_case_from_local_storage(previous_case_id);
                        g_case_cleanup_pending.delete(previous_case_id);
                        await get_specific_case(targetCaseId);
                    });
                }
                catch (_ex)
                {
                    g_case_cleanup_pending.delete(previous_case_id);
                    restore_case_hash_after_failed_save(previous_url);
                }
            }
            else
            {
                clear_case_from_local_storage(previous_case_id);
                g_case_cleanup_pending.delete(previous_case_id);
                
                await get_specific_case(targetCaseId);
            }

        }
        else
        {
            clear_case_chart_state();
            if(g_data_is_checked_out)
            {
                const current_data = g_data;
                try
                {
                    await save_case_and_wait(current_data, null, "hash_change_section_navigation");
                }
                catch (_ex)
                {
                    restore_case_hash_after_failed_save(e.oldURL || window.location.href);
                    return;
                }
            }


            g_render();
            
        }
      } 
      else 
      {
        if(g_data_is_checked_out)
        {
            const closing_case_id = g_data._id;
            const previous_url = e.oldURL || window.location.href;
            const release_tab_id = mmria_get_lock_release_tab_id(g_data);
            const old_date_last_updated = g_data.date_last_updated;
            const old_date_last_checked_out = g_data.date_last_checked_out;
            const old_last_checked_out_by = g_data.last_checked_out_by;
            const old_checked_out_by_tab_id = g_data.checked_out_by_tab_id;
            
            // Mark for cleanup before saving
            g_case_cleanup_pending.add(closing_case_id);
            
            g_data.date_last_updated = new Date();
            g_data.date_last_checked_out = null;
            g_data.last_checked_out_by = null;
            g_data.checked_out_by_tab_id = release_tab_id;
            g_data_is_checked_out = false;

            g_apply_sort(g_metadata, g_data, "","", "");

            try
            {
                await run_case_save_busy_indicator_flow(async function()
                {
                    await save_case_and_wait(g_data, null, "hash_change_close_case");
                    clear_case_from_local_storage(closing_case_id);
                    g_case_cleanup_pending.delete(closing_case_id);
                    g_data = null;
                    await get_case_set(function () {
                        g_render();
                    });
                });
            }
            catch (_ex)
            {
                g_case_cleanup_pending.delete(closing_case_id);
                g_data.date_last_updated = old_date_last_updated;
                g_data.date_last_checked_out = old_date_last_checked_out;
                g_data.last_checked_out_by = old_last_checked_out_by;
                g_data.checked_out_by_tab_id = old_checked_out_by_tab_id;
                g_data_is_checked_out = true;
                sync_edit_mode_auto_timers();
                restore_case_hash_after_failed_save(previous_url);
            }
        }
        else
        {
            // Clear localStorage for the case being closed (even if not checked out)
            if (g_data && g_data._id) {
                const closing_case_id = g_data._id;
                clear_case_from_local_storage(closing_case_id);
            }
            
            g_data = null;
            await get_case_set(function () {
                g_render();
            });
        }
      }
    }
  } 
  else if (e.isTrusted) 
  {
    var new_url = e.newURL || window.location.href;

    g_ui.url_state = url_monitor.get_url_state(new_url);

    if 
    (
      g_ui.url_state.path_array &&
      g_ui.url_state.path_array.length > 0 &&
      parseInt(g_ui.url_state.path_array[0]) >= 0
    ) 
    {
      const caseIndex = parseInt(g_ui.url_state.path_array[0]);
      
      // Check if we're in offline processing mode
      const isProcessingOfflineCases = window.OfflineStatus.isProcessingOfflineCases();
      
      // Check if we're in offline mode
      const isOffline = window.OfflineStatus.isOffline();
      const isBrowserOffline = !navigator.onLine;
      
      if (isProcessingOfflineCases) {
        // Processing offline cases mode: get case from offline session
   
        if (g_ui.process_offline_case_view_list_by_user && 
            g_ui.process_offline_case_view_list_by_user.case_documents &&
            caseIndex >= 0 && 
            caseIndex < g_ui.process_offline_case_view_list_by_user.case_documents.length) {
          
          const caseId = g_ui.process_offline_case_view_list_by_user.case_documents[caseIndex].documentId;

          
          g_ui.broken_rules = {};
          clear_case_chart_state();
          
          await get_specific_case(caseId);
        } else {
          const availableCount = g_ui.process_offline_case_view_list_by_user?.case_documents?.length || 0;
          offlineLog.error('CaseIndex', 'Invalid case index for offline session:', caseIndex, 'Available:', availableCount);
          alert('This case is not available in the current offline session. Please return to the case list.');
          window.location.hash = '#/summary';
        }
      }
      else if (isOffline || isBrowserOffline) {
        // Offline mode: ensure index map is synchronized first
        if (typeof window.OfflineCaseManager !== 'undefined' && window.OfflineCaseManager.updateOfflineCaseIndexMap) {
          window.OfflineCaseManager.updateOfflineCaseIndexMap();
        } else if (typeof update_offline_case_index_map === 'function') {
          update_offline_case_index_map();
        }
        
        let caseId = null;
        
        // Check if case exists in offline index map
        if (window.g_offline_case_index_map && caseIndex < window.g_offline_case_index_map.length && caseIndex >= 0) {
          caseId = window.g_offline_case_index_map[caseIndex];
          offlineLog.log('CaseIndex', `Loading offline case at index ${caseIndex} from index map:`, caseId);
        }
        
        if (caseId) {
          g_ui.broken_rules = {};
          clear_case_chart_state();
          
          // Load case data from service worker cache
          try {
            await get_offline_case(caseId);
          } catch (error) {
            offlineLog.error('CaseIndex', 'Error loading offline case in hash change:', error);           
            window.location.hash = '#/summary';
          }
        } else {
          const availableInIndexMap = window.g_offline_case_index_map ? window.g_offline_case_index_map.length : 0;
          const availableInCaseList = g_ui.case_view_list ? g_ui.case_view_list.length : 0;
          offlineLog.error('CaseIndex', `❌ HASH CHANGE DEBUG: Invalid offline case index: ${caseIndex}
  - Available in index map: ${availableInIndexMap}
  - Available in case list: ${availableInCaseList}
  - Current g_ui.case_view_list: ${JSON.stringify(g_ui.case_view_list)}
  - Current offline index map: ${JSON.stringify(window.g_offline_case_index_map)}
  - Full URL: ${window.location.href}
  - g_data is null: ${g_data === null}`);
          
          // Instead of showing alert, just redirect to summary if no cases available
          if (availableInCaseList === 0) {
            offlineLog.log('CaseIndex','📋 No cases available, redirecting to summary');
            window.location.hash = '#/summary';
          } else {
            offlineLog.log('CaseIndex','Case not found in offline list.');            
            window.location.hash = '#/summary';
          }
        }
      } else {
        // Online mode: use the regular case view list
        if (g_ui.case_view_list.length > caseIndex) 
        {
          g_ui.broken_rules = {};
          clear_case_chart_state();
          await get_specific_case
          (
            g_ui.case_view_list[caseIndex].id
          );
        } 
        else 
        {
          g_render();
        }
      }
    
    } 
    else 
    {

      g_render();
    }
  } 
  else 
  {
    // do nothing for now
  }
}

async function get_specific_case(p_id) 
{
  // Check if we're in offline mode first
  const isOffline = window.OfflineStatus.isOffline();

  if (isOffline) {
    offlineLog.log('CaseIndex', 'Offline mode detected - loading case from cache:', p_id);
    // In offline mode, use the offline case loading function
    try {
      await get_offline_case(p_id);
      return;
    } catch (error) {
      offlineLog.error('CaseIndex', 'Error loading offline case:', error);
      // If offline case loading fails, show error and redirect
      alert('This case is not available offline. Please check your network connection and try again when online.');
      window.location.hash = '#/summary';
      return;
    }
  }
  
  // Check if we're processing offline cases
  const isProcessingOfflineCases = window.OfflineStatus.isProcessingOfflineCases();
  
  if (isProcessingOfflineCases) {
    offlineLog.log('CaseIndex', 'Processing offline cases mode - loading case from offline session:', p_id);
    
    // Try to get the case from the offline session
    const offlineCase = get_case_from_offline_session(p_id);
    
    if (offlineCase) {
      // Successfully found the case in offline session
      g_data = offlineCase;
      g_data_is_checked_out = false; // Not checked out in processing mode
      
      stop_edit_mode_auto_timers();
      
      if (!g_is_pmss_enhanced) {
        g_case_narrative_original_value = offlineCase.case_narrative?.case_opening_overview;
      }
      
      g_render();
      return;
    } else {
      // Case not found in offline session - show error
      offlineLog.error('CaseIndex', 'Case not found in offline session:', p_id);
      alert('This case is not available in the current offline session. Please return to the case list.');
      window.location.hash = '#/summary';
      return;
    }
  }
  
  // Normal online mode - fetch from API
  const case_url = `${location.protocol}//${location.host}/api/case?case_id=${p_id}`;

  try
  {
    const case_response_promise = await fetch(case_url, {
        method: "get",
        headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json; charset=utf-8',
        'dataType': 'json',
        },

    });

    mmria_check_if_need_to_redirect(case_response_promise);

    if (case_response_promise.ok !== true)
    {
      throw new Error(`Case load failed with status ${case_response_promise.status}`);
    }

    const case_response =  await case_response_promise.json();

    if (case_response) 
    {
        if(g_is_pmss_enhanced)
        {
            await Attachment_GetFileList(p_id);
        }
    
        if(!g_is_pmss_enhanced)
        {
            g_case_narrative_original_value = case_response.case_narrative.case_opening_overview;
        }

        g_data = case_response;
        g_data_is_checked_out = is_case_checked_out(g_data);

        if (g_autosave_interval != null && g_data_is_checked_out == false) 
        {
            stop_edit_mode_auto_timers();
        }

        g_render();
    } 
    else 
    {
        throw new Error('Case load returned an empty response');
    }
  }
  catch(e)
  {
    console.log('get_specific_case:', e);
    g_data = null;
    g_data_is_checked_out = false;
    alert('Unable to load this case. Please return to the summary list and try again.');
    window.location.hash = '#/summary';
    return;
  }
}

function enqueue_case_save(p_data, p_call_back, p_note, p_options)
{
  const queue_item = get_new_save_queue_item(
    p_data,
    p_call_back,
    p_note,
    Object.assign({}, p_options, { isAwaited: false })
  );

  mmria_enqueue_save_queue_item(queue_item);
  mmria_schedule_save_queue_processing(0);
  return queue_item.id;
}

function save_case(p_data, p_call_back, p_note)
{
  console.warn(
    'save_case(...) is a fire-and-forget compatibility shim. Prefer enqueue_case_save(...) for background saves or save_case_and_wait(...) for awaited flows.'
  );
  return enqueue_case_save(p_data, p_call_back, p_note);
}

function save_case_and_wait(p_data, p_call_back, p_note, p_options)
{
  return new Promise((resolve, reject) => {
    const queue_item = get_new_save_queue_item(
      p_data,
      p_call_back,
      p_note,
      Object.assign({}, p_options, { isAwaited: true })
    );

    queue_item.completion = { resolve, reject };
    mmria_enqueue_save_queue_item(queue_item);
    mmria_schedule_save_queue_processing(0);
  });
}

async function process_save_case()
{
  if(save_queue.is_active === true) return;
  if(save_queue.item_list.length === 0) return;

  const item = save_queue.item_list[0];
  if(item == null)
  {
    save_queue.item_list.shift();
    mmria_schedule_save_queue_processing(0);
    return;
  }

  const now_ms = Date.now();
  if(item.next_attempt_ms != null && item.next_attempt_ms > now_ms)
  {
    mmria_schedule_save_queue_processing(item.next_attempt_ms - now_ms);
    return;
  }

  save_queue.is_active = true;
  save_queue.active_item = item;

  const complete_success = (response) =>
  {
    try
    {
      if(item.completion && typeof item.completion.resolve === 'function')
      {
        item.completion.resolve(response);
      }
    }
    catch(_ex) { /* ignore */ }
  };

  const complete_failure = (error) =>
  {
    try
    {
      if(item.completion && typeof item.completion.reject === 'function')
      {
        item.completion.reject(error);
      }
    }
    catch(_ex) { /* ignore */ }
  };

  const finalize_queue_state = () =>
  {
    save_queue.active_item = null;
    save_queue.is_active = false;
  };

  const fail_item = (err, p_options) =>
  {
    const options = Object.assign({
      dequeue: true,
      schedule_delay_ms: 0
    }, p_options);

    if(options.dequeue === true)
    {
      mmria_dequeue_save_queue_item(item);
    }

    finalize_queue_state();
    complete_failure(err);
    mmria_schedule_save_queue_processing(options.schedule_delay_ms);
  };

  const schedule_retry_or_fail = (err) =>
  {
    const case_id = item && item.data ? item.data._id : null;
    const awaited_save_is_queued =
      item.isAwaited !== true &&
      case_id != null &&
      mmria_has_awaited_save_for_case(case_id);

    if(awaited_save_is_queued)
    {
      mmria_dequeue_save_queue_item(item);
      finalize_queue_state();
      mmria_schedule_save_queue_processing(0);
      return;
    }

    const should_retry =
      item.retryMode === 'background-retry' &&
      mmria_is_retryable_transport_error(err);

    if(should_retry)
    {
      item.attempt_count = (item.attempt_count || 0) + 1;
      const delay_ms = mmria_get_save_retry_delay_ms(item.attempt_count);
      item.next_attempt_ms = Date.now() + delay_ms;

      const dialog_cooldown_ms = 30000;
      const last_shown = item.last_error_dialog_shown_ms || 0;
      if(Date.now() - last_shown > dialog_cooldown_ms)
      {
        item.last_error_dialog_shown_ms = Date.now();
        try { $mmria.unstable_network_dialog_show(err, item.note); } catch(_ex) { /* ignore */ }
      }

      finalize_queue_state();
      mmria_schedule_save_queue_processing(delay_ms);
      return;
    }

    try { $mmria.unstable_network_dialog_show(err, item.note); } catch(_ex) { /* ignore */ }
    fail_item(err);
  };

  try
  {
    const p_data = item.data;
    const is_data_analyst_mode =
      item.is_data_analyst_mode != null ||
      (p_data && p_data.is_data_analyst_mode != null);

    if(is_data_analyst_mode)
    {
      mmria_dequeue_save_queue_item(item);
      finalize_queue_state();

      if (typeof item.continuation_callback === 'function')
      {
        try { await item.continuation_callback(); } catch (callback_ex) { console.error('Save continuation failed:', callback_ex); }
      }

      complete_success(null);
      mmria_schedule_save_queue_processing(0);
      return;
    }

    const save_case_request = mmria_create_save_case_request_from_queue_item(item);
    let case_response = {};

    const isOffline = window.OfflineStatus.isOffline();
    if (isOffline)
    {
      offlineLog.log('CaseIndex', 'Offline mode detected - tracking document changes instead of saving to server');
      offlineLog.info('CaseIndex', 'Starting offline case save', {
        caseId: p_data && p_data._id,
        note: item.note || '',
        changeCount: save_case_request.Change_Stack.items.length
      });

      try
      {
        if (window.OfflineIntegrityValidator)
        {
          await window.OfflineIntegrityValidator.validateCurrentState({
            checkPoint: 'case_save',
            expectedOfflineIds: p_data && p_data._id ? [p_data._id] : []
          });
        }

        const changeStackCopy = mmria_safe_clone(save_case_request.Change_Stack.items);
        offlineLog.log('CaseIndex', 'Copying change stack with', changeStackCopy.length, 'items for offline tracking');

        if (typeof track_offline_document_change === 'function')
        {
          await track_offline_document_change(
            p_data._id,
            p_data,
            item.note || 'Document modified while offline',
            changeStackCopy
          );
        }
        else
        {
          throw new Error('Offline change tracker is not available');
        }

        case_response = {
          ok: true,
          rev: p_data._rev,
          id: p_data._id,
          offline_save: true
        };

        offlineLog.log('CaseIndex', 'Offline save completed for document:', p_data._id);
      }
      catch (error)
      {
        offlineLog.error('CaseIndex', 'Error tracking offline document change:', error);
        case_response = {
          ok: false,
          error_description: 'Failed to track offline changes: ' + error.message
        };
      }
    }
    else
    {
      if(typeof navigator !== 'undefined' && navigator && navigator.onLine === false)
      {
        schedule_retry_or_fail({
          status: 0,
          message: 'Browser is offline',
          responseText: 'Browser is offline'
        });
        return;
      }

      try
      {
        const case_response_promise = await mmria_fetch_case_save_response(
          save_case_request,
          item.timeout_ms
        );

        mmria_check_if_need_to_redirect(case_response_promise);

        try
        {
          case_response = await case_response_promise.json();
        }
        catch(_parse_ex)
        {
          schedule_retry_or_fail({
            status: case_response_promise.status,
            message: 'Failed to parse save response JSON',
            responseText: 'Failed to parse save response JSON'
          });
          return;
        }
      }
      catch(xhr)
      {
        schedule_retry_or_fail(xhr);
        return;
      }
    }

    const is_successful_offline_save =
      case_response != null &&
      case_response.ok === true &&
      case_response.offline_save === true &&
      case_response.id != null &&
      case_response.id !== '';

    const has_required_save_revision =
      case_response != null &&
      case_response.rev != null;

    if
    (
      case_response == null ||
      case_response.ok !== true ||
      (
        has_required_save_revision !== true &&
        is_successful_offline_save !== true
      )
    )
    {
      if(case_response != null && case_response.error_description != null)
      {
        const err_object = { status: 500, responseText: case_response.error_description };

        if
        (
          typeof err_object.responseText === "string" &&
          err_object.responseText.indexOf("Case is offline in another tab for this user.") > -1
        )
        {
          if (typeof show_edit_offline_case_tab_conflict_modal === 'function')
          {
            show_edit_offline_case_tab_conflict_modal(p_data._id);
          }

          fail_item(err_object);
          return;
        }

        if
        (
          typeof err_object.responseText === "string" &&
          err_object.responseText.indexOf("Case is locked by another tab for this user.") > -1
        )
        {
          if (typeof show_edit_lock_tab_conflict_modal === 'function')
          {
            show_edit_lock_tab_conflict_modal(p_data._id);
          }

          fail_item(err_object);
          return;
        }

        if
        (
          typeof err_object.responseText === "string" &&
          err_object.responseText.indexOf("Case is locked by ") === 0
        )
        {
          const lockedByMatch = err_object.responseText.match(/^Case is locked by (.+?)\. Please try again after /);
          if
          (
            lockedByMatch &&
            lockedByMatch[1] &&
            typeof show_case_locked_by_another_user_modal === 'function'
          )
          {
            show_case_locked_by_another_user_modal(p_data._id, lockedByMatch[1]);
            fail_item(err_object);
            return;
          }
        }

        if
        (
          typeof err_object.responseText === "string" &&
          err_object.responseText.indexOf("(409) Conflict") > -1
        )
        {
          err_object.responseText = "Unable to save document Conflict";
          $mmria.save_error_500_dialog_show(err_object, `${item.note} (409) Conflict`);
          fail_item(err_object);
          return;
        }

        $mmria.save_error_500_dialog_show(err_object, item.note);
        fail_item(err_object);
        return;
      }

      fail_item({
        status: 500,
        responseText: case_response != null ? case_response.error_description : 'Unknown save failure'
      });
      return;
    }

    mmria_reconcile_live_save_state_after_success(item);

    if(g_data && g_data._id == case_response.id)
    {
      if(case_response.rev != null)
      {
        g_data._rev = case_response.rev;
      }

      g_data.last_updated_by = g_user_name;
      g_data_is_checked_out = is_case_checked_out(g_data);

      if (g_data_is_checked_out)
      {
        set_local_case(g_data);
      }

      const node_list = document.querySelectorAll("#last_updated_span");
      for(const el of node_list)
      {
        if(el != null)
        {
          const date_part_display_value = convert_datetime_to_local_display_value(
            g_data.date_last_updated
          );

          const save_text = `${g_data.last_updated_by} ${date_part_display_value}`;
          el.innerHTML = save_text;
        }
      }
    }

    mmria_rebase_queued_items_to_new_rev(case_response.id, case_response.rev);
    mmria_dequeue_save_queue_item(item);
    item.date_completed = new Date();
    finalize_queue_state();

    if (typeof item.continuation_callback === 'function')
    {
      try { await item.continuation_callback(); } catch (callback_ex) { console.error('Save continuation failed:', callback_ex); }
    }

    complete_success(case_response);
    mmria_schedule_save_queue_processing(0);
  }
  catch(ex)
  {
    console.error('Unexpected case save processing error:', ex);
    const err_object = {
      status: 500,
      responseText: (ex && ex.message) ? ex.message : 'Unexpected case save processing error'
    };

    try { $mmria.save_error_500_dialog_show(err_object, item.note); } catch(_dialog_ex) { /* ignore */ }
    fail_item(err_object);
  }
}

function get_mmria_save_busy_indicator_api()
{
  if
  (
    window.MMRIAModals &&
    typeof window.MMRIAModals.showSaveBusyIndicator === 'function' &&
    typeof window.MMRIAModals.closeSaveBusyIndicator === 'function'
  )
  {
    return window.MMRIAModals;
  }

  return null;
}

async function run_case_save_busy_indicator_flow(p_flow, p_options)
{
  const options = p_options || {};
  const close_on_success = options.close_on_success !== false;
  const modal_api = get_mmria_save_busy_indicator_api();

  if (modal_api)
  {
    modal_api.showSaveBusyIndicator();
  }

  try
  {
    const result = await p_flow();

    if (modal_api && close_on_success)
    {
      modal_api.closeSaveBusyIndicator();
    }

    return result;
  }
  catch(ex)
  {
    if (modal_api)
    {
      modal_api.closeSaveBusyIndicator();
    }

    throw ex;
  }
}

async function delete_case(p_id, p_rev) 
{

  let tab_id = null;
  try
  {
    if (typeof window.mmria_get_unique_tab_id === 'function')
    {
      await window.mmria_get_unique_tab_id();
    }

    if (typeof get_mmria_tab_id === 'function')
    {
      tab_id = get_mmria_tab_id();
    }
  }
  catch (ex)
  {
    // Best-effort: delete can proceed even if tab id is unavailable.
    tab_id = null;
  }

    const case_response = await $.ajax({
        url:
          location.protocol +
          '//' +
          location.host +
          '/api/case?case_id=' +
          p_id +
          '&rev=' +
      p_rev +
      (tab_id ? ('&tab_id=' + encodeURIComponent(tab_id)) : ''),
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        //data: JSON.stringify(p_data),
        type: 'DELETE',
      }).fail(function (xhr, err) 
      {
        console.log('delete_case: failed', err);
      });
      
    clear_case_from_local_storage(p_id);
    await get_case_set();
    
}


function g_render() 
{
  var post_html_call_back = [];

  document.getElementById('navbar').innerHTML = navigation_render
  (
    g_metadata,
    0,
    g_ui
  ).join('');

  $('[data-toggle="tooltip"]').tooltip({
    classes: {
      'ui-tooltip': 'custom-tooltip'
    },
    position: {
      my: "left-10 top", //position from top of tooltip
      at: "bottom+10" //at bottom of element
    }
  });

  document.getElementById('form_content_id').innerHTML = page_render
  (
    g_metadata,
    g_data,
    g_ui,
    'g_metadata',
    'g_data',
    '',
    false,
    post_html_call_back
  ).join('');

  apply_tool_tips();
  sync_edit_mode_auto_timers();

  if (post_html_call_back.length > 0) 
  {
    try
    {
      eval(post_html_call_back.join('\n'));
    } 
    catch (ex) 
    {
      console.log(ex);
    }
  }

  var section_list = document.getElementsByTagName('section');

if (g_ui.url_state.path_array[0] == 'summary') 
{
    for (var i = 0; i < section_list.length; i++) 
    {
        var section = section_list[i];

        if (section.id == 'app_summary') 
        {
        section.style.display = 'block';
        //section.style.display = "grid";
        //section.style["grid-template-columns"] = "1fr 1fr 1fr";
        } 
        else 
        {
        section.style.display = 'none';
        }
    }
} 
else if 
(
    g_ui.url_state.path_array.length >= 2 &&
    g_ui.url_state.path_array[1] == 'field_search'
) 
{
    for (var i = 0; i < section_list.length; i++) 
    {
        var section = section_list[i];

        if (section.id == 'field_search_id') 
        {
            section.style.display = 'block';
            //section.style.display = "grid";
            //section.style["grid-template-columns"] = "1fr 1fr 1fr";
        } 
        else 
        {
            section.style.display = 'none';
        }
    }
} 
  else 
  {
    if 
    (
      g_ui.url_state.path_array.length > 2 &&
      parseInt(g_ui.url_state.path_array[0]) >= 0
    ) 
    {
      for (var i = 0; i < section_list.length; i++) 
      {
        var section = section_list[i];

        if (section.id == g_ui.url_state.path_array[1]) 
        {
          section.style.display = 'block';
          //section.style.display = "grid";
          //section.style["grid-template-columns"] = "1fr 1fr 1fr";
        } 
        else 
        {
          section.style.display = 'none';
        }
      }
    } 
    else 
    {
      for (var i = 0; i < section_list.length; i++) 
      {
        var section = section_list[i];

        if (section.id == g_ui.url_state.path_array[1] + '_id') 
        {
          section.style.display = 'block';
          //section.style.display = "grid";
          //section.style["grid-template-columns"] = "1fr 1fr 1fr";
        }
        else 
        {
          section.style.display = 'none';
        }
      }
    }
  }

  apply_validation();
  
}

function show_print_version() 
{
  window.open('./print-version', '_print_version');
}

function apply_tool_tips() 
{
  $('[rel=tooltip]').tooltip();

  if (!g_data_is_checked_out)
  {
    apply_validation();
    return;
  }

  const form_root = $('#form_content_id');
  if (form_root.length === 0)
  {
    apply_validation();
    return;
  }

  form_root.find('.time').each(function ()
  {
    const time_input = $(this);
    if (time_input.data('DateTimePicker'))
    {
      return;
    }

    time_input.datetimepicker({
      format: 'HH:mm:ss',
      defaultDate: '',
      keepInvalid: true,
      useCurrent: false,
      icons: {
        time: 'x24 fill-p cdc-icon-clock_01',
        date: 'x24 fill-p cdc-icon-calendar_01',
        up: 'x24 fill-p cdc-icon-chevron-circle-up',
        down: 'x24 fill-p cdc-icon-chevron-circle-down',
        previous: 'x24 fill-p fill-p cdc-icon-chevron-circle-left-light',
        next: 'x24 fill-p cdc-icon-chevron-circle-right-light',
      },
    });
  });

  form_root.find('input.number').numeric().attr('size', '15');
  form_root.find('input.number0').numeric({ decimal: false }).attr('size', '15');
  form_root.find('input.number1').numeric({ decimalPlaces: 1 }).attr('size', '15');
  form_root.find('input.number2').numeric({ decimalPlaces: 2 }).attr('size', '15');
  form_root.find('input.number3').numeric({ decimalPlaces: 3 }).attr('size', '15');
  form_root.find('input.number4').numeric({ decimalPlaces: 4 }).attr('size', '15');
  form_root.find('input.number5').numeric({ decimalPlaces: 5 }).attr('size', '15');


  apply_validation();
}

function apply_validation() 
{

    let list_has_items = false;
    let validation_summary = [];

    for (let key in g_ui.broken_rules) 
    {
        
        if(g_ui.broken_rules[key]!= null)
        {
            list_has_items = true;
            validation_summary.push(g_ui.broken_rules[key]);
        }
        
    }

    if(list_has_items)
    {
      validation_summary.unshift("$('#validation_summary_list').empty();")
      validation_summary.push("$('#validation_summary').css('display','');")
    }
    else
    {
        validation_summary.unshift("$('#validation_summary_list').empty();")
        validation_summary.push("$('#validation_summary').css('display','none');")
    }

  eval(validation_summary.join(""));

}



function dispose_all_modals() 
{
  $('.modal').modal('hide');
  $('.modal').remove();
  $('.modal-backdrop').remove();
}

function delete_record(p_index) 
{
    var data = g_ui.case_view_list[p_index];

    g_selected_delete_index = null;

    $.ajax({
      url:
        location.protocol +
        '//' +
        location.host +
        '/api/case?case_id=' +
        data.id,
    }).done(function (case_response) 
    {
      delete_case(case_response._id, case_response._rev);
    });

}

var save_interval_id = null;

function enable_print_button(event) 
{
  const { value } = event.target;
  //targeting next sibling buttons
  const printButton = event.target.nextSibling; 
  printButton.disabled = !value; // if there is a value it will be enabled.
  const pdfViewButton = printButton.nextSibling;
  pdfViewButton.disabled = !value;
  const pdfSaveButton = pdfViewButton.nextSibling;
  pdfSaveButton.disabled = !value;
}


let unique_tab_name = '';
function pdf_case_onclick(event, type_output) 
{
	//console.log('type_output: ', type_output);
  const btn = event.target;
 
	const dropdown = ( type_output == 'view' )
		? btn.previousSibling.previousSibling
		: btn.previousSibling.previousSibling.previousSibling;

  // get value of selected option
  let section_name = dropdown.value;

  unique_tab_name = '_pdf_tab_' + Math.random().toString(36).substring(2, 9);

  if (section_name) 
  {
    if (section_name == 'core-summary') 
    {

        window.setTimeout(function()
        {
            openTab('./pdf-version', unique_tab_name, section_name, type_output);
        }, 1000);	
    } 
    else 
    {
        // data-record of selected option
        const selectedOption = dropdown.options[dropdown.options.selectedIndex];
        const record_number = selectedOption.dataset.record;
				

        if(section_name == "all_hidden")
        {
            section_name = 'all';

            window.setTimeout(function()
            {
                openTab('./pdf-version',  unique_tab_name, section_name, type_output, record_number, true);
            }, 1000);	
        }
        else
        {
            window.setTimeout(function()
            {
                openTab('./pdf-version',  unique_tab_name, section_name, type_output, record_number);
            }, 1000);	
        }
      
    }
  }

}

function print_case_onclick(event) 
{
	const btn = event.target;
	const dropdown = btn.previousSibling;
	// get value of selected option
	let section_name = dropdown.value;
	unique_tab_name = '_print_tab_' + Math.random().toString(36).substring(2, 9);
  
	if (section_name) 
	{
	  if (section_name == 'core-summary') 
	  {
  
		  window.setTimeout(function()
		  {
			  openTab('./core-elements', unique_tab_name, 'all', 'print');
		  }, 1000);	
  
		
	  } 
	  else 
	  {
		// data-record of selected option
		const selectedOption = dropdown.options[dropdown.options.selectedIndex];
		const record_number = selectedOption.dataset.record;
  
        if(section_name == "all_hidden")
        {
            section_name = 'all';

            window.setTimeout(function()
            {
                openTab('./print-version', unique_tab_name, section_name, 'print', record_number, true);
            }, 1000);	
        }
        else
        {
  
            window.setTimeout(function()
            {
                openTab('./print-version', unique_tab_name, section_name, 'print', record_number);
            }, 1000);	
        }
		
	  }
	}
  
}

function openTab(pageRoute, tabName, p_section, p_type_output, p_number, p_show_hidden) 
{

    function clone(obj) 
    {
        if (null == obj || "object" != typeof obj) return obj;
        let copy = obj.constructor();
        for (var attr in obj) 
        {
            if (obj.hasOwnProperty(attr)) copy[attr] = obj[attr];
        }
        return copy;
    }


	// console.log('in openTab');
	// console.log('pageRoute: ', pageRoute);
	// console.log('tabName case: ', tabName);
	//console.log('g_metadata: ', g_metadata);
	//console.log('g_data: ', g_data);
	// console.log('p_section: ', p_section);
	// console.log('p_number: ', p_number);
	// console.log('p_type_output: ', p_type_output);


   // g_data.case_narrative.case_opening_overview = textarea_control_strip_html_attributes(g_data.case_narrative.case_opening_overview);


   let sorted_data = clone(g_data);

   g_apply_sort(g_metadata, sorted_data, "","", "");


  if (!window[tabName] || window[tabName].closed) 
  {
    window[tabName] = window.open(pageRoute, tabName, null, false);
    window[tabName].addEventListener('load', () => {
      window[tabName].create_print_version(
        g_metadata,
        sorted_data,
        p_section,
		p_type_output,
        p_number,
        g_metadata_summary,
        p_show_hidden
      );
    });
  } 
  else 
  {
    // if the WindowProxy Object already exists then just call the function on it
    window[tabName].create_print_version(
      g_metadata,
      sorted_data,
      p_section,
	p_type_output,
      p_number,
      g_metadata_summary,
      p_show_hidden
    );
  }
}

async function add_new_form_click(p_metadata_path, p_object_path, p_dictionary_path) 
{
  //console.log('add_new_form_click: ' + p_metadata_path + ' , ' + p_object_path);

  const spinner = $(event.target).siblings('.spinner-inline');
  spinner.addClass('spinner-active');

  var metadata = $mmria.get_object_value_by_full_path(g_metadata, p_metadata_path);
  var form_array = $mmria.get_object_value_by_full_path(g_data, p_object_path);
  var new_form = create_default_object(metadata, {}, true);
  var item = new_form[metadata.name][0];

  form_array.push(item);

  g_apply_sort(metadata, form_array, p_metadata_path, p_object_path, p_dictionary_path);

  try
  {
    await save_case_and_wait(g_data, null, "add_new_form");

    var post_html_call_back = [];
    document.getElementById(metadata.name + '_id').innerHTML = page_render
    (
        metadata,
        form_array,
        g_ui,
        p_metadata_path,
        p_object_path,
        p_dictionary_path,
        false,
        post_html_call_back
    ).join('');

    if (post_html_call_back.length > 0) 
    {
        eval(post_html_call_back.join('\n'));
    }
  }
  catch (_ex)
  {
    // Existing save queue error handling/modal flow already handles the failure path.
  }
  finally
  {
    spinner.removeClass('spinner-active');
  }
}

async function enable_edit_click() 
{
  if (g_data) 
  {
    if (typeof window.mmria_get_unique_tab_id === 'function')
    {
      await window.mmria_get_unique_tab_id();
    }

    const current_tab_id = get_mmria_tab_id();

    // Reload the case first to avoid editing with a stale _rev.
    // If the case is currently locked by another user, block with a simple alert.
    const case_id = g_data._id;
    await get_specific_case(case_id);

    if (!g_data || g_data._id !== case_id) return;

    if (
      (
        g_data.is_offline === true ||
        g_data.is_offline === 'true'
      ) &&
      g_data.offline_by != null &&
      g_data.offline_by != '' &&
      g_user_name != null &&
      g_user_name != '' &&
      g_data.offline_by.toLowerCase() == g_user_name.toLowerCase() &&
      g_data.offline_by_tab_id != null &&
      g_data.offline_by_tab_id != '' &&
      g_data.offline_by_tab_id != current_tab_id
    )
    {
      if (typeof show_edit_offline_case_tab_conflict_modal === 'function')
      {
        show_edit_offline_case_tab_conflict_modal(case_id);
      }
      g_data_is_checked_out = false;
      stop_edit_mode_auto_timers();
      g_render();
      return;
    }

    if (
      g_data.last_checked_out_by != null &&
      g_data.last_checked_out_by != '' &&
      g_user_name != null &&
      g_user_name != '' &&
      g_data.last_checked_out_by.toLowerCase() != g_user_name.toLowerCase() &&
      is_checked_out_expired(g_data) == false
    )
    {
        show_case_locked_by_another_user_modal(case_id, g_data.last_checked_out_by);      
        return;
    }

    if (
      g_data.last_checked_out_by != null &&
      g_data.last_checked_out_by != '' &&
      g_user_name != null &&
      g_user_name != '' &&
      g_data.last_checked_out_by.toLowerCase() == g_user_name.toLowerCase() &&
      is_checked_out_expired(g_data) == false &&
      g_data.checked_out_by_tab_id != null &&
      g_data.checked_out_by_tab_id != '' &&
      g_data.checked_out_by_tab_id != current_tab_id
    )
    {      
      show_edit_lock_tab_conflict_modal(case_id);
      // Ensure we remain in view mode in this tab.
      g_data_is_checked_out = false;
      stop_edit_mode_auto_timers();
      g_render();
      return;
    }

    let new_date = new Date();

    const change_stack_length_before_checkout = g_change_stack.length;
    const old_date_last_updated = g_data.date_last_updated;
    const old_date_last_checked_out = g_data.date_last_checked_out;
    const old_last_checked_out_by = g_data.last_checked_out_by;
    const old_checked_out_by_tab_id = g_data.checked_out_by_tab_id;

    g_change_stack.push({
        _id: g_data._id,
        _rev: g_data._rev,
      object_path: 'g_data.date_last_checked_out',
      metadata_path: '/date_last_checked_out',
      old_value: g_data.date_last_checked_out,
      new_value: new_date.toISOString(),
      dictionary_path: '/date_last_checked_out',
      metadata_type: 'datetime',
      prompt: 'date_last_checked_out',
      date_created: new_date.toISOString(),
      user_name: g_user_name
    });

    g_data.date_last_updated = new_date;
    g_data.date_last_checked_out = new_date;
    g_data.last_checked_out_by = g_user_name;
    g_data.checked_out_by_tab_id = current_tab_id;

    // Do not switch the UI into edit mode until the server accepts the checkout.
    try
    {
      await save_case_and_wait(g_data, null, "enable_edit");
      create_save_message();
    }
    catch(_ex)
    {
      // Revert local optimistic checkout fields and keep the UI in view mode.
      g_data.date_last_updated = old_date_last_updated;
      g_data.date_last_checked_out = old_date_last_checked_out;
      g_data.last_checked_out_by = old_last_checked_out_by;
      g_data.checked_out_by_tab_id = old_checked_out_by_tab_id;
      g_data_is_checked_out = false;

      if (g_change_stack.length > change_stack_length_before_checkout)
      {
        g_change_stack.length = change_stack_length_before_checkout;
      }

      stop_edit_mode_auto_timers();

      g_render();
      return;
    }

    g_data_is_checked_out = true;
    g_render();

    if ($global.case_document_begin_edit != null) 
    {
        $global.case_document_begin_edit();
    }
  }
}

async function save_form_click() 
{
  try
  {
    await run_case_save_busy_indicator_flow(async function()
    {
      await save_case_and_wait(g_data, null, 'save_form_click');
      create_save_message();
    });
  }
  catch (_ex)
  {
    // Existing save dialog flow handles the failure path.
  }
}

async function save_and_finish_click() 
{
  const current_data = g_data;
  const case_id = current_data._id;
  const release_tab_id = mmria_get_lock_release_tab_id(current_data);
  const old_date_last_updated = current_data.date_last_updated;
  const old_date_last_checked_out = current_data.date_last_checked_out;
  const old_last_checked_out_by = current_data.last_checked_out_by;
  const old_checked_out_by_tab_id = current_data.checked_out_by_tab_id;

  current_data.date_last_updated = new Date();
  current_data.date_last_checked_out = null;
  current_data.last_checked_out_by = null;
  current_data.checked_out_by_tab_id = release_tab_id;
  g_data_is_checked_out = false;
  g_apply_sort(g_metadata, current_data, "", "", "");
  
  // Mark for cleanup before saving
  g_case_cleanup_pending.add(case_id);

  try
  {
    await run_case_save_busy_indicator_flow(async function()
    {
      await save_case_and_wait(current_data, null, 'save_and_finish_click');
      current_data.checked_out_by_tab_id = null;
      clear_case_from_local_storage(case_id);
      g_case_cleanup_pending.delete(case_id);
      stop_edit_mode_auto_timers();
      create_save_message();
      g_render();
    });
  }
  catch (_ex)
  {
    g_case_cleanup_pending.delete(case_id);
    current_data.date_last_updated = old_date_last_updated;
    current_data.date_last_checked_out = old_date_last_checked_out;
    current_data.last_checked_out_by = old_last_checked_out_by;
    current_data.checked_out_by_tab_id = old_checked_out_by_tab_id;
    g_data_is_checked_out = true;
    sync_edit_mode_auto_timers();
    g_render();
  }
}

function clear_case_from_local_storage(case_id)
{
  if
  (
    !case_id ||
    typeof window === 'undefined' ||
    window.localStorage == null
  )
  {
    return;
  }

  try
  {
    window.localStorage.removeItem('case_' + case_id);

    const case_index_raw = window.localStorage.getItem('case_index');
    if (case_index_raw)
    {
      let case_index = null;

      try
      {
        case_index = JSON.parse(case_index_raw);
      }
      catch (_parse_ex)
      {
        case_index = null;
      }

      if
      (
        case_index != null &&
        Object.prototype.hasOwnProperty.call(case_index, case_id)
      )
      {
        delete case_index[case_id];
        window.localStorage.setItem('case_index', JSON.stringify(case_index));
      }
    }
  }
  catch (ex)
  {
    console.error('Error clearing case data from localStorage:', ex);
  }
}

function restore_case_hash_after_failed_save(previous_url)
{
  try
  {
    if(previous_url != null && previous_url !== '')
    {
      const parsed_url = new URL(previous_url, window.location.href);
      if(parsed_url.hash && parsed_url.hash !== window.location.hash)
      {
        g_case_hash_restore_in_progress = true;
        window.location.hash = parsed_url.hash;
        return;
      }
    }
  }
  catch(ex)
  {
    console.error('Unable to restore previous case hash after failed save:', ex);
  }

  g_ui.url_state = url_monitor.get_url_state(window.location.href);
  if (g_data)
  {
    g_render();
  }
}

async function save_case_before_full_navigation(target_url)
{
  if (g_case_navigation_save_in_progress)
  {
    return;
  }

  if (!g_data || !g_data_is_checked_out)
  {
    window.location.assign(target_url);
    return;
  }

  g_case_navigation_save_in_progress = true;

  const current_data = g_data;
  const case_id = current_data._id;
  const release_tab_id = mmria_get_lock_release_tab_id(current_data);
  const old_date_last_updated = current_data.date_last_updated;
  const old_date_last_checked_out = current_data.date_last_checked_out;
  const old_last_checked_out_by = current_data.last_checked_out_by;
  const old_checked_out_by_tab_id = current_data.checked_out_by_tab_id;

  try
  {
    await run_case_save_busy_indicator_flow(async function()
    {
      current_data.date_last_updated = new Date();
      current_data.date_last_checked_out = null;
      current_data.last_checked_out_by = null;
      current_data.checked_out_by_tab_id = release_tab_id;

      g_data_is_checked_out = false;
      g_case_cleanup_pending.add(case_id);
      g_apply_sort(g_metadata, current_data, '', '', '');
      stop_edit_mode_auto_timers();

      await save_case_and_wait(current_data, null, 'leave_case_navigation');

      clear_case_from_local_storage(case_id);
      g_case_cleanup_pending.delete(case_id);
      window.location.assign(target_url);
    }, { close_on_success: false });
  }
  catch (_ex)
  {
    g_case_cleanup_pending.delete(case_id);
    current_data.date_last_updated = old_date_last_updated;
    current_data.date_last_checked_out = old_date_last_checked_out;
    current_data.last_checked_out_by = old_last_checked_out_by;
    current_data.checked_out_by_tab_id = old_checked_out_by_tab_id;
    g_data_is_checked_out = true;
    sync_edit_mode_auto_timers();
    g_render();
    g_case_navigation_save_in_progress = false;
  }
}

function handle_case_page_link_navigation(event)
{
  if (g_case_navigation_save_in_progress || !g_data_is_checked_out || !g_data)
  {
    return;
  }

  if (event.defaultPrevented || event.button !== 0)
  {
    return;
  }

  if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey)
  {
    return;
  }

  const anchor = event.target && typeof event.target.closest === 'function'
    ? event.target.closest('a[href]')
    : null;

  if (!anchor)
  {
    return;
  }

  if (anchor.hasAttribute('download'))
  {
    return;
  }

  const target_attr = anchor.getAttribute('target');
  if (target_attr && target_attr.toLowerCase() !== '_self')
  {
    return;
  }

  const raw_href = anchor.getAttribute('href');
  if
  (
    !raw_href ||
    raw_href.startsWith('javascript:') ||
    raw_href.startsWith('mailto:') ||
    raw_href.startsWith('tel:')
  )
  {
    return;
  }

  let target_url;
  try
  {
    target_url = new URL(anchor.href, window.location.href);
  }
  catch (_ex)
  {
    return;
  }

  if (target_url.origin !== window.location.origin)
  {
    return;
  }

  const current_path = window.location.pathname.toLowerCase();
  const target_path = target_url.pathname.toLowerCase();
  const is_hash_only_navigation =
    target_path === current_path &&
    target_url.search === window.location.search &&
    target_url.hash &&
    target_url.hash !== window.location.hash;

  if (is_hash_only_navigation)
  {
    return;
  }

  if
  (
    target_path === current_path &&
    target_url.search === window.location.search &&
    target_url.hash === window.location.hash
  )
  {
    return;
  }

  event.preventDefault();
  save_case_before_full_navigation(target_url.toString());
}

function create_save_message() 
{
  var result = [];

  result.push(`
    <div class="alert alert-success alert-dismissible">
      <button class="close" data-dismiss="alert" aria-label="close">&times;</button>
      <p>Case information has been saved</p>
    </div>
  `);

  document.getElementById('nav_status_area').innerHTML = result.join('');

  window.setTimeout(clear_nav_status_area, 5000);
}

function clear_nav_status_area() 
{
  document.getElementById('nav_status_area').innerHTML = '<div>&nbsp;</div>';
}

function set_local_case(p_data, p_call_back) 
{
  if (typeof p_call_back === 'function')
  {
    p_call_back();
  }
}

function get_local_case(p_id) 
{
  return null;
}

function undo_click() 
{
  var current_change = g_change_stack.pop();

  if (current_change) 
  {
    var metadata = $mmria.get_object_value_by_full_path(g_metadata, current_change.metadata_path);

    if 
    (
      metadata.type.toLowerCase() == 'list' &&
      metadata['is_multiselect'] &&
      metadata.is_multiselect == true
    ) 
    {
      var item = $mmria.get_object_value_by_full_path(g_data, current_change.object_path);

      if (item.indexOf(current_change.old_value) > -1) 
      {
        item.splice(item.indexOf(current_change.old_value), 1);
      } 
      else 
      {
        item.push(current_change.old_value);
      }
    } 
    else if (metadata.type.toLowerCase() == 'boolean') 
    {
        $mmria.set_object_value_by_full_path(g_data, current_change.object_path, current_change.old_value);
    } 
    else 
    {
        $mmria.set_object_value_by_full_path(g_data, current_change.object_path, current_change.old_value);
    }
  }

  g_render();
}

function autosave() 
{
    const split_one = window.location.href.split('#');

    if (split_one.length <= 1) return;

    const split_two = split_one[0].split('/');

    if (split_two.length <= 3) return;
    
    if
    (
        !(
            split_two[3].toLocaleLowerCase() == 'case' ||
            split_two[3].toLocaleLowerCase() == 'abstractordeidentifiedcase' 
        )
    )
    {
        return;
    }

    const split_three = split_one[1].split('/');

    if
    (
        split_three.length <= 1 ||
        split_three[1].toLocaleLowerCase() == 'summary'
    ) 
    {
        return;
    }

    if (g_data == null  || g_data == undefined) return;

    if (check_edit_inactivity())
    {
        return;
    }

    
    const dt1 = new Date(g_data.date_last_updated);
    const dt2 = new Date();
    const number_of_minutes = diff_minutes(dt1, dt2);

    if (number_of_minutes < 3) return; 
    

    if (mmria_has_awaited_save_for_case(g_data._id))
    {
        return;
    }

    g_data.date_last_updated = new Date();
    enqueue_case_save(g_data, null, 'autosave', {
        intent: 'autosave',
        retryMode: 'background-retry'
    });
  
}

function is_case_view_locked(p_case)
{
    let result = false;

    let selected_value = 9999;
    
    if
    (
        p_case.case_status &&
        p_case.case_status != ""
    )
    {
        selected_value = new Number(p_case.case_status);
    }
    
    if
    (
        p_case.case_status &&
        p_case.case_locked_date != "" &&
        (
            selected_value == 4 ||
            selected_value == 5 ||
            selected_value == 6
        )
    )
    {
        if (! g_is_confirm_for_case_lock)
        {
            result = true;
        }
    }

    return result;
}



function is_case_locked(p_case)
{
    let result = false;

    let selected_value = 9999;
    
    if
    (
        p_case.home_record &&
        p_case.home_record.case_status &&
        p_case.home_record.case_status.overall_case_status &&
        p_case.home_record.case_status.overall_case_status != ""
    )
    {
        selected_value = new Number(p_case.home_record.case_status.overall_case_status);
    }
    
    if
    (
        p_case.home_record != null &&
        p_case.home_record.case_status != null  &&
        p_case.home_record.case_status.overall_case_status != null &&
        //p_case.home_record.case_status.case_locked_date != "" &&
        (
            selected_value == 4 ||
            selected_value == 5 ||
            selected_value == 6
        )
    )
    {
        if (! g_is_confirm_for_case_lock)
        {
            result = true;
        }
    }
    
    return result;
}



function is_case_checked_out(p_case) 
{
  let is_checked_out = false;

  // Add null check for p_case
  if (!p_case) {
    console.warn('is_case_checked_out called with null case data');
    return false;
  }

  let current_date = new Date();

  if 
  (
    p_case.date_last_checked_out != null &&
    p_case.date_last_checked_out != ''
  ) 
  {
    const current_tab_id = get_mmria_tab_id();

    let try_date = null;
    let is_date = false;
    if (!(p_case.date_last_checked_out instanceof Date)) 
    {
      try_date = new Date(p_case.date_last_checked_out);
    } 
    else 
    {
      try_date = p_case.date_last_checked_out;
    }

    if 
    (
      diff_minutes(try_date, current_date) <= 120 &&
      p_case.last_checked_out_by.toLowerCase() == g_user_name.toLowerCase()
    ) 
    {
      // If the server stored a tab id for this lock, require it to match this tab.
      if
      (
        p_case.checked_out_by_tab_id != null &&
        p_case.checked_out_by_tab_id != '' &&
        p_case.checked_out_by_tab_id != current_tab_id
      )
      {
        is_checked_out = false;
      }
      else
      {
        is_checked_out = true;
      }
    }
  }

  return is_checked_out;
}

function is_checked_out_expired(p_case) 
{
  let is_expired = true;

  let current_date = new Date();

  if 
  (
    p_case.date_last_checked_out != null &&
    p_case.date_last_checked_out != ''
  ) 
  {
    let try_date = null;
    if (!(p_case.date_last_checked_out instanceof Date)) 
    {
      try_date = new Date(p_case.date_last_checked_out);
    } 
    else 
    {
      try_date = p_case.date_last_checked_out;
    }

    if (diff_minutes(try_date, current_date) < 120) 
    {
      is_expired = false;
    }
  }

  return is_expired;
}

function diff_minutes(dt1, dt2) 
{
  let diff = (dt2.getTime() - dt1.getTime()) / 1000;

  diff /= 60;
  return Math.abs(Math.round(diff));
}

function g_textarea_oninput
(
    p_object_path,
    p_metadata_path,
    p_dictionary_path,
    value
) 
{
    var metadata = $mmria.get_object_value_by_full_path(g_metadata, p_metadata_path);

    g_case_narrative_is_updated = true;
    g_case_narrative_is_updated_date = new Date()

    try
    {
        $mmria.set_object_value_by_full_path(g_data, p_object_path, value);
        set_local_case(g_data, null);
    }
    catch(e)
    {
        const err = {
            status: 500,
            responseText : `unable to save field: ${p_dictionary_path}\n${e}`
        };
        $mmria.field_save_error_dialog_show(err, `unable to save field: ${p_dictionary_path} `);
    }

}

function navigation_away(e) 
{

  // BACKUP: Finalize unload cleanup using sendBeacon when page is unloading.
  // - Releases current case edit lock (tab-validated)
  // - Removes offline soft locks (batched)
  // Primary lock release still happens in window_on_hash_change.

  const split_one = window.location.href.split('#');
  const split_two = (split_one.length > 0 ? split_one[0].split('/') : []);
  const isCaseRoute = (split_two.length > 3 && split_two[3].toLocaleLowerCase() == 'case');

  const current_tab_id = get_mmria_tab_id();
  const current_case_id = (g_data_is_checked_out && g_data && g_data._id) ? g_data._id : null;

  let offline_case_ids = [];
  if (is_offline_mode_enabled === true && isCaseRoute)
  {
    const isOfflineMode = localStorage.getItem('is_offline') === 'true';
    const isProcessingOfflineCases = localStorage.getItem('process_offline_cases') === 'true';
    const offlineBypassUnlockBeacon = localStorage.getItem('offline_bypass_unlock_case_beacon') === 'true';

    if (!isOfflineMode &&
        !isProcessingOfflineCases &&
        !offlineBypassUnlockBeacon &&
        g_ui &&
        g_ui.offline_case_view_list_by_user &&
        g_ui.offline_case_view_list_by_user.length > 0)
    {
      offline_case_ids = g_ui.offline_case_view_list_by_user
        .map(c => c && c.id ? c.id : null)
        .filter(id => id);
    }
  }

  if (isCaseRoute && navigator.sendBeacon && (current_case_id || offline_case_ids.length > 0))
  {
    const batch_size = 20;
    const finalize_url = `${location.protocol}//${location.host}/api/case/finalize-unload`;

    let successCount = 0;
    let totalBeacons = 0;

    const offline_set = new Set(offline_case_ids);
    const offline_remaining = offline_case_ids.filter(id => !current_case_id || id != current_case_id);

    const first_batch = [];
    if (current_case_id && offline_set.has(current_case_id))
    {
      first_batch.push(current_case_id);
    }

    while (first_batch.length < batch_size && offline_remaining.length > 0)
    {
      first_batch.push(offline_remaining.shift());
    }

    if (current_case_id || first_batch.length > 0)
    {
      totalBeacons++;
      const payload = JSON.stringify({
        current_case_id: current_case_id,
        tab_id: current_tab_id,
        offline_case_ids: first_batch
      });

      const sent = navigator.sendBeacon(
        finalize_url,
        new Blob([payload], { type: 'application/json' })
      );

      if (sent) successCount++;
    }

    while (offline_remaining.length > 0)
    {
      totalBeacons++;
      const batch = offline_remaining.splice(0, batch_size);
      const payload = JSON.stringify({
        current_case_id: null,
        tab_id: current_tab_id,
        offline_case_ids: batch
      });

      const sent = navigator.sendBeacon(
        finalize_url,
        new Blob([payload], { type: 'application/json' })
      );

      if (sent) successCount++;
    }

    offlineLog.log('CaseIndex', `✓ Sent ${successCount}/${totalBeacons} finalize-unload beacons during page unload`);
  }
  else if (isCaseRoute && !navigator.sendBeacon)
  {
    offlineLog.warn('CaseIndex', 'navigator.sendBeacon not supported - unload cleanup may not run');
  }

  if (g_data_is_checked_out && g_data)
  {
    g_data.date_last_updated = new Date();
    g_data.date_last_checked_out = null;
    g_data.last_checked_out_by = null;
    g_data.checked_out_by_tab_id = null;
    g_data_is_checked_out = false;

    for (let i = 0; i < g_ui.case_view_list.length; i++)
    {
      let item = g_ui.case_view_list[i];
      if (item.id == g_data._id)
      {
        item.date_last_checked_out = null;
        item.last_checked_out_by = null;
        item.checked_out_by_tab_id = null;
        break;
      }
    }

    stop_edit_mode_auto_timers();

    clear_case_from_local_storage(g_data._id);
  }


}


function render_summary_validation
(
    p_metadata, 
    p_data, 
    p_path,  
    p_object_path,
    p_result, 
    p_form_index, 
    p_grid_index
)
{
    switch(p_metadata.type.toLocaleLowerCase())
    {
        case "app":
            
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                if
                (
                    p_data && 
                    p_data[child.name] &&
                    child.type.toLocaleLowerCase() == "form" &&
                    g_ui.url_state &&
                    g_ui.url_state.selected_id &&
                    g_ui.url_state.selected_id == child.name
                )
                {
                    render_summary_validation(child, p_data[child.name], p_path + "/" + child.name, p_object_path + "." + child.name, p_result, p_form_index, p_grid_index);
                }
            }
            break;
        case "form":
            if(p_metadata.cardinality == "1" || p_metadata.cardinality == "?")
            {
                for(let i = 0; i < p_metadata.children.length; i++)
                {
                    let child = p_metadata.children[i];

                    if(p_data && p_data[child.name])
                    {
                        render_summary_validation(child, p_data[child.name], p_path + "/" + child.name, p_object_path + "." + child.name, p_result, p_form_index, p_grid_index);
                    }
                    
                }
            }
            else // multiform
            {

                for(let form_index = 0; form_index < p_data.length; form_index++)
                {
                    let row_data = p_data[form_index]
                    for(let i = 0; i < p_metadata.children.length; i++)
                    {
                        let child = p_metadata.children[i];
    
                        if(row_data)
                        {
                            render_summary_validation(child, row_data, p_path + "/" + child.name, p_object_path + "[" + form_index + "]." + child.name, p_result, form_index, p_grid_index);
                        }
                        
                    }
                }
            }
            break;
        case "group":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                if(p_data)
                {
                    render_summary_validation(child, p_data[child.name], p_path + "/" + child.name, p_object_path + "." + child.name, p_result, p_form_index, p_grid_index);
                }
            }
            break;
        case "grid":
            for(let i = 0; i < p_data.length; i++)
            {
                let row_item = p_data[i];
                for(let j = 0; j <p_metadata.children.length; j++)
                {
                    let child = p_metadata.children[j];
                    render_summary_validation(child, row_item[child.name], p_path + "/" + child.name, p_object_path + "[" + i + "]." + child.name, p_result, p_form_index, i);
                }
            
            }
            break;
        case "string":
        case "number":
        case "time":
                // do nothing for now
            break;
        case "date":
            if (!is_valid_date(p_data)) 
            {
                //p_result.push('<li data-path="${p_dictionary_path.substring(1, p_dictionary_path.length)}" data-grid="'+ p_grid_index +'"><strong>'+legend_label+': ${p_metadata.prompt}, item '+(parseInt(p_grid_index)+1)+':</strong> Date must be a valid calendar date between 1900-2100</li>');
                p_result.push(`$('#validation_summary_list').append('<li><strong>${p_metadata.prompt} ${p_data}:</strong> Date must be a valid calendar date between 1900-2100 <button class="btn anti-btn ml-1"><span class="sr-only">Remove Item</span><span class="x20 cdc-icon-times-solid"></span></button></li>');`);
                
            }
            break;
        case "datetime":
            if (!is_valid_datetime(p_data)) 
            {
                //p_result.push('<li data-path="${p_dictionary_path.substring(1, p_dictionary_path.length)}" data-grid="'+p_grid_index+'"><strong>'+legend_label+': ${p_metadata.prompt}, item '+(parseInt(p_grid_index)+1)+':</strong> Date must be a valid calendar date between 1900-2100</li>');
                p_result.push(`$('.construct__header-alert ul').append('<li><strong>${p_metadata.prompt} ${p_data}:</strong> Date must be a valid calendar date between 1900-2100 <button class="btn anti-btn ml-1"><span class="sr-only">Remove Item</span><span class="x20 cdc-icon-times-solid"></span></button></li>');`);
                
            }
            break;
        case "list":
            // do nothing for now
            break;
        case "textarea":
            // do nothing for now
            break;
    }
}

function gui_remove_broken_rule_click(p_object_id)
{
    gui_remove_broken_rule(p_object_id);
    apply_validation();

}

function gui_remove_broken_rule(p_object_id)
{
    if(g_ui.broken_rules.hasOwnProperty(p_object_id))
    {
        g_ui.broken_rules[p_object_id] = null;
        delete g_ui.broken_rules[p_object_id];
    }

    let item = document.getElementById(`${p_object_id}-inline-validation-message`);
    if(item != null)
    {
        item.style.display = 'none';
    }

    //remove validation error from date control
    $(`#${p_object_id} .date-control`).removeClass('is-invalid');
    //remove validation error from datetime control
    $(`#${p_object_id}-innerdiv`).removeClass('is-invalid');
}




function build_other_specify_lookup(p_result, p_metadata, p_path = "")
{
    switch(p_metadata.type.toLocaleLowerCase())
    {
        case "app":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];

                build_other_specify_lookup(p_result, child, `${child.name}`);
            }
            break;
        case "form":
        case "group":
        case "grid":

                for(let i = 0; i < p_metadata.children.length; i++)
                {
                    let child = p_metadata.children[i];

                    build_other_specify_lookup(p_result, child, `${p_path}/${child.name}`);
                }
            break;
        case "list":
            let other_specify_list_key = [];
            let other_specify_list_path = [];

            if
            (
                p_metadata.other_specify_list != null && 
                p_metadata.other_specify_list.trim().length > 0
            )
            {
                let item_list = p_metadata.other_specify_list.split(',');
                for(let i = 0; i < item_list.length; i++)
                {
                    let kvp = item_list[i].split(' ');
                    if
                    (
                        kvp.length > 1 &&
                        kvp[0] != null &&
                        kvp[0].trim().length > 0 &&
                        kvp[1] != null &&
                        kvp[1].trim().length > 0
                    )
                    {
                        let key = kvp[0].trim();
                        let path = kvp[1].trim();

                        p_result[path] = { list: `${p_path}`, value: key }
                        
                    }
                }
            }


            for(let i = 0; i < other_specify_list_key.length; i++)
            {
                let item = other_specify_list_key[i];
                let object_path = `g_data.${other_specify_list_path[i].replace(/\//g,".")}`;
                
            }
        break;
        case "string":
        case "number":
        case "time":
        case "date":
        case "datetime":
        case "textarea":
        default:
            break;
    }
}





async function get_form_access_list()
{
	var metadata_url = location.protocol + '//' + location.host + '/_users/GetFormAccess';

	const response = await $.ajax
	({
			url: metadata_url
	});

	return response;
}

// Set up network monitoring for case pages
if (
    typeof window !== 'undefined' &&
    window.OfflineStatus &&
    typeof window.OfflineStatus.isOffline === 'function' &&
    window.OfflineStatus.isOffline() &&
    window.OfflineNetworkMonitor &&
    typeof window.OfflineNetworkMonitor.setupCasePageMonitoring === 'function'
) {
    window.OfflineNetworkMonitor.setupCasePageMonitoring();
}

// Tab id helpers live in /scripts/case/tab-id.js

let g_offline_softlock_recovery_context = null;
let g_offline_softlock_recovery_in_progress = false;
const mmria_enable_offline_softlock_reclaim_ui = false;

function mmria_normalize_case_id_list(caseIds) {
    if (!Array.isArray(caseIds)) {
        return [];
    }

    return caseIds
        .map(caseId => (caseId || '').toString().trim())
        .filter((caseId, index, source) => caseId.length > 0 && source.indexOf(caseId) === index);
}

function mmria_get_reclaimable_softlock_case_ids() {
    if (
        typeof g_ui === 'undefined' ||
        !g_ui ||
        !Array.isArray(g_ui.offline_case_view_list_by_user)
    ) {
        return [];
    }

    return mmria_normalize_case_id_list(
        g_ui.offline_case_view_list_by_user
            .filter(item => {
                const lockType = item && item.value ? `${item.value.offline_lock_type || ''}` : '';
                return lockType !== '2';
            })
            .map(item => item && item.id ? item.id : null)
    );
}

function mmria_set_offline_softlock_recovery_context(modalId, options) {
    const safeOptions = options || {};
    const context = {
        modalId: modalId,
        caseId: safeOptions.caseId || '',
        caseIds: mmria_normalize_case_id_list(safeOptions.caseIds || []),
        refreshMode: safeOptions.refreshMode || 'none'
    };

    if (context.caseIds.length === 0) {
        g_offline_softlock_recovery_context = null;
        return null;
    }

    g_offline_softlock_recovery_context = context;
    return context;
}

function mmria_clear_offline_softlock_recovery_context(modalId) {
    if (
        !g_offline_softlock_recovery_context ||
        !modalId ||
        g_offline_softlock_recovery_context.modalId === modalId
    ) {
        g_offline_softlock_recovery_context = null;
    }
}

function mmria_get_offline_softlock_recovery_button_html() {
    if (
        !mmria_enable_offline_softlock_reclaim_ui ||
        !g_offline_softlock_recovery_context ||
        !Array.isArray(g_offline_softlock_recovery_context.caseIds) ||
        g_offline_softlock_recovery_context.caseIds.length === 0
    ) {
        return '';
    }

    return `
        <button type="button" class="btn btn-primary" onclick="confirm_offline_softlock_recovery()" style="margin-right: 10px; padding: 8px 20px;">
            Reclaim to This Tab
        </button>
    `;
}

function mmria_close_offline_softlock_conflict_modal(modalId) {
    switch (modalId) {
        case 'remove-offline-softlock-tab-conflict-modal':
            close_remove_offline_softlock_tab_conflict_modal();
            break;
        case 'add-offline-softlock-tab-conflict-modal':
            close_add_offline_softlock_tab_conflict_modal();
            break;
        case 'go-offline-tab-conflict-modal':
            close_go_offline_tab_conflict_modal();
            break;
        case 'edit-offline-case-tab-conflict-modal':
            close_edit_offline_case_tab_conflict_modal();
            break;
        default:
            mmria_clear_offline_softlock_recovery_context(modalId);
            break;
    }
}

async function mmria_get_offline_softlock_recovery_tab_id() {
    if (typeof get_current_recovery_tab_id === 'function') {
        const currentRecoveryTabId = await get_current_recovery_tab_id();
        if (currentRecoveryTabId) {
            return currentRecoveryTabId;
        }
    }

    if (typeof window.mmria_get_unique_tab_id === 'function') {
        await window.mmria_get_unique_tab_id();
    }

    if (typeof get_mmria_tab_id === 'function') {
        return get_mmria_tab_id();
    }

    return null;
}

async function mmria_recover_offline_softlocks_to_current_tab(context) {
    const currentTabId = await mmria_get_offline_softlock_recovery_tab_id();
    if (!currentTabId) {
        throw new Error('Unable to determine the current browser tab id.');
    }

    const response = await fetch('/api/OfflineCase/recover-softlocks', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            offlineSessionId: '',
            caseIds: context.caseIds,
            tab_id: currentTabId
        })
    });

    let responseBody = null;
    try {
        responseBody = await response.json();
    } catch (_parseError) {
        responseBody = null;
    }

    if (!response.ok || !responseBody || responseBody.ok !== true) {
        const responseError =
            responseBody && responseBody.error_description
                ? responseBody.error_description
                : `Failed to reclaim offline cases: ${response.status} ${response.statusText}`;

        throw new Error(responseError);
    }

    if (
        context.caseId &&
        typeof g_data !== 'undefined' &&
        g_data &&
        g_data._id === context.caseId
    ) {
        g_data.is_offline = true;
        g_data.offline_by = g_user_name;
        g_data.offline_lock_type = 1;
        g_data.offline_by_tab_id = currentTabId;
    }

    return currentTabId;
}

async function mmria_refresh_after_offline_softlock_recovery(context) {
    if (context.refreshMode === 'list' && typeof get_case_set === 'function') {
        await get_case_set();
    }
}

async function confirm_offline_softlock_recovery() {
    if (g_offline_softlock_recovery_in_progress) {
        return;
    }

    const context = g_offline_softlock_recovery_context;
    if (!context || !Array.isArray(context.caseIds) || context.caseIds.length === 0) {
        return;
    }

    g_offline_softlock_recovery_in_progress = true;

    try {
        mmria_close_offline_softlock_conflict_modal(context.modalId);

        if (window.OfflineModals && typeof window.OfflineModals.showLoadingSpinner === 'function') {
            window.OfflineModals.showLoadingSpinner();
        }

        await mmria_recover_offline_softlocks_to_current_tab(context);
        await mmria_refresh_after_offline_softlock_recovery(context);
    } catch (error) {
        const errObject = {
            status: 500,
            responseText: error && error.message ? error.message : 'Unable to reclaim offline cases.'
        };

        if (typeof $mmria !== 'undefined' && $mmria && typeof $mmria.save_error_500_dialog_show === 'function') {
            $mmria.save_error_500_dialog_show(errObject, 'reclaim_offline_softlock');
        } else {
            alert(errObject.responseText);
        }
    } finally {
        g_offline_softlock_recovery_in_progress = false;

        if (window.OfflineModals && typeof window.OfflineModals.closeLoadingSpinner === 'function') {
            window.OfflineModals.closeLoadingSpinner();
        }
    }
}


function show_remove_offline_softlock_tab_conflict_modal(caseID) {
    const recoveryContext = mmria_set_offline_softlock_recovery_context('remove-offline-softlock-tab-conflict-modal', {
        caseId: caseID,
        caseIds: [caseID],
        refreshMode: 'list'
    });
    const showRecoveryUi = mmria_enable_offline_softlock_reclaim_ui && !!recoveryContext;
    const currentUserName =
        (typeof g_user_name === 'string' && g_user_name.trim().length > 0)
            ? g_user_name
            : 'current user';

    // Create modal HTML
    const modalHtml = `
        <div id="remove-offline-softlock-tab-conflict-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Action Unavailable</h4>
                        <button type="button" class="close" onclick="close_remove_offline_softlock_tab_conflict_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
         <div class="modal-body" style="padding: 10px;">
                        <ul style="list-style: none; padding-left: 10px;">
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                This case was selected for Offline work in a different browser tab or window by you, ${currentUserName}.
                            </li>
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                To remove this case from the Offline Case queue, please return to the original tab or browser window used to select the case for Offline work.
                            </li>
                            ${showRecoveryUi ? `
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                If the original tab or browser window is no longer available, you can reclaim this offline case to the current tab and then try again.
                            </li>
                            ` : ''}
                        </ul>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right; border-top: none;">
                        ${showRecoveryUi ? mmria_get_offline_softlock_recovery_button_html() : ''}
                        <button type="button" class="btn btn-light" onclick="close_remove_offline_softlock_tab_conflict_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Close
                        </button>                        
                    </div>
                </div>
            </div>
        </div>
        <div id="remove-offline-softlock-tab-conflict-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('remove-offline-softlock-tab-conflict-modal');
        const backdrop = document.getElementById('remove-offline-softlock-tab-conflict-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

function show_edit_lock_tab_conflict_modal(caseID) {
    const currentUserName =
        (typeof g_user_name === 'string' && g_user_name.trim().length > 0)
            ? g_user_name
            : 'current user';

    // Create modal HTML
    const modalHtml = `
        <div id="edit-lock-tab-conflict-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Action Unavailable</h4>
                        <button type="button" class="close" onclick="close_edit_lock_tab_conflict_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 10px;">
                        <ul style="list-style: none; padding-left: 10px;">
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                This case is currently being edited in another browser tab or window by you, ${currentUserName}.
                            </li>
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                While you may view the case in this tab, please return to the original tab or browser window to edit this case.
                            </li>
                        </ul>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right; border-top: none;">
                        <button type="button" class="btn btn-light" onclick="close_edit_lock_tab_conflict_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Close
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="edit-lock-tab-conflict-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    setTimeout(() => {
        const modal = document.getElementById('edit-lock-tab-conflict-modal');
        const backdrop = document.getElementById('edit-lock-tab-conflict-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

function show_add_offline_softlock_tab_conflict_modal(caseID) {
    const recoveryContext = mmria_set_offline_softlock_recovery_context('add-offline-softlock-tab-conflict-modal', {
        caseId: caseID,
        caseIds: mmria_get_reclaimable_softlock_case_ids(),
        refreshMode: 'list'
    });
    const showRecoveryUi = mmria_enable_offline_softlock_reclaim_ui && !!recoveryContext;

    // Create modal HTML
    const modalHtml = `
        <div id="add-offline-softlock-tab-conflict-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Offline Mode Blocked</h4>
                        <button type="button" class="close" onclick="close_add_offline_softlock_tab_conflict_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 10px;">
                        <ul style="list-style: none; padding-left: 10px;">
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                Cannot add this case to offline mode from a different browser tab than the one where your other offline cases were added.
                            </li>
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                Please use the original tab where your offline cases were selected or remove those cases from that tab first.
                            </li>
                            ${showRecoveryUi ? `
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                If the original tab is no longer available, reclaim your current offline cases to this tab and then try adding the case again.
                            </li>
                            ` : ''}
                        </ul>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right; border-top: none;">
                        ${showRecoveryUi ? mmria_get_offline_softlock_recovery_button_html() : ''}
                        <button type="button" class="btn btn-light" onclick="close_add_offline_softlock_tab_conflict_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Close
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="add-offline-softlock-tab-conflict-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;

    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);

    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('add-offline-softlock-tab-conflict-modal');
        const backdrop = document.getElementById('add-offline-softlock-tab-conflict-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

function show_go_offline_tab_conflict_modal() {
    const recoveryContext = mmria_set_offline_softlock_recovery_context('go-offline-tab-conflict-modal', {
        caseIds: mmria_get_reclaimable_softlock_case_ids(),
        refreshMode: 'list'
    });
    const showRecoveryUi = mmria_enable_offline_softlock_reclaim_ui && !!recoveryContext;

    // Create modal HTML
    const modalHtml = `
        <div id="go-offline-tab-conflict-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Offline Mode Blocked</h4>
                        <button type="button" class="close" onclick="close_go_offline_tab_conflict_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 10px;">
                        <ul style="list-style: none; padding-left: 10px;">
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                Cannot go into offline mode with cases added in another browser tab. Please try this tab from the original tab.
                            </li>
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                Please return to the tab where the offline cases were selected and start offline mode there.
                            </li>
                            ${showRecoveryUi ? `
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                If that tab is no longer available, reclaim your offline cases to this tab and then start offline mode again.
                            </li>
                            ` : ''}
                        </ul>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right; border-top: none;">
                        ${showRecoveryUi ? mmria_get_offline_softlock_recovery_button_html() : ''}
                        <button type="button" class="btn btn-light" onclick="close_go_offline_tab_conflict_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Close
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="go-offline-tab-conflict-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;

    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);

    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('go-offline-tab-conflict-modal');
        const backdrop = document.getElementById('go-offline-tab-conflict-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

function show_edit_offline_case_tab_conflict_modal(caseID) {
    const isProcessingOfflineCases = localStorage.getItem('process_offline_cases') === 'true';
    const recoveryContext = mmria_set_offline_softlock_recovery_context('edit-offline-case-tab-conflict-modal', {
        caseId: caseID,
        caseIds: isProcessingOfflineCases ? [] : [caseID],
        refreshMode: 'none'
    });
    const showRecoveryUi = mmria_enable_offline_softlock_reclaim_ui && !!recoveryContext;

    // Create modal HTML
    const modalHtml = `
        <div id="edit-offline-case-tab-conflict-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Offline Mode Blocked</h4>
                        <button type="button" class="close" onclick="close_edit_offline_case_tab_conflict_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 10px;">
                        <ul style="list-style: none; padding-left: 10px;">
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                Cannot edit this case from a different browser tab than the one where it was added to offline mode.
                            </li>
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                Please use the original tab where this case was added to offline mode or remove it from offline mode there first.
                            </li>
                            ${(!isProcessingOfflineCases && showRecoveryUi) ? `
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                If the original tab is no longer available, reclaim this offline case to the current tab and then try editing again.
                            </li>
                            ` : ''}
                        </ul>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right; border-top: none;">
                        ${showRecoveryUi ? mmria_get_offline_softlock_recovery_button_html() : ''}
                        <button type="button" class="btn btn-light" onclick="close_edit_offline_case_tab_conflict_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Close
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="edit-offline-case-tab-conflict-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    setTimeout(() => {
        const modal = document.getElementById('edit-offline-case-tab-conflict-modal');
        const backdrop = document.getElementById('edit-offline-case-tab-conflict-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

function close_remove_offline_softlock_tab_conflict_modal() {
    const modal = document.getElementById('remove-offline-softlock-tab-conflict-modal');
    const backdrop = document.getElementById('remove-offline-softlock-tab-conflict-backdrop');
    mmria_clear_offline_softlock_recovery_context('remove-offline-softlock-tab-conflict-modal');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) {
                modal.parentNode.removeChild(modal);
            }
            if (backdrop.parentNode) {
                backdrop.parentNode.removeChild(backdrop);
            }
        }, 150);
    }
}

function close_edit_lock_tab_conflict_modal() {
    const modal = document.getElementById('edit-lock-tab-conflict-modal');
    const backdrop = document.getElementById('edit-lock-tab-conflict-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) {
                modal.parentNode.removeChild(modal);
            }
            if (backdrop.parentNode) {
                backdrop.parentNode.removeChild(backdrop);
            }
        }, 150);
    }
}

function close_add_offline_softlock_tab_conflict_modal() {
    const modal = document.getElementById('add-offline-softlock-tab-conflict-modal');
    const backdrop = document.getElementById('add-offline-softlock-tab-conflict-backdrop');
    mmria_clear_offline_softlock_recovery_context('add-offline-softlock-tab-conflict-modal');

    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');

        setTimeout(() => {
            if (modal.parentNode) {
                modal.parentNode.removeChild(modal);
            }
            if (backdrop.parentNode) {
                backdrop.parentNode.removeChild(backdrop);
            }
        }, 150);
    }
}

function close_go_offline_tab_conflict_modal() {
    const modal = document.getElementById('go-offline-tab-conflict-modal');
    const backdrop = document.getElementById('go-offline-tab-conflict-backdrop');
    mmria_clear_offline_softlock_recovery_context('go-offline-tab-conflict-modal');

    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');

        setTimeout(() => {
            if (modal.parentNode) {
                modal.parentNode.removeChild(modal);
            }
            if (backdrop.parentNode) {
                backdrop.parentNode.removeChild(backdrop);
            }
        }, 150);
    }
}

function close_edit_offline_case_tab_conflict_modal() {
    const modal = document.getElementById('edit-offline-case-tab-conflict-modal');
    const backdrop = document.getElementById('edit-offline-case-tab-conflict-backdrop');
    mmria_clear_offline_softlock_recovery_context('edit-offline-case-tab-conflict-modal');

    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');

        setTimeout(() => {
            if (modal.parentNode) {
                modal.parentNode.removeChild(modal);
            }
            if (backdrop.parentNode) {
                backdrop.parentNode.removeChild(backdrop);
            }
        }, 150);
    }
}

function show_case_locked_by_another_user_modal(caseID, username) {
    // Create modal HTML
    const modalHtml = `
        <div id="case-locked-by-another-user-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Locked Case</h4>
                        <button type="button" class="close" onclick="close_case_locked_by_another_user_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 10px;">
                        <ul style="list-style: none; padding-left: 10px;">
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                This case is currently being edited by ${username}. Please wait for the case to be released.                                
                            </li>                                                                     
                        </ul>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right; border-top: none;">
                        <button type="button" class="btn btn-light" onclick="close_case_locked_by_another_user_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Close
                        </button>                        
                    </div>
                </div>
            </div>
        </div>
        <div id="case-locked-by-another-user-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('case-locked-by-another-user-modal');
        const backdrop = document.getElementById('case-locked-by-another-user-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

function close_case_locked_by_another_user_modal() {
    const modal = document.getElementById('case-locked-by-another-user-modal');
    const backdrop = document.getElementById('case-locked-by-another-user-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) {
                modal.parentNode.removeChild(modal);
            }
            if (backdrop.parentNode) {
                backdrop.parentNode.removeChild(backdrop);
            }
        }, 150);
    }
}


function Show_Confirm_Delete_Case(p_index) {
  const case_list = g_ui.case_view_list;
  const p_values = case_list[p_index];
  const lastName = p_values.value.last_name;
  const firstName = p_values.value.first_name;
  const lastUpdatedBy = p_values.value.last_updated_by;

  const dateLastUpdated = new Date(p_values.value.date_last_updated);
  const mm = (dateLastUpdated.getMonth() + 1).toString().length === 1
    ? `0${dateLastUpdated.getMonth() + 1}`
    : dateLastUpdated.getMonth() + 1;
  const dd = dateLastUpdated.getDate().toString().length === 1
    ? `0${dateLastUpdated.getDate()}`
    : dateLastUpdated.getDate();
  const yyyy = dateLastUpdated.getFullYear().toString().length === 1
    ? `0${dateLastUpdated.getFullYear()}`
    : dateLastUpdated.getFullYear();
  const hhmmss = get24HourFormat(dateLastUpdated.toLocaleTimeString());

  const modalHtml = `
    <div id="delete-case-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
      <div class="modal-dialog modal-lg" role="document">
        <div class="modal-content" style="width: 500px; margin: auto;">
          <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
            <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size: 17px;">Confirm Delete Case</h4>
            <button type="button" class="close" onclick="dispose_all_modals()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
              <span aria-hidden="true">&times;</span>
            </button>
          </div>
          <div class="modal-body" style="margin: 10px; padding: 10px; display: flex; align-items: flex-start; gap: 10px;">
            <img src="./img/offline-warn.svg" alt="Go Online Alert">
            <ul style="list-style: none; padding-left: 10px; flex: 1;">
              <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;" id="confirm_delete_case_message">Are you sure you want to delete this case?</li>
              <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;"><b>${lastName}, ${firstName}</b></li>
              <li id="delete_last_update_date" style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;" id="confirm_delete_case_last_updated">Last updated: ${lastUpdatedBy} ${mm}/${dd}/${yyyy} ${hhmmss}</li>
            </ul>
          </div>
          <div class="modal-footer" style="padding: 20px 30px; text-align: right; border-top: none;">
            <button id="confirm_delete_case_button" type="button" class="modal-confirm btn btn-primary flex-order-1 ml-0 mr-1" onclick="delete_record_async(${p_index})">Delete</button>
            <button id="cancel_delete_case_button" type="button" class="modal-cancel btn btn btn-outline-secondary flex-order-2 mr-0" data-dismiss="modal" onclick="dispose_all_modals()">Cancel</button>
            <button id="ok_delete_case_button" type="button" class="modal-confirm btn btn-primary flex-order-1 ml-0 mr-1 d-none" onclick="dispose_all_modals()">Ok</button>
          </div>
        </div>
      </div>
    </div>
    <div id="delete-case-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
  `;

  document.body.insertAdjacentHTML('beforeend', modalHtml);

  setTimeout(() => {
    const modal = document.getElementById('delete-case-modal');
    const backdrop = document.getElementById('delete-case-backdrop');
    if (modal && backdrop) {
      modal.classList.add('show');
      modal.style.display = 'block';
      backdrop.classList.add('show');
    }
  }, 10);
}


async function delete_record_async(p_index) {
    var data = g_ui.case_view_list[p_index];

    g_selected_delete_index = null;

    const confirm_message_element = document.getElementById('confirm_delete_case_message');
    if (confirm_message_element) {
        confirm_message_element.textContent = 'Deleting...';
    }

    const pad2 = (n) => (n.toString().length === 1 ? '0' + n : n.toString());
    const format_timestamp = (d) => {
        const month = d.getUTCMonth() + 1;
        const day = d.getUTCDate();
        const year = d.getUTCFullYear();
        const hour = pad2(d.getUTCHours());
        const min = pad2(d.getUTCMinutes());
        const second = pad2(d.getUTCSeconds());
        return `${month}/${day}/${year} ${hour}:${min}:${second}`;
    };

    const user_name =
        (typeof g_user_name === 'string' && g_user_name.trim().length > 0)
            ? g_user_name
            : (document.getElementById('user_logged_in')
                ? document.getElementById('user_logged_in').innerText
                : '');

    try {
        const case_response = await $.ajax({
            url:
                location.protocol +
                '//' +
                location.host +
                '/api/case?case_id=' +
                data.id,
            dataType: 'json'
        });

        try {
            await delete_case(case_response._id, case_response._rev);

            if (confirm_message_element) {
                confirm_message_element.textContent = `Deleted By ${user_name} ${format_timestamp(new Date())}`;
                confirm_message_element.style.color = '#8f0000';
                const confirm_delete_case_button = document.getElementById('confirm_delete_case_button');
                const cancel_delete_case_button = document.getElementById('cancel_delete_case_button');
                const ok_delete_case_button = document.getElementById('ok_delete_case_button');
                const delete_last_update_date_element = document.getElementById('delete_last_update_date');
                if (delete_last_update_date_element) delete_last_update_date_element.classList.add('d-none');
                if (confirm_delete_case_button) confirm_delete_case_button.classList.add('d-none');
                if (cancel_delete_case_button) cancel_delete_case_button.classList.add('d-none');
                if (ok_delete_case_button) ok_delete_case_button.classList.remove('d-none');
            }
        } catch (xhr) {
            UpdateDeleteCaseModal();
            const status = xhr && typeof xhr.status === 'number' ? xhr.status : null;
            if (confirm_message_element) {
                if (status === 409) {
                    const case_list = g_ui.case_view_list;
                    const p_values = case_list[p_index];
                    const last_checked_out_by = p_values.value.last_checked_out_by;                    

                    confirm_message_element.textContent = `This case is currently being edited by (${last_checked_out_by}). Please wait for the case to be released.`;
                } else {
                    confirm_message_element.textContent = 'Unable to delete case.';

                }
            }

            console.error('Unable to delete case:', xhr);
        }
    } catch (ex) {
        UpdateDeleteCaseModal();
        if (confirm_message_element) {
            confirm_message_element.textContent = 'Unable to load case for delete.';
        }
        console.error('Unable to load case for delete:', ex);
    }
}

function UpdateDeleteCaseModal(){
    const confirm_delete_case_button = document.getElementById('confirm_delete_case_button');
    const cancel_delete_case_button = document.getElementById('cancel_delete_case_button');
    const ok_delete_case_button = document.getElementById('ok_delete_case_button');
    const delete_last_update_date_element = document.getElementById('delete_last_update_date');
    if (delete_last_update_date_element) delete_last_update_date_element.classList.add('d-none');
    if (confirm_delete_case_button) confirm_delete_case_button.classList.add('d-none');
    if (cancel_delete_case_button) cancel_delete_case_button.classList.add('d-none');
    if (ok_delete_case_button) ok_delete_case_button.classList.remove('d-none');    
}
