function chart_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{
	var style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];
	
	p_result.push
	(
		`<div id='${convert_object_path_to_jquery_id(p_object_path)}'
		  mpath='id='${p_metadata_path}' 
		  style='${get_only_size_and_position_string(style_object.control.style)}'
		>
			<div id='${convert_object_path_to_jquery_id(p_object_path)}_chart'></div>
		</div>
		`
		
	);

	var chart_size = get_chart_size(style_object.control.style);
	var chart_gen_name = "chart_" + convert_object_path_to_jquery_id(p_object_path);

	p_post_html_render.push(` g_charts['${chart_gen_name}'] = 
	  c3.generate({
		size: {
		height: ${chart_size.height}
		, width: ${chart_size.width}
      },
	  transition: {
	    duration: null
      },
      bindto: '#${convert_object_path_to_jquery_id(p_object_path)}_chart',
      onrendered: function()
      {
		d3.select('#${convert_object_path_to_jquery_id(p_object_path)} svg').selectAll('g.c3-axis.c3-axis-x > g.tick > text')
          .attr('transform', 'rotate(325)translate(-25,0)');
      },`);


    if(p_metadata.x_axis && p_metadata.x_axis != "")
    {
        p_post_html_render.push("axis: {");
        p_post_html_render.push("x: {");
        p_post_html_render.push("type: 'timeseries',");
		p_post_html_render.push("localtime: false,");
		p_post_html_render.push("label: {");
		p_post_html_render.push(" position: 'outer-right',");
		p_post_html_render.push("},");
        p_post_html_render.push("tick: {");
        if(p_metadata.type.toLowerCase() == 'datetime')
        {
		    p_post_html_render.push(" format: '%m/%d/%Y %H:%M:%S',");
        }
        else
        {
            p_post_html_render.push(" format: '%m/%d/%Y',");
        }
		p_post_html_render.push("},");
		p_post_html_render.push("height: 55");
		p_post_html_render.push("        }");

		if (p_metadata.name === "temperature_graph") {
			p_post_html_render.push(",y: {");
			p_post_html_render.push("  tick: {");
			p_post_html_render.push("   format: d3.format('.1f'),");
			p_post_html_render.push("  },");
			p_post_html_render.push("  min: 0,");
            p_post_html_render.push("  padding: {top: 0, bottom: 0},");
			p_post_html_render.push("},");
		}
		else {
			p_post_html_render.push(",y: {");
			p_post_html_render.push("  tick: {");
			p_post_html_render.push("   format: d3.format('.0f'),");
			p_post_html_render.push("  },");
			p_post_html_render.push("  min: 0,");
            p_post_html_render.push("  padding: {top: 0, bottom: 0},");
			p_post_html_render.push("},");
		}

		p_post_html_render.push("        },");
    }

    p_post_html_render.push("  data: {");

    if(p_metadata.x_axis && p_metadata.x_axis != "")
    {
        p_post_html_render.push("x: 'x', xFormat: '%Y-%m-%d %H:%M:%S',");
    }

    p_post_html_render.push("      columns: [");

    if(p_metadata.x_axis && p_metadata.x_axis != "")
    {

        update_g_charts(p_metadata.x_axis, `${chart_gen_name}`);
        p_post_html_render.push(get_chart_x_range_from_path(p_metadata, p_metadata.x_axis, p_ui));
    }


    if(p_metadata.y_label && p_metadata.y_label != "")
    {
        var y_labels = p_metadata.y_label.split(",");
        var y_axis_paths = p_metadata.y_axis.split(",");
        for(var y_index = 0; y_index < y_axis_paths.length; y_index++)
        {
            const y_axis_path = y_axis_paths[y_index];
            update_g_charts(y_axis_path, `${chart_gen_name}`);

            p_post_html_render.push(get_chart_y_range_from_path(p_metadata, y_axis_paths[y_index], p_ui, y_labels[y_index]));
            if(y_index < y_axis_paths.length-1)
            {
                p_post_html_render.push(",");
            }
        }
    }
    else
    {

        var y_axis_paths = p_metadata.y_axis.split(",");
        for(var y_index = 0; y_index < y_axis_paths.length; y_index++)
        {
            const y_axis_path = y_axis_paths[y_index];
            update_g_charts(y_axis_path, `${chart_gen_name}`);

            p_post_html_render.push(get_chart_y_range_from_path(p_metadata, y_axis_paths[y_index], p_ui));
            if(y_index < y_axis_paths.length-1)
            {
                p_post_html_render.push(",");
            }
        }
	}

    //var g_charts = {} map chart_name to c3.generate;
    //var g_chart_data = {} map chart_name to metadata;





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




    p_post_html_render.push("  ]");
    p_post_html_render.push("  },");
	p_post_html_render.push("  line: {");
	p_post_html_render.push("     connectNull: true");
	p_post_html_render.push("  }");
    p_post_html_render.push("  });");

    p_post_html_render.push(" d3.select('#" + convert_object_path_to_jquery_id(p_object_path) + " svg').append('text')");
    p_post_html_render.push("     .attr('x', d3.select('#" + convert_object_path_to_jquery_id(p_object_path) + " svg').node().getBoundingClientRect().width / 2)");
    p_post_html_render.push("     .attr('y', 16)");
    p_post_html_render.push("     .attr('text-anchor', 'middle')");
    p_post_html_render.push("     .style('font-size', '1.4em')");
	p_post_html_render.push("     .text('" + p_metadata.prompt.replace(/'/g, "\\'") + "');");
	
}


function update_g_charts(p_path, p_chart_name)
{
    if( !g_charts.has(p_path))
    {
        g_charts.set(p_path, new Set());
    }

    g_charts.get(p_path).add(p_chart_name)
}


function get_chart_x_ticks_from_path(p_metadata, p_metadata_path, p_ui)
{
	//prenatal/routine_monitoring/systolic_bp,prenatal/routine_monitoring/diastolic
	// p_ui.url_state.path_array.length
	const result = [];
	const array_field = eval(convert_dictionary_path_to_array_field(p_metadata_path));

	const array = eval(array_field[0]);

	if(array)
	{
		const field = array_field[1];

		result.push("[");
		//result.push("['x'");
		// ['data2', 50, 20, 10, 40, 15, 25]
		//result.push(50, 20, 10, 40, 15, 25);

		//result = ['data2', 50, 20, 10, 40, 15, 25];
		for(let i = 0; i < array.length; i++)
		{
			const val = array[i][field];
			if(val)
			{
				result.push(parseFloat(val));
			}
			else
			{
				result.push(0);
			}
			
		}

		result[result.length-1] = result[result.length-1] + "]";
		return result.join(",");
	}
	else
	{
		return "";
	}
	
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
						result.push(parseFloat(val));
					}
				}
			}
			else
			{
				//result.push(0);
			}
			
		}

		result[result.length-1] = result[result.length-1] + "]";
		return result.join(",") + ",";
	}
	else
	{
		return "";
	}
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

		result[result.length-1] = result[result.length-1] + "]";
		return result.join(",");
	}
	else
	{
		return result.join("") + "]";;
	}
}