function group_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{

    let vital_group_set = {
        "/home_record/automated_vitals_group":true,
        "/death_certificate/vitals_import_group":true,
        "/birth_certificate_infant_fetal_section/vitals_import_group":true
    };


    p_result.push("<fieldset id='");
    p_result.push(convert_object_path_to_jquery_id(p_object_path));
    p_result.push("_id' class='group' style='");

    //var key = p_dictionary_path.substring(1);

    var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];
    if(style_object)
    {
        p_result.push(get_only_size_and_position_string(style_object.control.style));
    }

    if(g_data.created_by != "vitals-import" && vital_group_set[p_dictionary_path])
    {
        p_result.push(";display:none;");
    }



    p_result.push("' >"); // close opening div
    p_result.push("<legend style='");
    var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];
    if(style_object && style_object.prompt)
    {
        p_result.push(get_only_font_style_string(style_object.prompt.style));
    }
    p_result.push("'>");
    p_result.push(p_metadata.prompt);
    p_result.push("</legend>");



    for(var i = 0; i < p_metadata.children.length; i++)
    {
        var child = p_metadata.children[i];

        if(p_data[child.name] || p_data[child.name] == 0)
        {
            // do nothing 
        }
        else
        {
            p_data[child.name] = create_default_object(child, {})[child.name];
        }

        Array.prototype.push.apply(p_result, page_render(child, p_data[child.name], p_ui, p_metadata_path + '.children[' + i + "]", p_object_path + "." + child.name, p_dictionary_path + "/" + child.name, false, p_post_html_render, p_search_ctx, p_ctx));

    }
    p_result.push("</fieldset>");
    


}