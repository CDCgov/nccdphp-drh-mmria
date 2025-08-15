'use strict';

//http://www.w3schools.com/css/css3_flexbox.asp
var g_metadata = null;
var g_data = null;
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
    console.log("g_ui.set_value: ", p_path, p_value);
    console.log("value: ", p_value.value);
    console.log("get_eval_string(p_path): ", g_ui.get_eval_string(p_path));

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
    result.home_record.last_name = "new-last-name";
    result.home_record.first_name = "new-first-name";
		var new_data = [];

		for(var i in g_ui.data_list)
		{
			new_data.push(g_ui.data_list[i]);
		}

		var new_record_id = new Date().toISOString();
		new_data.push
		(
      result
		);

		g_ui.data_list = new_data;
    g_data = result;
		g_ui.selected_record_id = result._id;
		g_ui.selected_record_index = g_ui.data_list.length -1;

    var url = escape(location.protocol) + '//' + escape(location.host) + '#/' + escape(g_ui.selected_record_index) + '/home_record';

    window.location = url;

    return result;
  },
  
  case_view_list : [],
  
  case_view_request : {
    total_rows: 0,
    page :1,
    skip : 0,
    take : 100,
    sort : "by_date_created",
    search_key : null,
    descending : true,
    get_query_string : function(){
      var result = [];
      result.push("?skip=" + (this.page - 1) * this.take);
      result.push("take=" + this.take);
      result.push("sort=" + this.sort);
  
      if(this.search_key)
      {
        result.push("search_key=\"" + this.search_key.replace(/"/g, '\\"').replace(/\n/g,"\\n") + "\"");
      }
  
      result.push("descending=" + this.descending);
  
      return result.join("&");
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

  load_profile();
});


function load_values()
{
	$.ajax({
			url: location.protocol + '//' + location.host + '/api/values',
	}).done(function(response) {
			g_couchdb_url = response.couchdb_url;
      load_profile();

	});

}



function load_profile()
{

  var uid = g_uid;
	var pwd = $mmria.getCookie("pwd");
  var auth_session = $mmria.getCookie("AuthSession");


  if($mmria.getCookie("AuthSession"))
  {
		profile.is_logged_in = true;
		profile.user_name = uid;
    profile.password = pwd;
		profile.user_roles = [ "committee_member" ];
		profile.auth_session = auth_session;
    profile.render();
    get_metadata();
    get_case_set();
  }
  else
	{

    profile.is_logged_in=false;
    profile.user_name = '';
    profile.password = null;
    profile.user_roles=[];
    profile.auth_session='';
    profile.render();

  }
/*
    profile.on_login_call_back = function ()
    {

      get_metadata();
      get_case_set();
      
      //load_documents();

    };*/


  	
}


function get_case_set()
{
	$.ajax({
			url: location.protocol + '//' + location.host + '/api/case',
	}).done(function(response) 
  {
        g_ui.data_list = [];
        for(var i = 0; i < response.rows.length; i++)
        {
          if(response.rows[i].doc._id.indexOf("_design") < 0)
          {
            g_ui.data_list.push(response.rows[i].doc);
          }
          
        }

        document.getElementById('navbar').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");
        document.getElementById('form_content_id').innerHTML = page_render(true, g_metadata, default_object, g_ui, "g_metadata", "default_object", false, 0, 0, 0).join("");
        
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
              section.style.display = "none";
          }
        }


  }).fail(function(response){ console.log("fail get_case_set", response)});

}


function get_metadata()
{
  	$.ajax({
			url: location.protocol + '//' + location.host + '/api/metadata',
	}).done(function(response) {
			g_metadata = response;
      metadata_summary(g_metadata_summary, g_metadata, "g_metadata", 0, 0);
      default_object =  create_default_object(g_metadata, {});
      //create_validator_map(g_validator_map, g_validation_description_map, g_metadata, "g_metadata");

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
      var selected_record_id = g_data._id;
      if($.isNumeric(g_ui.url_state.path_array[0]))
      {
          g_selected_index = parseInt(g_ui.url_state.path_array[0]);
      }
      else //if(g_ui.data_list.length > 0)
      {
        g_selected_index = g_ui.data_list.length - 1;
      }
          
        if(e.isTrusted)
        {

          var new_url = e.newURL || window.location.href;
          g_ui.url_state = url_monitor.get_url_state(new_url);

          if(g_ui.url_state.path_array && g_ui.url_state.path_array.length > 0 && (parseInt(g_ui.url_state.path_array[0]) >= 0))
          {

            g_data = g_ui.data_list[parseInt(g_ui.url_state.path_array[0])];

            document.getElementById('navbar').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");
            document.getElementById('form_content_id').innerHTML = page_render(true, g_metadata, g_data, g_ui, "g_metadata", "g_data", false, 0, 0, 0).join("");
            apply_tool_tips();


            var section_list = document.getElementsByTagName("section");


            if(g_ui.url_state.path_array.length > 2 && (parseInt(g_ui.url_state.path_array[0]) >= 0))
            {
              for(var i = 0; i < section_list.length; i++)
              {
                var section = section_list[i];
                if(section.id == g_ui.url_state.path_array[1])
                {
                    section.style.display = "block";
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
          else
          {
            g_data = null;
            document.getElementById('navbar').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");
            document.getElementById('form_content_id').innerHTML = page_render(true, g_metadata, default_object, g_ui, "g_metadata", "default_object", false, 0, 0, 0).join("");
            apply_tool_tips();

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
                    section.style.display = "none";
                }
            }
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
      g_data = g_ui.data_list[parseInt(g_ui.url_state.path_array[0])];

      document.getElementById('navbar').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");
      document.getElementById('form_content_id').innerHTML = page_render(true, g_metadata, g_data, g_ui, "g_metadata", "g_data", false, 0, 0, 0).join("");
      apply_tool_tips();


  		var section_list = document.getElementsByTagName("section");
      if(g_ui.url_state.path_array.length > 2 && (parseInt(g_ui.url_state.path_array[0]) >= 0))
      {
        for(var i = 0; i < section_list.length; i++)
        {
          var section = section_list[i];
          if(section.id == g_ui.url_state.path_array[1])
          {
              section.style.display = "block";
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
    else
    {
              
      g_data = null;

      document.getElementById('navbar').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");

      document.getElementById('form_content_id').innerHTML = page_render(true, g_metadata, default_object, g_ui, "g_metadata", "default_object", false, 0, 0, 0).join("");
      apply_tool_tips();

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
              section.style.display = "none";
          }
      }

    }

  }
  else
  {
    // do nothing for now
  }
};


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

flatpickr(" .date", {
	utc: true,
	//defaultDate: "2016-12-27T00:00:00.000Z",
	enableTime: false,
});

$( ".datetime" ).datetimepicker();

$("input.number").TouchSpin(
{
  verticalbuttons: true,
  min: 0,
  max: 10000,
  step: 1,
  maxboostedstep: 10
});

$("input.number").mask("#,##0.00", {reverse: true});
    apply_validation();
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


function open_print_version(p_section)
{

	var print_window = window.open('./print-version','_print_version',null,false);

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

