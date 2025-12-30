/**
 * Offline Transition Manager Module
 * Manages transitions between online and offline modes
 */

// Timer for debounced key validation
let validation_timer = null;

// Retry counter and max retries for offline transition
let g_offline_transition_retry_count = 0;
const MAX_OFFLINE_TRANSITION_RETRIES = 3;

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
        console.log('Go Offline button clicked but disabled - no cases selected');
        return;
    }
    
    console.log('Go Offline button clicked - showing modal');
    show_go_offline_modal();
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

    console.log('Go Online button clicked - checking network connectivity...');
    
    // First check if we have network connectivity
    const isConnected = await window.OfflineNetworkMonitor.check();
    if (!isConnected) {
        console.error('Go Online blocked - no network connectivity. Cannot go online - no network connection detected. Please check your internet connection and try again.');
        return;
    }
    
    console.log('Network connectivity confirmed - transitioning back to online mode');
    
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

        console.log('Step 1: Transitioning service worker to online mode...');
        
        // IMPORTANT: Clear offline status FIRST so service worker allows API calls through
        localStorage.removeItem('is_offline');
        localStorage.removeItem('has_active_offline_session');
        
        // Immediately set service worker to online mode for faster transition
        if (window.ServiceWorkerManager) {
            window.ServiceWorkerManager.setOnlineImmediately();
        }
        
        // Give service worker a moment to process the status change
        console.log('Waiting for service worker to process status change...');
        await new Promise(resolve => setTimeout(resolve, 200)); // Increased slightly for safety
        
        console.log('Step 2: Saving cached cases to database...');
        // Now save cached case documents to the database (service worker should allow this through)
        await window.OfflineSyncManager.saveCasesToDatabase();
        console.log('saveCasesToDatabase completed successfully');
        
        console.log('Step 3: Final cleanup...');
        
        console.log('Step 3: Stopping service worker communications...');
        if (window.ServiceWorkerManager) {
            // Don't send any more messages to the service worker
            window.ServiceWorkerManager.sendMessage({ type: 'PREPARE_FOR_UNREGISTER' });
            // Wait for it to process
            await new Promise(resolve => setTimeout(resolve, 1000));
        }

        // Unregister service worker
        console.log('Unregistering service worker...');
        await unregister_service_worker();
        
        // Clear service worker caches
        if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
            navigator.serviceWorker.controller.postMessage({ type: 'CLEAR_CACHES' });
        }
        
        // Clear offline session
        console.log('Clearing offline session...');
        if (window.offlineSessionManager) {
            window.offlineSessionManager.clear();
        }
        
        // Clear all cached data
        console.log('Clearing cached data...');
        await clear_all_cached_data();
        
        // Clear remaining offline session data
        localStorage.removeItem('mmria_offline_session');
        localStorage.removeItem('mmria_cached_cases');
        localStorage.removeItem('mmria_offline_changes');
        
        // Remove offline mode indicator from body
        document.body.classList.remove('mmria-offline-mode');
        
        // Add a longer delay before page reload to ensure API call completes
        console.log('Waiting before page reload to ensure API call completes...');
        await new Promise(resolve => setTimeout(resolve, 2000));
        
        // Refresh the page to fully return to online mode
        console.log('Returning to online mode - refreshing page');
        window.location.href ='/account/login';
        
    } catch (error) {
        console.error('Error transitioning to online mode:', error);
        alert(`Error transitioning to online mode: ${error.message}\nSome cached data may remain. Check console for details.`);
        
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
    console.log('Continue to set key button clicked - opening set key modal');
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
    console.log('Canceling offline transition...');
    
    try {
        update_offline_modal_status('Canceling offline mode transition...', 'progress');
        
        if ('serviceWorker' in navigator) {
            const registration = await navigator.serviceWorker.getRegistration();
            if (registration) {
                await registration.unregister();
                update_offline_modal_status('Service worker unregistered', 'progress');
            }
        }
        
        if ('caches' in window) {
            const cacheNames = await caches.keys();
            for (const cacheName of cacheNames) {
                await caches.delete(cacheName);
            }
            update_offline_modal_status('All caches cleared', 'progress');
        }
        
        localStorage.removeItem('is_offline');
        localStorage.removeItem('mmria_offline_session');
        localStorage.removeItem('has_active_offline_session');
        localStorage.removeItem('mmria_cached_cases');
        update_offline_modal_status('Offline session data cleared', 'progress');
        
        setTimeout(() => {
            window.location.reload();
            //close_moving_to_offline_modal();
            //alert('Offline mode transition has been canceled. You remain in online mode.');
            
            //if (typeof get_case_set === 'function') {
            //    get_case_set();
            //}
        }, 1000);
        
    } catch (error) {
        console.error('Error during offline transition cancellation:', error);
        update_offline_modal_status('Error during cancellation, but cleanup attempted', 'progress');
        
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
    const keyInput = document.getElementById('offline-key-input');
    const key = keyInput ? keyInput.value : '';
    
    if (!window.OfflineSessionValidator.validateKey(key)) {
        console.log('Key validation failed on final check');
        return;
    }
    
    const offlineIds = g_ui.offline_case_view_list_by_user.map(doc => doc.id);
    
    console.log('Starting offline mode transition...');
    console.log('Offline key:', key);
    console.log('Offline case IDs:', offlineIds);
    
    g_offline_transition_retry_count = 0;
    
    close_set_offline_key_modal();
    
    setTimeout(() => {
        show_moving_to_offline_modal();
        attempt_offline_transition(key, offlineIds);
    }, 200);
}

// Function to attempt offline transition with retry logic
async function attempt_offline_transition(key, offlineIds) {
    const attemptNumber = g_offline_transition_retry_count + 1;
    
    try {
        update_offline_modal_status('Checking network connectivity...', 'progress');
        
        if (!navigator.onLine) {
            throw new Error('No internet connection detected');
        }
        
        const isConnected = await window.OfflineNetworkMonitor.check();
        if (!isConnected) {
            throw new Error('Cannot reach server - please check your internet connection');
        }
        
        update_offline_modal_status('✓ Network connection verified', 'progress');
        update_offline_modal_status('', 'clear-error');
        
        if (!('serviceWorker' in navigator)) {
            throw new Error('Service Worker not supported in this browser');
        }
        
        update_offline_modal_status('Preparing service worker...', 'progress');
        console.log('Registering service worker...');
        
        const existingRegistration = await navigator.serviceWorker.getRegistration();
        if (existingRegistration) {
            console.log('Found existing service worker registration, unregistering first...');
            update_offline_modal_status('Cleaning up previous service worker...', 'progress');
            await existingRegistration.unregister();
            await new Promise(resolve => setTimeout(resolve, 1500));
        }
        
        // Fetch stable service worker version from server
        update_offline_modal_status('Fetching service worker version...', 'progress');
        let swVersion;
        try {
            const versionResponse = await fetch('/api/OfflineCase/cache-version');
            if (versionResponse.ok) {
                swVersion = await versionResponse.json();
                swVersion = swVersion.version;
                console.log('Using server-provided service worker version:', swVersion);
            } else {
                console.warn('Failed to fetch cache version, falling back to timestamp');
                swVersion = Date.now().toString();
            }
        } catch (versionError) {
            console.warn('Error fetching cache version, falling back to timestamp:', versionError);
            swVersion = Date.now().toString();
        }
        
        update_offline_modal_status('Registering service worker...', 'progress');
        const registration = await navigator.serviceWorker.register(`/service-worker.js?v=${swVersion}`);
        console.log('Service worker registered successfully with version:', swVersion, registration);
        update_offline_modal_status('✓ Service worker registered', 'progress');
        
        update_offline_modal_status('Waiting for service worker to activate...', 'progress');
        await navigator.serviceWorker.ready;
        console.log('Service worker is ready');
        update_offline_modal_status('✓ Service worker ready', 'progress');
        
        console.log('Initializing new offline session...');
        if (window.offlineSessionManager) {
            try {
                update_offline_modal_status('Initializing offline session...', 'progress');
                const sessionInitPromise = window.offlineSessionManager.initialize();
                const timeoutPromise = new Promise((_, reject) => 
                    setTimeout(() => reject(new Error('Session initialization timeout')), 5000)
                );
                
                const sessionInfo = await Promise.race([sessionInitPromise, timeoutPromise]);
                console.log('Offline session initialized successfully:', sessionInfo);
                update_offline_modal_status('✓ Offline session initialized', 'progress');
            } catch (sessionError) {
                console.warn('Failed to initialize offline session, continuing with standard cache:', sessionError);
                update_offline_modal_status('⚠️ Session initialization skipped (non-critical)', 'progress');
            }
        } else {
            console.warn('Offline session manager not available, using standard cache');
        }
        
        if (registration.installing) {
            console.log('Service worker installing, sending skipWaiting message...');
            update_offline_modal_status('Activating service worker...', 'progress');
            registration.installing.postMessage({ type: 'SKIP_WAITING' });
        } else if (registration.waiting) {
            console.log('Service worker waiting, sending skipWaiting message...');
            update_offline_modal_status('Activating service worker...', 'progress');
            registration.waiting.postMessage({ type: 'SKIP_WAITING' });
        } else if (registration.active) {
            console.log('Service worker active, sending claim message...');
            registration.active.postMessage({ type: 'CLAIM_CLIENTS' });
        }
        
        if (!navigator.serviceWorker.controller) {
            console.log('Service worker not controlling yet, waiting for controllerchange...');
            update_offline_modal_status('Waiting for service worker control...', 'progress');
            
            await new Promise((resolve) => {
                const handleControllerChange = () => {
                    navigator.serviceWorker.removeEventListener('controllerchange', handleControllerChange);
                    console.log('Service worker now controlling the page');
                    update_offline_modal_status('✓ Service worker in control', 'progress');
                    resolve();
                };
                
                navigator.serviceWorker.addEventListener('controllerchange', handleControllerChange);
                
                setTimeout(() => {
                    navigator.serviceWorker.removeEventListener('controllerchange', handleControllerChange);
                    console.log('Timeout waiting for controller change, but proceeding');
                    update_offline_modal_status('⚠️ Service worker control timeout (proceeding)', 'progress');
                    resolve();
                }, 3000);
            });
        } else {
            console.log('Service worker already controlling the page');
            update_offline_modal_status('✓ Service worker in control', 'progress');
        }
        
        update_offline_modal_status('Verifying connection before saving...', 'progress');
        if (!navigator.onLine) {
            throw new Error('Network connection lost');
        }
        
        const isStillConnected = await window.OfflineNetworkMonitor.check();
        if (!isStillConnected) {
            throw new Error('Cannot reach server');
        }
                 
        const requestData = {
            offline_ids: offlineIds,
            offline_key: key,               
        };
        
        update_offline_modal_status('Saving offline session to server...', 'progress');
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
                const result = await response.json();
                console.log('Offline data saved successfully:', result);
                update_offline_modal_status('✓ Offline session saved to server', 'progress');
                
                if (result.ok) {
                    console.log('Starting offline resource caching...');
                    update_offline_modal_status('Preparing offline session data...', 'progress');
                
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
                    update_offline_modal_status('✓ Session data stored locally', 'progress');
                    
                    window.mmria_offline_session_data = offlineSessionData;
                    
                    if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
                        navigator.serviceWorker.controller.postMessage({
                            type: 'CACHE_OFFLINE_SESSION_DATA',
                            data: offlineSessionData
                        });
                        console.log('Secure offline session data (with derived key hash) sent to service worker for caching');
                    }
                    
                    const keySet = await ServiceWorkerManager.setOfflineKey(key, offlineSessionData.keySalt);
                    
                    update_offline_modal_status(`Downloading ${offlineIds.length} case(s) for offline use...`, 'progress');
                    await ServiceWorkerManager.prefetchCases(offlineIds);
                    update_offline_modal_status('✓ Cases downloaded and cached', 'progress');
                    
                    update_offline_modal_status('Caching essential pages...', 'progress');
                    await ServiceWorkerManager.precachePages();
                    update_offline_modal_status('✓ Essential pages cached', 'progress');
                    
                    update_offline_modal_status('Caching metadata and form definitions...', 'progress');
                    await ServiceWorkerManager.cacheMetadata();
                    update_offline_modal_status('✓ Metadata cached', 'progress');
                    
                    update_offline_modal_status('Setting up offline authentication...', 'progress');
                    await setup_offline_session_auth();
                    update_offline_modal_status('✓ Offline authentication ready', 'progress');

                    localStorage.setItem('is_offline', 'true');
                    localStorage.setItem('has_active_offline_session', 'true');

                    if (window.ServiceWorkerManager) {
                        window.ServiceWorkerManager.notifyOfflineStatusChange();
                        window.ServiceWorkerManager.notifyActiveOfflineSessionChange();
                    }
                    
                    update_offline_modal_status('✓ Offline mode transition complete!', 'progress');
                    update_offline_modal_status('Refreshing interface...', 'progress');
                    
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
                } else {
                    throw new Error(result.error_description || 'Server returned error during offline setup');
                }
            } else {
                console.error('Response is not JSON. Content-Type:', contentType);
                const responseText = await response.text();
                console.error('Response text preview:', responseText.substring(0, 500));
                throw new Error('Server returned an unexpected response format');
            }
        } else {
            console.error('HTTP error:', response.status, response.statusText);
            const responseText = await response.text();
            console.error('Error response:', responseText.substring(0, 500));
            throw new Error(`Server error: ${response.status} ${response.statusText}`);
        }
        
    } catch (error) {
        console.error('Error during offline transition attempt ' + attemptNumber + ':', error);
        
        g_offline_transition_retry_count++;
        
        if (g_offline_transition_retry_count < MAX_OFFLINE_TRANSITION_RETRIES) {
            const retriesLeft = MAX_OFFLINE_TRANSITION_RETRIES - g_offline_transition_retry_count;
            const errorMsg = `
                <p><strong>Error:</strong> ${error.message}</p>
                <p>Retrying automatically in 3 seconds... (${retriesLeft} attempt(s) remaining)</p>
            `;
            update_offline_modal_status(errorMsg, 'error');
            update_offline_modal_status(`❌ Attempt ${attemptNumber} failed: ${error.message}`, 'progress');
            
            await new Promise(resolve => setTimeout(resolve, 3000));
            
            update_offline_modal_status('', 'clear-error');
            update_offline_modal_status(`Retrying... (Attempt ${g_offline_transition_retry_count + 1} of ${MAX_OFFLINE_TRANSITION_RETRIES})`, 'progress');
            
            return attempt_offline_transition(key, offlineIds);
        } else {
            const errorMsg = `
                <p><strong>Final Error:</strong> ${error.message}</p>
                <p>Failed after ${MAX_OFFLINE_TRANSITION_RETRIES} attempts. Please check your internet connection and try again later.</p>
                <p>Click the Cancel button below to exit offline mode setup and clean up cached data.</p>
            `;
            update_offline_modal_status(errorMsg, 'error');
            update_offline_modal_status(`❌ All ${MAX_OFFLINE_TRANSITION_RETRIES} attempts failed. Offline transition aborted.`, 'progress');
            
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
        console.log('Offline auth token created:', result);
    } catch (error) {
        console.error('Error creating offline auth token:', error);
    }
}

// Function to unregister service worker (for going back online)
async function unregister_service_worker() {
    if ('serviceWorker' in navigator) {
        try {
            console.log('Starting service worker unregistration...');
            const registrations = await navigator.serviceWorker.getRegistrations();
            console.log(`Found ${registrations.length} service worker registrations to unregister`);
            
            for (const registration of registrations) {
                console.log('Unregistering service worker:', registration.scope);
                const result = await registration.unregister();
                console.log('Service worker unregistered successfully:', result);
            }
            
            await new Promise(resolve => setTimeout(resolve, 1000));
            
            if (navigator.serviceWorker.controller) {
                console.log('Service worker controller still present, waiting for it to clear...');
                await new Promise(resolve => setTimeout(resolve, 1000));
            }
            
            console.log('Service worker unregistration completed');
        } catch (error) {
            console.error('Error unregistering service worker:', error);
            throw error;
        }
    }
}

// Function to clear all cached data when going back online
async function clear_all_cached_data() {
    console.log('Clearing all cached data...');
    
    try {
        if ('caches' in window) {
            const cacheNames = await caches.keys();
            console.log(`Found ${cacheNames.length} caches to clear:`, cacheNames);
            
            for (const cacheName of cacheNames) {
                if (cacheName.startsWith('mmria-')) {
                    const deleted = await caches.delete(cacheName);
                    console.log(`Cache '${cacheName}' deleted:`, deleted);
                }
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
            if (localStorage.getItem(key)) {
                localStorage.removeItem(key);
                console.log(`Cleared localStorage key: ${key}`);
            }
        }
        
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key && (key.startsWith('mmria_static_') || key.startsWith('mmria_meta_'))) {
                localStorage.removeItem(key);
                console.log(`Cleared cached resource: ${key}`);
                i--;
            }
        }
        
        console.log('All cached data cleared successfully');
        
    } catch (error) {
        console.error('Error clearing cached data:', error);
        throw error;
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
    clear_all_cached_data: clear_all_cached_data
};

console.log('Offline Transition Manager module loaded');
