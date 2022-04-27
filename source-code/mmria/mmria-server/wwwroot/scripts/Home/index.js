'use strict';

var g_jurisdiction_list = [];
var g_metadata = null;
var default_object = null;

$(function ()
{
	$(document).keydown(function(evt){
		if (evt.keyCode==90 && (evt.ctrlKey)){
			evt.preventDefault();
			undo_click();
		}

	});

  //set_session_warning_interval();
  //$.datetimepicker.setLocale('en');
  get_metadata();
});

function get_metadata()
{
  $.ajax({
		url: location.protocol + '//' + location.host + '/api/metadata',
	}).done(function(response) {
    g_metadata = response;
    default_object =  create_default_object(g_metadata, {});
    load_user_role_jurisdiction();
	});
}

function load_user_role_jurisdiction()
{

  /*            
  int skip = 0,
  int take = 25,
  string sort = "by_date_created",
  string search_key = null,
  bool descending = false
  */

	$.ajax({
    url: location.protocol + '//' + location.host + '/api/user_role_jurisdiction_view/my-roles',//&search_key=' + g_uid,
    headers: {          
      Accept: "text/plain; charset=utf-8",         
      "Content-Type": "text/plain; charset=utf-8"   
    } 
	}).done(function(response) {
    g_jurisdiction_list = []

    var role_list_html = [];

    //role_list_html.push("<p>[ " + g_uid + " ] ");
    role_list_html.push("<p>");
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
    
    role_list_html.push("<table class='table'>");
      role_list_html.push("<thead class='thead'>");
      role_list_html.push("<tr class='tr bg-tertiary'>");
      role_list_html.push("<th class='th h4' colspan='7' scope='colgroup'>Role assignment list</th>");
      role_list_html.push("</tr>");
      role_list_html.push("</thead>");
      role_list_html.push("<thead class='thead'>");
      role_list_html.push("<tr class='tr'>");
      role_list_html.push("<th class='th' scope='col'>Role Name</th>");
      role_list_html.push("<th class='th' scope='col'>Case Folder Access</th>");
      role_list_html.push("<th class='th' scope='col'>Is Active</th>");
      role_list_html.push("<th class='th' scope='col'>Start Date</th>");
      role_list_html.push("<th class='th' scope='col'>End Date</th>");
      role_list_html.push("<th class='th' scope='col'>Days Til<br/>Role Expires</th>");
      role_list_html.push("<th class='th' scope='col'>Role Added By</th>");
      role_list_html.push("</tr>");
      role_list_html.push("</thead>");
      
      role_list_html.push("<tbody class='tbody'>");
        if(response)
        {
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
                role_list_html.push("<tr class='tr'>");
              }
              else
              {
                role_list_html.push("<tr class='tr'>");
              }
              
                role_list_html.push("<td class='td'>" + escape(value.role_name) + "</td>");
                if(value.jurisdiction_id == "/")
                {
                    role_list_html.push("<td class='td'>Top Folder</td>");
                }
                else
                {
                    role_list_html.push("<td class='td'>" + escape(value.jurisdiction_id) + "</td>");
                }
                
                if(diffDays < 0)
                {
                  role_list_html.push("<td class='td'>false</td>");
                }
                else
                {
                  role_list_html.push("<td class='td'>" + escape(value.is_active) + "</td>");
                }
                
                role_list_html.push("<td class='td'>" + escape(effective_start_date) + "</td>");
                role_list_html.push("<td class='td'>" + escape(effective_end_date) + "</td>");
                role_list_html.push("<td class='td'>" + diffDays + "</td>");
                role_list_html.push("<td class='td'>" + escape(value.last_updated_by) + "</td>");
              role_list_html.push("</tr>");
            //}
          }
            
        }
      
      role_list_html.push("</tbody>");
    role_list_html.push("</table>");
    
    document.getElementById("role_list").innerHTML = role_list_html.join("");

	});
}



function open_blank_version(p_section)
{
	var blank_window = window.open('./print-version','_blank_version',null,false);

	window.setTimeout(function()
	{
		blank_window.create_print_version(g_metadata, default_object, p_section)
	}, 1000);	
}
