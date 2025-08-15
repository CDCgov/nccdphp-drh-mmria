function dictionary_render(p_metadata, p_path)
{
    let long_name = "Maternal Mortality Review Information Application";
    let acronym = "MMRIA"

    if(g_is_pmss_enhanced)
    {
        long_name = "Pregnancy Mortality Surveillance System";
        acronym = "PMSS"
    }
	var result = [];
	let search_result = [];

	render_search_result(search_result, g_filter);

	result.push(`
		<div id="filter" class="sticky-section mt-2" data-prop="selection_type">
			<div style="position: sticky !important; top: 0px !important; background-color: white !important; z-index: 10;" class="sticky-header form-inline mb-2 row no-gutters align-items-center justify-content-between no-print">
				<form class="row no-gutters align-items-center" onsubmit="event.preventDefault()">
					<label for="search_text" class="mr-2"> Search for:</label>
					<input type="text"
								 class="form-control mr-2"
								 id="search_text"
								 value=""
								 style="width: 170px;"
								 onchange="search_text_change(this.value)" />
					<select aria-label='form filter' id="form_filter" class="custom-select mr-2">
						${render_form_filter(g_filter)}
					</select>
					<select aria-label='metadata version filter' id="metadata_version_filter" class="custom-select mr-2" onchange="metadata_version_filter_change(this.value)">
						${render_metadata_version_filter()}
					</select>
					<button
						type="submit"
						class="btn btn-secondary no-print"
						alt="clear search"
						onclick="init_inline_loader(search_click)">Search</button>
						<span class="spinner-container spinner-inline ml-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>
				</form>
				<div>
					<div class="row no-gutters justify-content-end">
						<button class="btn btn-secondary row no-gutters align-items-center no-print" onclick="handle_print()"><span class="mr-1 fill-p" aria-hidden="true" focusable="false"><svg xmlns="http://www.w3.org/2000/svg" width="24" height="24"><path d="M19 8H5c-1.66 0-3 1.34-3 3v6h4v4h12v-4h4v-6c0-1.66-1.34-3-3-3zm-3 11H8v-5h8v5zm3-7c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zm-1-9H6v4h12V3z"/><path d="M0 0h24v24H0z" fill="none"/></svg></span>Print</button>
					</div>
				</div>
			</div>

			<div class="vertical-control pl-0 pr-0 col-md-12">
				<table id="search_result_list" class="table table-layout-fixed align-cell-top" style="font-size: 14px">
                    <caption class="table-caption">
                        ${long_name} (${acronym}) data dictionary table
                        with descriptions and properites of all fields contained in the MMRIA database.
                    </caption>
					${search_result.join("")}
				</table>
			</div>

			${generate_system_generated_definition_list_table()}
			
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


function render_form_filter(p_filter)
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


function search_click()
{
	g_filter.selected_form = document.getElementById("form_filter").value;

	let search_result_list = document.getElementById("search_result_list");
	let result = [];
	
	render_search_result(result, g_filter);

	search_result_list.innerHTML = result.join("");
}


function render_search_result(p_result, p_filter)
{
	// Add toLowerCase() method to help with case sensitivity
	render_search_result_item(p_result, g_metadata, "", p_filter.selected_form, p_filter.search_text.toLowerCase());
}

// var that helps calculate current form and latest form
// Used to calc and create section headers
let last_form = null;

function render_search_result_item(p_result, p_metadata, p_path, p_selected_form, p_search_text)
{

    if(p_metadata.mirror_reference != null && p_metadata.mirror_reference != "")
    {
        return;
    }
	switch(p_metadata.type.toLowerCase())
	{
		case "form":			
			if(p_selected_form== null || p_selected_form=="")
			{
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

			if(el)
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

                if
                (
                    p_metadata.tags.length > 0 &&

                    p_metadata.tags[0].indexOf("CALC_DATE") > -1
                ) 
                {
                    render_group_item(p_result, p_metadata, p_path, p_selected_form, p_search_text);
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
				form_name = path_array[1];
				form_name = convert_form_name(form_name);
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
						<td class="td" width="140"></td>
						<td class="td p-0" colspan="5">
							<table class="table table-fixed-layout align-cell-top">
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
                    list_values.push(` sticky z-index-middle" style="top: 98px;">
										<th class="th" width="140" scope="col">Value</th>
										<th class="th" width="680" scope="col">Display</th>
										<th class="th" width="260" scope="col">Description</th>
									</tr>
								</thead>
								<tbody class="tbody">`);
                }
                else
                {
                    list_values.push(`">
                            <th class="th" width="140" scope="col">Value</th>
                            <th class="th" width="680" scope="col">Display</th>
                            <th class="th" width="260" scope="col">Description</th>
                        </tr>
                        </thead>
                        <tbody class="tbody">
                    `);
                }

					for(let i= 0; i < value_list.length; i++)
					{
						list_values.push(`
									<tr class="tr">
										<td class="td" width="140">${value_list[i].value}</td>
										<td class="td" width="680">${value_list[i].display}</td>
										<td class="td" width="260">${value_list[i].description}</td>
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

			// Remove fields who do not have a form_name or if it doesn't exist
			// if (!form_name || form_name == '(none)' || form_name == '(blank)') {
			// if (!form_name || form_name.includes('none') || form_name.includes('blank')) {
			if (
				!form_name ||
				 form_name.indexOf('none') !== -1 ||
				 form_name == '(none)' ||
				 form_name.indexOf('blank') !== -1 ||
				 form_name == '(blank)'
			) {
				return;
			}
            var mmria_label = "MMRIA";

            if(g_is_pmss_enhanced)
                mmria_label = "PMSS"
			// Adding a header per section
			if (last_form !== form_name) {
				last_form = form_name;
				p_result.push(`
					<thead class="thead">
						<tr class="header-level-top-white" style="font-size: 17px">
							<th colspan="7" scope="colgroup" width="1350">
								${form_name}
							</th>
						</tr>
					</thead>
					<thead class="thead" style="border-bottom: 1px solid #dee2e6;">
						<tr class="header-level-2 sticky z-index-middle" style="top: 57px;">
							<th width="140" scope="col">${mmria_label} Form</th>
							<th width="140" scope="col">Export File Name</th>
							<th width="120" scope="col">Export Field</th>
							<th width="80" scope="col">Prompt</th>
							<th width="180" scope="col">Description</th>
							<th width="260" scope="col">Path</th>
							<th width="110" scope="col">Data Type</th>
						</tr>
					</thead>
				`);
			}

			p_result.push(`
				<tr>
					<td width="140">${form_name}</td>
					<td width="140">${file_name}</td>
					<td width="120">${field_name}</td>
					<td width="80">${p_metadata.prompt}</td>
					<td width="180">${description}</td>
					<td width="260">${p_path}</td>
					<td style="border-right: none;" width="110">${(data_type.toLowerCase() == "textarea" || data_type.toLowerCase() == "jurisdiction")? "string": data_type}</td>
				</tr>
				${list_values.join("")}
			`);

			// Logic to dynamically highlight all matched search queries
			// TODO: Comment back in once approved
			// if (!isNullOrUndefined(p_search_text))
			// {
			// 	setTimeout(() => {
			// 		const container = document.querySelectorAll('#search_result_list td');
					
			// 		// Shorthand for loop, loop through container var
			// 		for (let td of container)
			// 		{
			// 			let tdHtml = td.innerHTML;
			// 			let newContainerHtml = '';
						
			// 			newContainerHtml = tdHtml.split(capitalizeFirstLetter(p_search_text)).join(`<span class="search-highlight">${capitalizeFirstLetter(p_search_text)}</span>`);
			// 			newContainerHtml = newContainerHtml.split(p_search_text).join(`<span class="search-highlight">${p_search_text}</span>`);
			// 			td.innerHTML = newContainerHtml;
			// 		}
			// 	}, 0);
			// }
			break;

	}
}

function render_group_item
(
    p_result, p_metadata, p_path, p_selected_form, p_search_text
)
{
    let file_name = "";
			let field_name = "";
			let file_field_item = g_release_version_specification.path_to_csv_all[p_path];

			if(file_field_item)
			{
				file_name = file_field_item.file_name;
				field_name = file_field_item.field_name;
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

			let form_name = "(none)";
			let path_array = p_path.split('/');
			let description = "";
			let list_values = [];
			let data_type = "date"

			if(path_array.length > 2)
			{
				form_name = path_array[1];
				form_name = convert_form_name(form_name);
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
						<td class="td" width="140"></td>
						<td class="td p-0" colspan="5">
							<table class="table table--standard rounded-0 m-0">
                                <caption class="table-caption">
                                    Table with all possible list values for the ${form_name}'s ${p_metadata.prompt} field type.
                                </caption>
								<thead class="thead">
									<tr class="tr bg-gray-l2">
										<th class="th" colspan="5" width="1080" scope="colgroup">List Values</th>
									</tr>
								</thead>
								<thead class="thead">
									<tr class="header-level-2	
				`);

                if(value_list.length > 15)
                    {
                        list_values.push(` sticky z-index-middle" style="top: 98px;">
                                            <th class="th" width="140" scope="col">Value</th>
                                            <th class="th" width="680" scope="col">Display</th>
                                            <th class="th" width="260" scope="col">Description</th>
                                        </tr>
                                    </thead>
                                    <tbody class="tbody">`);
                    }
                    else
                    {
                        list_values.push(`">
                                <th class="th" width="140" scope="col">Value</th>
                                <th class="th" width="680" scope="col">Display</th>
                                <th class="th" width="260" scope="col">Description</th>
                            </tr>
                            </thead>
                            <tbody class="tbody">
                        `);
                    }

					for(let i= 0; i < value_list.length; i++)
					{
						list_values.push(`
									<tr class="tr">
										<td class="td" width="140">${value_list[i].value}</td>
										<td class="td" width="680">${value_list[i].display}</td>
										<td class="td" width="260">${value_list[i].description}</td>
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

			// Remove fields who do not have a form_name or if it doesn't exist
			// if (!form_name || form_name == '(none)' || form_name == '(blank)') {
			// if (!form_name || form_name.includes('none') || form_name.includes('blank')) {
			if (
				!form_name ||
				 form_name.indexOf('none') !== -1 ||
				 form_name == '(none)' ||
				 form_name.indexOf('blank') !== -1 ||
				 form_name == '(blank)'
			) {
				return;
			}

			// Adding a header per section
			if (last_form !== form_name) {
				last_form = form_name;
				p_result.push(`
                    <thead class="thead">
						<tr class="header-level-top-white" style="font-size: 17px">
							<th colspan="7" scope="colgroup" width="1350">
								${form_name}
							</th>
						</tr>
					</thead>
					<thead class="thead">
						<tr class="header-level-2 sticky z-index-middle" style="top: 57px;">
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
				<tr class="tr">
					<td class="td" width="140">${form_name}</td>
					<td class="td" width="140">${file_name}</td>
					<td class="td" width="120">${field_name}</td>
					<td class="td" width="180">${p_metadata.prompt}</td>
					<td class="td" width="380">${description}</td>
					<td class="td" width="260">${p_path}</td>
					<td style="border-right: none;" class="td" width="110">${(data_type.toLowerCase() == "textarea" || data_type.toLowerCase() == "jurisdiction")? "string": data_type}</td>
				</tr>
				${list_values.join("")}
			`);

}


function convert_form_name(p_value)
{
    if(g_is_pmss_enhanced)
    {
        let lookup = {
            '(none)': '(none)',
            'tracking': 'Tracking (q1-9)',
            'demographic': 'Demog (q10-22)',
            'outcome': 'Outcome (q23-27)',
            'cause_of_death': 'COD (q28-37)',
            'preparer_remarks': 'Preparer Remarks',
            'committee_review': 'Committee Review',
            'vro_case_determination': 'VRO Technical Assistance',
            'ije_dc': 'STEVE - Death Cert.',
            'ije_bc': 'STEVE - Birth Cert.',
            'ije_fetaldc': 'STEVE - Fetal Death Cert.',
            'amss_tracking': 'AMSS Tracking',
        }

        return lookup[p_value.toLowerCase()];
    }
    else
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

function generate_system_generated_definition_list_table()
{

    let long_name = "Maternal Mortality Review Information Application";
    let acronym = "MMRIA"

    if(g_is_pmss_enhanced)
    {
        long_name = "Pregnancy Mortality Surveillance System";
        acronym = "PMSS"
    }


	return `
			<div class="vertical-control pl-0 pr-0 col-md-12">
				<table id="system_generated_definition_list" class="table table-layout-fixed align-cell-top" style="font-size: 14px">
                    <caption class="table-caption">
                        ${long_name} (${acronym}) data dictionary table
                        with descriptions and properites of all fields contained in the MMRIA database.
                    </caption>
					<thead class="thead">
						<tr class="header-level-top-white" style="font-size: 17px">
							<th class="th" colspan="2" scope="colgroup">
								SYSTEM
							</th>
						</tr>
					</thead>
					<thead class="thead">
						<tr class="header-level-2">
							<th class="th" width="266" scope="col">Export Field</th>
							<th class="th" width="1064" scope="col">Description</th>
						</tr>
					</thead>
					<tbody>
                        <tr class="tr">
                            <td class="td" width="266" >hr_r_id</td>
                            <td class="td" width="1064">Record ID is automatically generated based on Year of Death and MMRIA Jurisdiction.</td>
                        </tr>
                        <tr class="tr">
							<td class="td" width="266" >_id</td>
							<td class="td" width="1064">Automatically generated unique random ID# that can be used to link each MMRIA record across all exported MMRIA forms and grids.</td>
						</tr>
						<tr class="tr">
							<td class="td" width="266" >_versi</td>
							<td class="td" width="1064">${acronym} application release number</td>
						</tr>
						<tr class="tr">
							<td class="td" width="266" >d_creat</td>
							<td class="td" width="1064">Record created date and time</td>
						</tr>
						<tr class="tr">
							<td class="td" width="266" >c_by</td>
							<td class="td" width="1064">Record created by user</td>
						</tr>
						<tr class="tr">
							<td class="td" width="266" >dl_updat</td>
							<td class="td" width="1064">Record last updated date and time</td>
						</tr>
						<tr class="tr">
							<td class="td" width="266" >lu_by</td>
							<td class="td" width="1064">Record last updated by user</td>
						</tr>
						<tr class="tr">
							<td class="td" width="266" >dlc_out</td>
							<td class="td" width="1064">Date record was locked</td>
						</tr>
						<tr class="tr">
							<td class="td" width="266" >lco_by</td>
							<td class="td" width="1064">Record locked by user</td>
						</tr>
						<tr class="tr">
							<td class="td" width="266" >h_state</td>
							<td class="td" width="1064">>${acronym} host site</td>
						</tr>
                           
                        <tr class="tr">
							<td class="td" width="266" >addquarter</td>
							<td class="td" width="1064">>${acronym} record added in Quarter-Year</td>
						</tr>
                          
                        <tr class="tr">
                        <td class="td" width="266" >cmpquarter</td>
                        <td class="td" width="1064">>${acronym} record reviewed by committee in Quarter-Year</td>
                    </tr>
					</tbody>
					<thead class="thead">
						<tr class="header-level-top-white" style="font-size: 17px">
							<th class="th" colspan="2" scope="colgroup">
								SYSTEM - Grid
							</th>
						</tr>
					</thead>
					<thead class="thead">
						<tr class="header-level-2">
							<th class="th" width="266"  scope="col">Export Field</th>
							<th class="th" width="1064" scope="col">Description</th>
						</tr>
					</thead>
					<tbody>
                    <tr class="tr">
                            <td class="td" width="266" >hr_r_id</td>
                            <td class="td" width="1064">Record ID is automatically generated based on Year of Death and MMRIA Jurisdiction.</td>
                        </tr>
                        <tr class="tr">
							<td class="td" width="266" >_id</td>
							<td class="td" width="1064">Automatically generated unique random ID# that can be used to link each MMRIA record across all exported MMRIA forms and grids.</td>
						</tr>
						<tr class="tr">
							<td class="td" width="266" >_record_index</td>
							<td class="td" width="1064">The record index of the grid_item</td>
						</tr>
					</tbody>
					<thead class="thead">
						<tr class="header-level-top-white" style="font-size: 17px">
							<th class="th" colspan="2" scope="colgroup">
								SYSTEM - Multiform
							</th>
						</tr>
					</thead>
					<thead class="thead">
						<tr class="header-level-2">
							<th class="th" width="266"  scope="col">Export Field</th>
							<th class="th" width="1064" scope="col">Description</th>
						</tr>
					</thead>
					<tbody>
                    <tr class="tr">
                            <td class="td" width="266" >hr_r_id</td>
                            <td class="td" width="1064">Record ID is automatically generated based on Year of Death and MMRIA Jurisdiction.</td>
                        </tr>
                        <tr class="tr">
							<td class="td" width="266" >_id</td>
							<td class="td" width="1064">Automatically generated unique random ID# that can be used to link each MMRIA record across all exported MMRIA forms and grids.</td>
						</tr>
						<tr class="tr">
							<td class="td" width="266" >_record_index</td>
							<td class="td" width="1064">The record index of the multiform</td>
						</tr>
					</tbody>
					<thead class="thead">
						<tr class="header-level-top-white" style="font-size: 17px">
							<th class="th" colspan="2" scope="colgroup">
								SYSTEM - Grid on a Multiform
							</th>
						</tr>
					</thead>
					<thead class="thead">
						<tr class="header-level-2">
							<th class="th" width="266"  scope="col">Export Field</th>
							<th class="th" width="1064" scope="col">Description</th>
						</tr>
					</thead>
					<tbody>
                    <tr class="tr">
                            <td class="td" width="266" >hr_r_id</td>
                            <td class="td" width="1064">Record ID is automatically generated based on Year of Death and MMRIA Jurisdiction.</td>
                        </tr>
                        <tr class="tr">
							<td class="td" width="266" >_id</td>
							<td class="td" width="1064">Automatically generated unique random ID# that can be used to link each MMRIA record across all exported MMRIA forms and grids.</td>
						</tr>
						<tr class="tr">
							<td class="td" width="266" >_record_index</td>
							<td class="td" width="1064">The record index of the grid_item</td>
						</tr>
						<tr class="tr">
							<td class="td" width="266" >_parent_record_index</td>
							<td class="td" width="1064">The _record_index of the associated "multiform" csv file</td>
						</tr>
					</tbody>
				</table>
			</div>
            <div class="vertical-control pl-0 pr-0 col-md-12 mt-3">
                <table class="table table-fixed-layout align-cell-top">
                    <caption class="table-caption">
                    Table containing detailed information about MMRIA form and their corresponding export file names
                    </caption>
                    <thead>
                    <tr class="header-level-top-white">
                        <th colspan="7">MMRIA Form and Export File Names</th>
                    </tr>
                    </thead>
                    <thead>
                    <tr class="header-level-2 align-middle sticky z-index-top" style="top: 45px;">
                        <th class="text-center">#</th>
                        <th>MMRIA Form Name</th>
                        <th width="60" class="text-center">Repeated Form</th>
                        <th>Export Filename(s)</th>
                        <th>Grid Filename</th>
                        <th>Grid Name</th>
                    </tr>
                    </thead>
                    <tbody>
                    <tr class="align-top">
                        <td>01)</td>
                        <td>Home Record (HR)</td>
                        <td></td>
                        <td>mmria_case_export.csv</td>
                        <td></td>
                        <td></td>
                    </tr>
                    <tr class="align-top">
                        <td>02)</td>
                        <td>Death Certificate (DC)</td>
                        <td></td>
                        <td>mmria_case_export.csv</td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            dc_causes_of_death.csv
                            </li>
                        </ul>
                        </td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            Causes of Death
                            </li>
                        </ul>
                        </td>
                    </tr>
                    <tr class="align-top">
                        <td>03)</td>
                        <td>Birth/Fetal Death Cert. - Parent (BCDC-P)</td>
                        <td></td>
                        <td>mmria_case_export.csv</td>
                        <td></td>
                        <td></td>
                    </tr>
                    <tr class="align-top">
                        <td>04)</td>
                        <td>Birth/Fetal Death Cert. - Infant/Fetal</td>
                        <td class="text-center">
                        <span class="x24 info-icon  fill-p cdc-icon-check-square-light"></span>
                        </td>
                        <td>certificate_infant_fetal_section.csv</td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            bcifs_causes_of_death.csv
                            </li>
                        </ul>
                        </td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            Causes of Fetal Death
                            </li>
                        </ul>
                        </td>
                    </tr>
                    <tr class="align-top">
                        <td>05)</td>
                        <td>Community Vital Signs (CVS)</td>
                        <td></td>
                        <td>mmria_case_export.csv</td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            c_cvs_grid.csv
                            </li>
                        </ul>
                        </td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            Community Vital Signs Hidden Grid
                            </li>
                        </ul>
                        </td>
                    </tr>
                    <tr class="align-top">
                        <td>06)</td>
                        <td>Social and Environmental Profile Form (SEP)</td>
                        <td></td>
                        <td>mmria_case_export.csv</td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            saep_members_of_household.csv
                            </li>
                            <li>
                            saep_details_of_incarcerations.csv
                            </li>
                            <li>
                            saep_details_of_arrests.csv
                            </li>
                            <li>
                            aep_social_and_medical_referrals.csv
                            </li>
                            <li>
                            ices_information_for_this_record.csv
                            </li>
                            <li>
                            saep_if_yes_specify_substances.csv
                            </li>
                        </ul>
                        </td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            Members of Household
                            </li>
                            <li>
                            Details of Incarcerations
                            </li>
                            <li>
                            Details of Arrests
                            </li>
                            <li>
                            Social and Medical Referrals
                            </li>
                            <li>
                            Sources of Social Services Information
                            </li>
                            <li>
                            If Yes, Specify Substance(s)
                            </li>
                        </ul>
                        </td>
                    </tr>
                    <tr class="align-top">
                        <td>07)</td>
                        <td>Autopsy Report (AR)</td>
                        <td></td>
                        <td>mmria_case_export.csv</td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            arrmdf_gross_findings.csv
                            </li>
                            <li>
                            arrmdf_microscopic_findings.csv
                            </li>
                            <li>
                            ar_toxicology.csv
                            </li>
                            <li>
                            ar_causes_of_death.csv
                            </li>
                        </ul>
                        </td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            Gross Findings
                            </li>
                            <li>
                            Microscopic Findings
                            </li>
                            <li>
                            Toxicology Findings
                            </li>
                            <li>
                            Coroner/Medical Examiner Causes of Death
                            </li>
                        </ul>
                        </td>
                    </tr>
                    <tr class="align-top">
                        <td>08)</td>
                        <td>Prenatal Care Record (PCR)</td>
                        <td></td>
                        <td>mmria_case_export.csv</td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            ical_procedures_before_pregnancy.csv
                            </li>
                            <li>
                            p_pre_existing_conditions_grid.csv
                            </li>
                            <li>
                            p_family_medical_history.csv
                            </li>
                            <li>
                            p_substance_use_grid.csv
                            </li>
                            <li>
                            pph_details_grid.csv
                            </li>
                            <li>
                            p_routine_monitoring.csv
                            </li>
                            <li>
                            p_other_lab_tests.csv
                            </li>
                            <li>
                            p_diagnostic_procedures.csv
                            </li>
                            <li>
                            p_problems_identified_grid.csv
                            </li>
                            <li>
                            tions_and_drugs_during_pregnancy.csv
                            </li>
                            <li>
                            elivery_hospitalizations_details.csv
                            </li>
                            <li>
                            p_medical_referrals.csv
                            </li>
                            <li>
                            p_other_sources_of_prenatal_care.csv
                            </li>
                        </ul>
                        </td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            Prior Surgical Procedures Before this Pregnancy
                            </li>
                            <li>
                            Pre-existing Conditions
                            </li>
                            <li>
                            Family Medical History
                            </li>
                            <li>
                            Evidence of Substance Use
                            </li>
                            <li>
                            Pregnancy History Details
                            </li>
                            <li>
                            Routine Monitoring
                            </li>
                            <li>
                            Other Laboratory Tests
                            </li>
                            <li>
                            Diagnostic Procedures
                            </li>
                            <li>
                            Problems Identified During the Sentinel Pregnancy
                            </li>
                            <li>
                            Prescribed Medications/Drugs
                            </li>
                            <li>
                            Pre-Delivery Hospitalization Details
                            </li>
                            <li>
                            Medical Referral Details
                            </li>
                            <li>
                            Sources of Prenatal Care Information
                            </li>
                        </ul>
                        </td>
                    </tr>
                    <tr class="align-top">
                        <td>09)</td>
                        <td>ER Visits and Hospitalizations (ERH)</td>
                        <td class="text-center">
                        <span class="x24 info-icon  fill-p cdc-icon-check-square-light"></span>
                        </td>
                        <td>sit_and_hospital_medical_records.csv</td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            evahmr_internal_transfers.csv
                            </li>
                            <li>
                            mr_physical_exam.csv
                            </li>
                            <li>
                            sychological_exam_and_assesments.csv
                            </li>
                            <li>
                            evahmr_labratory_tests.csv
                            </li>
                            <li>
                            evahmr_pathology.csv
                            </li>
                            <li>
                            evahmr_vital_signs.csv
                            </li>
                            <li>
                            evahmr_birth_attendant.csv
                            </li>
                            <li>
                            evahmr_anesthesia.csv
                            </li>
                            <li>
                            evahmr_list_of_medications.csv
                            </li>
                            <li>
                            evahmr_surgical_procedures.csv
                            </li>
                            <li>
                            evahmr_blood_product_grid.csv
                            </li>
                            <li>
                            evahmr_diagnostic_imaging_grid.csv
                            </li>
                            <li>
                            ahmr_referrals_and_consultations.csv
                            </li>
                        </ul>
                        </td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            Internal Transfers
                            </li>
                            <li>
                            Physical Examinations and Evaluations
                            </li>
                            <li>
                            Psychological Examinations and Evaluations
                            </li>
                            <li>
                            Labratory Tests
                            </li>
                            <li>
                            Pathology
                            </li>
                            <li>
                            Vital Signs
                            </li>
                            <li>
                            Birth Attendant(s)
                            </li>
                            <li>
                            Anesthesia
                            </li>
                            <li>
                            Prescribed Medications/Drugs
                            </li>
                            <li>
                            Surgical Procedures
                            </li>
                            <li>
                            Blood Products
                            </li>
                            <li>
                            Daignostic Imaging and Other Technology
                            </li>
                            <li>
                            Referrals and Consultations
                            </li>
                        </ul>            
                        </td>
                    </tr>
                    <tr class="align-top">
                        <td>10)</td>
                        <td>Other Medical Office Visits (OMV)</td>
                        <td class="text-center">
                        <span class="x24 info-icon  fill-p cdc-icon-check-square-light"></span>
                        </td>
                        <td>other_medical_office_visits.csv</td>
                        <td>
                        <ul class="list-style-decimal">
                                        <li>
                            omov_relevant_medical_history.csv
                            </li>
                            <li>
                            omov_relevant_family_history.csv
                            </li>
                            <li>
                            omov_relevant_social_history.csv
                            </li>
                            <li>
                            omov_vital_signs.csv
                            </li>
                            <li>
                            omov_labratory_tests.csv
                            </li>
                            <li>
                            tic_imaging_and_other_technology.csv
                            </li>
                            <li>
                            omov_physical_exam.csv
                            </li>
                            <li>
                            omov_referrals_and_consultations.csv
                            </li>
                            <li>
                            omov_medications.csv
                            </li>
                            <li>
                            omov_new_grid.csv
                            </li>
                        </ul>
                        </td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            Relevant Medical History
                            </li>
                            <li>
                            Relevant Family History
                            </li>
                            <li>
                            Relevant Social History
                            </li>
                            <li>
                            Vital Signs
                            </li>
                            <li>
                            Labratory Tests
                            </li>
                            <li>
                            Daignostic Imaging and Other Technology
                            </li>
                            <li>
                            Physical Examinations
                            </li>
                            <li>
                            Referrals and Consultations
                            </li>
                            <li>
                            Prescribed Medications/Drugs
                            </li>
                            <li>
                            Visit Summary
                            </li>
                        </ul>
                        </td>
                    </tr>
                    <tr class="align-top">
                        <td>11)</td>
                        <td>Medical Transport (MT)</td>
                        <td class="text-center">
                        <span class="x24 info-icon fill-p cdc-icon-check-square-light"></span>
                        </td>
                        <td>medical_transport.csv</td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            mt_transport_vital_signs.csv
                            </li>
                        </ul>
                        </td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>Transport Vital Signs</li>
                        </ul>
                        </td>
                    </tr>
                    <tr class="align-top">
                        <td>12)</td>
                        <td>Mental Health Profile Form (MHP)</td>
                        <td></td>
                        <td>mmria_case_export.csv</td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            xisting_mental_health_conditions.csv
                            </li>
                            <li>
                            unmented_mental_health_conditions.csv
                            </li>
                        </ul>
                        </td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            Documented Preexisting Mental Health Conditions
                            </li>
                            <li>
                            Were There Documented Screenings and Referrals
                            </li>
                        </ul>
                        </td>
                    </tr>
                    <tr class="align-top">
                        <td>13)</td>
                        <td>Informant Interviews (II)</td>
                        <td class="text-center">
                        <span class="x24 info-icon fill-p cdc-icon-check-square-light"></span>
                        </td>
                        <td><ul class='pl-0' style='list-style-type: none;'><li>informant_interviews.csv</li><li>informant-interview-plaintext.txt</li></ul></td>
                        <td></td>
                        <td></td>
                    </tr>
                    <tr class="align-top">
                        <td>14)</td>
                        <td>Case Narrative Form (CN)</td>
                        <td></td>
                        <td><ul class='pl-0' style='list-style-type: none;'><li>mmria_case_export.csv</li><li>case-narrative-plaintext.txt</li></ul></td>
                        <td></td>
                        <td></td>
                    </tr>
                    <tr class="align-top">
                        <td>15)</td>
                        <td>Committee Decisions Form (CDF)</td>
                        <td></td>
                        <td>mmria_case_export.csv</td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            determination_of_causes_of_death.csv
                            </li>
                            <li>
                            cr_critical_factors_worksheet.csv
                            </li>
                            <li>
                            cr_recommendations_of_committee.csv
                            </li>
                        </ul>
                        </td>
                        <td>
                        <ul class="list-style-decimal">
                            <li>
                            Committee Determination of Cause(s) of Death
                            </li>
                            <li>
                            Contributing Factors and Recommendations for Action
                            </li>
                            <li>
                            Recommendations for Action
                            </li>
                        </ul>
                        </td>
                    </tr>
                    </tbody>
                    <tfoot>
                    <tr>
                        <td class="td" colspan="7">
                        <span style="font-weight: 600">Please Note:</span>
                        <ul style="margin-left: 0px;" class="list-style-decimal">
                            <li>
                            MMRIA primary data export file: [mmria_case_export.csv], includes fields from ALL non-repeated forms.
                            </li>
                            <li>
                            Repeated Forms and Grids are exported as separate files, for example [medical_transport.csv, informant_interviews.csv, etc.].
                            </li>
                        </ul>
                        </td>
                    </tr>
                    </tfoot>
                </table>
            </div>
			`;
}
