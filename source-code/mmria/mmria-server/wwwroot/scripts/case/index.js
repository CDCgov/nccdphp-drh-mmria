'use strict';

var g_metadata = null;
var g_user_name = null;
var g_is_data_analyst_mode = null;
var g_data_is_checked_out = false;
var g_data = null;
var g_source_db = null;
var g_jurisdiction_list = [];
var g_user_role_jurisdiction_list = [];
var g_jurisdiction_tree = [];
var g_metadata_path = [];
var g_validator_map = [];
var g_event_map = [];
var g_validation_description_map = [];
var g_selected_index = null;
var g_selected_delete_index = null;
var g_couchdb_url = null;
var g_localDB = null;
var g_remoteDB = null;
var g_metadata_summary = [];
var default_object = null;
var g_change_stack = [];
var g_default_ui_specification = null;
var g_use_position_information = true;
var g_look_up = {};
var g_release_version = null;
var g_autosave_interval = null;
var g_process_save_interval = null;
var g_value_to_display_lookup = {};
var g_name_to_value_lookup = {};
var g_display_to_value_lookup = {};
var g_value_to_index_number_lookup = {};
var g_name_to_value_lookup = {};
var g_is_confirm_for_case_lock = false;
var g_target_case_status = null;
var g_previous_case_status = null;
var g_other_specify_lookup = {};
var g_record_id_list = new Set();
var g_form_access_list = new Map();
var role_set = new Set();
const g_charts = new Map();
const g_chart_data = new Map();
const g_duplicate_path_set = new Set();
var g_case_narrative_is_updated = false;
var g_case_narrative_is_updated_date = null;
var g_case_narrative_original_value = null;

var g_is_committee_member_view = false;

var g_pinned_case_set = null;

var g_pinned_case_count = 0;
var g_is_jurisdiction_admin = false;

let save_start_time, save_end_time;

const g_is_version_based = false;

const g_cvs_api_request_data = new Map();

const g_dependent_parent_to_child = new Map();
const g_dependent_child_to_parent = new Map();
const g_dependent_child_metadata = new Map();


const  disable_on_selected_item_list = new Map()/*
disable_on_selected_item_list.set("tracking/admin_info/steve_transfer", new Map());
disable_on_selected_item_list.get("tracking/admin_info/steve_transfer").set(1, new Set());
//disable_on_selected_item_list.get("tracking/admin_info/steve_transfer").get(1).set("");


const event = new Event("build");

// Listen for the event.
elem.addEventListener(
  "build",
  (e) => {
    /* … * /
},
false,
);

// Dispatch the event.
elem.dispatchEvent(event);


const event = new Event("steve_transfer_changed");


// drop down = elem

*/


function calc_index_is_disabled(p_path, p_value)
{
    const dictionary = disable_on_selected_item_list.get("tracking/admin_info/steve_transfer");
    var result = false;
    var compare = false;

    for(const [k, v] of dictionary)
    {
        if(p_path.indexOf(p_path) == 0)
        {
            compare = true;
            break;
        }

    }

    if(!compare)
        return result;


    if(p_value == 1)
    {
       result = true;
    }
    
    return result;
}

function perform_index_enable_disable
(
    p_value, 
    p_dictionary_path, 
    p_form_index, 
    p_grid_index
)
{
    if(p_value == 1)
    {
       $mmria.set_disable(p_dictionary_path, p_form_index, p_grid_index) 
    }
    else
    {
        $mmria.set_enable(p_dictionary_path, p_form_index, p_grid_index)
    }
}

var is_faulted = false;

const peg_parser = peg.generate(`
start = blank_space html_start_tag  (blank_space ( balanced_tag / single_tag ) blank_space)* blank_space html_end_tag blank_space
html_start_tag = '<html>'
html_end_tag = '</html>'


single_tag = horizontal_line_tag / soft_return_tag
horizontal_line_tag = '<hr>'
soft_return_tag = '<br>'

balanced_tag = paragraph_tag / table_tag / unordered_list_tag / ordered_list_tag

in_line_able_tag = span_tag / bold_tag / underline_tag / italic_tag


span_in_line_able_tag =  bold_tag / underline_tag / italic_tag

paragraph_tag = paragraph_start_tag (inner_text / in_line_able_tag)+ paragraph_end_tag
paragraph_start_tag = '<p>' / '<p ' + style_attribute + '>'
paragraph_end_tag = '</p>'


span_tag = span_start_tag (inner_text / span_in_line_able_tag)+ span_end_tag
span_start_tag = '<span>' / '<span ' + style_attribute + '>'
span_end_tag = '</span>'

bold_tag = bold_start_tag inner_text bold_end_tag
bold_start_tag = '<b>'
bold_end_tag = '</b>'

underline_tag = underline_start_tag inner_text underline_end_tag
underline_start_tag = '<u>'
underline_end_tag = '</u>'

italic_tag = italic_start_tag inner_text italic_end_tag
italic_start_tag = '<i>'
italic_end_tag = '</i>'


unordered_list_tag = unordered_list_start_tag (blank_space list_item_tag)* blank_space unordered_list_end_tag
unordered_list_start_tag = '<ul>'
unordered_list_end_tag = '</ul>'


ordered_list_tag = ordered_list_start_tag  (blank_space list_item_tag)* blank_space ordered_list_end_tag
ordered_list_start_tag = '<ol>'
ordered_list_end_tag = '</ol>'

list_item_tag = list_item_start_tag (inner_text / in_line_able_tag)* list_item_end_tag
list_item_start_tag = '<li>'
list_item_end_tag = '</li>'

table_tag = table_start_tag (blank_space table_row_tag)* blank_space table_end_tag
table_start_tag = '<table>'  / '<table ' + table_attribue_list + '>' 
table_end_tag = '</table>'


table_row_tag = table_row_start_tag (blank_space table_header_tag / blank_space table_detail_tag)* blank_space table_row_end_tag
table_row_start_tag = '<tr>' / '<tr ' + table_attribue_list + '>' 
table_row_end_tag = '</tr>'

table_header_tag = table_header_start_tag (blank_space inner_text / blank_space in_line_able_tag)* blank_space table_header_end_tag
table_header_start_tag = '<th>' / '<th ' + table_attribue_list + '>' 
table_header_end_tag = '</th>'

table_detail_tag = table_detail_start_tag (blank_space inner_text / blank_space in_line_able_tag)* blank_space table_detail_end_tag
table_detail_start_tag = '<td>' / '<td ' + table_attribue_list + '>' 
table_detail_end_tag = '</td>'


style_attribute =  'style="' + name_value_list + '"'

name_value_list = name_value_pair / (name_value_pair + ';')+
name_value_pair = color_name + ':' + color_value
/ color_name + ': ' + color_value
/ backgroud_color_name + ':' + color_value 
/ backgroud_color_name + ': ' + color_value
/ text_align_name + ':' + align_attribute_value
/ text_align_name + ': ' + align_attribute_value
/ vertical_align_name + ':' + vertical_align_value
/ vertical_align_name + ': ' + vertical_align_value
/ font_family_name + ':' + font_family_value
/ font_family_name + ': ' + font_family_value
/ font_size_name + ':' + font_size_value
/ font_size_name + ': ' + font_size_value
/ width_attribute_name + ':' + width_attribute_value
/ width_attribute_name + ': ' + width_attribute_value
/ height_attribute_name + ':' + height_attribute_value
/ height_attribute_name + ': ' + height_attribute_value


font_family_name = 'font-family'
font_family_value = 'Times New Roman' /  'Calibri' / 'Ariel' / 'Helvetica' / 'Times' / 'serif' / 'sans-serif' / 'monospace'

/* font_family_value = [ a-zA-Z0-9,]* */

font_size_name = 'font-size'
font_size_value = '9pt' / '11pt' / '12pt' / '14pt' / '16pt' / '18pt'

color_name = 'color'
backgroud_color_name = 'background-color'

color_value = color_hex_value / color_name_value
color_hex_value = '#' + [a-fA-F0-9][a-fA-F0-9][a-fA-F0-9][a-fA-F0-9][a-fA-F0-9][a-fA-F0-9]
color_name_value = 'black' / 'red' / 'yellow' / 'green' / 'purple' / 'orange'


vertical_align_name = 'vertical-align'
vertical_align_value = 'baseline' /'text-top' / 'text-bottom' / 'super' / 'sub'
text_align_name = 'text-align'


table_attribue_list = table_attribue / (table_attribue + one_or_more_blank_space)+ 

table_attribue = valign_attribute_name + '=' + valign_attribute_value
/ align_attribute_name + '=' + align_attribute_value
/ width_attribute_name + '=' + width_attribute_value
/ height_attribute_name + '=' + height_attribute_value
/ col_span_attribute_name + '=' + col_span_attribute_value
/ row_span_attribute_name + '=' + row_span_attribute_value
/ border_attribute_name + '=' + border_attribute_value
/ style_attribute

valign_attribute_name = 'valign'
valign_attribute_value = 'top' / 'middle' / 'bottom' / 'baseline'

align_attribute_name = 'align'
align_attribute_value = 'left' / 'center' / 'right' / 'justify' / 'char'

width_attribute_name = 'width' 
width_attribute_value = one_or_more_digits + 'px'

height_attribute_name = 'height'
height_attribute_value = one_or_more_digits + 'px'

col_span_attribute_name = 'colspan'
col_span_attribute_value = one_or_more_digits

row_span_attribute_name = 'rowspan'
row_span_attribute_value = one_or_more_digits

border_attribute_name = 'border'
border_attribute_value = one_or_more_digits


//one_or_more_spaces = [ ]+
one_or_more_digits = [0-9]+

inner_text = basic_text / entity_text

entity_text = '&amp;' / '&lt;'
basic_text = [\\] a-zA-Z0-9\\.\\n\\[\\+\\*\\(\\)"'!@#$%^,>:;\\?=_-]+
blank_space "Blank space" = [ \\t\\n\\r]*
one_or_more_blank_space "One or more blank space" = [ \\t\\n\\r]+

`);


const save_queue = {
    is_active: false,
    item_list: [],
}

function get_new_save_queue_item
(
    p_data, 
    p_call_back, 
    p_note
)
{
    return {
        id: $mmria.get_new_guid(),
        date_created: new Date(),
        date_completed: null,
        data: p_data, 
        call_back: p_call_back, 
        note: p_note,
        is_data_analyst_mode: g_is_data_analyst_mode,
        post_rev: null
    };
}

async function g_set_data_object_from_path
(
  p_object_path,
  p_metadata_path,
  p_dictionary_path,
  value,
  p_form_index,
  p_grid_index,
  p_date_object,
  p_time_object
) 
{

  //save_start_time = performance.now();
  var is_search_result = false;
  var search_text = null;

  if 
  (
    g_ui.url_state.selected_id &&
    g_ui.url_state.selected_id == 'field_search'
  ) 
  {
    is_search_result = true;
    search_text = g_ui.url_state.path_array[2].replace(/%20/g, ' ');
  }

  var current_value = eval(p_object_path);

  if (g_validator_map[p_metadata_path]) 
  {
    if (g_validator_map[p_metadata_path](value)) 
    {
      var metadata = eval(p_metadata_path);

    if (metadata.type.toLowerCase() == 'boolean') 
    {
        eval(p_object_path + ' = ' + value);
    } 
    else 
    {
        eval(
          p_object_path +
            ' = "' +
            value.trim().replace(/"/g, '\\"').replace(/\n/g, '\\n') +
            '"'
        );
      }
      g_data.date_last_updated = new Date();

      //g_data.last_updated_by = g_uid;

      g_change_stack.push({
        _id: g_data._id,
        _rev: g_data._rev,
        object_path: p_object_path,
        metadata_path: p_metadata_path,
        old_value: Array.isArray (current_value)?JSON.stringify(current_value) : current_value,
        new_value: value,
        dictionary_path: p_dictionary_path,
        metadata_type: metadata.type,
        prompt: metadata.prompt,
        date_created: new Date().toISOString(),
        user_name: g_user_name,
        form_index: p_form_index,
        grid_index: p_grid_index
      });

      window.setTimeout(async ()=> { await autorecalculate(p_dictionary_path) });

        set_local_case
        (
            g_data, 
            function () 
            {
                var post_html_call_back = [];

                document.getElementById
                (
                    convert_object_path_to_jquery_id(p_object_path)
                ).innerHTML = page_render(
                metadata,
                eval(p_object_path),
                g_ui,
                p_metadata_path,
                p_object_path,
                '',
                false,
                post_html_call_back
                ).join('');
                if (post_html_call_back.length > 0) 
                {
                    eval(post_html_call_back.join(''));
                }

                apply_validation();
            }
        );
    } 
    else 
    {
        // do nothing for now
      //g_ui.broken_rules[p_object_path] = true;
      //console.log("didn't pass validation");
    }
  } 
  else 
  {
    var metadata = eval(p_metadata_path);
    var current_value = eval(p_object_path);
    var valid_date_or_datetime = true;
    var entered_date_or_datetime_value = value;

    if(metadata === null)
      return;

    if(metadata.type.toLowerCase() == 'html_area')
    {
        try
        {
            peg_parser.parse(value);
            document.getElementById("ii-validation").value  = "passed html validation";
            eval(
                p_object_path +
                  ' = "' +
                  value.replace(/"/g, '\\"').replace(/\n/g, '\\n') +
                  '"'
              );
        }
        catch(e)
        {
            const el = document.getElementById(convert_object_path_to_jquery_id(p_object_path) + '_control');
            let from = e.location.start.offset-5;
            
            if(from < 0)
            {
                from = 0;
            }

            let end = 10;
            if(from + end > el.value.length)
            {
                end = el.value.length - from;
            }

            const el_value = el.value.substr(from, end).replace(/</g, "&lt;");

            document.getElementById("ii-validation").innerHTML = `
            <p>Line: ${e.location.start.line} Column: ${e.location.start.column} expected: ${e.expected[0].type} ${e.expected[0].text}</p>
            <p style="color:#990000"> -> ${el_value}</p>
            <p>${e.message}</p>
            
            `;

            el.setSelectionRange(from, from + end);
            el.focus();

            return;
        }

        
    }
    else if 
    (
      metadata.type.toLowerCase() == 'list' &&
      metadata['is_multiselect'] &&
      metadata.is_multiselect == true
    ) 
    {
      let item = eval(p_object_path);
      

      if
      (
        metadata.data_type=='number' && 
        !isNaN(parseInt(value))
      )
      {
        const number_value = parseInt(value);

        item = list_create_number_set(item);
        
        if 
        (

           item.has(number_value)
        ) 
        {
            item.delete(number_value);
        } 
        else 
        {
            item.add(number_value);
        }

      }
      else
      {
        item = new Set(item);
        if (item.has(value)) 
        {
            item.delete(value);
        } 
        else 
        {
            item.add(value);
        }
      }

      const new_list = Array.from(item);
      eval(p_object_path + ' = ' + JSON.stringify(new_list));
      
    } 
    else if (metadata.type.toLowerCase() == 'boolean') 
    {
      eval(p_object_path + ' = ' + value);
    }
    else if (metadata.type.toLowerCase() == 'date') 
    {


      if (!is_valid_date(value)) 
      {
        valid_date_or_datetime = false;
        eval(
            p_object_path +
                ' = ""'
            );
      }
      else
      {
          if(value!= null && value!="")
          {
            let save_datetime = new Date(value);
            eval(
                p_object_path +
                ' = "' +
                convert_date_to_storage_format(save_datetime).replace(/"/g, '\\"').replace(/\n/g, '\\n') +
                '"'
            );
          }
          else
          {
            eval(
                p_object_path +
                    ' = ""'
                );
          }

      }
    } 
    else if (metadata.type.toLowerCase() == 'datetime') 
    {
      if (!is_valid_datetime(value)) 
      {
        valid_date_or_datetime = false;
        eval(
            p_object_path +
                ' = ""'
            );
      }
      else
      {

        if(value!= null && value!="")
        {
            let save_datetime = new Date(value);
            eval(
                p_object_path +
                ' = "' +
                save_datetime.toISOString().replace(/"/g, '\\"').replace(/\n/g, '\\n') +
                '"'
            );
        }
        else
        {
            eval(
                p_object_path +
                    ' = ""'
                );
        }
      }
    }
    else if(typeof value == "number")
    {
        eval(
            p_object_path +
                ' = "' +
                value +
                '"'
            );
    } 
    else 
    {


      try
      {
          
        eval(
            p_object_path +
              ' = "' +
              value.trim().replace(/\\/g, '/').replace(/"/g, '\\"').replace(/\n/g, '\\n') +
              '"'
          );

      }
      catch(e)
      {
          const err = {
              status: 500,
              responseText : `unable to save field: ${p_dictionary_path}\n${e}`
          };
          $mmria.field_save_error_dialog_show(err, `unable to save field: ${p_dictionary_path} `);
      }

    }

    g_change_stack.push({
        _id: g_data._id,
        _rev: g_data._rev,
      object_path: p_object_path,
      metadata_path: p_metadata_path,
      old_value: Array.isArray (current_value)?JSON.stringify(current_value) : current_value,
      new_value: value,
      dictionary_path: p_dictionary_path,
      metadata_type: metadata.type,
      prompt: metadata.prompt,
      date_created: new Date().toISOString(),
      user_name: g_user_name,
      form_index: p_form_index,
      grid_index: p_grid_index
    });

    g_data.date_last_updated = new Date();
    //g_data.last_updated_by = g_uid;

    if(!g_is_pmss_enhanced)
    {
        window.setTimeout(async ()=> { await autorecalculate(p_dictionary_path, p_form_index, p_grid_index) });
    }
    

if
(
    metadata.type.toLowerCase() == 'date' &&
    valid_date_or_datetime
)
{
    set_local_case
    (
        g_data,
        function () 
        {
            gui_remove_broken_rule_click(convert_object_path_to_jquery_id(p_object_path));
        }
    );
}
else if
(
    metadata.type.toLowerCase() == 'datetime' &&
    valid_date_or_datetime
)
{
    set_local_case
    (
        g_data,
        function () 
        {

            const new_value = eval(p_object_path);

            let date_part_display_value = "";
            let time_part_display_value = '00:00:00';
            if(new_value != null && new_value != "")
            {
                /*
                 do nothing
                 
                */
            }
            else
            {
                document.getElementById(convert_object_path_to_jquery_id(p_object_path) + '-time').value = time_part_display_value;
            }

            gui_remove_broken_rule_click(convert_object_path_to_jquery_id(p_object_path));
        }
    );
}
else
    set_local_case
    (
        g_data, 
        function () 
        {
            var post_html_call_back = [];

            let ctx = {
                form_index: p_form_index,
                grid_index: p_grid_index,
                is_valid_date_or_datetime: valid_date_or_datetime,
                entered_date_or_datetime_value: entered_date_or_datetime_value,
            };

            if (is_search_result) 
            {
                let new_context = get_seach_text_context
                (
                [],
                post_html_call_back,
                metadata,
                eval(p_object_path),
                p_dictionary_path,
                p_metadata_path,
                p_object_path,
                search_text,
                false,
                ctx.form_index,
                ctx.grid_index,
                valid_date_or_datetime,
                entered_date_or_datetime_value
                );

                render_search_text(new_context);

                var new_html = new_context.result.join('');
                let result = $('#' + convert_object_path_to_jquery_id(p_object_path));

                if (result.length) 
                {
                result[0].outerHTML = new_html;
                } 
                else 
                {
                result.replaceWith(new_html);
                }
                //$("#" + convert_object_path_to_jquery_id(p_object_path))[0].outerHTML = new_html;
            }
            else if (metadata.type.toLowerCase() == 'textarea') 
            {
                var new_html = page_render(
                metadata,
                eval(p_object_path),
                g_ui,
                p_metadata_path,
                p_object_path,
                p_dictionary_path,
                false,
                post_html_call_back,
                null,
                ctx
                ).join('');

                $(
                '#' + convert_object_path_to_jquery_id(p_object_path)
                )[0].outerHTML = new_html;
            } 
            else 
            {
                var new_html = page_render(
                metadata,
                eval(p_object_path),
                g_ui,
                p_metadata_path,
                p_object_path,
                p_dictionary_path,
                false,
                post_html_call_back,
                null,
                ctx
                ).join('');

                $('#' + convert_object_path_to_jquery_id(p_object_path)).replaceWith(
                new_html
                );
                //$("#" + convert_object_path_to_jquery_id(p_object_path))[0].outerHTML = new_html;
            }

            switch (metadata.type.toLowerCase()) 
            {
                case 'time':
                $(
                    '#' + convert_object_path_to_jquery_id(p_object_path) + ' input'
                ).datetimepicker({
                    format: 'HH:mm:ss',
                    defaultDate: '',
                    keepInvalid: true,
                    useCurrent: false,
                    icons: {
                    time: 'x24 fill-p cdc-icon-clock_01',
                    date: 'x24 fill-p cdc-icon-calendar_01',
                    up: 'x24 fill-p cdc-icon-chevron-circle-up',
                    down: 'x24 fill-p cdc-icon-chevron-circle-down',
                    previous: 'x24 fill-p fill-p cdc-icon-chevron-circle-left-light',
                    next: 'x24 fill-p cdc-icon-chevron-circle-right-light',
                    },
                });
                break;

                case 'date':
                  $(`#${convert_object_path_to_jquery_id(p_object_path)} input`).datetimepicker({
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
                  });
                  
                  // flatpickr("#" + convert_object_path_to_jquery_id(p_object_path) + " input.date", {
                  //     utc: true,
                  //     enableTime: false,
                  //     defaultDate: value,
                  //     onChange: function(selectedDates, p_value, instance) {
                  //         g_set_data_object_from_path(p_object_path, p_metadata_path, p_dictionary_path, p_value);
                  //     }
                  // });

                  break;

                case 'datetime':
                  $(`#${convert_object_path_to_jquery_id(p_object_path)}-date`).datetimepicker({
                    format: 'MM/DD/YYYY',
                    keepInvalid: true,
                    useCurrent: false,
                    useStrict: true,
                    icons: {
                      up: "x16 cdc-icon-chevron-circle-up-light",
                      down: "x16 cdc-icon-chevron-circle-down-light",
                      previous: 'x16 cdc-icon-chevron-double-right',
                      next: 'x16 cdc-icon-chevron-double-right'
                    }
                  });

                  $(`#${convert_object_path_to_jquery_id(p_object_path)}-time`).datetimepicker({
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


                 if (!isNullOrUndefined(p_date_object)) 
                 {
                  post_html_call_back.push(
                    `$('#${convert_object_path_to_jquery_id(
                        p_object_path
                    )}-time').focus();`
                    );
                 }
                 
                break;

                case 'date':

                break;

                case 'number':
                //$("#" + convert_object_path_to_jquery_id(p_object_path) + " input.number").numeric();
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number'
                ).numeric();
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number0'
                ).numeric({ decimal: false });
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number1'
                ).numeric({ decimalPlaces: 1 });
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number2'
                ).numeric({ decimalPlaces: 2 });
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number3'
                ).numeric({ decimalPlaces: 3 });
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number4'
                ).numeric({ decimalPlaces: 4 });
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number5'
                ).numeric({ decimalPlaces: 5 });
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number'
                ).attr('size', '15');
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number0'
                ).attr('size', '15');
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number1'
                ).attr('size', '15');
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number2'
                ).attr('size', '15');
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number3'
                ).attr('size', '15');
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number4'
                ).attr('size', '15');
                $(
                    '#' +
                    convert_object_path_to_jquery_id(p_object_path) +
                    ' input.number5'
                ).attr('size', '15');

                break;

                case 'list':
                if 
                (
                    metadata.control_style != null &&
                    metadata.control_style == 'radio'
                ) 
                {
                    //console("bubba");
                    post_html_call_back.push
                    (
                        `$('#${convert_object_path_to_jquery_id(
                            p_object_path
                        )}${value}').focus()`
                    );
                }
                break;
            }

            if (post_html_call_back.length > 0) 
            {
                eval(post_html_call_back.join(''));
            }

            apply_validation();


        }
    );
  }

  window.setTimeout(function() { update_charts(p_dictionary_path) }, 0);
 //console.log('test');
}



function g_add_grid_item(p_object_path, p_metadata_path, p_dictionary_path) 
{
  
  let metadata = eval(p_metadata_path);
  let new_line_item = create_default_object(metadata, {}, true);
  let grid = eval(p_object_path);

  grid.push(new_line_item[metadata.name][0]);
  set_local_case(g_data, function () {
    let post_html_call_back = [];
    let render_result = page_render(
      metadata,
      eval(p_object_path),
      g_ui,
      p_metadata_path,
      p_object_path,
      p_dictionary_path,
      false,
      post_html_call_back
    ).join('');

    let element = document.getElementById(p_metadata_path);
    element.outerHTML = render_result;

    apply_tool_tips();

    let jump_value = 9999;
    
    post_html_call_back.push
    (
      `document.getElementById("${p_metadata_path}").children[1].scrollTop = ${jump_value};
      set_focus_on_first_grid_item("${p_metadata_path}");`
    );

    if (post_html_call_back.length > 0) 
    {
      eval(post_html_call_back.join(''));
    }
  });
  

}


function set_focus_on_first_grid_item(p_metadata_path)
{

    var element = document.getElementById(p_metadata_path);
    let li_list = element.querySelectorAll("ul li");
    var lastchild = li_list[li_list.length-1];
    lastchild.querySelector("input, select, textarea, button").focus();
}

function g_delete_grid_item
(
	p_object_path,
	p_metadata_path,
    p_dictionary_path,
    p_metadata_prompt,
    p_data_length,
	p_index
) 
{
    var record_number = new Number(p_index) + new Number(1);

    const modal = build_delete_grid_dialog(record_number, p_object_path, p_metadata_path, p_dictionary_path, p_index, p_metadata_prompt, p_data_length);
    const box = $("#content");

    box.append(modal[0]);
    $(`#case_modal_${p_index}`).modal("show");
    $(`#case_modal_${p_index} .modal-footer .modal-cancel`).focus();
	
}

function g_delete_grid_item_action
    (
        p_object_path,
        p_metadata_path,
        p_dictionary_path,
        p_index
    )
{
	var metadata = eval(p_metadata_path);
	var index = p_object_path
		.match(new RegExp("\\[\\d+\\]$"))[0]
		.replace("[", "")
		.replace("]", "");
	var object_string = p_object_path.replace(new RegExp("(\\[\\d+\\]$)"), "");

	eval(object_string).splice(index, 1);

	set_local_case(g_data, function () {
		var post_html_call_back = [];

		var render_result = page_render(
			metadata,
			eval(object_string),
			g_ui,
			p_metadata_path,
			object_string,
			p_dictionary_path,
			false,
			post_html_call_back
		).join("");
		var element = document.getElementById(p_metadata_path);
		element.outerHTML = render_result;
		if (post_html_call_back.length > 0) {
			eval(post_html_call_back.join(""));
		}
    });


}

async function g_duplicate_record_item(p_object_path, p_metadata_path, p_index) 
{
    const metadata = eval(p_metadata_path);
    var object_string = p_object_path.replace(new RegExp("(\\[\\d+\\]$)"), "");

    const original = eval(object_string)[p_index];

    let clone = {};

    clone_multiform_object
    (
        metadata, 
        clone, 
        false,
        original,
        metadata.name
    )

    const multiform_path = p_object_path.substring(0, p_object_path.indexOf("["));
    var form_array = eval(multiform_path);      
    form_array.push(clone[metadata.name]);
    
    g_apply_sort(metadata, form_array, p_metadata_path, multiform_path, "/" + metadata.name);
    
        await save_case
        (
            g_data,
            function () 
            {
                var post_html_call_back = [];
                document.getElementById(metadata.name + '_id').innerHTML = page_render
                (
                    metadata,
                    form_array,
                    g_ui,
                    p_metadata_path,
                    multiform_path,
                    metadata.name,
                    false,
                    post_html_call_back
                ).join('');
                if (post_html_call_back.length > 0) 
                {
                    eval(post_html_call_back.join(''));
                }
            }
        );

        $mmria.duplicate_multiform_dialog_click();

}


function g_delete_record_item(p_object_path, p_metadata_path, p_index) 
{
		var metadata = eval(p_metadata_path);
		var index = p_object_path
			.match(new RegExp("\\[\\d+\\]$"))[0]
			.replace("[", "")
			.replace("]", "");
		var object_string = p_object_path.replace(new RegExp("(\\[\\d+\\]$)"), "");

		eval(object_string).splice(index, 1);
		set_local_case(g_data, function () {
			var post_html_call_back = [];
			document.getElementById(metadata.name + "_id").innerHTML = page_render(
				metadata,
				eval(object_string),
				g_ui,
				p_metadata_path,
				object_string,
				"",
				false,
				post_html_call_back
			).join("");
			if (post_html_call_back.length > 0) {
				eval(post_html_call_back.join(""));
			}
		});
}




var $$ = {
  is_id: function (value) {
    // 2016-06-12T13:49:24.759Z
    if (value) {
      var test = value.match(/^\d+-\d+-\d+T\d+:\d+:\d+.\d+Z$/);
      return test ? true : false;
    } else {
      return false;
    }
  },
};

$(function () 
{
    /*
    if (window.IsDuplicate()) 
    {

      //alert("This is duplicate window\n\n Closing...");

      //document.getElementById('form_content_id').innerHTML = "It looks like you may have opened the view/edit case data in another browser tab.<br/> To ensure proper handling please use one broswer tab for editing a case.";

      //window.close();
 
      //return;
    }*/
  

  $(document).keydown(function (evt) 
  {
    if (evt.keyCode == 90 && evt.ctrlKey) 
    {
      evt.preventDefault();
      undo_click();
    }
  });

  let working_space = 1000; // 1GB
  let default_local_storage_limit = 5000; // 5GB

  /*
  let working_space = 100;
  let default_local_storage_limit = 300; */



  window.setTimeout(()=> { $mmria.get_cvs_api_server_info(()=>{},()=>{}); }, 0);

  $('#profile_form2').on('submit', navigation_away);

  if 
  (
    default_local_storage_limit - get_local_storage_space_usage_in_kilobytes() <
    working_space
  ) 
  {
    let case_index = create_local_storage_index();
    let case_index_array = convert_local_storage_index_to_array(case_index);
    let is_update_case_index = false;
    let space_removed = 0;

    for 
    (
      let index = 0;
      index < case_index_array.length && space_removed < working_space;
      index++
    ) 
    {
      let item = case_index_array[index];
      let key = item._id;

      try 
      {
        delete case_index[key];
        space_removed += item.size_in_kilobytes;
        is_update_case_index = true;
        localStorage.removeItem('case_' + key);
      } 
      catch (ex) 
      {
        //console.log("remove this");
      }
    }

    if (is_update_case_index) 
    {
      window.localStorage.setItem('case_index', JSON.stringify(case_index));
    }
  }

  if (window.location.pathname == '/analyst-case') 
  {
    g_is_data_analyst_mode = 'da';
  }


  // https://pure-essence.net/2010/02/14/jquery-session-timeout-countdown/
  // create the warning window and set autoOpen to false
  var sessionTimeoutWarningDialog = $('#sessionTimeoutWarningDiv');

  $('#sessionTimeoutOkButton').click(function () {
    // close dialog

    clearInterval(timer);
    running = false;
    $('#sessionTimeoutWarningDiv').dialog('close');
    profile.update_session_timer();
  });

  //$(sessionTimeoutWarningDialog).html(initialSessionTimeoutMessage);
  $(sessionTimeoutWarningDialog).dialog({
    title: 'Session Expiration Warning',
    autoOpen: false, // set this to false so we can manually open it
    closeOnEscape: false,
    draggable: false,
    width: 600,
    minHeight: 50,
    backgroundColor: 0xadc71a, // rgb(173, 199, 26),
    modal: true,
    beforeclose: function () {
      // bind to beforeclose so if the user clicks on the "X" or escape to close the dialog, it will work too
      // stop the timer
      clearInterval(timer);

      // stop countdown
      running = false;
    },
    buttons: {
      OK: function () {
        // close dialog

        clearInterval(timer);
        running = false;
        $(this).dialog('close');
        profile.update_session_timer();
      },
    },
    resizable: false,
    open: function () {
      // scrollbar fix for IE
      $('#sessionTimeoutWarningDiv').css('display', 'block');
      $('body').css('overflow', 'hidden');
      $('#sessionTimeoutExpiredId').hide();
      $('#sessionTimeoutPendingId').css('display', 'block');
    },
    close: function () {
      // reset overflow
      $('body').css('overflow', 'auto');
      clearInterval(timer);
      running = false;
      $(this).dialog('close');
      profile.update_session_timer();
    },
  });
  // end of dialog

  // start the idle timer
  //$.idleTimer(idleTime);

  // bind to idleTimer's idle.idleTimer event
  $(document).bind('sessionWarning', function () {
    $(sessionTimeoutWarningDialog).show();
    // if the user is idle and a countdown isn't already running
    //if($.data(document,'idleTimer') === 'idle' && !running)
    if (!running) {
      var counter = redirectAfter;
      running = true;

      // intialisze timer
      $('#' + sessionTimeoutCountdownId).html(redirectAfter);
      // open dialog
      $(sessionTimeoutWarningDialog).dialog('open');

      // create a timer that runs every second
      timer = setInterval(function () {
        counter -= 1;

        // if the counter is 0, redirect the user
        if (counter === 0) {
          //$(sessionTimeoutWarningDialog).html(expiredMessage);
          $('#sessionTimeoutExpiredId').show();
          $('#sessionTimeoutPendingId').css('display', 'none');
          $(sessionTimeoutWarningDialog).dialog('disable');
          //window.location = redirectTo;
          running = false;
          clearInterval(timer);
          clearInterval(session_warning_interval_id);
          profile.logout();
        } else {
          $('#' + sessionTimeoutCountdownId).html(counter);
        }
      }, 1000);
    }
  });

  //set_session_warning_interval();

  $.datetimepicker.setLocale('en');

  g_process_save_interval = window.setInterval(process_save_case, 1000);

  window.setTimeout(load_and_set_data, 0);
});


async function Get_Record_Id_List(p_call_back) 
{
    const url = `${location.protocol}//${location.host}/api/case_view/record-id-list`;

    const response = await $.ajax
    ({
        url: url,
    });

    if(response!= null)
    {
        for(var i = 0; i < response.length; i++)
        {
            let item = response[i];
            g_record_id_list.add(item.toUpperCase());
        }

        if(p_call_back!= null)
        {
            p_call_back();
        }
    }

}

async function load_and_set_data() 
{
    const metadata_url = `${location.protocol}//${location.host}/api/jurisdiction_tree`;

    const jurisdiction_tree = await $.ajax
    ({
        url: metadata_url,
    });

    const form_access_response = await get_form_access_list();

    for(const item of form_access_response.access_list)
    {
        
        g_form_access_list.set(item.form_path.substr(1), item);
    }

    g_jurisdiction_tree = jurisdiction_tree;

    const my_user_response = await $.ajax
    ({
        url: location.protocol + '//' + location.host + '/api/user/my-user',
    });

    if(!g_is_pmss_enhanced)
    {
        const duplicate_path_set_response = await $.ajax
        ({
            url: location.protocol + '//' + location.host + '/Case/GetDuplicateMultiFormList',
        });

        for(const i of duplicate_path_set_response.field_list)
        {
            g_duplicate_path_set.add(i);
        }
    }
    
    g_user_name = my_user_response.name;


    const my_role_list_response = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/user_role_jurisdiction_view/my-roles`, //&search_key=' + g_uid,
    });
    
    g_user_role_jurisdiction_list = [];
    for (let i in my_role_list_response.rows) 
    {
        let value = my_role_list_response.rows[i].value;
        role_set.add(value.role_name);
        if(value.role_name=="abstractor")
        {
            g_user_role_jurisdiction_list.push(value.jurisdiction_id);
        }
        else if(value.role_name=="jurisdiction_admin")
        {
            g_is_jurisdiction_admin = true;
        }
    }

    if
    (
        g_user_role_jurisdiction_list.length == 0 &&
        my_role_list_response.rows.length == 1 &&
        my_role_list_response.rows[0].value.role_name == "vro"
    )
    {
        const value = my_role_list_response.rows[0].value;
        g_user_role_jurisdiction_list.push(value.jurisdiction_id);

        g_ui.case_view_request.status = "STEVE: Pending Vro Investigation";
        g_ui.case_view_request.jurisdiction = value.jurisdiction_id.substr(1);
    }

    if(location.href.endsWith("/CaseVRO"))
    {
        g_ui.case_view_request.status = "STEVE: Pending Vro Investigation";
    }

    create_jurisdiction_list(g_jurisdiction_tree);

    $('#landing_page').hide();
    $('#logout_page').hide();
    $('#footer').hide();
    $('#root').removeClass('header');

    const release_version = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/version/release-version`,
    });
    
    
    g_release_version = release_version;

    const default_ui_specification = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/version/${g_release_version}/ui_specification`,
    });
  
    g_default_ui_specification = default_ui_specification;
    

    document.getElementById('form_content_id').innerHTML = '<h4>Fetching data from database.</h4><h5>Please wait a few moments...</h5>';

    const metadata_response = await $.ajax
    ({
        url: `${location.protocol}//${location.host}/api/version/${g_release_version}/metadata`,
    });

    g_metadata = metadata_response;
    metadata_summary(g_metadata_summary, g_metadata, 'g_metadata', 0, 0);
    default_object = create_default_object(g_metadata, {});

    build_other_specify_lookup(g_other_specify_lookup, g_metadata);

    set_list_lookup
    (
      g_display_to_value_lookup,
      g_value_to_display_lookup,
      g_value_to_index_number_lookup,
      g_metadata,
      '',
      'g_metadata'
    );


    for (let i in g_metadata.lookup) 
    {
      const child = g_metadata.lookup[i];

      g_look_up['lookup/' + child.name] = child.values;
    }

    

    g_ui.url_state = url_monitor.get_url_state(window.location.href);
    if (window.onhashchange) 
    {
      window.onhashchange({ isTrusted: true, newURL: window.location.href });
    } 
    else 
    {
      window.onhashchange = window_on_hash_change;
      window.onhashchange({ isTrusted: true, newURL: window.location.href });
    }

    window.onbeforeunload = navigation_away;


    await get_case_set();
}
  

function create_jurisdiction_list(p_case_folder) 
{
  for (var i = 0; i < g_user_role_jurisdiction_list.length; i++) 
  {
    var jurisdiction_regex = new RegExp('^' + g_user_role_jurisdiction_list[i]);
    var match = p_case_folder.name.match(jurisdiction_regex);

    if (match) 
    {
      g_jurisdiction_list.push(p_case_folder.name);
      break;
    }
  }

  if (p_case_folder.children != null) 
  {
    for (var i = 0; i < p_case_folder.children.length; i++) 
    {
      var child = p_case_folder.children[i];

      create_jurisdiction_list(child);
    }
  }
}

var update_session_timer_interval_id = null;

async function apply_filter_click() 
{
    g_ui.case_view_request.page=1;
    await get_case_set();
}

async function get_case_set(p_call_back) 
{

    //var url = `${location.protocol}//${location.host}/api/pinned_cases`;
    
  var case_view_url =
    location.protocol +
    '//' +
    location.host +
    '/api/case_view' +
    g_ui.case_view_request.get_query_string();




    if(g_is_data_analyst_mode == null || g_is_data_analyst_mode !="da")
    {
        var url = `${location.protocol}//${location.host}/api/pinned_cases`;
        g_pinned_case_set = await $.ajax
        ({
            url: url
        });
    }

    const case_view_response = await $.ajax({
        url: case_view_url,
    })  ;

    const new_list = [];
    const new_list_id_set = new Set();

    for(const i in g_ui.case_view_list)
    {
        const item = g_ui.case_view_list[i];
        if
        (
            app_is_item_pinned(item.id) != 0 && 
            !new_list_id_set.has(item.id)
        ) 
        { 
            new_list.push(item); 
            new_list_id_set.add(item.id); 
        }

    }

    g_ui.case_view_request.total_rows = case_view_response.total_rows;

    if(new_list.length != 0)
    {
        g_ui.case_view_list = new_list;
        
    }
    else
    {
        //case_view_response.rows.map((item, i) => { if(app_is_item_pinned(item.id) != 0 && !new_list_id_set.has(item.id)) { new_list.push(item); new_list_id_set.add(item.id)} });
        for(const i in case_view_response.rows)
        {
            const item = case_view_response.rows[i];
            if
            (
                app_is_item_pinned(item.id) != 0 && 
                !new_list_id_set.has(item.id)
            ) 
            { 
                new_list.push(item); 
                new_list_id_set.add(item.id);
            }

        }
        g_ui.case_view_list = new_list;
    }

    for (var i = 0; i < case_view_response.rows.length; i++) 
    {
        const item = case_view_response.rows[i];
        if(!new_list_id_set.has(item.id))
            g_ui.case_view_list.push(case_view_response.rows[i]);
    }

    if (p_call_back) 
    {
        p_call_back();
    } 
    else 
    {
        var post_html_call_back = [];

        document.getElementById('navbar').innerHTML = navigation_render
        (
            g_metadata,
            0,
            g_ui
            ).join('');
            document.getElementById('form_content_id').innerHTML =
            '<h4>Fetching data from database.</h4><h5>Please wait a few moments...</h5>';
            document.getElementById('form_content_id').innerHTML = page_render(
            g_metadata,
            default_object,
            g_ui,
            'g_metadata',
            'default_object',
            '',
            false,
            post_html_call_back
        ).join('');

        if (post_html_call_back.length > 0) 
        {
            eval(post_html_call_back.join(''));
        }

        var section_list = document.getElementsByTagName('section');

        for (var i = 0; i < section_list.length; i++) 
        {
            var section = section_list[i];

            if (section.id == 'app_summary') 
            {
                section.style.display = 'block';
            } 
            else 
            {
                section.style.display = 'block';
            }
        }
    }
}

async function window_on_hash_change(e) 
{

  if (g_data) 
  {
    
    if (e.isTrusted) 
    {
      var new_url = e.newURL || window.location.href;
      g_ui.url_state = url_monitor.get_url_state(new_url);

      if 
      (
        g_ui.url_state.path_array &&
        g_ui.url_state.path_array.length > 0 &&
        parseInt(g_ui.url_state.path_array[0]) >= 0
      ) 
      {

        g_apply_sort(g_metadata, g_data, "","", "");
        var case_id = g_data._id;

        if( g_ui.case_view_list[parseInt(g_ui.url_state.path_array[0])].id != case_id)
        {
            g_ui.broken_rules = {};
            chart_function_params_map.clear();
            g_charts.clear();
            g_chart_data.clear();
            if(g_data_is_checked_out)
            {
                await save_case(g_data, async function () 
                {
                await get_specific_case(
                    g_ui.case_view_list[parseInt(g_ui.url_state.path_array[0])].id
                );
                }, "hash_change");
            }
            else
            {
                await get_specific_case(
                    g_ui.case_view_list[parseInt(g_ui.url_state.path_array[0])].id
                );
            }

        }
        else
        {
            chart_function_params_map.clear();
            g_charts.clear();
            g_chart_data.clear();
            if(g_data_is_checked_out)
            {
                const current_data = g_data;
                window.setTimeout(async () =>await save_case(current_data,  null, "hash_change"), 0);
            }


            g_render();
            
        }
      } 
      else 
      {
        if(g_data_is_checked_out)
        {
            g_data.date_last_checked_out = null;
            g_data.last_checked_out_by = null;
            g_data_is_checked_out = false;

            g_apply_sort(g_metadata, g_data, "","", "");

            await save_case(g_data, async function () {
            g_data = null;
            await get_case_set(function () {
                g_render();
            });
            }, "hash_change");
        }
        else
        {
            g_data = null;
            await get_case_set(function () {
                g_render();
            });
        }
      }
    }
  } 
  else if (e.isTrusted) 
  {
    var new_url = e.newURL || window.location.href;

    g_ui.url_state = url_monitor.get_url_state(new_url);

    if 
    (
      g_ui.url_state.path_array &&
      g_ui.url_state.path_array.length > 0 &&
      parseInt(g_ui.url_state.path_array[0]) >= 0
    ) 
    {
        
      if (g_ui.case_view_list.length > 0) 
      {
        g_ui.broken_rules = {};
        chart_function_params_map.clear();
        g_charts.clear();
        g_chart_data.clear();
        await get_specific_case
        (
          g_ui.case_view_list[parseInt(g_ui.url_state.path_array[0])].id
        );
      } 
      else 
      {
        g_render();
      }
    
    } 
    else 
    {

      g_render();
    }
  } 
  else 
  {
    // do nothing for now
  }
}

async function get_specific_case(p_id) 
{
  const case_url = `${location.protocol}//${location.host}/api/case?case_id=${p_id}`;

  try
  {
    /*
    const case_response = await $.ajax({
        url: case_url,
    });   
    */

    const case_response_promise = await fetch(case_url, {
        method: "get",
        headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json; charset=utf-8',
        'dataType': 'json',
        },

    });

    mmria_check_if_need_to_redirect(case_response_promise);

    const case_response =  await case_response_promise.json();

    if (case_response) 
    {
        if(g_is_pmss_enhanced)
        {
            await Attachment_GetFileList(p_id);
        }
    
        if(!g_is_pmss_enhanced)
        {
            g_case_narrative_original_value = case_response.case_narrative.case_opening_overview;
        }

        let local_data = get_local_case(p_id);

        if (local_data) 
        {
        if (local_data._rev && local_data._rev == case_response._rev) 
        {
            g_data = local_data;
            g_data_is_checked_out = is_case_checked_out(g_data);
        } 
        else 
        {
            local_data = case_response;
            set_local_case(local_data);
            g_data = local_data;
            g_data_is_checked_out = is_case_checked_out(g_data);
        }

        if (g_autosave_interval != null && g_data_is_checked_out == false) 
        {
            window.clearInterval(g_autosave_interval);
            g_autosave_interval = null;
        }

            // do nothing
            //g_render();
        } 
        else 
        {
            g_data = case_response;
            g_data_is_checked_out = is_case_checked_out(g_data);

            if (g_autosave_interval != null && g_data_is_checked_out == false) 
            {
                window.clearInterval(g_autosave_interval);
                g_autosave_interval = null;
            }


        }


        g_render();
    } 
    else 
    {
        g_render();
    }


  }
  catch(e)
  {
    //
    //console.log('get_specific_case:', e);
    //if(e.message == 'Failed to fetch')
    //    window.location = "/Case"
    
    g_data = get_local_case(p_id);
    g_data_is_checked_out = is_case_checked_out(g_data);
  }



/*
    .done(function (case_response) 
    {
      
    })
    .fail(function (jqXHR, textStatus, errorThrown) 
    {
      console.log('get_specific_case:', textStatus, errorThrown);
      g_data = get_local_case(p_id);
      g_data_is_checked_out = is_case_checked_out(g_data);
    });
    */



}


async function save_case(p_data, p_call_back, p_note)
{
    const queue_item = get_new_save_queue_item(p_data, p_call_back, p_note);
    save_queue.item_list.push(queue_item);
}

async function process_save_case() 
{
    if(save_queue.is_active == true) return;

    if(save_queue.item_list.length == 0) return;

    const before_pop = save_queue.item_list.length;

    const item = save_queue.item_list.pop();

    const after_pop = save_queue.item_list.length;

    if(before_pop != after_pop + 1)
    {
        console.log("removal problem");
    }

    if(item == null || item == undefined) return;

    save_queue.is_active = true;

    const p_data = item.data;
    const p_call_back = item.call_back;
    const p_note = item.note;
    
    /*

    {
        id: $mmria.get_new_guid(),
        date_created: new Date(),
        date_completed: null,
        data: p_data, 
        call_back: p_call_back, 
        note: p_note,
        post_rev: null
    };

    */


  if (p_data.host_state == null || p_data.host_state == '') 
  {
    p_data.host_state = window.location.host.split('-')[0];
  }

  if (p_data.is_data_analyst_mode == null) 
  {

    if(p_data._id != g_data._id)
    {
        const err = {
            status: 500,
            responseText : "Save Logic Error: p_data._id != g_data._id "
        };
        $mmria.save_error_500_dialog_show(err, p_note);
        save_queue.is_active = false;
        return;
    }

    let save_case_request = { 
        Change_Stack:{
            _id: $mmria.get_new_guid(),
            case_id: p_data._id,
            case_rev: p_data._rev,
            date_created: new Date().toISOString(),
            user_name: g_user_name, 
            items: g_change_stack,
            metadata_version: g_release_version,
            note: (p_note != null)? p_note : ""

        },
        Case_Data:p_data
    };

    if(g_case_narrative_is_updated)
    {
        save_case_request.Change_Stack.items.push({
            _id: p_data._id,
            _rev: p_data._rev,
        object_path: "g_data.case_narrative.case_opening_overview",
        metadata_path: "/case_narrative/case_opening_overview",
        old_value: g_case_narrative_original_value,
        new_value: g_data.case_narrative.case_opening_overview,
        dictionary_path: "/case_narrative/case_opening_overview",
        metadata_type: "textarea",
        prompt: 'Case Narrative',
        date_created: g_case_narrative_is_updated_date.toISOString(),
        user_name: g_user_name
        });
    }
    


    let case_response = {};

    try
    {
        const case_response_promise = await fetch(location.protocol + '//' + location.host + '/api/case', {
            method: "post",
            headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json; charset=utf-8',
            'dataType': 'json',
            },
        
            //make sure to serialize your JSON body
            body: JSON.stringify(save_case_request)
        });

        mmria_check_if_need_to_redirect(case_response_promise);
        
        case_response = await case_response_promise.json();
        

        
    }  
    catch(xhr) 
    {
        //alert(`server save_case: failed\n${err}\n${xhr.responseText}`);

        $mmria.unstable_network_dialog_show(xhr, p_note);
        if (xhr.status == 401) 
        {
            let redirect_url = location.protocol + '//' + location.host;
            window.location = redirect_url;
        }
        else if (xhr.status == 200 && xhr.responseText.length >= 49000) 
        {
            let redirect_url = location.protocol + '//' + location.host;
            window.location = redirect_url;
        }
    }



    if
    (
        case_response.ok == null ||
        case_response.ok == false ||
        case_response.rev == null
    ) 
    {
        if
        (
            case_response != null &&
            case_response.error_description != null //&&
            //case_response.error_description.indexOf("(409) Conflict") > -1
        ) 
        {
            save_queue.is_active = false;

            const err_object = { "status": 500, "responseText": case_response.error_description }
            if(err_object.responseText.indexOf("(409) Conflict") > -1)
            {
                err_object.responseText ="Unable to save document Conflict";
                $mmria.save_error_500_dialog_show
                (
                    err_object, 
                    p_note + " (409) Conflict"
                );
            }
            else
            {
                $mmria.save_error_500_dialog_show
                (
                    err_object, 
                    p_note
                );
            }
            
            return;
        }

        const err = {
            status: 500,
            responseText : case_response.error_description
        };
        
        //$mmria.save_error_500_dialog_show(err, p_note);
        save_queue.is_active = false;
        return;
    } 
    else if
    (
        case_response.ok == true 
    )
    {
        g_change_stack = [];
        g_case_narrative_is_updated = false;
        g_case_narrative_is_updated_date = null;

        if(g_data._id == case_response.id)
        {


            g_data._rev = case_response.rev;
            g_data.last_updated_by = g_user_name;
            g_data_is_checked_out = is_case_checked_out(g_data);
            if(!g_is_pmss_enhanced)
            {
                g_case_narrative_original_value = g_data.case_narrative.case_opening_overview;
            }
            
            set_local_case(g_data);

            const node_list = document.querySelectorAll("#last_updated_span");
            for(const el of node_list)
            {
                if(el != null)
                {
                    const date_part_display_value = convert_datetime_to_local_display_value(
                        g_data.date_last_updated
                    );

                    const save_text = `${
                        g_data.last_updated_by
                    } ${date_part_display_value}`
                    
                    el.innerHTML = save_text;
                }
            }
            //console.log('set_value save finished');
        }
        else
        {
            console.log('save_case info data._id != case_response.id');
        }

        for(let i = 0; i < save_queue.item_list.length; i++)
        {
            const item = save_queue.item_list[i];
            if(item.data._id == case_response.id)
            {
                item.data._rev == case_response.rev
                //break;
            }
        }
    
        //save_queue.is_active = false;

    }
    else
    {
        console.log('save_case error case_response.ok != true');
        is_faulted = true;
        $mmria.save_error_500_dialog_show(`Prolem saving Please close case: is_faulted: true, g_data._id: ${g_data._id}\n case_response: ${case_response} please close case`, p_note);
    }
    
    save_queue.is_active = false;

    if (item.call_back) 
    {
        item.call_back();
    }


      
  } 
  else 
  {
    //save_queue.is_active = false;

    if (item.call_back) 
    {
      item.call_back();
    }
  }
}

async function delete_case(p_id, p_rev) 
{

    const case_response = await $.ajax({
        url:
          location.protocol +
          '//' +
          location.host +
          '/api/case?case_id=' +
          p_id +
          '&rev=' +
          p_rev,
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        //data: JSON.stringify(p_data),
        type: 'DELETE',
      }).fail(function (xhr, err) 
      {
        console.log('delete_case: failed', err);
      });
      
    try 
    {
        localStorage.removeItem('case_' + p_id);
    } 
    catch (ex) 
    {
    // do nothing for now
    }
    await get_case_set();
    
}


function g_render() 
{
  var post_html_call_back = [];

  document.getElementById('navbar').innerHTML = navigation_render
  (
    g_metadata,
    0,
    g_ui
  ).join('');

  $('[data-toggle="tooltip"]').tooltip({
    classes: {
      'ui-tooltip': 'custom-tooltip'
    },
    position: {
      my: "left-10 top", //position from top of tooltip
      at: "bottom+10" //at bottom of element
    }
  });

  document.getElementById('form_content_id').innerHTML = page_render
  (
    g_metadata,
    g_data,
    g_ui,
    'g_metadata',
    'g_data',
    '',
    false,
    post_html_call_back
  ).join('');

  apply_tool_tips();

  if (post_html_call_back.length > 0) 
  {
    try
    {
      eval(post_html_call_back.join(''));
    } 
    catch (ex) 
    {
      console.log(ex);
    }
  }

  var section_list = document.getElementsByTagName('section');

if (g_ui.url_state.path_array[0] == 'summary') 
{
    for (var i = 0; i < section_list.length; i++) 
    {
        var section = section_list[i];

        if (section.id == 'app_summary') 
        {
        section.style.display = 'block';
        //section.style.display = "grid";
        //section.style["grid-template-columns"] = "1fr 1fr 1fr";
        } 
        else 
        {
        section.style.display = 'none';
        }
    }
} 
else if 
(
    g_ui.url_state.path_array.length >= 2 &&
    g_ui.url_state.path_array[1] == 'field_search'
) 
{
    for (var i = 0; i < section_list.length; i++) 
    {
        var section = section_list[i];

        if (section.id == 'field_search_id') 
        {
            section.style.display = 'block';
            //section.style.display = "grid";
            //section.style["grid-template-columns"] = "1fr 1fr 1fr";
        } 
        else 
        {
            section.style.display = 'none';
        }
    }
} 
  else 
  {
    if 
    (
      g_ui.url_state.path_array.length > 2 &&
      parseInt(g_ui.url_state.path_array[0]) >= 0
    ) 
    {
      for (var i = 0; i < section_list.length; i++) 
      {
        var section = section_list[i];

        if (section.id == g_ui.url_state.path_array[1]) 
        {
          section.style.display = 'block';
          //section.style.display = "grid";
          //section.style["grid-template-columns"] = "1fr 1fr 1fr";
        } 
        else 
        {
          section.style.display = 'none';
        }
      }
    } 
    else 
    {
      for (var i = 0; i < section_list.length; i++) 
      {
        var section = section_list[i];

        if (section.id == g_ui.url_state.path_array[1] + '_id') 
        {
          section.style.display = 'block';
          //section.style.display = "grid";
          //section.style["grid-template-columns"] = "1fr 1fr 1fr";
        }
        else 
        {
          section.style.display = 'none';
        }
      }
    }
  }

  apply_validation();
  
}

function show_print_version() 
{
  window.open('./print-version', '_print_version');
}

function apply_tool_tips() 
{
  $('[rel=tooltip]').tooltip();
  $('.time').datetimepicker({
    format: 'HH:mm:ss',
    defaultDate: '',
    keepInvalid: true,
    useCurrent: false,
    icons: {
      time: 'x24 fill-p cdc-icon-clock_01',
      date: 'x24 fill-p cdc-icon-calendar_01',
      up: 'x24 fill-p cdc-icon-chevron-circle-up',
      down: 'x24 fill-p cdc-icon-chevron-circle-down',
      previous: 'x24 fill-p fill-p cdc-icon-chevron-circle-left-light',
      next: 'x24 fill-p cdc-icon-chevron-circle-right-light',
    },
  });


  $('input.number').numeric();
  $('input.number0').numeric({ decimal: false });
  $('input.number1').numeric({ decimalPlaces: 1 });
  $('input.number2').numeric({ decimalPlaces: 2 });
  $('input.number3').numeric({ decimalPlaces: 3 });
  $('input.number4').numeric({ decimalPlaces: 4 });
  $('input.number5').numeric({ decimalPlaces: 5 });
  $('input.number').attr('size', '15');
  $('input.number0').attr('size', '15');
  $('input.number1').attr('size', '15');
  $('input.number2').attr('size', '15');
  $('input.number3').attr('size', '15');
  $('input.number4').attr('size', '15');
  $('input.number5').attr('size', '15');


  apply_validation();
}

function apply_validation() 
{

    let list_has_items = false;
    let validation_summary = [];

    for (let key in g_ui.broken_rules) 
    {
        
        if(g_ui.broken_rules[key]!= null)
        {
            list_has_items = true;
            validation_summary.push(g_ui.broken_rules[key]);
        }
        
    }

    if(list_has_items)
    {
      validation_summary.unshift("$('#validation_summary_list').empty();")
      validation_summary.push("$('#validation_summary').css('display','');")
    }
    else
    {
        validation_summary.unshift("$('#validation_summary_list').empty();")
        validation_summary.push("$('#validation_summary').css('display','none');")
    }

  eval(validation_summary.join(""));

}

function init_delete_dialog(p_index, callback) 
{
    const case_list = g_ui.case_view_list;
	const modal = build_delete_dialog(case_list[p_index], p_index);
	const box = $("#content");

	box.append(modal[0]);
	$(`#case_modal_${p_index}`).modal("show");
	$(`#case_modal_${p_index} .modal-footer .modal-cancel`).focus();
}

function build_delete_dialog(p_values, p_index) 
{
    const modal_ui        = [];
    const hostState       = p_values.value.host_state;
    const jurisdictionID  = p_values.value.jurisdiction_id;
    const lastName        = p_values.value.last_name;
    const firstName       = p_values.value.first_name;
    const lastUpdatedBy   = p_values.value.last_updated_by;

    const dateLastUpdated = new Date(p_values.value.date_last_updated);
    const mm              = (dateLastUpdated.getMonth() + 1).toString().length === 1 ? `0${dateLastUpdated.getMonth() + 1}` : dateLastUpdated.getMonth() + 1;
    const dd              = dateLastUpdated.getDate().toString().length === 1 ? `0${dateLastUpdated.getDate()}` : dateLastUpdated.getDate();
    const yyyy            = dateLastUpdated.getFullYear().toString().length === 1 ? `0${dateLastUpdated.getFullYear()}` : dateLastUpdated.getFullYear();
    const hhmmss          = get24HourFormat(dateLastUpdated.toLocaleTimeString());

    modal_ui.push(`
        <div  aria-modal="true" id="case_modal_${p_index}" class="modal modal-${p_index}" tabindex="-1" role="dialog" aria-labelledby="case_modal_label_${p_index}" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered" role="document">
                <div class="modal-content">
                    <div class="modal-header bg-primary">
                    <h5 id="case_modal_label_${p_index}" class="modal-title">Confirm Delete Case</h5>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                    </div>
                    <div class="modal-body row no-gutters">
                    <div class="modal-icons col" style="max-width: 40px;">
                        <span class="x40 fill-amber-p cdc-icon-alert_02" aria-hidden="true"></span>
                        <span class="spinner-container spinner-inline" style="display: none">
                        <span class="spinner-body text-primary">
                            <span class="spinner"></span>
                        </span>
                        </span>
                    </div>
                    <div class="modal-messages col pl-3">
                        <p>Are you sure you want to delete this case?</p>
                        <p>
                            <strong>
                                <span style="display: none;">${hostState ? `${hostState} ` : ''}${jurisdictionID ? `${jurisdictionID}:` : ''}</span>
                                ${lastName ? lastName : ''}${firstName ? `, ${firstName}` : ''}
                            </strong>
                        </p>
                        <p>
                            Last updated: 
                            ${lastUpdatedBy} 
                            ${`${mm}/${dd}/${yyyy} ${hhmmss}`}
                        </p>
                    </div>
                    </div>
                    <div class="modal-footer">
                    <button type="button" class="modal-cancel btn btn btn-outline-secondary flex-order-2 mr-0" data-dismiss="modal" onclick="dispose_all_modals()">Cancel</button>
                    <button type="button" class="modal-confirm btn btn-primary flex-order-1 ml-0 mr-2" onclick="update_delete_dialog(${p_index}, () => { delete_record(${p_index}) })">Delete</button>
                    <button type="button" class="modal-confirm btn btn-primary flex-order-1 ml-0 mr-2" style="display: none" onclick="dispose_all_modals()">OK</button>
                    </div>
                </div>
            </div>
        </div>
    `);

  return modal_ui;
}

function build_delete_grid_dialog(p_number, p_object_path, p_metadata_path, p_dictionary_path, p_index, p_metadata_prompt, p_data_length) {
    const modal_ui = [];

    modal_ui.push(`
        <div  aria-modal="true" id="case_modal_${p_index}" class="modal modal-${p_index}" tabindex="-1" role="dialog" aria-labelledby="case_modal_label_${p_index}" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered" role="document">
                <div class="modal-content">
                    <div class="modal-header bg-primary">
                    <h5 id="case_modal_label_${p_index}" class="modal-title">Confirm Delete ${p_metadata_prompt} Item</h5>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                    </div>
                    <div class="modal-body row no-gutters">
                    <div class="modal-icons col" style="max-width: 40px;">
                        <span class="x40 fill-amber-p cdc-icon-alert_02" aria-hidden="true"></span>
                        <span class="spinner-container spinner-inline" style="display: none">
                        <span class="spinner-body text-primary">
                            <span class="spinner"></span>
                        </span>
                        </span>
                    </div>
                    <div class="modal-messages col pl-3">
                        <p>Are you sure you want to delete <strong>${p_metadata_prompt} item ${p_number} of ${p_data_length}</strong>?</p>
                    </div>
                    </div>
                    <div class="modal-footer">
                    <button type="button" class="modal-cancel btn btn btn-outline-secondary flex-order-2 mr-0" data-dismiss="modal" onclick="dispose_all_modals()">Cancel</button>
                    <button type="button" class="modal-confirm btn btn-primary flex-order-1 ml-0 mr-2" onclick="update_delete_dialog(${p_index}, () => { g_delete_grid_item_action('${p_object_path}', '${p_metadata_path}', '${p_dictionary_path}', ${p_index}) })">Delete</button>
                    <button type="button" class="modal-confirm btn btn-primary flex-order-1 ml-0 mr-2" style="display: none" onclick="dispose_all_modals()">OK</button>
                    </div>
                </div>
            </div>
        </div>
    `);

    return modal_ui;
}

function update_delete_dialog(p_index, callback) 
{
  const modal = $(`#case_modal_${p_index}`);
  const modal_msgs = modal.find('.modal-messages p');
  const modal_icons = modal.find('.modal-icons > span');
  const modal_btns = modal.find('.modal-footer button');

  $(modal_msgs[0]).text('Deleting...');
  $(modal_msgs[2]).hide();
  $(modal_icons[0]).hide();
  $(modal_icons[1]).show();
//   modal_msgs.first().text('Deleting...');
//   modal_msgs.last().hide();
//   modal_icons.first().hide();
//   modal_icons.last().show();

  setTimeout(() => {
    const date = new Date();
    const month = date.getMonth() + 1;
    const day = date.getDate();
    const year = date.getFullYear().toString().length === 1 ? '0' + date.getFullYear().toString() : date.getFullYear().toString();
    const hour = date.getHours().toString().length === 1 ? '0' + date.getHours().toString() : date.getHours().toString();
    const min = date.getMinutes().toString().length === 1 ? '0' + date.getMinutes().toString() : date.getMinutes().toString();
    const second = date.getSeconds().toString().length === 1 ? '0' + date.getSeconds().toString() : date.getSeconds().toString();
    const user_name = document.getElementById('user_logged_in').innerText;

    //callback to actually delete the record
    callback();

    modal_icons.parent().hide();
    modal_msgs.first().css({color: '#8f0000', fontWeight: 'bold'});
    $(modal_msgs[0]).text(`Deleted ${user_name && 'by ' + user_name} ${month}/${day}/${year} ${hour}:${min}:${second}`).show();
    $(modal_msgs[1]).find('span').show();
    $(modal_msgs[2]).hide();
    modal_btns.hide().last().show();

    // modal_icons.parent().hide();
    // modal_msgs.first().text(`Deleted ${user_name && 'by ' + user_name} ${month}/${day}/${year} ${hour}:${min}:${second}`);
    // modal_msgs.first().css({
    //   color: '#8f0000',
    //   fontWeight: 'bold',
    // });
    // modal_msgs.last().hide();
    // modal_btns.hide();
    // modal_btns.last().show();
  }, 500);
}

function init_multirecord_delete_dialog(p_object_path, p_metadata_path, p_index) {
    const oPath = p_object_path;
    const mPath = p_metadata_path;
    const index = p_index;
    const gData = g_data;
    const modal = build_multirecord_delete_dialog(oPath, mPath, index, gData);
    const box = $('#content');

    box.append(modal[0]);
    $(`#record_modal_${index}`).modal("show");
	$(`#record_modal_${index} .modal-footer .modal-cancel`).focus();
}

function build_multirecord_delete_dialog(p_object_path, p_metadata_path, p_index, p_data) {
    const modal_ui        = [];
    const displayIndex    = parseInt(p_index) + 1;
    const lastUpdatedBy   = p_data.last_updated_by;
    
    const dateLastUpdated = new Date(p_data.date_last_updated);
    const mm              = (dateLastUpdated.getMonth() + 1).toString().length === 1 ? `0${dateLastUpdated.getMonth() + 1}` : dateLastUpdated.getMonth() + 1;
    const dd              = dateLastUpdated.getDate().toString().length === 1 ? `0${dateLastUpdated.getDate()}` : dateLastUpdated.getDate();
    const yyyy            = dateLastUpdated.getFullYear().toString().length === 1 ? `0${dateLastUpdated.getFullYear()}` : dateLastUpdated.getFullYear();
    const hhmmss          = get24HourFormat(dateLastUpdated.toLocaleTimeString());

    modal_ui.push(`
        <div id="record_modal_${p_index}" class="modal modal-${p_index}" tabindex="-1" role="dialog" aria-labelledby="record_modal_label_${p_index}" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered" role="document">
                <div class="modal-content">
                    <div class="modal-header bg-primary">
                        <h5 id="record_modal_label_${p_index}" class="modal-title">Confirm Delete Record</h5>
                        <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                    </div>
                    <div class="modal-body row no-gutters">
                        <div class="modal-icons col" style="max-width: 40px">
                            <span class="x40 fill-amber-p cdc-icon-alert_02" aria-hidden="true"></span>
                            <span class="spinner-container spinner-inline" style="display: none">
                                <span class="spinner-body text-primary">
                                <span class="spinner"></span>
                                </span>
                            </span>
                        </div>
                        <div class="modal-messages col pl-3">
                            <p>Are you sure you want to delete this record?</p>
                            <p style="font-size: 18px"><strong>Record ${displayIndex}</strong></p>
                            <p>
                                Last updated: 
                                ${lastUpdatedBy} 
                                ${`${mm}/${dd}/${yyyy} ${hhmmss}`}
                            </p>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="modal-cancel btn btn btn-outline-secondary flex-order-2 mr-0" data-dismiss="modal" onclick="dispose_all_modals()">Cancel</button>
                        <button type="button" class="modal-confirm btn btn-primary flex-order-1 ml-0 mr-2" onclick="update_multirecord_delete_dialog(${p_index}, () => { g_delete_record_item('${p_object_path}', '${p_metadata_path}', '${p_index}') })">Delete</button>
                        <button type="button" class="modal-confirm btn btn-primary flex-order-1 ml-0 mr-2" style="display: none" onclick="dispose_all_modals()">OK</button>
                    </div>
                </div>
            </div>
        </div>`
    );

    return modal_ui;
}

function update_multirecord_delete_dialog(p_index, callback) 
{
    const modal = $(`#record_modal_${p_index}`);
    const modal_msgs = modal.find('.modal-messages p');
    const modal_icons = modal.find('.modal-icons > span');
    const modal_btns = modal.find('.modal-footer button');

    modal_msgs.first().text('Deleting...');
    modal_msgs.last().hide();
    modal_icons.first().hide();
    modal_icons.last().show();

    setTimeout(() => {
        const date = new Date();
        const month = date.getMonth() + 1;
        const day = date.getDate();
        const year = date.getFullYear().toString().length === 1 ? '0' + date.getFullYear().toString() : date.getFullYear().toString();
        const hour = date.getHours().toString().length === 1 ? '0' + date.getHours().toString() : date.getHours().toString();
        const min = date.getMinutes().toString().length === 1 ? '0' + date.getMinutes().toString() : date.getMinutes().toString();
        const second = date.getSeconds().toString().length === 1 ? '0' + date.getSeconds().toString() : date.getSeconds().toString();
        const user_name = document.getElementById('user_logged_in').innerText;

        //callback to actually delete the record
        callback();

        modal_icons.parent().hide();
        modal_msgs.first().text(`Deleted ${user_name && 'by ' + user_name} ${month}/${day}/${year} ${hour}:${min}:${second}`);
        modal_msgs.first().css({
            color: '#8f0000',
            fontWeight: 'bold',
        });
        modal_msgs.last().hide();
        modal_btns.hide();
        modal_btns.last().show();
    }, 500);
}

function dispose_all_modals() 
{
  $('.modal').modal('hide');
  $('.modal').remove();
  $('.modal-backdrop').remove();
}

function delete_record(p_index) 
{
    var data = g_ui.case_view_list[p_index];

    g_selected_delete_index = null;

    $.ajax({
      url:
        location.protocol +
        '//' +
        location.host +
        '/api/case?case_id=' +
        data.id,
    }).done(function (case_response) 
    {
      delete_case(case_response._id, case_response._rev);
    });

}

var save_interval_id = null;

function enable_print_button(event) 
{
  const { value } = event.target;
  //targeting next sibling buttons
  const printButton = event.target.nextSibling; 
  printButton.disabled = !value; // if there is a value it will be enabled.
  const pdfViewButton = printButton.nextSibling;
  pdfViewButton.disabled = !value;
  const pdfSaveButton = pdfViewButton.nextSibling;
  pdfSaveButton.disabled = !value;
}


let unique_tab_name = '';
function pdf_case_onclick(event, type_output) 
{
	//console.log('type_output: ', type_output);
  const btn = event.target;
 
	const dropdown = ( type_output == 'view' )
		? btn.previousSibling.previousSibling
		: btn.previousSibling.previousSibling.previousSibling;

  // get value of selected option
  let section_name = dropdown.value;

  unique_tab_name = '_pdf_tab_' + Math.random().toString(36).substring(2, 9);

  if (section_name) 
  {
    if (section_name == 'core-summary') 
    {

        window.setTimeout(function()
        {
            openTab('./pdf-version', unique_tab_name, section_name, type_output);
        }, 1000);	
    } 
    else 
    {
        // data-record of selected option
        const selectedOption = dropdown.options[dropdown.options.selectedIndex];
        const record_number = selectedOption.dataset.record;
				

        if(section_name == "all_hidden")
        {
            section_name = 'all';

            window.setTimeout(function()
            {
                openTab('./pdf-version',  unique_tab_name, section_name, type_output, record_number, true);
            }, 1000);	
        }
        else
        {
            window.setTimeout(function()
            {
                openTab('./pdf-version',  unique_tab_name, section_name, type_output, record_number);
            }, 1000);	
        }
      
    }
  }

}

function print_case_onclick(event) 
{
	const btn = event.target;
	const dropdown = btn.previousSibling;
	// get value of selected option
	let section_name = dropdown.value;
	unique_tab_name = '_print_tab_' + Math.random().toString(36).substring(2, 9);
  
	if (section_name) 
	{
	  if (section_name == 'core-summary') 
	  {
  
		  window.setTimeout(function()
		  {
			  openTab('./core-elements', unique_tab_name, 'all', 'print');
		  }, 1000);	
  
		
	  } 
	  else 
	  {
		// data-record of selected option
		const selectedOption = dropdown.options[dropdown.options.selectedIndex];
		const record_number = selectedOption.dataset.record;
  
        if(section_name == "all_hidden")
        {
            section_name = 'all';

            window.setTimeout(function()
            {
                openTab('./print-version', unique_tab_name, section_name, 'print', record_number, true);
            }, 1000);	
        }
        else
        {
  
            window.setTimeout(function()
            {
                openTab('./print-version', unique_tab_name, section_name, 'print', record_number);
            }, 1000);	
        }
		
	  }
	}
  
}

function openTab(pageRoute, tabName, p_section, p_type_output, p_number, p_show_hidden) 
{

    function clone(obj) 
    {
        if (null == obj || "object" != typeof obj) return obj;
        let copy = obj.constructor();
        for (var attr in obj) 
        {
            if (obj.hasOwnProperty(attr)) copy[attr] = obj[attr];
        }
        return copy;
    }


	// console.log('in openTab');
	// console.log('pageRoute: ', pageRoute);
	// console.log('tabName case: ', tabName);
	//console.log('g_metadata: ', g_metadata);
	//console.log('g_data: ', g_data);
	// console.log('p_section: ', p_section);
	// console.log('p_number: ', p_number);
	// console.log('p_type_output: ', p_type_output);


   // g_data.case_narrative.case_opening_overview = textarea_control_strip_html_attributes(g_data.case_narrative.case_opening_overview);


   let sorted_data = clone(g_data);

   g_apply_sort(g_metadata, sorted_data, "","", "");


  if (!window[tabName] || window[tabName].closed) 
  {
    window[tabName] = window.open(pageRoute, tabName, null, false);
    window[tabName].addEventListener('load', () => {
      window[tabName].create_print_version(
        g_metadata,
        sorted_data,
        p_section,
		p_type_output,
        p_number,
        g_metadata_summary,
        p_show_hidden
      );
    });
  } 
  else 
  {
    // if the WindowProxy Object already exists then just call the function on it
    window[tabName].create_print_version(
      g_metadata,
      sorted_data,
      p_section,
	p_type_output,
      p_number,
      g_metadata_summary,
      p_show_hidden
    );
  }
}

async function add_new_form_click(p_metadata_path, p_object_path, p_dictionary_path) 
{
  //console.log('add_new_form_click: ' + p_metadata_path + ' , ' + p_object_path);

  const spinner = $(event.target).siblings('.spinner-inline');
  spinner.addClass('spinner-active');

  var metadata = eval(p_metadata_path);
  var form_array = eval(p_object_path);
  var new_form = create_default_object(metadata, {}, true);
  var item = new_form[metadata.name][0];

  form_array.push(item);

  g_apply_sort(metadata, form_array, p_metadata_path, p_object_path, p_dictionary_path);

    await save_case
    (
        g_data
    );

    var post_html_call_back = [];
    document.getElementById(metadata.name + '_id').innerHTML = page_render
    (
        metadata,
        form_array,
        g_ui,
        p_metadata_path,
        p_object_path,
        p_dictionary_path,
        false,
        post_html_call_back
    ).join('');

    if (post_html_call_back.length > 0) 
    {
        eval(post_html_call_back.join(''));
    }

    spinner.removeClass('spinner-active');
}

async function enable_edit_click() 
{
  if (g_data) 
  {
    let new_date = new Date();

    g_change_stack.push({
        _id: g_data._id,
        _rev: g_data._rev,
      object_path: 'g_data.date_last_checked_out',
      metadata_path: '/date_last_checked_out',
      old_value: g_data.date_last_checked_out,
      new_value: new_date.toISOString(),
      dictionary_path: '/date_last_checked_out',
      metadata_type: 'datetime',
      prompt: 'date_last_checked_out',
      date_created: new_date.toISOString(),
      user_name: g_user_name
    });

    g_data.date_last_updated = new_date;
    g_data.date_last_checked_out = new_date;
    g_data.last_checked_out_by = g_user_name;
    g_data_is_checked_out = true;
    const current_data = g_data;
    window.setTimeout(async ()=>await save_case(current_data, create_save_message, "enable_edit"), 0);
    
    g_autosave_interval = window.setInterval(autosave, 10000);


    g_render();

    if ($global.case_document_begin_edit != null) 
    {
        $global.case_document_begin_edit();
    }
  }
}

async function save_form_click() 
{
    
    await save_case(g_data, create_save_message, 'save_form_click');
}

async function save_and_finish_click() 
{
  g_data.date_last_updated = new Date();
  g_data.date_last_checked_out = null;
  g_data.last_checked_out_by = null;
  g_data_is_checked_out = false;
  g_apply_sort(g_metadata, g_data, "","", "");

  const current_data = g_data;
  window.setTimeout(async ()=>await save_case(current_data, create_save_message, 'save_and_finish_click'), 0);
  g_render();
  window.clearInterval(g_autosave_interval);
  g_autosave_interval = null;
}

function create_save_message() 
{
  var result = [];

  result.push(`
    <div class="alert alert-success alert-dismissible">
      <button class="close" data-dismiss="alert" aria-label="close">&times;</button>
      <p>Case information has been saved</p>
    </div>
  `);

  document.getElementById('nav_status_area').innerHTML = result.join('');

  window.setTimeout(clear_nav_status_area, 5000);
}

function clear_nav_status_area() 
{
  document.getElementById('nav_status_area').innerHTML = '<div>&nbsp;</div>';
}

function set_local_case(p_data, p_call_back) 
{
  let local_storage_index = get_local_storage_index();

  if (local_storage_index == null) 
  {
    local_storage_index = create_local_storage_index();
  }

  local_storage_index[p_data._id] = create_local_storage_index_item(p_data);
  window.localStorage.setItem
  (
    'case_index',
    JSON.stringify(local_storage_index)
  );

  window.localStorage.setItem('case_' + p_data._id, JSON.stringify(p_data));

  if (p_call_back) 
  {
    p_call_back();
  }
}

function create_local_storage_index() 
{
  let result = {};

  for (let key in window.localStorage) 
  {
    if (key == 'case_index' || key.indexOf("blazor-resource") == 0) 
    {
      continue;
    }

    if (window.localStorage.hasOwnProperty(key)) 
    {
      let item_string = window.localStorage[key];
      let item_object = JSON.parse(item_string);

      result[item_object._id] = create_local_storage_index_item(item_object);
    }
  }

  window.localStorage.setItem('case_index', JSON.stringify(result));

  return result;
}

function get_local_storage_space_usage_in_kilobytes() 
{
  let allStrings = '';

  for (let key in window.localStorage)
   {
    if (window.localStorage.hasOwnProperty(key)) 
    {
      allStrings += window.localStorage[key];
    }
  }
  return allStrings ? 3 + (allStrings.length * 16) / (8 * 1024) : 0;
}

function calc_local_storage_space_usage_in_kilobytes(p_string) 
{
  return p_string ? 3 + (p_string.length * 16) / (8 * 1024) : 0;
}

function convert_local_storage_index_to_array(p_case_index) 
{
  let result = [];

  for (let key in p_case_index) 
  {
    if (p_case_index.hasOwnProperty(key)) 
    {
      let item = p_case_index[key];
      let item_object = JSON.parse(window.localStorage['case_' + key]);

      if (!(item.date_last_updated instanceof Date)) 
      {
        item.date_last_updated = new Date(item.date_last_updated);
      }

      item.size_in_kilobytes = calc_local_storage_space_usage_in_kilobytes
      (
        JSON.stringify(item_object)
      );

      result.push(item);
    }
  }

  //result.sort(function(p1, p2){return p1.size_in_kilobytes-p2.size_in_kilobytes});
  result.sort(function (p1, p2) 
  {
    return p1.date_last_updated - p2.date_last_updated;
  });

  return result;
}

function get_local_storage_index() 
{
  let result = JSON.parse(window.localStorage.getItem('case_index'));

  if (result == null) 
  {
    result = create_local_storage_index();
  } 
  else if 
  (
    Object.keys(result).length === 0 &&
    result.constructor === Object &&
    window.localStorage.length > 0
  ) 
  {
    result = create_local_storage_index();
  }

  return result;
}

function create_local_storage_index_item(p_data) 
{
  return {
    _id: p_data._id,
    _rev: p_data._rev,
    date_created: p_data.date_created,
    created_by: p_data.created_by,
    date_last_updated: p_data.date_last_updated,
    last_updated_by: p_data.last_updated_by,
  };
}

function get_local_case(p_id) 
{
  var result = null;

  result = JSON.parse(window.localStorage.getItem('case_' + p_id));

  return result;
}

function undo_click() 
{
  var current_change = g_change_stack.pop();

  if (current_change) 
  {
    var metadata = eval(current_change.metadata_path);

    if 
    (
      metadata.type.toLowerCase() == 'list' &&
      metadata['is_multiselect'] &&
      metadata.is_multiselect == true
    ) 
    {
      var item = eval(current_change.object_path);

      if (item.indexOf(current_change.old_value) > -1) 
      {
        item.splice(item.indexOf(current_change.old_value), 1);
      } 
      else 
      {
        item.push(current_change.old_value);
      }
    } 
    else if (metadata.type.toLowerCase() == 'boolean') 
    {
      eval(current_change.object_path + ' = ' + current_change.old_value);
    } 
    else 
    {
      eval
      (
        current_change.object_path +
          ' = "' +
          current_change.old_value.replace(/"/g, '\\"').replace(/\n/g, '\\n') +
          '"'
      );
    }
  }

  g_render();
}

async function autosave() 
{
    const split_one = window.location.href.split('#');

    if (split_one.length <= 1) return;

    const split_two = split_one[0].split('/');

    if (split_two.length <= 3) return;
    
    if
    (
        !(
            split_two[3].toLocaleLowerCase() == 'case' ||
            split_two[3].toLocaleLowerCase() == 'abstractordeidentifiedcase' 
        )
    )
    {
        return;
    }

    const split_three = split_one[1].split('/');

    if
    (
        split_three.length <= 1 ||
        split_three[1].toLocaleLowerCase() == 'summary'
    ) 
    {
        return;
    }

    if (g_data == null  || g_data == undefined) return;

    
    const dt1 = new Date(g_data.date_last_updated);
    const dt2 = new Date();
    const number_of_minutes = diff_minutes(dt1, dt2);

    if (number_of_minutes < 3) return; 
    

    g_data.date_last_updated = new Date();
    await save_case(g_data, null, 'autosave');
  
}

function is_case_view_locked(p_case)
{
    let result = false;

    let selected_value = 9999;
    
    if
    (
        p_case.case_status &&
        p_case.case_status != ""
    )
    {
        selected_value = new Number(p_case.case_status);
    }
    
    if
    (
        p_case.case_status &&
        p_case.case_locked_date != "" &&
        (
            selected_value == 4 ||
            selected_value == 5 ||
            selected_value == 6
        )
    )
    {
        if (! g_is_confirm_for_case_lock)
        {
            result = true;
        }
    }

    return result;
}



function is_case_locked(p_case)
{
    let result = false;

    let selected_value = 9999;
    
    if
    (
        p_case.home_record &&
        p_case.home_record.case_status &&
        p_case.home_record.case_status.overall_case_status &&
        p_case.home_record.case_status.overall_case_status != ""
    )
    {
        selected_value = new Number(p_case.home_record.case_status.overall_case_status);
    }
    
    if
    (
        p_case.home_record != null &&
        p_case.home_record.case_status != null  &&
        p_case.home_record.case_status.overall_case_status != null &&
        //p_case.home_record.case_status.case_locked_date != "" &&
        (
            selected_value == 4 ||
            selected_value == 5 ||
            selected_value == 6
        )
    )
    {
        if (! g_is_confirm_for_case_lock)
        {
            result = true;
        }
    }
    
    return result;
}



function is_case_checked_out(p_case) 
{
  let is_checked_out = false;

  let current_date = new Date();

  if 
  (
    p_case.date_last_checked_out != null &&
    p_case.date_last_checked_out != ''
  ) 
  {
    let try_date = null;
    let is_date = false;
    if (!(p_case.date_last_checked_out instanceof Date)) 
    {
      try_date = new Date(p_case.date_last_checked_out);
    } 
    else 
    {
      try_date = p_case.date_last_checked_out;
    }

    if 
    (
      diff_minutes(try_date, current_date) <= 120 &&
      p_case.last_checked_out_by.toLowerCase() == g_user_name.toLowerCase()
    ) 
    {
      is_checked_out = true;
    }
  }

  return is_checked_out;
}

function is_checked_out_expired(p_case) 
{
  let is_expired = true;

  let current_date = new Date();

  if 
  (
    p_case.date_last_checked_out != null &&
    p_case.date_last_checked_out != ''
  ) 
  {
    let try_date = null;
    if (!(p_case.date_last_checked_out instanceof Date)) 
    {
      try_date = new Date(p_case.date_last_checked_out);
    } 
    else 
    {
      try_date = p_case.date_last_checked_out;
    }

    if (diff_minutes(try_date, current_date) < 120) 
    {
      is_expired = false;
    }
  }

  return is_expired;
}

function diff_minutes(dt1, dt2) 
{
  let diff = (dt2.getTime() - dt1.getTime()) / 1000;

  diff /= 60;
  return Math.abs(Math.round(diff));
}

function g_textarea_oninput
(
    p_object_path,
    p_metadata_path,
    p_dictionary_path,
    value
) 
{
    var metadata = eval(p_metadata_path);

    g_case_narrative_is_updated = true;
    g_case_narrative_is_updated_date = new Date()

    try
    {
        

        eval
        (
            `${p_object_path}="${value.replace(/"/g, '\\"').replace(/\n/g, '\\n')}"`
        );
  

        set_local_case(g_data, null);
    }
    catch(e)
    {
        const err = {
            status: 500,
            responseText : `unable to save field: ${p_dictionary_path}\n${e}`
        };
        $mmria.field_save_error_dialog_show(err, `unable to save field: ${p_dictionary_path} `);
    }

}

function navigation_away(e) 
{

  if (g_data_is_checked_out && g_data) 
  {
    g_data.date_last_updated = new Date();
    g_data.date_last_checked_out = null;
    g_data.last_checked_out_by = null;
    g_data_is_checked_out = false;

    for (let i = 0; i < g_ui.case_view_list.length; i++) 
    {
      let item = g_ui.case_view_list[i];
      if (item.id == g_data._id) 
      {
        item.date_last_checked_out = null;
        item.last_checked_out_by = null;
        break;
      }
    }

    const current_data = g_data;
    save_case(current_data, null, 'navigation_away').then
    (
        ()=>{
        window.clearInterval(g_autosave_interval);
        g_autosave_interval = null;
        }
    );
  }


}


function render_summary_validation
(
    p_metadata, 
    p_data, 
    p_path,  
    p_object_path,
    p_result, 
    p_form_index, 
    p_grid_index
)
{
    switch(p_metadata.type.toLocaleLowerCase())
    {
        case "app":
            
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                if
                (
                    p_data && 
                    p_data[child.name] &&
                    child.type.toLocaleLowerCase() == "form" &&
                    g_ui.url_state &&
                    g_ui.url_state.selected_id &&
                    g_ui.url_state.selected_id == child.name
                )
                {
                    render_summary_validation(child, p_data[child.name], p_path + "/" + child.name, p_object_path + "." + child.name, p_result, p_form_index, p_grid_index);
                }
            }
            break;
        case "form":
            if(p_metadata.cardinality == "1" || p_metadata.cardinality == "?")
            {
                for(let i = 0; i < p_metadata.children.length; i++)
                {
                    let child = p_metadata.children[i];

                    if(p_data && p_data[child.name])
                    {
                        render_summary_validation(child, p_data[child.name], p_path + "/" + child.name, p_object_path + "." + child.name, p_result, p_form_index, p_grid_index);
                    }
                    
                }
            }
            else // multiform
            {

                for(let form_index = 0; form_index < p_data.length; form_index++)
                {
                    let row_data = p_data[form_index]
                    for(let i = 0; i < p_metadata.children.length; i++)
                    {
                        let child = p_metadata.children[i];
    
                        if(row_data)
                        {
                            render_summary_validation(child, row_data, p_path + "/" + child.name, p_object_path + "[" + form_index + "]." + child.name, p_result, form_index, p_grid_index);
                        }
                        
                    }
                }
            }
            break;
        case "group":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];
                if(p_data)
                {
                    render_summary_validation(child, p_data[child.name], p_path + "/" + child.name, p_object_path + "." + child.name, p_result, p_form_index, p_grid_index);
                }
            }
            break;
        case "grid":
            for(let i = 0; i < p_data.length; i++)
            {
                let row_item = p_data[i];
                for(let j = 0; j <p_metadata.children.length; j++)
                {
                    let child = p_metadata.children[j];
                    render_summary_validation(child, row_item[child.name], p_path + "/" + child.name, p_object_path + "[" + i + "]." + child.name, p_result, p_form_index, i);
                }
            
            }
            break;
        case "string":
        case "number":
        case "time":
                // do nothing for now
            break;
        case "date":
            if (!is_valid_date(p_data)) 
            {
                //p_result.push('<li data-path="${p_dictionary_path.substring(1, p_dictionary_path.length)}" data-grid="'+ p_grid_index +'"><strong>'+legend_label+': ${p_metadata.prompt}, item '+(parseInt(p_grid_index)+1)+':</strong> Date must be a valid calendar date between 1900-2100</li>');
                p_result.push(`$('#validation_summary_list').append('<li><strong>${p_metadata.prompt} ${p_data}:</strong> Date must be a valid calendar date between 1900-2100 <button class="btn anti-btn ml-1"><span class="sr-only">Remove Item</span><span class="x20 cdc-icon-times-solid"></span></button></li>');`);
                
            }
            break;
        case "datetime":
            if (!is_valid_datetime(p_data)) 
            {
                //p_result.push('<li data-path="${p_dictionary_path.substring(1, p_dictionary_path.length)}" data-grid="'+p_grid_index+'"><strong>'+legend_label+': ${p_metadata.prompt}, item '+(parseInt(p_grid_index)+1)+':</strong> Date must be a valid calendar date between 1900-2100</li>');
                p_result.push(`$('.construct__header-alert ul').append('<li><strong>${p_metadata.prompt} ${p_data}:</strong> Date must be a valid calendar date between 1900-2100 <button class="btn anti-btn ml-1"><span class="sr-only">Remove Item</span><span class="x20 cdc-icon-times-solid"></span></button></li>');`);
                
            }
            break;
        case "list":
            // do nothing for now
            break;
        case "textarea":
            // do nothing for now
            break;
    }
}

function gui_remove_broken_rule_click(p_object_id)
{
    gui_remove_broken_rule(p_object_id);
    apply_validation();

}

function gui_remove_broken_rule(p_object_id)
{
    if(g_ui.broken_rules.hasOwnProperty(p_object_id))
    {
        g_ui.broken_rules[p_object_id] = null;
        delete g_ui.broken_rules[p_object_id];
    }

    let item = document.getElementById(`${p_object_id}-inline-validation-message`);
    if(item != null)
    {
        item.style.display = 'none';
    }

    //remove validation error from date control
    $(`#${p_object_id} .date-control`).removeClass('is-invalid');
    //remove validation error from datetime control
    $(`#${p_object_id}-innerdiv`).removeClass('is-invalid');
}




function build_other_specify_lookup(p_result, p_metadata, p_path = "")
{
    switch(p_metadata.type.toLocaleLowerCase())
    {
        case "app":
            for(let i = 0; i < p_metadata.children.length; i++)
            {
                let child = p_metadata.children[i];

                build_other_specify_lookup(p_result, child, `${child.name}`);
            }
            break;
        case "form":
        case "group":
        case "grid":

                for(let i = 0; i < p_metadata.children.length; i++)
                {
                    let child = p_metadata.children[i];

                    build_other_specify_lookup(p_result, child, `${p_path}/${child.name}`);
                }
            break;
        case "list":
            let other_specify_list_key = [];
            let other_specify_list_path = [];

            if
            (
                p_metadata.other_specify_list != null && 
                p_metadata.other_specify_list.trim().length > 0
            )
            {
                let item_list = p_metadata.other_specify_list.split(',');
                for(let i = 0; i < item_list.length; i++)
                {
                    let kvp = item_list[i].split(' ');
                    if
                    (
                        kvp.length > 1 &&
                        kvp[0] != null &&
                        kvp[0].trim().length > 0 &&
                        kvp[1] != null &&
                        kvp[1].trim().length > 0
                    )
                    {
                        let key = kvp[0].trim();
                        let path = kvp[1].trim();

                        p_result[path] = { list: `${p_path}`, value: key }
                        
                    }
                }
            }


            for(let i = 0; i < other_specify_list_key.length; i++)
            {
                let item = other_specify_list_key[i];
                let object_path = `g_data.${other_specify_list_path[i].replace(/\//g,".")}`;
                
            }
        break;
        case "string":
        case "number":
        case "time":
        case "date":
        case "datetime":
        case "textarea":
        default:
            break;
    }
}





async function get_form_access_list()
{
	var metadata_url = location.protocol + '//' + location.host + '/_users/GetFormAccess';

	const response = await $.ajax
	({
			url: metadata_url
	});

	return response;
}