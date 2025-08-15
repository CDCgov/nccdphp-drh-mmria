
var g_policy_values = null;
var g_jurisdiction_tree = null;
var g_user_role_jurisdiction = null;
var g_current_u_id = null;
var g_jurisdiction_list = [];

let g_managed_jurisdiction_set = {}

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
      let test = value.match(/^\d+-\d+-\d+T\d+:\d+:\d+.\d+Z$/);
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
{
    //http://www.w3schools.com/html/html_layout.asp
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
			load_user_name();
			
	});

}

function load_user_name()
{
	$.ajax
	(
		{
			url: location.protocol + '//' + location.host + '/api/user_role_jurisdiction_view/my-roles',
			headers: {          
				Accept: "text/plain; charset=utf-8",         
				"Content-Type": "text/plain; charset=utf-8"   
			} 
		}
	).done(function(response) 
	{

		if(response)
		{
			for(let i = 0; i < response.rows.length; i++)
			{
				
				let value = response.rows[i].value;
				g_current_u_id = value.user_id
				break;
			}
			
			load_curent_user_role_jurisdiction();
		}
        
	});

}

function load_curent_user_role_jurisdiction()
{

  /*            
  int skip = 0,
  int take = 25,
  string sort = "by_date_created",
  string search_key = null,
  bool descending = false
  */

	$.ajax
    ({
    url: location.protocol + '//' + location.host + '/api/user_role_jurisdiction_view/my-roles',//&search_key=' + g_uid,
    headers: {          
      Accept: "text/plain; charset=utf-8",         
      "Content-Type": "text/plain; charset=utf-8"   
    } 
	})
    .done(function(response) 
    {
        g_jurisdiction_list = []

        if(response)
        {
          for(var i in response.rows)
          {

            var current_date = new Date();
            var oneDay = 24*60*60*1000; // hours*minutes*seconds*milliseconds

            var value = response.rows[i].value;

            var diffDays = 0;
            var effective_start_date = "";
            var effective_end_date = "never";

            if(value.effective_start_date && value.effective_start_date != "")
            {
                effective_start_date = value.effective_start_date.split('T')[0];
            }

            if(value.effective_end_date && value.effective_end_date != "")
            {
                effective_end_date = value.effective_end_date.split('T')[0];
                diffDays = Math.round((new Date(value.effective_end_date).getTime() - current_date.getTime())/(oneDay));
            }


            if(diffDays < 0)
            {
                role_list_html.push("<td class='td'>false</td>");
            }
            else
            {
                g_jurisdiction_list.push(value);
                if
                (
                    value.role_name == "jurisdiction_admin" 
                )
                {
                    g_managed_jurisdiction_set[value.jurisdiction_id] = true;
                }
                else if
                (
                    value.role_name == "installation_admin"
                )
                {
                    if(value.jurisdiction_id == null)
                    {
                        g_managed_jurisdiction_set["/"] = true;
                    }
                    else
                    {
                        g_managed_jurisdiction_set[value.jurisdiction_id] = true;
                    }
                }
            }
          }
        }
        load_jurisdictions()
	});
}



function load_jurisdictions()
{
	var metadata_url = location.protocol + '//' + location.host + '/api/jurisdiction_tree';

	$.ajax
	({
			url: metadata_url
	}).done(function(response) 
	{

			g_jurisdiction_tree = response;
            document.getElementById('form_content_id').innerHTML = jurisdiction_render(g_jurisdiction_tree).join("");
			//load_user_jurisdictions();
			//document.getElementById('navigation_id').innerHTML = navigation_render(g_jurisdiction_list, 0, g_uid).join("");

	});
}

function server_save(p_user)
{
	console.log("server save");
	//var current_auth_session = profile.get_auth_session_cookie();

	if(current_auth_session)
	{ 
		$.ajax({
					url: location.protocol + '//' + location.host + '/api/user',
					contentType: 'application/json; charset=utf-8',
					dataType: 'json',
					data: JSON.stringify(p_user),
					type: "POST"
			}).done(function(response) 
			{


						var response_obj = eval(response);
						if(response_obj.ok)
						{
							g_user_list._rev = response_obj.rev; 
							document.getElementById('form_content_id').innerHTML = editor_render(g_user_list, "").join("");
						}
						//{ok: true, id: "2016-06-12T13:49:24.759Z", rev: "3-c0a15d6da8afa0f82f5ff8c53e0cc998"}
					console.log("metadata sent", response);
			});
	}

}

function set_jurisdiction_show_hide_children_state(p_parent_id, shouldShow, is_nested = false, parent_should_show)
{
    var hide_button_element = document.getElementById(p_parent_id + "_hide_children");
    var show_button_element = document.getElementById(p_parent_id + "_show_children");
    var case_folders = document.getElementById('form_content_id').children;
    show_button_element.setAttribute('aria-hidden', '' + shouldShow + '');
    show_button_element.hidden = shouldShow;
    hide_button_element.setAttribute('aria-hidden', '' + !shouldShow + '');
    hide_button_element.hidden = !shouldShow;
    if(!is_nested && hide_button_element.hidden)
        show_button_element.focus();
    else if(!is_nested && !hide_button_element.hidden)
        hide_button_element.focus();
    for(let i = 0; i < case_folders.length; i++)
    {
        var parent_child = case_folders.item(i);
        if(parent_child.classList.length > 0 && parent_child.classList[0].includes(p_parent_id + '-child'))
        {
            var label_child = parent_child.children[0].children[0];
            if(label_child.children.length > 0 && label_child.children[0].hidden == true)
            {
                set_jurisdiction_show_hide_children_state(label_child.children[0].id.split('_show_children')[0], true, true, parent_should_show);
            }
            else if(label_child.children.length > 0 && label_child.children[0].hidden == false)
            {
                set_jurisdiction_show_hide_children_state(label_child.children[0].id.split('_show_children')[0], false, true, parent_should_show == true ? false : parent_should_show);
            }
            if(!is_nested)
            {
                if(shouldShow)
                {
                    parent_child.removeAttribute('style');
                    parent_child.setAttribute('aria-hidden', 'false');
                }
                else
                {
                    parent_child.setAttribute('style', 'display: none !important;');
                    parent_child.setAttribute('aria-hidden', 'true');
                }
            }
            else
            {
                if(shouldShow && !parent_should_show)
                {
                    parent_child.setAttribute('style', 'display: none !important;');
                    parent_child.setAttribute('aria-hidden', 'true');
                }
                else if(!shouldShow && parent_should_show)
                {
                    parent_child.setAttribute('style', 'display: none !important;');
                    parent_child.setAttribute('aria-hidden', 'true');
                }
                else if(!shouldShow && !parent_should_show)
                {
                    parent_child.setAttribute('style', 'display: none !important;');
                    parent_child.setAttribute('aria-hidden', 'true');
                }
                else
                {
                    parent_child.removeAttribute('style');
                    parent_child.setAttribute('aria-hidden', 'false');
                }
            }
        }
    }
    //set_show_hide_folders_state(case_folders, shouldShow, p_parent_id);
}

function set_show_hide_folders_state(case_folders, shouldShow, p_parent_id)
{
    for(let i = 0; i < case_folders.length; i++)
    {
        var parent_child = case_folders.item(i);
        if(parent_child.classList.length > 0 && parent_child.classList[0].includes(p_parent_id + '-child'))
        {
            if(shouldShow)
            {
                parent_child.removeAttribute('style');
                parent_child.setAttribute('aria-hidden', 'false');
            }
            else
            {
                parent_child.setAttribute('style', 'display: none !important;');
                parent_child.setAttribute('aria-hidden', 'true');
            }
        }
    }
}

function jurisdiction_add_child_click(p_parent_id, p_name, p_user_id)
{
    if(p_name == "" || p_name == null)
    {
        set_jurisdiction_add_child_control_valid_state(p_parent_id, false, 'Node name is required');
    }
    else
    {
        set_jurisdiction_add_child_control_valid_state(p_parent_id, true);
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
                var case_folders_parent = document.getElementById('form_content_id');
                var y = document.getElementById('add-node-form-' + p_parent_id.replace("/", "_"));
                var x = render_new_case_folder(new_child, null, parseInt(y.dataset.nestedLevel));
                if(y.dataset.nestedLevel == '0')
                {
                    $('#case_folder_break').before(x);
                }
                else
                {
                    document.getElementById(p_parent_id + '-label').replaceWith(render_show_hide_buttons(node_to_add_to, parseInt(y.dataset.nestedLevel)));
                    case_folders_parent.insertBefore(x, case_folders_parent.children.namedItem('add-node-form-' + p_parent_id.replace("/", "_")).nextSibling);
                }
            }
            document.getElementById('add_child_of_' + p_parent_id.replace("/", "_")).value = "";
        }
        else if (get_jurisdiction(new_child.id, g_jurisdiction_tree) != null)
        {
            set_jurisdiction_add_child_control_valid_state(p_parent_id, false, 'Child node name already exists.');
        }
        else if (p_name.match(/\W/) != null)
        {
            set_jurisdiction_add_child_control_valid_state(p_parent_id, false, 'Node name cannot contain spaces.');
        }
    }
}

function set_jurisdiction_add_child_control_valid_state(p_parent_id, is_valid, message = "")
{
    var control_id = p_parent_id.replace("/", "_");
    var add_child_form_control = document.getElementById('add_child_of_' + control_id);
    var add_child_form_control_error = document.getElementById('error_add_child_of_' + control_id);
    if(!is_valid)
    {
        add_child_form_control.setAttribute('aria-invalid', true);
        add_child_form_control.classList.add('is-invalid');
        add_child_form_control_error.innerHTML = message;
    }
    else
    {
        add_child_form_control.setAttribute('aria-invalid', false);
        add_child_form_control.classList.remove('is-invalid');
        add_child_form_control_error.innerHTML = '';
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
            var case_folders_parent = document.getElementById('form_content_id');
            var current_case_folder = case_folders_parent.children.namedItem('add-node-form-' + p_node_id.replace('/', '_'));
            var current_case_folder_parent = document.getElementById('add-node-form-' + p_parent_id.replace('/', '_'));
            var current_nested_level = parseInt(current_case_folder.dataset.nestedLevel);
            var case_folders = [...case_folders_parent.children];
            for(var child_case of case_folders.slice(case_folders.indexOf(current_case_folder)))
            {
                child_case_nested_level = parseInt(child_case.dataset.nestedLevel);
                if((child_case_nested_level === current_nested_level) && child_case.id === current_case_folder.id)
                {
                    $(child_case).remove();
                }
                else if(child_case_nested_level > current_nested_level)
                {
                    $(child_case).remove();
                }
                else if (child_case_nested_level <= current_nested_level)
                {
                    child_case.focus();
                    break;
                }
            }
            if(parseInt(current_case_folder_parent.dataset.nestedLevel) === parseInt(current_case_folder_parent.nextSibling.dataset.nestedLevel))
            {
                var hide_button_element = document.getElementById(p_parent_id + "_hide_children");
                var show_button_element = document.getElementById(p_parent_id + "_show_children");
                show_button_element.setAttribute('aria-hidden', 'true');
                show_button_element.hidden = true;
                hide_button_element.setAttribute('aria-hidden', 'true');
                hide_button_element.hidden = true;
            }
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

async function save_jurisdiction_tree_click()
{
    const save_status = document.getElementById('case_folder_save_status');
    const save_status_success = document.getElementById('case_folder_save_success');
    save_status.classList.add('spinner-active');
	if(g_jurisdiction_tree && g_current_u_id)
	{
        const response = await get_http_post_response("api/jurisdiction_tree", g_jurisdiction_tree);
        const response_obj = eval(response);
        if(response_obj.ok)
        {
            g_jurisdiction_tree._rev = response_obj.rev;
            save_status_success.classList.add('spinner-active');
                setTimeout(() => {
                    save_status_success.classList.remove('spinner-active');
                }, 4000);
        }
        save_status.classList.remove('spinner-active');
	}
}

async function get_http_post_response
(
    p_url_suffix,
    p_data
)
{
    let response = {};

    const url = `${location.protocol}//${location.host}/${p_url_suffix}`
    try
    {
        const response_promise = await fetch(url, {
            method: "post",
            headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json; charset=utf-8',
            'dataType': 'json',
            },
            body: JSON.stringify(p_data)
        });

        mmria_check_if_need_to_redirect(response_promise);
        
        response = await response_promise.json();
    }  
    catch(xhr) 
    {
        $mmria.unstable_network_dialog_show(xhr, xhr.status);
        if (xhr.status == 401) 
        {
            let redirect_url = location.protocol + '//' + location.host;
            window.location = redirect_url;
        }
        else if (xhr.status == 200 && xhr.responseText.length >= 49000) 
        {
            let redirect_url = location.protocol + '//' + location.host;
            window.location = redirect_url;
        }
    }

    return response;
}