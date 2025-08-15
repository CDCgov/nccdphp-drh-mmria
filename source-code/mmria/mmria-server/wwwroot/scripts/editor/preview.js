'use strict';

//http://www.w3schools.com/css/css3_flexbox.asp

var g_metadata = null;
var g_data = null;

var g_ui = {
  url_state: {
    selected_form_name: null,
    "selected_id": null,
    "selected_child_id": null,
    "path_array" : []

  },
  data_list : [],
  set_value: function(p_path, p_value)
  {
    console.log("g_ui.set_value: ", p_path, p_value);
    console.log("value: ", p_value.value);
    console.log("get_eval_string(p_path): ", g_ui.get_eval_string(p_path));

    eval(g_ui.get_eval_string(p_path + ' = "' + p_value.value.replace('"', '\\"')  + '"'));

    var target = eval(g_ui.get_eval_string(p_path));
  },
  get_eval_string: function (p_path)
  {
  	var result = "g_data" + p_path.replace(new RegExp('/','gm'),".").replace(new RegExp('.(\\d+).','g'),"[$1].").replace(new RegExp('.(\\d+)$','g'),"[$1]");
    //return an  array with 2 parts.
      // g_data['attribute'].attribute...

  	return result;

  },
  add_new_case: function()
	{

    var result = create_default_object(g_metadata, {});

    if(g_is_pmss_enhanced)
    {
        result.tracking.last_name = "new-last-name";
        result.tracking.first_name = "new-first-name";
    }
    else
    {
        result.home_record.last_name = "new-last-name";
        result.home_record.first_name = "new-first-name";
    }

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


    let url = location.protocol + '//' + location.host + '#/' + result._id + '/home_record';
    if(g_is_pmss_enhanced)
    {
        url = location.protocol + '//' + location.host + '#/' + result._id + '/tracking';
    }
    window.location = url;

    return result;
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

window.onhashchange = function(e)
{

  if(e.isTrusted)
  {
    var new_url = e.newURL || window.location.href;

    g_ui.url_state = url_monitor.get_url_state(new_url);

	if(g_ui.url_state.path_array.length > 1)
	{

		var post_html_call_back = [];
		document.getElementById('navbar').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");
		document.getElementById('form_content_id').innerHTML = page_render(g_metadata, g_data, g_ui, "g_metadata", "g_data", "", false, post_html_call_back).join("");
		apply_tool_tips();


		var section_list = document.getElementsByTagName("section");
		for(var i = 0; i < section_list.length; i++)
		{
			var section = section_list[i];
			if(section.id == g_ui.url_state.path_array[1] + "_id")
			{
				section.style.display = "block";
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

  }
  else
  {
    // do nothing for now
  }
};
/*
    profile.on_login_call_back = function (){
				if(g_metadata)
				{
					if(!g_data)
					{
							g_data = create_default_object(g_metadata, {});
								g_ui.data_list.push(g_data);
								g_ui.selected_record_id = g_data._id;
								g_ui.selected_record_index = g_ui.data_list.length -1;
					}

					document.getElementById('navbar').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");
					document.getElementById('form_content_id').innerHTML = page_render(g_metadata, g_data, g_ui).join("");
					apply_tool_tips();
				}

    };
	profile.initialize_profile();*/

});

function metadata_changed(p_metadata)
{
	
	g_metadata = p_metadata;

	if(!g_data)
	{
			g_data = create_default_object(g_metadata, {});
		    g_ui.data_list.push(g_data);
        g_ui.selected_record_id = g_data._id;
				g_ui.selected_record_index = g_ui.data_list.length -1;
				g_data.home_record.first_name = "first";
				g_data.home_record.last_name = "last";
	}

	var post_html_call_back = [];
	document.getElementById('navbar').innerHTML = navigation_render(g_metadata, 0, g_ui).join("");
	document.getElementById('form_content_id').innerHTML = page_render(g_metadata, g_data, g_ui, "g_metadata", "default_object", "", false, post_html_call_back).join("");
	apply_tool_tips();
}


function apply_tool_tips()
{
  $('[rel=tooltip]').tooltip();
    //apply_validation();
}