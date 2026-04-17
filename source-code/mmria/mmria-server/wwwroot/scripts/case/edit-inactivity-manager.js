'use strict';

const case_edit_inactivity_config = window.case_edit_inactivity_config || {};
const read_case_edit_inactivity_minutes = (value, defaultValue) => {
  const parsedValue = Number(value);
  return Number.isFinite(parsedValue) ? parsedValue : defaultValue;
};

const edit_inactivity_lock_minutes = Math.max(
  0,
  read_case_edit_inactivity_minutes(case_edit_inactivity_config.lock_minutes, 120)
);
// Despite the legacy config name, this value is interpreted as the absolute
// number of inactivity minutes before the warning modal is shown.
const edit_inactivity_warning_minutes_before_lock = Math.max(
  0,
  Math.min(
    edit_inactivity_lock_minutes,
    read_case_edit_inactivity_minutes(case_edit_inactivity_config.warning_minutes_before_lock, 110)
  )
);
const edit_inactivity_check_interval_ms = 10000;
const raw_case_edit_auto_save_freq_minutes = Number(case_edit_inactivity_config.auto_save_freq_minutes);
const case_edit_auto_save_freq_minutes =
  Number.isFinite(raw_case_edit_auto_save_freq_minutes)
    ? (raw_case_edit_auto_save_freq_minutes < 0 ? 2 : raw_case_edit_auto_save_freq_minutes)
    : 2;
const case_edit_auto_save_interval_ms = case_edit_auto_save_freq_minutes * 60 * 1000;

var g_edit_inactivity_interval = null;
var g_last_edit_activity_at = null;
var g_edit_inactivity_warning_shown = false;
var g_edit_inactivity_warning_dismissed = false;
var g_edit_inactivity_action_in_progress = false;
var g_edit_activity_listeners_attached = false;

const g_edit_activity_document_events = ['keydown', 'pointerdown', 'input'];
const g_edit_activity_window_events = ['scroll'];

function get_edit_inactivity_lock_ms()
{
  return Math.max(0, edit_inactivity_lock_minutes) * 60 * 1000;
}

function get_edit_inactivity_warning_ms()
{
  const warning_minutes = Math.max(0, edit_inactivity_warning_minutes_before_lock);
  return warning_minutes * 60 * 1000;
}

function get_edit_inactivity_duration_text()
{
  const total_minutes = Math.max(0, edit_inactivity_lock_minutes);
  const hours = Math.floor(total_minutes / 60);
  const minutes = total_minutes % 60;
  const parts = [];

  if (hours > 0)
  {
    parts.push(hours === 1 ? '1 hour' : `${hours} hours`);
  }

  if (minutes > 0)
  {
    parts.push(minutes === 1 ? '1 minute' : `${minutes} minutes`);
  }

  if (parts.length === 0)
  {
    return '0 minutes';
  }

  return parts.join(' ');
}

function is_edit_inactivity_warning_modal_open()
{
  return document.getElementById('edit-inactivity-warning-modal') != null;
}

function update_edit_inactivity_timestamp()
{
  g_last_edit_activity_at = Date.now();
  g_edit_inactivity_warning_shown = false;
  g_edit_inactivity_warning_dismissed = false;
}

function on_edit_activity_detected(event)
{
  if (!g_data_is_checked_out || g_edit_inactivity_action_in_progress)
  {
    return;
  }

  const target = event && event.target ? event.target : null;
  if (target && typeof target.closest === 'function' && target.closest('#edit-inactivity-warning-modal'))
  {
    return;
  }

  update_edit_inactivity_timestamp();
}

function attach_edit_activity_listeners()
{
  if (g_edit_activity_listeners_attached)
  {
    return;
  }

  for (const event_name of g_edit_activity_document_events)
  {
    document.addEventListener(event_name, on_edit_activity_detected, true);
  }

  for (const event_name of g_edit_activity_window_events)
  {
    window.addEventListener(event_name, on_edit_activity_detected, true);
  }

  g_edit_activity_listeners_attached = true;
}

function detach_edit_activity_listeners()
{
  if (!g_edit_activity_listeners_attached)
  {
    return;
  }

  for (const event_name of g_edit_activity_document_events)
  {
    document.removeEventListener(event_name, on_edit_activity_detected, true);
  }

  for (const event_name of g_edit_activity_window_events)
  {
    window.removeEventListener(event_name, on_edit_activity_detected, true);
  }

  g_edit_activity_listeners_attached = false;
}

function stop_edit_inactivity_monitoring()
{
  if (g_edit_inactivity_interval != null)
  {
    window.clearInterval(g_edit_inactivity_interval);
    g_edit_inactivity_interval = null;
  }

  detach_edit_activity_listeners();
  g_last_edit_activity_at = null;
  g_edit_inactivity_warning_shown = false;
  g_edit_inactivity_warning_dismissed = false;

  close_edit_inactivity_warning_modal();
}

function start_edit_inactivity_monitoring()
{
  attach_edit_activity_listeners();

  if (g_last_edit_activity_at == null)
  {
    update_edit_inactivity_timestamp();
  }

  if (g_edit_inactivity_interval != null)
  {
    return;
  }

  g_edit_inactivity_interval = window.setInterval(check_edit_inactivity, edit_inactivity_check_interval_ms);
}

function start_case_autosave_timer()
{
  if (case_edit_auto_save_freq_minutes === 0)
  {
    stop_case_autosave_timer();
    return;
  }

  if (g_autosave_interval == null)
  {
    g_autosave_interval = window.setInterval(autosave, case_edit_auto_save_interval_ms);
  }
}

function stop_case_autosave_timer()
{
  if (g_autosave_interval != null)
  {
    window.clearInterval(g_autosave_interval);
    g_autosave_interval = null;
  }
}

function sync_case_autosave_timer()
{
  if (g_data_is_checked_out)
  {
    start_case_autosave_timer();
  }
  else
  {
    stop_case_autosave_timer();
  }
}

function start_edit_mode_auto_timers()
{
  start_case_autosave_timer();
  start_edit_inactivity_monitoring();
}

function stop_edit_mode_auto_timers()
{
  stop_case_autosave_timer();
  stop_edit_inactivity_monitoring();
}

function sync_edit_mode_auto_timers()
{
  sync_case_autosave_timer();

  if (g_data_is_checked_out)
  {
    start_edit_inactivity_monitoring();
  }
  else
  {
    stop_edit_inactivity_monitoring();
  }
}

async function continue_edit_after_inactivity_warning()
{
  if (!g_data || !g_data_is_checked_out || g_edit_inactivity_action_in_progress)
  {
    close_edit_inactivity_warning_modal();
    return;
  }

  g_edit_inactivity_action_in_progress = true;
  update_edit_inactivity_timestamp();
  close_edit_inactivity_warning_modal();

  try
  {
    g_data.date_last_updated = new Date();
    await save_case_and_wait(g_data, null, 'edit_inactivity_continue');
    create_save_message();
  }
  catch (_ex)
  {
    // Existing save queue error handling/modal flow already handles the failure path.
  }
  finally
  {
    g_edit_inactivity_action_in_progress = false;
  }
}

async function release_edit_lock_due_to_inactivity()
{
  if (!g_data || !g_data_is_checked_out || g_edit_inactivity_action_in_progress)
  {
    return;
  }

  g_edit_inactivity_action_in_progress = true;
  const release_tab_id =
    typeof mmria_get_lock_release_tab_id === 'function'
      ? mmria_get_lock_release_tab_id(g_data)
      : g_data.checked_out_by_tab_id;
  const old_date_last_updated = g_data.date_last_updated;
  const old_date_last_checked_out = g_data.date_last_checked_out;
  const old_last_checked_out_by = g_data.last_checked_out_by;
  const old_checked_out_by_tab_id = g_data.checked_out_by_tab_id;

  try
  {
    g_data.date_last_updated = new Date();
    g_data.date_last_checked_out = null;
    g_data.last_checked_out_by = null;
    g_data.checked_out_by_tab_id = release_tab_id;
    g_data_is_checked_out = false;

    stop_edit_mode_auto_timers();
    g_apply_sort(g_metadata, g_data, '', '', '');

    await save_case_and_wait(g_data, null, 'edit_inactivity_lock_release', {
      authRefreshPolicy: 'suppress'
    });
    g_data.checked_out_by_tab_id = null;
    g_render();

    if (typeof mmria_redirect_case_page_to_autologin_summary === 'function')
    {
      mmria_redirect_case_page_to_autologin_summary();
      return;
    }

    show_edit_inactivity_locked_modal();
  }
  catch (_ex)
  {
    if
    (
      typeof g_case_session_autologin_in_progress !== 'undefined' &&
      g_case_session_autologin_in_progress === true
    )
    {
      return;
    }

    g_data.date_last_updated = old_date_last_updated;
    g_data.date_last_checked_out = old_date_last_checked_out;
    g_data.last_checked_out_by = old_last_checked_out_by;
    g_data.checked_out_by_tab_id = old_checked_out_by_tab_id;
    g_data_is_checked_out = true;
    update_edit_inactivity_timestamp();
    start_edit_mode_auto_timers();
    g_render();
  }
  finally
  {
    g_edit_inactivity_action_in_progress = false;
  }
}

function check_edit_inactivity()
{
  if (!g_data_is_checked_out || !g_data || g_edit_inactivity_action_in_progress)
  {
    return false;
  }

  if (g_last_edit_activity_at == null)
  {
    update_edit_inactivity_timestamp();
    return false;
  }

  const inactive_ms = Date.now() - g_last_edit_activity_at;
  const lock_ms = get_edit_inactivity_lock_ms();
  const warning_ms = get_edit_inactivity_warning_ms();

  if (inactive_ms >= lock_ms)
  {
    release_edit_lock_due_to_inactivity();
    return true;
  }

  if (inactive_ms >= warning_ms)
  {
    if (!g_edit_inactivity_warning_shown && !g_edit_inactivity_warning_dismissed)
    {
      show_edit_inactivity_warning_modal();
      g_edit_inactivity_warning_shown = true;
    }

    return true;
  }

  return false;
}

function show_edit_inactivity_warning_modal()
{
  const modalHtml = `
    <div id="edit-inactivity-warning-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
      <div class="modal-dialog modal-lg" role="document">
        <div class="modal-content">
          <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
            <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Inactivity Warning</h4>
            <button type="button" class="close" onclick="cancel_edit_inactivity_warning_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
              <span aria-hidden="true">&times;</span>
            </button>
          </div>
          <div class="modal-body" style="padding: 10px;">
            <ul style="list-style: none; padding-left: 10px;">
              <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                This case will leave edit mode after ${edit_inactivity_lock_minutes} minutes of inactivity.
              </li>
              <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                Select Continue to save and remain in edit mode.
              </li>
            </ul>
          </div>
          <div class="modal-footer" style="padding: 20px 30px; text-align: right; border-top: none;">
            <button type="button" class="btn btn-light" onclick="logout_after_edit_inactivity_warning()" style="margin-right: 10px; padding: 8px 20px;">
              Log out
            </button>
            <button type="button" class="btn btn-primary" onclick="continue_edit_after_inactivity_warning()" style="padding: 8px 20px;">
              Continue
            </button>
          </div>
        </div>
      </div>
    </div>
    <div id="edit-inactivity-warning-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
  `;

  document.body.insertAdjacentHTML('beforeend', modalHtml);

  setTimeout(() => {
    const modal = document.getElementById('edit-inactivity-warning-modal');
    const backdrop = document.getElementById('edit-inactivity-warning-backdrop');
    if (modal && backdrop)
    {
      modal.classList.add('show');
      modal.style.display = 'block';
      backdrop.classList.add('show');
    }
  }, 10);
}

function close_edit_inactivity_warning_modal()
{
  const modal = document.getElementById('edit-inactivity-warning-modal');
  const backdrop = document.getElementById('edit-inactivity-warning-backdrop');

  if (modal && backdrop)
  {
    modal.classList.remove('show');
    backdrop.classList.remove('show');

    setTimeout(() => {
      if (modal.parentNode)
      {
        modal.parentNode.removeChild(modal);
      }

      if (backdrop.parentNode)
      {
        backdrop.parentNode.removeChild(backdrop);
      }
    }, 150);
  }
}

function cancel_edit_inactivity_warning_modal()
{
  g_edit_inactivity_warning_shown = false;
  g_edit_inactivity_warning_dismissed = true;
  close_edit_inactivity_warning_modal();
}

function logout_after_edit_inactivity_warning()
{
  const logoutForm = document.getElementById('profile_form2');

  if (!logoutForm)
  {
    return;
  }

  const submitButton = logoutForm.querySelector('button[type="submit"], input[type="submit"]');
  if (submitButton)
  {
    submitButton.click();
    return;
  }

  if (typeof logoutForm.requestSubmit === 'function')
  {
    logoutForm.requestSubmit();
  }
}

function show_edit_inactivity_locked_modal()
{
  const duration_text = get_edit_inactivity_duration_text();
  const modalHtml = `
    <div id="edit-inactivity-locked-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
      <div class="modal-dialog modal-lg" role="document">
        <div class="modal-content">
          <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
            <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Inactivity</h4>
            <button type="button" class="close" onclick="close_edit_inactivity_locked_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
              <span aria-hidden="true">&times;</span>
            </button>
          </div>
          <div class="modal-body" style="padding: 10px;">
            <ul style="list-style: none; padding-left: 10px;">
              <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                This case was automatically saved after ${duration_text} of inactivity.
              </li>
              <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                Edit mode has ended and the case is now back in view mode.
              </li>
            </ul>
          </div>
          <div class="modal-footer" style="padding: 20px 30px; text-align: right; border-top: none;">
            <button type="button" class="btn primary-button" onclick="close_edit_inactivity_locked_modal()" style="padding: 8px 20px;">
              OK
            </button>
          </div>
        </div>
      </div>
    </div>
    <div id="edit-inactivity-locked-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
  `;

  document.body.insertAdjacentHTML('beforeend', modalHtml);

  setTimeout(() => {
    const modal = document.getElementById('edit-inactivity-locked-modal');
    const backdrop = document.getElementById('edit-inactivity-locked-backdrop');
    if (modal && backdrop)
    {
      modal.classList.add('show');
      modal.style.display = 'block';
      backdrop.classList.add('show');
    }
  }, 10);
}

function close_edit_inactivity_locked_modal()
{
  const modal = document.getElementById('edit-inactivity-locked-modal');
  const backdrop = document.getElementById('edit-inactivity-locked-backdrop');

  if (modal && backdrop)
  {
    modal.classList.remove('show');
    backdrop.classList.remove('show');

    setTimeout(() => {
      if (modal.parentNode)
      {
        modal.parentNode.removeChild(modal);
      }

      if (backdrop.parentNode)
      {
        backdrop.parentNode.removeChild(backdrop);
      }
    }, 150);
  }
}
