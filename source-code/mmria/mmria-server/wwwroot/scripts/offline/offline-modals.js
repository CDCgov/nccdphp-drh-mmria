/**
 * Offline Modals Module
 * Manages modal dialogs for offline mode operations
 */

// Function to show revision mismatch modal
function show_revision_mismatch_modal(documentId, originalDocument, serverDocument, modifiedDocument) {
    const modalHtml = `
        <div id="revision-mismatch-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #dc3545; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Revision Conflict Detected</h4>
                        <button type="button" class="close" onclick="window.OfflineModals.closeRevisionMismatch()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 30px;">
                        <p style="font-size: 16px; margin-bottom: 25px; color: #333;">
                            The document "${documentId}" has been modified on the server since you started working offline.
                        </p>
                        <p style="font-size: 14px; margin-bottom: 20px; color: #666;">
                            Original revision: <strong>${originalDocument._rev || 'unknown'}</strong><br>
                            Server revision: <strong>${serverDocument._rev || 'unknown'}</strong>
                        </p>
                        <p style="font-size: 14px; color: #666;">
                            Please manually resolve this conflict by reviewing both versions.
                        </p>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-light" onclick="window.OfflineModals.closeRevisionMismatch()" style="margin-right: 10px; padding: 8px 20px;">
                            Close
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="revision-mismatch-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
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

// Function to close revision mismatch modal
function close_revision_mismatch_modal() {
    const modal = document.getElementById('revision-mismatch-modal');
    const backdrop = document.getElementById('revision-mismatch-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) modal.parentNode.removeChild(modal);
            if (backdrop.parentNode) backdrop.parentNode.removeChild(backdrop);
        }, 150);
    }
}

// Function to show case already offline modal
function show_case_already_offline_modal(caseId) {
    const modalHtml = `
        <div id="case-already-offline-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #ffc107; color: #333; padding: 7px;">
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
                    <div class="modal-header" style="background-color: #ffc107; color: #333; padding: 7px;">
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

// Function to show abandon case modal
function show_abandon_case_modal(caseId) {
    const modalHtml = `
        <div id="abandon-case-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #dc3545; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Abandon Case Changes</h4>
                        <button type="button" class="close" onclick="window.OfflineModals.closeAbandonCase()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 30px;">
                        <p style="font-size: 16px; margin-bottom: 20px; color: #333;">
                            Are you sure you want to abandon all changes for this case?
                        </p>
                        <p style="font-size: 14px; color: #dc3545; font-weight: bold;">
                            This action cannot be undone.
                        </p>
                    </div>
                    <div class="modal-footer" style="padding: 20px 30px; text-align: right;">
                        <button type="button" class="btn btn-light" onclick="window.OfflineModals.closeAbandonCase()" style="margin-right: 10px; padding: 8px 20px;">
                            Cancel
                        </button>
                        <button type="button" class="btn btn-danger" onclick="window.OfflineModals.confirmAbandon('${caseId}')" style="padding: 8px 20px;">
                            Abandon Changes
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="abandon-case-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
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

// Function to close abandon case modal
function close_abandon_case_modal() {
    const modal = document.getElementById('abandon-case-modal');
    const backdrop = document.getElementById('abandon-case-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) modal.parentNode.removeChild(modal);
            if (backdrop.parentNode) backdrop.parentNode.removeChild(backdrop);
        }, 150);
    }
}

// Function to confirm abandon case
async function confirm_abandon_case(caseId) {
    console.log('Confirming abandon for case:', caseId);
    
    try {
        // Close the modal
        close_abandon_case_modal();
        
        // Delete the offline changes
        await window.OfflineSyncManager.deleteChanges(caseId);
        
        // Refresh the UI
        if (typeof refresh_offline_documents_list === 'function') {
            await refresh_offline_documents_list();
        }
        
    } catch (error) {
        console.error('Error abandoning case:', error);
        show_message(`Error abandoning case: ${error.message}`, 'error');
    }
}

// Function for offline mode abandon offline changes (called from button)
function offline_mode_abandon_offline_changes() {
    console.log('Offline mode abandon offline changes button clicked');
    
    // Show confirmation dialog
    const confirmed = confirm('Are you sure you want to abandon all offline changes? This cannot be undone.');
    
    if (confirmed) {
        window.OfflineSyncManager.abandon();
    }
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
    confirmAbandon: confirm_abandon_case,
    abandonOfflineChanges: offline_mode_abandon_offline_changes,
    hideOnlineElements: hideOnlineCaseListingElements,
    showOnlineElements: showOnlineCaseListingElements
};

console.log('Offline Modals module loaded');
