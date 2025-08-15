var g_user_audit_history = [];
const ACTION_TYPE = {
    ADD_USER: 'add_user',
    DELETE_USER: 'delete_user',
    ADD_ROLE: 'add_role',
    DELETE_ROLE: 'delete_role',
    EDIT_ROLE: 'edit_role',
    EDIT_PASSWORD: 'edit_password',
    EDIT_USERNAME: 'edit_username',
}
var init_audit_history_object = null;

var g_audit_first_index  = 0;
var g_audit_last_index = 10;
var g_audit_current_page_number = 1;
var g_total_audit = 0;
var g_total_audits_per_page = 10;
var g_filtered_audit_list = [];
var g_is_audit_log_view = false;

const empty_assigned_roles_html = '<tr id="no-roles-placeholder" class="text-center"><td colspan="6">No roles assigned.</td></tr>';

function init_audit_history() 
{
    g_user_audit_history = [];
    init_audit_history_object = null;
    sort_audit_history_list();
    reset_audit_pagination();
}

function reset_audit_pagination()
{
    g_audit_first_index = g_filtered_audit_list.length === 0 ? -1 : 0;
    g_audit_last_index = g_total_audits_per_page;
    g_audit_current_page_number = 1;
    g_total_audit = g_filtered_audit_list.length;
    if (g_total_audit < g_total_audits_per_page) {
        g_audit_last_index = g_total_audit;
    }
}

function add_to_audit_history(p_user_id, p_elem_id, p_action, p_prev_val, p_val, p_data_id)
{
    const element = document.getElementById(p_elem_id);
    if (element) {
        element.dataset.previousValue = p_val;
    }
    g_user_audit_history.push(create_audit_object(p_user_id, p_elem_id, p_action, p_prev_val, p_val, p_data_id));
    set_audit_history_undo_button_enable_state(true);
}

function create_audit_object(p_user_id, p_elem_id, p_action, p_prev_val, p_val, p_data_id)
{
    var parent_id = '';
    if(user_roles && user_roles.length > 0)
        parent_id =  user_roles.find(role => role._id === p_elem_id.split("_")[0])?.role_name || '';
    if(p_action === ACTION_TYPE.DELETE_ROLE)
    {
        parent_id = p_prev_val;
        p_val = '';
    }
    return {
        _id: $mmria.get_new_guid(),
        user_id: p_user_id,
        action: p_action,
        element_id: p_elem_id,
        old_value: p_prev_val,
        new_value: p_val,
        field: '',
        date_created: new Date(),
        created_by: g_userName,
        parent_id: parent_id,
        data_id: p_data_id,
        data_type: "audit_history"
    };
}

function set_audit_history_undo_button_enable_state(p_is_enabled)
{
    const undoButton = document.getElementById('undo_button');
    undoButton.disabled = !p_is_enabled;
    undoButton.setAttribute('aria-disabled', p_is_enabled ? 'false' : 'true');
}

function audit_history_undo() 
{
    const last_audit = g_user_audit_history.pop();
    if (last_audit) {
        const role = user_roles.find(role => role._id === last_audit.element_id.split('_')[0]);
        switch (last_audit.action) 
        {
            case ACTION_TYPE.ADD_ROLE:
                user_roles.splice(user_roles.findIndex(r => r._id === last_audit.element_id), 1);
                document.getElementById(`${last_audit.element_id}_role`).remove();
                if (user_roles.length <= 0) {
                    document.getElementById("user_roles").innerHTML = empty_assigned_roles_html;
                }
                break;
            case ACTION_TYPE.DELETE_ROLE:
                if (deleted_user_roles.length > 0) {
                    const restored_role = deleted_user_roles.pop();
                    user_roles.push(restored_role.role);
                    const userRolesElement = document.getElementById('user_roles');
                    userRolesElement.insertAdjacentHTML('beforeend', restored_role.html);
                    const placeholder = document.getElementById('no-roles-placeholder');
                    if (placeholder) {
                        placeholder.remove();
                    }
                }
                break;
            case ACTION_TYPE.EDIT_ROLE:
                if (role) {
                    role.role_name = last_audit.old_value;
                    const element = document.getElementById(last_audit.element_id);
                    if (element) {
                        element.new_value = last_audit.old_value;
                        element.dataset.previousValue = last_audit.old_value;
                    }
                    if(last_audit.element_id.includes('role_active_status')) {
                        const newValueRadio = document.querySelector(`input[name="${last_audit.element_id}"][value="${last_audit.new_value}"]`);
                        const prevValueRadio = document.querySelector(`input[name="${last_audit.element_id}"][value="${last_audit.old_value}"]`);
                        if (newValueRadio) newValueRadio.checked = false;
                        if (prevValueRadio) prevValueRadio.checked = true;
                    }
                }
                break;
            case ACTION_TYPE.EDIT_PASSWORD:
            case ACTION_TYPE.EDIT_USERNAME:
                const element = document.getElementById(last_audit.element_id);
                if (element) {
                    element.new_value = last_audit.old_value;
                    element.dataset.previousValue = last_audit.old_value;
                }
                break;
        }
        set_audit_history_undo_button_enable_state(g_user_audit_history.length > 0);
        if (last_audit.action === ACTION_TYPE.EDIT_ROLE) {
            assigned_roles_validation_check();
        }
    }
}

async function save_audit_history(p_user_id = "")
{
    const audit_history_to_send = create_audit_history();
    const audit_history_url = 'api/_audit/audit-manage-user';
    var formatted_user_id = "";
    g_manage_user_audit.items.push(...audit_history_to_send);
    if(p_user_id !== "")
        formatted_user_id = p_user_id.includes("org.couchdb.user:") ? p_user_id : `org.couchdb.user:${p_user_id}`;
    else
        formatted_user_id = "";
    const response = await get_http_post_response(audit_history_url, g_manage_user_audit);
     if(response)
    {
        var response_obj = eval(response);
        if(response_obj.ok)
        {
            g_manage_user_audit._rev = response_obj._rev;
            if(formatted_user_id !== "")
                view_user_click(formatted_user_id);
        }
    }
}

function mock_audit_history_push()
{
    const audit_history = create_audit_history();
    audit_history.forEach(audit => {
        g_manage_user_audit.items.push(audit);
    });
}

function create_initial_audit(p_user_id, p_elem_id, p_action, p_prev_val, p_val, p_data_id)
{
    init_audit_history_object = create_audit_object(p_user_id, p_elem_id, p_action, p_prev_val, p_val, p_data_id);
}

function create_audit_history()
{
    var audit_history_id_set = new Set();
    var audit_history_set = new Set();
    if(init_audit_history_object)
        sanitize_audit_history_for_first_audit();
    else
        sanitize_audit_history();
    var temp_audit_history = init_audit_history_object ? [init_audit_history_object, ...g_user_audit_history] : [...g_user_audit_history];
    while (temp_audit_history.length > 0)
    {
        var audit = temp_audit_history.pop();
        var audit_set_object = 
        {
            element_id: audit.element_id,
            action: audit.action,
            audit_data: audit
        }
        if(!audit_history_id_set.has(audit_set_object.element_id + audit_set_object.action))
        {
            if(initial_user_roles)
            {
                audit_history_id_set.add(audit_set_object.element_id + audit_set_object.action);
                audit_history_set.add(audit_set_object.audit_data);
            }
        }
    }
    return create_finalized_audit_history(audit_history_set);
}

function create_finalized_audit_history(p_audit_history_set)
{
    var finalized_audit_history_set = [];
    [...p_audit_history_set].forEach(audit => {
        audit.user_id = role_user_name;
        if ((audit.action === ACTION_TYPE.EDIT_ROLE || audit.action === ACTION_TYPE.ADD_ROLE))
            audit.parent_id = user_roles.find(role => role._id === audit.element_id.split('_')[0]).role_name;
        var initial_user_role = initial_user_roles.find(role => role._id === audit.element_id.split('_')[0]);
        if(initial_user_role)
        {
            audit.field = get_updated_field_path(audit);
            if(audit.action !== ACTION_TYPE.DELETE_ROLE && audit.action !== ACTION_TYPE.ADD_ROLE && audit.action !== ACTION_TYPE.ADD_USER && !init_audit_history_object)
                audit.old_value = initial_user_role[audit.data_id].toString();
            else
                audit.old_value = "";
            if(audit.old_value.toString() !== audit.new_value.toString() && (initial_user_role[audit.data_id].toString() !== audit.new_value.toString()))
                finalized_audit_history_set.push(audit);
            else if (audit.action === ACTION_TYPE.DELETE_ROLE)
                finalized_audit_history_set.push(audit);
        }
        else
        {
            audit.old_value = "";
            audit.field = get_updated_field_path(audit);
            if(audit.action === ACTION_TYPE.EDIT_ROLE || audit.action === ACTION_TYPE.ADD_ROLE)
            {
                finalized_audit_history_set.push(audit);
            }
            else if (audit.action === ACTION_TYPE.ADD_USER)
            {
                audit.new_value = role_user_name;
                finalized_audit_history_set.push(audit);
            }
            else if (((audit.action === ACTION_TYPE.EDIT_PASSWORD && audit.element_id === 'user_password') || audit.action === ACTION_TYPE.DELETE_USER) && !init_audit_history_object)
            {
                audit.new_value = "";
                finalized_audit_history_set.push(audit);
            }
        }
    });
    finalized_audit_history_set.forEach(audit => {
        delete audit.element_id;
    });
    return finalized_audit_history_set;
}

function sanitize_audit_history_for_first_audit()
{
    if(deleted_user_roles && deleted_user_roles.length > 0)
    {
        deleted_user_roles.forEach(deleted_role => {
            g_user_audit_history = g_user_audit_history.filter(audit => audit.element_id.split("_")[0] !== deleted_role._id);
        });
    }
}

function sanitize_audit_history()
{
    var deleted_audit_history_id_set = new Set();
    g_user_audit_history.forEach(audit => {
        if(audit.action === ACTION_TYPE.DELETE_ROLE)
            deleted_audit_history_id_set.add(audit.element_id);
    });
    if(initial_user_roles && initial_user_roles.length > 0)
    {
        deleted_audit_history_id_set.forEach(id => {
            const initial_user_role = initial_user_roles.find(role => role._id === id);
            if(initial_user_role === undefined)
            {
                g_user_audit_history = g_user_audit_history.filter(audit => audit.element_id.split("_")[0] !== id);
            }
        });
    }
}

function first_audit_page_click()
{
    set_audit_page_navigation('first');
    render_audit_pagination_controls();
    render_audit_summary_table();
}

function previous_audit_page_click()
{
    set_audit_page_navigation('previous');
    render_audit_pagination_controls();
    render_audit_summary_table();
}

function next_audit_page_click()
{
    set_audit_page_navigation('next');
    render_audit_pagination_controls();
    render_audit_summary_table();
}

function last_audit_page_click() 
{
    set_audit_page_navigation('last');
    render_audit_pagination_controls();
    render_audit_summary_table();
}

function render_audit_history_body()
{
    const filtered_audit_list = g_filtered_audit_list.slice(g_audit_first_index, g_audit_last_index);
    if( filtered_audit_list.length === 0)
        return '<tr class="text-center"><td colspan="6">No audit history available.</td></tr>';
    else
        return filtered_audit_list.map(audit => render_account_history_row(audit)).join("");
}

function render_audit_log_history_body()
{
    const filtered_audit_list = g_filtered_audit_list.slice(g_audit_first_index, g_audit_last_index);
    if( filtered_audit_list.length === 0)
        return '<tr class="text-center"><td colspan="7">No audit history available.</td></tr>';
    else
        return filtered_audit_list.map(audit => render_account_log_history_row(audit)).join("");
}

function render_account_log_history_table_navigation_view()
{
        const result = [`
        <div class='d-flex mb-2 mt-2'>
            <div class='ml-auto mr-3 d-flex'>
                <div class='d-flex align-items-center'>
                    <div>Showing ${g_audit_first_index + 1}-${Math.min(g_audit_last_index, g_total_audit)} of ${g_filtered_audit_list.length} cases</div>
                    <div class='row ml-2'>
                    <button onclick="first_audit_page_click()"
                        aria-label="First page results" ${g_audit_first_index <= 0 ? 'disabled="true"' : ''}
                        class='icon-button btn-tab-navigation reverse'>
                        <span class='x24 cdc-icon-chevron-double-right'></span>
                    </button>
                    <button onclick="previous_audit_page_click()"
                        aria-label="Previous page results" ${g_audit_first_index <= 0 ? 'disabled="true"' : ''}
                        class='icon-button btn-tab-navigation reverse'>
                        <span class='x24 cdc-icon-chevron-right'></span>
                    </button>
                    <span tabindex="-1" aria-label="Current page Results" class='icon-button btn-navigation-style'>
                        ${g_audit_current_page_number}
                    </span>
                    <button onclick="next_audit_page_click()"
                        ${g_audit_last_index >= g_filtered_audit_list.length ? 'disabled="true"' : ''}
                        class='icon-button btn-tab-navigation'>
                        <span class='x24 cdc-icon-chevron-right pt-1'></span>
                    </button>
                    <button onclick="last_audit_page_click()"
                        ${g_audit_last_index >= g_filtered_audit_list.length ? 'disabled="true"' : ''}
                        class='icon-button btn-tab-navigation'>
                        <span class='x24 cdc-icon-chevron-double-right pt-1'></span>
                    </button>
                </div>
                </div>
            </div>
        </div>
    `];
    return result.join("");
}

function render_account_history_table_navigation_view()
{
    const result = [`
        <div class='d-flex mb-2 mt-2'>
            <div class='ml-auto mr-3 d-flex'>
                <div class='d-flex align-items-center'>
                    <div>Showing ${g_audit_first_index + 1}-${Math.min(g_audit_last_index, g_total_audit)} of ${g_filtered_audit_list.length} cases</div>
                    <div class='row ml-2'>
                    <button onclick="first_audit_page_click()"
                        aria-label="First page results" ${g_audit_first_index <= 0 ? 'disabled="true"' : ''}
                        class='icon-button btn-tab-navigation reverse'>
                        <span class='x24 cdc-icon-chevron-double-right'></span>
                    </button>
                    <button onclick="previous_audit_page_click()"
                        aria-label="Previous page results" ${g_audit_first_index <= 0 ? 'disabled="true"' : ''}
                        class='icon-button btn-tab-navigation reverse'>
                        <span class='x24 cdc-icon-chevron-right'></span>
                    </button>
                    <span tabindex="-1" aria-label="Current page Results" class='icon-button btn-navigation-style'>
                        ${g_audit_current_page_number}
                    </span>
                    <button onclick="next_audit_page_click()"
                        ${g_audit_last_index >= g_filtered_audit_list.length ? 'disabled="true"' : ''}
                        class='icon-button btn-tab-navigation'>
                        <span class='x24 cdc-icon-chevron-right pt-1'></span>
                    </button>
                    <button onclick="last_audit_page_click()"
                        ${g_audit_last_index >= g_filtered_audit_list.length ? 'disabled="true"' : ''}
                        class='icon-button btn-tab-navigation'>
                        <span class='x24 cdc-icon-chevron-double-right pt-1'></span>
                    </button>
                </div>
                </div>
            </div>
        </div>
    `];
    return result.join("");
}
// {
//     _id: $mmria.get_new_guid(),
//     user_id: p_user_id,
//     action: p_action,
//     old_value: p_prev_val,
//     new_value: p_val,
//     date_created: new Date(),
//     created_by: g_userName,
//     data_id: p_data_id,
//     parent_id: '',
//     data_type: "audit_history"
// }


function render_account_history_row(p_audit)
{
    return `
        <tr>
            <td>${create_date_time_string(p_audit.date_created)}</td>
            <td>${p_audit.created_by}</td>
            <td>${get_action_type(p_audit.action)}</td>
            <td>${p_audit.field}</td>
            <td>${format_value_label(p_audit.old_value)}</td>
            ${
                p_audit.action === ACTION_TYPE.ADD_USER 
                    ? `<td>${p_audit.new_value}</td>`
                    : `<td>${format_value_label(p_audit.new_value)}</td>`
            }
        </tr>
    `
}

function render_account_log_history_row(p_audit)
{
    return `
        <tr>
            <td>${p_audit.user_id}</td>
            <td>${create_date_time_string(p_audit.date_created)}</td>
            <td>${p_audit.created_by}</td>
            <td>${get_action_type(p_audit.action)}</td>
            <td>${p_audit.field}</td>
            <td>${format_value_label(p_audit.old_value)}</td>
            ${
                p_audit.action === ACTION_TYPE.ADD_USER 
                    ? `<td>${p_audit.new_value}</td>`
                    : `<td>${format_value_label(p_audit.new_value)}</td>`
            }
        </tr>
    `
}

function format_value_label(p_value)
{
    if (p_value === null || p_value === undefined || p_value === "")
    {
        return "";
    }
    else if (typeof p_value === "string" || typeof p_value === "boolean")
    {
        if (p_value === "/")
        {
            return "Top Folder";
        }
        else if (isDateFormat(p_value))
        {
            // For YYYY-MM-DD format, create date as local time to avoid timezone shift
            const parts = p_value.split('-');
            const year = parseInt(parts[0], 10);
            const month = parseInt(parts[1], 10) - 1; // Month is 0-based
            const day = parseInt(parts[2], 10);
            const localDate = new Date(year, month, day);
            return localDate.toLocaleDateString('en-US');
        }
        else
        {
            return role_id_to_proper_case(p_value);
        }
    }
    else if (p_value instanceof Date)
    {
        return p_value.toString();
    }
}

function isDateFormat(p_value)
{
    // Check if it matches YYYY-MM-DD format or ISO date format with timezone
    const dateFormatRegex = /^\d{4}-\d{2}-\d{2}$/;
    const isoDateFormatRegex = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{3})?Z?$/;
    return dateFormatRegex.test(p_value) || isoDateFormatRegex.test(p_value);
}

function create_date_time_string(p_date)
{
    var date = new Date(p_date);
    return date.toLocaleDateString('en-US') + " " + date.toLocaleTimeString('en-US');
}

function get_action_type(p_action)
{
    switch (p_action) {
        case ACTION_TYPE.EDIT_ROLE:
            return "Edit Role";
        case ACTION_TYPE.ADD_USER:
            return "Add User";
        case ACTION_TYPE.ADD_ROLE:
            return "Add Role";
        case ACTION_TYPE.DELETE_ROLE:
            return "Delete Role";
        case ACTION_TYPE.EDIT_PASSWORD:
            return "Edit Password";
        case ACTION_TYPE.DELETE_USER:
            return "Delete User";
        default:
            return "Unknown Action";
    }
}

function get_updated_field_path(p_audit)
{
    if (p_audit.action === ACTION_TYPE.EDIT_PASSWORD || p_audit.action === ACTION_TYPE.DELETE_USER)
    {
        return '';
    }
    else if (p_audit.action === ACTION_TYPE.ADD_ROLE || p_audit.action === ACTION_TYPE.DELETE_ROLE)
    {
        return `Assigned Roles / ${role_id_to_proper_case(p_audit.parent_id)}`
    }
    else
    {
        return `Assigned Roles / ${role_id_to_proper_case(p_audit.parent_id)}${p_audit.parent_id === "" ? "" : " / "}${role_id_to_proper_case(p_audit.data_id == 'jurisdiction_id' ? 'Case Folder Access' : p_audit.data_id)}`;
    }
}

function set_audit_page_navigation(buttonType)
{
    const total_pages = Math.ceil(g_total_audit / g_total_audits_per_page);
    switch (buttonType) {
        case 'first':
            g_audit_current_page_number = 1;
            break;
        case 'previous':
            if (g_audit_current_page_number > 1) {
                g_audit_current_page_number--;
            }
            break;
        case 'next':
            if (g_audit_current_page_number < total_pages) {
                g_audit_current_page_number++;
            }
            break;
        case 'last':
            g_audit_current_page_number = total_pages;
            break;
        default:
            console.error("Invalid button type:", buttonType);
            return;
    }
    g_audit_first_index = (g_audit_current_page_number - 1) * g_total_audits_per_page;
    g_audit_last_index = Math.min(g_audit_first_index + g_total_audits_per_page, g_total_audit);
}

function render_audit_pagination_controls()
{
    const pagination_containers = document.querySelectorAll(".audit_navigation_container");
    pagination_containers.forEach(container => {
        container.innerHTML = render_account_history_table_navigation_view();
    });
}

function render_audit_summary_table()
{
    const audit_table_body = document.getElementById("audit_history_table_body");
    if(g_is_audit_log_view)
        audit_table_body.innerHTML = render_audit_log_history_body();
    else
        audit_table_body.innerHTML = render_audit_history_body();
}

function filter_audit_history(p_filter_value)
{
    if(p_filter_value && p_filter_value.length > 0)
    {
        p_filter_value !== "" ? toggle_clear_filter_enabled(true) : toggle_clear_filter_enabled(false);
        g_filtered_audit_list = g_manage_user_audit.items
        .map(audit => {
            const updatedAudit = { ...audit };
            if (updatedAudit.old_value === '/')
            {
                updatedAudit.old_value = 'Top Folder';
            }
            else if (isDateFormat(updatedAudit.old_value))
            {
                // For YYYY-MM-DD format, create date as local time to avoid timezone shift
                const parts = updatedAudit.old_value.split('-');
                const year = parseInt(parts[0], 10);
                const month = parseInt(parts[1], 10) - 1; // Month is 0-based
                const day = parseInt(parts[2], 10);
                const localDate = new Date(year, month, day);
                updatedAudit.old_value = localDate.toLocaleDateString('en-US');
            }
            if (updatedAudit.new_value === '/')
            {
                updatedAudit.new_value = 'Top Folder';
            }
            else if (isDateFormat(updatedAudit.new_value))
            {
                // For YYYY-MM-DD format, create date as local time to avoid timezone shift
                const parts = updatedAudit.new_value.split('-');
                const year = parseInt(parts[0], 10);
                const month = parseInt(parts[1], 10) - 1; // Month is 0-based
                const day = parseInt(parts[2], 10);
                const localDate = new Date(year, month, day);
                updatedAudit.new_value = localDate.toLocaleDateString('en-US');
            }
            return updatedAudit;
        })
        .filter(audit => {
            var date_created =
                new Date(audit.date_created).toLocaleDateString('en-US') +
                " " +
                new Date(audit.date_created).toLocaleTimeString('en-US');
            return g_is_audit_log_view
            ?  audit.action.split("_").join(" ").toLowerCase().includes(p_filter_value.toLowerCase()) ||
                audit.created_by.toLowerCase().includes(p_filter_value.toLowerCase()) ||
                date_created.toString().toLowerCase().includes(p_filter_value.toLowerCase()) ||
                audit.field.toLowerCase().includes(p_filter_value.toLowerCase()) ||
                audit.old_value.toString().toLowerCase().includes(p_filter_value.toLowerCase()) ||
                audit.new_value.toString().toLowerCase().includes(p_filter_value.toLowerCase()) ||
                audit.user_id.toLowerCase().includes(p_filter_value.toLowerCase())
            : audit.user_id === role_user_name && 
                (audit.action.split("_").join(" ").toLowerCase().includes(p_filter_value.toLowerCase()) ||
                audit.created_by.toLowerCase().includes(p_filter_value.toLowerCase()) ||
                date_created.toString().toLowerCase().includes(p_filter_value.toLowerCase()) ||
                audit.field.toLowerCase().includes(p_filter_value.toLowerCase()) ||
                audit.old_value.toString().toLowerCase().includes(p_filter_value.toLowerCase()) ||
                audit.new_value.toString().toLowerCase().includes(p_filter_value.toLowerCase()))
        }).sort((a, b) => new Date(b.date_created) - new Date(a.date_created));
    }
    else
    {
        sort_audit_history_list();
    }
    reset_audit_pagination();
    render_audit_pagination_controls();
    render_audit_summary_table();
}

function clear_audit_filter()
{
    document.getElementById("audit_history_filter").value = "";
    sort_audit_history_list();
    reset_audit_pagination();
    render_audit_pagination_controls();
    render_audit_summary_table();
    toggle_clear_filter_enabled(false);
}

function sort_audit_history_list()
{
    if(g_is_audit_log_view)
        g_filtered_audit_list = g_manage_user_audit.items.sort((a, b) => new Date(b.date_created) - new Date(a.date_created));
    else
        g_filtered_audit_list = g_manage_user_audit.items.filter(audit => audit.user_id === role_user_name).sort((a, b) => new Date(b.date_created) - new Date(a.date_created));
}

function create_initial_audit_document()
{
    return {
        _id: 'audit-manage-user',
        _rev: null,
        doc_type: 'Audit_Manage_User',
        items: [],
        date_created: new Date(),
    };
}