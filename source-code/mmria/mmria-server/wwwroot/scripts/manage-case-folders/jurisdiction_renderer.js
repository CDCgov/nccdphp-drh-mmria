function jurisdiction_render(p_data, p_path, p_nested_level = 0)
{
	var result = [];
    var top_level_indent = 25;
    var indent_level = p_nested_level * top_level_indent;

    if(p_path == null)
    {
        p_path = "";
    }
	if( p_data._id)
	{ 
        result.push("<form data-nested-level='" + p_nested_level + "' onsubmit='event.preventDefault()' id='add-node-form-" + p_data._id.replace("/", "_") + "'>");
		result.push("<div class='horizontal-control' id='" + p_data._id.replace("/","_") + "'>");
        if(p_data.name == "/")
        {
            result.push("<label class='mr-3'>Top Folder</label>");
        }
        else
        {
            result.push("<label id='" + p_data._id + "-label' class='mr-3'>");
		    result.push(p_data.name.split('/').pop());
            result.push("</label>")
        }
        for (const key in g_managed_jurisdiction_set) 
        {
            if (g_managed_jurisdiction_set.hasOwnProperty(key)) 
            {
                if(p_data.name.indexOf(key) == 0)
                {
                    result.push("<input aria-invalid='false' aria-describedby='add_child_of_" + p_data._id.replace("/", "_") + "' placeholder='Enter node name*' aria-label='Add child for " + p_data._id.replace("/","_") + "' class='form-control ml-auto mr-3 mt-2 col-3' id='add_child_of_" + p_data._id.replace("/","_") + "' /><button style='margin-right: 350px;' style='margin-right: 215px;' value='Add Folder' class='secondary-button' onclick='init_small_loader(function(){ jurisdiction_add_child_click(\"" + p_data._id + "\", document.getElementById(\"add_child_of_" + p_data._id.replace("/","_") + "\").value, \"\") })'>Add Folder</button>");
                    //result.push(`<span class="spinner-container spinner-small ml-1"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>`);
                    $('#add_child_of_' + p_data._id.replace("/", "_").replace(/\//g, '\\/')).on('change', function(e) {
                        if($(e.target).attr('is-invalid') === 'true')
                            set_jurisdiction_add_child_control_valid_state(p_data._id, true);
                    });
                    break;
                }
            }
        }
		result.push("</div>");
        result.push("<div class='horizontal-control errorMessage' style='margin-left: 545px;' id='error_add_child_of_" + p_data._id.replace("/","_") + "'></div>");
	}
	else
	{
        result.push("<form data-nested-level='" + p_nested_level + "'  class='" + p_data.parent_id + "-child' onsubmit='event.preventDefault()' id='add-node-form-" + p_data.id.replace("/", "_") + "'>");
		result.push("<div class='horizontal-control' id='" + p_data.id.replace("/","_") + "'>");
        result.push("<label id='" + p_data.id + "-label' style='padding-left: " + indent_level + "px;' class='mr-3'>");
        if(p_data.children != null && p_data.children.length > 0)
        {
            result.push("<button aria-label='Show " + p_data.name.split('/').pop() + " children' aria-expanded='false' formnovalidate='formnovalidate' aria-hidden='true' hidden onclick='init_small_loader(function(){ set_jurisdiction_show_hide_children_state(\"" + p_data.id + "\", true, false, true)})' id='" + p_data.id + "_show_children' class='btn primary-color p-0 transparent-button'><span class='x20 fill-p cdc-icon-plus'></span></button>");
            result.push("<button aria-label='Hide " + p_data.name.split('/').pop() + " children' aria-expanded='true' formnovalidate='formnovalidate' aria-hidden='false' onclick='init_small_loader(function(){ set_jurisdiction_show_hide_children_state(\"" + p_data.id + "\", false, false, false)})' id='" + p_data.id + "_hide_children' class='btn primary-color p-0 transparent-button'><span class='x20 fill-p cdc-icon-minus'></span></button>");
        }
        // else
        // {
        //     result.push("<button aria-label='Show " + p_data.name.split('/').pop() + " children' aria-expanded='false' formnovalidate='formnovalidate' aria-hidden='true' hidden onclick='init_small_loader(function(){ set_jurisdiction_show_hide_children_state(\"" + p_data.id + "\", true, false, true)})' id='" + p_data.id + "_show_children' class='btn primary-color p-0 transparent-button'><span class='x20 fill-p cdc-icon-plus'></span></button>");
        //     result.push("<button aria-label='Hide " + p_data.name.split('/').pop() + " children' aria-expanded='true' formnovalidate='formnovalidate' aria-hidden='true' hidden onclick='init_small_loader(function(){ set_jurisdiction_show_hide_children_state(\"" + p_data.id + "\", false, false, false)})' id='" + p_data.id + "_hide_children' class='btn primary-color p-0 transparent-button'><span class='x20 fill-p cdc-icon-minus'></span></button>");           
        // }
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
                    result.push("<input aria-invalid='false' aria-describedby='add_child_of_" + p_data.id.replace("/", "_") + "' placeholder='Enter node name*' aria-label='Add child for " + p_data.id.replace("/","_") + "' class='form-control ml-auto mr-3 mt-2 col-3' id='add_child_of_" + p_data.id.replace("/","_") + "' /><button value='Add Folder' class='secondary-button mr-3' onclick='init_small_loader(function(){ jurisdiction_add_child_click(\"" + p_data.id + "\", document.getElementById(\"add_child_of_" + p_data.id.replace("/","_") + "\").value, \"\") })'>Add Folder</button></label><input style='margin-right: 215px;' class='delete-button' type='button' value='Delete Folder' onclick='init_small_loader(function(){ jurisdiction_remove_child_click(\"" + p_data.parent_id + "\", \"" + p_data.id + "\", \"\") })' />");
                    //result.push(`<div style='margin-right: 215px;'><span class="spinner-container spinner-small ml-1"><span class="spinner-body text-primary"><span class="spinner"></span></span></div></span>`);
                    $('#add_child_of_' + p_data.id.replace("/", "_").replace(/\//g, '\\/')).on('change', function(e) {
                        if($(e.target).attr('is-invalid') === 'true')
                            set_jurisdiction_add_child_control_valid_state(p_data.id, true);
                    });
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
		result.push(
            `<br id='case_folder_break'/>
                <div class="d-flex align-items-center">
                    <input class='primary-button mr-3' type='button' value='Save Folder Changes' onclick='init_small_loader(function(){ save_jurisdiction_tree_click(\"\") })' />
                    <span id="case_folder_save_status" role="status" class="mr-2 spinner-container spinner-content">
                        <span class="spinner-body text-primary">
                            <span class="spinner"></span>
                            <span class="sr-only">Saving case folder...</span>
                        </span>
                    </span>
                </div>
        `);
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
	if( p_data._id)
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
            result.push("</label>")
        }
        for (const key in g_managed_jurisdiction_set) 
        {
            if (g_managed_jurisdiction_set.hasOwnProperty(key)) 
            {
                if(p_data.name.indexOf(key) == 0)
                {
                    result.push("<input aria-invalid='false' aria-describedby='add_child_of_" + p_data._id.replace("/", "_") + "' placeholder='Enter node name*' aria-label='Add child for " + p_data._id.replace("/","_") + "' class='form-control ml-auto mr-3 mt-2 col-3' id='add_child_of_" + p_data._id.replace("/","_") + "' /><button style='margin-right: 350px;' style='margin-right: 215px;' value='Add Folder' class='secondary-button' onclick='init_small_loader(function(){ jurisdiction_add_child_click(\"" + p_data._id + "\", document.getElementById(\"add_child_of_" + p_data._id.replace("/","_") + "\").value, \"\") })'>Add Folder</button>");
                    //result.push(`<span class="spinner-container spinner-small ml-1"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>`);
                    $('#add_child_of_' + p_data._id.replace("/", "_").replace(/\//g, '\\/')).on('change', function(e) {
                        if($(e.target).attr('is-invalid') === 'true')
                            set_jurisdiction_add_child_control_valid_state(p_data._id, true);
                    });
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
        if(p_data.children != null && p_data.children.length > 0)
        {
            result.push("<button aria-label='Show " + p_data.name.split('/').pop() + " children' aria-expanded='false' formnovalidate='formnovalidate' aria-hidden='true' hidden onclick='init_small_loader(function(){ set_jurisdiction_show_hide_children_state(\"" + p_data.id + "\", true, false, true)})' id='" + p_data.id + "_show_children' class='btn primary-color p-0 transparent-button'><span class='x20 fill-p cdc-icon-plus'></span></button>");
            result.push("<button aria-label='Hide " + p_data.name.split('/').pop() + " children' aria-expanded='true' formnovalidate='formnovalidate' aria-hidden='false' onclick='init_small_loader(function(){ set_jurisdiction_show_hide_children_state(\"" + p_data.id + "\", false, false, false)})' id='" + p_data.id + "_hide_children' class='btn primary-color p-0 transparent-button'><span class='x20 fill-p cdc-icon-minus'></span></button>");
        }
        // else
        // {
        //     result.push("<button aria-label='Show " + p_data.name.split('/').pop() + " children' aria-expanded='false' formnovalidate='formnovalidate' aria-hidden='true' hidden onclick='init_small_loader(function(){ set_jurisdiction_show_hide_children_state(\"" + p_data.id + "\", true, false, true)})' id='" + p_data.id + "_show_children' class='btn primary-color p-0 transparent-button'><span class='x20 fill-p cdc-icon-plus'></span></button>");
        //     result.push("<button aria-label='Hide " + p_data.name.split('/').pop() + " children' aria-expanded='true' formnovalidate='formnovalidate' aria-hidden='true' hidden onclick='init_small_loader(function(){ set_jurisdiction_show_hide_children_state(\"" + p_data.id + "\", false, false, false)})' id='" + p_data.id + "_hide_children' class='btn primary-color p-0 transparent-button'><span class='x20 fill-p cdc-icon-minus'></span></button>");           
        // }
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
                    result.push("<input aria-invalid='false' aria-describedby='add_child_of_" + p_data.id.replace("/", "_") + "' placeholder='Enter node name*' aria-label='Add child for " + p_data.id.replace("/","_") + "' class='form-control ml-auto mr-3 mt-2 col-3' id='add_child_of_" + p_data.id.replace("/","_") + "' /><button value='Add Folder' class='secondary-button mr-3' onclick='init_small_loader(function(){ jurisdiction_add_child_click(\"" + p_data.id + "\", document.getElementById(\"add_child_of_" + p_data.id.replace("/","_") + "\").value, \"\") })'>Add Folder</button></label><input style='margin-right: 215px;' class='delete-button' type='button' value='Delete Folder' onclick='init_small_loader(function(){ jurisdiction_remove_child_click(\"" + p_data.parent_id + "\", \"" + p_data.id + "\", \"\") })' />");
                    //result.push(`<div style='margin-right: 215px;'><span class="spinner-container spinner-small ml-1"><span class="spinner-body text-primary"><span class="spinner"></span></span></div></span>`);
                    $('#add_child_of_' + p_data.id.replace("/", "_").replace(/\//g, '\\/')).on('change', function(e) {
                        if($(e.target).attr('is-invalid') === 'true')
                            set_jurisdiction_add_child_control_valid_state(p_data._id, true);
                    });
                    break;
                }
            }
        }
        result.push("</div>");
        result.push("<div class='horizontal-control errorMessage' style='margin-left: 545px;' id='error_add_child_of_" + p_data.id.replace("/","_") + "'></div>");

	}
    var new_case_form_element = document.createElement('form');
    new_case_form_element.classList.add(p_data.parent_id + '-child');
    new_case_form_element.id = p_data._id ? ('add-node-form-' + p_data._id.replace("/", "_")) : ('add-node-form-' + p_data.id.replace("/", "_"));
    new_case_form_element.dataset.nestedLevel = p_nested_level;
    new_case_form_element.onsubmit = function(e) {
        e.preventDefault();
    }
    new_case_form_element.innerHTML = result.join("");
	return new_case_form_element;
}

function render_show_hide_buttons(p_data, indent_level)
{
    var result = [];
    result.push("<button aria-label='Show " + p_data.name.split('/').pop() + " children' aria-expanded='false' formnovalidate='formnovalidate' aria-hidden='true' hidden onclick='init_small_loader(function(){ set_jurisdiction_show_hide_children_state(\"" + p_data.id + "\", true, false, true)})' id='" + p_data.id + "_show_children' class='btn primary-color p-0 transparent-button'><span class='x20 fill-p cdc-icon-plus'></span></button>");
    result.push("<button aria-label='Hide " + p_data.name.split('/').pop() + " children' aria-expanded='true' formnovalidate='formnovalidate' aria-hidden='false' onclick='init_small_loader(function(){ set_jurisdiction_show_hide_children_state(\"" + p_data.id + "\", false, false, false)})' id='" + p_data.id + "_hide_children' class='btn primary-color p-0 transparent-button'><span class='x20 fill-p cdc-icon-minus'></span></button>");
    result.push(p_data.name.split("/").pop());
    var new_label_element = document.createElement('label');
    new_label_element.classList.add('mr-3');
    new_label_element.id = p_data.id + '-label';
    new_label_element.setAttribute('style', 'padding-left: ' + indent_level * 25 + 'px;');
    new_label_element.innerHTML = result.join("");
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
