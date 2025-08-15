let g_filtered_user_list = [];
let g_role_set = [];    
let g_sort_order = "ascending"; 

function summary_render() 
{
    let result = [];
    g_role_set = get_role_list();
    g_filtered_user_list = [...g_ui.user_summary_list];
    initial_user_roles = [];
    reset_pagination();
    result.push(`
        <div class='d-flex pb-2'>
            <select id="role_filter" onchange="filter_by_role(this.value)" aria-label="Select role to filter by" class='form-control col-3'>
                ${role_filter_options_renderer()}
            </select>
            <span class="pt-2" style="margin-left: 12px;">OR</span>
            <div class='vertical-control col-md-3'>
                <div class='input-group'>
                    <input id="username_filter" onkeyup="filter_by_username(this.value)" autocomplete='off' class='form-control' type='text' placeholder='Filter by username...'>
                </div>
            </div>
            <button disabled aria-disabled="true" id="clear_filter_button" onclick="clear_filter()" class="btn btn-link mb-2 pl-0" aria-label="clear filter" value="clear filter">Clear filter</button>
        </div>
        <div class="pagination-container">
            ${render_user_table_navigation()}
        </div>
        <div id="user_summary_table" style='clear:both;'>
            ${render_user_table()}
        </div>
        <div class="pagination-container">
            ${render_user_table_navigation()}
        </div>
    `);
    show_hide_user_management_back_button(false);
    set_page_title('Manage Users');
    init_audit_history();
    document.getElementById('form_content_id').innerHTML = result.join("");
}

function role_filter_options_renderer()
{
    const temp_result = [];
    g_role_set.forEach(role => {
        if (role === "") {
            temp_result.push("<option selected>Filter by Role</option>");
        } else {
            temp_result.push("<option value='" + role + "'>");
            var role_name = role.split('_');
            role_name = role_name.map(section => {
                if (section === 'steve' || section === 'mmria' || section === 'prams') {
                    return section.toUpperCase();
                } else {
                    return section[0].toUpperCase() + section.slice(1);
                }
            });
            temp_result.push(role_name.join(" "));
            temp_result.push("</option>");
        }
    });
    return temp_result.join("");
}

function toggle_clear_filter_enabled(p_is_enabled)
{
    const clearFilterButton = document.querySelector("#clear_filter_button");
    if (p_is_enabled) {
        clearFilterButton.removeAttribute("disabled");
        clearFilterButton.setAttribute("aria-disabled", "false");
        clearFilterButton.classList.remove("disabled");
    } else {
        clearFilterButton.setAttribute("disabled", "true");
        clearFilterButton.setAttribute("aria-disabled", "true");
        clearFilterButton.classList.add("disabled");
    }
}

function clear_filter()
{
    g_filtered_user_list = [...g_ui.user_summary_list];
    document.getElementById("username_filter").value = "";
    document.getElementById("role_filter").value = "Filter by Role";
    reset_pagination();
    render_pagination_controls();
    render_user_summary_table();
    toggle_clear_filter_enabled(false);
}

function filter_by_username(selectedValue)
{
    const usernameFilterInput = document.getElementById("username_filter");
    const currentFilterValue = usernameFilterInput.value;
    usernameFilterInput.value = currentFilterValue;
    if (selectedValue === "") {
        g_filtered_user_list = [...g_ui.user_summary_list];
    } else {
        document.getElementById("role_filter").value = "Filter by Role";
        g_filtered_user_list = g_ui.user_summary_list.filter(user => {
            return user.name.toLowerCase().includes(selectedValue.toLowerCase());
        });
    }
    selectedValue !== "" ? toggle_clear_filter_enabled(true) : toggle_clear_filter_enabled(false);
    reset_pagination();
    render_user_summary_table();
    render_pagination_controls();
}

function filter_by_role(selectedValue) {
    document.getElementById("username_filter").value = "";
    console.log(`Selected Role: ${selectedValue}`);
    if (selectedValue === "Filter by Role") {
        console.log("No specific role selected. Resetting filter...");
        g_filtered_user_list = [...g_ui.user_summary_list];
    } else {
        g_filtered_user_list = [];
        console.log(`Filtering users by role: ${selectedValue}`);
        const filter_jurisdiction = g_jurisdiction_list.filter(jurisdiction => {
            return jurisdiction.role_name === selectedValue && jurisdiction.user_id !== "" && jurisdiction.user_id !== null;
        });
        g_filtered_user_list = g_ui.user_summary_list.filter(user => {
            return filter_jurisdiction.some(jurisdiction => jurisdiction.user_id.endsWith(user.name) || jurisdiction.user_id === user.name);
        });
        console.log(`Filtered Users:`, g_filtered_user_list);
    }
    selectedValue !== "Filter by Role" ? toggle_clear_filter_enabled(true) : toggle_clear_filter_enabled(false);
    reset_pagination();
    render_user_summary_table();
    render_pagination_controls();
}

function render_user_table()
{
    const filtered_user_list = g_filtered_user_list.slice(g_first_index, g_last_index);
    let result = [`
        <table class='table table-layout-fixed align-cell-top'>
            <caption class='table-caption'>User management table</caption>
            <thead>
                <tr class='header-level-2 sticky-header z-index-top'>
                    <th ${g_sort_order === 'ascending' ? 'aria-sort="ascending"' : 'aria-sort="descending"'} width='275'>
                        Username (Email Address)
                    </th>
                    <th>Role(s)</th>
                    <th width='250'>Actions</th>
                </tr>
            </thead>
            <tbody>
    `];
    for (var i = 0; i < filtered_user_list.length; i++) {
        var item = filtered_user_list[i];
        if (item._id != "org.couchdb.user:mmrds") {
            Array.prototype.push.apply(result, user_entry_render(item, g_role_set));
        }
    }
    result.push("</tbody></table>");
    return result.join("");
}

function sort_user_list(event) {
    if (event && event.type === "keydown" && event.key !== "Enter") {
        return; // Exit if it's not the Enter key
    }
    g_sort_order = g_sort_order === "ascending" ? "descending" : "ascending";
    g_filtered_user_list.sort((a, b) => {
        const nameA = a.name.toLowerCase();
        const nameB = b.name.toLowerCase();
        if (g_sort_order === "ascending") {
            return nameA < nameB ? -1 : nameA > nameB ? 1 : 0;
        } else {
            return nameA > nameB ? -1 : nameA < nameB ? 1 : 0;
        }
    });
    render_user_summary_table();
    document.getElementById('sort_users').focus();
}

function render_user_table_navigation() 
{
    const result = [`
        <div class='d-flex mb-3 mt-2'>
            <button class='btn secondary-button d-flex' aria-label='View Audit Log' value='View Audit Log'
                onclick='view_audit_log_click()'>
                <span class='x16 fill-p cdc-icon-clipboard-list-check-solid'>
                    <span class='ml-1'>View Audit Log</span>
                </span>
            </button>
            <div class='ml-auto mr-3 d-flex'>
            <div class='d-flex align-items-center'>
                <div>
                    Showing ${g_first_index + 1}-${Math.min(g_last_index, g_total_users)} of ${g_filtered_user_list.length} user(s)
                </div>
                <div class='row ml-2'>
                    <button aria-label="Navigate to first page" onclick="first_page_click()"
                        aria-label="First page results"
                        ${g_first_index == 0 ? 'disabled="true" aria-disabled="true"' : ''}
                        class='icon-button btn-tab-navigation reverse'>
                        <span class='x24 cdc-icon-chevron-double-right'></span>
                    </button>
                    <button aria-label="Navigate to previous page"  onclick="previous_page_click()"
                        aria-label="Previous page results"
                        ${g_first_index == 0 ? 'disabled="true" aria-disabled="true"' : ''}
                        class='icon-button btn-tab-navigation reverse'>
                        <span class='x24 cdc-icon-chevron-right'></span>
                    </button>
                    <span tabindex="-1" aria-label="Current page Results" class='icon-button btn-navigation-style'>
                        ${g_current_page_number}
                    </span>
                    <button aria-label="Navigate to next page" onclick="next_page_click()"
                        ${g_last_index >= g_filtered_user_list.length ? 'disabled="true" aria-disabled="true"' : ''}
                        class='icon-button btn-tab-navigation'>
                        <span class='x24 cdc-icon-chevron-right pt-1'></span>
                    </button>
                    <button aria-label="Navigate to last page" onclick="last_page_click()"
                        ${g_last_index >= g_filtered_user_list.length ? 'disabled="true" aria-disabled="true"' : ''}
                        class='icon-button btn-tab-navigation'>
                        <span class='x24 cdc-icon-chevron-double-right pt-1'></span>
                    </button>
                </div>
            </div>
            </div>
            <button class='btn primary-button ml-1 d-flex' aria-label='Add New User' value='View Audit Log'
                onclick='add_new_user_click()'>
                <span class='x16 cdc-icon-plus'>
                    <span class='ml-1'>Add New User</span>
                </span>
            </button>
            <button class='btn primary-button ml-1 d-flex' aria-label='Export User list' value='Export User List'
                onclick='export_user_list_click()'>
                <span class='x16 cdc-icon-share'>
                    <span class='ml-1'>Export User List</span>
                </span>
            </button>
        </div>
    `];
    return result.join("");
}

function reset_pagination()
{    
    g_first_index = 0;
    g_current_page_number = 1;
    g_last_index = Math.min(g_first_index + 10, g_filtered_user_list.length);
    g_first_index = (g_current_page_number - 1) * 10;
    g_total_users = g_filtered_user_list.length;
}

function first_page_click()
{
    set_page_navigation('first');
    render_pagination_controls();
    render_user_summary_table();
}

function previous_page_click()
{
    set_page_navigation('previous');
    render_pagination_controls();
    render_user_summary_table();
}

function next_page_click()
{
    set_page_navigation('next');
    render_pagination_controls();
    render_user_summary_table();
}

function last_page_click() 
{
    set_page_navigation('last');
    render_pagination_controls();
    render_user_summary_table();
}

function set_page_navigation(buttonType) 
{
    const total_pages = Math.ceil(g_total_users / g_total_users_per_page);
    switch (buttonType) {
        case 'first':
            g_current_page_number = 1;
            break;
        case 'previous':
            g_current_page_number = Math.max(1, g_current_page_number - 1);
            break;
        case 'next':
            g_current_page_number = Math.min(total_pages, g_current_page_number + 1);
            break;
        case 'last':
            g_current_page_number = total_pages;
            break;
        default:
            console.error("Invalid button type:", buttonType);
            return;
    }
    g_first_index = (g_current_page_number - 1) * g_total_users_per_page;
    g_last_index = Math.min(g_first_index + g_total_users_per_page, g_total_users);
}

function render_user_summary_table()
{
    const user_summary_table = document.getElementById("user_summary_table");
    user_summary_table.innerHTML = render_user_table();
}

function render_pagination_controls()
{
    const pagination_containers = document.querySelectorAll(".pagination-container");
    pagination_containers.forEach(container => {
        container.innerHTML = render_user_table_navigation();
    });
}

function user_entry_render(p_user, role_set) 
{
    var role_count = 0;
    var disable_inactive_state_button = true;

    const role_result = [];
    for (var i = 0; i < g_user_role_jurisdiction.length; i++) {
        var user_role = g_user_role_jurisdiction[i];
        if (
            role_set.indexOf(user_role.role_name) > -1 &&
            (
                (
                    g_managed_jurisdiction_set[user_role.jurisdiction_id] != null &&
                    g_managed_jurisdiction_set[user_role.jurisdiction_id] == true
                )
                ||
                g_managed_jurisdiction_set["/"] == true
            )
            &&
            user_role.user_id == p_user.name
        ) {
            role_count = role_count + 1;
            if(user_role.is_active)
                role_result.push('<div>');
            else
                role_result.push('<div class="inactive-role">');
            var role_name = user_role.role_name.split('_');
            role_name = role_name.map(section => {
                if (section === 'steve' || section === 'mmria' || section === 'prams')
                    return section.toUpperCase();
                else
                    return section[0].toUpperCase() + section.slice(1)
            });
            role_result.push(role_name.join(" "));
            role_result.push(" / ");
            role_result.push(user_role.jurisdiction_id == "/" ? "Top Folder" : user_role.jurisdiction_id);
            role_result.push(" / ");
            if (user_role.is_active) {
                role_result.push(" Active");
                disable_inactive_state_button = false;
            } else {
                role_result.push(" Inactive");
            }
            role_result.push('</div>');
        }
    }

    if (role_count <= 0) {
        disable_inactive_state_button = true;
    }
    const result = [`
        <tr id=" +  ${convert_to_jquery_id(p_user._id)} + " valign=top>
            <td>
                <div class="d-flex align-items-center">
                    <button aria-label="View user ${p_user.name}" onclick="view_user_click('${p_user._id}')" class="btn btn-link">${p_user.name}</button>
                    <span id="${p_user._id.split("org.couchdb.user:")[1]}_role_status" role="status" class="mr-2 spinner-container spinner-content">
                        <span style="background: transparent;" class="spinner-body text-primary">
                            <span class="spinner"></span>
                            <span class="sr-only">Saving new user...</span>
                        </span>
                    </span>
                </div>
            </td>
            <td>
                <div id="role_results_${p_user.name}" style="overflow-x: hidden; overflow-y: auto; height: 100px;">
                    ${role_result.join("")}
                </div>
            </td>
            <td>
                <div class="d-flex">
                    <div class="d-flex flex-column col-12">
                        <button
                            aria-disabled="${disable_inactive_state_button ? true : false}" ${disable_inactive_state_button ? 'disabled' : ''}
                            id="set_role_active_state_button_${p_user.name}"
                            class="btn secondary-button"
                            aria-label="Set all roles to inactive for ${p_user.name}"
                            onclick="set_all_roles_active_state('${p_user.name}')"
                        >
                            Set All Roles to Inactive
                        </button>
                        <button
                            id="delete_button_${p_user.name}"
                            class="btn delete-button"
                            aria-label="Delete user ${p_user.name}"
                            onclick="init_small_loader(function(){ $mmria.confirm_user_delete_dialog_show('${p_user._id}', '${p_user._rev}', delete_user_click) })"
                        >
                            Delete
                        </button>
                    </div>
                </div>
            </td>
        </tr>
    `];
    return result;
}