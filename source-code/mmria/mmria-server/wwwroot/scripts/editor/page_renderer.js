
function page_render(p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{
	var stack = [];
	var result = [];

    if(p_metadata.is_hidden != null && p_metadata.is_hidden == true)
    {
        hidden_render(result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
    }
    else switch(p_metadata.type.toLowerCase())
	{
		case 'grid':
			grid_render(result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		case 'group':
			group_render(result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		case 'form':
			form_render(result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		case 'app':
			app_render(result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		case 'label':
			label_render(result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		case 'button':
			page_render_create_input(result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path, p_ctx);
			break;
        case 'always_enabled_button':
            page_render_create_input(result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path, p_ctx);
            break;            

		case 'string':
			let string_data = p_data;

			if
			(
				p_metadata.mirror_reference &&
				p_metadata.mirror_reference.length > 0
			)
			{
				let mirror_object = get_data_object_for_mirror(p_metadata.mirror_reference);

				if(mirror_object)
				{
					string_data = mirror_object;
				}
			}		
			string_render(result, p_metadata, string_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;
				
		case 'address':
		case 'textarea':
			let textarea_data = p_data;

			if
			(
				p_metadata.mirror_reference &&
				p_metadata.mirror_reference.length > 0
			)
			{
				let mirror_object = get_data_object_for_mirror(p_metadata.mirror_reference);

				if(mirror_object)
				{
					textarea_data = mirror_object;
				}
			}
			textarea_render(result, p_metadata, textarea_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;
        case 'html_area':
            html_area_render(result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
            break;
		case 'number':
			let number_data = p_data;

			if
			(
				p_metadata.mirror_reference &&
				p_metadata.mirror_reference.length > 0
			)
			{
				let mirror_object = get_data_object_for_mirror(p_metadata.mirror_reference);

				if(mirror_object)
				{
					number_data = mirror_object;
				}
			}			
			number_render(result, p_metadata, number_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		case 'boolean':
			let boolean_data = p_data;

			if
			(
				p_metadata.mirror_reference &&
				p_metadata.mirror_reference.length > 0
			)
			{
				let mirror_object = get_data_object_for_mirror(p_metadata.mirror_reference);

				if(mirror_object)
				{
					boolean_data = mirror_object;
				}
			}				
			boolean_render(result, p_metadata, boolean_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		case 'list':
			let list_data = p_data;

			if
			(
				p_metadata.mirror_reference &&
				p_metadata.mirror_reference.length > 0
			)
			{
				let mirror_object = get_data_object_for_mirror(p_metadata.mirror_reference);

				if(mirror_object)
				{
					list_data = mirror_object;
				}
			}				
			list_render(result, p_metadata, list_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		case 'date':
			let date_data = p_data;

			if
			(
				p_metadata.mirror_reference &&
				p_metadata.mirror_reference.length > 0
			)
			{
				let mirror_object = get_data_object_for_mirror(p_metadata.mirror_reference);

				if(mirror_object)
				{
					date_data = mirror_object;
				}
			}
			date_render(result, p_metadata, date_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		case 'datetime':
			let datetime_data = p_data;

			if
			(
				p_metadata.mirror_reference &&
				p_metadata.mirror_reference.length > 0
			)
			{
				let mirror_object = get_data_object_for_mirror(p_metadata.mirror_reference);

				if(mirror_object)
				{
					datetime_data = mirror_object;
				}
			}			
			datetime_render(result, p_metadata, datetime_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		case 'time':
			let time_data = p_data;

			if
			(
				p_metadata.mirror_reference &&
				p_metadata.mirror_reference.length > 0
			)
			{
				let mirror_object = get_data_object_for_mirror(p_metadata.mirror_reference);

				if(mirror_object)
				{
					time_data = mirror_object;
				}
			}			
			time_render(result, p_metadata, time_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		case 'chart':
			chart_render(result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;		

		case 'hidden':
			hidden_render(result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		case 'jurisdiction':
			let jurisdiction_data = p_data;

			if
			(
				p_metadata.mirror_reference &&
				p_metadata.mirror_reference.length > 0
			)
			{
				let mirror_object = get_data_object_for_mirror(p_metadata.mirror_reference);

				if(mirror_object)
				{
					jurisdiction_data = mirror_object;
				}
			}
			user_jurisdiction_render(result, p_metadata, jurisdiction_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx);
			break;

		default:
			console.log("page_render not processed", p_metadata);
			break;
	}

	return result;
}

/*
function get_style_string(p_style_string)
{
	//"{"position":"absolute","top":12,"left":8,"height":50,"width":110.219,"font-weight":"400","font-size":"16px","font-style":"normal","color":"rgb(0, 0, 0)"}"
	var result = p_style_string.substring(1);

	result = result.replace(/["']/g, "").replace("{","").replace("}","").replace(/,/g, ";");

	return result;
}
*/

function get_grid_style_string(p_grid_style_string, p_style_string)
{
	var top_regex = /"top":(\d+\.?\d*),/;
	var left_regex = /"left":(\d+\.?\d*),/;
	var grid_top =  null;
	var grid_left = null;
	var other_top = null;
	var other_left = null;
	var new_top = null;
	var new_left = null;
	var match = top_regex.exec(p_grid_style_string);

	grid_top = new Number(match[1]);
	match = left_regex.exec(p_grid_style_string);
	grid_left = new Number(match[1]);
	match = top_regex.exec(p_style_string);
	other_top = new Number(match[1]);
	match = left_regex.exec(p_style_string);
	other_left = new Number(match[1]);
	//"{"position":"absolute","top":12,"left":8,"height":50,"width":110.219,"font-weight":"400","font-size":"16px","font-style":"normal","color":"rgb(0, 0, 0)"}"

	var result = p_style_string.substring(1);

	result = result.replace(/["']/g, "").replace("{","").replace("}","").replace(/,/g, ";");

	return result;
}


function convert_dictionary_path_to_array_field(p_path)
{
	//g_data.prenatal.routine_monitoring.systolic_bp
	//er_visit_and_hospital_medical_records/vital_signs/date_and_time
	//er_visit_and_hospital_medical_records[current_index]/vital_signs[]/date_and_time
	/* [
			er_visit_and_hospital_medical_records[current_index]/vital_signs,
			date_and_time
	 ]

	*/
	//g_data.er_visit_and_hospital_medical_records[current_index].vital_signs[].date_and_time
	//var temp = "g_data." + p_path.replace(new RegExp('/','gm'),".").replace(new RegExp('\\.(\\d+)\\.','gm'),"[$1].").replace(new RegExp('\\.(\\d+)$','g'),"[$1]");

	var result = []
	var temp = "g_data." + p_path.replace(new RegExp('/','gm'),".");
	var multi_form_check = temp.split(".") ;
	var check_path = eval(multi_form_check[0] + "." + multi_form_check[1]);

	if(Array.isArray(check_path))
	{
		var new_path = [];

		for(var i = 0; i < multi_form_check.length; i++)
		{
			
			if(i == 1)
			{
				new_path.push(multi_form_check[i] + "[" + $mmria.get_current_multiform_index() + "]");
			}
			else
			{
				new_path.push(multi_form_check[i]);
			}
		}
		var path = new_path.join('.');
		var index = path.lastIndexOf('.');

		result.push(path.substr(0, index));
		result.push(path.substr(index + 1, path.length - (index + 1)));
	}
	else
	{
		var index = temp.lastIndexOf('.');

		result.push(temp.substr(0, index));
		result.push(temp.substr(index + 1, temp.length - (index + 1)));
	}

	return result;
}


function convert_dictionary_path_to_lookup_object(p_path)
{
	//g_data.prenatal.routine_monitoring.systolic_bp
	var result = null;
	var temp_result = []
	var temp = "g_metadata." + p_path.replace(new RegExp('/','gm'),".").replace(new RegExp('\\.(\\d+)\\.','gm'),"[$1].").replace(new RegExp('\\.(\\d+)$','g'),"[$1]");
	var index = temp.lastIndexOf('.');

	temp_result.push(temp.substr(0, index));
	temp_result.push(temp.substr(index + 1, temp.length - (index + 1)));

	var lookup_list = eval(temp_result[0]);

	for(var i = 0; i < lookup_list.length; i++)
	{
		if(lookup_list[i].name == temp_result[1])
		{
			result = lookup_list[i].values;
			break;
		}
	}

	return result;
}

function page_render_create_input(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path, p_ctx, p_valid)
{
	var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];

    let is_highlight_border = false;

    if
    (
        p_metadata.type.toLowerCase() == 'string' &&
        p_metadata.max_length != null &&
        p_data != null &&
        p_data.toString().length >= parseInt(p_metadata.max_length) //&&
        //p_metadata.is_display_field_length != null &&
        //.is_display_field_length == true
    )
    {
       
       is_highlight_border = true;
    }
	
	if (!isNullOrUndefined(p_valid))
	{
		p_valid = p_valid;
		console.log('valid? ', p_valid)
	}

	// Buttons will have an outer div container with styles assigned to it
	// and not on the actual input control like everything else
	if (p_metadata.type === 'button') 
    {
		p_result.push
        (`
			<div data-render-type="${p_metadata.type.toLowerCase()}" class="row no-gutters align-items-center" style="${get_style_string(style_object.control.style)}">
		`);
	}
    else if (p_metadata.type === 'always_enabled_button') 
    {
		p_result.push
        (`
			<div data-render-type="${p_metadata.type.toLowerCase()}" class="row no-gutters align-items-center" style="${get_style_string(style_object.control.style)}">
		`);
	}

/*
	let disabled_html = " disabled = 'disabled' ";

	if(g_data_is_checked_out || p_metadata.type == 'always_enabled_button')
	{
		disabled_html = " ";
	}
*/
    let disabled_html = form_get_disabled(p_metadata, p_dictionary_path.substr(1));

	p_result.push("<input autocomplete=off ");
	p_result.push(` id="${convert_object_path_to_jquery_id(p_object_path)}_control" `);
	p_result.push(disabled_html);
		if
		(
			p_metadata.is_read_only && 	
			(
				p_metadata.is_read_only == true ||
				p_metadata.is_read_only == "true" ||
				(
					p_metadata.mirror_reference &&
					p_metadata.mirror_reference.length > 0
				)
			)
		)
		{
      p_result.push(" readonly='true' ");
		}

		p_result.push(" style='");
        if(is_highlight_border) p_result.push("border-color: #BB6C49;");

        if
        (
            style_object && 
            p_metadata.type !== 'button' &&
            p_metadata.type !== 'always_enabled_button' 
        )
        {
            p_result.push(get_style_string(style_object.control.style));
        }
    p_result.push("' ");

		p_result.push(" class='form-control ");
		  p_result.push(`${p_metadata.type.toLowerCase()} `);
		
		if
		(
			p_metadata.type.toLowerCase() == "number" && 	
			p_metadata.decimal_precision && 
			p_metadata.decimal_precision != ""
		)
		{
			p_result.push(p_metadata.decimal_precision);
		}

		if(p_metadata.type=="date") p_result.push(`date-control`);

        if(p_metadata.type=="time") p_result.push(`date-control`);

        

    if (p_metadata.type === 'date' && p_metadata.name === 'case_locked_date') p_result.push(` plain-date-control`);
		
		if(p_metadata.type=="button") p_result.push(` btn btn-secondary`);

        if(p_metadata.type=="always_enabled_button") p_result.push(` btn btn-secondary`);
		
		p_result.push("' dpath='");
		p_result.push(p_dictionary_path.substring(1, p_dictionary_path.length));

		if(p_ctx!=null)
		{
			if(p_ctx.form_index != null)
			{
				p_result.push("' form_index='");
				p_result.push(p_ctx.form_index);
			}

			if(p_ctx.grid_index != null)
			{
				p_result.push("' grid_index='");
				p_result.push(p_ctx.grid_index);
			}
		}

		if
        (
            p_metadata.type=="button" ||
            p_metadata.type=="always_enabled_button"
        )
		{
			p_result.push("' type='button' name='");
			p_result.push(p_metadata.name);
			p_result.push("' value='");
			p_result.push(p_metadata.prompt.replace(/'/g, "\\'"));
			p_result.push("' ");

			if(p_metadata.type == "")
			{
				p_result.push("placeholder='");
				if(p_metadata.prompt.length > 25)
				{
					p_result.push(p_metadata.prompt.substring(0, 25).replace(/'/g, "\\'"));
				}
				else
				{
					p_result.push(p_metadata.prompt.replace(/'/g, "\\'"));
				}
				
				p_result.push("' ");
			}

			let f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_ocl";

			if(path_to_onclick_map[p_metadata_path])
			{
				page_render_create_event(p_result, "onclick", p_metadata.onclick, p_metadata_path, p_object_path, p_dictionary_path, p_ctx);
			}
		}
		else
		{
			if(p_metadata.type.toLowerCase() == "hidden")
			{
				p_result.push("' type='hidden' name='");
			}
			else if (p_metadata.type.toLowerCase() == "date")
			{
				p_result.push("' maxlength='10' type='text' placeholder='mm/dd/yyyy' name='");
			// 	p_result.push("' type='date' min='1900-01-01' max='2100-12-31' name='");
			}
            else if (p_metadata.type.toLowerCase() == "string")
            {
                p_result.push("' ");
                if
                (
                    p_metadata.max_length != null &&
                    p_metadata.max_length != "" &&
                    !isNaN(p_metadata.max_length) && 
                    parseInt(Number(p_metadata.max_length)) == p_metadata.max_length && 
                    !isNaN(parseInt(p_metadata.max_length, 10))
                )
                {
                    p_result.push("maxlength=");
                    p_result.push(p_metadata.max_length);
                }
                p_result.push(" type='text' name='")
            }
			else
			{
				p_result.push("' type='text' name='");
            }
			
            p_result.push(p_metadata.name);
            p_result.push("' ");

            //p_result.push(`data-value='${p_data}'`);
      
			p_result.push(" value='");
            if(p_data || p_data == 0)
            { 
            if (typeof p_data === 'string' || p_data instanceof String)
            {
                if (p_metadata.type.toLowerCase() == "date")
                {
                    p_result.push
                    (
                        `${p_data.split('T')[0]}`
                    );
                }
                else
                {
                    p_result.push(p_data.replace(/'/g, "&apos;"));
                }
            }
            else
            {
                if (p_metadata.type.toLowerCase() == "date")
                {
                    p_result.push
                    (
                        `${p_data.split('T')[0]}`
                    );
                }
                else
                {
                    p_result.push(p_data);
                }
            }
        }

        p_result.push("' ");

        if
        (
            !(
                p_metadata.mirror_reference &&
                p_metadata.mirror_reference.length > 0
            )
        )
        {
            let f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_of";

            if(path_to_onfocus_map[p_metadata_path])
            {
                page_render_create_event(p_result, "onfocus", p_metadata.onfocus, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
            }

            f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_och";
            if(path_to_onchange_map[p_metadata_path])
            {
                page_render_create_event(p_result, "onchange", p_metadata.onchange, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
            }
            
            f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_ocl";
            if(path_to_onclick_map[p_metadata_path])
            {
                page_render_create_event(p_result, "onclick", p_metadata.onclick, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
            }
				
            page_render_create_onblur_event(p_result, p_metadata, p_metadata_path, p_object_path, p_dictionary_path, p_ctx);
		}
	}

    if 
    (
        p_metadata.type === 'date' && 
        p_metadata.name === 'case_locked_date'
    ) 
    {
        p_result.push(` disabled="true" `);
    }


	p_result.push("/>");
	
	if 
    (
        p_metadata.type == "button" ||
        p_metadata.type == "always_enabled_button" 

    )
	{
		p_result.push(`<span class="spinner-container spinner-small mt-2"><span class="spinner-body text-primary"><span class="spinner"></span><span class="spinnerinfo">Loading...</span></span></span>`);
		p_result.push("</div>");
	}
}

function page_render_create_event
(
	p_result, 
	p_event_name, 
	p_code_json, 
	p_metadata_path, 
	p_object_path, 
	p_dictionary_path, 
	p_ctx
)
{
	var post_fix = null;



	let is_parent_depended_upon = false;
	
	if(g_dependent_parent_to_child.has(p_dictionary_path.substr(1)))
	{
		is_parent_depended_upon = true;
	}

  /*
  var path_to_int_map = [];
  var path_to_onblur_map = [];
  var path_to_onclick_map = [];
  var path_to_onfocus_map = [];
  var path_to_onchange_map = [];
  var path_to_source_validation = [];
  var path_to_derived_validation = [];
  var path_to_validation_description = [];
  */

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
	code_array.push(", this")
	if(p_ctx!=null)
	{
		if(p_ctx.form_index != null)
		{
			code_array.push(", ");
			code_array.push(p_ctx.form_index);
		}

		if(p_ctx.grid_index != null)
		{
			code_array.push(", ");
			code_array.push(p_ctx.grid_index);
		}
	}
	
	if(is_parent_depended_upon)
	{
		p_result.push
	}

	code_array.push(");")
	code_array.push(`list_check_for_dependent_change(this, "${p_dictionary_path.substr(1)}");`)

	p_result.push(" ");
	p_result.push(p_event_name);
	p_result.push("='");
	p_result.push(code_array.join('').replace(/'/g,"\""));
	p_result.push("'");


}

function page_render_create_onblur_event(p_result, p_metadata, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
{
    /*
    var path_to_int_map = [];
    var path_to_onblur_map = [];
    var path_to_onclick_map = [];
    var path_to_onfocus_map = [];
    var path_to_onchange_map = [];
    var path_to_source_validation = [];
    var path_to_derived_validation = [];
    var path_to_validation_description = [];
    */
    var f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_ob";

	if(path_to_onblur_map[p_metadata_path])
	{
		//var source_code = escodegen.generate(p_metadata.onfocus);
		var code_array = [];
		
		code_array.push("(function x" + path_to_int_map[p_metadata_path].toString(16) + "_sob(p_control){\n");
		code_array.push("x" + path_to_int_map[p_metadata_path].toString(16) + "_ob");
		code_array.push(".call(");
		code_array.push(p_object_path.substring(0, p_object_path.lastIndexOf(".")));
		code_array.push(", p_control");
		code_array.push(");\n");
		
		code_array.push("g_set_data_object_from_path(\"");
		code_array.push(p_object_path);
		code_array.push("\",\"");
		code_array.push(p_metadata_path);
		code_array.push("\",\"");
		code_array.push(p_dictionary_path);
		code_array.push("\",p_control.value, null");
		if(p_ctx!=null)
		{
			if(p_ctx.form_index != null)
			{
				code_array.push(", ");
				code_array.push(p_ctx.form_index);
			}
	
			if(p_ctx.grid_index != null)
			{
				code_array.push(", ");
				code_array.push(p_ctx.grid_index);
			}
		}
		code_array.push(");\n}).call(");
		code_array.push(p_object_path.substring(0, p_object_path.lastIndexOf(".")));
		code_array.push(", event.target);");

		p_result.push(" onblur='");
		p_result.push(code_array.join('').replace(/'/g,"\""));
		p_result.push("'");
	}
	else //if(p_metadata.type!="number")
	{
		p_result.push(" onblur='g_set_data_object_from_path(\"");
		p_result.push(p_object_path);
		p_result.push("\",\"");
		p_result.push(p_metadata_path);
		p_result.push("\",\"");
        p_result.push(p_dictionary_path);
    
		if(p_metadata.type=="boolean")
		{
			p_result.push("\", this.checked");
        }
		else
		{
			p_result.push("\", this.value");
		}

		if(p_ctx!=null)
		{
			if(p_ctx.form_index != null)
			{
				p_result.push(", ");
				p_result.push(p_ctx.form_index);
			}
			else
			{
				p_result.push(", null");
			}
	
			if(p_ctx.grid_index != null)
			{
				p_result.push(", ");
				p_result.push(p_ctx.grid_index);
			}
			else
			{
				p_result.push(", null");
			}
		}

		p_result.push(")'");
	}
}

function page_render_create_onchange_event(p_result, p_metadata, p_metadata_path, p_object_path)
{
  /*
  var path_to_int_map = [];
  var path_to_onblur_map = [];
  var path_to_onclick_map = [];
  var path_to_onfocus_map = [];
  var path_to_onchange_map = [];
  var path_to_source_validation = [];
  var path_to_derived_validation = [];
  var path_to_validation_description = [];
  */

	var f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_och";
	if(path_to_onchange_map[p_metadata_path])
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
		code_array.push("\",\"");
		code_array.push(p_dictionary_path);
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
		p_result.push("\",\"");
		p_result.push(p_dictionary_path);
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

function page_render_create_checkbox(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
{
	p_result.push("<input  class='checkbox' type='checkbox' name='");
  p_result.push(p_metadata.name);
  
	if(p_data == true)
	{
		p_result.push("' checked='true'");
	}
	else
	{
		p_result.push("' value='");
	}
	p_result.push(p_data);
	p_result.push("' ");

	  var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];
    if(style_object)
    {
        p_result.push(" style='");
        p_result.push(get_style_string(style_object.control.style));
        p_result.push("'");
    }

	var f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_of";
	if(path_to_onfocus_map[p_metadata_path])
	{
		page_render_create_event(p_result, "onfocus", p_metadata.onfocus, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
	}

	f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_och";
	if(path_to_onchange_map[p_metadata_path])
	{
		page_render_create_event(p_result, "onchange", p_metadata.onchange, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
	}
	
	f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_ocl";
	if(path_to_onclick_map[p_metadata_path])
	{
		page_render_create_event(p_result, "onclick", p_metadata.onclick, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
	}
	
	page_render_create_onblur_event(p_result, p_metadata, p_metadata_path, p_object_path, p_dictionary_path, p_ctx);

	p_result.push("/>");
}

function page_render_create_textarea(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
{
	let disabled_html = " disabled ";

    let is_highlight_border = false;

    if
    (
        p_metadata.type.toLowerCase() == 'textarea' &&
        p_metadata.max_length != null &&
        p_data != null &&
        p_data.toString().length >= parseInt(p_metadata.max_length) //&&
        //p_metadata.is_display_field_length != null &&
        //.is_display_field_length == true
    )
    {
       
       is_highlight_border = true;
    }

	if(g_data_is_checked_out)
	{
		disabled_html = " ";
	}

	if(p_metadata.name == "case_opening_overview")
	{
		p_result.push(`<p class="mb-2">CTRL+B to bold, CTRL+I to italicize, CTRL+U to underline</p>`);
		p_result.push(`<textarea id='case_narrative_editor' ${disabled_html} name='`);
	}
	else
	{
		p_result.push(`<textarea id="${convert_object_path_to_jquery_id(p_object_path)}_control" ${disabled_html} name='`);
	}
    p_result.push(p_metadata.name);
    p_result.push("' ");

    p_result.push(" class='form-control ");
    p_result.push(p_metadata.type.toLowerCase());
    p_result.push("' ");

    if
    (
      p_metadata.is_read_only && 	
      (
        p_metadata.is_read_only == true ||
        p_metadata.is_read_only == "true"
      )
    )
    {
        p_result.push(" readonly='true' ");
    }

    if
    (
        p_metadata.type.toLowerCase() == 'textarea' &&
        p_metadata.max_length != null &&
        parseInt(p_metadata.max_length) > 0 &&
        p_metadata.is_display_field_length != null &&
        p_metadata.is_display_field_length == true
    )
    {
        p_result.push(` maxlength='${ parseInt(p_metadata.max_length)}' `);
    }

    var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];
    
    if(style_object && p_metadata.name != "case_opening_overview")
    {
        p_result.push(" style='");
        if(is_highlight_border) p_result.push("border-color: #BB6C49;");
        p_result.push(get_style_string(style_object.control.style));
        p_result.push("'");
    }

    var f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_of";
    
    if(path_to_onfocus_map[p_metadata_path])
    {
      page_render_create_event(p_result, "onfocus", p_metadata.onfocus, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
    }

    f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_ochs";
    if(path_to_onchange_map[p_metadata_path])
    {
      page_render_create_event(p_result, "onchange", p_metadata.onchange, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
    }

    if(p_metadata.name == "case_opening_overview")
    {
        p_result.push(" oninput='g_textarea_oninput(\"");
        p_result.push(p_object_path);
        p_result.push("\",\"");
        p_result.push(p_metadata_path);
        p_result.push("\",\"");
        p_result.push(p_dictionary_path);
        p_result.push("\",this.value)' ");
    }

    f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_ocl";
    
    if(path_to_onclick_map[p_metadata_path])
    {
      page_render_create_event(p_result, "onclick", p_metadata.onclick, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
    }
    
    page_render_create_onblur_event(p_result, p_metadata, p_metadata_path, p_object_path, p_dictionary_path, p_ctx);

    p_result.push(" >");
    p_result.push(p_data);

	p_result.push("</textarea>");
}

function convert_object_path_to_jquery_id(p_value)
{
	return p_value.replace(/\./g,"_").replace(/\[/g,"_").replace(/\]/g,"_")
}


function formatToTwoDigits(dateString) {
    
    if (!dateString || !dateString.length || dateString.length === 0 || dateString.indexOf("T") > 0) return dateString;

    const parts = dateString.split('-'); // Assuming 'YYYY-MM-DD'

    if (parts.length !== 3) return dateString;

    const year = parts[0];
    const month = parts[1];
    const day = parts[2];

    if (month.length === 1 || day.length === 1) {

        const formattedMonth = String(month).padStart(2, '0');
        const formattedDay = String(day).padStart(2, '0');

        return `${year}-${formattedMonth}-${formattedDay}`;
    }
    else {
        return dateString;
    }
}


function make_c3_date(p_value)
{   
    
    p_value = formatToTwoDigits(p_value)//check and and convert single digit day/month to two digit values
    
    if (p_value.indexOf('T') === -1)
        p_value += "T05:00:00.000Z";//specifying T00:00:00Z would be a subtraction from UTC possibly showing a prior date
    
	var date_time = new Date(p_value);
	var result = [];

	result.push(date_time.getFullYear());
	result.push("-");
	result.push(date_time.getMonth() + 1);
	result.push("-");
	result.push(date_time.getDate());

	result.push(" ");
	result.push(date_time.getHours());
	result.push(":");
	result.push(date_time.getMinutes());
	//result.push(":");
	//result.push(date_time.getSeconds());

	return result.join("");
}

function get_style_string(p_specicification_style_string)
{
    var result = [];
    var properly_formated_style = p_specicification_style_string;
    properly_formated_style = properly_formated_style.replace(/[{}]/g, ""); 
    properly_formated_style = properly_formated_style.replace(/['"]+/g, '');
    properly_formated_style = properly_formated_style.replace(/[,]+/g, ';');

    properly_formated_style = properly_formated_style.replace(/(\d+); (\d+); (\d+)/g, '$1, $2, $3');
    //"position:absolute;top:4;left:13;height:46px;width:146.188px;font-weight:400;font-size:16px;font-style:normal;color:rgb(33; 37; 41)"ui_specification
    var items = properly_formated_style.split(";")

    for(var i in items)
    {
        var pair = items[i].split(":");

        switch(pair[0].toLocaleLowerCase())
        {
            case "top":
            case "left":
            case "height":
            case "width":
            case "font-size":
                var value = pair[1].trim();
                if(/px$/.test(value))
                {
                    result.push(pair[0] + ":" + value);
                }
                else
                {
                    result.push(pair[0] + ":" + pair[1].trim() + "px");
                }
            break;

            default:
                result.push(pair.join(":"));
            break;
        }
    }

    return result.join(";");
}

function get_only_size_and_position_string(p_specicification_style_string)
{
    var result = [];

    var properly_formated_style = p_specicification_style_string;
    properly_formated_style = properly_formated_style.replace(/[{}]/g, ""); 
    properly_formated_style = properly_formated_style.replace(/['"]+/g, '');
    properly_formated_style = properly_formated_style.replace(/[,]+/g, ';');

    properly_formated_style = properly_formated_style.replace(/(\d+); (\d+); (\d+)/g, '$1, $2, $3');
    //"position:absolute;top:4;left:13;height:46px;width:146.188px;font-weight:400;font-size:16px;font-style:normal;color:rgb(33; 37; 41)"ui_specification
    var items = properly_formated_style.split(";")

    for(var i in items)
    {
        var pair = items[i].split(":");

        switch(pair[0].toLocaleLowerCase())
        {
            case "top":
            case "left":
            case "height":
            case "width":
                var value = pair[1].trim();
                if(/px$/.test(value))
                {
                    result.push(pair[0] + ":" + value);
                }
                else
                {
                    result.push(pair[0] + ":" + pair[1].trim() + "px");
                }
                break;
            case "position":
                result.push(pair.join(":"));
                break;
        }
    }

    return result.join(";");
}

function get_only_font_style_string(p_specicification_style_string)
{
    var result = [];
    var properly_formated_style = p_specicification_style_string;
    properly_formated_style = properly_formated_style.replace(/[{}]/g, ""); 
    properly_formated_style = properly_formated_style.replace(/['"]+/g, '');
    properly_formated_style = properly_formated_style.replace(/[,]+/g, ';');
    properly_formated_style = properly_formated_style.replace(/(\d+); (\d+); (\d+)/g, '$1, $2, $3');
    //"position:absolute;top:4;left:13;height:46px;width:146.188px;font-weight:400;font-size:16px;font-style:normal;color:rgb(33; 37; 41)"
    var items = properly_formated_style.split(";")

    for(var i in items)
    {
        var pair = items[i].split(":");
        switch(pair[0].toLocaleLowerCase())
        {
            case "font-size":
                var value = pair[1].trim();
                if(/px$/.test(value))
                {
                    result.push(pair[0] + ":" + value);
                }
                else
                {
                    result.push(pair[0] + ":" + pair[1].trim() + "px");
                }
                break;

            case "font-weight":
            case "color":
                result.push(pair.join(":"));
                break;
        }

    }

    return result.join(";");
}

function get_chart_size(p_style_string)
{

	var result = {
		height: 240,
		width: 480,
		top: 10,
		left:10
	};

	var top_regex = /"top":(\d+\.?\d*),/;
	var left_regex = /"left":(\d+\.?\d*),/;

	var width_regex = /"width":(\d+\.?\d*),/;
	var height_regex = /"height":(\d+\.?\d*),/;

	var outer_witdh =  null;
	var outer_height = null;
	var outer_top = null;
	var outer_left = null;

	var new_top = null;
	var new_left = null;
	var new_width = null;
	var new_height = null;

	var match = top_regex.exec(p_style_string);
	outer_top = new Number(match[1]);

	match = left_regex.exec(p_style_string);
	outer_left = new Number(match[1]);

	match = width_regex.exec(p_style_string);
	outer_witdh = new Number(match[1]);

	match = height_regex.exec(p_style_string);
	outer_height = new Number(match[1]);

	result.height = outer_height - 5;
	result.width = outer_witdh - 5;

	//"{"position":"absolute","top":12,"left":8,"height":50,"width":110.219,"font-weight":"400","font-size":"16px","font-style":"normal","color":"rgb(0, 0, 0)"}"

	return result;
}

function get_data_object_for_mirror(p_mirror_reference)
{
	return eval("g_data." + p_mirror_reference.replace(/\//g, "."));
}


function get_form_height_attribute_height(p_metadata, p_dictionary_path)
{
		let result = null;
		let height = null;
		let top = null;
		let child = p_metadata.children[p_metadata.children.length -1];
		let dictionary_path = p_dictionary_path + "/" + child.name;
		let style_object = g_default_ui_specification.form_design[dictionary_path.substring(1)];
		let specicification_style_string = style_object.control.style;
		let properly_formated_style = specicification_style_string;
		
    properly_formated_style = properly_formated_style.replace(/[{}]/g, ""); 
    properly_formated_style = properly_formated_style.replace(/['"]+/g, '');
    properly_formated_style = properly_formated_style.replace(/[,]+/g, ';');
    properly_formated_style = properly_formated_style.replace(/(\d+); (\d+); (\d+)/g, '$1, $2, $3');
    //"position:absolute;top:4;left:13;height:46px;width:146.188px;font-weight:400;font-size:16px;font-style:normal;color:rgb(33; 37; 41)"
		let items = properly_formated_style.split(";")
		let height_is_found = false;
		let top_is_found = false;
	
    for(let i = 0; i < items.length && (!height_is_found || !top_is_found); i++)
    {
		let value = null;
        let pair = items[i].split(":");
        switch(pair[0].toLocaleLowerCase())
        {
            case "height":
                value = pair[1].trim();
                if(/px$/.test(value))
                {
                    height = value.substring(0, str.length - 2);
                }
                else
                {
                    height = pair[1];
				}
				height_is_found = true;
				break;

			case "top":
				value = pair[1].trim();
				if(/px$/.test(value))
				{
					top = value.substring(0, str.length - 2);
				}
				else
				{
					top = pair[1];
				}
				top_is_found = true;
				break;
				
			default:
				break;
			}
    }

	if(height_is_found && top_is_found)
	{
		result = new Number(top) + new Number(height) + 35;

		result = result + "px";
	}

	return result;
}
