var g_metadata = null;
var g_data = null;
var g_copy_clip_board = null;
var g_delete_value_clip_board = null;
var g_delete_attribute_clip_board = null;
var g_delete_node_clip_board = null;
var g_metadata_summary = [];

var g_ui = { is_collapsed : [] };




$(function ()
{//http://www.w3schools.com/html/html_layout.asp
  'use strict';

  	//profile.initialize_profile();

	  //load_metadata();
});


function create_print_version(p_metadata, p_data, p_section)
{
	g_metadata = p_metadata;
	metadata_summary(g_metadata_summary, p_metadata, "g_metadata", 0, 0);
	document.getElementById('form_content_id').innerHTML = core_summary_render(p_metadata, p_data, "/", g_ui, false, "").join("");

	if(p_section && p_section.toLowerCase() != "all")
	{
		var section_list = document.getElementsByTagName("section");
		for(var i = 0; i < section_list.length; i++)
		{
			var section = section_list[i];
			if(section.id == p_section)
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



function load_metadata()
{
	var metadata_url = location.protocol + '//' + location.host + '/api/metadata';

	$.ajax({
			url: metadata_url
	}).done(function(response) {
			g_metadata = response;
			metadata_summary(g_metadata_summary, g_metadata, "g_metadata", 0, 0);
			g_data = create_default_object(g_metadata, {});
			g_ui.url_state = url_monitor.get_url_state(window.location.href);

			create_print_version();
	});
}



