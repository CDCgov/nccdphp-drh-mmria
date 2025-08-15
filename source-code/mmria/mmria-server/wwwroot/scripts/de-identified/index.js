'use strict';

var g_is_committee_member_view = true;
var g_metadata = null;
var g_data = null;
var g_source_db = null;
var g_data_is_checked_out = null;
var g_jurisdiction_list = [];
var g_metadata_path = [];
var g_validator_map = [];
var g_event_map = [];
var g_validation_description_map = [];
var g_selected_index = null;
var g_selected_delete_index = null;
var g_couchdb_url = null;
var g_localDB = null;
var g_remoteDB = null;
var g_metadata_summary = [];
var default_object = null;
var g_change_stack = [];
var g_default_ui_specification = null;
var g_use_position_information = true;
var g_look_up = {};
var g_release_version = null;

var g_value_to_display_lookup = {};
var g_display_to_value_lookup = {};
var g_value_to_index_number_lookup = {};
var g_other_specify_lookup = {};
var g_charts = new Map();
var g_chart_data = new Map();
var g_is_data_analyst_mode = false;

const g_dependent_parent_to_child = new Map();
const g_dependent_child_to_parent = new Map();
const g_dependent_child_metadata = new Map();


function g_set_data_object_from_path(p_object_path, p_metadata_path, p_dictionary_path, value)
{
    return;
}

var g_ui = {
  url_state: {
    selected_form_name: null,
    "selected_id": null,
    "selected_child_id": null,
    "path_array" : []

  },

  data_list : [],

  broken_rules : [],

  set_value: function(p_path, p_value)
  {
    // console.log("g_ui.set_value: ", p_path, p_value);
    // console.log("value: ", p_value.value);
    // console.log("get_eval_string(p_path): ", g_ui.get_eval_string(p_path));

    eval(g_ui.get_eval_string(p_path + ' = "' + p_value.value.replace('"', '\\"')  + '"'));

    //var target = eval(g_ui.get_eval_string(p_path));
  },

  get_eval_string: function (p_path)
  {
  	var result = "g_data" + p_path.replace(new RegExp('/','gm'),".").replace(new RegExp('\\.(\\d+)\\.','g'),"[$1]\\.").replace(new RegExp('\\.(\\d+)$','g'),"[$1]");
    //return an  array with 2 parts.
      // g_data['attribute'].attribute...

  	return result;
  },

  add_new_case: function()
	{
    var result = create_default_object(g_metadata, {});

    result.date_created = new Date();
    //result.created_by = g_uid;
    result.date_last_updated = new Date();
    //result.last_updated_by = g_uid;
    result.home_record.last_name = "new-last-name";
    result.home_record.first_name = "new-first-name";

		var new_data = [];

		for(var i in g_ui.case_view_list)
		{
			new_data.push(g_ui.case_view_list[i]);
		}

		var new_record_id = new Date().toISOString();
		new_data.push
		(
      {
        id:result._id,
        key:result._id,
        value: {
        first_name: result.home_record.first_name,
        middle_name: result.home_record.middle_name,
        last_name: result.home_record.last_name,
        date_of_death_year: result.home_record.date_of_death.year,
        date_of_death_month: result.home_record.date_of_death.month,

        date_created: result.date_created,
        created_by: result.created_by,
        date_last_updated: result.date_last_updated,
        last_updated_by: result.last_updated_by,

        record_id: result.record_id,
        agency_case_id: result.agency_case_id,
        date_of_committee_review: result.committee_review.date_of_review
        }
      }
		);

		g_ui.case_view_list = new_data;
    g_data = result;
    g_change_stack = [];
		g_ui.selected_record_id = result._id;
    g_ui.selected_record_index = g_ui.case_view_list.length -1;
    
    set_local_case(g_data, function (){
      save_case(g_data, function(){
            
      var url = location.protocol + '//' + location.host + '/Case#/' + g_ui.selected_record_index + '/home_record';
      window.location = url;
      });
    });

    return result;
  },

  case_view_list : [],
   case_view_request : {
    total_rows: 0,
    page :1,
    skip : 0,
    take : 100,
    sort : "date_last_updated",
    search_key : null,
    descending : true,
    case_status: "3",
    get_query_string: function () {
        var result = [];
        result.push('?skip=' + (this.page - 1) * this.take);
        result.push('take=' + this.take);
        result.push('sort=' + this.sort);
        result.push('descending=' + this.descending);
        result.push('case_status=' + this.case_status);
        
        if (this.search_key)
        {
          result.push(
            'search_key=' +
            encodeURIComponent(this.search_key)

          );
        }
  
        return result.join('&');
      }
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
{


	$(document).keydown(function(evt){
		if (evt.keyCode==90 && (evt.ctrlKey)){
			evt.preventDefault();
			undo_click();
		}

	});


/*
        // Get the modal
        var modal = document.getElementById('myModal');

        // Get the button that opens the modal
        var btn = document.getElementById("myBtn");
    
        // Get the <span> element that closes the modal
        var span = document.getElementsByClassName("close")[0];
    
        // When the user clicks on the button, open the modal
        btn.onclick = function() {
            modal.style.display = "block";
        }
    
        // When the user clicks on <span> (x), close the modal
        span.onclick = function() {
            modal.style.display = "none";
        }
    
        // When the user clicks anywhere outside of the modal, close it
        window.onclick = function(event) {
            if (event.target == modal) {
                modal.style.display = "none";
            }
        } 

*/


  // https://pure-essence.net/2010/02/14/jquery-session-timeout-countdown/
  // create the warning window and set autoOpen to false
  var sessionTimeoutWarningDialog = $("#sessionTimeoutWarningDiv");

  $("#sessionTimeoutOkButton").click(function() {
    // close dialog

    clearInterval(timer);
    running = false;
    $("#sessionTimeoutWarningDiv").dialog('close');
    profile.update_session_timer();
  });

  //$(sessionTimeoutWarningDialog).html(initialSessionTimeoutMessage);
  $(sessionTimeoutWarningDialog).dialog({
      title: 'Session Expiration Warning',
      autoOpen: false,    // set this to false so we can manually open it
      closeOnEscape: false,
      draggable: false,
      width: 600,
      minHeight: 50,
      backgroundColor: 0xADC71A, // rgb(173, 199, 26),
      modal: true,
      beforeclose: function() { // bind to beforeclose so if the user clicks on the "X" or escape to close the dialog, it will work too
          // stop the timer
          clearInterval(timer);

          // stop countdown
          running = false;
      },
      buttons: {
          OK: function() {
              // close dialog

              clearInterval(timer);
              running = false;
              $(this).dialog('close');
              profile.update_session_timer();
              
          }
      },
      resizable: false,
      open: function() {
          // scrollbar fix for IE
          $("#sessionTimeoutWarningDiv").css('display','block');
          $('body').css('overflow','hidden');
          $('#sessionTimeoutExpiredId').hide();
          $('#sessionTimeoutPendingId').css('display','block');
          
      },
      close: function() {
          // reset overflow
          $('body').css('overflow','auto');
          clearInterval(timer);
          running = false;
          $(this).dialog('close');
          profile.update_session_timer();
      }
  });
  // end of dialog


  // start the idle timer
  //$.idleTimer(idleTime);

  // bind to idleTimer's idle.idleTimer event
  $(document).bind("sessionWarning", function()
  {


    $(sessionTimeoutWarningDialog).show();
      // if the user is idle and a countdown isn't already running
      //if($.data(document,'idleTimer') === 'idle' && !running)
      if(!running)
      {
          var counter = redirectAfter;
          running = true;


          // intialisze timer
          $('#'+sessionTimeoutCountdownId).html(redirectAfter);
          // open dialog
          $(sessionTimeoutWarningDialog).dialog('open');

          // create a timer that runs every second
          timer = setInterval(function(){
              counter -= 1;

              // if the counter is 0, redirect the user
              if(counter === 0) {
                  //$(sessionTimeoutWarningDialog).html(expiredMessage);
                  $('#sessionTimeoutExpiredId').show();
                  $('#sessionTimeoutPendingId').css('display','none');
                  $(sessionTimeoutWarningDialog).dialog('disable');
                  //window.location = redirectTo;
                  running = false;
                  clearInterval(timer);
                  clearInterval(session_warning_interval_id);
                  profile.logout();
              } else {
                  $('#'+sessionTimeoutCountdownId).html(counter);
              };
          }, 1000);
      };
  });



  //set_session_warning_interval();

  $.datetimepicker.setLocale('en');

  load_user_role_jurisdiction();
});



function load_user_role_jurisdiction()
{

  /*            int skip = 0,
            int take = 25,
            string sort = "by_date_created",
            string search_key = null,
            bool descending = false
            */



	$.ajax({
			url: location.protocol + '//' + location.host + '/api/user_role_jurisdiction_view/my-roles',//&search_key=' + g_uid,
	}).done(function(response) {

      g_jurisdiction_list = []
      for(var i in response.rows)
      {

          var value = response.rows[i].value;
          //if(value.user_id == g_uid && value.role_name == "abstractor")
          //{
            g_jurisdiction_list.push(value.jurisdiction_id);
          //}
          
      }
			
      $("#landing_page").hide();
      $("#logout_page").hide();
      $("#footer").hide();
      $("#root").removeClass("header");

      get_release_version();

 

	});

}
var update_session_timer_interval_id = null;

function load_profile()
{
    profile.on_login_call_back = function ()
    {
      $("#landing_page").hide();
      $("#logout_page").hide();
      $("#footer").hide();
      $("#root").removeClass("header");
      get_release_version();
      /*
      if
      (
          g_source_db == "mmrds" &&
          profile.user_roles && profile.user_roles.length > 0 &&
          profile.user_roles.indexOf("_admin") < 0
      )
      {

        update_session_timer_interval_id = window.setInterval(profile.update_session_timer, 120000);
      }*/
    };

    profile.on_logout_call_back = function (p_user_name, p_password)
    {
      if(update_session_timer_interval_id != null)
      {
        window.clearInterval(update_session_timer_interval_id);
        update_session_timer_interval_id = null;
      }

      //$("#landing_page").show();
      $("#root").addClass("header");
      $("#footer").show();
      if
      (
          profile.user_roles && profile.user_roles.length > 0 && 
          profile.user_roles.indexOf("_admin") < 0 &&
          profile.user_roles.indexOf("committee_member") < 0
      )
      {
        //replicate_db_and_log_out(p_user_name, p_password);
      }

       document.getElementById('navbar').innerHTML = "";
      document.getElementById('form_content_id').innerHTML ="";


      var url = location.protocol + '//' + location.host + '/';
      window.location.href = url;
    };


  	profile.initialize_profile();
}


function apply_filter_click() 
{
    g_ui.case_view_request.page=1;
    get_case_set();
}


function get_case_set(p_call_back)
{

  var case_view_url = location.protocol + '//' + location.host + '/api/de_id_view' + g_ui.case_view_request.get_query_string();

  $.ajax({
    url: case_view_url,
  }).done(function(case_view_response) {
    
    // console.log(case_view_response);
    g_ui.case_view_list = [];
    g_ui.case_view_request.total_rows = case_view_response.total_rows;

    for(var i = 0; i < case_view_response.rows.length; i++)
    {
        g_ui.case_view_list.push(case_view_response.rows[i]);
    }

    if(p_call_back)
    {
      p_call_back();
    }
    else
    {
      var post_html_call_back = [];
      document.getElementById('navbar').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");
      document.getElementById('form_content_id').innerHTML ="<h4>Fetching data from database.</h4><h5>Please wait a few moments...</h5>";
  
  
      document.getElementById('form_content_id').innerHTML = page_render(g_metadata, default_object, g_ui, "g_metadata", "default_object", "", false, post_html_call_back).join("");
      if(post_html_call_back.length > 0)
      {
        eval(post_html_call_back.join(""));
      }
  
      var section_list = document.getElementsByTagName("section");
      for(var i = 0; i < section_list.length; i++)
      {
        var section = section_list[i];
        if(section.id == "app_summary")
        {
            section.style.display = "block";
        }
        else
        {
          
            section.style.display = "block";
            //section.style["grid-template-columns"] = "1fr 1fr 1fr";
          
        }
      }
    }
    
    

});


}


function get_release_version()
{
  $.ajax
  ({

      url: location.protocol + '//' + location.host + '/api/version/release-version',
  })
  .done(function(response) 
  {
      g_release_version = response;
      get_ui_specification();
      
	});
}


function get_ui_specification()
{
 	$.ajax({
			url: location.protocol + '//' + location.host + `/api/version/${g_release_version}/ui_specification`,
	}).done(function(response) {
      g_default_ui_specification = response;
      get_metadata();
	});
}

function get_metadata()
{
  document.getElementById('form_content_id').innerHTML ="<h4>Fetching data from database.</h4><h5>Please wait a few moments...</h5>";

  	$.ajax({
			url: location.protocol + '//' + location.host + `/api/version/${g_release_version}/metadata`,
	}).done(function(response) {
			g_metadata = response;
      metadata_summary(g_metadata_summary, g_metadata, "g_metadata", 0, 0);
      default_object =  create_default_object(g_metadata, {});

      set_list_lookup(g_display_to_value_lookup, g_value_to_display_lookup, g_value_to_index_number_lookup, g_metadata, "");      

      get_case_set();
      
     g_ui.url_state = url_monitor.get_url_state(window.location.href);
      if(window.onhashchange)
      {
        window.onhashchange ({ isTrusted: true, newURL : window.location.href });
      }
      else
      {
        window.onhashchange = window_on_hash_change;
        window.onhashchange ({ isTrusted: true, newURL : window.location.href });
      }
	});
}

function window_on_hash_change(e)
{
  /*
  e = HashChangeEvent
  {
    isTrusted: true,
    oldURL: "http://localhost:12345/react-test/#/",
    newURL: "http://localhost:12345/rea
  }*/

  if(g_data)
  {
    
        if(e.isTrusted)
        {

          var new_url = e.newURL || window.location.href;
          g_ui.url_state = url_monitor.get_url_state(new_url);

          if(g_ui.url_state.path_array && g_ui.url_state.path_array.length > 0 && (parseInt(g_ui.url_state.path_array[0]) >= 0))
          {

            var case_id = g_data._id;
            save_case(g_data, function()
            {
              get_specific_case(g_ui.case_view_list[parseInt(g_ui.url_state.path_array[0])].id);
            });
            

          }
          else
          {
            save_case(g_data, function(){
              g_data = null;
              get_case_set(function(){ g_render();} );
            });
            
          }
      }
      
    
   // g_data_access.set_data(g_data);
			
  }
  else if(e.isTrusted)
  {

    var new_url = e.newURL || window.location.href;

    g_ui.url_state = url_monitor.get_url_state(new_url);


    if(g_ui.url_state.path_array && g_ui.url_state.path_array.length > 0 && (parseInt(g_ui.url_state.path_array[0]) >= 0))
    {
      if(g_ui.case_view_list.length > 0)
      {
        get_specific_case(g_ui.case_view_list[parseInt(g_ui.url_state.path_array[0])].id); 
              //g_data = g_ui.data_list[parseInt(g_ui.url_state.path_array[0])];
      }
      else
      {
        g_render();
      }
           //      
		}
    else
    {

      g_render();

    }

  }
  else
  {
    // do nothing for now
  }


};


function get_specific_case(p_id)
{

  var case_url = location.protocol + '//' + location.host + '/api/de_id?case_id=' + p_id;

  $.ajax({
    url: case_url,
  }).done(function(case_response) {
  
    g_data = case_response;
    g_render();
  });

}

function save_case(p_data, p_call_back)
{


    if(p_call_back)
    {
      p_call_back();
    }
  

}


function delete_case(p_id, p_rev)
{

  
}



function g_render()
{

  var post_html_call_back = [];

  document.getElementById('navbar').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");
  document.getElementById('form_content_id').innerHTML = page_render(g_metadata, g_data, g_ui, "g_metadata", "g_data", "", false, post_html_call_back).join("");

  apply_tool_tips();

  if(post_html_call_back.length > 0)
  {
    try
    {
      eval(post_html_call_back.join(""));
    }
    catch(ex)
    {
      console.log(ex);
    }
  }

  var section_list = document.getElementsByTagName("section");

  if(g_ui.url_state.path_array[0] == "summary")
  {
    for(var i = 0; i < section_list.length; i++)
    {
      var section = section_list[i];

      if(section.id == "app_summary")
      {
        section.style.display = "block";
        //section.style.display = "grid";
        //section.style["grid-template-columns"] = "1fr 1fr 1fr";
      }
      else
      {
          section.style.display = "none";
      }
    }
  }
  else
  {
    if(g_ui.url_state.path_array.length > 2 && (parseInt(g_ui.url_state.path_array[0]) >= 0))
    {
      for(var i = 0; i < section_list.length; i++)
      {
        var section = section_list[i];
        if(section.id == g_ui.url_state.path_array[1])
        {
          section.style.display = "block";
          //section.style.display = "grid";
          //section.style["grid-template-columns"] = "1fr 1fr 1fr";
        }
        else
        {
            section.style.display = "none";
        }
      }
    }
    else
    {
      for(var i = 0; i < section_list.length; i++)
      {
        var section = section_list[i];
  
        if(section.id == g_ui.url_state.path_array[1] + "_id")
        {
          section.style.display = "block";
        }
        else
        {
            section.style.display = "none";
        }
      }
    }
  }
}



function show_print_version()
{
  window.open("./print-version", "_print_version");
}

function show_data_dictionary()
{
  window.open("./data-dictionary", "_data_dictionary");
}

function show_user_administration()
{
  window.open("./_users", "_users");
}


function apply_tool_tips()
{
  $('[rel=tooltip]').tooltip();
  $( ".time" ).datetimepicker({
    format: 'LT',
    icons: {
      time: 'x24 fill-p cdc-icon-clock_01',
      date: 'x24 fill-p cdc-icon-calendar_01',
      up: "x24 fill-p cdc-icon-chevron-circle-up",
      down: "x24 fill-p cdc-icon-chevron-circle-down",
      previous: 'x24 fill-p fill-p cdc-icon-chevron-circle-left-light',
      next: 'x24 fill-p cdc-icon-chevron-circle-right-light'
    }
  });
  //$( "[metadata_type='date']" ).datetimepicker();
  /*
  flatpickr(" .date", {
    utc: true,
    //defaultDate: "2016-12-27T00:00:00.000Z",
    enableTime: false,
    onSelect: function(p_value,evnt) 
    {
                  g_set_data_object_from_path(p_object_path, p_metadata_path, p_value);
    }
  });*/

  //$( ".datetime" ).datetimepicker();


  $("input.number").numeric();
  $("input.number0").numeric({ decimal: false });
  $("input.number1").numeric({ decimalPlaces: 1 });
  $("input.number2").numeric({  decimalPlaces: 2 });
  $("input.number3").numeric({ decimalPlaces: 3 });
  $("input.number4").numeric({ decimalPlaces: 4 });
  $("input.number5").numeric({ decimalPlaces: 5 });
  $("input.number").attr("size", "15");
  $("input.number0").attr("size", "15");
  $("input.number1").attr("size", "15");
  $("input.number2").attr("size", "15");
  $("input.number3").attr("size", "15");
  $("input.number4").attr("size", "15");
  $("input.number5").attr("size", "15");

  /*
  $("input.number").TouchSpin({
                  verticalbuttons: true,
          decimals: 3,
                  min: 0,
                  max: 10000,
                  step: 1,
                  maxboostedstep: 10
              });*/

  //$("input.number").mask("#,##0[.00", {reverse: true});

    //apply_validation();
}




function apply_validation()
{
    for(var i in g_ui.broken_rules)
    {
      var element = document.getElementById(i);
      if(g_ui.broken_rules[i] == true)
      {
        
        if
        (
          element &&
          element.className.indexOf('failed-validation') < 0
        )
        {
         element.className += ' failed-validation';
         /*
         var validation_text = element.getAttribute('validation-tooltip');
         if(validation_text)
         {
          var span_node = document.createElement("span");
          span_node.setAttribute('class', 'tooltip-content');
          span_node.innerText = element.getAttribute('validation-tooltip');
          element.appendChild(span_node);
         }
         */

        }
      }
      else
      {
        
        if
        (
          element &&
          element.className.indexOf('failed-validation') > 0
        )
        {
          var class_array = element.className.split(' ');
          class_array.splice(class_array.indexOf('failed-validation'),1);
          element.className = class_array.join(' ');
        }
        
      }
    }
}

function delete_record(p_index)
{
  if(p_index == g_selected_delete_index)
  {
    var data = g_ui.case_view_list[p_index];
    g_selected_delete_index = null;
 
    $.ajax({
      url: location.protocol + '//' + location.host + '/api/case?case_id=' + data.id,
  }).done(function(case_response) {
  
      delete_case(case_response._id, case_response._rev);

  });

  }
  else
  {
      if(g_selected_delete_index != null && g_selected_delete_index > -1)
      {
          var old_id = g_ui.case_view_list[g_selected_delete_index].id;
          $("tr[path='" + old_id + "']").css("background", "");
      }

      g_selected_delete_index = p_index;
      var id = g_ui.case_view_list[p_index].id;
      $("tr[path='" + id + "']").css("background", "#BBBBBB");
      
  }
}




var save_interval_id = null;

	
function print_case_onchange()
{
  var section_id = escape(document.getElementById("print_case_id").value);

  if(section_id && section_id.length > 0)
  {
    if(section_id == "core-summary")
    {
      open_core_summary("all");
    }
    else
    {
      open_print_version(section_id);
    }
    
  }
}

function open_print_version(p_section)
{

	var print_window = window.open('./print-version','_print_version',null,false);
	// console.log('open_print_version de-identified:', g_data);
	window.setTimeout(function()
	{
		print_window.create_print_version(g_metadata, g_data, p_section)
	}, 1000);	
}


function open_core_summary(p_section)
{

	var print_window = window.open('./core-elements','_core_summary',null,false);

	window.setTimeout(function()
	{
		print_window.create_print_version(g_metadata, g_data, p_section)
	}, 1000);	
}


function open_blank_version(p_section)
{

	var blank_window = window.open('./print-version','_blank_version',null,false);

	window.setTimeout(function()
	{
		blank_window.create_print_version(g_metadata, default_object, p_section)
	}, 1000);	
}



function add_new_form_click(p_metadata_path, p_object_path)
{
//   console.log("add_new_form_click: " + p_metadata_path + " , " + p_object_path);
  var metadata = eval(p_metadata_path);
  var form_array = eval(p_object_path);

  var new_form = create_default_object(metadata, {});
  var item = new_form[metadata.name][0];
  form_array.push(item);


  save_case(g_data, function(){
    
    var post_html_call_back = [];
    document.getElementById(metadata.name + "_id").innerHTML = page_render(metadata, form_array, g_ui, p_metadata_path, p_object_path, "", false, post_html_call_back).join("");
    if(post_html_call_back.length > 0)
    {
      eval(post_html_call_back.join(""));
    }
  });

}



function save_form_click()
{
  if(profile.user_roles && profile.user_roles.length > 0 && profile.user_roles.indexOf("abstractor") > -1)
  {
    save_case(g_data, create_save_message);
  }
  
}

function create_save_message()
{
	var result = [];

	result.push('<div class="alert alert-success alert-dismissible">');
	result.push('<a href="#" class="close" data-dismiss="alert" aria-label="close">&times;</a>');
	result.push('<p>Case information has been saved</p>');
	result.push('</div>');

	document.getElementById("nav_status_area").innerHTML = result.join("");

	window.setTimeout(clear_nav_status_area, 5000);
}

function clear_nav_status_area()
{
	document.getElementById("nav_status_area").innerHTML = "<div>&nbsp;</div>";
}

function set_local_case(p_data, p_call_back)
{

  if(profile.user_roles && profile.user_roles.indexOf("abstractor") > -1)
  {
    //localStorage.setItem('case_' + p_data._id, JSON.stringify(p_data));

    if(p_call_back)
    {
      p_call_back();
    }
  }
  else
  {
    if(p_call_back)
    {
      p_call_back();
    }
  }
}


function get_local_case(p_id)
{
  var result = null;
  if(profile.user_roles && profile.user_roles.indexOf("abstractor") > -1)
  {
    result = JSON.parse(localStorage.getItem('case_' + p_id));
  }

  return result;
}

function undo_click()
{
  var current_change = g_change_stack.pop();
  if(current_change)
  {

    var metadata = eval(current_change.metadata_path);


    if(metadata.type.toLowerCase() == "list" && metadata['is_multiselect'] && metadata.is_multiselect == true)
    {
      var item = eval(current_change.object_path);
      if(item.indexOf(current_change.old_value) > -1)
      {
        item.splice(item.indexOf(current_change.old_value), 1);
      }
      else
      {
        item.push(current_change.old_value);
      }
    }
    else if(metadata.type.toLowerCase() == "boolean")
    {
      eval(current_change.object_path + ' = ' + current_change.old_value);
    }
    else
    {
      eval(current_change.object_path + ' = "' + current_change.old_value.replace(/"/g, '\\"').replace(/\n/g,"\\n") + '"');
    }

  }

  g_render();
}

function enable_print_button(event) 
{
  const { value } = event.target;
  //targeting next sibling buttons
  const printButton = event.target.nextSibling; 
  printButton.disabled = !value; // if there is a value it will be enabled.
  const pdfViewButton = printButton.nextSibling;
  pdfViewButton.disabled = !value;
  const pdfSaveButton = pdfViewButton.nextSibling;
  pdfSaveButton.disabled = !value;
}

let unique_tab_name = '';
function pdf_case_onclick(event, type_output) 
{
  const btn = event.target;

	const dropdown = ( type_output == 'view' )
		? btn.previousSibling.previousSibling
		: btn.previousSibling.previousSibling.previousSibling;

	// get value of selected option
  const section_name = dropdown.value;

  unique_tab_name = '_pdf_tab_' + Math.random().toString(36).substring(2, 9);

  if (section_name) 
  {
    if (section_name == 'core-summary') 
    {

        window.setTimeout(function()
        {
            openTab('./pdf-version', unique_tab_name, section_name, type_output);
        }, 1000);	

      
    } 
    else 
    {
      // data-record of selected option, set default in case it comes from the summary case list
	  // If selected from detail list
	  const selectedOption = dropdown.options[dropdown.options.selectedIndex];
      const record_number = selectedOption.dataset.record;
  
      window.setTimeout(function()
      {
          openTab('./pdf-version', unique_tab_name, section_name, type_output, record_number);
      }, 1000);	
      
    }
  }

}

function pdf_case_onclick_summary(event, id, section, p_type_output) 
{
	// get the g_data for the selected case
	let case_url = location.protocol + '//' + location.host + '/api/de_id?case_id=' + id;

	$.ajax({
		url: case_url,
	}).done(function(case_response) {
		g_data = case_response;
	});

	const btn = event.target;
	const	section_name = section;

	unique_tab_name = '_pdf_tab_' + Math.random().toString(36).substring(2, 9);
	const record_number = undefined;
	const tabName = ( section == 'all' ) ? '_all' : '_pdf_print_version';

	window.setTimeout(function()
	{
		openTab('./pdf-version', unique_tab_name, section_name, p_type_output, record_number);
	}, 1000);

}


  
function print_case_onclick(event) 
{
	const btn = event.target;
	const dropdown = btn.previousSibling;
	// get value of selected option
	const section_name = dropdown.value;
	unique_tab_name = '_print_tab_' + Math.random().toString(36).substring(2, 9);
 
	if (section_name) 
	{
	  if (section_name == 'core-summary') 
	  {
  
		  window.setTimeout(function()
		  {
			  openTab('./core-elements', unique_tab_name, 'all');
		  }, 1000);	
  
		
	  } 
	  else 
	  {
		// data-record of selected option
		const selectedOption = dropdown.options[dropdown.options.selectedIndex];
		const record_number = selectedOption.dataset.record;
  
  
		window.setTimeout(function()
		{
			openTab('./print-version', unique_tab_name, section_name, record_number);
		}, 1000);	
		
	  }
	}
  
}

function openTab(pageRoute, tabName, p_section, p_type_output, p_number) 
{
  // check if a WindowProxy object has already been created.

  if (!window[tabName] || window[tabName].closed) 
  {
    window[tabName] = window.open(pageRoute, tabName, null, false);
    window[tabName].addEventListener('load', () => {
      window[tabName].create_print_version(
        g_metadata,
        g_data,
        p_section,
				p_type_output,
        p_number,
        g_metadata_summary,
        false,
        true
      );
    });
  } 
  else 
  {
	// if the WindowProxy Object already exists then just call the function on it
    window[tabName].create_print_version(
      g_metadata,
      g_data,
      p_section,
			p_type_output,
      p_number,
      g_metadata_summary,
      false,
      true
    );
  }
}