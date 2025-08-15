const chart_function_params_map = new Map();
const chart_start_increment_map = new Map();

chart_start_increment_map.set("blood_pressure_graph", { start: 40, increment: 20});
//chart_start_increment_map.set("prm_diast", { start: 40, increment: 20});
chart_start_increment_map.set("weight_gain_graph", { start: 100, increment: 20});
chart_start_increment_map.set("hematocrit_graph", { start: 10, increment: 2});
chart_start_increment_map.set("temperature_graph", { start: 90, increment: 2});
chart_start_increment_map.set("pulse_graph", { start: 0, increment: 10});
chart_start_increment_map.set("respiration_graph", { start: 0, increment: 2});
//chart_start_increment_map.set("evahmrvs_b_systo", { start: 40, increment: 20});
//chart_start_increment_map.set("evahmrvs_b_dias", { start: 40, increment: 20});


      /*


weight_gain_graph 
hematocrit_graph 
temperature_graph 
pulse_graph 
respiration_graph 



Blood Pressure 
prenatal/routine_monitoring/systolic_bp
    systolic_bp prm_s_bp Systolic 40 20

prenatal/routine_monitoring/diastolic
    diastolic prm_diast Diastolic 40 20

prenatal/routine_monitoring/weight
prm_weigh
Weight Gain weight Weight Gain (lbs.) 100 20

prenatal/routine_monitoring/blood_hematocrit
Hematocrit prm_b_hemat Blood Hematocrit 10 2

er_visit_and_hospital_medical_records/vital_signs/temperature
Temperature evahmrvs_tempe Temperature 90 2

er_visit_and_hospital_medical_records/vital_signs/pulse
Heart Rate evahmrvs_pulse Pulse 0 10

er_visit_and_hospital_medical_records/vital_signs/respiration
Respiration evahmrvs_respi Respiration 0 2

er_visit_and_hospital_medical_records/vital_signs/bp_systolic
Blood Pressure evahmrvs_b_systo Systolic 40 20

er_visit_and_hospital_medical_records/vital_signs/bp_diastolic
evahmrvs_b_dias Diastolic 40 20

*/

function chart_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx, p_is_de_identified = false)
{
	let style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];

  const function_params = {
      p_result: p_result, 
      p_metadata: p_metadata, 
      p_data: p_data, 
      p_ui: p_ui, 
      p_metadata_path: p_metadata_path, 
      p_object_path: p_object_path, 
      p_dictionary_path: p_dictionary_path, 
      p_is_grid_context: p_is_grid_context, 
      p_post_html_render: p_post_html_render, 
      p_search_ctx: p_search_ctx, 
      p_ctx: p_ctx, 
      p_is_de_identified: p_is_de_identified

  };

  const map_key = convert_object_path_to_jquery_id(p_object_path);
  chart_function_params_map.set(map_key, function_params);

	p_result.push
	(
		`<div id='${map_key}'
		  mpath='id='${p_metadata_path}' 
		  style='${get_only_size_and_position_string(style_object.control.style)}'
		>
            <table style='border-color:#e0e0e0;padding:5px;' border=1>
            <tr align=center style='background-color:#b890bb;'>
              <th style="padding-bottom: 0.2rem;" colspan="100">
                <div style="display: flex; align-items: center;">
                  <span style="flex: 2; padding-left: 6rem;">
                    ${p_metadata.prompt.replace(" Graph", "")} 
                  </span>
                  <span style="background: #FFFFFF; font-size: small; margin-left: auto; padding: .05rem; margin-right: .2rem; margin-bottom: .2rem; margin-top: .2rem;">
                    Graph |
                    <a href="javascript:chart_switch_to_table('${map_key}')">Table</a>
                  </span>
                </div>
              </th>
            </tr>
            <tr align=center><td>
			<div id='${map_key}_chart'>
            
            </div>
            </td></tr>
            </table>
		</div>
		`
		
	);

  

	var chart_size = get_chart_size(style_object.control.style);
	var chart_gen_name = "chart_" + map_key;

   let translate_x = "-30";
   if
   (
       p_metadata.x_type != null &&
       p_metadata.x_type.toLowerCase() == 'datetime'
   )
   {
        translate_x = "-25";
   }

   const computed_height = chart_size.height - 23;

	p_post_html_render.push(` g_charts['${chart_gen_name}'] = 
	  c3.generate({
		size: {
		height: ${computed_height}
		, width: ${chart_size.width}
      },
	  transition: {
	    duration: null
      },
      bindto: '#${map_key}_chart',
      onrendered: function()
      {
		const el = d3.select('#${map_key} svg').selectAll('g.c3-axis.c3-axis-x > g.tick > text');
        el.attr('transform', 'rotate(325)translate(${translate_x},0)');

      },`);



    if(p_metadata.x_axis && p_metadata.x_axis != "")
    {
        p_post_html_render.push("axis: {");
        p_post_html_render.push("x: {");
        p_post_html_render.push("type: 'timeseries',");
        p_post_html_render.push("localtime: true,");
        //p_post_html_render.push("label: {");

        //p_post_html_render.push(" position: 'outer-center',");
        //p_post_html_render.push("},");
        p_post_html_render.push("tick: {");
        if
        (
            p_metadata.x_type != null &&
            p_metadata.x_type.toLowerCase() == 'datetime'
        )
        {
		    p_post_html_render.push(" format: '%m/%d/%Y %H:%M',");
        }
        else
        {
            p_post_html_render.push(" format: '%m/%d/%Y',");
        }
		p_post_html_render.push("},");
		p_post_html_render.push("height: 55");
		p_post_html_render.push("        }");


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

    p_post_html_render.push("  data: {");

    if(p_metadata.x_axis && p_metadata.x_axis != "")
    {
        p_post_html_render.push("x: 'x', xFormat: '%Y-%m-%d %H:%M',");
    }

    p_post_html_render.push("      columns: [");

    var y_axis_paths = p_metadata.y_axis.split(",");

    if( ! g_charts.has(p_metadata.x_axis))
    {
        g_charts.set(p_metadata.x_axis, new Set()); 
    }

    g_charts.get(p_metadata.x_axis).add(chart_gen_name); 

    const x_array = get_chart_x_range_from_path(p_metadata, p_metadata.x_axis, p_ui);
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
        const y_axis_path = y_axis_paths[y_index].trim();

        if( ! g_charts.has(y_axis_path))
        {
            g_charts.set(y_axis_path, new Set()); 
        }

        g_charts.get(y_axis_path).add(chart_gen_name); 
       
        const y_array = get_chart_y_range_from_path(p_metadata, y_axis_path, p_ui)
        
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
        const y_axis_path = y_axis_paths[y_index].trim();
        
        const y_array = get_chart_y_range_from_path(p_metadata, y_axis_paths[y_index], p_ui)
        
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
    




	g_chart_data.set
    (
        `${chart_gen_name}`, 
        {
            div_id: map_key,
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
    
        }
    );




    p_post_html_render.push("  ]");
    p_post_html_render.push("  },");
	p_post_html_render.push("  line: {");
	p_post_html_render.push("     connectNull: true");
	p_post_html_render.push("  }");
    p_post_html_render.push("  });");

    p_post_html_render.push(" d3.select('#" + map_key + " svg').append('text')");
    p_post_html_render.push("     .attr('x', d3.select('#" + map_key + " svg').node().getBoundingClientRect().width / 2)");
    p_post_html_render.push("     .attr('y', 16)");
    p_post_html_render.push("     .attr('text-anchor', 'middle')");
    p_post_html_render.push("     .style('font-size', '1.4em');");
	//p_post_html_render.push("     .text('" + p_metadata.prompt.replace(/'/g, "\\'") + "');");
	
}


function get_chart_x_range_from_path(p_metadata, p_metadata_path, p_ui)
{
	//prenatal/routine_monitoring/systolic_bp,prenatal/routine_monitoring/diastolic
	// p_ui.url_state.path_array.length
	let result = [];
	const array_field = eval(convert_dictionary_path_to_array_field(p_metadata_path));

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
					const res2 = val.match(/^\d\d\d\d-\d\d?-\d\d?[ T]?\d?\d:\d\d:\d\d(.\d\d\d)?[Z]?$/)
					if(res2)
					{
						//let date_time = new Date(val);
						//result.push("'" + date_time.toISOString() + "'");
						result.push("'" + make_c3_date(val) +"'");
					}
                    else
                    {
                        // '2017-06-01T07:30:00-04:00'
                        // '2017-06-01T11:30:00+00:00'
                        const res3 = val.match(/^\d\d\d\d-\d\d?-\d\d?[ T]?\d?\d:\d\d:\d\d[-+]\d\d:\d\d$/)
                        if(res3)
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

function get_chart_y_range_from_path(p_metadata, p_metadata_path, p_ui, p_label)
{
	//prenatal/routine_monitoring/systolic_bp,prenatal/routine_monitoring/diastolic
	// p_ui.url_state.path_array.length
	const result = [];
	const array_field = eval(convert_dictionary_path_to_array_field(p_metadata_path));

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


function update_charts(p_path)
{


    if
    (
        p_path != null &&
        ! g_charts.has(p_path.substring(1))
    )
    {
        return;
    }

    const chart_set = g_charts.get(p_path.substring(1));
    
    for (const chart of chart_set)
    {
        const chart_data = g_chart_data.get(chart);

        const p_result = [];
        const p_post_html_render = [];

        chart_render
        (
            p_result, 
            chart_data.p_metadata, 
            null, // undefined
            chart_data.p_ui, // g_ui
            chart_data.p_metadata_path, //"g_metadata.children[17].children[12]"
            chart_data.p_object_path, // "g_data.er_visit_and_hospital_medical_records[0].temperature_graph"
            chart_data.p_dictionary_path, // "/er_visit_and_hospital_medical_records/temperature_graph"
            chart_data.p_is_grid_context, // false
            p_post_html_render, 
            chart_data.p_search_ctx, // undefined
            chart_data.p_ctx // { form_index: 0, grid_index: null }
        );

        document.getElementById(chart_data.div_id).outerHTML = p_result.join('');
      
        if (p_post_html_render.length > 0) 
        {
          try
          {
            eval(p_post_html_render.join(''));
          } 
          catch (ex) 
          {
            console.log(ex);
          }
        }


           // console.log("here");

    }
}

function chart_onrendered()
{
    const el = d3.select('#${convert_object_path_to_jquery_id(p_object_path)} svg').selectAll('g.c3-axis.c3-axis-x > g.tick > text');

    el.attr('transform', 'rotate(325)translate(${translate_x},0)');
    el.innerText = '';
}


function chart_switch_to_table(p_ui_div_id)
{
    const el = document.getElementById(p_ui_div_id);
    const params = chart_function_params_map.get(p_ui_div_id);
    let style_object = g_default_ui_specification.form_design[params.p_dictionary_path.substring(1)];

    // Date         Systolic Diastolic
    // Date         Weight (lbs.)
    // Date         Blood Hematocrit
    // MM/DD/YYYY   ###

    let result = [];
    const metadata = eval(params.p_metadata_path);
    const x_data_type = metadata.x_type;
    const y_data_type = metadata.y_type;
    let graph_prefix = "";
    let bp_header_prefix = "bp_";
    let bp_header_suffix = "_bp";

    if(metadata.x_axis.indexOf("vital_signs") > -1)
    {
        graph_prefix = ".vital_signs.";
    }

    let last_index = metadata.x_axis.lastIndexOf("/") + 1;
    const x_axis = metadata.x_axis.substr(last_index).trim();
    const y_axis = [];
    metadata.y_axis.split(',').forEach(element => {
        
        const index = element.lastIndexOf("/") + 1;
        y_axis.push(graph_prefix + element.substr(index).trim());
    });

    last_index = metadata.x_axis.lastIndexOf("/");
    const object_path_last_index = params.p_object_path.lastIndexOf(".")
    
    const pre_object = params.p_object_path.substring(0, object_path_last_index);
    let data = null;
    if(graph_prefix == "")
    {
        data = eval("g_data." + metadata.x_axis.trim().replace("/",".").substring(0, last_index));
    }
    else
    {
        data = eval(pre_object + graph_prefix.substring(0,graph_prefix.length - 1));
    }

    const data_table_header_html = [];
    const data_table_body_html = [];
    let xTypeLabel = metadata.x_type.indexOf("time") == -1 ? "Date" : "Date / Time";
    data_table_header_html.push(`<tr><th style="background-color: #E3D3E4; padding-left: 5px;">${xTypeLabel}</th>`)
    y_axis.forEach(element => {
        let header_string = "";
        header_string = element.replace(graph_prefix, "").replace(bp_header_prefix, "").replace(bp_header_suffix, "");
        header_string = header_string.replace("_", " ");
        header_string = header_string.split(' ').map(word => word.charAt(0).toUpperCase() + word.slice(1, word.length + 1)).join(' ');
        //if(header_string == "Weight")
        //  header_string += " (lbs.)";
        data_table_header_html.push(`<th style="background-color: #E3D3E4; padding-left: 5px;">${header_string}</th>`)
    });
    data_table_header_html.push(`</tr>`);

data.forEach(row => {
  let date_string = "";
  let temp_date_data = row[x_axis.replace(graph_prefix, "")];
  if (metadata.x_type.indexOf("time") == -1) {
    let parts = temp_date_data.split('-');
    let localDate = new Date(parts[0], parts[1] - 1, parts[2]);
    date_string = localDate.toLocaleDateString('en-us', { month: '2-digit', day: '2-digit', year: 'numeric'});
  } else {
    date_string = new Date(temp_date_data).toLocaleDateString('en-us', { month: '2-digit', day: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false});
  }
  data_table_body_html.push(`<tr><td style="padding-left: 5px;">${date_string.replace(",", "")}</td>`)
  y_axis.forEach(col => {
    data_table_body_html.push(`<td style="padding-left: 5px;">${row[col.replace(graph_prefix, "")]}</td>`)
  });
  data_table_body_html.push(`</tr>`);
});

    el.outerHTML = 	`
        <div id='${convert_object_path_to_jquery_id(params.p_object_path)}'
        mpath='id='${params.p_metadata_path}' 
        style='${get_only_size_and_position_string(style_object.control.style)};overflow-y: auto;'>
        <table style='border-color:#e0e0e0;padding:5px; width: 100%;' border=1>
        <thead style="position: sticky; top: 0px">
          <tr align=center style='background-color:#b890bb;'>
              <th style="padding-bottom: 0.2rem;" colspan="100">
              <div style="display: flex; align-items: center;">
                <span style="flex: 2; padding-left: 6rem;">
                    ${params.p_metadata.prompt.replace(" Graph", "")} 
                </span>
                <span style="background: #FFFFFF; font-size: small; margin-left: auto; padding: .05rem; margin-right: 0.2rem; margin-bottom: 0.2rem; margin-top: 0.2rem;">
                    <a role="button" href="javascript:chart_switch_to_graph('${convert_object_path_to_jquery_id(params.p_object_path)}')">Graph</a> |
                    Table
                </span>
              </div>
              </th>
          </tr>
          <tr style="display: none;" aria-hidden="true"  align=center>
            <td>
              <div id='${convert_object_path_to_jquery_id(params.p_object_path)}_chart'>
                ${data_table_header_html.join("")}
              </div>
            </td>
          </tr>     
        </thead>
        <tbody>
          ${data_table_body_html.join("")}
        </tbody>        
        </table>
    </div>`;
}

function chart_switch_to_graph(p_ui_div_id)
{

    var params = chart_function_params_map.get(p_ui_div_id);

    const el = document.getElementById(p_ui_div_id);
    const result = [];
    const post_html_render = [];
    chart_render
    (
        result, 
        params.p_metadata, 
        params.p_data, 
        params.p_ui, 
        params.p_metadata_path, 
        params.p_object_path, 
        params.p_dictionary_path, 
        params.p_is_grid_context, 
        post_html_render, 
        params.p_search_ctx, 
        params.p_ctx, 
        params.p_is_de_identified
    );


    el.outerHTML = result.join("");

    if(post_html_render.length > 0)
    {
        eval(post_html_render.join(""));
    }
}