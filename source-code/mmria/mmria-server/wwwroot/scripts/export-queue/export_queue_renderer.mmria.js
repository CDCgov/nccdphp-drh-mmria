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

  let filter_decending = '';

  if (g_case_view_request.descending) {
    filter_decending = 'checked=true';
  }

  let export_report_type = render_export_report_type(p_answer_summary['all_or_core']);


  result.push(`
		<div class="row">
			<div class="col">
				<ol class="font-weight-bold pl-3">
					<li class="mb-4">
						<label for="grantee-name" class="mb-3">Confirm Jurisdiction name (added to each exported case):</label>
						<input id="grantee-name"
							 class="form-control w-auto"
							 type="text"
							 value="${p_answer_summary.grantee_name}"
							 disabled
							 readonly="true" />
					</li>				
					<li class="mb-4">
						<p class="mb-3">Select data to export (all data, core data, custom data):<small class="d-block mt-1">A zip file of the selected data will be downloaded directly to your computer's local "Downloads" folder.</small></p>
                        <label for="all-data" class="mb-0 font-weight-normal mr-2">Select Export Type</label>
						<select name="export-type"
											 id="all-data"
											 type="radio"
											 value="all"
											 data-prop="all_or_core"
											 onchange="setAnswerSummary(event).then(renderSummarySection(this))">
                                            ${export_report_type}
                        </select>
					</li>

					<li class="mb-4">
						<p class="mb-3">Select password setting:</p>
						<input name="password-protect"
											 id="password-protect-no"
											 type="radio"
											 value="no"
											 data-prop="is_encrypted"
											 ${p_answer_summary['is_encrypted'] == 'no' ? 'checked=true' : ''}
											 onchange="setAnswerSummary(event).then(handleElementDisplay(event, 'none')).then(renderSummarySection(this))" />
						<label for="password-protect-no" class="mb-0 font-weight-normal mr-2">No password</label>
						<input name="password-protect"
											 id="password-protect-yes"
											 type="radio"
											 value="yes"
											 data-prop="is_encrypted"
											 ${p_answer_summary['is_encrypted'] == 'yes' ? 'checked' : ''}
											 onchange="setAnswerSummary(event).then(handleElementDisplay(event, 'block')).then(renderSummarySection(this))" />
						<label for="password-protect-yes" class="mb-0 font-weight-normal">Set password</label>
						<div class="mt-2" data-show="is_encrypted"  style="display: ${
              p_answer_summary['is_encrypted'] == 'yes' ? 'block' : 'none'
            };">
							<label for="encryption-key" class="mb-2">Add encryption key</label>
							<input id="encryption-key"
										 class="form-control w-auto"
										 type="text"
										 value="${p_answer_summary.zip_key}" onchange="zip_key_changed(this.value)" />
						</div>
					</li>

					<li class="mb-4">
						<p class="mb-3">Select de-identified fields:</p>
						<input name="de-identify"
											 id="de-identify-none"
											 type="radio"
											 value="none"
											 data-prop="de_identified_selection_type"
											 ${
                         p_answer_summary.de_identified_selection_type == 'none'
                           ? 'checked=true'
                           : ''
                       }
											 onchange="de_identify_filter_type_click(this).then(renderSummarySection(this))" /> 
						<label for="de-identify-none" class="mb-0 font-weight-normal mr-2">None</label>
						<input name="de-identify"
											 id="de-identify-standard"
											 type="radio"
											 value="standard"
											 data-prop="de_identified_selection_type"
											 ${
                         p_answer_summary.de_identified_selection_type ==
                         'standard'
                           ? 'checked=true'
                           : ''
                       }
											 onchange="de_identify_filter_type_click(this).then(renderSummarySection(this))" />
						<label for="de-identify-standard" class="mb-0 font-weight-normal mr-2">Standard</label>
						<input name="de-identify"
											 id="de-identify-custom"
											 type="radio"
											 value="custom"
											 data-prop="de_identified_selection_type"
											 ${
                         p_answer_summary.de_identified_selection_type ==
                         'custom'
                           ? 'checked=true'
                           : ''
                       }
											 onchange="de_identify_filter_type_click(this).then(renderSummarySection(this))" />
						<label for="de-identify-custom" class="mb-0 font-weight-normal">Custom</label>
						<div id="de_identify_filter_standard" class="p-3 mt-3 bg-gray-l3" data-prop="de_identified_selection_type" style="display: ${
              p_answer_summary.de_identified_selection_type == 'standard'
                ? 'block'
                : 'none'
            }; border: 1px solid #bbb;">
							<div class="" style="border: 1px solid #bbbbbb; overflow:hidden; overflow-y: auto; max-height: 346px;">
								<table class="table rounded-0 mb-0">
									<thead class="thead">
										<tr class="tr bg-tertiary">
											<th class="th" colspan="2" scope="colgroup">
												<span class="row no-gutters justify-content-between">
													<span>Standard fields that will be de-identified</span>
												</span>
											</th>
										</tr>
									</thead>
									<tbody class="tbody">
										<tr class="tr">
											<td class="td">
												<table class="table rounded-0 mb-0">
													<tbody class="tbody">
														${render_standard_de_identify_fields(g_standard_de_identified_list)}
													</tbody>
												</table>
											</td>
										</tr>
									</tbody>
								</table>
							</div>
						</div>
						<div id="de_identify_filter" class="p-3 mt-3 bg-gray-l3" data-prop="de_identified_selection_type" style="display: ${
              p_answer_summary.de_identified_selection_type == 'custom'
                ? 'block'
                : 'none'
            }; border: 1px solid #bbb;">
							<p class="font-weight-bold">To customize, please search/choose your options below and check the resulting fields you want to de-identify from the list.</p>
							<div class="form-inline mb-2">
								<label for="de_identify_search_text" class="mr-2"> Search for:</label>
								<input type="text"
											 class="form-control mr-2"
											 id="de_identify_search_text"
											 value="" onchange="de_identify_search_text_change(this.value)"/>
								<select id="de_identify_form_filter" class="custom-select mr-2" onchange="">
									${render_de_identify_form_filter(p_filter)}
								</select>
								<button type="button" class="btn btn-tertiary" alt="clear search" onclick="init_inline_loader(de_identified_search_click)">Search</button>
								<span class="spinner-container spinner-inline ml-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>
							</div>
							<div class="row">

								<button class="btn btn-secondary ml-3" id="select-all-deidentified" onclick="de_identified_select_all_click()">
									Select All Search Results
								</button>
                                <button class="btn btn-secondary ml-3" id="select-all-deidentified-clear" onclick="de_identified_clear_selected_search_result_click()">
                                    Clear All Search Results
                                </button>
                                
                                    <button class="btn btn-secondary ml-3" id="add-all-standard-deidentified" onclick="add_standard_de_identified_fields_click()">
                                        Add Standard De-Identified Fields
                                    </button>
                                
							</div>
							
							<div class="mt-3" style="border: 1px solid #bbbbbb; overflow:hidden; overflow-y: auto; max-height: 346px;">
								<table class="table rounded-0 mb-0">
									<thead class="thead">
										<tr class="tr bg-tertiary">
											<th class="th" colspan="2" scope="colgroup">
												<span class="row no-gutters justify-content-between">
													<span>Search Results</span>
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
								<table class="table rounded-0 mb-0">
									<thead class="thead">
										<tr class="tr bg-tertiary">
											<th class="th" colspan="2" scope="colgroup">
												<span class="row no-gutters justify-content-between">
													<span id="de_identified_count">Selected Fields to De-identify (${
                            p_answer_summary.de_identified_field_set.length
                          })</span>
                                        <button class="btn btn-secondary ml-3" onclick="de_identified_clear_all_click()">
                                            Clear All Selected
                                        </button>
												</span>
											</th>
										</tr>
									</thead>
									<tbody class="tbody" id="selected_de_identified_field_list">
										${selected_de_identified_list}
									</tbody>
								</table>
							</div>
						</div>
					</li>

					<li class="mb-4">
						<p class="mb-3">Select cases to include in export:</p>
						<span class="font-weight-normal mr-2">
							<input id="case_filter_type_all"
										 type="radio"
										 name="case_filter_type"
										 value="all"
										 data-prop="case_filter_type"
										 ${p_answer_summary['case_filter_type'] == 'all' ? 'checked=true' : ''}
										 onclick="case_filter_type_click(this)" aria-label="All"/> All
						</span>
						<span for="case_filter_type_custom" class="font-weight-normal">
							<input id="case_filter_type_custom"
										 type="radio"
										 name="case_filter_type"
										 value="custom"
										 data-prop="case_filter_type"
										 ${p_answer_summary['case_filter_type'] == 'custom' ? 'checked=true' : ''}
										 onclick="case_filter_type_click(this)" aria-label="Custom" /> Custom
						</span>
						<ul class="font-weight-bold list-unstyled mt-3" id="custom_case_filter" style="display:${
              p_answer_summary['case_filter_type'] == 'custom'
                ? 'block'
                : 'none'
            }">
							<li class="mb-4" >
								<div class="form-inline mb-2">
									<label for="filter_search_text" class="font-weight-normal mr-2">Search for:</label>
									<input type="text"
												 class="form-control mr-2"
												 id="filter_search_text"
												 value=""
												 onchange="filter_serach_text_change(this.value)">
                                    <div class="form-inline mb-2">
                                        <label for="search_field_selection" class="font-weight-normal mr-2">Search in:</label>
                                        <select id="search_field_selection" class="custom-select" onchange="search_field_selection_onchange(this.value)">
                                            ${render_field_selection(g_case_view_request)}
                                        </select>
                                    </div>



								</div>

                            <div class="form-inline mb-2">
                                <label for="search_case_status" class="font-weight-normal mr-2">Case Status:</label>
                                <select id="search_case_status" class="custom-select" onchange="search_case_status_onchange(this.value)">
                                    ${renderSortCaseStatus(g_case_view_request)}
                                </select>
                            </div>

                            <div class="form-inline mb-2">
                                <label for="search_pregnancy_relatedness" class="font-weight-normal mr-2">Pregnancy Relatedness:</label>
                                <select id="search_pregnancy_relatedness" class="custom-select" onchange="search_pregnancy_relatedness_onchange(this.value)">
                                    ${renderPregnancyRelatedness(g_case_view_request)}
                                </select>
                            </div>

                            
                                  ${render_pregnancy_filter(g_case_view_request)}
                                
                            

                            <div class="form-inline mb-2">
                                <label for="filter_sort_by" class="font-weight-normal mr-2">Sort by:</label>
                                <select id="filter_sort_by" class="custom-select" >
                                    ${render_sort_by_include_in_export(g_case_view_request)}
                                </select>
                            </div>

							<div class="form-inline mb-2">
                                <label for="search_records_per_page" class="font-weight-normal mr-2">Records per page:</label>
                                <select id="search_records_per_page" class="custom-select" onchange="records_per_page_change(this.value);">
                                    ${render_filter_records_per_page(g_case_view_request)}
                                </select>
                            </div>

								<div class="form-inline mb-2">
									<label for="filter_decending" class="font-weight-normal mr-2">Descending order:</label>
									<input id="filter_decending" type="checkbox" ${filter_decending}/>
								</div>

                                <button type="button" class="btn btn-secondary" alt="apply filters" onclick="init_inline_loader(apply_filter_button_click)">Apply Filters</button>
                                <span class="spinner-container spinner-inline ml-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>
							</li>

							<li class="mb-3" style="overflow:hidden; overflow-y: auto; height: 360px; border: 1px solid #ced4da;">
								<div id='case_result_pagination' class='table-pagination row align-items-center no-gutters pt-1 pb-1 pl-2 pr-2'>
									${pagination_html.join('')}
								</div>
								<table class="table rounded-0 m-0">
									<thead class="thead">
										<tr class="tr bg-tertiary">
											<th class="th" colspan="14" scope="colgroup">
												<span class="row no-gutters">
													<span>Filtered Cases</span>
													<a href="javascript:select_all_filtered_cases_click()" id="selectAllLink" class="ml-2">Select all on this page</a>
												</span>
											</th>
										</tr>
									</thead>
									<thead class="thead">
										<tr class="tr">
											<th class="th" width="38" scope="col">x</th>
											<th class="th" scope="col">Date last updated <br/>Last updated by</th>
											<th class="th" scope="col">Name [Jurisdiction ID]</th>
											<th class="th" scope="col">Record ID</th>
											<th class="th" scope="col">Date of death</th>
											<th class="th" scope="col">Committee review date</th>
											<th class="th" scope="col">Agency case ID</th>
											<th class="th" scope="col">Date created<br/>Created by</th>
										</tr>
									</thead>
									<tbody id="search_result_list" class="tbody">
										<!-- items get dynamically generated -->
									</tbody>
								</table>
							</li>

							<li class="" style="overflow:hidden; overflow-y: auto; height: 360px; border: 1px solid #ced4da;">
								<table class="table rounded-0 mb-0">
									<thead class="thead">
										<tr class="tr bg-tertiary">
											<th class="th" colspan="14" scope="colgroup">
												<span class="row no-gutters">
													<span id="exported_cases_count">Cases to be included in export (${
                            p_answer_summary.case_set.length
                          }):</span>
													<a href="javascript:deselect_all_filtered_cases_click()" class="ml-2">Deselect all</a>
												</span>
											</th>
										</tr>
									</thead>
									<thead class="thead">
										<tr class="tr">
											<th class="th" width="38" scope="col">x</th>
											<th class="th" scope="col">Date last updated <br/>Last updated by</th>
											<th class="th" scope="col">Name [Jurisdiction ID]</th>
											<th class="th" scope="col">Record ID</th>
											<th class="th" scope="col">Date of death</th>
											<th class="th" scope="col">Committee review date</th>
											<th class="th" scope="col">Agency case ID</th>
											<th class="th" scope="col">Date created<br/>Created by</th>
										</tr>
									</thead>
									<tbody id="selected_case_list" class="tbody">
										${selected_case_list.join('')}
									</tbody>
								</table>
							</li>
						</ul>
					</li>

                    <li class="mb-4">
						<p class="mb-3">Select export file type:</p>
						<label for="case_file_type_csv" class="font-weight-normal mr-2">
							<input id="case_file_type_csv"
										 type="radio"
										 name="case_file_type"
										 value="csv"
										 data-prop="case_file_type"
										 ${p_answer_summary['case_file_type'] == 'csv' ? 'checked=true' : ''}
										 onclick="case_file_type_click(this)" /> CSV
						</label>
						<label for="case_file_type_xlsx" class="font-weight-normal">
							<input id="case_file_type_xlsx"
										 type="radio"
										 name="case_file_type"
										 value="xlsx"
										 data-prop="case_file_type"
										 ${p_answer_summary['case_file_type'] == 'xlsx' ? 'checked=true' : ''}
										 onclick="case_file_type_click(this)" /> Excel (.xlsx)
						</label>
                          </li>
				</ol>
			</div>
		</div>
		<div class="row">
			<div class="col">
				${export_queue_comfirm_render(p_answer_summary)}
			</div>
		</div>
	`);
  result.push(
    `<table class="table mt-4 mb-0">
			<thead class="thead">
			<tr class="tr bg-tertiary">
				<th class="th h4" colspan="8" scope="colgroup">
					Export Request History
				</th>
			</tr>
			<tr class="tr bg-quaternary">
				<th class="th" colspan="8" scope="colgroup">
					(*Please note that the export queue is deleted at midnight each day.)
				</th>
			</tr>
			<tr class="tr">
				<th class="th" scope="col">Date created</th>
				<th class="th" scope="col">Created by</th>
				<th class="th" scope="col">Date last updated</th>
				<th class="th" scope="col">Last updated by</th>
				<th class="th" scope="col">File name</th>
				<th class="th" scope="col">Export type</th>
				<th class="th" scope="col">Status</th>
				<th class="th" scope="col">Action</th>
				</tr>
				</thead>
				<tbody class="tbody">`
  );
  function td(content) {
    result.push(`<td class="td">${content}</td>`);
  }
  for (var i = 0; i < p_queue_data.length; i++) {
    var item = p_queue_data[i];

    result.push('<tr class="tr">');
    td(item.date_created);
    td(item.created_by);
    td(item.date_last_updated);
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
        return `<input type="button" value='${value}' onclick="${clickType}_export_item('${item._id}')" />`;
      }
      if (item.status == 'Confirmation Required') 
      {
        return buttonEl('Confirm') + '|' + buttonEl('Cancel');
      } 
      else if (item.status == 'Download') 
      {
        return buttonEl('Download');
      } 
      else if (item.status == 'Downloaded') 
      {
        return buttonEl('Download') + '|' + buttonEl('Delete');
      } 
      else 
      {
        return '';
      }
    }
    td(getButtons());
    result.push('</tr>');
  }
  result.push('</tbody>');
  result.push('</table>');

  return result;
}

function renderSummarySection(el = undefined) 
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
  var summary_of_de_identified_fields = document.getElementById(
    'summary_of_de_identified_fields'
  );
  summary_of_de_identified_fields.innerHTML = render_summary_de_identified_fields(
    answer_summary
  );

  var summary_of_selected_cases = document.getElementById(
    'summary_of_selected_cases'
  );
  summary_of_selected_cases.innerHTML = render_summary_of_selected_cases(
    answer_summary
  );
}

function export_queue_comfirm_render(p_answer_summary) 
{
  var result = `
		<div id="answer-summary-card" class="card">
			<div class="card-header bg-gray-l3">
				<h2 class="h5 font-weight-bold">Summary of your Export Data choices</h2>
			</div>
			<div class="card-body bg-gray-l3">
				<ul>
					<li>
						Export/Jurisdiction name: ${p_answer_summary.grantee_name}
					</li>
					<li>
						Export <span data-prop="all_or_core">${capitalizeFirstLetter(
              p_answer_summary.all_or_core
            )}</span> data
						<ul>
							<li>
								Exporting <span data-prop="all_or_core">${capitalizeFirstLetter(
                  p_answer_summary.all_or_core
                )}</span> data and a <a href="/data-dictionary" target="_blank">data dictionary</a>
							</li>
						</ul>
					</li>

					<li>
						Password protected: <span data-prop="is_encrypted">${capitalizeFirstLetter(
              p_answer_summary.is_encrypted
            )}</span>
					</li>

					<li>
						De-identify fields: <span data-prop="de_identified_selection_type">${capitalizeFirstLetter(
              p_answer_summary.de_identified_selection_type
            )}</span>
						<div id="summary_of_de_identified_fields" class="" style="max-height:120px; overflow:auto">
							${render_summary_de_identified_fields(p_answer_summary)}
						</div>
					</li>
					
					<li>
						Filter by: <span data-prop="case_filter_type">${capitalizeFirstLetter(
              p_answer_summary.case_filter_type
            )}</span>
						<div id="summary_of_selected_cases" class="" style="max-height:120px; overflow:auto">
							${render_summary_of_selected_cases(p_answer_summary)}
						</div>
					</li>
				</ul>
			</div>
			<div class="card-footer bg-gray-l3">
				<button class="btn btn-primary btn-lg w-100" onclick="add_new_all_export_item()">Confirm & Start Export</button>
			</div>
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

  return new Promise((resolve, reject) => {
    if (!isNullOrUndefined(tars)) {
      for (let i = 0; i < tars.length; i++) {
        if (tars[i].style.display === 'none') {
          tars[i].style.display = str;
        } else {
          tars[i].style.display = 'none';
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
  var filter_records_perPage = document.getElementById('filter_records_perPage');
  var filter_decending = document.getElementById('filter_decending');
  //g_case_view_request.take = filter_records_perPage.value;
  g_case_view_request.sort = filter_sort_by.value;
  g_case_view_request.search_key = filter_search_text.value;
  g_case_view_request.descending = filter_decending.checked;

  get_case_set();
  document.getElementById('selectAllLink').style.display = 'block';
}

function result_checkbox_click(p_checkbox) 
{
  let value = p_checkbox.value;

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

  el = document.getElementById('exported_cases_count');
  el.innerHTML = `Cases to be included in export (${answer_summary.case_set.length}):`;

  el = document.getElementById('case_result_pagination');
  result = [];
  render_pagination(result, g_case_view_request);
  el.innerHTML = result.join('');

  var summary_of_selected_cases = document.getElementById(
    'summary_of_selected_cases'
  );
  summary_of_selected_cases.innerHTML = render_summary_of_selected_cases(
    answer_summary
  );

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
  
    const search_result_input = document.getElementById(escape(value));
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

    var summary_of_selected_cases = document.getElementById
    (
    'summary_of_selected_cases'
    );
    summary_of_selected_cases.innerHTML = render_summary_of_selected_cases
    (
    answer_summary
    );

    check_if_all_filtered_cases_selected();
}

var g_case_view_request = {
  total_rows: 0,
  page: 1,
  skip: 0,
  take: 1000,
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
							<input id=${escape(item.id)}
										 type="checkbox"
										 value=${escape(item.id)}
										 type="checkbox"
										 onclick="result_checkbox_click(this)" ${checked} />
							<label for="${escape(item.id)}" class="sr-only">${escape(item.id)}</label>
						</td>
						<td class="td" data-type="date_last_updated">
							${escape(value_list.date_last_updated)
                .replace(/%20/g, ' ')
                .replace(/%3A/g, '-')} <br/> ${escape(
        value_list.last_updated_by
      )}
						</td>
						<td class="td" data-type="jurisdiction_id">
							${escape(value_list.last_name)
                .replace(/%20/g, ' ')
                .replace(/%3A/g, '-')}, ${escape(value_list.first_name)
        .replace(/%20/g, ' ')
        .replace(/%3A/g, '-')} ${escape(value_list.middle_name)
        .replace(/%20/g, ' ')
        .replace(/%3A/g, '-')} [${escape(value_list.jurisdiction_id)}]  
						</td>
						<td class="td" data-type="record_id">
							${escape(value_list.record_id).replace(/%20/g, ' ').replace(/%3A/g, '-')}
						</td>
						<td class="td" data-type="date_of_death">
						${
              value_list.date_of_death_year != null
                ? escape(value_list.date_of_death_year)
                : ''
            }-${
        value_list.date_of_death_month != null
          ? escape(value_list.date_of_death_month)
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
							${escape(value_list.agency_case_id).replace(/%20/g, ' ').replace(/%3A/g, '-')}
						</td>
						<td class="td" data-type="date_last_updated">
							${escape(value_list.date_last_updated)
                .replace(/%20/g, ' ')
                .replace(/%3A/g, '-')}<br/>
							${escape(value_list.created_by).replace(/%20/g, ' ').replace(/%3A/g, '-')}
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
					<input id=${escape(item_id)}
								 type="checkbox"
								 value=${escape(item_id)}
								 type="checkbox"
								 onclick="cart_checkbox_click(this)" ${checked} />
					<label for="${escape(item_id)}" class="sr-only">${escape(item_id)}</label>
				</td>
				<td class="td" data-type="date_last_updated">
					${escape(value_list.date_last_updated)
            .replace(/%20/g, ' ')
            .replace(/%3A/g, '-')} <br/> ${escape(value_list.last_updated_by)}
				</td>
				<td class="td" data-type="jurisdiction_id">
					${escape(value_list.last_name)
            .replace(/%20/g, ' ')
            .replace(/%3A/g, '-')}, ${escape(value_list.first_name)
      .replace(/%20/g, ' ')
      .replace(/%3A/g, '-')} ${escape(value_list.middle_name)
      .replace(/%20/g, ' ')
      .replace(/%3A/g, '-')} [${escape(value_list.jurisdiction_id)}]  
				</td>
				<td class="td" data-type="record_id">
					${escape(value_list.record_id).replace(/%20/g, ' ').replace(/%3A/g, '-')}
				</td>
				<td class="td" data-type="date_of_death">
				${
          value_list.date_of_death_year != null
            ? escape(value_list.date_of_death_year)
            : ''
        }-${
      value_list.date_of_death_month != null
        ? escape(value_list.date_of_death_month)
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
					${escape(value_list.agency_case_id).replace(/%20/g, ' ').replace(/%3A/g, '-')}
				</td>
				<td class="td" data-type="date_last_updated">
					${escape(value_list.date_last_updated)
            .replace(/%20/g, ' ')
            .replace(/%3A/g, '-')}<br/>
					${escape(value_list.created_by).replace(/%20/g, ' ').replace(/%3A/g, '-')}
				</td>
			</tr>
		`);
  }
}

function de_identified_search_click() 
{
  g_filter.selected_form = document.getElementById('de_identify_form_filter').value;

  const search_text = document.getElementById('de_identify_search_text').value;

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
    for(const item of g_de_identified_search_result)
    {
        //render_de_identified_search_result
        result.push(render_de_identified_search_result_item(item[1]));
    }

    return result.join("");
}

function render_de_identified_search_result_item(p_item) 
{
  let item_id = p_item.path.replace(/\//g, '-');
  selected_metadata_dictionary.set(item_id, p_item.node);
  const checked = answer_summary.de_identified_field_set.includes(item_id);
  return `<tr class="tr">
				<td class="td text-center" width="38">
					<input id="unique_id_1" type="checkbox" onclick="de_identified_result_checkbox_click(this)" value="${item_id}"${
    checked ? ' checked=true' : ''
  }/>
					<label for="unique_id_1" class="sr-only">unique_id_1</label>
				</td>
				<td class="td">
					<table class="table rounded-0 mb-0">
						<thead class="thead">
							<tr class="tr">
								<th class="th" colspan="4" scope="colgroup">
									<button class="anti-btn w-100 row no-gutters align-items-center justify-content-between"
													data-prop="search--${p_item.path}"
													onclick="handleElementDisplay(event, 'table-row', 'none')">
										<span class="pointer-none">[${p_item.node.sass_export_name == null ? '' : p_item.node.sass_export_name}]  <strong>Path:</strong> ${p_item.path}</span>
									</button>
								</th>
							</tr>
						</thead>
						<thead class="thead">
							<tr class="tr bg-white" data-show="search--${p_item.path}" style="display: none">
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

function renderSelectedSearchedSummary() 
{

  const selectedFieldList = document.getElementById('selected_de_identified_field_list');
  selectedFieldList.innerHTML = render_selected_de_identified_list(answer_summary);

  const de_identify_search_result_list = document.getElementById('de_identify_search_result_list');
  de_identify_search_result_list.innerHTML = render_de_identified_search_result(g_metadata.children);
  const countEl = document.getElementById('de_identified_count');
  countEl.innerHTML = `Fields that have been de-identified (${answer_summary.de_identified_field_set.length})`;
}

function de_identified_clear_all_click() 
{
    answer_summary.de_identified_field_set = [];
    renderSelectedSearchedSummary();
    renderSummarySection();
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


    renderSelectedSearchedSummary();
    renderSummarySection();
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
    renderSelectedSearchedSummary();
    renderSummarySection();
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
    renderSelectedSearchedSummary();
    renderSummarySection();
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

  renderSelectedSearchedSummary();
  renderSummarySection(p_checkbox);
}

function render_selected_de_identified_list(p_answer_summary) 
{
  return p_answer_summary.de_identified_field_set
    .map((item_id) => {
        if( !selected_metadata_dictionary.has(item_id))
        {
            return '';
        }

      const value_list = selected_metadata_dictionary.get(item_id);
      return `<tr class="tr">
				<td class="td text-center" width="38">
					<input id="unique_id_1" type="checkbox" onclick="de_identified_result_checkbox_click(this)" value="${item_id}" checked=true />
					<label for="unique_id_1" class="sr-only">unique_id_1</label>
				</td>
				<td class="td">
					<table class="table rounded-0 mb-0">
						<thead class="thead">
							<tr class="tr">
								<th class="th" colspan="4" scope="colgroup">
									<button class="anti-btn w-100 row no-gutters align-items-center justify-content-between"
													data-prop="selected--${item_id.replace(/-/g, '/')}"
													onclick="handleElementDisplay(event, 'table-row', 'none')">
										<span class="pointer-none"> [${value_list.sass_export_name == null ? '': value_list.sass_export_name}] <strong>Path:</strong>${item_id.replace(
                      /-/g,
                      '/'
                    )}</span>
									</button>
								</th>
							</tr>
						</thead>
						<thead class="thead">
							<tr class="tr bg-white" data-show="selected--${item_id.replace(
                /-/g,
                '/'
              )}" style="display: none;">
								<th class="th" scope="col">Name</th>
								<th class="th" scope="col">Type</th>
								<th class="th" scope="col">Prompt</th>
								<th class="th" scope="col">Values</th>
							</tr>
						</thead>
						<tbody class="tbody">
							<tr class="tr" data-show="selected--${item_id.replace(
                /-/g,
                '/'
              )}" style="display: none;">
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

  renderSummarySection(p_value);
}


function case_file_type_click(p_value) 
{
  answer_summary.case_file_type = p_value.value.toLowerCase();

  renderSummarySection(p_value);
}

function de_identify_filter_type_click(p_value) {
  var de_identify_filter_standard = document.getElementById(
    'de_identify_filter_standard'
  );
  var de_identify_filter = document.getElementById('de_identify_filter');

  /*
		setAnswerSummary(event).then(updateSummarySection(event)).then(handleElementDisplay(event, 'block'))
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
        de_identified_search_click();
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

    //p_result.push("<div id='case_result_pagination' class='table-pagination row align-items-center no-gutters'>");
  p_result.push("<div class='col'>");
  p_result.push("<div class='row no-gutters'>");
  p_result.push("<p class='mb-0'>Total Records: ");
  p_result.push('<strong>' + p_case_view_request.total_rows + '</strong>');
  p_result.push('</p>');
  p_result.push("<p class='mb-0 ml-2 mr-2'>|</p>");
  p_result.push("<p class='mb-0'>Viewing Page(s): ");
  p_result.push('<strong>' + pagination_current_page + '</strong> ');
  p_result.push('of ');
  p_result.push(
    '<strong>' +
    pagination_number_of_pages +
      '</strong>'
  );
  p_result.push('</p>');
  p_result.push('</div>');
  p_result.push('</div>');
  p_result.push(
    "<div class='col row no-gutters align-items-center justify-content-end'>"
  );
  p_result.push("<p class='mb-0'>Select by page:</p>");
  for (
    let current_page = 1;
    (current_page - 1) * p_case_view_request.take <
    p_case_view_request.total_rows;
    current_page++
  ) 
  {
    p_result.push(
      "<button type='button' class='table-btn-link btn btn-link' alt='select page " +
        current_page +
        "' onclick='g_case_view_request.page="
    );
    p_result.push(current_page);
    p_result.push(";get_case_set();'>");
    p_result.push(current_page);
    p_result.push('</button>');
  }
  p_result.push('</div>');
  //p_result.push("</div>");
}

function render_summary_de_identified_fields(p_answer_summary) {
  let result = [];
  function createRow(path) {
    result.push(`
					<tr class="tr">
						<td class="td">
							<strong>Path:</strong> ${path}
						</td>
					</tr>
				`);
  }
  switch (p_answer_summary.de_identified_selection_type.toLowerCase()) {
    case 'none':
      break;
    case 'standard':
      result.push("<table class='bg-white mt-1 w-100'>");
      for (let i = 0; i < g_standard_de_identified_list.paths.length; i++) {
        createRow(g_standard_de_identified_list.paths[i]);
      }
      result.push('</table>');
      break;

    case 'custom':
      result.push('<table>');
      for (
        let i = 0;
        i < p_answer_summary.de_identified_field_set.length;
        i++
      ) {
        createRow(p_answer_summary.de_identified_field_set[i]);
      }
      result.push('</table>');
      break;
  }

  return result.join('');
}

function render_summary_of_selected_cases(p_answer_summary) {
  let result = [];

  switch (p_answer_summary.case_filter_type.toLowerCase()) {
    case 'all':
      break;

    case 'custom':
      result.push('<table>');

      for (let i = 0; i < p_answer_summary.case_set.length; i++) {
        let value_list = selected_dictionary[p_answer_summary.case_set[i]];
        //let path = p_answer_summary.case_set[i];

        let text_value =
          escape(value_list.date_last_updated)
            .replace(/%20/g, ' ')
            .replace(/%3A/g, '-') +
          '<br/>' +
          escape(value_list.last_updated_by) +
          ' ' +
          escape(value_list.last_name)
            .replace(/%20/g, ' ')
            .replace(/%3A/g, '-') +
          ', ' +
          escape(value_list.first_name)
            .replace(/%20/g, ' ')
            .replace(/%3A/g, '-') +
          ' ' +
          escape(value_list.middle_name)
            .replace(/%20/g, ' ')
            .replace(/%3A/g, '-') +
          ' [' +
          escape(value_list.jurisdiction_id) +
          ']';
        result.push(`
						<tr class="tr">
							<td class="td">
							${text_value}
							</td>
						</tr>
					`);
      }
      result.push('</table>');
      break;
  }

  return result.join('');
}

function check_if_all_filtered_cases_selected()
{
    let isAllSelected = false;
    let selectAllLink = document.getElementById('selectAllLink');

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

    if (isAllSelected)
    {
        selectAllLink.style.display = 'none';
    }
    else
    {
        selectAllLink.style.display = 'block';
    }
    set_records_on_page_text()
}

function set_records_on_page_text()
{
    let count = g_case_view_request.respone_rows.length;
    let selectAllLink = document.getElementById('selectAllLink');

    if (count && count > 0)
    {
        selectAllLink.textContent = `Select all on this page (${count})`;
    }
    else
    {
        selectAllLink.textContent = "Select all on this page";
    }
}

function select_all_filtered_cases_click()
{
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

    check_if_all_filtered_cases_selected()

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
    summary_of_selected_cases.innerHTML = render_summary_of_selected_cases(
      answer_summary
    );
}

function deselect_all_filtered_cases_click()
{
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
    summary_of_selected_cases.innerHTML = render_summary_of_selected_cases(
      answer_summary
    );

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

        <div class="form-inline mt-3" style="margin-left:0px;margin-bottom:0px;padding-bottom:0px;">
        <table style="margin-top:-15px;border-colapse: inherit;margin-bottom:10px;">
        <tr style="margin-bottom:20px;height:50px;">
            <td class="font-weight-normal mr-2">
                Review Dates:
            </td>
            <td style="padding-left:15px">
                <label for="all_review_dates_radio" class="font-weight-normal mr-2" style="justify-content:left">
                <input type="radio" onchange="date_of_review_panel_select(this.value)" name="select_date_of_review_panel" id="all_review_dates_radio" value="all" ${g_filter.include_blank_date_of_reviews == true ? 'checked="true"' : '' } />
                &nbsp;All cases</label>
            </td>
            <td>
            <label for="select_review_dates_radio" class="font-weight-normal mr-2" style="justify-content:left">
            <input type="radio" onchange="date_of_review_panel_select(this.value)" name="select_date_of_review_panel" id="select_review_dates_radio"  value="select"  ${g_filter.include_blank_date_of_reviews == false ? 'checked="true"' : '' }/>
            &nbsp;Select dates</label>
            </td>
            
            <td>
                <span id="date_of_review_panel_begin" style="${display_date_of_reviews_html};">
                <label for="review_begin_date" class="font-weight-normal mr-2">Begin
                    &nbsp;<input id="review_begin_date" type="date" value="${ControlFormatDate(g_filter.date_of_review.begin)}" max="${ControlFormatDate(g_filter.date_of_review.end)}" onblur="review_begin_date_change(this.value)" />
                </label>
                </span>
            </td>
            <td>
                <span id="date_of_review_panel_end" style="${display_date_of_reviews_html};">
                    <label for="review_end_date" class="font-weight-normal mr-2">End
                        &nbsp;<input  id="review_end_date" type="date" value="${ControlFormatDate(g_filter.date_of_review.end)}"  min="${ControlFormatDate(g_filter.date_of_review.begin)}" onblur="review_end_date_change(this.value)" />
                    </label>
                </span>
            </td>
            
            </td>
        </tr>
        <tr style="margin-top:20px;">
            <td class="font-weight-normal mr-2">
                Dates of Death:
            </td>
            <td style="padding-left:15px">
                <label for="all_date_of_death_radio" class="font-weight-normal mr-2" style="justify-content:left">
                <input type="radio" onchange="date_of_death_panel_select(this.value)" name="select_date_of_death_panel" id="all_date_of_death_radio" value="all" ${g_filter.include_blank_date_of_deaths == true ? 'checked="true"' : '' } />
                &nbsp;All cases</label>
            </td>
            <td>
            <label for="select_date_of_death_radio" class="font-weight-normal mr-2" style="justify-content:left">
            <input type="radio" onchange="date_of_death_panel_select(this.value)" name="select_date_of_death_panel" id="select_date_of_death_radio"  value="select"  ${g_filter.include_blank_date_of_deaths == false ? 'checked="true"' : '' }/>
            &nbsp;Select dates</label>      
            </td>
            <td>
                <span id="date_of_death_panel_begin" style="${display_date_of_deaths_html}">
                <label for="death_begin_date" class="font-weight-normal mr-2">Begin
                    &nbsp;<input id="death_begin_date" type="date" value="${ControlFormatDate(g_filter.date_of_death.begin)}" max="${ControlFormatDate(g_filter.date_of_death.end)}" onblur="death_begin_date_change(this.value)" />
                </label>
                </span>
            </td>
            <td>
                <span id="date_of_death_panel_end" style="${display_date_of_deaths_html}">
                <label for="death_end_date" class="font-weight-normal mr-2">End
                    &nbsp;<input  id="death_end_date" type="date" value="${ControlFormatDate(g_filter.date_of_death.end)}"  min="${ControlFormatDate(g_filter.date_of_death.begin)}" onblur="death_end_date_change(this.value)" />
                </label>
                </span>
            </td>
            
        </tr>
        </table>
        <br/>
        </div>

`;

   

}