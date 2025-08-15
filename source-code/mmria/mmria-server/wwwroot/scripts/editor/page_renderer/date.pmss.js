function date_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{

    p_result.push("<div id='");
    p_result.push(convert_object_path_to_jquery_id(p_object_path));
    
    p_result.push("' ");
    p_result.push(" mpath='");
    p_result.push(p_metadata_path);
    p_result.push("' ");

    p_result.push(">");
        p_result.push(`<label for="${convert_object_path_to_jquery_id(p_object_path)}_control" `);
        if(p_metadata.description && p_metadata.description.length > 0)
        {
            p_result.push("rel='tooltip' data-original-title='");
            p_result.push(p_metadata.description.replace(/'/g, "\\'"));
            p_result.push("'");
        }

        let style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];
        
        if(style_object)
        {
            p_result.push(" style='");
            p_result.push(get_style_string(style_object.prompt.style));
            p_result.push("'");
        }
        p_result.push(">");

        p_result.push(p_metadata.prompt);

        var lookup_path = p_dictionary_path;

        if
        (
            p_metadata.mirror_reference != null &&
            p_metadata.mirror_reference.length > 2
        )
        {
            lookup_path = "/"  + p_metadata.mirror_reference;
        }

        p_result.push
        (
            `${g_is_data_analyst_mode? render_data_analyst_dictionary_link
            (
                p_metadata, 
                lookup_path
            ) : ""}`
        );

        p_result.push("</label> ");
        
        let is_valid = true;



        if(p_ctx && p_ctx.hasOwnProperty("is_valid_date_or_datetime"))
        {
          is_valid = p_ctx.is_valid_date_or_datetime;
        }
        else if(p_data != null && p_data != "")
        {
            is_valid = is_valid_date(p_data);
        }

        if(!is_valid)
        {
            let form_html = '';
            let grid_html = '';

            if(p_ctx && p_ctx.hasOwnProperty("is_valid_date_or_datetime"))
            {
                /*
                if(p_ctx.form_index != null)
                {
                    form_html = ` Form Item(${p_ctx.form_index + 1}) `;
                }

                if(p_ctx.grid_index != null)
                {
                    grid_html = ` Grid Item(${p_ctx.grid_index + 1}) `;
                }
                */

                g_ui.broken_rules[convert_object_path_to_jquery_id(p_object_path)] = `$('#validation_summary_list').append('<li><strong>${p_metadata.prompt} ${p_ctx.entered_date_or_datetime_value}:</strong> This invalid date has been cleared in the form below. Please enter a valid calendar date between 1/1/1900 and 12/31/2100 in mm/dd/yyyy format. <button class="btn anti-btn ml-1" onclick="gui_remove_broken_rule_click(\\'${convert_object_path_to_jquery_id(p_object_path)}\\')"><span class="sr-only">Remove Item</span><span class="x20 cdc-icon-times-solid"></span></button></li>');`;
            }
            else
            {
                g_ui.broken_rules[convert_object_path_to_jquery_id(p_object_path)] = `$('#validation_summary_list').append('<li><strong>${p_metadata.prompt} ${p_data}:</strong> This invalid date has been cleared in the form below. Please enter a valid calendar date between 1/1/1900 and 12/31/2100 in mm/dd/yyyy format. <button class="btn anti-btn ml-1" onclick="gui_remove_broken_rule_click(\\'${convert_object_path_to_jquery_id(p_object_path)}\\')"><span class="sr-only">Remove Item</span><span class="x20 cdc-icon-times-solid"></span></button></li>');`;
            }

          p_post_html_render.push(`$('${convert_object_path_to_jquery_id(p_object_path)} .date-control').addClass('is-invalid');`);
        }
        else
        {
            //p_post_html_render.push(`gui_remove_broken_rule('${convert_object_path_to_jquery_id(p_object_path)}')`);
        }
        
        let input_value = p_data;

        if(p_data !=null && p_data != "")
        {
            input_value = convert_date_to_local_display_value(p_data);
        }
        
        page_render_create_input(p_result, p_metadata, input_value, p_metadata_path, p_object_path, p_dictionary_path, p_ctx);

        p_result.push(
          `<div class="input-group-addon">
            <span class="glyphicon glyphicon-th"></span>
          </div>`
        );
/*
        p_post_html_render.push(`
          $('#g_data_tracking_case_status_case_locked_date input').hide();

          if (${
            g_data.tracking.case_status.case_locked_date !== null &&
            g_data.tracking.case_status.case_locked_date.length > 0 &&
            g_data.tracking.case_status.case_locked_date !== ''
            })
          {
            $('#g_data_tracking_case_status_case_locked_date input').show();
          }
        `);*/

        let validation_top = get_style_string(style_object.control.style).split('top:').pop().split('px;')[0];
        let validation_height = get_style_string(style_object.control.style).split('height:').pop().split('px;')[0];
        let validation_height_new = 'auto';
        let validation_top_new = parseInt(validation_top) + parseInt(validation_height) + 4;

        if(! is_valid)
        {
            p_result.push(`<small id="${convert_object_path_to_jquery_id(p_object_path)}-inline-validation-message" class="validation-msg text-danger" style="${get_style_string(style_object.control.style)}; height:${validation_height_new}; top:${validation_top_new}px">Invalid date</small>`);
        }
        
        p_post_html_render.push(`
          $("#${convert_object_path_to_jquery_id(p_object_path)} input").datetimepicker({
            format: 'MM/DD/YYYY',
            keepInvalid: true,
            useCurrent: false,
            useStrict: true,
            icons: {
              time: "x24 cdc-icon-clock_01",
              date: "x24 cdc-icon-calendar_01",
              up: "x24 cdc-icon-chevron-double-right",
              down: "x24 cdc-icon-chevron-double-right",
              previous: 'x16 cdc-icon-chevron-double-right',
              next: 'x16 cdc-icon-chevron-double-right'
            }
          })
        `);

        p_post_html_render.push(`
          if (${!is_valid}) {
            $("#${convert_object_path_to_jquery_id(p_object_path)} input").addClass('is-invalid')
          } else {
            $("#${convert_object_path_to_jquery_id(p_object_path)} input").removeClass('is-invalid')
          }
        `);

        p_post_html_render.push(`
          document.getElementById("${convert_object_path_to_jquery_id(p_object_path)}").onkeypress = date_field_key_press;
        `);


    p_result.push("</div>");
}

function date_field_key_press(e) 
{
    let chr = String.fromCharCode(e.which);
    if (chr == 9) 
    {
        return true;
    }
    else if ("0123456789/".indexOf(chr) < 0)
    {
        return false;
    }
}

function convert_date_to_local_display_value(p_value)
{
    let result = p_value;

    if(p_value && p_value!= "")
    {
        if(typeof p_value.getMonth === 'function')
        {
            result = 
            (p_value.getMonth() + 1) + "/" + 
            p_value.getDate() + "/" + 
            p_value.getFullYear(); 
        }
        else if(p_value && p_value.indexOf("T") > -1)
        {
            let date_time_object = new Date(p_value);
            result = 
            (date_time_object.getMonth() + 1) + "/" + 
            date_time_object.getDate() + "/" + 
            date_time_object.getFullYear(); 
        }
        else if(p_value.indexOf("-") > -1)
        {
            let date_array = p_value.split("-")
            if(date_array.length > 2)
            {
                /*
                let date_time_object = new Date(p_value);
                date_time_object.getFullYear() + "-" +
                (date_time_object.getMonth() + 1) + "-" + 
                date_time_object.getDate(); 
                */
                result = 
                ("00" + date_array[1]).slice(-2) + "/" +
                ("00" + date_array[2]).slice(-2) + "/" +
                date_array[0];
                

            }

        }
        else if(p_value.indexOf("/") > -1)
        {
            let date_time_object = new Date(p_value);
            result = 
            (date_time_object.getMonth() + 1) + "/" + 
            date_time_object.getDate() + "/" + 
            date_time_object.getFullYear(); 
        }
    }

    return result;
}

function convert_date_to_storage_format(p_value)
{
    let result = p_value;

    if(p_value && p_value!= "")
    {
        if(typeof p_value.getMonth === 'function')
        {
            result = 
            p_value.getFullYear() + "-" +
            (p_value.getMonth() + 1) + "-" + 
            p_value.getDate(); 
        }
        else if(p_value && p_value.indexOf("T") > -1)
        {
            let date_time_object = new Date(p_value);
            result = 
            date_time_object.getFullYear() + "-" +
            (date_time_object.getMonth() + 1) + "-" + 
            date_time_object.getDate(); 
        }
        else if(p_value.indexOf("-") > -1)
        {
            result = p_value;
            /*
            let date_time_object = new Date(p_value);
            result = 
            date_time_object.getFullYear() + "-" +
            (date_time_object.getMonth() + 1) + "-" + 
            date_time_object.getDate(); 
            */
        }
        else if(p_value.indexOf("/") > -1)
        {
            let date_time_object = new Date(p_value);
            result = 
            date_time_object.getFullYear() + "-" +
            (date_time_object.getMonth() + 1) + "-" + 
            date_time_object.getDate(); 
        }
    }

    return result;
}


function is_valid_date(p_value) 
{
    let result = false; //flag set false by default, we will validate against this

    if (p_value.length === 0 || p_value === '')
    {
        result = true;
    } 
    else if(p_value.indexOf("/") < 0 && p_value.indexOf("-") < 0)
    {
        result = false;
    }
    else if (p_value.indexOf("/") > -1)
    {
        let value_array = p_value.split('/'); //get year and convert to a number

        if(value_array.length > 2)
        {
            let year = parseInt(value_array[2]);
            let month = parseInt(value_array[0]);
            let day = parseInt(value_array[1]);

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
                result = true;
            }
        }
    } 
    else if(p_value.indexOf('T') > -1)
    {
        let date_array = p_value.split('T')[0]; 
        if(date_array.indexOf("-") > -1)
        {
            let value_array = date_array.split("-");

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
                    result = true;
                }
            }
        }

    }
    else if(p_value.indexOf('-') > -1)
    {

        let value_array = p_value.split("-");

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
                result = true;
            }
        }
        

    }

    return result;
}

function datetimepicker_onselect(p_value, p_old)
{
 
     console.log("Selected date: " + p_value + "; input's current value: " + this.value);
   
}