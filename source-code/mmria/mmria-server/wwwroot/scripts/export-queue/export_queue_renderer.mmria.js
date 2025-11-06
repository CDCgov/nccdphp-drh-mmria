var g_case_view_request = {
  total_rows: 0,
  page: 1,
  skip: 0,
  take: 25,
  sort: 'date_last_updated',
  search_key: null,
  descending: true,
  case_status: "all",
  field_selection: "all",
  pregnancy_relatedness:"all",
  get_query_string: function () {
    var result = [];
    result.push('?skip=' + (this.page - 1) * this.take);
    result.push('take=' + this.take);
    result.push('sort=' + this.sort);
    result.push('case_status=' + this.case_status);
    result.push('field_selection=' + this.field_selection);
    result.push('pregnancy_relatedness=' + this.pregnancy_relatedness);
    if(g_filter.include_blank_date_of_reviews == false)
    {
      result.push(`date_of_review_range=${ControlFormatDate(g_filter.date_of_review.begin)}T${ControlFormatDate(g_filter.date_of_review.end)}`);
    }
    else
    {
      result.push('date_of_review_range=All');
    }
    if(g_filter.include_blank_date_of_deaths == false)
    {
      result.push(`date_of_death_range=${ControlFormatDate(g_filter.date_of_death.begin)}T${ControlFormatDate(g_filter.date_of_death.end)}`);
    }
    else
    {
      result.push('date_of_death_range=All');
    }
    if (this.search_key) {
      result.push(
        'search_key=' +
        encodeURI(this.search_key) +
          ''
      );
    }
    result.push('descending=' + this.descending);
    return result.join('&');
  },
};

const g_default_case_view_request = JSON.parse(JSON.stringify(g_case_view_request));

function export_queue_render(p_queue_data, p_answer_summary, p_filter) {
  var result = [];

  const de_identified_search_result = render_de_identified_search_result(
    g_metadata.children
  );
  const selected_de_identified_list = render_selected_de_identified_list(
    p_answer_summary
  );

  let selected_case_list = [];
  render_selected_case_list(selected_case_list, p_answer_summary);

  let pagination_html = [];
  render_pagination(pagination_html, g_case_view_request);

  let export_report_type = render_export_report_type(p_answer_summary['all_or_core']);
  result.push(`
		<div class="row">
			<div class="col">
				<div class="pl-2">
					<div class="vertical-control">
						<label for="grantee-name" class="font-weight-semi">Confirm Jurisdiction name</label>
                        <div class="additional-note">This is added to each exported case</div>
						<input id="grantee-name"
                            class="form-control col-md-3"
                            type="text"
                            value="${p_answer_summary.grantee_name}"
                            disabled
                            readonly="true"
                        />
					</div>				
					<div class="vertical-control mt-4">
                        <label for="all-data" class="font-weight-semi">Select Export Data</label>
                        <div class="additional-note">A zip file of the selected data will be downloaded directly to your computer's local "Downloads" folder</div>
						<select 
                            name="export-type"
                            id="all-data"
                            value="all"
                            data-prop="all_or_core"
                            class="form-select form-control col-md-3"
                            onchange="set_answer_summary(event).then(render_summary_section(this))"
                        >
                            ${export_report_type}
                        </select>
					</div>
					<div>
                        <fieldset class="horizontal-control mt-4">
                            <legend class="font-weight-semi">Select Password Setting</legend>
                            <div class="form-check">                            
                                <input
                                    name="password-protect"
                                    id="password-protect-no"
                                    class="form-check-input big-radio"
                                    style="margin-left: 0px !important;"
                                    type="radio"
                                    value="no"
                                    data-prop="is_encrypted"
                                    ${p_answer_summary['is_encrypted'] == 'no' ? 'checked=true' : ''}
                                    onchange="set_answer_summary(event).then(handleElementDisplay(event, 'none')).then(render_summary_section(this))"
                                />
                                <label style="margin-left: 0px !important;" for="password-protect-no" class="form-check-label">No password</label>
                            </div>
                            <div class="form-check">
                                <input
                                    name="password-protect"
                                    id="password-protect-yes"
                                    type="radio"
                                    value="yes"
                                    data-prop="is_encrypted"
                                    class="form-check-input big-radio"
                                    style="margin-left: 0px !important;"
                                    ${p_answer_summary['is_encrypted'] == 'yes' ? 'checked' : ''}
                                    onchange="set_answer_summary(event).then(handleElementDisplay(event, 'block')).then(render_summary_section(this))"
                                />
                                <label style="margin-left: 0px !important;" for="password-protect-yes" class="form-check-label">Set password</label>
                            </div>
                        </fieldset>
                        <div class="vertical-control mt-4" data-show="is_encrypted"  style="display: ${p_answer_summary['is_encrypted'] == 'yes' ? 'block' : 'none'};">
                            <label class="font-weight-semi" for="encryption-key">Set Password</label>
                            <input id="encryption-key"
                                class="form-control col-md-3"
                                type="text"
                                value="${p_answer_summary.zip_key}" onchange="zip_key_changed(this.value)"
                            />
                        </div>
					</div>
					<div class="vertical-control mt-4">
                        <fieldset class="horizontal-control mt-4">
                            <legend class="font-weight-semi">Select De-Identified Fields</legend>
                            <div class="form-check">
                                <input name="de-identify"
                                    id="de-identify-none"
                                    type="radio"
                                    value="none"
                                    class="form-check-input big-radio"
                                    style="margin-left: 0px !important;"
                                    data-prop="de_identified_selection_type"
                                    ${p_answer_summary.de_identified_selection_type == 'none' ? 'checked=true' : ''}
                                    onchange="de_identify_filter_type_click(this).then(render_summary_section(this))"
                                /> 
                                <label style="margin-left: 5px !important;" for="de-identify-none" class="mb-0 font-weight-normal mr-3">None</label>
                            </div>
                            <div class="form-check">
                                <input name="de-identify"
                                    id="de-identify-standard"
                                    type="radio"
                                    value="standard"
                                    class="form-check-input big-radio"
                                    style="margin-left: 0px !important;"
                                    data-prop="de_identified_selection_type"
                                    ${p_answer_summary.de_identified_selection_type == 'standard' ? 'checked=true' : ''}
                                    onchange="de_identify_filter_type_click(this).then(render_summary_section(this))"
                                />
                                <label style="margin-left: 5px !important;" for="de-identify-standard" class="mb-0 font-weight-normal mr-3">Standard</label>
                            </div>
                            <div class="form-check">
                                <input name="de-identify"
                                    id="de-identify-custom"
                                    type="radio"
                                    value="custom"
                                    data-prop="de_identified_selection_type"
                                    class="form-check-input big-radio"
                                    style="margin-left: 0px !important;"
                                    ${p_answer_summary.de_identified_selection_type == 'custom' ? 'checked=true' : ''}
                                    onchange="de_identify_filter_type_click(this).then(render_summary_section(this))"
                                />
                                <label style="margin-left: 5px !important;" for="de-identify-custom" class="mb-0 font-weight-normal">Custom</label>
                            </div>
                        </fieldset>
						<div
                            id="de_identify_filter_standard""
                            data-prop="de_identified_selection_type"
                            style="display: ${p_answer_summary.de_identified_selection_type == 'standard' ? 'block' : 'none'};"
                        >
							<div class="" style="overflow:hidden; overflow-y: auto; max-height: 346px;">
								<table class="table" style="border: 1px solid #E3D3E4;">
									<thead>
										<tr style="top: 0" class="header-level-2 sticky z-index-middle">
											<th class="th" colspan="2" scope="colgroup">
												<span class="row no-gutters justify-content-between">
													<span class="font-weight-semi">Standard De-Identified Fields</span>
												</span>
											</th>
										</tr>
									</thead>
                                    <tbody>
                                        ${render_standard_de_identify_fields(g_standard_de_identified_list)}
                                    </tbody>
								</table>
							</div>
						</div>
						<div
                            id="de_identify_filter"
                            data-prop="de_identified_selection_type"
                            style="display: ${p_answer_summary.de_identified_selection_type == 'custom' ? 'block' : 'none'};"
                        >
							<div class="additional-note mb-3">To customize, please search/choose your options below and check the resulting fields you want to de-identify from the list.</div>
							<div class="form-inline mb-2">
                                <div class="vertical-control">
                                    <label class="justify-content-start font-weight-semi" for="de_identify_search_text"> Search for</label>
                                    <input
                                        type="text"
                                        class="form-control mr-2"
                                        id="de_identify_search_text"
                                        value=""
                                        onchange="de_identify_search_text_change(this.value)"
                                    />
                                </div>
                                <div class="vertical-control col-md-3">
                                    <label class="justify-content-start font-weight-semi" for="de_identify_form_filter"> Form Type</label>
                                    <select id="de_identify_form_filter" class="form-select form-control mr-2 col-md-12" onchange="">
                                        ${render_de_identify_form_filter(p_filter)}
                                    </select>
                                </div>
								<button
                                    type="button"
                                    style="margin-top: 1.2rem"
                                    class="btn primary-button mb-0"
                                    alt="apply filter"
                                    onclick="de_identified_search_click()"
                                >
                                    Apply Filters
                                </button>
                                <button
                                    id="reset_de_identified_filters_button"
                                    aria-disabled="true"
                                    disabled
                                    type="button"
                                    style="margin-top: 1.2rem"
                                    class="btn cancel-button mb-0 ml-2"
                                    alt="reset filters"
                                    onclick="de_identified_search_click(true)"
                                >
                                    Reset Filters
                                </button>
								<span class="spinner-container spinner-inline ml-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>
							</div>
							<div class="row">
								<button class="btn primary-button ml-3" id="select-all-deidentified" onclick="de_identified_select_all_click()">
									Select All Results
								</button>
                                <button class="btn cancel-button ml-3" id="select-all-deidentified-clear" onclick="de_identified_clear_selected_search_result_click()">
                                    Clear All Results
                                </button>
                                <button class="btn secondary-button ml-3" id="add-all-standard-deidentified" onclick="add_standard_de_identified_fields_click()">
                                    Add Standard De-Identified Fields
                                </button>                            
							</div>
							<div class="mt-3" style="border: 1px solid #bbbbbb; overflow:hidden; overflow-y: auto; max-height: 346px;">
								<table class="table">
									<thead>
										<tr style="top: 0;" class="header-level-2 sticky z-index-middle">
											<th class="th" colspan="2" scope="colgroup">
												<span class="row no-gutters justify-content-between">
													<span class="font-weight-semi">Search Results</span>
												</span>
											</th>
										</tr>
									</thead>
									<tbody class="tbody" id="de_identify_search_result_list">
										${de_identified_search_result}
									</tbody>
								</table>
							</div>
							<div class="mt-3" style="border: 1px solid #bbbbbb; overflow:hidden; overflow-y: auto; max-height: 346px;">
								<table class="table">
									<thead>
										<tr style="top: 0;" class="header-level-2 sticky z-index-middle">
											<th class="th" colspan="2" scope="colgroup">
												<span class="row no-gutters justify-content-between">
													<span
                                                        class="font-weight-semi"
                                                        id="de_identified_count"
                                                    >
                                                        De-Identified (Selected) Fields (${p_answer_summary.de_identified_field_set.length})
                                                    </span>
												</span>
											</th>
										</tr>
									</thead>
									<tbody class="tbody" id="selected_de_identified_field_list">
										${selected_de_identified_list}
									</tbody>
								</table>
							</div>
                            <button class="btn primary-button mt-2" onclick="de_identified_clear_all_click()">
                                Clear All Selected
                            </button>
						</div>
					</div>
					<div>
                        <fieldset class="horizontal-control mt-4">
                            <legend class="font-weight-semi">Select Cases to Export</legend>
                            <div class="form-check">
                                <input id="case_filter_type_all"
                                    type="radio"
                                    name="case_filter_type"
                                    value="all"
                                    data-prop="case_filter_type"
                                    ${p_answer_summary['case_filter_type'] == 'all' ? 'checked=true' : ''}
                                    onclick="case_filter_type_click(this)" aria-label="All"
                                    class="form-check-input big-radio"
                                    style="margin-left: 0px !important;"
                                />
                                <label style="margin-left: .2rem !important;" for="case_filter_type_all" class="mb-0 font-weight-normal mr-3">All</label>
                            </div>
                            <div class="form-check">
                                <input id="case_filter_type_custom"
                                    type="radio"
                                    name="case_filter_type"
                                    value="custom"
                                    data-prop="case_filter_type"
                                    ${p_answer_summary['case_filter_type'] == 'custom' ? 'checked=true' : ''}
                                    onclick="case_filter_type_click(this)" aria-label="Custom"
                                    class="form-check-input big-radio"
                                    style="margin-left: 0px !important;"
                                />
                                <label style="margin-left: .2rem !important;" for="case_filter_type_custom" class="mb-0 font-weight-normal mr-3">Custom</label>
                            </div>
                        </fieldset>
						<div
                            class="font-weight-semi list-unstyled mt-3"
                            id="custom_case_filter"
                            style="display:${p_answer_summary['case_filter_type'] == 'custom'? 'block': 'none'}"
                        >
							<div class="d-flex flex-column" >
                                <div class="d-flex mt-2">
                                    <div class="vertical-control col-md-4 pl-0 mr-2">
                                        <label for="filter_search_text" class="font-weight-semi">Keyword</label>
                                        <input type="text"
                                            class="form-control"
                                            id="filter_search_text"
                                            value=""
                                            onchange="filter_serach_text_change(this.value)"
                                        />
                                    </div>
                                    <div class="vertical-control col-md-4 pl-0 mr-2">
                                        <label for="search_field_selection" class="font-weight-semi">Keyword Type</label>
                                        <select id="search_field_selection" class="form-select form-control" onchange="search_field_selection_onchange(this.value)">
                                            ${render_field_selection(g_case_view_request)}
                                        </select>
                                    </div>
                                    <div class="vertical-control col-md-4 mr-2 pl-0 pr-4">
                                        <label for="search_case_status" class="font-weight-semi">Case Status</label>
                                        <select id="search_case_status" class="form-select form-control" onchange="search_case_status_onchange(this.value)">
                                            ${renderSortCaseStatus(g_case_view_request)}
                                        </select>
                                    </div>
                                </div>
                                <div class="d-flex mt-2">
                                    <div class="vertical-control col-md-4 mr-2 pl-0">
                                        <label for="search_pregnancy_relatedness" class="font-weight-semi">Pregnancy Relatedness</label>
                                        <select id="search_pregnancy_relatedness" class="form-select form-control" onchange="search_pregnancy_relatedness_onchange(this.value)">
                                            ${renderPregnancyRelatedness(g_case_view_request)}
                                        </select>
                                    </div>
                                </div>
                                <div class="d-flex flex-column mt-2">
                                    ${render_pregnancy_filter(g_case_view_request)}
                                </div>
                                <div class="col-md-12 border border-top border-dark-sm mt-2 mb-2"></div>
                                <div class="d-flex mt-2">
                                    <div class="vertical-control col-md-3 p-0">
                                        <label for="filter_sort_by" class="font-weight-semi">Sort By</label>
                                        <select id="filter_sort_by" class="form-select form-control">
                                            ${render_sort_by_include_in_export(g_case_view_request)}
                                        </select>
                                    </div>
                                    <div class="vertical-control ml-4 col-md-3 p-0">
                                        <label for="search_records_per_page" class="font-weight-semi">Records Per Page</label>
                                        <select
                                            id="search_records_per_page"
                                            class="form-select form-control"
                                            onchange="records_per_page_change(this.value)"
                                        >
                                            ${render_filter_records_per_page(g_case_view_request)}
                                        </select>
                                    </div>
                                    <div class="vertical-control ml-4 col-md-3 p-0 mr-4">
                                        <label for="filter_decending" class="font-weight-semi">Sort Order</label>
                                        <select id="filter_decending" class="form-select form-control">
                                            <option value="asc" ${g_case_view_request.descending ? '' : 'selected'}>Ascending</option>
                                            <option value="desc" ${g_case_view_request.descending ? 'selected' : ''}>Descending</option>
                                        </select>
                                    </div>
                                    <div class="d-flex align-self-end col-md-3">
                                        <button
                                            type="button"
                                            class="btn primary-button ml-2 mr-2"
                                            alt="apply filters"
                                            onclick="apply_filter_button_click()"
                                        >
                                            Apply Filters
                                        </button>
                                        <button
                                            id="reset_case_filters_button"
                                            aria-disabled="true"
                                            disabled
                                            type="button"
                                            class="btn cancel-button"
                                            alt="reset filters"
                                            onclick="reset_filter_button_click()"
                                        >
                                            Reset Filters
                                        </button>
                                        <span class="spinner-container spinner-inline ml-2">
                                            <span class="spinner-body text-primary">
                                                <span class="spinner"></span>
                                            </span>
                                        </span>
                                    </div>
                                </div>
							</div>
							<div class="mb-3 mt-3">
								<div id='case_result_pagination' class="d-flex mb-2">
									${pagination_html.join('')}
								</div>
                                <div id="filter_table" style="overflow:hidden; overflow-y: auto;max-height: 360px;">
                                    <table style="border: 1px solid #bbbbbb;" class="table">
                                        <thead class="thead">
                                            <tr class="header-level-top-black">
                                                <th class="th" colspan="14" scope="colgroup">
                                                    <span class="row no-gutters">
                                                        <span class="font-weight-semi">Filtered Cases</span>
                                                    </span>
                                                </th>
                                            </tr>
                                            <tr style="top: -1px; position: sticky;" class="header-level-2 sticky z-index-middle">
                                                <th class="th" width="38" scope="col"></th>
                                                <th class="th" width="150"scope="col">Last Update</th>
                                                <th class="th" width="285" scope="col">Name [Jurisdiction ID]</th>
                                                <th class="th" scope="col">Record ID</th>
                                                <th class="th" scope="col">Date of Death</th>
                                                <th class="th" scope="col">Committee Review Date</th>
                                                <th class="th" width="150"scope="col">Agency Case ID</th>
                                                <th class="th" scope="col">Case Creation</th>
                                            </tr>
                                        </thead>
                                        <tbody id="search_result_list" class="tbody">
                                            <tr class="tr">
                                                <td align="center" colspan="8" class="td">Filter to Begin Searching</td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
							</div>
							<div id="selected_case_table" class="mb-3 mt-4" style="overflow:hidden; overflow-y: auto;max-height: 360px;">
								<table style="border: 1px solid #bbbbbb;" class="table">
									<thead >
										<tr class="header-level-top-black">
											<th class="th" colspan="14" scope="colgroup">
												<span class="row no-gutters">
													<span class="font-weight-semi" id="exported_cases_count">Selected Cases for Export</span>
												</span>
											</th>
										</tr>
										<tr class="header-level-2 sticky z-index-middle" style="top: -1px; position: sticky;">
                                            <th class="th" width="38" scope="col"></th>
                                            <th class="th" width="150" scope="col">Last Update</th>
                                            <th class="th" width="285" scope="col">Name [Jurisdiction ID]</th>
                                            <th class="th" scope="col">Record ID</th>
                                            <th class="th" scope="col">Date of Death</th>
                                            <th class="th" scope="col">Committee Review Date</th>
                                            <th class="th" width="150" scope="col">Agency Case ID</th>
                                            <th class="th" scope="col">Case Creation</th>
										</tr>
									</thead>
									<tbody id="selected_case_list" class="tbody">
										${selected_case_list.join('')}
                                        ${selected_case_list.length == 0 ? `<tr class="tr"><td align="center" colspan="8" class="td">No Cases Selected</td></tr>` : ''}
									</tbody>
								</table>
							</div>
                            <div>
                                <button
                                    id="clear_all_selections_button"
                                    ${selected_case_list.length == 0 ? 'disabled aria-disabled="true"' : 'aria-disabled="false"'}
                                    onclick="deselect_all_filtered_cases_click()"
                                    class="btn primary-button"
                                >
                                    Clear All Selections
                                </button>
                            </div>
						</div>
					</div>
                    <div class="mb-4">
                    <fieldset class="horizontal-control mt-4">
                            <legend class="font-weight-semi">Select Export File Type</legend>
                            <div class="form-check">
							<input
                                id="case_file_type_csv"
								type="radio"
								name="case_file_type"
								value="csv"
								data-prop="case_file_type"
								${p_answer_summary['case_file_type'] == 'csv' ? 'checked=true' : ''}
                                class="form-check-input big-radio"
                                style="margin-left: 0px !important;"
								onclick="case_file_type_click(this)" />
                                <label style="margin-left: .2rem !important;" for="case_file_type_csv" class="mb-0 font-weight-normal mr-3">CSV</label>
                            </div>
                            <div class="form-check">
                                <input
                                    id="case_file_type_xlsx"
                                    type="radio"
                                    name="case_file_type"
                                    value="xlsx"
                                    data-prop="case_file_type"
                                    ${p_answer_summary['case_file_type'] == 'xlsx' ? 'checked=true' : ''}
                                    onclick="case_file_type_click(this)" aria-label="excel (.xlsx)"
                                    class="form-check-input big-radio"
                                    style="margin-left: 0px !important;"
                                />
                                <label style="margin-left: .2rem !important;" for="case_file_type_xlsx" class="mb-0 font-weight-normal mr-3">Excel (.xlsx)</label>
                            </div>
                        </fieldset>
                    </div>
				</div>
			</div>
		</div>
        <div class="border-top border-dark-sm pt-4 col-md-12"></div>
		<div class="row">
			<div class="col">
				${export_queue_comfirm_render(p_answer_summary)}
			</div>
		</div>
	`);
  result.push(
    `<table class="table mt-4 mb-0">
        <caption class="table-caption">Export Request History table giving the current status of each export and available actions.</caption>
        <thead>
			<tr class="header-level-top-black">
				<th class="th h4" colspan="8" scope="colgroup">
					Export Request History
				</th>
			</tr>
			<tr class="">
				<th class="th" colspan="8" scope="colgroup">
					<span class="font-weight-semi">NOTE:</span>
                    <span class="font-weight-normal">The export queue is deleted at midnight each day</span>
				</th>
			</tr>
			<tr class="header-level-2">
				<th width="140" class="th" scope="col">Date Created</th>
				<th width="110" class="th" scope="col">Created By</th>
				<th width="175" class="th" scope="col">Date Last Updated</th>
				<th width="150" class="th" scope="col">Last Updated By</th>
				<th width="250" class="th" scope="col">File Name</th>
				<th class="th" scope="col">Export Type</th>
				<th class="th" scope="col">Status</th>
				<th class="text-center" scope="col">Actions</th>
				</tr>
        </thead>
        <tbody class="tbody">`
  );
  function td(content) {
    result.push(`<td>${content}</td>`);
  }
  for (var i = 0; i < p_queue_data.length; i++) {
    var item = p_queue_data[i];

    result.push('<tr class="tr">');
    td(format_date_time(item.date_created));
    td(item.created_by);
    td(format_date_time(item.date_last_updated));
    td(item.last_updated_by);
    td(item.file_name);
    td(item.export_type);
    const inQueue = item.status.includes('Queue') && !item.status.includes("Queue Failed");
    const creating = item.status.includes('Creating');
    if (inQueue || creating) 
    {
      td(
        `<span class="spinner-container spinner-small spinner-active">
            <span class="spinner-body text-primary">
                <span class="spinner"></span>
                <span class="spinner-info">${inQueue ? 'In Queue' : 'Creating Export'}...</span>
            </span>
        </span>`
      );
    } 
    else 
    {
        let queue_status = item.status;
        if(queue_status.length > 100)
        {
            queue_status = queue_status.substr(0, 100);
        }
      td(queue_status);
    }
    function getButtons() 
    {
      function buttonEl(value) 
      {
        if 
        (
            !['Confirm', 'Cancel', 'Download', 'Delete'].includes(value)
        ) 
        {
            console.error('Unknown button type: ' + value);
        }
        
        const clickType = value.toLowerCase();
        if(value === 'Download')
            return `
            <button
                style="padding:0rem!important;"
                tooltip="Download Export"
                aria-label="download ${item._id} export"
                class="primary-button icon-button"
                value='${value}'
                onclick="${clickType}_export_item('${item._id}')"
            >
                <span class="icon cdc-icon-download"></span>
            </button>
        `;
        else if(value === 'Delete')
            return `
            <button
                style="padding:0rem!important;"
                tooltip="Delete Export"
                aria-label="delete ${item._id} export"
                class="delete-icon-button icon-button pb-1"
                value='${value}'
                onclick="${clickType}_export_item('${item._id}')"
            >
                <img src="./img/delete-icon.svg">
            </button>
        `;
        else if(value === 'Confirm')
            return `
            <button
                style="padding:0rem!important;"
                tooltip="Confirm Export"
                aria-label="confirm ${item._id} export"
                class="primary-button icon-button"
                value='${value}'
                onclick="${clickType}_export_item('${item._id}')"
            >
                <span class="icon cdc-icon-check"></span>
            </button>
        `;
        else
            return `
            <button
                style="padding:0rem!important;"
                tooltip="Cancel Export"
                aria-label="cancel ${item._id} export"
                class="cancel-button icon-button"
                value='${value}'
                onclick="${clickType}_export_item('${item._id}')"
            >
                <span class="icon cdc-icon-close"></span>
            </button>
        `;
      }
      if (item.status == 'Confirmation Required') 
      {
        return buttonEl('Confirm') + buttonEl('Cancel');
      } 
      else if (item.status == 'Download') 
      {
        return buttonEl('Download');
      } 
      else if (item.status == 'Downloaded') 
      {
        return buttonEl('Download') + buttonEl('Delete');
      } 
      else 
      {
        return '';
      }
    }
    result.push(`<td align="center">${getButtons()}</td>`);
    result.push('</tr>');
  }
  result.push('</tbody>');
  result.push('</table>');

  return result;
}

function format_date_time(dateString) {
  if (!dateString) return '';
  // Remove microseconds for compatibility
  const cleaned = dateString.replace(/\.\d+/, '');
  const date = new Date(cleaned);
  if (isNaN(date.getTime())) return dateString; // fallback if invalid
  return date.toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: true,
    timeZoneName: 'short'
  });
}

function render_summary_section(el = undefined) 
{
  if (el) 
  {
    let val = capitalizeFirstLetter(el.value);
    let prop = el.dataset.prop;
    const props = document.querySelectorAll(
      `#answer-summary-card [data-prop="${prop}"]`
    );
    props.forEach((propEl) => {
      propEl.innerText = val;
    });
  }
  var de_identified_field_selection = document.getElementById('de_identified_field_selection');
  if (de_identified_field_selection)
  {
    de_identified_field_selection.innerHTML = answer_summary.de_identified_selection_type;
  }
  render_summary_de_identified_fields(answer_summary);
  var all_or_core = document.getElementById('selected_cases_all_or_core');
  var summary_of_selected_cases = document.getElementById(
  'summary_of_selected_cases'
  );
  var summary_of_selected_cases_result = render_summary_of_selected_cases(
    answer_summary
  );
  summary_of_selected_cases.innerHTML = summary_of_selected_cases_result;
  all_or_core.innerText = capitalizeFirstLetter(answer_summary.all_or_core) + ' data,';
  var summary_of_file_type = document.getElementById('summary_of_file_type');
  if (summary_of_file_type) 
  {
    summary_of_file_type.innerText =
      answer_summary.case_file_type == 'csv' ? 'CSV' : 'Excel (.xlsx)';
  }
}

function export_queue_comfirm_render(p_answer_summary) 
{
var result = `
    <div id="answer-summary-card" class="border border-top border-dark-sm pt-3 pl-3 pr-3 pb-2 mt-2">
        <h2 class="h3">Export Data Selection Summary</h2>
        <div class="d-flex mt-3">
            <div class="d-flex flex-column col-md-3 pl-0">
                <div class="font-weight-semi mr-2">Export/Jurisdiction Name:</div>
                <div class="font-weight-semi mr-2">Export Data:</div>
                <div class="font-weight-semi mr-2">Password Protection:</div>
                <div class="font-weight-semi mr-2">De-Identified Fields:</div>
            </div>
            <div class="d-flex flex-column ml-4">
                <div>${p_answer_summary.grantee_name}</div>
                <div>
                    <span class="pr-0" id="selected_cases_all_or_core" data-prop="all_or_core">
                        ${capitalizeFirstLetter(p_answer_summary.all_or_core)} data,
                    </span>
                    <a href="/data-dictionary" target="_blank">data dictionary</a>
                </div>
                <div>
                    <span data-prop="is_encrypted">
                        ${capitalizeFirstLetter(p_answer_summary.is_encrypted)}
                    </span>
                </div>
                <div>
                    <span data-prop="de_identified_selection_type">
                        ${capitalizeFirstLetter(p_answer_summary.de_identified_selection_type)}
                    </span>                        
                </div>
            </div>
        </div>
        <div style="max-height:160px;overflow:auto;" class="d-flex">
            <div id="de_identified_filtered_case_selections" class="d-flex flex-column col-md-3 pl-0">
            </div>
            <div class="d-flex flex-column ml-4">
                <div class="d-flex flex-column ml-1" id="summary_of_de_identified_fields"></div>
            </div>
        </div>
        <div class="d-flex">
            <div class="d-flex flex-column col-md-3 pl-0">
                <div class="font-weight-semi mr-2">Filtered By:</div>
            </div>
            <div class="d-flex flex-column ml-4">
                <div>
                    <span data-prop="case_filter_type">
                        ${capitalizeFirstLetter(p_answer_summary.case_filter_type)}
                    </span>
                </div>
            </div>
        </div>
        <div class="d-flex">
            <div style="max-height:160px;overflow:auto" id="summary_of_selected_cases" class="d-flex col-md-12 pl-0"></div>
        </div>
        <div class="d-flex">
            <div class="d-flex flex-column col-md-3 pl-0">
                <div class="font-weight-semi mr-2">File Type:</div>
            </div>
            <div class="d-flex flex-column ml-4">
                <div>
                    <span id="summary_of_file_type" data-prop="file_type">
                        ${capitalizeFirstLetter(p_answer_summary.case_file_type) === 'Csv' ? 'CSV' : 'Excel (.xlsx)'}
                    </span>          
                </div>
            </div>
        </div>
        <button class="btn primary-button mt-3" onclick="add_new_all_export_item()">
            <span class="x16 cdc-icon-share mr-1">
                <span>Confirm & Start Export</span>
            </span>
        </button>
    </div>
`;

  return result;
}

// Function returned after promise to update/set answer_summary to new value
function updateSummarySection(event) 
{
  const tar = event.target;
  const prop = tar.dataset.prop;
  const val = tar.value;
  const el = document.querySelectorAll(
    `#answer-summary-card [data-prop='${prop}']`
  );
  let path;

  // if prop doesn't have path
  if (prop.indexOf('/') < 0) 
  {
    el.forEach((i) => {
      i.innerText = capitalizeFirstLetter(val);
    });
  } else {
    path = prop.split('/');
    switch (path[1]) {
      case 'case_status':
        const cs_opts = tar.options;
        let cs_html = '';
        for (let i = 0; i < cs_opts.length; i++) {
          if (cs_opts[i].selected) {
            cs_html += '<li>';
            cs_html += cs_opts[i].text;
            cs_html += '</li>';
          }
        }
        el[0].innerHTML = cs_html;
        break;

      case 'case_jurisdiction':
        const cj_opts = tar.options;
        let cj_html = '';
        for (let i = 0; i < cj_opts.length; i++) 
        {
          if (cj_opts[i].selected) 
          {
            cj_html += '<li>';
            cj_html += cj_opts[i].text;
            cj_html += '</li>';
          }
        }
        el[0].innerHTML = cj_html;
        break;
      default:
        el.forEach((i) => {
          i.innerText = capitalizeFirstLetter(val);
        });
        break;
    }
  }
}

// Function returned after promise to update/set answer_summary to new value
function handleElementDisplay(event, str) 
{
    
  const prop = event.target.dataset.prop;
  const tars = document.querySelectorAll(`[data-show='${prop}']`);
  const expand_icon_element = document.getElementById(`${prop}`);

  return new Promise((resolve, reject) => {
    if (!isNullOrUndefined(tars)) {
      for (let i = 0; i < tars.length; i++) {
        if (tars[i].style.display === 'none') {
          tars[i].style.display = str;
          if(expand_icon_element)
          {
            expand_icon_element.classList.remove('rotate-down');
            expand_icon_element.classList.add('rotate-up');
          }
        } else {
          tars[i].style.display = 'none';
          if(expand_icon_element)
          {
            expand_icon_element.classList.remove('rotate-up');
            expand_icon_element.classList.add('rotate-down');
          }
        }
      }
      resolve();
    } else {
      // target doesn't exist, reject
      reject('Target(s) do not exist');
    }
  });
}

// Class to dynamically create a new 'numeric' dropdown
class NumericDropdown 
{
  constructor(type) 
  {
    this.type = type;
    this.iterator = 1;
    this.condition = 1;
    this.opts = '<option value="all" selected>All</option>'; // options should be 'All' by default
  }

  buildNumericDropdown() 
  {
    // based on case type, we change iterator and/or condition
    switch (this.type) {
      case 'y':
      case 'year':
        this.iterator = new Date().getFullYear() - 119;
        this.condition = new Date().getFullYear();
        break;
      case 'm':
      case 'month':
        this.condition = 12;
        break;
      case 'd':
      case 'day':
        this.condition = 31;
        break;
    }

    // iterate through iterator and condition to build the options
    for (let i = this.iterator; i <= this.condition; i++) 
    {
      this.opts += `<option value='${i}'>`;
      this.opts += i;
      this.opts += '</option>';
    }
    return this.opts;
  }
}

function apply_filter_button_click() 
{
  var filter_search_text = document.getElementById('filter_search_text');
  var filter_sort_by = document.getElementById('filter_sort_by');
  var filter_records_per_page = document.getElementById('search_records_per_page');
  var filter_decending = document.getElementById('filter_decending');
  var reset_filters_button = document.getElementById('reset_case_filters_button');

  reset_filters_button.disabled = false;
  reset_filters_button.setAttribute('aria-disabled', 'false');
  //g_case_view_request.take = filter_records_perPage.value;
  g_case_view_request.sort = filter_sort_by.value;
  g_case_view_request.search_key = filter_search_text.value;
  g_case_view_request.descending = filter_decending.value === 'asc' ? false : true;

  get_case_set();
}

function reset_filter_button_click()
{
  var filter_search_text = document.getElementById('filter_search_text');
  var filter_search_type = document.getElementById('search_field_selection');
  var filter_case_status = document.getElementById('search_case_status');
  var filter_pregnancy_relatedness = document.getElementById('search_pregnancy_relatedness');
  var filter_sort_by = document.getElementById('filter_sort_by');
  var filter_records_per_page = document.getElementById('search_records_per_page');
  var filter_decending = document.getElementById('filter_decending');
  var date_of_review_begin = document.getElementById('review_begin_date');
  var date_of_review_end = document.getElementById('review_end_date');
  var date_of_death_begin = document.getElementById('death_begin_date');
  var date_of_death_end = document.getElementById('death_end_date');
  var review_dates_radio = document.getElementsByName('select_date_of_review_panel');
  var death_dates_radio = document.getElementsByName('select_date_of_death_panel');
  var reset_filters_button = document.getElementById('reset_case_filters_button');

  reset_filters_button.disabled = true;
  reset_filters_button.setAttribute('aria-disabled', 'true');

  g_case_view_request.sort = g_default_case_view_request.sort;
  filter_sort_by.value = 'by_' + g_default_case_view_request.sort;
  g_case_view_request.take = g_default_case_view_request.take;
  filter_records_per_page.value = g_default_case_view_request.take;
  g_case_view_request.search_key = null;
  filter_search_text.value = '';
  g_case_view_request.field_selection = g_default_case_view_request.field_selection;
  filter_search_type.value = g_default_case_view_request.field_selection;
  g_case_view_request.case_status = g_default_case_view_request.case_status;
  filter_case_status.value = g_default_case_view_request.case_status;
  g_case_view_request.pregnancy_relatedness = g_default_case_view_request.pregnancy_relatedness;
  filter_pregnancy_relatedness.value = g_default_case_view_request.pregnancy_relatedness;
  g_case_view_request.descending = g_default_case_view_request.descending;
  filter_decending.value = g_default_case_view_request.descending ? 'desc' : 'asc';
  if(date_of_review_begin && date_of_review_end)
  {
    date_of_review_begin.value = '1900-01-01';
    date_of_review_end.value = new Date().toISOString().split('T')[0];
  }
  if(date_of_death_begin && date_of_death_end)
  {
    date_of_death_begin.value = '1900-01-01';
    date_of_death_end.value = new Date().toISOString().split('T')[0];
  }
  review_dates_radio[0].checked = true;
  death_dates_radio[0].checked = true;
  date_of_review_panel_select('all');
  date_of_death_panel_select('all');
  g_filter.include_blank_date_of_reviews = true;
  g_filter.include_blank_date_of_deaths = true;

  get_case_set();
}

function result_checkbox_click(p_checkbox) 
{
  let value = p_checkbox.value;
  let clear_selection_button = document.getElementById('clear_all_selections_button');

  if (p_checkbox.checked) 
  {
    if (answer_summary.case_set.indexOf(value) < 0) 
    {
      answer_summary.case_set.push(value);
    }
  } 
  else 
  {
    let index = answer_summary.case_set.indexOf(value);

    if (index > -1) 
    {
      answer_summary.case_set.splice(index, 1);
    }
  }

  let el = document.getElementById('selected_case_list');
  let result = [];

  render_selected_case_list(result, answer_summary);
  el.innerHTML = result.join('');


  el = document.getElementById('case_result_pagination');
  result = [];
  render_pagination(result, g_case_view_request);
  el.innerHTML = result.join('');  

  el = document.getElementById('exported_cases_count');
  el.innerHTML = `Selected Cases for Export (${answer_summary.case_set.length}):`;

  var summary_of_selected_cases = document.getElementById(
      'summary_of_selected_cases'
  );
  var all_or_core = document.getElementById('selected_cases_all_or_core');
  var summary_of_selected_cases_result = render_summary_of_selected_cases(
      answer_summary
  );
  summary_of_selected_cases.innerHTML = summary_of_selected_cases_result;
  summary_of_selected_cases_result === ''
      ? all_or_core.innerHTML = 'All data,'
      : all_or_core.innerHTML = 'Custom data,';

  if(answer_summary.case_set.length == 0)
  {
    clear_selection_button.disabled = true;
    clear_selection_button.setAttribute('aria-disabled', 'true');
  }
  else
  {
    clear_selection_button.disabled = false;
    clear_selection_button.setAttribute('aria-disabled', 'false');
  }
  check_if_all_filtered_cases_selected();
}


function cart_checkbox_click(p_checkbox) 
{
    let value = p_checkbox.value;
    let index = answer_summary.case_set.indexOf(value);

    if (index > -1) 
    {
        answer_summary.case_set.splice(index, 1);
    }
  
    const search_result_input = document.getElementById(escape_HTML(value));
    search_result_input.checked = false;

    let el = document.getElementById('selected_case_list');
    let result = [];

    render_selected_case_list(result, answer_summary);
    el.innerHTML = result.join('');

    el = document.getElementById('exported_cases_count');
    el.innerHTML = `Cases to be included in export (${answer_summary.case_set.length}):`;

    el = document.getElementById('case_result_pagination');
    result = [];
    render_pagination(result, g_case_view_request);
    el.innerHTML = result.join('');

    var summary_of_selected_cases = document.getElementById(
      'summary_of_selected_cases'
    );
    var all_or_core = document.getElementById('selected_cases_all_or_core');
    var summary_of_selected_cases_result = render_summary_of_selected_cases(
      answer_summary
    );
    summary_of_selected_cases.innerHTML = summary_of_selected_cases_result;
    summary_of_selected_cases_result === ''
        ? all_or_core.innerHTML = 'All data,'
        : all_or_core.innerHTML = 'Custom data,';

    check_if_all_filtered_cases_selected();
}

function get_case_set() 
{
  var case_view_url =
    location.protocol +
    '//' +
    location.host +
    '/api/case_view' +
    g_case_view_request.get_query_string();

  $.ajax({
    url: case_view_url,
  }).done(function (case_view_response) {

    g_case_view_request.total_rows = case_view_response.total_rows;
    g_case_view_request.respone_rows = case_view_response.rows;

    render_search_result_list();

    el = document.getElementById('case_result_pagination');
    html = [];
    render_pagination(html, g_case_view_request);
    el.innerHTML = html.join('');

    check_if_all_filtered_cases_selected();
  });
}



function render_search_result_list()
{
    if(g_case_view_request.respone_rows == null) return;

    let el = document.getElementById('search_result_list');
    let html = [];

    for (let i = 0; i < g_case_view_request.respone_rows.length; i++) {
      let item = g_case_view_request.respone_rows[i];
      let value_list = item.value;

      selected_dictionary[item.id] = value_list;

      let checked = '';
      let index = answer_summary.case_set.indexOf(item.id);

      if (index > -1) {
        checked = 'checked=true';
      }

      // Items generated after user applies filters
      html.push(`
					<tr class="tr font-weight-normal">
						<td class="td" data-type="date_created" width="38" align="center">
							<input id=${escape_HTML(item.id)}
										 type="checkbox"
										 value=${escape_HTML(item.id)}
										 type="checkbox"
										 onclick="result_checkbox_click(this)" ${checked} />
							<label for="${escape_HTML(item.id)}" class="sr-only">${escape_HTML(item.id)}</label>
						</td>
						<td class="td" data-type="date_last_updated">
							${escape_HTML(value_list.date_last_updated)
                .replace(/%20/g, ' ')
                .replace(/%3A/g, '-')} <br/> ${escape_HTML(
        value_list.last_updated_by
      )}
						</td>
						<td class="td" data-type="jurisdiction_id">
							${escape_HTML(value_list.last_name)
                .replace(/%20/g, ' ')
                .replace(/%3A/g, '-')}, ${escape_HTML(value_list.first_name)
        .replace(/%20/g, ' ')
        .replace(/%3A/g, '-')} ${escape_HTML(value_list.middle_name)
        .replace(/%20/g, ' ')
        .replace(/%3A/g, '-')} [${escape_HTML(value_list.jurisdiction_id)}]  
						</td>
						<td class="td" data-type="record_id">
							${escape_HTML(value_list.record_id).replace(/%20/g, ' ').replace(/%3A/g, '-')}
						</td>
						<td class="td" data-type="date_of_death">
						${
              value_list.date_of_death_year != null
                ? escape_HTML(value_list.date_of_death_year)
                : ''
            }-${
        value_list.date_of_death_month != null
          ? escape_HTML(value_list.date_of_death_month)
          : ''
      }
						</td>
						<td class="td" data-type="committee_review_date">
						${
              value_list.date_of_committee_review != null
                ? value_list.date_of_committee_review
                : 'N/A'
            }
						</td>
						<td class="td" data-type="agency_case_id">
							${escape_HTML(value_list.agency_case_id).replace(/%20/g, ' ').replace(/%3A/g, '-')}
						</td>
						<td class="td" data-type="date_last_updated">
							${escape_HTML(value_list.date_last_updated)
                .replace(/%20/g, ' ')
                .replace(/%3A/g, '-')}<br/>
							${escape_HTML(value_list.created_by).replace(/%20/g, ' ').replace(/%3A/g, '-')}
						</td>
					</tr>
				`);
      
    }

    el.innerHTML = html.join('');
}
function render_selected_case_list(p_result, p_answer_summary) 
{
  for (let i = 0; i < p_answer_summary.case_set.length; i++) 
  {
    let item_id = p_answer_summary.case_set[i];
    let value_list = selected_dictionary[item_id];
    const checked = p_answer_summary.case_set.includes(item_id)
      ? 'checked=true'
      : '';
    // Items generated after user applies filters
    p_result.push(`
			<tr class="tr font-weight-normal">
				<td class="td" data-type="date_created" width="38" align="center">
					<input id=${escape_HTML(item_id)}
								 type="checkbox"
								 value=${escape_HTML(item_id)}
								 type="checkbox"
								 onclick="cart_checkbox_click(this)" ${checked} />
					<label for="${escape_HTML(item_id)}" class="sr-only">${escape_HTML(item_id)}</label>
				</td>
				<td class="td" data-type="date_last_updated">
					${escape_HTML(value_list.date_last_updated)
            .replace(/%20/g, ' ')
            .replace(/%3A/g, '-')} <br/> ${escape_HTML(value_list.last_updated_by)}
				</td>
				<td class="td" data-type="jurisdiction_id">
					${escape_HTML(value_list.last_name)
            .replace(/%20/g, ' ')
            .replace(/%3A/g, '-')}, ${escape_HTML(value_list.first_name)
      .replace(/%20/g, ' ')
      .replace(/%3A/g, '-')} ${escape_HTML(value_list.middle_name)
      .replace(/%20/g, ' ')
      .replace(/%3A/g, '-')} [${escape_HTML(value_list.jurisdiction_id)}]  
				</td>
				<td class="td" data-type="record_id">
					${escape_HTML(value_list.record_id).replace(/%20/g, ' ').replace(/%3A/g, '-')}
				</td>
				<td class="td" data-type="date_of_death">
				${
          value_list.date_of_death_year != null
            ? escape_HTML(value_list.date_of_death_year)
            : ''
        }-${
      value_list.date_of_death_month != null
        ? escape_HTML(value_list.date_of_death_month)
        : ''
    }
				</td>
				<td class="td" data-type="committee_review_date">
				${
          value_list.date_of_committee_review != null
            ? value_list.date_of_committee_review
            : 'N/A'
        }
				</td>
				<td class="td" data-type="agency_case_id">
					${escape_HTML(value_list.agency_case_id).replace(/%20/g, ' ').replace(/%3A/g, '-')}
				</td>
				<td class="td" data-type="date_last_updated">
					${escape_HTML(value_list.date_last_updated)
            .replace(/%20/g, ' ')
            .replace(/%3A/g, '-')}<br/>
					${escape_HTML(value_list.created_by).replace(/%20/g, ' ').replace(/%3A/g, '-')}
				</td>
			</tr>
		`);
  }
}

function de_identified_search_click(p_reset_filter = false) 
{
  let search_form_control = document.getElementById('de_identify_search_text');
  let form_filter_form_control = document.getElementById('de_identify_form_filter');
  let reset_filter_button = document.getElementById('reset_de_identified_filters_button');
  let search_text = '';
  if (!p_reset_filter)
  {
    g_filter.selected_form = form_filter_form_control.value;
    search_text = search_form_control.value;
    reset_filter_button.disabled = false;
    reset_filter_button.setAttribute('aria-disabled', 'false');
  }
  else
  {
    form_filter_form_control.value = '';
    search_form_control.value = '';
    g_filter.selected_form = form_filter_form_control.value;
    search_text = search_form_control.value;
    reset_filter_button.disabled = true;
    reset_filter_button.setAttribute('aria-disabled', 'true');
  }

  let de_identify_search_result_list = document.getElementById('de_identify_search_result_list');

  g_de_identified_search_result.clear();
  get_de_identified_search_results(g_metadata, '', search_text, g_filter.selected_form);
  de_identify_search_result_list.innerHTML = render_de_identified_search_result();
}

function get_de_identified_search_results(p_node, p_path,  p_search_text, p_form)
{
    switch(p_node.type.toLowerCase())
    {
        case 'form':
        case 'app':
        case 'group':
        case 'grid':
            if
            (
                p_form != null &&
                p_form != '' && 
                p_node.type.toLowerCase() == 'form'
            )
            {
                if(p_form.toLowerCase() != p_node.name.toLowerCase())
                {
                    return;
                }
            }
            for(let i = 0; i < p_node.children.length; i++)
            {
                const child = p_node.children[i];
                get_de_identified_search_results(child, `${p_path}/${child.name}`,  p_search_text, p_form)
            }
        default:
            if
            (
                p_node.type.toLowerCase() == 'label' ||
                p_node.type.toLowerCase() == 'mirror' ||
                p_node.type.toLowerCase() == 'group' ||
                p_node.type.toLowerCase() == 'grid' ||
                p_node.type.toLowerCase() == 'form' ||
                p_node.type.toLowerCase() == 'button' ||
                p_node.sass_export_name == null ||
                p_node.sass_export_name == ''

            )
            {
                return;
            }

            if
            (
                p_node.name.indexOf(p_search_text) > -1 ||
                p_path.indexOf(p_search_text) > -1 ||
                p_node.prompt.indexOf(p_search_text) > -1 ||
                (
                    p_node.sass_export_name != null &&
                    p_node.sass_export_name.indexOf(p_search_text) > -1
                )
            )
            {
                g_de_identified_search_result.set(p_path, { node: p_node, path:p_path });
            }
            break;
    }
}

function render_de_identified_search_result() 
{
    const result = [];
    let index = 0;
    for(const item of g_de_identified_search_result)
    {
        //render_de_identified_search_result
        result.push(render_de_identified_search_result_item(item[1], index));
        index++;
    }

    if(g_de_identified_search_result.size === 0)
    {
        return `
            <tr class="tr" colspan="4" align="center">
                <td class="td" colspan="4">No results found</td>
            </tr>
        `;
    }

    return result.join("");
}

function render_de_identified_search_result_item(p_item, index) 
{
  let item_id = p_item.path.replace(/\//g, '-');
  selected_metadata_dictionary.set(item_id, p_item.node);
  const checked = answer_summary.de_identified_field_set.includes(item_id);
  return `<tr class="tr">
				<td class="td text-center" width="38">
					<input style="margin-left: .1rem !important;" class="form-check-input big-checkbox" id="unique_id_1" type="checkbox" onclick="de_identified_result_checkbox_click(this)" value="${item_id}"${
    checked ? ' checked=true' : ''
  }/>
					<label for="unique_id_1" class="sr-only">unique_id_1</label>
				</td>
				<td style="padding: 0px !important;" class="td">
					<table class="table">
						<thead class="thead">
							<tr
                                tabindex="0"
                                role="button"
                                onclick="handleElementDisplay(event, 'table-row', 'none')"
                                data-prop="search--${p_item.path}"
                                onkeydown="if(event.key==='Enter'||event.key===' '){handleElementDisplay(event, 'table-row', 'none');}"
                                style="cursor:pointer;"
                                class="tr"
                            >
								<th style="background-color: ${index % 2 === 1 ? '#f5f5f5' : '#ffff'};border:none !important;padding: .75rem !important;" class="th" colspan="4" scope="colgroup">
                                    <button
                                        style="cursor:pointer; background:transparent; border:none; width:100%;"
                                        class="anti-btn w-100 row no-gutters align-items-center justify-content-between"
                                        data-prop="search--${p_item.path}"
                                        tabindex="-1"
                                    >
                                        <span class="pointer-none">
                                            [${p_item.node.sass_export_name == null ? '' : p_item.node.sass_export_name}]  <strong>Path:</strong> ${p_item.path}
                                        </span>
                                        <span
                                            id="search--${item_id.replace(/-/g, '/')}"
                                            class="x24 fill-p cdc-icon-chevron-right rotate-down"
                                            style="cursor:pointer;"
                                            onclick="handleElementDisplay(event, 'table-row', 'none')"
                                        ></span>
                                    </button>
								</th>
							</tr>
						</thead>
						<thead class="thead">
							<tr class="header-level-2" data-show="search--${p_item.path}" style="display: none">
								<th class="th" scope="col">Name</th>
								<th class="th" scope="col">Type</th>
								<th class="th" scope="col">Prompt</th>
								<th class="th" scope="col">Values</th>
							</tr>
						</thead>
						<tbody class="tbody">
							<tr class="tr" data-show="search--${p_item.path}" style="display: none">
								<td class="td">${p_item.node.name}</td>
								<td class="td">${p_item.node.type}</td>
								<td class="td">${p_item.node.prompt}</td>
								<td class="td"></td>
							</tr>
						</tbody>
					</table>
				</td>
			</tr>`;
}

function render_standard_de_identify_fields(p_paths) 
{
  let result = '';

  for (let i = 0; i < p_paths.paths.length; i++) {
    let path = p_paths.paths[i];
    result += `
			<tr class="tr">
				<td class="td">
					<strong>Path:</strong> ${path}
				</td>
			</tr>
		`;
  }

  return result;
}

function render_de_identify_form_filter(p_filter) 
{
  const result = [];

  result.push(`<option value="">(Any Form)</option>`);

  for (let i = 0; i < g_metadata.children.length; i++) 
  {
    let item = g_metadata.children[i];

    if (item.type.toLowerCase() == 'form') 
    {
      if (p_filter.selected_form == item.name) 
      {
        result.push
        (
          `<option value="${item.name}" selected>${item.prompt}</option>`
        );
      } 
      else 
      {
        result.push(`<option value="${item.name}">${item.prompt}</option>`);
      }
    }
  }

  return result.join('');
}

function render_selected_searched_summary() 
{

  const selectedFieldList = document.getElementById('selected_de_identified_field_list');
  selectedFieldList.innerHTML = render_selected_de_identified_list(answer_summary);

  const de_identify_search_result_list = document.getElementById('de_identify_search_result_list');
  de_identify_search_result_list.innerHTML = render_de_identified_search_result(g_metadata.children);
  const countEl = document.getElementById('de_identified_count');
  countEl.innerHTML = `De-Identified (Selected) Fields (${answer_summary.de_identified_field_set.length})`;
}

function de_identified_clear_all_click() 
{
    answer_summary.de_identified_field_set = [];
    render_selected_searched_summary();
    render_summary_section();
}

function de_identified_clear_selected_search_result_click() 
{

    for(const [key, value] of g_de_identified_search_result)
    {
        const path = value.path;
        const key = `${path.replace(/\//g, '-')}`;
        const selected_index = answer_summary.de_identified_field_set.indexOf(key);
        if(selected_index > -1)
        {
            //answer_summary.de_identified_field_set.push(key);
            //selected_metadata_dictionary.set(key, g_path_to_node.get(path));

            answer_summary.de_identified_field_set.splice(selected_index, 1);
        }

    }


    render_selected_searched_summary();
    render_summary_section();
}

function de_identified_select_all_click() 
{
    for(const [key, value] of g_de_identified_search_result)
    {
        const path = value.path;
        const key = `${path.replace(/\//g, '-')}`;
        if(answer_summary.de_identified_field_set.indexOf(key) < 0)
        {
            answer_summary.de_identified_field_set.push(key);
            selected_metadata_dictionary.set(key, g_path_to_node.get(path));
        }
    }

    let de_identify_search_result_list = document.getElementById('de_identify_search_result_list');

    de_identify_search_result_list.innerHTML = render_de_identified_search_result();
    render_selected_searched_summary();
    render_summary_section();
}

function add_standard_de_identified_fields_click()
{
    for(const i in g_standard_de_identified_list.paths)
    {
        const path = g_standard_de_identified_list.paths[i];
        const key = `-${path.replace(/\//g, '-')}`;
        if(answer_summary.de_identified_field_set.indexOf(key) < 0)
        {
            answer_summary.de_identified_field_set.push(key);
            selected_metadata_dictionary.set(key, g_path_to_node.get(`/${path}`));
        }
    }

    let de_identify_search_result_list = document.getElementById('de_identify_search_result_list');

    de_identify_search_result_list.innerHTML = render_de_identified_search_result();
    render_selected_searched_summary();
    render_summary_section();
}

function de_identified_result_checkbox_click(p_checkbox) 
{
  const value = p_checkbox.value;
  const index = answer_summary.de_identified_field_set.indexOf(value);
  if (p_checkbox.checked) 
  {
    if (index < 0) 
    {
      answer_summary.de_identified_field_set.push(value);
    }
  } 
  else 
  {
    if (index > -1) 
    {
      answer_summary.de_identified_field_set.splice(index, 1);
    }
  }

  render_selected_searched_summary();
  render_summary_section(p_checkbox);
}

function render_selected_de_identified_list(p_answer_summary) 
{
  if(p_answer_summary.de_identified_field_set.length === 0)
    return `
        <tr class="tr" colspan="4" align="center">
            <td class="td" colspan="4">No selected fields</td>
        </tr>
    `;

  return p_answer_summary.de_identified_field_set
    .map((item_id, index) => {
        if( !selected_metadata_dictionary.has(item_id))
        {
            return '';
        }

      const value_list = selected_metadata_dictionary.get(item_id);
      return `<tr class="tr">
				<td class="td text-center" width="38">
					<input style="margin-left: .1rem;" class="form-check-input big-checkbox" id="unique_id_1" type="checkbox" onclick="de_identified_result_checkbox_click(this)" value="${item_id}" checked=true />
					<label for="unique_id_1" class="sr-only">unique_id_1</label>
				</td>
				<td style="padding: 0px !important;">
					<table class="table rounded-0 mb-0">
						<thead class="thead">
                                <tr class="tr"
                                    onclick="handleElementDisplay(event, 'table-row', 'none')"
                                    data-prop="selected--${item_id.replace(/-/g, '/')}"
                                    tabindex="0"
                                    role="button"
                                    onkeydown="if(event.key==='Enter'||event.key===' '){handleElementDisplay(event, 'table-row', 'none');}"
                                    style="cursor:pointer;${index % 2 === 1 ? 'background-color: #f5f5f5;' : ''}">
                                    <th style="padding-top: 1rem !important;padding-bottom: 1rem !important;" class="th" colspan="4" scope="colgroup">
                                        <button
                                            class="anti-btn w-100 row no-gutters align-items-center justify-content-between"
                                            data-prop="selected--${item_id.replace(/-/g, '/')}"
                                            tabindex="-1"  // Prevents button from being focused
                                            style="cursor:pointer;background:transparent;border:none;">
                                            <span class="pointer-none">
                                                [${value_list.sass_export_name == null ? '' : value_list.sass_export_name}] <strong>Path:</strong>${item_id.replace(/-/g, '/')}
                                            </span>
                                            <span id="selected--${item_id.replace(/-/g, '/')}" class="x24 fill-p cdc-icon-chevron-right rotate-down" style="cursor:pointer;"></span>
                                        </button>
                                    </th>
                                </tr>
						</thead>
						<thead class="thead">
							<tr class="header-level-2" data-show="selected--${item_id.replace(/-/g,'/')}" style="display: none;">
								<th class="th" scope="col">Name</th>
								<th class="th" scope="col">Type</th>
								<th class="th" scope="col">Prompt</th>
								<th class="th" scope="col">Values</th>
							</tr>
						</thead>
						<tbody class="tbody">
							<tr class="tr" data-show="selected--${item_id.replace(/-/g,'/')}" style="display: none;">
								<td class="td">${value_list != null ? value_list.name : ''}</td>
								<td class="td">${value_list != null ? value_list.type : ''}</td>
								<td class="td">${value_list != null ? value_list.prompt : ''}</td>
								<td class="td"></td>
							</tr>
						</tbody>
					</table>
				</td>
			</tr>
		`;
    })
    .join('');
}


function render_field_selection(p_sort)
{
	const sort_list = [
        {
            value : 'all',
            display : '-- All --'
        },
        {
            value : 'by_agency_case_id',
            display : 'Agency-Based Case Identifier'
        },
        {
            value : 'by_record_id',
            display : 'Record Id'
        },
        {
            value : 'by_last_name',
            display : 'Last Name'
        },
        {
            value : 'by_first_name',
            display : 'First Name'
        },
        {
            value : 'by_middle_name',
            display : 'Middle Name'
        },
        {
            value : 'by_state_of_death',
            display : 'State of Death'
        },
        {
            value : 'by_year_of_death',
            display : 'Year of Death'
        },
        {
            value : 'by_month_of_death',
            display : 'Month of Death'
        },
        {
            value : 'by_committee_review_date',
            display : 'Committee Review Date'
        },
        {
            value : 'by_date_created',
            display : 'Date Created'
        },
        {
            value : 'by_date_last_updated',
            display : 'Date Last Updated'
        },
        {
            value : 'by_created_by',
            display : 'Created By'
        },
        {
            value : 'by_last_updated_by',
            display : 'Last Updated By'
        }
	];

    const f_result = [];

	sort_list.map((item) => {
       f_result.push(`<option value="${item.value}" ${item.value === p_sort.field_selection ? 'selected' : ''}>${item.display}</option>`);
    });

	return f_result.join('');
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

function render_sort_by_include_in_export(p_case_view_request) 
{
  const export_include_list = [
    'first_name',
    'middle_name',
    'last_name',
    'date_of_death_year',
    'date_of_death_month',
    'date_created',
    'created_by',
    'date_last_updated',
    'last_updated_by',
    'record_id',
    'agency_case_id',
    'date_of_committee_review',
    'jurisdiction_id',
  ];

  const result = [];

  export_include_list.map((item) => {
    result.push(
      `<option value="by_${item}" ${
        item === p_case_view_request.sort ? 'selected' : ''
      }>${capitalizeFirstLetter(item).replace(/_/g, ' ')}</option>`
    );
  });

  return result.join('');
}


function render_filter_records_per_page(p_sort)
{
    const sort_list = [25, 50, 100, 250, 500, 1000];
    const f_result = [];

    sort_list.map((item) => {
        f_result.push(`<option value="${item}" ${item == p_sort.take ? 'selected' : ''}>${item}</option>`)
    });

    return f_result.join('');
}

function case_filter_type_click(p_value) 
{
  answer_summary.case_filter_type = p_value.value.toLowerCase();

  var custom_case_filter = document.getElementById('custom_case_filter');
  if (p_value.value.toLowerCase() == 'custom') 
  {
    custom_case_filter.style.display = 'block';
    
  } 
  else 
  {
    custom_case_filter.style.display = 'none';
  }

  render_summary_section(p_value);
}


function case_file_type_click(p_value) 
{
  answer_summary.case_file_type = p_value.value.toLowerCase();

  render_summary_section(p_value);
}

function de_identify_filter_type_click(p_value) {
  var de_identify_filter_standard = document.getElementById(
    'de_identify_filter_standard'
  );
  var de_identify_filter = document.getElementById('de_identify_filter');

  /*
		set_answer_summary(event).then(updateSummarySection(event)).then(handleElementDisplay(event, 'block'))
	*/

  // Making this a promise so I can return a 'then' method
  return new Promise((resolve, reject) => {
    if (true) 
    {
      if (p_value.value.toLowerCase() == 'standard') 
      {
        de_identify_filter.style.display = 'none';
        de_identify_filter_standard.style.display = 'block';
      } 
      else if (p_value.value.toLowerCase() == 'custom') 
      {
        de_identify_filter_standard.style.display = 'none';
        de_identify_filter.style.display = 'block';
        de_identified_search_click(true);
      } 
      else 
      {
        de_identify_filter_standard.style.display = 'none';
        de_identify_filter.style.display = 'none';
      }
      answer_summary.de_identified_selection_type = p_value.value.toLowerCase();
      resolve();
    } 
    else 
    {
      reject();
    }
  });
}

function de_identify_search_text_change(p_value) {
  g_filter.search_text = p_value;
}

function filter_serach_text_change(p_value) {
  g_case_view_request.search_key = p_value;
}

function render_pagination(p_result, p_case_view_request) 
{
    let pagination_current_page = p_case_view_request.page;
    const pagination_number_of_pages = Math.ceil(p_case_view_request.total_rows / p_case_view_request.take);
    if(pagination_number_of_pages == 0)
    {
        pagination_current_page = 0;
    }
    if(p_case_view_request.total_rows > 0)
    {
        p_result.push(`
            <div>
                <button onclick="select_all_filtered_cases_click()" id="select_all_filtered_cases_button" class="btn primary-button">Select All ${Math.min(p_case_view_request.take, g_case_view_request.respone_rows.length)} Results</button>
            </div>
            <div class="ml-auto mr-3 d-flex align-items-center">
                <div>Showing ${(pagination_current_page - 1) * p_case_view_request.take + 1}-${Math.min(pagination_current_page * p_case_view_request.take, p_case_view_request.total_rows)} of ${p_case_view_request.total_rows} cases</div>
                <div class="row ml-2">
                <button ${pagination_current_page <= 1 ? 'disabled aria-disabled="true"' : ''} onclick="g_case_view_request.page=${1};get_case_set();" class="icon-button btn-tab-navigation reverse">
                    <span class="x24 cdc-icon-chevron-double-right"></span>
                </button>
                <button ${pagination_current_page <= 1 ? 'disabled aria-disabled="true"' : ''} onclick="g_case_view_request.page=${pagination_current_page - 1 <= 0 ? 1 : pagination_current_page - 1};get_case_set();" class="icon-button btn-tab-navigation reverse">
                    <span class="x24 cdc-icon-chevron-right"></span>
                </button>
                <button style="cursor: default;background-color: #ffffff;" tabindex="-1" class="icon-button btn-tab-navigation">
                    ${pagination_current_page}
                </button>
                <button ${pagination_current_page >= pagination_number_of_pages ? 'disabled aria-disabled="true"' : ''} onclick="g_case_view_request.page=${pagination_current_page + 1};get_case_set();" class="icon-button btn-tab-navigation">
                    <span class="x24 cdc-icon-chevron-right pt-1"></span>
                </button>
                <button ${pagination_current_page >= pagination_number_of_pages ? 'disabled aria-disabled="true"' : ''} onclick="g_case_view_request.page=${pagination_number_of_pages};get_case_set();" class="icon-button btn-tab-navigation">
                    <span class="x24 cdc-icon-chevron-double-right pt-1"></span>
                </button>
                </div>
            </div>
        `);
    }
    else
    {
        p_result.push(`
            <div>
                <button disabled aria-disabled="true" onclick="select_all_filtered_cases_click()" id="select_all_filtered_cases_button" class="btn primary-button">Select All Results</button>
            </div>
            <div class="ml-auto mr-3 d-flex align-items-center">
                <div>Showing 0-0 of 0 cases</div>
                <div class="row ml-2">
                <button tabindex="-1" disabled aria-disabled="true" class="icon-button btn-tab-navigation reverse">
                    <span class="x24 cdc-icon-chevron-double-right"></span>
                </button>
                <button tabindex="-1" disabled aria-disabled="true" class="icon-button btn-tab-navigation reverse">
                    <span class="x24 cdc-icon-chevron-right"></span>
                </button>
                <button disabled aria-disabled="true" tabindex="-1" class="icon-button btn-tab-navigation">
                    -
                </button>
                <button aria-disabled="true" disabled tabindex="-1" class="icon-button btn-tab-navigation">
                    <span class="x24 cdc-icon-chevron-right pt-1"></span>
                </button>
                <button aria-disabled="true" disabled tabindex="-1" class="icon-button btn-tab-navigation">
                    <span class="x24 cdc-icon-chevron-double-right pt-1"></span>
                </button>
                </div>
            </div>
        `);       
    }

    //p_result.push("<div id='case_result_pagination' class='table-pagination row align-items-center no-gutters'>");
//   p_result.push("<div class='col'>");
//   p_result.push("<div class='row no-gutters'>");
//   p_result.push("<p class='mb-0'>Total Records: ");
//   p_result.push('<strong>' + p_case_view_request.total_rows + '</strong>');
//   p_result.push('</p>');
//   p_result.push("<p class='mb-0 ml-2 mr-2'>|</p>");
//   p_result.push("<p class='mb-0'>Viewing Page(s): ");
//   p_result.push('<strong>' + pagination_current_page + '</strong> ');
//   p_result.push('of ');
//   p_result.push(
//     '<strong>' +
//     pagination_number_of_pages +
//       '</strong>'
//   );
//   p_result.push('</p>');
//   p_result.push('</div>');
//   p_result.push('</div>');
//   p_result.push(
//     "<div class='col row no-gutters align-items-center justify-content-end'>"
//   );
//   p_result.push("<p class='mb-0'>Select by page:</p>");
//   for (
//     let current_page = 1;
//     (current_page - 1) * p_case_view_request.take <
//     p_case_view_request.total_rows;
//     current_page++
//   ) 
//   {
//     p_result.push(
//       "<button type='button' class='table-btn-link btn btn-link' alt='select page " +
//         current_page +
//         "' onclick='g_case_view_request.page="
//     );
//     p_result.push(current_page);
//     p_result.push(";get_case_set();'>");
//     p_result.push(current_page);
//     p_result.push('</button>');
//   }
//   p_result.push('</div>');
//   //p_result.push("</div>");
}

function render_summary_de_identified_fields(p_answer_summary) {
  var de_identified_filtered_case_selections = document.getElementById('de_identified_filtered_case_selections');
  var summary_of_de_identified_fields = document.getElementById('summary_of_de_identified_fields');
  var header = `<div style="margin-left: 6.5rem !important;" class="d-flex font-weight-semi">Path:</div>`;
  let headers = [];
  let items = [];

  switch(p_answer_summary.de_identified_selection_type.toLowerCase())
  {
    case 'none':
        break;
    case 'standard':
        g_standard_de_identified_list.paths.map((item) => {
            headers.push(header);
            items.push(`
                <span>- ${item}</span>
            `);
        });
        break;
    case 'custom':
        p_answer_summary.de_identified_field_set.map((item_id) => {
                headers.push(header);
                items.push(`
                    <span>${item_id.replace(/^(-)(.*)/, '$1 $2')}</span>
                `);
        });
        break;
  }
  if(p_answer_summary.de_identified_selection_type.toLowerCase() === 'none')
  {
    de_identified_filtered_case_selections.innerHTML = '';
    summary_of_de_identified_fields.innerHTML = '';
    return;
  }
  else
  {
    de_identified_filtered_case_selections.innerHTML = headers.join('');
    summary_of_de_identified_fields.innerHTML = items.join('');
  }
}

function render_summary_of_selected_cases(p_answer_summary) {
  let selected_cases_result = [];
  let selected_cases_labels_result = [];
  let selected_case_label = '<div>&nbsp;</div>';

  switch (p_answer_summary.case_filter_type.toLowerCase()) {
    case 'all':
      break;

    case 'custom':
      selected_cases_labels_result.push(`<div class="d-flex flex-column col-md-3 mt-1 mb-1">`);
      selected_cases_result.push('<div class="d-flex flex-column col-md-9 mt-1 mb-1">');
      for (let i = 0; i < p_answer_summary.case_set.length; i++) {
        selected_cases_labels_result.push(selected_case_label);
        let value_list = selected_dictionary[p_answer_summary.case_set[i]];
        //let path = p_answer_summary.case_set[i];
        var first_name = escape_HTML(value_list.first_name).replace(/%20/g, ' ').replace(/%3A/g, '-');
        var middle_name = escape_HTML(value_list.middle_name).replace(/%20/g, ' ').replace(/%3A/g, '-');
        var last_name = escape_HTML(value_list.last_name).replace(/%20/g, ' ').replace(/%3A/g, '-');
        let text_value =
          ' ' +
          escape_HTML(value_list.date_last_updated)
            .replace(/%20/g, ' ')
            .replace(/%3A/g, '-') +
          ' ' +
          escape_HTML(value_list.last_updated_by) +
          ' ' +
          set_character_limit(last_name, 20) +
          ', ' +
          set_character_limit(first_name, 20) +
          ' ' +
          set_character_limit(middle_name, 20) +
          ' [' +
          escape_HTML(value_list.jurisdiction_id) +
          ']';
        selected_cases_result.push(`<span class="ml-3">-${text_value}</span>`);
      }
      selected_cases_labels_result.push('</div>')
      selected_cases_result.push('</div>')
      break;
  }
  if(p_answer_summary.case_set.length > 0)
    return selected_cases_labels_result.join('') + selected_cases_result.join('');
  else
    return '';
}

function check_if_all_filtered_cases_selected()
{
    let isAllSelected = false;

    for (let i = 0; i < g_case_view_request.respone_rows.length; i++) {
        let item = g_case_view_request.respone_rows[i];
        let value_list = item.value;

        //selected_dictionary[item.id] = value_list;

        let checked = '';
        let index = answer_summary.case_set.indexOf(item.id);

        if (index < 0)
        {
            isAllSelected = false;
            break;
        }
        else
        {
            isAllSelected = true;
        }
    }
    set_records_on_page_text()
}

function set_records_on_page_text()
{
    let count = g_case_view_request.respone_rows.length;
}

function select_all_filtered_cases_click()
{
    let clear_selection_button = document.getElementById('clear_all_selections_button');
    for (let i = 0; i <  g_case_view_request.respone_rows.length; i++) 
    {
        let item =  g_case_view_request.respone_rows[i];
        let value_list = item.value;
  
        selected_dictionary[item.id] = value_list;
  
        let checked = '';
        let index = answer_summary.case_set.indexOf(item.id);
  
        if (index < 0) 
        {
            answer_summary.case_set.push(item.id);
        }
    }

    clear_selection_button.disabled = false;
    clear_selection_button.setAttribute('aria-disabled', 'false');

    check_if_all_filtered_cases_selected()

    render_search_result_list();
  
    let el = document.getElementById('selected_case_list');
    let result = [];
  
    render_selected_case_list(result, answer_summary);
    el.innerHTML = result.join('');
  
    el = document.getElementById('exported_cases_count');
    el.innerHTML = `Selected Cases for Export (${answer_summary.case_set.length}):`;
  
    el = document.getElementById('case_result_pagination');
    result = [];
    render_pagination(result, g_case_view_request);
    el.innerHTML = result.join('');
  
    var summary_of_selected_cases = document.getElementById(
      'summary_of_selected_cases'
    );
    var all_or_core = document.getElementById('selected_cases_all_or_core');
    var summary_of_selected_cases_result = render_summary_of_selected_cases(
      answer_summary
    );
    summary_of_selected_cases.innerHTML = summary_of_selected_cases_result;
    summary_of_selected_cases_result === ''
        ? all_or_core.innerHTML = 'All data,'
        : all_or_core.innerHTML = 'Custom data,';
}

function deselect_all_filtered_cases_click()
{
    let clear_selection_button = document.getElementById('clear_all_selections_button');
    answer_summary.case_set = [];

    render_search_result_list();
  
    let el = document.getElementById('selected_case_list');
    let result = [];
  
    render_selected_case_list(result, answer_summary);
    el.innerHTML = result.join('');
  
    el = document.getElementById('exported_cases_count');
    el.innerHTML = `Cases to be included in export (${answer_summary.case_set.length}):`;
  
    el = document.getElementById('case_result_pagination');
    result = [];
    render_pagination(result, g_case_view_request);
    el.innerHTML = result.join('');
  
    var summary_of_selected_cases = document.getElementById(
      'summary_of_selected_cases'
    );
    var all_or_core = document.getElementById('selected_cases_all_or_core');
    summary_of_selected_cases.innerHTML = render_summary_of_selected_cases(
      answer_summary
    );
    summary_of_selected_cases.innerHTML = summary_of_selected_cases_result;
    summary_of_selected_cases_result === ''
        ? all_or_core.innerHTML = 'All data,'
        : all_or_core.innerHTML = 'Custom data,';
    clear_selection_button.disabled = true;
    clear_selection_button.setAttribute('aria-disabled', 'true');

    check_if_all_filtered_cases_selected();
}

function search_case_status_onchange(p_value)
{
    if(g_case_view_request.case_status != p_value)
    {
        g_case_view_request.case_status = p_value;
        g_case_view_request.page = 1;
        g_case_view_request.skip = 0;
    }
}

function search_pregnancy_relatedness_onchange(p_value)
{
    if(g_case_view_request.pregnancy_relatedness != p_value)
    {
        g_case_view_request.pregnancy_relatedness = p_value;
        g_case_view_request.page = 1;
        g_case_view_request.skip = 0;
    }
}

function search_field_selection_onchange(p_value)
{
    if(g_case_view_request.field_selection != p_value)
    {
        g_case_view_request.field_selection = p_value;
        g_case_view_request.page = 1;
        g_case_view_request.skip = 0;
    }
}

function records_per_page_change(p_value)
{
    if(p_value != g_case_view_request.take)
    {
        g_case_view_request.take = p_value;
        g_case_view_request.page = 1;
        g_case_view_request.skip = 0;
    }
}

function render_export_report_type(p_value)
{
    const result = [];

    if(p_value == "all")
    {
        result.push(`<option value='all' selected>All</option>`)
    }
    else
    {
        result.push(`<option value='all'>All</option>`)
    }
    

    if(p_value == "core")
    {
        result.push(`<option value='core' selected>Core</option>`)
    }
    else
    {
        result.push(`<option value='core'>Core</option>`)
    }

    for(const sort_index in g_standard_export_report_set.sort_order)
    {
        const list_name =  g_standard_export_report_set.sort_order[sort_index];
        if(list_name == p_value)
        {
            result.push(`<option value='${list_name}' selected>${list_name}</option>`);
        }
        else
        {
            result.push(`<option value='${list_name}'>${list_name}</option>`);
        }
    }

    
    //result.push(`<option value=''></option>`)


    return result.join("");
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
        begin.style["display"] = "";
        end.style["display"] = "";
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
        begin.style["display"] = "";
        end.style["display"] = "";
    }


}

function render_pregnancy_filter(p_case_view)
{
    let display_date_of_reviews_html = "display:none;";
    let display_date_of_deaths_html = "display:none;";

    if(g_filter.include_blank_date_of_reviews == false)
    {
        display_date_of_reviews_html = "display:inline;";
    }
    
    if(g_filter.include_blank_date_of_deaths == false)
    {
        display_date_of_deaths_html = "display:inline;";
    }
    
    return `
        <div class="horizontal-control mt-2">
            <fieldset class="d-flex col-md-4 p-0">
                <legend class="font-weight-semi">Review Dates</legend>
                <div style="margin-left: 1.3rem !important;" class="form-check">
                    <input class="form-check-input big-radio" type="radio" onchange="date_of_review_panel_select(this.value)" name="select_date_of_review_panel" id="all_review_dates_radio" value="all" ${g_filter.include_blank_date_of_reviews == true ? 'checked="true"' : '' } />
                    <label style="margin-left: .4rem !important;" for="all_review_dates_radio">All dates</label>
                </div>
                <div class="form-check ml-4">
                    <input class="form-check-input big-radio" type="radio" onchange="date_of_review_panel_select(this.value)" name="select_date_of_review_panel" id="select_review_dates_radio"  value="select"  ${g_filter.include_blank_date_of_reviews == false ? 'checked="true"' : '' }/>
                    <label style="margin-left: .4rem !important;" for="select_review_dates_radio">Select dates</label>
                </div>
            </fieldset>
            <div class="d-flex col-md-4 pl-2 pr-1">
                <div class="col-md-12 p-0" id="date_of_review_panel_begin" style="${display_date_of_reviews_html};">
                    <label for="review_begin_date" class="font-weight-semi">Begin Review Date</label>
                    <input class="form-control" id="review_begin_date" type="date" value="${ControlFormatDate(g_filter.date_of_review.begin)}" max="${ControlFormatDate(g_filter.date_of_review.end)}" onblur="review_begin_date_change(this.value)" />
                </div>
            </div>
            <div class="d-flex col-md-4 pl-3 pr-2">
                <div class="col-md-12 p-0" id="date_of_review_panel_end" style="${display_date_of_reviews_html};">
                    <label for="review_end_date" class="font-weight-semi">End Review Date</label>
                    <input class="form-control" id="review_end_date" type="date" value="${ControlFormatDate(g_filter.date_of_review.end)}"  min="${ControlFormatDate(g_filter.date_of_review.begin)}" onblur="review_end_date_change(this.value)" />
                </div>
            </div>
        </div>
        <div class="horizontal-control mt-2">
            <fieldset class="d-flex col-md-4 p-0">
                <legend class="font-weight-semi">Dates of Death</legend>
                <div style="margin-left: 1.3rem !important;" class="form-check">
                    <input class="form-check-input big-radio" type="radio" onchange="date_of_death_panel_select(this.value)" name="select_date_of_death_panel" id="all_date_of_death_radio" value="all" ${g_filter.include_blank_date_of_deaths == true ? 'checked="true"' : '' } />
                    <label style="margin-left: .4rem !important;" for="all_date_of_death_radio">All dates</label>
                </div>
                <div class="form-check ml-4">
                    <input class="form-check-input big-radio" type="radio" onchange="date_of_death_panel_select(this.value)" name="select_date_of_death_panel" id="select_date_of_death_radio"  value="select"  ${g_filter.include_blank_date_of_deaths == false ? 'checked="true"' : '' }/>
                    <label style="margin-left: .4rem !important;" for="select_date_of_death_radio">Select dates</label>
                </div>
            </fieldset>
            <div class="d-flex col-md-4 pl-2 pr-1">
                <div class="col-md-12 p-0" id="date_of_death_panel_begin" style="${display_date_of_deaths_html}">
                    <label for="death_begin_date" class="font-weight-semi">Begin Date of Death</label>
                    <input class="form-control" id="death_begin_date" type="date" value="${ControlFormatDate(g_filter.date_of_death.begin)}" max="${ControlFormatDate(g_filter.date_of_death.end)}" onblur="death_begin_date_change(this.value)" />
                </div>
            </div>
            <div class="d-flex col-md-4 pl-3 pr-2">
                <div class="col-md-12 p-0" id="date_of_death_panel_end" style="${display_date_of_deaths_html}">
                    <label for="death_end_date" class="font-weight-semi">End Date of Death</label>
                    <input class="form-control" id="death_end_date" type="date" value="${ControlFormatDate(g_filter.date_of_death.end)}"  min="${ControlFormatDate(g_filter.date_of_death.begin)}" onblur="death_end_date_change(this.value)" />
                </div>
            </div>
        </div>
    `;
}