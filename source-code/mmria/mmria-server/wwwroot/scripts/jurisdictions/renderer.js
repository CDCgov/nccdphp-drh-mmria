
function jurisdiction_render(p_ui, p_data, p_metadata_path, p_object_path, p_is_grid_context)
{
	var result = [];

	result.push("<li>");


	result.push("name:&nbsp;");
	result.push(p_data.name);
	result.push("&nbsp;<br/>");
	if(p_data.children != null)
	{
		result.push("<ul>");
		for(var i = 0; i < p_data.children.length; i++)
		{
	
			var child = p_data.children[i];
			Array.prototype.push.apply(result, jurisdiction_render(p_ui, child));
	
		}
		result.push("</ul>");
	}

	result.push("</li>");


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

function user_entry_render(p_user, p_i)
{
	var result = [];

	if(p_i % 2)
	{
		result.push("<tr id='" +  convert_to_jquery_id(p_user._id) + "' style='background:#DDDDDD;' valign=top><td>");
	}
	else
	{
		result.push("<tr id='" +  convert_to_jquery_id(p_user._id) + "' valign=top><td>");
	}

	result.push(p_user.name);
	result.push("</td><td>");
	result.push("<table>");
	result.push("<tr><th>Role Name</th><th>&nbsp;</th><tr>");
	for(var j = 0; j < p_user.roles.length; j++)
	{
		result.push("<tr><td>");
		result.push(p_user.roles[j]);
		result.push("</td><td><input type='button' value='Remove Role' onclick='remove_role(\"" + p_user._id + "\",\"" + p_user.roles[j] + "\")'/></td></tr>");
	}
	result.push("<tr><td colspan=2 align=right>");
	Array.prototype.push.apply(result, user_role_render(p_user));
	result.push("</td></tr>")
	result.push("</table></td>");
	result.push("<td>");
	result.push("<strong>");
	result.push(p_user.name);
	result.push("</strong><br/>");
	/*
	result.push("<input type='text' value='");
	result.push(p_user.name);
	result.push("'/>");
	result.push("<input type='button' value='change user name'/>");
	result.push("<br/><br/>");
	*/	
	result.push("New Password <input type='password' value='' role='confirm_1' path='" + p_user._id + "' />");
	result.push("<br/>Verify Password<input type='password' value='' role='confirm_2' path='" + p_user._id + "' />");
	result.push("</td>");
	result.push("<td>&nbsp;")
	//result.push("<input type='button' value='disable user'/>");
	result.push("<input type='button' value='Save User " + p_user.name + " changes' onclick='change_password_user_click(\"" + p_user._id + "\")'/>");
	
	result.push("<span id='");
	result.push(convert_to_jquery_id(p_user._id));
	result.push("_status_area'><span id=");
	result.push("</td>")
	result.push("</tr>");



	return result;
}

function user_role_render(p_user)
{
	var result = [];
	var role_set = [ '', 'abstractor','committee_member','form_designer'];

	result.push("<select size='1' path='" + p_user._id + "'>")
	for(var i = 0; i < role_set.length; i++)
	{
		var item = role_set[i];
		if(p_user.roles.indexOf(item) > -1)
		{
			// do nothing
			/*
			result.push("<option selected='true'>");
			result.push(item);
			result.push("</option>");
			*/
		}
		else
		{
			result.push("<option>");
			result.push(item);
			result.push("</option>");
		}
	}
	result.push("</select>");
	result.push("<input type='button' value='Add Role' onclick='add_role(\"" + p_user._id + "\")' />");
	return result;
}
