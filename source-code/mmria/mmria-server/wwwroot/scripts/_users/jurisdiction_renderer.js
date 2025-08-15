function jurisdiction_render(p_data, p_path)
{
	var result = [];

    if(p_path == null)
    {
        p_path = "";
    }

	if( p_data._id)
	{ 
		
		result.push("<div id='" + p_data._id.replace("/","_") + "'");
		result.push("<p><b>case_folder tree<b> ");

        for (const key in g_managed_jurisdiction_set) 
        {
            if (g_managed_jurisdiction_set.hasOwnProperty(key)) 
            {
                if(p_data.name.indexOf(key) == 0)
                {
                    result.push("<br/><input type='button' value='save changes to case_folder tree' onclick='init_small_loader(function(){ save_jurisdiction_tree_click(\"\") })' />");
                    result.push(`<span class="spinner-container spinner-small ml-1"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>`);
                    break;
                }
            }
        }
        
		
		result.push("</p><ul>");
		result.push("<li>");
        if(p_data.name == "/")
        {
            result.push("<label>Top Folder");
        }
        else
        {
            result.push("<label>");
		    result.push(p_data.name);
        }
		result.push("&nbsp;");
        for (const key in g_managed_jurisdiction_set) 
        {
            if (g_managed_jurisdiction_set.hasOwnProperty(key)) 
            {
                if(p_data.name.indexOf(key) == 0)
                {
                    result.push(`<span>`);
                    result.push("<input id='add_child_of_" + p_data._id.replace("/","_") + "' /></label>&nbsp;<input type='button' value='add' onclick='init_small_loader(function(){ jurisdiction_add_child_click(\"" + p_data._id + "\", document.getElementById(\"add_child_of_" + p_data._id.replace("/","_") + "\").value, \"\") })' />");
                    result.push(`<span class="spinner-container spinner-small ml-1"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>`);
                    result.push(`</span>`);
                    break;
                }
            }
        }
		
	}
	else
	{
		result.push("<li id='" + p_data.id.replace("/","_") + "'>");
        result.push("<label>");
		result.push(p_data.name);
		result.push("&nbsp;");
		result.push(`<span>`);

        let new_path = `${p_path}${p_data.name}`;
        if(p_path == "/")
        {
            new_path = p_data.name;
        }

        for (const key in g_managed_jurisdiction_set) 
        {
            if (g_managed_jurisdiction_set.hasOwnProperty(key)) 
            {
                if(new_path.indexOf(key) == 0)
                {
                    result.push("<input id='add_child_of_" + p_data.id.replace("/","_") + "' />&nbsp;<input type='button' value='add' onclick='init_small_loader(function(){ jurisdiction_add_child_click(\"" + p_data.id + "\", document.getElementById(\"add_child_of_" + p_data.id.replace("/","_") + "\").value, \"\") })' /></label>&nbsp;<input type='button' value='delete' onclick='init_small_loader(function(){ jurisdiction_remove_child_click(\"" + p_data.parent_id + "\", \"" + p_data.id + "\", \"\") })' />");
                    result.push(`<span class="spinner-container spinner-small ml-1"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>`);
                    result.push(`</span>`);
                    break;
                }
            }
        }


	}

	if(p_data.children != null)
	{
		for(var i = 0; i < p_data.children.length; i++)
		{
			result.push("<ul>");
			var child = p_data.children[i];
            let new_path = `${p_path}${p_data.name}`;
            if(p_path == "")
            {
                new_path = p_data.name;
            }
			Array.prototype.push.apply(result, jurisdiction_render(child, new_path));
			result.push("</ul>");
			
		}
	}
	result.push("</li>");
	
	if(p_data._id)
	{
		result.push("</ul>");
		result.push("<br/><input type='button' value='save changes to case_folder tree' onclick='init_small_loader(function(){ save_jurisdiction_tree_click(\"\") })' />");
		result.push(`<span class="spinner-container spinner-small ml-1"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>`);
		result.push("</div>")
	}

	return result;
}


/*
{
	_id: "jurisdiction_tree", 
	_rev: "1-b3e65347756f3cf46116dac1e8d9cec7", 
	name: "/", 
	children:null
	created_by:null
	date_created:"0001-01-01T00:00:00"
	date_last_updated:"0001-01-01T00:00:00"
	last_updated_by:null
	data_type:"jursidiction_tree"
}
*/


function save_jurisdiction_tree_click()
{
	if(g_jurisdiction_tree && g_current_u_id)
	{ 
		$.ajax({
					url: location.protocol + '//' + location.host + '/api/jurisdiction_tree',
					contentType: 'application/json; charset=utf-8',
					dataType: 'json',
					data: JSON.stringify(g_jurisdiction_tree),
					type: "POST"
			}).done(function(response) 
			{
						var response_obj = eval(response);
						if(response_obj.ok)
						{
							g_jurisdiction_tree._rev = response_obj.rev; 
							//document.getElementById('form_content_id').innerHTML = editor_render(g_user_list, "", g_ui).join("");
						}
						//{ok: true, id: "2016-06-12T13:49:24.759Z", rev: "3-c0a15d6da8afa0f82f5ff8c53e0cc998"}
					console.log("jurisdiction_tree sent", response);
			});
	}
}


function save_user_role_jurisdiction(p_user_role, p_user, p_user_id)
{
	if(p_user_role && p_user_id)
	{ 
		$.ajax({
			url: location.protocol + '//' + location.host + '/api/user_role_jurisdiction',
			contentType: 'application/json; charset=utf-8',
			dataType: 'json',
			data: JSON.stringify(p_user_role),
			type: "POST",
		})
		.done(
			function(response) 
			{
				if(response)
				{
					var response_obj = eval(response);
					if(response_obj.ok)
					{
						
						for(var i in g_user_role_jurisdiction)
						{
							if(g_user_role_jurisdiction[i]._id == response_obj.id)
							{
								g_user_role_jurisdiction[i]._rev = response_obj.rev; 
								//document.getElementById('form_content_id').innerHTML = editor_render(g_user_list, "", g_ui).join("");
	
								var render_result = render_role_list_for(p_user, p_user_id);
								var role_list_for_ = document.getElementById("role_list_for_" + p_user.name);
								role_list_for_.outerHTML = render_result.join("");

								break;
							}
						}
					}
						//{ok: true, id: "2016-06-12T13:49:24.759Z", rev: "3-c0a15d6da8afa0f82f5ff8c53e0cc998"}
					console.log("jurisdiction_tree sent", response);
				}
				else
				{
					alert("You are not authorized to make this change.");
				}
			}
		);
	}
}
