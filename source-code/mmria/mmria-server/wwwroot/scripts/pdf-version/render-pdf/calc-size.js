function print_pdf_calc_size(p_ctx) 
{
    switch(p_ctx.metadata.type.toLocaleLowerCase())
    {
        case "app":
            for(let i = 0; i < p_ctx.metadata.children.length; i++)
            {
                let child = p_ctx.metadata.children[i];
                if(p_ctx.data && child.type.toLocaleLowerCase() == "form")
                {
                    let new_context = get_print_pdf_context(p_ctx.result, p_ctx.post_html_render, child, p_ctx.data[child.name], p_ctx.mmria_path+ "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path + "." + child.name, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
                    print_pdf_calc_size(new_context);
                }
            }
            break;
        case "form":
            if(p_ctx.metadata.cardinality == "1" || p_ctx.metadata.cardinality == "?")
            {
                for(let i = 0; i < p_ctx.metadata.children.length; i++)
                {
                    let child = p_ctx.metadata.children[i];

                    if(p_ctx.data && p_ctx.data[child.name])
                    {
                        let new_context = get_print_pdf_context(p_ctx.result, p_ctx.post_html_render, child, p_ctx.data[child.name], p_ctx.mmria_path+ "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path + "." + child.name, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
                        print_pdf_calc_size(new_context);
                    }
                    
                }
            }
            else // multiform
            {

                for(let row = 0; row < p_ctx.data.length; row++)
                {
                    let row_data = p_ctx.data[row]
                    for(let i = 0; i < p_ctx.metadata.children.length; i++)
                    {
                        let child = p_ctx.metadata.children[i];
    
                        if(row_data)
                        {
                            let new_context = get_print_pdf_context(p_ctx.result, p_ctx.post_html_render, child, row_data[child.name], p_ctx.mmria_path + "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path + "[" + row + "]." + child.name, p_ctx.search_text, p_ctx.is_read_only, row, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
                            new_context.multiform_index = row;
                            print_pdf_calc_size(new_context);
                        }
                        
                    }
                }
            }
            break;
        case "group":
            for(let i = 0; i < p_ctx.metadata.children.length; i++)
            {
                let child = p_ctx.metadata.children[i];
                if(p_ctx.data)
                {
                    let new_context = get_print_pdf_context(p_ctx.result, p_ctx.post_html_render, child, p_ctx.data[child.name], p_ctx.mmria_path+ "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path + "." + child.name, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
                    print_pdf_calc_size(new_context);
                }
            }
            break;
        case "grid":
            for(let i = 0; i < p_ctx.data.length; i++)
            {
                let row_item = p_ctx.data[i];
                for(let j in p_ctx.metadata.children)
                {
                    let child = p_ctx.metadata.children[j];
                    
                    let new_context = get_print_pdf_context(p_ctx.result, p_ctx.post_html_render, child, row_item[child.name], p_ctx.mmria_path + "/" + child.name, p_ctx.metadata_path  + ".children[" +j + "]", p_ctx.object_path + "[" + i + "]." + child.name, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, i, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
                    print_pdf_calc_size(new_context);
                }
            
            }
            break;
        case "string":
        case "number":
        case "time":
            g_size[p_ctx.mmria_path] = { 
                prompt: p_ctx.metadata.prompt.length,
                data: p_ctx.data != null ? p_ctx.data.length : 0 
            };
            
            break;
        case "date":
            
                //renderSearchDateControl(p_ctx);
            
            break;
        case "datetime":
            
                //renderSearchDateTimeControl(p_ctx);
            
            break;
        case "list":
            
                //render_search_text_select_control(p_ctx);
            
            break;
        case "textarea":
            
                //render_search_text_textarea_control(p_ctx);
             
            break;
    }
}