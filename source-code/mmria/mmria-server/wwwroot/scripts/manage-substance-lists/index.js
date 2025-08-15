
var g_substance_mapping = null;
var g_selected_list = null;
var g_metadata;
var g_release_version;

var g_substance_text_count = {};

var g_value_list = null;

var message_history = [];

window.onload = function () 
{
    let selection_list = document.getElementById("select-list");
    selection_list.onchange = on_selection_changed;

    let save_button = document.getElementById("save_button");
    save_button.onclick = confirm_save;

    get_release_version();
}

function on_selection_changed()
{
    let selection_list = document.getElementById("select-list");
    g_selected_list = selection_list.value;
    render();
}

function get_release_version() 
{
  $.ajax({
    url:
      location.protocol + '//' + location.host + '/api/version/release-version',
  }).done(function (response) {
    g_release_version = response;
    load_metadata();
  });
}

function load_metadata()
{
	var metadata_url = location.protocol + '//' + location.host + `/api/version/${g_release_version}/metadata`;

	$.ajax({
			url: metadata_url
	}).done(function(response) 
    {
			g_metadata = response;

            let subtance_node = null;

            for(let i = 0; i < g_metadata.lookup.length; i++)
            {
                let item = g_metadata.lookup[i];
                if(item.name == "substance")
                {
                    g_value_list = item.values;
                    break;
                }
            }

            load_substance_mapping();

	});
}

function load_substance_mapping()
{

	$.ajax({
		url: location.protocol + '//' + location.host + '/api/substance_mapping',
	}).done(function(response) 
	{
		g_substance_mapping = response;


        g_substance_text_count
		
		render();
	});

}

function correct_trim()
{
    let selected_list = g_substance_mapping.substance_lists[g_selected_list];
    if(selected_list)
    {
        g_substance_text_count = {};
        for(let i = 0; i < selected_list.length; i++)
        {
            let item = selected_list[i];

            item.source_value = item.source_value.trim();
        }
    }
    
}

function count_substances()
{
    let selected_list = g_substance_mapping.substance_lists[g_selected_list];
    if(selected_list)
    {
        g_substance_text_count = {};
        for(let i = 0; i < selected_list.length; i++)
        {
            let item = selected_list[i];

            if(g_substance_text_count[item.source_value.toLowerCase()]!=null)
            {
                g_substance_text_count[item.source_value.toLowerCase()] += 1;
            }
            else
            {
                g_substance_text_count[item.source_value.toLowerCase()] = 1;
            }
        }
    }
    
}

function render_substance_list(p_id, p_value)
{
    let html = [];
    html.push(`<select aria-label='Substance list' id='${p_id}'>`);
    for(let i = 0; i < g_value_list.length; i++)
    {
        let item = g_value_list[i];
        let is_selected = "";
        if
        (
            (
                p_value == null || 
                p_value == ""
            ) &&
            item.value == "Other"
        )
        {
            is_selected = "selected";
        }
        else if(item.value == p_value)
        {
            is_selected = "selected";
        }

        html.push(`<option value='${item.value}' ${is_selected}>${item.display}</option>`);
    }

    html.push("</select>");

    return html.join("");
}

function render_header()
{
    let html = [];

    html.push(`rev: ${g_substance_mapping._rev}`)

    document.getElementById('header-id').innerHTML = html.join("");
}

function render_messages()
{
    var html = [];
    html.push("<select aria-label='Message History' size=3>");
    for(let i = message_history.length - 1; i > -1; i--)
    {
        
        html.push(`<option>${message_history[i]}</option>`);
    }
    html.push("</select>");

    document.getElementById("message-area-id").innerHTML = html.join("");
}

function render()
{
    render_header();

    let html = [];

    correct_trim();
    count_substances();

    let selected_list = g_substance_mapping.substance_lists[g_selected_list];
    if(selected_list)
    {
        html.push(`<br/><table border=1><tr bgcolor=silver align=center><th colspan=3>${g_selected_list}</th></tr>`);
        html.push(`<tr bgcolor=silver><th><a href="javascript:sort_by_source()">Other Specify Text</a></th><th><a href="javascript:sort_by_target()">=> List Value</a></th><th>action</th></tr>`);
        for(let i = 0; i < selected_list.length; i++)
        {
            let item = selected_list[i];
            let color = "";
            if(i % 2 == 1)
            {
                color = "bgcolor=CCCCCC";
            }

            if(g_substance_text_count[item.source_value.toLowerCase()] > 1)
            {
                color = "bgcolor=FFCCCC";
            }
            html.push(`<tr id=item-${i} ${color}><td>${item.source_value}</td><td>=> ${item.target_value}</td><td><a href="javascript:select_row(${i})">edit</a> | <a href="javascript:confirm_delete(${i})">remove</a></td></tr>`)
        }
        html.push(`<tr><td><input aria-label="Enter new source item" id=new-source-item type="text" value=""/></td><td>=> ${render_substance_list("new-target-item","")}</td><td> <a href="javascript:add_row()">Add New Mapping</a></td></tr>`);
        html.push('</table>')
    }

	document.getElementById('output').innerHTML = html.join("");
    render_messages();
}

function select_row(p_index)
{
    let selected_list = g_substance_mapping.substance_lists[g_selected_list];
    let item = selected_list[p_index];
    console.log('bbbubba ' + item.source_value);

    let element = document.getElementById(`item-${p_index}`);

    let html = [];
    html.push(`<td>${item.source_value}</td><td>=> ${render_substance_list("new-item-"+ p_index, item.target_value)}</td><td><a href="javascript:cancel_row(${p_index})">cancel</a> | <a href="javascript:save_row(${p_index})">save</a></td>`);
    element.innerHTML = html.join("");

}

function confirm_delete(p_index)
{
    let selected_list = g_substance_mapping.substance_lists[g_selected_list];
    let item = selected_list[p_index];

    if(prompt(`are you sure you want to remove mapping ${item.source_value} => ${item.target_value}`, "no").toLocaleLowerCase() == "yes")
    {
        delete_row(p_index);
    }
    else
    {
        cancel_row(p_index);
    }

}

function delete_row(p_index)
{
    let selected_list = g_substance_mapping.substance_lists[g_selected_list];
    let item = selected_list[p_index];
    console.log('bbbubba ' + item.source_value);

    selected_list.splice(p_index, 1);

    render();

}

function cancel_row(p_index)
{
    let selected_list = g_substance_mapping.substance_lists[g_selected_list];
    let item = selected_list[p_index];
    console.log('bbbubba ' + item.source_value);

    let element = document.getElementById(`item-${p_index}`);
    

    let html = [];
    html.push(`<td>${item.source_value}</td><td>${item.target_value}</td><td><a href="javascript:select_row(${p_index})">edit</a> | <a href="javascript:confirm_delete(${p_index})">remove</a></td>`);
    element.innerHTML = html.join("");

}

function save_row(p_index)
{
    
    let selected_list = g_substance_mapping.substance_lists[g_selected_list];
    let item = selected_list[p_index];

    let input_element = document.getElementById(`new-item-${p_index}`);
    selected_list[p_index].target_value = input_element.value.trim();

    console.log('bbbubba ' + item.source_value);

    let element = document.getElementById(`item-${p_index}`);
    let html = [];
    html.push(`<td>${item.source_value}</td><td>${item.target_value}</td><td><a href="javascript:select_row(${p_index})">edit</a> | <a href="javascript:confirm_delete(${p_index})">remove</a></td>`);
    element.innerHTML = html.join("");

}

function add_row()
{
    
    let selected_list = g_substance_mapping.substance_lists[g_selected_list];

    let source_element = document.getElementById(`new-source-item`);
    let target_element = document.getElementById(`new-target-item`);
    selected_list.push({ "source_value": source_element.value.trim(), "target_value": target_element.value.trim() });

    render();

}


function validate_save()
{
    let result = 0;

    let duplicate_entries = [];

    let indexes = [
    "autopsy_report/toxicology/substance",
    "prenatal/substance_use_grid/substance",
    "social_and_environmental_profile/if_yes_specify_substances/substance"
    ];

    for(let index = 0; index < indexes.length; index++)
    {
        let list_name = indexes[index];
        let selected_list = g_substance_mapping.substance_lists[list_name];
        if(selected_list)
        {
            g_substance_text_count = {};
            for(let i = 0; i < selected_list.length; i++)
            {
                let item = selected_list[i];

                if(g_substance_text_count[item.source_value.toLowerCase()]!=null)
                {
                    g_substance_text_count[item.source_value] += 1;
                    duplicate_entries.push(`${list_name} - ${item.source_value}`);
                }
                else
                {
                    g_substance_text_count[item.source_value.toLowerCase()] = 1;
                }
            }
        }
    }

    if(duplicate_entries.length > 0)
    {
        if(prompt(`Validation: Duplicate entries found for:\n${duplicate_entries.join("\n")}`,"no").toLocaleLowerCase() == "yes")
        {
            result = 2;
        }
        else
        {
            result = 1;
        }
    }
    else
    {
        result = 0;
    }

    return result;
}

function confirm_save()
{
    let message_area = document.getElementById("message-area-id");
    message_area.innerHTML = "";
    let validate_result = validate_save();

    if(validate_result == 1)
    {
        message_history.push(`${new Date()}: Save cancelled`);
        render_messages();
    }
    else if(validate_result == 2)
    {
        server_save();
    }
    else if(prompt(`are you sure you want to save your changes?`, "no").toLocaleLowerCase() == "yes")
    {
        server_save();
    }

}


function server_save()
{

	$.ajax({
				url: location.protocol + '//' + location.host + '/api/substance_mapping',
				contentType: 'application/json; charset=utf-8',
				dataType: 'json',
				data: JSON.stringify(g_substance_mapping),
				type: "POST"
		}).done(function(response) 
		{

			let response_obj = eval(response);
			if(response_obj.ok)
			{
				g_substance_mapping._rev = response_obj.rev; 
                message_history.push(`${new Date()}: Save successful.  Data saved to database.`);
				render();
			}
            else
            {
                message_history.push(`${new Date()}: Problem saving!  Data NOT saved to database.`);
                render_messages();
            }
		});
		
}
function compare_source_value(a, b)
{
    return a.source_value.localeCompare(b.source_value);
}

function compare_target_value(a, b)
{
    return a.target_value.localeCompare(b.target_value);
}

function sort_by_source()
{
    let selected_list = g_substance_mapping.substance_lists[g_selected_list];
    selected_list.sort(compare_source_value);   
    
    render();
}

function sort_by_target()
{
    let selected_list = g_substance_mapping.substance_lists[g_selected_list];
    selected_list.sort(compare_target_value);    

    render();
}

function encodeHTML(s) 
{
	let result = s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
    return result;
}
