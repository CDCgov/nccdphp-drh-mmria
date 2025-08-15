var user_roles = [];
var deleted_user_roles = [];
var can_undo = false;

var user = {
    "_id": '',
    "password": null,
    "iterations": 10,
    "name": '',
    "roles": [],
    "type": "user",
    "derived_key": "a1bb5c132df5b7df7654bbfa0e93f9e304e40cfe",
    "salt": "510427706d0deb511649021277b2c05d"
};

function add_new_user_render() {
    user_roles = [];
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
                <button id="save_user_button" onclick="save_user_click()" class="btn primary-button">Save New User</button>
                <button id="undo_button" onclick="audit_history_undo()" id="audit_history_undo_button" ${can_undo === true ? '' : 'disabled'} aria-disabled="${can_undo ? 'false' : 'true'}" class="btn primary-button">Undo</button>
            </div>
        </div>
        <div class="d-flex">
            <div class="vertical-control required col-4 pl-0">
                <label id="username_label">Username (i.e., Email Address)</label>
                <input aria-required="true" aria-labelledby="username_label" autocomplete="off" class="form-control" type="text" id="user_email" value="${user.name}">
            </div>
            <div class="vertical-control required col-4 pl-0 pr-0">
                <label id="password_label">Password</label>
                <div class="input-group">
                    <input aria-required="true" aria-labelledby="password_label" type="password" autocomplete="off" class="form-control" id="user_password" data-ms-reveal="false">
                    <div class="input-group-append">
                        <button id="show_hide_password"  aria-label="Show password" onclick="show_hide_password('user_password')" type="button" class="btn btn-inline-primary mr-3">
                            <span class="x22 fill-p cdc-icon-eye-solid"></span>
                        </button>
                    </div>
                </div>
            </div>
            <div class="vertical-control required col-4 pl-0 pr-0">
                <label id="password_verify_label">Verify Password</label>
                <div class="input-group">
                    <input aria-required="true" aria-labelledby="password_verify_label" type="password" autocomplete="off" class="form-control" id="user_password_verify" data-ms-reveal="false">
                    <div class="input-group-append">
                        <button id="show_hide_password_verify" aria-label="Show password verify" onclick="show_hide_password('user_password_verify')" type="button" class="btn btn-inline-primary">
                            <span class="x22 fill-p cdc-icon-eye-solid"></span>
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div class="d-flex">
            <div id="username_validation" class="col-4 pl-0 pr-2"></div>
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
                    <tr id="no-roles-placeholder" class="text-center"><td colspan="6">No roles assigned.</td></tr>
                </tbody>
            </table>
            <div class="d-flex ml-auto mt-2">
                <button class="btn secondary-button col-12" aria-label="Add new role" onclick="add_role()">
                    Add New Role
                </button>
            </div>
        </div>
    `;
    show_hide_user_management_back_button(true);
    set_page_title("Add New User");
    init_audit_history();
    create_initial_audit("", "add_user", ACTION_TYPE.ADD_USER, "", "New user creation started", 'add_user');
    document.getElementById('form_content_id').innerHTML = result;
}

function show_hide_password(field_id) {
    const passwordField = document.getElementById(field_id);
    const button = passwordField.nextElementSibling.querySelector('button');
    const icon = button.querySelector('span');

    if (passwordField.type === 'password') {
        passwordField.type = 'text';
        icon.classList.remove('cdc-icon-eye-solid');
        icon.classList.remove('x22');
        icon.classList.add('x24');
        icon.classList.add('cdc-icon-minus');
    } else {
        passwordField.type = 'password';
        icon.classList.remove('cdc-icon-minus');
        icon.classList.remove('x24');
        icon.classList.add('x22');
        icon.classList.add('cdc-icon-eye-solid');
    }
}

function add_assigned_role() {
    const unique_guid = $mmria.get_new_guid();
    const new_role = {
        _id: unique_guid,
        role_name : "",
        user_id: "",
        jurisdiction_id: "",
        effective_start_date: new Date(new Date().setHours(0,0,0,0)),
        effective_end_date: g_policy_values.default_days_in_effective_date_interval != null && parseInt(g_policy_values.default_days_in_effective_date_interval) >0? new Date(new Date().getTime() + parseInt(g_policy_values.default_days_in_effective_date_interval)*24*60*60*1000).setHours(0,0,0,0) : "",
        is_active: true,
        date_created: new Date(),
        created_by: g_userName,
        date_last_updated: new Date(),
        last_updated_by: g_userName,
        data_type:"user_role_jursidiction"
    };
    user_roles.push(new_role);
    const role_description = `New role added (ID: ${unique_guid})`;
    add_to_audit_history(g_current_u_id, unique_guid, ACTION_TYPE.ADD_ROLE, "", "", 'add_role');
    return user_assigned_role_render(unique_guid);
}
    
function user_assigned_role_render(p_unique_guid)
{
 return result = `
        <tr id="${p_unique_guid}_role">
            <td width="485">
                <div class="vertical-control p-0 mb-4 col-md-12">
                    <select aria-required="true" id="${p_unique_guid}_role_type" aria-label="Select Role" class="form-select form-control role-select-controls" aria-label="Select user role">
                        ${user_role_render()}
                    </select>
                    <span id="${p_unique_guid}_role_type_validation" class="col-12 data-cell-error-message pl-0 pr-0"></span>
                </div>
            </td>
            <td width="485">
                <div class="vertical-control p-0 mb-4 col-md-12">
                    <select aria-required="true" id="${p_unique_guid}_role_jurisdiction_type" aria-label="Select folder" class="form-select form-control role-jursidiction-controls" aria-label="Select case access folder">
                        ${user_role_jurisdiction_render(g_jurisdiction_tree).join('')}
                    </select>
                    <span id="${p_unique_guid}_role_jurisdiction_validation" class="col-12 data-cell-error-message pl-0 pr-0"></span>
                </div>
            </td>
            <td>
                <div class="vertical-control p-0 mb-4 col-md-12">
                    <input aria-required="true" value="${format_date(new Date().toISOString())}" id="${p_unique_guid}_role_effective_start_date" aria-label="Effective Start Date for role ${p_unique_guid}" autocomplete="off" class="form-control mb-4" type="date" placeholder="MM/DD/YYYY">
                    <span id="${p_unique_guid}_role_start_date_validation" class="col-12 data-cell-error-message pl-0 pr-0"></span>
                </div>
            </td>
            <td>
                <div class="vertical-control p-0 mb-4 col-md-12">
                    <input id="${p_unique_guid}_role_effective_end_date" aria-label="Effective End Date for role ${p_unique_guid}" autocomplete="off" class="form-control mb-4" type="date" placeholder="MM/DD/YYYY">
                    <span id="${p_unique_guid}_role_end_date_validation" class="col-12 data-cell-error-message pl-0 pr-0"></span>           
                </div>  
            </td>
            <td>
                <div class="vertical-control col-md-12">
                    <fieldset>
                        <legend class="accessible-hide">Active Status</legend>
                        <div class="form-check">
                            <input checked class="form-check-input big-radio" name="${p_unique_guid}_role_active_status" type="radio" value="true" id="${p_unique_guid}_role_active_status_true">
                            <label class="form-check-label" for="${p_unique_guid}_role_active_status_true">
                                Active
                            </label>
                        </div>
                        <div class="form-check">
                            <input class="form-check-input big-radio" type="radio" value="false" name="${p_unique_guid}_role_active_status" id="${p_unique_guid}_role_active_status_false">
                            <label class="form-check-label" for="${p_unique_guid}_role_active_status_false">
                                Inactive
                            </label>
                        </div>
                    </fieldset>
                </div>
            </td>
            <td class="d-flex pt-3 justify-content-center border-none">
                <button id="${p_unique_guid}_delete" class="btn delete-button col-12" aria-label="Delete role" onclick="delete_role('${p_unique_guid}')">
                    Delete Role
                </button>
            </td>
        </tr>
    `;
}

function user_role_render()
{
    const role_set = get_role_list();
    const temp_result = [];
    temp_result.push("<option value=''>Select Role</option>");
    role_set.forEach(role => {
        if(role !== "") {
            var role_name = role.split('_');
            role_name = role_name.map(section => {
                if (section === 'steve' || section === 'mmria' || section === 'prams')
                    return section.toUpperCase();
                else
                    return section[0].toUpperCase() + section.slice(1);
            });
            temp_result.push("<option ");
            temp_result.push("value ='" + role + "'>");
            temp_result.push(role_name.join(" "));
            temp_result.push("</option>");
        }
    });
    return temp_result.join('');
}

function user_role_jurisdiction_render(p_data, p_level)
{
    var p_level = p_level || 0;
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
        result.push(">");
        result.push(p_data.name == "/" ? "Top Folder" : p_data.name);
        result.push("</option>");
    }
    if(p_data.children != null)
    {
        for(var i = 0; i < p_data.children.length; i++)
        {
            var child = p_data.children[i];
            Array.prototype.push.apply(result, user_role_jurisdiction_render(child, p_level + 1));
        }
    }
	return result;
}

function delete_role(p_role_id) 
{
    const p_role_index = user_roles.findIndex(role => role._id === p_role_id);
    if (p_role_index !== -1) 
    {
        const role_to_delete = user_roles.splice(p_role_index, 1)[0];
        const role_description = `${role_to_delete.role_name || ''}`;
        add_to_audit_history(g_current_u_id, p_role_id, ACTION_TYPE.DELETE_ROLE, role_description, null, 'delete_role');
        
        deleted_user_roles.push({
            _id: p_role_id,
            html: document.getElementById(`${p_role_id}_role`).outerHTML,
            role: role_to_delete
        });
        const row = document.getElementById(`${p_role_id}_role`);
        if (row) row.remove();
        console.log(user_roles);
        if (user_roles.length <= 0) {
            const userRoles = document.getElementById("user_roles");
            if (userRoles) {
                const tr = document.createElement('tr');
                tr.id = "no-roles-placeholder";
                tr.className = "text-center";
                tr.innerHTML = '<td colspan="6">No roles assigned.</td>';
                userRoles.appendChild(tr);
            }
        }
    } 
    else 
    {
        console.error(`Role with id ${p_role_id} not found`);
    }
}

function add_role() 
{
    const noRolesPlaceholder = document.getElementById("no-roles-placeholder");
    if (noRolesPlaceholder) noRolesPlaceholder.remove();
    console.log("Adding new role");
    console.log(user_roles);
    const userRoles = document.getElementById('user_roles');
    if (userRoles) 
    {
        const temp = document.createElement('tbody');
        temp.innerHTML = add_assigned_role("");
        Array.from(temp.children).forEach(child => userRoles.appendChild(child));
    }
    const undoBtn = document.getElementById('audit_history_undo_button');
    if (undoBtn) undoBtn.disabled = false;
}

document.addEventListener('input', function(e) 
{
    if (e.target && (e.target.id === 'user_password' || e.target.id === 'user_password_verify')) 
    {
        check_passwords_match();
    }
});

document.addEventListener('change', function(e) 
{
    if (e.target && (e.target.type === 'text' || e.target.type === 'password')) 
    {
        user_email_change.call(e.target);
    }
});

function user_email_change() 
{
    const id = this.id;
    const value = this.value;
    if(id.includes('password')) 
    {
        add_to_audit_history(g_current_u_id, id, ACTION_TYPE.EDIT_PASSWORD, this.dataset.previousValue, value, 'password');
    } 
    else if (id.includes('user_email')) 
    {
        role_user_name = value;
        add_to_audit_history(g_current_u_id, id, ACTION_TYPE.EDIT_USERNAME, this.dataset.previousValue, value, 'user_id');
    }
}

document.addEventListener('focus', function(e) 
{
    if (e.target && (e.target.tagName === 'INPUT' || e.target.tagName === 'SELECT')) 
    {
        e.target.dataset.previousValue = e.target.value;
    }
}, true);


document.addEventListener('mousedown', function(e) 
{
    if (e.target && e.target.tagName === 'SELECT') 
    {
        e.target.dataset.previousValue = e.target.value;
    }
});

document.addEventListener('click', function(e) 
{
    if (e.target && e.target.tagName === 'SELECT' && !e.target.dataset.previousValue) 
    {
        e.target.dataset.previousValue = e.target.value;
    }
});

document.addEventListener('change', function(e) 
{
    if (e.target && e.target.type === 'radio') 
    {
        const value = e.target.value;
        const role_id = e.target.name.split('_')[0];
        const role = user_roles.find(role => role._id === role_id);
        const active_role_true = document.getElementById(`${role_id}_role_active_status_true`);
        const active_role_false = document.getElementById(`${role_id}_role_active_status_false`);
        if(value === 'true')
        {
            active_role_true.setAttribute('checked', '');
            active_role_false.removeAttribute('checked');
        }
        else
        {
            active_role_false.setAttribute('checked', '');
            active_role_true.removeAttribute('checked');
        }
        // Update the data model
        role.is_active = value === 'true';
        console.log(`Role ${role_id} active status changed to: ${value}`);
        add_to_audit_history(g_current_u_id, `${role_id}_role_active_status`, ACTION_TYPE.EDIT_ROLE, value === "true" ? "false" : "true", value, 'is_active');
    }
    if (e.target && e.target.tagName === 'SELECT') 
    {
        const selectedValue = e.target.value;
        e.target.value = selectedValue; // Ensure the select value is set
        console.log(`Select input changed to: ${selectedValue}`);
        const role_id = e.target.id.split('_')[0];
        const role = user_roles.find(role => role._id === role_id);
        if(e.target.id.includes('role_type')) 
        {
            role.role_name = selectedValue;
            const options = e.target.querySelectorAll('option');
            options.forEach(option => {
                option.removeAttribute('selected');
            });
            const selectedOption = e.target.querySelector(`option[value="${selectedValue}"]`);
            if (selectedOption) {
                selectedOption.setAttribute('selected', '');
                console.log(`Updated selected attribute for option with value: ${selectedValue}`);
            } else {
                console.warn(`No option found with value: ${selectedValue}`);
            }
            add_to_audit_history(g_current_u_id, `${role_id}_role_type`, ACTION_TYPE.EDIT_ROLE, e.target.dataset.previousValue, selectedValue, 'role_name');
        }
        else if(e.target.id.includes('role_jurisdiction_type')) 
        {
            role.jurisdiction_id = selectedValue;
            const options = e.target.querySelectorAll('option');
            options.forEach(option => {
                option.removeAttribute('selected');
            });
            const selectedOption = e.target.querySelector(`option[value="${selectedValue}"]`);
            if (selectedOption) {
                selectedOption.setAttribute('selected', '');
                console.log(`Updated selected attribute for option with value: ${selectedValue}`);
            } else {
                console.warn(`No option found with value: ${selectedValue}`);
            }
            add_to_audit_history(g_current_u_id, `${role_id}_role_jurisdiction_type`, ACTION_TYPE.EDIT_ROLE, e.target.dataset.previousValue, selectedValue, 'jurisdiction_id');
        }
        console.log(`Role ${role_id} jurisdiction changed to: ${selectedValue}`);
        assigned_roles_validation_check();
    }
});

document.addEventListener('blur', function(e) 
{
    if (e.target && e.target.type === 'date') 
    {
        const date = e.target.value;
        e.target.setAttribute('value', date); // Ensure the date input value is set
        const role_id = e.target.id.split('_')[0];
        const role = user_roles.find(role => role._id === role_id);
        if(e.target.id.includes('role_effective_start_date')) 
        {
            role.effective_start_date = date === "" ? "" : new Date(date);
            if (!e.target.validity.valid) 
            {
                addInvalid(`${role_id}_role_effective_start_date`, `${role_id}_role_start_date_validation`, 'Invalid Date');
            } 
            else if (date === "") 
            {
                addInvalid(`${role_id}_role_effective_start_date`, `${role_id}_role_start_date_validation`, 'Start Date is required');
            } 
            else if (role.effective_end_date && new Date(role.effective_end_date) < new Date(date)) 
            {
                addInvalid(`${role_id}_role_effective_end_date`, `${role_id}_role_end_date_validation`, 'Must be after Start Date');
            }
            if (e.target.validity.valid) 
            {
                removeInvalid(`${role_id}_role_effective_start_date`, `${role_id}_role_start_date_validation`);
            }
            add_to_audit_history(g_current_u_id, `${role_id}_role_effective_start_date`, ACTION_TYPE.EDIT_ROLE, e.target.dataset.previousValue, date, 'effective_start_date');
        } 
        else if(e.target.id.includes('role_effective_end_date')) 
        {
            role.effective_end_date = date === "" ? "" : new Date(date);
            if (!e.target.validity.valid) 
            {
                addInvalid(`${role_id}_role_effective_end_date`, `${role_id}_role_end_date_validation`, 'Invalid Date');
            } 
            else if (new Date(date) < new Date(role.effective_start_date)) 
            {
                addInvalid(`${role_id}_role_effective_end_date`, `${role_id}_role_end_date_validation`, 'Must be after Start Date');
            } 
            else if (e.target.validity.valid && new Date(date) >= new Date(role.effective_start_date)) 
            {
                removeInvalid(`${role_id}_role_effective_end_date`, `${role_id}_role_end_date_validation`);
            }
            add_to_audit_history(g_current_u_id, `${role_id}_role_effective_end_date`, ACTION_TYPE.EDIT_ROLE, e.target.dataset.previousValue, date, 'effective_end_date');
        }
        console.log(`Role ${role_id} date changed to: ${date}`);
    }
}, true);

function check_passwords_match() 
{
    const password = document.getElementById('user_password').value;
    const verifyPassword = document.getElementById('user_password_verify').value;

    if (password && verifyPassword) 
    {
        if (password === verifyPassword) 
        {
            document.getElementById('user_password').classList.remove('is-invalid');
            document.getElementById('user_password').style.color = 'green';
            document.getElementById('user_password_verify').classList.remove('is-invalid');
            document.getElementById('user_password_verify').style.color = 'green';
            document.getElementById('show_hide_password').classList.remove('is-invalid-button');
            document.getElementById('show_hide_password_verify').classList.remove('is-invalid-button');
            document.getElementById('password_validation').textContent = '';
            document.getElementById('password_verify_validation').textContent = '';
        } 
        else 
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
        }
    } 
    else 
    {
        document.getElementById('user_password').classList.remove('is-invalid');
        document.getElementById('user_password').style.color = '';
        document.getElementById('user_password_verify').classList.remove('is-invalid');
        document.getElementById('user_password_verify').style.color = '';
        document.getElementById('show_hide_password').classList.remove('is-invalid-button');
        document.getElementById('show_hide_password_verify').classList.remove('is-invalid-button');
        document.getElementById('password_validation').textContent = '';
        document.getElementById('password_verify_validation').textContent = '';
    }
}

function save_user_click() 
{
    disable_save_button();
    disable_undo_button();
    let is_valid = true;
    const user_email = document.getElementById('user_email').value;
    let user_password = document.getElementById('user_password').value;
    let user_password_verify = document.getElementById('user_password_verify').value;

    if(g_policy_values.sams_is_enabled.toLowerCase() == "true") 
    {
        user_password = $mmria.get_guid().replace("-","");
        user_password_verify = user_password;
    }

    if (!user_email) 
    {
        document.getElementById('user_email').classList.add('is-invalid');
        document.getElementById('user_email').style.color = 'red';
        document.getElementById('username_validation').textContent = 'Username is required';
        document.getElementById('username_validation').style.color = 'red';
        is_valid = false;
    }
    if(!user_password) 
    {
        document.getElementById('user_password').classList.add('is-invalid');
        document.getElementById('user_password').style.color = 'red';
        document.getElementById('password_validation').textContent = 'Password is required';
        document.getElementById('password_validation').style.color = 'red';
        document.getElementById('show_hide_password').classList.add('is-invalid-button');
        is_valid = false;
    }
    if(!user_password_verify) 
    {
        document.getElementById('user_password_verify').classList.add('is-invalid');
        document.getElementById('user_password_verify').style.color = 'red';
        document.getElementById('password_verify_validation').textContent = 'Verify Password is required';
        document.getElementById('password_verify_validation').style.color = 'red';
        document.getElementById('show_hide_password_verify').classList.add('is-invalid-button');
        is_valid = false;
    }
    if (user_password !== user_password_verify) 
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
        is_valid = false;
    }
    if (user_password && !is_valid_password(user_password))
    {
        document.getElementById('user_password').classList.add('is-invalid');
        document.getElementById('user_password').style.color = 'red';
        document.getElementById('user_password_verify').classList.add('is-invalid');
        document.getElementById('user_password_verify').style.color = 'red';
        document.getElementById('show_hide_password').classList.add('is-invalid-button');
        document.getElementById('show_hide_password_verify').classList.add('is-invalid-button');
        document.getElementById('password_validation').textContent = 'Invalid password.  Minimum length is: ' + g_policy_values.minimum_length + ' and should only include characters [a-zA-Z0-9!@#$%?* ]';
        document.getElementById('password_validation').style.color = 'red';
        is_valid = false;
    }
    if (user_email && !is_valid_user_name(user_email))
    {
        document.getElementById('user_email').classList.add('is-invalid');
        document.getElementById('user_email').style.color = 'red';
        document.getElementById('username_validation').textContent = 'Invalid user name. User name should be unique and at least 5 characters long';
        document.getElementById('username_validation').style.color = 'red';
        is_valid = false;
    }
    if (!assigned_roles_validation_check()) is_valid = false;
    if (is_valid) check_if_existing_user(user_email, user_password);
    if (is_valid)
    {
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

function disable_save_button()
{
    document.getElementById('user_save_status').classList.add('spinner-active');
    document.getElementById('save_user_button').disabled = true;
    document.getElementById('save_user_button').attributes['aria-disabled'] = 'true';
}

function disable_undo_button()
{
    document.getElementById('undo_button').disabled = true;
    document.getElementById('undo_button').attributes['aria-disabled'] = 'true';
}

function enable_undo_button()
{
    document.getElementById('undo_button').disabled = false;
    document.getElementById('undo_button').attributes['aria-disabled'] = 'false';
}

function enable_save_button()
{
    document.getElementById('user_save_status').classList.remove('spinner-active');
    document.getElementById('save_user_button').disabled = false;
    document.getElementById('save_user_button').attributes['aria-disabled'] = 'false';
}

function assigned_roles_validation_check() {
    let is_valid = true;
    user_roles.forEach(function(role) {
        const role_id = role._id;
        const role_type = document.getElementById(`${role_id}_role_type`).value;
        const role_jurisdiction = document.getElementById(`${role_id}_role_jurisdiction_type`).value;
        const role_effective_start_date = document.getElementById(`${role_id}_role_effective_start_date`).value;
        const role_effective_end_date = document.getElementById(`${role_id}_role_effective_end_date`).value;
        const matching_role_case = user_roles.filter(r => r.role_name === role_type && r.jurisdiction_id === role_jurisdiction);

        if (matching_role_case.length > 1) {
            addInvalid(`${role_id}_role_type`, `${role_id}_role_type_validation`, 'Role/Case combo already exists');
            addInvalid(`${role_id}_role_jurisdiction_type`, `${role_id}_role_jurisdiction_validation`, 'Role/Case combo already exists');
            matching_role_case.forEach(function(r) {
                addInvalid(`${r._id}_role_type`, `${r._id}_role_type_validation`, 'Role/Case combo already exists');
                addInvalid(`${r._id}_role_jurisdiction_type`, `${r._id}_role_jurisdiction_validation`, 'Role/Case combo already exists');
            });
            is_valid = false;
        } else {
            removeInvalid(`${role_id}_role_type`, `${role_id}_role_type_validation`);
            removeInvalid(`${role_id}_role_jurisdiction_type`, `${role_id}_role_jurisdiction_validation`);
        }

        if (!role_type) {
            addInvalid(`${role_id}_role_type`, `${role_id}_role_type_validation`, 'Role is required');
            is_valid = false;
        }
        if (!role_jurisdiction) {
            addInvalid(`${role_id}_role_jurisdiction_type`, `${role_id}_role_jurisdiction_validation`, 'Jurisdiction is required');
            is_valid = false;
        }
        if (!role_effective_start_date) {
            addInvalid(`${role_id}_role_effective_start_date`, `${role_id}_role_start_date_validation`, 'Start Date is required');
            is_valid = false;
        } else {
            removeInvalid(`${role_id}_role_effective_start_date`, `${role_id}_role_start_date_validation`);
        }
        if (role_effective_end_date && new Date(role_effective_end_date) < new Date(role_effective_start_date)) {
            addInvalid(`${role_id}_role_effective_end_date`, `${role_id}_role_end_date_validation`, 'Must be after Start Date');
            is_valid = false;
        } else {
            removeInvalid(`${role_id}_role_effective_end_date`, `${role_id}_role_end_date_validation`);
        }
    });
    return is_valid;
}

function addInvalid(id, validationId, message) {
    const el = document.getElementById(id);
    if (el) {
        el.classList.add('is-invalid');
        el.style.color = 'red';
    }
    const valEl = document.getElementById(validationId);
    if (valEl) {
        valEl.textContent = message;
        valEl.style.color = 'red';
    }
}
function removeInvalid(id, validationId) {
    const el = document.getElementById(id);
    if (el) {
        el.classList.remove('is-invalid');
        el.style.color = '';
    }
    const valEl = document.getElementById(validationId);
    if (valEl) {
        valEl.textContent = '';
        valEl.style.color = '';
    }
}

async function check_if_existing_user(p_user_id, p_user_password)
{
    const response = await get_http_get_response(`api/user/check-user/org.couchdb.user:${p_user_id}`);
    if(response.name === null || response.name === undefined || response.name === "")
    {
        create_user_account(p_user_id, p_user_password);
    }
    else
    {
        document.getElementById('user_email').classList.add('is-invalid');
        document.getElementById('user_email').style.color = 'red';
        document.getElementById('username_validation').textContent = 'Invalid user name. User name should be unique.';
        document.getElementById('username_validation').style.color = 'red';
        is_valid = false;
    }
}

async function create_user_account(p_user_id, p_user_password)
{
    let user = create_user(p_user_id.toLowerCase(), p_user_password);
    g_ui.user_summary_list.push(user);
    
    const user_response = await get_http_post_response('api/user', user);
    if(user_response.ok)
    {
        for(var i = 0; i < g_ui.user_summary_list.length; i++)
        {
            if(g_ui.user_summary_list[i]._id == user_response.id)
            {
                g_ui.user_summary_list[i]._rev = user_response.rev; 
                break;
            }
        }
        if (user_roles && user_roles.length > 0) 
        {
            add_user_roles(p_user_id);
        }
        else
        {
            //TODO: add_audit_history();
            save_audit_history(p_user_id);
        }
    }
}

async function add_user_roles(p_user_id)
{
    let was_successful = true;
    user_roles.forEach(role => {
        role.user_id = p_user_id;
    });
    //initial_user_roles = [...user_roles];
    const user_roles_response = await get_http_post_response
    (
        "api/user_role_jurisdiction/bulk",
        user_roles
    );

    user_roles_response.forEach(response => {
        if (response.ok)
        {
            const user_role = user_roles.find(role => role._id === response.id);
            user_role._rev = response.rev;
            g_user_role_jurisdiction.push(user_role);
        }
        else
        {
            was_successful = false;
            console.error(`Failed to add role ${response.id}:`, response);
        }
    });
    if(was_successful)
    {
        save_audit_history(p_user_id);
    }
}

function save_user(p_user_id)
{
	var user_index = -1;
	var user_list = g_ui.user_summary_list;
	for(var i = 0; i < user_list.length; i++)
	{
		if(user_list[i]._id == p_user_id)
		{
			user_index = i;
			break;
		}
	}
	if(user_index > -1)
	{
		var user = user_list[user_index];
        $.ajax({
            url: location.protocol + '//' + location.host + '/api/user',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            data: JSON.stringify(user),
            type: "POST"
        }).done(function(response) 
        {
            var response_obj = eval(response);
            if(response_obj.ok)
            {
                for(var i = 0; i < g_ui.user_summary_list.length; i++)
                {
                    if(g_ui.user_summary_list[i]._id == response_obj.id)
                    {
                        g_ui.user_summary_list[i]._rev = response_obj.rev; 
                        break;
                    }
                }
                g_render();
                create_status_message("user information saved", convert_to_jquery_id(user._id));
                console.log("password saved sent", response);
            }
        });
	}
}

function create_user(p_name, p_password)
{
    return {
    "_id": "org.couchdb.user:" + p_name,
    "password": p_password,
    "iterations": 10,
    "name": p_name,
    "roles": [  ],
    "type": "user",
    "derived_key": "a1bb5c132df5b7df7654bbfa0e93f9e304e40cfe",
    "salt": "510427706d0deb511649021277b2c05d"
    };
}