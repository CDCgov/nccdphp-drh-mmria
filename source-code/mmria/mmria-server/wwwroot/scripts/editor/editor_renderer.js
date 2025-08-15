var colors = [];
colors.push(0xFF8888);
colors.push(0x88FF88);
colors.push(0x8888FF);

function editor_render(p_metadata, p_path, p_ui, p_object_path)
{

	var result = [];


	if(p_metadata.tags != null)
	{
		if
		(
			p_metadata.control_style != null &&
			p_metadata.control_style.toLowerCase() == "editable" &&
			p_metadata.tags.indexOf("EDITABLELIST") == -1
		)
		{
			p_metadata.tags.push("EDITABLELIST");
		}


		if
		(
			p_metadata.is_core_summary != null &&
			p_metadata.is_core_summary == true &&
			p_metadata.tags.indexOf("COREVARIABLE") == -1
		)
		{
			p_metadata.tags.push("COREVARIABLE");
		}


		if
		(
			p_metadata.is_required != null &&
			p_metadata.is_required == true &&
			p_metadata.tags.indexOf("REQUIRED") == -1
		)
		{
			p_metadata.tags.push("REQUIRED");
		}

	}


	switch(p_metadata.type.toLowerCase())
  	{
		case 'grid':
		case 'group':
		case 'form':
			result.push('<li path="');
			result.push(p_path);
			/*
			result.push('" style=" overflow: auto;background-color:#');
			var color_index = new Number(p_path.match(new RegExp("\\d+$"))) % 3;
			result.push(colors[color_index].toString(16));
			result.push(';">');*/
			result.push('">');
			result.push('<input type="button" value="-" onclick="editor_toggle(this, g_ui)"/> ');
			result.push(' <input type="button" value="^" onclick="editor_move_up(this, g_ui)" /> ');
			if(p_metadata.type.toLowerCase()!= 'form')
			{
				result.push('<input type="button" value="c"  onclick="editor_set_copy_clip_board(this,\'' + p_path + '\')" /> ');
			}
			result.push('<input type="button" value="d" onclick="editor_delete_node(this,\'' + p_path + '\')"/> ');
			result.push(p_metadata.name);
			result.push(' ');
			Array.prototype.push.apply(result, render_attribute_add_control(p_path, p_metadata.type));
			result.push(' <input type="button" value="ps" onclick="editor_paste_to_children(\'' + p_path + '\', true)" />');
			result.push(' <input type="button" value="kp" onclick="editor_cut_to_children(\'' + p_path + '\', true)" />');
			result.push(' <br/><ul tag="attribute_list" ');
			if(p_ui.is_collapsed[p_path])
			{
				result.push(' style="display:none">');
			}
			else
			{
			 result.push(' style="display:block">');
			}
			Array.prototype.push.apply(result, attribute_renderer(p_metadata, p_path, p_object_path));
			result.push('<li path="');
			result.push(p_path);
			result.push('/children"><input type="button" value="-" onclick="editor_toggle(this, g_ui)"/> children:');
			
			result.push(' <select path="');
			result.push(p_path);
			result.push('" /> ');
			result.push("<option></option>");
			for(var i = 0; i < valid_types.length; i++)
			{
				var item = valid_types[i];
				if(item.toLowerCase() != 'app')
				{
					result.push('<option>');
					result.push(valid_types[i]);
					result.push('</option>');
				}
			}
			result.push('</select>');

			
			result.push(' <input type="button" value="add" onclick="editor_add_to_children(this, g_ui)" path="');
			result.push(p_path);
			result.push('" />');
			result.push(' <input type="button" value="p" onclick="editor_paste_to_children(\'' + p_path + '\')" /> ');
			result.push(' <input type="button" value="k" onclick="editor_cut_to_children(\'' + p_path + '\')" /> ');
			result.push(p_object_path);
			result.push(' <ul>');

			for(var i = 0; i < p_metadata.children.length; i++)
			{
				var child = p_metadata.children[i];
				Array.prototype.push.apply(result, editor_render(child, p_path + "/children/" + i, p_ui, p_object_path + "/" + child.name));
			}

			result.push('<li><select path="');
			result.push(p_path);
			result.push('" /> ');
			result.push("<option></option>");
			for(var i = 0; i < valid_types.length; i++)
			{
				var item = valid_types[i];
				if(item.toLowerCase() != 'app')
				{
					result.push('<option>');
					result.push(valid_types[i]);
					result.push('</option>');
				}
			}
			result.push('</select>');

			
			result.push(' <input type="button" value="add new item to ');
			result.push(p_metadata.name);
			result.push(' ');
			result.push(p_metadata.type);
			result.push('" onclick="editor_add_to_children(this, g_ui)" path="');
			result.push(p_path);
			result.push('" /> ');
			result.push('<input type="button" value="p" onclick="editor_paste_to_children(\'' + p_path + '\')" /> ');
			result.push('<input type="button" value="k" onclick="editor_cut_to_children(\'' + p_path + '\')" /> ');
			result.push('</li>');
				result.push('</ul></li></ul></li>');
	      break;
    case 'app':
			result.push('<div style="margin-top:10px" path="/" >');
			result.push(p_metadata.name);
			result.push('<ul tag="attribute_list" ');
			if(p_ui.is_collapsed["/"])
			{
				result.push(' style="display:none">');
			}
			else
			{
			 result.push(' style="display:block">');
			}
			Array.prototype.push.apply(result, attribute_renderer(p_metadata, "/", p_object_path));
			// lookup - begin
			if(p_metadata.lookup)
			{
				result.push('<li path="');
				result.push(p_path + "/lookup");
				result.push('"><input type="button" value="-" onclick="editor_toggle(this, g_ui)" /> lookup: <input type="button" value="add list" onclick="editor_add_list(this)"/> ');
				//result.push(p_path + "/children");
				result.push('<ul>');

				for(var i = 0; i < p_metadata.lookup.length; i++)
				{
					var child = p_metadata.lookup[i];
					Array.prototype.push.apply(result, editor_render(child, "/lookup/" + i, p_ui, p_object_path + "/lookup/" + child.name));
				}
				result.push('</ul></li>');
			}
			// lookup - end



			result.push('<li path="');
			result.push(p_path + "/children");
			result.push('"><input type="button" value="-" onclick="editor_toggle(this, g_ui)" /> children: <input type="button" value="add" onclick="editor_add_form(this)"/> ');
			//result.push(p_path + "/children");
			result.push('<ul>');

	       for(var i = 0; i < p_metadata.children.length; i++)
	       {
	         var child = p_metadata.children[i];
					 Array.prototype.push.apply(result, editor_render(child, "/children/" + i, p_ui, p_object_path + "/" + child.name));
			}
			result.push('</ul></li></ul></div>');
       break;
	    case 'label':
		case 'button':
        case 'always_enabled_button':
		case 'boolean':
		case 'date':
		case 'datetime':
		case 'number':
		case 'string':
		case 'time':
		case 'address':
		case 'textarea':
        case 'html_area':
		case 'hidden':
		case 'jurisdiction':

			if(p_metadata.type.toLowerCase() == 'hidden')
			{
				p_metadata.data_type = "string";
			}

			result.push('<li path="');
			result.push(p_path);
			result.push('">');
			result.push('<input type="button" value="-" onclick="editor_toggle(this, g_ui)"/> ');
			result.push('<input type="button" value="^" onclick="editor_move_up(this, g_ui)"/> <input type="button" value="c" onclick="editor_set_copy_clip_board(this,\'' + p_path + '\')" /> ');
			result.push('<input type="button" value="d" onclick="editor_delete_node(this,\'' + p_path + '\')" /> ');
			result.push(p_metadata.name);
			result.push(' ');
			Array.prototype.push.apply(result, render_attribute_add_control(p_path, p_metadata.type));
			result.push(' <input type="button" value="ps" onclick="editor_paste_to_children(\'' + p_path + '\', true)" /> ');
			result.push(' <input type="button" value="kp" onclick="editor_cut_to_children(\'' + p_path + '\', true)" /> ');

			if
			(
				(
					p_metadata.type.toLowerCase() == 'string' ||
					p_metadata.type.toLowerCase() == 'hidden'
				) 
				&&
				(
					p_metadata.max_length == null ||
					p_metadata.max_length == ""
				)
				
			)
			{
				result.push(' <input type="button" value="upgrade to 500 limit" onclick="editor_add_500Limit(\'' + p_path + '\')" style="background-color:aqua;" /> ');
			}


			if
			(

				p_metadata.type.toLowerCase() == 'textarea' &&
				(
					p_metadata.max_length == null ||
					p_metadata.max_length == ""
				)
			)
			{
				result.push(' <input type="button" value="upgrade to 32K limit" onclick="editor_add_30K(\'' + p_path + '\')" style="background-color:cadetblue;" /> ');
			}
			
			result.push(p_object_path);
			result.push(' <ul tag="attribute_list" ');
			if(p_ui.is_collapsed[p_path])
			{
				result.push(' style="display:none">');
			}
			else
			{
			result.push(' style="display:block">');
			}
			Array.prototype.push.apply(result, attribute_renderer(p_metadata, p_path, p_object_path));
			result.push('</ul></li>');


			
			
           break;
	case 'yes_no':
	case 'race':
	case 'list':
		result.push('<li path="');
		result.push(p_path);
		result.push('">');
		result.push(' <input type="button" value="-"  onclick="editor_toggle(this, g_ui)"/> ');
		result.push(' <input type="button" value="^" onclick="editor_move_up(this, g_ui)" /> ' );
		result.push('<input type="button" value="c"  onclick="editor_set_copy_clip_board(this,\'' + p_path + '\')" /> ');
		result.push('<input type="button" value="d" onclick="editor_delete_node(this,\'' + p_path + '\')"/> ');
		result.push(p_metadata.name);
		if(p_metadata.data_type == null)
		{
			if(p_metadata.list_item_data_type != null)
			{
				p_metadata.data_type = p_metadata.list_item_data_type;

				p_metadata.list_item_data_type = null;

			}
			else
			{
				p_metadata.data_type = "number";
			}
		}


		Array.prototype.push.apply(result, render_attribute_add_control(p_path, p_metadata.type));
		result.push(' <input type="button" value="ps" onclick="editor_paste_to_children(\'' + p_path + '\', true)" /> ');
		result.push(' <input type="button" value="kp" onclick="editor_cut_to_children(\'' + p_path + '\', true)" /> ');
		result.push(p_object_path);
		result.push('<br/><ul  tag="attribute_list">');
		Array.prototype.push.apply(result, attribute_renderer(p_metadata, p_path, p_object_path));
		result.push('<li>values:');
		result.push(' <input type="button" value="add" onclick="editor_add_value(\'' + p_path + "/" + "values" + '\')" /> ');

		
		let is_conversion_needed = false;
		for(let list_iterator = 0; list_iterator < p_metadata.values.length; list_iterator++)
		{
			let list_item = p_metadata.values[list_iterator];
			if(list_item.value < 0)
			{
				is_conversion_needed = true;
				break;	
			}
		}

		if(is_conversion_needed)
		{
			result.push(' <input type="button" value="upgrade to numeric/display" onclick="editor_upgrade_numeric_and_display(\'' + p_path + "/" + "values" + '\')" style="    background-color: blanchedalmond;" /> ');
		}

		result.push(' <ul>');


		for(var i = 0; i < p_metadata.values.length; i++)
		{
            var child = p_metadata.values[i];
            
            if(child.is_not_selectable == null)
            {
                child.is_not_selectable = false;
            }

            if
            (
                (
                    p_metadata.is_multiselect!= null &&
                    p_metadata.is_multiselect == true ||
                    p_metadata.name == "race" 
                )  &&
                child.is_mutually_exclusive == null

            )
            {
                child.is_mutually_exclusive == false;
            }



			result.push('<li path="');
			result.push(p_path + "/" + "values/" + i);
			result.push('"> <input type="button" value="^" onclick="editor_move_up(this, g_ui)"/> <input type="button" value="d" onclick="editor_delete_value(this,\'' + p_path + "/" + "values/" + i + '\')" /> value: <input type="text" value="');
			result.push(child.value);
			result.push('" size=');
			result.push(child.value.length + 5);
			result.push('  onBlur="editor_set_value(this, g_ui)" path="');
			result.push(p_path  + "/" + "values/" + i + "/value");
            result.push('" />  parent_value: <input type="text" value="');
			result.push(child.parent_value);
			result.push('" size=');
			result.push(child.value.length + 5);
			result.push('  onBlur="editor_set_value(this, g_ui)" path="');
			result.push(p_path  + "/" + "values/" + i + "/parent_value");
			result.push('" /> display: <input type="text" value="');
			result.push(child.display);
			result.push('" size=');
			if(child.display)
			{
				if(child.display.length == 0)
				{
					result.push(20);
				}
				else
				{
					result.push(child.display.length + 5);
				}
			}
			else
			{
				result.push(20);
			}
			result.push('  onBlur="editor_set_value(this, g_ui)" path="');
			result.push(p_path  + "/" + "values/" + i + "/display");
			result.push('" /> description: <input type="text" value="');
			result.push(child.description);
			result.push('" size=');			
			if(child.description.length == 0)
			{
				result.push(20);
			}
			else
			{
				result.push(child.description.length + 5);
			}
			result.push('  onBlur="editor_set_value(this, g_ui)" path="');
			result.push(p_path  + "/" + "values/" + i + "/description");
            result.push('"  /> ');

            result.push(
                `<input type="checkbox" onChange="editor_set_value(this, g_ui)"  path="${p_path}/values/${i}/is_not_selectable" value="${child.is_not_selectable}" ${child.is_not_selectable ? "checked" : "" }/> is NOT selectable `
            )
            
            if
            (
                
                p_metadata.is_multiselect!= null &&
                p_metadata.is_multiselect == true ||
                p_metadata.name == "race" 
            )
            {
                result.push(
                    `<input type="checkbox" onChange="editor_set_value(this, g_ui)"  path="${p_path}/values/${i}/is_mutually_exclusive" value="${child.is_mutually_exclusive}" ${child.is_mutually_exclusive ? "checked" : "" }/> is mutually exclusive`
                )
            }

			result.push(' </li>');

		}

		result.push('</ul></li></ul></li>');

		break;

		case 'chart':
			result.push('<li path="');
			result.push(p_path);
			result.push('">');
			result.push('<input type="button" value="-" onclick="editor_toggle(this, g_ui)"/> ');
			result.push('<input type="button" value="^" onclick="editor_move_up(this, g_ui)"/> <input type="button" value="c" onclick="editor_set_copy_clip_board(this,\'' + p_path + '\')" /> ');
			result.push('<input type="button" value="d" onclick="editor_delete_node(this,\'' + p_path + '\')" /> ');
			result.push(p_metadata.name);
			result.push(' ');
			Array.prototype.push.apply(result, render_attribute_add_control(p_path, p_metadata.type));
			result.push(' <input type="button" value="ps" onclick="editor_paste_to_children(\'' + p_path + '\', true)" /> ');
			result.push(' <input type="button" value="kp" onclick="editor_cut_to_children(\'' + p_path + '\', true)" /> ');
			result.push(p_object_path);
			result.push(' <ul tag="attribute_list" ');
			if(p_ui.is_collapsed[p_path])
			{
				result.push(' style="display:none">');
			}
			else
			{
			result.push(' style="display:block">');
			}
			Array.prototype.push.apply(result, attribute_renderer(p_metadata, p_path, p_object_path));
			result.push('</ul></li>');

           break;		
     default:
          console.log("editor_render not processed", p_metadata);
       break;
  }

	return result;

}

var valid_types = [
"string",
"hidden",
"number",
"datetime",
"date",
"list",
"app",
"form",
"grid",
"group",
"time",
"textarea",
"html_area",
"boolean",
"label",
"button",
"always_enabled_button",
"address",
"chart",
"jurisdiction"
];


function attribute_renderer(p_metadata, p_path, p_object_path)
{
	var result = [];

	if(p_metadata.type.toLowerCase() != 'app' && p_metadata.tags == null)
	{
		p_metadata.tags = [];
	}

  if(p_path.indexOf("/lookup/") < 0)
  {
    switch(p_metadata.type.toLowerCase())
    {
        case "string":
        case "hidden":
        case "number":
        case "datetime":
        case "date":
        case "list":
        case "time":
        case "textarea":
        case "html_area":     
        case "boolean":
        case "jurisdiction":
            if(p_metadata.sass_export_name == null)
            { 
            p_metadata.sass_export_name = "";
            }

            if(p_metadata.sass_export_name == "" && p_object_path!= null)
            {
            //g_path_to_csv_all[path] = { "file_name": file_name, "field_name": field_name };
            
            let new_name = g_path_to_csv_all[p_object_path.replace("app/", "/")]; 
            if (new_name != null)
            {
                p_metadata.sass_export_name = new_name.field_name;
            }
            }
        break;

      /*
  "app",
      "form",
      "grid",
      "group",
      "label",
      "button",
      "address",
      "chart",
      */
    }
  }

	for(var prop in p_metadata)
	{
		var name_check = prop.toLowerCase();
		switch(name_check)
		{

			case 'tags':
					result.push('<li>tags: <input type="text" value="');
					result.push(p_metadata[prop].join(' '));
					result.push('" onBlur="editor_set_value(this, g_ui)" path="');
					result.push(p_path + "/" + prop);
					result.push('" /> </li>');
        break;
        case 'sass_export_name':
					result.push('<li>sass_export_name: <input type="text" value="');
					result.push(p_metadata[prop]);
					result.push('" onBlur="editor_set_value(this, g_ui)" path="');
					result.push(p_path + "/" + prop);
					result.push('" /> </li>');
				break;        
			case 'children':
			case 'values':

				break;
			
			case 'data_type':
					result.push('<li>data_type: ');
					result.push('<select onChange="editor_set_value(this, g_ui)" path="');
					result.push(p_path + "/" + prop);
					result.push('" /> ');

					result.push('<option>select item datatype</option>');
	

					if
					(
						p_metadata[prop].toLowerCase() == "number"
					)
					{
						result.push('<option selected>number</option>');
					}
					else
					{
						result.push('<option>number</option>');
					}

					if
					(
						p_metadata[prop].toLowerCase() == "string"
					)
					{
						result.push('<option selected>string</option>');
					}
					else
					{
						result.push('<option>string</option>');
					}
				
					result.push('</select>');
				break;
			case 'type':
				if(p_metadata.type.toLowerCase() == "app")
				{
					result.push('<li>')
					result.push(prop);
					result.push(' : ');
					result.push(p_metadata[prop]);
					result.push('</li>');
				}
				else
				{
			
					result.push('<li>type: ');
					if
                    (
						p_metadata[prop].toLowerCase() == "app" ||
						p_metadata[prop].toLowerCase() == "list" ||
						p_metadata[prop].toLowerCase() == "group" ||
						p_metadata[prop].toLowerCase() == "form" ||
						p_metadata[prop].toLowerCase() == "chart" 
					)
					{
						result.push(p_metadata[prop]);
					}
					else
					{
						result.push('<select onChange="editor_set_value(this, g_ui)" path="');
						result.push(p_path + "/" + prop);
						result.push('" /> ');
						for(var i = 0; i < valid_types.length; i++)
						{
							var item = valid_types[i];
							switch(item.toLowerCase())
							{
							
								case 'app':
								case 'list':
								case 'group':
								case 'form':
									break;
								default:
									if
                                    (
                                        p_metadata[prop] && 
                                        item.toLowerCase() == p_metadata[prop].toLowerCase()
                                    )
									{
										result.push('<option selected>');
									}
									else
									{
										result.push('<option>');
									}
									result.push(valid_types[i]);
									result.push('</option>');
									break;
							}
						}
						result.push('</select>');
					}
				}
			break;
			case "name":
			case "prompt":
				if(p_metadata.type.toLowerCase() == "app")
				{
					result.push('<li>')
					result.push(prop);
					result.push(' : ');
					result.push(p_metadata[prop]);
					result.push('</li>');
				}
				else if(p_metadata.type.toLowerCase() == "label" && prop == "prompt")
				{
					result.push('<li>')
					result.push(prop);
					result.push('<br/><textarea rows=3 cols=35');
					result.push(' onBlur="editor_set_value(this, g_ui)" path="');
					result.push(p_path + "/" + prop);
					result.push('" /> ');
					result.push(p_metadata[prop]);
					result.push('</textarea>');
					//result.push(p_path + "/" + prop);
					result.push('</li>');
				}
				else
				{
					result.push('<li>')
					result.push(prop);
					result.push(' : <input type="text" value="');
					result.push(p_metadata[prop]);
					result.push('" size=');
					if(p_metadata[prop])
					{
						result.push((p_metadata[prop].length)?  p_metadata[prop].length + 5: 5);
					}
					else
					{
						result.push(15);
					}

					result.push(' onBlur="editor_set_value(this, g_ui)" path="');
					result.push(p_path + "/" + prop);
					result.push('" /> ');
					//result.push(p_path + "/" + prop);
					result.push('</li>');

				}
			
				break;
			case "cardinality":
				if(p_metadata.type.toLowerCase() == "app")
				{
					result.push('<li>')
					result.push(prop);
					result.push(' : ');
					result.push(p_metadata[prop]);
					result.push('</li>');
				}
				else
				{
					result.push('<li>')
					result.push(prop);
					result.push(' : ');
					Array.prototype.push.apply(result, render_cardinality_control(p_path + "/" + prop, p_metadata[prop]));
					result.push(" ");
					//result.push(p_path + "/" + prop);
					result.push('</li>');
				}
				break;
			case "path_reference":

					result.push('<li>')
					result.push(prop);
					result.push(' : ');
					Array.prototype.push.apply(result, render_path_reference_control(p_path + "/" + prop, p_metadata[prop]));
					result.push(" ");
					//result.push(p_path + "/" + prop);
					result.push('</li>');
				break;
			case "is_core_summary":
			case "is_required":
			case "is_multiselect":
			case "is_read_only":
            case "is_display_field_length":
			case "is_save_value_display_description":
					if(p_metadata[prop])
					{
						result.push('<li>')
						result.push(prop);
						result.push(' : <input type="checkbox" ');
						if(p_metadata[prop]== true)
						{
							result.push(' checked="true" value="true" ');
						}
						else
						{
							result.push(' value="false" ');
						}
						result.push('" onblur="editor_set_value(this, g_ui)" path="');
						result.push(p_path + "/" + prop);
						result.push('" /> ');
						//result.push(p_path + "/" + prop);
						
						result.push(' <input type="button" value="d"  path="' + p_path + "/" + prop + '" onclick="editor_delete_attribute(this,\'' + p_path + "/" + prop + '\')" /> </li>');
					}
				break;

			case "validation":
			case "onblur":
			case "onclick":
			case "onfocus":
			case "onchange":
			case "global":
					result.push('<li>')
					result.push(prop);

					if(p_metadata.type.toLowerCase() == "app")
					{
						result.push(' : <br/> <textarea rows=5 cols=50 onBlur="editor_set_value(this, g_ui)" path="/');
						result.push(prop);
					}
					else
					{
						result.push(' : <input type="button" value="d" path="' + p_path + "/" + prop + '" onclick="editor_delete_attribute(this,\'' + p_path + "/" + prop + '\')" /> <br/> <textarea rows=5 cols=50 onBlur="editor_set_value(this, g_ui)" path="');
						result.push(p_path + "/" + prop);
					}
				
					
					result.push('">');
					
					if(p_metadata[prop] && p_metadata[prop]!="")
					{
						try
						{
							if(p_metadata[prop].comments)
							{
								var temp_ast = escodegen.attachComments(p_metadata[prop], p_metadata[prop].comments, p_metadata[prop].tokens);
								result.push(escodegen.generate(temp_ast, { comment: true }));
								//result.push(escodegen.generate(p_metadata[prop], { comment: true }));
							}
							else
							{
								result.push(escodegen.generate(p_metadata[prop]));
							}
						}
						catch(err)
						{
							result.push(p_metadata[prop]);
						}
						
					}
					else
					{
						result.push(p_metadata[prop]);
					}
					
					result.push('</textarea> </li>');			
				break;
            case "data_summary_report_description":
			case "validation_description":
            case "committee_description":
			case "description":
            case "other_specify_list":
            case "disable_on_selected_item_list":
            case "parent_list":     
			case "grid_template_areas":
			case "pre_fill":
            case "sort_order":
					result.push('<li>')
					result.push(prop);
					result.push(' : <input type="button" value="d" path="' + p_path + "/" + prop + '" onclick="editor_delete_attribute(this,\'' + p_path + "/" + prop + '\')" /> <br/> <textarea rows=5 cols=50 onBlur="editor_set_value(this, g_ui)" path="');
					result.push(p_path + "/" + prop);
					result.push('">');
					result.push(p_metadata[prop]);
					result.push('</textarea> </li>');			
				break;
			case "regex_pattern":
					result.push('<li>')
					result.push(prop);
					result.push(' : <input type="text" value="');
					result.push(p_metadata[prop]);
					result.push('" size=');
					if(p_metadata[prop])
					{
						result.push((p_metadata[prop].length)?  p_metadata[prop].length + 7: 20);
					}
					else
					{
						result.push(20);
					}
					result.push(' onBlur="editor_set_value(this, g_ui)" path="');
					result.push(p_path + "/" + prop);
					result.push('" reg_ex_path="' + p_path + "/" + prop + '" />  <input type="button" value="d"  onclick="editor_delete_attribute(this,\'' + p_path + "/" + prop + '\')" />  </li>');
					result.push(' test pattern: <input type="text" value="" onchange="check_reg_ex(this,\'' + p_path + "/" + prop + '\')"  onblur="check_reg_ex(this,\'' + p_path + "/" + prop + '\')"/>');
					result.push(' syntax example: ^\\d\\d$ 2 digit number <a href="https://duckduckgo.com/?q=javascript+regex&t=hq&ia=web">refrence search</a>');
					
				break;

				case "list_display_size":
					result.push('<li>')
					result.push(prop);
					result.push(' : <input type="number" value="');
					result.push(p_metadata[prop]);
					result.push('" size=');
					if(p_metadata[prop])
					{
						result.push((p_metadata[prop].length)?  p_metadata[prop].length + 5: 5);
					}
					else
					{
						result.push(5);
					}
					result.push(' onBlur="editor_set_value(this, g_ui)" path="');
					result.push(p_path + "/" + prop);
					result.push('" /> ');
					//result.push(p_path + "/" + prop);
					
					result.push(' <input type="button" value="d"  onclick="editor_delete_attribute(this,\'' + p_path + "/" + prop + '\')"/> </li>');
					break;
				case "decimal_precision":
					result.push('<li>')
					result.push(prop);
					result.push(' : <select ');
					result.push(' onchange="editor_set_value(this, g_ui)" path="');
					result.push(p_path + "/" + prop);
					result.push('" > ');

					if(p_metadata[prop] && p_metadata[prop] == "")
					{
						result.push("<option selected></option>");
					}
					else
					{
						result.push("<option></option>");
					}


					for(var i = 0; i < 6; i++)
					{
						result.push("<option ");
						if(p_metadata[prop] && p_metadata[prop] == i)
						{
							result.push("selected");
						}
						result.push("> ");
						
						result.push(i);
						result.push("</option>");
					}
					result.push('</select>');
					//result.push(p_path + "/" + prop);
					
					result.push(' <input type="button" value="d"  onclick="editor_delete_attribute(this,\'' + p_path + "/" + prop + '\')"/> </li>');
					break;

				case "chart":
					result.push('<li>')
					result.push(prop);
					result.push(' : <input type="text" value="');
					result.push(p_metadata[prop]);
					result.push('" size=');
					if(p_metadata[prop])
					{
						result.push((p_metadata[prop].length)?  p_metadata[prop].length + 5: 5);
					}
					else
					{
						result.push(15);
					}
					result.push(' onBlur="editor_set_value(this, g_ui)" path="');
					result.push(p_path + "/" + prop);
					result.push('" /> ');
					//result.push(p_path + "/" + prop);
					
					result.push(' <input type="button" value="d"  onclick="editor_delete_attribute(this,\'' + p_path + "/" + prop + '\')"/> </li>');
				break;					
			default:
				if(p_metadata.type.toLowerCase() == "app")
				{
					if(prop != "lookup")
					{
						result.push('<li>')
						result.push(prop);
						result.push(' : ');
						result.push(p_metadata[prop]);
						result.push('</li>');
					}
				}
				else
				{
					result.push('<li>')
					result.push(prop);
					result.push(' : <input type="text" value="');
					result.push(p_metadata[prop]);
					result.push('" size=');
					if(p_metadata[prop])
					{
						result.push((p_metadata[prop].length)?  p_metadata[prop].length + 5: 5);
					}
					else
					{
						result.push(15);
					}
					result.push(' onBlur="editor_set_value(this, g_ui)" path="');
					result.push(p_path + "/" + prop);
					result.push('" /> ');
					//result.push(p_path + "/" + prop);
					
					result.push(' <input type="button" value="d"  onclick="editor_delete_attribute(this,\'' + p_path + "/" + prop + '\')"/> </li>');

				}


				break;
		}
	}
	return result;
}


function render_cardinality_control(p_path, p_value)
{
	var result = [];
	
	result.push('<select onChange="editor_set_value(this, g_ui)" path="');
	result.push(p_path);
	result.push('">');
	if(p_value=="?")
	{
		result.push('<option selected>?</option>');
	}
	else
	{
		result.push('<option>?</option>');
	}
	
	if(p_value=="1")
	{
		result.push('<option selected>1</option>');
	}
	else
	{
		result.push('<option>1</option>');
	}
	
	if(p_value=="*")
	{
		result.push('<option selected>*</option>');
	}
	else
	{
		result.push('<option>*</option>');
	}
	
	if(p_value=="+")
	{
		result.push('<option selected>+</option>');
	}
	else
	{
		result.push('<option>+</option>');
	}
	
	result.push('</select>');

	
	return result;
}



function render_path_reference_control(p_path, p_value)
{
	var result = [];
	
	result.push('<select onChange="editor_set_value(this, g_ui)" path="');
	result.push(p_path);
	result.push('">');
	result.push('<option></option>');
	for(var i = 0; i < g_metadata.lookup.length; i++)
	{
		var lookup_path = "lookup/" + g_metadata.lookup[i].name;


		if(p_value == lookup_path)
		{
			result.push('<option selected>');
		}
		else
		{
			result.push('<option>');
		}
		result.push(lookup_path);
		result.push('</option>');

	}
	
	result.push('</select>');

	
	return result;
}


function render_attribute_add_control(p_path, node_type)
{
	var result = [];
	var is_collection_node = false;
	var is_list = false;
	var is_range = false;
	var is_pre_fillable = false;

	switch(node_type.toLowerCase())
	{
		case "app":
		case "form":
		case "group":
		case "grid":
		case 'lookup':
			is_collection_node = true;
			break;
		case "list":
			is_list = true;
			break;
		case "string":
		case "hidden":
		case "number":
		case "date":
		case "datetime":
		case "time":
		case "jurisdiction":
			is_range = true;
			break;
		case "address":
		case "textarea":
        case "html_area":
			is_pre_fillable = true;
			break;
		default:

			break;

	}

	result.push(' <select path="');
	result.push(p_path);
	result.push('">');
	result.push('<option></option>');
    result.push('<option>description</option>');
    result.push('<option>data_summary_report_description</option>')
    result.push('<option>committee_description</option>');
    result.push('<option>other_specify_list</option>');
    result.push('<option>disable_on_selected_item_list</option>')
    result.push('<option>parent_list</option>');
    result.push('<option>is_hidden</option>');

	if(node_type.toLowerCase()== "chart")
	{
		result.push('<option>x_start</option>');
		result.push('<option>control_style</option>');
		result.push('</select>');
		result.push(' <input type="button" value="add optional attribute" onclick="editor_add_to_attributes(this, g_ui)" path="');
		result.push(p_path);
		result.push('" /> ');
		return result;
	}

	if(!is_collection_node)
	{
		result.push('<option>is_core_summary</option>');
		result.push('<option>is_required</option>');
		result.push('<option>is_read_only</option>');
        result.push('<option>is_display_field_length</option>')
		
		if(node_type.toLowerCase()== "number")
		{
			result.push('<option>decimal_precision</option>');
		}

		if
		(
			node_type.toLowerCase()== "string" ||
			node_type.toLowerCase()== "textarea" ||
            node_type.toLowerCase() == "html_area"
		)
		{
			result.push('<option>max_length</option>');
		}

		if(is_list)
		{
			result.push('<option>is_multiselect</option>');
			result.push('<option>list_display_size</option>');
			result.push('<option>is_save_value_display_description</option>');
		}
		
		result.push('<option>default_value</option>');
		result.push('<option>regex_pattern</option>');
	}
	else if(node_type.toLowerCase()== "grid" || node_type.toLowerCase()== "group")
	{
		result.push('<option>is_core_summary</option>');
		result.push('<option>is_read_only</option>');
	}


    if(is_collection_node)
	{
        result.push('<option>sort_order</option>');
	}


	if(node_type.toLowerCase()== "app")
	{
		result.push('<option>global</option>');
	}



	result.push('<option>validation</option>');
	result.push('<option>validation_description</option>');
	result.push('<option>onfocus</option>');
	result.push('<option>onchange</option>');
	result.push('<option>onblur</option>');
	result.push('<option>onclick</option>');
	result.push('<option>mirror_reference</option>');
	result.push('<option>pre_populate_reference</option>');

	if(!is_collection_node)
	{
		if(is_pre_fillable)
		{
			result.push('<option>pre_fill</option>');
		}
		
		if(is_range)
		{
			result.push('<option>max_value</option>');
			result.push('<option>min_value</option>');
		}

		if(is_list)
		{
			result.push('<option>control_style</option>');
			result.push('<option>path_reference</option>');
        }
        

        
	}
	result.push('</select>');
	
	result.push(' <input type="button" value="add optional attribute" onclick="editor_add_to_attributes(this, g_ui)" path="');
	result.push(p_path);
	result.push('" /> ');
	
	return result;
}

function editor_set_value(e, p_ui)
{
	var path = e.attributes['path'].value;
	var path_array = path.split('/');
	var attribute_name = path_array[path_array.length - 1];
	var item_path = get_eval_string(path);

	switch(attribute_name.toLowerCase())
	{

		case 'tags':
			let tag_array = e.value.split(' ');
			let new_value = [];

			for(let i = 0; i < tag_array.length; i++)
			{
				if(tag_array[i])
				{
					new_value[i] = '"' + tag_array[i].trim().replace(/"/g, '\\"').replace(/\n/g, "\\n").replace(/,/g,"").toUpperCase() + '"';
				}
			}

			eval(item_path + ' = [' + new_value.join(', ') + '] ');
			window.dispatchEvent(metadata_changed_event);
		break;
		case 'name':
			if(eval(item_path) != e.value)
			{
				if(e.value)
				{
					var test = e.value.trim().match(/^[a-zA-z][a-zA-z0-9_]*$/);
					if(test)
					{
						//var item = eval(item_path);
						eval(item_path + ' = "' + e.value.trim() + '"');
						//var after = eval(item_path);
						window.dispatchEvent(metadata_changed_event);
						e.style.color = "black";
					}
					else
					{
						e.style.color = "red";
					}
				}
				else
				{
					//var item = eval(item_path);
					//eval(item_path + ' = "' + e.value + '"');
					//e.dispatchEvent(metadata_changed_event);
					//var after = eval(item_path);
					e.style.color = "red";
				}
			}
			else if (e.style.color != "black")
			{
				e.style.color = "black"
			}
				
			
			break;
		case "validation":
		case "onblur":
		case "onclick":
		case "onfocus":
		case "onchange":
		case "global":
			try
			{
				var valid_code = esprima.parse(e.value, { comment: true, tokens: true, range: true, loc: true });
				var object_array = convert_to_indexed_path(item_path);
				var node_to_update = eval(object_array[0]);
				var attribute_text = object_array[1];
				node_to_update[attribute_text] = valid_code;
				e.style.color = "black"
					
			}
			catch(err)
			{
				e.style.color = "red";
				console.log("set from esprima code: " ,err);
			}
			break;
		case "is_core_summary":
		case "is_required":
		case "is_multiselect":
		case "is_read_only":
        case "is_not_selectable":
        case "is_mutually_exclusive":
		case "is_save_value_display_description":
        case "is_display_field_length":
			eval(item_path + ' = !' + e.value);
			window.dispatchEvent(metadata_changed_event);
			break;
		case "regex_pattern":
			try
			{
				if(e.value && e.value!='')
				{
					var reg_ex = new RegExp(e.value.trim());
					eval(item_path + ' ="' + e.value.replace(/\\/g, '\\\\').replace(/"/g, '\\"') + '"');
				}
				else
				{
					eval(item_path + ' = ""');
				}
				e.style.color = "black"
			}
			catch(err)
			{
				e.style.color = "red";
				console.log("invalid regex_pattern: " ,e.value);
			}
			
			break;
		default:
			eval(item_path + ' = "' + e.value.trim().replace(/"/g, '\\"').replace(/\n/g, "\\n") + '"');
			window.dispatchEvent(metadata_changed_event);
			//var after = eval(item_path);
			break;
	}
}


function editor_paste_to_children(p_ui, p_is_index_paste)
{
	if(g_copy_clip_board)
	{
		if(p_is_index_paste)
		{
			var path_array = p_ui.split('/');
			var target_index = path_array[path_array.length -1];

			if(path_array[1] != "lookup")
			{
				path_array.splice(path_array.length -2, 2);
			}
			else
			{
				path_array.splice(path_array.length -1, 1);
			}

			var collection_path = path_array.join('/');


			var item_path = get_eval_string(collection_path);	

			var clone_path = get_eval_string(g_copy_clip_board);
			
			var clone = editor_clone(eval(clone_path));
			
			var paste_target = eval(item_path);

			if(paste_target.children)
			{
				for(var i = 0; i < paste_target.children.length; i++)
				{
					if(clone.name == paste_target.children[i].name)
					{
						clone.name = "new_clone_name_" + paste_target.children.length;
						break;
					}
				}

				paste_target.children.splice(target_index, 0, clone);
			}
			else
			{
				for(var i = 0; i < paste_target.length; i++)
				{
					if(clone.name == paste_target[i].name)
					{
						clone.name = "new_clone_name_" + paste_target.length;
						break;
					}
				}

				paste_target.splice(target_index, 0, clone);
			}
		
		
			if(collection_path == "" || collection_path == "/lookup")
			{
	
				if(collection_path=="/lookup")
				{
					paste_target = g_metadata;
				}
				var node = editor_render(paste_target, "/", g_ui);
				
				var node_to_render = document.querySelector("div[path='/']");
				node_to_render.innerHTML = node.join("");
				window.dispatchEvent(metadata_changed_event);

			}
			else
			{
				var node = editor_render(paste_target, collection_path, g_ui);
				
				var node_to_render = document.querySelector("li[path='" + collection_path + "']");
				node_to_render.innerHTML = node.join("");
				window.dispatchEvent(metadata_changed_event);
			}


			
		}
		else
		{
			var path_array = p_ui.split('/');
			var attribute_name = p_ui[path_array.length - 1];
			var item_path = get_eval_string(p_ui);	

			var clone_path = get_eval_string(g_copy_clip_board);
			
			var clone = editor_clone(eval(clone_path));
			

			var paste_target = eval(item_path);

			for(var i = 0; i < paste_target.children.length; i++)
			{
				if(clone.name == paste_target.children[i].name)
				{
					clone.name = "new_clone_name_" + paste_target.children.length;
					break;
				}
			}

			paste_target.children.push(clone);

			var node = editor_render(paste_target, p_ui, g_ui);
			
			var node_to_render = document.querySelector("li[path='" + p_ui + "']");
			node_to_render.innerHTML = node.join("");
			window.dispatchEvent(metadata_changed_event);
		}
		


	}
}


function editor_cut_to_children(p_ui, p_is_index_paste)
{
	if(g_copy_clip_board)
	{
		if(p_is_index_paste)
		{
			var path_array = p_ui.split('/');
			var target_index = path_array[path_array.length -1];

			if(path_array[1] != "lookup")
			{
				path_array.splice(path_array.length -2, 2);
			}
			else
			{
				path_array.splice(path_array.length -1, 1);
			}

			var collection_path = path_array.join('/');

			var item_path = get_eval_string(collection_path);	

			var clone_path = get_eval_string(g_copy_clip_board);
			
			var clone = editor_clone(eval(clone_path));
			
			var paste_target = eval(item_path);

			if(paste_target.children)
			{
				for(var i = 0; i < paste_target.children.length; i++)
				{
					if(clone.name == paste_target.children[i].name)
					{
						clone.name = "new_clone_name_" + paste_target.children.length;
						break;
					}
				}

				paste_target.children.splice(target_index, 0, clone);
			}
			else
			{
				for(var i = 0; i < paste_target.length; i++)
				{
					if(clone.name == paste_target[i].name)
					{
						clone.name = "new_clone_name_" + paste_target.length;
						break;
					}
				}

				paste_target.splice(target_index, 0, clone);
			}

			if(collection_path == "" || collection_path == "/lookup")
			{
	
				if(collection_path=="/lookup")
				{
					paste_target = g_metadata;
				}
	
				var node = editor_render(paste_target, "/", g_ui);
				
				var node_to_render = document.querySelector("div[path='/']");
				node_to_render.innerHTML = node.join("");
				window.dispatchEvent(metadata_changed_event);

			}
			else
			{
				var node = editor_render(paste_target, collection_path, g_ui);
				
				var node_to_render = document.querySelector("li[path='" + collection_path + "']");
				node_to_render.innerHTML = node.join("");
				window.dispatchEvent(metadata_changed_event);
			}



			if(g_copy_clip_board.indexOf(collection_path) == 0)
			{
				g_delete_node_clip_board = g_delete_node_clip_board = collection_path + g_copy_clip_board.replace(collection_path,"").replace(/(\d+)/, function(x) { return new Number(x) + 1; });
				g_copy_clip_board = null;
				editor_delete_node(null, g_delete_node_clip_board);
			}
			else
			{
				g_delete_node_clip_board = g_copy_clip_board;
				g_copy_clip_board = null;
				editor_delete_node(null, g_delete_node_clip_board);
			}

			
		}
		else
		{
			var path_array = p_ui.split('/');
			var attribute_name = p_ui[path_array.length - 1];
			var item_path = get_eval_string(p_ui);	

			var clone_path = get_eval_string(g_copy_clip_board);
			
			var clone = editor_clone(eval(clone_path));
			

			var paste_target = eval(item_path);

			for(var i = 0; i < paste_target.children.length; i++)
			{
				if(clone.name == paste_target.children[i].name)
				{
					clone.name = "new_clone_name_" + paste_target.children.length;
					break;
				}
			}

			paste_target.children.push(clone);

			var node = editor_render(paste_target, p_ui, g_ui);
			
			var node_to_render = document.querySelector("li[path='" + p_ui + "']");
			node_to_render.innerHTML = node.join("");
			window.dispatchEvent(metadata_changed_event);

			if(g_copy_clip_board.indexOf(collection_path) == 0)
			{
				g_delete_node_clip_board = collection_path + g_copy_clip_board.replace(collection_path,"").replace(/(\d+)/, function(x) { return new Number(x) + 1; });
				g_copy_clip_board = null;
				editor_delete_node(null, g_delete_node_clip_board);
			}
			else
			{
				g_delete_node_clip_board = g_copy_clip_board;
				g_copy_clip_board = null;
				editor_delete_node(null, g_delete_node_clip_board);
			}
		}
		


	}
}


function editor_clone(obj) 
{
    if (null == obj || "object" != typeof obj) return obj;
	
    var copy = obj.constructor();
    for (var attr in obj) 
	{
        if (obj.hasOwnProperty(attr)) 
		{
			if(attr == "children")
			{
				copy[attr] = [];
				for(var i = 0; i < obj[attr].length; i++)
				{
					copy[attr].push(editor_clone(obj[attr][i]));
				}
			}
			else if(attr == "values")
			{
				copy[attr] = [];
				for(var i = 0; i < obj[attr].length; i++)
				{
					copy[attr].push(editor_clone(obj[attr][i]));
				}
			}
			else
			{
				copy[attr] = obj[attr];
			}
			
		}
    }
    return copy;
}

function editor_add_to_children(e, p_ui)
{
	var element_list = document.querySelectorAll('select[path="' + e.attributes['path'].value + '"]');
	var element_value = null;
	for(var  i = 0; i < element_list.length; i++)
	{
		var el = element_list[i];
		if(el.value)
		{
			element_value = el.value;
			break;
		}
	}

	if(element_value)
	{

		var parent_path = e.attributes['path'].value;
		var parent_eval_path = get_eval_string(parent_path); 
		var item_path =  parent_eval_path + ".children";

		switch(element_value.toLowerCase())
		{
			//"app":
			//"form":
			case "string":
			case "number":
			case "date":
			case "datetime":
			case "time":
			case "address":
			case "textarea":
            case "html_area":
			case "boolean":
			case "label":
			case "button":
            case "always_enabled_button":
					eval(item_path).push(md.create_value("new_" + element_value, "new " + element_value + " prompt", element_value));

					var node = editor_render(eval(parent_eval_path), parent_path, g_ui);

					var node_to_render = document.querySelector("li[path='" + parent_path + "']");
					node_to_render.innerHTML = node.join("");
					window.dispatchEvent(metadata_changed_event);

					break;
			case "chart":
					eval(item_path).push(md.create_chart("new_" + element_value, "new " + element_value));
					var node = editor_render(eval(parent_eval_path), parent_path, g_ui);

					var node_to_render = document.querySelector("li[path='" + parent_path + "']");
					node_to_render.innerHTML = node.join("");					
					window.dispatchEvent(metadata_changed_event);
					break;
			
			case "list":
					eval(item_path).push(md.create_value_list("new_" + element_value, "new " + element_value, element_value, "list"));
					var node = editor_render(eval(parent_eval_path), parent_path, g_ui);

					var node_to_render = document.querySelector("li[path='" + parent_path + "']");
					node_to_render.innerHTML = node.join("");					
					window.dispatchEvent(metadata_changed_event);
					break;
			case "grid":					
					eval(item_path).push(md.create_grid("new_" + element_value, "new " + element_value));
					var node = editor_render(eval(parent_eval_path), parent_path, g_ui);

					var node_to_render = document.querySelector("li[path='" + parent_path + "']");
					node_to_render.innerHTML = node.join("");		
					window.dispatchEvent(metadata_changed_event);			
					break;
			case "lookup":
					eval(item_path).push(md.create_group("new_" + element_value, "new " + element_value, element_value));
					var node = editor_render(eval(parent_eval_path), parent_path, g_ui);

					var node_to_render = document.querySelector("li[path='" + parent_path + "']");
					node_to_render.innerHTML = node.join("");		
					window.dispatchEvent(metadata_changed_event);			
					break;	
			case "group":
					eval(item_path).push(md.create_group("new_" + element_value, "new " + element_value, element_value));
					var node = editor_render(eval(parent_eval_path), parent_path, g_ui);

					var node_to_render = document.querySelector("li[path='" + parent_path + "']");
					node_to_render.innerHTML = node.join("");		
					window.dispatchEvent(metadata_changed_event);			
					break;					
			case "form":
					eval(item_path).push(md.create_form("new_form", "new form","?"));
					var node = editor_render(eval(parent_eval_path), parent_path, g_ui);

					var node_to_render = document.querySelector("li[path='" + parent_path + "']");
					node_to_render.innerHTML = node.join("");		
					window.dispatchEvent(metadata_changed_event);		
				break;
			default:
				console.log("e.value, path", element_value, e.attributes['path'].value);
				break;
			
		}
	}
	
}

function editor_add_to_attributes(e, p_ui)
{
	var element = document.querySelector('select[path="' + e.attributes['path'].value + '"]');
	if(element.value)
	{
		var attribute = element.value.toLowerCase();
		switch(attribute)
		{
			case "is_core_summary":
			case "is_required":
			case "is_read_only":
			case "is_multiselect":
			case "is_save_value_display_description":
            case "is_hidden":
            case "is_display_field_length":
				var path = e.attributes['path'].value;
				var item = get_eval_string(path);
					eval(item)[attribute] = true;
					
				var node = editor_render(eval(item), path, g_ui);
	
				var node_to_render = document.querySelector("li[path='" + path + "']");
				node_to_render.innerHTML = node.join("");
				window.dispatchEvent(metadata_changed_event);
				break;
			case "default_value":
			case "description":
            case "data_summary_report_description":
            case "other_specify_list":
            case "disable_on_selected_item_list":
            case "parent_list":
            case "sort_order":
			case "regex_pattern":
			case "validation":
			case "validation_description":
            case "committee_description":
			case "onfocus":
			case "onchange":
			case "onblur":
			case "onclick":
			case "pre_fill":
			case "max_length":
			case "max_value":
			case "min_value":
			case "mirror_reference":
			case "control_style":
			case "path_reference":
			case "pre_populate_reference":
			case "x_start":
			case "grid_template":
			case "grid_template_areas":
			case "grid_gap":
			case "grid_auto_flow":
			case "grid_area":
			case "grid_row":
			case "grid_column":
				var path = e.attributes['path'].value;
				var item = get_eval_string(path);
				eval(item)[attribute] = new String();
					
				var node = editor_render(eval(item), path, g_ui);
	
				var node_to_render = document.querySelector("li[path='" + path + "']");
				node_to_render.innerHTML = node.join("");
			
				window.dispatchEvent(metadata_changed_event);
				break;
			case "list_display_size":
				var path = e.attributes['path'].value;
				var item = get_eval_string(path);
				eval(item)[attribute] = new Number(1);
					
				var node = editor_render(eval(item), path, g_ui);
	
				var node_to_render = document.querySelector("li[path='" + path + "']");
				node_to_render.innerHTML = node.join("");
			
				window.dispatchEvent(metadata_changed_event);
				break;
			case "decimal_precision":
				var path = e.attributes['path'].value;
				var item = get_eval_string(path);
				eval(item)[attribute] = new Number(1);
					
				var node = editor_render(eval(item), path, g_ui);
	
				var node_to_render = document.querySelector("li[path='" + path + "']");
				node_to_render.innerHTML = node.join("");
			
				window.dispatchEvent(metadata_changed_event);
				break;				
			default:
				//console.log("e.value, path", element.value, e.attributes['path'].value);
				break;
			
		}
	}
	
}


function editor_delete_attribute(e, p_path)
{
	if(p_path == g_delete_attribute_clip_board)
	{

		g_delete_attribute_clip_board = null;

		var item = get_eval_string(p_path);
		var index = item.lastIndexOf(".");
		var attribute = item.slice(index + 1, item.length);
		var begin = item.slice(0, index);

		var path_index = p_path.lastIndexOf("/");
		var parent_path = p_path.slice(0, path_index);

		delete eval(begin)[attribute];

		var node = editor_render(eval(begin), parent_path, g_ui);

		var node_to_render = document.querySelector("li[path='" + parent_path + "'], li select[path='" + parent_path + "']");
		node_to_render.innerHTML = node.join("");
		window.dispatchEvent(metadata_changed_event);
	}
	else
	{
		var node_to_render = null;

		if(g_delete_attribute_clip_board)
		{
			node_to_render = document.querySelector("li input[path='" + g_delete_attribute_clip_board + "'], li select[path='" + g_delete_attribute_clip_board + "']").parentElement;

			if(node_to_render)
			{
				node_to_render.style.background = "#FFFFFF";
			}
		}

		node_to_render = document.querySelector("li input[path='" + p_path + "'], li select[path='" + p_path + "']").parentElement;
		g_delete_attribute_clip_board = p_path;
		node_to_render.style.background = "#999999";
	}
}

function editor_add_value(p_path)
{
	var item_path = get_eval_string(p_path);
	eval(item_path).push({ value: "new value", description:"", "display": "" });

	var path_index = p_path.lastIndexOf("/");
	var parent_path = p_path.slice(0, path_index);

	var node = editor_render(eval(get_eval_string(parent_path)), parent_path, g_ui);

	var node_to_render = document.querySelector("li[path='" + parent_path + "']");
	node_to_render.innerHTML = node.join("");
	window.dispatchEvent(metadata_changed_event);

}

function editor_upgrade_numeric_and_display(p_path)
{
	var item_path = get_eval_string(p_path);
	var value_list = eval(item_path);

	let current_index = 0;
	for(let i = 0; i < value_list.length; i++)
	{

		if(value_list[i].value == -9)
		{
			value_list[i].value = 9999;
		}
		else if(value_list[i].value == -8)
		{					
			value_list[i].value = 8888;
		}
		else if(value_list[i].value == -7)
		{
			value_list[i].value = 7777;
		}

	}

	var path_index = p_path.lastIndexOf("/");
	var parent_path = p_path.slice(0, path_index);

	var node = editor_render(eval(get_eval_string(parent_path)), parent_path, g_ui);

	var node_to_render = document.querySelector("li[path='" + parent_path + "']");
	node_to_render.innerHTML = node.join("");
	window.dispatchEvent(metadata_changed_event);
}


function editor_add_30K(p_path)
{
	let item_path = get_eval_string(p_path);
	let item = eval(item_path);

	if(item.max_length == null)
	{
		item.max_length = "31000";
	}
	else if(item.max_length == "")
	{
		item.max_length = "31000";
	}


	let node = editor_render(eval(get_eval_string(p_path)), p_path, g_ui);

	let node_to_render = document.querySelector("li[path='" + p_path + "']");
	node_to_render.innerHTML = node.join("");
	window.dispatchEvent(metadata_changed_event);
}

function editor_add_500Limit(p_path)
{
	let item_path = get_eval_string(p_path);
	let item = eval(item_path);

	if(item.max_length == null)
	{
		item.max_length = "500";
	}
	else if(item.max_length == "")
	{
		item.max_length = "500";
	}

	let node = editor_render(eval(get_eval_string(p_path)), p_path, g_ui);

	let node_to_render = document.querySelector("li[path='" + p_path + "']");
	node_to_render.innerHTML = node.join("");
	window.dispatchEvent(metadata_changed_event);
}

function editor_upgrade_pmss_and_display(p_path)
{
	var item_path = get_eval_string(p_path);
	var value_list = eval(item_path);

	for(let i = 0; i < value_list.length; i++)
	{
		if(value_list[i].value == null || value_list[i].value == "")
		{
			value_list[i].display = "";
			value_list[i].value = -9;
		}
		else
		{
			let split_index = value_list[i].value.indexOf(' ');

			value_list[i].display = value_list[i].value;
			value_list[i].value = value_list[i].value.substring(0, split_index).trim();
			
		}
	}

	var path_index = p_path.lastIndexOf("/");
	var parent_path = p_path.slice(0, path_index);

	var node = editor_render(eval(get_eval_string(parent_path)), parent_path, g_ui);

	var node_to_render = document.querySelector("li[path='" + parent_path + "']");
	node_to_render.innerHTML = node.join("");
	window.dispatchEvent(metadata_changed_event);
}

function editor_delete_value(e, p_path)
{
	if(p_path == g_delete_value_clip_board)
	{

		var item = get_eval_string(p_path);

		var item_index = p_path.match(new RegExp('\\d+$','g'));
		var index = item.lastIndexOf(".");
		var attribute = item.slice(index + 1, item.length);
		var begin = item.slice(0, index);
		//index = begin.lastIndexOf(".");
		//begin = begin.slice(0, index);

		var path_index = p_path.lastIndexOf("/");
		var temp_path = p_path.slice(0, path_index);
		path_index = temp_path.lastIndexOf("/");
		var parent_path = temp_path.slice(0, path_index);
		
		//path_index = parent_path.lastIndexOf("/");
		//parent_path = parent_path.slice(0, path_index);

		eval(begin).values.splice(item_index[0], 1);

		var node = editor_render(eval(begin), parent_path, g_ui);

		var node_to_render = document.querySelector("li[path='" + parent_path + "']");
		node_to_render.innerHTML = node.join("");
		window.dispatchEvent(metadata_changed_event);

		g_delete_value_clip_board = null;
	}
	else
	{
		var node_to_render = null;

		if(g_delete_value_clip_board)
		{
			node_to_render = document.querySelector("li [path='" + g_delete_value_clip_board + "']");

			if(node_to_render)
			{
				node_to_render.style.background = "#FFFFFF";
			}
		}

		node_to_render = document.querySelector("li [path='" + p_path + "']");
		g_delete_value_clip_board = p_path;
		node_to_render.style.background = "#999999";
	}
}


function editor_set_copy_clip_board(e, p_path)
{
		g_copy_clip_board = p_path;
}


function editor_delete_node(e, p_path)
{
	if(p_path == g_delete_node_clip_board)
	{
		g_delete_node_clip_board = null;

		var path_index = p_path.lastIndexOf("/");
		var collection_path = p_path.slice(0, path_index);
		var object_path = get_eval_string(collection_path);
		var index = p_path.match(/\d*$/)[0];

		//delete eval(parent_path)[index];
		eval(object_path).splice(index, 1);

		path_index = collection_path.lastIndexOf("/");
		var parent_path = collection_path.slice(0, path_index);

		var node = editor_render(eval(get_eval_string(parent_path)), parent_path, g_ui);

		var node_to_render = null;

		if(parent_path != "")
		{
			node_to_render = document.querySelector("li[path='" + parent_path + "']");
		}
		else
		{
			node_to_render = document.querySelector("div[path='/']");
		}

		node_to_render.innerHTML = node.join("");
		window.dispatchEvent(metadata_changed_event);
	}
	else
	{
		var node_to_render = null;

		if(g_delete_node_clip_board)
		{
			node_to_render = document.querySelector("li[path='" + g_delete_node_clip_board + "']");
			if(node_to_render)
			{
				node_to_render.style.background = "#FFFFFF";
			}
		}

		node_to_render = document.querySelector("li[path='" + p_path + "']");
		g_delete_node_clip_board = p_path;
		node_to_render.style.background = "#999999";
	}
}

function editor_add_form(e)
{
	var form = md.create_form(			
			'new_form_name',
			'form prompt',
			'?');
	g_metadata.children.push(form);
	var node = editor_render(g_metadata, "", g_ui);
	
	var node_to_render = document.getElementById('form_content_id');
	node_to_render.innerHTML = node.join("");
	window.dispatchEvent(metadata_changed_event);
	
}


function editor_add_list(e)
{
	var list = md.create_value_list(			
			'new_list_name',
			'list prompt',
			'list');
	g_metadata.lookup.push(list);
	var node = editor_render(g_metadata, "", g_ui);
	
	var node_to_render = document.getElementById('form_content_id');
	node_to_render.innerHTML = node.join("");
	window.dispatchEvent(metadata_changed_event);
	
}

function editor_toggle(e, p_ui)
{
	var element = document.querySelector('li[path="' + e.parentElement.attributes['path'].value + '"] ul');

	if(element.style.display=="none")
	{
		element.style.display="block";
		e.value = "-";
		p_ui.is_collapsed[e.parentElement.attributes['path'].value] = false;
	}
	else
	{
			element.style.display="none";
			e.value = "+";
			p_ui.is_collapsed[e.parentElement.attributes['path'].value] = true;
	}
	//console.log('toggle: path', e.parentElement.attributes['path']);
}

function editor_move_up(e, p_ui)
{

	var current_li = e.parentElement;
	var parent = current_li.parentElement;
	var path = current_li.attributes['path'].value;

	var item = get_eval_string(path);


	var node_path = get_eval_string(remove_last_digit_in_path(path));
	var list = eval(node_path);
	var y = path.match(/\d*$/)[0];
	var x = y - 1;

	//swap toggle state
	var temp = p_ui.is_collapsed[node_path + y];
	p_ui.is_collapsed[node_path + y] = p_ui.is_collapsed[node_path + x];
	p_ui.is_collapsed[node_path + x] = temp;
	

	if(x >= 0)
	{
		var b = list[y];
		list[y] = list[x];
		list[x] = b;

		var parent_path = get_parent_path(path);
		var metadata_path = get_eval_string(parent_path);
		var metadata = eval(metadata_path);
		var node = editor_render(metadata, parent_path, p_ui);

		var node_to_render = null;
		if(parent_path == "")
		{
			node_to_render = document.querySelector("div[path='/']");
		}
		else
		{
			node_to_render = document.querySelector("li[path='" + parent_path + "']");

		}
		node_to_render.innerHTML = node.join("");
		window.dispatchEvent(metadata_changed_event);

	}

}

function remove_last_digit_in_path(p_path)
{
    var index = p_path.lastIndexOf("/");
    var result = p_path.substring(0,index)
    return result;
}

function get_eval_string(p_path)
{
	var result = "g_metadata" + p_path.replace(new RegExp('/','gm'),".").replace(new RegExp('\\.(\\d+)\\.','gm'),"[$1].").replace(new RegExp('\\.(\\d+)$','g'),"[$1]");

	return result;

}

function get_parent_path(p_path)
{
	var result = null;
	if(p_path.match(new RegExp('/values/(\\d+)$','g')))
	{
		result = p_path.replace(new RegExp('/values/(\\d+)$','g'),"");
	}
	else
	{

		result = p_path.replace(new RegExp('/children/(\\d+)$','g'),"");
	}

	return result;

}


function convert_to_indexed_path(p_path)
{
	// example g_metadata.children[0].validation
	var result = [];
    var temp = p_path.split(".");
    var last = temp[temp.length-1];
    
    temp.pop();

	result.push(temp.join("."));
	result.push(last);
	return result;

}

function check_reg_ex(p_control, p_path)
{
	var reg_ex_control = document.querySelector("input[reg_ex_path='" + p_path + "']");

	var value = p_control.value;

	var regexp = new RegExp(reg_ex_control.value);
	var matches_array = value.match(regexp);
	if(matches_array)
	{
		if(matches_array.length < 1) 
		{
			p_control.style.background = "#FFCCCC";
		}
		else
		{
			p_control.style.background = "#FFFFFF";
		}
	}
	else 
	{
		p_control.style.background = "#FFCCCC";
	}
	

}