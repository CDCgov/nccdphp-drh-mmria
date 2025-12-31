/**
 * Offline Modals Module
 * Manages modal dialogs for offline mode operations
 */

// Function to show revision mismatch modal
function show_revision_mismatch_modal(caseID) {
    // Create modal HTML
    const modalHtml = `
        <div id="revision-mismatch-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                     <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Case Upload Failed</h4>
                        <button type="button" class="close" onclick="close_revision_mismatch_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 10px;">
                        
                        <ul style="list-style: none; padding-left: 10px; margin-bottom: 30px;">
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                <strong>This case was unlocked by an administrator while you were offline.</strong>
                            </li>
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                Your changes cannot be uploaded and have been abandoned to prevent data conflicts.
                            </li>
                        </ul>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-primary" onclick="close_revision_mismatch_modal()" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">
                            OK
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="revision-mismatch-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('revision-mismatch-modal');
        const backdrop = document.getElementById('revision-mismatch-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}
function close_revision_mismatch_modal() {
    const modal = document.getElementById('revision-mismatch-modal');
    const backdrop = document.getElementById('revision-mismatch-backdrop');
    
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
        }, 300);
    }
}

// Function to show case already offline modal
function show_case_already_offline_modal(caseId) {
    const modalHtml = `
        <div id="case-already-offline-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Case Already Offline</h4>
                        <button type="button" class="close" onclick="window.OfflineModals.closeCaseAlreadyOffline()" style="color: #333; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 30px;">
                        <p style="font-size: 16px; color: #333;">
                            This case is already marked for offline work.
                        </p>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-primary" onclick="window.OfflineModals.closeCaseAlreadyOffline()" style="padding: 8px 20px;">
                            OK
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="case-already-offline-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    setTimeout(() => {
        const modal = document.getElementById('case-already-offline-modal');
        const backdrop = document.getElementById('case-already-offline-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

// Function to close case already offline modal
function close_case_already_offline_modal() {
    const modal = document.getElementById('case-already-offline-modal');
    const backdrop = document.getElementById('case-already-offline-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) modal.parentNode.removeChild(modal);
            if (backdrop.parentNode) backdrop.parentNode.removeChild(backdrop);
        }, 150);
    }
}

// Function to show case already online modal
function show_case_already_online_modal(caseId) {
    const modalHtml = `
        <div id="case-already-online-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog" role="document">
                <div class="modal-content">
                   <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Case Already Online</h4>
                        <button type="button" class="close" onclick="window.OfflineModals.closeCaseAlreadyOnline()" style="color: #333; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 30px;">
                        <p style="font-size: 16px; color: #333;">
                            This case is already online.
                        </p>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-primary" onclick="window.OfflineModals.closeCaseAlreadyOnline()" style="padding: 8px 20px;">
                            OK
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="case-already-online-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    setTimeout(() => {
        const modal = document.getElementById('case-already-online-modal');
        const backdrop = document.getElementById('case-already-online-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

// Function to close case already online modal
function close_case_already_online_modal() {
    const modal = document.getElementById('case-already-online-modal');
    const backdrop = document.getElementById('case-already-online-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) modal.parentNode.removeChild(modal);
            if (backdrop.parentNode) backdrop.parentNode.removeChild(backdrop);
        }, 150);
    }
}

// Function to show go online modal
function show_go_online_modal() {
    const modalHtml = `
        <div id="go-online-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Go Online</h4>
                        <button type="button" class="close" onclick="window.OfflineModals.closeGoOnline()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 30px;">
                        <p style="font-size: 16px; margin-bottom: 20px; color: #333;">
                            Are you ready to go back online and sync your offline changes?
                        </p>
                        <p style="font-size: 14px; color: #666;">
                            This will sync all your offline changes to the server.
                        </p>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-light" onclick="window.OfflineModals.closeGoOnline()" style="margin-right: 10px; padding: 8px 20px;">
                            Cancel
                        </button>
                        <button type="button" class="btn btn-primary" onclick="go_online_clicked(event)" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">
                            Go Online
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="go-online-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    setTimeout(() => {
        const modal = document.getElementById('go-online-modal');
        const backdrop = document.getElementById('go-online-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

// Function to close go online modal
function close_go_online_modal() {
    const modal = document.getElementById('go-online-modal');
    const backdrop = document.getElementById('go-online-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) modal.parentNode.removeChild(modal);
            if (backdrop.parentNode) backdrop.parentNode.removeChild(backdrop);
        }, 150);
    }
}

// Function to show abandon changes processing modal (for processing mode)
function show_abandon_changes_processing_modal(caseID, syncState) {
    // Create modal HTML
    const modalHtml = `
        <div id="abandon-changes-processing-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Abandon Case</h4>
                        <button type="button" class="close" onclick="close_abandon_changes_processing_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 30px;">
                        <p style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                            Are you sure you want to abandon this case?
                        </p>
                        <p style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                            If this case was created in offline mode, it will be deleted.<br/>
                            If this case was edited in offline mode, changes will be removed.
                        </p>
                        <p style="margin-bottom: 0; font-size: 17px; line-height: 1.5;">
                            This action cannot be undone.
                        </p>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-light" onclick="close_abandon_changes_processing_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Cancel
                        </button>
                        <button type="button" class="btn btn-primary" onclick="confirm_abandon_changes_processing('${caseID}', ${syncState})" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">
                            Abandon Case
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="abandon-changes-processing-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('abandon-changes-processing-modal');
        const backdrop = document.getElementById('abandon-changes-processing-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

function close_abandon_changes_processing_modal(skipRefresh = false) {
    const modal = document.getElementById('abandon-changes-processing-modal');
    const backdrop = document.getElementById('abandon-changes-processing-backdrop');
    
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
    
    // Reset the processing flag and refresh the list only when canceling (not confirming)
    // When confirming, the operation itself will handle the refresh after completion

}

// Function to confirm abandon changes in processing mode
async function confirm_abandon_changes_processing(caseID, syncState) {
    try {
        console.log('🗑️ Abandoning changes in processing mode:', caseID);
            
        // Set global flag and disablehandle_abandon_changes_click all processing buttons
        g_processing_operation_in_progress = true;
        disable_all_processing_buttons();
        
        // Close the modal without refreshing (skipRefresh=true)
        // The operation will handle the refresh after completion
        close_abandon_changes_processing_modal(true);
        
        // Call the backend function to abandon offline changes
        if (typeof window.OfflineSyncManager !== 'undefined' && window.OfflineSyncManager.abandon) {
            await window.OfflineSyncManager.abandon(caseID, syncState);
        } else if (typeof abandon_offline_changes === 'function') {
            await abandon_offline_changes(caseID, syncState);
        } else {
            console.error('Abandon offline changes function not available');
            alert('Error: Unable to abandon changes. Please refresh the page and try again.');
        }
    } catch (error) {
        console.error('Error abandoning changes:', error);
        alert('Error abandoning changes: ' + error.message);
    }
}

// Function to show delete changes processing modal (for processing mode)
function show_delete_changes_processing_modal(caseID) {
    // Create modal HTML
    const modalHtml = `
        <div id="delete-changes-processing-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Abandon Case</h4>
                        <button type="button" class="close" onclick="close_delete_changes_processing_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 30px;">
                        <p style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                            Are you sure you want to abandon this case?
                        </p>
                        <p style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                            If this case was created in offline mode, it will be deleted.<br/>
                            If this case was edited in offline mode, changes will be removed.
                        </p>
                        <p style="margin-bottom: 0; font-size: 17px; line-height: 1.5;">
                            This action cannot be undone.
                        </p>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-light" onclick="close_delete_changes_processing_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Cancel
                        </button>
                        <button type="button" class="btn btn-primary" onclick="confirm_delete_changes_processing('${caseID}')" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">
                            Abandon Case
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="delete-changes-processing-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('delete-changes-processing-modal');
        const backdrop = document.getElementById('delete-changes-processing-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

function close_delete_changes_processing_modal(skipRefresh = false) {
    const modal = document.getElementById('delete-changes-processing-modal');
    const backdrop = document.getElementById('delete-changes-processing-backdrop');
    
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

// Function to confirm delete changes in processing mode
async function confirm_delete_changes_processing(caseID) {
    try {
        console.log('🗑️ Deleting changes in processing mode:', caseID);
        // Set global flag and disable all processing buttons
        g_processing_operation_in_progress = true;
        disable_all_processing_buttons();        

        // Close the modal without refreshing (skipRefresh=true)
        // The operation will handle the refresh after completion
        close_delete_changes_processing_modal(true);
        
        // Call the backend function to delete offline changes
        if (typeof window.OfflineSyncManager !== 'undefined' && window.OfflineSyncManager.delete) {
            await window.OfflineSyncManager.delete(caseID);
        } else if (typeof delete_offline_changes === 'function') {
            await delete_offline_changes(caseID);
        } else {
            console.error('Delete offline changes function not available');
            alert('Error: Unable to delete changes. Please refresh the page and try again.');
        }
    } catch (error) {
        console.error('Error deleting changes:', error);
        alert('Error deleting changes: ' + error.message);
    }
}

// Function to show abandon case modal
function show_abandon_case_modal(caseID) {
    // Create modal HTML
    const modalHtml = `
        <div id="abandon-case-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Confirm Abandon Case</h4>
                        <button type="button" class="close" onclick="close_abandon_case_modal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 10px;">
                        <ul style="list-style: none; padding-left: 10px; margin-bottom: 30px;">
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                Are you sure you want to abandon this case?
                            </li>
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                If this case was created in offline mode, it will be deleted.<br/>
                                If this case was edited in offline mode, changes will be removed.
                            </li>                            
                            <li style="margin-bottom: 15px; font-size: 17px; line-height: 1.5;">
                                This action cannot be undone.
                            </li>
                        </ul>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-light" onclick="close_abandon_case_modal()" style="margin-right: 10px; padding: 8px 20px;">
                            Cancel
                        </button>
                        <button type="button" class="btn btn-primary" onclick="confirm_abandon_case('${caseID}')" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">
                            Abandon Case
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="abandon-case-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal with fade effect
    setTimeout(() => {
        const modal = document.getElementById('abandon-case-modal');
        const backdrop = document.getElementById('abandon-case-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

function close_abandon_case_modal() {
    const modal = document.getElementById('abandon-case-modal');
    const backdrop = document.getElementById('abandon-case-backdrop');
    
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

// Function to confirm abandon case
async function confirm_abandon_case(caseID) {
    try {
        console.log('🗑️ Abandoning offline case:', caseID);
        
        // Close the modal first
        close_abandon_case_modal();
        
        // Remove from Service Worker cache
        if ('caches' in window) {
            try {
                const cacheName = await getActualApiCacheName();
                const cache = await caches.open(cacheName);
                const caseUrl = `${window.location.origin}/api/case?case_id=${caseID}`;
                const deleted = await cache.delete(caseUrl);
                if (deleted) {
                    console.log('✅ Removed case from cache:', cacheName);
                }
            } catch (cacheError) {
                console.error('Error removing case from cache:', cacheError);
            }
        }
        
        // Remove from g_ui.offline_mode_case_view_list
        if (g_ui.offline_mode_case_view_list && Array.isArray(g_ui.offline_mode_case_view_list)) {
            const originalLength = g_ui.offline_mode_case_view_list.length;
            g_ui.offline_mode_case_view_list = g_ui.offline_mode_case_view_list.filter(item => item.id !== caseID);
            console.log('✅ Removed case from offline_mode_case_view_list. Before:', originalLength, 'After:', g_ui.offline_mode_case_view_list.length);
            
            // Rebuild the offline case index map from the updated list
            g_offline_case_index_map = g_ui.offline_mode_case_view_list.map(doc => doc.id);
            window.g_offline_case_index_map = g_offline_case_index_map;
            console.log('✅ Updated offline case index map. New length:', g_offline_case_index_map.length);
        }
        
        // Clear from g_offline_changes Map
        if (g_offline_changes && g_offline_changes.has(caseID)) {
            g_offline_changes.delete(caseID);
            console.log('✅ Removed case from offline changes tracking');
        }
        
        // Clear from g_original_offline_documents Map
        if (g_original_offline_documents && g_original_offline_documents.has(caseID)) {
            g_original_offline_documents.delete(caseID);
            console.log('✅ Removed case from original offline documents tracking');
        }
        
        // Persist changes to localStorage
        save_offline_changes_to_storage();
        
        // Refresh the case list table
        console.log('🔄 Refreshing offline case list table...');
        if (typeof get_case_set === 'function') {
            await get_case_set();
        } else {
            console.warn('get_case_set function not available, page may need manual refresh');
        }
        
        console.log('✅ Successfully abandoned offline case:', caseID);
        
    } catch (error) {
        console.error('❌ Error abandoning offline case:', error);
    }
}

// Function for offline mode abandon offline changes (called from button)
function offline_mode_abandon_offline_changes(caseID) {
    show_abandon_case_modal(caseID);
}

// Function to hide online case listing elements
function hideOnlineCaseListingElements() {
    console.log('Hiding online case listing elements...');
    
    // Hide case listing table
    const caseListingTable = document.querySelector('.case-listing-table');
    if (caseListingTable) {
        caseListingTable.style.display = 'none';
    }
    
    // Hide filters
    const filtersContainer = document.querySelector('.filters-container');
    if (filtersContainer) {
        filtersContainer.style.display = 'none';
    }
    
    // Hide pagination
    const paginationContainer = document.querySelector('.pagination-container');
    if (paginationContainer) {
        paginationContainer.style.display = 'none';
    }
    
    console.log('Online case listing elements hidden');
}

// Function to show online case listing elements
function showOnlineCaseListingElements() {
    console.log('Showing online case listing elements...');
    
    // Show case listing table
    const caseListingTable = document.querySelector('.case-listing-table');
    if (caseListingTable) {
        caseListingTable.style.display = '';
    }
    
    // Show filters
    const filtersContainer = document.querySelector('.filters-container');
    if (filtersContainer) {
        filtersContainer.style.display = '';
    }
    
    // Show pagination
    const paginationContainer = document.querySelector('.pagination-container');
    if (paginationContainer) {
        paginationContainer.style.display = '';
    }
    
    console.log('Online case listing elements shown');
}

// Function to show invalid offline state recovery modal
function show_invalid_offline_state_recovery_modal() {
    const modalHtml = `
        <div id="invalid-offline-state-recovery-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Offline Setup Interrupted</h4>
                    </div>
                    <div class="modal-body" style="padding: 30px; text-align: center;">
                        <p style="font-size: 17px; margin-bottom: 20px; color: #333;">
                            Your offline mode setup was interrupted.
                        </p>
                        <p style="font-size: 17px; margin-bottom: 20px; color: #333;">
                            The system is automatically cleaning up and will reload the page once complete.
                        </p>
                        <p style="font-size: 15px; color: #666; font-style: italic;">
                            Please wait...
                        </p>
                    </div>
                </div>
            </div>
        </div>
        <div id="invalid-offline-state-recovery-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    setTimeout(() => {
        const modal = document.getElementById('invalid-offline-state-recovery-modal');
        const backdrop = document.getElementById('invalid-offline-state-recovery-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
        
        // Automatically trigger recovery after showing the modal
        if (window.OfflineTransitionManager && window.OfflineTransitionManager.confirmInvalidOfflineStateRecovery) {
            setTimeout(() => {
                window.OfflineTransitionManager.confirmInvalidOfflineStateRecovery();
            }, 500);
        }
    }, 10);
}

// Function to close invalid offline state recovery modal
function close_invalid_offline_state_recovery_modal() {
    const modal = document.getElementById('invalid-offline-state-recovery-modal');
    const backdrop = document.getElementById('invalid-offline-state-recovery-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) modal.parentNode.removeChild(modal);
            if (backdrop.parentNode) backdrop.parentNode.removeChild(backdrop);
        }, 150);
    }
}

// Expose the offline modals API to the global scope
window.OfflineModals = {
    showRevisionMismatch: show_revision_mismatch_modal,
    closeRevisionMismatch: close_revision_mismatch_modal,
    showCaseAlreadyOffline: show_case_already_offline_modal,
    closeCaseAlreadyOffline: close_case_already_offline_modal,
    showCaseAlreadyOnline: show_case_already_online_modal,
    closeCaseAlreadyOnline: close_case_already_online_modal,
    showGoOnline: show_go_online_modal,
    closeGoOnline: close_go_online_modal,
    showAbandonCase: show_abandon_case_modal,
    closeAbandonCase: close_abandon_case_modal,
    confirmAbandonCase: confirm_abandon_case,
    showAbandonChangesProcessing: show_abandon_changes_processing_modal,
    closeAbandonChangesProcessing: close_abandon_changes_processing_modal,
    confirmAbandonChangesProcessing: confirm_abandon_changes_processing,
    showDeleteChangesProcessing: show_delete_changes_processing_modal,
    closeDeleteChangesProcessing: close_delete_changes_processing_modal,
    confirmDeleteChangesProcessing: confirm_delete_changes_processing,
    abandonOfflineChanges: offline_mode_abandon_offline_changes,
    hideOnlineElements: hideOnlineCaseListingElements,
    showOnlineElements: showOnlineCaseListingElements,
    showInvalidOfflineStateRecovery: show_invalid_offline_state_recovery_modal,
    closeInvalidOfflineStateRecovery: close_invalid_offline_state_recovery_modal
};

console.log('Offline Modals module loaded');
