var initial_user_roles = [];

function edit_user_renderer()
{
    if(g_ui.user_summary_list.find(user => user._id === g_current_user_id) === undefined)
        back_to_landing_clicked();
    g_is_audit_log_view = false;
    role_user_name = g_current_user_id;
    if(role_user_name.indexOf(":") > -1)
    {
        role_user_name =  role_user_name.split("org.couchdb.user:")[1];
    }
    user_roles = [];
    user_roles = g_user_role_jurisdiction.filter(jurisdiction => jurisdiction.user_id === role_user_name);
    initial_user_roles = JSON.parse(JSON.stringify(user_roles));
    init_audit_history();
    const result = `
        <div class="d-flex mt-4">
            <div>
                <h2 class="h4">User Info</h2>
            </div>
            <div class="ml-auto d-flex">
                <span id="user_save_status" role="status" class="mr-2 spinner-container spinner-content">
                    <span class="spinner-body text-primary">
                        <span class="spinner"></span>
                        <span class="sr-only">Saving new user...</span>
                    </span>
                </span>
                <button disabled aria-disabled="true" class="btn primary-button">Enable Edit</button>
                <button id="save_user_button" onclick="save_user_edits()" class="btn primary-button">Save User Edits</button>
                <button id="undo_button" onclick="audit_history_undo()" id="audit_history_undo_button" ${can_undo === true ? '' : 'disabled'} aria-disabled="${can_undo ? 'false' : 'true'}" class="btn primary-button">Undo</button>
            </div>
        </div>
        <div class="d-flex">
            <div class="vertical-control col-4 pl-0">
                <label>Username (i.e., Email Address)</label>
                <input disabled aria-disabled="true" value="${role_user_name}" autocomplete="off" class="form-control" type="text" id="user_email">
            </div>
        </div>
        <div class="mt-3 mb-3">
            <h2 class="h4">Change Password</h2>
        </div>
        <div class="d-flex flex-row">
            <div class="vertical-control col-4 pl-0 pr-0">
                <label>New Password</label>
                <div class="input-group">
                    <input type="password" autocomplete="off" class="form-control" id="user_password" data-ms-reveal="false">
                    <div class="input-group-append">
                        <button id="show_hide_password"  aria-label="Show password" onclick="show_hide_password('user_password')" type="button" class="btn btn-inline-primary mr-3">
                            <span class="x22 fill-p cdc-icon-eye-solid"></span>
                        </button>
                    </div>
                </div>
            </div>
            <div class="vertical-control col-4 pl-0 pr-0">
                <label>Verify Password</label>
                <div class="input-group">
                    <input type="password" autocomplete="off" class="form-control" id="user_password_verify" data-ms-reveal="false">
                    <div class="input-group-append">
                        <button id="show_hide_password_verify" aria-label="Show password verify" onclick="show_hide_password('user_password_verify')" type="button" class="btn btn-inline-primary">
                            <span class="x22 fill-p cdc-icon-eye-solid"></span>
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div class="d-flex">
            <div id="password_validation" class="col-4 pl-0 pr-2"></div>
            <div id="password_verify_validation" class="col-4 pl-0 pr-2"></div>
        </div>
        <div class="d-flex flex-column mt-4">
            <div>
                <h2 class="h4">Assigned Roles</h2>
            </div>
            <table class="table mt-3">
                <thead>
                    <tr class="header-level-2 sticky-header z-index-top">
                        <th>Role Name</th>
                        <th>Case Folder Access</th>
                        <th>Effective Start Date</th>
                        <th>Effective End Date</th>
                        <th>Active Status</th>
                        <th>Action</th>
                    </tr>
                </thead>
                <tbody id="user_roles">
                    ${
                        user_roles && user_roles.length > 0
                            ? render_editable_role_rows(user_roles)
                            : '<tr id="no-roles-placeholder" class="text-center"><td colspan="6">No roles assigned.</td></tr>'
                    }
                </tbody>
            </table>
            <div id="add_new_role_container" class="ml-auto mt-2">
                <button class="btn secondary-button col-12" aria-label="Add new role" onclick='add_role()'>
                    Add New Role
                </button>
            </div>
        </div>
        <div class="d-flex mt-4">
            <div>
                <h2 class="h4">Account History</h2>
            </div>
        </div>
        <div class="d-flex">
            <div class='horizontal-control mb-3 mt-3 pl-0 col-md-4'>
                <input id="audit_history_filter" onkeyup="filter_audit_history(this.value)" class='form-control' type='text' placeholder='Filter account history...'>
                <button disabled aria-disabled="true" id="clear_filter_button" onclick="clear_audit_filter()" class="btn btn-link ml-2 pl-0" aria-label="clear filter" value="clear filter">Clear filter</button>
            </div>
            <div class="d-flex ml-auto audit_navigation_container">
                ${render_account_history_table_navigation_view()}
            </div>
        </div>
        <div>
            <table class='table table-layout-fixed align-cell-top'><caption class='table-caption'>User account history table</caption>
                <thead>
                    <tr class='header-level-2'>
                        <th width='200' scope='colgroup'>Date/Time</th>
                        <th width="175">Editor</th>
                        <th width='150' scope='colgroup;'>Action</th>
                        <th>Field</th>
                        <th width="150">Old Value</th>
                        <th width="150">New Value</th>
                    </tr>
                </thead>
                <tbody id="audit_history_table_body">
                    ${g_filtered_audit_list && g_filtered_audit_list.length > 0
                        ? g_filtered_audit_list.map(audit => render_account_history_row(audit)).join("")
                        : '<tr class="text-center"><td colspan="6">No audit history available.</td></tr>'
                    }
                </tbody>
            </table>
        </div>
    `;
    show_hide_user_management_back_button(true);
    set_page_title("Edit User");
    document.getElementById("form_content_id").innerHTML = result;
}

function save_user_edits()
{
    if(g_user_audit_history.length <= 0)
    {
        view_user_click(g_current_user_id);
    }
    else
    {
        var is_valid = true;
        disable_save_button();
        disable_undo_button();
        const password = document.getElementById("user_password").value;
        const password_verify = document.getElementById("user_password_verify").value;
        
        if(password && password.length > 0 && password_verify && password_verify.length > 0)
        {
            if(password !== password_verify)
            {
                is_valid = false;
                document.getElementById('user_password').classList.add('is-invalid');
                document.getElementById('user_password').style.color = 'red';
                document.getElementById('user_password_verify').classList.add('is-invalid');
                document.getElementById('user_password_verify').style.color = 'red';
                document.getElementById('show_hide_password').classList.add('is-invalid-button');
                document.getElementById('show_hide_password_verify').classList.add('is-invalid-button');
                document.getElementById('password_validation').textContent = 'Passwords do not match';
                document.getElementById('password_validation').style.color = 'red';
                document.getElementById('password_verify_validation').textContent = 'Passwords do not match';
                document.getElementById('password_verify_validation').style.color = 'red';
            }
            if(!is_valid_password(password))
            {
                is_valid = false;
                document.getElementById('user_password').classList.add('is-invalid');
                document.getElementById('user_password').style.color = 'red';
                document.getElementById('user_password_verify').classList.add('is-invalid');
                document.getElementById('user_password_verify').style.color = 'red';
                document.getElementById('show_hide_password').classList.add('is-invalid-button');
                document.getElementById('show_hide_password_verify').classList.add('is-invalid-button');
                document.getElementById('password_validation').textContent = 'Invalid password.  Minimum length is: ' + g_policy_values.minimum_length + ' and should only include characters [a-zA-Z0-9!@#$%?* ]';
                document.getElementById('password_validation').style.color = 'red';
            }
            if(user_roles.length > 0 && user_roles_changed())
            {
                if(!assigned_roles_validation_check()) is_valid = false;
            }
            if (is_valid)
            {
                update_user_password(password, password_verify);
                disable_save_button();
                disable_undo_button();
            }
            else if (!is_valid && g_user_audit_history.length > 0)
            {
                enable_save_button();
                enable_undo_button();
            }
            else if(!is_valid && g_user_audit_history.length <= 0)
            {
                enable_save_button();
            }
        }
        else if ((!password && password_verify) || (password && !password_verify))
        {
            document.getElementById('user_password').classList.add('is-invalid');
            document.getElementById('user_password').style.color = 'red';
            document.getElementById('user_password_verify').classList.add('is-invalid');
            document.getElementById('user_password_verify').style.color = 'red';
            document.getElementById('show_hide_password').classList.add('is-invalid-button');
            document.getElementById('show_hide_password_verify').classList.add('is-invalid-button');
            document.getElementById('password_validation').textContent = 'Passwords do not match';
            document.getElementById('password_validation').style.color = 'red';
            document.getElementById('password_verify_validation').textContent = 'Passwords do not match';
            document.getElementById('password_verify_validation').style.color = 'red';
            enable_save_button();
            if( g_user_audit_history.length > 0)
            {
                enable_undo_button();
            }
        }
        else if(user_roles.length > 0 && user_roles_changed())
        {
            if(assigned_roles_validation_check())
            {
                update_assigned_roles();
            }
            else if(!assigned_roles_validation_check() && g_user_audit_history.length > 0) 
            {
                enable_save_button();
                enable_undo_button();
            }
            else if(!assigned_roles_validation_check() && g_user_audit_history.length <= 0)
            {
                enable_save_button();
            }
        }
        else
        {
            view_user_click(g_current_user_id);
        }
    }

}

function temp_update_roles_audit_test()
{
    create_audit_history();
}

async function update_user_password(p_password)
{
    const user = g_ui.user_summary_list.find(user => user._id === g_current_user_id);
    if(user)
    {
        user.password = p_password;
        const response = await get_http_post_response("api/user", user);
        const response_obj = eval(response);
        if(response_obj.ok)
        {
            user._rev = response_obj.rev; 
            if(user_roles.length > 0 && user_roles_changed())
            {
                if (assigned_roles_validation_check())
                {
                    await update_assigned_roles();
                }
                else if (!assigned_roles_validation_check() && g_user_audit_history.length > 0)
                {
                    enable_save_button();
                    enable_undo_button();
                }
                else if (!assigned_roles_validation_check() && g_user_audit_history.length <= 0)
                {
                    enable_save_button();
                }
            }
            else
            {
                save_audit_history(g_current_user_id);
            }
        }
        else
        {
            enable_save_button();
            if(g_user_audit_history.length > 0)
            {
                enable_undo_button();
            }
        }
    }
}

async function update_assigned_roles()
{
    let was_successful = true;
    user_roles.forEach((role) => {
        role.user_id = role_user_name;
    });
    deleted_user_roles.forEach((deleted_role) => {
        if(deleted_role.role.hasOwnProperty('_rev'))
        {
            deleted_role.role._deleted = true;
            user_roles.push(deleted_role.role);
        }
    });
    const response = await get_http_post_response
    (
        "api/user_role_jurisdiction/bulk",
        user_roles
    );

    response.forEach
    (  
        (response_obj) => {
            if (response_obj.ok) 
            {
                const current_urj = g_user_role_jurisdiction.find(urj => urj._id == response_obj.id);
                if(current_urj) 
                {
                    current_urj._rev = response_obj.rev;
                    if(current_urj._deleted)
                        g_user_role_jurisdiction = g_user_role_jurisdiction.filter(urj => urj._id != response_obj.id);
                }
                else
                {
                    const new_role = user_roles.find(urj => urj._id == response_obj.id);
                    if(new_role)
                    {
                        new_role._rev = response_obj.rev;
                        g_user_role_jurisdiction.push(new_role);
                    }
                }
            }
            else
            {
                was_successful = false;
            }
        }
    );
    if(was_successful)
    {
        save_audit_history(g_current_user_id);
    }
}


function user_roles_changed() {
    var has_changed = false;
    if(initial_user_roles.length !== user_roles.length) has_changed = true;
    user_roles.forEach((role, index) => 
    {
        var current_role_string = JSON.stringify(role);
        var initial_role_string = JSON.stringify(initial_user_roles[index]);
        if (current_role_string !== initial_role_string) has_changed = true;
    });
    return has_changed;
}


function render_editable_role_rows(p_user_role_jurisdiction)
{
    const result = [];
    for(var i = 0; i < p_user_role_jurisdiction.length; i++)
    {
        const item = p_user_role_jurisdiction[i];
        result.push(user_assigned_role_renderer(item))
    }
    return result.join("");
}

function user_assigned_role_renderer(p_user_jurisdiction)
{
    const result = `
        <tr id="${p_user_jurisdiction._id}_role">
            <td width="485">
                <div class="vertical-control p-0 mb-4 col-md-12">
                    <select id="${p_user_jurisdiction._id}_role_type" aria-label="Select Role" class="form-select form-control" aria-label="Select user role">
                    ${edit_user_role_list_render(p_user_jurisdiction)}
                    </select>
                    <span id="${p_user_jurisdiction._id}_role_type_validation" class="col-12 data-cell-error-message pl-0 pr-0"></span>
                </div>
            </td>
            <td width="485">
                <div class="vertical-control p-0 mb-4 col-md-12">
                    <select id="${p_user_jurisdiction._id}_role_jurisdiction_type" aria-label="Select folder" class="form-select form-control" aria-label="Select case access folder">
                        ${user_role_jurisdiction_render(g_jurisdiction_tree, p_user_jurisdiction.jurisdiction_id, 0).join("")}
                    </select>
                    <span id="${p_user_jurisdiction._id}_role_jurisdiction_validation" class="col-12 data-cell-error-message pl-0 pr-0"></span>
                </div>
            </td>
            <td>
                <input
                    id="${p_user_jurisdiction._id}_role_effective_start_date"
                    aria-label="Effective Start Date for role ${p_user_jurisdiction._id}"
                    value="${p_user_jurisdiction.effective_start_date != null ? format_date(p_user_jurisdiction.effective_start_date) : ""}"
                    autocomplete="off"
                    class="form-control mb-4"
                    type="date"
                    placeholder="MM/DD/YYYY"
                >
                <span id="${p_user_jurisdiction._id}_role_start_date_validation" class="col-12 data-cell-error-message pl-0 pr-0"></span>
            </td>
            <td>
                <input
                    id="${p_user_jurisdiction._id}_role_effective_end_date"
                    aria-label="Effective End Date for role ${p_user_jurisdiction._id}"
                    value="${p_user_jurisdiction.effective_end_date != null ? format_date(p_user_jurisdiction.effective_end_date.toString()) : ""}"
                    autocomplete="off"
                    class="form-control mb-4"
                    type="date"
                    placeholder="MM/DD/YYYY"
                >
                <span id="${p_user_jurisdiction._id}_role_end_date_validation" class="col-12 data-cell-error-message pl-0 pr-0"></span>
            </td>
            <td>
                <div class="vertical-control col-md-12">
                    <fieldset>
                        <legend class="accessible-hide">Active Status</legend>
                        <div class="form-check">
                            <input
                                id="${p_user_jurisdiction._id}_role_active_status_true"
                                ${p_user_jurisdiction.is_active ? "checked" : ""}
                                class="form-check-input big-radio"
                                name="${p_user_jurisdiction._id}_role_active_status"
                                type="radio"
                                value="true"
                            >
                            <label class="form-check-label" for="${p_user_jurisdiction._id}_role_active_status_true">
                                Active
                            </label>
                        </div>
                        <div class="form-check">
                            <input
                                ${p_user_jurisdiction.is_active ? "" : "checked"}
                                class="form-check-input big-radio"
                                type="radio"
                                value="false"
                                name="${p_user_jurisdiction._id}_role_active_status"
                                id="${p_user_jurisdiction._id}_role_active_status_false"
                            >
                            <label class="form-check-label" for="${p_user_jurisdiction._id}_role_active_status_false">
                                Inactive
                            </label>
                        </div>
                    </fieldset>
                </div>
            </td>
            <td class="d-flex pt-3 justify-content-center border-none">
                <button class="btn delete-button col-12" aria-label="Delete role" onclick="delete_role('${p_user_jurisdiction._id}')">
                    Delete Role
                </button>
            </td>
        </tr>
    `;
    return result;
}

function edit_user_role_list_render(p_user_jurisdiction) 
{
    const role_set = get_role_list();
    const result = [];
    result.push('<option value="">Select Role</option>');
    role_set.forEach(role => {
        if(role !== "")
        {
            var role_name = role.split('_');
            role_name = role_name.map(section => {
                if (section === 'steve' || section === 'mmria' || section === 'prams')
                    return section.toUpperCase();
                else
                    return section[0].toUpperCase() + section.slice(1);
            });
            result.push("<option ");
            if(p_user_jurisdiction.role_name === role)
                result.push( "selected ");
            result.push("value ='" + role + "'>");
            result.push(role_name.join(" "));
            result.push("</option>");
        }
    })
    return result.join('');
}

function user_role_jurisdiction_render(p_data, p_selected_id, p_level)
{
	var result = [];
	if( p_data._id)
	{
		result.push("<option value=''>Select Case Folder</option>")
		p_level = 0;
	}
    let is_managed_jusisdiction = false;
    for (const key in g_managed_jurisdiction_set) 
    {
        if (g_managed_jurisdiction_set.hasOwnProperty(key)) 
        {
            if(p_data.name.indexOf(key) == 0)
            {
                is_managed_jusisdiction = true;
                break;
            }
        }
    }
    if(is_managed_jusisdiction)
    {
        result.push(`<option value="${p_data.name}"`);
        if(p_data.name == p_selected_id)
        {
            result.push(" selected=true")
        }
        result.push(">");
        result.push(p_data.name == "/" ? "Top Folder" : p_data.name);
        result.push("</option>");
    }
    if(p_data.children != null)
    {
        for(var i = 0; i < p_data.children.length; i++)
        {
            var child = p_data.children[i];
            Array.prototype.push.apply(result, user_role_jurisdiction_render(child, p_selected_id, p_level + 1));
            
        }
    }
	return result;
}

function edit_add_assigned_role(user_role_jurisdiction)
{
    const unique_guid = $mmria.get_new_guid();
    user_role_jurisdiction.push({
        _id: unique_guid,
        role_name : "",
        user_id: "",
        jurisdiction_id: "",
        effective_start_date: new Date(new Date().setHours(0,0,0,0)),
        effective_end_date: g_policy_values.default_days_in_effective_date_interval != null && parseInt(g_policy_values.default_days_in_effective_date_interval) >0? new Date(new Date().getTime() + parseInt(g_policy_values.default_days_in_effective_date_interval)*24*60*60*1000).setHours(0,0,0,0) : "",
        is_active: true,
        date_created: new Date(),
        created_by: g_current_u_id,
        date_last_updated: new Date(),
        last_updated_by: g_current_u_id,
        data_type:"user_role_jursidiction"
    });
    const result = `
        <tr id="${user_roles.length.toString()}_role">
            <td width="485">
                <div class="vertical-control p-0 mb-4 col-md-12">
                    <select id="${unique_guid}_role_type" aria-label="Select Role" class="form-select form-control" aria-label="Select user role">
                        ${edit_user_add_role_render()}
                    </select>
                </div>
            </td>
            <td width="485">
                <div class="vertical-control p-0 mb-4 col-md-12">
                    <select id="${unique_guid}_role_jurisdiction_type" aria-label="Select folder" class="form-select form-control" aria-label="Select case access folder">
                        ${user_role_jurisdiction_render(g_jurisdiction_tree, "", 0).join("")}
                    </select>
                </div>
            </td>
            <td>
                <input
                    value="${format_date(new Date().toISOString())}"
                    id="${unique_guid}_role_effective_start_date"
                    aria-label="Effective Start Date for role ${unique_guid}"
                    autocomplete="off"
                    class="form-control mb-4"
                    type="date"
                    placeholder="MM/DD/YYYY"
                >
            </td>
            <td>
                <input
                    id="${unique_guid}_role_effective_end_date"
                    aria-label="Effective End Date for role ${unique_guid}"
                    autocomplete="off"
                    class="form-control mb-4"
                    type="date"
                    placeholder="MM/DD/YYYY"
                >
            </td>
            <td>
                <div class="vertical-control col-md-12">
                    <fieldset>
                        <legend class="accessible-hide">Active Status</legend>
                        <div class="form-check">
                            <input checked class="form-check-input big-radio" name="${unique_guid}_role_active_status" type="radio" value="true" id="${unique_guid}_role_active_status_true">
                            <label class="form-check-label" for="${unique_guid}_role_active_status_true">
                                Active
                            </label>
                        </div>
                        <div class="form-check">
                            <input class="form-check-input big-radio" type="radio" value="false" name="${unique_guid}_role_active_status" id="${unique_guid}_role_active_status_false">
                            <label class="form-check-label" for="${unique_guid}_role_active_status_false">
                                Inactive
                            </label>
                        </div>
                    </fieldset>
                </div>
            </td>
            <td class="d-flex pt-3 justify-content-center border-none">
                <button id="${unique_guid}_delete" class="btn delete-button col-12" aria-label="Delete role" onclick="delete_new_role('${unique_guid}')">
                    Delete Role
                </button>
            </td>
        </tr>
    `;
    return result;
}

function edit_user_add_role_render()
{
    const role_set = get_role_list();
    const result = [];
    result.push('<option value="">Select Role</option>');
    role_set.forEach(role => {
        if(role !== "")
        {
            var role_name = role.split('_');
            role_name = role_name.map(section => {
                if (section === 'steve' || section === 'mmria' || section === 'prams')
                    return section.toUpperCase();
                else
                    return section[0].toUpperCase() + section.slice(1);
            });
            result.push("<option ");
            result.push("value ='" + role + "'>");
            result.push(role_name.join(" "));
            result.push("</option>");
        }

    })
}