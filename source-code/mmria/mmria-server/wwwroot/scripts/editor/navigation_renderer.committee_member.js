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
        result.push('<li class="list-group-item"><a href="#/summary">Summary</a></li>');

            
        if(parseInt(p_ui.url_state.path_array[0]) >= 0)
        {
          result.push('<li class="list-group-item">');
            result.push('<div class="form-group fake-list-group-anchor">');
              result.push('<label for="search_case_fields">Search for fields</label>');
              result.push('<div class="form-control-wrap">');
                if(p_ui.url_state.selected_id == "field_search")
                {
                  result.push('<input id="search_case_fields" class="form-control" type="text" onchange="search_text_change(this);" value="' + p_ui.url_state.path_array[2].replace(/%20/g, " ") + '" />');
                }
                else
                {
                  result.push('<input id="search_case_fields" class="form-control" type="text" onchange="search_text_change(this);"/>');
                }

                result.push('<span class="fancy-form-icon 24 fill-p cdc-icon-search-solid" aria-hidden="true"></span>');
              result.push('</div>');
            result.push('</div>');
          result.push('</li>');


          result.push('<li class="list-group-item">');
            result.push('<div class="form-group fake-list-group-anchor">');
              result.push('<label for="selected_form">Select case form</label>');
              result.push('<div class="form-control-wrap">');
                result.push('<select id="selected_form" class="form-control" onChange="updateUrlFromSelectValue(event,this.value);">');
                  result.push('<option value=""> </option>');
                  for(var i = 0; i < p_metadata.children.length; i++)
                  {
                    var child = p_metadata.children[i];
                    var url = p_ui.url_state.path_array[0] + "/" + child.name;
                    
                    if (child.type === 'form') {

                      if(p_ui.url_state.selected_id.toLowerCase() == child.name.toLowerCase())
                      {
                        result.push('<option value="' + url + '" selected>');
                      }
                      else
                      {
                        result.push('<option value="' + url + '">');
                      }
                      
                        result.push(child.prompt);
                      result.push('</option>');
                    }
                  }
                result.push("</ul>");
                result.push('</select>');
              result.push('</div>');
            result.push('</div>');
          result.push('</li>');

        }

        if(parseInt(p_ui.url_state.path_array[0]) >= 0)
        {        
        // print version start
        result.push('<li class="list-group-item">');
          result.push('<div class="form-group fake-list-group-anchor">');
            result.push('<label for="print_case">Print case form</label>');
            result.push('<div class="form-control-wrap">');
              result.push('<select id="print_case_id" class="form-control" onChange="print_case_onchange()">');
                result.push('<option value="" selected>Select one</option>');  
                result.push('<option value="core-summary">Core Elements Only</option>');
                result.push('<option value="all">All</option>');  
                for(var i = 0; i < p_metadata.children.length; i++)
                {
                  var child = p_metadata.children[i];
                  if(child.type.toLowerCase() == 'form')
                  {
                    result.push('<option value="' + child.name + '">');
                      result.push(child.prompt)
                    result.push('</option>');
                  }
                }
              result.push('</select>');
            result.push('</div>');
          result.push('</div>');
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
  
  currLocation.hash = "/" + val;
}
