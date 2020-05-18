function datetime_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{
	/*
			if(typeof(p_data) == "string")
			{
				p_data = new Date(p_data);
			}*/
			p_result.push("<div class='datetime' id='");
			p_result.push(convert_object_path_to_jquery_id(p_object_path));
			p_result.push("' ");
			p_result.push(" mpath='");
			p_result.push(p_metadata_path);
			p_result.push("' ");

			p_result.push(">");
			p_result.push("<label ");
			if(p_metadata.description && p_metadata.description.length > 0)
			{
				p_result.push("rel='tooltip'  data-original-title='");
				p_result.push(p_metadata.description.replace(/'/g, "\\'"));
				p_result.push("'");
			}

			
			var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];
			if(style_object)
			{
				p_result.push(" style='");
				p_result.push(get_style_string(style_object.prompt.style));
				p_result.push("'");
			}
			p_result.push(">");

			p_result.push(p_metadata.prompt);

			p_result.push("</label> ");

			page_render_create_input(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path, p_ctx);
			p_post_html_render.push('$("#' + convert_object_path_to_jquery_id(p_object_path) + ' input").datetimepicker({');
				p_post_html_render.push(' format: "Y-MM-D H:mm:ss", ');
				p_post_html_render.push(' defaultDate: "' + p_data + '",');
				p_post_html_render.push(`
					icons: {
						time: "x24 fill-p cdc-icon-clock_01",
						date: "x24 fill-p cdc-icon-calendar_01",
						up: "x24 fill-p cdc-icon-chevron-circle-up",
            down: "x24 fill-p cdc-icon-chevron-circle-down",
            previous: 'x24 fill-p fill-p cdc-icon-chevron-circle-left-light',
            next: 'x24 fill-p cdc-icon-chevron-circle-right-light'
					}`);
			p_post_html_render.push('});');
			
			p_result.push("</div>");	
}
