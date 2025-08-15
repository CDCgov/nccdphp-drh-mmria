
var g_de_identified_list = null;

$(function ()
{//http://www.w3schools.com/html/html_layout.asp
  'use strict';
	/*profile.on_login_call_back = function (){
				load_users();
    };*/
	//profile.initialize_profile();

	load_de_identification_list();

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



function load_de_identification_list()
{

	$.ajax({
		url: location.protocol + '//' + location.host + '/api/de_identified_list',
	}).done(function(response) 
	{
		g_de_identified_list = response;
		
		document.getElementById('output').innerHTML = render_de_identified_list().join("");
	});

}

function render_de_identified_list()
{

	var result = [];
	result.push("<br/>");
	result.push("<table>");
	result.push("<tr><th colspan='3' bgcolor='silver' scope='colgroup'>de identified list (" + g_de_identified_list.paths.length + ")</th></tr>");
	result.push("<tr>");
	result.push("<th scope='col'>path</th>");
	result.push("<th scope='col'>&nbsp;</th>");
	result.push("</tr>");

	//result.push("<tr><td colspan=3 align=center><input type='button' value='save list' onclick='server_save()' /></td></tr>")
	result.push("<tr><td colspan=3 align=right><input type='button' value='add item' onclick='add_new_item_click()' /></td></tr>")

    g_de_identified_list.paths.sort();
	for(var i in g_de_identified_list.paths)
	{
		var item = g_de_identified_list.paths[i];

		if(i % 2)
		{
			result.push("<tr bgcolor='#CCCCCC'>");
		}
		else
		{
			result.push("<tr>");
		}

        let row_number = new Number(i);
        row_number++;
        result.push(`<td>${row_number}</td>`)
		result.push("<td><label title='");
		result.push(item);
		result.push("'><input size='120' type='text' value='");
		result.push(item);
		result.push("' onblur='update_item("+ i+", this.value)'/></label></td>");
		result.push("<td><input type=button value=delete onclick='delete_item(" + i + ")' /></td>");
		result.push("</tr>");		
		
	}


	result.push("<tr><td colspan=3 align=center><input type='button' value='save list' onclick='server_save()' /></td></tr>")

	
	result.push("</table>");
	result.push("<br/>");
	
	return result;

}

function update_item(p_index, p_value)
{
	g_de_identified_list.paths[p_index] = p_value;


}

function delete_item(p_index)
{
	g_de_identified_list.paths.splice(p_index,1);
	document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

function add_new_item_click()
{
	
	g_de_identified_list.paths.push("");

	document.getElementById('output').innerHTML = render_de_identified_list().join("");
}

function server_save()
{

	$.ajax({
				url: location.protocol + '//' + location.host + '/api/de_identified_list',
				contentType: 'application/json; charset=utf-8',
				dataType: 'json',
				data: JSON.stringify(g_de_identified_list),
				type: "POST"/*,
				beforeSend: function (request)
				{
					request.setRequestHeader ("Authorization", "Basic " + btoa(g_uid  + ":" + $mmria.getCookie("pwd")));
					request.setRequestHeader("AuthSession", $mmria.getCookie("AuthSession"));
				},*/
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



function server_delete(p_migration_plan)
{
	$.ajax({
				url: location.protocol + '//' + location.host + '/api/migration_plan?migration_plan_id=' + p_migration_plan._id + '&rev=' + p_migration_plan._rev,
				contentType: 'application/json; charset=utf-8',
				dataType: 'json',
				//data: JSON.stringify(p_migration_plan),
				type: "DELETE"/*,
				beforeSend: function (request)
				{
					request.setRequestHeader ("Authorization", "Basic " + btoa(g_uid  + ":" + $mmria.getCookie("pwd")));
					request.setRequestHeader("AuthSession", $mmria.getCookie("AuthSession"));
				},*/
		}).done(function(response) 
		{

			var response_obj = eval(response);
			if(response_obj.ok)
			{
				for(var i = 0; i < g_migration_plan_list.length; i++)
				{
					if(g_migration_plan_list[i]._id == response_obj.id)
					{
						g_migration_plan_list.splice(i, 1);; 
						break;
					}
				}			

				document.getElementById('output').innerHTML = render_migration_plan().join("");
			}
		});

}



function delete_plan_click(p_id)
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

		var answer = prompt ("Are you sure you want to delete plan: " + selected_plan.name, "Enter yes to confirm");
		if(answer == "yes")
		{
			server_delete(selected_plan);
		}
		

	}
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
