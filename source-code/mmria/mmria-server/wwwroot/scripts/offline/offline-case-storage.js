'use strict';

(function () {

const CASE_INDEX_KEY = 'case_index';
const CASE_KEY_PREFIX = 'case_';
const WORKING_SPACE_KILOBYTES = 1000;
const DEFAULT_LOCAL_STORAGE_LIMIT_KILOBYTES = 5000;

function has_local_storage()
{
  try
  {
    return typeof window !== 'undefined' && window.localStorage != null;
  }
  catch (_ex)
  {
    return false;
  }
}

function safe_json_parse(p_value)
{
  try
  {
    return JSON.parse(p_value);
  }
  catch (_ex)
  {
    return null;
  }
}

function get_case_key(p_case_id)
{
  return CASE_KEY_PREFIX + p_case_id;
}

function create_case_index_item(p_data)
{
  return {
    _id: p_data._id,
    _rev: p_data._rev,
    date_created: p_data.date_created,
    created_by: p_data.created_by,
    date_last_updated: p_data.date_last_updated,
    last_updated_by: p_data.last_updated_by
  };
}

function set_case_index(p_case_index)
{
  if (!has_local_storage())
  {
    return;
  }

  window.localStorage.setItem(CASE_INDEX_KEY, JSON.stringify(p_case_index || {}));
}

function create_case_index_from_storage()
{
  const result = {};

  if (!has_local_storage())
  {
    return result;
  }

  for (let index = 0; index < window.localStorage.length; index++)
  {
    const key = window.localStorage.key(index);
    if (!key || !key.startsWith(CASE_KEY_PREFIX) || key === CASE_INDEX_KEY)
    {
      continue;
    }

    const item_object = safe_json_parse(window.localStorage.getItem(key));
    if (item_object && item_object._id != null)
    {
      result[item_object._id] = create_case_index_item(item_object);
    }
  }

  set_case_index(result);
  return result;
}

function get_case_index()
{
  if (!has_local_storage())
  {
    return {};
  }

  let result = safe_json_parse(window.localStorage.getItem(CASE_INDEX_KEY));

  if (result == null)
  {
    result = create_case_index_from_storage();
  }
  else if
  (
    Object.keys(result).length === 0 &&
    result.constructor === Object &&
    window.localStorage.length > 0
  )
  {
    result = create_case_index_from_storage();
  }

  return result || {};
}

function get_local_storage_space_usage_in_kilobytes()
{
  if (!has_local_storage())
  {
    return 0;
  }

  let all_strings = '';

  for (let index = 0; index < window.localStorage.length; index++)
  {
    const key = window.localStorage.key(index);
    if (!key)
    {
      continue;
    }

    const value = window.localStorage.getItem(key);
    if (value != null)
    {
      all_strings += value;
    }
  }

  return all_strings ? 3 + (all_strings.length * 16) / (8 * 1024) : 0;
}

function calc_local_storage_space_usage_in_kilobytes(p_string)
{
  return p_string ? 3 + (p_string.length * 16) / (8 * 1024) : 0;
}

function convert_case_index_to_array(p_case_index)
{
  const result = [];
  const case_index = p_case_index || {};

  for (const key in case_index)
  {
    if (!Object.prototype.hasOwnProperty.call(case_index, key))
    {
      continue;
    }

    const item = Object.assign({}, case_index[key]);
    const item_object = safe_json_parse(window.localStorage.getItem(get_case_key(key)));

    if (item_object == null)
    {
      continue;
    }

    if (!(item.date_last_updated instanceof Date))
    {
      item.date_last_updated = new Date(item.date_last_updated);
    }

    item.size_in_kilobytes = calc_local_storage_space_usage_in_kilobytes(JSON.stringify(item_object));
    result.push(item);
  }

  result.sort(function (p_left, p_right)
  {
    return p_left.date_last_updated - p_right.date_last_updated;
  });

  return result;
}

function ensure_capacity()
{
  if (!has_local_storage())
  {
    return;
  }

  if
  (
    DEFAULT_LOCAL_STORAGE_LIMIT_KILOBYTES - get_local_storage_space_usage_in_kilobytes() >=
    WORKING_SPACE_KILOBYTES
  )
  {
    return;
  }

  const case_index = create_case_index_from_storage();
  const case_index_array = convert_case_index_to_array(case_index);
  let space_removed = 0;
  let did_update_case_index = false;

  for
  (
    let index = 0;
    index < case_index_array.length && space_removed < WORKING_SPACE_KILOBYTES;
    index++
  )
  {
    const item = case_index_array[index];
    const key = item._id;

    try
    {
      delete case_index[key];
      space_removed += item.size_in_kilobytes;
      did_update_case_index = true;
      window.localStorage.removeItem(get_case_key(key));
    }
    catch (_ex)
    {
      // best effort only
    }
  }

  if (did_update_case_index)
  {
    set_case_index(case_index);
  }
}

function set_case(p_data, p_call_back)
{
  try
  {
    if (!has_local_storage() || p_data == null || p_data._id == null)
    {
      return;
    }

    ensure_capacity();

    const case_index = get_case_index();
    case_index[p_data._id] = create_case_index_item(p_data);
    set_case_index(case_index);

    window.localStorage.setItem(get_case_key(p_data._id), JSON.stringify(p_data));
  }
  catch (ex)
  {
    console.error('OfflineCaseStorage.setCase failed:', ex);
  }
  finally
  {
    if (typeof p_call_back === 'function')
    {
      p_call_back();
    }
  }
}

function get_case(p_case_id)
{
  if (!has_local_storage() || p_case_id == null)
  {
    return null;
  }

  return safe_json_parse(window.localStorage.getItem(get_case_key(p_case_id)));
}

function clear_case(p_case_id)
{
  if (!has_local_storage() || p_case_id == null)
  {
    return;
  }

  try
  {
    window.localStorage.removeItem(get_case_key(p_case_id));

    const case_index = get_case_index();
    if (case_index && case_index[p_case_id])
    {
      delete case_index[p_case_id];
      set_case_index(case_index);
    }
  }
  catch (ex)
  {
    console.error('OfflineCaseStorage.clearCase failed:', ex);
  }
}

function clear_all_cases()
{
  if (!has_local_storage())
  {
    return;
  }

  try
  {
    const keys_to_remove = [];

    for (let index = 0; index < window.localStorage.length; index++)
    {
      const key = window.localStorage.key(index);
      if (key && key.startsWith(CASE_KEY_PREFIX))
      {
        keys_to_remove.push(key);
      }
    }

    for (const key of keys_to_remove)
    {
      window.localStorage.removeItem(key);
    }

    window.localStorage.removeItem(CASE_INDEX_KEY);
  }
  catch (ex)
  {
    console.error('OfflineCaseStorage.clearAllCases failed:', ex);
  }
}

window.OfflineCaseStorage = {
  setCase: set_case,
  getCase: get_case,
  clearCase: clear_case,
  clearAllCases: clear_all_cases
};

})();
