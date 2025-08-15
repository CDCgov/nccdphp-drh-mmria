
var g_couchdb_url = null;
var g_data = null;

var g_ui = { 
	jurisdiction_tree: null,
	data:null,
	url_state: {
    selected_form_name: null,
    selected_id: null,
    selected_child_id: null,
    path_array : []

  }
};

var $$ = {
 is_id: function(value){
   // 2016-06-12T13:49:24.759Z
    if(value)
    {
      var test = value.match(/^\d+-\d+-\d+T\d+:\d+:\d+.\d+Z$/);
      return (test)? true : false;
    }
    else
    {
        return false;
    }
  }
};



$(function ()
{//http://www.w3schools.com/html/html_layout.asp
  'use strict';
	/*profile.on_login_call_back = function (){
				load_jurisdictions();
    };*/
	//profile.initialize_profile();

	load_jurisdictions();

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




function load_jurisdictions()
{
	var metadata_url = location.protocol + '//' + location.host + '/api/jurisdiction_tree';

	$.ajax
	({
			url: metadata_url,
			beforeSend: function (request)
			{
				request.setRequestHeader("AuthSession", $mmria.getCookie("AuthSession"));
			}
	}).done(function(response) 
	{

			g_jurisdiction_tree = response;

			//document.getElementById('navigation_id').innerHTML = navigation_render(g_jurisdiction_list, 0, g_ui).join("");

			document.getElementById('form_content_id').innerHTML = "<p>tree</p><ul>" + jurisdiction_render(g_ui, g_jurisdiction_tree, g_ui).join("") + "</ul>";

	});
}








function server_save(p_jurisdiction)
{
	console.log("server save");
	var current_auth_session = profile.get_auth_session_cookie();

	if(current_auth_session)
	{ 
		$.ajax({
					url: location.protocol + '//' + location.host + '/api/jurisdiction',
					contentType: 'application/json; charset=utf-8',
					dataType: 'json',
					data: JSON.stringify(p_jurisdiction),
					type: "POST",
					beforeSend: function (request)
					{
						request.setRequestHeader ("Authorization", "Basic " + btoa(g_uid  + ":" + $mmria.getCookie("pwd")));
						request.setRequestHeader("AuthSession", $mmria.getCookie("AuthSession"));
					}//,
			}).done(function(response) 
			{


						var response_obj = eval(response);
						if(response_obj.ok)
						{
							g_jurisdiction_list._rev = response_obj.rev; 
							document.getElementById('form_content_id').innerHTML = editor_render(g_jurisdiction_list, "", g_ui).join("");
						}
						//{ok: true, id: "2016-06-12T13:49:24.759Z", rev: "3-c0a15d6da8afa0f82f5ff8c53e0cc998"}
					console.log("metadata sent", response);
			});
	}

}

function add_new_jurisdiction_click()
{
	var new_jurisdiction_name = document.getElementById('new_jurisdiction_name').value;
	//var new_jurisdiction_password = document.getElementById('new_jurisdiction_password').value;
	var jurisdiction_id = null;
	if(
		is_valid_jurisdiction_name(new_jurisdiction_name) //&& 
		//is_valid_password(new_jurisdiction_password)
	)
	{

		var new_jurisdiction = $$.add_new_jurisdiction(new_jurisdiction_name, "password");
		jurisdiction_id = new_jurisdiction._id;
		g_ui.jurisdiction_summary_list.push(new_jurisdiction);
		document.getElementById('form_content_id').innerHTML = jurisdiction_render(g_ui, "", g_ui).join("");
		create_status_message("new jurisdiction has been added.", "new_jurisdiction");
		//console.log("greatness awaits.");
	}
	else
	{
		create_status_warning("invalid jurisdiction name.", "new_jurisdiction");
		console.log("got nothing.");
	}
}


function change_password_jurisdiction_click(p_jurisdiction_id)
{
	
	var new_jurisdiction_password = document.querySelector('[role="confirm_1"][path="' + p_jurisdiction_id + '"]').value;
	var new_confirm_password = document.querySelector('[role="confirm_2"][path="' + p_jurisdiction_id + '"]').value;

	var jurisdiction_index = -1;
	var jurisdiction_list = g_ui.jurisdiction_summary_list;
	var jurisdiction = null;
	for(var i = 0; i < jurisdiction_list.length; i++)
	{
		if(jurisdiction_list[i]._id == p_jurisdiction_id)
		{
			jurisdiction = jurisdiction_list[i];
			break;
		}
	}


	if(
		is_valid_password(new_jurisdiction_password) && 
		is_valid_password(new_confirm_password) &&
		new_jurisdiction_password == new_confirm_password
	)
	{



		if(jurisdiction)
		{
			jurisdiction.password = new_jurisdiction_password;

			//var current_auth_session = profile.get_auth_session_cookie();

			//if(current_auth_session)
			//{ 
				$.ajax({
					url: location.protocol + '//' + location.host + '/api/jurisdiction',
					contentType: 'application/json; charset=utf-8',
					dataType: 'json',
					data: JSON.stringify(jurisdiction),
					type: "POST"/*,
					beforeSend: function (request)
					{
						request.setRequestHeader("AuthSession", current_auth_session);
					}*/
				}).done(function(response) 
				{
					var response_obj = eval(response);
					if(response_obj.ok)
					{
						for(var i = 0; i < g_ui.jurisdiction_summary_list.length; i++)
						{
							if(g_ui.jurisdiction_summary_list[i]._id == response_obj.id)
							{
								g_ui.jurisdiction_summary_list[i]._rev = response_obj.rev; 
								break;
							}
						}

						if(response_obj.auth_session)
						{
							//profile.auth_session = response_obj.auth_session;
							$mmria.addCookie("AuthSession", response_obj.auth_session);
						}

						document.getElementById('form_content_id').innerHTML = jurisdiction_render(g_ui, "", g_ui).join("");
						create_status_message("jurisdiction information saved", convert_to_jquery_id(jurisdiction._id));
						console.log("password saved sent", response);


					}
					//{ok: true, id: "2016-06-12T13:49:24.759Z", rev: "3-c0a15d6da8afa0f82f5ff8c53e0cc998"}
					
				});
			//}
		}
		else
		{
			document.getElementById('form_content_id').innerHTML = jurisdiction_render(g_ui, "", g_ui).join("");
			//console.log("greatness awaits.");
		}
	}
	else
	{
		create_status_warning("invalid password and confirm", convert_to_jquery_id(jurisdiction._id));
		console.log("got nothing.");
	}
}


function is_valid_jurisdiction_name(p_value)
{
	var result = true;

	if(
		p_value && 
		p_value.length >4
	)
	{
		//console.log("greatness awaits.");
	}
	else
	{
		result = false;
	}

	return result;
}

function is_valid_password(p_value)
{
	var result = true;

	if(
		p_value &&
		p_value.length >= 8
	)
	{
		//console.log("greatness awaits.");
	}
	else
	{
		result = false;
	}

	return result;
	
}

function save_jurisdiction(p_jurisdiction_id)
{
	var jurisdiction_index = -1;
	var jurisdiction_list = g_ui.jurisdiction_summary_list;

	for(var i = 0; i < jurisdiction_list.length; i++)
	{
		if(jurisdiction_list[i]._id == p_jurisdiction_id)
		{
			jurisdiction_index = i;
			break;
		}
	}

	if(jurisdiction_index > -1)
	{
		var jurisdiction = jurisdiction_list[jurisdiction_index];
		var current_auth_session = profile.get_auth_session_cookie();

			if(current_auth_session)
			{ 
				$.ajax({
					url: location.protocol + '//' + location.host + '/api/jurisdiction',
					contentType: 'application/json; charset=utf-8',
					dataType: 'json',
					data: JSON.stringify(jurisdiction),
					type: "POST",
					beforeSend: function (request)
					{
						request.setRequestHeader("AuthSession", current_auth_session);
					}
				}).done(function(response) 
				{
					var response_obj = eval(response);
					if(response_obj.ok)
					{
						for(var i = 0; i < g_ui.jurisdiction_summary_list.length; i++)
						{
							if(g_ui.jurisdiction_summary_list[i]._id == response_obj.id)
							{
								g_ui.jurisdiction_summary_list[i]._rev = response_obj.rev; 
								break;
							}
						}

						if(response_obj.auth_session)
						{
							//profile.auth_session = response_obj.auth_session;
							$mmria.addCookie("AuthSession", response_obj.auth_session);
						}

						document.getElementById('form_content_id').innerHTML = jurisdiction_render(g_ui, "", g_ui).join("");
						create_status_message("jurisdiction information saved", convert_to_jquery_id(jurisdiction._id));
						console.log("password saved sent", response);


					}
					//{ok: true, id: "2016-06-12T13:49:24.759Z", rev: "3-c0a15d6da8afa0f82f5ff8c53e0cc998"}
					
				});
			}
	}
}

function add_role(p_jurisdiction_id)
{
	var selected_role = document.querySelector('select[path="' + p_jurisdiction_id + '"]');
	if(selected_role && selected_role.value != '')
	{
		var jurisdiction_index = -1;
		var jurisdiction_list = g_ui.jurisdiction_summary_list;
		var escaped_id =  convert_to_jquery_id(p_jurisdiction_id);
		for(var i = 0; i < jurisdiction_list.length; i++)
		{
			if(jurisdiction_list[i]._id == p_jurisdiction_id)
			{
				jurisdiction_index = i;
				break;
			}
		}

		if(jurisdiction_index > -1)
		{
			var jurisdiction = jurisdiction_list[jurisdiction_index];
			jurisdiction.roles.push(selected_role.value);
			g_ui.jurisdiction_summary_list[jurisdiction_index] = jurisdiction;
			$( "#" + escaped_id).replaceWith( jurisdiction_entry_render(jurisdiction, "", g_ui).join("") );
			
		}
	}
}


function remove_role(p_jurisdiction_id, p_role)
{
	var jurisdiction_index = -1;
	var jurisdiction_list = g_ui.jurisdiction_summary_list;
	var escaped_id =  convert_to_jquery_id(p_jurisdiction_id);
	for(var i = 0; i < jurisdiction_list.length; i++)
	{
		if(jurisdiction_list[i]._id == p_jurisdiction_id)
		{
			jurisdiction_index = i;
			break;
		}
	}

	if(jurisdiction_index > -1)
	{
		var jurisdiction = jurisdiction_list[jurisdiction_index];
		var role_index = jurisdiction.roles.indexOf(p_role);
		if(role_index > -1)
		{
			jurisdiction.roles.splice(role_index, 1);
			g_ui.jurisdiction_summary_list[jurisdiction_index] = jurisdiction;
			$( "#" + escaped_id).replaceWith( jurisdiction_entry_render(jurisdiction, "", g_ui).join("") );
		}
	}
}

function change_password(p_jurisdiction_id, p_role)
{
	var jurisdiction_index = -1;
	var jurisdiction_list = g_ui.jurisdiction_summary_list;
	var escaped_id =  convert_to_jquery_id(p_jurisdiction_id);
	for(var i = 0; i < jurisdiction_list.length; i++)
	{
		if(jurisdiction_list[i]._id == p_jurisdiction_id)
		{
			jurisdiction_index = i;
			break;
		}
	}

	if(jurisdiction_index > -1)
	{
		var jurisdiction = jurisdiction_list[jurisdiction_index];
		var role_index = jurisdiction.roles.indexOf(p_role);
		if(role_index > -1)
		{
			jurisdiction.roles.splice(role_index, 1);
			g_ui.jurisdiction_summary_list[jurisdiction_index] = jurisdiction;
			$( "#" + escaped_id).replaceWith( jurisdiction_entry_render(jurisdiction, "", g_ui).join("") );
			create_status_message("jurisdiction information saved", convert_to_jquery_id(jurisdiction._id));
		}
	}
}


function convert_to_jquery_id(p_value)
{
	return p_value.replace('@', 'ATT').replace(':','COL').replace(/\./g,'DOT');
}


function create_status_message(p_message, p_div_id)
{
	var result = [];

	result.push('<div class="alert alert-success alert-dismissible">');
	result.push('<a href="#" class="close" data-dismiss="alert" aria-label="close">&times;</a>');
	// result.push('<strong>Info!</strong> ');
	result.push(p_message);
	result.push('</div>');

	document.getElementById(p_div_id + "_status_area").innerHTML = result.join("");

	window.setTimeout(clear_status, 30000);
}

function create_status_warning(p_message, p_div_id)
{
	var result = [];

	result.push('<div class="alert alert-danger">');
	result.push('<a href="#" class="close" data-dismiss="alert" aria-label="close">&times;</a>');
	result.push('<strong>Warning!</strong> ');
	result.push(p_message);
	result.push('</div>');

	document.getElementById(p_div_id + "_status_area").innerHTML = result.join("");

	window.setTimeout(clear_status, 30000);
}

function clear_status()
{
	document.getElementById("status_area").innerHTML = "<div>&nbsp;</div>";
}
