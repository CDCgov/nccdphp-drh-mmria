'use strict';

var g_jurisdiction_list = null;
var g_policy_values = null;

$(function ()
{


	$(document).keydown(function(evt){
		if (evt.keyCode==90 && (evt.ctrlKey)){
			evt.preventDefault();
			undo_click();
		}

	});
  load_policy_values();

  document.getElementById("password_div").innerHTML = render_pass_word().join("");
  
});



function load_policy_values()
{
	$.ajax({
			url: location.protocol + '//' + location.host + '/api/policyvalues',
	}).done(function(response) {
			g_policy_values = response;
			load_user_role_jurisdiction();
	});

}


function load_user_role_jurisdiction()
{

  /*            int skip = 0,
            int take = 25,
            string sort = "by_date_created",
            string search_key = null,
            bool descending = false
            */



	$.ajax({
			url: location.protocol + '//' + location.host + '/api/user_role_jurisdiction_view/my-roles',
			headers: {          
				Accept: "text/plain; charset=utf-8",         
				"Content-Type": "text/plain; charset=utf-8"   
			  } 
	}).done(function(response) {

      g_jurisdiction_list = []

      var role_list_html = [];
	  //role_list_html.push("<p>[ ] ");
      if(g_sams_is_enabled.toLowerCase() != "true" && g_config_days_before_expires > 0)
      {
        if(g_days_til_expires >= 0)
        {
          role_list_html.push("Your password will expire in " + g_days_til_expires + " day(s).");
        }
        else
        {
          role_list_html.push("Your password expired " + (-1 * g_days_til_expires) + " day(s) ago.");
        }
      }
      role_list_html.push("</p>");
      role_list_html.push("<table border=1>");
      role_list_html.push("<tr bgcolor='#CCCCCC'><th colspan='7' scope='colgroup'>Role assignment list</th></tr>");
      role_list_html.push("<tr bgcolor='#CCCCCC'>");
      role_list_html.push("<th scope='col'>Role Name</th>");
      role_list_html.push("<th scope='col'>Jurisdiction</th>");
      role_list_html.push("<th scope='col'>Is Active</th>");
      role_list_html.push("<th scope='col'>Start Date</th>");
      role_list_html.push("<th scope='col'>End Date</th>");
      role_list_html.push("<th scope='col'>Days Till<br/>Role Expires</th>");
      role_list_html.push("<th scope='col'>Role&nbsp;Added&nbsp;By</th>");
      role_list_html.push("</tr>");
      for(var i in response.rows)
      {

          var current_date = new Date();
          var oneDay = 24*60*60*1000; // hours*minutes*seconds*milliseconds

          var value = response.rows[i].value;
          //if(value.user_id == g_uid)
          //{
            g_jurisdiction_list.push(value);

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

            if(i % 2 == 0)
            {
              role_list_html.push("<tr>");
              
            }
            else
            {
              role_list_html.push("<tr bgcolor='silver'>");
            }
            
            role_list_html.push("<td>" + escape(value.role_name) + "</td>");
			role_list_html.push("<td>" + escape(value.jurisdiction_id) + "</td>");
			
            if(diffDays < 0)
            {
              role_list_html.push("<td>false</td>");
            }
            else
            {
              role_list_html.push("<td>" + escape(value.is_active) + "</td>");
            }
            role_list_html.push("<td>" + escape(effective_start_date) + "</td>");
            role_list_html.push("<td>" + escape(effective_end_date) + "</td>");
            role_list_html.push("<td align='right'>" + diffDays + "</td>");
            role_list_html.push("<td>" + escape(value.last_updated_by) + "</td>");
            role_list_html.push("</tr>");
          //}
          
      }

      role_list_html.push("</table>");
      

      document.getElementById("role_list").innerHTML = role_list_html.join("");
      

	});

}

function render_pass_word()
{
  var result = [];
	if(g_sams_is_enabled != "True")
	{
		result.push("<table>");
		result.push("<tr><th colspan=2>Change Password</th></tr>");
		result.push("<tr><td><label for='new_password'>New Password</label></td><td><input id='new_password' type='password' value=''  /></td></tr>");
		result.push("<tr><td><label for='confirm_password'>Verify Password</label></td><td><input id='confirm_password' type='password' value=''  /></td></tr>");
		result.push("<tr><td>&nbsp;</td><td><input type='button' value='Update password' onclick='change_password_user_click()'/></td></tr>");
		result.push("<tr><td>&nbsp;</td><td id='message_area'></td></tr>");

		result.push("</table>");
	}
	else
	{
		result.push("&nbsp;")
	}
  return result;
}

function change_password_user_click()
{
	document.getElementById('message_area').innerHTML = "";
	
	var new_user_password = document.getElementById('new_password').value;
	var new_confirm_password = document.getElementById('confirm_password').value;

	if
	(
		new_user_password == new_confirm_password &&
		is_valid_password(new_user_password) 
	)
	{

		var user_message = {
			UserName: g_uid,
			Value: new_user_password
		}

		$.ajax({
			url: location.protocol + '//' + location.host + '/api/passwordChange',
			contentType: 'application/json; charset=utf-8',
			dataType: 'json',
			data: JSON.stringify(user_message),
			type: "POST"
		}).done(function(response) 
		{
			var response_obj = eval(response);
			if(response_obj.ok)
			{
				//document.getElementById('form_content_id').innerHTML = user_render(g_ui, "", g_ui).join("");
				document.getElementById('message_area').innerHTML = "new password saved";
				document.getElementById('new_password').value = "";
				document.getElementById('confirm_password').value = "";
			}
		});
		
	}
	else
	{

		document.getElementById('message_area').innerHTML = "invalid password.<br/>be sure that verify and password match,<br/>  minimum length is: " + g_policy_values.minimum_length + " and should only include characters [a-zA-Z0-9!@#$%?* ]";
		//create_status_warning("invalid password and confirm", convert_to_jquery_id(user._id));
		//console.log("got nothing.");
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

    var valid_character_re = /^[a-zA-Z0-9!@#$\%\?\* \-]+$/g;

	if(
		p_value &&
		p_value.length >= g_policy_values.minimum_length &&
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

