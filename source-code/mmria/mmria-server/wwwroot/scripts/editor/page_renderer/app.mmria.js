// Global function for offline status toggle
async function toggle_offline_status(caseId, caseIndex) {
    try {
        // Show loading state
        var button = document.getElementById('offline_toggle_' + caseIndex);
        var originalContent = button.innerHTML;
        button.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';

        // Make API call to toggle offline status
        var response = await fetch('/api/case_view/toggle-offline/' + caseId, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
        });

        var result = await response.json();
        
        if (response.ok && result.success) {
            // Update the button state
            var isOffline = result.is_offline;
            button.className = 'btn ' + (isOffline ? 'btn-primary' : 'btn-outline-secondary');
            button.title = isOffline ? 'Remove from offline use' : 'Mark for offline use';
            button.innerHTML = isOffline ? 'Remove from Offline List' : '<span class="x14 fill-p cdc-icon-download-cloud"></span> Add to Offline List';

            // Update the case data in the UI
            if (g_ui.case_view_list[caseIndex]) {
                g_ui.case_view_list[caseIndex].value.is_offline = isOffline;
                g_ui.case_view_list[caseIndex].value.offline_date = new Date().toISOString();
                g_ui.case_view_list[caseIndex].value.offline_by = g_user_name; // Assuming g_user_name is available
            }

            // Show success message
            show_message('Case offline status updated successfully.', 'success');

            // Refresh offline documents list
            refresh_offline_documents_list();
        } else {
            throw new Error(result.message || 'Failed to toggle offline status');
        }
    } catch (error) {
        console.log('Error toggling offline status:', error);
        show_message('Error updating offline status: ' + error.message, 'error');
    } finally {
        // Restore button state
        button.disabled = false;
    }
}

// Function to remove a case from offline list (called from offline documents table)
async function remove_from_offline_list(caseId) {
    try {
        // Show loading state
        const buttons = document.querySelectorAll(`button[onclick*="${caseId}"]`);
        buttons.forEach(button => {
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';
        });

        // Make API call to toggle offline status
        const response = await fetch('/api/case_view/toggle-offline/' + caseId, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
        });

        const result = await response.json();
        
        if (response.ok && result.success) {
            // Show success message
            show_message('Case removed from offline list successfully.', 'success');

            // Refresh offline documents list
            refresh_offline_documents_list();

            // Also refresh the main case list to update button states
            if (typeof get_case_set === 'function') {
                get_case_set();
            }
        } else {
            throw new Error(result.message || 'Failed to remove case from offline list');
        }
    } catch (error) {
        console.error('Error removing case from offline list:', error);
        show_message('Error removing case from offline list: ' + error.message, 'error');
    } finally {
        // Restore button states
        const buttons = document.querySelectorAll(`button[onclick*="${caseId}"]`);
        buttons.forEach(button => {
            button.disabled = false;
            button.innerHTML = 'Remove from Offline List';
        });
    }
}

// Function to refresh the offline documents list
async function refresh_offline_documents_list() {
    try {
        const offlineDocuments = await get_offline_documents();
        const offlineSection = document.getElementById('offline-documents-section');
        if (offlineSection) {
            offlineSection.innerHTML = render_offline_documents_table(offlineDocuments);
        }
    } catch (error) {
        console.error('Error refreshing offline documents list:', error);
    }
}

// Function to fetch offline documents
async function get_offline_documents() {
    try {
        const response = await fetch('/api/case_view/offline-documents', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
        });

        if (response.ok) {
            const result = await response.json();
            return result.rows || [];
        } else {
            console.error('Failed to fetch offline documents:', response.statusText);
            return [];
        }
    } catch (error) {
        console.error('Error fetching offline documents:', error);
        return [];
    }
}

// Function to render offline documents table
function render_offline_documents_table(offlineDocuments) {
    if (!offlineDocuments || offlineDocuments.length === 0) {
        return `
            <div class="alert alert-info" role="alert">
                No cases currently selected for offline work.
            </div>
        `;
    }

    const rows = offlineDocuments.map((item, i) => render_offline_document_item(item, i)).join('');

    return `
        <table class="table mb-0">
            <thead class='thead'>
                <tr class='tr bg-tertiary'>
                    <th class='th h4' colspan='6' scope='colgroup'>Cases Selected for Offline Work</th>
                </tr>
                <tr class='tr'>
                    <th class='th' scope='col'>Case Information</th>
                    <th class='th' scope='col'>Case Status</th>
                    <th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>
                    <th class='th' scope='col'>Created</th>
                    <th class='th' scope='col'>Last Updated</th>
                    <th class='th' scope='col' style="width: 115px;">Actions</th>
                </tr>
            </thead>
            <tbody class="tbody">
                ${rows}
            </tbody>
        </table>
    `;
}

// Function to render individual offline document item
function render_offline_document_item(item, i) {
    const caseStatuses = {
        "9999":"(blank)",	
        "1":"Abstracting (Incomplete)",
        "2":"Abstraction Complete",
        "3":"Ready for Review",
        "4":"Review Complete and Decision Entered",
        "5":"Out of Scope and Death Certificate Entered",
        "6":"False Positive and Death Certificate Entered",
        "0":"Vitals Import"
    }; 

    const caseID = item.id;
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.jurisdiction_id;
    const firstName = item.value.first_name;
    const lastName = item.value.last_name;
    const recordID = item.value.record_id ? `- (${item.value.record_id})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const currentCaseStatus = item.value.case_status == null ? '(blank)' : caseStatuses[item.value.case_status.toString()];
    const dateCreated = item.value.date_created ? new Date(item.value.date_created).toLocaleDateString('en-US') : '';
    const lastUpdatedDate = item.value.date_last_updated ? new Date(item.value.date_last_updated).toLocaleDateString('en-US') : '';
    
    let projectedReviewDate = item.value.review_date_projected ? new Date(item.value.review_date_projected).toLocaleDateString('en-US') : '';
    let actualReviewDate = item.value.review_date_actual ? new Date(item.value.review_date_actual).toLocaleDateString('en-US') : '';
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;

    return `
        <tr class="tr" path="${caseID}">
            <td class="td"><a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a></td>
            <td class="td">${currentCaseStatus}</td>
            <td class="td">${reviewDates}</td>
            <td class="td">${createdBy} - ${dateCreated}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">
                <button type="button" class="btn btn-primary" onclick="remove_from_offline_list('${caseID}')" style="line-height: 1.15">
                    Remove from Offline List
                </button>
            </td>
        </tr>
    `;
}

function app_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx) 
{
    if (window.location.hash == '')
      window.location.hash = "#/summary";
    g_pinned_case_count = 0;
    
    p_result.push("<section id='app_summary'>");

    /* The Intro */
    p_result.push("<div>");
    p_result.push("<h1 class='content-intro-title h2' tabindex='-1'>");
    g_is_data_analyst_mode ? p_result.push("Analyst ") : p_result.push("Abstractor ");
    p_result.push("Line Listing Summary</h1>");
    p_result.push("<div class='row no-gutters align-items-center'>");
    
    let is_read_only_html = '';
    
    if(g_is_data_analyst_mode)
    {
        is_read_only_html = "disabled='disabled'";
        // is_read_only_html = "disabled='disabled'";
    }

    p_result.push(`<button id='add-new-case' class='btn btn-primary' onclick='init_inline_loader(add_new_case_button_click)' ${is_read_only_html}>Add New Case</button>`);
    
    p_result.push("<span class='spinner-container spinner-inline ml-2'><span class='spinner-body text-primary'><span class='spinner'></span></span>");
    p_result.push("</div>");
    p_result.push("</div> <!-- end .content-intro -->");
    
    p_result.push(`<hr class="border-top mt-4 mb-4" />`);

    p_result.push("<div class='mb-4'>");
    /* Custom Search */
    p_result.push("<div class='form-inline mb-2'>");
    p_result.push("<label for='search_text_box' class='mr-2'> Search for:</label>");
    p_result.push("<input type='text' class='form-control mr-2' id='search_text_box' onchange='g_ui.case_view_request.search_key=this.value;' value='");
    if (g_ui.case_view_request.search_key != null) 
    {
        p_result.push(p_ui.case_view_request.search_key.replace(/'/g, "&quot;"));
    }
    p_result.push("' />");

    p_post_html_render.push("$('#search_text_box').bind(\"enterKey\",function(e){");
    p_post_html_render.push("	get_case_set();");
    p_post_html_render.push(" });");
    p_post_html_render.push("$('#search_text_box').keyup(function(e){");
    p_post_html_render.push("	if(e.keyCode == 13)");
    p_post_html_render.push("	{");
    p_post_html_render.push("	$(this).trigger(\"enterKey\");");
    p_post_html_render.push("	}");
    p_post_html_render.push("});");

    // Load offline documents after page render
    p_post_html_render.push("(async function() {");
    p_post_html_render.push("    const offlineDocuments = await get_offline_documents();");
    p_post_html_render.push("    const offlineSection = document.getElementById('offline-documents-section');");
    p_post_html_render.push("    if (offlineSection) {");
    p_post_html_render.push("        offlineSection.innerHTML = render_offline_documents_table(offlineDocuments);");
    p_post_html_render.push("    }");
    p_post_html_render.push("})();");

    p_result.push(
        `<div class="form-inline mb-2">
            <label for="search_field_selection" class="mr-2">Search in:</label>
            <select id="search_field_selection" name="search_field_selection" class="custom-select" onchange="search_field_selection_onchange(this.value)">
                ${render_field_selection(p_ui.case_view_request)}
            </select>
        </div>`
    );

    
    p_result.push("</div>");

    /* Case Status */
    p_result.push(
        `<div class="form-inline mb-2">
            <label for="search_case_status" class="mr-2">Case Status:</label>
            <select id="search_case_status" class="custom-select" onchange="search_case_status_onchange(this.value)">
                ${renderSortCaseStatus(p_ui.case_view_request)}
            </select>
        </div>`
    );
    p_result.push(
        `<div class="form-inline mb-2">
            <label for="search_pregnancy_relatedness" class="mr-2">Pregnancy Relatedness:</label>
            <select id="search_pregnancy_relatedness" class="custom-select" onchange="search_pregnancy_relatedness_onchange(this.value)">
                ${renderPregnancyRelatedness(p_ui.case_view_request)}
            </select>
        </div>`
    );
    /* Sort By: */
    p_result.push(
        `<div class="form-inline mb-2">
            <label for="search_sort_by" class="mr-2">Sort:</label>
            <select id="search_sort_by" class="custom-select" onchange="g_ui.case_view_request.sort = this.options[this.selectedIndex].value;">
                ${render_sort_by_include_in_export(p_ui.case_view_request)}
            </select>
        </div>`
    );

    /* Records per page */
    p_result.push(
        `<div class="form-inline mb-2">
            <label for="search_records_per_page" class="mr-2">Records per page:</label>
            <select id="search_records_per_page" class="custom-select" onchange="records_per_page_change(this.value);">
                ${render_filter_records_per_page(p_ui.case_view_request)}
            </select>
        </div>`
    );

    /* Descending Order */
    p_result.push(
        `<div class="form-inline mb-3">
            <label for="sort_descending" class="mr-2">Descending order:</label>
            <input id="sort_descending" name="sort_descending" type="checkbox" onchange="g_ui.case_view_request.descending = this.checked" ${p_ui.case_view_request.descending && 'checked' || ''} />
        </div>`
    );

    /* Apply Filters Btn */
    p_result.push(
        `<div class="form-inline">
            <button type="button" class="btn btn-secondary mr-2" alt="Apply filters" onclick="init_inline_loader(async function(){ await apply_filter_click() })">Apply Filters</button>
            <button type="button" class="btn btn-secondary" alt="Reset filters" id="search_command_button" onclick="init_inline_loader(function(){ clear_case_search() })">Reset</button>
            <span class="spinner-container spinner-inline ml-2"><span class="spinner-body text-primary"><span class="spinner"></span></span></span>
        </div>`
    );

    p_result.push("</div> <!-- end .content-intro -->");

    // Add offline documents section
    p_result.push("<div id='offline-documents-section' class='mb-4'>");
    p_result.push("</div>");

    let pagination_current_page = p_ui.case_view_request.page;
    const pagination_number_of_pages = Math.ceil(p_ui.case_view_request.total_rows / p_ui.case_view_request.take);
    if(pagination_number_of_pages == 0)
    {
        pagination_current_page = 0;
    }

    p_result.push("<div class='table-pagination row align-items-center no-gutters'>");
        p_result.push("<div class='col'>");
            p_result.push("<div class='row no-gutters'>");
                p_result.push("<p class='mb-0'>Total Records: ");
                    p_result.push("<strong>" + p_ui.case_view_request.total_rows + "</strong>");
                p_result.push("</p>");
                p_result.push("<p class='mb-0 ml-2 mr-2'>|</p>");
                p_result.push("<p class='mb-0'>Viewing Page(s): ");
                    p_result.push("<strong>" + pagination_current_page + "</strong> ");
                    p_result.push("of ");
                    p_result.push("<strong>" + pagination_number_of_pages + "</strong>");
                p_result.push("</p>");
            p_result.push("</div>");
        p_result.push("</div>");
        p_result.push("<div class='col row no-gutters align-items-center justify-content-end'>");
            p_result.push("<p class='mb-0'>Select by page:</p>");
            for(var current_page = 1; (current_page - 1) * p_ui.case_view_request.take < p_ui.case_view_request.total_rows; current_page++)
            {
                p_result.push("<button type='button' class='table-btn-link btn btn-link' alt='select page " + current_page + "' onclick='g_ui.case_view_request.page=");
                    p_result.push(current_page);
                    p_result.push(";get_case_set();'>");
                    p_result.push(current_page);
                p_result.push("</button>");
            }
        p_result.push("</div>");
    p_result.push("</div>");
    
    // Ensure case_view_list is defined and is an array
    if (!p_ui.case_view_list || !Array.isArray(p_ui.case_view_list)) {
        p_ui.case_view_list = [];
    }
    
    p_result.push(`
        <table class="table mb-0">
            <thead class='thead'>
                <tr class='tr bg-tertiary'>
                    <th class='th h4' colspan='7' scope='colgroup'>Case Listing</th>
                </tr>
                <tr class='tr'>
                    <th class='th' scope='col'>Case Information</th>
                    <th class='th' scope='col'>Case Status</th>
                    <th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>
                    <th class='th' scope='col'>Created</th>
                    <th class='th' scope='col'>Last Updated</th>
                    <th class='th' scope='col'>Currently Edited By</th>
                    ${!g_is_data_analyst_mode ? `<th class='th' scope='col' style="width: 115px;">Actions</th>` : ''}
                </tr>
            </thead>
            <tbody class="tbody">
                
                ${ !g_is_data_analyst_mode ? p_ui.case_view_list.map((item, i) => render_app_pinned_summary_result(item, i)).join('') : ""}

                ${p_ui.case_view_list.map((item, i) => render_app_summary_result_item(item, i)).join('')}
            </tbody>
        </table>
    `);

    p_result.push("<div class='table-pagination row align-items-center no-gutters'>");
        p_result.push("<div class='col'>");
            p_result.push("<div class='row no-gutters'>");
                p_result.push("<p class='mb-0'>Total Records: ");
                    p_result.push("<strong>" + p_ui.case_view_request.total_rows + "</strong>");
                p_result.push("</p>");
                p_result.push("<p class='mb-0 ml-2 mr-2'>|</p>");
                p_result.push("<p class='mb-0'>Viewing Page(s): ");
                    p_result.push("<strong>" + pagination_current_page + "</strong> ");
                    p_result.push("of ");
                    p_result.push("<strong>" + pagination_number_of_pages + "</strong>");
                p_result.push("</p>");
            p_result.push("</div>");
        p_result.push("</div>");
        p_result.push("<div class='col row no-gutters align-items-center justify-content-end'>");
            p_result.push("<p class='mb-0'>Select by page:</p>");
            for(var current_page = 1; (current_page - 1) * p_ui.case_view_request.take < p_ui.case_view_request.total_rows; current_page++) 
            {
                p_result.push("<button type='button' class='table-btn-link btn btn-link' alt='select page " + current_page + "' onclick='g_ui.case_view_request.page=");
                    p_result.push(current_page);
                    p_result.push(";get_case_set();'>");
                    p_result.push(current_page);
                p_result.push("</button>");
            }
        p_result.push("</div>");
    p_result.push("</div>");

    p_result.push("</section>");

    if (p_ui.url_state.path_array.length > 1) 
    {
        if(p_ui.url_state.path_array[1] == "field_search")
        {
            var search_text = p_ui.url_state.path_array[2].replace(/%20/g, " ");
            p_result.push("<section id='field_search_id'>");
            let is_case_read_only = false;
            let is_checked_out = is_case_checked_out(g_data);
            let case_is_locked = is_case_locked(g_data);


            if(case_is_locked || g_is_data_analyst_mode)
            {
                is_case_read_only = true;
            }
            else if(!is_checked_out)
            {
                is_case_read_only = true;
            }

            quick_edit_header_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, { search_text: search_text, is_read_only: is_case_read_only });
            
            var search_text_context = get_seach_text_context(p_result, [], p_metadata, p_data, p_dictionary_path, p_metadata_path, p_object_path, search_text, is_case_read_only);

            render_search_text(search_text_context);

            Array.prototype.push.apply(p_post_html_render, search_text_context.post_html_render);
            
            p_result.push("</section>");
        }
        else
        {
            for (var i = 0; i < p_metadata.children.length; i++) 
            {
                var child = p_metadata.children[i];

                if (child.type.toLowerCase() == 'form' && p_ui.url_state.path_array[1] == child.name) 
                {
                    if (p_data[child.name] || p_data[child.name] == 0) 
                    {
                        // do nothing 
                    }
                    else 
                    {
                        p_data[child.name] = create_default_object(child, {})[child.name];
                    }

                    const page_render_array = page_render(child, p_data[child.name], p_ui, p_metadata_path + ".children[" + i + "]", p_object_path + "." + child.name, p_dictionary_path + "/" + child.name, false, p_post_html_render);
                    for(let j = 0; j < page_render_array.length; j++)
                    {
                        p_result.push(page_render_array[j]);
                    }
                }
            }

        }
    }
}

async function unpin_case_clicked(p_id)
{
    if(g_is_jurisdiction_admin)
    {
        $mmria.pin_un_pin_dialog_show(p_id, false);
    }
    else
    {
        await mmria_pin_case_click(p_id, true)
    }
}

// Helper function to show messages (if not already available)
function show_message(message, type) {
    if (!type) type = 'info';
    
    // Create a simple toast notification
    var toast = document.createElement('div');
    var alertClass = 'alert-info';
    if (type === 'error') alertClass = 'alert-danger';
    else if (type === 'success') alertClass = 'alert-success';
    
    toast.className = 'alert ' + alertClass + ' alert-dismissible fade show';
    toast.style.position = 'fixed';
    toast.style.top = '20px';
    toast.style.right = '20px';
    toast.style.zIndex = '9999';
    toast.style.minWidth = '300px';
    toast.innerHTML = message + '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>';
    
    document.body.appendChild(toast);
    
    // Auto-remove after 5 seconds
    setTimeout(function() {
        if (toast.parentNode) {
            toast.parentNode.removeChild(toast);
        }
    }, 5000);
}

function render_sort_by_include_in_export(p_sort)
{
	const sort_list = [
        {
            value : 'by_date_created',
            display : 'By date created'
        },
        {
            value : 'by_date_last_updated',
            display : 'By date last updated'
        },
        {
            value : 'by_last_name',
            display : 'By last name'
        },
        {
            value : 'by_first_name',
            display : 'By first name'
        },
        {
            value : 'by_middle_name',
            display : 'By middle name'
        },
        {
            value : 'by_year_of_death',
            display : 'By year of death'
        },
        {
            value : 'by_month_of_death',
            display : 'By month of death'
        },
        {
            value : 'by_committee_review_date',
            display : 'By committee review date'
        },
        {
            value : 'by_created_by',
            display : 'By created by'
        },
        {
            value : 'by_last_updated_by',
            display : 'By last updated by'
        },
        {
            value : 'by_state_of_death',
            display : 'By state of death'
        },
        {
            value : 'by_agency_case_id',
            display : 'By agency-based case identifier'
        },
        {
            value : 'by_record_id',
            display : 'By Record id'
        },
        {
            value : 'by_pregnancy_relatedness',
            display : 'By pregnancy relatedness'
        }
	];

    const f_result = [];

	sort_list.map((item) => {
        f_result.push(`<option value="${item.value}" ${item.value === p_sort.sort ? 'selected' : ''}>${item.display}</option>`);
    });

	return f_result.join(''); 
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


function render_filter_records_per_page(p_sort)
{
    const sort_list = [25, 50, 100, 250, 500, 1000];
    const f_result = [];

    sort_list.map((item) => {
        f_result.push(`<option value="${item}" ${item == p_sort.take ? 'selected' : ''}>${item}</option>`)
    });

    return f_result.join('');
}

function clear_case_search() 
{
    g_ui.case_view_request.search_key = '';
    g_ui.case_view_request.sort = 'by_date_created';
    g_ui.case_view_request.case_status = 'all'
    g_ui.case_view_request.pregnancy_relatedness = 'all';
    g_ui.case_view_request.field_selection = 'all';
    g_ui.case_view_request.descending = true;
    g_ui.case_view_request.take = 100;
    g_ui.case_view_request.page = 1;
    g_ui.case_view_request.skip = 0;
    g_ui.case_view_list = [];

    get_case_set();
}

function search_case_status_onchange(p_value)
{
    if(g_ui.case_view_request.case_status != p_value)
    {
        g_ui.case_view_request.case_status = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
}

function search_pregnancy_relatedness_onchange(p_value)
{
    if(g_ui.case_view_request.pregnancy_relatedness != p_value)
    {
        g_ui.case_view_request.pregnancy_relatedness = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
    
}

function search_field_selection_onchange(p_value)
{
    if(g_ui.case_view_request.field_selection != p_value)
    {
        g_ui.case_view_request.field_selection = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
    
}

function records_per_page_change(p_value)
{
    if(p_value != g_ui.case_view_request.take)
    {
        g_ui.case_view_request.take = p_value;
        g_ui.case_view_request.page = 1;
        g_ui.case_view_request.skip = 0;
    }
}


function app_is_item_pinned(p_id)
{
    var is_pin = 0;
    
    if
    (
        g_pinned_case_set!= null && 
        Object.hasOwn(g_pinned_case_set, 'list')
    )
    {
        if(Object.hasOwn(g_pinned_case_set.list, 'everyone'))
        {
            if(g_pinned_case_set.list.everyone.indexOf(p_id) != -1)
            {
                is_pin = 2;
            }
        }

        if(is_pin == 0)
        {
            if(Object.hasOwn(g_pinned_case_set.list, g_user_name))
            {
                if(g_pinned_case_set.list[g_user_name].indexOf(p_id) != -1)
                {
                    is_pin = 1;
                }
            }
        }
    }

    return is_pin;
}

function render_pin_un_pin_button
(
    p_case_view_item,
    p_is_checked_out,
    p_is_checked_out_expired,
    p_delete_enabled_html
)
{


    const is_pinned = app_is_item_pinned(p_case_view_item.id);

    if(is_pinned == 0)
    {
        return `
    
    <input type="image" src="../img/icon_pin.png" title="Pin this case." alt="Pin this case." style="width:16px;height:32px;vertical-align:middle;" onclick="pin_case_clicked('${p_case_view_item.id}')"/>
    
    `;
    }
    else if(is_pinned == 1)
    {
        return `
    
    <input type="image" src="../img/icon_unpin.png"  title="Unpin this case." alt="Unpin this case." style="width:16px;height:32px;vertical-align:middle;" onclick="unpin_case_clicked('${p_case_view_item.id}')"/>
    
    `;
    }
    else
    {
        let click_event = ` onclick="unpin_case_clicked('${p_case_view_item.id}')" `;
        let cursor_pointer = "";
        if(is_pinned == 2 && g_is_jurisdiction_admin == false)
        {
            cursor_pointer = "disabled=disabled";
            click_event = "";
        }


        return `
    
    <input type="image" src="../img/icon_unpinMultiple.png" title="Unpin this case." alt="Unpin this case." style="width:16px;height:32px;vertical-align:middle;" ${cursor_pointer} ${click_event}/>
    
    `;
    }

}



function render_app_summary_result_item(item, i)
{

    if(app_is_item_pinned(item.id) != 0)
    {
        return "";
    }

    // Ensure offline state properties have default values
    if (item.value.is_offline === undefined || item.value.is_offline === null) {
        item.value.is_offline = false;
    }

    let is_checked_out = is_case_checked_out(item.value);
    let case_is_locked = is_case_view_locked(item.value);
    // let checked_out_html = ' [not checked out] ';
    let checked_out_html = '';
    let delete_enabled_html = ''; 

    if(case_is_locked || g_is_data_analyst_mode)
    {
        // checked_out_html = ' [ read only ] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else if(is_checked_out)
    {
        // checked_out_html = ' [checked out by you] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else  if(!is_checked_out_expired(item.value))
    {
        // checked_out_html = ` [checked out by ${item.value.last_checked_out_by}] `;
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }

    
    const caseStatuses = {
        "9999":"(blank)",	
        "1":"Abstracting (Incomplete)",
        "2":"Abstraction Complete",
        "3":"Ready for Review",
        "4":"Review Complete and Decision Entered",
        "5":"Out of Scope and Death Certificate Entered",
        "6":"False Positive and Death Certificate Entered",
        "0":"Vitals Import"
    }; 
    const caseID = item.id;
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.jurisdiction_id;
    const firstName = item.value.first_name;
    const lastName = item.value.last_name;
    const recordID = item.value.record_id ? `- (${item.value.record_id})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const lockedBy = item.value.last_checked_out_by;
    const currentCaseStatus = item.value.case_status == null ? '(blank)' : caseStatuses[item.value.case_status.toString()];
    const dateCreated = item.value.date_created ? new Date(item.value.date_created).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    const lastUpdatedDate = item.value.date_last_updated ? new Date(item.value.date_last_updated).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    
    let projectedReviewDate = item.value.review_date_projected ? new Date(item.value.review_date_projected).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    let actualReviewDate = item.value.review_date_actual ? new Date(item.value.review_date_actual).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;


    return (
    `<tr class="tr" path="${caseID}">
        <td class="td"><a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
            ${checked_out_html}</td>
        <td class="td">${currentCaseStatus}</td>
        <td class="td">${reviewDates}</td>
        <td class="td">${createdBy} - ${dateCreated}</td>
        <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
        <td class="td">
            ${is_checked_out ? (`
            <span class="icn-info">${lockedBy}</span>
            `) : ''}
            ${!is_checked_out && !is_checked_out_expired(item.value) ? (`
            <span class="row no-gutters align-items-center">
                <span class="icn icn--round icn--border bg-primary" title="Case is locked"><span class="d-flex x14 fill-w cdc-icon-lock-alt"></span></span>
                <span class="icn-info">${lockedBy}</span>
            </span>
            `) : ''}
        </td>
        ${!g_is_data_analyst_mode ? (
            `<td class="td">       
                <button type="button" id="id_for_record_${i}" class="btn btn-primary" onclick="init_delete_dialog(${i})" style="line-height: 1.15" ${delete_enabled_html}>Delete</button>

                ${render_pin_un_pin_button
                    (
                        item, 
                        is_checked_out, 
                        is_checked_out_expired(item.value),
                        delete_enabled_html
                    )
                }

                <button type="button" id="offline_toggle_${i}" class="btn ${(item.value.is_offline === true) ? 'btn-primary' : 'btn-outline-secondary'}" 
                    onclick="toggle_offline_status('${caseID}', ${i})" 
                    style="line-height: 1.15" 
                    ${delete_enabled_html}
                    title="${(item.value.is_offline === true) ? 'Remove from offline use' : 'Mark for offline use'}">
                    ${(item.value.is_offline === true) ? 'Remove from Offline List' : '<span class="x14 fill-p cdc-icon-download-cloud"></span> Add to Offline List'}
                </button>
                </td>`
            ) : ''}
        </tr>`
    );


}


function render_app_pinned_summary_result(item, i)
{
    if(app_is_item_pinned(item.id) == 0)
    {
        return "";
    }

    // Ensure offline state properties have default values
    if (item.value.is_offline === undefined || item.value.is_offline === null) {
        item.value.is_offline = false;
    }

    let is_checked_out = is_case_checked_out(item.value);
    let case_is_locked = is_case_view_locked(item.value);
    // let checked_out_html = ' [not checked out] ';
    let checked_out_html = '';
    let delete_enabled_html = ''; 

    if(case_is_locked || g_is_data_analyst_mode)
    {
        // checked_out_html = ' [ read only ] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else if(is_checked_out)
    {
        // checked_out_html = ' [checked out by you] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else  if(!is_checked_out_expired(item.value))
    {
        // checked_out_html = ` [checked out by ${item.value.last_checked_out_by}] `;
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }

    
    const caseStatuses = {
        "9999":"(blank)",	
        "1":"Abstracting (Incomplete)",
        "2":"Abstraction Complete",
        "3":"Ready for Review",
        "4":"Review Complete and Decision Entered",
        "5":"Out of Scope and Death Certificate Entered",
        "6":"False Positive and Death Certificate Entered",
        "0":"Vitals Import"
    }; 
    const caseID = item.id;
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.jurisdiction_id;
    const firstName = item.value.first_name;
    const lastName = item.value.last_name;
    const recordID = item.value.record_id ? `- (${item.value.record_id})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const lockedBy = item.value.last_checked_out_by;
    const currentCaseStatus = item.value.case_status == null ? '(blank)' : caseStatuses[item.value.case_status.toString()];
    const dateCreated = item.value.date_created ? new Date(item.value.date_created).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    const lastUpdatedDate = item.value.date_last_updated ? new Date(item.value.date_last_updated).toLocaleDateString('en-US') : ''; //convert ISO format to MM/DD/YYYY
    
    let projectedReviewDate = item.value.review_date_projected ? new Date(item.value.review_date_projected).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    let actualReviewDate = item.value.review_date_actual ? new Date(item.value.review_date_actual).toLocaleDateString('en-US') : ''; //convert ISO format to mm/dd/yyyy if exists
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;

    g_pinned_case_count += 1;


    let border_bottom_color = ""
    if(g_pinned_case_count == mmria_count_number_pinned())
    {
        border_bottom_color = 'style="border-bottom-color: #712177;border-bottom-width:2px"';
    }

    return (
    `<tr class="tr" path="${caseID}" style="background-color: #f7f2f7;">
        <td class="td" ${border_bottom_color}><a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
            ${checked_out_html}</td>
        <td class="td" ${border_bottom_color}>${currentCaseStatus}</td>
        <td class="td" ${border_bottom_color}>${reviewDates}</td>
        <td class="td" ${border_bottom_color}>${createdBy} - ${dateCreated}</td>
        <td class="td" ${border_bottom_color}>${lastUpdatedBy} - ${lastUpdatedDate}</td>
        <td class="td" ${border_bottom_color}>
            ${is_checked_out ? (`
            <span class="icn-info">${lockedBy}</span>
            `) : ''}
            ${!is_checked_out && !is_checked_out_expired(item.value) ? (`
            <span class="row no-gutters align-items-center">
                <span class="icn icn--round icn--border bg-primary" title="Case is locked"><span class="d-flex x14 fill-w cdc-icon-lock-alt"></span></span>
                <span class="icn-info">${lockedBy}</span>
            </span>
            `) : ''}
        </td>
        ${!g_is_data_analyst_mode ? (
            `<td class="td" ${border_bottom_color}>
                <button type="button" id="id_for_record_${i}" class="btn btn-primary" onclick="init_delete_dialog(${i})" style="line-height: 1.15" ${delete_enabled_html}>Delete</button>
                
                ${render_pin_un_pin_button
                    (
                        item, 
                        is_checked_out, 
                        is_checked_out_expired(item.value),
                        delete_enabled_html
                    )
                }

                <button type="button" id="offline_toggle_${i}" class="btn ${(item.value.is_offline === true) ? 'btn-primary' : 'btn-outline-secondary'}" 
                    onclick="toggle_offline_status('${caseID}', ${i})" 
                    style="line-height: 1.15" 
                    ${delete_enabled_html}
                    title="${(item.value.is_offline === true) ? 'Remove from offline use' : 'Mark for offline use'}">
                    ${(item.value.is_offline === true) ? 'Remove from Offline List' : '<span class="x14 fill-p cdc-icon-download-cloud"></span> Add to Offline List'}
                </button>
                </td>`
            ) : ''}
        </tr>`
    );
}

async function pin_case_clicked(p_id)
{
    if(g_is_jurisdiction_admin)
    {
        $mmria.pin_un_pin_dialog_show(p_id, true);
    }
    else
    {
        await mmria_pin_case_click(p_id, false)
    }
}

async function unpin_case_clicked(p_id)
{
    if(g_is_jurisdiction_admin && app_is_item_pinned(p_id) != 1)
    {
        $mmria.pin_un_pin_dialog_show(p_id, false);
    }
    else
    {
        await mmria_un_pin_case_click(p_id, false)
    }
}