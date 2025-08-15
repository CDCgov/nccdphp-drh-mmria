function grid_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{

    if
    (
        p_metadata.name == "recommendations_of_committee" &&
        (
            p_data == null ||
            p_data.length == 0
        )
    )
    {

    }
    else
    {

        var is_grid_context = true;

        var visibility_html = ';display:block;';
        if(p_metadata.is_hidden == true)
        {
            visibility_html = ';display:none;';
        }

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
            p_result.push(visibility_html);

            p_result.push("' >"); // close opening div

            p_result.push("<legend class='grid-control-legend' style='");
            
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
            p_result.push("<ul class='grid-control-items p-0 list-unstyled'>");
            for(var i = 0; i < p_data.length; i++)
            {
                var ctx = { "grid_index": i };
                if( p_ctx != null && p_ctx.form_index != null)
                {
                    ctx["form_index"] = p_ctx.form_index;
                }

                p_result.push("<li class='grid-control-item mb-0'>");
                    for(var j = 0; j < p_metadata.children.length; j++)
                    {
                        var child = p_metadata.children[j];

                        if(p_data[i] == null)
                        {
                           continue; 
                        }
                        else if(p_data[i][child.name] || p_data[i][child.name] == 0)
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
                                p_post_html_render,
                                p_search_ctx,
                                ctx
                            )
                        );
                    }

                    p_result.push("<div class='grid-control-action-icn row no-gutters'>");
                    if(p_metadata.is_read_only == null || p_metadata.is_read_only != true)
                    {
                        let disable_html = " disabled='disabled' ";

                        if(g_is_data_analyst_mode == null)
                        {
                            if(g_data_is_checked_out)
                            {
                                disable_html = '';
                            }
                            else if(!is_checked_out_expired(g_data) && g_data.last_checked_out_by === g_user_name)
                            {
                                disable_html = '';
                            }
                        }

                        p_result.push("<button type='button' class='grid-control-action-btn mr-1' title='delete' id='delete_");
                            p_result.push(p_object_path.replace(/\./g,"_") + "[" + i + "]");
                            p_result.push("' onclick='g_delete_grid_item(\"");
                            p_result.push(p_object_path + "[" + i + "]");
                            p_result.push("\", \"");
                            p_result.push(p_metadata_path);
                            p_result.push("\", \"");
                            p_result.push(p_dictionary_path); 
                            p_result.push("\", \"");
                            p_result.push(p_metadata.prompt); 
                            p_result.push("\", \"");
                            p_result.push(p_data.length); 
                            p_result.push("\", " + i + ")'");
                            p_result.push(disable_html);
                            p_result.push(">");
                            p_result.push("<span class='x24 fill-p text-secondary cdc-icon-close'></span>");
                            p_result.push("<span class='sr-only'>Close</span>");
                        p_result.push("</button>");
                    }
                        p_result.push("<span>");
                            p_result.push(" item ");
                            p_result.push(new Number(i + 1));
                            p_result.push(" of ");
                            p_result.push(p_data.length);
                        p_result.push("</span>");
                    p_result.push("</div>");
                p_result.push("</li>");
            }
            // p_result.push("<br/>");
            p_result.push("</ul>");
            if(p_metadata.is_read_only == null && p_metadata.is_read_only != true)
            {
                let disable_html = " disabled='disabled' "
                if(g_is_data_analyst_mode == null)
                {
                    if(g_data_is_checked_out)
                    {
                        disable_html = '';
                    }
                    else if(!is_checked_out_expired(g_data) && g_data.last_checked_out_by === g_user_name)
                    {
                        disable_html = '';
                    }
                }

                p_result.push("<button type='button' class='grid-control-btn btn btn-primary d-flex align-items-center' onclick='g_add_grid_item(\"");
                    p_result.push(p_object_path);
                    p_result.push("\", \"");
                    p_result.push(p_metadata_path);
                    p_result.push("\", \"");
                    p_result.push(p_dictionary_path);    
                    p_result.push("\")'");
                    p_result.push(disable_html);
                    p_result.push("><span class='x24 cdc-icon-plus'></span> Add Item");
                p_result.push("</button>");
            }
        p_result.push("</fieldset>");
    }
}
