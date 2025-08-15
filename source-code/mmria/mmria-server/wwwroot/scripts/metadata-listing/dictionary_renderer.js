function dictionary_render(p_metadata, p_path)
{
	var result = [];

	let de_identified_search_result = [];
	render_de_identified_search_result(de_identified_search_result, g_filter);

	result.push(`
		<div id="de_identify_filter" class="mt-2" data-prop="de_identified_selection_type" style="">
			<div class="sticky-header form-inline mb-2 row no-gutters align-items-center justify-content-between no-print">
				<div class="row no-gutters align-items-center">
					<label for="de_identify_search_text" class="mr-2"> Search for:</label>
					<input type="text"
								class="form-control mr-2"
								id="de_identify_search_text"
								value=""
								style="width: 170px;"
								onchange="de_identify_search_text_change(this.value)"/>
					<select id="de_identify_form_filter" class="custom-select mr-2">
						${render_de_identify_form_filter(g_filter)}
					</select>
					<select id="metadata_version_filter" class="custom-select mr-2">
						<option value="">Select Metadata Version</option>
						<option value="19.10.17">19.10.17</option>
					</select>
					<button type="submit" class="btn btn-secondary no-print" alt="clear search" onclick="handle_search(de_identified_search_click)">Search</button>
				</div>
				<div>
					<div class="row no-gutters justify-content-end">
						<button class="btn btn-secondary row no-gutters align-items-center no-print" onclick="handle_print()"><span class="mr-1 fill-p" aria-hidden="true" focusable="false"><svg xmlns="http://www.w3.org/2000/svg" width="24" height="24"><path d="M19 8H5c-1.66 0-3 1.34-3 3v6h4v4h12v-4h4v-6c0-1.66-1.34-3-3-3zm-3 11H8v-5h8v5zm3-7c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zm-1-9H6v4h12V3z"/><path d="M0 0h24v24H0z" fill="none"/></svg></span>Print</button>
					</div>
				</div>
			</div>

			<div class="mt-2">
				<div id="de_identify_search_result_list" style="font-size: 14px">
					${de_identified_search_result.join("")}
				</div>
			</div>
	`);

	return result;
}


function handle_print() {
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


function render_de_identify_form_filter(p_filter)
{
	let result = [];

	result.push(`<option value="">(Any Form)</option>`)

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

function handle_search(callback)
{
	const search_input = document.getElementById('de_identify_search_text');
	const search_list = document.getElementById('de_identify_search_result_list');
	callback();
}


function de_identified_search_click()
{
	g_filter.selected_form = document.getElementById("de_identify_form_filter").value;

	let de_identify_search_result_list = document.getElementById("de_identify_search_result_list");

	let result = [];
	render_de_identified_search_result(result, g_filter);

	de_identify_search_result_list.innerHTML = result.join("");
}

function render_de_identified_search_result(p_result, p_filter)
{
	render_de_identified_search_result_item(p_result, g_metadata, "", p_filter.selected_form, p_filter.search_text);
}

function render_de_identified_search_result_item(p_result, p_metadata, p_path, p_selected_form, p_search_text)
{
	switch(p_metadata.type.toLowerCase())
	{
		case "form":
				if(p_selected_form== null || p_selected_form=="")
				{
					for(let i = 0; i < p_metadata.children.length; i++)
					{
						let item = p_metadata.children[i];
						render_de_identified_search_result_item(p_result, item, p_path + "/" + item.name, p_selected_form, p_search_text);
					}
				}
				else
				{
					if(p_metadata.name.toLowerCase() == p_selected_form.toLowerCase())
					{
						for(let i = 0; i < p_metadata.children.length; i++)
						{
							let item = p_metadata.children[i];
							render_de_identified_search_result_item(p_result, item, p_path + "/" + item.name, p_selected_form, p_search_text);
						}
					}
				}
				
				break;
		case "app":
		case "group":
		case "grid":
			for(let i = 0; i < p_metadata.children.length; i++)
			{
				let item = p_metadata.children[i];
				render_de_identified_search_result_item(p_result, item, p_path + "/" + item.name, p_selected_form, p_search_text);
			}
			break;
		default:
			if(p_search_text != null && p_search_text !="")
			{
				if
				(
					!(
						p_metadata.name.indexOf(p_search_text) > -1 ||
						p_metadata.prompt.indexOf(p_search_text) > -1 
					)
				
				)
				{
					return;
				}
			}

			let form_name = "(none)";
			let path_array = p_path.split('/');
			let description = "";
			let list_values = [];

			if(path_array.length > 2)
			{
				form_name = path_array[1];
			}


			if(p_metadata.description != null)
			{
				description = p_metadata.description;
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
						<td class="td"></td>
						<td class="td p-0" colspan="3">
							<table class="table table--standard rounded-0 m-0">
								<thead class="thead">
									<tr class="tr">
										<th class="th" colspan=3>List Values</th>
									</tr>
								</thead>
								<thead class="thead">
									<tr class="tr">
										<th class="th" width="200">Value</th>
										<th class="th" width="480">Display</th>
										<th class="th" width="350">Description</th>
									</tr>
								</thead>
								<tbody class="tbody">	
				`);

					for(let i= 0; i < value_list.length; i++)
					{
						list_values.push(`
									<tr class="tr">
										<td class="td">${value_list[i].value}</td>
										<td class="td">${value_list[i].display}</td>
										<td class="td">${value_list[i].description}</td>
									</tr>
						`);
					}
				
				list_values.push(`
								</tbody>
							</table>
						</td>
						<td class="td" colspan="2"></td>
					</tr>
				`);
			}

			p_result.push(`
				<table class="table table--standard rounded-0 mb-3">
					<thead class="thead">
						<tr class="tr bg-gray">
							<th class="th" colspan="3" style="font-size: 20px">
								MMRIA Form: ${form_name}
							</th>
							<th class="th" colspan="3">
								Export File: <span class="font-weight-normal">example_file_name.csv</span>
							</th>
						</tr>
					</thead>
					<thead class="thead">
						<tr class="tr  bg-gray-l2">
							<th class="th" width="160">Export Field</th>
							<th class="th" width="200">Prompt</th>
							<th class="th" width="480">Description</th>
							<th class="th" width="350">Path</th>
							<th class="th" width="110">Type</th>
							<!-- <th class="th" width="100">Calculated</th> -->
						</tr>
					</thead>
					<tbody class="tbody">
						<tr class="tr">
							<td class="td">example_export_field</td>
							<td class="td">${p_metadata.prompt}</td>
							<td class="td">${description}</td>
							<td class="td">${p_path}</td>
							<td class="td">${p_metadata.type}</td>
							<!-- <td class="td">no</td> -->
						</tr>
						${list_values.join("")}
					</tbody>
				</table>
			`);
			break;
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
