var g_de_identified_list = null;
var g_selected_list = null;
var g_selected_index = -1;
var g_selected_clone_source = null;
var g_release_version = null;
var g_metadata = null;
var g_form_map = new Map();

$(function ()
{//http://www.w3schools.com/html/html_layout.asp
  'use strict';
	/*profile.on_login_call_back = function (){
				load_users();
    };*/
	//profile.initialize_profile();

	load_report_set();

	$(document).keydown(function(evt){
		if (evt.keyCode==83 && (evt.ctrlKey)){
			evt.preventDefault();
			//metadata_save();
		}
	});



	window.onhashchange = function(e)
	{
		if(e.isTrusted)
		{
			var new_url = e.newURL || window.location.href;

			g_ui.url_state = url_monitor.get_url_state(new_url);
		}
	};
});



async function load_report_set()
{

    const release_version = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/version/release-version`,
    });
    
    
    g_release_version = release_version;

    const metadata_response = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/version/${g_release_version}/metadata`,
    });

    g_metadata = metadata_response;


    create_metadata_map(g_form_map, g_metadata, "");

	const g_de_identified_list_response = await $.ajax
    ({
		url: location.protocol + '//' + location.host + '/api/export_list_manager',
	});

    g_de_identified_list = g_de_identified_list_response;

    g_selected_list = Object.keys( g_de_identified_list.name_path_list)[0];

    if
    (
        g_de_identified_list.sort_order == null ||
        g_de_identified_list.sort_order.length == 0
    )
    {
        g_de_identified_list.sort_order = Object.keys( g_de_identified_list.name_path_list);
    }
    
    document.getElementById('output').innerHTML = render_de_identified_list().join("");


}



function on_clone_source_change(p_value)
{
    g_selected_clone_source = p_value;

    //document.getElementById('output').innerHTML = render_de_identified_list().join("");
}


function on_export_list_type_change(p_value)
{
    g_selected_list = p_value;

    document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

function render_de_identified_list()
{

	var result = [];
	result.push(`<table class='table'>
                    <thead>
                        <tr class='header-level-2'>
                            <th></th>
                            <th>List</th>                                                        
                            <th>Actions</th>
                        </tr>
                    </thead>
                `);
    result.push("<tbody>");
    for(const sort_index in g_de_identified_list.sort_order)
    {
        const sort_index_number = new Number(sort_index) + 1;
        const list_name =  g_de_identified_list.sort_order[sort_index];
        result.push(`<tr draggable='true' ondragstart='handle_drag_start(event, "${list_name}")' ondragover='handle_drag_over(event)' ondrop='handle_drop(event, "${list_name}")' ondragend='handle_drag_end(event)'>`);
        result.push(`<td><img id='drag_${list_name}' src='./img/icon_drag_drop.svg' style="cursor:grab;" />  <input style="width: 50px;" type='text'class='form-control' value='${sort_index_number}' onchange='update_sort_order("${list_name}", this.value)'></input></td>`);
        result.push(`<td><input  type='text' class='form-control' value='${list_name}' onchange='update_list_name("${list_name}", this.value)'></input></td>`);
        result.push(`<td>
                           <button class='secondary-button' aria-label='Add New Item' onclick='add_new_item_click()'>
                        Clone Fields
                    </button>
            <button class='delete-button' onclick='remove_name_path_list_click("${list_name}")'>Delete List</button></td>`);
        result.push("</tr>");

    }
        result.push("<tr>");
        result.push(`<td></td>`);
        result.push(`<td><input class='form-control' type='text' id='new_list_name' value='' title='Enter new list name' placeholder='Enter new list name'></input></td>`);
        result.push(`<td>
                    <button class='secondary-button' aria-label='Add New List' onclick='add_name_path_list_click()'>
                        <span class='x16 cdc-icon-plus pl-2'>
                            <span style='padding-left: 4px;'>Add New List</span>
                        </span>
                    </button>
            </td>`);
        result.push("</tr>");


    result.push("</tbody>");

      // const list_name =  g_de_identified_list.sort_order[sort_index];
      // if(list_name == g_selected_list)
      // {
      //     result.push(`<option value='${list_name}' selected>${list_name}</option>`);
      // }
      // else
      // {
      //     result.push(`<option value='${list_name}'>${list_name}</option>`);
      // }
    
  
    
    result.push(`
    </td>
    <td valign='top'>
    <input id='sort-order' type='text' value=${g_de_identified_list.sort_order.indexOf(g_selected_list) + 1} placeholder='Sort Order' title='Sort Order' onchange='update_sort_order("${g_selected_list}", this.value)' style='text-align:center;' />
    </td>
    <td valign='top'>
    <input type='button' value='remove [${g_selected_list}] list ...' onclick='remove_name_path_list_click()'/>
    </td>
    </tr>



    </table>
    `);
    
    
    
    result.push("<hr/><br/>");




    result.push('<div class="row mb-2 mt-2"><div class="col-md-4 horizontal-control">');
     result.push("<label style='width:175px;' for='export-list-type'>Selected list:</label>");

  result.push("<select id='export-list-type' onchange='on_export_list_type_change(this.value)' class='form-select form-control'>");

    for(const sort_index in g_de_identified_list.sort_order)
    {
        const list_name =  g_de_identified_list.sort_order[sort_index];
        if(list_name == g_selected_list)
        {
            result.push(`<option value='${list_name}' selected>${list_name}</option>`);
        }
        else
        {
            result.push(`<option value='${list_name}'>${list_name}</option>`);
        }
    }

    result.push("</select>")
  
    result.push('</div>');
    result.push(`<div class='col-md-6'>

                </div></div>`);


    result.push('<div class="row mb-2 mt-2"><div class="col-md-4 horizontal-control">');
   result.push("<label style='width:175px;' for='clone-source'>Clone source:</label> <select class='form-select form-control' id='clone-source' onchange='on_clone_source_change(this.value)'>");
    
    result.push(`<option value='9999' disabled='' style='font-weight:bold;'> lists</option>`);
    for (let [key, value] of Object.entries(g_de_identified_list.name_path_list)) 
    {
        if(key == g_selected_clone_source)
        {
            result.push(`<option value='${key}' selected>${key}</option>`);
        }
        else
        {
            result.push(`<option value='${key}'>${key}</option>`);
        }
    }
    result.push(`<option value='9999' disabled='' style='font-weight:bold;'>form</option>`);

    g_form_map.forEach((value, key)=>
    {
        if(key == g_selected_clone_source)
        {
            result.push(`<option value='${key}' selected>${key}</option>`);
        }
        else
        {
            result.push(`<option value='${key}'>${key}</option>`);
        }
        
    });
    result.push("</select>")    
    result.push('</div>');
    result.push(`<div class='col-md-6 mb-2'>
                <button class='secondary-button' onclick='clone_list_click()'>Clone Fields</button>
                </div></div>`);




    let selected_list = g_de_identified_list.name_path_list[g_selected_list];
  result.push('<div class="row mb-2"><div class="col-md-6">');
    result.push("<button class='primary-button' onclick='server_save()'>Save Custom Lists</button>");
    result.push('</div>');
    result.push(`<div class='col-md-6'>
                    <button class='secondary-button d-flex float-right' aria-label='Add New Item' onclick='add_new_item_click()'>
                        <span class='x16 cdc-icon-plus pl-2'>
                        <span style='padding-left: 4px;'>Add New Field</span>
                        </span>
                    </button>
                </div></div>`);
	result.push(`<table class='table'>
                <thead>
                        <tr class='header-level-2'>
                            <th></th>
                            <th>List</th>                                                        
                            <th>Actions</th>
                        </tr>
                    </thead><tbody>`);
	
	

	//result.push("<tr><td colspan=2 align=center><input type='button' value='save list' onclick='server_save()' /></td></tr>")

	
	for(let i in selected_list)
	{
		let item = selected_list[i];

		if(i % 2)
		{
			result.push("<tr bgcolor='#CCCCCC' draggable='true' ondragstart='handle_field_drag_start(event, " + i + ")' ondragover='handle_drag_over(event)' ondrop='handle_field_drop(event, " + i + ")' ondragend='handle_drag_end(event)'>");
		}
		else
		{
			result.push("<tr draggable='true' ondragstart='handle_field_drag_start(event, " + i + ")' ondragover='handle_drag_over(event)' ondrop='handle_field_drop(event, " + i + ")' ondragend='handle_drag_end(event)'>");
		}
        let row_number = new Number(i);
        row_number++;
        result.push(`<td><img src='./img/icon_drag_drop.svg' style="cursor:grab;" /> ${row_number} </td>`)
		result.push(`<td>`);
		result.push(`<input id='row_${row_number}' class='form-control' size='95' type='text' title='${item}' aria-labelledby='path_label' value='`);
		result.push(item);
		result.push("' onblur='update_item("+ i+", this.value)'/></label></td>");
		result.push("<td><button class='secondary-button' onclick=cut_selected(${row_number})>Clone Fields</button>  <button class='secondary-button' onclick=paste_selected(${row_number})>Paste Field</button>  <button class='secondary-button' onclick='delete_item(" + i + ")'>Delete Field</button></td>");
		result.push("</tr>");		
		
	}


	result.push("<tr><td colspan=3 align=center><input type='button' value='save lists' onclick='server_save()' /></td></tr>")

	
	result.push("</tbody></table>");
	  result.push('<div class="row mb-2"><div class="col-md-6">');
    result.push("<button class='primary-button' onclick='server_save()'>Save Custom Lists</button>");
    result.push('</div>');
    result.push(`<div class='col-md-6'>
                    <button class='secondary-button d-flex float-right' aria-label='Add New Item' onclick='add_new_item_click()'>
                        <span class='x16 cdc-icon-plus pl-2'>
                        <span style='padding-left: 4px;'>Add New Field</span>
                        </span>
                    </button>
                </div></div>`);
	
	return result;

}

function update_item(p_index, p_value)
{
	g_de_identified_list.name_path_list[g_selected_list][p_index] = p_value;
}

function update_list_name(p_old_name, p_new_name)
{
	if (p_old_name === p_new_name || p_new_name.trim() === '') {
		return;
	}
	
	// Update the name_path_list with new key
	g_de_identified_list.name_path_list[p_new_name] = g_de_identified_list.name_path_list[p_old_name];
	delete g_de_identified_list.name_path_list[p_old_name];
	
	// Update sort_order array
	const index = g_de_identified_list.sort_order.indexOf(p_old_name);
	if (index > -1) {
		g_de_identified_list.sort_order[index] = p_new_name;
	}
	
	// Update selected list if it was the renamed one
	if (g_selected_list === p_old_name) {
		g_selected_list = p_new_name;
	}
	
	document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

function delete_item(p_index)
{
	g_de_identified_list.name_path_list[g_selected_list].splice(p_index,1);
	document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

function add_new_item_click()
{
	
	g_de_identified_list.name_path_list[g_selected_list].splice(0,0,"");

	document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

function server_save()
{

	$.ajax({
				url: location.protocol + '//' + location.host + '/api/export_list_manager',
				contentType: 'application/json; charset=utf-8',
				dataType: 'json',
				data: JSON.stringify(g_de_identified_list),
				type: "POST"
		}).done(function(response) 
		{

			var response_obj = eval(response);
			if(response_obj.ok)
			{
				g_de_identified_list._rev = response_obj.rev; 

				document.getElementById('output').innerHTML = render_de_identified_list().join("");
			}
		});

}





function remove_name_path_list_click(list_name)
{
    var answer = prompt ("Are you sure you want to remove the " + list_name + " list?", "Enter yes to confirm");
    if(answer == "yes")
    {
        g_de_identified_list.name_path_list[list_name] = [];
        delete g_de_identified_list.name_path_list[list_name];

        const current_index = g_de_identified_list.sort_order.indexOf(list_name);
        g_de_identified_list.sort_order.splice(current_index, 1);

        if( Object.keys( g_de_identified_list.name_path_list).length > 0)
        {
            g_selected_list =  Object.keys( g_de_identified_list.name_path_list)[0];
        }

        document.getElementById('output').innerHTML = render_de_identified_list().join("");
    }
}

function clone_list_click()
{
    const clone_target = document.getElementById("clone-source").value;
    var answer = prompt ("Are you sure you want to clone [" + clone_target + "] ?", "Enter yes to confirm");
    if(answer == "yes")
    {

        let list = g_de_identified_list.name_path_list[clone_target]; 
        if(list == null)
        {
            list = g_form_map.get(clone_target);
        }

        if
        (
            list != null &&
            g_de_identified_list.name_path_list[g_selected_list] != null

        )
        {
            const target_list = g_de_identified_list.name_path_list[g_selected_list];

            for (let i = 0; i < list.length; i++) 
            {
                const new_path = list[i];
                if(target_list.indexOf(new_path) < 0)
                {
                    target_list.push(new_path);
                }
            }
        }

        document.getElementById('output').innerHTML = render_de_identified_list().join("");

    }
}

function add_name_path_list_click(p_id)
{
    let new_name = document.getElementById("new_list_name").value.trim();

	g_de_identified_list.name_path_list[new_name] = [];

            g_selected_list = new_name;

            g_de_identified_list.sort_order.push(new_name);

            document.getElementById('output').innerHTML = render_de_identified_list().join("");

}

function edit_plan_click(p_id)
{
	var selected_plan = null;

	for(var i = 0; i < g_migration_plan_list.length; i++)
	{
		if(g_migration_plan_list[i]._id == p_id)
		{
			selected_plan = g_migration_plan_list[i]; 
			break;
		}
	}	

	if(selected_plan)
	{
		document.getElementById('output').innerHTML = render_edit_migration_plan(selected_plan).join("");

	}
	
}



function render_edit_migration_plan(p_migration_plan)
{

	var result = [];

	result.push("<a href=/migrationplan>back to migration plan list</a><br/>");
	result.push("<table>");
	result.push("<tr bgcolor='#DDDD88'><th colspan='2' scope='colgroup'>selected migration plan</th></tr>");
	result.push("<tr><td><b>name:</b></td>");
	result.push("<td><span title='" + p_migration_plan.name + "'><input type='text' value='");
	result.push(p_migration_plan.name);
	result.push("' onblur='update_plan_name_click(\"" + p_migration_plan._id + "\", this.value)'/></span> <input type=button value='== run migration plan ==' onclick='run_migration_plan_item_click(\"" + p_migration_plan._id + "\")' /></td>");
	result.push("</tr>");
	result.push("<tr><td valign=top><b>description:</b></td>");
	result.push("<td><textarea cols=35 rows=7 onblur='update_plan_description_click(\"" + p_migration_plan._id + "\", this.value)'>");
	result.push(p_migration_plan.description);
	result.push("</textarea></td>");
	result.push("</tr>");
	result.push("<tr><td><b>created by:</b></td><td>");
	result.push(p_migration_plan.created_by);
	result.push("</td>");
	result.push("<tr><td><b>date created:</b></td><td>");
	result.push(p_migration_plan.date_created);
	result.push("</td>");
	result.push("<tr><td><b>last updated by:</b></td><td>");
	result.push(p_migration_plan.date_last_updated);
	result.push("</td>");
	result.push("<tr><td><b>created by:</b></td><td>");
	result.push(p_migration_plan.last_updated_by);
	result.push("</td>");
	result.push("</tr>");		
	
	result.push("</table>");


	Array.prototype.push.apply(result, render_migration_plan_item_list(p_migration_plan))

	result.push("<br/><input type=button value='== save migration plan ==' onclick='save_migration_plan_item_click(\"" + p_migration_plan._id + "\")' /><br/>");

	result.push("<br/><a href=/migrationplan>back to migration plan list</a><br/>");
	return result;

}


function render_migration_plan_item_list(p_migration_plan)
{

	var result = [];

	result.push("<table>");
	result.push("<tr><th colspan='6' bgcolor='#DDDD88' scope='colgroup'>migration plan item list</th></tr>");
	result.push("<tr>");
	result.push("<th scope='col'>old_mmria_path</th>");
	result.push("<th scope='col'>new_mmria_path</th>");
	result.push("<th scope='col'>old_value</th>");
	result.push("<th scope='col'>new_value</th>");
	result.push("<th scope='col'>comment</th>");
	result.push("<th scope='col'>&nbsp;</th>");
	result.push("</tr>");
	for(var i in p_migration_plan.plan_items)
	{
		var item = p_migration_plan.plan_items[i];

		if(i % 2)
		{
			result.push("<tr valign=top bgcolor='#CCCCCC'>");
		}
		else
		{
			result.push("<tr valign=top>");
		}

		create_input_box_td(result, item.old_mmria_path, "oldmmriapath_" + i, "update_plan_item_old_mmria_path_onblur", p_migration_plan._id, i);
		create_input_box_td(result, item.new_mmria_path, "newmmriapath_" + i, "update_plan_item_new_mmria_path_onblur", p_migration_plan._id, i);
		create_input_box_td(result, item.old_value, "oldvalue_" + i, "update_plan_item_old_value_onblur", p_migration_plan._id, i);
		create_input_box_td(result, item.new_value, "newvalue_" + i, "update_plan_item_new_value_onblur", p_migration_plan._id, i);
		create_textarea_td(result, item.comment, "comment_" + i, "update_plan_item_comment_onblur", p_migration_plan._id, i);
		
		result.push("<td><input type=button value=delete onclick='delete_plan_item_click(\"" + p_migration_plan._id + "\"," + i + ")' /></td>");
		result.push("</tr>");		
		
	}


	result.push("<tr>");
	result.push("<td colspan=6 align=right><input type=button value='add new item' onclick='add_new_plan_item_click(\"" + p_migration_plan._id + "\")' /></td>");
	result.push("</tr>");

	result.push("</table>");

	return result;

}

function create_input_box_td(p_result, p_item_text, p_id, p_onblur, p_plan_id,  p_index)
{
	p_result.push("<td><span title='");
	p_result.push(p_item_text);
	p_result.push("'><input type='text' value='");
	p_result.push(p_item_text);

	if(p_id)
	{
		p_result.push("' id='");
		p_result.push(p_id);
	}
	p_result.push("'");

	if(p_onblur)
	{
		p_result.push(" onblur='" + p_onblur + "(\"" + p_plan_id + "\",\"" + p_index + "\", this.value)'");
	}

	p_result.push("/><span></td>");
		
}


function create_textarea_td(p_result, p_item_text, p_id, p_onblur, p_plan_id, p_index)
{
	p_result.push("<td><span title='");
	p_result.push(p_item_text);
	if(p_id)
	{
		p_result.push("' id='");
		p_result.push(p_id);
	}
	p_result.push("'><textarea cols=35 rows=3 ");
	
	if(p_onblur)
	{
		p_result.push(" onblur='" + p_onblur + "(\"" + p_plan_id + "\",\"" + p_index + "\", this.value)'");
	}

	p_result.push(">");
	p_result.push(p_item_text);
	p_result.push("</textarea></span></td>");
		
}


function delete_plan_item_click(p_id, p_item_index)
{
	var selected_plan = null;

	for(var i = 0; i < g_migration_plan_list.length; i++)
	{
		if(g_migration_plan_list[i]._id == p_id)
		{
			selected_plan = g_migration_plan_list[i]; 
			break;
		}
	}	

	if(selected_plan)
	{
		selected_plan.plan_items.splice(p_item_index, 1);

		document.getElementById('output').innerHTML = render_edit_migration_plan(selected_plan).join("");

	}
}


function add_new_plan_item_click(p_id)
{
	var selected_plan = null;

	for(var i = 0; i < g_migration_plan_list.length; i++)
	{
		if(g_migration_plan_list[i]._id == p_id)
		{
			selected_plan = g_migration_plan_list[i]; 
			break;
		}
	}	

	if(selected_plan)
	{
		var plan_item = get_migation_plan_item_default();
		selected_plan.plan_items.push(plan_item);

		document.getElementById('output').innerHTML = render_edit_migration_plan(selected_plan).join("");

	}
}


function update_plan_name_click(p_id, p_value)
{
	var selected_plan = null;

	for(var i = 0; i < g_migration_plan_list.length; i++)
	{
		if(g_migration_plan_list[i]._id == p_id)
		{
			selected_plan = g_migration_plan_list[i]; 
			break;
		}
	}	

	if(selected_plan)
	{
		selected_plan.name = p_value;
		//document.getElementById('output').innerHTML = render_edit_migration_plan(selected_plan).join("");
	}
}

function update_plan_description_click(p_id, p_value)
{
	var selected_plan = null;

	for(var i = 0; i < g_migration_plan_list.length; i++)
	{
		if(g_migration_plan_list[i]._id == p_id)
		{
			selected_plan = g_migration_plan_list[i]; 
			break;
		}
	}	

	if(selected_plan)
	{
		selected_plan.description = p_value;

		//document.getElementById('output').innerHTML = render_edit_migration_plan(selected_plan).join("");

	}
}


function save_migration_plan_item_click(p_id)
{
	var selected_plan = null;

	for(var i = 0; i < g_migration_plan_list.length; i++)
	{
		if(g_migration_plan_list[i]._id == p_id)
		{
			selected_plan = g_migration_plan_list[i]; 
			break;
		}
	}	

	if(selected_plan)
	{
		server_save(selected_plan)
	}
}

function run_migration_plan_item_click(p_id)
{
	var selected_plan = null;

	for(var i = 0; i < g_migration_plan_list.length; i++)
	{
		if(g_migration_plan_list[i]._id == p_id)
		{
			selected_plan = g_migration_plan_list[i]; 
			break;
		}
	}	

	if(selected_plan)
	{
		var answer = prompt ("Are you sure you want to run the [" + selected_plan.name + "] migration plan?", "Enter yes to confirm");
		if(answer == "yes")
		{
			
		}
	}
}


function update_plan_item_old_mmria_path_onblur(p_id, p_item_index, p_value)
{
	var selected_plan = null;

	for(var i = 0; i < g_migration_plan_list.length; i++)
	{
		if(g_migration_plan_list[i]._id == p_id)
		{
			selected_plan = g_migration_plan_list[i]; 
			break;
		}
	}	

	if(selected_plan)
	{
		var plan_item = selected_plan.plan_items[p_item_index];
		plan_item.old_mmria_path = p_value;
	}
}

function update_plan_item_new_mmria_path_onblur(p_id, p_item_index, p_value)
{
	var selected_plan = null;

	for(var i = 0; i < g_migration_plan_list.length; i++)
	{
		if(g_migration_plan_list[i]._id == p_id)
		{
			selected_plan = g_migration_plan_list[i]; 
			break;
		}
	}	

	if(selected_plan)
	{
		var plan_item = selected_plan.plan_items[p_item_index];
		plan_item.new_mmria_path = p_value;
	}
}

function update_plan_item_old_value_onblur(p_id, p_item_index, p_value)
{
	var selected_plan = null;

	for(var i = 0; i < g_migration_plan_list.length; i++)
	{
		if(g_migration_plan_list[i]._id == p_id)
		{
			selected_plan = g_migration_plan_list[i]; 
			break;
		}
	}	

	if(selected_plan)
	{
		var plan_item = selected_plan.plan_items[p_item_index];

		plan_item.old_value = p_value;

	}
}

function update_plan_item_new_value_onblur(p_id, p_item_index, p_value)
{
	var selected_plan = null;

	for(var i = 0; i < g_migration_plan_list.length; i++)
	{
		if(g_migration_plan_list[i]._id == p_id)
		{
			selected_plan = g_migration_plan_list[i]; 
			break;
		}
	}	

	if(selected_plan)
	{
		var plan_item = selected_plan.plan_items[p_item_index];

		plan_item.new_value = p_value;
	}
}


function cut_selected(p_value)
{
    g_selected_index = p_value;
    //console.log(`cut selected ${p_value}`);
}

function paste_selected(p_value)
{

    let x = p_value -1;
    let y = g_selected_index -1;
    const list  = g_de_identified_list.name_path_list[g_selected_list];

    if
    (
        g_de_identified_list != null &&
        g_selected_list != null &&
        list != null && 
        g_selected_index > -1 &&
        x < list.length &&
        y < list.length
    )
    {
        //console.log(`paste selected ${p_value}`);

        let y_value = list[y];
        list.splice(y, 1);
        
        list.splice(x, 0, y_value);

        document.getElementById('output').innerHTML = render_de_identified_list().join("");
    }
    
}


function create_metadata_map(p_result, p_metadata, p_path, p_current_key)
{	
    let next_path = p_path + "/" + p_metadata.name;
    if(p_metadata.type == "app")
    {
        next_path = "/";

    }
    else if(next_path.startsWith("//"))
    {
        next_path = next_path.substring(2);
    }

	if(p_metadata.children && p_metadata.children.length > 0)
	{	
        
        if(p_metadata.type == "form")
        {
            p_result.set(p_metadata.name, []);
            p_current_key = p_metadata.name;
        }

		var total_core_summary = 0;
		for(var i = 0; i < p_metadata.children.length; i++)
		{
			var child = p_metadata.children[i];
			if
            (
                child.type.toLowerCase() != "grid" &&
                child.type.toLowerCase() != "label" &&
                child.type.toLowerCase() != "button" &&
                child.type.toLowerCase() != "chart"


            )
			{
				create_metadata_map(p_result, child, next_path, p_current_key);
			}
            /*
			else
			{
				create_metadata_map(p_result, child, next_path, p_current_key);
			}*/
		}
	}
    else if
    (
        p_current_key!=null
    )
    {
        p_result.get(p_current_key).push(next_path);
    }
}

async function update_sort_order(p_list_name, p_desired_index)
{
    let sort_index = p_desired_index - 1;
    if(sort_index < 0)
    {
        sort_index = 0;
    }
    else if (sort_index > g_de_identified_list.sort_order.length - 1)
    {
        sort_index = g_de_identified_list.sort_order.length - 1;
    }

    const current_index = g_de_identified_list.sort_order.indexOf(p_list_name);

    g_de_identified_list.sort_order.splice(current_index, 1);
    g_de_identified_list.sort_order.splice(sort_index, 0, p_list_name);


    //console.log(`${p_list_name} ${p_desired_index} ${p_desired_index -1}`)
    

    document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

var g_drag_source_list_name = null;
var g_drag_source_field_index = null;

function handle_drag_start(event, list_name)
{
    g_drag_source_list_name = list_name;
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/html', list_name);
    event.currentTarget.style.opacity = '0.4';
}

function handle_drag_over(event)
{
    if (event.preventDefault) {
        event.preventDefault();
    }
    event.dataTransfer.dropEffect = 'move';
    return false;
}

function handle_drop(event, target_list_name)
{
    if (event.stopPropagation) {
        event.stopPropagation();
    }
    
    if (g_drag_source_list_name !== target_list_name)
    {
        const source_index = g_de_identified_list.sort_order.indexOf(g_drag_source_list_name);
        const target_index = g_de_identified_list.sort_order.indexOf(target_list_name);
        
        // Remove from old position
        g_de_identified_list.sort_order.splice(source_index, 1);
        // Insert at new position
        g_de_identified_list.sort_order.splice(target_index, 0, g_drag_source_list_name);
        
        document.getElementById('output').innerHTML = render_de_identified_list().join("");
    }
    
    return false;
}

function handle_drag_end(event)
{
    event.currentTarget.style.opacity = '1';
    g_drag_source_list_name = null;
}

function handle_field_drag_start(event, field_index)
{
    g_drag_source_field_index = field_index;
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/html', field_index);
    event.currentTarget.style.opacity = '0.4';
}

function handle_field_drop(event, target_field_index)
{
    if (event.stopPropagation) {
        event.stopPropagation();
    }
    
    if (g_drag_source_field_index !== null && g_drag_source_field_index !== target_field_index)
    {
        const list = g_de_identified_list.name_path_list[g_selected_list];
        const source_index = parseInt(g_drag_source_field_index);
        const target_index = parseInt(target_field_index);
        
        // Get the item being moved
        const item = list[source_index];
        
        // Remove from old position
        list.splice(source_index, 1);
        // Insert at new position
        list.splice(target_index, 0, item);
        
        document.getElementById('output').innerHTML = render_de_identified_list().join("");
    }
    
    g_drag_source_field_index = null;
    return false;
}