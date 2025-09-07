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
            // Update the case data in the UI
            if (g_ui.case_view_list[caseIndex]) {
                g_ui.case_view_list[caseIndex].value.is_offline = result.is_offline;
                g_ui.case_view_list[caseIndex].value.offline_date = new Date().toISOString();
                g_ui.case_view_list[caseIndex].value.offline_by = g_user_name; // Assuming g_user_name is available
            }

            // Hide the button after adding to offline list (since Remove functionality is only in offline table)
            if (result.is_offline) {
                button.style.display = 'none';
            }

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
            // Refresh offline documents list only - this will update the content without causing flicker
            refresh_offline_documents_list();

            // Update any "Add to Offline List" buttons in the main case list to be visible again
            // Instead of refreshing the entire case list, just update the relevant button states
            const mainCaseButtons = document.querySelectorAll(`button[id*="offline_toggle_"][onclick*="${caseId}"]`);
            mainCaseButtons.forEach(button => {
                button.style.display = 'block'; // Show the "Add to Offline List" button again
                button.disabled = false;
                // Reset button text in case it was in loading state
                button.innerHTML = 'Add to Offline List';
            });
        } else {
            throw new Error(result.message || 'Failed to remove case from offline list');
        }
    } catch (error) {
        console.error('Error removing case from offline list:', error);
        show_message('Error removing case from offline list: ' + error.message, 'error');
    } finally {
        // Restore button states for remove buttons in offline table
        const buttons = document.querySelectorAll(`button[onclick*="${caseId}"]`);
        buttons.forEach(button => {
            button.disabled = false;
            button.innerHTML = 'Remove From List';
        });
    }
}

// Global variable to store current offline documents
let g_current_offline_documents = [];

// Global array to map offline case indices to case IDs (for routing)
let g_offline_case_index_map = [];

// Function to refresh the offline documents list
async function refresh_offline_documents_list() {
    try {
        const offlineDocuments = await get_offline_documents();
        g_current_offline_documents = offlineDocuments; // Store globally
        
        // Build index map for offline case routing
        g_offline_case_index_map = offlineDocuments.map(doc => doc.id);
        
        // Make the index map globally accessible for navigation
        window.g_offline_case_index_map = g_offline_case_index_map;
        
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
        console.log('Fetching offline documents...');
        const response = await fetch('/api/case_view/offline-documents', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
        });

        console.log('Offline documents response:', response.status, response.statusText);
        
        if (response.ok) {
            const result = await response.json();
            console.log('Offline documents result:', result);
            return result.rows || [];
        } else {
            console.error('Failed to fetch offline documents:', response.status, response.statusText);
            return [];
        }
    } catch (error) {
        console.error('Error fetching offline documents:', error);
        return [];
    }
}

// Function to render offline documents table
function render_offline_documents_table(offlineDocuments) {
    let rows;
    const hasOfflineCases = offlineDocuments && offlineDocuments.length > 0;
    
    // Get offline status for debugging
    const isOfflineStatus = localStorage.getItem('is_offline') || 'false';
    
    if (!hasOfflineCases) {
        rows = `
            <tr class="tr">
                <td class="td" colspan="6" style="text-align: center; padding: 20px; color: #6c757d; font-style: italic;">
                    No cases currently selected for offline work.
                </td>
            </tr>
        `;
    } else {
        rows = offlineDocuments.map((item, i) => render_offline_document_item(item, i)).join('');
    }

    return `
        <div style="margin-bottom: 10px; padding: 8px 12px; background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 4px; font-size: 12px; color: #495057;">
            <strong>DEBUG:</strong> is_offline = ${isOfflineStatus}
        </div>
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
            <tfoot class='tfoot'>
                <tr class='tr'>
                    <td class='td' colspan='5' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6;'>
                        <ul style='margin: 0; padding-left: 20px; font-size: 13px; color: #6c757d; line-height: 1.4; font-style: italic;'>
                            <li style='margin-bottom: 4px;'>Up to 3 existing cases can be brought offline at once.</li>
                            <li style='margin-bottom: 4px;'>Up to 3 new cases can be created offline.</li>
                            <li style='margin-bottom: 4px;'>Once offline, you assume the risk of losing your data. Please bring all cases back online regularly to ensure your data is saved to the system.</li>
                            <li style='margin-bottom: 0;'>Navigating to another page will reset the list of cases selected for offline work.</li>
                        </ul>
                    </td>
                    <td class='td' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: right; vertical-align: middle;'>
                        ${isOfflineStatus === 'true' ? `
                            <button type="button" class="btn btn-success" onclick="go_online_clicked()" style="line-height: 1.15;">
                                <span class="x14 fill-w cdc-icon-upload-cloud" style="margin-right: 8px;"></span>Go Online
                            </button>
                        ` : `
                            <button type="button" class="btn btn-primary" onclick="go_offline_clicked()" style="line-height: 1.15; ${!hasOfflineCases ? 'opacity: 0.6; cursor: not-allowed;' : ''}" ${!hasOfflineCases ? 'disabled' : ''}>
                                <span class="x14 fill-w cdc-icon-ban" style="margin-right: 8px;"></span>Go Offline
                            </button>
                        `}
                    </td>
                </tr>
            </tfoot>
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
                <button type="button" class="btn btn-primary" onclick="remove_from_offline_list('${caseID}')" style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;">
                    Remove From List
                </button>
            </td>
        </tr>
    `;
}

// Function to hide case listing elements when going offline
function hideOnlineCaseListingElements() {
    console.log('Hiding case listing elements for offline mode');
    
    // Hide the case listing table specifically (by looking for "Case Listing" header)
    const allTables = document.querySelectorAll('table.table.mb-0');
    allTables.forEach(table => {
        const headers = table.querySelectorAll('th');
        let isCaseListingTable = false;
        headers.forEach(header => {
            if (header.textContent.includes('Case Listing')) {
                isCaseListingTable = true;
            }
        });
        
        if (isCaseListingTable) {
            table.style.display = 'none';
            console.log('Case listing table hidden');
        }
    });
    
    // Hide pagination elements
    const paginationElements = document.querySelectorAll('.table-pagination');
    paginationElements.forEach(element => {
        element.style.display = 'none';
        console.log('Pagination element hidden');
    });
    
    // Hide the search/filter form elements
    console.log('Looking for search/filter elements to hide...');
    
    // Hide individual search/filter elements by their IDs
    const searchElements = [
        'search_text_box',
        'search_field_selection', 
        'search_case_status',
        'search_pregnancy_relatedness',
        'search_sort_by',
        'search_records_per_page',
        'sort_descending'
    ];
    
    searchElements.forEach(elementId => {
        const element = document.getElementById(elementId);
        if (element) {
            // Hide the parent container (form-inline div)
            const parentDiv = element.closest('.form-inline');
            if (parentDiv) {
                parentDiv.style.display = 'none';
                console.log(`Search element container hidden: ${elementId}`);
            } else {
                element.style.display = 'none';
                console.log(`Search element hidden: ${elementId}`);
            }
        }
    });
    
    // Hide the Apply Filters and Reset buttons
    const applyFilterButton = document.querySelector('button[onclick*="apply_filter_click"]');
    if (applyFilterButton) {
        const buttonContainer = applyFilterButton.closest('.form-inline');
        if (buttonContainer) {
            buttonContainer.style.display = 'none';
            console.log('Apply Filters button container hidden');
        }
    }
    
    // Hide any remaining form elements that might be missed
    const searchForm = document.querySelector('form[onsubmit*="get_case_set"]');
    if (searchForm) {
        searchForm.style.display = 'none';
        console.log('Search form hidden');
    }
    
    // Alternative approach - hide by class or parent elements if the direct selectors don't work
    const searchContainer = document.querySelector('.search-container, .case-search-form, [id*="search"], [class*="search"]');
    if (searchContainer) {
        searchContainer.style.display = 'none';
        console.log('Search container hidden');
    }
}

// Function to show case listing elements when going online
function showOnlineCaseListingElements() {
    console.log('Showing case listing elements for online mode');
    
    // Show the case listing table specifically (by looking for "Case Listing" header)
    const allTables = document.querySelectorAll('table.table.mb-0');
    allTables.forEach(table => {
        const headers = table.querySelectorAll('th');
        let isCaseListingTable = false;
        headers.forEach(header => {
            if (header.textContent.includes('Case Listing')) {
                isCaseListingTable = true;
            }
        });
        
        if (isCaseListingTable) {
            table.style.display = '';
            console.log('Case listing table shown');
        }
    });
    
    // Show pagination elements
    const paginationElements = document.querySelectorAll('.table-pagination');
    paginationElements.forEach(element => {
        element.style.display = '';
        console.log('Pagination element shown');
    });
    
    // Show the search/filter form elements
    console.log('Looking for search/filter elements to show...');
    
    // Show individual search/filter elements by their IDs
    const searchElements = [
        'search_text_box',
        'search_field_selection', 
        'search_case_status',
        'search_pregnancy_relatedness',
        'search_sort_by',
        'search_records_per_page',
        'sort_descending'
    ];
    
    searchElements.forEach(elementId => {
        const element = document.getElementById(elementId);
        if (element) {
            // Show the parent container (form-inline div)
            const parentDiv = element.closest('.form-inline');
            if (parentDiv) {
                parentDiv.style.display = '';
                console.log(`Search element container shown: ${elementId}`);
            } else {
                element.style.display = '';
                console.log(`Search element shown: ${elementId}`);
            }
        }
    });
    
    // Show the Apply Filters and Reset buttons
    const applyFilterButton = document.querySelector('button[onclick*="apply_filter_click"]');
    if (applyFilterButton) {
        const buttonContainer = applyFilterButton.closest('.form-inline');
        if (buttonContainer) {
            buttonContainer.style.display = '';
            console.log('Apply Filters button container shown');
        }
    }
    
    // Show any remaining form elements that might be missed
    const searchForm = document.querySelector('form[onsubmit*="get_case_set"]');
    if (searchForm) {
        searchForm.style.display = '';
        console.log('Search form shown');
    }
    
    // Show search container
    const searchContainer = document.querySelector('.search-container, .case-search-form, [id*="search"], [class*="search"]');
    if (searchContainer) {
        searchContainer.style.display = '';
        console.log('Search container shown');
    }
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
    
    // Load offline documents after page render
    p_post_html_render.push("(async function() {");
    p_post_html_render.push("    try {");
    p_post_html_render.push("        console.log('Starting offline documents load...');");
    p_post_html_render.push("        const offlineDocuments = await get_offline_documents();");
    p_post_html_render.push("        console.log('Offline documents loaded:', offlineDocuments);");
    p_post_html_render.push("        g_current_offline_documents = offlineDocuments;"); // Store globally
    p_post_html_render.push("        // Build index map for offline case routing");
    p_post_html_render.push("        g_offline_case_index_map = offlineDocuments.map(doc => doc.id);");
    p_post_html_render.push("        // Make the index map globally accessible for navigation");
    p_post_html_render.push("        window.g_offline_case_index_map = g_offline_case_index_map;");
    p_post_html_render.push("        console.log('Offline case index map:', window.g_offline_case_index_map);");
    p_post_html_render.push("        const offlineSection = document.getElementById('offline-documents-section');");
    p_post_html_render.push("        if (offlineSection) {");
    p_post_html_render.push("            offlineSection.innerHTML = render_offline_documents_table(offlineDocuments);");
    p_post_html_render.push("            console.log('Offline documents table rendered');");
    p_post_html_render.push("        } else {");
    p_post_html_render.push("            console.log('Offline section element not found');");
    p_post_html_render.push("        }");
    p_post_html_render.push("    } catch (error) {");
    p_post_html_render.push("        console.error('Error in offline documents load:', error);");
    p_post_html_render.push("    }");
    p_post_html_render.push("})();");

    // Check if we're in offline mode - if so, skip case listing and filters
    const isOfflineStatus = localStorage.getItem('is_offline') || 'false';
    
    if (isOfflineStatus !== 'true') {
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
    }

    // Add offline documents section
    p_result.push("<div id='offline-documents-section' class='mb-4'>");
    p_result.push("</div>");

    // Only show case listing table and pagination if not in offline mode
    const isOfflineMode = localStorage.getItem('is_offline') || 'false';
    
    if (isOfflineMode !== 'true') {
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
    }    p_result.push("</section>");

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
    // Check if we're in offline mode - if so, skip API calls
    const isOffline = localStorage.getItem('is_offline') === 'true';
    
    if (isOffline) {
        console.log('In offline mode - skipping clear_case_search API call');
        return;
    }

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
        return `<input type="image" src="../img/icon_pin.png" title="Pin this case." alt="Pin this case." style="width:16px;height:32px;vertical-align:middle;" onclick="pin_case_clicked('${p_case_view_item.id}')"/>`;
    }
    else if(is_pinned == 1)
    {
        return `<input type="image" src="../img/icon_unpin.png"  title="Unpin this case." alt="Unpin this case." style="width:16px;height:32px;vertical-align:middle;" onclick="unpin_case_clicked('${p_case_view_item.id}')"/>`;
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

        return `<input type="image" src="../img/icon_unpinMultiple.png" title="Unpin this case." alt="Unpin this case." style="width:16px;height:32px;vertical-align:middle;" ${cursor_pointer} ${click_event}/>`;
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
                <div>
                    <button type="button" id="id_for_record_${i}" class="btn btn-primary" onclick="init_delete_dialog(${i})" style="line-height: 1.15; margin-right: 8px;" ${delete_enabled_html}>Delete</button>${render_pin_un_pin_button(item, is_checked_out, is_checked_out_expired(item.value), delete_enabled_html)}
                </div>

                ${(item.value.is_offline !== true) ? `
                <div style="margin-top: 8px;">
                    <button type="button" id="offline_toggle_${i}" class="btn btn-outline-secondary" 
                        onclick="toggle_offline_status('${caseID}', ${i})" 
                        style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" 
                        ${delete_enabled_html}
                        title="Mark for offline use">
                        <span class="x14 fill-p cdc-icon-download-cloud"></span> Add to Offline List
                    </button>
                </div>` : ''}
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
                <div>
                    <button type="button" id="id_for_record_${i}" class="btn btn-primary" onclick="init_delete_dialog(${i})" style="line-height: 1.15; margin-right: 8px;" ${delete_enabled_html}>Delete</button>${render_pin_un_pin_button(item, is_checked_out, is_checked_out_expired(item.value), delete_enabled_html)}
                </div>

                ${(item.value.is_offline !== true) ? `
                <div style="margin-top: 8px;">
                    <button type="button" id="offline_toggle_${i}" class="btn btn-outline-secondary" 
                        onclick="toggle_offline_status('${caseID}', ${i})" 
                        style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" 
                        ${delete_enabled_html}
                        title="Mark for offline use">
                        <span class="x14 fill-p cdc-icon-download-cloud"></span> Add to Offline List
                    </button>
                </div>` : ''}
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

// Function for Go Offline button
function go_offline_clicked() {
    // Check if button is disabled (no cases selected)
    const button = event.target.closest('button');
    if (button && button.disabled) {
        console.log('Go Offline button clicked but disabled - no cases selected');
        return;
    }
    
    console.log('Go Offline button clicked - showing modal');
    show_go_offline_modal();
}

// Function for Go Online button
async function go_online_clicked() {
    console.log('Go Online button clicked - transitioning back to online mode');
    
    try {
        // Unregister service worker first
        console.log('Unregistering service worker...');
        await unregister_service_worker();
        
        // Clear service worker caches
        if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
            navigator.serviceWorker.controller.postMessage({ type: 'CLEAR_CACHES' });
        }
        
        // Clear all cached data
        console.log('Clearing cached data...');
        await clear_all_cached_data();
        
        // Clear offline session data
        localStorage.removeItem('mmria_offline_session');
        localStorage.removeItem('is_offline');
        localStorage.removeItem('mmria_cached_cases');
        
        // Remove offline mode indicator from body
        document.body.classList.remove('mmria-offline-mode');
        
        // Refresh the page to fully return to online mode
        console.log('Returning to online mode - refreshing page');
        window.location.reload();
        
    } catch (error) {
        console.error('Error transitioning to online mode:', error);
        alert('Error transitioning to online mode. Some cached data may remain.');
    }
}

// Function to show the Go Offline modal
function show_go_offline_modal() {
    // Create modal HTML
    const modalHtml = `
        <div id="go-offline-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 20px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: bold;">Go Offline</h4>
                        <button type="button" class="close" onclick="close_go_offline_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 30px;">
                        <p style="font-size: 16px; margin-bottom: 25px; color: #333;">Please review the following before going offline:</p>
                        
                        <ul style="list-style: disc; padding-left: 20px; margin-bottom: 30px;">
                            <li style="margin-bottom: 15px; font-size: 14px; line-height: 1.5;">
                                To prevent data loss, it is highly recommended to <strong>avoid Incognito mode</strong> when using MMRIA Offline.
                            </li>
                            <li style="margin-bottom: 15px; font-size: 14px; line-height: 1.5;">
                                Once offline, you assume the <strong>risk of losing your data</strong>. All cases created or edited in offline mode will need to be saved and brought back online regularly to be permanently saved in MMRIA.
                            </li>
                            <li style="margin-bottom: 0; font-size: 14px; line-height: 1.5;">
                                Remember the offline login key for use while in offline mode.
                            </li>
                        </ul>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-secondary" onclick="close_go_offline_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Cancel
                        </button>
                        <button type="button" class="btn btn-primary" onclick="continue_to_set_key()" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">
                            Continue to set key
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="go-offline-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('go-offline-modal');
        const backdrop = document.getElementById('go-offline-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

// Function to close the Go Offline modal
function close_go_offline_modal() {
    const modal = document.getElementById('go-offline-modal');
    const backdrop = document.getElementById('go-offline-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) {
                modal.parentNode.removeChild(modal);
            }
            if (backdrop.parentNode) {
                backdrop.parentNode.removeChild(backdrop);
            }
        }, 150);
    }
}

// Stub function for Continue to set key button
function continue_to_set_key() {
    console.log('Continue to set key button clicked - opening set key modal');
    // Close the current modal first
    close_go_offline_modal();
    // Then show the set key modal
    setTimeout(() => {
        show_set_offline_key_modal();
    }, 200);
}

// Function to show the Set Offline Key modal
function show_set_offline_key_modal() {
    // Create modal HTML
    const modalHtml = `
        <div id="set-offline-key-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 20px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: bold;">Set Offline Key</h4>
                        <button type="button" class="close" onclick="close_set_offline_key_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 30px;">
                        <p style="font-size: 16px; margin-bottom: 20px; color: #333;">Set a key to log in while in offline mode:</p>
                        
                        <input type="text" id="offline-key-input" class="form-control" style="margin-bottom: 10px; padding: 12px; font-size: 14px; border: 1px solid #ccc; border-radius: 4px;" placeholder="Enter your offline key" oninput="handle_key_input()" autocomplete="off" tabindex="1">
                        
                        <div id="key-validation-error" style="display: none; color: #dc3545; font-size: 14px; margin-bottom: 20px; line-height: 1.4;">
                            The provided key does not fulfill one or more of the requirements below. Please update the key and try again.
                        </div>
                        
                        <p style="font-size: 14px; margin-bottom: 20px; color: #666; font-weight: bold;">NOTE: This key will be visible and accessible to the jurisdiction administrator.</p>
                        
                        <p style="font-size: 14px; margin-bottom: 15px; color: #333;">Please follow the following guidance when setting your offline key. The key must contain 10 characters including:</p>
                        
                        <ul style="list-style: disc; padding-left: 20px; margin-bottom: 0;">
                            <li style="margin-bottom: 8px; font-size: 14px; line-height: 1.4;">
                                one uppercase character (A-Z)
                            </li>
                            <li style="margin-bottom: 8px; font-size: 14px; line-height: 1.4;">
                                one lowercase character (a-z)
                            </li>
                            <li style="margin-bottom: 8px; font-size: 14px; line-height: 1.4;">
                                one number (0-9)
                            </li>
                            <li style="margin-bottom: 0; font-size: 14px; line-height: 1.4;">
                                one special character (!@#$%^&*_?><~)
                            </li>
                        </ul>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-secondary" onclick="close_set_offline_key_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Cancel
                        </button>
                        <button type="button" id="go-offline-btn" class="btn btn-primary" onclick="go_offline_final()" style="background-color: #7b2d8e; border-color: #7b2d8e; color: white; padding: 8px 20px; opacity: 0.6;" disabled>
                            <span class="cdc-icon-ban" style="margin-right: 5px;"></span>Go Offline
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="set-offline-key-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('set-offline-key-modal');
        const backdrop = document.getElementById('set-offline-key-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
        // Focus on the input field
        const input = document.getElementById('offline-key-input');
        if (input) {
            input.disabled = false; // Ensure it's enabled
            input.focus();
            input.select(); // Select any existing text
        }
    }, 10);
}

// Function to close the Set Offline Key modal
function close_set_offline_key_modal() {
    const modal = document.getElementById('set-offline-key-modal');
    const backdrop = document.getElementById('set-offline-key-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) {
                modal.parentNode.removeChild(modal);
            }
            if (backdrop.parentNode) {
                backdrop.parentNode.removeChild(backdrop);
            }
        }, 150);
    }
}

// Variable to store the validation timer
let validation_timer = null;

// Function to handle key input with delayed validation
function handle_key_input() {
    // Clear existing timer
    if (validation_timer) {
        clearTimeout(validation_timer);
    }
    
    // Set new timer for 1 second delay
    validation_timer = setTimeout(() => {
        validate_key_realtime();
    }, 1000);
}

// Function to validate key in real-time
function validate_key_realtime() {
    const keyInput = document.getElementById('offline-key-input');
    const key = keyInput ? keyInput.value : '';
    const errorDiv = document.getElementById('key-validation-error');
    const goOfflineBtn = document.getElementById('go-offline-btn');
    
    const isValid = validate_offline_key(key);
    
    if (key.length === 0) {
        // Empty key - hide error, disable button, default border
        if (errorDiv) {
            errorDiv.style.display = 'none';
        }
        if (keyInput) {
            keyInput.disabled = false; // Ensure input stays enabled
            keyInput.style.borderColor = '#ccc';
        }
        if (goOfflineBtn) {
            goOfflineBtn.disabled = true;
            goOfflineBtn.style.opacity = '0.6';
            goOfflineBtn.style.color = 'white';
            goOfflineBtn.style.backgroundColor = '#7b2d8e';
            goOfflineBtn.style.borderColor = '#7b2d8e';
        }
    } else if (!isValid) {
        // Invalid key - show error, disable button, red border
        if (errorDiv) {
            errorDiv.style.display = 'block';
        }
        if (keyInput) {
            keyInput.disabled = false; // Ensure input stays enabled
            keyInput.style.borderColor = '#dc3545';
        }
        if (goOfflineBtn) {
            goOfflineBtn.disabled = true;
            goOfflineBtn.style.opacity = '0.6';
            goOfflineBtn.style.color = 'white';
            goOfflineBtn.style.backgroundColor = '#7b2d8e';
            goOfflineBtn.style.borderColor = '#7b2d8e';
        }
    } else {
        // Valid key - hide error, enable button, default border
        if (errorDiv) {
            errorDiv.style.display = 'none';
        }
        if (keyInput) {
            keyInput.disabled = false; // Ensure input stays enabled
            keyInput.style.borderColor = '#ccc';
        }
        if (goOfflineBtn) {
            goOfflineBtn.disabled = false;
            goOfflineBtn.style.opacity = '1';
            goOfflineBtn.style.color = 'white';
            goOfflineBtn.style.backgroundColor = '#7b2d8e';
            goOfflineBtn.style.borderColor = '#7b2d8e';
        }
    }
}

// Function for final Go Offline button
async function go_offline_final() {
    const keyInput = document.getElementById('offline-key-input');
    const key = keyInput ? keyInput.value : '';
    
    // Double-check validation before proceeding
    if (!validate_offline_key(key)) {
        console.log('Key validation failed on final check');
        return;
    }
    
    // Collect offline case IDs from the current offline documents
    const offlineIds = g_current_offline_documents.map(doc => doc.id);
    
    if (offlineIds.length === 0) {
        console.log('No offline cases found to save');
        alert('No cases selected for offline work.');
        return;
    }
    
    console.log('Starting offline mode transition...');
    console.log('Offline key:', key);
    console.log('Offline case IDs:', offlineIds);
    
    try {
        // First, register and enable the service worker
        if (!('serviceWorker' in navigator)) {
            throw new Error('Service Worker not supported in this browser');
        }
        
        console.log('Registering service worker...');
        const registration = await navigator.serviceWorker.register('/service-worker.js');
        console.log('Service worker registered successfully:', registration);
        
        // Wait for service worker to be ready
        await navigator.serviceWorker.ready;
        console.log('Service worker is ready');
        
        // Prepare the request data
        const requestData = {
            OfflineIds: offlineIds,
            OfflineKey: key
        };
        
        // Send POST request to OfflineCaseController
        const response = await fetch('/api/OfflineCase', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(requestData)
        });
        
        if (response.ok) {
            const result = await response.json();
            console.log('Offline data saved successfully:', result);
            
            if (result.ok) {
                // Success - start offline mode transition
                console.log('Starting offline resource caching...');
                
                // Store offline session data in localStorage
                const offlineSessionData = {
                    offlineSessionId: result.id,
                    offlineKey: key,
                    offlineIds: offlineIds,
                    dateCreated: new Date().toISOString(),
                    isOffline: true
                };
                
                localStorage.setItem('mmria_offline_session', JSON.stringify(offlineSessionData));
                
                // Set simple offline flag for debugging
                localStorage.setItem('is_offline', 'true');
                
                // Pre-fetch and cache the selected offline cases using service worker
                await prefetch_offline_cases(offlineIds);
                
                // Pre-cache essential pages for navigation
                await precache_essential_pages();
                
                // Cache metadata using service worker
                await cache_metadata_with_service_worker();
                
                // Set up service worker message listener for offline status checks
                setupServiceWorkerMessageListener();
                
                // Close modal and show success message
                close_set_offline_key_modal();
                
                // Refresh the offline documents table to update debug display
                await refresh_offline_documents_list();
                
                // Hide case listing and filters when going offline
                hideOnlineCaseListingElements();
                
                // Set offline mode indicator
                document.body.classList.add('mmria-offline-mode');
                
            } else {
                console.error('Server returned error:', result.error_description);
                alert('Error saving offline data: ' + (result.error_description || 'Unknown error'));
            }
        } else {
            console.error('HTTP error:', response.status, response.statusText);
            alert('Error saving offline data. Please try again.');
        }
        
    } catch (error) {
        console.error('Error setting up offline mode:', error);
        alert('Error setting up offline mode: ' + error.message);
    }
}

// Function to validate offline key
function validate_offline_key(key) {
    // Check if key is at least 10 characters
    if (key.length < 10) {
        return false;
    }
    
    // Check for at least one uppercase character (A-Z)
    if (!/[A-Z]/.test(key)) {
        return false;
    }
    
    // Check for at least one lowercase character (a-z)
    if (!/[a-z]/.test(key)) {
        return false;
    }
    
    // Check for at least one number (0-9)
    if (!/[0-9]/.test(key)) {
        return false;
    }
    
    // Check for at least one special character (!@#$%^&*_?><~)
    if (!/[!@#$%^&*_?><~]/.test(key)) {
        return false;
    }
    
    return true;
}

// Function to pre-fetch offline cases using the service worker
async function prefetch_offline_cases(offlineIds) {
    console.log('Pre-fetching offline cases...');
    
    try {
        // Wait for service worker to be ready and controlling
        await navigator.serviceWorker.ready;
        
        // Wait a bit for the service worker to take control
        let attempts = 0;
        while (!navigator.serviceWorker.controller && attempts < 10) {
            console.log('Waiting for service worker to take control...');
            await new Promise(resolve => setTimeout(resolve, 500));
            attempts++;
        }
        
        const serviceWorker = navigator.serviceWorker.controller;
        if (!serviceWorker) {
            throw new Error('Service worker not controlling the page');
        }
        
        console.log('Service worker is controlling, starting pre-fetch...');
        
        // Pre-fetch each case using the /api/case?case_id= endpoint
        for (const caseId of offlineIds) {
            try {
                console.log(`Pre-fetching case: ${caseId}`);
                const response = await fetch(`/api/case?case_id=${caseId}`);
                
                if (response.ok) {
                    const caseData = await response.json();
                    console.log(`Successfully fetched case ${caseId}, now sending to service worker`);
                    
                    // Send case data to service worker for caching
                    serviceWorker.postMessage({
                        type: 'CACHE_CASE_DATA',
                        data: {
                            caseId: caseId,
                            caseData: caseData
                        }
                    });
                    
                    console.log(`Successfully sent case ${caseId} to service worker for caching`);
                } else {
                    console.error(`Failed to pre-fetch case ${caseId}: ${response.status} ${response.statusText}`);
                }
            } catch (error) {
                console.error(`Error pre-fetching case ${caseId}:`, error);
            }
        }
        
        console.log(`Completed pre-fetching ${offlineIds.length} cases`);
        
    } catch (error) {
        console.error('Error in prefetch_offline_cases:', error);
        throw error;
    }
}

// Function to pre-cache essential pages for offline mode
async function precache_essential_pages() {
    console.log('Pre-caching essential pages...');
    
    const essentialPages = [
        '/Case'
        // Note: /Case/summary doesn't exist as a server route
        // Client-side routes like /Case#/summary are handled by the main /Case page
    ];
    
    try {
        for (const pagePath of essentialPages) {
            try {
                console.log(`Pre-caching page: ${pagePath}`);
                const response = await fetch(pagePath);
                
                if (response.ok) {
                    // The service worker should automatically cache this response
                    console.log(`Successfully pre-cached page: ${pagePath}`);
                } else {
                    console.warn(`Failed to pre-cache page ${pagePath}: ${response.status} ${response.statusText}`);
                }
            } catch (error) {
                console.warn(`Error pre-caching page ${pagePath}:`, error);
            }

        }
        
        console.log('Essential pages pre-caching completed');
        
    } catch (error) {
        console.error('Error in precache_essential_pages:', error);
        throw error;
    }
}

// Function to cache metadata using service worker
async function cache_metadata_with_service_worker() {
    console.log('Caching metadata with service worker...');
    
    const metadataEndpoints = [
        '/api/metadata',
        '/api/metadata/version_specification',
        '/api/user_role_jurisdiction_view/my-roles',
        '/api/jurisdiction_tree'
    ];
    
    try {
        // First get the metadata to determine the current version
        let currentVersion = null;
        try {
            const metadataResponse = await fetch('/api/metadata');
            if (metadataResponse.ok) {
                const metadata = await metadataResponse.json();
                currentVersion = metadata.version || metadata.data_dictionary?.version;
                console.log('Current metadata version:', currentVersion);
            }
        } catch (error) {
            console.warn('Could not determine metadata version:', error);
        }
        
        // Add validation endpoint if we have a version
        if (currentVersion) {
            metadataEndpoints.push(`/api/version/${currentVersion}/validation`);
        }
        
        for (const endpoint of metadataEndpoints) {
            try {
                const response = await fetch(endpoint);
                if (response.ok) {
                    console.log(`Cached metadata endpoint: ${endpoint}`);
                } else {
                    console.warn(`Failed to cache metadata endpoint ${endpoint}: ${response.status}`);
                }
            } catch (error) {
                console.warn(`Error caching metadata endpoint ${endpoint}:`, error);
            }
        }
        
        console.log('Metadata caching completed');
        
    } catch (error) {
        console.error('Error caching metadata:', error);
        throw error;
    }
}

// Function to set up service worker message listener
function setupServiceWorkerMessageListener() {
    if (!navigator.serviceWorker) return;
    
    navigator.serviceWorker.addEventListener('message', event => {
        const { type, data } = event.data;
        
        switch (type) {
            case 'CHECK_OFFLINE_STATUS':
                // Respond with current offline status
                const isOffline = localStorage.getItem('is_offline') === 'true';
                event.source.postMessage({
                    type: 'OFFLINE_STATUS_RESPONSE',
                    isOffline: isOffline
                });
                break;
                
            default:
                console.log('Service Worker message:', event.data);
        }
    });
    
    console.log('Service worker message listener set up');
}

// Function to unregister service worker (for going back online)
async function unregister_service_worker() {
    if ('serviceWorker' in navigator) {
        try {
            const registrations = await navigator.serviceWorker.getRegistrations();
            for (const registration of registrations) {
                const result = await registration.unregister();
                console.log('Service worker unregistered:', result);
            }
        } catch (error) {
            console.error('Error unregistering service worker:', error);
        }
    }
}

/* OLD CACHE FUNCTIONS - Replaced by Service Worker implementation
// Function to cache offline resources and case documents
async function cache_offline_resources(offlineIds, offlineKey, sessionId) {
    console.log('Starting resource caching for offline mode...');
    
    try {
        // Initialize caches
        await initialize_offline_caches();
        
        // Cache static resources (CSS, JS, HTML)
        console.log('Caching static resources...');
        await cache_static_resources();
        
        // Cache case documents
        console.log('Caching case documents...');
        await cache_case_documents(offlineIds);
        
        // Cache metadata and form definitions
        console.log('Caching metadata...');
        await cache_metadata();
        
        console.log('All resources cached successfully');
        
    } catch (error) {
        console.error('Error caching offline resources:', error);
        throw error;
    }
}

// Function to initialize cache storage
async function initialize_offline_caches() {
    if ('caches' in window) {
        // Create cache for static resources
        await caches.open('mmria-static-v1');
        
        // Create cache for case documents
        await caches.open('mmria-cases-v1');
        
        // Create cache for metadata
        await caches.open('mmria-metadata-v1');
        
        console.log('Cache storage initialized');
    } else {
        console.warn('Cache API not supported, using localStorage fallback');
    }
}

// Function to cache static resources
async function cache_static_resources() {
    const staticResources = [
        // CSS files
        '/css/index.css',
        '/css/bootstrap.min.css',
        '/css/mmria.css',
        
        // JavaScript files
        '/scripts/editor/page_renderer/app.mmria.js',
        '/scripts/editor/page_renderer/string.js',
        '/scripts/jquery.min.js',
        '/scripts/bootstrap.min.js',
        
        // Essential HTML pages (if any)
        '/',
        '/Home/Index',
        
        // Icons and images
        '/img/icon_pin.png',
        '/img/icon_unpin.png',
        '/img/icon_unpinMultiple.png',
    ];
    
    if ('caches' in window) {
        const cache = await caches.open('mmria-static-v1');
        
        for (const resource of staticResources) {
            try {
                const response = await fetch(resource);
                if (response.ok) {
                    await cache.put(resource, response);
                    console.log(`Cached static resource: ${resource}`);
                }
            } catch (error) {
                console.warn(`Failed to cache resource ${resource}:`, error);
            }
        }
    } else {
        // Fallback to localStorage for static resources
        for (const resource of staticResources) {
            try {
                const response = await fetch(resource);
                if (response.ok) {
                    const content = await response.text();
                    localStorage.setItem(`mmria_static_${resource.replace(/[^a-zA-Z0-9]/g, '_')}`, content);
                }
            } catch (error) {
                console.warn(`Failed to cache resource ${resource}:`, error);
            }
        }
    }
}

// Function to cache case documents
async function cache_case_documents(offlineIds) {
    const cacheStorage = 'caches' in window ? await caches.open('mmria-cases-v1') : null;
    const caseDocuments = [];
    
    console.log(`Fetching ${offlineIds.length} case documents for offline caching...`);
    
    for (const caseId of offlineIds) {
        try {
            // Fetch full case document using the correct API endpoint
            const response = await fetch(`/api/case?case_id=${caseId}`);
            if (response.ok) {
                const caseDocument = await response.json();
                caseDocuments.push(caseDocument);
                console.log(`Fetched case document: ${caseId}`);
            } else {
                console.error(`Failed to fetch case ${caseId}: ${response.status} ${response.statusText}`);
            }
        } catch (error) {
            console.error(`Failed to fetch case ${caseId}:`, error);
        }
    }
    
    // Cache all documents as a single array
    if (caseDocuments.length > 0) {
        const cacheKey = 'mmria_offline_case_documents';
        
        if (cacheStorage) {
            // Store in Cache API as a single entry
            const response = new Response(JSON.stringify(caseDocuments));
            await cacheStorage.put(cacheKey, response);
            console.log(`Cached ${caseDocuments.length} case documents in Cache API`);
        } else {
            // Store in localStorage as a single entry
            localStorage.setItem(cacheKey, JSON.stringify(caseDocuments));
            console.log(`Cached ${caseDocuments.length} case documents in localStorage`);
        }
    }
    
    // Store the full case documents array in mmria_cached_cases
    localStorage.setItem('mmria_cached_cases', JSON.stringify(caseDocuments));
    
    return caseDocuments;
}

// Function to cache metadata and form definitions
async function cache_metadata() {
    const metadataResources = [
        '/api/metadata',
        '/api/metadata/version_specification',
        '/api/user_role_jurisdiction_view/my-roles'
    ];
    
    const cacheStorage = 'caches' in window ? await caches.open('mmria-metadata-v1') : null;
    
    for (const resource of metadataResources) {
        try {
            const response = await fetch(resource);
            if (response.ok) {
                if (cacheStorage) {
                    await cacheStorage.put(resource, response.clone());
                } else {
                    const content = await response.text();
                    localStorage.setItem(`mmria_meta_${resource.replace(/[^a-zA-Z0-9]/g, '_')}`, content);
                }
                console.log(`Cached metadata: ${resource}`);
            }
        } catch (error) {
            console.warn(`Failed to cache metadata ${resource}:`, error);
        }
    }
}
END OLD CACHE FUNCTIONS */

// Function to check if application is in offline mode
function is_offline_mode() {
    const offlineSession = localStorage.getItem('mmria_offline_session');
    return offlineSession ? JSON.parse(offlineSession).isOffline : false;
}

// Function to get offline session data
function get_offline_session() {
    const offlineSession = localStorage.getItem('mmria_offline_session');
    return offlineSession ? JSON.parse(offlineSession) : null;
}

// Function to clear all cached data
async function clear_all_cached_data() {
    console.log('Starting cache cleanup...');
    
    // Clear Cache API data
    if ('caches' in window) {
        try {
            // Get all cache names and delete MMRIA-related ones
            const cacheNames = await caches.keys();
            const mmriaCaches = cacheNames.filter(name => name.startsWith('mmria-'));
            
            for (const cacheName of mmriaCaches) {
                await caches.delete(cacheName);
                console.log(`Deleted cache: ${cacheName}`);
            }
            
            console.log('Cache API data cleared');
        } catch (error) {
            console.warn('Error clearing Cache API data:', error);
        }
    }
    
    // Clear localStorage cached items
    const keysToRemove = [];
    for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key && (
            key.startsWith('mmria_static_') || 
            key.startsWith('mmria_case_') || 
            key.startsWith('mmria_meta_') ||
            key === 'mmria_cached_cases' ||
            key === 'mmria_offline_session' ||
            key === 'mmria_offline_case_documents' ||
            key === 'is_offline'
        )) {
            keysToRemove.push(key);
        }
    }
    
    // Remove all identified keys
    keysToRemove.forEach(key => {
        localStorage.removeItem(key);
        console.log(`Removed localStorage item: ${key}`);
    });
    
    console.log(`Cache cleanup complete. Removed ${keysToRemove.length} localStorage items.`);
}

// Function to get cached case documents
async function get_cached_case_documents() {
    const cacheKey = 'mmria_offline_case_documents';
    
    // Try Cache API first
    if ('caches' in window) {
        try {
            const cache = await caches.open('mmria-cases-v1');
            const response = await cache.match(cacheKey);
            if (response) {
                const caseDocuments = await response.json();
                console.log(`Retrieved ${caseDocuments.length} cached case documents from Cache API`);
                return caseDocuments;
            }
        } catch (error) {
            console.warn('Error retrieving from Cache API:', error);
        }
    }
    
    // Fallback to localStorage
    try {
        const cachedData = localStorage.getItem(cacheKey);
        if (cachedData) {
            const caseDocuments = JSON.parse(cachedData);
            console.log(`Retrieved ${caseDocuments.length} cached case documents from localStorage`);
            return caseDocuments;
        }
    } catch (error) {
        console.warn('Error retrieving from localStorage:', error);
    }
    
    console.log('No cached case documents found');
    return [];
}

// Function to exit offline mode
async function exit_offline_mode() {
    // Clear all cached data using the dedicated function
    await clear_all_cached_data();
    
    // Remove offline mode indicator
    document.body.classList.remove('mmria-offline-mode');
    
    console.log('Offline mode deactivated');
}

// Function to check offline status on page load and hide elements if needed
function checkOfflineStatusOnLoad() {
    const isOffline = localStorage.getItem('is_offline') === 'true';
    if (isOffline) {
        console.log('Page loaded in offline mode - hiding case listing elements');
        // Use setTimeout to ensure DOM is fully rendered
        setTimeout(() => {
            hideOnlineCaseListingElements();
        }, 100);
    }
}

// Call on page load
document.addEventListener('DOMContentLoaded', checkOfflineStatusOnLoad);

// Also call when the app content is rendered (for single-page app scenarios)
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', checkOfflineStatusOnLoad);
} else {
    // DOM is already loaded
    checkOfflineStatusOnLoad();
}