function html_area_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{

    p_result.push("<div class='textarea' id='");
    p_result.push(convert_object_path_to_jquery_id(p_object_path));
    p_result.push("'");
    p_result.push(" mpath='");
    p_result.push(p_metadata_path);
    p_result.push("' ");
    p_result.push(">");

    p_result.push(`<label for="${convert_object_path_to_jquery_id(p_object_path)}_control" `);
    if(p_metadata.description && p_metadata.description.length > 0)
    {
        p_result.push("rel='tooltip' data-original-title='");
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
    p_result.push("</label>");


    page_render_create_html_area(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path);
    
    const control_style = JSON.parse(style_object.control.style);

    control_style.top = control_style.top + control_style.height + 5;
    const validation_style = `position:absolute;top:${control_style.top}px;left:${control_style.left}px;height:${control_style.height}px;width:${control_style.width}px;font-weight:400;font-size:16px;font-style:normal;color:rgb(0, 0, 0)`;
    
    const html_value = p_data.replace("<html>", "").replace("</html>", "");
    
    p_result.push(`
    <div id="ii-validation" style="${validation_style};border:1px solid black;margin:5px;padding:10px;overflow-y:scroll">
        ${html_value}
    </div>
    
    
    
    
    </div>
    
    
    `);
    
}



function page_render_create_html_area(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
{
	let disabled_html = " disabled ";

	if(g_data_is_checked_out)
	{
		disabled_html = " ";
	}


	p_result.push(`<textarea id="${convert_object_path_to_jquery_id(p_object_path)}_control" ${disabled_html} name='`);
	
    p_result.push(p_metadata.name);
    p_result.push("' ");

    p_result.push(" class='form-control ");
    p_result.push(p_metadata.type.toLowerCase());
    p_result.push("' ");

    if
    (
      p_metadata.is_read_only && 	
      (
        p_metadata.is_read_only == true ||
        p_metadata.is_read_only == "true"
      )
    )
    {
        p_result.push(" readonly='true' ");
    }

    var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];
    
    if(style_object)
    {
        p_result.push(" style='");
        p_result.push(get_style_string(style_object.control.style));
        p_result.push("'");
    }

    var f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_of";
    
    if(path_to_onfocus_map[p_metadata_path])
    {
      page_render_create_event(p_result, "onfocus", p_metadata.onfocus, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
    }

    f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_ochs";
    if(path_to_onchange_map[p_metadata_path])
    {
      page_render_create_event(p_result, "onchange", p_metadata.onchange, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
    }
/*
    p_result.push(" oninput='g_textarea_oninput(\"");
    p_result.push(p_object_path);
    p_result.push("\",\"");
    p_result.push(p_metadata_path);
    p_result.push("\",\"");
    p_result.push(p_dictionary_path);
    p_result.push("\",this.value)' ");
    */
    
    f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_ocl";
    
    if(path_to_onclick_map[p_metadata_path])
    {
      page_render_create_event(p_result, "onclick", p_metadata.onclick, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
    }
    
    page_render_create_onblur_event(p_result, p_metadata, p_metadata_path, p_object_path, p_dictionary_path, p_ctx);

    p_result.push(" >");
    p_result.push(p_data.replace(/&/g, "&amp;"));

	p_result.push("</textarea>");
}


