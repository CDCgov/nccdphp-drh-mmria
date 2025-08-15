const chart_start_increment_map = new Map();

chart_start_increment_map.set("blood_pressure_graph", { start: 40, increment: 20});

chart_start_increment_map.set("weight_gain_graph", { start: 100, increment: 20});
chart_start_increment_map.set("hematocrit_graph", { start: 10, increment: 2});
chart_start_increment_map.set("temperature_graph", { start: 90, increment: 2});
chart_start_increment_map.set("pulse_graph", { start: 0, increment: 10});

function print_version_render
(
  p_metadata,
  p_data,
  p_path,
  p_ui,
  p_metadata_path,
  p_object_path,
  p_post_html_render,
  p_multiform_index,
  p_is_grid_context
) 
{
  let result = [];

  switch (p_metadata.type.toLowerCase()) 
  {
    case 'group':
      result.push('<fieldset>');
      //result.push(p_path)
      result.push('<legend><strong>');
      result.push(p_metadata.prompt);
      result.push('</strong></legend> ');
      //result.push(p_data[p_metadata.name]);
      for (let i = 0; i < p_metadata.children.length; i++) 
      {
        let child = p_metadata.children[i];
        if (p_data[child.name] != null || child.type == 'chart')
        {
          Array.prototype.push.apply(
            result,
            print_version_render
            (
              child,
              p_data[child.name],
              p_path + '.' + child.name,
              p_ui,
              p_metadata_path + '.children[' + i + ']',
              p_object_path + '.' + child.name,
              p_post_html_render,
              p_multiform_index,
              p_is_grid_context
            )
          );
        }
      }
      result.push('</fieldset>');

      break;

    case 'grid':
      result.push('<table border="1">');
      //result.push(p_path)
      result.push('<tr><th colspan=');
      result.push(p_metadata.children.length);
      result.push('>');
      result.push(p_metadata.prompt);
      result.push('</th></tr>');
      //result.push(p_data[p_metadata.name]);
      result.push('<tr>');
      for (let i = 0; i < p_metadata.children.length; i++) 
      {
        let child = p_metadata.children[i];
        result.push("<td data-child='td-1'>");
        result.push(child.prompt);
        result.push('</td>');
      }
      result.push('</tr>');

      for (let i = 0; i < p_data.length; i++) 
      {
        result.push('<tr>');
        for (let j = 0; j < p_metadata.children.length; j++) 
        {
          result.push("<td data-child='td-2'>");
          let child = p_metadata.children[j];

          if (p_data[i][child.name] != null)
          {
              Array.prototype.push.apply
              (
                  result,
                  print_version_render
                  (
                      child,
                      p_data[i][child.name],
                      p_path + '.' + child.name,
                      p_ui,
                      p_metadata_path,
                      p_object_path + '[' + i + '].' + child.name,
                      p_post_html_render,
                      p_multiform_index,
                      true
                  )
              );
          }
          result.push('</td>');
        }
        result.push('</tr>');
      }
      result.push('</table>');

      break;

    case 'form':
      if (p_metadata.cardinality == '+' || p_metadata.cardinality == '*') 
      {
        result.push('<section id="');
        result.push(p_metadata.name);
        result.push('">');

        for (let form_index = 0; form_index < p_data.length; form_index++) 
        {
          let form_item = p_data[form_index];

          //result.push(p_metadata.name)
          result.push('<div data-record="' + (form_index + 1) + '">');
          result.push('<h2>');
          result.push(p_metadata.prompt);
          result.push(' Record: ');
          result.push(form_index + 1);
          result.push('</h2> ');
          //result.push(p_data[p_metadata.name]);

          if (p_metadata.children) 
          {
            for (let i = 0; i < p_metadata.children.length; i++) 
            {
                let child = p_metadata.children[i];
                if (form_item[child.name] != null || child.type == 'chart')
                {
                    Array.prototype.push.apply
                    (
                        result,
                        print_version_render
                        (
                            child,
                            form_item[child.name],
                            p_path + '.' + child.name,
                            p_ui,
                            p_metadata_path + '.children[' + i + ']',
                            p_object_path + '[' + i + '].' + child.name,
                            p_post_html_render,
                            form_index,
                            false
                        )
                    );
                }
            }
          }
          result.push('</div>');
        }

        result.push('</section>');
      } 
      else 
      {
        result.push('<section id="');
        result.push(p_metadata.name);
        result.push('"> <h2>');
        result.push(p_metadata.prompt);
        result.push('</h2> ');
        //result.push(p_data[p_metadata.name]);

        if (p_metadata.children) 
        {
          for (let i = 0; i < p_metadata.children.length; i++) 
          {
            let child = p_metadata.children[i];
            if (p_data[child.name] != null || child.type == 'chart')
            {
              Array.prototype.push.apply(
                result,
                print_version_render(
                  child,
                  p_data[child.name],
                  p_path + '.' + child.name,
                  p_ui,
                  p_metadata_path + '.children[' + i + ']',
                  p_object_path + '.' + child.name,
                  p_post_html_render,
                  p_multiform_index,
                  false
                )
              );
            }
          }
        }
        result.push('</section>');
      }

      break;

    case 'list':
      let data_value_list = p_metadata.values;
      let list_lookup = {};
      let is_omit_display_value = false;
      let is_show_display_only = false;




      if (p_metadata.path_reference && p_metadata.path_reference != '') 
      {
        if
        (
            p_metadata.path_reference.indexOf("lookup/year") > -1 ||
            p_metadata.path_reference.indexOf("lookup/month") > -1 ||
            p_metadata.path_reference.indexOf("lookup/day") > -1
        )
        {
            is_omit_display_value = true;
        }

        if(p_metadata.path_reference.indexOf("lookup/substance") > -1)
        {
            is_show_display_only = true;
        }

        data_value_list = eval
        (
          convert_dictionary_path_to_lookup_object(p_metadata.path_reference)
        );

        if (data_value_list == null) 
        {
          data_value_list = p_metadata.values;
        }
      }

      for 
      (
        let list_index = 0;
        list_index < data_value_list.length;
        list_index++
      ) 
      {
        let list_item = data_value_list[list_index];
        list_lookup[list_item.value] = list_item.display;
      }

      result.push('<p>');

      if(! p_is_grid_context)
      {
        result.push('<h9>');
        result.push(' <strong>');
        result.push(p_metadata.prompt);
        result.push('</strong>: ');
      }


      if (Array.isArray(p_data)) 
      {
        result.push('<ul>');

        for (let i = 0; i < p_data.length; i++) 
        {
            if
            (
                (p_data[i] == 9999 || p_data[i] == "9999") &&
                p_data.length > 1
            )
            {
                continue;
            }
          result.push('<li>');

          if(! is_show_display_only)
          {
            result.push(p_data[i]);
          }

          if(! is_omit_display_value)
          {
            if(! is_show_display_only)
            {
                result.push(' - ');
            }
            result.push(list_lookup[p_data[i]]);
          }
          result.push('</li>');
        }
        result.push('</ul>');
      } 
      else 
      {
        if(! is_show_display_only)
        {
            result.push(p_data);
        }

        if(! is_omit_display_value)
        {
            if(! is_show_display_only)
            {
                result.push(' - ');
            }
            result.push(list_lookup[p_data]);
        }

      }

      if(! p_is_grid_context)
      {
        result.push('</h9>');
      }
      result.push('</p>');
      break;

    case 'app':
      /*
				result.push('<p>');
				//result.push(p_path)
				result.push(' <strong>')
				result.push(p_metadata.prompt);
				result.push('</strong>: ');
				result.push(p_data[p_metadata.name]);
				result.push('</p>');
				*/
      if (p_metadata.children) 
      {
        for (let i = 0; i < p_metadata.children.length; i++) 
        {
            let child = p_metadata.children[i];
            if (child.type.toLowerCase() == 'form' && p_data[child.name] != null)
            {
                Array.prototype.push.apply
                (
                    result,
                    print_version_render
                    (
                        child,
                        p_data[child.name],
                        p_path + '.' + child.name,
                        p_ui,
                        p_metadata_path + '.children[' + i + ']',
                        p_object_path + '.' + child.name,
                        p_post_html_render,
                        p_multiform_index,
                        p_is_grid_context
                    )
                );
            }
        }
      }
      break;

    case 'chart':

        let minimum_graph_value = 0;
        let increment_graph_value = 10;
        
        if
        (
            chart_start_increment_map.has(p_metadata.name)
        )
        {
            const key_value = chart_start_increment_map.get(p_metadata.name);

            minimum_graph_value = key_value.start;
            increment_graph_value = key_value.increment;

        }


      result.push("<div  class='chart' id='");
      result.push
      (
        convert_object_path_to_jquery_id(p_object_path, p_multiform_index)
      );

      result.push("' ");
      result.push(" mpath='");
      result.push(p_metadata_path);
      result.push("' ");
      result.push('>');
      result.push('<span ');
      if (p_metadata.description && p_metadata.description.length > 0) 
      {
        result.push("rel='tooltip'  data-original-title='");
        result.push(p_metadata.description.replace(/'/g, "\\'"));
        result.push("'>");
      } 
      else 
      {
        result.push('>');
      }
      result.push(p_metadata.prompt);
      result.push('</span>');
      result.push('</div>');
      p_post_html_render.push('  c3.generate({');
      p_post_html_render.push('  size: {');
      p_post_html_render.push('  height: 240,');
      p_post_html_render.push('  width: 480');
      p_post_html_render.push('  },');
      p_post_html_render.push("  bindto: '#");
      p_post_html_render.push
      (
        convert_object_path_to_jquery_id(p_object_path, p_multiform_index)
      );
      p_post_html_render.push("',");

      /*
d3.select('#chart svg').append('text')
    .attr('x', d3.select('#chart svg').node().getBoundingClientRect().width / 2)
    .attr('y', 16)
    .attr('text-anchor', 'middle')
    .style('font-size', '1.4em')
    .text('Title of this chart');
*/

      if (p_metadata.x_axis && p_metadata.x_axis != '') 
      {
        p_post_html_render.push('axis: {');
        p_post_html_render.push('x: {');
        p_post_html_render.push("type: 'timeseries',");
        p_post_html_render.push('localtime: false,');
        p_post_html_render.push('tick: {');
        if
        (
            p_metadata.x_type != null &&
            p_metadata.x_type.toLowerCase() == 'datetime'
        )
        {
		    p_post_html_render.push(" format: '%m/%d/%Y %H:%M:%S',");
        }
        else
        {
            p_post_html_render.push(" format: '%m/%d/%Y'");
        }
        p_post_html_render.push('},');
        p_post_html_render.push("height: 55");
        p_post_html_render.push("        }");

        let format_text_size = ".0f";
        if (p_metadata.name === "temperature_graph") 
        {
            format_text_size = ".1f"
        }


        p_post_html_render.push
        (`
            ,y: {
                
                tick: {
                        values: d3.range(${minimum_graph_value}, 450, ${increment_graph_value}),
                        format: d3.format('${format_text_size}'),
                        },
                min: ${minimum_graph_value},
                padding: {top: 0, bottom: 0},
            },
        `);

        p_post_html_render.push("        },");
      }

      p_post_html_render.push('  data: {');

      if (p_metadata.x_axis && p_metadata.x_axis != '') 
      {
        p_post_html_render.push("x: 'x', xFormat: '%Y-%m-%d %H:%M',");
        //p_post_html_render.push("x: 'x', ");
      }

      p_post_html_render.push('      columns: [');

      var y_axis_paths = p_metadata.y_axis.split(",");

    const x_array = get_chart_x_range_from_path(p_metadata, p_metadata.x_axis, p_multiform_index);
    const x_has_value = [];
    const y_has_value = [];
    if(x_array.length > 0)
    {
        for(const index in x_array)
        {
            if
            (
                x_array[index] != null &&
                x_array[index] != ''
            )
            {
                x_has_value [index] = true;
               
            }
            else
            {
                x_has_value[index] = false;
            }
        }
    }


    for(var y_index = 0; y_index < y_axis_paths.length; y_index++)
    {
        const y_axis_path = y_axis_paths[y_index];
       
        const y_array = get_chart_y_range_from_path(p_metadata, y_axis_paths[y_index], p_multiform_index)
        
        if(y_array.length > 0)
        {
            for(const index in y_array)
            {
                if
                (
                    y_array[index] != null && 
                    y_array[index] != '' &&
                    y_array[index] != 'null'
                )
                {
                    y_has_value [index] = true;
                }
                else
                {
                    y_has_value[index] = false;
                }
            }
        }


        if(x_array.length > 0)
        {
            for(const index in x_array)
            {
                if
                (
                    x_has_value [index] &&
                    y_has_value [index]
                )
                {
                    p_post_html_render.push(x_array[index]);
                    if(index != x_array.length - 1)
                    {
                        p_post_html_render.push(",")
                    }
                }
            }
    
            p_post_html_render.push("],")
        }
    
            
        for(var y_index = 0; y_index < y_axis_paths.length; y_index++)
        {
            const y_axis_path = y_axis_paths[y_index];
            
            const y_array = get_chart_y_range_from_path(p_metadata, y_axis_paths[y_index], p_multiform_index)
            
            if(y_array.length > 0)
            {
                for(const index in y_array)
                {
                    if
                    (
                        y_has_value[index] && 
                        x_has_value[index]
                    )
                    {
                        p_post_html_render.push(y_array[index]);
                        if(index != y_array.length - 1)
                        {
                            p_post_html_render.push(",")
                        }
                    }
                }
        
                p_post_html_render.push("],")
            }
        }
    

    }




/*

	g_chart_data.set(`${chart_gen_name}`, {
    div_id: convert_object_path_to_jquery_id(p_object_path),
	p_result: p_result,
	p_metadata: p_metadata,
    p_ui: p_ui,
    p_metadata_path: p_metadata_path,
    p_object_path: p_object_path,
    p_dictionary_path: p_dictionary_path,
    p_is_grid_context: p_is_grid_context,
    p_post_html_render: p_post_html_render,
    p_search_ctx: p_search_ctx,
    p_ctx: p_ctx,
    style_object: style_object
    
    });
*/
      p_post_html_render.push('  ]');
      p_post_html_render.push("  },");
      p_post_html_render.push("  line: {");
      p_post_html_render.push("     connectNull: true");
      p_post_html_render.push("  }");
      p_post_html_render.push('  });');

      p_post_html_render.push
      (
        " d3.select('#" +
          convert_object_path_to_jquery_id(p_object_path, p_multiform_index) +
          " svg').append('text')"
      );
      p_post_html_render.push
      (
        "     .attr('x', d3.select('#" +
          convert_object_path_to_jquery_id(p_object_path, p_multiform_index) +
          " svg').node().getBoundingClientRect().width / 2)"
      );
      p_post_html_render.push("     .attr('y', 16)");
      p_post_html_render.push("     .attr('text-anchor', 'middle')");
      p_post_html_render.push("     .style('font-size', '1.4em')");
      p_post_html_render.push("     .text('" + p_metadata.prompt + "');");

      if (p_metadata.x_axis && p_metadata.x_axis != '') {
          p_post_html_render.push(" d3.select('#" + convert_object_path_to_jquery_id(p_object_path, p_multiform_index) + " svg').selectAll('g.c3-axis.c3-axis-x > g.tick > text')");
          p_post_html_render.push("     .attr('transform', 'rotate(325)translate(-25,0)');");
      }

      break;

    case 'boolean':
      result.push('<h9>');
      result.push('<p>');
      if(! p_is_grid_context)
      {
        result.push(' <strong>');
        result.push(p_metadata.prompt);
        result.push('</strong>: ');
      }

      if (p_data) 
      {
        result.push(p_data);
      } 
      else 
      {
        result.push('&nbsp;');
      }
      result.push('</p>');
      result.push('</h9>');

      break;
    case 'textarea':
        if (p_metadata.name == 'case_opening_overview') 
        {
            /*
            result.push('<h9>');
            result.push('<p>');
            
            if(! p_is_grid_context)
            {
                result.push(' <strong>');
                result.push("Case Narrative");
                result.push('</strong>: ');
            }
            result.push('</p>');
            result.push('</h9>');*/
            result.push('<div>');
            result.push(print_version_textarea_replace_return_with_br(p_data));
            result.push('</div>');
        }
        else
        {
            result.push('<h9>');
            result.push('<p>');
            if(! p_is_grid_context)
            {
                result.push(' <strong>');
                result.push(p_metadata.prompt);
                result.push('</strong>: ');
            }
            result.push('</p>');
            result.push('</h9>');
            result.push('<div>');
            result.push(print_version_textarea_replace_return_with_br(p_data));
            result.push('</div>');
        }
        
    break;
    case 'hidden':
        if(g_show_hidden)
        {
            result.push('<h9>');
            result.push('<p>');
            if(! p_is_grid_context)
            {
                result.push(' <strong>');
                result.push(p_metadata.prompt);
                result.push('</strong>: ');
            }
    
            result.push('</p>');
            result.push('</h9>');
            result.push('<div>');
            result.push(p_data);
            result.push('</div>');
        }

    break;
    default:
      if (p_metadata.name != 'case_opening_overview') 
      {
        result.push('<h9>');
        result.push('<p>');
        if(! p_is_grid_context)
        {
            result.push(' <strong>');
            result.push(p_metadata.prompt);
            result.push('</strong>: ');
        }

        result.push('</p>');
        result.push('</h9>');
        result.push('<div>');
        result.push(p_data);
        result.push('</div>');
      }
      else//if (p_metadata.name == 'case_opening_overview') 
      {
        result.push('<div>');

        if (g_data.home_record.record_id) 
        {
          result.push(g_data.home_record.record_id);
          result.push('<br>');
        }

        result.push(p_data);
        result.push('</div>');
      }



      break;
  }

  return result;
}

function get_chart_x_range_from_path(p_metadata, p_metadata_path, p_multiform_index)
{
	//prenatal/routine_monitoring/systolic_bp,prenatal/routine_monitoring/diastolic
	// p_ui.url_state.path_array.length
	let result = [];
	const array_field = eval(convert_dictionary_path_to_array_field(p_metadata_path, p_multiform_index));

	const array = eval(array_field[0]);
	if(array)
	{
		const field = array_field[1];


		result.push("['x'");
		// ['data2', 50, 20, 10, 40, 15, 25]
		//result.push(50, 20, 10, 40, 15, 25);

		//result = ['data2', 50, 20, 10, 40, 15, 25];
		for(let i = 0; i < array.length; i++)
		{
			const val = array[i][field];
			if(val)
			{
				const res = val.match(/^\d\d\d\d-\d\d?-\d+$/);
				if(res)
				{
					result.push("'" + make_c3_date(val) +"'");
				}
				else 
				{
					//const res2 = val.match(/^\d\d\d\d-\d\d?-\d\d?[ T]?\d?\d:\d\d:\d\d(.\d\d\d)?[Z]?$/)
                    const res2 = val.match(/^\d{4}-\d{2}-\d{2}[ T]?\d{2}:\d{2}:\d{2}(\.\d{3})?([+-]\d{2}:\d{2}|Z)?$/);
					if(res2)
					{
						//let date_time = new Date(val);
						//result.push("'" + date_time.toISOString() + "'");
						result.push("'" + make_c3_date(val) +"'");
					}
					else
					{
						result.push(parseFloat(val));
					}
				}
			}
			else
			{
				result.push(null);
			}
			
		}

		//result[result.length-1] = result[result.length-1] + "]";
		//return result.join(",") + ",";
	}
	else
	{
		//return "";
	}

    return result;
}

function get_chart_y_range_from_path(p_metadata, p_metadata_path, p_multiform_index, p_label)
{
	//prenatal/routine_monitoring/systolic_bp,prenatal/routine_monitoring/diastolic
	// p_ui.url_state.path_array.length
	const result = [];
	const array_field = eval(convert_dictionary_path_to_array_field(p_metadata_path, p_multiform_index));

	const array = eval(array_field[0]);

	const field = array_field[1];

	if(p_label)
	{
		result.push("['" + p_label + "'");
	}
	else
	{
		result.push("['" + array_field[1] + "'");
	}
	
	if(array)
	{
		// ['data2', 50, 20, 10, 40, 15, 25]
		//result.push(50, 20, 10, 40, 15, 25);

		//result = ['data2', 50, 20, 10, 40, 15, 25];
		for(let i = 0; i < array.length; i++)
		{
			const val = array[i][field];
			if(val)
			{
				result.push(parseFloat(val).toFixed(2));
			}
			else
			{
				result.push('null');
			}
			
		}

		//result[result.length-1] = result[result.length-1] + "]";
		//return result.join(",");
	}
	else
	{
		//return result.join("") + "]";;
	}

    return result;
}

function convert_dictionary_path_to_array_field
(
  p_path,
  p_current_multiform_index
) 
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
  //let temp = "g_data." + p_path.replace(new RegExp('/','gm'),".").replace(new RegExp('\\.(\\d+)\\.','gm'),"[$1].").replace(new RegExp('\\.(\\d+)$','g'),"[$1]");

  let result = [];
  let temp = 'g_data.' + p_path.replace(new RegExp('/', 'gm'), '.');
  let multi_form_check = temp.split('.');
  let check_path = eval(multi_form_check[0] + '.' + multi_form_check[1]);

  if (Array.isArray(check_path)) 
  {
    let new_path = [];

    for (let i = 0; i < multi_form_check.length; i++) 
    {
      if (i == 1) 
      {
        new_path.push
        (
          multi_form_check[i] + '[' + p_current_multiform_index + ']'
        );
      } 
      else 
      {
        new_path.push(multi_form_check[i]);
      }
    }
    let path = new_path.join('.');
    let index = path.lastIndexOf('.');

    result.push(path.substr(0, index));
    result.push(path.substr(index + 1, path.length - (index + 1)));
  } 
  else 
  {
    let index = temp.lastIndexOf('.');

    result.push(temp.substr(0, index));
    result.push(temp.substr(index + 1, temp.length - (index + 1)));
  }

  return result;
}

function convert_dictionary_path_to_lookup_object(p_path) 
{
  //g_data.prenatal.routine_monitoring.systolic_bp
  let result = null;
  let temp_result = [];
  let temp =
    'g_metadata.' +
    p_path
      .replace(new RegExp('/', 'gm'), '.')
      .replace(new RegExp('\\.(\\d+)\\.', 'gm'), '[$1].')
      .replace(new RegExp('\\.(\\d+)$', 'g'), '[$1]');
  let index = temp.lastIndexOf('.');

  temp_result.push(temp.substr(0, index));
  temp_result.push(temp.substr(index + 1, temp.length - (index + 1)));

  let lookup_list = eval(temp_result[0]);

  for (let i = 0; i < lookup_list.length; i++) 
  {
    if (lookup_list[i].name == temp_result[1]) 
    {
      result = lookup_list[i].values;
      break;
    }
  }

  return result;
}

function convert_object_path_to_jquery_id(p_value, p_multiform_index) 
{
  if (p_multiform_index) 
  {
    return (
      p_value.replace(/\./g, '_').replace(/\[/g, '_').replace(/\]/g, '_') +
      p_multiform_index
    );
  } 
  else 
  {
    return p_value.replace(/\./g, '_').replace(/\[/g, '_').replace(/\]/g, '_');
  }
}

function make_c3_date(p_value) 
{
  //'%Y-%m-%d %H:%M:%S
  let date_time = new Date(p_value);
  let result = [];

  result.push(date_time.getFullYear());
  result.push('-');
  result.push(date_time.getMonth() + 1);
  result.push('-');
  result.push(date_time.getDate());

  result.push(' ');
  result.push(date_time.getHours());
  result.push(':');
  result.push(date_time.getMinutes());
  // result.push(':');
  // result.push(date_time.getSeconds());

  return result.join('');
}


function print_version_textarea_replace_return_with_br(p_value)
{
    let crlf_regex = /\n/g;

    let result = p_value;

    if(p_value!= null)
    {
        result = p_value.replace(crlf_regex, "<br/>");
    }

    return result
}
