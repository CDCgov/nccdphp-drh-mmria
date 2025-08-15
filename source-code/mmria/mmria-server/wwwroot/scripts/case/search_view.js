function get_seach_text_context(p_result, p_post_html_render, p_metadata, p_data, p_path, p_metadata_path, p_object_path, p_search_text, p_is_read_only, p_form_index, p_grid_index, p_valid_date_or_datetime, p_entered_date_or_datetime_value)
{
    let result = {
        result : p_result,
        post_html_render: p_post_html_render,
        metadata:p_metadata, 
        
        data:p_data, 
        mmria_path:p_path,
        metadata_path:p_metadata_path,
        object_path:p_object_path,
        search_text:p_search_text,
        form_index: p_form_index,
        grid_index: p_grid_index,
        is_read_only: p_is_read_only,

        is_valid_date_or_datetime: p_valid_date_or_datetime,
        entered_date_or_datetime_value: p_entered_date_or_datetime_value

    };

    return result;
}

function search_text_change(event)
{
    let search_text = event.target.value;
    let record_index = g_ui.url_state.path_array[0];

    if(search_text != null && search_text.length >= 3)
    {
        window.location.hash = "/" + record_index + "/field_search/" + search_text;

        // when searching, scroll back to top
        window.scrollTo(0,0);
    }
}

function render_search_text(p_ctx)
{
    switch(p_ctx.metadata.type.toLocaleLowerCase())
    {
        case "app":
            for(let i = 0; i < p_ctx.metadata.children.length; i++)
            {
                let child = p_ctx.metadata.children[i];
                if(p_ctx.data && child.type.toLocaleLowerCase() == "form")
                {
                    let new_context = get_seach_text_context(p_ctx.result, p_ctx.post_html_render, child, p_ctx.data[child.name], p_ctx.mmria_path+ "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path + "." + child.name, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
                    render_search_text(new_context);
                }
            }
            break;
        case "form":
            if(p_ctx.metadata.cardinality == "1" || p_ctx.metadata.cardinality == "?")
            {
                for(let i = 0; i < p_ctx.metadata.children.length; i++)
                {
                    let child = p_ctx.metadata.children[i];

                    if(p_ctx.data && p_ctx.data[child.name])
                    {
                        let new_context = get_seach_text_context(p_ctx.result, p_ctx.post_html_render, child, p_ctx.data[child.name], p_ctx.mmria_path+ "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path + "." + child.name, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
                        render_search_text(new_context);
                    }
                    
                }
            }
            else // multiform
            {

                for(let row = 0; row < p_ctx.data.length; row++)
                {
                    let row_data = p_ctx.data[row]
                    for(let i = 0; i < p_ctx.metadata.children.length; i++)
                    {
                        let child = p_ctx.metadata.children[i];
    
                        if(row_data)
                        {
                            let new_context = get_seach_text_context(p_ctx.result, p_ctx.post_html_render, child, row_data[child.name], p_ctx.mmria_path + "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path + "[" + row + "]." + child.name, p_ctx.search_text, p_ctx.is_read_only, row, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
                            new_context.multiform_index = row;
                            render_search_text(new_context);
                        }
                        
                    }
                }
            }
            break;
        case "group":
            for(let i = 0; i < p_ctx.metadata.children.length; i++)
            {
                let child = p_ctx.metadata.children[i];
                if(p_ctx.data)
                {
                    let new_context = get_seach_text_context(p_ctx.result, p_ctx.post_html_render, child, p_ctx.data[child.name], p_ctx.mmria_path+ "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path + "." + child.name, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
                    render_search_text(new_context);
                }
            }
            break;
        case "grid":
            for(let i = 0; i < p_ctx.data.length; i++)
            {
                let row_item = p_ctx.data[i];
                for(let j in p_ctx.metadata.children)
                {
                    let child = p_ctx.metadata.children[j];
                    
                    let new_context = get_seach_text_context(p_ctx.result, p_ctx.post_html_render, child, row_item[child.name], p_ctx.mmria_path + "/" + child.name, p_ctx.metadata_path  + ".children[" +j + "]", p_ctx.object_path + "[" + i + "]." + child.name, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, i, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);
                    render_search_text(new_context);
                }
            
            }
            break;
        case "string":
        case "number":
        case "time":
            if(p_ctx.metadata.prompt.toLocaleLowerCase().search(p_ctx.search_text.toLocaleLowerCase()) > -1)
            {
                render_search_text_input_control(p_ctx);
            }
            break;
        case "date":
            if(p_ctx.metadata.prompt.toLocaleLowerCase().search(p_ctx.search_text.toLocaleLowerCase()) > -1)
            {
                renderSearchDateControl(p_ctx);
            }
            break;
        case "datetime":
            if(p_ctx.metadata.prompt.toLocaleLowerCase().search(p_ctx.search_text.toLocaleLowerCase()) > -1)
            {
                renderSearchDateTimeControl(p_ctx);
            }
            break;
        case "list":
            if(p_ctx.metadata.prompt.toLocaleLowerCase().search(p_ctx.search_text.toLocaleLowerCase()) > -1)
            {
                render_search_text_select_control(p_ctx);
            }
            break;
        case "textarea":
            if(p_ctx.metadata.prompt.toLocaleLowerCase().search(p_ctx.search_text.toLocaleLowerCase()) > -1)
            {
                render_search_text_textarea_control(p_ctx);
            }  
            break;
    }
}

function render_search_text_input_control(p_ctx)
{   
    let control_type = p_ctx.metadata.type;
    let result = p_ctx.result;
    let style_object = g_default_ui_specification.form_design[p_ctx.mmria_path.substring(1)];
    let style_string = get_only_size_and_font_style_string(style_object.prompt.style);
    let control_string = get_only_size_and_font_style_string(style_object.control.style);

    result.push(`<div id="${convert_object_path_to_jquery_id(p_ctx.object_path)}" metadata="${p_ctx.mmria_path}" class="form-group mb-4">`);
        result.push("<p>");
            //result.push(p_ctx.mmria_path.substring(1).replace(/\//g, " > "));
            let path_items = p_ctx.mmria_path.split("/");

            for(let i = 1; i < path_items.length; i++)
            {
                let item = path_items[i];

                if(i === 1)
                {
                    let array = window.location.href.split("/field_search/");
                    //window.location.hash = "/" + record_index + "/field_search/" + search_text;
                    let link_url = array[0] + "/" + item;

                    result.push(`<a href='${link_url}'>${item}</a>`);
                }
                else
                {
                    result.push(" > ");
                    result.push(item);
                }
            }
        result.push("</p>");

        //style attr uses 'Short-Circuit Conditional' to clean up JS a bit
        //https://codeburst.io/javascript-short-circuit-conditionals-bbc13ac3e9eb
        result.push(`
            <label class="row no-gutters w-auto h-auto" for="${p_ctx.mmria_path.replace(/\//g, "--")}" 
                ${
                    style_object && style_object.prompt && style_object.prompt.style && `style="${style_string}"`
                }
            >${p_ctx.metadata.prompt}
            

                ${render_data_analyst_dictionary_link
                (
                    p_ctx.metadata, 
                    p_ctx.mmria_path
                )}
            
            
            </label>
        `);

            
        result.push("<input id='");
            result.push(convert_object_path_to_jquery_id(p_ctx.object_path));

            result.push("_input' class='form-control' ");

            if (control_type === 'date')
            {
                result.push(`type="date" min="1900-01-01" max="2100-12-31"`);
            }
            else
            {
                result.push("type='text'");
            }

            result.push(" style='");

            if
            (
                style_object &&
                style_object.control &&
                style_object.control.style
            )
            result.push(control_string);
            result.push("' value='"); 
            result.push(p_ctx.data); 
            result.push("' "); 

            switch (p_ctx.metadata.type.toLocaleLowerCase())
            {
                case "date":
                    result.push(" class='date' ");
                    break;
                case "datetime":
                    result.push(" class='datetime' ");
                    break;
                case "time":
                    result.push(" class='time' ");
                    break;
            }

            if
            (
                (
                    p_ctx.metadata.is_read_only != null &&
                    p_ctx.metadata.is_read_only == true
                ) ||
                p_ctx.metadata.mirror_reference ||
                p_ctx.is_read_only
            )
            {
                result.push(" readonly=true ");
            }
            else
            {

            let f_name = "x" + path_to_int_map[p_ctx.metadata_path].toString(16) + "_of";

            if(path_to_onfocus_map[p_ctx.metadata_path])
            {
                page_render_create_event(result, "onfocus", p_ctx.metadata.onfocus, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
            }

            f_name = "x" + path_to_int_map[p_ctx.metadata_path].toString(16) + "_och";
            if(path_to_onchange_map[p_ctx.metadata_path])
            {
                page_render_create_event(result, "onchange", p_ctx.metadata.onchange, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
            }
            
            f_name = "x" + path_to_int_map[p_ctx.metadata_path].toString(16) + "_ocl";
            if(path_to_onclick_map[p_ctx.metadata_path])
            {
                page_render_create_event(result, "onclick", p_ctx.metadata.onclick, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
            }
            
            page_render_create_onblur_event(result, p_ctx.metadata, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
        }
        result.push(" />");

        //~~~~ Validation Error Message
        // switch (p_ctx.metadata.type.toLocaleLowerCase())
        // {
        //     case "date":
        //         result.push(`<small class="text-danger">Invalid date</small>`);
        //         break;
        //     default :
        //         //do nothing, linting requires empty default case
        //         break;
        // }

    result.push("</div>");

    // post html start
    
    switch (p_ctx.metadata.type.toLocaleLowerCase())
    {
        
        case "date":
            /*
            p_ctx.post_html_render.push(` flatpickr
            (
                "#${convert_object_path_to_jquery_id(p_ctx.object_path)}_input", 
                {
                    utc: true,
                    defaultDate:"${p_ctx.data}",
                    enableTime: false,
                    onClose: function(selectedDates, p_value, instance)
                    {
                            let elem = document.querySelector("#${convert_object_path_to_jquery_id(p_ctx.object_path)} input");
                            elem.value = p_value;
                            g_set_data_object_from_path("${p_ctx.object_path}", "${p_ctx.metadata_path}", "${p_ctx.mmria_path}", p_value);
                    }
                }
            );`);*/

            /*
                START datetimepicker() init and options
                TODO: Comment out when going to test
                    ~ 7/6/20: Removed datepicker plugin, using browser supported 'type="date"' attribute
            */
            // p_ctx.post_html_render.push('$("#' + convert_object_path_to_jquery_id(p_ctx.object_path) + ' input").datetimepicker({');
            // p_ctx.post_html_render.push(' format: "Y-MM-DD", ');
            // p_ctx.post_html_render.push(' defaultDate: "' + p_ctx.data + '",');
            // p_ctx.post_html_render.push(`
            //     icons: {
            //         time: "x24 fill-p cdc-icon-clock_01",
            //         date: "x24 fill-p cdc-icon-calendar_01",
            //         up: "x24 fill-p cdc-icon-chevron-circle-up",
            //         down: "x24 fill-p cdc-icon-chevron-circle-down",
            //         previous: 'x24 fill-p fill-p cdc-icon-chevron-circle-left-light',
            //         next: 'x24 fill-p cdc-icon-chevron-circle-right-light'
            //     }
            // `);
            // p_ctx.post_html_render.push('});');
            /* END datetimepicker() */

            break;

        // case "datetime":
        //     p_ctx.post_html_render.push('$("#' + convert_object_path_to_jquery_id(p_ctx.object_path) + ' input").datetimepicker({');
        //     p_ctx.post_html_render.push(' format: "Y-MM-D H:mm:ss", ');
        //     p_ctx.post_html_render.push(' defaultDate: "' + p_ctx.data + '",');
        //     p_ctx.post_html_render.push(`
        //         icons: {
        //             time: 'x24 fill-p cdc-icon-clock_01',
        //             date: 'x24 fill-p cdc-icon-calendar_01',
        //             up: "x24 fill-p cdc-icon-chevron-circle-up",
        //             down: "x24 fill-p cdc-icon-chevron-circle-down",
        //             previous: 'x24 fill-p fill-p cdc-icon-chevron-circle-left-light',
        //             next: 'x24 fill-p cdc-icon-chevron-circle-right-light'
        //         },
        //     `);
        //     // p_ctx.post_html_render.push(' changeYear: true, ');
        //     // p_ctx.post_html_render.push(' changeMonth: true, ');
        //     p_ctx.post_html_render.push('});');
        //     break;

        case "time":
            p_ctx.post_html_render.push(' $("#' + convert_object_path_to_jquery_id(p_ctx.object_path) + ' input" ).datetimepicker({');
            p_ctx.post_html_render.push(`
                format: 'LT',
                icons: {
                    time: 'x24 fill-p cdc-icon-clock_01',
                    date: 'x24 fill-p cdc-icon-calendar_01',
                    up: "x24 fill-p cdc-icon-chevron-circle-up",
                    down: "x24 fill-p cdc-icon-chevron-circle-down",
                    previous: 'x24 fill-p fill-p cdc-icon-chevron-circle-left-light',
                    next: 'x24 fill-p cdc-icon-chevron-circle-right-light'
                },
            `);
            p_ctx.post_html_render.push('});');
            break;

    }
    
    // post html end
}

function renderSearchDateControl(p_ctx)
{
  const result = p_ctx.result;
  const style_object = g_default_ui_specification.form_design[p_ctx.mmria_path.substring(1)];
  const control_string = get_only_size_and_font_style_string(style_object.control.style);
  const is_readonly = (p_ctx.metadata.is_read_only != null && p_ctx.metadata.is_read_only == true) || p_ctx.metadata.mirror_reference || p_ctx.is_read_only ? true : false;
  let is_valid = p_ctx.is_valid_date_or_datetime;
  if (is_valid === undefined) is_valid = !is_valid;

  result.push(`<div id="${convert_object_path_to_jquery_id(p_ctx.object_path)}" metadata="${p_ctx.mmria_path}" class="form-group mb-4">`);
    const path_items = p_ctx.mmria_path.split("/");
    result.push("<p>");
      for(let i = 1; i < path_items.length; i++)
      {
        const item = path_items[i];
        if(i === 1)
        {
          const array = window.location.href.split("/field_search/");
          const link_url = array[0] + "/" + item;
          result.push(`<a href='${link_url}'>${item}</a>`);
        }
        else
        {
          result.push(" > ");
          result.push(item);
        }
      }
    result.push("</p>");

    result.push(`
    <label class="row no-gutters w-auto h-auto" for="${p_ctx.mmria_path.replace(/\//g, "--")}">${p_ctx.metadata.prompt}
    
    ${render_data_analyst_dictionary_link
        (
            p_ctx.metadata, 
            p_ctx.mmria_path
        )}
    </label>`);

    result.push(`<input style="${style_object && style_object.control && style_object.control.style ? control_string : ''}" id="${convert_object_path_to_jquery_id(p_ctx.object_path)}_input" class="form-control date ${!is_valid && 'is-invalid'}" type="date" min="1900-01-01" max="2100-12-31"`);
      result.push(` value="${p_ctx.data}" `); 
      if (is_readonly) {
        result.push(" readonly=true disabled=true ");
      } else {
        let f_name = `x${path_to_int_map[p_ctx.metadata_path].toString(16)}_of`;
        if(path_to_onfocus_map[p_ctx.metadata_path])
        {
          page_render_create_event(result, "onfocus", p_ctx.metadata.onfocus, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
        }
        f_name = `x${path_to_int_map[p_ctx.metadata_path].toString(16)}_och`;
        if(path_to_onchange_map[p_ctx.metadata_path])
        {
          page_render_create_event(result, "onchange", p_ctx.metadata.onchange, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
        }
        f_name = `x${path_to_int_map[p_ctx.metadata_path].toString(16)}_ocl`;
        if(path_to_onclick_map[p_ctx.metadata_path])
        {
          page_render_create_event(result, "onclick", p_ctx.metadata.onclick, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
        }
        
        page_render_create_onblur_event(result, p_ctx.metadata, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
      }
    result.push(` />`);
    result.push(`<small class="validation-msg text-danger mt-2" style="${is_valid ? 'display: none;' : 'display: false;'}">Invalid date</small>`);
  result.push(`</div>`);

  p_ctx.post_html_render.push(`
    if (${is_valid}) {
      //check if item is already there
      if ($('#validation_summary ul').find('[data-path="${convert_object_path_to_jquery_id(p_ctx.object_path)}"]').length > 0) {
        $('[data-path="${convert_object_path_to_jquery_id(p_ctx.object_path)}"]').remove();
      }
      
      if ($('#validation_summary li').length < 1) {
        $('#validation_summary').hide();
      }
    } else {
      //check if item is already there
      if ($('#validation_summary ul').find('[data-path="${convert_object_path_to_jquery_id(p_ctx.object_path)}"]').length < 1) {
        const li = document.createElement('li');
        const strong = document.createElement('strong');
        
        li.setAttribute('data-path', '${convert_object_path_to_jquery_id(p_ctx.object_path)}');
        strong.innerText = '${p_ctx.metadata.prompt}: ';
        li.innerText = 'Date must be a valid calendar date between 1900-2100';
        li.prepend(strong);
        $('#validation_summary ul').append(li);
      }

      $('#validation_summary').show();
    }
  `);
}

function renderSearchDateTimeControl(p_ctx)
{
  const result = p_ctx.result;
  const style_object = g_default_ui_specification.form_design[p_ctx.mmria_path.substring(1)];
  const control_string = get_only_size_and_font_style_string(style_object.control.style);
  const is_readonly = (p_ctx.metadata.is_read_only != null && p_ctx.metadata.is_read_only == true) || p_ctx.metadata.mirror_reference || p_ctx.is_read_only ? true : false;
  let is_valid = p_ctx.is_valid_date_or_datetime;
  if (is_valid === undefined) is_valid = !is_valid;

  result.push(`<div id="${convert_object_path_to_jquery_id(p_ctx.object_path)}" metadata="${p_ctx.mmria_path}" class="form-group mb-4">`);
    const path_items = p_ctx.mmria_path.split("/");
    result.push(`<p>`);
      for(let i = 1; i < path_items.length; i++)
      {
        const item = path_items[i];
        if(i === 1)
        {
          const array = window.location.href.split("/field_search/");
          const link_url = array[0] + "/" + item;
          result.push(`<a href='${link_url}'>${item}</a>`);
        }
        else
        {
          result.push(" > ");
          result.push(item);
        }
      }
    result.push(`</p>`);
    
    result.push
    (`
    <label class="row no-gutters w-auto h-auto" for="${p_ctx.mmria_path.replace(/\//g, "--")}">${p_ctx.metadata.prompt}
    ${render_data_analyst_dictionary_link
        (
            p_ctx.metadata, 
            p_ctx.mmria_path
        )}
    </label>`);
    
    result.push(`<div class="datetime-control row no-gutters ${!is_valid && 'is-invalid'}" style="${style_object && style_object.control && style_object.control.style ? control_string : ''}">`);
      result.push(`<input id="${convert_object_path_to_jquery_id(p_ctx.object_path)}_dateinput" class="form-control datetime-date form-control w-50 h-100" type="date" min="1900-01-01" max="2100-12-31"`);
        result.push(` value="${p_ctx.data.split('T')[0]}" `); 
        if (is_readonly) {
          result.push(" readonly=true disabled=true ");
        } else {
          let f_name = `x${path_to_int_map[p_ctx.metadata_path].toString(16)}_of`;
          if(path_to_onfocus_map[p_ctx.metadata_path])
          {
            page_render_create_event(result, "onfocus", p_ctx.metadata.onfocus, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
          }
          f_name = `x${path_to_int_map[p_ctx.metadata_path].toString(16)}_och`;
          if(path_to_onchange_map[p_ctx.metadata_path])
          {
            page_render_create_event(result, "onchange", p_ctx.metadata.onchange, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
          }
          f_name = `x${path_to_int_map[p_ctx.metadata_path].toString(16)}_ocl`;
          if(path_to_onclick_map[p_ctx.metadata_path])
          {
            page_render_create_event(result, "onclick", p_ctx.metadata.onclick, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
          }
          
          create_onblur_datetime_event(result, p_ctx.metadata, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
        }
      result.push(` />`);

      const newData = p_ctx.data;
      const newDateValue = newData.split('T')[0];
      const newTimeValue = newData.indexOf('.') !== -1 ? newData.substring(newData.indexOf('T')+1, newData.indexOf('.')) : newData.substring(newData.indexOf('T')+1, newData.indexOf('Z'));
      result.push(`<input id="${convert_object_path_to_jquery_id(p_ctx.object_path)}_timeinput" class="form-control datetime-time form-control w-50 h-100" type="text" value="${newDateValue ? newTimeValue : '00:00:00'}"`);
        if (is_readonly) {
          result.push(" readonly=true disabled=true ");
        } else {
          let f_name = `x${path_to_int_map[p_ctx.metadata_path].toString(16)}_of`;
          if(path_to_onfocus_map[p_ctx.metadata_path])
          {
            page_render_create_event(result, "onfocus", p_ctx.metadata.onfocus, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
          }
          f_name = `x${path_to_int_map[p_ctx.metadata_path].toString(16)}_och`;
          if(path_to_onchange_map[p_ctx.metadata_path])
          {
            page_render_create_event(result, "onchange", p_ctx.metadata.onchange, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
          }
          f_name = `x${path_to_int_map[p_ctx.metadata_path].toString(16)}_ocl`;
          if(path_to_onclick_map[p_ctx.metadata_path])
          {
            page_render_create_event(result, "onclick", p_ctx.metadata.onclick, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
          }
          
          create_onblur_datetime_event(result, p_ctx.metadata, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
        }
      result.push(` />`);
      result.push(`<small class="validation-msg text-danger mt-2" style="${is_valid ? 'display: none;' : 'display: false;'}">Invalid date and time</small>`);
      result.push(`</div>`);
  result.push(`</div>`);

  p_ctx.post_html_render.push(`
    if (${is_valid}) {
      //check if item is already there
      if ($('#validation_summary ul').find('[data-path="${convert_object_path_to_jquery_id(p_ctx.object_path)}"]').length > 0) {
        $('[data-path="${convert_object_path_to_jquery_id(p_ctx.object_path)}"]').remove();
      }
      
      if ($('#validation_summary li').length < 1) {
        $('#validation_summary').hide();
      }
    } else {
      //check if item is already there
      if ($('#validation_summary ul').find('[data-path="${convert_object_path_to_jquery_id(p_ctx.object_path)}"]').length < 1) {
        const li = document.createElement('li');
        const strong = document.createElement('strong');
        
        li.setAttribute('data-path', '${convert_object_path_to_jquery_id(p_ctx.object_path)}');
        strong.innerText = '${p_ctx.metadata.prompt}: ';
        li.innerText = 'Date must be a valid calendar date between 1900-2100';
        li.prepend(strong);
        $('#validation_summary ul').append(li);
      }

      $('#validation_summary').show();
    }
  `);

  //Initialize the custom 'bootstrap timepicker'
  p_ctx.post_html_render.push(`
    $('#${convert_object_path_to_jquery_id(p_ctx.object_path)}_timeinput').timepicker({
      defaultTime: false,
      minuteStep: 1,
      secondStep: 1,
      showMeridian: false,
      showSeconds: true,
      template: false,
      icons: {
        up: 'x24 fill-p cdc-icon-arrow-down',
        down: 'x24 fill-p cdc-icon-arrow-down'
      }
    });
  `);

  //helper fn to toggle disabled attr
  p_ctx.post_html_render.push(`
    function toggle_disabled(el, tar) {				
      if (isNullOrUndefined(el.val())) {
        tar.attr('disabled', true);
      }
      else
      {
        tar.attr('disabled', false);
      }
    }
  `);

  //On load, IF case has been checked out
  //we want to toggle disabled attr on time incase date is valid/invalid
  if (g_data_is_checked_out)
  {
    p_ctx.post_html_render.push(`
      toggle_disabled($('#${convert_object_path_to_jquery_id(p_ctx.object_path)} .datetime-date'), $('#${convert_object_path_to_jquery_id(p_ctx.object_path)} .datetime-time'));
    `);
  }
}

function render_search_text_textarea_control(p_ctx)
{   
    let style_object = g_default_ui_specification.form_design[p_ctx.mmria_path.substring(1)];

    if(style_object)
    {
        p_ctx.result.push(`<div metadata="${p_ctx.mmria_path}" class="quickedit-wrapper form-group mb-4"`);
          p_ctx.result.push("<p>");
              // p_ctx.result.push(p_ctx.mmria_path.substring(1).replace(/\//g, " > "));
              let path_items = p_ctx.mmria_path.split('/');

              for(let i = 1; i < path_items.length; i++)
              {
                  let item = path_items[i];

                  if(i == 1)
                  {
                      let array = window.location.href.split("/field_search/");
                      //window.location.hash = "/" + record_index + "/field_search/" + search_text;
                      let link_url = array[0] + "/" + item;

                      p_ctx.result.push(`<a href='${link_url}'>${item}</a>`);
                  }
                  else
                  {
                      p_ctx.result.push(" > ");
                      p_ctx.result.push(item);
                  }
              }
          p_ctx.result.push("</p>");

          p_ctx.result.push(`<label class="row no-gutters w-auto h-auto" for="${p_ctx.mmria_path.replace(/\//g, "--")}" style="${get_only_size_and_font_style_string(style_object.prompt.style)}">${p_ctx.metadata.prompt}
          ${render_data_analyst_dictionary_link
            (
                p_ctx.metadata, 
                p_ctx.mmria_path
            )}
          </label>`);

          p_ctx.result.push(`<textarea id="${p_ctx.mmria_path.replace(/\//g, "--")}" class="${p_ctx.mmria_path.indexOf('case_narrative') !== -1 ? 'quicksearch-rich-text-editor': ''} form-control" style="${get_only_size_and_font_style_string(style_object.control.style)}; ${p_ctx.mmria_path.indexOf('case_narrative') !== -1 ? 'height: auto': ''}" `);

          if
          (
              (
                  p_ctx.metadata.is_read_only != null &&
                  p_ctx.metadata.is_read_only == true
              ) ||
              p_ctx.metadata.mirror_reference ||
              p_ctx.is_read_only
          )
          {
              p_ctx.result.push(` readonly=true disabled=true `);
          }
          else
          {
              let f_name = "x" + path_to_int_map[p_ctx.metadata_path].toString(16) + "_of";

              if(path_to_onfocus_map[p_ctx.metadata_path])
              {
                  page_render_create_event(result, "onfocus", p_ctx.metadata.onfocus, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
              }

              f_name = "x" + path_to_int_map[p_ctx.metadata_path].toString(16) + "_och";
              if(path_to_onchange_map[p_ctx.metadata_path])
              {
                  page_render_create_event(result, "onchange", p_ctx.metadata.onchange, p_ctx.metadata_path, p_ctx.object_path, p_ctx.dictionmmria_pathary_path);
              }
              
              f_name = "x" + path_to_int_map[p_ctx.metadata_path].toString(16) + "_ocl";
              if(path_to_onclick_map[p_ctx.metadata_path])
              {
                  page_render_create_event(result, "onclick", p_ctx.metadata.onclick, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
              }
              
              page_render_create_onblur_event(p_ctx.result, p_ctx.metadata, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path);
          }

          p_ctx.result.push(">");
            p_ctx.result.push(p_ctx.data);
          p_ctx.result.push("</textarea>");

          let opts = {
            btns: [
              ['viewHTML'],
              ['undo', 'redo'],
              ['strong', 'em', 'underline', 'del'],
              ['fontsize'],
              ['foreColor', 'backColor'],
              ['justifyLeft', 'justifyCenter', 'justifyRight'],
              ['unorderedList', 'orderedList'],
              ['horizontalRule'],
              ['removeformat'],
              ['fullscreen'],
            ],
            plugins: {
              // Add font sizes manually
              fontsize: {
                sizeList: [
                  '14px',
                  '16px',
                  '18px',
                  '24px',
                  '32px',
                  '48px'
                ],
                allowCustomSize: false
              },
              // Add colors manually
              // Currently utilizing all primary, secondary, tertiary colors in color wheel
              colors: {
                colorList: [
                  'FFFFFF',
                  'CCCCCC',
                  '777777',
                  '333333',
                  '000000',
                  'FF0000',
                  '00FF00',
                  '0000FF',
                  'FFFF00',
                  'FF00FF',
                  '00FFFF',
                  'FF7F00',
                  'FF007F',
                  '7FFF00',
                  '7F00FF',
                  '00FF7F',
                  '007FFF'
                ]
              }
            },
            semantic: true
          }

          p_ctx.post_html_render.push(`
            $('.quicksearch-rich-text-editor').trumbowyg(${JSON.stringify(opts)})
              .on('tbwchange', function ()
              {
                let data = $('.trumbowyg-editor').html();
                data = data.split('&quot;').join('\\'');
                $('.trumbowyg-textarea').val(data);
                
                g_textarea_oninput("${p_ctx.object_path}","${p_ctx.metadata_path}","${p_ctx.dictionary_path}", data);
              })
              .on('tbwpaste', function ()
              {
                let data = $('.trumbowyg-editor').html();
                data = data.split('&quot;').join('\\'');
                $('.trumbowyg-textarea').val(data);
                
                g_textarea_oninput("${p_ctx.object_path}","${p_ctx.metadata_path}","${p_ctx.dictionary_path}", data);
              });
          `);

        p_ctx.result.push("</div>");
    }
}

function render_search_text_group_control(p_ctx)
{   
    let result = [];

    for(let i = 0; i < p_ctx.metadata.children.length; i++)
    {
        let child = p_search_text_context.metadata.children[i];

        Array.prototype.push.apply(result, render_search_text(child, p_ctx.mmria_path+ "/" + child.name, p_search_text));
    }

    return result;
}

function render_search_text_grid_control(p_ctx)
{   
    p_ctx.result.push("<fieldset id='");
    
    p_ctx.result.push(p_ctx.metadata_path);
    p_ctx.result.push("' ");
    p_ctx.result.push(" mpath='" + p_ctx.metadata_path + "'");
    p_ctx.result.push(" class='grid2 grid-control' style='");

    let style_object = g_default_ui_specification.form_design[p_ctx.mmria_path.substring(1)];

    if(style_object)
    {
        p_ctx.result.push(get_only_size_and_position_string(style_object.control.style));
        is_grid_context = style_object;
    }
    p_ctx.result.push("' >"); // close opening div

    p_ctx.result.push("<legend class='grid-control-legend' style='");
    
    if(style_object && style_object.prompt)
    {
        p_ctx.result.push(get_only_font_style_string(style_object.prompt.style));
    }
    p_ctx.result.push("'>");
    p_ctx.result.push(p_ctx.metadata.prompt);
    p_ctx.result.push(" - ");
    p_ctx.result.push(p_ctx.data.length)
    p_ctx.result.push(" item(s) </legend>");
    p_ctx.result.push("<div class='grid-control-items'>");
    for(let i = 0; i < p_ctx.metadata.children.length; i++)
    {
        let child = p_ctx.metadata.children[i];
        let new_context = get_seach_text_context(p_ctx.result, p_ctx.post_html_render, child.name, p_ctx.data[child.name], p_ctx.mmria_path + "/" + child.name, p_ctx.metadata_path  + ".children[" + i + "]", p_ctx.object_path, p_ctx.search_text, p_ctx.is_read_only, p_ctx.form_index, p_ctx.grid_index, p_ctx.is_valid_date_or_datetime, p_ctx.entered_date_or_datetime_value);

        render_search_text(new_context);

        //Array.prototype.push.apply(result, render_search_text(child, p_ctx.mmria_path+ "/" + child.name, p_search_text, false));
    }

    p_ctx.result.push("</div>");
    p_ctx.result.push("<button type='button'class='grid-control-btn btn btn-primary d-flex align-items-center' onclick='g_add_grid_item(\"");
    // p_result.push("<input type='button' style='width:90px'  class='btn btn-primary' value='Add Item' onclick='g_add_grid_item(\"");
    p_ctx.result.push(p_ctx.object_path);
    p_ctx.result.push("\", \"");
    p_ctx.result.push(p_ctx.metadata_path);
    p_ctx.result.push("\", \"");
    p_ctx.result.push(p_ctx.dictionary_path);    
    p_ctx.result.push("\")'><span class='x24 cdc-icon-plus'></span> Add Item");
    p_ctx.result.push("</button>");
    p_ctx.result.push("</fieldset>");    

}

function render_search_text_select_control(p_ctx)
{
    const is_readonly = (p_ctx.metadata.is_read_only != null && p_ctx.metadata.is_read_only == true) || p_ctx.metadata.mirror_reference || p_ctx.is_read_only ? true : false;
    
    if(p_ctx.metadata.control_style && p_ctx.metadata.control_style.toLowerCase().indexOf("editable") > -1)
    {
        Array.prototype.push.apply(p_ctx.result, render_search_text_list_editable_render(p_ctx.result, p_ctx.metadata, p_ctx.data, p_ctx.ui, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path, p_ctx.is_grid_context, p_ctx.post_html_render, p_ctx));
        return;
    }
    else if
    (
        (p_ctx.metadata.is_multiselect && p_ctx.metadata.is_multiselect == true) ||
        (p_ctx.metadata.control_style && p_ctx.metadata.control_style.toLowerCase().indexOf("checkbox") > -1)
    )
    {
        Array.prototype.push.apply(p_ctx.result, render_search_text_list_checkbox_render(p_ctx.result, p_ctx.metadata, p_ctx.data, p_ctx.ui, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path, p_ctx.is_grid_context, p_ctx.post_html_render, p_ctx));
        return;
    }
    else if(p_ctx.metadata.control_style && p_ctx.metadata.control_style.toLowerCase().indexOf("radio") > -1) 
    {
        Array.prototype.push.apply(p_ctx.result, render_search_text_list_radio_render(p_ctx.result, p_ctx.metadata, p_ctx.data, p_ctx.ui, p_ctx.metadata_path, p_ctx.object_path, p_ctx.mmria_path, p_ctx.is_grid_context, p_ctx.post_html_render, p_ctx));
        return;
    }

    let style_object = g_default_ui_specification.form_design[p_ctx.mmria_path.substring(1)];

    if(style_object)
    {
        p_ctx.result.push("<div metadata='");
        p_ctx.result.push(p_ctx.mmria_path_path);
        p_ctx.result.push("' class='form-group mb-4'>");
        p_ctx.result.push("<p>");
            // p_ctx.result.push(p_ctx.mmria_path.substring(1).replace(/\//g, " > "));
            let path_items = p_ctx.mmria_path.split('/');

            for(let i = 1; i < path_items.length; i++)
            {
                let item = path_items[i];

                if(i == 1)
                {
                    let array = window.location.href.split("/field_search/");
                    //window.location.hash = "/" + record_index + "/field_search/" + search_text;
                    let link_url = array[0] + "/" + item;

                    p_ctx.result.push(`<a href='${link_url}'>${item}</a>`);
                }
                else
                {
                    p_ctx.result.push(" > ");
                    p_ctx.result.push(item);
                }
            }
        p_ctx.result.push("</p>");
            
        p_ctx.result.push("<label class='row no-gutters w-auto h-auto' for='");
        p_ctx.result.push(p_ctx.mmria_path.replace(/\//g, "--"));
        p_ctx.result.push("' style='");
                //if(style_object.prompt)
                //result.push(get_style_string(style_object.prompt.style)); 
                p_ctx.result.push("'>");
                p_ctx.result.push(p_ctx.metadata.prompt);
                p_ctx.result.push
                (`
                    ${render_data_analyst_dictionary_link
                    (
                        p_ctx.metadata, 
                        p_ctx.mmria_path
                    )}
                `);
                p_ctx.result.push("</label>");
            
                p_ctx.result.push(`<select ${is_readonly ? 'disabled' : ''} id='`);
                p_ctx.result.push(p_ctx.mmria_path);
                p_ctx.result.push("' class='custom-select' ");

            if(style_object.control)
            {
                p_ctx.result.push("style='")
                p_ctx.result.push(get_only_size_and_font_style_string(style_object.control.style));
                p_ctx.result.push("' "); 
            }

            if
            (
                (
                    p_ctx.metadata.is_read_only != null &&
                    p_ctx.metadata.is_read_only == true
                ) ||
                p_ctx.metadata.mirror_reference ||
                p_ctx.is_read_only
            )
            {
                p_ctx.result.push(" readonly=true ");
            }
            else
            {
                p_ctx.result.push("  onchange='g_set_data_object_from_path(\"");
                p_ctx.result.push(p_ctx.object_path);
                p_ctx.result.push("\",\"");
                p_ctx.result.push(p_ctx.metadata_path);
                p_ctx.result.push("\",\"");
                p_ctx.result.push(p_ctx.dictionary_path);
                p_ctx.result.push("\",this.value)'  ");
            }

            if(p_ctx.metadata.list_display_size != null)
            {
                p_ctx.result.push(" size='"); 
                p_ctx.result.push(p_ctx.metadata.list_display_size);
            }
            
            p_ctx.result.push("' >"); 


            let data_value_list = p_ctx.metadata.values;

            if(p_ctx.metadata.path_reference && p_ctx.metadata.path_reference != "")
            {
                data_value_list = eval(convert_dictionary_path_to_lookup_object(p_ctx.metadata.path_reference));
        
                if(data_value_list == null)	
                {
                    data_value_list = p_ctx.metadata.values;
                }
            }

            for(let i = 0; i < data_value_list.length; i++)
            {
                let child = data_value_list[i];

                p_ctx.result.push("<option ");
                if(p_ctx.data == child.value)
                {
                    p_ctx.result.push("selected ");
                }
                p_ctx.result.push(" value='");
                p_ctx.result.push(child.value);
                p_ctx.result.push("'>");


                if(child.display)
                {

                    p_ctx.result.push(child.display);
                }
                else if(child.value == 9999)
                {
                    p_ctx.result.push("(blank)");
                }
                else
                {
                    p_ctx.result.push(child.value);
                }
                p_ctx.result.push("</option>");

            }
            
            p_ctx.result.push("</select>");
            p_ctx.result.push("</div>");
    }
}

function get_only_size_and_font_style_string(p_specicification_style_string)
{
    let result = [];
    let properly_formated_style = p_specicification_style_string.replace(/[{}]/g, "");

    properly_formated_style = properly_formated_style.replace(/['"]+/g, '');
    properly_formated_style = properly_formated_style.replace(/[,]+/g, ';');
    properly_formated_style = properly_formated_style.replace(/(\d+); (\d+); (\d+)/g, '$1, $2, $3');
    //"position:absolute;top:4;left:13;height:46px;width:146.188px;font-weight:400;font-size:16px;font-style:normal;color:rgb(33; 37; 41)"
    let items = properly_formated_style.split(";");

    for(let i = 0; i < items.length; i++)
    {
        let pair = items[i].split(":");
        let value = null;

        switch(pair[0].toLocaleLowerCase())
        {
            case "height":
            case "width":
                value = pair[1].trim();
                if(/px$/.test(value))
                {
                    result.push(pair[0] + ":" + value);
                }
                else
                {
                    result.push(pair[0] + ":" + pair[1].trim() + "px");
                }
                break;
            case "font-size":
                value = pair[1].trim();
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


function render_search_text_list_editable_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx)
{
    var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];

    p_result.push("<div class='list' id='");
    p_result.push(convert_object_path_to_jquery_id(p_object_path));
    
    p_result.push("' ");
    p_result.push(" mpath='");
    p_result.push(p_metadata_path);
    p_result.push("' ");

    p_result.push(">");
    p_result.push("<label for='");
    p_result.push(p_object_path.replace(/\//g, "--"));
    p_result.push("' style='");
    if(style_object && style_object.prompt)
    {
        p_result.push(get_style_string(style_object.prompt.style));
    }
    p_result.push("' ");

    if(p_metadata.description && p_metadata.description.length > 0)
    {
        p_result.push("rel='tooltip'  data-original-title='");
        p_result.push(p_metadata.description.replace(/'/g, "\\'"));
        p_result.push("'>");
    }
    else
    {
        p_result.push(">");
    }
    
    p_result.push(p_metadata.prompt);

    p_result.push
    (`
        ${render_data_analyst_dictionary_link
        (
            p_metadata, 
            p_dictionary_path
        )}
    `);
    p_result.push("</label>");

    p_result.push("<div style='");
    if(style_object && style_object.prompt)
    {
        p_result.push(get_only_size_and_position_string(style_object.control.style));
    }

    p_result.push("'>");

    if(p_metadata.list_display_size && p_metadata.list_display_size!= "")
    {
        p_result.push("<select class='form-control list-control-select 1' size=");
        p_result.push(p_metadata.list_display_size);
        p_result.push(" name='");
    }
    else if(p_metadata.is_multiselect && p_metadata.is_multiselect == true)
    {
        if(p_metadata.values.length > 6)
        {
            p_result.push("<select class='form-control list-control-select 2' size='6' name='");
        }
        else
        {
            p_result.push(" <select size=");
            p_result.push(p_metadata.values.length);
            p_result.push(" name='");
        }
        
    }
    else
    {
        p_result.push("<select class='form-control list-control-select 3' size=");
        p_result.push(1);
        p_result.push(" name='");
    }
    
    p_result.push(p_metadata.name);
    
    p_result.push("'");
    // p_result.push("' style='width:98%;height:49%;'");

    if
    (
        (
            p_metadata.is_read_only != null &&
            p_metadata.is_read_only == true
        ) ||
        p_metadata.mirror_reference
    )
    {
        p_result.push(" readonly=true ");
    }
    else
    {

        p_result.push("  onblur='g_set_data_object_from_path(\"");
        p_result.push(p_object_path);
        p_result.push("\",\"");
        p_result.push(p_metadata_path);
        p_result.push("\",\"");
        p_result.push(p_dictionary_path);
        p_result.push("\",this.value)'  ");
    }

    if(p_metadata['is_multiselect'] && p_metadata.is_multiselect == true)
    {
        p_result.push(" multiple>");

        var metadata_value_list = p_metadata.values;

        if(p_metadata.path_reference && p_metadata.path_reference != "")
        {
            metadata_value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));

            if(metadata_value_list == null)	
            {
                metadata_value_list = p_metadata.values;
            }
        }

        for(var i = 0; i < metadata_value_list.length; i++)
        {
            var item = metadata_value_list.values[i];

            if(p_data.indexOf(item.value) > -1)
            {
                    p_result.push("<option value='");
                    p_result.push(item.value.replace(/'/g, "&#39;"));
                    p_result.push("' selected>");
                    if(item.display)
                    {
                        p_result.push(item.display);
                    }
                    else if(item.value == 9999)
                    {
                        p_result.push("(blank)");
                    }
                    else
                    {
                        p_result.push(item.value);
                    }
                    p_result.push("</option>");
            }
            else
            {
                    p_result.push("<option value='");
                    p_result.push(item.value.replace(/'/g, "&#39;"));
                    p_result.push("' >");
                    if(item.display)
                    {
                        p_result.push(item.display);
                    }
                    else if(item.value == 9999)
                    {
                        p_result.push("(blank)");
                    }
                    else
                    {
                        p_result.push(item.value);
                    }
                    p_result.push("</option>");
            }
        }
        p_result.push("</select>");

        p_result.push("<label for='"+p_metadata.name+"' class='sr-only'>"+p_metadata.name+"</label>");
        p_result.push("<input id='"+p_metadata.name+"' class='list-control-input mt-1' placeholder='Specify Other' class='list' type='text3' name='");
        // p_result.push("<br/><label><input style='width:98%;height:49%;' placeholder='Specify Other' class='list' type='text3' name='");
        p_result.push(p_metadata.name);
        p_result.push("' value='");
        p_result.push(p_data);
        p_result.push("' ");

        if
        (
            (
                p_metadata.is_read_only != null &&
                p_metadata.is_read_only == true
            ) ||
            p_metadata.mirror_reference
        )
        {
            p_result.push(" readonly=true ");
        }
        else
        {
            p_result.push(" onblur='g_set_data_object_from_path(\"");
            p_result.push(p_object_path);
            p_result.push("\",\"");
            p_result.push(p_metadata_path);
            p_result.push("\",\"");
            p_result.push(p_dictionary_path);
            p_result.push("\",this.value)' />");
        }
        p_result.push("</label> </div> ");


    }
    else
    {
        p_result.push(">");

        var metadata_value_list = p_metadata.values;

        if(p_metadata.path_reference && p_metadata.path_reference != "")
        {
            metadata_value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));

            if(metadata_value_list == null)	
            {
                metadata_value_list = p_metadata.values;
            }
        }

        for(var i = 0; i < metadata_value_list.length; i++)
        {
            var item = metadata_value_list[i];

            if(p_data == item.value)
            {
                p_result.push("<option value='");
                p_result.push(item.value.replace(/'/g, "&#39;"));
                p_result.push("' selected>");
                if(item.display)
                {
                    p_result.push(item.display);
                }
                else  if(item.value == 9999)
                {
                    p_result.push("(blank)");
                }
                else
                {
                    p_result.push(item.value);
                }
                p_result.push("</option>");
            }
            else
            {
                p_result.push("<option value='");
                p_result.push(item.value.replace(/'/g, "&#39;"));
                p_result.push("' >");
                if(item.display)
                {
                    p_result.push(item.display);
                }
                else if(item.value == 9999)
                {
                    p_result.push("(blank)");
                }
                else
                {
                    p_result.push(item.value);
                }
                p_result.push("</option>");
            }
        }
        p_result.push("</select> ");
        
    //if(p_metadata.list_display_size && p_metadata.list_display_size!="")
    //{
        // p_result.push("<label>");
        p_result.push("<label for='"+p_metadata.name+"' class='sr-only'>"+p_metadata.name+"</label>");
        p_result.push("<input placeholder='Specify Other' id='"+p_metadata.name+"' class='list-control-input mt-1' type='text3' name='");
        p_result.push(p_metadata.name);
        p_result.push("' value='");
        p_result.push(p_data);
        p_result.push("' ");

        if
        (
            (
                p_metadata.is_read_only != null &&
                p_metadata.is_read_only == true
            ) ||
            p_metadata.mirror_reference
        )
        {
            p_result.push(" readonly=true ");
        }
        else
        {
            p_result.push(" onblur='g_set_data_object_from_path(\"");
            p_result.push(p_object_path);
            p_result.push("\",\"");
            p_result.push(p_metadata_path);
            p_result.push("\",\"");
            p_result.push(p_dictionary_path);
            p_result.push("\",this.value)' /> ");
            // p_result.push("</label>");
        }
        p_result.push("</div>");
        
    //}
    }

    p_result.push("</div>");
    

}


function render_search_text_list_radio_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx)
{
    var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];

    p_result.push("<div class='list form-group mb-4' id='");
    p_result.push(convert_object_path_to_jquery_id(p_object_path));
    
    p_result.push("' ");
    p_result.push(" mpath='");
    p_result.push(p_metadata_path);
    p_result.push("' ");
    p_result.push(">");
    p_result.push("<p>")
        let path_items = p_dictionary_path.split("/");

        for(let i = 1; i < path_items.length; i++)
        {
            let item = path_items[i];

            if(i == 1)
            {
                let array = window.location.href.split("/field_search/");
                //window.location.hash = "/" + record_index + "/field_search/" + search_text;
                let link_url = array[0] + "/" + item;

                p_result.push(`<a href='${link_url}'>${item}</a>`);
            }
            else
            {
                p_result.push(" > ");
                p_result.push(item);
            }
        }
    p_result.push("</p>")

    p_result.push("<fieldset id='");
    p_result.push(p_metadata.name);
    p_result.push("_id' class='radio-list' ");

    //var key = p_dictionary_path.substring(1);

    var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];

    if(style_object)
    {
        p_result.push(" style='");
        p_result.push(get_only_size_and_font_style_string(style_object.control.style).replace('absolute', 'relative'));
        p_result.push("' ");
    }

    p_result.push(" >"); // close opening div
    p_result.push("<legend ");

    if(style_object && style_object.prompt)
    {
        p_result.push(" style='");
        p_result.push(get_only_font_style_string(style_object.prompt.style).replace('absolute', 'relative'));
        p_result.push("'");
    }

    if(p_metadata.description && p_metadata.description.length > 0)
    {
        p_result.push(" rel='tooltip'  data-original-title='");
        p_result.push(p_metadata.description.replace(/'/g, "&#39;"));
        p_result.push("' ");
    }

    p_result.push(">");
    p_result.push(p_metadata.prompt);
    p_result.push
    (`
        ${render_data_analyst_dictionary_link
        (
            p_metadata, 
            p_dictionary_path
        )}
    `);
    p_result.push("</legend>");

    let data_value_list = p_metadata.values;

    if(p_metadata.path_reference && p_metadata.path_reference != "")
    {
        data_value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));

        if(data_value_list == null)	
        {
            data_value_list = p_metadata.values;
        }
    }

    for(let i = 0; i < data_value_list.length; i++)
    {
        var item = data_value_list[i];
        let item_key = null;

        if(item.value == null | item.value == "")
        {
            item_key = p_dictionary_path.substring(1) + "/";
        }
        else
        {
            item_key = p_dictionary_path.substring(1) + "/" + item.value.replace(/ /g, "--").replace(/--/g, '/').replace(/'/g, "-");//.replace(/\//g, "--")
        }

        var item_style = g_default_ui_specification.form_design[item_key];
        var is_selected = "";

        if (item.value == p_data)
        {
            is_selected = " checked ";
        }

        var is_read_only = "";
        let onclick_text = "";

        if
        (
            (
                p_metadata.is_read_only != null &&
                p_metadata.is_read_only == true
            ) ||
            p_metadata.mirror_reference
        )
        {
            is_read_only= " disabled=true ";
            
        }
        else
        {
            onclick_text = `onclick='g_set_data_object_from_path("${p_object_path}","${p_metadata_path}","${p_dictionary_path}",this.value)'`;
        }

        let object_id = convert_object_path_to_jquery_id(p_object_path) + item.value.replace(/\//g, "--").replace(/ /g, "--").replace(/'/g, "-");
        let input_html = 
            `<input 
                id='${object_id}' name='${convert_object_path_to_jquery_id(p_object_path)}' 
                type='radio' 
                value='${item.value}'
                ${onclick_text}
                ${is_selected}
                ${is_read_only}
                
             />`;

        if (item.display) 
        {
            
            p_result.push(`<label class="choice-control" style='${get_only_font_style_string(item_style.prompt.style.replace('absolute','relative'))}' for="${object_id}">${input_html}<span class="choice-control-info"> ${item.display}</span></label>`);
            
            
        }
        else if(item.value == 9999)
        {
            p_result.push(`<label class="choice-control" style='${get_only_font_style_string(item_style.prompt.style.replace('absolute','relative'))}' for="${object_id}">${input_html}<span class="choice-control-info"> (blank)</span></label>`);
        }
        else 
        {
            p_result.push(`<label class="choice-control" style='${get_only_font_style_string(item_style.prompt.style.replace('absolute','relative'))}' for="${object_id}" >${input_html}<span class="choice-control-info"> ${item.value}</span></label>`);
        }
    }

    p_result.push("</fieldset>");
    p_result.push("</div>");
}

function render_search_text_list_checkbox_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx)
{
    let style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];

    p_result.push("<div class='list form-group mb-4' id='");
    p_result.push(convert_object_path_to_jquery_id(p_object_path));
    
    p_result.push("' ");
    p_result.push(" mpath='");
    p_result.push(p_metadata_path);
    p_result.push("' ");

    p_result.push(">");

    p_result.push("<fieldset id='");
    p_result.push(p_metadata.name);
    p_result.push("_id' class='checkbox' ");

    if(style_object)
    {
        p_result.push(" style='");
        p_result.push(get_only_font_style_string(style_object.control.style).replace('absolute','relative'));
        p_result.push("' ");
    }

    p_result.push(">"); // close opening div
    p_result.push("<p>");
        let path_items = p_dictionary_path.split('/');

        for(let i = 1; i < path_items.length; i++)
        {
            let item = path_items[i];

            if(i == 1)
            {
                let array = window.location.href.split("/field_search/");
                //window.location.hash = "/" + record_index + "/field_search/" + search_text;
                let link_url = array[0] + "/" + item;

                p_result.push(`<a href='${link_url}'>${item}</a>`);
            }
            else
            {
                p_result.push(" > ");
                p_result.push(item);
            }
        }

        // p_result.push("<legend ");

        // //var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];
        // if(style_object && style_object.prompt)
        // {
        //     p_result.push(" style='");
        //     p_result.push(get_only_font_style_string(style_object.prompt.style));
        //     p_result.push("'");
        // }

        // if(p_metadata.description && p_metadata.description.length > 0)
        // {
        //     p_result.push(" rel='tooltip'  data-original-title='");
        //     p_result.push(p_metadata.description.replace(/'/g, "&#39;"));
        //     p_result.push("' ");
        // }

        // p_result.push(">");
        // p_result.push(p_metadata.prompt);
        // p_result.push("</legend>");
        p_result.push
        (`
            ${render_data_analyst_dictionary_link
                (
                    p_metadata, 
                    p_dictionary_path
                )}
        `);
    p_result.push("</p>");

    let data_value_list = p_metadata.values;

    if(p_metadata.path_reference && p_metadata.path_reference != "")
    {
        data_value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));

        if(data_value_list == null)	
        {
            data_value_list = p_metadata.values;
        }
    }

    let has_mutually_exclusive_items = false;
    let mutually_exclusive_items = [];
    for(let i = 0; i < data_value_list.length; i++)
    {
        let item = data_value_list[i];
        if(item.is_mutually_exclusive!=null && item.is_mutually_exclusive == true)
        {
            has_mutually_exclusive_items = true;
            mutually_exclusive_items.push(data_value_list[i].value);
        }
    }

    let is_mutually_exclusive_item_selected = false;
    let mutually_exclusive_item = null;

    for(let i = 0; i < mutually_exclusive_items.length; i++)
    {
        let item = mutually_exclusive_items[i];
        if (p_data.indexOf(item) > -1)
        {
            is_mutually_exclusive_item_selected = true;
            mutually_exclusive_item = item;
            break;
        }
    }


    for(let i = 0; i < data_value_list.length; i++)
    {
        var item = data_value_list[i];
        let item_key = null;

        if(item.value == null | item.value == "")
        {
            item_key = p_dictionary_path.substring(1) + "/";
        }
        else
        {
            item_key = p_dictionary_path.substring(1) + "/" + item.value.replace(/ /g, "--").replace(/--/g, '/').replace(/'/g, "-");
        }
        
        let item_style = g_default_ui_specification.form_design[item_key];
        let is_selected = "";

        if(is_mutually_exclusive_item_selected)
        {
            if (item.value == mutually_exclusive_item)
            {
                is_selected = " checked ";
            }
    
        }
        else if (p_data.indexOf(item.value) > -1)
        {
            is_selected = " checked ";
        }

        var is_read_only = "";

        if
        (
            (
                p_metadata.is_read_only != null &&
                p_metadata.is_read_only == true
            ) ||
            p_metadata.mirror_reference ||
            p_search_ctx.is_read_only
        )
        {
            is_read_only= " disabled=true ";
        }
        else if
        (
            is_mutually_exclusive_item_selected && 
            item.value != mutually_exclusive_item &&
            is_read_only == ""
        )
        {
            is_read_only= " disabled=true ";
        }

        //let object_id = ;
        let object_id = convert_object_path_to_jquery_id(p_object_path) + item.value.replace(/\//g, "--").replace(/ /g, "--").replace(/'/g, "-");

        if (item.display) 
        {
            p_result.push("<label class='choice-control' style='" + get_only_font_style_string(item_style.prompt.style) + "' for='" + object_id + "'>");
            render_search_text_list_checkbox_input_render(p_result, object_id,  item, p_object_path, p_metadata_path, p_dictionary_path, is_selected, is_read_only, has_mutually_exclusive_items);
            p_result.push("<span class='choice-control-info'> " + item.display + "</span></label>");
        }
        else if(item.value == 9999)
        {
            p_result.push("<label class='choice-control' style='" + get_only_font_style_string(item_style.prompt.style) + "' for='" + object_id + "'>");
            render_search_text_list_checkbox_input_render(p_result, object_id,  item, p_object_path, p_metadata_path, p_dictionary_path, is_selected, is_read_only, has_mutually_exclusive_items);
            p_result.push("<span class='choice-control-info'> (blank)</span></label>");
        }
        else 
        {
            p_result.push("<label class='choice-control' style='" + get_only_font_style_string(item_style.prompt.style) + "' for='" + object_id + "'>");
            render_search_text_list_checkbox_input_render(p_result, object_id,  item, p_object_path, p_metadata_path, p_dictionary_path, is_selected, is_read_only, has_mutually_exclusive_items);
            p_result.push("<span class='choice-control-info'> " + item.value + "</span></label>");
        }
    }
    p_result.push("</fieldset>");

    p_result.push("</div>");
    
}

function render_search_text_list_checkbox_input_render(p_result, p_id,  p_item, p_object_path, p_metadata_path, p_dictionary_path, p_is_selected, p_is_read_only, p_is_mutually_exclusive)
{
    if(p_is_mutually_exclusive)
    {
        list_checkbox_mutually_exclusive_input_render(p_result, p_id,  p_item, p_object_path, p_metadata_path, p_dictionary_path, p_is_selected, p_is_read_only, p_is_mutually_exclusive);
    }
    else
    {
        p_result.push("<input id='");
        p_result.push(p_id);
        p_result.push("' type='checkbox' ");
        p_result.push(" value='");
        p_result.push(p_item.value);
        p_result.push("' ");

        if(p_is_read_only == null || p_is_read_only == "")
        {
            p_result.push(" onclick=g_set_data_object_from_path(\'");
            p_result.push(p_object_path);
            p_result.push("\',\'");
            p_result.push(p_metadata_path);
            p_result.push("\',\'");
            p_result.push(p_dictionary_path);
            p_result.push("\',this.value) ");
        }

        p_result.push(p_is_selected);
        p_result.push(p_is_read_only);
        p_result.push("></input>");
    }
}
