
var config = null;
var config_master = null;
var selected_config = "shared";
var applied_config_master = null;
var key_set = new Set();
var copy_key = null;
var copy_value = null;
var copy_config = null;

window.onload = main;

async function main()
{
    config = await get_config();
    config_master = await get_config_master();
    applied_config_master = await get_applied_config_master();

    const boolean_keys =  Object.keys(config_master.boolean_keys);


    if(config_master._id == null)
    {
        config_master._id = applied_config_master._id;
    }

    if(boolean_keys.length == 0)
    {
        config_master.boolean_keys = applied_config_master.boolean_keys;
        for(const key in config_master.boolean_keys)
        {
            key_set.add(key);
        }
    }
    else
    {
        for(const key of boolean_keys)
        {
            key_set.add(key);
        }
    }

    const integer_keys =  Object.keys(config_master.integer_keys);

    if(integer_keys.length == 0)
    {
        config_master.integer_keys = applied_config_master.integer_keys;
        for(const key in config_master.integer_keys)
        {
            key_set.add(key);
        }
    }
    else
    {
        for(const key of integer_keys)
        {
            key_set.add(key);
        }
    }


    const string_keys =  Object.keys(config_master.string_keys);

    if(string_keys.length == 0)
    {
        config_master.string_keys = applied_config_master.string_keys;
        for(const key in config_master.string_keys)
        {
            key_set.add(key);
        }
    }
    else
    {
        for(const key of string_keys)
        {
            key_set.add(key);
        }
    }

    const el = document.getElementById("content");

    const result = render();
    
    el.innerHTML = result.join("");
}

async function get_config()
{
    const url = `${location.protocol}//${location.host}/_config/GetConfiguration`;

    const result = await $.ajax
    ({
        url: url,
    });

    return result;
}

async function get_config_master()
{
    const url = `${location.protocol}//${location.host}/_config/GetConfigurationMaster`;

    const result = await $.ajax
    ({
        url: url,
    });

    return result;
}

async function get_applied_config_master()
{
    const url = `${location.protocol}//${location.host}/_config/GetAppliedConfiguration`;

    const result = await $.ajax
    ({
        url: url,
    });

    return result;
}


async function save()
{
    const save_output = document.getElementById("save_output");

    const response = await set_config_master();

    if(response.ok)
    {
        save_output.innerHTML = "Config saved.";
    }
    else
    {
        save_output.innerHTML = "Problem saving";
    }

    window.setTimeout(clear_save_message, 3000);
}

function clear_save_message()
{
    save_output.innerHTML = "";
}

async function set_config_master()
{
    const url = `${location.protocol}//${location.host}/_config/SetConfigurationMaster`;

    const response = await $.ajax
    ({
        url: url,
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify(config_master),
        type: 'POST',
    });

    if (response.ok) 
    {
        config_master._rev = response.rev;
    }

    return response;
}


function render_config_select(value)
{
    const result = [];

    result.push('<select id="prefix" size=5 onchange="config_selection_changed(this.value)">');

    for (const key of key_set) 
    {
        if(key == value)
        {
            result.push(`<option value="${key}" selected>${key}</option>`);
        }
        else
        {
            result.push(`<option value="${key}">${key}</option>`);
        }
    }

    result.push('</select>');

    return result.join('');
}

function render()
{

    const result = []

    result.push(
   `<br/>
   <br/>
   <div id="host_site"><b>host site:</b> ${host_site}</div> 
   <br/>
<div id="select">${render_config_select(selected_config)}</div>

<div>

<br/>
<input type="text" id="add_config" value=""></input>
<input type="button" value="add" onclick="add_item('add_config')"></input>
<input id="delete-config" type="button" value="delete [${selected_config}}]" onclick="delete_item('delete-config-${selected_config}')"></input>
</div>
<br/>
<div id="copy_buffer">&nbsp</div>
<br/>
</tr>
<hr/>

<div id="db_keys" style="background-color:#BBBBCB;font-family:tahoma;">${render_db_keys()}</div>
<hr/>
<div id="sams_keys" style="background-color:#BBCBBB;font-family:tahoma;">${render_sams_keys()}</div>
<hr/>
<div id="boolean_keys" style="background-color:#CBBBBB;font-family:tahoma;">${render_boolean_keys()}</div>
<hr/>
<div id="integer_keys" style="background-color:#CBCBBB;font-family:tahoma;">${render_integer_keys()}</div>
<hr/>
<div id="string_keys" style="background-color:#CDCCCD;font-family:tahoma;">${render_string_keys()}</div>

<hr/>
<br/>
<input type="button" value="save" onclick="save()"/>
<br/>
<div id="save_output"></div>
<br/>
<br/>
    `
    );

    return result;
}

function render_boolean_keys()
{
    const result = [];
    if(selected_config == null) 
        selected_config = "shared";

    const val = config_master.boolean_keys[selected_config];

    if(val == null) return "";

    result.push(
        ` <table>
        <tr>
            <th colspan=3>boolean keys [${selected_config}]</th>
        </tr>        
        `
    )

    
    for (const key in val) 
    {
        const value = val[key];
        const size = typeof(value) === "string" ? value.length + 3: value.toString().length + 3;

        if(typeof(value) === 'object')
        {


        }
        else
        {
            const id = `detail-${selected_config}-${key}`;
            result.push(`<tr><td colspan=3><label><b>${key}</b> <input id="${id}" type="text" value="${value}" size="${size}" onblur="update_boolean_item('${id}')"></input></label>`);
            result.push(` <input type="button" value="copy" onclick="copy_item('${id}')"></input> | 
                        <input type="button" value="delete" onclick="delete_item('${id}')"></input></td></tr>`)

        }
        
    }

    const id = `add_new_boolean_key-${selected_config}`;
    result.push(`<tr>
    <td>
    <input type="text" id="${id}" value=""></input>
    <input type="button" value="add key" onclick="add_item('${id}')"></input>
    </td>
    <td>
    &nbsp;
    </td>
    <td>
    <input type="button" value="paste" onclick="paste_item('${id}')"></input>
    </td>

    </tr>`);

    result.push("</table>");

    return result.join("");

}

function render_integer_keys()
{
    const result = [];
    if(selected_config == null) 
        selected_config = "shared";

    const val = config_master.integer_keys[selected_config];

    if(val == null) return "";

    result.push(
        ` <table>
        <tr>
            <th colspan=3>integer keys [${selected_config}]</th>
        </tr>        
        `
    )

    
    for (const key in val) 
    {
        const value = val[key];
        const size = typeof(value) === "string" ? value.length + 3: value.toString().length + 3;

        if(typeof(value) === 'object')
        {


        }
        else
        {
            const id = `detail-${selected_config}-${key}`;
            result.push(`<tr><td colspan=3><label><b>${key}</b> <input id="${id}" type="text" value="${value}" size="${size}" onblur="update_integer_item('${id}')"></input></label>`);
            result.push(` <input type="button" value="copy" onclick="copy_item('${id}')"></input> | 
                        <input type="button" value="delete" onclick="delete_item('${id}')"></input></td></tr>`)

        }
        
    }

    const id = `add_new_integer_key-${selected_config}`;
    result.push(`<tr>
    <td>
    <input type="text" id="${id}" value=""></input>
    <input type="button" value="add key" onclick="add_item('${id}')"></input>
    </td>
    <td>
    &nbsp;
    </td>
    <td>
    <input type="button" value="paste" onclick="paste_item('${id}')"></input>
    </td>

    </tr>`);

    result.push("</table>");

    return result.join("");

}

function render_string_keys()
{
    const result = [];
    if(selected_config == null) 
        selected_config = "shared";

    const val = config_master.string_keys[selected_config];

    if(val == null) return "";

    result.push(
        ` <table>
        <tr>
            <th colspan=3>string keys [${selected_config}]</th>
        </tr>        
        `
    )

    
    for (const key in val) 
    {
        const value = val[key];
        const size = typeof(value) === "string" ? value.length + 3: value.toString().length + 3;

        if
        (
            typeof(value) === 'object' ||
            db_key_set.has(key) ||
            sams_key_set.has(key)
        )
        {


        }
        else
        {
            const id = `detail-${selected_config}-${key}`;
            result.push(`<tr><td colspan=3><label><b>${key}</b> <input id="${id}" type="text" value="${value}" size="${size}" onblur="update_string_item('${id}')"></input></label>`);
            result.push(` <input type="button" value="copy" onclick="copy_item('${id}')"></input> | 
                        <input type="button" value="delete" onclick="delete_item('${id}')"></input></td></tr>`)

        }
        
    }

    const id = `add_new_string_key-${selected_config}`;
    result.push(`<tr>
    <td>
    <input type="text" id="${id}" value=""></input>
    <input type="button" value="add key" onclick="add_item('${id}')"></input>
    </td>
    <td>
    &nbsp;
    </td>
    <td>
    <input type="button" value="paste" onclick="paste_item('${id}')"></input>
    </td>

    </tr>`);

    result.push("</table>");

    return result.join("");

}


const db_key_set = new Set();
db_key_set.add()
db_key_set.add("couchdb_url");
db_key_set.add("db_prefix");
db_key_set.add("timer_user_name");
db_key_set.add("timer_value");
//db_key_set.add("app_instance_name");

function render_db_keys()
{

    const result = [];
    if(selected_config == null) 
        selected_config = "shared";

    const val = config_master.string_keys[selected_config];

    if(val == null) return "";

    result.push(
        ` <table>
        <tr>
            <th colspan=3>db string keys [${selected_config}]</th>
        </tr>        
        `
    )

    let key_count = 0;

    render_key("couchdb_url");
    render_key("db_prefix");
    render_key("timer_user_name");
    render_key("timer_value");
    //render_key("app_instance_name");
    

    function render_key(key)
    {
        const value = val[key];
        if(value == null) return;
        key_count+=1;

        const size = typeof(value) === "string" ? value.length + 3: value.toString().length + 3;


            const id = `detail-${selected_config}-${key}`;
            result.push(`<tr><td colspan=3><label><b>${key}</b> <input id="${id}" type="text" value="${value}" size="${size}" onblur="update_string_item('${id}')"></input> ${(key=="db_prefix")? " <- include trailing underscore":""}</label>`);
            result.push(` <input type="button" value="copy" onclick="copy_item('${id}')"></input> | 
                        <input type="button" value="delete" onclick="delete_item('${id}')"></input></td></tr>`)
    }

    if(key_count == 0)
    {
        const id = `add_new_db_string_set-${selected_config}`;
        result.push(`<tr>
        <td>
        <input type="button" value="add db string set" onclick="add_item('${id}')"></input>
        </td>
        <td>
        &nbsp;
        </td>
        </tr>`);
    }

    result.push("</table>");

    return result.join("");

}


const sams_key_set = new Set();
sams_key_set.add("sams:client_id");
sams_key_set.add("sams:client_secret");
sams_key_set.add("sams:callback_url");
sams_key_set.add("sams:activity_name");

function render_sams_keys()
{
    const result = [];
    if(selected_config == null) 
        selected_config = "shared";

    const val = config_master.string_keys[selected_config];

    let key_count = 0;

    if(val == null) return "";

    result.push(
        ` <table>
        <tr>
            <th colspan=3>sams client keys [${selected_config}]</th>
        </tr>        
        `
    )


    render_key("sams:client_id");
    render_key("sams:client_secret");
    render_key("sams:callback_url");
    render_key("sams:activity_name");
        
    function render_key(key)
    {
        const value = val[key];

        if(value == null) return;

        key_count += 1;

        const size = typeof(value) === "string" ? value.length + 3: value.toString().length + 3;


            const id = `detail-${selected_config}-${key}`;
            result.push(`<tr><td colspan=3><label><b>${key}</b> <input id="${id}" type="text" value="${value}" size="${size}" onblur="update_string_item('${id}')"></input></label>`);
            result.push(` <input type="button" value="copy" onclick="copy_item('${id}')"></input> | 
                        <input type="button" value="delete" onclick="delete_item('${id}')"></input></td></tr>`)
    }

    if(key_count == 0)
    {
        const id = `add_new_custom_sams_string_set-${selected_config}`;
        result.push(`<tr>
        <td>
        <input type="button" value="add new sams custom string set" onclick="add_item('${id}')"></input>
        </td>
        <td>
        &nbsp;
        </td>
        </tr>`);
    }
    result.push("</table>");

    return result.join("");

}


function config_selection_changed(value)
{
    if(value == null) return;

    selected_config = value;



    document.getElementById("db_keys").innerHTML = render_db_keys(selected_config);

    document.getElementById("sams_keys").innerHTML = render_sams_keys(selected_config);

    document.getElementById("delete-config").value = `delete [${selected_config}]`;

    document.getElementById("boolean_keys").innerHTML = render_boolean_keys(selected_config);

    document.getElementById("integer_keys").innerHTML = render_integer_keys(selected_config);

    document.getElementById("string_keys").innerHTML = render_string_keys(selected_config);

}

function copy_item(p_id)
{
    const el_id = document.getElementById(p_id);
    //let new_value = null;
    //let config_name = null;

    const arr = p_id.trim().split("-");

    copy_config = arr[1];
    copy_key= arr[2];
    copy_value = el_id.value;

    const copy_buffer = document.getElementById("copy_buffer");
    copy_buffer.innerHTML = `config: ${copy_config} key: ${copy_key} value: ${copy_value}`;

}

function delete_item(p_id)
{
    console.log("delete: " + p_id);
}

function add_item(p_id)
{
    const el_id = document.getElementById(p_id);
    let new_value = null;
    let config_name = null;

    const arr = p_id.trim().split("-");
    switch(arr[0])
    {
        case "add_config":
            
            new_value = el_id.value.trim().toLowerCase();
            if(new_value.trim().length < 1) return;

            key_set.add(new_value)
            config_master.boolean_keys[new_value] = {};
            config_master.integer_keys[new_value] = {};
            config_master.string_keys[new_value] = {};
            const select = document.getElementById("select");
            select.innerHTML = render_config_select();
            break;
        case "add_new_boolean_key":
            new_value = el_id.value.trim().toLowerCase();
            if(new_value.length == 0) return;
            config_name = arr[1];
            config_master.boolean_keys[config_name][new_value] = false;
            document.getElementById("boolean_keys").innerHTML = render_boolean_keys(selected_config);
            break;
        case "add_new_integer_key":
            new_value = el_id.value.trim().toLowerCase();
            if(new_value.length == 0) return;
            config_name = arr[1];
            config_master.integer_keys[config_name][new_value] = 0;
            document.getElementById("integer_keys").innerHTML = render_integer_keys(selected_config);
            break;

        case "add_new_string_key":
            new_value = el_id.value.trim().toLowerCase();
            if(new_value.length == 0) return;
            config_name = arr[1];
            config_master.string_keys[config_name][new_value] = "";
            document.getElementById("string_keys").innerHTML = render_string_keys(selected_config);
            break;

        case "add_new_db_string_set":
            config_name = arr[1];

            if(!config_master.string_keys[config_name].hasOwnProperty("couchdb_url"))
                config_master.string_keys[config_name]["couchdb_url"] = "";

            if(!config_master.string_keys[config_name].hasOwnProperty("db_prefix"))
                config_master.string_keys[config_name]["db_prefix"] = "";

            if(!config_master.string_keys[config_name].hasOwnProperty("timer_user_name"))
                config_master.string_keys[config_name]["timer_user_name"] = "";

            if(!config_master.string_keys[config_name].hasOwnProperty("timer_value"))
                config_master.string_keys[config_name]["timer_value"] = "";

            document.getElementById("db_keys").innerHTML = render_db_keys(selected_config);
            
            break;

        case "add_new_custom_sams_string_set":
            config_name = arr[1];
            if(!config_master.string_keys[config_name].hasOwnProperty("sams:client_id"))
                config_master.string_keys[config_name]["sams:client_id"] = "";
            
            if(!config_master.string_keys[config_name].hasOwnProperty("sams:client_secret"))
                config_master.string_keys[config_name]["sams:client_secret"] = "";
        
            if(!config_master.string_keys[config_name].hasOwnProperty("sams:callback_url"))
                config_master.string_keys[config_name]["sams:callback_url"] = "";
        
            if(!config_master.string_keys[config_name].hasOwnProperty("sams:activity_name"))
                config_master.string_keys[config_name]["sams:activity_name"] = "";
        
        
            document.getElementById("sams_keys").innerHTML = render_sams_keys(selected_config);
            
            break;
    }
}


function update_boolean_item(p_id)
{
    console.log("update: " + p_id);

    const el_id = document.getElementById(p_id);
    const new_value = el_id.value;


    const arr = p_id.trim().split("-");
    const config_name = arr[1];
    const key = arr[2];
    config_master.boolean_keys[config_name][key] = new_value;

    const size = typeof(new_value) === "string" ? new_value.length + 3: new_value.toString().length + 3;
    el_id.size = size;
    

}

function update_integer_item(p_id)
{
    console.log("update: " + p_id);

    const el_id = document.getElementById(p_id);
    const new_value = el_id.value;


    const arr = p_id.trim().split("-");
    const config_name = arr[1];
    const key = arr[2];
    config_master.integer_keys[config_name][key] = new_value;

    const size = typeof(new_value) === "string" ? new_value.length + 3: new_value.toString().length + 3;
    el_id.size = size;

}

function update_string_item(p_id)
{
    console.log("update: " + p_id);

    const el_id = document.getElementById(p_id);
    const new_value = el_id.value;


    const arr = p_id.trim().split("-");
    const config_name = arr[1];
    const key = arr[2];

    config_master.string_keys[config_name][key] = new_value;

    const size = typeof(new_value) === "string" ? new_value.length + 3: new_value.toString().length + 3;
    el_id.size = size;
}

function paste_item(p_id)
{
    function isInt(value) 
    {
        var x = parseFloat(value);
        return !isNaN(value) && (x | 0) === x;
    }

    //const el_id = document.getElementById(p_id);
    //let new_value = null;
    //let config_name = null;

    const arr = p_id.trim().split("-");

    const target_copy_config = arr[1];
    //const target_copy_key= arr[2];
    //const target_copy_value = el_id.value;
    
    if
    (
        typeof copy_value === 'boolean' ||
        copy_value == "true" ||
        copy_value == "false"
    )
    {
        config_master.boolean_keys[target_copy_config][copy_key] = copy_value;
        document.getElementById("boolean_keys").innerHTML = render_boolean_keys(selected_config);
    }
    else if(isInt(copy_value))
    {
        config_master.integer_keys[target_copy_config][copy_key] = copy_value;
        document.getElementById("integer_keys").innerHTML = render_integer_keys(selected_config);
    }
    else
    {
        config_master.string_keys[target_copy_config][copy_key] = copy_value;
        document.getElementById("string_keys").innerHTML = render_string_keys(selected_config);
    }
}