function core_summary_render(p_metadata, p_data,  p_path, p_ui, p_is_core_summary, p_metadata_path)
{
	var is_core_summary = false;

	if(p_is_core_summary)
	{
		is_core_summary = true;
	}


	var result = [];
	switch(p_metadata.type.toLowerCase())
	{ 
		case 'group':

				if(g_metadata_summary[p_metadata_path].is_core_summary > 0 ||
					p_metadata.is_core_summary && p_metadata.is_core_summary == true)
				{						
					result.push('<fieldset>');

					result.push('<legend>')
					result.push(p_metadata.prompt);
					result.push('</legend> ');
					
					
					if(p_metadata.is_core_summary || p_metadata.is_core_summary == true)
					{
						is_core_summary = true;
					}

					if(p_metadata.children)
					{
						for(var i = 0; i < p_metadata.children.length; i++)
						{
							var child = p_metadata.children[i];
							if(p_data[child.name] != null)
							Array.prototype.push.apply(result, core_summary_render(child, p_data[child.name], p_path + "." + child.name, p_ui, is_core_summary, p_metadata_path  + ".children[" + i + "]"));
						}
					}
					result.push('</fieldset>');
				}
				break;	
		case 'form':
		if(
			g_metadata_summary[p_metadata_path].is_core_summary > 0 && 
			(
				p_metadata.cardinality == "+" ||
			 	p_metadata.cardinality == "*"
			)
		
		)
		{
			result.push('<section id="');
			result.push(p_metadata.name)
			result.push('">');

			for(var form_index = 0; form_index < p_data.length; form_index++)
			{
				var form_item = p_data[form_index];

				result.push('<h2>')
				result.push(p_metadata.prompt);
				result.push(' Record: ');
				result.push(form_index + 1);
				result.push('</h2> ');
				//result.push(p_data[p_metadata.name]);
				
				if(p_metadata.children)
				{
					for(var i = 0; i < p_metadata.children.length; i++)
					{
						var child = p_metadata.children[i];
						if(form_item[child.name] != null)
						Array.prototype.push.apply(result, core_summary_render(child, form_item[child.name], p_path + "." + child.name, p_ui, is_core_summary, p_metadata_path  + ".children[" + i + "]"));
					}
				}
			}
		}
		else if(g_metadata_summary[p_metadata_path].is_core_summary > 0)
		{
				result.push('<section id="');
				result.push(p_metadata.name)
				result.push('"> <h2>')
				result.push(p_metadata.prompt);
				result.push('</h2> ');
				//result.push(p_data[p_metadata.name]);
				
				if(p_metadata.children)
				{
					for(var i = 0; i < p_metadata.children.length; i++)
					{
						var child = p_metadata.children[i];
						if(p_data[child.name] != null)
						Array.prototype.push.apply(result, core_summary_render(child, p_data[child.name], p_path + "." + child.name, p_ui, is_core_summary, p_metadata_path  + ".children[" + i + "]"));
					}
				}
			}
				result.push('</section>');
				break;	
		case "grid":
				if(
					
					p_metadata.is_core_summary && p_metadata.is_core_summary == true

				)
				{
					result.push('<table border=1>');
					result.push('<tr><th colspan=')
					result.push(p_metadata.children.length);
					result.push('>');
					result.push(p_metadata.prompt);
					result.push('</th></tr>');

					result.push('<tr>');
					for(var j = 0; j < p_metadata.children.length; j++)
					{
						var child = p_metadata.children[j];
						result.push('<td>');
						result.push(child.prompt);
						result.push('</td>');
					}
					result.push('</tr>');

					for(var i = 0; i < p_data.length; i++)
					{
						if(p_data[i] != null)
						{
							result.push('<tr>');
							for(var j = 0; j < p_metadata.children.length; j++)
							{
								var child = p_metadata.children[j];
								result.push('<td>');
								if(p_data[i][child.name] != null)
								{
									result.push(p_data[i][child.name]);
								}
								else
								{
									result.push("&nbsp;");
								}
								result.push('</td>');
							}
							result.push('</tr>');
						}
					}
					result.push('</table>');
				}
				else if(g_metadata_summary[p_metadata_path].is_core_summary > 0)
				{
					result.push('<table border=1>');
					result.push('<tr><th colspan=')
					var colspan = 0;
					for(var j = 0; j < p_metadata.children.length; j++)
					{
						var child = p_metadata.children[j];
						if(child.is_core_summary && child.is_core_summary == true)
						{
							colspan = colspan + 1;	
						}
					}
					result.push(colspan);
					result.push('>');
					result.push(p_metadata.prompt);
					result.push('</th></tr>');

					result.push('<tr>');
					for(var j = 0; j < p_metadata.children.length; j++)
					{
						var child = p_metadata.children[j];
						if(child.is_core_summary && child.is_core_summary == true)
						{
							result.push('<td>');
							result.push(child.prompt);
							result.push('</td>');
						}

					}
					result.push('</tr>');

					for(var i = 0; i < p_data.length; i++)
					{
						if(p_data[i] != null)
						{
							result.push('<tr>');
							for(var j = 0; j < p_metadata.children.length; j++)
							{
								var child = p_metadata.children[j];
								if(child.is_core_summary && child.is_core_summary == true)
								{
									result.push('<td>');
									if(p_data[i][child.name] != null)
									{
										result.push(p_data[i][child.name]);
									}
									else
									{
										result.push("&nbsp;");
									}
									result.push('</td>');
								}
							}
							result.push('</tr>');
						}
					}
					result.push('</table>');
				}
				break;				
		case 'app':	
				if(
					(p_metadata.is_core_summary || p_metadata.is_core_summary == true) ||
					is_core_summary == true
				)
				{
				result.push('<p>');
				//result.push(p_path)
				result.push(' <strong>')
				result.push(p_metadata.prompt);
				result.push('</strong>: ');
				result.push(p_data[p_metadata.name]);
				result.push('</p>');
				}

				if(p_metadata.children)
				{
					for(var i = 0; i < p_metadata.children.length; i++)
					{
						var child = p_metadata.children[i];
						if(child.type.toLowerCase() == "form" && p_data[child.name] != null)
						Array.prototype.push.apply(result, core_summary_render(child, p_data[child.name], p_path + "." + child.name, p_ui, is_core_summary, "g_metadata.children[" + i + "]"));
					}
				}
				break;		
		case 'list':
				if(
					(p_metadata.is_core_summary || p_metadata.is_core_summary == true) ||
					is_core_summary == true
				)
				{

					let data_value_list = p_metadata.values;
					let list_lookup = {};
	
					if(p_metadata.path_reference && p_metadata.path_reference != "")
					{
						data_value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));
				
						if(data_value_list == null)	
						{
							data_value_list = p_metadata.values;
						}
					}
	
					for(let list_index = 0; list_index < data_value_list.length; list_index++)
					{
						let list_item = data_value_list[list_index];
						list_lookup[list_item.value] = list_item.display;
					}
	
					result.push('<p>');
					//result.push(p_path)
					result.push('<h9>');
					result.push(' <strong>')
					result.push(p_metadata.prompt);
					result.push('</strong>: ');
					
					//result.push(p_data[p_metadata.name]);
					if(Array.isArray(p_data))
					{
						result.push("<ul>");
						for(var i = 0; i < p_data.length; i++)
						{
                            if
                            (
                                (p_data[i] == 9999 || p_data[i] == "9999") &&
                                p_data.length > 1
                            )
                            {
                                continue;
                            }
                            
							result.push("<li>");
							result.push(p_data[i]);
							result.push(" - ");
							result.push(list_lookup[p_data[i]]);
							result.push("</li>");
	
						}
						result.push("</ul>");
					}
					else
					{
						result.push(p_data);
						result.push(" - ");
						result.push(list_lookup[p_data]);
					}
					result.push('</h9>');
					result.push('</p>');
					break;
				}
			break;
		default:
				if(
					(p_metadata.is_core_summary || p_metadata.is_core_summary == true) ||
					is_core_summary == true
				)
				{

					result.push('<p>');
					//result.push(p_path)
					result.push(' <strong>')
					result.push(p_metadata.prompt);
					result.push('</strong>: ');
					result.push(p_data);
					result.push('</p>');
				}

				if(p_metadata.children)
				{
					for(var i = 0; i < p_metadata.children.length; i++)
					{
						var child = p_metadata.children[i];
						if(p_data[child.name] != null)
						Array.prototype.push.apply(result, core_summary_render(child, p_data[child.name], p_path + "." + child.name, p_ui, is_core_summary));
					}
				}
				break;
	}

	return result;

}

function convert_dictionary_path_to_lookup_object(p_path)
{

	//g_data.prenatal.routine_monitoring.systolic_bp
	let result = null;
	let temp_result = []
	let temp = "g_metadata." + p_path.replace(new RegExp('/','gm'),".").replace(new RegExp('\\.(\\d+)\\.','gm'),"[$1].").replace(new RegExp('\\.(\\d+)$','g'),"[$1]");
	let index = temp.lastIndexOf('.');
	temp_result.push(temp.substr(0, index));
	temp_result.push(temp.substr(index + 1, temp.length - (index + 1)));

	let lookup_list = eval(temp_result[0]);

	for(let i = 0; i < lookup_list.length; i++)
	{
		if(lookup_list[i].name == temp_result[1])
		{
			result = lookup_list[i].values;
			break;
		}
	}


	return result;
}