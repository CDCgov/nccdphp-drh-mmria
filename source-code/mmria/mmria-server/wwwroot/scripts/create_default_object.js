function create_default_object(p_metadata, p_parent, p_create_grid)
{

  switch(p_metadata.type.toLowerCase())
  {
    case 'grid':
      p_parent[p_metadata.name] = [];
      
      if(p_create_grid)
      {
        if(p_metadata.name != "recommendations_of_committee")
        {

          var sample_grid_item = {};
          for(var i = 0; i < p_metadata.children.length; i++)
          {
            var child = p_metadata.children[i];
            create_default_object(child, sample_grid_item);
          }
          p_parent[p_metadata.name].push(sample_grid_item);
        }
      }
      break;
    case 'form':
      var temp_object = {};
      for(var i = 0; i < p_metadata.children.length; i++)
      {
        var child = p_metadata.children[i];
        create_default_object(child, temp_object);
      }

      if(p_metadata.cardinality)
      {
        switch(p_metadata.cardinality)
        {
          case "+":
          case "*":
              if(p_create_grid)
              {
                p_parent[p_metadata.name] = [];
                p_parent[p_metadata.name].push(temp_object);
              }
              else
              {
                p_parent[p_metadata.name] = [];
              }
              break;
          case "?":
          case "1":
          default:
              p_parent[p_metadata.name] = temp_object;
            break;
        }
      }
      else
      {
        p_parent[p_metadata.name] = temp_object;
      }

      break;

    case 'group':
      p_parent[p_metadata.name] = {};
      for(var i = 0; i < p_metadata.children.length; i++)
      {
        var child = p_metadata.children[i];
        create_default_object(child, p_parent[p_metadata.name]);
      }
      break;
    case 'app':
        p_parent["_id"] = $mmria.get_new_guid();
       for(var i = 0; i < p_metadata.children.length; i++)
       {
         var child = p_metadata.children[i];
         create_default_object(child, p_parent);
       }
       p_parent["host_state"] = sanitize_encodeHTML(window.location.host.split("-")[0]);
       p_parent["addquarter"] = $mmria.get_year_and_quarter(new Date());
       break;
    case 'string':
    case 'textarea':
    case 'html_area':
    case 'address':
          if(p_metadata.default_value && p_metadata.default_value != "")
          {
            p_parent[p_metadata.name] = new String(p_metadata.default_value);
          }
          else if(p_metadata.pre_fill && p_metadata.pre_fill != "")
          {
            p_parent[p_metadata.name] = new String(p_metadata.pre_fill);
          }
          else
          {
            p_parent[p_metadata.name] = new String();
          }
           break;
     case 'number':
            if(p_metadata.default_value && p_metadata.default_value != "")
            {
              p_parent[p_metadata.name] = new Number(p_metadata.default_value);
            }
            else
            {
              p_parent[p_metadata.name] = "";
            }
           break;
     case 'boolean':
            if(p_metadata.default_value && p_metadata.default_value != "")
            {
              p_parent[p_metadata.name] = new Boolean(p_metadata.default_value);
            }
            else
            {
              p_parent[p_metadata.name] = new Boolean();
            }
            break;
    case 'list':
    case 'yes_no':
        if(p_metadata['is_multiselect'] && p_metadata.is_multiselect == true)
        {
          p_parent[p_metadata.name] = [];
        }
        else
        {
            if(p_metadata.default_value && p_metadata.default_value != "")
            {
              p_parent[p_metadata.name] = new String(p_metadata.default_value);
            }
            else
            {
              p_parent[p_metadata.name] = "9999";
            }
        }
            
        break;
     case 'date':
     case 'datetime':
            if(p_metadata.default_value && p_metadata.default_value != "")
            {
              p_parent[p_metadata.name] = new Date(p_metadata.default_value);
            }
            else
            {
              p_parent[p_metadata.name] = "";
            }
            break;
    case 'time':
           //p_parent[p_metadata.name] = new Date("2016-01-01T00:00:00.000Z");
          p_parent[p_metadata.name] = "";
          break;
    case 'hidden':
          p_parent[p_metadata.name] = "";
          break;
    case 'jurisdiction':
          p_parent[p_metadata.name] = "/";
          break;           
    case 'label':
    case 'button':
    case 'always_enabled_button':
    case 'chart':
            break;
     default:
          console.log("create_default_object not processed", p_metadata);
       break;
  }



  return p_parent;
}

function sanitize_encodeHTML(s) 
{
	let result = s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
    return result;
}


function clone_multiform_object
(
    p_metadata, 
    p_parent, 
    p_create_grid,
    p_from,
    p_path
)
{

  switch(p_metadata.type.toLowerCase())
  {
    case 'grid':
      p_parent[p_metadata.name] = [];
      
      if(p_create_grid)
      {
        if(p_metadata.name != "recommendations_of_committee")
        {

          var sample_grid_item = {};
          for(var i = 0; i < p_metadata.children.length; i++)
          {
            var child = p_metadata.children[i];
            clone_multiform_object
            (
                child, 
                sample_grid_item
            );
          }
          p_parent[p_metadata.name].push(sample_grid_item);
        }
      }
      break;
    case 'form':
      var temp_object = {};
      for(var i = 0; i < p_metadata.children.length; i++)
      {
        var child = p_metadata.children[i];
        clone_multiform_object
        (
            child, 
            temp_object,
            p_create_grid,
            p_from[child.name],
            `${p_path}/${child.name}`
        );
      }


        p_parent[p_metadata.name] = temp_object;
      

      break;

    case 'group':
      p_parent[p_metadata.name] = {};
      for(var i = 0; i < p_metadata.children.length; i++)
      {
        var child = p_metadata.children[i];
        clone_multiform_object
        (
            child, 
            p_parent[p_metadata.name],
            p_create_grid,
            p_from[child.name],
            `${p_path}/${child.name}`
        );
      }
      break;
    case 'app':
        p_parent["_id"] = $mmria.get_new_guid();
       for(var i = 0; i < p_metadata.children.length; i++)
       {
         var child = p_metadata.children[i];
         clone_multiform_object
         (
            child, 
            p_parent
        );
       }
       p_parent["host_state"] = sanitize_encodeHTML(window.location.host.split("-")[0]);
       p_parent["addquarter"] = $mmria.get_year_and_quarter(new Date());
       break;
    case 'string':
    case 'textarea':
    case 'html_area':
    case 'address':
        if(g_duplicate_path_set.has(p_path))
          {
            p_parent[p_metadata.name] = p_from;
          }
          else if(p_metadata.default_value && p_metadata.default_value != "")
          {
            p_parent[p_metadata.name] = new String(p_metadata.default_value);
          }
          else if(p_metadata.pre_fill && p_metadata.pre_fill != "")
          {
            p_parent[p_metadata.name] = new String(p_metadata.pre_fill);
          }
          else
          {
            p_parent[p_metadata.name] = new String();
          }
          
           break;
     case 'number':
            if(g_duplicate_path_set.has(p_path))
            {
                p_parent[p_metadata.name] = p_from;
            }
           else if(p_metadata.default_value && p_metadata.default_value != "")
            {
              p_parent[p_metadata.name] = new Number(p_metadata.default_value);
            }
            else
            {
              p_parent[p_metadata.name] = "";
            }
           break;
     case 'boolean':
            if(g_duplicate_path_set.has(p_path))
            {
                p_parent[p_metadata.name] = p_from;
            }
            else if(p_metadata.default_value && p_metadata.default_value != "")
            {
              p_parent[p_metadata.name] = new Boolean(p_metadata.default_value);
            }
            else
            {
              p_parent[p_metadata.name] = new Boolean();
            }
            break;
    case 'list':
    case 'yes_no':
        if(g_duplicate_path_set.has(p_path))
        {
            p_parent[p_metadata.name] = p_from;
        }
        else if(p_metadata['is_multiselect'] && p_metadata.is_multiselect == true)
        {
          p_parent[p_metadata.name] = [];
        }
        else
        {
          p_parent[p_metadata.name] = "9999";
        }
            
        break;
     case 'date':
     case 'datetime':
            if(g_duplicate_path_set.has(p_path))
            {
                p_parent[p_metadata.name] = p_from;
            }
            else if(p_metadata.default_value && p_metadata.default_value != "")
            {
              p_parent[p_metadata.name] = new Date(p_metadata.default_value);
            }
            else
            {
              p_parent[p_metadata.name] = "";
            }
            break;
    case 'time':
            if(g_duplicate_path_set.has(p_path))
            {
                p_parent[p_metadata.name] = p_from;
            }
            else
            {
                //p_parent[p_metadata.name] = new Date("2016-01-01T00:00:00.000Z");
                p_parent[p_metadata.name] = "";
            }
          break;
    case 'hidden':
            if(g_duplicate_path_set.has(p_path))
            {
                p_parent[p_metadata.name] = p_from;
            }
            else
            {
                p_parent[p_metadata.name] = "";
            }
          break;
    case 'jurisdiction':
            if(g_duplicate_path_set.has(p_path))
            {
                p_parent[p_metadata.name] = p_from;
            }
            else
            {
                p_parent[p_metadata.name] = "/";
            }
          break;           
    case 'label':
    case 'button':
    case 'always_enabled_button':
    case 'chart':
            break;
     default:
          console.log("clone_multiform_object not processed", p_metadata);
       break;
  }



  return p_parent;
}