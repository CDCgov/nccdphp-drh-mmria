function grid_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{
    var is_grid_context = true;

    //p_result.push("<table style='grid-column:1/-1'  id='");
    p_result.push("<fieldset id='");
    
        p_result.push(p_metadata_path);
        p_result.push("' ");
        p_result.push(" mpath='" + p_metadata_path + "'");
        p_result.push(" class='grid2 grid-control' style='");
        var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];
        if(style_object)
        {
            p_result.push(get_only_size_and_position_string(style_object.control.style));
            is_grid_context = style_object;
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
        p_result.push(" - ");
        p_result.push(p_data.length)
        p_result.push(" item(s) </legend>");
        p_result.push("<div class='grid-control-items'>");
        for(var i = 0; i < p_data.length; i++)
        {
            p_result.push("<div class='grid-control-item'>");
            for(var j = 0; j < p_metadata.children.length; j++)
            {
                var child = p_metadata.children[j];

                if(p_data[i][child.name] || p_data[child.name] == 0)
                {
                    // do nothing 
                }
                else
                {
                    p_data[i][child.name] = create_default_object(child, {})[child.name];
                }
                Array.prototype.push.apply
                (
                    p_result,
                    page_render
                    (
                        child,
                        p_data[i][child.name],
                        p_ui, p_metadata_path + ".children[" + j + "]",
                        p_object_path + "[" + i + "]." + child.name,
                        p_dictionary_path + "/" + child.name,
                        is_grid_context,
                        p_post_html_render
                    )
                );

            }

            p_result.push(" item ");
            p_result.push(new Number(i + 1));
            p_result.push(" of ");
            p_result.push(p_data.length);
            p_result.push("</div>");
        }

        p_result.push("</div>");
        
    p_result.push("</fieldset>");
}
