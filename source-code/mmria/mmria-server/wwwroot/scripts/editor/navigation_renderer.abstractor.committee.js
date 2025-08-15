function navigation_render(p_metadata, p_level, p_ui)
{
   var result = [];
   if(p_level > 1) return result;

   switch(p_metadata.type.toLowerCase())
   {
      case 'group':
       result.push('<li><div>');
       result.push('<a href="#/');
       result.push(p_metadata.name);
       result.push('">');
       result.push(p_metadata.prompt);
       result.push('</a><ul>');
       for(var i = 0; i < p_metadata.children.length; i++)
       {
         var child = p_metadata.children[i];
         if(child.type == "group")
         {
           result.push('<li>');
           Array.prototype.push.apply(result,navigation_render(child, p_level + 1, p_ui));
           result.push('</li>');
        }
       }
       result.push('</ul>');
       result.push('</div></li>');
       break;

      case 'form':
        if(parseInt(p_ui.url_state.path_array[0]) >= 0) 
        {
          result.push('<li>');
          result.push('<a href="#/');
          if(parseInt(p_ui.url_state.path_array[0]) >= 0)
          {
            result.push(p_ui.url_state.path_array[0]);
          }
          else
          {
            result.push(p_ui.data_list[p_ui.selected_record_index]._id);
          }
          result.push("/");
          result.push(p_metadata.name);
          result.push('">');
          result.push(p_metadata.prompt);
          result.push('</a><ul>');
          for(var i = 0; i < p_metadata.children.length; i++)
          {
            var child = p_metadata.children[i];
            if(child.type == "group")
            {
              Array.prototype.push.apply(result,navigation_render(child, p_level + 1, p_ui));
            }
          }
          result.push('</ul></li>');
        }
        break;

      case 'app':
        // result.push('<li class="list-group-item"><a href="#/summary">Summary</a></li>');

        const breadcrumb_list = $('#breadcrumb_list');
        const bread_crumb_summary_link = `<li class="breadcrumb-item"><a href="#/summary">Summary</a></li>`;
        let currHash = window.location.hash;

        if (currHash === '' || currHash === '#/summary')
        {
          result.push('<li id="summary-item" class="list-group-item nav-lvl1 ml-0 active selected"><a href="#/summary" onclick="g_render();">Summary</a></li>');

          if (breadcrumb_list.children().length >= 2)
          {
            breadcrumb_list.children().last().remove();
          }
        }
        else
        {
          result.push('<li id="summary-item" class="list-group-item nav-lvl1 ml-0 active"><a href="#/summary" onclick="g_render();">Summary</a></li>');
        }

        if(parseInt(p_ui.url_state.path_array[0]) >= 0)
        {
          // result.push('<li id="case_item" class="list-group-item nav-lvl2">');
          if (currHash !== '' && currHash !== '#/summary' && !currHash.includes('field_search'))
          {
            result.push('<li id="case_item" class="list-group-item nav-lvl2 active selected">');
            
            if (breadcrumb_list.children().length < 2)
            {
              breadcrumb_list.append(bread_crumb_summary_link);
            }
          }
          else
          {
            result.push('<li id="case_item" class="list-group-item nav-lvl2">');
          }
            result.push('<div class="form-group fake-list-group-anchor">');
              result.push('<label for="selected_form">Select case form</label>');
              result.push('<div class="form-control-wrap">');
                result.push('<select id="selected_form" class="form-control" onChange="updateUrlFromSelectValue(event,this.value);" disabled aria-disabled="true">');
                  result.push('<option value=""></option>');
                  for(var i = 0; i < p_metadata.children.length; i++)
                  {
                    var child = p_metadata.children[i];
                    var url = p_ui.url_state.path_array[0] + "/" + child.name;
                    
                    if (child.type == 'form')
                    {
                      if(p_ui.url_state.selected_id.toLowerCase() == child.name.toLowerCase())
                      {
                        result.push('<option value="' + url + '" selected>');
                        result.push(child.prompt);
                        result.push('</option>');
                      }
                    }
                    
                  }
                result.push('</select>');
              result.push('</div>');
            result.push('</div>');
          result.push('</li>');

          // result.push('<li id="quickedit_item" class="list-group-item">');
          if (currHash !== '' && currHash !== '#/summary' && currHash.includes('field_search'))
          {
            result.push('<li id="quickedit_item" class="list-group-item active selected">');

            if (currHash !== '' && currHash !== '#/summary' && !currHash.includes('field_search'))
            {
              result.push('<li id="case_item" class="list-group-item nav-lvl2 active selected">');
            }
          }
          else
          {
            result.push('<li id="quickedit_item" class="list-group-item">');
          }
            result.push('<div class="form-group fake-list-group-anchor">');
              result.push('<label for="search_case_fields" class="row no-gutters align-items-center"><span>Quick Edit</span> <button class="info-icon anti-btn x20 fill-p cdc-icon-info-circle-solid ml-1" data-toggle="tooltip" data-placement="bottom" title="Use Quick Edit to locate and edit similar fields across all forms for this case. Type at least three characters to begin your search."></button></label>');
              result.push('<div class="form-control-wrap">');
                if(p_ui.url_state.selected_id == "field_search")
                {
                  result.push('<input id="search_case_fields" class="form-control" type="text" onchange="init_content_loader(function(){ search_text_change(event) })" value="' + p_ui.url_state.path_array[2].replace(/%20/g, " ") + '"  disabled aria-disabled="true"/>');
                }
                else
                {
                  result.push('<input id="search_case_fields" class="form-control" type="text" onchange="init_content_loader(function(){ search_text_change(event) })"  disabled aria-disabled="true"/>');
                }
                


                result.push('<button class="anti-btn fancy-form-icon 24 fill-p cdc-icon-search-solid" type="submit"><span class="sr-only">Click to search</span></button>');

              result.push('</div>');
            result.push('</div>');


            if
            (
                g_is_data_analyst_mode == null ||
                g_is_data_analyst_mode == false
            )
            {                
                result.push('<br/><br/>');
                result.push(render_conversion_calculator_button(result));
            }
          result.push('</li>');

        }
        result.push('<li id="nav_status_area">&nbsp;</li>');
        break;

      default:
        break;
   }

   return result;
}


function updateUrlFromSelectValue(event, val) 
{
  var currLocation = window.location;
  var options = event.target.options;
  var selected = options.selectedIndex;

  g_ui.broken_rules = {};
  
  currLocation.hash = "/" + val;

  // scroll to top of page regardless of form change
  document.body.scrollTop = document.documentElement.scrollTop = 0;
}


function render_conversion_calculator_button(p_result)
{


    p_result.push(
        `
        <input 
            type="button" 
            class="btn btn-primary ml-3" 
            value="Conversion Calculator" 
            onclick="$mmria.converter_calculater_dialog_show()"
        >

        `
    );
}