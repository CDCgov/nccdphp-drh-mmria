
var g_power_bi_user_list = null;

$(function ()
{//http://www.w3schools.com/html/html_layout.asp
  'use strict';
	/*profile.on_login_call_back = function (){
				load_users();
    };*/
	//profile.initialize_profile();

	load_user_list();

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



function load_user_list()
{

	$.ajax({
		url: location.protocol + '//' + location.host + '/api/user',
	}).done(function(response) 
	{
		g_power_bi_user_list = response;
		
		render();
	});

}

function render_power_bi_user_list()
{

	var result = [];
	result.push("<br/>");
	result.push("<table>");
	result.push("<tr  bgcolor='silver'>")
	result.push("<th scope='col'>user name</th>");
	result.push("<th scope='col'>power bi user</th>");
	result.push("<th scope='col'>action</th>");
	result.push("</tr>");

	//result.push("<tr><td colspan=2 align=center><input type='button' value='save list' onclick='server_save()' /></td></tr>")

	
	for(let i = 0; i < g_power_bi_user_list.rows.length; i++)
	{
		var item = g_power_bi_user_list.rows[i].doc;

		if(i % 2)
		{
			result.push("<tr bgcolor='#CCCCCC'>");
		}
		else
		{
			result.push("<tr>");
		}
		result.push("<td>");
		result.push(encodeHTML(item.name));
		result.push("</td>");
		result.push("<td><label title='");
		if(item.alternate_email != null)
		{
			result.push(encodeHTML(item.alternate_email));
		}
		result.push("'><input size='120' type='text' value='");
		if(item.alternate_email != null)
		{
			result.push(escape(item.alternate_email));
		}
		result.push("' onchange='update_item("+ i +", this.value)'/></label></td>");
		result.push("<td colspan=2 align=center><input type='button' value='save' onclick='server_save("+ i +")' /></td>")
		result.push("</tr>");		
		
	}

	
	result.push("</table>");
	result.push("<br/>");
	
	return result;

}

function update_item(p_index, p_value)
{
	g_power_bi_user_list.rows[p_index].doc.alternate_email = p_value;


}

function server_save(p_index)
{

	let user = g_power_bi_user_list.rows[p_index].doc;

	$.ajax({
				url: location.protocol + '//' + location.host + '/api/user',
				contentType: 'application/json; charset=utf-8',
				dataType: 'json',
				data: JSON.stringify(user),
				type: "POST"
		}).done(function(response) 
		{

			let response_obj = eval(response);
			if(response_obj.ok)
			{
				user._rev = user.rev; 

				render();
			}
		});
		
}


function render()
{
	document.getElementById('output').innerHTML = render_power_bi_user_list().join("");
}

function encodeHTML(s) 
{
	let result = s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
    return result;
}




