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
        if(
            (parseInt(p_ui.url_state.path_array[0]) >= 0) || 
            window.location.href.indexOf('preview.html') > 0
            )
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
        result.push('<ul  class="nav navbar-nav">');
        result.push('<li><a href="#/summary">Summary</a></li>');
        if(parseInt(p_ui.url_state.path_array[0]) >= 0)
        {  
          // forms start
          result.push('<li class="dropdown">');
          result.push('<a class="dropdown-toggle" data-toggle="dropdown" href="#" id="themes">Case Forms<span class="caret"></span></a>');
            result.push('<ul class="dropdown-menu" aria-labelledby="themes">');
            for(var i = 0; i < p_metadata.children.length; i++)
            {
              var child = p_metadata.children[i];
              Array.prototype.push.apply(result,navigation_render(child, p_level + 1, p_ui));
            }
            result.push("</ul>");
          result.push('</li>');
          // forms end
        }



        if(parseInt(p_ui.url_state.path_array[0]) >= 0)
        {        
        // print version start
        /*
        result.push('<li class="dropdown">');
          result.push('<a  class="dropdown-toggle" data-toggle="dropdown" id="print_blank">Print Version  <span class="caret"></span></a>');
          result.push('<ul class="dropdown-menu" role="menu" aria-labelledby="print_blank">');
          result.push('<li><a tabindex="-1" onclick="open_print_version(\'all\')">All</a></li>');
          for(var i = 0; i < p_metadata.children.length; i++)
          {
            var child = p_metadata.children[i];
            if(child.type.toLowerCase() == 'form')
            {
              result.push('<li><a tabindex="-1" onclick="open_print_version(\'');
              result.push(child.name)
              result.push('\')">');
              result.push(child.prompt)
              result.push('</a></li>');
            }
          }
          result.push('</ul>'); 
        result.push('</li>')
        */
        // print version end
        }


        // actions start

        result.push('<li class="dropdown">');
        result.push('<a class="dropdown-toggle" data-toggle="dropdown" href="#" id="actions">Actions <span class="caret"></span></a>');
          result.push('<ul class="dropdown-menu" role="menu" aria-labelledby="actions">');

            if(parseInt(p_ui.url_state.path_array[0]) >= 0)
            {
              //result.push('<li><a href="print-version" target="_print_version">Print Version</a></li>');
              result.push('<li><a onclick="open_core_summary(\'all\')">Populate Core Elements</a></li>');
            }

            result.push('<li><a href="data-dictionary" target="_data_dictionary">Show Data Dictionary</a></li>');
            result.push('<li><a tabindex="-1" onclick="open_aggregate_report_version()">View Aggregate Report</a></li>');



          result.push('</ul>');
        result.push('</li>');
        // Actions end

        // print blank start
        /*
        result.push('<li class="dropdown">');
          result.push('<a  class="dropdown-toggle" data-toggle="dropdown" id="print_blank">Print Blank  <span class="caret"></span></a>');
          result.push('<ul class="dropdown-menu" role="menu" aria-labelledby="print_blank">');
          result.push('<li><a tabindex="-1"  onclick="open_blank_version(\'all\')">All</a></li>');
          for(var i = 0; i < p_metadata.children.length; i++)
          {
            var child = p_metadata.children[i];
            if(child.type.toLowerCase() == 'form')
            {
              result.push('<li><a tabindex="-1" onclick="open_blank_version(\'');
              result.push(child.name)
              result.push('\')">');
              result.push(child.prompt)
              result.push('</a></li>');
            }
          }
          
          result.push('</ul>'); 
        result.push('</li>')
        */
        // print blank end
  



        result.push('</ul>');
        break;
      default:
        break;

   }


   //Array.prototype.push.apply(result,b)
   //result.push("<div><fieldset><legend>navigation:</legend><div>root<ul><li>item1<li><li>item2</li></ul> - :: - hello this is a test</fieldset></div>");
   return result;
}
