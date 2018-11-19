
var g_policy_values = null;
var g_jurisdiction_tree = null;
var g_user_role_jurisdiction = null;

var g_ui = { 
	user_summary_list:[],
	user_list:[],
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
  },
  add_new_user: function(p_name, p_password)
  {
	  return {
		"_id": "org.couchdb.user:" + p_name,
		"password": p_password,
		"password_scheme": "pbkdf2",
		"iterations": 10,
		"name": p_name,
		"roles": [  ],
		"type": "user",
		"derived_key": "a1bb5c132df5b7df7654bbfa0e93f9e304e40cfe",
		"salt": "510427706d0deb511649021277b2c05d"
		};
  }
};



$(function ()
{//http://www.w3schools.com/html/html_layout.asp
  'use strict';
	/*profile.on_login_call_back = function (){
				load_users();
    };*/
	//profile.initialize_profile();

	load_values();

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



function load_values()
{
	$.ajax({
			url: location.protocol + '//' + location.host + '/api/policyvalues',
	}).done(function(response) {
			g_policy_values = response;
			load_jurisdictions();
	});

}

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

			load_user_jurisdictions();
			//document.getElementById('navigation_id').innerHTML = navigation_render(g_jurisdiction_list, 0, g_ui).join("");

	});
}


function load_user_jurisdictions()
{
	var metadata_url = location.protocol + '//' + location.host + '/api/user_role_jurisdiction';

	$.ajax
	({
			url: metadata_url,
			beforeSend: function (request)
			{
				request.setRequestHeader("AuthSession", $mmria.getCookie("AuthSession"));
			}
	}).done(function(response) 
	{
	
		g_user_role_jurisdiction = [];
		if(response)
		{
			g_user_role_jurisdiction = response;

			load_users();
			//document.getElementById('navigation_id').innerHTML = navigation_render(g_jurisdiction_list, 0, g_ui).join("");
		}

	});
}





function load_users()
{
	var metadata_url = location.protocol + '//' + location.host + '/api/user';

	$.ajax({
			url: metadata_url,
			beforeSend: function (request)
			{
				request.setRequestHeader("AuthSession", $mmria.getCookie("AuthSession"));
			}
	}).done(function(response) {
			
			var temp = [];
			for(var i = 0; i < response.rows.length; i++)
			{
				temp.push(response.rows[i].doc);
			}
			//console.log(temp);
			g_ui.user_summary_list = temp;
			console.log(g_ui.user_summary_list);
			g_ui.url_state = url_monitor.get_url_state(window.location.href);

			//document.getElementById('navigation_id').innerHTML = navigation_render(g_user_list, 0, g_ui).join("");

			document.getElementById('form_content_id').innerHTML = user_render(g_ui, $mmria.getCookie("uid")).join("")
			+ "" + jurisdiction_render(g_jurisdiction_tree).join("");
			;

	});
}








function server_save(p_user)
{
	console.log("server save");
	var current_auth_session = profile.get_auth_session_cookie();

	if(current_auth_session)
	{ 
		$.ajax({
					url: location.protocol + '//' + location.host + '/api/user',
					contentType: 'application/json; charset=utf-8',
					dataType: 'json',
					data: JSON.stringify(p_user),
					type: "POST",
					beforeSend: function (request)
					{
						request.setRequestHeader ("Authorization", "Basic " + btoa($mmria.getCookie("uid")  + ":" + $mmria.getCookie("pwd")));
						request.setRequestHeader("AuthSession", $mmria.getCookie("AuthSession"));
					}//,
			}).done(function(response) 
			{


						var response_obj = eval(response);
						if(response_obj.ok)
						{
							g_user_list._rev = response_obj.rev; 
							document.getElementById('form_content_id').innerHTML = editor_render(g_user_list, "", g_ui).join("");
						}
						//{ok: true, id: "2016-06-12T13:49:24.759Z", rev: "3-c0a15d6da8afa0f82f5ff8c53e0cc998"}
					console.log("metadata sent", response);
			});
	}

}

function add_new_user_click()
{
	var new_user_name = document.getElementById('new_user_name').value;
	var new_user_password = document.getElementById('new_user_password').value;
	var new_user_verify= document.getElementById('new_user_verify').value;
	var user_id = null;
	if(is_valid_user_name(new_user_name))
	{

		if
		(
			new_user_password == new_user_verify &&
			is_valid_password(new_user_password) && 
			is_valid_password(new_user_verify)
			
		)
		{

			var new_user = $$.add_new_user(new_user_name, new_user_password);
			user_id = new_user._id;
			g_ui.user_summary_list.push(new_user);
	
			$.ajax({
				url: location.protocol + '//' + location.host + '/api/user',
				contentType: 'application/json; charset=utf-8',
				dataType: 'json',
				data: JSON.stringify(new_user),
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
	
					if(response_obj.auth_session)
					{
						//profile.auth_session = response_obj.auth_session;
						$mmria.addCookie("AuthSession", response_obj.auth_session);
					}
	
					document.getElementById('form_content_id').innerHTML = user_render(g_ui, "", g_ui).join("");
					create_status_message("new user has been added.", "new_user");
	
				}
				//{ok: true, id: "2016-06-12T13:49:24.759Z", rev: "3-c0a15d6da8afa0f82f5ff8c53e0cc998"}
				
			});
		}
		else
		{
			create_status_warning("invalid password.<br/>be sure that verify and password match,<br/> minimum length is: " + g_policy_values.password_minimum_length + " and should only include characters [a-zA-Z0-9!@#$%?* ]", "new_user");
			
		}

	}
	else
	{
		create_status_warning("invalid user name. user name should be unique and at least 5 characters long. ", "new_user");
		console.log("got nothing.");
	}
}


function change_password_user_click(p_user_id)
{
	
	var new_user_password = document.querySelector('[role="confirm_1"][path="' + p_user_id + '"]').value;
	var new_confirm_password = document.querySelector('[role="confirm_2"][path="' + p_user_id + '"]').value;

	var user_index = -1;
	var user_list = g_ui.user_summary_list;
	var user = null;
	for(var i = 0; i < user_list.length; i++)
	{
		if(user_list[i]._id == p_user_id)
		{
			user = user_list[i];
			break;
		}
	}


	if(
		new_user_password == new_confirm_password &&
		is_valid_password(new_user_password) && 
		is_valid_password(new_confirm_password)
		
	)
	{



		if(user)
		{
			user.password = new_user_password;

			//var current_auth_session = profile.get_auth_session_cookie();

			//if(current_auth_session)
			//{ 
				$.ajax({
					url: location.protocol + '//' + location.host + '/api/user',
					contentType: 'application/json; charset=utf-8',
					dataType: 'json',
					data: JSON.stringify(user),
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
						for(var i = 0; i < g_ui.user_summary_list.length; i++)
						{
							if(g_ui.user_summary_list[i]._id == response_obj.id)
							{
								g_ui.user_summary_list[i]._rev = response_obj.rev; 
								break;
							}
						}

						if(response_obj.auth_session)
						{
							//profile.auth_session = response_obj.auth_session;
							$mmria.addCookie("AuthSession", response_obj.auth_session);
						}

						document.getElementById('form_content_id').innerHTML = user_render(g_ui, "", g_ui).join("");
						create_status_message("user information saved", convert_to_jquery_id(user._id));
						console.log("password saved sent", response);


					}
					//{ok: true, id: "2016-06-12T13:49:24.759Z", rev: "3-c0a15d6da8afa0f82f5ff8c53e0cc998"}
					
				});
			//}
		}
		else
		{
			document.getElementById('form_content_id').innerHTML = user_render(g_ui, "", g_ui).join("");
			//console.log("greatness awaits.");
		}
	}
	else
	{

		create_status_warning("invalid password.<br/>be sure that verify and password match,<br/>  minimum length is: " + g_policy_values.password_minimum_length + " and should only include characters [a-zA-Z0-9!@#$%?* ]", convert_to_jquery_id(user._id));
		//create_status_warning("invalid password and confirm", convert_to_jquery_id(user._id));
		console.log("got nothing.");
	}
}


function is_valid_user_name(p_value)
{
	var result = true;

	if(
		p_value && 
		p_value.length > 4
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

    var valid_character_re = /^[a-zA-Z0-9!@#$\%\?\* ]+$/g;


	if(
		p_value &&
		p_value.length >= g_policy_values.password_minimum_length &&
		p_value.match(valid_character_re)
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
		var current_auth_session = profile.get_auth_session_cookie();

			if(current_auth_session)
			{ 
				$.ajax({
					url: location.protocol + '//' + location.host + '/api/user',
					contentType: 'application/json; charset=utf-8',
					dataType: 'json',
					data: JSON.stringify(user),
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
						for(var i = 0; i < g_ui.user_summary_list.length; i++)
						{
							if(g_ui.user_summary_list[i]._id == response_obj.id)
							{
								g_ui.user_summary_list[i]._rev = response_obj.rev; 
								break;
							}
						}

						if(response_obj.auth_session)
						{
							//profile.auth_session = response_obj.auth_session;
							$mmria.addCookie("AuthSession", response_obj.auth_session);
						}

						document.getElementById('form_content_id').innerHTML = user_render(g_ui, "", g_ui).join("");
						create_status_message("user information saved", convert_to_jquery_id(user._id));
						console.log("password saved sent", response);


					}
					//{ok: true, id: "2016-06-12T13:49:24.759Z", rev: "3-c0a15d6da8afa0f82f5ff8c53e0cc998"}
					
				});
			}
	}
}

function add_role(p_user_id, p_created_by)
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

		var temp_user_role = user_role_jurisdiction_add
		(
			"",
			user.name,
			"",
			new Date(),
			g_policy_values.default_days_in_effective_date_interval != null && parseInt(g_policy_values.default_days_in_effective_date_interval) >0? new Date(new Date().getTime() + parseInt(g_policy_values.default_days_in_effective_date_interval)*24*60*60*1000) : "",
			true,
			p_created_by
		);

		g_user_role_jurisdiction.push(temp_user_role);

		var role_list_for_ = document.getElementById("role_list_for_" + user.name);


		var opt = document.createElement('option');
		var option_text = [];
		option_text.push(temp_user_role.user_id);
		option_text.push(" ");
		option_text.push(temp_user_role.role_name);
		option_text.push(" ");
		option_text.push(temp_user_role.jurisdiction_id);
		option_text.push(" ");

		if(temp_user_role.effective_start_date instanceof Date)
		{
			option_text.push(temp_user_role.effective_start_date.toISOString());
		}
		else
		{
			option_text.push(temp_user_role.effective_start_date);
		}
		
		option_text.push(" ");
		if(temp_user_role.effective_end_date instanceof Date)
		{
			if(user_role.effective_end_date != "Invalid Date")
			{
				option_text.push(temp_user_role.effective_end_date.toISOString());
			}
			else
			{
				option_text.push(temp_user_role.effective_end_date);
			}
			
		}
		else
		{
			option_text.push(temp_user_role.effective_end_date);
		}		
		option_text.push(" ");
		option_text.push(temp_user_role.is_active);

		opt.value = temp_user_role._id;
		opt.selected = true;
		opt.innerHTML = option_text.join("");

		role_list_for_.appendChild(opt);

	
		var render_result = user_role_edit_render(user, temp_user_role, p_created_by);
		var selected_user_role_for_ = document.getElementById("selected_user_role_for_" + user.name);
		selected_user_role_for_.outerHTML = render_result.join("");

	}

}


function update_role(p_user_role_jurisdiction_id, p_user_id)
{
	var user_role_index = -1;
	for(var i = 0; i < g_user_role_jurisdiction.length; i++)
	{
		if(g_user_role_jurisdiction[i]._id == p_user_role_jurisdiction_id)
		{
			user_role_index = i;
			break;
		}
	}



	if(user_role_index > -1)
	{
		var user_index = -1;
		var user_list = g_ui.user_summary_list;
		for(var i = 0; i < user_list.length; i++)
		{
			if(user_list[i].name ==  g_user_role_jurisdiction[user_role_index].user_id)
			{
				user_index = i;
				break;
			}
		}

		if(user_index > -1)
		{

			
		
			var user_role = g_user_role_jurisdiction[user_role_index];
			var user = user_list[user_index];


			

			var selected_user_role_for_ = null;
			var role = document.getElementById("selected_user_role_for_" + user_role.user_id + "_role");
			var jurisdiction = document.getElementById("selected_user_role_for_" + user_role.user_id+ "_jurisdiction");
			var effective_start_date = document.getElementById("selected_user_role_for_" + user_role.user_id + "_effective_start_date");
			var effective_end_date = document.getElementById("selected_user_role_for_" + user_role.user_id + "_effective_end_date");
			var is_active = document.getElementById("selected_user_role_for_" + user_role.user_id + "_is_active");

			user_role.role_name = role.value;
			user_role.jurisdiction_id = jurisdiction.value;
			user_role.effective_start_date = new Date(effective_start_date.value);
			user_role.effective_end_date = new Date(effective_end_date.value);
			user_role.is_active = (is_active.value == "true")? true: false;
			user_role.last_updated_by = p_user_id;

			if(user_role.jurisdiction_id && user_role.role_name)
			{
				document.getElementById(convert_to_jquery_id(user._id) + "_status_area").innerHTML = "";
				document.getElementById("selected_user_role_for_" + user_role.user_id).innerHTML = '';
				save_user_role_jurisdiction(user_role, user, p_user_id);
			}
			else
			{
				create_status_warning("invalid jusidiction or role name", convert_to_jquery_id(user._id));
			}

		}
	}
}




function remove_role(p_user_role_id)
{
	var user_role_index = -1;
	for(var i = 0; i < g_user_role_jurisdiction.length; i++)
	{
		if(g_user_role_jurisdiction[i]._id == p_user_role_id)
		{
			user_role_index = i;
			break;
		}
	}

	if(user_role_index > -1)
	{
		var user_role = g_user_role_jurisdiction[user_role_index];

		var retVal = null
		
		if(user_role._rev)
		{
			retVal = prompt("Confirm role [" + user_role.role_name + "] removal for user [" + user_role.user_id + "] by entering the role name: ", "role name to remove here");
		

			document.getElementById("selected_user_role_for_" + user_role.user_id).innerHTML = '';

			document.getElementById(convert_to_jquery_id("org.couchdb.user:" + user_role.user_id) + "_status_area").innerHTML = "";
		}

		if(retVal && retVal.toLocaleLowerCase() == user_role.role_name && user_role._rev)
		{ 
			$.ajax({
				url: location.protocol + '//' + location.host + '/api/user_role_jurisdiction?_id=' + user_role._id + '&rev=' + user_role._rev,
				contentType: 'application/json; charset=utf-8',
				dataType: 'json',
				type: "DELETE"
			}).done(function(response) 
			{
				if(response.ok)
				{

					g_user_role_jurisdiction.splice(user_role_index, 1);

					for(var i = 0; i < g_ui.user_summary_list.length; i++)
					{
						if(g_ui.user_summary_list[i].name == user_role.user_id)
						{
							var escaped_id =  convert_to_jquery_id(g_ui.user_summary_list[i]._id);
							$( "#" + escaped_id).replaceWith( user_entry_render(g_ui.user_summary_list[i], "", g_ui).join("") );
							break;
						}
					}
					
				}
			});
		}
		else
		{
				
			g_user_role_jurisdiction.splice(user_role_index, 1);

			for(var i = 0; i < g_ui.user_summary_list.length; i++)
			{
				if(g_ui.user_summary_list[i].name == user_role.user_id)
				{
					var escaped_id =  convert_to_jquery_id(g_ui.user_summary_list[i]._id);
					$( "#" + escaped_id).replaceWith( user_entry_render(g_ui.user_summary_list[i], "", g_ui).join("") );
					break;
				}
			}

		}

		
	}
}

function change_password(p_user_id, p_role)
{
	var user_index = -1;
	var user_list = g_ui.user_summary_list;
	var escaped_id =  convert_to_jquery_id(p_user_id);
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
		var role_index = user.roles.indexOf(p_role);
		if(role_index > -1)
		{
			user.roles.splice(role_index, 1);
			g_ui.user_summary_list[user_index] = user;
			$( "#" + escaped_id).replaceWith( user_entry_render(user, "", g_ui).join("") );
			create_status_message("user information saved", convert_to_jquery_id(user._id));
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

	//result.push('<div class="alert alert-success alert-dismissible">');
	result.push('<div>');
	//result.push('<a href="#" class="close" data-dismiss="alert" aria-label="close">&times;</a>');
	result.push('<strong>Info!</strong> ');
	result.push(p_message);
	result.push('</div>');

	document.getElementById(p_div_id + "_status_area").innerHTML = result.join("");

	window.setTimeout(clear_status, 30000);
}

function create_status_warning(p_message, p_div_id)
{
	var result = [];

	//result.push('<div class="alert alert-danger">');
	result.push('<div>');
	//result.push('<a href="#" class="close" data-dismiss="alert" aria-label="close">&times;</a>');
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


function jurisdiction_add_child_click(p_parent_id, p_name, p_user_id)
{
	var parent = get_jurisdiction(p_parent_id, g_jurisdiction_tree);
	var new_child  = null;

	if(parent)
	{
		if(parent.name == "/")
		{
			new_child  = jurisdiction_add(p_parent_id, "/" + p_name, p_user_id);
		}
		else
		{
			new_child  = jurisdiction_add(p_parent_id, parent.name + "/" + p_name, p_user_id);
		}
		
	}
	else
	{
		new_child  = jurisdiction_add(p_parent_id, p_name, p_user_id);
	}
	
	
	if
	(
		p_name != null && 
		p_name != "" && 
		p_name.match(/\W/) == null && 
		get_jurisdiction(new_child.id, g_jurisdiction_tree) == null
	)
	{
		var node_to_add_to = get_jurisdiction(p_parent_id, g_jurisdiction_tree);
		if(node_to_add_to)
		{
			node_to_add_to.children.push(new_child);
			g_jurisdiction_tree.date_last_updated = new Date();
			g_jurisdiction_tree.last_updated_by = p_user_id;
			var x = jurisdiction_render(node_to_add_to);

			var y=document.getElementById(p_parent_id.replace("/","_"));

			y.outerHTML = x.join("");
			
		}

	}
	
}

function jurisdiction_remove_child_click(p_parent_id, p_node_id, p_user_id)
{

	if(p_node_id != "jurisdiction_tree")
	{
		remove_jurisdiction(p_node_id, g_jurisdiction_tree)
		g_jurisdiction_tree.date_last_updated = new Date();
		g_jurisdiction_tree.last_updated_by = p_user_id;
		var node_to_add_to = get_jurisdiction(p_parent_id, g_jurisdiction_tree);
		if(node_to_add_to)
		{
			var x = jurisdiction_render(node_to_add_to);

			var y=document.getElementById(p_parent_id.replace("/","_"));

			y.outerHTML = x.join("");
			
		}

	}
	
}

function get_jurisdiction(p_search_id, p_node)
{
	var result = null;

	if(p_node._id && p_node._id == p_search_id)
	{
		return p_node;
	}

	if(p_node.id && p_node.id == p_search_id)
	{
		return p_node;
	}

	if(p_node.children != null)
	{
		for(var i = 0; i < p_node.children.length; i++)
		{
			var child = p_node.children[i];
			result = get_jurisdiction(p_search_id, child);
			if(result != null)
			{
				return result;
			}
		}
	}

	return result;
}


function remove_jurisdiction(p_search_id, p_node)
{
	if(p_node._id && p_node._id == p_search_id)
	{
		return;
	}

	if(p_node.children != null)
	{
		for(var i = 0; i < p_node.children.length; i++)
		{
			var child = p_node.children[i];
			if(p_node.children[i].id == p_search_id)
			{
				p_node.children.splice(i, 1);
				return;
			}
			else
			{
				remove_jurisdiction(p_search_id, child)
			}
		}
	}

	return;
}


function jurisdiction_add(p_parent_id, p_name, p_user_id)
{
	var result = {
		id: p_parent_id + "/" + p_name,
		name: p_name,
		date_created: new Date(),
		created_by: p_user_id,
		date_last_updated: new Date(),
		last_updated_by: p_user_id,
		is_active: true,
		is_enabled: true,
		children:[],
		parent_id: p_parent_id
	}

	return result;
}

function jurisdiction_update()
{
	
}


function jurisdiction_delete()
{
	
}


function user_role_jurisdiction_add
(
	p_role_name,
	p_user_id,
	p_jurisdiction_id,
	p_effective_start_date,
	p_effective_end_date,
	p_is_active,
	p_created_by
)
{
	var result = {
		_id: $mmria.get_new_guid(),
		role_name : p_role_name,
		user_id: p_user_id,
		jurisdiction_id: p_jurisdiction_id,

		effective_start_date: p_effective_start_date,
		effective_end_date: p_effective_end_date,
		is_active: p_is_active,
		date_created: new Date(),
		created_by: p_created_by,
		date_last_updated: new Date(),
		last_updated_by: p_created_by,
		data_type:"user_role_jursidiction"
	}

	return result;
}