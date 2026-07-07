const chart_function_params_map = new Map();
const chart_start_increment_map = new Map();
const g_pending_chart_update_paths = new Set();
let g_chart_update_flush_timer = null;
const chart_update_debounce_ms = 25;
const max_chart_tick_count = 100;

chart_start_increment_map.set("blood_pressure_graph", { start: 40, increment: 20});
//chart_start_increment_map.set("prm_diast", { start: 40, increment: 20});
chart_start_increment_map.set("weight_gain_graph", { start: 100, increment: 20});
chart_start_increment_map.set("hematocrit_graph", { start: 10, increment: 2});
chart_start_increment_map.set("temperature_graph", { start: 90, increment: 2});
chart_start_increment_map.set("pulse_graph", { start: 0, increment: 10});
chart_start_increment_map.set("respiration_graph", { start: 0, increment: 2});
//chart_start_increment_map.set("evahmrvs_b_systo", { start: 40, increment: 20});
//chart_start_increment_map.set("evahmrvs_b_dias", { start: 40, increment: 20});

function get_finite_chart_numbers(p_values)
{
    return p_values
        .map(function (number)
        {
            return Number.parseFloat(number);
        })
        .filter(function (number)
        {
            return Number.isFinite(number);
        })
        .sort(function (left, right)
        {
            return left - right;
        });
}

function get_safe_chart_axis_config(p_minimum_graph_value, p_maximum_graph_value, p_increment_graph_value)
{
    let increment = Number.isFinite(p_increment_graph_value) && p_increment_graph_value > 0
        ? p_increment_graph_value
        : 1;

    let minimum = Number.isFinite(p_minimum_graph_value)
        ? p_minimum_graph_value
        : 0;

    let maximum = Number.isFinite(p_maximum_graph_value) && p_maximum_graph_value > minimum
        ? p_maximum_graph_value
        : minimum + increment;

    const graph_range = maximum - minimum;
    const estimated_tick_count = Math.ceil(graph_range / increment);

    if(estimated_tick_count > max_chart_tick_count)
    {
        const scaled_increment = Math.ceil(graph_range / max_chart_tick_count);
        increment = increment > 1
            ? Math.ceil(scaled_increment / increment) * increment
            : scaled_increment;
    }

    minimum = Math.floor(minimum / increment) * increment;
    maximum = Math.ceil(maximum / increment) * increment;

    if(maximum <= minimum)
    {
        maximum = minimum + increment;
    }

    const values = [];
    for(let value = minimum; value < maximum && values.length < max_chart_tick_count; value += increment)
    {
        values.push(value);
    }

    if(values.length === 0)
    {
        values.push(minimum);
    }

    return {
        values: values,
        minimum: minimum,
        maximum: maximum,
        increment: increment
    };
}

function get_chart_instance_id(p_ui_div_id)
{
    return `chart_${p_ui_div_id}`;
}

function destroy_chart_instance(p_chart_id)
{
    if(!p_chart_id)
    {
        return;
    }

    if
    (
        typeof g_chart_instances !== 'undefined' &&
        g_chart_instances != null &&
        typeof g_chart_instances.get === 'function'
    )
    {
        const chart_instance = g_chart_instances.get(p_chart_id);
        if(chart_instance && typeof chart_instance.destroy === 'function')
        {
            try
            {
                chart_instance.destroy();
            }
            catch(_ex)
            {
                // Best-effort cleanup only.
            }
        }

        g_chart_instances.delete(p_chart_id);
    }

    if
    (
        typeof g_charts !== 'undefined' &&
        g_charts != null &&
        Object.prototype.hasOwnProperty.call(g_charts, p_chart_id)
    )
    {
        const legacy_chart_instance = g_charts[p_chart_id];
        if(legacy_chart_instance && typeof legacy_chart_instance.destroy === 'function')
        {
            try
            {
                legacy_chart_instance.destroy();
            }
            catch(_ex)
            {
                // Best-effort cleanup only.
            }
        }

        delete g_charts[p_chart_id];
    }
}

function destroy_all_chart_instances()
{
    if
    (
        typeof g_chart_instances !== 'undefined' &&
        g_chart_instances != null &&
        typeof g_chart_instances.entries === 'function'
    )
    {
        for(const [chart_id, chart_instance] of g_chart_instances.entries())
        {
            if(chart_instance && typeof chart_instance.destroy === 'function')
            {
                try
                {
                    chart_instance.destroy();
                }
                catch(_ex)
                {
                    // Best-effort cleanup only.
                }
            }
        }

        g_chart_instances.clear();
    }

    // Clean up any legacy chart instances that were stored as object properties on the Map.
    if(typeof g_charts !== 'undefined' && g_charts != null)
    {
        for(const key in g_charts)
        {
            if
            (
                Object.prototype.hasOwnProperty.call(g_charts, key) &&
                key.indexOf('chart_') === 0
            )
            {
                const legacy_chart_instance = g_charts[key];
                if
                (
                    legacy_chart_instance &&
                    typeof legacy_chart_instance.destroy === 'function'
                )
                {
                    try
                    {
                        legacy_chart_instance.destroy();
                    }
                    catch(_ex)
                    {
                        // Best-effort cleanup only.
                    }
                }

                delete g_charts[key];
            }
        }
    }
}

function clear_chart_state()
{
    destroy_all_chart_instances();
    if(g_chart_update_flush_timer != null)
    {
        window.clearTimeout(g_chart_update_flush_timer);
        g_chart_update_flush_timer = null;
    }

    g_pending_chart_update_paths.clear();
    chart_function_params_map.clear();
    g_charts.clear();
    g_chart_data.clear();
}


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

function mmria_vitals_is_out_of_range(fieldPath, value) {
    if (!window.mmria_validation_rules) return false;
    var rule = window.mmria_validation_rules[fieldPath];
    if (!rule) {
        // Try to find a rule by searching for a matching field path ending
        for (var key in window.mmria_validation_rules) {
            if (key.endsWith('/' + fieldPath) || key === fieldPath) {
                rule = window.mmria_validation_rules[key];
                break;
            }
        }
    }
    if (!rule) return false;
    var v = parseFloat(value);
    if (value === '' || value == null || isNaN(v)) return false;
    return (v < parseFloat(rule.min_value) || v > parseFloat(rule.max_value));
}

function mmria_vitals_validate_field(inputEl)
{
    if (!window.mmria_validation_rules) { return; }
    var fieldName = inputEl.name;
    var chartFormPath = (inputEl.dataset && inputEl.dataset.chartFormPath) ? inputEl.dataset.chartFormPath : null;
    var rule = null;

    if (chartFormPath) {
        // Form-scoped lookup: match only rules whose field_path prefix equals chartFormPath.
        // Prevents an enabled rule on Form A from firing on Form B's same-named field (Bug #4).
        for (var key in window.mmria_validation_rules) {
            var kParts = key.split('/');
            if (kParts[kParts.length - 1] !== fieldName) { continue; }
            var kPrefix = kParts.slice(0, -1).join('/');
            if (kPrefix === chartFormPath) { rule = window.mmria_validation_rules[key]; break; }
        }
    } else {
        // No form context (e.g. called from validation-state.js focusout on a non-chart input).
        // Non-chart saves are separately guarded by the normalized-path lookup in index.js Block 2.
        if (window.mmria_validation_rules[fieldName]) {
            rule = window.mmria_validation_rules[fieldName];
        } else {
            for (var key2 in window.mmria_validation_rules) {
                if (key2.endsWith('/' + fieldName) || key2 === fieldName) {
                    rule = window.mmria_validation_rules[key2];
                    break;
                }
            }
        }
    }

    if (!rule) { return; }
    if (inputEl.value === '' || inputEl.value === null) { return; }
    // Skip the modal if the value has not changed from the originally rendered value
    // (i.e. this is historical invalid data — the user just tabbed through without editing).
    if (inputEl.value === inputEl.defaultValue) { return; }
    var value = parseFloat(inputEl.value);
    if (isNaN(value)) { return; }
    if (value < parseFloat(rule.min_value) || value > parseFloat(rule.max_value))
    {
        mmria_vitals_show_field_modal(rule, inputEl);
    }
}

function mmria_vitals_show_field_modal(range, inputEl)
{
    var existingModal = document.getElementById('vitals-range-modal');
    if (existingModal && existingModal.parentNode) { existingModal.parentNode.removeChild(existingModal); }
    var existingBackdrop = document.getElementById('vitals-range-backdrop');
    if (existingBackdrop && existingBackdrop.parentNode) { existingBackdrop.parentNode.removeChild(existingBackdrop); }

    var modalHtml =
        '<div id="vitals-range-modal" class="modal fade" tabindex="-1" role="dialog" aria-modal="true" aria-labelledby="vitals-range-modal-title" style="z-index: 1050;">'
        + '<div class="modal-dialog" role="document">'
        + '<div class="modal-content">'
        + '<div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">'
        + '<h4 id="vitals-range-modal-title" class="modal-title" style="margin: 0; font-weight: 600; font-size: 17px;">Out of Range</h4>'
        + '</div>'
        + '<div class="modal-body" style="padding: 20px;">'
        + '<p id="vitals-range-modal-msg" style="font-size: 16px; color: #333; margin: 0;"></p>'
        + '</div>'
        + '<div class="modal-footer" style="padding: 15px 20px; text-align: right;">'
        + '<button type="button" id="vitals-range-modal-ok" class="btn btn-primary" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">OK</button>'
        + '</div>'
        + '</div>'
        + '</div>'
        + '</div>'
        + '<div id="vitals-range-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>';

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    var msgEl = document.getElementById('vitals-range-modal-msg');
    if (msgEl)
    {
        msgEl.textContent = range.message
            || ('The value entered falls outside of the permitted range.'
                + ' Please enter a valid input between ' + range.min_value + '\u2013' + range.max_value + '.');
    }

    var modal = document.getElementById('vitals-range-modal');
    var backdrop = document.getElementById('vitals-range-backdrop');

    setTimeout(function()
    {
        if (modal) { modal.classList.add('show'); modal.style.display = 'block'; }
        if (backdrop) { backdrop.classList.add('show'); }
        var okBtn = document.getElementById('vitals-range-modal-ok');
        if (okBtn) { okBtn.focus(); }
    }, 10);

    function closeVitalsModal()
    {
        if (modal) { modal.classList.remove('show'); }
        if (backdrop) { backdrop.classList.remove('show'); }
        setTimeout(function()
        {
            if (modal && modal.parentNode) { modal.parentNode.removeChild(modal); }
            if (backdrop && backdrop.parentNode) { backdrop.parentNode.removeChild(backdrop); }
        }, 150);
        if (inputEl && typeof inputEl.focus === 'function') { inputEl.focus(); }
    }

    var okBtn = document.getElementById('vitals-range-modal-ok');
    if (okBtn) { okBtn.onclick = closeVitalsModal; }

    if (modal)
    {
        modal.addEventListener('keydown', function(e)
        {
            if (e.key === 'Escape') { e.preventDefault(); closeVitalsModal(); }
        });
    }
}



function mmria_vitals_revalidate_all()
{
    if (!window.mmria_validation_rules) { return false; }
    var records = window.g_data && window.g_data.er_visit_and_hospital_medical_records;
    if (!records || !Array.isArray(records)) { return false; }
    var rule_keys = Object.keys(window.mmria_validation_rules);
    for (var i = 0; i < records.length; i++)
    {
        var vital_signs = records[i] && records[i].vital_signs;
        if (!vital_signs || !Array.isArray(vital_signs)) { continue; }
        for (var j = 0; j < vital_signs.length; j++)
        {
            var measurement = vital_signs[j];
            for (var k = 0; k < rule_keys.length; k++)
            {
                // Extract field name from the full path (e.g., "temperature" from "er_visit_and_hospital_medical_records/vital_signs/temperature")
                var fieldName = rule_keys[k].split('/').pop();
                if (mmria_vitals_is_out_of_range(rule_keys[k], measurement[fieldName]))
                {
                    return true;
                }
            }
        }
    }
    return false;
}

function mmria_vitals_case_is_closed()
{
    if (!window.g_data) { return false; }
    var hr = g_data.home_record;
    if (!hr || !hr.case_status || !hr.case_status.overall_case_status) { return false; }
    var status = Number(hr.case_status.overall_case_status);
    return status === 4 || status === 5 || status === 6;
}

function mmria_vitals_has_hard_violations()
{
    if (!window.g_data || !window.mmria_validation_rules) { return false; }
    var records = window.g_data.er_visit_and_hospital_medical_records;
    if (!records || !Array.isArray(records)) { return false; }
    var rule_keys = Object.keys(window.mmria_validation_rules);
    for (var i = 0; i < records.length; i++)
    {
        var vital_signs = records[i] && records[i].vital_signs;
        if (!vital_signs || !Array.isArray(vital_signs)) { continue; }
        for (var j = 0; j < vital_signs.length; j++)
        {
            var measurement = vital_signs[j];
            for (var k = 0; k < rule_keys.length; k++)
            {
                var rule = window.mmria_validation_rules[rule_keys[k]];
                if (!rule || rule.severity !== 'hard') { continue; }
                var fieldName = rule_keys[k].split('/').pop();
                if (mmria_vitals_is_out_of_range(rule_keys[k], measurement[fieldName]))
                {
                    return true;
                }
            }
        }
    }
    return false;
}

function mmria_vitals_show_print_gate_modal(actionLabel, isHardBlock, onConfirm)
{
    var existingModal = document.getElementById('vitals-print-gate-modal');
    if (existingModal && existingModal.parentNode) { existingModal.parentNode.removeChild(existingModal); }
    var existingBackdrop = document.getElementById('vitals-print-gate-backdrop');
    if (existingBackdrop && existingBackdrop.parentNode) { existingBackdrop.parentNode.removeChild(existingBackdrop); }

    var message = isHardBlock
        ? 'This case contains vital sign records with values outside the permitted range. These values must be corrected before printing or viewing.'
        : 'This case contains vital sign records with values outside the permitted range. These values are excluded from graphs, tables, print and pdf views.';

    var proceedButtonHtml = isHardBlock
        ? ''
        : '<button type="button" id="vitals-print-gate-modal-proceed" class="btn btn-primary" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">' + actionLabel + '</button>';

    var modalHtml =
        '<div id="vitals-print-gate-modal" class="modal fade" tabindex="-1" role="dialog" aria-modal="true" aria-labelledby="vitals-print-gate-modal-title" style="z-index: 1050;">'
        + '<div class="modal-dialog" role="document">'
        + '<div class="modal-content">'
        + '<div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">'
        + '<h4 id="vitals-print-gate-modal-title" class="modal-title" style="margin: 0; font-weight: 600; font-size: 17px;">Vital Signs Out of Range</h4>'
        + '</div>'
        + '<div class="modal-body" style="padding: 20px;">'
        + '<p style="font-size: 16px; color: #333; margin: 0;">' + message + '</p>'
        + '</div>'
        + '<div class="modal-footer" style="padding: 15px 20px; text-align: right;">'
        + '<button type="button" id="vitals-print-gate-modal-close" class="btn btn-default" style="padding: 8px 20px; margin-right: 8px;">Close</button>'
        + proceedButtonHtml
        + '</div>'
        + '</div>'
        + '</div>'
        + '</div>'
        + '<div id="vitals-print-gate-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>';

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    var modal = document.getElementById('vitals-print-gate-modal');
    var backdrop = document.getElementById('vitals-print-gate-backdrop');

    setTimeout(function()
    {
        if (modal) { modal.classList.add('show'); modal.style.display = 'block'; }
        if (backdrop) { backdrop.classList.add('show'); }
        var closeBtn = document.getElementById('vitals-print-gate-modal-close');
        if (closeBtn) { closeBtn.focus(); }
    }, 10);

    function closeGateModal()
    {
        if (modal) { modal.classList.remove('show'); }
        if (backdrop) { backdrop.classList.remove('show'); }
        setTimeout(function()
        {
            if (modal && modal.parentNode) { modal.parentNode.removeChild(modal); }
            if (backdrop && backdrop.parentNode) { backdrop.parentNode.removeChild(backdrop); }
        }, 150);
    }

    var closeBtn = document.getElementById('vitals-print-gate-modal-close');
    if (closeBtn) { closeBtn.onclick = closeGateModal; }

    var proceedBtn = document.getElementById('vitals-print-gate-modal-proceed');
    if (proceedBtn)
    {
        proceedBtn.onclick = function()
        {
            closeGateModal();
            if (typeof onConfirm === 'function') { onConfirm(); }
        };
    }

    if (modal)
    {
        modal.addEventListener('keydown', function(e)
        {
            if (e.key === 'Escape') { e.preventDefault(); closeGateModal(); }
        });
    }
}

function chart_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx, p_is_de_identified = false)
{
	let style_object = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];

  const function_params = {
      p_metadata: p_metadata, 
      p_data: p_data, 
      p_ui: p_ui, 
      p_metadata_path: p_metadata_path, 
      p_object_path: p_object_path, 
      p_dictionary_path: p_dictionary_path, 
      p_is_grid_context: p_is_grid_context, 
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

	p_post_html_render.push(` g_chart_instances.set('${chart_gen_name}', c3.generate({
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
        window.requestAnimationFrame(function ()
        {
            const el = d3.select('#${map_key} svg').selectAll('g.c3-axis.c3-axis-x > g.tick > text');
            if(!el.empty())
            {
                el.attr('transform', 'rotate(325)translate(${translate_x},0)');
            }
        });

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
        let maximum_graph_value = 450;
        let has_nonzero_value = false;
        
        if
        (
            chart_start_increment_map.has(p_metadata.name)
        )
        {
            const key_value = chart_start_increment_map.get(p_metadata.name);

            minimum_graph_value = key_value.start;
            increment_graph_value = key_value.increment;

             var y_axis_paths = p_metadata.y_axis.split(",");
             const y_values = get_chart_y_values_from_path(p_metadata, y_axis_paths[0]);
             const y_values2 = y_axis_paths && y_axis_paths.length > 1 ? get_chart_y_values_from_path(p_metadata, (y_axis_paths[1]).trim()) : [];

             const arr1 = get_finite_chart_numbers(y_values);
             const arr2 = get_finite_chart_numbers(y_values2);
             const arrayValues = arr1.concat(arr2);

             if (arrayValues.length > 0) {
                 const minValue = Math.min(...arrayValues);
                 const maxValue = Math.max(...arrayValues);
                 has_nonzero_value = arrayValues.some(val => val !== 0);
                 if (minValue < minimum_graph_value) {
                     value_below_floor = true;
                     minimum_graph_value = Math.floor(minValue / increment_graph_value) * increment_graph_value;
                 }
                 if (maxValue && has_nonzero_value) {
                     // Round up to the next increment boundary and add two increments
                     // (one for spacing, one because d3.range stops before the end value)
                     maximum_graph_value = Math.ceil(maxValue / increment_graph_value) * increment_graph_value + (increment_graph_value * 2);
                 }
             }

        }

        let format_text_size = ".0f";
        if (p_metadata.name === "temperature_graph") 
        {
            format_text_size = ".1f"
        }

        const axis_config = get_safe_chart_axis_config
        (
            minimum_graph_value,
            maximum_graph_value,
            increment_graph_value
        );
        
        let y_axis_config = `
            ,y: {
                
                tick: {
                        values: ${JSON.stringify(axis_config.values)},
                        format: d3.format('${format_text_size}'),
                        },
                min: ${axis_config.minimum},`;
        
        if (has_nonzero_value) {
            y_axis_config += `
                max: ${axis_config.maximum - axis_config.increment},`;
        }
        
        y_axis_config += `
                padding: {top: 0, bottom: 0},
            },
        `;
        
        p_post_html_render.push(y_axis_config);

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
    
    p_post_html_render.push("  ]");
    p_post_html_render.push("  },");
	p_post_html_render.push("  line: {");
	p_post_html_render.push("     connectNull: true");
	p_post_html_render.push("  }");
    p_post_html_render.push("  }));");

    p_post_html_render.push(
        "(function() {" +
        " var chartEl = document.getElementById('" + map_key + "');" +
        " if (!chartEl) { return; }" +
        " var parent = chartEl.parentElement;" +
        " if (!parent) { return; }" +
        // Compute the form path prefix for rule scoping: strip array indices so
        // 'er_visit_and_hospital_medical_records/0/vital_signs' becomes
        // 'er_visit_and_hospital_medical_records/vital_signs', matching the
        // field_path prefix used in mmria_validation_rules keys (Bug #4 fix).
        " var chartFormPath = '" + p_object_path.replace(/\/\d+/g, '') + "';" +
        " var inputs = parent.querySelectorAll('input.number');" +
        " for (var i = 0; i < inputs.length; i++) {" +
        "  var inp = inputs[i];" +
        "  if (inp.dataset.vitalsValidationAttached) { continue; }" +
        "  inp.dataset.vitalsValidationAttached = '1';" +
        "  inp.dataset.chartFormPath = chartFormPath;" +
        "  inp.addEventListener('blur', function(e) { mmria_vitals_validate_field(e.target); });" +
        "  inp.addEventListener('keydown', function(e) { if (e.key === 'Tab') { mmria_vitals_validate_field(e.target); } });" +
        "  inp.addEventListener('paste', (function(t) { return function() { setTimeout(function() { mmria_vitals_validate_field(t); }, 0); }; })(inp));" +
        " }" +
        "})();"
    );

	g_chart_data.set
    (
        `${chart_gen_name}`, 
        {
            div_id: map_key,
            p_metadata: p_metadata,
            p_ui: p_ui,
            p_metadata_path: p_metadata_path,
            p_object_path: p_object_path,
            p_dictionary_path: p_dictionary_path,
            p_is_grid_context: p_is_grid_context,
            p_search_ctx: p_search_ctx,
            p_ctx: p_ctx,
            last_render_signature: get_chart_render_signature(p_result, p_post_html_render)
    
        }
    );
	
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
                if (mmria_vitals_is_out_of_range(field, val))
                {
                    result.push('null');
                }
                else
                {
                    const parsed_value = Number.parseFloat(val);
                    result.push(Number.isFinite(parsed_value) ? parsed_value.toFixed(2) : 'null');
                }
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

function get_chart_y_values_from_path(p_metadata, p_metadata_path, p_multiform_index)
{
	
	const result = [];
	const array_field = eval(convert_dictionary_path_to_array_field(p_metadata_path, p_multiform_index));

	const array = eval(array_field[0]);

	const field = array_field[1];

	if(array)
	{
		
		for(let i = 0; i < array.length; i++)
		{
			const val = array[i][field];
			if(val)
			{
                if (mmria_vitals_is_out_of_range(field, val))
                {
                    // skip out-of-range values from axis range calculation
                }
                else
                {
                    const parsed_value = Number.parseFloat(val);
                    if(Number.isFinite(parsed_value))
                    {
                        result.push(parsed_value.toFixed(2));
                    }
                }
			}		
		}

	}	

    return result;
}

function get_chart_render_signature(p_result, p_post_html_render)
{
    return `${p_result.join('')}||${p_post_html_render.join('')}`;
}

function schedule_chart_update_flush()
{
    if(g_chart_update_flush_timer != null)
    {
        return;
    }

    g_chart_update_flush_timer = window.setTimeout(function ()
    {
        g_chart_update_flush_timer = null;
        flush_pending_chart_updates();
    }, chart_update_debounce_ms);
}

function flush_pending_chart_updates()
{
    if(g_pending_chart_update_paths.size === 0)
    {
        return;
    }

    const pending_paths = Array.from(g_pending_chart_update_paths);
    g_pending_chart_update_paths.clear();

    const chart_ids_to_update = new Set();

    for(const pending_path of pending_paths)
    {
        if
        (
            pending_path == null ||
            pending_path === ''
        )
        {
            continue;
        }

        const normalized_path = pending_path.startsWith('/') ? pending_path.substring(1) : pending_path;
        if(!g_charts.has(normalized_path))
        {
            continue;
        }

        const chart_set = g_charts.get(normalized_path);
        if(!chart_set)
        {
            continue;
        }

        for(const chart_id of chart_set)
        {
            chart_ids_to_update.add(chart_id);
        }
    }

    for(const chart_id of chart_ids_to_update)
    {
        rerender_chart(chart_id);
    }
}

function rerender_chart(p_chart_id)
{
    if(!p_chart_id)
    {
        return;
    }

    const existing_chart_data = g_chart_data.get(p_chart_id);
    if(!existing_chart_data)
    {
        return;
    }

    const p_result = [];
    const p_post_html_render = [];

    chart_render
    (
        p_result, 
        existing_chart_data.p_metadata, 
        null, // undefined
        existing_chart_data.p_ui, // g_ui
        existing_chart_data.p_metadata_path, //"g_metadata.children[17].children[12]"
        existing_chart_data.p_object_path, // "g_data.er_visit_and_hospital_medical_records[0].temperature_graph"
        existing_chart_data.p_dictionary_path, // "/er_visit_and_hospital_medical_records/temperature_graph"
        existing_chart_data.p_is_grid_context, // false
        p_post_html_render, 
        existing_chart_data.p_search_ctx, // undefined
        existing_chart_data.p_ctx // { form_index: 0, grid_index: null }
    );

    const next_signature = get_chart_render_signature(p_result, p_post_html_render);
    const next_chart_data = g_chart_data.get(p_chart_id);
    const previous_signature = existing_chart_data.last_render_signature || null;

    if(next_chart_data)
    {
        next_chart_data.last_render_signature = next_signature;
    }

    if
    (
        previous_signature != null &&
        previous_signature === next_signature
    )
    {
        return;
    }

    destroy_chart_instance(p_chart_id);

    const chart_element = document.getElementById(existing_chart_data.div_id);
    if(!chart_element)
    {
        return;
    }

    chart_element.outerHTML = p_result.join('');

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

    g_pending_chart_update_paths.add(p_path);
    schedule_chart_update_flush();
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
    if(!el)
    {
        return;
    }

    destroy_chart_instance(get_chart_instance_id(p_ui_div_id));

    const params = chart_function_params_map.get(p_ui_div_id);
    if(!params)
    {
        return;
    }

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
    const fieldName = col.replace(graph_prefix, "");
    const rawVal = row[fieldName];
    data_table_body_html.push(`<td style="padding-left: 5px;">${mmria_vitals_is_out_of_range(fieldName, rawVal) ? '<span style="color:#b87a00;font-style:italic;font-size:small;">Out of range</span>' : rawVal}</td>`)
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
    if(!params)
    {
        return;
    }

    const el = document.getElementById(p_ui_div_id);
    if(!el)
    {
        return;
    }

    destroy_chart_instance(get_chart_instance_id(p_ui_div_id));

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
