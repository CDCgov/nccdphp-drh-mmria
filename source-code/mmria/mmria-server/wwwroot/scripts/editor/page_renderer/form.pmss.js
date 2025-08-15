function form_render(
	p_result,
	p_metadata,
	p_data,
	p_ui,
	p_metadata_path,
	p_object_path,
	p_dictionary_path,
	p_is_grid_context,
	p_post_html_render,
	p_search_ctx,
	p_ctx
) {
	//~~~~~ START SETUP Concurrent Edit
	/*
        Concurrent Edit for Abstractors
        - By default, cases are read only
        - Cases becomes editable once a user checks out the case
        - Once a case is checked out by a user, that particular case is locked for 'other' users
    */
	let currently_locked_by_html = "";
	let enable_edit_disable_attribute = "";
	let undo_disable_attribute = " disabled='disabled' ";
	let save_and_continue_disable_attribute = " disabled='disabled' ";
	let save_and_finish_disable_attribute = " disabled='disabled' ";
	let delete_disable_attribute = " disabled='disabled' ";

	let case_is_locked = is_case_locked(g_data);

	if (case_is_locked) 
    {
		// do nothing for now
	} 
    else if (g_is_data_analyst_mode == null) 
    {
		//if case is checked out by ANYONE
		if (g_data_is_checked_out) 
        {
			// console.log('anyone has locked it out')
			enable_edit_disable_attribute = " disabled='disabled' "; //disabled enable edit btn
			undo_disable_attribute = ""; //enable undo btn
			save_and_continue_disable_attribute = ""; //enable save and continue btn
			save_and_finish_disable_attribute = ""; //enable save and finish btn
			delete_disable_attribute = "";
			currently_locked_by_html =
				"<i>(Currently Locked By: <b>" + g_user_name + "</b>)</i>"; //show user locked info
		}

		//if case is checked out by YOU
		if 
        (
			!is_checked_out_expired(g_data) &&
			g_data.last_checked_out_by === g_user_name
		) 
        {
			// console.log('you')
			enable_edit_disable_attribute = " disabled "; //disable enable edit btn
			currently_locked_by_html = ""; //hide user locked info
			delete_disable_attribute = "";
		}

		//if case is checked out by SOMEONE ELSE
		if 
        (
			!is_checked_out_expired(g_data) &&
			g_data.last_checked_out_by !== g_user_name
		) 
        {
			enable_edit_disable_attribute = " disabled "; //disable enable edit btn
			currently_locked_by_html =
				"<i>(Currently Locked By: <b>" +
				g_data.last_checked_out_by +
				"</b>)</i>"; //show user locked info
		}
	}
	//~~~~~ END SETUP Concurrent Edit

	if (p_metadata.cardinality == "+" || p_metadata.cardinality == "*") 
    {
		p_result.push("<section id='");
		p_result.push(p_metadata.name);
		p_result.push("_id' class='construct'>");
		p_result.push
        (
			"<header data-header='multi-form' class='construct__header content-intro' tabindex='-1'>"
        );
        
		if (g_data) {
            /*
            render_validation_error_summary(
                p_result,
                p_metadata,
                p_data,
                p_ui,
                p_metadata_path,
                p_object_path,
                p_dictionary_path,
                p_is_grid_context,
                p_post_html_render,
                p_search_ctx,
                p_ctx
            );
            */

			p_result.push
            (
				"<div class='construct__header-main position-relative row no-gutters align-items-start'>"
			);
			p_result.push("<div class='col-4 position-static'>");
			if (g_data) 
            {
				p_result.push
                (
					"<p class='construct__title h1 text-primary single-form-title' tabindex='-1'>"
				);
				p_result.push(get_header_name(g_data.tracking.admin_info.jurisdiction));
				p_result.push(`</p>`);
			}
            p_result.push(`<p><button type="button"   onclick="show_audit_click('${g_data._id}')">View Audit Log</button></p>`);

            p_result.push(" <p class='construct__info mb-0'><strong>Case Folder:</strong> ")
            if(g_data.tracking.admin_info.case_folder == "/")
            {
                p_result.push("Top Folder");
            }
            else
            {
                p_result.push(g_data.tracking.admin_info.case_folder);
        
            }
            if (g_data.tracking.admin_info.pmssno) 
            {
                p_result.push
                (
                    "&nbsp;&nbsp;&nbsp;<strong>PMSS#:</strong> " + g_data.tracking.admin_info.pmssno
                );
                
            }
            p_result.push("</p>");

			p_result.push("<p class='construct__subtitle'");

			if (p_metadata.description && p_metadata.description.length > 0) 
            {
				p_result.push("rel='tooltip' data-original-title='");
				p_result.push(p_metadata.description.replace(/'/g, "\\'"));
				p_result.push("'>");
			} 
            else 
            {
				p_result.push(">");
			}

            p_result.push(p_metadata.prompt);
            

            
            p_result.push(`</p>`);

			if (g_data.host_state && !isNullOrUndefined(g_data.host_state)) 
            {
				p_result.push(
					`<p class='construct__info mb-0'>Reporting state: <span>${g_data.host_state}</span></p>`
				);
			}

			if 
            (
				g_data.tracking.case_status &&
				!isNullOrUndefined(g_data.tracking.case_status.overall_case_status)
			) 
            {
				let current_value = g_data.tracking.case_status.overall_case_status;
				let look_up = get_metadata_value_node_by_mmria_path
                (
					g_metadata,
					"/tracking/case_status/overall_case_status",
					""
				);
				let label = current_value;
				for (let i = 0; i < look_up.values.length; i++) 
                {
					let item = look_up.values[i];
					if (item.value == current_value) {
						label = item.display;
						break;
					}
				}

				p_result.push
                (
					`<p class='construct__info mb-0'>Case Status: <span>${label}</span></p>`
				);
			}

			if (g_data.date_created && !isNullOrUndefined(g_data.date_created)) 
            {
				let date_part_display_value = convert_datetime_to_local_display_value(
					g_data.date_created
				);

				p_result.push(
					`<p class='construct__info mb-0'>Date created: <span>${
						g_data.created_by && g_data.created_by
					} ${date_part_display_value}</span></p>`
				);
			}

			if 
            (
				g_data.date_last_updated &&
				!isNullOrUndefined(g_data.date_last_updated)
			) 
            {
				let date_part_display_value = convert_datetime_to_local_display_value(
					g_data.date_last_updated
				);

				p_result.push(
					`<p class='construct__info mb-0'>Last server save: <span id='last_updated_span'>${
						g_data.last_updated_by && g_data.last_updated_by
					} ${date_part_display_value}</span></p>`
				);
			}

			let add_button_disable_attribute = ' disabled="disabled" ';
			if (g_data_is_checked_out) 
            {
				add_button_disable_attribute = "";
			}
			p_result.push('<div class="row no-gutters align-items-center mt-3">');
			if (!(g_is_data_analyst_mode || case_is_locked)) 
            {
				p_result.push(
					'<input path="" type="button" class="btn btn-primary" value="Add A New Record"'
				);
				p_result.push(add_button_disable_attribute);
				p_result.push(
					" onclick=\"add_new_form_click(' " +
						p_metadata_path +
						"','" +
						p_object_path +
                        "','" +
						p_dictionary_path +
						"')\" />"

				);
			}
			p_result.push
            (
				'<span class="spinner-container spinner-inline ml-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>'
			);
			p_result.push("</div>");

			p_result.push("</div>");
			p_result.push
            (
				"<div class='construct__controller col-8 row no-gutters justify-content-end'>"
			);
			p_result.push
            (
				"<div class='row no-gutters align-items-center justify-content-end'>"
			);
			p_result.push
            (
				"<span class='spinner-container spinner-inline mr-2'><span class='spinner-body text-primary'><span class='spinner'></span></span></span>"
			);
			if (!(g_is_data_analyst_mode || case_is_locked)) 
            {
				p_result.push(
                    `${currently_locked_by_html}
                    <input type="button" class="btn btn-primary ml-3" value="Enable Edit" onclick="init_inline_loader(function() { enable_edit_click() })" ${enable_edit_disable_attribute} />
                    <input type="button" class="btn btn-primary ml-3" value="Save & Continue" onclick="init_inline_loader(function() { save_form_click() })" ${save_and_continue_disable_attribute} />
                    <input type="button" class="btn btn-primary ml-3" value="Save & Finish" onclick="init_inline_loader(function() { save_and_finish_click() })" ${save_and_finish_disable_attribute} />`
                );
			}
			p_result.push("</div>");
			p_result.push("<div class='mt-4 row no-gutters justify-content-end'>");
			render_print_form_control(p_result, p_ui, p_metadata);
			p_result.push("</div>");
			p_result.push("</div> <!-- end .construct__controller -->");
			p_result.push("</div>");
		}
		p_result.push("</header> <!-- end .construct__header -->");

		p_result.push(
            `<span class="spinner-container spinner-content">
                <span class="spinner-body text-primary">
                <span class="spinner" aria-hidden="true"></span>
                <span class="spinner-info">Loading...</span>
                </span>
            </span>`
        );

		// The 'Records' Table
		p_result.push('<div class="construct__table row no-gutters search_wrapper">');
		p_result.push('<table class="table">');
		p_result.push(
            `<thead class="thead">
                <tr class="tr bg-tertiary">
                    <th class="th h4" colspan="99"  scope="colgroup">List of Records</th>
                </tr>
            </thead>`
        );



		//~~ birth_certificate_infant_fetal_section
		if (p_metadata.name === "birth_certificate_infant_fetal_section") {
			p_result.push(
                `<thead class="thead">
                    <tr class="tr">
                        <th class="th" scope="col">Record #</th>
                        <th class="th" scope="col">Record Type</th>
                        <th class="th" scope="col">Birth Order</th>
                        <th class="th" scope="col">Time of Delivery</th>
                        <th class="th" width="120" scope="col">Actions</th>
                    </tr>
                </thead>
                <tbody class="tbody">`
            );
			for (let i = 0; i < p_data.length; i++) 
            {
				let item = p_data[i];
				let url = `${window.location.pathname}#${p_ui.url_state.path_array.join(
					"/"
				)}`; // /Case#0/birth_certificate_infant_fetal_section/
				let recordType =
					g_value_to_display_lookup[`/${p_metadata.name}/record_type`][
						g_data[p_metadata.name][i].record_type
					]; // lookup

				p_result.push(
                    `<tr class="tr">
                        <td class="td">
                            <a href="${url}/${i}">View Record ${i + 1}</a>
                        </td>
                        <td class="td">${recordType}</td>
                        <td class="td">${item.birth_order}</td>
                        <td class="td">${item.record_identification!= null && item.record_identification.time_of_delivery != null ? item.record_identification.time_of_delivery: ""}</td>
                        <td class="td">
                            <button class="btn btn-primary" onclick="init_multirecord_delete_dialog('${p_object_path}[${i}]', '${p_metadata_path}', '${i}')" ${delete_disable_attribute}>Delete</button>
                            <!--<button class="btn btn-primary" onclick="g_delete_record_item('${p_object_path}[${i}]', '${p_metadata_path}', '${i}')" ${delete_disable_attribute}>Delete Record</button>-->
                        </td>
                    </tr>`
                );
			}
		}

		//~~ er_visit_and_hospital_medical_records
		else if (p_metadata.name === "er_visit_and_hospital_medical_records") 
        {
			p_result.push(
                `<thead class="thead">
                    <tr class="tr">
                        <th class="th" scope="col">Record #</th>
                        <th class="th" scope="col">Date of Arrival</th>
                        <th class="th" scope="col">Discharge Pregnancy Status</th>
                        <th class="th" width="120" scope="col">Actions</th>
                    </tr>
                </thead>
                <tbody class="tbody">`
            );
			for (let i = 0; i < p_data.length; i++) 
            {
				let item = p_data[i];
				let url = `${window.location.pathname}#${p_ui.url_state.path_array.join(
					"/"
				)}`; // /Case#0/birth_certificate_infant_fetal_section/
				let arrivalMonth =
					g_value_to_display_lookup[
						`/${p_metadata.name}/basic_admission_and_discharge_information/date_of_arrival/month`
					][
						g_data[p_metadata.name][i].basic_admission_and_discharge_information
							.date_of_arrival.month
					];
				let arrivalDay =
					g_value_to_display_lookup[
						`/${p_metadata.name}/basic_admission_and_discharge_information/date_of_arrival/day`
					][
						g_data[p_metadata.name][i].basic_admission_and_discharge_information
							.date_of_arrival.day
					];
				let arrivalYear =
					g_value_to_display_lookup[
						`/${p_metadata.name}/basic_admission_and_discharge_information/date_of_arrival/year`
					][
						g_data[p_metadata.name][i].basic_admission_and_discharge_information
							.date_of_arrival.year
					];
				let pregStatus =
					g_value_to_display_lookup[
						`/${p_metadata.name}/basic_admission_and_discharge_information/discharge_pregnancy_status`
					][
						g_data[p_metadata.name][i].basic_admission_and_discharge_information
							.discharge_pregnancy_status
					];

				p_result.push(
                    `<tr class="tr">
                        <td class="td"><a href="${url}/${i}">View Record ${i + 1}</a></td>
                        <td class="td">
                            ${
                                !isNullOrUndefined(arrivalMonth) &&
                                arrivalMonth !== "(blank)" &&
                                !isNullOrUndefined(arrivalDay) &&
                                arrivalDay !== "(blank)" &&
                                !isNullOrUndefined(arrivalYear) &&
                                arrivalYear !== "(blank)" ?
                                    `${arrivalMonth}/${arrivalDay}/${arrivalYear}` : ""
                            }
                        </td>
                        <td class="td">${pregStatus}</td>
                        <td class="td">
                            <button class="btn btn-primary" onclick="init_multirecord_delete_dialog('${p_object_path}[${i}]', '${p_metadata_path}', '${i}')" ${delete_disable_attribute}>Delete</button>
                            <!--<button class="btn btn-primary" onclick="g_delete_record_item('${p_object_path}[${i}]', '${p_metadata_path}', '${i}')" ${delete_disable_attribute}>Delete Record</button>-->
                        </td>
                    </tr>`
                );
			}
		}

		//~~ other_medical_office_visits
		else if (p_metadata.name === "other_medical_office_visits") {
			p_result.push(
                `<thead class="thead">
                    <tr class="tr">
                        <th class="th" scope="col">Record #</th>
                        <th class="th" scope="col">Date of Medical Office Visit</th>
                        <th class="th" scope="col">Visit Type</th>
                        <th class="th" scope="col">Provider Type</th>
                        <th class="th" scope="col">Pregnancy Status</th>
                        <th class="th" width="120" scope="col">Actions</th>
                    </tr>
                </thead>
                <tbody class="tbody">`
            );
			for (let i = 0; i < p_data.length; i++) {
				let item = p_data[i];
				let url = `${window.location.pathname}#${p_ui.url_state.path_array.join(
					"/"
				)}`; // /Case#0/birth_certificate_infant_fetal_section/
				let visitMonth =
					g_value_to_display_lookup[
						`/${p_metadata.name}/visit/date_of_medical_office_visit/month`
					][
						g_data[p_metadata.name][i].visit.date_of_medical_office_visit.month
					];
				let visitDay =
					g_value_to_display_lookup[
						`/${p_metadata.name}/visit/date_of_medical_office_visit/day`
					][g_data[p_metadata.name][i].visit.date_of_medical_office_visit.day];
				let visitYear =
					g_value_to_display_lookup[
						`/${p_metadata.name}/visit/date_of_medical_office_visit/year`
					][g_data[p_metadata.name][i].visit.date_of_medical_office_visit.year];
				let visitType =
					g_value_to_display_lookup[`/${p_metadata.name}/visit/visit_type`][
						g_data[p_metadata.name][i].visit.visit_type
					];
				let providerType =
					g_value_to_display_lookup[
						`/${p_metadata.name}/medical_care_facility/provider_type`
					][g_data[p_metadata.name][i].medical_care_facility.provider_type];
				let pregnancyStatus =
					g_value_to_display_lookup[
						`/${p_metadata.name}/medical_care_facility/pregnancy_status`
					][g_data[p_metadata.name][i].medical_care_facility.pregnancy_status];

				p_result.push(
                    `<tr class="tr">
                        <td class="td"><a href="${url}/${i}">View Record ${i + 1}</a></td>
                        <td class="td">
                            ${
                                !isNullOrUndefined(visitMonth) &&
                                visitMonth !== "(blank)" &&
                                !isNullOrUndefined(visitDay) &&
                                visitDay !== "(blank)" &&
                                !isNullOrUndefined(visitYear) &&
                                visitYear !== "(blank)" ?
                                    `${visitMonth}/${visitDay}/${visitYear}` : ""
                            }
                        </td>
                        <td class="td">${visitType}</td>
                        <td class="td">${providerType}</td>
                        <td class="td">${pregnancyStatus}</td>
                        <td class="td">
                            <button class="btn btn-primary" onclick="init_multirecord_delete_dialog('${p_object_path}[${i}]', '${p_metadata_path}', '${i}')" ${delete_disable_attribute}>Delete</button>
                            <!--<button class="btn btn-primary" onclick="g_delete_record_item('${p_object_path}[${i}]', '${p_metadata_path}', '${i}')" ${delete_disable_attribute}>Delete Record</button>-->
                        </td>
                    </tr>`
                );
			}
		}

		//~~ medical_transport
		if (p_metadata.name === "medical_transport") {
			p_result.push(
                `<thead class="thead">
                    <tr class="tr">
                        <th class="th" scope="col">Record #</th>
                        <th class="th" scope="col">Date of Transport</th>
                        <th class="th" scope="col" width="360">Reason for Transport</th>
                        <th class="th" width="120" scope="col">Actions</th>
                    </tr>
                </thead>
                <tbody class="tbody">`
            );
			for (let i = 0; i < p_data.length; i++) {
				let item = p_data[i];
				let url = `${window.location.pathname}#${p_ui.url_state.path_array.join("/")}`; // /Case#0/medical_transport/
				let transportMonth =
					g_value_to_display_lookup[
						`/${p_metadata.name}/date_of_transport/month`
					][g_data[p_metadata.name][i].date_of_transport.month];
				let transportDay =
					g_value_to_display_lookup[
						`/${p_metadata.name}/date_of_transport/day`
					][g_data[p_metadata.name][i].date_of_transport.day];
				let transportYear =
					g_value_to_display_lookup[
						`/${p_metadata.name}/date_of_transport/year`
					][g_data[p_metadata.name][i].date_of_transport.year];
				let transportReason = item.reason_for_transport;

				// Truncates if text length over 100
				if (transportReason.length >= 100) {
					// Then trim off anything after 100 characters
					// And add elipsis
					transportReason = transportReason.substring(0, 100) + "...";
				}

				p_result.push(
                    `<tr class="tr">
                        <td class="td"><a href="${url}/${i}">View Record ${i + 1}</a></td>
                        <td class="td">
                            ${
                                !isNullOrUndefined(transportMonth) &&
                                transportMonth !== "(blank)" &&
                                !isNullOrUndefined(transportDay) &&
                                transportDay !== "(blank)" &&
                                !isNullOrUndefined(transportYear) &&
                                transportYear !== "(blank)"?
                                    `${transportMonth}/${transportDay}/${transportYear}` : ""
                            }
                        </td>
                        <td class="td">${transportReason}</td>
                        <td class="td">
                            <button class="btn btn-primary" onclick="init_multirecord_delete_dialog('${p_object_path}[${i}]', '${p_metadata_path}', '${i}')" ${delete_disable_attribute}>Delete</button>
                            <!--<button class="btn btn-primary" onclick="g_delete_record_item('${p_object_path}[${i}]', '${p_metadata_path}', '${i}')" ${delete_disable_attribute}>Delete Record</button>-->
                        </td>
                    </tr>
                `);
			}
		}

		//~~ informant_interviews
		if (p_metadata.name === "informant_interviews") 
        {
			p_result.push(
                `<thead class="thead">
                    <tr class="tr">
                        <th class="th" scope="col">Record #</th>
                        <th class="th" scope="col">Date of Interview</th>
                        <th class="th" scope="col">Interview Type</th>
                        <th class="th" scope="col">Relationship to Deceased</th>
                        <th class="th" width="120" scope="col">Actions</th>
                    </tr>
                </thead>
                <tbody class="tbody">`
            );

			for (let i = 0; i < p_data.length; i++) 
            {
				let item = p_data[i];
				let url = `${window.location.pathname}#${p_ui.url_state.path_array.join("/")}`; // /Case#0/informant_interviews/
				let interviewMonth =
					g_value_to_display_lookup[
						`/${p_metadata.name}/date_of_interview/month`
					][g_data[p_metadata.name][i].date_of_interview.month];
				let interviewDay =
					g_value_to_display_lookup[
						`/${p_metadata.name}/date_of_interview/day`
					][g_data[p_metadata.name][i].date_of_interview.day];
				let interviewYear =
					g_value_to_display_lookup[
						`/${p_metadata.name}/date_of_interview/year`
					][g_data[p_metadata.name][i].date_of_interview.year];
				let interviewType =
					g_value_to_display_lookup[`/${p_metadata.name}/interview_type`][
						g_data[p_metadata.name][i].interview_type
					];
				let relationshipToDeceased =
					g_value_to_display_lookup[
						`/${p_metadata.name}/relationship_to_deceased`
					][g_data[p_metadata.name][i].relationship_to_deceased];

				p_result.push(
                    `<tr class="tr">
                        <td class="td"><a href="${url}/${i}">View Record ${i + 1}</a></td>
                        <td class="td">
                            ${
                                !isNullOrUndefined(interviewMonth) &&
                                interviewMonth !== "(blank)" &&
                                !isNullOrUndefined(interviewDay) &&
                                interviewDay !== "(blank)" &&
                                !isNullOrUndefined(interviewYear) &&
                                interviewYear !== "(blank)" ?
                                    `${interviewMonth}/${interviewDay}/${interviewYear}` : ""
                            }
                        </td>
                        <td class="td">${interviewType}</td>
                        <td class="td">${relationshipToDeceased}</td>
                        <td class="td">
                            <button class="btn btn-primary" onclick="init_multirecord_delete_dialog('${p_object_path}[${i}]', '${p_metadata_path}', '${i}')" ${delete_disable_attribute}>Delete</button>
                            <!--<button class="btn btn-primary" onclick="g_delete_record_item('${p_object_path}[${i}]', '${p_metadata_path}', '${i}')" ${delete_disable_attribute}>Delete Record</button>-->
                        </td>
                    </tr>`
                );
			}
		}

		p_result.push("</tbody>");
		p_result.push("</table>");
		p_result.push("</div>");
		p_result.push("</section> <!-- end.construct -->");

		if (p_ui.url_state.path_array.length > 2) 
        {
			var data_index = parseInt(p_ui.url_state.path_array[2]);
			var form_item = p_data[data_index];

			p_result.push("<section id='");
			p_result.push(p_metadata.name);
			p_result.push("' class='construct'>");

			p_result.push("<header data-header='multi-single-form' class='construct__header'>");

			render_validation_error_summary(
				p_result,
				p_metadata,
				p_data,
				p_ui,
				p_metadata_path,
				p_object_path,
				p_dictionary_path,
				p_is_grid_context,
				p_post_html_render,
				p_search_ctx,
				p_ctx
			);

			p_result.push("<div class='construct__header-main position-relative row no-gutters align-items-start'>");
			p_result.push("<div class='col-4 position-static'>");
			if (g_data) 
            {
				p_result.push("<p class='construct__title h1 text-primary single-form-title' tabindex='-1'>");
				p_result.push(get_header_name(g_data.tracking.admin_info.jurisdiction));
				p_result.push(`</p>`);
			}
            p_result.push(`<p><button type="button"  onclick="show_audit_click('${g_data._id}')">View Audit Log</button></p>`);
			
            p_result.push(" <p class='construct__info mb-0'><strong>Case Folder:</strong> ")
            if(g_data.tracking.admin_info.case_folder == "/")
            {
                p_result.push("Top Folder");
            }
            else
            {
                p_result.push(g_data.tracking.admin_info.case_folder);
        
            }
            if (g_data.tracking.admin_info.pmssno) 
            {
                p_result.push
                (
                    "&nbsp;&nbsp;&nbsp;<strong>PMSS#:</strong> " + g_data.tracking.admin_info.pmssno
                );
                
            }
            p_result.push("</p>");

			p_result.push("<p class='construct__subtitle'");

			if (p_metadata.description && p_metadata.description.length > 0) 
            {
				p_result.push("rel='tooltip' data-original-title='");
				p_result.push(p_metadata.description.replace(/'/g, "\\'"));
                p_result.push("'>");
                
			} 
            else 
            {
				p_result.push(">");
			}

            p_result.push(p_metadata.prompt);
            p_result.push(" <span>(Record " + (data_index + 1) + ")<span>");
            //p_result.push("</p>");

			if (g_data.host_state && !isNullOrUndefined(g_data.host_state))
            {
				p_result.push(
					`<p class='construct__info mb-0'>Reporting state: <span>${g_data.host_state}</span></p>`
				);
			}

			if (g_data.date_created && !isNullOrUndefined(g_data.date_created)) {
				let date_part_display_value = convert_datetime_to_local_display_value(g_data.date_created);

				p_result.push(
					`<p class='construct__info mb-0'>Date created: <span>${
						g_data.created_by && g_data.created_by
					} ${date_part_display_value}</span></p>`
				);
			}

			if (
				g_data.date_last_updated &&
				!isNullOrUndefined(g_data.date_last_updated)
			) 
            {
				let date_part_display_value = convert_datetime_to_local_display_value(g_data.date_last_updated);

				p_result.push(
					`<p class='construct__info mb-0'>Last server save: <span id='last_updated_span'>${
						g_data.last_updated_by && g_data.last_updated_by
					} ${date_part_display_value}</span></p>`
				);
			}

			p_result.push("</div>");
			p_result.push(
				"<div class='construct__controller col-8 row no-gutters justify-content-end'>"
			);
			p_result.push(
				"<div class='row no-gutters align-items-center justify-content-end'>"
			);
			p_result.push(
				"<span class='spinner-container spinner-inline mr-2'><span class='spinner-body text-primary'><span class='spinner'></span></span></span>"
			);

			if (!(g_is_data_analyst_mode || case_is_locked)) 
            {
				p_result.push(
                    `${currently_locked_by_html}
                    <input type="button" class="btn btn-primary ml-3" value="Enable Edit" onclick="init_inline_loader(function() { enable_edit_click() })" ${enable_edit_disable_attribute} />
                    <input type="button" class="btn btn-primary ml-3" value="Save & Continue" onclick="init_inline_loader(function() { save_form_click() })" ${save_and_continue_disable_attribute} />
                    <input type="button" class="btn btn-primary ml-3" value="Save & Finish" onclick="init_inline_loader(function() { save_and_finish_click() })" ${save_and_finish_disable_attribute} />`
                );
			}
			p_result.push("</div>");
			p_result.push("<div class='mt-4 row no-gutters justify-content-end'>");
			render_print_form_control(p_result, p_ui, p_metadata);
			p_result.push("</div>");
			p_result.push("<div class='mt-4 row no-gutters justify-content-end'>");
			if (!(g_is_data_analyst_mode || case_is_locked)) 
            {
				p_result.push(
					`<input type='button' class='btn btn-primary' value='Undo' onclick='init_inline_loader(function() { undo_click() })' ${undo_disable_attribute}/>`
				);
			}
			p_result.push("</div>");
			p_result.push("</div>");
			p_result.push("</div> <!-- end .construct__controller -->");
            p_result.push("</div>");
            
            var url = p_ui.url_state.path_array[0] + "/" + p_metadata.name;
            p_result.push(`<a class="construct href="#"  href="#${url}"><span class="construct__icon x16 fill-p cdc-icon-chevron-right"></span>Back to List of Records</a>`);

            p_result.push("</header>");

            p_result.push("<div class='construct__body' tabindex='-1'>");
            
			let height_attribute = get_form_height_attribute_height(p_metadata, p_dictionary_path);

			p_result.push(
				`<div class='construct-output' style='height:${height_attribute}'>`
			);
			for (var i = 0; form_item && i < p_metadata.children.length; i++) 
            {
				var child = p_metadata.children[i];
				//var item = p_data[data_index][child.name];

				if (form_item[child.name] || form_item[child.name] == 0) 
                {

				} 
                else 
                {
					form_item[child.name] = create_default_object(child, {})[child.name];
				}

				if (child.type == "group") 
                {
					Array.prototype.push.apply(
						p_result,
						page_render(
							child,
							form_item[child.name],
							p_ui,
							p_metadata_path + ".children[" + i + "]",
							p_object_path + "[" + data_index + "]." + child.name,
							p_dictionary_path + "/" + child.name,
							false,
							p_post_html_render,
							p_search_ctx,
							{ form_index: data_index }
						)
					);
				} 
                else 
                {
					Array.prototype.push.apply(
						p_result,
						page_render(
							child,
							form_item[child.name],
							p_ui,
							p_metadata_path + ".children[" + i + "]",
							p_object_path + "[" + data_index + "]." + child.name,
							p_dictionary_path + "/" + child.name,
							false,
							p_post_html_render,
							p_search_ctx,
							{ form_index: data_index }
						)
					);
				}
			}
			p_result.push("</div>");
			p_result.push("</div>");

			p_result.push("<div class='construct__footer'>");
			if (!(g_is_data_analyst_mode || case_is_locked)) {
				p_result.push(`
                    <input type='button' class='btn btn-primary ml-3' value='Save & Continue' onclick='init_inline_loader(save_form_click)' ${save_and_continue_disable_attribute}/>
                        <input type='button' class='btn btn-primary ml-3' value='Save & Finish' onclick='init_inline_loader(save_and_finish_click)' ${save_and_finish_disable_attribute}/>
                    <input type='button' class='btn btn-primary ml-3' value='Undo' onclick='init_inline_loader(undo_click)' ${undo_disable_attribute}/>
                `);
			}
			p_result.push(
				'<span class="spinner-container spinner-inline ml-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>'
			);
			p_result.push("</div>");
			p_result.push("</section>");
		}
	} 
    else 
    {
        if(p_metadata.name == "tracking")
        {
            p_post_html_render.push("$global.case_document_begin_edit();")
        }

		p_result.push("<section id='");
		p_result.push(p_metadata.name);
		p_result.push("_id' class='construct' ");
		p_result.push(" style='' class='construct'>");
		p_result.push("<div data-header='single-form' class='construct__header'>");

		render_validation_error_summary(
			p_result,
			p_metadata,
			p_data,
			p_ui,
			p_metadata_path,
			p_object_path,
			p_dictionary_path,
			p_is_grid_context,
			p_post_html_render,
			p_search_ctx,
			p_ctx
		);

		p_result.push("<div class='construct__header-main position-relative row no-gutters align-items-start'>");
		p_result.push("<div class='col-4 position-static'>");
		if (g_data) 
        {
			p_result.push("<p class='construct__title h1 text-primary single-form-title' tabindex='-1'>");
			p_result.push(get_header_name(g_data.tracking.admin_info.jurisdiction));
			p_result.push(`</p>`);
		}

        p_result.push(`<p><button type="button"  onclick="show_audit_click('${g_data._id}')">View Audit Log</button></p>`);

        p_result.push(" <p class='construct__info mb-0'><strong>Case Folder:</strong> ")
        if(g_data.tracking.admin_info.case_folder == "/")
        {
            p_result.push("Top Folder");
        }
        else
        {
            p_result.push(g_data.tracking.admin_info.case_folder);
    
        }
        if (g_data.tracking.admin_info.pmssno) 
        {
            p_result.push
            (
                "&nbsp;&nbsp;&nbsp;<strong>PMSS#:</strong> " + g_data.tracking.admin_info.pmssno
            );
            
        }
        p_result.push("</p>");

		p_result.push("<p class='construct__subtitle'");
		if (p_metadata.description && p_metadata.description.length > 0) {
			p_result.push("rel='tooltip' data-original-title='");
			p_result.push(p_metadata.description.replace(/'/g, "\\'"));
			p_result.push("'>");
		} else {
			p_result.push(">");
		}
		p_result.push(p_metadata.prompt);
		p_result.push("</p>");

		if (g_data.host_state && !isNullOrUndefined(g_data.host_state)) {
			p_result.push(`<p class='construct__info mb-0'>Reporting state: <span>${g_data.host_state}</span></p>`);
		}

		if (
			g_data.tracking.case_status &&
			!isNullOrUndefined(g_data.tracking.case_status.overall_case_status)
		) {
			let current_value = g_data.tracking.case_status.overall_case_status;
			let look_up = get_metadata_value_node_by_mmria_path(
				g_metadata,
				"/tracking/case_status/overall_case_status",
				""
			);
			let label = current_value;
			for (let i = 0; i < look_up.values.length; i++) {
				let item = look_up.values[i];
				if (item.value == current_value) {
					label = item.display;
					break;
				}
			}

			p_result.push(
				`<p class='construct__info mb-0'>Case Status: <span>${label}</span></p>`
			);
		}

		if (g_data.date_created && !isNullOrUndefined(g_data.date_created)) {
			let date_part_display_value = convert_datetime_to_local_display_value(g_data.date_created);

			p_result.push(
				`<p class='construct__info mb-0'>Date created: <span>${
					g_data.created_by && g_data.created_by
				} ${date_part_display_value}</span></p>`
			);
		}

		if (
			g_data.date_last_updated &&
			!isNullOrUndefined(g_data.date_last_updated)
		) {
			let date_part_display_value = convert_datetime_to_local_display_value(
				g_data.date_last_updated
			);

			p_result.push(
				`<p class='construct__info mb-0'>Last server save: <span id='last_updated_span'>${
					g_data.last_updated_by && g_data.last_updated_by
				} ${date_part_display_value}</span></p>`
			);
		}

		p_result.push("</div>");
		p_result.push(
			"<div class='construct__controller col-8 row no-gutters justify-content-end'>"
		);
		p_result.push(
			"<div class='row no-gutters align-items-center justify-content-end'>"
		);
		p_result.push(
			"<span class='spinner-container spinner-inline mr-2'><span class='spinner-body text-primary'><span class='spinner'></span></span></span>"
		);

		if (!(g_is_data_analyst_mode || case_is_locked)) {
			p_result.push(
                `${currently_locked_by_html}
                <input type="button" class="btn btn-primary ml-3" value="Enable Edit" onclick="init_inline_loader(function() { enable_edit_click() })" ${enable_edit_disable_attribute} />
                <input type="button" class="btn btn-primary ml-3" value="Save & Continue" onclick="init_inline_loader(function() { save_form_click() })" ${save_and_continue_disable_attribute} />
                <input type="button" class="btn btn-primary ml-3" value="Save & Finish" onclick="init_inline_loader(function() { save_and_finish_click() })" ${save_and_finish_disable_attribute} />`
            );
		}
		p_result.push("</div>");
		p_result.push("<div class='mt-4 row no-gutters justify-content-end'>");
		render_print_form_control(p_result, p_ui, p_metadata);
		p_result.push("</div>");
		p_result.push("<div class='mt-4 row no-gutters justify-content-end'>");
		if (!(g_is_data_analyst_mode || case_is_locked)) {
			p_result.push(
				`<input type='button' class='btn btn-primary' value='Undo' onclick='init_inline_loader(function() { undo_click() })' ${undo_disable_attribute}/>`
			);
		}
		p_result.push("</div>");
		p_result.push("</div>");
		p_result.push("</div> <!-- end .construct__controller -->");
		p_result.push("</div>");
		p_result.push("</div> <!-- end .construct__header -->");

		p_result.push(
            `<span class="spinner-container spinner-content mb-3">
                <span class="spinner-body text-primary">
                <span class="spinner" aria-hidden="true"></span>
                <span class="spinner-info">Loading...</span>
                </span>
            </span>`
        );

		p_result.push("<div class='construct__body' tabindex='-1'>");

		let height_attribute = get_form_height_attribute_height(
			p_metadata,
			p_dictionary_path
		);

		p_result.push(
			`<div class='construct-output' style='height:${height_attribute}'>`
		);

		if (g_data && p_metadata.name !== "case_narrative") {
			//~~~ # RENDERS EACH INIDVIDUAL FIELD
			for (var i = 0; i < p_metadata.children.length; i++) {
				var child = p_metadata.children[i];

				if (p_data[child.name] || p_data[child.name] == 0) {
					// do nothing
				} else {
					p_data[child.name] = create_default_object(child, {})[child.name];
				}
				Array.prototype.push.apply(
					p_result,
					page_render(
						child,
						p_data[child.name],
						p_ui,
						p_metadata_path + ".children[" + i + "]",
						p_object_path + "." + child.name,
						p_dictionary_path + "/" + child.name,
						false,
						p_post_html_render,
						p_search_ctx
					)
				);
			}
		} 
        else if (g_data && p_metadata.name === "case_narrative") 
        {
			let noteTitle = null;
			let noteUrl = null;
			let notes = null;

			//~~~ # RENDER THE CASE NARRATIVE TEXTAREA
			for (var i = 0; i < p_metadata.children.length; i++) 
            {
				var child = p_metadata.children[i];

				if (p_data[child.name] || p_data[child.name] == 0) 
                {
					// do nothing
				} 
                else 
                {
					p_data[child.name] = create_default_object(child, {})[child.name];
				}

				Array.prototype.push.apply
                (
					p_result,
					page_render
                    (
						child,
						p_data[child.name],
						p_ui,
						p_metadata_path + ".children[" + i + "]",
						p_object_path + "." + child.name,
						p_dictionary_path + "/" + child.name,
						false,
						p_post_html_render,
						p_search_ctx
					)
				);
			}

			//~~~~~~~ QUEUE the WEIRDNESS
			// A bit weird/hacky but works
			// Because label is created dynamically, it doesn't exist until a few miliseconds later after DOM render
			// The logic below runs aand scans on a timed interval every 25ms...
			// It then stops after the label finally exists in the DOM
			// Finally it sets the label HTML to the new version (see below)
			let scan_for_narrative_label = setInterval(changeNarrativeLabel, 25);

			function changeNarrativeLabel() 
            {
				let caseNarrativeLabel = document.querySelectorAll
                (
					"#g_data_case_narrative_case_opening_overview"
				)[0].children[0];

				// Checks if the label exists
				if (!isNullOrUndefined(caseNarrativeLabel)) 
                {
					// Insert new HTML/TEXT
                    caseNarrativeLabel.innerHTML =`<h3 class="h3 mb-2 mt-0 font-weight-bold">Case Narrative ${g_is_data_analyst_mode? render_data_analyst_dictionary_link
                        (
                            p_metadata, 
                            "/case_narrative/case_opening_overview"
                        ) : ""} </h3><p class="mb-0" style="line-height: normal">Use the pre-fill text below, and copy and paste from Reviewer's Notes below to create a comprehensive case narrative. Whatever you type here is what will be printed in the Print Version.</p>`;
					// Stop the scanning
					clearInterval(scan_for_narrative_label);
				}
			}
			//~~~~~~~ END the WEIRDNESS

			//~~~ Introduction text
			p_result.push(`<p class="mt-4">The Reviewer’s Notes below come from each individual form. To make edits, navigate to each form. This content is included for reference in order to complete the Case Narrative at the top of the page.</p>`);

			//~~~~~~~ LOOPS through each key and prints out data unique to that form
			for (let key in g_data) {
				//~~~ death_certificate
				if (key === "death_certificate") {
					noteTitle = "Death Certificate";
					noteUrl = window.location.hash.replace(p_metadata.name, key);
					notes = g_data[key];

					p_result.push(
                        `<h3 class="font-weight-bold mb-2">${noteTitle}</h3>
                        <p class="mb-2 font-weight-bold">Reviewer's Notes from <a href="${noteUrl}#content">Case Form</a></p>
                        <p>
                            ${
                                notes == null || (notes.reviewer_note != null && notes.reviewer_note.length < 1) ? "<em>No data entered</em>" : textarea_control_replace_return_with_br(notes.reviewer_note)
                            }
                        </p>`
                    );
				}

				//~~~ birth_fetal_death_certificate_parent
				else if (key === "birth_fetal_death_certificate_parent") {
					noteTitle = "Birth/Fetal Death Certificate- Parent Section";
					noteUrl = window.location.hash.replace(p_metadata.name, key);
					notes = g_data[key];

					p_result.push(
                        `<h3 class="font-weight-bold mb-2">${noteTitle}</h3>
                        <p class="mb-2 font-weight-bold">Reviewer's Notes from <a href="${noteUrl}#content">Case Form</a></p>
                        <p>
                            ${
                                notes == null || (notes.reviewer_note != null && notes.reviewer_note.length < 1) ? "<em>No data entered</em>" : textarea_control_replace_return_with_br(notes.reviewer_note)
                            }
                        </p>`
                    );
				}

				// MULTI RECORD FORM
				//~~~ birth_certificate_infant_fetal_section
				else if (key === "birth_certificate_infant_fetal_section") {
					noteTitle = "Birth/Fetal Death Certificate- Infant/Fetal Section";
					noteUrl = window.location.hash.replace(p_metadata.name, key);
					notes = g_data[key];

					p_result.push(
                        `<h3 class="font-weight-bold mb-2">${noteTitle}</h3>
                        <ul class="list-unstyled">`
                    );
					if (notes)
						for (let i = 0; i < notes.length; i++) {
							let recordType =
								g_value_to_display_lookup[`/${key}/record_type`][
									g_data[key][i].record_type
								];
							let birthOrder = g_data[key][i].birth_order;
							let timeOfDelivery =
								g_data[key][i].record_identification.time_of_delivery;

							p_result.push(
                                `<li>
                                    <p class="mb-2 font-weight-bold">
                                        Reviewer's Notes from <a href="${noteUrl}/${i}">Case Form</a>: Record ${i + 1}
                                        ${!isNullOrUndefined(recordType) ? `– ${recordType}` : ''}
                                        ${!isNullOrUndefined(birthOrder) ? `– ${birthOrder}` : ''}
                                        ${!isNullOrUndefined(timeOfDelivery) ? `– ${timeOfDelivery}` : ""}
                                    </p>
                                    <p>
                                        ${!notes[i].reviewer_note ? "<em>No data entered</em>" : textarea_control_replace_return_with_br(notes[i].reviewer_note)}
                                    </p>
                                </li>`
                            );
						}
					p_result.push(`</ul>`);
				}

				// SINGLE FORM
				//~~~ autopsy_report
				else if (key === "autopsy_report") {
					noteTitle = "Autopsy Report";
					noteUrl = window.location.hash.replace(p_metadata.name, key);
					notes = g_data[key];

					p_result.push(
                        `<h3 class="font-weight-bold mb-2">${noteTitle}</h3>
                        <p class="mb-2 font-weight-bold">Reviewer's Notes from <a href="${noteUrl}#content">Case Form</a></p>
                        <p>
                            ${
							    notes == null || (notes.reviewer_note != null && notes.reviewer_note.length < 1) ? "<em>No data entered</em>" : textarea_control_replace_return_with_br(notes.reviewer_note)
                            }
                        </p>`
                    );
				}

				// SINGLE FORM
				//~~~ prenatal
				else if (key === "prenatal") {
					noteTitle = "Prenatal Care Record";
					noteUrl = window.location.hash.replace(p_metadata.name, key);
					notes = g_data[key];

					p_result.push(
                        `<h3 class="font-weight-bold mb-2">${noteTitle}</h3>
                        <p class="mb-2 font-weight-bold">Reviewer's Notes from <a href="${noteUrl}#content">Case Form</a></p>
                        <p>${notes == null || (notes.reviewer_note != null && notes.reviewer_note.length < 1) ? "<em>No data entered</em>" : textarea_control_replace_return_with_br(notes.reviewer_note)}</p>`
                    );
				}

				// MULTI RECORD FORM
				//~~~ er_visit_and_hospital_medical_records
				else if (key === "er_visit_and_hospital_medical_records") {
					noteTitle = "ER Visits and Hospitalizations";
					noteUrl = window.location.hash.replace(p_metadata.name, key);
					notes = g_data[key];

					p_result.push(
                        `<h3 class="font-weight-bold mb-2">${noteTitle}</h3>
                        <ul class="list-unstyled">`
                    );
					if (notes)
						for (let i = 0; i < notes.length; i++) {
							let month =
								g_value_to_display_lookup[
									`/${key}/basic_admission_and_discharge_information/date_of_arrival/month`
								][
									g_data[key][i].basic_admission_and_discharge_information
										.date_of_arrival.month
								];
							let day =
								g_value_to_display_lookup[
									`/${key}/basic_admission_and_discharge_information/date_of_arrival/day`
								][
									g_data[key][i].basic_admission_and_discharge_information
										.date_of_arrival.day
								];
							let year =
								g_value_to_display_lookup[
									`/${key}/basic_admission_and_discharge_information/date_of_arrival/year`
								][
									g_data[key][i].basic_admission_and_discharge_information
										.date_of_arrival.year
								];
							let dischargeStatus =
								g_value_to_display_lookup[
									`/${key}/basic_admission_and_discharge_information/discharge_pregnancy_status`
								][
									g_data[key][i].basic_admission_and_discharge_information
										.discharge_pregnancy_status
								];

							p_result.push(
                                `<li>
                                    <p class="mb-2 font-weight-bold">
                                        Reviewer's Notes from <a href="${noteUrl}/${i}">Case Form</a>: Record ${i + 1}
                                        ${
                                            !isNullOrUndefined(month) &&
                                            !isNullOrUndefined(day) &&
                                            !isNullOrUndefined(year) &&
                                            month != "(blank)" &&
                                            day != "(blank)" &&
                                            year != "(blank)" ?
                                                `– ${month}/${day}/${year}` : ''
                                        }
                                        ${
										    !isNullOrUndefined(dischargeStatus) &&
                                            dischargeStatus !== "(blank)" ?
                                                `– ${dischargeStatus}` : ''
										}
                                    </p>
                                    <p>
                                        ${
											!notes[i].reviewer_note ? "<em>No data entered</em>" : textarea_control_replace_return_with_br(notes[i].reviewer_note)
                                        }
                                    </p>
                                </li>`
                            );
						}

					p_result.push(`</ul>`);
				}

				// MULTI RECORD FORM
				//~~ other_medical_office_visits
				else if (key === "other_medical_office_visits") {
					noteTitle = "Other Medical Office Visits";
					noteUrl = window.location.hash.replace(p_metadata.name, key);
					notes = g_data[key];

					p_result.push(
                        `<h3 class="font-weight-bold mb-2">${noteTitle}</h3>
                        <ul class="list-unstyled">`
                    );
					if (notes) {
						for (let i = 0; i < notes.length; i++) {
							let month =
								g_value_to_display_lookup[
									`/${key}/visit/date_of_medical_office_visit/month`
								][g_data[key][i].visit.date_of_medical_office_visit.month];
							let day =
								g_value_to_display_lookup[
									`/${key}/visit/date_of_medical_office_visit/day`
								][g_data[key][i].visit.date_of_medical_office_visit.day];
							let year =
								g_value_to_display_lookup[
									`/${key}/visit/date_of_medical_office_visit/year`
								][g_data[key][i].visit.date_of_medical_office_visit.year];
							let visitType =
								g_value_to_display_lookup[`/${key}/visit/visit_type`][
									g_data[key][i].visit.visit_type
								];
							let providerType =
								g_value_to_display_lookup[
									`/${key}/medical_care_facility/provider_type`
								][g_data[key][i].medical_care_facility.provider_type];
							let pregnancyStatus =
								g_value_to_display_lookup[
									`/${key}/medical_care_facility/pregnancy_status`
								][g_data[key][i].medical_care_facility.pregnancy_status];

							p_result.push(`
                                <li>
                                    <p class="mb-2 font-weight-bold">
                                        Reviewer's Notes from <a href="${noteUrl}/${i}">Case Form</a>: Record ${i + 1}
                                        ${
                                            !isNullOrUndefined(month) &&
                                            !isNullOrUndefined(day) &&
                                            !isNullOrUndefined(year) &&
                                            month != "(blank)" &&
                                            day != "(blank)" &&
                                            year != "(blank)" ?
                                                `– ${month}/${day}/${year}` : ""
                                        }
                                        ${
                                            !isNullOrUndefined(visitType) &&
                                            visitType !== "(blank)" ? `– ${visitType}` : ""
                                        }
                                        ${
											!isNullOrUndefined(providerType) &&
                                            providerType !== "(blank)" ?
                                                `– ${providerType}` : ""
                                        }
                                        ${
                                            !isNullOrUndefined(pregnancyStatus) &&
                                            pregnancyStatus !== "(blank)" ? `– ${pregnancyStatus}` : ""
                                        }
                                    </p>
                                    <p>
                                        ${
                                            !notes[i].reviewer_note ?
                                                "<em>No data entered</em>" : textarea_control_replace_return_with_br(notes[i].reviewer_note)
                                        }
                                    </p>
                                </li>
                            `);
                        }
                    }

					p_result.push(`</ul>`);
				}

				// MULTI RECORD FORM
				//~~~ medical_transport
				else if (key === "medical_transport") {
					noteTitle = "Medical Transport";
					noteUrl = window.location.hash.replace(p_metadata.name, key);
					notes = g_data[key];

					p_result.push(
                        `<h3 class="font-weight-bold mb-2">${noteTitle}</h3>
                        <ul class="list-unstyled">`
                    );
					if (notes)
						for (let i = 0; i < notes.length; i++) {
							let month =
								g_value_to_display_lookup[`/${key}/date_of_transport/month`][
									g_data[key][i].date_of_transport.month
								];
							let day =
								g_value_to_display_lookup[`/${key}/date_of_transport/day`][
									g_data[key][i].date_of_transport.day
								];
							let year =
								g_value_to_display_lookup[`/${key}/date_of_transport/year`][
									g_data[key][i].date_of_transport.year
								];
							let reasonForTransport = g_data[key][i].reason_for_transport;

							p_result.push(`
                                <li>
                                    <p class="mb-2 font-weight-bold">
                                        Reviewer's Notes from <a href="${noteUrl}/${i}">Case Form</a>: Record ${i + 1}
                                        ${
                                            !isNullOrUndefined(month) &&
                                            !isNullOrUndefined(day) &&
                                            !isNullOrUndefined(year) &&
                                            month != "(blank)" &&
                                            day != "(blank)" &&
                                            year != "(blank)" ?
                                            `– ${month}/${day}/${year}` : ''
                                        }
                                        ${
                                            !isNullOrUndefined(reasonForTransport) ?
                                                `– ${reasonForTransport}` : ''
                                        }
                                    </p>
                                    <p>
                                        ${
                                            !notes[i].reviewer_note ?
                                                "<em>No data entered</em>" : textarea_control_replace_return_with_br(notes[i].reviewer_note)
                                        }
                                    </p>
                                </li>
                            `);
						}
					p_result.push(`</ul>`);
				}

				// SINGLE FORM
				//~~~ social_and_environmental_profile
				else if (key === "social_and_environmental_profile") {
					noteTitle = "Social and Environmental Profile";
					noteUrl = window.location.hash.replace(p_metadata.name, key);
					notes = g_data[key];

					p_result.push(
                        `<h3 class="font-weight-bold mb-2">${noteTitle}</h3>
                        <p class="mb-2 font-weight-bold">Reviewer's Notes from <a href="${noteUrl}#content">Case Form</a></p>
                        <p>
                            ${
                                notes == null ||
                                (notes.reviewer_note != null &&
                                notes.reviewer_note.length < 1) ?
                                    "<em>No data entered</em>" : textarea_control_replace_return_with_br(notes.reviewer_note)
                            }
                        </p>
                    `);
				}

                				//~~~ social_and_environmental_profile
				else if (key === "cvs") {
					noteTitle = "Community Vital Signs";
					noteUrl = window.location.hash.replace(p_metadata.name, key);
					notes = g_data[key];

					p_result.push(
                        `<h3 class="font-weight-bold mb-2">${noteTitle}</h3>
                        <p class="mb-2 font-weight-bold">Reviewer's Notes from <a href="${noteUrl}#content">Case Form</a></p>
                        <p>
                            ${
                                notes == null ||
                                (notes.reviewer_note != null &&
                                notes.reviewer_note.length < 1) ?
                                    "<em>No data entered</em>" : textarea_control_replace_return_with_br(notes.reviewer_note)
                            }
                        </p>
                    `);
				}

				// SINGLE FORM
				//~~~ mental_health_profile
				else if (key === "mental_health_profile") {
					noteTitle = "Mental Health Profile";
					notes = g_data[key];
					noteUrl = window.location.hash.replace(p_metadata.name, key);

					p_result.push(
                        `<h3 class="font-weight-bold mb-2">${noteTitle}</h3>
                        <p class="mb-2 font-weight-bold">Reviewer's Notes from <a href="${noteUrl}#content">Case Form</a></p>
                        <p>
                            ${
                                notes == null ||
                                (notes.reviewer_note != null &&
                                notes.reviewer_note.length < 1) ?
                                    "<em>No data entered</em>" : textarea_control_replace_return_with_br(notes.reviewer_note)
                            }
                        </p>
                    `);
				}

				// MULTI RECORD FORM
				//~~~ informant_interviews
				else if (key === "informant_interviews") {
					noteTitle = "Informant Interviews";
					noteUrl = window.location.hash.replace(p_metadata.name, key); // replace 'case_narrative' with 'informant_interviews'
					notes = g_data[key]; // array of forms

					p_result.push(
                        `<h3 class="font-weight-bold mb-2">${noteTitle}</h3>
                        <ul class="list-unstyled">`
                    );
					if (notes)
						for (let i = 0; i < notes.length; i++) {
							let month =
								g_value_to_display_lookup[`/${key}/date_of_interview/month`][
									g_data[key][i].date_of_interview.month
								];
							let day =
								g_value_to_display_lookup[`/${key}/date_of_interview/day`][
									g_data[key][i].date_of_interview.day
								];
							let year =
								g_value_to_display_lookup[`/${key}/date_of_interview/year`][
									g_data[key][i].date_of_interview.year
								];
							let interviewType =
								g_value_to_display_lookup[`/${key}/interview_type`][
									g_data[key][i].interview_type
								];
							let relationshipToDeceased =
								g_value_to_display_lookup[`/${key}/relationship_to_deceased`][
									g_data[key][i].relationship_to_deceased
								];

							p_result.push(
                                `<li>
                                    <p class="mb-2 font-weight-bold">
                                        Reviewer's Notes from <a href="${noteUrl}/${i}">Case Form</a>: Record ${i + 1}
                                        ${
                                            !isNullOrUndefined(month) &&
                                            !isNullOrUndefined(day) &&
                                            !isNullOrUndefined(year) &&
                                            month != "(blank)" &&
                                            day != "(blank)" &&
                                            year != "(blank)" ?
                                                `– ${month}/${day}/${year}` : ''
                                        }
                                        ${
                                            !isNullOrUndefined(
                                                interviewType
                                            ) && interviewType !== "(blank)"
                                                ? `– ${interviewType}` : ''
                                        }
                                        ${
                                            !isNullOrUndefined(relationshipToDeceased) &&
                                            relationshipToDeceased !== "(blank)" ?
                                                `– ${relationshipToDeceased}` : ''
                                        }
                                    </p>
                                    <p>
                                        ${
                                            !notes[i].reviewer_note ?
                                                "<em>No data entered</em>" : textarea_control_replace_return_with_br(notes[i].reviewer_note)
                                        }
                                    </p>
                                </li>
                            `);
						}
					p_result.push(`</ul>`);
				}
			}
		}

		p_result.push("</div> <!-- end .construct-output -->");
		p_result.push("</div> <!-- end .construct__body -->");
		p_result.push(
			"<div class='construct__footer row no-gutters align-items-center justify-content-start'>"
		);
		if (!(g_is_data_analyst_mode || case_is_locked)) {
			p_result.push(
                `<input type='button' class='btn btn-primary ml-3' value='Save & Continue' onclick='init_inline_loader(save_form_click)' ${save_and_continue_disable_attribute} />
                <input type='button' class='btn btn-primary ml-3' value='Save & Finish' onclick='init_inline_loader(save_and_finish_click)' ${save_and_finish_disable_attribute} />
                <input type='button' class='btn btn-primary ml-3' value='Undo' onclick='init_inline_loader(undo_click)' ${undo_disable_attribute} />`
            );
		}
		p_result.push(`<span class="spinner-container spinner-inline ml-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>`);
		p_result.push(`</div> <!-- end .construct__footer -->`);
		p_result.push(`</section>`);
	}
}

function render_validation_error_summary(
	p_result,
	p_metadata,
	p_data,
	p_ui,
	p_metadata_path,
	p_object_path,
	p_dictionary_path,
	p_is_grid_context,
	p_post_html_render,
	p_search_ctx
) {
	p_result.push(
        `<div id="validation_summary" class="construct__header-alert row no-gutters p-2" style="display: none">
            <span class="left-col x32 fill-p cdc-icon-alert_02"></span>
            <div class="right-col pl-3">
                <p class="mb-1">Please correct errors below:</p>
                <ul id="validation_summary_list" class="mb-0">
                    <!-- place alerts here -->
                </ul>
            </div>
        </div>`
    );
}

function quick_edit_header_render(
	p_result,
	p_metadata,
	p_data,
	p_ui,
	p_metadata_path,
	p_object_path,
	p_dictionary_path,
	p_is_grid_context,
	p_post_html_render,
	p_search_ctx
) {
	p_result.push(`<div data-header='quick-edit' class='construct__header'>`);

	let save_and_continue_disable_attribute = " disabled='disabled' ";
	if (!p_search_ctx.is_read_only) {
		save_and_continue_disable_attribute = "";
	}

	render_validation_error_summary(
		p_result,
		p_metadata,
		p_data,
		p_ui,
		p_metadata_path,
		p_object_path,
		p_dictionary_path,
		p_is_grid_context,
		p_post_html_render,
		p_search_ctx
	);

	p_result.push("<div class='construct__header-main row no-gutters'>");
	p_result.push("<div class='col col-8'>");
	if (g_data) 
    {
		p_result.push(
			"<h1 class='construct__title text-primary h1' tabindex='-1'>"
		);
		p_result.push(get_header_name(g_data.tracking.admin_info.jurisdiction));
		p_result.push("</h1>"); 

        
	}
    p_result.push(`<p><button type="button"  onclick="show_audit_click('${g_data._id}')">View Audit Log</button></p>`);
    
    p_result.push(" <p class='construct__info mb-0'><strong>Case Folder:</strong> ")
    if(g_data.tracking.admin_info.case_folder == "/")
    {
        p_result.push("Top Folder");
    }
    else
    {
        p_result.push(g_data.tracking.admin_info.case_folder);

    }
	if (g_data.tracking.admin_info.pmssno) 
    {
		p_result.push
        (
			"&nbsp;&nbsp;&nbsp;<strong>PMSS#:</strong> " + g_data.tracking.admin_info.pmssno
		);
		
	}
    p_result.push("</p>");

	p_result.push("<p class='construct__subtitle'");
	if (p_metadata.description && p_metadata.description.length > 0) {
		p_result.push("rel='tooltip' data-original-title='");
		p_result.push(p_metadata.description.replace(/'/g, "\\'"));
		p_result.push("'>");
	} else {
		p_result.push(">");
	}

	if (g_data.host_state && !isNullOrUndefined(g_data.host_state)) {
		p_result.push(`<p class='construct__info mb-0'>Reporting state: <span>${g_data.host_state}</span></p>`);
	}

	p_result.push(
		"Quick edit results for: <em>" +
			p_search_ctx.search_text +
			"</em><br/><br/>"
	);
	p_result.push("</p>");
	p_result.push("</div>");
	p_result.push("<div class='col col-4 text-right'>");
	if (!p_search_ctx.is_read_only) {
		p_result.push(` <input type='button' class='btn btn-secondary' value='Undo' onclick='undo_click()'/>
                 <input type='button' class='btn btn-primary' value='Save' onclick='save_form_click()' ${save_and_continue_disable_attribute}/>`);
	}
	p_result.push("</div>");
	p_result.push("</div>");
	p_result.push("</div> <!-- end .construct__header -->");
	p_result.push(`
        <span class="spinner-container spinner-content">
            <span class="spinner-body text-primary">
            <span class="spinner" aria-hidden="true"></span>
            <span class="spinner-info">Loading...</span>
            </span>
        </span>
    `);
}

function render_print_form_control(p_result, p_ui, p_metadata, p_data) {
	if (parseInt(p_ui.url_state.path_array[0]) >= 0) {
		p_result.push('<label for="print_case_id" class="sr-only">Print version</label>');
		p_result.push('<select id="print_case_id" onchange="enable_print_button(event)" class="form-control" style="width:280px">');
		p_result.push('<option value="">Select a form to print</option>');
		p_result.push('<optgroup label="Current form">');

		const path_to_check_multi_form = parseInt(p_ui.url_state.path_array[2]);
		const recordNumber = path_to_check_multi_form + 1;

		if (!isNaN(path_to_check_multi_form)) {
			// Render options for specific 'Record Number'
			p_result.push(
				'<option value="' +
					p_metadata.name +
					'" data-record="' +
					recordNumber +
					'">'
			);
			p_result.push(p_metadata.prompt + " (Record " + recordNumber + ")");
			p_result.push("</option>");
		} else if (!isNullOrUndefined(p_data) && isNaN(path_to_check_multi_form)) {
			// Render options for 'Multi Forms'
			p_result.push('<option value="' + p_metadata.name + '">');
			p_result.push("All " + p_metadata.prompt);
			p_result.push("</option>");
		} else {
			// Render print options for 'Single Forms'
			p_result.push('<option value="' + p_metadata.name + '">');
			p_result.push(p_metadata.prompt);
			p_result.push("</option>");
		}
		p_result.push("</optgroup>");

		if
		(
			role_set.size == 1 &&
			role_set.has('vro') 
		)
		{
			// do nothing
		}
		else
		{
			p_result.push('<optgroup label="Other">');
			//p_result.push('<option value="core-summary">Core Elements Only</option>');
			//p_result.push('<option value="all">All Case Forms</option>');
			p_result.push('<option value="all_hidden">All Case Forms</option>');
			p_result.push("</optgroup>");
		}
		p_result.push("</select>");

		p_result.push(`<input type="button" id="print-case-form" class="btn btn-primary ml-3" value="View" onclick="print_case_onclick(event)" disabled="true" />`);
		p_result.push(`<input type="button" id="pdf-case-view-form" class="btn btn-primary ml-3" value="View PDF" onclick="pdf_case_onclick(event, 'view')" disabled="true" />`);
		p_result.push(`<input type="button" id="pdf-case-save-form" class="btn btn-primary ml-3" value="Save PDF" onclick="pdf_case_onclick(event, 'save')" disabled="true" />`);
	}
}

function get_metadata_value_node_by_mmria_path
(
	p_metadata,
	p_search_path,
	p_path
) 
{
	let result = null;
	switch (p_metadata.type.toLowerCase()) {
		case "app":
		case "form":
		case "group":
		case "grid":
			for (let i = 0; i < p_metadata.children.length; i++) {
				let child = p_metadata.children[i];
				result = get_metadata_value_node_by_mmria_path(
					child,
					p_search_path,
					p_path + "/" + child.name
				);
				if (result != null) {
					break;
				}
			}
			break;
		default:
			if (p_search_path == p_path) {
				result = p_metadata;
			}
			break;
	}
	return result;
}


function show_audit_click(p_id)
{
    window.open('./_audit/' + p_id + '/1', '_audit');
   //window.open('./_audit/' + p_id, '_audit');
}

function get_header_name(p_value)
{
	const metadata_value_list = eval(convert_dictionary_path_to_lookup_object("lookup/state"));
	let display_name = p_value;
	for(const element of metadata_value_list)
	{
		if( element.value == p_value)
		{
			const start_index = element.display.indexOf("(");
			const last_index = element.display.indexOf(")");

			display_name = element.display.substring(start_index + 1, last_index);

			break;
		}
	}	
	
	return `${display_name} - ${g_data.tracking.admin_info.track_year} - ${g_data.tracking.death_certificate_number}`;
}

function form_get_disabled(p_metadata, p_dictionary_path)
{
    let result = " disabled = 'disabled' ";

    const control_path = new Set();

    control_path.add('tracking/cdc_case_matching_results/preg_cb_match');
    control_path.add('tracking/cdc_case_matching_results/literal_cod_match');
    control_path.add('tracking/cdc_case_matching_results/icd10_match');
    control_path.add('tracking/cdc_case_matching_results/bc_match');
    control_path.add('tracking/cdc_case_matching_results/fdc_match');

    const check_value = g_data.tracking.admin_info.steve_transfer;

    const form_path = new Set();

    form_path.add('ije_dc');
    form_path.add('ije_bc');
    form_path.add('ije_fetaldc');



	if(g_data_is_checked_out || p_metadata.type == 'always_enabled_button')
	{
        if(g_data_is_checked_out)
        {
            if(check_value != 1)
            {
                result = " ";
            }
            else
            {
                let is_found = false;

                if(control_path.has(p_dictionary_path))
                {
                    is_found = true;
                }
                else
                {
                    for(let i of form_path)
                    {
                        if(p_dictionary_path.indexOf(i) == 0)
                        {
                            is_found = true;
                            break;
                        }
                    }
                }

                if (! is_found)
                {
                    result = " ";
                }
            }
        }
        else
        {
            result = " ";
        }

	}

    

    return result;
}

/*

Stated Rule: 	If “12c. Maternal Birthplace” is “1:Born in the United States (including territories)” Then ENABLE “12c1. Birhtplace State” AND DISABLE	“12c2. Birthplace Country”

Coded Rule:	IF  app/demographic/q12/matbplc = 1 THEN ENABLE app/demographic/q12/matbplc_us  AND DISABLE 	app/demographic/q12/matbplc_else			(1:US)
		ELSE IF  app/demographic/q12/matbplc = 2 THEN ENABLE app/demographic/q12/matbplc_else  AND DISABLE  app/demographic/q12/matbplc_us	
		ELSE IF  app/demographic/q12/matbplc = 9 THEN DISABLE app/demographic/q12/matbplc_us  AND DISABLE  app/demographic/q12/matbplc_else
When:		When user edits data in the “12c. Maternal Birthplace” field (app/demographic/q12/matbplc)    OR  when the form is first opened (data being prefilled via vitals import or manually entered previously).


path=tracking/admin_info/steve_transfer value == 1

        1 ije_dc
        1 ije_bc
        1 ije_fetaldc
        1 tracking/cdc_case_matching_results/preg_cb_match
        1 tracking/cdc_case_matching_results/literal_cod_match
        1 tracking/cdc_case_matching_results/icd10_match
        1 tracking/cdc_case_matching_results/bc_match
        1 tracking/cdc_case_matching_results/fdc_match

        set_enable: function(p_dictionary_path, p_form_index, p_grid_index)
        
         if(p_value == 1)
            set_disable: function(p_dictionary_path, p_form_index, p_grid_index)
         else
            set_enable

        


*/