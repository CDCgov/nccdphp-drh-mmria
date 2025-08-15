function string_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx)
{
    let styleObject = g_default_ui_specification.form_design[p_dictionary_path.substring(1)];
    let pDescription = p_metadata.description;
    let pValidationDescript = p_metadata.validation_description;

    let other_specify = g_other_specify_lookup[p_dictionary_path.substr(1)];

    let visibility_html = 'display:block;';

    if(other_specify!= null)
    {
        //let object_path = `g_data.${other_specify.list.replace(/\//g,".")}`;
        
        const target_index = other_specify.list.lastIndexOf("/");
        const target_name = other_specify.list.substr(target_index).replace("/",".");
        const proper_index = p_object_path.lastIndexOf(".");

        let object_path = p_object_path.substring(0,proper_index) + target_name;
        let other_data = eval(object_path);


        const check_is_number = parseInt(other_specify.value);
        if(Array.isArray(other_data))
        {
            if(isNaN(check_is_number))
            {
                if(other_data.indexOf(other_specify.value) > -1)
                    {
                        visibility_html = 'display:block;';
                    }
                    else
                    {
                        visibility_html = 'display:none;';
                    }
            }
            else
            {
                if(other_data.indexOf(check_is_number) > -1)
                {
                    visibility_html = 'display:block;';
                }
                else
                {
                    visibility_html = 'display:none;';
                }
            }
            
        }
        else
        {
            if(isNaN(check_is_number))
            {
                if(other_specify.value == other_data)
                {
                    visibility_html = 'display:block;';
                }
                else
                {
                    visibility_html = 'display:none;';
                }
            }
            else
            {
                if(check_is_number == other_data)
                {
                    visibility_html = 'display:block;';
                }
                else
                {
                    visibility_html = 'display:none;';
                }
            }
        }
    }

    if(p_dictionary_path == "/death_certificate/certificate_identification/dmaiden")
    {
        if(g_data.created_by != "vitals-import")
        {
            visibility_html = 'display:none;';
        }
    }


    let prompt = p_metadata.prompt;
    if
    (
        p_metadata.type.toLowerCase() == 'string' &&
        p_metadata.max_length != null &&
        parseInt(p_metadata.max_length) > 0 &&
        p_metadata.is_display_field_length != null &&
        p_metadata.is_display_field_length == true
    )
    {
        let is_highlight_border = false;

        if
        (

            p_data != null &&
            p_data.toString().length >= parseInt(p_metadata.max_length) 
        )
        {
            is_highlight_border = true;
            prompt += ` <span style='color: #BB6C49;'>(Max ${p_metadata.max_length} characters)</span>`
        }
        else
        {
            prompt += ` (Max ${p_metadata.max_length} characters)`
        }


        
    }

    p_result.push(`<div id="${convert_object_path_to_jquery_id(p_object_path)}" class="form-control-outer" mpath="${p_metadata_path}" style="${visibility_html}">`);
        p_result.push(`
            <label for="${convert_object_path_to_jquery_id(p_object_path)}_control"
                   ${styleObject ? `style="${get_style_string(styleObject.prompt.style)}"` : ''}
                   ${pDescription && pDescription.length > 0 ? `rel="tooltip" data-original-title="${pDescription.replace(/'/g, "\\'")}"` : ''}
                   ${pValidationDescript && pValidationDescript.length > 0 ? `validation-tooltip="${p_validation_description.replace(/'/g, "\\'")}"` : ''}>
                ${prompt}
                ${render_data_analyst_dictionary_link
                    (
                        p_metadata, 
                        p_dictionary_path
                    )}
            </label>
        `);

        page_render_create_input(p_result, p_metadata, p_data, p_metadata_path, p_object_path, p_dictionary_path, p_ctx);



        
    p_result.push(`</div>`);
    
}


function render_data_analyst_dictionary_link
(
    p_metadata,
    p_dictionary_path
)
{
    if(g_is_data_analyst_mode)
    {
        return `
        <button
            aria-label="Tooltip"
            class="info-icon anti-btn x20 fill-p cdc-icon-info-circle-solid ml-1"
            data-toggle="tooltip" 
            data-placement="bottom"
            onclick="on_dictionary_lookup_click('${p_dictionary_path}')" >
        </button>
    `;
    }
    else if
    (
        window.location.pathname == "/abstractorDeidentifiedCase" &&
        p_metadata.committee_description != null &&
        p_metadata.committee_description != ""

    )
    {
        return `
        <button 
            class="info-icon anti-btn x20 fill-p cdc-icon-info-circle-solid ml-1"
            aria-label="Tooltip"
            data-toggle="tooltip" 
            data-placement="bottom"
            onblur="g_set_data_object_from_path(null, null,'${p_dictionary_path}')"
            onclick="on_abstractor_committee_dictionary_lookup_click('${p_dictionary_path}')" >
        </button>
    `;
    }
    else
    {
        return ``;
    }

}


let g_release_version_specification = null;

async function on_dictionary_lookup_click(p_path)
{
    if(g_release_version_specification == null)
    {

        response = await $.ajax({
            url: `${location.protocol}//${location.host}/api/metadata/version_specification-${g_release_version}`
        });

        g_release_version_specification = response;
    }
	

    const result = []
    render_search_result(result, p_path.toLowerCase());

    await $mmria.data_dictionary_dialog_show
    (
        `${selected_dictionary_info.form_name} - ${selected_dictionary_info.field_name}`,
        result.join("")
    );
}



async function on_abstractor_committee_dictionary_lookup_click(p_path)
{
    if(g_release_version_specification == null)
    {

        response = await $.ajax({
            url: `${location.protocol}//${location.host}/api/metadata/version_specification-${g_release_version}`
        });

        g_release_version_specification = response;
    }
	

    const result = []
    render_search_result(result, p_path.toLowerCase(), true);

    await $mmria.committee_description_dialog_show
    (
        `${selected_dictionary_info.form_name} - ${selected_dictionary_info.field_name}`,
        result.join("")
    );
}


function render_search_result(p_result, p_path, p_is_abstractor_committee)
{
	selected_dictionary_info = {};
	render_search_result_item(p_result, g_metadata, "", null, p_path.toLowerCase(), p_is_abstractor_committee);
}




let last_form = null;
let selected_dictionary_info = {};

function render_search_result_item(p_result, p_metadata, p_path, p_selected_form, p_search_text, p_is_abstractor_committee)
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

					render_search_result_item(p_result, item, p_path + "/" + item.name.toLowerCase(), p_selected_form, p_search_text, p_is_abstractor_committee);
				}
			}
			else
			{
				if(p_metadata.name.toLowerCase() == p_selected_form.toLowerCase())
				{
					for(let i = 0; i < p_metadata.children.length; i++)
					{
						let item = p_metadata.children[i];

						render_search_result_item(p_result, item, p_path + "/" + item.name.toLowerCase(), p_selected_form, p_search_text, p_is_abstractor_committee);
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
					render_search_result_item(p_result, item, p_path + "/" + item.name.toLowerCase(), p_selected_form, p_search_text, p_is_abstractor_committee);
				}
				else if(item.type.toLowerCase() == "form")
				{
					render_search_result_item(p_result, item, p_path + "/" + item.name.toLowerCase(), p_selected_form, p_search_text, p_is_abstractor_committee);
				}
				
			}
			break;

		case "group":
		case "grid":
			for(let i = 0; i < p_metadata.children.length; i++)
			{
				let item = p_metadata.children[i];
				render_search_result_item(p_result, item, p_path + "/" + item.name.toLowerCase(), p_selected_form, p_search_text, p_is_abstractor_committee);
			}
			break;

        case "chart":
        case "label":
        case "button":
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

			if(p_path != p_search_text)
			{
                return;
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


            selected_dictionary_info.form_name = form_name;
            selected_dictionary_info.field_name = p_metadata.prompt;

			if(p_metadata.description != null)
			{
				description = p_metadata.description;
			}

            if
            (
                p_is_abstractor_committee != null &&
                p_is_abstractor_committee == true
            )
            {

                p_result.push(`
                    
                            <table class="table table--standard rounded-0 mb-3" style="font-size: 14px" >
                                <thead class="thead">
                                    <tr class="tr bg-gray-l2">
                                        <th class="th" width="1080" scope="colgroup">Committee Decisions Form - Note</th>
                                    </tr>
                                </thead>
                                <tbody class="tbody">	
                                <tr class="tr">
                                    <td class="td">${p_metadata.committee_description}</td>
                                </tr>
                                </tbody>
                            </table>
                        
                `);


                return;
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
								<thead class="thead">
									<tr class="tr bg-gray-l2">
										<th class="th" colspan="5" width="1080" scope="colgroup">List Values</th>
									</tr>
								</thead>
								<thead class="thead">
									<tr class="tr bg-gray-l2">
										<th class="th" width="140" scope="col">Value</th>
										<th class="th" width="680" scope="col">Display</th>
										<th class="th" width="260" scope="col">Description</th>
									</tr>
								</thead>
								<tbody class="tbody">	
				`);
                    
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
			) 
            {
				return;
			}


			p_result.push(`
				<tr class="tr">
					<td class="td" width="140">${form_name}</td>
					<td class="td" width="140">${file_name}</td>
					<td class="td" width="120">${field_name}</td>
					<td class="td" width="180">${p_metadata.prompt}</td>
					<td class="td" width="380">${description}</td>
					<td class="td" width="260">${p_path}</td>
					<td class="td" width="110">${(data_type.toLowerCase() == "textarea" || data_type.toLowerCase() == "jurisdiction")? "string": data_type}</td>
				</tr>
				${list_values.join("")}
			`);

			break;

	}
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

/*
function safe_decodeURI(value)
{
    let result = value;

    try
    {
        result = decodeURI(value);
    }
    catch(e)
    {
        result = decodeURI(encodeURI(value));
    }

    return result;
}
*/