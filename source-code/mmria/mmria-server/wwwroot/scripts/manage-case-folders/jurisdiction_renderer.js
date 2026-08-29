function render_show_hide_toggle_button_markup(p_node_id, p_folder_name, should_show)
{
    var button_suffix = should_show ? "show_children" : "hide_children";
    var icon_class = should_show ? "cdc-icon-plus" : "cdc-icon-minus";
    var action_name = should_show ? "show-children" : "hide-children";
    var aria_expanded = should_show ? "false" : "true";
    var aria_hidden = should_show ? "true" : "false";
    var hidden_attribute = should_show ? " hidden" : "";

    return "<button type='button' aria-label='" + (should_show ? "Show " : "Hide ") + p_folder_name + " children' aria-expanded='" + aria_expanded + "' formnovalidate='formnovalidate' aria-hidden='" + aria_hidden + "'" + hidden_attribute + " data-folder-action='" + action_name + "' data-node-id='" + p_node_id + "' id='" + p_node_id + "_" + button_suffix + "' class='btn primary-color p-0 transparent-button'><span class='x20 fill-p " + icon_class + "'></span></button>";
}

function render_show_hide_buttons_markup(p_data)
{
    var result = [];

    if(p_data.children != null && p_data.children.length > 0)
    {
        var folder_name = p_data.name.split('/').pop();
        result.push(render_show_hide_toggle_button_markup(p_data.id, folder_name, true));
        result.push(render_show_hide_toggle_button_markup(p_data.id, folder_name, false));
    }

    return result.join("");
}

function render_add_child_input_markup(p_parent_id)
{
    var control_id = p_parent_id.replace("/", "_");

    return "<input data-add-child-parent-id='" + p_parent_id + "' aria-invalid='false' aria-describedby='add_child_of_" + control_id + "' placeholder='Enter node name*' aria-label='Add child for " + control_id + "' class='form-control ml-auto mr-3 mt-2 col-3' id='add_child_of_" + control_id + "' />";
}

function render_add_folder_button_markup(p_parent_id, p_class_name, p_style = "")
{
    var control_id = p_parent_id.replace("/", "_");
    var style_markup = p_style == "" ? "" : " style='" + p_style + "'";

    return "<button type='button' value='Add Folder' class='" + p_class_name + "' data-folder-action='add-folder' data-add-child-parent-id='" + p_parent_id + "' data-input-id='add_child_of_" + control_id + "'" + style_markup + ">Add Folder</button>";
}

function render_delete_folder_button_markup(p_parent_id, p_node_id)
{
    return "<input style='margin-right: 215px;' class='delete-button' type='button' value='Delete Folder' data-folder-action='delete-folder' data-parent-id='" + p_parent_id + "' data-node-id='" + p_node_id + "' />";
}

function render_save_button_markup()
{
    return `<br id='case_folder_break'/>
                <div class="d-flex align-items-center">
                    <input class='primary-button mr-3' type='button' value='Save Folder Changes' data-folder-action='save-tree' />
                    <span id="case_folder_save_status" role="status" class="mr-2 spinner-container spinner-content">
                        <span class="spinner-body text-primary">
                            <span class="spinner"></span>
                            <span class="sr-only">Saving case folder...</span>
                        </span>
                    </span>
                </div>
        `;
}

function jurisdiction_render(p_data, p_path, p_nested_level = 0)
{
	var result = [];
    var top_level_indent = 25;
    var indent_level = p_nested_level * top_level_indent;

    if(p_path == null)
    {
        p_path = "";
    }

	if(p_data._id)
	{ 
        result.push("<form data-nested-level='" + p_nested_level + "' id='add-node-form-" + p_data._id.replace("/", "_") + "'>");
		result.push("<div class='horizontal-control' id='" + p_data._id.replace("/","_") + "'>");
        if(p_data.name == "/")
        {
            result.push("<label class='mr-3'>Top Folder</label>");
        }
        else
        {
            result.push("<label id='" + p_data._id + "-label' class='mr-3'>");
		    result.push(p_data.name.split('/').pop());
            result.push("</label>");
        }
        for (const key in g_managed_jurisdiction_set) 
        {
            if (g_managed_jurisdiction_set.hasOwnProperty(key)) 
            {
                if(p_data.name.indexOf(key) == 0)
                {
                    result.push(render_add_child_input_markup(p_data._id));
                    result.push(render_add_folder_button_markup(p_data._id, "secondary-button", "margin-right: 350px;"));
                    break;
                }
            }
        }
		result.push("</div>");
        result.push("<div class='horizontal-control errorMessage' style='margin-left: 545px;' id='error_add_child_of_" + p_data._id.replace("/","_") + "'></div>");
	}
	else
	{
        result.push("<form data-nested-level='" + p_nested_level + "' class='" + p_data.parent_id + "-child' id='add-node-form-" + p_data.id.replace("/", "_") + "'>");
		result.push("<div class='horizontal-control' id='" + p_data.id.replace("/","_") + "'>");
        result.push("<label id='" + p_data.id + "-label' style='padding-left: " + indent_level + "px;' class='mr-3'>");
        result.push(render_show_hide_buttons_markup(p_data));
		result.push(p_data.name.split('/').pop());
        result.push("</label>");

        let new_path = `${p_path}${p_data.name}`;
        if(p_path == "/")
        {
            new_path = p_data.name;
        }

        for (const key in g_managed_jurisdiction_set) 
        {
            if (g_managed_jurisdiction_set.hasOwnProperty(key)) 
            {
                if(new_path.indexOf(key) == 0)
                {
                    result.push(render_add_child_input_markup(p_data.id));
                    result.push(render_add_folder_button_markup(p_data.id, "secondary-button mr-3"));
                    result.push(render_delete_folder_button_markup(p_data.parent_id, p_data.id));
                    break;
                }
            }
        }
        result.push("</div>");
        result.push("<div class='horizontal-control errorMessage' style='margin-left: 545px;' id='error_add_child_of_" + p_data.id.replace("/","_") + "'></div>");
	}
    result.push("</form>");
    if(p_data.children != null)
    {
        for(var i = 0; i < p_data.children.length; i++)
        {
            var child = p_data.children[i];
            let new_path = `${p_path}${p_data.name}`;
            if(p_path == "")
            {
                new_path = p_data.name;
            }
            Array.prototype.push.apply(result, jurisdiction_render(child, new_path, p_nested_level + 1));			
        }
    }
	
	if(p_data._id)
	{
		result.push(render_save_button_markup());
	}

	return result;
}

function render_new_case_folder(p_data, p_path, p_nested_level)
{
	var result = [];
    var top_level_indent = 25;
    p_nested_level = p_nested_level + 1;
    var indent_level = p_nested_level * top_level_indent;

    if(p_path == null)
    {
        p_path = "";
    }

	if(p_data._id)
	{ 
		result.push("<div class='horizontal-control' id='" + p_data._id.replace("/","_") + "'>");
        if(p_data.name == "/")
        {
            result.push("<label id='" + p_data._id + "-label' class='mr-3'>Top Folder</label>");
        }
        else
        {
            result.push("<label class='mr-3'>");
		    result.push(p_data.name.split('/').pop());
            result.push("</label>");
        }
        for (const key in g_managed_jurisdiction_set) 
        {
            if (g_managed_jurisdiction_set.hasOwnProperty(key)) 
            {
                if(p_data.name.indexOf(key) == 0)
                {
                    result.push(render_add_child_input_markup(p_data._id));
                    result.push(render_add_folder_button_markup(p_data._id, "secondary-button", "margin-right: 350px;"));
                    break;
                }
            }
        }
		result.push("</div>");
        result.push("<div class='horizontal-control errorMessage' style='margin-left: 545px;' id='error_add_child_of_" + p_data._id.replace("/","_") + "'></div>");
	}
	else
	{
		result.push("<div class='horizontal-control' id='" + p_data.id.replace("/","_") + "'>");
        result.push("<label id='" + p_data.id + "-label' style='padding-left: " + indent_level + "px;' class='mr-3'>");
        result.push(render_show_hide_buttons_markup(p_data));
		result.push(p_data.name.split('/').pop());
        result.push("</label>");

        let new_path = `${p_path}${p_data.name}`;
        if(p_path == "/")
        {
            new_path = p_data.name;
        }

        for (const key in g_managed_jurisdiction_set) 
        {
            if (g_managed_jurisdiction_set.hasOwnProperty(key)) 
            {
                if(new_path.indexOf(key) == 0)
                {
                    result.push(render_add_child_input_markup(p_data.id));
                    result.push(render_add_folder_button_markup(p_data.id, "secondary-button mr-3"));
                    result.push(render_delete_folder_button_markup(p_data.parent_id, p_data.id));
                    break;
                }
            }
        }
        result.push("</div>");
        result.push("<div class='horizontal-control errorMessage' style='margin-left: 545px;' id='error_add_child_of_" + p_data.id.replace("/","_") + "'></div>");

	}
    var new_case_form_element = document.createElement('form');
    if(p_data.parent_id != null)
    {
        new_case_form_element.classList.add(p_data.parent_id + '-child');
    }
    new_case_form_element.id = p_data._id ? ('add-node-form-' + p_data._id.replace("/", "_")) : ('add-node-form-' + p_data.id.replace("/", "_"));
    new_case_form_element.dataset.nestedLevel = p_nested_level;
    $mmria.set_sanitized_html(new_case_form_element, result.join(""));
	return new_case_form_element;
}

function render_show_hide_buttons(p_data, indent_level)
{
    var new_label_element = document.createElement('label');
    new_label_element.classList.add('mr-3');
    new_label_element.id = p_data.id + '-label';
    new_label_element.setAttribute('style', 'padding-left: ' + indent_level * 25 + 'px;');
    $mmria.set_sanitized_html(new_label_element, render_show_hide_buttons_markup(p_data) + p_data.name.split("/").pop());
    return new_label_element;
}

/*
{
	_id: "jurisdiction_tree", 
	_rev: "1-b3e65347756f3cf46116dac1e8d9cec7", 
	name: "/", 
	children:null
	created_by:null
	date_created:"0001-01-01T00:00:00"
	date_last_updated:"0001-01-01T00:00:00"
	last_updated_by:null
	data_type:"jursidiction_tree"
}
*/
