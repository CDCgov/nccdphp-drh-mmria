'use strict';

var g_metadata = null;

var g_filter = 	{
	date_of_death:
	{
		year: [
			'all'
		],
		month: [
			'all'
		],
		day: [
			'all'
		],
	},
	date_range: [
		{
			from: 'all',
			to:	'all'
		}
	],
	case_status: [
		'all'
	],
	case_jurisdiction: [
		'/all'
	],
	selected_form: "",
	search_text: ""
};


$(function ()
{
	
	load_metadata();
});


function load_metadata()
{
	var metadata_url = location.protocol + '//' + location.host + '/api/metadata';

	$.ajax({
			url: metadata_url
	}).done(function(response) {
			g_metadata = response;
			document.getElementById('form_content_id').innerHTML = dictionary_render(g_metadata, "").join("")  + '<br/>';
			get_available_versions()
	});
}


function de_identify_search_text_change(p_value)
{
	g_filter.search_text = p_value;
}


function get_available_versions()
{
  

  $.ajax
  ({

      url: location.protocol + '//' + location.host + '/api/version/list',
  })
  .done(function(response) 
  {

      let available_version = document.getElementById("metadata_version_filter");

      let version_list = response;

      let result = []
      for(let i = 0; i < version_list.length; i++)
      {
        let item = version_list[i];
        let is_selected = "";
        if(i== 0)
        {
            is_selected = "selected=true"
        }
        if(item._id.indexOf("_design/auth") < 0)
        {
            result.push(`<option value="${item._id}" ${is_selected}>${item.name}</option>`)
        }
      }
      available_version.innerHTML = result.join("");
 
      
	});
}