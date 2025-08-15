let g_is_initial_search = true;
let last_form = null;
let is_field_list_expanded = false;

$(document).on('click', function(e) {
    var checkboxes = document.getElementById("checkboxes");
    var field_filter_control = document.getElementById('field_filter');
    if ((!$(e.target).closest('.overSelect').length && !$(e.target).closest('.filter-field-checkbox').length && !$(e.target).closest('.filter-field-checkbox-label').length) && is_field_list_expanded)
    {
        checkboxes.style.display = "none";
        is_field_list_expanded = false;
        field_filter_control.setAttribute('aria-expanded', 'false');
    }
    else if ($(e.target).closest('.overSelect').length)
    {
        checkboxes.style.display = "block";
        is_field_list_expanded = true;
        document.getElementById('ff-All').focus();
        field_filter_control.setAttribute('aria-expanded', 'true');
    }
});

function dictionary_render(p_metadata, p_path)
{
	var result = [];
	let search_result = [];

    if(!g_is_initial_search)
    {
        g_is_initial_search = false;
	    render_search_result(search_result);
    }

    

	result.push(`
		<div id="filter" class="sticky-header z-index-top mt-2" data-prop="selection_type" style="background: white;">
            <form id="view-data-filter-form" class="row no-gutters align-items-center" onsubmit="event.preventDefault()">
                ${g_is_pmss_enhanced? form_render_pmss(): form_render_mmria()}
            </form>
			<div class="mt-2 vertical-control pl-0 pr-0 col-md-12">
				<table id="search_result_list" class="table table-layout-fixed align-cell-top mb-3" style="font-size: 14px">
					${search_result.join("")}
				</table>
			</div>
			
	`);1
	return result;
}



function handle_print() 
{
	window.print();
}


function convert_dictionary_path_to_lookup_object(p_path)
{
	//g_data.prenatal.routine_monitoring.systolic_bp
	var result = null;
	var temp_result = []
	var temp = "g_metadata." + p_path.replace(new RegExp('/','gm'),".").replace(new RegExp('\\.(\\d+)\\.','gm'),"[$1].").replace(new RegExp('\\.(\\d+)$','g'),"[$1]");
	var index = temp.lastIndexOf('.');

	temp_result.push(temp.substr(0, index));
	temp_result.push(temp.substr(index + 1, temp.length - (index + 1)));

	var lookup_list = eval(temp_result[0]);

	for(var i = 0; i < lookup_list.length; i++)
	{
		if(lookup_list[i].name == temp_result[1])
		{
			result = lookup_list[i].values;
			break;
		}
	}

	return result;
}


function render_form_filter(p_filter)
{
	let result = [];

	result.push(`<option value="all">(Any Form)</option>`)

	for(let i = 0; i < g_metadata.children.length; i++)
	{
		let item = g_metadata.children[i];

		if(item.type.toLowerCase() == "form")
		{
			if(p_filter.selected_form == item.name)
			{
				result.push(`<option value="${item.name}" selected>${item.prompt}</option>`)
			}
			else
			{
				result.push(`<option value="${item.name}">${item.prompt}</option>`)
			}
		}
	}

	return result.join("");
}

function on_form_filter_changed(value)
{
    //g_filter.field_selection = new Set(['all']);
    //g_filter.field_selection.add(value);

    g_filter.selected_form = value;
    const html = render_field_filter(g_filter);
    const el = document.getElementById("checkboxes");

    el.innerHTML = html;

    show_needs_apply_id(true)
}

function on_field_filter_changed(event, value)
{
    if(event.key == "Escape" || event.key == "Tab")
    {
        event.preventDefault();
        show_needs_apply_id(false);
        showCheckboxes(event);
    }
    else if (event.key == 'Enter' || event.key == undefined)
    {
        if(value == "all")
        {
            if(g_filter.field_selection.has(value))
            {
                g_filter.field_selection.clear();
            }
            else
            {
                g_filter.field_selection.add(value);
            }
            const html = render_field_filter(g_filter);
            const el = document.getElementById("checkboxes");
            el.innerHTML = html;
        }
        else if(g_filter.field_selection.has(value))
        {
            g_filter.field_selection.delete(value);
            document.getElementById(value).checked = false;
            if(g_filter.field_selection.size == 0)
            {
                g_filter.field_selection.add("all");
    
                const html = render_field_filter(g_filter);
                const el = document.getElementById("checkboxes");
                el.innerHTML = html;
            }
        }
        else
        {
            g_filter.field_selection.add(value);
            document.getElementById(value).checked = true;
            if(g_filter.field_selection.has("all"))
            {
                g_filter.field_selection.delete("all");
    
                const html = render_field_filter(g_filter);
                const el = document.getElementById("checkboxes");
                el.innerHTML = html;
            }
        }
    
        show_needs_apply_id(true);
    }
    document.getElementById(value == 'all' ? 'ff-All' : value).focus();
    //event.preventDefault();
    event.stopPropagation();
}

function on_arrow_keys_filter_field(event, value)
{
    if (event.key == 'ArrowUp')
    {
        event.preventDefault();
        var filter_field_checkboxes = [...document.getElementsByClassName('filter-field-checkbox')]; 
        var index = filter_field_checkboxes.indexOf(document.getElementById(value == 'all' ? 'ff-All' : value));
        if(index == 0)
        {
            document.getElementById('ff-All').focus();
        }
        else
        {
            document.getElementsByClassName('filter-field-checkbox').item(index - 1).focus();
        }
    }
    else if (event.key == 'ArrowDown')
    {
        event.preventDefault();
        var filter_field_checkboxes = [...document.getElementsByClassName('filter-field-checkbox')]; 
        var index = filter_field_checkboxes.indexOf(document.getElementById(value == 'all' ? 'ff-All' : value));
        if(index >= filter_field_checkboxes.length)
        {
            document.getElementsByClassName('filter-field-checkbox').item(filter_field_checkboxes.length - 1).focus();
        }
        else
        {
            document.getElementsByClassName('filter-field-checkbox').item(index + 1).focus();
            
        }
    }
}

function render_field_filter(p_filter)
{
	let result = [];
    
/*
    <div id="checkboxes">
      <label for="one">
        <input type="checkbox" id="one" />First checkbox</label>
      <label for="two">
        <input type="checkbox" id="two" />Second checkbox</label>
      <label for="three">
        <input type="checkbox" id="three" />Third checkbox</label>
    </div>

    */

    let is_checked = "";
    if(p_filter.field_selection.has("all"))
    {
        is_checked = 'checked';
    }

    const style = `style="width:23px;height:23px;background-color:#712177;"`;

    //const style = ``;
	result.push(`<label class="d-flex align-items-center filter-field-checkbox-label"><input aria-controls="field_filter" class="m-2 filter-field-checkbox" type="checkbox" id="ff-All" value="all" title="All Fields" onkeyup="on_field_filter_changed(event, this.value)" onkeydown="on_arrow_keys_filter_field(event, this.value)"  onclick="on_field_filter_changed(event, this.value)" ${is_checked} /><span>All Fields</span></label>`)
    result.push('<hr />');

    for(const [k, v] of g_form_field_map)
    {
        if(p_filter.field_selection && p_filter.field_selection.has("all"))
        {
            is_checked = 'checked';
            for(const [k2, v2] of v)
            {
                result.push(`<label class="d-flex align-items-center filter-field-checkbox-label">`);
                result.push(`<input aria-controls="field_filter" class="m-2 filter-field-checkbox" type="checkbox" ${style} id="${v2.field_name}"  value="${v2.field_name}" title="${v2.title_prompt}" onkeydown="on_arrow_keys_filter_field(event, this.value)" onkeyup="on_field_filter_changed(event, this.value)" onclick="on_field_filter_changed(event, this.value)" ${is_checked} /><span>${v2.display_prompt}</span></label>`);
            }
        }
        else if(p_filter.selected_form == '' || p_filter.selected_form == 'all')
        {
            
            for(const [k2, v2] of v)
            {
                is_checked = '';
                if(g_filter.field_selection.has(v2.field_name))
                {
                    is_checked = "checked";
                }
                result.push(`<label class="d-flex align-items-center filter-field-checkbox-label">`);
                result.push(`<input aria-controls="field_filter"  class="m-2 filter-field-checkbox" type="checkbox" ${style} id="${v2.field_name}" value="${v2.field_name}" title="${v2.title_prompt}" onkeydown="on_arrow_keys_filter_field(event, this.value)" onkeyup="on_field_filter_changed(event, this.value)" onclick="on_field_filter_changed(event, this.value)" ${is_checked} /><span>${v2.display_prompt}</span></label>`);
            }
        }
        else if(k == p_filter.selected_form)
        {
            
            for(const [k2, v2] of v)
            {
                is_checked = '';
                if(g_filter.field_selection.has(v2.field_name))
                {
                    is_checked = "checked";
                }
                result.push(`<label class="d-flex align-items-center filter-field-checkbox-label">`);
                result.push(`<input aria-controls="field_filter"  class="m-2 filter-field-checkbox" type="checkbox" ${style} id="${v2.field_name}" value="${v2.field_name}" title="${v2.title_prompt}" onkeydown="on_arrow_keys_filter_field(event, this.value)" onkeyup="on_field_filter_changed(event, this.value)" onclick="on_field_filter_changed(event, this.value)" ${is_checked} /><span>${v2.display_prompt}</span></label>`);
            }
        }
    }

	return result.join("");
}


async function search_click()
{
    set_form_enable(false);
    $('.spinner-content').addClass('spinner-active');

    last_form = null;

    show_needs_apply_id(false);


  

    if(document.getElementById("form_filter").value != "")
    {
	    g_filter.selected_form = document.getElementById("form_filter").value;
    }


    const search_text_control = document.getElementById("search_text");
    const regex = /\w+-\d\d\d\d-\d\d\d\d/;
    if(regex.test(search_text_control.value))
    {
        g_filter.selected_record_id = search_text_control.value;
        g_filter.search_text = '';
    }
    else
    {
        g_filter.selected_record_id = null;
    }
    

	const result = [];
	render_search_result(result);

    const search_result_list = document.getElementById("search_result_list");
    if(result.length == 0)
    {
        search_result_list.innerHTML = `<tr><td align=center>No matching results found for these filter settings. Please adjust filter settings and select “Apply Filters” to search again.</td></tr>`
    }
    else
    {
	    search_result_list.innerHTML = result.join("");
    }

    window.setTimeout(build_report,0);
    //await build_report()


    //build_report();	

}

async function reset_click()
{
    $('.spinner-content').addClass('spinner-active');

    last_form = null;

    show_needs_apply_id(false);

    const search_text_control = document.getElementById("search_text");
    search_text_control.value = ""
    g_filter.search_text = "";

    if(document.getElementById("form_filter").value != "")
    {
	    g_filter.selected_form = document.getElementById("form_filter").value;
    }

    g_filter.selected_record_id = null;

	let search_result_list = document.getElementById("search_result_list");
	let result = [];
	
	render_search_result(result);

    if(result.length == 0)
    {
        search_result_list.innerHTML = `<tr><td align=center>No matching results found for these filter settings. Please adjust filter settings and select “Apply Filters” to search again.</td></tr>`
    }
    else
    {
	    search_result_list.innerHTML = result.join("");
    }


    window.setTimeout(build_report,0);
    //build_report()
}


function render_search_result(p_result)
{

    render_search_result_item
        (
            p_result, g_metadata, 
            "", 
            g_filter.selected_form, 
            g_filter.search_text.toLowerCase()
        );     
    

}




function render_search_result_item(p_result, p_metadata, p_path, p_selected_form, p_search_text)
{

    
    if(p_metadata.mirror_reference != undefined && p_metadata.mirror_reference != "")
    {
        return;
    }

    
	switch(p_metadata.type.toLowerCase())
	{
		case "form":

            if(!g_form_field_map.has(p_metadata.name))
            {
                g_form_field_map.set(p_metadata.name, new Map());
            }	

			if
            (
                p_selected_form== null || 
                p_selected_form=="" ||
                p_selected_form=="all" 
            )
			{   
                 //console.log("form bubba");
				for(let i = 0; i < p_metadata.children.length; i++)
				{
					let item = p_metadata.children[i];

					render_search_result_item(p_result, item, p_path + "/" + item.name, p_selected_form, p_search_text);
				}
			}
			else
			{
				if(p_metadata.name.toLowerCase() == p_selected_form.toLowerCase())
				{
					for(let i = 0; i < p_metadata.children.length; i++)
					{
						let item = p_metadata.children[i];

						render_search_result_item(p_result, item, p_path + "/" + item.name, p_selected_form, p_search_text);
					}
				}
			}
			break;

		case "app":
			let form_filter = "(any form)";
			let el = document.getElementById("form_filter");

			if(el && document.getElementById("form_filter").value != 'all')
			{
				form_filter = el.value;
			}

			for(let i = 0; i < p_metadata.children.length; i++)
			{
				let item = p_metadata.children[i];

				if(form_filter.toLowerCase() == "(any form)")
				{
					render_search_result_item(p_result, item, p_path + "/" + item.name, p_selected_form, p_search_text);
				}
				else if(item.type.toLowerCase() == "form")
				{
					render_search_result_item(p_result, item, p_path + "/" + item.name, p_selected_form, p_search_text);
				}
				
			}
			break;

		case "group":
                if(p_metadata.tags.includes("CALC_DATE"))
                {
                    g_path_to_value_map.set(p_path.substr(1), new Map());
                }

                for(let i = 0; i < p_metadata.children.length; i++)
                {
                    let item = p_metadata.children[i];
                    render_search_result_item(p_result, item, p_path + "/" + item.name, p_selected_form, p_search_text);
                }
            break;
		case "grid":
			for(let i = 0; i < p_metadata.children.length; i++)
			{
				let item = p_metadata.children[i];
				render_search_result_item(p_result, item, p_path + "/" + item.name, p_selected_form, p_search_text);
			}
			break;

        case "chart":
        case "label":
        case "button":
        case "always_enabled_button":
            break;

		default:
            
			let file_name = "";
			let field_name = "";
			let file_field_item = g_release_version_specification.path_to_csv_all[p_path];

			if(file_field_item)
			{
				file_name = file_field_item.file_name;
				field_name = file_field_item.field_name;
			}

            if(p_metadata.tags.length == 0)
            {
                break;
            } 
              
            if
            (
                !p_metadata.tags.includes("FREQ") &&
                !p_metadata.tags.includes("STAT_N") &&
                !p_metadata.tags.includes("STAT_D") &&
                !p_metadata.tags.includes("CALC_DATE")
            )
            {
                break;
            }

            g_path_to_value_map.set(p_path.substr(1), new Map());

            let form_data_name = "";
			let form_name = "(none)";
			let path_array = p_path.split('/');
			let description = "";
			let list_values = [];
			let data_type = p_metadata.type.toLowerCase();

			if
			(
				p_metadata.data_type != null &&
				p_metadata.data_type != ""
			)
			{
				data_type = p_metadata.data_type
			}

			if(path_array.length > 2)
			{
				form_data_name = path_array[1];
				form_name = convert_form_name(form_data_name);
			}

            if
            (
                g_form_field_map.has(form_data_name) &&
                !g_form_field_map.get(form_data_name).has(field_name)
            )
            {
                const max_length = 40;

                let display_prompt = `${p_metadata.prompt} [${field_name}]`;
                if(display_prompt.length> max_length)
                {
                    display_prompt = p_metadata.prompt.substring(0, max_length);
                }


                let title_prompt = `[${field_name}] ${p_metadata.prompt}`;
                g_form_field_map.get(form_data_name).set
                (
                    field_name, 
                    { 
                        field_name: field_name, 
                        data_name: p_metadata.name,
                        display_prompt: display_prompt,
                        title_prompt: title_prompt
                    }
                );
            }

            let is_single_field_filter = false;
            if
            (
                !g_filter.field_selection.has("all")
            )
            {
                if(g_filter.field_selection.size == 1)
                {
                    if(g_filter.field_selection.has(field_name))
                    {
                        is_single_field_filter = true;
                    }
                    else
                    {
                        return;
                    }
                    
                }

            }

			if(p_search_text != null && p_search_text !="")
			{
				let is_search_match = false;
				let search_array = p_search_text.trim().split(" ");
				
				for(let i = 0; i < search_array.length; i++)
				{
					let search_term = search_array[i].toLowerCase().trim();
					
					if
					(
						search_term == null || 
						search_term == ""
					)
					{
						continue;
                    }
                    /*
                    if
                    (
                        p_metadata.name.toLowerCase().indexOf(search_term.trim()) > -1 ||
						p_metadata.prompt.toLowerCase().indexOf(search_term.trim()) > -1
                    )
					{
						is_search_match = true;
						break;
					}*/

					for(let j = 0; j < p_metadata.tags.length; j++)
					{
						let check_item = p_metadata.tags[j].toLowerCase();

                        if
                        (
                            check_item.indexOf(search_term) > -1

                        )
						{
							is_search_match = true;
							break;
						}
					}	
				}
				
				if
				(
					!is_search_match && 
					!(
						p_metadata.name.toLowerCase().indexOf(p_search_text.trim()) > -1 ||
						p_metadata.prompt.toLowerCase().indexOf(p_search_text.trim()) > -1 ||
                        (p_metadata.sass_export_name!= null && p_metadata.sass_export_name.toLowerCase().indexOf(p_search_text.trim()) > -1) ||
                        (file_name!=null && file_name.indexOf(p_search_text.trim()) > -1) ||
						(field_name!=null && field_name.indexOf(p_search_text.trim()) > -1)
					)
				
				)
				{
					return;
				}
			}





			if(p_metadata.data_summary_report_description != null)
			{
				description = p_metadata.data_summary_report_description;
			}

            const stat_type = p_metadata.tags.filter(filter_tag).join("").toUpperCase();
            const context = {
                dictionary_path: p_path.substr(1)
            };

            switch(stat_type)
            {
                case "FREQ":
                    //console.log('FREQ');
                    if(!g_path_to_stat_type.has(p_path.substr(1))) g_path_to_stat_type.set(p_path.substr(1), "FREQ");
                    list_values.push(render_FREQ(context, p_metadata.prompt));
                    break;
                case "STAT_D":
                   //console.log('STAT_D');
                   if(!g_path_to_stat_type.has(p_path.substr(1))) g_path_to_stat_type.set(p_path.substr(1), "STAT_D");
                    list_values.push(render_STAT_D(context, p_metadata.prompt));
                    break;
                case "STAT_N":
                    //console.log('STAT_N');
                    if(!g_path_to_stat_type.has(p_path.substr(1))) g_path_to_stat_type.set(p_path.substr(1), "STAT_N");
                    list_values.push(render_STAT_N(context, p_metadata.prompt));
                break;
            }


            

			if(p_metadata.type.toLowerCase() == "list")
			{
				let value_list = p_metadata.values;

				if(p_metadata.path_reference && p_metadata.path_reference != "")
				{
					value_list = eval(convert_dictionary_path_to_lookup_object(p_metadata.path_reference));
			
					if(value_list == null)	
					{
						value_list = p_metadata.values;
					}
				}

                

				list_values.push(`
					<tr class="tr">
						<td class="td" width="140"></td>
						<td class="td p-0" colspan="5">
							<table class="table table-fixed-layout align-cell-top m-0">
                                <caption class="table-caption">
                                    Table with all possible list values for the ${form_name}'s ${p_metadata.prompt} field type.
                                </caption>
								<thead class="thead">
									<tr class="header-level-top-black">
										<th class="th" colspan="5" width="1080" scope="colgroup">List Values</th>
									</tr>
								</thead>
								<thead class="thead">
									<tr class="header-level-2
				`);
                
                if(value_list.length > 15)
                    {
                        list_values.push(` sticky z-index-middle" style="top: 355px;">
                                            <th class="th" width="140" scope="col">Value</th>
                                            <th class="th" width="680" scope="col">Display - ${p_metadata.sass_export_name}</th>
                                            <th class="th" width="260" scope="col">N (Counts)</th>
                                        </tr>
                                    </thead>
                                    <tbody class="tbody">`);
                    }
                    else
                    {
                        list_values.push(`">
                                <th class="th" width="140" scope="col">Value</th>
                                <th class="th" width="680" scope="col">Display - ${p_metadata.sass_export_name}</th>
                                <th class="th" width="260" scope="col">N (Counts)</th>
                            </tr>
                            </thead>
                            <tbody class="tbody">
                        `);
                    }

                    list_values.push
                    (
                        `<tr class="tr"  id="tr-${p_path.substr(1)}-missing-detail">
										<td class="td" width="140">(-)</td>
										<td class="td" width="680">missing</td>
										<td class="td" width="260" id="${p_path.substr(1)}-missing-detail"  align=right>0</td>
						</tr>`
                    );

                   
                    const is_written = new Set();

					for(let i= 0; i < value_list.length; i++)
					{
                        const value_list_value = value_list[i].value.trim().toLowerCase();

                        const id_key = `-${p_path.substr(1)}-${value_list_value}`;
                        is_written.add(id_key);

						list_values.push(`
									<tr class="tr"  id="tr-${p_path.substr(1)}-${value_list_value}">
										<td class="td" width="140">${value_list[i].value}</td>
										<td class="td" width="680">${value_list[i].display}</td>
										<td class="td" width="260" id="${p_path.substr(1)}-${value_list_value}"  align=right>0</td>
									</tr>
						`);
					}


                    const current_path = p_path.substr(1);
                    if(g_report_map.has(current_path))
                    {
                        const vi_map = g_report_map.get(current_path);

                        let keys = Array.from( vi_map.keys() ).sort();
                        for(const ki of keys)
                        {
                            const vi = vi_map.get(ki);

                            const id_key = `-${p_path.substr(1)}-${ki}`;

                            if(is_written.has(id_key)) continue;

                            if(ki == "(-)")
                            {

                                const html_link = render_link
                                (
                                    current_path,
                                    "missing",
                                    vi,
                                    "Missing List Values"
                                );

                                list_values.push(`
                                    <tr class="tr"  id="tr-${current_path}-${ki.replace(/"/g,"&quot;")}">
                                        <td class="td" width="820" colspan=2>${ki.replace(/"/g,"&quot;")} : Missing</td>
                                        <td class="td" width="260" id="${current_path}-${ki.replace(/"/g,"&quot;")}"  align=right>${html_link}</td>
                                    </tr>
                                `);
                            }
                            else
                            {
                                list_values.push(`
                                    <tr class="tr"  id="tr-${current_path}-${ki.replace(/"/g,"&quot;")}">
                                        <td class="td" width="820" colspan=2>${ki.replace(/"/g,"&quot;")}</td>
                                        <td class="td" width="260" id="${current_path}-${ki.replace(/"/g,"&quot;")}"  align=right>${vi}</td>
                                    </tr>
                                `);
                            }
                            
                        }   
                    }
				
				list_values.push(`
								</tbody>
							</table>
						</td>
						<td class="td" colspan="1"></td>
					</tr>
				`);
			}
            else if(stat_type == "FREQ")
            {
				list_values.push(`
					<tr class="tr">
						<td class="td" width="140"></td>
						<td class="td p-0" colspan="5">
							<table class="table table-fixed-layout align-cell-top m-0">
                                <caption class="table-caption">
                                    Table with all possible list values for the ${form_name}'s ${p_metadata.prompt} field type.
                                </caption>
								<thead class="thead">
									<tr class="header-level-top-black">
										<th class="th" colspan="5" width="1080" scope="colgroup">Field Values</th>
									</tr>
								</thead>
								<thead class="thead">
									<tr class="header-level-2
				`);
                

                        list_values.push(` sticky z-index-middle" style="top: 355px;">
                                            <th class="th" width="820" scope="col" colspan=2>Value - ${p_metadata.sass_export_name}</th>
                                            <th class="th" width="260" scope="col" align=right>N (Counts)</th>
                                        </tr>
                                    </thead>
                                    <tbody class="tbody">`);

             
                const current_path = p_path.substr(1);
                if(g_report_map.has(current_path))
                {
                    const vi_map = g_report_map.get(current_path);

                    let keys = Array.from( vi_map.keys() ).sort();
                    for(const ki of keys)
                    {
                        const vi = vi_map.get(ki);

                        if(ki == "(-)")
                        {

                            const html_link = render_link
                            (
                                current_path,
                                "missing",
                                vi,
                                "Missing List Values"
                            );

                            list_values.push(`
                                <tr class="tr"  id="tr-${current_path}-${ki.replace(/"/g,"&quot;")}">
                                    <td class="td" width="820" colspan=2>${ki.replace(/"/g,"&quot;")} : Missing</td>
                                    <td class="td" width="260" id="${current_path}-${ki.replace(/"/g,"&quot;")}"  align=right>${html_link}</td>
                                </tr>
                            `);
                        }
                        else
                        {
                            list_values.push(`
                                <tr class="tr"  id="tr-${current_path}-${ki.replace(/"/g,"&quot;")}">
                                    <td class="td" width="820" colspan=2>${ki.replace(/"/g,"&quot;")}</td>
                                    <td class="td" width="260" id="${current_path}-${ki.replace(/"/g,"&quot;")}"  align=right>${vi}</td>
                                </tr>
                            `);
                        }
                        
                    }   
                }
                
				
				list_values.push(`
								</tbody>
							</table>
						</td>
						<td class="td" colspan="1"></td>
					</tr>
				`);
            }

			if 
            (
				!form_name ||
				 form_name.indexOf('none') !== -1 ||
				 form_name == '(none)' ||
				 form_name.indexOf('blank') !== -1 ||
				 form_name == '(blank)'
			) 
            {
                //console.log("reject bubba");
				return;
			}

			// Adding a header per section
			if 
            (
                last_form != form_name ||
                is_single_field_filter
            ) 
            {
				last_form = form_name;
				p_result.push(`
					<thead class="thead">
						<tr class="header-level-top-white" style="font-size: 17px">
							<th class="th" colspan="7" scope="colgroup">
								${form_name}
							</th>
						</tr>
					</thead>
					<thead class="thead" style="border-bottom: 1px solid #dee2e6;">
						<tr class="header-level-2 sticky z-index-middle font-weight-bold" style-"top: 57px;">
							<th class="th" width="140" scope="col">MMRIA Form</th>
							<th class="th" width="140" scope="col">Export File Name</th>
							<th class="th" width="120" scope="col">Export Field</th>
							<th class="th" width="180" scope="col">Prompt</th>
							<th class="th" width="380" scope="col">Description</th>
							<th class="th" width="260" scope="col">Path</th>
							<th class="th" width="110" scope="col">Data Type</th>
						</tr>
					</thead>
				`);
			}

			p_result.push(`
				<tr>
					<td width="140">${form_name}</td>
					<td width="140">${file_name}</td>
					<td width="120">${field_name}</td>
					<td width="180">${p_metadata.prompt}</td>
					<td width="380">${description}</td>
					<td width="260">${p_path}</td>
					<td width="110">${(data_type.toLowerCase() == "textarea" || data_type.toLowerCase() == "jurisdiction")? "string": data_type}</td>
				</tr>
				${list_values.join("")}
			`);

			break;

	}
}

/*
2	FREQ	N (Total Count)
3	STAT_N	N (Total Count), Missing Count, Min, Max, Mean, Standard Deviation, Median, Mode
4	STAT_D	N (Total Count), Missing Count, Min, Max



*/

function filter_tag(val)
{
    if
    (
        val != "FREQ" &&
        val != "STAT_N" &&
        val != "STAT_D"
    )
    {
        return false;
    }

    return true;
}

function convert_form_name(p_value)
{
	let lookup = {
		'(none)': '(none)',
		'home_record': 'Home Record',
		'death_certificate': 'Death Certificate',
		'birth_fetal_death_certificate_parent': 'Birth/Fetal Death Certificate - Parent Section',
		'birth_certificate_infant_fetal_section': 'Birth/Fetal Death Certificate - Infant/Fetal Section',
		'autopsy_report': 'Autopsy Report',
		'prenatal': 'Prenatal Care Record',
		'er_visit_and_hospital_medical_records': 'ER Visits & Hospitalizations',
		'other_medical_office_visits': 'Other Medical Office Visits',
		'medical_transport': 'Medical Transport',
		'social_and_environmental_profile': 'Social & Environment Profile',
		'mental_health_profile': 'Mental Health Profile',
		'informant_interviews': 'Informant Interviews',
		'case_narrative': 'Case Narrative',
		'committee_review': 'Committee Decisions',
        'cvs':'Community Vital Signs'
	}

    if(g_is_pmss_enhanced)
    {
        return p_value;
    }
    else
    {
        return lookup[p_value.toLowerCase()];    
    }

	
}


function convert_dictionary_path_to_lookup_object(p_path)
{
	//g_data.prenatal.routine_monitoring.systolic_bp
	var result = null;
	var temp_result = []
	var temp = "g_metadata." + p_path.replace(new RegExp('/','gm'),".").replace(new RegExp('\\.(\\d+)\\.','gm'),"[$1].").replace(new RegExp('\\.(\\d+)$','g'),"[$1]");
	var index = temp.lastIndexOf('.');
	
	temp_result.push(temp.substr(0, index));
	temp_result.push(temp.substr(index + 1, temp.length - (index + 1)));

	var lookup_list = eval(temp_result[0]);

	for(var i = 0; i < lookup_list.length; i++)
	{
		if(lookup_list[i].name == temp_result[1])
		{
			result = lookup_list[i].values;
			break;
		}
	}

	return result;
}


function renderSortCaseStatus(p_case_view)
{
	const sortCaseStatuses = [
        {
            value : 'all',
            display : '-- All --'
        },
        {
            value : '9999',
            display : '(blank)'
        },
        ,
        {
            value : '1',
            display : 'Abstracting (incomplete)'
        },
        {
            value : '2',
            display : 'Abstraction Complete'
        },
        {
            value : '3',
            display : 'Ready For Review'
        },
        {
            value : '4',
            display : 'Review complete and decision entered'
        },
        {
            value : '5',
            display : 'Out of Scope and death certificate entered'
        },
        {
            value : '6',
            display : 'False Positive and death certificate entered'
        },
        {
            value : '0',
            display : 'Vitals Import'
        },
    ];
    const sortCaseStatusList = [];

	sortCaseStatuses.map((status, i) => {

        return sortCaseStatusList.push(`<option value="${status.value}" ${status.value == p_case_view.case_status ? ' selected ' : ''}>${status.display}</option>`);
    });

	return sortCaseStatusList.join('');
}


function renderPregnancyRelatedness(p_case_view)
{
	const sortCaseStatuses = [
        {
            value : 'all',
            display : '-- All --'
        },
        {
            value : '9999',
            display : '(blank)'
        },
        ,
        {
            value : '1',
            display : 'Pregnancy-related'
        },
        {
            value : '0',
            display : 'Pregnancy-Associated, but NOT-Related'
        },
        {
            value : '2',
            display : 'Pregnancy-Associated, but unable to Determine Pregnancy-Relatedness'
        },
        {
            value : '99',
            display : 'Not Pregnancy-Related or -Associated (i.e. False Positive)'
        }
    ];
    const sortCaseStatusList = [];

	sortCaseStatuses.map((status, i) => {

        return sortCaseStatusList.push(`<option value="${status.value}" ${status.value == p_case_view.pregnancy_relatedness ? ' selected ' : ''}>${status.display}</option>`);
    });

	return sortCaseStatusList.join(''); 
}

function render_display_frequency_check_box(p_filter)
{
    let is_checked_string = '';

    if(p_filter.do_not_display_frequencies_equal_to_zero == true)
    {
        is_checked_string = 'checked'
    }

    return `
    <label style="text-align:left;justify-content:left;"><input type="checkbox" ${is_checked_string} value=true name="display_zero_values" style="text-align:left;justify-content:left" onclick="on_display_zero_values_click(this)" />&nbsp;Do not display values with frequency count = 0</label>
    `
}

function render_pregnancy_filter(p_case_view)
{

    let display_date_of_reviews_html = "display:none;";
    let display_date_of_deaths_html = "display:none;";

    if(g_filter.include_blank_date_of_reviews == false)
    {
        display_date_of_reviews_html = "display:flex;";
    }
    
    if(g_filter.include_blank_date_of_deaths == false)
    {
        display_date_of_deaths_html = "display:flex;";
    }
    
    return `

    <div class="row" style="display:block;margin-left:0px;margin-bottom:0px;padding-bottom:0px;">
        <div>
            <div role="group" aria-labelledby="review_dates_label" class="d-flex align-items-center mb-2">
                <div id="review_dates_label" class="font-weight-normal mr-2 align-items-center">
                    Review Dates:
                </div>
                    <div style="padding-left:15px">
                        <label for="all_review_dates_radio" class="font-weight-normal mb-0 mr-2" style="justify-content:left">
                        <input type="radio" onchange="date_of_review_panel_select(this.value)" name="select_date_of_review_panel" id="all_review_dates_radio" value="all" ${g_filter.include_blank_date_of_reviews == true ? 'checked="true"' : '' } />
                        &nbsp;All cases</label>
                    </div>
                    <div>
                        <label aria-label="Select date range for Review Dates" id="select_dates_review_label" for="select_review_dates_radio" class="font-weight-normal mb-0 mr-2" style="justify-content:left">
                        <input type="radio" onchange="date_of_review_panel_select(this.value)" name="select_date_of_review_panel" id="select_review_dates_radio"  value="select"  ${g_filter.include_blank_date_of_reviews == false ? 'checked="true"' : '' }/>
                        &nbsp;Select dates</label>
                    </div>
                    <div role="group" aria-labelledby="select_dates_review_label" class="d-flex align-items-center">
                        <span class="mr-3" id="date_of_review_panel_begin" style="${display_date_of_reviews_html};">
                            <label for="review_begin_date" class="font-weight-normal mt-2 mr-2">
                                Begin
                            </label>
                            <input class="form-control" id="review_begin_date" type="date" value="${ControlFormatDate(g_filter.date_of_review.begin)}" max="${ControlFormatDate(g_filter.date_of_review.end)}" onblur="review_begin_date_change(this.value)" />
                        </span>
                        <span class="mr-3" id="date_of_review_panel_end" style="${display_date_of_reviews_html};">
                            <label for="review_end_date" class="font-weight-normal mt-2 mr-2">
                                End
                            </label>
                            <input class="form-control" id="review_end_date" type="date" value="${ControlFormatDate(g_filter.date_of_review.end)}"  min="${ControlFormatDate(g_filter.date_of_review.begin)}" onblur="review_end_date_change(this.value)" />
                        </span>
                    </div>
                </div>
            </div>
            <div role="group" aria-labelledby="dates_of_death_label" class="d-flex align-items-center">
                <div id="dates_of_death_label" class="font-weight-normal mr-2">
                    Dates of Death:
                </div>
                <div class="d-flex align-items-center">
                    <div style="padding-left:4px">
                        <label for="all_date_of_death_radio" class="font-weight-normal mb-0 mr-2" style="justify-content:left">
                        <input type="radio" onchange="date_of_death_panel_select(this.value)" name="select_date_of_death_panel" id="all_date_of_death_radio" value="all" ${g_filter.include_blank_date_of_deaths == true ? 'checked="true"' : '' } />
                        &nbsp;All cases</label>
                    </div>
                    <div class="d-flex align-items-center">
                        <label aria-label="Select date range for Dates of Death" id="select_dates_death_label" for="select_date_of_death_radio" class="font-weight-normal mb-0 mr-2" style="justify-content:left">
                        <input type="radio" onchange="date_of_death_panel_select(this.value)" name="select_date_of_death_panel" id="select_date_of_death_radio"  value="select"  ${g_filter.include_blank_date_of_deaths == false ? 'checked="true"' : '' }/>
                        &nbsp;Select dates</label>      
                    </div>
                    <div role="group" aria-labelledby="select_dates_death_label" class="d-flex align-items-center">
                        <span class="mr-3" id="date_of_death_panel_begin" style="${display_date_of_deaths_html}">
                            <label for="death_begin_date" class="font-weight-normal mt-2 mr-2">
                                Begin
                            </label>
                            <input class="form-control" id="death_begin_date" type="date" value="${ControlFormatDate(g_filter.date_of_death.begin)}" max="${ControlFormatDate(g_filter.date_of_death.end)}" onblur="death_begin_date_change(this.value)" />
                        </span>
                        <span class="mr-3" id="date_of_death_panel_end" style="${display_date_of_deaths_html}">
                            <label for="death_end_date" class="font-weight-normal mt-2 mr-2">
                                End
                            </label>
                            <input class="form-control" id="death_end_date" type="date" value="${ControlFormatDate(g_filter.date_of_death.end)}"  min="${ControlFormatDate(g_filter.date_of_death.begin)}" onblur="death_end_date_change(this.value)" />
                        </span>
                    </div>
                </div>
            </div>
        </div>
    </div>

`;

   

}


  function date_of_review_panel_select(p_value)
{
    const begin = document.getElementById("date_of_review_panel_begin");
    const end= document.getElementById("date_of_review_panel_end");
    if(p_value=="all")
    {
        g_filter.include_blank_date_of_reviews = true;
        begin.style["display"] = "none";
        end.style["display"] = "none";
    }
    else
    {
        g_filter.include_blank_date_of_reviews = false;
        begin.style["display"] = "flex";
        end.style["display"] = "flex";
    }
}


function date_of_death_panel_select(p_value)
{
    const begin = document.getElementById("date_of_death_panel_begin");
    const end = document.getElementById("date_of_death_panel_end");
    if(p_value=="all")
    {
        g_filter.include_blank_date_of_deaths = true;
        begin.style["display"] = "none";
        end.style["display"] = "none";
    }
    else
    {
        g_filter.include_blank_date_of_deaths = false;
        begin.style["display"] = "flex";
        end.style["display"] = "flex";
    }


}

function pad_number(n) 
{
    n = n + '';
    return n.length >= 2 ? n : new Array(2 - n.length + 1).join("0") + n;
}

function formatDate(p_value)
{
    const result= pad_number(p_value.getMonth() + 1) + '/' + pad_number(p_value.getDate()) + '/' +  p_value.getFullYear();

    return result;
}

function ControlFormatDate(p_value)
{
    const result= p_value.getFullYear() + '-' + pad_number(p_value.getMonth() + 1) + '-' + pad_number(p_value.getDate());

    return result;
}

function review_begin_date_change(p_value)
{
    const arr = p_value.split("-");

    let date_changed = arr[0] >= 1900 ? false :true;



    let test_date = new Date(arr[0] > 1900 ? arr[0] : 1900, arr[1] - 1, arr[2]);

    if(arr[0] < 1900)
    {
        test_date = new Date(1900 , 0, 1);
    }
    const current_date = new Date();


    if(test_date <= current_date && test_date <= g_filter.date_of_review.end )
    {
        g_filter.date_of_review.begin = test_date;
        const el = document.getElementById("review_end_date");
        el.setAttribute("min", p_value);

        if(date_changed)
        {
            el.setAttribute("min", ControlFormatDate(test_date));

            const el2 = document.getElementById("review_begin_date");
            el2.value = ControlFormatDate(g_filter.date_of_review.begin);
        }
    }
    else
    {
        const el = document.getElementById("review_begin_date");
        el.value = ControlFormatDate(g_filter.date_of_review.begin);
    }

    show_needs_apply_id(true);
}
function review_end_date_change(p_value)
{
    const arr = p_value.split("-");
    
    let date_changed = arr[0] >= 1900 ? false :true;

    let test_date = new Date(arr[0] > 1900 ? arr[0] : 1900, arr[1] - 1, arr[2]);

    if(arr[0] < 1900)
    {
        test_date = new Date(1900 , 0, 1);
    }

    const current_date = new Date();

    if(test_date <= current_date && g_filter.date_of_review.begin <= test_date)
    {
        g_filter.date_of_review.end = test_date;
        const el = document.getElementById("review_begin_date");
        el.setAttribute("max", p_value);

        if(date_changed)
        {
            el.setAttribute("max", ControlFormatDate(test_date));

            const el2 = document.getElementById("review_end_date");
            el2.value = ControlFormatDate(g_filter.date_of_review.end);
        }
    }
    else
    {
        const el = document.getElementById("review_end_date");
        el.value = ControlFormatDate(g_filter.date_of_review.end);
    }

    show_needs_apply_id(true);
}

function death_begin_date_change(p_value)
{
    const arr = p_value.split("-");
    
    let date_changed = arr[0] >= 1900 ? false :true;

    let test_date = new Date(arr[0] > 1900 ? arr[0] : 1900, arr[1] - 1, arr[2]);

    if(arr[0] < 1900)
    {
        test_date = new Date(1900 , 0, 1);
    }

    const current_date = new Date();

    if(test_date <= current_date && test_date <= g_filter.date_of_death.end)
    {
        g_filter.date_of_death.begin = test_date;
        const el = document.getElementById("death_end_date");
        el.setAttribute("min", p_value);

        if(date_changed)
        {
            el.setAttribute("min", ControlFormatDate(test_date));

            const el2 = document.getElementById("death_begin_date");
            el2.value = ControlFormatDate(g_filter.date_of_death.begin);
        }
    }
    else
    {
        const el = document.getElementById("death_begin_date");
        el.value = ControlFormatDate(g_filter.date_of_death.begin);
    }

    show_needs_apply_id(true);
}

function death_end_date_change(p_value)
{
    const arr = p_value.split("-");
    
    let date_changed = arr[0] >= 1900 ? false :true;

    let test_date = new Date(arr[0] > 1900 ? arr[0] : 1900, arr[1] - 1, arr[2]);

    if(arr[0] < 1900)
    {
        test_date = new Date(1900 , 0, 1);
    }

    const current_date = new Date();

    if(test_date <= current_date && g_filter.date_of_death.begin <=  test_date)
    {
        g_filter.date_of_death.end = test_date;
        const el = document.getElementById("death_begin_date");
        el.setAttribute("max", p_value);

        if(date_changed)
        {
            el.setAttribute("max", ControlFormatDate(test_date));

            const el2 = document.getElementById("death_end_date");
            el2.value = ControlFormatDate(g_filter.date_of_death.end);
        }
    }
    else
    {
        const el = document.getElementById("death_end_date");
        el.value = ControlFormatDate(g_filter.date_of_death.end);
    }

    show_needs_apply_id(true);
}


function render_FREQ(p_context, prompt)
{
    const detail = "FREQ";

    return `<tr class="tr">
    <td class="td" width="140" style="border:white"></td>
    <td class="td p-0" colspan="5">
        <table class="table table-fixed-layout align-cell-top m-0">
            <caption class="table-caption">
                Total Frequency distribution for ${prompt} field type.
            </caption>
            <thead class="thead">
                <tr class="header-level-top-black">
                    <th class="th" scope="col" colspan=2>Summary Type</th>
                    <th class="th" width="260" scope="col" style="text-align:right">N (Total Count)</th>
                </tr>
            </thead>
            <tbody class="tbody">	
            <tr class="tr">
            <td class="td" width="150" colspan=2><b>Frequency&nbsp;Distribution</b></td>
            <!--td class="td"></td-->
            <td class="td"id="${p_context.dictionary_path}-count" align=right>0</td>
            </tr>
            </tbody>
            </table>
        </td>
        <td class="td" colspan="1"></td>
    </tr>`;

}

function render_STAT_D(p_context, prompt)
{
    const detail = "STAT_D";
    return `<tr class="tr">
    <td class="td" width="140" style="border:white"></td>
    <td class="td p-0" colspan="5">
        <table class="table table-fixed-layout align-cell-top m-0">
            <caption class="table-caption">
                Date summary for ${prompt} field type.
            </caption>
            <thead class="thead">
            <tr class="header-level-top-black">
                <th class="th" width="170" scope="col">Summary Type</th>
                <th class="th" width="260" scope="col" style="text-align:right">N (Total Count)</th>
                <th class="th" width="260" scope="col" style="text-align:right">Missing Count</th>
                <th class="th" width="260" scope="col" style="text-align:right">Minimum</th>
                <th class="th" width="260" scope="col" style="text-align:right">Maximum</th>
            </tr>
        </thead>
        <tbody class="tbody">	
        <tr class="tr">
        <td class="td" width="140">Date&nbsp;Summary</td>
        <td class="td" id="${p_context.dictionary_path}-count" align=right></td>
        <td class="td" id="${p_context.dictionary_path}-missing" align=right></td>
        <td class="td" id="${p_context.dictionary_path}-min" align=right></td>
        <td class="td" id="${p_context.dictionary_path}-max" align=right></td>
        </tr>
        </tbody>
            </table>
        </td>
        <td class="td" colspan="1"></td>
    </tr>`;
}

function render_STAT_N(p_context, prompt)
{
    const detail = "STAT_N";
    return `<tr class="tr">
    <td class="td" width="140" style="border:white"></td>
    <td class="td p-0" colspan="5">
        <table class="table table-fixed-layout align-cell-top m-0">
            <caption class="table-caption">
                Numeric summary for ${prompt} field type.
            </caption>
            <thead class="thead">
                <tr class="header-level-top-black">
                    <th class="th" width="170" scope="col">Summary Type</th>
                    <th class="th" width="260" scope="col" style="text-align:right">N (Total Count)</th>
                    <th class="th" width="260" scope="col" style="text-align:right">Missing Count</th>
                    <th class="th" width="260" scope="col" style="text-align:right">Minimum</th>
                    <th class="th" width="260" scope="col" style="text-align:right">Maximum</th>
                    <th class="th" width="260" scope="col" style="text-align:right">Mean</th>
                    <th class="th" width="260" scope="col" style="text-align:right">Standard<br/>Deviation</th>
                    <th class="th" width="260" scope="col" style="text-align:right">Median</th>
                    <th class="th" width="260" scope="col" style="text-align:right">Mode</th>
                </tr>
            </thead>
            <tbody class="tbody">	
            <tr class="tr">
            <td class="td" width="170">Numeric<br/>Summary</td>
            <td class="td" id="${p_context.dictionary_path}-count" align=right></td>
            <td class="td" id="${p_context.dictionary_path}-missing" align=right></td>
            <td class="td" id="${p_context.dictionary_path}-min" align=right></td>
            <td class="td" id="${p_context.dictionary_path}-max" align=right></td>
            <td class="td" id="${p_context.dictionary_path}-mean" align=right></td>
            <td class="td" id="${p_context.dictionary_path}-std_dev" align=right></td>
            <td class="td" id="${p_context.dictionary_path}-median" align=right></td>
            <td class="td" id="${p_context.dictionary_path}-mode" align=right></td>
            </tr>
            </tbody>
            </table>
        </td>
        <td class="td" colspan="1"></td>
    </tr>`;
}


function show_needs_apply_id(value)
{
    const el = document.getElementById("needs_apply_id");
    if(value)
    {
        el.style.visibility = "visible";
    }
    else
    {
        el.style.visibility = "hidden";
    }
}

function showCheckboxes(event) 
{
    
    const checkboxes = document.getElementById("checkboxes");
    const field_filter_control = document.getElementById('field_filter');
    if (!is_field_list_expanded && (event.key == 'Enter' || event.key == 'Space' || event.key == undefined)) 
    {
        checkboxes.style.display = "block";
        is_field_list_expanded = true;
        document.getElementById('ff-All').focus();
        field_filter_control.setAttribute('aria-expanded', 'true');
    }
    else if (event.key == 'Escape' || event.key == 'Tab')
    {
        checkboxes.style.display = "none";
        is_field_list_expanded = false;
        document.getElementById('field_filter').focus();
        field_filter_control.setAttribute('aria-expanded', 'false');
    }
    else if (is_field_list_expanded && (event.key == 'Enter' || event.key == 'Space' || event.key == undefined)) 
    {
        event.preventDefault();
        checkboxes.style.display = "none";
        is_field_list_expanded = false;
        field_filter_control.setAttribute('aria-expanded', 'false');
        
    }
}

async function on_display_zero_values_click(p_value)
{
    g_filter.do_not_display_frequencies_equal_to_zero = p_value.checked;
}


function form_render_mmria()
{
    return `
            <div class="d-flex flex-column mb-2 row no-gutters justify-content-between no-print">
            <div class="d-flex mb-3">
                <label class="mr-3" for="search_text" style="text-align:left; margin-top: 5px;">Search text or MMRIA ID:</label>
                <input type="text" 
                    placeholder="Enter field name or MMRIA ID"
                    class="form-control mr-2"
                    id="search_text"
                    value=""
                    style="width: 570px;"
                    onchange="search_text_change(this.value)" 
                />
            </div>
            <div class="d-flex container-flex-wrap mb-3">
                <div class="align-text-top pl-0 col-6">
                    <select aria-label='form filter' id="form_filter" class="custom-select mr-2" onchange="on_form_filter_changed(this.value)">
                        ${render_form_filter(g_filter)}
                    </select>
                </div>
                <div class="multiselect pl-0 col-5" style="width:410px;">
                    <div aria-label='field filter' aria-owns="checkboxes" aria-expanded="false" role="combobox" tabindex="0" class="selectBox" onkeyup="showCheckboxes(event)" id="field_filter" onclick="showCheckboxes(event)">
                        <select aria-hidden="true" tabindex="-1"  class="custom-select mr-2" >
                            <option>(Any Field)</option>
                        </select>
                        <div class="overSelect"></div>
                    </div>
                    <div id="checkboxes" style="height:200px;overflow-y:scroll;">
                        ${render_field_filter(g_filter)}
                    </div>
                </div>
                <div class="d-flex flex-wrap align-items-start pl-0 col-sm-12 button-container">
                    <button
                        id="apply_filters"
                        type="button"
                        class="btn primary-button no-print mt-0 mr-2"
                        alt="clear search"
                        onclick="search_click()">Apply Filters</button>
                    <button
                        id="reset_button"
                        type="button"
                        class="btn primary-button no-print mt-0 mr-2"
                        alt="reset search"
                        onclick="reset_click()">Reset</button>
                    <button type="button" id="print_button" class="btn primary-button row no-gutters align-items-center mt-0 no-print" onclick="handle_print()"><span style="fill: white" class="mr-1 fill-p" aria-hidden="true" focusable="false"><svg xmlns="http://www.w3.org/2000/svg" width="24" height="24"><path d="M19 8H5c-1.66 0-3 1.34-3 3v6h4v4h12v-4h4v-6c0-1.66-1.34-3-3-3zm-3 11H8v-5h8v5zm3-7c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zm-1-9H6v4h12V3z"></path><path d="M0 0h24v24H0z" fill="none"></path></svg></span>Print</button>
                    <span class="spinner-container spinner-inline ml-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>          
                </div>
            </div>
            <div class="form-inline mb-3">
                <label for="search_case_status" class="font-weight-normal mr-2">Case Status:</label>
                <select style="flex: 0 0 57.5%; max-width: 58.333333%;" id="search_case_status" class="custom-select" onchange="search_case_status_onchange(this.value)">
                    ${renderSortCaseStatus(g_case_view_request)}
                </select>
            </div>
            <div class="form-inline mb-3">
                <label for="search_pregnancy_relatedness" class="font-weight-normal mr-2">Pregnancy Relatedness:</label>
                <select id="search_pregnancy_relatedness" class="custom-select" onchange="search_pregnancy_relatedness_onchange(this.value)">
                    ${renderPregnancyRelatedness(g_case_view_request)}
                </select>
            </div>
            <div>
                ${render_pregnancy_filter(g_case_view_request)}
            </div>
            <div class="no-print">
                ${render_display_frequency_check_box(g_filter)}
            </div>
        </div> 
        <div id="needs_apply_id" style="visibility:hidden">
            <b>Click the Apply Filters button to apply changes</b>
        </div>
        `;
}


function form_render_pmss()
{
    return `
            <div class="d-flex flex-column mb-2 row no-gutters justify-content-between no-print">
            <div class="d-flex mb-3">
                <label class="mr-3" for="search_text" style="text-align:left; margin-top: 5px;">Search text or MMRIA ID:</label>
                <input type="text" 
                    placeholder="Enter field name or MMRIA ID"
                    class="form-control mr-2"
                    id="search_text"
                    value=""
                    style="width: 570px;"
                    onchange="search_text_change(this.value)" 
                />
            </div>
            <div class="d-flex container-flex-wrap mb-3">
                <div class="align-text-top pl-0 col-6">
                    <select aria-label='form filter' id="form_filter" class="custom-select mr-2" onchange="on_form_filter_changed(this.value)">
                        ${render_form_filter(g_filter)}
                    </select>
                </div>
                <div class="multiselect pl-0 col-5" style="width:410px;">
                    <div aria-label='field filter' aria-owns="checkboxes" aria-expanded="false" role="combobox" tabindex="0" class="selectBox" onkeyup="showCheckboxes(event)" id="field_filter" onclick="showCheckboxes(event)">
                        <select aria-hidden="true" tabindex="-1"  class="custom-select mr-2" >
                            <option>(Any Field)</option>
                        </select>
                        <div class="overSelect"></div>
                    </div>
                    <div id="checkboxes" style="height:200px;overflow-y:scroll;">
                        ${render_field_filter(g_filter)}
                    </div>
                </div>
                <div class="d-flex flex-wrap pl-0 col-sm-12 button-container">
                    <button
                        id="apply_filters"
                        type="button"
                        class="btn primary-button no-print mt-0 mr-2"
                        alt="clear search"
                        onclick="search_click()">Apply Filters</button>
                    <button
                        id="reset_button"
                        type="button"
                        class="btn primary-button no-print mt-0 mr-2"
                        alt="reset search"
                        onclick="reset_click()">Reset</button>
                    <button type="button" id="print_button" class="btn primary-button row no-gutters align-items-center mt-0 no-print" onclick="handle_print()"><span style="fill: white" class="mr-1 fill-p" aria-hidden="true" focusable="false"><svg xmlns="http://www.w3.org/2000/svg" width="24" height="24"><path d="M19 8H5c-1.66 0-3 1.34-3 3v6h4v4h12v-4h4v-6c0-1.66-1.34-3-3-3zm-3 11H8v-5h8v5zm3-7c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zm-1-9H6v4h12V3z"></path><path d="M0 0h24v24H0z" fill="none"></path></svg></span>Print</button>
                    <span class="spinner-container spinner-inline ml-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>          
                </div>
            </div>
            <div class="form-inline mb-3">
                <label for="search_case_status" class="font-weight-normal mr-2">Case Status:</label>
                <select style="flex: 0 0 57.5%; max-width: 58.333333%;" id="search_case_status" class="custom-select" onchange="search_case_status_onchange(this.value)">
                    ${renderSortCaseStatus(g_case_view_request)}
                </select>
            </div>
            <div class="form-inline mb-3">
                <label for="search_pregnancy_relatedness" class="font-weight-normal mr-2">Pregnancy Relatedness:</label>
                <select id="search_pregnancy_relatedness" class="custom-select" onchange="search_pregnancy_relatedness_onchange(this.value)">
                    ${renderPregnancyRelatedness(g_case_view_request)}
                </select>
            </div>
            <div>
                ${render_pregnancy_filter(g_case_view_request)}
            </div>
            <div class="no-print">
                ${render_display_frequency_check_box(g_filter)}
            </div>
        </div> 
        <div id="needs_apply_id" style="visibility:hidden">
            <b>Click the Apply Filters button to apply changes</b>
        </div>
        `;
}