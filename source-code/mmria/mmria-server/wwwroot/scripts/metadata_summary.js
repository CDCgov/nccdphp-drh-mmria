function metadata_summary(p_result, p_metadata, p_path, p_left, p_group_level)
{	
	p_result[p_path] = metadata_summary_new_tuple(p_metadata, p_path, p_left, p_group_level);

	if(p_metadata.children && p_metadata.children.length > 0)
	{		
		var total_core_summary = 0;
		for(var i = 0; i < p_metadata.children.length; i++)
		{
			var child = p_metadata.children[i];
			if(child.type.toLowerCase() == "group")
			{
				metadata_summary(p_result, child, p_path + ".children[" + i + "]", p_left + 1, p_group_level + 1);
			}
			else
			{
				metadata_summary(p_result, child, p_path + ".children[" + i + "]", p_left + 1, p_group_level);
			}


			if(child['is_core_summary'] && child['is_core_summary'] == true)
			{ 
				p_result[p_path].is_core_summary = p_result[p_path].is_core_summary + 1;
			}

			metadata_summary_add_tuples(p_result[p_path], p_result[p_path + ".children[" + i + "]"])
		}
	}




	//return result;
}


function metadata_summary_new_tuple(p_metadata, p_path, p_left, p_group_level)
{

	var result = {
		node: p_metadata,
		path: p_path,
		type: p_metadata.type,
		left: p_left,
		right: 0,
		children:0,
		groups:0,
		group_level: p_group_level,
		dates:0,
		datetimes:0,
		times:0,
		strings:0,
		numbers:0,
		textareas:0,
		labels:0,
		grids:0,
		forms:0,
		lists:0,
		list_values: 0,
		is_core_summary: 0,
		is_required:0,
		is_multiselect:0,
		is_read_only:0,
		default_value:0,
		regex_pattern:0,
		pre_fill:0,
		max_value:0,
		min_value:0,	
		validation:0,
		validation_description:0,
		onfocus:0,
		onchange:0,
		onblur:0,
		onclick:0,
		hidden:0,
		boolean:0,
		buttons:0,
		charts:0
	};

switch(p_metadata.type.toLowerCase())
{
		

		case "group":
			result.groups = 1;
			result.children = p_metadata.children.length;
			break;
		case "date":
			result.dates = 1;
			result.right = result.left + 1;
			break;
		case "datetime":
			result.datetimes = 1;
			result.right = result.left + 1;
			break;
		case "time":
			result.times = 1;
			result.right = result.left + 1;
			break;
		case "string":
			result.strings = 1;
			result.right = result.left + 1;
			break;
		case "number":
			result.numbers = 1;
			result.right = result.left + 1;
			break;
		case "textarea":
        case "html_area":
			result.textareas = 1;
			result.right = result.left + 1;
			break;
		case "label":
			result.labels = 1;
			result.right = result.left + 1;
			break;
		case "grid":
			result.grids = 1;
			result.children = p_metadata.children.length;
			break;
		case "form":
			result.forms = 1;
			result.children = p_metadata.children.length;
			break;
		case "list":
			result.lists = 1;
			result.list_values = p_metadata.values.length;
			result.right = result.left + 1;
			break;
		case "boolean":
			result.boolean = 1;
			break;
		case "hidden":
			result.hidden = 1;
			break;
		case "button":
        case "always_enabled_button":
			result.buttons = 1;
			break;
		case "chart":
			result.charts = 1;
			break;	
        case "jurisdiction":
                break;		
		default:
			if(p_metadata.type != "app")
			{
				console.log("metadata_summary_add: Missing" + p_metadata.type);
			}
			
			break;
		
		

}


	if(p_metadata['is_core_summary'] && p_metadata['is_core_summary'] == true)
	{ 
		result['is_core_summary'] = 1;
	}

	if(p_metadata['is_required'] && p_metadata['is_required'] == true)
	{ 
		result['is_required'] = 1;
	}

	if(p_metadata['is_multiselect'] && p_metadata['is_multiselect'] == true)
	{ 
		result['is_multiselect'] = 1;
	}

	if(p_metadata['is_read_only'] && p_metadata['is_read_only'] == true)
	{ 
		result['is_read_only'] = 1;
	}

	if(p_metadata['default_value'] && p_metadata['default_value'] !='')
	{ 
		result['default_value'] = 1;
	}

	if(p_metadata['regex_pattern'] && p_metadata['regex_pattern'] !='')
	{ 
		result['regex_pattern'] = 1;
	}

	if(p_metadata['pre_fill'] && p_metadata['pre_fill'] !='')
	{ 
		result['pre_fill'] = 1;
	}

	if(p_metadata['max_value'] && p_metadata['max_value'] !='')
	{ 
		result['max_value'] = 1;
	}

	if(p_metadata['min_value'] && p_metadata['min_value'] !='')
	{ 
		result['min_value'] = 1;
	}	

	if(p_metadata['validation'] && p_metadata['validation'] !='')
	{ 
		result['validation'] = 1;
	}

	if(p_metadata['validation_description'] && p_metadata['validation_description'] !='')
	{ 
		result['validation_description'] = 1;
	}

	if(p_metadata['onfocus'] && p_metadata['onfocus'] !='')
	{ 
		result['onfocus'] = 1;
	}

	if(p_metadata['onchange'] && p_metadata['onchange'] !='')
	{ 
		result['onchange'] = 1;
	}


	if(p_metadata['onblur'] && p_metadata['onblur'] !='')
	{ 
		result['onblur'] = 1;
	}

	if(p_metadata['onclick'] && p_metadata['onclick'] !='')
	{ 
		result['onclick'] = 1;
	}

	return result;
}


function metadata_summary_add_tuples(result, p_other)
{

/*
		result.path+= p_other. p_path,
		result.type+= p_other. p_metadata.type,
		result.left+= p_other. p_left,
		result.right+= p_other. p_right,
*/
		result.right+= p_other.right + 1,
		result.children += p_other.children;
		result.groups += p_other.groups;
		/*
		result.dates += p_other.dates;
		result.datetimes += p_other.datetimes;
		result.times += p_other.times;
		result.strings += p_other.strings;
		result.numbers += p_other.numbers;
		result.textareas += p_other.textareas;
		result.labels += p_other.labels;
		result.grids += p_other.grids;
		result.forms += p_other.forms;
		result.lists += p_other.lists;
		result.list_values += p_other.list_values;
		result.is_core_summary += p_other.is_core_summary;
		result.is_required += p_other.is_required;
		result.is_multiselect += p_other.is_multiselect;
		result.is_read_only += p_other.is_read_only;
		result.default_value += p_other.default_value;
		result.regex_pattern += p_other.regex_pattern;
		result.pre_fill += p_other.pre_fill;
		result.max_value += p_other.max_value;
		result.min_value += p_other.min_value;	
		result.validation += p_other.validation;
		result.validation_description += p_other.validation_description;
		result.onfocus += p_other.onfocus;
		result.onchange += p_other.onchange;
		result.onblur += p_other.onblur;
		result.onclick += p_other.onclick;
		*/

}