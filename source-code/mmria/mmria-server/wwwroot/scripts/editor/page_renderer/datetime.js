function datetime_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{
	/*
	if(typeof(p_data) == "string")
	{
		p_data = new Date(p_data);
	}*/
	p_result.push("<div class='datetime form-control-outer' id='");
	p_result.push(convert_object_path_to_jquery_id(p_object_path));
	p_result.push("' ");
	p_result.push(" mpath='");
	p_result.push(p_metadata_path);
	p_result.push("' ");
    p_result.push(">");
    
    p_result.push("<label ");
    if(p_metadata.description && p_metadata.description.length > 0)
    {
        p_result.push("rel='tooltip'  data-original-title='");
        p_result.push(p_metadata.description.replace(/'/g, "\\'"));
        p_result.push("'");
    }

    var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];

    if(style_object)
    {
        p_result.push(" style='");
        p_result.push(get_style_string(style_object.prompt.style));
        p_result.push("'");
    }
    p_result.push(">");
        p_result.push(p_metadata.prompt);
    p_result.push("</label>");

    p_result.push(`
        <div class="row no-gutters datetime-control"
                style="${style_object && get_style_string(style_object.control.style)}"
                dpath="${p_object_path}"
                ${p_ctx && p_ctx.form_index != null ? `form_index="${p_ctx.form_index && p_ctx.form_index}"` : ''}
                ${p_ctx && p_ctx.grid_index != null ? `grid_index="${p_ctx.grid_index && p_ctx.grid_index}"` : ''}
        >
    `);

    let disabled_html = " disabled = 'disabled' ";
    if(g_data_is_checked_out)
    {
        disabled_html = " ";
    }
    // console.log(g_data_is_checked_out);

    /*
"is_valid_date_or_datetime": valid_date_or_datetime,
"entered_date_or_datetime_value":entered_date_or_datetime_value
*/

    let is_valid = true;
    
    let date_part_display_value = "";
    let time_part_display_value = '00:00:00';
    
    if(p_ctx && p_ctx.hasOwnProperty("is_valid_date_or_datetime"))
    {
        is_valid = p_ctx.is_valid_date_or_datetime;

        
    }
    else if(p_data != null && p_data != "")
    {
        is_valid = is_valid_datetime(p_data);
    }

    if(! is_valid)
    {
        if(p_ctx && p_ctx.hasOwnProperty("is_valid_date_or_datetime"))
        {
            g_ui.broken_rules[convert_object_path_to_jquery_id(p_object_path)] = `$('#validation_summary_list').append('<li><strong>${p_metadata.prompt} ${p_ctx.entered_date_or_datetime_value}:</strong> Date must be a valid calendar date between 1900-2100 <button class="btn anti-btn ml-1"  onclick="gui_remove_broken_rule(\\'${convert_object_path_to_jquery_id(p_object_path)}\\')"><span class="sr-only">Remove Item</span><span class="x20 cdc-icon-close"></span></button></li>');`;
        }
        else
        {
            g_ui.broken_rules[convert_object_path_to_jquery_id(p_object_path)] = `$('#validation_summary_list').append('<li><strong>${p_metadata.prompt} ${p_data}:</strong> Date must be a valid calendar date between 1900-2100 <button class="btn anti-btn ml-1"  onclick="gui_remove_broken_rule(\\'${convert_object_path_to_jquery_id(p_object_path)}\\')"><span class="sr-only">Remove Item</span><span class="x20 cdc-icon-close"></span></button></li>');`;
        }
    }
    


    if(p_data != null && p_data != "")
    {
        if(p_data.indexOf("T"))
        {
            const date_time_object = new Date(p_data);

            date_part_display_value = 
            (date_time_object.getMonth() + 1) + "/" + 
            date_time_object.getDate() + "/" + 
            date_time_object.getFullYear();

            time_part_display_value = ("00" + date_time_object.getHours()).slice(-2) + ":" 
            + ("00" + date_time_object.getMinutes()).slice(-2) 
            + ":" + ("00" + date_time_object.getSeconds()).slice(-2); 

        }
    }
    
 

    p_result.push
    (
        `<input id="${convert_object_path_to_jquery_id(p_object_path)}-date" class="datetime-date form-control w-50 h-100"
        dpath="${p_object_path}"
        ${p_ctx && p_ctx.form_index != null ? `form_index="${p_ctx.form_index && p_ctx.form_index}"` : ''}
        ${p_ctx && p_ctx.grid_index != null ? `grid_index="${p_ctx.grid_index && p_ctx.grid_index}"` : ''}
        type="text"
        name="${p_metadata.name}"
        data-value="${p_data}"
        maxlength="10"
        value="${date_part_display_value}"
        placeholder="mm/dd/yyyy"
        ${disabled_html}`
    );


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
            create_datetime_event(p_result, "onfocus", p_metadata.onfocus, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
        }

        f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_och";
        if(path_to_onchange_map[p_metadata_path])
        {
            create_datetime_event(p_result, "onchange", p_metadata.onchange, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
        }
        
        f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_ocl";
        if(path_to_onclick_map[p_metadata_path])
        {
            create_datetime_event(p_result, "onclick", p_metadata.onclick, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
        }

        create_onblur_datetime_event(p_result, p_metadata, p_metadata_path, p_object_path, p_dictionary_path, p_ctx);
    }
    p_result.push(` min="1900-01-01" max="2100-12-31">`);
        

      
    p_result.push(
`<input id="${convert_object_path_to_jquery_id(p_object_path)}-time" class="datetime-time form-control w-50 h-100"
                dpath="${p_object_path}"
                ${p_ctx && p_ctx.form_index != null ? `form_index="${p_ctx.form_index && p_ctx.form_index}"` : ''}
        ${p_ctx && p_ctx.grid_index != null ? `grid_index="${p_ctx.grid_index && p_ctx.grid_index}"` : ''}
        type="text" name="${p_metadata.name}"
        data-value="${p_data}"
        placeholder="hh:mm:ss"
        maxlength="8"
        value="${time_part_display_value}"
        ${disabled_html}`
        // `<input class="datetime-time form-control w-50 h-100 input-group bootstrap-timepicker timepicker"
        //    dpath="${p_object_path}"
        //    ${p_ctx && p_ctx.form_index != null ? `form_index="${p_ctx.form_index && p_ctx.form_index}"` : ''}
        //    ${p_ctx && p_ctx.grid_index != null ? `grid_index="${p_ctx.grid_index && p_ctx.grid_index}"` : ''}
        //    type="text" name="${p_metadata.name}"
        //    data-value="${p_data}"
        //    value="${newData.split('T')[0] ? newTimeValue : '00:00:00'}"
        //    ${disabled_html}`
    );

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
            create_datetime_event(p_result, "onfocus", p_metadata.onfocus, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
        }

        f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_och";
        if(path_to_onchange_map[p_metadata_path])
        {
            create_datetime_event(p_result, "onchange", p_metadata.onchange, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
        }
        
        f_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_ocl";
        if(path_to_onclick_map[p_metadata_path])
        {
            create_datetime_event(p_result, "onclick", p_metadata.onclick, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
        }

        create_onblur_datetime_event(p_result, p_metadata, p_metadata_path, p_object_path, p_dictionary_path, p_ctx);
    }
    p_result.push(`>`);

    //TODO: Future route to implement a fake time control for easier 'hh:mm:ss' placeholder
    //at the moment our plugin is not playing nicely with the time control
    // p_result.push(
    // 	`<input class="datetime-fauxtime form-control w-50 h-100 input-group bootstrap-timepicker timepicker"
    // 					placeholder="hh:mm:ss"
    // 					${disabled_html}
    // 					aria-hidden="true"
    // 					focusable="false">`
    // );

    let validation_top = get_style_string(style_object.control.style).split('top:').pop().split('px;')[0];
    let validation_height = get_style_string(style_object.control.style).split('height:').pop().split('px;')[0];
    let validation_height_new = 'auto';
    let validation_top_new = 'auto';
    let validation_bottom_new = '-24px';
    let validation_left_new = 'auto';

    if(! is_valid)
    {
        p_result.push(`<small class="validation-msg text-danger" style="${get_style_string(style_object.control.style)}; height:${validation_height_new}; top: ${validation_top_new}; bottom: ${validation_bottom_new}; left: ${validation_left_new};">Invalid date and time</small>`);
    }

	p_result.push("</div>");

		//Initialize the custom 'bootstrap timepicker'
    p_post_html_render.push(`
      $("#${convert_object_path_to_jquery_id(p_object_path)}-date").datetimepicker({
        format: 'MM/DD/YYYY',
        keepInvalid: true,
        useCurrent: false,
        icons: {
          up: "x16 cdc-icon-chevron-circle-up-light",
          down: "x16 cdc-icon-chevron-circle-down-light",
          previous: 'x16 cdc-icon-chevron-double-right',
          next: 'x16 cdc-icon-chevron-double-right'
        }
      });

      $("#${convert_object_path_to_jquery_id(p_object_path)}-time").datetimepicker({
        format: 'HH:mm:ss',
        keepInvalid: true,
        useCurrent: false,
        icons: {
          up: "x16 cdc-icon-chevron-circle-up-light",
          down: "x16 cdc-icon-chevron-circle-down-light",
          previous: 'x16 cdc-icon-chevron-double-right',
          next: 'x16 cdc-icon-chevron-double-right'
        }
      });
    `);


    p_post_html_render.push(`document.getElementById("${convert_object_path_to_jquery_id(p_object_path)}-date").onkeypress = date_field_key_press;`);
    p_post_html_render.push(`document.getElementById("${convert_object_path_to_jquery_id(p_object_path)}-time").onkeypress = time_field_key_press;`);

	p_result.push("</div>");
}

//Custom function to create onblur event ONLY on datetime control
function create_onblur_datetime_event(p_result, p_metadata, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
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
	var t_name = "x" + path_to_int_map[p_metadata_path].toString(16) + "_ob";

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
	else 
	{
		//TODO: Refactor the below condition once we figure how to write 'nested' ternary operators
		if (p_ctx)
		{
			//p_ctx exits, setting form_index and grid_index value
			p_result.push(
				` onblur="DateTime_Onblur('${p_object_path}', '${p_metadata_path}', '${p_dictionary_path}', ${p_ctx.form_index}, ${p_ctx.grid_index})"`
			);
		}
		else
		{
			//no p_ctx arguments, setting form_index and grid_index value to 'null'
			p_result.push(
				` onblur="DateTime_Onblur('${p_object_path}', '${p_metadata_path}', '${p_dictionary_path}', null, null)"`
			);
		}
	}
}

//Custom function to create events ONLY on datetime control
function create_datetime_event(p_result, p_event_name, p_code_json, p_metadata_path, p_object_path, p_dictionary_path, p_ctx)
{
	var post_fix = null;

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
	
	code_array.push(");")

	p_result.push(" ");
	p_result.push(p_event_name);
	p_result.push("='");
	p_result.push(code_array.join('').replace(/'/g,"\""));
	p_result.push("'");
}


function DateTime_Onblur
(
    p_object_path,
    p_metadata_path,
    p_dictionary_path,
    p_form_index,
    p_grid_index
)
{
    let value = "";

    let object_id = convert_object_path_to_jquery_id(p_object_path)

    let date_element = document.querySelector(`#${object_id} .datetime-date`);
    let time_element = document.querySelector(`#${object_id} .datetime-time`);
    
    let date_value = date_element.value;
    let time_value = time_element.value;
    
    let date_part_array = date_value.split("/")
    if(date_value != null && date_value != "")
    {
        if(date_part_array.length > 2)
        {
            date_value = date_part_array[2] + "-" + date_part_array[0] + "-" + date_part_array[1];
        }

        value = date_value + "T" + time_value;
    }
 
    g_set_data_object_from_path
    (
        p_object_path,
        p_metadata_path,
        p_dictionary_path,
        value,
        p_form_index,
        p_grid_index
    );
}

function time_field_key_press(e) 
{
    var chr = String.fromCharCode(e.which);
    if ("0123456789:".indexOf(chr) < 0)
    {
        return false;
    }
}

function convert_datetime_to_local_display_value(p_value)
{
    let result = p_value;

    if(p_value && p_value!= "")
    {
        if(typeof p_value.getMonth === 'function')
        {
            result = 
            (p_value.getMonth() + 1) + "/" + 
            p_value.getDate() + "/" + 
            p_value.getFullYear() + " "  +  ("00" + p_value.getHours()).slice(-2) + ":" 
            + ("00" + p_value.getMinutes()).slice(-2) 
            + ":" + ("00" + p_value.getSeconds()).slice(-2); 
        }
        else if(p_value && p_value.indexOf("T") > -1)
        {
            let date_time_object = new Date(p_value);
            result = 
            (date_time_object.getMonth() + 1) + "/" + 
            date_time_object.getDate() + "/" + 
            date_time_object.getFullYear() + " "  +  ("00" + date_time_object.getHours()).slice(-2) + ":" 
            + ("00" + date_time_object.getMinutes()).slice(-2) 
            + ":" + ("00" + date_time_object.getSeconds()).slice(-2); 
        }
    }

    return result;
}



function is_valid_datetime(p_value) 
{

    let is_valid_date = false;
    let is_valid_time = false;


    if (p_value === '' || p_value.length === 0) 
    {
        is_valid_date = true;
        is_valid_time = true;
    } 
    else if(p_value.indexOf('T') > -1)
    {
        let split_array = p_value.split('T'); 
        if(split_array.length > 1)
        {
            let date_component = split_array[0];
            let time_component =  split_array[1];

            if(date_component.indexOf("-") > -1)
            {
                let value_array = date_component.split("-");

                if(value_array.length > 2)
                {
                    let year = parseInt(value_array[0]);
                    let month = parseInt(value_array[1]);
                    let day = parseInt(value_array[2]);

                    if 
                    (
                        year >= 1900 && 
                        year <= 2100 &&
                        month >= 1 &&
                        month <= 12 &&
                        day >= 1 &&
                        day <= 31
                    ) 
                    {
                        is_valid_date = true;
                    }
                }

                if(time_component.indexOf(":") > -1)
                {
                    let value_array = time_component.split(":");
                    if(value_array.length > 2)
                    {
                        let hour = parseInt(value_array[0]);
                        let minute = parseInt(value_array[1]);
                        let second = 0;
                        let millisecond = null;
                        if(value_array[2].indexOf(".") > -1)
                        {
                            let second_array = value_array[2].split(".");
                            second = parseInt(second_array[0]);
                            millisecond = parseInt(second_array[1]);
                        }
                        else
                        {
                            second = parseInt(value_array[2]);
                        }

                        if 
                        (
                            hour >= 0 && 
                            hour <= 23 &&
                            minute >= 0 &&
                            minute <= 59 &&
                            second >= 0 &&
                            second <= 59
                        ) 
                        {
                            if
                            (
                                millisecond != null &&
                                millisecond >=0 &&
                                millisecond <= 999
                            )
                            {
                                is_valid_time = true;
                            }
                            else
                            {
                                is_valid_time = true;
                            }
                        }
                        
                    }
                }
            }
        }
    }

    return is_valid_date && is_valid_time;

}