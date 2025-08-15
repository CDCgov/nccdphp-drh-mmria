function hidden_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{
    
    p_result.push("<input  type='hidden' name='");
    p_result.push(p_metadata.name);
    p_result.push("' value='");
    if(p_data || p_data == 0)
    { 
        if (typeof p_data === 'string' || p_data instanceof String)
        {
            p_result.push(p_data.replace(/'/g, "&apos;"));
        }
        else
        {
            p_result.push(p_data);
        }
    }
    p_result.push("' id='");
    p_result.push(convert_object_path_to_jquery_id(p_object_path));
    p_result.push("'");
    p_result.push(" dpath='");
	p_result.push(p_dictionary_path.substring(1, p_dictionary_path.length));
    p_result.push("'  mpath='");
    p_result.push(p_metadata_path);
    p_result.push("'/>");
    
}