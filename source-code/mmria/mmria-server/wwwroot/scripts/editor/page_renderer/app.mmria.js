// Cache version fetching - lazy-loaded from server endpoint
let cachedApiVersionInfo = null;
let apiVersionPromise = null;

// Global flag to track if an offline toggle operation is in progress
let g_offline_operation_in_progress = false;

// Global flag to track if a processing operation (abandon/delete) is in progress
let g_processing_operation_in_progress = false;

// Fetch cache version from server endpoint (single source of truth)
async function fetchCacheVersionFromServer() {
    try {
        // Return cached version if available
        if (cachedApiVersionInfo) {
            return cachedApiVersionInfo.cacheVersion;
        }

        // Return existing promise if already fetching
        if (apiVersionPromise) {
            const versionInfo = await apiVersionPromise;
            return versionInfo.cacheVersion;
        }

        // Create fetch promise
        apiVersionPromise = fetch('/api/OfflineCase/cache-version')
            .then(response => {
                if (response.ok) {
                    return response.json();
                } else {
                    throw new Error(`Failed to fetch cache version: ${response.status}`);
                }
            })
            .catch(error => {
                offlineLog.error('AppMMRIA', 'Could not fetch cache version from server:', error);
                // Throw error instead of using hardcoded fallback
                throw error;
            });

        const versionInfo = await apiVersionPromise;
        cachedApiVersionInfo = versionInfo;
        return versionInfo.cacheVersion;
    } catch (error) {
        offlineLog.error('AppMMRIA', 'Error in fetchCacheVersionFromServer:', error);
        // Re-throw error instead of returning hardcoded fallback
        throw error;
    }
}

// Function to get the actual API cache name (handles session-specific caches)
async function getActualApiCacheName() {
    try {
        if (!('caches' in window)) {
            return await fetchCacheVersionFromServer();
        }
        
        // Get the current cache version from server
        const baseVersion = await fetchCacheVersionFromServer();
        const cacheNames = await caches.keys();
        
        // First, check for session-specific cache (offline mode with active session)
        // Session caches follow pattern: baseVersion-session-{sessionId}
        const sessionCacheName = cacheNames.find(name => 
            name.startsWith(baseVersion + '-session-')
        );
        if (sessionCacheName) {
            offlineLog.log('AppMMRIA', 'Service Worker: Cache version fetched from server:', sessionCacheName);
            return sessionCacheName;
        }
        
        // Otherwise, look for the base API cache (online mode)
        const baseCacheName = cacheNames.find(name => 
            name === baseVersion
        );
        if (baseCacheName) {
            offlineLog.log('AppMMRIA', 'Found base API cache:', baseCacheName);
            return baseCacheName;
        }
        
        // Fallback to base name from server
        offlineLog.warn('AppMMRIA', 'No API cache found, using fallback:', baseVersion);
        return baseVersion;
    } catch (error) {
        offlineLog.error('AppMMRIA', 'Error getting actual cache name:', error);
        // Re-throw error instead of returning hardcoded fallback
        throw error;
    }
}

// Expose function globally for access from other scripts
window.getActualApiCacheName = getActualApiCacheName;
window.fetchCacheVersionFromServer = fetchCacheVersionFromServer;



// Global array to map offline case indices to case IDs (for routing)
let g_offline_case_index_map = [];

// Global variable to track offline document changes
// Initialize from localStorage if available, otherwise create empty Map
let g_offline_changes = (() => {
    try {
        const isOfflineMode = localStorage.getItem('is_offline') === 'true';
        if (isOfflineMode) {
            const storedChanges = localStorage.getItem('mmria_offline_changes');
            if (storedChanges) {
                offlineLog.log('AppMMRIA', 'Initializing g_offline_changes from localStorage');
                return new Map(JSON.parse(storedChanges));
            }
        }
    } catch (error) {
        offlineLog.error('AppMMRIA', 'Error initializing g_offline_changes from localStorage:', error);
    }
    return new Map();
})();

// Global variable to track original documents for comparison
let g_original_offline_documents = new Map();

function commaSeperatedListOfHtmlTags(tags) {
    if (!tags) return '';
    if (!Array.isArray(tags)) return String(tags);
    return tags.filter(x => x !== null && x !== undefined && String(x).trim().length > 0).join(', ');
}

// Make offline change tracking functions globally available
// Use wrapper functions to ensure modules are available at call time
window.track_offline_document_change = function(...args) {
    return window.OfflineChangeTracker?.track?.(...args);
};
window.initialize_offline_change_tracking = function(...args) {
    return window.OfflineChangeTracker?.initialize?.(...args);
};
window.get_all_offline_changes = function(...args) {
    return window.OfflineChangeTracker?.getAll?.(...args);
};
window.clear_offline_changes = function(...args) {
    return window.OfflineChangeTracker?.clear?.(...args);
};
window.fetchAndStoreOriginalDocument = function(...args) {
    return window.OfflineChangeTracker?.fetchAndStoreOriginal?.(...args);
};
window.sync_offline_changes = function(...args) {
    return window.OfflineSyncManager?.sync?.(...args);
};
window.abandon_offline_changes = function(...args) {
    return window.OfflineSyncManager?.abandon?.(...args);
};
window.finish_online_processing_mode = function(...args) {
    return window.OfflineSyncManager?.finishOnlineProcessingMode?.(...args);
};
window.update_cached_case_document = function(...args) {
    return window.OfflineSyncManager?.updateCachedDocument?.(...args);
};
window.offline_mode_abandon_offline_changes = function(...args) {
    return window.OfflineModals?.abandonOfflineChanges?.(...args);
};
window.show_abandon_case_modal = function(...args) {
    return window.OfflineModals?.showAbandonCase?.(...args);
};
window.close_abandon_case_modal = function(...args) {
    return window.OfflineModals?.closeAbandonCase?.(...args);
};
window.confirm_abandon_case = function(...args) {
    return window.OfflineModals?.confirmAbandonCase?.(...args);
};
window.show_revision_mismatch_modal = function(...args) {
    return window.OfflineModals?.showRevisionMismatch?.(...args);
};
window.close_revision_mismatch_modal = function(...args) {
    return window.OfflineModals?.closeRevisionMismatch?.(...args);
};
window.show_case_already_offline_modal = function(...args) {
    return window.OfflineModals?.showCaseAlreadyOffline?.(...args);
};
window.close_case_already_offline_modal = function(...args) {
    return window.OfflineModals?.closeCaseAlreadyOffline?.(...args);
};
window.show_case_already_online_modal = function(...args) {
    return window.OfflineModals?.showCaseAlreadyOnline?.(...args);
};
window.close_case_already_online_modal = function(...args) {
    return window.OfflineModals?.closeCaseAlreadyOnline?.(...args);
};
window.show_go_online_modal = function(...args) {
    return window.OfflineModals?.showGoOnline?.(...args);
};
window.close_go_online_modal = function(...args) {
    return window.OfflineModals?.closeGoOnline?.(...args);
};
window.show_abandon_changes_processing_modal = function(...args) {
    return window.OfflineModals?.showAbandonChangesProcessing?.(...args);
};
window.close_abandon_changes_processing_modal = function(...args) {
    return window.OfflineModals?.closeAbandonChangesProcessing?.(...args);
};
window.confirm_abandon_changes_processing = function(...args) {
    return window.OfflineModals?.confirmAbandonChangesProcessing?.(...args);
};
window.show_delete_changes_processing_modal = function(...args) {
    return window.OfflineModals?.showDeleteChangesProcessing?.(...args);
};
window.close_delete_changes_processing_modal = function(...args) {
    return window.OfflineModals?.closeDeleteChangesProcessing?.(...args);
};
window.confirm_delete_changes_processing = function(...args) {
    return window.OfflineModals?.confirmDeleteChangesProcessing?.(...args);
};

// Make network monitoring functions globally available
window.check_network_connectivity = function(...args) {
    return window.OfflineNetworkMonitor?.check?.(...args);
};
window.update_go_online_button_state = function(...args) {
    return window.OfflineNetworkMonitor?.updateGoOnlineButtonState?.(...args);
};
window.handle_network_status_change = function(...args) {
    return window.OfflineNetworkMonitor?.handleStatusChange?.(...args);
};
window.initialize_network_monitoring = function(...args) {
    return window.OfflineNetworkMonitor?.initialize?.(...args);
};

// Make offline transition functions globally available
window.go_offline_clicked = function(...args) {
    return window.OfflineTransitionManager?.goOfflineClicked?.(...args);
};
window.go_online_clicked = function(...args) {
    return window.OfflineTransitionManager?.goOnlineClicked?.(...args);
};

// Banner helpers for post-sync geo updates
window.clear_cases_to_update_geo = function() {
    try {
        localStorage.removeItem('cases_to_update_geo');
    } catch (e) {
        console.error('Error clearing cases_to_update_geo:', e);
    }

    // Refresh summary view to hide banner
    if (typeof get_case_set === 'function') {
        get_case_set();
    }
};

window.render_synced_case_link = function(caseId) {
    try {
        if (!caseId) return '';
        if (!g_ui || !Array.isArray(g_ui.case_view_list)) return String(caseId);

        const i = g_ui.case_view_list.findIndex(x => x && x.id === caseId);
        if (i < 0) return String(caseId);

        const item = g_ui.case_view_list[i];
        const v = item && item.value ? item.value : {};

        const hostState = v.host_state || '';
        const jurisdictionID = v.jurisdiction_id || '';
        const firstName = v.first_name || '';
        const lastName = v.last_name || '';
        const recordID = v.record_id ? `${v.record_id}` : '';
        const agencyCaseID = v.agency_case_id;

        //return `<a href="#/${i}/home_record">${hostState} ${jurisdictionID}: ${lastName}, ${firstName} ${recordID} ${agencyCaseID ? ` ac_id: ${agencyCaseID}` : ''}</a>`;
        return `<a href="#/${i}/home_record">${recordID}</a>`;
    } catch (e) {
        console.error('Error rendering synced case link:', e);
        return String(caseId || '');
    }
};

// Make offline utility functions globally available
window.generateSecureOfflineKeySalt = function(...args) {
    return window.OfflineUtils?.generateKeySalt?.(...args);
};
window.deriveOfflineKeyHash = function(...args) {
    return window.OfflineUtils?.deriveKeyHash?.(...args);
};

// Function to render individual offline document item
function app_render(p_result, p_metadata, p_data, p_ui, p_metadata_path, p_object_path, p_dictionary_path, p_is_grid_context, p_post_html_render, p_search_ctx, p_ctx) 
{
    const isGoOfflineError = localStorage.getItem('is_go_offline_error') || 'false';    
    const casesToUpdateGeo = localStorage.getItem('cases_to_update_geo') || '';    
    const isProcessingOfflineCases = localStorage.getItem('process_offline_cases') || 'false';
    const isOfflineMode = localStorage.getItem('is_offline') || 'false';
    const isAbandonOfflineChangesInProgress = localStorage.getItem('abandon_offline_session') || 'false';
        
    if(isAbandonOfflineChangesInProgress ==='true'){
            p_result.push(`
            <div class=""  role="alert" style="background-color:border-top: 1px; background-color: #fff7e1;border: 1px solid #ffecb3; padding: 20px; ">
               <div style="display: flex; align-items: flex-start; gap: 10px;">
                    <img src="./img/offline-warn.svg" alt="Go Online Alert"> 
                    <div style="font-size: 18px; "> 
                            This account has an active offline session. Please try logging in from the original brower/device. If you are unable to return to the original session, you have the option of abandoning the active offline session.
                            <br><br>
                            Proceeding will result in the permanent loss of any changes made offline and prevent them from syncing. Are you sure you want to continue?               
                    </div>
              </div>
              <div style="width:100%; text-align:right; margin-top:10px;">
                     <button id="abandon-offline-session" type="button" class="btn btn-primary " onclick="abandon_offline_session()" title="Clear offline processing mode and return to normal case listing">
                               Yes, abandon offline session
                     </button>
                </div>
            </div>`)
            
            return;
    }

    const offlineSession = localStorage.getItem('mmria_offline_session');
    let sessionData;
    let offlineSessionId;
    try {
        sessionData = JSON.parse(offlineSession);
        // Try both possible field names for session ID
        offlineSessionId = sessionData.sessionId || sessionData.offlineSessionId;        
    } catch (error) {        
    }

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
    // Post-sync geo reminder banner (only when back online)
    if (isOfflineMode !== 'true' && isProcessingOfflineCases !== 'true') {
        const syncedCaseIds = (casesToUpdateGeo || '')
            .split(',')
            .map(x => (x || '').trim())
            .filter(x => x && x.toLowerCase() !== 'false');

        if (syncedCaseIds.length > 0) {
            const uniqueIds = Array.from(new Set(syncedCaseIds));
            p_result.push(`
            <div class="" role="alert" style="background-color:border-top: 1px; background-color: #ffecb3;border: 1px solid #ffecb3; padding: 20px; margin-top: 20px; margin-bottom: 20px; ">
                <div style="display: flex; align-items: flex-start; gap: 10px;">
                    <img src="./img/offline-warn.svg" alt="Go Online Alert"> 
                    <div style="font-size: 17px; flex: 1;"> 
                        Cases synced. Please return to these cases and <b>click the "Validate Address and Get Geography Context"</b> button for each Address you added/updated while in Offline Mode.
                        <br/><br/>
                        ${commaSeperatedListOfHtmlTags(uniqueIds.map(id => render_synced_case_link(id)))}
                        <div style="width: 100%; text-align: right; margin-top: 10px;">
                            <button id='add-new-case' class='btn btn-primary' onclick='clear_cases_to_update_geo()' >Dismiss</button>
                        </div>
                    </div>
                </div>              
            </div>`);
        }
    }

    if(isOfflineMode === 'true' || isProcessingOfflineCases === 'true'){ 
        const newCaseCount =  g_ui.offline_mode_case_view_list ? g_ui.offline_mode_case_view_list.filter(doc => doc.rev == null).length : 0;
        const newCaseButtonDisabled = (newCaseCount >= offline_mode_max_new_cases) ? true : false;
        if(newCaseButtonDisabled){
            p_result.push(`<button id='add-new-case offline-processing-disable' class='btn btn-primary' onclick='init_inline_loader(add_new_case_button_click)' disabled='disabled' ${is_read_only_html}>Add New Case</button>`);
        }
        else if (isProcessingOfflineCases !== 'true') {
            p_result.push(`<button id='add-new-case' class='btn btn-primary' onclick='init_inline_loader(add_new_case_button_click)' ${is_read_only_html}>Add New Case</button>`);
        }

    }   
    else{
        p_result.push(`<button id='add-new-case' class='btn btn-primary' onclick='init_inline_loader(add_new_case_button_click)' ${is_read_only_html}>Add New Case</button>`);
    }

    p_result.push("<span class='spinner-container spinner-inline ml-2'><span class='spinner-body text-primary'><span class='spinner'></span></span>");
    p_result.push("</div>");
    p_result.push("</div> <!-- end .content-intro -->");
    

    // Check if we're in offline mode - if so, skip case listing and filters
    const isOfflineStatus = localStorage.getItem('is_offline') || 'false';
    
    if (isOfflineStatus !== 'true' && isProcessingOfflineCases !== 'true') {
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

    // Add offline-only documents section (only shown when in offline mode)
    //p_result.push("<div id='offline-only-documents-section' class='mb-4'>");
    //p_result.push("</div>");

    // Add offline documents section
    //p_result.push("<div id='offline-documents-section' class='mb-4'>");
    //p_result.push("</div>");

    // Add offline processing section
    p_result.push("<div id='offline-processing-section' class='mb-4'>");
    p_result.push("</div>");


    if(isGoOfflineError ==='true'){
        console.log(`[RENDER-DEBUG] Rendering error message | isGoOfflineError=${isGoOfflineError}`);
        p_result.push(`
        <div class=""  role="alert" style="background-color:border-top: 1px; background-color: #ffe7e7;border: 1px solid #ffc2c2; padding: 20px; margin-top: 20px; margin-bottom: 20px; ">
            <div style="display: flex; align-items: flex-start; gap: 10px;">
                <img src="./img/offline-warning.svg" alt="Go Online Alert"> 
                <div style="font-size: 18px; "> 
                    Cases could not be brought offline. Please try again later. If the issue persists, please contact MMRIASupport@cdc.gov for assistance.
                </div>
            </div>              
        </div>`)

        localStorage.setItem('is_go_offline_error', 'false');
    }         


    if (g_ui.process_offline_case_view_list_by_user && g_ui.process_offline_case_view_list_by_user.length > 0) {
        p_result.push("<table class='table mb-0'>");
        p_result.push("<thead class='thead'>");
        p_result.push("<tr class='tr bg-tertiary'>");
        p_result.push("<th class='th h4' colspan='7' scope='colgroup'>Offline Processing Cases</th>");
        p_result.push("</tr>");
        p_result.push("<tr class='tr'>");
        p_result.push("<th class='th' scope='col'>Case Information</th>");
        p_result.push("<th class='th' scope='col'>Case Status</th>");
        p_result.push("<th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>");
        p_result.push("<th class='th' scope='col'>Created</th>");
        p_result.push("<th class='th' scope='col'>Last Updated</th>");
        p_result.push("<th class='th' scope='col'>Currently Edited By</th>");
        p_result.push("<th class='th' scope='col' style=\"width: 115px;\">Actions</th>");
        p_result.push("</tr>");
        p_result.push("</thead>");
        p_result.push("<tbody class='tbody'>");
        g_ui.process_offline_case_view_list_by_user.forEach((item, i) => {
            p_result.push(render_offline_processing_item(item, i));
        });
        p_result.push("</tbody>");
        p_result.push("</table>");
    }

    //code to render offline processing table g_ui.process_offline_case_view_list_by_user 
    //offline case processing
    if (isProcessingOfflineCases === 'true') {

        if(!g_ui.process_offline_case_view_list_by_user || !g_ui.process_offline_case_view_list_by_user.case_documents)return "";

        const allDocumentsSynced = g_ui.process_offline_case_view_list_by_user.case_documents.every(doc => doc.syncState !== 0);
        const exit_button_class = !allDocumentsSynced ? 'offline-processing-disabled' : '';
        p_result.push(`
            <div class="alert alert-success" style="border-top: 1px;" role="alert">
               <img src="./img/go-online-alert.svg" alt="Go Online Alert"> Return to online mode successful. Please upload all offline cases to save changes and access other online cases.
            </div>
            <table class="table mb-0">
                <thead class='thead'>
                    <tr class='tr bg-tertiary'>
                        <th class='th h4' colspan='5' scope='colgroup'>Offline Case List</th>
                        <th class='th h4' colspan='2' scope='colgroup'>
                            <button type="button" class="d-none btn btn-primary btn-sm ${exit_button_class}" onclick="finish_online_processing_mode()" title="Clear offline processing mode and return to normal case listing" ${!allDocumentsSynced ? 'disabled' : ''}>
                                Exit Processing Mode
                            </button>
                        </th>
                    </tr>
                    <tr class='tr'>
                        <th class='th' scope='col'>Case Information</th>
                        <th class='th' scope='col'>Case Status</th>
                        <th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>
                        <th class='th' scope='col'>Created</th>
                        <th class='th' scope='col'>Last Updated</th>
                        <th class='th' scope='col'>Activity Status</th>
                        <th class='th' scope='col' style="width: 115px;">Actions</th>
                    </tr>
                </thead>
                <tbody class="tbody">
                    ${g_ui.process_offline_case_view_list_by_user.case_documents.map((item, i) => render_offline_processing_item(item, i)).join('')}
                </tbody>
            <tfoot class='tfoot'>
                ${g_ui.offline_ids_not_changed.length > 0 ? `<tr class='tr'>
                    <td class='td' colspan='7' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: center;'>
                        <p style='margin: 0; font-size: 17px; color: #6c757d;'>
                            *This offline case does not contain any changes. Cases with no changes are automatically unlocked after you go back Online, but remain listed in the table for reference. No further action required.
                        </p>                        
                    </td>
                </tr>` : ''}
                </tr>
                <tr class='tr'>
                    <td class='td' colspan='7' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: center;'>
                        <p style='margin: 0; font-size: 13px; color: #6c757d; font-style: italic;'>${localStorage.getItem("offline_session_id")}</p>
                    </td>
                </tr>                      
            </tfoot>        
            </table>
            </br>
            Online case listing will not be available until outstanding offline cases are brought back online.
            </br>Please resolve any cases from the Offline Cases list.
        `);
    }

    // code to render offline-only documents table (only shown when in offline mode)
    if(is_offline_mode_enabled && isOfflineMode === 'true'){
        if(!g_ui.offline_mode_case_view_list || g_ui.offline_mode_case_view_list == undefined)return"error";

        const newCaseCount =  g_ui.offline_mode_case_view_list ? g_ui.offline_mode_case_view_list.filter(doc => doc.rev == null).length : 0;
        const newCaseButtonDisabled = newCaseCount >= offline_mode_max_new_cases ? true : false;

        g_current_offline_documents = g_ui.offline_mode_case_view_list;
        // Build index map for offline case routing");
        g_offline_case_index_map = g_ui.offline_mode_case_view_list.map(doc => doc.id);
        // Make the index map globally accessible for navigation");
        window.g_offline_case_index_map = g_offline_case_index_map;
   
        if (!window.g_offline_tracking_initialized) {
            // g_offline_changes is already loaded from localStorage during initialization
            // Just initialize the original documents tracking
            if (typeof window.OfflineChangeTracker !== 'undefined' && window.OfflineChangeTracker.initialize) {
                window.OfflineChangeTracker.initialize(g_ui.offline_mode_case_view_list);
                window.g_offline_tracking_initialized = true;
            } else if (typeof initialize_offline_change_tracking === 'function') {
                initialize_offline_change_tracking(g_ui.offline_mode_case_view_list);
                window.g_offline_tracking_initialized = true;
            } else {
                offlineLog.warn('AppMMRIA', 'OfflineChangeTracker not available, skipping initialization');
            } 
        } 

        // Initialize network monitoring for Go Online button");
        if (typeof initialize_network_monitoring === 'function') {
            initialize_network_monitoring();
        }      
        p_result.push(`
            <table class="table mb-0">
                <thead class='thead'>
                    <tr class='tr bg-tertiary'>
                        <th class='th h4' colspan='7' scope='colgroup'>Offline Case List</th>
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
                    ${g_ui.offline_mode_case_view_list.length === 0 ?"<tr class='tr'><td class='td' colspan='7'><i>No cases to display</i></td></tr>":g_ui.offline_mode_case_view_list.map((item, i) => render_offline_only_document_item(item, i)).join('')}
                </tbody>
                <tfoot class='tfoot'> 
                    <tr class='tr'>
                        <td class='td' colspan='5' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6;'>
                            <ul style='margin: 0; padding-left: 20px; font-size: 13px; color: #6c757d; line-height: 1.4; font-style: italic;'>
                                <li style='margin-bottom: 4px;'>Up to 3 existing cases can be brought offline at once.</li>
                                <li style='margin-bottom: 4px;font-weight: ${newCaseButtonDisabled ? 'bold' :'normal'};'>Up to 3 new cases can be created offline.</li>    
                                <li style='margin-bottom: 4px;'>Once offline, you assume the risk of losing your data.</li>
                                <li style='margin-bottom: 4px;'>Please bring all cases back online regularly to ensure your data is saved to the system - for security reasons, cases that are offline for more than 30 days will be automatically deleted.</li>                                                                
                                <li style='margin-bottom: 4px;'>Only new cases created in offline mode can be deleted in offline mode.</li>                                
                            </ul>
                        </td>                    
                        <td class='td' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: right; vertical-align: middle;'>
                            ${isOfflineStatus === 'true' ? `
                                <button type="button" id="go-online-btn" class="btn btn-primary" onclick="show_go_online_modal(event)" style="line-height: 1.15;" title="Go back online and sync your changes">
                                    <img src="../img/online-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Offline">Go Online
                                </button>
                            ` : `
                                <button type="button" class="btn btn-primary" onclick="go_offline_clicked(event)" style="line-height: 1.15; ${g_offline_operation_in_progress ? 'opacity: 0.6; cursor: not-allowed;' : ''}" ${g_offline_operation_in_progress ? 'disabled' : ''}>
                                    <img src="../img/offline-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Offline">Go Offline
                                </button>
                            `}
                        </td>
                    </tr>
                    <tr class='tr'>
                        <td class='td' colspan='6' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: center;'>
                            <p style='margin: 0; font-size: 13px; color: #6c757d; font-style: italic;'>${offlineSessionId}</p>
                        </td>
                    </tr>
                </tfoot>
            </table>
        `);
    }

    if(is_offline_mode_enabled && isOfflineMode !== 'true' && isProcessingOfflineCases !== 'true'){
        const currentOfflineCount = g_ui.offline_case_view_list_by_user ? g_ui.offline_case_view_list_by_user.length : 0;
        const offline_button_disabled = currentOfflineCount >= offline_mode_max_existing_cases ? true: false;
        const hasOfflineCases = g_ui.offline_case_view_list_by_user && g_ui.offline_case_view_list_by_user.length > 0;

        if(!g_ui.offline_case_view_list_by_user)return "";
        p_result.push(`

            
            <table class="table mb-3">
                <thead class='thead'>
                    <tr class='tr bg-tertiary'>
                        <th class='th h4' colspan='7' scope='colgroup'>Cases Selected for Offline Work</th>
                    </tr>
                    <tr class='tr'>
                        <th class='th' scope='col'>Case Information</th>
                        <th class='th' scope='col'>Case Status</th>
                        <th class='th' scope='col'>Review Date (Projected Date, Actual Date)</th>
                        <th class='th' scope='col'>Created</th>
                        <th class='th' scope='col'>Last Updated</th>
                        <th class='th' scope='col'>Currently Edited By</th>
                        <th class='th' scope='col' style="width: 115px;">Actions</th>
                    </tr>
                </thead>
                <tbody class="tbody">
                ${g_ui.offline_case_view_list_by_user.length == 0 ?"<tr class='tr'><td class='td' colspan='7' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6; text-align: center;'>Select cases for offline work from the Case Listing table below</td></tr>":g_ui.offline_case_view_list_by_user.map((item, i) => render_offline_document_item(item, i)).join('')}
                    
                </tbody>
                <tfoot class='tfoot'>
                    <tr class='tr'>
                        <td class='td' colspan='7' style='padding: 16px 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6;'>
                            <div style='display: flex; justify-content: space-between; align-items: flex-start; gap: 20px;'>                        
                                <ul style='margin: 0; padding-left: 20px; font-size: 13px; color: #6c757d; line-height: 1.4; font-style: italic; flex: 1;'>
                                    <li style='margin-bottom: 4px;font-weight:${offline_button_disabled ? "bold" : "normal"}'>Up to 3 existing cases can be brought offline at once.</li>
                                    <li style='margin-bottom: 4px;'>Up to 3 new cases can be created offline.</li>
                                    <li style='margin-bottom: 4px;'>Once offline, you assume the risk of losing your data.</li>
                                    <li style='margin-bottom: 4px;'>Please bring all cases back online regularly to ensure your data is saved to the system - for security reasons, cases that are offline for more than 30 days will be automatically deleted.</li>
                                    <li style='margin-bottom: 4px;'>Navigating to another page will reset the list of cases selected for offline work.</li>
                                    
                                </ul>
                                <div style='flex-shrink: 0; display: flex; align-items: flex-start;'>
                                ${isOfflineStatus === 'true' ? `
                                    <button type="button" id="go-online-btn" class="btn btn-primary" onclick="go_online_clicked(event)" style="line-height: 1.15;" title="Go back online and sync your changes">
                                        <img src="../img/online-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Offline">Go Online
                                    </button>
                                ` : `
                                    <button type="button" class="btn btn-primary" onclick="go_offline_clicked(event)" style="line-height: 1.15; ${g_offline_operation_in_progress ? 'opacity: 0.6; cursor: not-allowed;' : ''}" ${g_offline_operation_in_progress ? 'disabled' : ''}>
                                        <img src="../img/offline-go.svg" style="width: 14px; height: 14px; margin-right: 8px; vertical-align: middle;" alt="Go Offline">Go Offline
                                    </button>
                                `}     
                                </div>                      
                            </div>                      
                        </td>                    
                    </tr>
                </tfoot>            
            </table>
        `);
     }

    // Only show case listing table and pagination if not in offline mode and not processing offline cases
    
    
    
    if (isOfflineMode !== 'true' && isProcessingOfflineCases !== 'true') {
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
        offlineLog.log('AppMMRIA', 'In offline mode - skipping clear_case_search API call');
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

// Expose app_is_item_pinned globally for use by index.js
window.app_is_item_pinned = app_is_item_pinned;

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
    
    // Determine offline status display based on offline_lock_type
    let offline_html = '';
    if (item.value.offline_lock_type === 2) {
        // Hard lock - actually offline
        offline_html = ` Offline - ${item.value.offline_by}`;
    } else if (item.value.offline_lock_type === 1) {
        // Soft lock - added to offline queue
        offline_html = ` Offline Queue - ${item.value.offline_by}`;
    }

    // Check if case is offline by another user
    let is_offline_by_other_user = item.value.is_offline === true && item.value.offline_by && 
        item.value.offline_by !== g_user_name;

    if(case_is_locked || g_is_data_analyst_mode || is_offline_by_other_user)
    {
        // checked_out_html = ' [ read only ] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else if(!is_checked_out && !is_checked_out_expired(item.value))
    {
        // checked_out_html = ` [checked out by ${item.value.last_checked_out_by}] `;
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    // If is_checked_out is true (current user has it checked out) or case is available,
    // then buttons should be enabled (delete_enabled_html stays empty)

    // Check if offline case limit is reached or if an operation is in progress
    const currentOfflineCount = g_ui.offline_case_view_list_by_user ? g_ui.offline_case_view_list_by_user.length : 0;
    const offline_button_disabled = (currentOfflineCount >= offline_mode_max_existing_cases) || g_offline_operation_in_progress;
    const offline_button_disabled_attr = offline_button_disabled ? 'disabled="disabled"' : '';
    const offline_button_class = g_offline_operation_in_progress || offline_button_disabled ? 'offline-processing-disabled' : '';

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
            ${offline_html ? `            <span class="row no-gutters align-items-center">
                <span class="icn icn--round icn--border bg-primary" title="${offline_html}"><span class="d-flex x14 fill-w cdc-icon-lock-alt"></span></span>
                <span class="icn-info">${offline_html}</span>
            </span>` : ''}
        </td>
        ${!g_is_data_analyst_mode ? (
            `<td class="td">       
                <div>
                    <button type="button" id="id_for_record_${i}" class="btn btn-primary" onclick="init_delete_dialog(${i})" style="line-height: 1.15; margin-right: 8px;" ${delete_enabled_html}>Delete</button>${render_pin_un_pin_button(item, is_checked_out, is_checked_out_expired(item.value), delete_enabled_html)}
                </div>

                ${(is_offline_mode_enabled && item.value.is_offline !== true) ? `
                <div style="margin-top: 8px;">
                    <button type="button" id="offline_toggle_${i}" class="btn btn-primary ${offline_button_class}" 
                        onclick="add_offline_mode_softlock('${caseID}', ${i})" 
                        style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" 
                        ${delete_enabled_html}
                        ${offline_button_disabled_attr}
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
    
    
    // Determine offline status display based on offline_lock_type
    let offline_html = '';
    if (item.value.offline_lock_type === 2) {
        // Hard lock - actually offline
        offline_html = ` Offline - ${item.value.offline_by}`;
    } else if (item.value.offline_lock_type === 1) {
        // Soft lock - added to offline queue
        offline_html = ` Offline Queue - ${item.value.offline_by}`;
    }

    // Check if case is offline by another user
    let is_offline_by_other_user = item.value.is_offline === true && item.value.offline_by && 
        item.value.offline_by !== g_user_name;

    if(case_is_locked || g_is_data_analyst_mode || is_offline_by_other_user)
    {
        // checked_out_html = ' [ read only ] ';
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    else if(!is_checked_out && !is_checked_out_expired(item.value))
    {
        // checked_out_html = ` [checked out by ${item.value.last_checked_out_by}] `;
        checked_out_html = '';
        delete_enabled_html = ' disabled = "disabled" ';
    }
    // If is_checked_out is true (current user has it checked out) or case is available,
    // then buttons should be enabled (delete_enabled_html stays empty)

    // Check if offline case limit is reached or if an operation is in progress
    const currentOfflineCount = g_ui.offline_case_view_list_by_user ? g_ui.offline_case_view_list_by_user.length : 0;
    const offline_button_disabled = (currentOfflineCount >= offline_mode_max_existing_cases) || g_offline_operation_in_progress;
    const offline_button_disabled_attr = offline_button_disabled ? 'disabled="disabled"' : '';
    const offline_button_class = g_offline_operation_in_progress || offline_button_disabled ? 'offline-processing-disabled' : '';
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
            ${offline_html ? `            <span class="row no-gutters align-items-center">
                <span class="icn icn--round icn--border bg-primary" title="${offline_html}"><span class="d-flex x14 fill-w cdc-icon-lock-alt"></span></span>
                <span class="icn-info">${offline_html}</span>
            </span>` : ''}
        </td>
        ${!g_is_data_analyst_mode ? (
            `<td class="td" ${border_bottom_color}>
                <div>
                    <button type="button" id="id_for_record_${i}" class="btn btn-primary" onclick="init_delete_dialog(${i})" style="line-height: 1.15; margin-right: 8px;" ${delete_enabled_html}>Delete</button>${render_pin_un_pin_button(item, is_checked_out, is_checked_out_expired(item.value), delete_enabled_html)}
                </div>

                ${(is_offline_mode_enabled && item.value.is_offline !== true) ? `
                <div style="margin-top: 8px;">
                    <button type="button" id="offline_toggle_${i}" class="btn btn-primary ${offline_button_class}" 
                        onclick="add_offline_mode_softlock('${caseID}', ${i})" 
                        style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;" 
                        ${delete_enabled_html}
                        ${offline_button_disabled_attr}
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

