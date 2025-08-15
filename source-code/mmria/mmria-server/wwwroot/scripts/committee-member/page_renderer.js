function page_render(p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_is_grid_context, p_group_level, p_row, p_column)
{
	var stack = [];
	var result = [];
	var current_column = p_column;

	switch(p_metadata.type.toLowerCase())
  {
    case 'grid':
		var is_grid_context = true;

		result.push("<table id='");
		result.push(p_metadata_path);
		result.push("' class='grid'><tr><th colspan=");
		result.push(p_metadata.children.length + 1)
		result.push(">");
		result.push(p_metadata.prompt);
		result.push("</th></tr>");

		result.push('<tr>');
		for(var i = 0; i < p_metadata.children.length; i++)
		{
			var child = p_metadata.children[i];
			result.push('<th>');
			result.push(child.prompt);
			result.push('</th>')

		}
		result.push('<th>&nbsp;</th></tr>');

		for(var i = 0; i < p_data.length; i++)
		{
			result.push('<tr>');
			for(var j = 0; j < p_metadata.children.length; j++)
			{
				var child = p_metadata.children[j];
				result.push("<td>");
				if(p_data[i][child.name])
				{
					// do nothing 
				}
				else
				{
					p_data[i][child.name] = create_default_object(child, {})[child.name];
				}
				Array.prototype.push.apply(result, page_render(child, p_data[i][child.name], p_ui, p_metadata_path + ".children[" + j + "]", p_object_path + "[" + i + "]." + child.name, is_grid_context, p_group_level, p_row, p_column));
				result.push("</td>");
			}
			result.push('<td> <input type="button" value="delete" id="delete_');
			result.push(p_object_path.replace(/\./g,"_") + "[" + i + "]");
			result.push('" onclick="g_delete_grid_item(\'');
			result.push(p_object_path + "[" + i + "]");
			result.push("', '");
			result.push(p_metadata_path);
			result.push("', '");
			result.push(p_dictionary_path);
			result.push('\', ' + i + ')" /></td></tr>');
		}
    	result.push("<tr><td colspan=");
		result.push(p_metadata.children.length + 1);
		result.push(" align=right> <input type='button' value='Add Item' onclick='g_add_grid_item(\"");
		result.push(p_object_path);
		result.push("\", \"");
		result.push(p_metadata_path);
		result.push("\")' /></td></tr>");

		result.push("</table>");
		break;
    case 'group':
		result.push("<h3 id='");
		result.push(p_metadata.name);
		result.push("_id' class='group'>");
		result.push(p_metadata.prompt);
		result.push("</h3>");

		var group_stack = [];

		for(var i = 0; i < p_metadata.children.length; i++)
		{
			var child = p_metadata.children[i];

			if(p_data[child.name])
			{
				// do nothing 
			}
			else
			{
				p_data[child.name] = create_default_object(child, {})[child.name];
			}

			Array.prototype.push.apply(result, page_render(child, p_data[child.name], p_ui, p_metadata_path + '.children[' + i + "]", p_object_path + "." + child.name, false, p_group_level + 1, p_row, current_column + i));

		}
		break;
    case 'form':
		if(
			 p_metadata.cardinality == "+" ||
			 p_metadata.cardinality == "*"
		
		)
		{
			result.push("<section id='");
			result.push(p_metadata.name);
			result.push("_id' class='form'><h2 ");
			if(p_metadata.description && p_metadata.description.length > 0)
			{
				result.push("rel='tooltip'  data-original-title='");
				result.push(p_metadata.description.replace(/'/g, "\\'"));
				result.push("'>");
			}
			else
			{
				result.push(">");
			}

			result.push(p_metadata.prompt);
			result.push("</h2>");

			if(g_data)
			{
				result.push("<h3  style='color: purple'>");
				result.push(g_data.home_record.last_name);
				result.push(", ");
				result.push(g_data.home_record.first_name);
				if(g_data.home_record.record_id)
				{
					result.push("  - ");
					result.push(g_data.home_record.record_id);
				}
				result.push("</h3>");
			}

			/*
			result.push('<input path="" type="button" value="Add New ');
			result.push(p_metadata.prompt);
			result.push(' form" onclick="add_new_form_click(\'' + p_metadata_path + '\',\'' + p_object_path + '\')" />');
			*/
			
			result.push('<div class="search_wrapper">');
			for(var i = 0; i < p_data.length; i++)
			{
				var item = p_data[i];
				if(item)
				{
					if(i % 2)
					{
						result.push('		  <div class="result_wrapper_grey"> <a href="#/');
					}
					else
					{
						result.push('		  <div class="result_wrapper"> <a href="#/');
					}
					result.push(p_ui.url_state.path_array.join("/"));
					//result.push(p_metadata.name);
					result.push("/");
					result.push(i);
					result.push("\">");
					result.push('Record ');
					result.push(i + 1);
		
					result.push('</a>');
					result.push('</div>');
				}

			}
			result.push('		</div>');
			result.push("</section>");

			if(p_ui.url_state.path_array.length > 2)
			{
				var data_index = parseInt(p_ui.url_state.path_array[2]);
				var form_item = p_data[data_index];

				result.push("<section id='");
				result.push(p_metadata.name);
				result.push("' class='form'><h2 ");
				if(p_metadata.description && p_metadata.description.length > 0)
				{
					result.push("rel='tooltip'  data-original-title='");
					result.push(p_metadata.description.replace(/'/g, "\\'"));
					result.push("'>");
				}
				else
				{
					result.push(">");
				}

				result.push(p_metadata.prompt);
				result.push("</h2><h4>");
				result.push(" record: ");
				result.push(data_index + 1);
				result.push("</h4>");
				
				
				if(g_data)
				{
					result.push("<h3  style='color: purple'>");
					result.push(g_data.home_record.last_name);
					result.push(", ");
					result.push(g_data.home_record.first_name);
					if(g_data.home_record.record_id)
					{
						result.push("  - ");
						result.push(g_data.home_record.record_id);
					}
					result.push("</h3>");
				}

				for(var i = 0; form_item && i < p_metadata.children.length; i++)
				{
					var child = p_metadata.children[i];
					//var item = p_data[data_index][child.name];
					if(form_item[child.name])
					{

					}
					else
					{
						form_item[child.name] = create_default_object(child, {})[child.name];
					}

					if(child.type=="group")
					{
						Array.prototype.push.apply(result, page_render(child,form_item[child.name], p_ui, p_metadata_path + '.children[' + i + "]", p_object_path + "[" + data_index + "]." + child.name, false, 1, 0, current_column));
					}
					else
					{
						Array.prototype.push.apply(result, page_render(child, form_item[child.name], p_ui, p_metadata_path + '.children[' + i + "]", p_object_path + "[" + data_index + "]." + child.name, false, 0, 0, current_column));
					}
					
					current_column += 1;
					//result.push("</div>");
				}
				result.push("</section>");

			}

		}
		else
		{

			result.push("<section id='");
			result.push(p_metadata.name);
			result.push("_id' class='form'><h2 ");
			if(p_metadata.description && p_metadata.description.length > 0)
			{
				result.push("rel='tooltip'  data-original-title='");
				result.push(p_metadata.description.replace(/'/g, "\\'"));
				result.push("'>");
			}
			else
			{
				result.push(">");
			}

			result.push(p_metadata.prompt);
			result.push("</h2>");
			if(g_data)
			{
				result.push("<h3  style='color: purple'>");
				result.push(g_data.home_record.last_name);
				result.push(", ");
				result.push(g_data.home_record.first_name);
				if(g_data.home_record.record_id)
				{
					result.push("  - ");
					result.push(g_data.home_record.record_id);
				}
				result.push("</h3>");
			}
			

			if(g_data && p_metadata.name == "case_narrative")
			{
				//death_certificate/reviewer_note
				result.push("<h3>Death Certificate Reviewer's Notes</h3>");
				result.push("<textarea cols=80 rows=7>");
				result.push(g_data.death_certificate.reviewer_note);
				result.push("</textarea>");

				//birth_fetal_death_certificate_parent/reviewer_note
				result.push("<h3>Birth/Fetal Death Certificate- Parent Section Reviewer's Notes</h3>");
				result.push("<textarea cols=80 rows=7>");
				result.push(g_data.birth_fetal_death_certificate_parent.reviewer_note);
				result.push("</textarea>");
				
				//birth_certificate_infant_fetal_section/reviewer_note
				result.push("<h3>Birth/Fetal Death Certificate- Infant/Fetal Section Reviewer's Notes</h3>");
				for(var i = 0; i < g_data.birth_certificate_infant_fetal_section.length; i++)
				{
					result.push("<p>Note: ");
					result.push(i+1);
					result.push("<br/>");
					result.push("<textarea cols=80 rows=7>");
					result.push(g_data.birth_certificate_infant_fetal_section[i].reviewer_note);
					result.push("</textarea>");
				}
				
				//autopsy_report/reviewer_note
				result.push("<h3>Autopsy Report Reviewer's Notes</h3>");
				result.push("<textarea cols=80 rows=7>");
				result.push(g_data.autopsy_report.reviewer_note);
				result.push("</textarea>");
				
				//prenatal/reviewer_note
				result.push("<h3>Prenatal Care Record Reviewer's Notes</h3>");
				result.push("<textarea cols=80 rows=7>");
				result.push(g_data.prenatal.reviewer_note);
				result.push("</textarea>");
				

				
				//er_visit_and_hospital_medical_records/reviewer_note
				result.push("<h3>ER Visits and Hospitalizations Reviewer's Notes</h3>");
				for(var i = 0; i < g_data.er_visit_and_hospital_medical_records.length; i++)
				{
					result.push("<p>Note: ");
					result.push(i+1);
					result.push("<br/>");
					result.push("<textarea cols=80 rows=7>");
					result.push(g_data.er_visit_and_hospital_medical_records[i].reviewer_note);
					result.push("</textarea>");
					result.push("</p>");
				}
				
				//other_medical_office_visits/reviewer_note
				result.push("<h3>Other Medical Office Visits Reviewer's Notes</h3>");
				for(var i = 0; i < g_data.other_medical_office_visits.length; i++)
				{
					result.push("<p>Note: ");
					result.push(i+1);
					result.push("<br/>");
					result.push("<textarea cols=80 rows=7>");
					result.push(g_data.other_medical_office_visits[i].reviewer_note);
					result.push("</textarea>");
					result.push("</p>");
				}
				///medical_transport/transport_narrative_summary
				result.push("<h3>Medical Transport Reviewer's Notes</h3>");
				for(var i = 0; i < g_data.medical_transport.length; i++)
				{
					result.push("<p>Note: ");
					result.push(i+1);
					result.push("<br/>");
					result.push("<textarea cols=80 rows=7>");
					result.push(g_data.medical_transport[i].reviewer_note);
					result.push("</textarea>");
					result.push("</p>");
				}
				
				//social_and_environmental_profile/reviewer_note
				result.push("<h3>Social and Environmental Profile Reviewer's Notes</h3>");
				result.push("<textarea cols=80 rows=7>");
				result.push(g_data.social_and_environmental_profile.reviewer_note);
				result.push("</textarea>");

				result.push("<h3>Mental Health Profile Reviewer's Notes</h3>");
				result.push("<textarea cols=80 rows=7>");
				result.push(g_data.mental_health_profile.reviewer_note);
				result.push("</textarea>");

				result.push("<h3>Informant Interviews Reviewer's Notes</h3>");
				for(var i = 0; i < g_data.other_medical_office_visits.length; i++)
				{
					result.push("<p>Note: ");
					result.push(i+1);
					result.push("<br/>");
					result.push("<textarea cols=80 rows=7>");
					result.push(g_data.informant_interviews[i].reviewer_note);
					result.push("</textarea>");
					result.push("</p>");
				}
			}

			if(p_ui.url_state.path_array.length > 1)
			{
				for(var i = 0; i < p_metadata.children.length; i++)
				{
					var child = p_metadata.children[i];
					if(p_data[child.name])
					{
						// do nothing 
					}
					else
					{
						p_data[child.name] = create_default_object(child, {})[child.name];
					}
					Array.prototype.push.apply(result, page_render(child, p_data[child.name], p_ui, p_metadata_path + '.children[' + i + "]", p_object_path + "." + child.name, false, p_group_level, p_row, p_column));
				}
			}
			result.push("</section>");
		}
		break;
    case 'app':
      result.push("<section id='app_summary'><h2>Line Listing Summary</h2>");
      //result.push("<fieldset><legend>filter line listing</legend>");
      //result.push("<input type='text' id='search_text_box' value='' /> ");
      //result.push("<img src='/images/search.png' alt='search' height=8px width=8px valign=bottom class='btn-green' id='search_command_button'>");
      result.push("</fieldset>");

      result.push('<div class="search_wrapper">');
      for(var i = 0; i < p_ui.data_list.length; i++)
      {
        var item = p_ui.data_list[i];

        if(i % 2)
        {
          result.push('<div class="result_wrapper_grey" path="');
        }
        else
        {
          result.push('<div class="result_wrapper" path="');
        }
          result.push(item._id);
          result.push('"><p class="result">');
          result.push(item.home_record.last_name);
          result.push(', ');
          result.push(item.home_record.first_name);
          result.push(' - ');
          result.push(item.home_record.record_id);
          result.push('	(');
          result.push(item.home_record.state_of_death);
          result.push('	) <a href="#/'+ i + '/home_record" role="button" class="btn-purple">select</a> </p>');
        result.push('</div>');
        
      }
      result.push('</div>');
      result.push("</section>");

      for(var i = 0; i < p_metadata.children.length; i++)
      {
        var child = p_metadata.children[i];
        if(child.type.toLowerCase() == 'form')
        {
          if(p_data[child.name])
          {
            // do nothing 
          }
          else
          {
            p_data[child.name] = create_default_object(child, {})[child.name];
          }

          Array.prototype.push.apply(result, page_render(child, p_data[child.name], p_ui, p_metadata_path  + ".children[" + i + "]", p_object_path + "." + child.name, false, p_group_level, p_row, p_column));				 		
        }
      }

      result.push('<footer class="footer_wrapper">');
      result.push('<p>&nbsp;</p>');
      result.push('</footer>');

      break;
      
     case 'label':
			result.push("<div class='label' id='");
			result.push(p_object_path.replace(".","_"));
			result.push("' ");
			if(p_metadata.description && p_metadata.description.length > 0)
			{
				result.push("rel='tooltip'  data-original-title='");
				result.push(p_metadata.description.replace(/'/g, "\\'"));
				result.push("'>");
			}
			else
			{
				result.push(">");
			}

			result.push(p_metadata.prompt);
			result.push("</div>");
			break;
        case 'button':
        case'always_enabled_button':
	 		page_render_create_input(result, p_metadata, p_data, p_metadata_path, p_object_path)

			break;
		case 'string':
			result.push("<div class='string' id='");
			result.push(p_object_path);
			result.push("'><span ");
			if(p_metadata.description && p_metadata.description.length > 0)
			{
				result.push("rel='tooltip'  data-original-title='");
				result.push(p_metadata.description.replace(/'/g, "\\'"));
				result.push("' ");
			}
			else
			{
				result.push(" ");
			}

			if(p_metadata.validation_description && p_metadata.validation_description.length > 0)
			{
				result.push(" validation-tooltip='");
				result.push(p_metadata.validation_description.replace(/'/g, "\\'"));
				result.push("'>");
			}
			else
			{
				result.push(">");
			}
			

			result.push(p_metadata.prompt);

			result.push("</span><br/>");

			var dictionary_path = p_object_path.replace("g_data.", "").replace(/\./g,"/");
			if(de_identified_list[dictionary_path] && de_identified_list[dictionary_path] == true)
			{
				result.push("de-identified");
			}
			else
			{
				result.push(p_data);
			}
			//page_render_create_input(result, p_metadata, p_data, p_metadata_path, p_object_path);
			result.push("</div>");
			
			break;
			   
		case 'address':
		case 'textarea':
				result.push("<div class='textarea' id='");
          result.push(p_object_path);
          result.push("'>");
          result.push("<span ");
          if(p_metadata.description && p_metadata.description.length > 0)
          {
            result.push("rel='tooltip'  data-original-title='");
            result.push(p_metadata.description.replace(/'/g, "\\'"));
            result.push("'>");
          }
          else
          {
            result.push(">");
          }

          result.push(p_metadata.prompt);
          
          result.push("</span>");
          result.push("<br/>");
          result.push("<textarea>");
            result.push(p_data);
            //page_render_create_textarea(result, p_metadata, p_data, p_metadata_path, p_object_path);
          result.push("</textarea>");
				result.push("</div>");
           break;
    case 'number':
			result.push("<div class='number' id='");
			result.push(p_object_path);
			result.push("'><span ");
			if(p_metadata.description && p_metadata.description.length > 0)
			{
				result.push("rel='tooltip'  data-original-title='");
				result.push(p_metadata.description.replace(/'/g, "\\'"));
				result.push("'>");
			}
			else
			{
				result.push(">");
			}
			
			result.push(p_metadata.prompt);
			
			result.push("</span> ");
			var dictionary_path = p_object_path.replace("g_data.", "").replace(/\./g,"/");
			if(de_identified_list[dictionary_path] && de_identified_list[dictionary_path] == true)
			{
				result.push("de-identified");
			}
			else
			{
				result.push(p_data);
			}

			
			//page_render_create_input(result, p_metadata, p_data, p_metadata_path, p_object_path);
			result.push("</div>");
			
      break;
    case 'boolean':
			result.push("<div class='boolean' id='");
			result.push(p_object_path);
			result.push("'> <input type='checkbox' name='");
			result.push(p_metadata.name);
			if(p_data == true)
			{
				result.push("' checked='true'");
			}
			else
			{
				result.push("'  value='");
			}
			page_render_create_checkbox(result, p_metadata, p_data, p_metadata_path, p_object_path);

			result.push("<span ");
			if(p_metadata.description && p_metadata.description.length > 0)
			{
				result.push("rel='tooltip'  data-original-title='");
				result.push(p_metadata.description.replace(/'/g, "\\'"));
				result.push("'>");
			}
			else
			{
				result.push(">");
			}
			

			result.push(p_metadata.prompt);

			result.push("</span></div>");
			result.push("</div>");

      break;
    case 'list':
			if(p_metadata.control_style && p_metadata.control_style.toLowerCase().indexOf("editable") > -1)
			{
				result.push("<div class='list' id='");
				result.push(p_object_path.replace(/\./g,"_"))
				
				result.push("'> <span ");
				if(p_metadata.description && p_metadata.description.length > 0)
				{
					result.push("rel='tooltip'  data-original-title='");
					result.push(p_metadata.description.replace(/'/g, "\\'"));
					result.push("'>");
				}
				else
				{
					result.push(">");
				}
				
				result.push(p_metadata.prompt);

				result.push("</span> <br/>");

				if(p_metadata.list_display_size && p_metadata.list_display_size!= "")
				{
					result.push(" <select size=");
					result.push(p_metadata.list_display_size);
					result.push(" name='");
				}
				else if(p_metadata.is_multiselect && p_metadata.is_multiselect == true)
				{
					
					if(p_metadata.values.length > 6)
					{
						result.push("<select size='6' name='");
					}
					else
					{
						result.push(" <select size=");
						result.push(p_metadata.values.length);
						result.push(" name='");
					}
					
				}
				else
				{
					result.push("<select size=");
					result.push(1);
					result.push(" name='");
				}

				result.push(p_metadata.name);
				result.push("'  onchange='g_set_data_object_from_path(\"");
				result.push(p_object_path);
				result.push("\",\"");
				result.push(p_metadata_path);
				result.push("\",this.value)'  ");

				if(p_metadata['is_multiselect'] && p_metadata.is_multiselect == true)
				{
					result.push(" multiple>");

					for(var i = 0; i < p_metadata.values.length; i++)
					{
						var item = p_metadata.values[i];
						if(p_data.indexOf(item.value) > -1)
						{
								result.push("<option value='");
								result.push(item.value.replace(/'/g, "&#39;"));
								result.push("' selected>");
								if(p_metadata.is_save_value_display_description && p_metadata.is_save_value_display_description == true)
								{
									result.push(item.description);
								}
								else
								{
									result.push(item.value);
								}
								result.push("</option>");
						}
						else
						{
								result.push("<option value='");
								result.push(item.value.replace(/'/g, "&#39;"));
								result.push("' >");
								if(p_metadata.is_save_value_display_description && p_metadata.is_save_value_display_description == true)
								{
									result.push(item.description);
								}
								else
								{
									result.push(item.value);
								}
								result.push("</option>");
						}
					}
					result.push("</select>");


				//if(p_metadata.list_display_size && p_metadata.list_display_size!="")
				//{
					result.push("<br/> <input placeholder='Specify Other' class='list' type='text' name='");
					result.push(p_metadata.name);
					result.push("' value='");
					result.push(p_data);
					result.push("' onblur='g_set_data_object_from_path(\"");
					result.push(p_object_path);
					result.push("\",\"");
					result.push(p_metadata_path);
					result.push("\",this.value)' /> <br/> ");
				//}

				}
				else
				{
					result.push(">");

					for(var i = 0; i < p_metadata.values.length; i++)
					{
						var item = p_metadata.values[i];
						if(p_data == item.value)
						{
							result.push("<option value='");
							result.push(item.value.replace(/'/g, "&#39;"));
							result.push("' selected>");
							if(p_metadata.is_save_value_display_description && p_metadata.is_save_value_display_description == true)
							{
								result.push(item.description);
							}
							else
							{
								result.push(item.value);
							}
							result.push("</option>");
						}
						else
						{
							result.push("<option value='");
							result.push(item.value.replace(/'/g, "&#39;"));
							result.push("' >");
							if(p_metadata.is_save_value_display_description && p_metadata.is_save_value_display_description == true)
							{
								result.push(item.description);
							}
							else
							{
								result.push(item.value);
							}
							result.push("</option>");
						}
					}
					result.push("</select> ");

				//if(p_metadata.list_display_size && p_metadata.list_display_size!="")
				//{
					result.push("<br/> <input placeholder='Specify Other' class='list' type='text' name='");
					result.push(p_metadata.name);
					result.push("' value='");
					result.push(p_data);
					result.push("' onblur='g_set_data_object_from_path(\"");
					result.push(p_object_path);
					result.push("\",\"");
					result.push(p_metadata_path);
					result.push("\",this.value)' /> </div> ");
				//}


				}
			}
			else
			{
				result.push("<div class='list' id='");
				result.push(p_object_path.replace(/\./g,"_"))
				
				result.push("'> <span ");
				if(p_metadata.description && p_metadata.description.length > 0)
				{
					result.push("rel='tooltip'  data-original-title='");
					result.push(p_metadata.description.replace(/'/g, "&#39;"));
					result.push("'>");
				}
				else
				{
					result.push(">");
				}
				
				result.push(p_metadata.prompt);
				result.push("</span> <br/> ");

				if(p_metadata.list_display_size && p_metadata.list_display_size!="")
				{
					result.push("<select size=");
					result.push(p_metadata.list_display_size);
					result.push(" name='");
				}
				else if(p_metadata.is_multiselect && p_metadata.is_multiselect == true)
				{
					
					if(p_metadata.values.length > 6)
					{
						result.push("<select size='6' name='");
					}
					else
					{
						result.push("<select size=");
						result.push(p_metadata.values.length);
						result.push(" name='");
					}
					
				}
				else
				{
					result.push("<select size=");
					result.push(1);
					result.push(" name='");
				}

				result.push(p_metadata.name);
				result.push("'  onchange='g_set_data_object_from_path(\"");
				result.push(p_object_path);
				result.push("\",\"");
				result.push(p_metadata_path);
				result.push("\",this.value)'  ");

				if(p_metadata['is_multiselect'] && p_metadata.is_multiselect == true)
				{
					result.push(" multiple>");
					for(var i = 0; i < p_metadata.values.length; i++)
					{
						var item = p_metadata.values[i];
						if(p_data && p_data.indexOf(item.value) > -1)
						{
								result.push("<option value='");
								result.push(item.value.replace(/'/g, "&#39;"));
								result.push("' selected>");
								if(p_metadata.is_save_value_display_description && p_metadata.is_save_value_display_description == true)
								{
									result.push(item.description);
								}
								else
								{
									result.push(item.value);
								}
								result.push("</option>");
						}
						else
						{
								result.push("<option value='");
								result.push(item.value.replace(/'/g, "&#39;"));
								result.push("' >");
								if(p_metadata.is_save_value_display_description && p_metadata.is_save_value_display_description == true)
								{
									result.push(item.description);
								}
								else
								{
									result.push(item.value);
								}
								result.push("</option>");
						}
					}
					result.push("</select></div>");
				}
				else
				{
					result.push(">");

					for(var i = 0; i < p_metadata.values.length; i++)
					{
						var item = p_metadata.values[i];
						if(p_data == item.value)
						{
							result.push("<option value='");
							result.push(item.value.replace(/'/g, "&#39;"));
							result.push("' selected>");
							if(p_metadata.is_save_value_display_description && p_metadata.is_save_value_display_description == true)
							{
								result.push(item.description);
							}
							else
							{
								result.push(item.value);
							}
							result.push("</option>");
						}
						else
						{
							result.push("<option value='");
							result.push(item.value.replace(/'/g, "&#39;"));
							result.push("' >");
							if(p_metadata.is_save_value_display_description && p_metadata.is_save_value_display_description == true)
							{
								result.push(item.description);
							}
							else
							{
								result.push(item.value);
							}
							result.push("</option>");
						}
					}
					result.push("</select></div>");
				}
			}

           break;
	case 'date':
			if (p_metadata.name == "date_of_screening")
			{
				console.log("break");
			}

			result.push("<div class='date' id='");
			result.push(p_object_path.replace(/\./g,"_"))
			result.push("'> ");
			result.push("<span ");

			if(p_metadata.description && p_metadata.description.length > 0)
			{
				result.push("rel='tooltip'  data-original-title='");
				result.push(p_metadata.description.replace(/'/g, "\\'"));
				result.push("'>");
			}
			else
			{
				result.push(">");
			}
			
			result.push(p_metadata.prompt);

			result.push("</span> ");
			result.push("<div style='position:relative'>");

			var dictionary_path = p_object_path.replace("g_data.", "").replace(/\./g,"/");

			if(de_identified_list[dictionary_path] && de_identified_list[dictionary_path] == true)
			{
				result.push("de-identified");
			}
			else
			{
				result.push(p_data);
			}
			//page_render_create_input(result, p_metadata, p_data, p_metadata_path, p_object_path);
			result.push("</div>");
			result.push("</div>");
			break;

	case 'datetime':

			result.push("<div class='date' id='");
			result.push(p_object_path.replace(/\./g,"_"))
			result.push("'> ");
			result.push("<span ");
			if(p_metadata.description && p_metadata.description.length > 0)
			{
				result.push("rel='tooltip'  data-original-title='");
				result.push(p_metadata.description.replace(/'/g, "\\'"));
				result.push("'>");
			}
			else
			{
				result.push(">");
			}
			
			result.push(p_metadata.prompt);

			result.push("</span> ");
			result.push("<div style='position:relative'>");
			var dictionary_path = p_object_path.replace("g_data.", "").replace(/\./g,"/");
			if(de_identified_list[dictionary_path] && de_identified_list[dictionary_path] == true)
			{
				result.push("de-identified");
			}
			else
			{
				result.push(p_data);
			}
			//page_render_create_input(result, p_metadata, p_data, p_metadata_path, p_object_path);
			result.push("</div>");	
			result.push("</div>");	
			 break;
		case 'time':
		/*
			if(typeof(p_data) == "string")
			{
				p_data = new Date(p_data);
			}*/
			result.push("<div  class='time' id='");
			result.push(p_object_path.replace(/\./g,"_"))
			
			result.push("'> ");
			result.push("<span ");
			if(p_metadata.description && p_metadata.description.length > 0)
			{
				result.push("rel='tooltip'  data-original-title='");
				result.push(p_metadata.description.replace(/'/g, "\\'"));
				result.push("'>");
			}
			else
			{
				result.push(">");
			} 
			
			result.push(p_metadata.prompt);

			result.push("</span> ");
			result.push("<div style='position:relative'>");
			var dictionary_path = p_object_path.replace("g_data.", "").replace(/\./g,"/");
			if(de_identified_list[dictionary_path] && de_identified_list[dictionary_path] == true)
			{
				result.push("de-identified");
			}
			else
			{
				result.push(p_data);
			}
			//page_render_create_input(result, p_metadata, p_data, p_metadata_path, p_object_path);
			result.push("</div>");
			result.push("</div>");

			break;
     default:
          console.log("page_render not processed", p_metadata);
       break;
  }

	return result;

}


function page_render_create_input(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
{
	p_result.push("<input  class='");
	p_result.push(p_metadata.type.toLowerCase());
	if
    (
        p_metadata.type=="button" ||
        p_metadata.type=="always_enabled_button"
    )
	{
		p_result.push("' type='button' name='");
		p_result.push(p_metadata.name);
		p_result.push("' value='");
		p_result.push(p_metadata.prompt);
		p_result.push("' ");

		if(p_metadata.type == "")
		{
			p_result.push("placeholder='");
			if(p_metadata.prompt.length > 25)
			{
				p_result.push(p_metadata.prompt.substring(0, 25));
			}
			else
			{
				p_result.push(p_metadata.prompt);
			}
			
			p_result.push("' ");
		}

		if(p_metadata.onclick && p_metadata.onclick != "")
		{
			page_render_create_event(p_result, "onclick", p_metadata.onclick, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
		}
	}
	else
	{
		p_result.push("' type='text' name='");
		p_result.push(p_metadata.name);
		p_result.push("' value='");
		p_result.push(p_data);
		p_result.push("'");

		if(p_metadata.onfocus && p_metadata.onfocus != "")
		{
			//page_render_create_event(p_result, "onfocus", p_metadata.onfocus, p_metadata_path, p_object_path)
		}


		if(
			p_metadata.type == "number" ||
			p_metadata.type == "datetime" ||
			p_metadata.type == "date" ||
			p_metadata.type == "time" 
		)
		{
			//page_render_create_onchange_event(p_result, p_metadata, p_metadata_path, p_object_path)
		}
		else if(p_metadata.onchange && p_metadata.onchange != "")
		{
			//page_render_create_event(p_result, "onchange", p_metadata.onchange, p_metadata_path, p_object_path)
		}
		
		if(p_metadata.onclick && p_metadata.onclick != "")
		{
			//page_render_create_event(p_result, "onclick", p_metadata.onclick, p_metadata_path, p_object_path)
		}
		
		//page_render_create_onblur_event(p_result, p_metadata, p_metadata_path, p_object_path);
	}


	p_result.push("/>");
	
}


function page_render_create_event(p_result, p_event_name, p_code_json, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
{
	var post_fix = null;

	switch(p_event_name)
	{
		case "onfocus":
			post_fix = "_of";
			break;
		case "onchange":
			post_fix = "_och";
			break;
		case "onclick":
			post_fix = "_ocl";
			break;
		default:
			console.log("page_render_create_event - missing case: " + p_event_name);
			break;
	}

	//var source_code = escodegen.generate(p_metadata.onfocus);
	var code_array = [];
	
	code_array.push("x" + path_to_int_map[p_metadata_path].toString(16) + post_fix);
	code_array.push(".call(");
	code_array.push(p_object_path.substring(0, p_object_path.lastIndexOf(".")));
	code_array.push(", this);");

	p_result.push(" ");
	p_result.push(p_event_name);
	p_result.push("='");
	p_result.push(code_array.join('').replace(/'/g,"\""));
	p_result.push("'");
	
}




function page_render_create_onblur_event(p_result, p_metadata, p_metadata_path, p_object_path, p_ctx)
{
	if(p_metadata.onblur && p_metadata.onblur != "")
	{
		//var source_code = escodegen.generate(p_metadata.onfocus);
		var code_array = [];
		
		
		code_array.push("(function x" + path_to_int_map[p_metadata_path].toString(16) + "_sob(p_control){\n");
		code_array.push("x" + path_to_int_map[p_metadata_path].toString(16) + "_ob");
		code_array.push(".call(");
		code_array.push(p_object_path.substring(0, p_object_path.lastIndexOf(".")));
		code_array.push(", p_control);\n");
		
		code_array.push("g_set_data_object_from_path(\"");
		code_array.push(p_object_path);
		code_array.push("\",\"");
		code_array.push(p_metadata_path);
		code_array.push("\",p_control.value);\n}).call(");
		code_array.push(p_object_path.substring(0, p_object_path.lastIndexOf(".")));
		code_array.push(", event.target);");

		p_result.push(" onblur='");
		p_result.push(code_array.join('').replace(/'/g,"\""));
		p_result.push("'");
	}
	else
	{
		p_result.push(" onblur='g_set_data_object_from_path(\"");
		p_result.push(p_object_path);
		p_result.push("\",\"");
		p_result.push(p_metadata_path);
		if(p_metadata.type=="boolean")
		{
			p_result.push("\",this.checked)'");
		}
		else
		{
			p_result.push("\",this.value)'");
		}
		
	}
	
}



function page_render_create_onchange_event(p_result, p_metadata, p_metadata_path, p_object_path, p_ctx)
{
	if(p_metadata.onchange && p_metadata.onchange != "")
	{
		//var source_code = escodegen.generate(p_metadata.onfocus);
		var code_array = [];
		
		
		code_array.push("(function x" + path_to_int_map[p_metadata_path].toString(16) + "_sob(p_control){\n");
		code_array.push("x" + path_to_int_map[p_metadata_path].toString(16) + "_och");
		code_array.push(".call(");
		code_array.push(p_object_path.substring(0, p_object_path.lastIndexOf(".")));
		code_array.push(", p_control);\n");
		
		code_array.push("g_set_data_object_from_path(\"");
		code_array.push(p_object_path);
		code_array.push("\",\"");
		code_array.push(p_metadata_path);
		code_array.push("\",p_control.value);\n}).call(");
		code_array.push(p_object_path.substring(0, p_object_path.lastIndexOf(".")));
		code_array.push(", event.target);");

		p_result.push(" onchange='");
		p_result.push(code_array.join('').replace(/'/g,"\""));
		p_result.push("'");
	}
	else
	{
		p_result.push(" onchange='g_set_data_object_from_path(\"");
		p_result.push(p_object_path);
		p_result.push("\",\"");
		p_result.push(p_metadata_path);
		if(p_metadata.type=="boolean")
		{
			p_result.push("\",this.checked)'");
		}
		else
		{
			p_result.push("\",this.value)'");
		}
		
	}
	
}

function page_render_create_checkbox(p_result, p_metadata, p_data, p_metadata_path, p_object_path)
{
	p_result.push("<input  class='checkbox' type='checkbox' name='");
	p_result.push(p_metadata.name);
	if(p_data == true)
	{
		p_result.push("' checked='true'");
	}
	else
	{
		p_result.push("'  value='");
	}
	p_result.push(p_data);
	p_result.push("' ");

	if(p_metadata.onfocus && p_metadata.onfocus != "")
	{
		page_render_create_event(p_result, "onfocus", p_metadata.onfocus, p_metadata_path, p_object_path, p_dictionary_path)
	}

	if(p_metadata.onchange && p_metadata.onchange != "")
	{
		page_render_create_event(p_result, "onchange", p_metadata.onchange, p_metadata_path, p_object_path, p_dictionary_path)
	}
	
	if(p_metadata.onclick && p_metadata.onclick != "")
	{
		page_render_create_event(p_result, "onclick", p_metadata.onclick, p_metadata_path, p_object_path, p_dictionary_path)
	}
	
	//page_render_create_onblur_event(p_result, p_metadata, p_metadata_path, p_object_path);



	p_result.push("/>");
	
}


function page_render_create_textarea(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path)
{

	p_result.push("<textarea  class='");
	p_result.push(p_metadata.type.toLowerCase());
	//hack
	if(p_metadata.name == "case_opening_overview")
	{
		p_result.push("'<textarea'  rows=5 cols=80 name='");
	}
	else
	{
		p_result.push("'<textarea'  rows=5 cols=40 name='");
	}
	p_result.push(p_metadata.name);
	p_result.push("' ");

	if(p_metadata.onfocus && p_metadata.onfocus != "")
	{
		page_render_create_event(p_result, "onfocus", p_metadata.onfocus, p_metadata_path, p_object_path, p_dictionary_path)
	}

	if(p_metadata.onchange && p_metadata.onchange != "")
	{
		page_render_create_event(p_result, "onchange", p_metadata.onchange, p_metadata_path, p_object_path, p_dictionary_path)
	}
	
	if(p_metadata.onclick && p_metadata.onclick != "")
	{
		page_render_create_event(p_result, "onclick", p_metadata.onclick, p_metadata_path, p_object_path, p_dictionary_path)
	}
	
	//page_render_create_onblur_event(p_result, p_metadata, p_metadata_path, p_object_path);

	p_result.push(" >");
	p_result.push(p_data);
	p_result.push("</textarea>");
	
}