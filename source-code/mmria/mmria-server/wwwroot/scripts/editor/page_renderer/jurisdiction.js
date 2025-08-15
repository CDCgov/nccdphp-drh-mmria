function user_jurisdiction_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{
    p_result.push("<div class='form-control-outer' id='" + convert_object_path_to_jquery_id(p_object_path) + "'");
    p_result.push(" mpath='" + p_metadata_path + "' >");

    var styleObject = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];
    p_result.push(
        `<label for="${convert_object_path_to_jquery_id(p_object_path)}_control"
            ${styleObject ? `style="${get_style_string(styleObject.prompt.style)}"` : ''}>
            Case Folder
            ${render_data_analyst_dictionary_link
                (
                    p_metadata, 
                    p_dictionary_path
                )}
        </label>`
    );

    p_result.push("<select id='"+convert_object_path_to_jquery_id(p_object_path)+"_control' class='form-control' name='" + p_metadata.name + "' onchange='g_set_data_object_from_path(\"");
    p_result.push(p_object_path);
    p_result.push("\",\"");
    p_result.push(p_metadata_path);
    p_result.push("\",\"");
    p_result.push(p_dictionary_path);
    p_result.push("\",this.value)'  ");

    p_result.push(`${styleObject ? `style="${get_style_string(styleObject.control.style)}"` : ''}`);
    // p_result.push(" style='");
    //   if(styleObject)
    //   {
    //       p_result.push(get_style_string(styleObject.control.style));
    //   }
    // p_result.push("' ");

    let disabled_html = " disabled='true' ";
    
    if(g_data_is_checked_out)
    {
      disabled_html = " ";
    }
    p_result.push(disabled_html);

    p_result.push(">");

    for(var i = 0; i < g_jurisdiction_list.length; i++)
    {
        var child = g_jurisdiction_list[i];
        
        if(p_data == child)
        {
            if(child == "/")
            {
                p_result.push("<option value='");
                p_result.push(child.replace(/'/g, "&#39;"));
                p_result.push("' selected ");
                p_result.push(">");
                p_result.push("Top Folder");
                p_result.push("</option>");
            }
            else
            {
                p_result.push("<option value='");
                p_result.push(child.replace(/'/g, "&#39;"));
                p_result.push("' selected ");
                p_result.push(">");
                p_result.push(child);
                p_result.push("</option>");
            }
            
        }
        else
        {
            if(child == "/")
            {
                p_result.push("<option value='");
                p_result.push(child.replace(/'/g, "&#39;"));
                p_result.push("' ");
                p_result.push(">");
                p_result.push("Top Folder");
                p_result.push("</option>");
            }
            else
            {
                p_result.push("<option value='");
                p_result.push(child.replace(/'/g, "&#39;"));
                p_result.push("' ");
                p_result.push(">");
                p_result.push(child);
                p_result.push("</option>");
            }
        }
    }
    p_result.push("</select></div>");
}
