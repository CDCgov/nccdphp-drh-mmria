/**
 * Offline UI Renderer Module
 * Handles rendering of offline case listings and UI elements
 */

// Function to render individual offline processing item
function render_offline_processing_item(caseDoc, i) {
    const modifiedDocument = caseDoc.modifiedDocument || caseDoc.ModifiedDocument || {};
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

    // Try multiple possible property names for sync state
    let syncState = caseDoc.syncState;

    // Access nested properties from the proper mmria_case structure
    const caseID = modifiedDocument._id;
    
    const rev = modifiedDocument._rev;    
    const hostState = modifiedDocument.host_state;
    const jurisdictionID = modifiedDocument.home_record?.jurisdiction_id;
    const firstName = modifiedDocument.home_record?.first_name;
    const lastName = modifiedDocument.home_record?.last_name;
    const recordID = modifiedDocument.home_record?.record_id;
    const recordIDDisplay = recordID ? `- (${recordID})` : '';
    const agencyCaseID = modifiedDocument.home_record?.agency_case_id;
    const createdBy = modifiedDocument.created_by;
    const lastUpdatedBy = modifiedDocument.last_updated_by;
    const caseStatus = modifiedDocument.home_record?.case_status?.overall_case_status;
    const currentCaseStatus = caseStatus == null ? '(blank)' : caseStatuses[caseStatus.toString()];
    const dateCreated = modifiedDocument.date_created ? new Date(modifiedDocument.date_created).toLocaleDateString('en-US') : '';
    const lastUpdatedDate = modifiedDocument.date_last_updated ? new Date(modifiedDocument.date_last_updated).toLocaleDateString('en-US') : '';
    const isOfflineCreated = agencyCaseID && agencyCaseID.indexOf('-offline') !== -1;

    let projectedReviewDate = modifiedDocument.home_record?.case_status?.projected_review_date ? new Date(modifiedDocument.home_record.case_status.projected_review_date).toLocaleDateString('en-US') : '';
    let actualReviewDate = modifiedDocument.home_record?.case_status?.committee_review_date ? new Date(modifiedDocument.home_record.case_status.committee_review_date).toLocaleDateString('en-US') : '';
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;

    // Check if case was released by admin
    // Search for the case in g_ui.case_view_list by record_id to verify offline status
    let wasReleasedByAdmin = false;
    if (recordID && g_ui.case_view_list && syncState === 0) {
        const currentCase = g_ui.case_view_list.find(c => 
            c.value && c.value.record_id === recordID
        );
        
        if (currentCase && currentCase.value) {
            const isOffline = currentCase.value.is_offline === 'true' || currentCase.value.is_offline === true;
            const offlineBy = currentCase.value.offline_by;
            const currentUser = g_user_name;
            
            // If case is NOT offline OR is offline by a different user, it was released by admin
            if (!isOffline || (offlineBy && offlineBy !== currentUser)) {
                wasReleasedByAdmin = true;
                syncState = 4; // Override to "Released by Admin"
                offlineLog.log('OfflineUIRenderer', `Case ${caseID} (record_id: ${recordID}) was released by admin. is_offline: ${isOffline}, offline_by: ${offlineBy}, current_user: ${currentUser}`);
            }
        }
    }

    const canSync = syncState === 0; // Only allow sync if pending
    const canAbandon = syncState === 0 || (syncState === 4 && !caseDoc.syncState); // Allow abandon if pending or released by admin
    const canDelete = syncState === 0 || (syncState === 4 && !caseDoc.syncState); // Allow delete if pending or released by admin

    // Map sync state to human-readable text
    const syncStateText = {
        0: 'Upload Pending',
        1: 'Upload Successful',
        2: 'Upload Abandoned',
        3: 'Upload Deleted',
        4: 'Released by Admin',
        5: 'Error'
    };

    const syncStatusDisplay = syncStateText[syncState] || 'Unknown';
    
    // Get timestamp and format it
    const timestamp = syncState === 4 ? new Date().toISOString() : (caseDoc.timestamp || caseDoc.Timestamp || modifiedDocument.date_last_updated);
    const timestampDisplay = timestamp ? new Date(timestamp).toLocaleString('en-US') : '';

    // Check if this document has offline changes
    let hasChanges = false;
    let changeIndicator = '';
    try {
        if (g_offline_changes && g_offline_changes.has(caseID)) {
            hasChanges = true;
            const changeRecord = g_offline_changes.get(caseID);
            changeIndicator = `
                <div style="margin-top: 4px;">
                    <span class="badge badge-warning" title="Document has offline changes made at ${new Date(changeRecord.timestamp).toLocaleString()}">
                        <i class="fa fa-edit"></i> Modified Offline
                    </span>
                </div>
            `;
        }
    } catch (error) {
        offlineLog.warn('OfflineUIRenderer', 'Error checking for offline changes:', error);
    }

    // Define CSS class for disabled button styling
    const upload_button_class = !canSync ? 'offline-processing-disabled' : '';
    const delete_button_class = !canDelete ? 'offline-processing-disabled' : '';
    const abandon_button_class = !canAbandon ? 'offline-processing-disabled' : '';

    return `
        <tr class="tr" path="${caseID}" ${hasChanges ? 'style="background-color: #fff3cd;"' : ''}>
            <td class="td">
                <a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordIDDisplay} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
                ${changeIndicator}
            </td>
            <td class="td">${currentCaseStatus}</td>
            <td class="td">${reviewDates}</td>
            <td class="td">${createdBy} - ${dateCreated}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">${syncStatusDisplay}${timestampDisplay ? ' - ' + timestampDisplay : ''}</td>
            <td class="td">
                <button type="button" class="btn btn-primary ${upload_button_class}" onclick="sync_offline_changes('${caseID}')" style="line-height: 1.0; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" ${!canSync ? 'disabled' : ''}>
                    Upload
                </button>            
                ${isOfflineCreated ? `
                <button type="button" class="btn btn-primary ${delete_button_class}" onclick="handle_delete_changes_click('${caseID}', ${syncState === 4 ? 4 : 2})" style="margin-top:2px;line-height: 1.0; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" ${!canDelete ? 'disabled' : ''}>
                     Abandon</br> Changes
                </button>
                ` : `
                <button type="button" class="btn btn-primary ${abandon_button_class}" onclick="handle_abandon_changes_click('${caseID}', ${syncState === 4 ? 4 : 2})" style="margin-top:2px; line-height: 1.0; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" ${!canAbandon ? 'disabled' : ''}>
                    Abandon</br> Changes
                </button>
                `}          
                
            </td>
        </tr>
    `;
}

// Function to render individual offline-only document item
function render_offline_only_document_item(item, i) {
    const caseStatuses = {
        9999:"(blank)",	
        1:"Abstracting (Incomplete)",
        2:"Abstraction Complete",
        3:"Ready for Review",
        4:"Review Complete and Decision Entered",
        5:"Out of Scope and Death Certificate Entered",
        6:"False Positive and Death Certificate Entered",
        0:"Vitals Import"
    }; 

    const caseID = item.id;
    const rev = item.rev;
    
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.jurisdiction_id;
    const firstName = item.value.first_name;
    const lastName = item.value.last_name;
    const recordID = item.value.record_id ? `- (${item.value.record_id})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const currentCaseStatus = item.value.case_status == null ? '(blank)' : caseStatuses[parseInt((item.value.case_status.overall_case_status != null ? item.value.case_status.overall_case_status : item.value.case_status).toString())];
    const dateCreated = item.value.date_created ? new Date(item.value.date_created).toLocaleDateString('en-US') : '';
    const lastUpdatedDate = item.value.date_last_updated ? new Date(item.value.date_last_updated).toLocaleDateString('en-US') : '';
    
    let projectedReviewDate = item.value.review_date_projected ? new Date(item.value.review_date_projected).toLocaleDateString('en-US') : '';
    let actualReviewDate = item.value.review_date_actual ? new Date(item.value.review_date_actual).toLocaleDateString('en-US') : '';
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;

    // Check if this document has offline changes
    let hasChanges = false;
    let changeIndicator = '';
    const isNew = rev == null;
    let isNewIndicator = '';
    if (isNew) {
        isNewIndicator = `
            <div style="margin-top: 4px;">
                <span class="badge badge-success" title="This is a new offline document that has not been uploaded yet">
                    <i class="fa fa-plus"></i> New Offline Document
                </span>
            </div>
        `;
    }
    try {
        if (g_offline_changes && g_offline_changes.has(caseID)) {
            hasChanges = true;
            const changeRecord = g_offline_changes.get(caseID);
            changeIndicator = `
                <div style="margin-top: 4px;">
                    <span class="badge badge-warning" title="Document has offline changes made at ${new Date(changeRecord.timestamp).toLocaleString()}">
                        <i class="fa fa-edit"></i> Modified Offline
                    </span>
                </div>
            `;
        }
    } catch (error) {
        offlineLog.warn('OfflineUIRenderer', 'Error checking for offline changes:', error);
    }

    return `
        <tr class="tr" path="${caseID}" ${hasChanges ? 'style="background-color: #fff3cd;"' : ''}>
            <td class="td">
                <a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
                ${changeIndicator} ${isNewIndicator}
            </td>
            <td class="td">${currentCaseStatus}</td>
            <td class="td">${reviewDates}</td>
            <td class="td">${createdBy} - ${dateCreated}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>            
            <td class="td">
                <button type="button" class="btn btn-primary" onclick="offline_mode_abandon_offline_changes('${caseID}')" style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;">
                    Abandon Changes
                </button>
            </td>
        </tr>
    `;
}

// Function to render individual offline document item
function render_offline_document_item(item, i) {
    const caseStatuses = {
        9999:"(blank)",	
        1:"Abstracting (Incomplete)",
        2:"Abstraction Complete",
        3:"Ready for Review",
        4:"Review Complete and Decision Entered",
        5:"Out of Scope and Death Certificate Entered",
        6:"False Positive and Death Certificate Entered",
        0:"Vitals Import"
    }; 

    const caseID = item.id;
    
    // Find the actual index in the main case list for proper routing
    const actualIndex = g_ui.case_view_list ? g_ui.case_view_list.findIndex(c => c.id === caseID) : -1;
    const caseIndex = actualIndex >= 0 ? actualIndex : i;
    
    const hostState = item.value.host_state;
    const jurisdictionID = item.value.jurisdiction_id;
    const firstName = item.value.first_name;
    const lastName = item.value.last_name;
    const recordID = item.value.record_id ? `- (${item.value.record_id})` : '';
    const agencyCaseID = item.value.agency_case_id;
    const createdBy = item.value.created_by;
    const lastUpdatedBy = item.value.last_updated_by;
    const currentCaseStatus = item.value.case_status == null ? '(blank)' : caseStatuses[parseInt(item.value.case_status)];
    const dateCreated = item.value.date_created ? new Date(item.value.date_created).toLocaleDateString('en-US') : '';
    const lastUpdatedDate = item.value.date_last_updated ? new Date(item.value.date_last_updated).toLocaleDateString('en-US') : '';
    
    let projectedReviewDate = item.value.review_date_projected ? new Date(item.value.review_date_projected).toLocaleDateString('en-US') : '';
    let actualReviewDate = item.value.review_date_actual ? new Date(item.value.review_date_actual).toLocaleDateString('en-US') : '';
    if (projectedReviewDate.length < 1 && actualReviewDate.length > 0) projectedReviewDate = '(blank)';
    if (projectedReviewDate.length > 0 && actualReviewDate.length < 1) actualReviewDate = '(blank)';
    const reviewDates = `${projectedReviewDate}${projectedReviewDate || actualReviewDate ? ', ' : ''} ${actualReviewDate}`;

    // Check if this document has offline changes
    let hasChanges = false;
    let changeIndicator = '';
    try {
        if (g_offline_changes && g_offline_changes.has(caseID)) {
            hasChanges = true;
            const changeRecord = g_offline_changes.get(caseID);
            changeIndicator = `
                <div style="margin-top: 4px;">
                    <span class="badge badge-warning" title="Document has offline changes made at ${new Date(changeRecord.timestamp).toLocaleString()}">
                        <i class="fa fa-edit"></i> Modified Offline
                    </span>
                </div>
            `;
        }
    } catch (error) {
        offlineLog.warn('OfflineUIRenderer', 'Error checking for offline changes:', error);
    }

    return `
        <tr class="tr" path="${caseID}" ${hasChanges ? 'style="background-color: #fff3cd;"' : ''}>
            <td class="td">
                <a href="#/${caseIndex}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>
                ${changeIndicator}
            </td>
            <td class="td">${currentCaseStatus}</td>
            <td class="td">${reviewDates}</td>
            <td class="td">${createdBy} - ${dateCreated}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">${lastUpdatedBy} - ${lastUpdatedDate}</td>
            <td class="td">
                <button type="button" class="btn btn-primary" onclick="remove_offline_mode_softlock('${caseID}')" style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px; ${g_offline_operation_in_progress ? 'color: white; background-color: rgba(113, 33, 119, 0.7450980392); border-color: #cfcfcf;' : ''}" ${g_offline_operation_in_progress ? 'disabled' : ''}>
                    Remove</br> From List
                </button>
            </td>
        </tr>
    `;
}

// Function to hide case listing elements when going offline
function hideOnlineCaseListingElements() {
    offlineLog.log('OfflineUIRenderer', 'Hiding case listing elements for offline mode');
    
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
            offlineLog.log('OfflineUIRenderer', 'Case listing table hidden');
        }
    });
    
    // Hide pagination elements
    const paginationElements = document.querySelectorAll('.table-pagination');
    paginationElements.forEach(element => {
        element.style.display = 'none';
        offlineLog.log('OfflineUIRenderer', 'Pagination element hidden');
    });
    
    // Hide the search/filter form elements
    offlineLog.log('OfflineUIRenderer', 'Looking for search/filter elements to hide...');
    
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
                offlineLog.log('OfflineUIRenderer', `Search element container hidden: ${elementId}`);
            } else {
                element.style.display = 'none';
                offlineLog.log('OfflineUIRenderer', `Search element hidden: ${elementId}`);
            }
        }
    });
    
    // Hide the Apply Filters and Reset buttons
    const applyFilterButton = document.querySelector('button[onclick*="apply_filter_click"]');
    if (applyFilterButton) {
        const buttonContainer = applyFilterButton.closest('.form-inline');
        if (buttonContainer) {
            buttonContainer.style.display = 'none';
            offlineLog.log('OfflineUIRenderer', 'Apply Filters button container hidden');
        }
    }
    
    // Hide any remaining form elements that might be missed
    const searchForm = document.querySelector('form[onsubmit*="get_case_set"]');
    if (searchForm) {
        searchForm.style.display = 'none';
        offlineLog.log('OfflineUIRenderer', 'Search form hidden');
    }
    
    // Alternative approach - hide by class or parent elements if the direct selectors don't work
    const searchContainer = document.querySelector('.search-container, .case-search-form, [id*="search"], [class*="search"]');
    if (searchContainer) {
        searchContainer.style.display = 'none';
        offlineLog.log('OfflineUIRenderer', 'Search container hidden');
    }
}

// Function to show case listing elements when going online
function showOnlineCaseListingElements() {
    offlineLog.log('OfflineUIRenderer', 'Showing case listing elements for online mode');
    
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
            offlineLog.log('OfflineUIRenderer', 'Case listing table shown');
        }
    });
    
    // Show pagination elements
    const paginationElements = document.querySelectorAll('.table-pagination');
    paginationElements.forEach(element => {
        element.style.display = '';
        offlineLog.log('OfflineUIRenderer', 'Pagination element shown');
    });
    
    // Show the search/filter form elements
    offlineLog.log('OfflineUIRenderer', 'Looking for search/filter elements to show...');
    
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
                offlineLog.log('OfflineUIRenderer', `Search element container shown: ${elementId}`);
            } else {
                element.style.display = '';
                offlineLog.log('OfflineUIRenderer', `Search element shown: ${elementId}`);
            }
        }
    });
    
    // Show the Apply Filters and Reset buttons
    const applyFilterButton = document.querySelector('button[onclick*="apply_filter_click"]');
    if (applyFilterButton) {
        const buttonContainer = applyFilterButton.closest('.form-inline');
        if (buttonContainer) {
            buttonContainer.style.display = '';
            offlineLog.log('OfflineUIRenderer', 'Apply Filters button container shown');
        }
    }
    
    // Show any remaining form elements that might be missed
    const searchForm = document.querySelector('form[onsubmit*="get_case_set"]');
    if (searchForm) {
        searchForm.style.display = '';
        offlineLog.log('OfflineUIRenderer', 'Search form shown');
    }
    
    // Show search container
    const searchContainer = document.querySelector('.search-container, .case-search-form, [id*="search"], [class*="search"]');
    if (searchContainer) {
        searchContainer.style.display = '';
        offlineLog.log('OfflineUIRenderer', 'Search container shown');
    }
}

// Make functions globally available
window.render_offline_processing_item = render_offline_processing_item;
window.render_offline_only_document_item = render_offline_only_document_item;
window.render_offline_document_item = render_offline_document_item;
window.hideOnlineCaseListingElements = hideOnlineCaseListingElements;
window.showOnlineCaseListingElements = showOnlineCaseListingElements;
