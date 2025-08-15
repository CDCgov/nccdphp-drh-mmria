'use strict';

var g_message_data = {};
var g_jurisdiction_list = [];
var g_metadata = null;
var default_object = null;

var g_message_data = null;

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
  //get_metadata();
});


window.onload = main;



async function main()
{
    await init_broadcast_messages();
}

async function init_broadcast_messages()
{
    const get_data_response = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/broadcast-message/GetBroadcastMessageList`
    });

    g_message_data = get_data_response;

    render_broadcast_message_one();
    render_broadcast_message_two();

}

function render_broadcast_message_one()
{
    const broadcast_message_container = document.getElementById("broadcast_published_message_one");
    if(g_message_data.message_one.publish_status == 1)
        broadcast_message_container.innerHTML = render_published_version(g_message_data.message_one, "broadcast_message_detail_button_one_click");
}

function render_broadcast_message_two()
{
    const broadcast_message_container = document.getElementById("broadcast_published_message_two");
    if(g_message_data.message_two.publish_status == 1)
        broadcast_message_container.innerHTML = render_published_version(g_message_data.message_two, "broadcast_message_detail_button_two_click");
}


function render_published_version(message, p_button)
{
    var publishedAlertTypeStyling = [];
    if (message.published.type == "information")
        publishedAlertTypeStyling = ["bg-tertiary", "cdc-icon-alert_01 text-primary", "btn-primary"]
    else if (message.published.type == "warning")
        publishedAlertTypeStyling = ["alert-warning", "cdc-icon-alert_02", "btn-primary"]
    else
        publishedAlertTypeStyling = ["alert-danger", "cdc-icon-close-circle", "btn-primary"]    
    return `
        <div class="alert border-top-0 ${publishedAlertTypeStyling[0]} col-md-12" id="alert_unique_16262b641c316a">
        <div class="row d-flex padding-pagealert align-items-center">
            <div class="flex-grow-0 col">
                <span class="fi ${publishedAlertTypeStyling[1]}" aria-hidden="true"></span>                        
            </div>
            <div class="col">
                <p class="margin-pagealert">
                ${message.published.title}
                </p>		
            </div>
            ${message.published.body.length > 0 ? `<div class="col flex-grow-0"><input class="btn ${publishedAlertTypeStyling[2]}" type="button" onclick="${p_button}()" value="Details" /></div>` : ``}
        </div>
    `;
}

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

function broadcast_message_detail_button_one_click() 
{
    //var p_capitalized_message_type = p_message_type.charAt(0).toUpperCase() + p_message_type.slice(1);
    $mmria.info_dialog_show("System Message 1", "", g_message_data.message_one.published.body.replace("\n","<br/><br/>"), g_message_data.message_one.published.type);
}

function broadcast_message_detail_button_two_click() 
{
    //var p_capitalized_message_type = p_message_type.charAt(0).toUpperCase() + p_message_type.slice(1);
    $mmria.info_dialog_show("System Message 2", "", g_message_data.message_two.published.body.replace("\n","<br/><br/>"), g_message_data.message_two.published.type);
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
    role_list_html.push("<p class='pl-3'>");
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
    
      role_list_html.push("<div class='vertical-control pl-0 pr-0 col-md-12 rounded'>");
      role_list_html.push("<table aria-label='Role assignment list' class='table'>");
      role_list_html.push("<thead class='thead'>");
      role_list_html.push("<tr class='header-level-top-black'>");
      role_list_html.push("<th colspan='7'>Role assignment list</th>");
      role_list_html.push("</tr>");
      role_list_html.push("<tr class='header-level-2'>");
      role_list_html.push("<th>Role Name</th>");
      role_list_html.push("<th>Case Folder Access</th>");
      role_list_html.push("<th>Is Active</th>");
      role_list_html.push("<th>Start Date</th>");
      role_list_html.push("<th>End Date</th>");
      role_list_html.push("<th>Days until<br/>Role Expires</th>");
      role_list_html.push("<th>Role Added By</th>");
      role_list_html.push("</tr>");
      role_list_html.push("</thead>");
      
      role_list_html.push("<tbody>");
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
                role_list_html.push("<tr>");
              }
              else
              {
                role_list_html.push("<tr>");
              }
              
                role_list_html.push("<td>" + escape(value.role_name) + "</td>");
                if(value.jurisdiction_id == "/")
                {
                    role_list_html.push("<td>Top Folder</td>");
                }
                else
                {
                    role_list_html.push("<td>" + escape(value.jurisdiction_id) + "</td>");
                }
                
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
                role_list_html.push("<td>" + diffDays + "</td>");
                role_list_html.push("<td>" + escape(value.last_updated_by) + "</td>");
              role_list_html.push("</tr>");
            //}
          }
            
        }
      
      role_list_html.push("</tbody>");
    role_list_html.push("</table>");
    role_list_html.push("</div>");
    
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


async function external_link_click(p_url)
{
    function cancel()
    {
        $mmria.confirm_external_nav_dialog_confirm_close();
    }

    function confirm()
    {
        $mmria.confirm_external_nav_dialog_confirm_close();
        window.open
        (
            p_url, 
            "_blank"
        );
    }

    $mmria.confirm_external_nav_dialog_show
    (
        confirm, 
        cancel
    )
}


async function erase_mm_link_click()
{
    function cancel()
    {
        $mmria.confirm_external_nav_dialog_confirm_close();
    }

    function confirm()
    {
        $mmria.confirm_external_nav_dialog_confirm_close();
        window.open
        (
            "https://www.erase-mm-cvsproject.org", 
            "_blank"
        );
    }

    $mmria.confirm_external_nav_dialog_show
    (
        confirm, 
        cancel
    )
}

async function review_to_action_link_click()
{
    function cancel()
    {
        $mmria.confirm_external_nav_dialog_confirm_close();
    }

    function confirm()
    {
        $mmria.confirm_external_nav_dialog_confirm_close();
        window.open
        (
            "https://reviewtoaction.org/tools/resource-center", 
            "_blank"
        );
    }

    $mmria.confirm_external_nav_dialog_show
    (
        confirm, 
        cancel
    )
}

async function system_documentation_link_click()
{
    function cancel()
    {
        $mmria.confirm_external_nav_dialog_confirm_close();
    }

    function confirm()
    {
        $mmria.confirm_external_nav_dialog_confirm_close();
        window.open
        (
            "https://cdcpartners.sharepoint.com/sites/NCCDPHP/MMRIA", 
            "_blank"
        );
    }

    $mmria.confirm_external_nav_dialog_show
    (
        confirm, 
        cancel
    )
}

