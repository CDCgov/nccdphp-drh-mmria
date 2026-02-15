/**
 * Offline Transition Manager Module
 * Manages transitions between online and offline modes
 */

// Timer for debounced key validation
let validation_timer = null;

// Retry counter and max retries for offline transition
let g_offline_transition_retry_count = 0;
const MAX_OFFLINE_TRANSITION_RETRIES = 3;

// Persistent keep-alive interval for service worker (prevents SW termination during offline mode)
let g_service_worker_keep_alive_interval = null;

// Function for Go Offline button click handler
function go_offline_button_clicked(event) {
    // Prevent any default behavior and stop event propagation
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }
    
    // Check if button is disabled (no cases selected)
    const button = event.target.closest('button');
    if (button && button.disabled) {
        offlineLog.log('OfflineTransitionManager', 'Go Offline button clicked but disabled - no cases selected');
        return;
    }
    
    offlineLog.log('OfflineTransitionManager', 'Go Offline button clicked - showing modal');
    show_go_offline_modal();
}

async function sync_log_data() {
    // Sync logs to server before transitioning back online
    offlineLog.log('OfflineTransitionManager', 'Syncing logs to server...');
    try {
        const syncResult = await offlineLog.syncToServer();
        if (syncResult.success) {
            offlineLog.log('OfflineTransitionManager', `Successfully synced ${syncResult.synced} logs to server`);           
        } else {
            offlineLog.warn('OfflineTransitionManager', 'Log sync failed:', syncResult.message);
        }
    } catch (syncError) {
        offlineLog.error('OfflineTransitionManager', 'Error syncing logs:', syncError);
    }
}

// Function for Go Online button
async function go_online_clicked(event) {
    // Prevent any default behavior and stop event propagation
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }
    

    //hide modal
    if (window.OfflineModals) {
        window.OfflineModals.closeGoOnline();
    }

    // First check if we have network connectivity
    const isConnected = await window.OfflineNetworkMonitor.check();
    if (!isConnected) {
        offlineLog.error('OfflineTransitionManager', 'Go Online blocked - no network connectivity. Cannot go online - no network connection detected. Please check your internet connection and try again.');
        return;
    }
  
      
    // Disable the button to prevent multiple clicks
    const goOnlineButton = document.getElementById('go-online-btn');
    if (goOnlineButton) {
        goOnlineButton.disabled = true;
        goOnlineButton.style.opacity = '0.6';
        const buttonText = goOnlineButton.querySelector('.button-text');
        if (buttonText) {
            buttonText.textContent = 'Going Online...';
        }
    }
    
    // Add a delay to ensure we can see the console logs
    await new Promise(resolve => setTimeout(resolve, 100));
    
    try {
        //add modal while going online
        show_moving_to_online_modal();

        // DIAGNOSTIC LOGGING: Capture state at moment of "Go Online" click
        // This helps diagnose post-restart issues
        offlineLog.log('OfflineTransitionManager', '=== Go Online Diagnostic Info ===');
        offlineLog.log('OfflineTransitionManager', `Timestamp: ${new Date().toISOString()}`);
        
        // Log localStorage state
        const offlineSession = localStorage.getItem('mmria_offline_session');
        const offlineSessionId = localStorage.getItem('offline_session_id');
        const isOffline = localStorage.getItem('is_offline');
        const hasActiveSession = localStorage.getItem('has_active_offline_session');
        const processOfflineCases = localStorage.getItem('process_offline_cases');
        
        offlineLog.log('OfflineTransitionManager', 'localStorage state:', {
            mmria_offline_session: offlineSession ? `Present (length: ${offlineSession.length})` : 'Not found',
            offline_session_id: offlineSessionId || 'Not found',
            is_offline: isOffline || 'Not found',
            has_active_offline_session: hasActiveSession || 'Not found',
            process_offline_cases: processOfflineCases || 'Not found'
        });
        
        // Parse and log session data details
        if (offlineSession) {
            try {
                const sessionData = JSON.parse(offlineSession);
                offlineLog.log('OfflineTransitionManager', 'Parsed session data:', {
                    sessionId: sessionData.sessionId || sessionData.offlineSessionId || 'Not found',
                    offlineIds_count: sessionData.offlineIds?.length || sessionData.offline_ids?.length || 0,
                    offlineIds: sessionData.offlineIds || sessionData.offline_ids || []
                });
            } catch (e) {
                offlineLog.error('OfflineTransitionManager', 'Failed to parse mmria_offline_session:', e);
            }
        }
        
        // Log service worker state
        if ('serviceWorker' in navigator) {
            const swRegistration = await navigator.serviceWorker.getRegistration();
            const hasController = !!navigator.serviceWorker.controller;
            offlineLog.log('OfflineTransitionManager', 'Service Worker state:', {
                registration_active: !!swRegistration,
                has_controller: hasController,
                controller_state: navigator.serviceWorker.controller?.state || 'No controller'
            });
        } else {
            offlineLog.warn('OfflineTransitionManager', 'Service Worker API not available');
        }
        
        // Log g_offline_changes Map state
        if (window.OfflineChangeTracker) {
            const allChanges = window.OfflineChangeTracker.getAll();
            offlineLog.log('OfflineTransitionManager', `g_offline_changes Map has ${allChanges.length} tracked changes`);
            if (allChanges.length > 0) {
                offlineLog.log('OfflineTransitionManager', 'Changed case IDs:', allChanges.map(c => c.documentId));
            }
        } else {
            offlineLog.warn('OfflineTransitionManager', 'OfflineChangeTracker not available');
        }
        
        offlineLog.log('OfflineTransitionManager', '=== End Diagnostic Info ===');

        
 
        offlineLog.log('OfflineTransitionManager', 'Saving cached cases to database...');
        // CRITICAL: Save cases to database BEFORE clearing offline status
        // This must succeed before we proceed
        const saveResult = await window.OfflineSyncManager.saveCasesToDatabase();
        
        if (!saveResult.shouldSetProcessOffline) {
            await sync_log_data();
            close_moving_to_online_modal();
            offlineLog.error('OfflineTransitionManager','Failed to save cached cases to database - saveCasesToDatabase returned false');
            alert(`Error transitioning to online mode: Please try again.`);
            window.location.reload();           
            return;           
        }
        else {     
      
            // Stop the continuous service worker keep-alive
            if (g_service_worker_keep_alive_interval) {
                clearInterval(g_service_worker_keep_alive_interval);
                g_service_worker_keep_alive_interval = null;
                offlineLog.log('OfflineTransitionManager', 'Stopped continuous service worker keep-alive');
            }            

            //set local storage item to indicate we just went online
            localStorage.setItem('process_offline_cases', true);

            // Immediately set service worker to online mode for faster transition
            if (window.ServiceWorkerManager) {
                window.ServiceWorkerManager.setOnlineImmediately();
            }
            
            offlineLog.log('OfflineTransitionManager', 'Transitioning service worker to online mode...');
                    
            // IMPORTANT: Clear offline status FIRST so service worker allows API calls through
            localStorage.removeItem('is_offline');
            localStorage.removeItem('has_active_offline_session');

            // Give service worker a moment to process the status change
            await new Promise(resolve => setTimeout(resolve, 200)); // Increased slightly for safety
        

            // Clear service worker caches BEFORE unregistering (while it's still active)
            if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
                navigator.serviceWorker.controller.postMessage({ type: 'CLEAR_CACHES' });
                await new Promise(resolve => setTimeout(resolve, 500)); // Wait for it to process
            }

            // Unregister service worker
            offlineLog.log('OfflineTransitionManager', 'Unregistering service worker...');
            await unregister_service_worker();
            
            // Clear offline session
            offlineLog.log('OfflineTransitionManager', 'Clearing offline session...');
            if (window.offlineSessionManager) {
                window.offlineSessionManager.clear();
            }       

            // Clear all cached data
            await clear_all_cached_data();
            
            // Clear remaining offline session data
            localStorage.removeItem('mmria_offline_session');
            localStorage.removeItem('mmria_cached_cases');
            localStorage.removeItem('mmria_offline_changes');
            
            // Remove offline mode indicator from body
            document.body.classList.remove('mmria-offline-mode');
            
            // Add a longer delay before page reload to ensure API call completes
            await new Promise(resolve => setTimeout(resolve, 2000));        

            // Refresh the page to fully return to online mode
            offlineLog.log('OfflineTransitionManager', 'Returning to online mode - refreshing page');
            await sync_log_data();
            window.location.href = '/account/auto-login';
        
          }
    } catch (error) {
        offlineLog.error('OfflineTransitionManager', 'Error transitioning to online mode:', error);
        alert(`Error transitioning to online mode: Please try again.`);
        window.location.reload();
        close_moving_to_online_modal();

         await sync_log_data();

        // Re-enable the button if there was an error
        const goOnlineButton = document.getElementById('go-online-btn');
        if (goOnlineButton) {
            goOnlineButton.disabled = false;
            goOnlineButton.style.opacity = '1';
            const buttonText = goOnlineButton.querySelector('.button-text');
            if (buttonText) {
                buttonText.textContent = 'Go Online';
            }
        }
        
        // Don't reload the page if there was an error - this allows debugging
        return false;
    }
}

// Function to show the Go Offline modal
function show_go_offline_modal() {
    const modalHtml = `
        <div id="go-offline-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Go Offline</h4>
                        <button type="button" class="close" onclick="window.OfflineTransitionManager.closeGoOfflineModal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
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
                        <button type="button" class="btn btn-light" onclick="window.OfflineTransitionManager.closeGoOfflineModal()" style="margin-right: 10px; padding: 8px 20px;">
                            Cancel
                        </button>
                        <button type="button" class="btn btn-primary" onclick="window.OfflineTransitionManager.continueToSetKey()" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">
                            Continue to set key
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="go-offline-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
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

// Function for Continue to set key button
function continue_to_set_key() {   
    close_go_offline_modal();
    setTimeout(() => {
        show_set_offline_key_modal();
    }, 200);
}

// Function to show the Set Offline Key modal
function show_set_offline_key_modal() {
    const modalHtml = `
        <div id="set-offline-key-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Set Offline Key</h4>
                        <button type="button" class="close" onclick="window.OfflineTransitionManager.closeSetKeyModal()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body" style="padding: 30px;">
                        <p style="font-size: 16px; margin-bottom: 20px; color: #333;">Set a key to log in while in offline mode:</p>
                        
                        <input type="text" id="offline-key-input" class="form-control" style="margin-bottom: 10px; padding: 12px; font-size: 14px; border: 1px solid #ccc; border-radius: 4px;" placeholder="Enter your offline key" oninput="window.OfflineTransitionManager.handleKeyInput()" autocomplete="off" tabindex="1" value="sssDDDkkk@@@2">
                        
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
                        <button type="button" class="btn btn-light" onclick="window.OfflineTransitionManager.closeSetKeyModal()" style="margin-right: 10px; padding: 8px 20px;">
                            Cancel
                        </button>
                        <button type="button" id="go-offline-btn" class="btn btn-primary" onclick="window.OfflineTransitionManager.goOfflineFinal()" style="background-color: #7b2d8e; border-color: #7b2d8e; color: white; padding: 8px 20px; opacity: 0.6;" disabled>
                            <img src="../img/offline-go.svg" style="width: 14px; height: 14px; margin-right: 5px; vertical-align: middle;" alt="Go Offline">Go Offline
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="set-offline-key-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    setTimeout(() => {
        const modal = document.getElementById('set-offline-key-modal');
        const backdrop = document.getElementById('set-offline-key-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
        const input = document.getElementById('offline-key-input');
        if (input) {
            input.disabled = false;
            input.focus();
            input.select();
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

// Function to handle key input with delayed validation
function handle_key_input() {
    if (validation_timer) {
        clearTimeout(validation_timer);
    }
    
    validation_timer = setTimeout(() => {
        validate_key_realtime();
    }, 300);
}

// Function to validate key in real-time
function validate_key_realtime() {
    const keyInput = document.getElementById('offline-key-input');
    const key = keyInput ? keyInput.value : '';
    const errorDiv = document.getElementById('key-validation-error');
    const goOfflineBtn = document.getElementById('go-offline-btn');
    
    const isValid = window.OfflineSessionValidator.validateKey(key);
    
    if (key.length === 0) {
        if (errorDiv) errorDiv.style.display = 'none';
        if (keyInput) {
            keyInput.disabled = false;
            keyInput.style.borderColor = '#ccc';
        }
        if (goOfflineBtn) {
            goOfflineBtn.disabled = true;
            goOfflineBtn.style.opacity = '0.6';
        }
    } else if (!isValid) {
        if (errorDiv) errorDiv.style.display = 'block';
        if (keyInput) {
            keyInput.disabled = false;
            keyInput.style.borderColor = '#dc3545';
        }
        if (goOfflineBtn) {
            goOfflineBtn.disabled = true;
            goOfflineBtn.style.opacity = '0.6';
        }
    } else {
        if (errorDiv) errorDiv.style.display = 'none';
        if (keyInput) {
            keyInput.disabled = false;
            keyInput.style.borderColor = '#ccc';
        }
        if (goOfflineBtn) {
            goOfflineBtn.disabled = false;
            goOfflineBtn.style.opacity = '1';
        }
    }
}

// Function to show the Moving to Offline Mode modal
function show_moving_to_offline_modal() {
    const modalHtml = `
        <div id="moving-to-offline-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: bold; font-size:17px;">Moving to Offline Mode</h4>
                    </div>
                    <div class="modal-body" style="padding-top: 10px;padding-bottom: 10px; text-align: center;">                        
                        <p style="font-size:17px; color: #333;">Now switching to offline mode - this process may take several minutes.</p>                  
                        <p style="font-size:17px; color: #666;">This screen will refresh when the system is in offline mode.</p>
                        <p style="font-size:17px; color: #666;">Do not refresh your browser while offline mode is activating.</p>
                        
                        <div id="offline-progress-container" style="display:none;margin-top: 20px; text-align: left; padding: 0 20px;">
                            <div id="offline-progress-message" style="font-size: 14px; color: #555; margin-bottom: 10px;"></div>
                        </div>
                        
                        <div id="offline-error-container" style="display: none; margin-top: 20px; padding: 15px; background-color: #fff3cd; border: 1px solid #ffc107; border-radius: 4px; text-align: left;">
                            <div style="display: flex; align-items: center; margin-bottom: 10px;">
                                <span style="color: #856404; font-weight: bold; font-size: 16px;">⚠️ Connection Issue</span>
                            </div>
                            <div id="offline-error-message" style="font-size: 14px; color: #856404;"></div>
                        </div>
                    </div>
                    <div style="width:100%; text-align: right; padding-right:10px; padding-bottom:10px;">
                        <button type="button" id="offline-cancel-btn" class="btn btn-primary" disabled="true" onclick="window.OfflineTransitionManager.cancelTransition()" style="line-height: 1.15; max-width: 160px; white-space: normal; padding-left: 8px; padding-right: 8px;">
                            Cancel
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div id="moving-to-offline-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    setTimeout(() => {
        const modal = document.getElementById('moving-to-offline-modal');
        const backdrop = document.getElementById('moving-to-offline-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

// Function to close the Moving to Offline Mode modal
function close_moving_to_offline_modal() {
    const modal = document.getElementById('moving-to-offline-modal');
    const backdrop = document.getElementById('moving-to-offline-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) modal.parentNode.removeChild(modal);
            if (backdrop.parentNode) backdrop.parentNode.removeChild(backdrop);
        }, 150);
    }
}

// Function to update offline modal status with progress or error messages
function update_offline_modal_status(message, type = 'progress') {
    const progressContainer = document.getElementById('offline-progress-container');
    const progressMessage = document.getElementById('offline-progress-message');
    const errorContainer = document.getElementById('offline-error-container');
    const errorMessage = document.getElementById('offline-error-message');
    
    if (type === 'progress') {
        if (progressMessage) {
            const timestamp = new Date().toLocaleTimeString();
            const messageHtml = `<div style="margin-bottom: 5px;"><span style="color: #666; font-size: 12px;">[${timestamp}]</span> ${message}</div>`;
            progressMessage.innerHTML += messageHtml;
            
            if (progressContainer) {
                progressContainer.scrollTop = progressContainer.scrollHeight;
            }
        }
    } else if (type === 'error') {
        if (errorContainer && errorMessage) {
            errorContainer.style.display = 'block';
            errorMessage.innerHTML = message;
        }
    } else if (type === 'clear-error') {
        if (errorContainer && errorMessage) {
            errorContainer.style.display = 'none';
            errorMessage.innerHTML = '';
        }
    }
}

// Function to enable the cancel button in offline modal
function enable_offline_cancel_button() {
    const cancelBtn = document.getElementById('offline-cancel-btn');
    if (cancelBtn) {
        cancelBtn.disabled = false;
        cancelBtn.style.opacity = '1';
        cancelBtn.style.cursor = 'pointer';
    }
}

// Function to cancel offline transition and clean up
async function cancel_offline_transition() {  
    
    try {
        offlineLog.log('OfflineTransitionManager', 'Canceling offline mode transition...');
        
        if ('serviceWorker' in navigator) {
            const registration = await navigator.serviceWorker.getRegistration();
            if (registration) {
                await registration.unregister();
                offlineLog.log('OfflineTransitionManager', 'Service worker unregistered');
            }
        }
        
        if ('caches' in window) {
            const cacheNames = await caches.keys();
            for (const cacheName of cacheNames) {
                await caches.delete(cacheName);
            }
            offlineLog.log('OfflineTransitionManager', 'All caches cleared');
        }
        
        localStorage.removeItem('is_offline');
        localStorage.removeItem('mmria_offline_session');
        localStorage.removeItem('has_active_offline_session');
        localStorage.removeItem('mmria_cached_cases');
        offlineLog.log('OfflineTransitionManager', 'Offline session data cleared');
        
        setTimeout(() => {
            window.location.reload();
            //close_moving_to_offline_modal();
            //alert('Offline mode transition has been canceled. You remain in online mode.');
            
            //if (typeof get_case_set === 'function') {
            //    get_case_set();
            //}
        }, 1000);
        
    } catch (error) {
        offlineLog.error('OfflineTransitionManager', 'Error during offline transition cancellation:', error);
       
        
        setTimeout(() => {
            close_moving_to_offline_modal();
            alert('Offline mode transition canceled. Please refresh the page if you experience any issues.');
        }, 1000);
    }
}

// Function to show the Moving to Online Mode modal
function show_moving_to_online_modal() {
    const modalHtml = `
        <div id="moving-to-online-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                        <h4 class="modal-title" style="margin: 0; font-weight: bold; font-size:17px;">Moving to Online Mode</h4>
                    </div>
                    <div class="modal-body" style="padding-top: 10px;padding-bottom: 10px; text-align: center;">                        
                        <p style="font-size:17px; color: #333;">Now switching to online mode - this process may take several minutes.</p>                  
                        <p style="font-size:17px; color: #666;">This screen will refresh when the system is back online.</p>
                    </div>
                </div>
            </div>
        </div>
        <div id="moving-to-online-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    setTimeout(() => {
        const modal = document.getElementById('moving-to-online-modal');
        const backdrop = document.getElementById('moving-to-online-backdrop');
        if (modal && backdrop) {
            modal.classList.add('show');
            modal.style.display = 'block';
            backdrop.classList.add('show');
        }
    }, 10);
}

// Function to close the Moving to Online Mode modal
function close_moving_to_online_modal() {
    const modal = document.getElementById('moving-to-online-modal');
    const backdrop = document.getElementById('moving-to-online-backdrop');
    
    if (modal && backdrop) {
        modal.classList.remove('show');
        backdrop.classList.remove('show');
        
        setTimeout(() => {
            if (modal.parentNode) modal.parentNode.removeChild(modal);
            if (backdrop.parentNode) backdrop.parentNode.removeChild(backdrop);
        }, 150);
    }
}

// Function for final Go Offline button
async function go_offline_final() {
    let result = null;
    const keyInput = document.getElementById('offline-key-input');
    const key = keyInput ? keyInput.value : '';
    
    if (!window.OfflineSessionValidator.validateKey(key)) {
        offlineLog.log('OfflineTransitionManager', 'Key validation failed on final check');
        return;
    }
    localStorage.setItem('offline_bypass_unlock_case_beacon', 'true');

    const offlineIds = g_ui.offline_case_view_list_by_user.map(doc => doc.id);

    offlineLog.log('OfflineTransitionManager', 'Starting offline mode transition... Offline case IDs:', offlineIds);
    
    g_offline_transition_retry_count = 0;
    
    close_set_offline_key_modal();
    show_moving_to_offline_modal();

        const requestData = {
            offline_ids: offlineIds,
            offline_key: key,               
        };        
        offlineLog.log('OfflineTransitionManager', 'Creating offline session');
        const response = await fetch('/api/OfflineCase', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(requestData)
        });
        if (response.ok) {
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                result = await response.json();

                if (result.ok) {
                    offlineLog.log('OfflineTransitionManager', 'Offline session created successfully:', result);
                  
                         
                    setTimeout(() => {       
                        attempt_offline_transition(key, offlineIds, result);       
                    }, 200);
                } else {
                    throw new Error(result.error_description || 'Server returned error during offline session creation');
                }
            } else {
                offlineLog.error('OfflineTransitionManager', 'Response is not JSON. Content-Type:', contentType);
                const responseText = await response.text();
                offlineLog.error('OfflineTransitionManager', 'Response text preview:', responseText.substring(0, 500));
                throw new Error('Server returned an unexpected response format');
            }
        } else {
            offlineLog.error('OfflineTransitionManager', 'HTTP error:', response.status, response.statusText);
            const responseText = await response.text();
            offlineLog.error('OfflineTransitionManager', 'Error response:', responseText.substring(0, 500));
            throw new Error(`Server error: ${response.status} ${response.statusText}`);
        }


}

async function sync_log_data() {
    // Sync logs to server before transitioning back online
    offlineLog.log('OfflineTransitionManager', 'Syncing logs to server...');
    try {
        const syncResult = await offlineLog.syncToServer({ keepalive: true });
        if (syncResult.success) {
            offlineLog.log('OfflineTransitionManager', `Successfully synced ${syncResult.synced} logs to server`);
        } else {
            offlineLog.warn('OfflineTransitionManager', 'Log sync failed:', syncResult.message);
        }
    } catch (syncError) {
        offlineLog.error('OfflineTransitionManager', 'Error syncing logs:', syncError);
    }
}

// Function to attempt offline transition with retry logic
async function attempt_offline_transition(key, offlineIds, result) {
    const attemptNumber = g_offline_transition_retry_count + 1;
    
    try {

        const offlineSessionId = result.id;
                
        // Update existing logs with offline session ID
        await offlineLog.updateLogsWithSessionId(offlineSessionId);

        //set offline session ID 
        localStorage.setItem('offline_session_id', offlineSessionId);        
        offlineLog.log('OfflineTransitionManager', 'Setting localStorage offline_session_id:', offlineSessionId);

        if (!navigator.onLine) {
            throw new Error('No internet connection detected');
        }
        
        const isConnected = await window.OfflineNetworkMonitor.check();
        if (!isConnected) {
            throw new Error('Cannot reach server - please check your internet connection');
        }       
        
        // Clean up any previous service workers and caches before starting fresh
        if (!('serviceWorker' in navigator)) {
            throw new Error('Service Worker not supported in this browser');
        }
        
        if (!('caches' in window)) {
            throw new Error('Cache API not supported in this browser');
        }
        
        offlineLog.log('OfflineTransitionManager', 'Preparing service worker...');
        
        //sync log data before going offline and the service worker takes over
        sync_log_data();   

        // Fetch service worker version from server
        let swVersion;
        const versionResponse = await fetch('/api/OfflineCase/cache-version');
        if (versionResponse.ok) {
            swVersion = await versionResponse.json();
            swVersion = swVersion.version;
            offlineLog.log('OfflineTransitionManager', 'Server service worker version:', swVersion);
        } else {
            offlineLog.warn('OfflineTransitionManager', 'Failed to fetch service worker version');
            throw new Error('Failed to fetch service worker version');
        }

        // Register service worker with BOTH version and session ID
        // Version changes only on deployments, but session ID is unique for each offline session
        // This forces the browser to install a fresh service worker for each new offline session
        const registration = await navigator.serviceWorker.register(
            `/service-worker.js?v=${swVersion}&session=${offlineSessionId}`,
            { updateViaCache: 'none' }  // Always fetch fresh, never use HTTP cache
        );
        offlineLog.log('OfflineTransitionManager', `Service worker registered: v${swVersion}, session: ${offlineSessionId}`);

        // Send SKIP_WAITING immediately if there's a waiting worker (BEFORE waiting for ready)
        if (registration.waiting) {
            offlineLog.log('OfflineTransitionManager', 'Service worker is waiting, sending SKIP_WAITING...');
            registration.waiting.postMessage({ type: 'SKIP_WAITING' });
        } else if (registration.installing) {
            offlineLog.log('OfflineTransitionManager', 'Service worker is installing, sending SKIP_WAITING...');
            registration.installing.postMessage({ type: 'SKIP_WAITING' });
        }

        // Wait for ready with a timeout to prevent infinite hang
        offlineLog.log('OfflineTransitionManager', 'Waiting for service worker to be ready...');
        const readyTimeout = new Promise((_, reject) => 
            setTimeout(() => reject(new Error('Service worker ready timeout after 10 seconds')), 10000)
        );

        try {
            await Promise.race([navigator.serviceWorker.ready, readyTimeout]);
            offlineLog.log('OfflineTransitionManager', 'Service worker is ready');
        } catch (error) {
            offlineLog.error('OfflineTransitionManager', 'Service worker ready timeout - proceeding anyway:', error);
            // Continue anyway - the service worker may still work
        }

        // Send offline session ID to service worker as early as possible
        if (registration.installing) {
            registration.installing.postMessage({
                type: 'SET_OFFLINE_SESSION_ID',
                sessionId: offlineSessionId
            });
        } else if (registration.waiting) {
            registration.waiting.postMessage({
                type: 'SET_OFFLINE_SESSION_ID',
                sessionId: offlineSessionId
            });
        } else if (registration.active) {
            registration.active.postMessage({
                type: 'SET_OFFLINE_SESSION_ID',
                sessionId: offlineSessionId
            });
        }

        offlineLog.log('OfflineTransitionManager', 'Initializing new offline session...');
        if (window.offlineSessionManager) {
            try {               
                const sessionInitPromise = window.offlineSessionManager.initialize();
                const timeoutPromise = new Promise((_, reject) => 
                    setTimeout(() => reject(new Error('Session initialization timeout')), 5000)
                );
                
                const sessionInfo = await Promise.race([sessionInitPromise, timeoutPromise]);        
               
            } catch (sessionError) {
                offlineLog.warn('OfflineTransitionManager', 'Failed to initialize offline session, continuing with standard cache:', sessionError);
              
            }
        } 
        
        if (registration.installing) {
            registration.installing.postMessage({ type: 'SKIP_WAITING' });
        } else if (registration.waiting) {
            registration.waiting.postMessage({ type: 'SKIP_WAITING' });
        } else if (registration.active) {
            registration.active.postMessage({ type: 'CLAIM_CLIENTS' });
        }
        
        if (!navigator.serviceWorker.controller) {
            offlineLog.log('OfflineTransitionManager', 'Waiting for service worker to control page...');
           
            
            await new Promise((resolve) => {
                const handleControllerChange = () => {
                    navigator.serviceWorker.removeEventListener('controllerchange', handleControllerChange);
                    resolve();
                };
                
                navigator.serviceWorker.addEventListener('controllerchange', handleControllerChange);
                
                setTimeout(() => {
                    navigator.serviceWorker.removeEventListener('controllerchange', handleControllerChange);
                    resolve();
                }, 3000);
            });
        }             
               
        // CRITICAL: Set encryption key BEFORE any caching operations to prevent fetch events with no key
        const keySalt = await window.OfflineUtils.generateKeySalt(result.id, new Date().toISOString());
        const derivedKeyHash = await window.OfflineUtils.deriveKeyHash(key, keySalt);
        
        const offlineSessionData = {
            offlineSessionId: result.id,
            keySalt: keySalt,
            derivedKeyHash: derivedKeyHash,
            offlineIds: offlineIds,
            dateCreated: new Date().toISOString(),
            user_id: g_user_name || 'unknown_user'
        };
        
        localStorage.setItem('mmria_offline_session', JSON.stringify(offlineSessionData));
        
        window.mmria_offline_session_data = offlineSessionData;
        
        if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
            navigator.serviceWorker.controller.postMessage({
                type: 'CACHE_OFFLINE_SESSION_DATA',
                data: offlineSessionData
            });          
        }
        
        // Set encryption key IMMEDIATELY after service worker is controlling
        // This must happen BEFORE any caching operations that trigger fetch events
        const keySet = await ServiceWorkerManager.setOfflineKey(key, offlineSessionData.keySalt);
        
        if (!keySet) {
            throw new Error('Failed to set encryption key in service worker');
        }
        
        // Wait briefly to ensure key is fully set before starting cache operations
        await new Promise(resolve => setTimeout(resolve, 500));
        offlineLog.log('OfflineTransitionManager', 'Encryption key set and ready');
        
        // FIX: Keep service worker alive for ENTIRE offline session
        // Problem: Browser terminates idle service worker after ~30 seconds, losing offlineCryptoKey from memory
        // Solution: Send periodic keep-alive messages every 15 seconds continuously until going back online
        // Note: This is a best-effort approach - browsers can still terminate SW under memory pressure or tab backgrounding
        // TODO: Future enhancement - implement periodic re-authentication (every 30 min) for more reliable key availability
        
        // Start persistent keep-alive that runs for entire offline session
        g_service_worker_keep_alive_interval = setInterval(() => {
            if (navigator.serviceWorker.controller) {
                navigator.serviceWorker.controller.postMessage({ type: 'KEEP_ALIVE' });
            }
        }, 15000); // Every 15 seconds
        
        offlineLog.log('OfflineTransitionManager', 'Service worker keep-alive started');
        
        offlineLog.log('OfflineTransitionManager', `Caching offline resources for ${offlineIds.length} case(s)...`);
        await ServiceWorkerManager.prefetchCases(offlineIds);
        await ServiceWorkerManager.precachePages();
        await ServiceWorkerManager.cacheMetadata();
        await setup_offline_session_auth();
        offlineLog.log('OfflineTransitionManager', 'Offline resources cached and authentication ready');

        localStorage.setItem('is_offline', 'true');
        localStorage.setItem('has_active_offline_session', 'true');

        if (window.ServiceWorkerManager) {
            window.ServiceWorkerManager.notifyOfflineStatusChange();
            window.ServiceWorkerManager.notifyActiveOfflineSessionChange();
        }
        
        offlineLog.log('OfflineTransitionManager', 'Offline mode transition complete - refreshing interface');               

        sync_log_data();  

        setTimeout(() => {
            close_moving_to_offline_modal();
        }, 1000);
        
        await refresh_offline_documents_list();
        
        if (window.OfflineModals) {
            window.OfflineModals.hideOnlineElements();
        }
        
        document.body.classList.add('mmria-offline-mode');
        
        window.OfflineNetworkMonitor.initialize();
        
        if (window.updateOfflineModeIndicator) {
            window.updateOfflineModeIndicator();
        }
        
        if (typeof get_case_set === 'function') {
            get_case_set();
        }          
        

        
    } catch (error) {
        offlineLog.error('OfflineTransitionManager', 'Error during offline transition attempt ' + attemptNumber + ':', error);
        
        g_offline_transition_retry_count++;
        
        if (g_offline_transition_retry_count < MAX_OFFLINE_TRANSITION_RETRIES) {
            const retriesLeft = MAX_OFFLINE_TRANSITION_RETRIES - g_offline_transition_retry_count;
            offlineLog.error('OfflineTransitionManager', `Attempt ${attemptNumber} failed: ${error.message}. Retrying in 3s... (${retriesLeft} attempts remaining)`);
            
            await new Promise(resolve => setTimeout(resolve, 3000));
            
            return attempt_offline_transition(key, offlineIds);
        } else {
            offlineLog.error('OfflineTransitionManager', `Failed after ${MAX_OFFLINE_TRANSITION_RETRIES} attempts: ${error.message}. Click Cancel to exit offline mode setup.`);
            
            enable_offline_cancel_button();
        }
    }
}

// Function to setup offline session token
async function setup_offline_session_auth() {
    try {
        const response = await fetch('/api/offlinecase/create-offline-auth-token', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ })
        });

        if (!response.ok) {
            throw new Error('Failed to create offline auth token');
        }

        const result = await response.json();
        offlineLog.log('OfflineTransitionManager', 'Offline auth token created:', result);
    } catch (error) {
        offlineLog.error('OfflineTransitionManager', 'Error creating offline auth token:', error);
    }
}

// Function to unregister service worker (for going back online)
async function unregister_service_worker() {
    if ('serviceWorker' in navigator) {
        try {
            const registrations = await navigator.serviceWorker.getRegistrations();
            offlineLog.log('OfflineTransitionManager', `Unregistering ${registrations.length} service worker(s)`);
            
            // Check if there's a controller before unregistering
            const hadController = !!navigator.serviceWorker.controller;
            
            for (const registration of registrations) {
                await registration.unregister();
            }
            
            // Wait for controller to be cleared if one existed
            if (hadController) {
                // Poll for controller to clear with 10-second timeout
                await new Promise((resolve) => {
                    if (!navigator.serviceWorker.controller) {
                        resolve();
                        return;
                    }
                    
                    const checkInterval = setInterval(() => {
                        if (!navigator.serviceWorker.controller) {
                            clearInterval(checkInterval);
                            resolve();
                        }
                    }, 100);
                    
                    // Timeout after 10 seconds
                    setTimeout(() => {
                        clearInterval(checkInterval);
                        offlineLog.warn('OfflineTransitionManager', 'Controller clear timeout - proceeding');
                        resolve();
                    }, 10000);
                });
            }
            
            // Additional wait to ensure browser fully processes the unregistration
            await new Promise(resolve => setTimeout(resolve, 2000));
            
            // Verify complete cleanup
            const remainingRegistrations = await navigator.serviceWorker.getRegistrations();
            if (remainingRegistrations.length > 0) {
                offlineLog.warn('OfflineTransitionManager', `${remainingRegistrations.length} registrations still found after cleanup`);
            }
            
        } catch (error) {
            offlineLog.error('OfflineTransitionManager', 'Error unregistering service worker:', error);
            throw error;
        }
    }
}

// Function to clear all cached data when going back online
async function clear_all_cached_data() {
    try {
        if ('caches' in window) {
            const cacheNames = await caches.keys();
            let deletedCount = 0;
            
            for (const cacheName of cacheNames) {
                if (cacheName.startsWith('mmria-')) {
                    await caches.delete(cacheName);
                    deletedCount++;
                }
            }
            
            if (deletedCount > 0) {
                offlineLog.log('OfflineTransitionManager', `Cleared ${deletedCount} cache(s)`);
            }
        }
        
        const localStorageKeys = [
            'has_active_offline_session',
            'mmria_offline_session',
            'is_offline',
            'mmria_cached_cases',
            'mmria_offline_changes',
            'mmria_offline_case_documents',
            'process_offline_cases',
            'offline_session_id'
        ];
        
        for (const key of localStorageKeys) {
            localStorage.removeItem(key);
        }
        
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key && (key.startsWith('mmria_static_') || key.startsWith('mmria_meta_'))) {
                localStorage.removeItem(key);
                i--;
            }
        }
        
        offlineLog.log('OfflineTransitionManager', 'Cached data cleared');
        
    } catch (error) {
        offlineLog.error('OfflineTransitionManager', 'Error clearing cached data:', error);
        throw error;
    }
}

// Function to confirm invalid offline state recovery
async function confirm_invalid_offline_state_recovery() {
    try {
        offlineLog.log('OfflineTransitionManager', 'User confirmed invalid offline state recovery, cleaning up...');
      
        // Check if there's an offline session that needs to be abandoned
        let offlineSessionId = localStorage.getItem('offline_session_id');
        
        // If no offline_session_id in localStorage, check the database for an active session
        if (!offlineSessionId || offlineSessionId === '') {
            offlineLog.log('OfflineTransitionManager', 'No offline_session_id in localStorage, checking database for active session...');
            
            try {
                const activeSessionResponse = await fetch('/api/OfflineCase/active-user-session');
                
                if (activeSessionResponse.ok) {
                    const activeSessionData = await activeSessionResponse.json();
                    
                    if (activeSessionData && activeSessionData._id && activeSessionData.error !== 'no active sessions') {
                        offlineSessionId = activeSessionData._id;
                        offlineLog.log('OfflineTransitionManager', 'Found active offline session in database:', offlineSessionId);
                        
                        // Store it in localStorage for the abandon process
                        localStorage.setItem('offline_session_id', offlineSessionId);
                    } else {
                        offlineLog.log('OfflineTransitionManager', 'No active offline session found in database');
                    }
                } else {
                    offlineLog.warn('OfflineTransitionManager', 'Failed to check for active session:', activeSessionResponse.status);
                }
            } catch (error) {
                offlineLog.warn('OfflineTransitionManager', 'Error checking for active offline session:', error);
            }
        }
   
        
        if (offlineSessionId) {
            offlineLog.log('OfflineTransitionManager', 'Found offline session, abandoning before cleanup:', offlineSessionId);
            
            // Call the abandon offline session function from OfflineSyncManager
            if (window.OfflineSyncManager && window.OfflineSyncManager.abandonOfflineSession) {
                try {
                    await window.OfflineSyncManager.abandonOfflineSession(false); // Don't reload yet
                    await new Promise(resolve => setTimeout(resolve, 500));
                } catch (error) {
                    offlineLog.error('OfflineTransitionManager', 'Error abandoning offline session:', error);
                    offlineLog.log('OfflineTransitionManager', 'Proceeding with standard cleanup...');
                }
            } else {
                offlineLog.warn('OfflineTransitionManager', 'OfflineSyncManager.abandonOfflineSession not available, proceeding with standard cleanup');
            }
        }
        else {
            // Only try to release case locks if both OfflineSyncManager and g_ui are available
            // During invalid offline state detection (early page load), these may not be loaded yet
            if (typeof window.OfflineSyncManager !== 'undefined' && 
                window.OfflineSyncManager && 
                typeof g_ui !== 'undefined' && 
                g_ui) {
                offlineLog.log('OfflineTransitionManager', 'Attempting to release case locks...');
                await new Promise(resolve => setTimeout(resolve, 500));
                await window.OfflineSyncManager.releaseCaseLocks();
            } else {
                offlineLog.log('OfflineTransitionManager', 'OfflineSyncManager or g_ui not available yet - skipping case lock release during invalid state cleanup');
            }
        }   


        // Standard cleanup - unregister service worker and clear caches
        const registration = await navigator.serviceWorker.getRegistration();
        if (registration) {
            // Unregister service worker
            await registration.unregister();
            await new Promise(resolve => setTimeout(resolve, 1500));
        }
        // Use existing clear cache helper
        await clear_all_cached_data();
        
        await new Promise(resolve => setTimeout(resolve, 500));
        offlineLog.log('OfflineTransitionManager', 'Recovery complete, reloading page...');

        localStorage.removeItem('offline_mode_invalid_state_detected');

        //sync log data before exiting offline processing mode (non-blocking, keepalive ensures completion)
        await sync_log_data();
        await offlineLog.clearLogs()        

        window.location.reload();        
    } catch (error) {
        offlineLog.error('OfflineTransitionManager', 'Error during invalid offline state recovery:', error);
        alert('Error during recovery: ' + error.message);
    }
}

// Expose the offline transition manager API to the global scope
window.OfflineTransitionManager = {
    goOfflineClicked: go_offline_button_clicked,
    goOnlineClicked: go_online_clicked,
    closeGoOfflineModal: close_go_offline_modal,
    continueToSetKey: continue_to_set_key,
    closeSetKeyModal: close_set_offline_key_modal,
    handleKeyInput: handle_key_input,
    goOfflineFinal: go_offline_final,
    cancelTransition: cancel_offline_transition,
    clear_all_cached_data: clear_all_cached_data,
    confirmInvalidOfflineStateRecovery: confirm_invalid_offline_state_recovery,
    // Expose keep-alive interval for external access and cleanup
    g_service_worker_keep_alive_interval: g_service_worker_keep_alive_interval
};
