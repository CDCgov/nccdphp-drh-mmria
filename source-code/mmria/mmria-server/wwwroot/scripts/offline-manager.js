// Offline Manager JavaScript
'use strict';

class OfflineManager {
    constructor() {
        this.isOfflineMode = localStorage.getItem('mmria_offline_mode') === 'true';
        this.offlineCases = new Map();
        this.pendingChanges = new Map();
        this.syncQueue = [];
        this.serviceWorker = null;
        this.cachingInProgress = false;
        this.init();
    }

    async init() {
        await this.registerServiceWorker();
        this.updateOfflineModeUI();
        this.loadOfflineCases();
        this.loadPendingChanges();
        this.checkNetworkStatus();
        
        // Set up periodic sync check
        setInterval(() => {
            this.checkPendingChanges();
        }, 30000); // Check every 30 seconds

        // Listen for online/offline events
        window.addEventListener('online', () => {
            this.showNotification('Network connection restored', 'success');
            this.updateNetworkStatus();
            this.renderOfflineCases(); // Re-render to enable network-dependent buttons
        });

        window.addEventListener('offline', () => {
            this.showNotification('Network connection lost - working offline', 'info');
            this.updateNetworkStatus();
            this.renderOfflineCases(); // Re-render to disable network-dependent buttons
        });

        // Set up fetch interceptor for API calls
        this.setupFetchInterceptor();
        this.setupJQueryInterceptor();
    }

    updateOfflineModeUI() {
        const toggle = document.getElementById('offline-mode-toggle');
        const text = document.getElementById('offline-mode-text');
        const description = document.getElementById('offline-mode-description');
        
        // Update sidebar status indicator
        const statusIcon = document.getElementById('offline-status-icon');
        const statusText = document.getElementById('offline-status-text');
        
        if (this.isOfflineMode) {
            if (toggle) {
                toggle.classList.add('active');
                // Add requires-network class so button is disabled when offline
                toggle.classList.add('requires-network');
            }
            if (text) text.innerHTML = '<i class="fa fa-wifi-slash"></i> Exit Offline Mode';
            if (description) description.textContent = 'Currently in offline mode. Only offline cases are visible. Network connection required to return to online mode.';
            if (statusIcon) statusIcon.className = 'fa fa-wifi-slash';
            if (statusText) statusText.textContent = 'Offline Mode';
        } else {
            if (toggle) {
                toggle.classList.remove('active');
                // Remove requires-network class when in online mode
                toggle.classList.remove('requires-network');
            }
            if (text) text.innerHTML = '<i class="fa fa-wifi"></i> Enter Offline Mode';
            if (description) description.textContent = 'Click to switch to offline mode. In offline mode, only cases marked for offline access will be visible.';
            if (statusIcon) statusIcon.className = 'fa fa-wifi';
            if (statusText) statusText.textContent = 'Online Mode';
        }
        
        // Update network-dependent elements immediately
        this.updateNetworkStatus();
    }

    async loadOfflineCases() {
        try {
            console.log('Loading offline cases...');
            
            // If offline and no network, try local interception first
            if (this.isOfflineMode && !navigator.onLine) {
                const interceptedCases = await this.interceptOfflineCasesApi();
                if (interceptedCases) {
                    this.offlineCases.clear();
                    interceptedCases.forEach(caseDoc => {
                        this.offlineCases.set(caseDoc._id, caseDoc);
                    });
                    console.log(`Loaded ${interceptedCases.length} offline cases from localStorage`);
                    return;
                }
            }
            
            // Normal API call when online or when interception fails
            const response = await fetch('/api/offline/offline-cases');
            console.log('Response status:', response.status);
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Failed to load offline cases:', response.status, errorText);
                throw new Error(`Failed to load offline cases: ${response.status} ${errorText}`);
            }
            
            const cases = await response.json();
            console.log('Loaded offline cases:', Array.isArray(cases) ? cases.length : 'Not an array', cases);
            
            this.offlineCases.clear();
            
            // Cases are now full case documents, not wrapped in rows
            if (Array.isArray(cases)) {
                cases.forEach(caseDoc => {
                    // Store the full case document in memory
                    this.offlineCases.set(caseDoc._id, caseDoc);
                    
                    // Also cache the full document in localStorage for offline editing
                    this.cacheOfflineCase(caseDoc);
                });
                
                // Cache the entire response for offline use
                localStorage.setItem('mmria_cached_offline_cases', JSON.stringify(cases));
            }
            
            console.log('Offline cases loaded successfully:', this.offlineCases.size, 'cases');
            this.renderOfflineCases();
        } catch (error) {
            console.error('Error loading offline cases:', error);
            this.showNotification('Failed to load offline cases: ' + error.message, 'error');
        }
    }

    renderOfflineCases() {
        const container = document.getElementById('offline-cases-list');
        
        if (this.offlineCases.size === 0) {
            container.innerHTML = '<div class="text-center text-muted">No cases marked for offline access</div>';
            return;
        }

        const casesHtml = Array.from(this.offlineCases.values()).map(caseData => {
            // Extract data from the full case document structure
            const recordId = caseData.home_record?.record_id || caseData.record_id || 'N/A';
            const lastName = caseData.home_record?.last_name || '';
            const firstName = caseData.home_record?.first_name || '';
            const syncStatus = this.pendingChanges.has(caseData._id) ? 'pending' : 'synced';
            
            // Check if network is available for online actions
            const isOnline = navigator.onLine;
            const markOnlineClass = isOnline ? 'btn-warning' : 'btn-warning requires-network';
            const markOnlineDisabled = isOnline ? '' : 'disabled';
            const markOnlineTitle = isOnline ? 'Mark this case as online' : 'Network connection required to mark case online';
            
            return `
                <div class="offline-case-item" data-case-id="${caseData._id}">
                    <div class="offline-case-info">
                        <h5>${lastName}, ${firstName} (${recordId})</h5>
                        <small>Case ID: ${caseData._id}</small>
                        <br>
                        <span class="sync-status ${syncStatus}">
                            ${syncStatus === 'pending' ? 'Changes pending sync' : 'Synced'}
                        </span>
                    </div>
                    <div class="offline-actions">
                        <button class="btn btn-sm btn-primary" onclick="openCase('${caseData._id}')">
                            <i class="fa fa-edit"></i> Edit
                        </button>
                        <button class="btn btn-sm btn-secondary" onclick="viewCase('${caseData._id}')">
                            <i class="fa fa-eye"></i> View
                        </button>
                        <button class="btn btn-sm ${markOnlineClass}" 
                                onclick="removeFromOffline('${caseData._id}')" 
                                title="${markOnlineTitle}"
                                ${markOnlineDisabled}>
                            <i class="fa fa-cloud"></i> Mark Online
                        </button>
                    </div>
                </div>
            `;
        }).join('');
        
        container.innerHTML = casesHtml;
    }

    async toggleOfflineMode() {
        // If trying to exit offline mode, check if network is available
        if (this.isOfflineMode && !navigator.onLine) {
            this.showNotification('Network connection required to exit offline mode. Please check your internet connection.', 'error');
            return;
        }
        
        if (!this.isOfflineMode) {
            // Entering offline mode - start caching
            this.showNotification('Preparing for offline mode...', 'info');
            await this.startCaching();
            
            // Pre-cache API responses
            await this.cacheApiResponsesOnSuccess();
        }
        
        this.isOfflineMode = !this.isOfflineMode;
        localStorage.setItem('mmria_offline_mode', this.isOfflineMode.toString());
        
        this.updateOfflineModeUI();
        
        // Dispatch custom event for other components to listen to
        window.dispatchEvent(new CustomEvent('offlineModeChanged', { 
            detail: { isOffline: this.isOfflineMode } 
        }));
        
        if (this.isOfflineMode) {
            this.showNotification('Switched to offline mode', 'success');
            // Filter case list to show only offline cases
            this.filterCaseListForOfflineMode();
        } else {
            this.showNotification('Switched to online mode', 'success');
            // Clear cached files when exiting offline mode
            await this.clearCache();
            // Show all cases
            this.showAllCases();
        }
    }

    filterCaseListForOfflineMode() {
        // This integrates with the main case list view to show only offline cases
        console.log('Filtering case list for offline mode');
        
        // If we're on the case index page, refresh the case list
        if (window.location.pathname.includes('/case')) {
            // Add a delay to ensure offline mode is fully initialized
            setTimeout(() => {
                if (typeof get_case_set === 'function') {
                    console.log('Refreshing case list for offline mode');
                    get_case_set();
                } else {
                    console.log('get_case_set function not available, reloading page');
                    window.location.reload();
                }
            }, 500);
        }
    }

    showAllCases() {
        // Remove offline filter and show all cases
        console.log('Showing all cases (online mode)');
        
        // If we're on the case index page, refresh the case list
        if (window.location.pathname.includes('/case')) {
            // Add a delay to ensure online mode is fully initialized  
            setTimeout(() => {
                if (typeof get_case_set === 'function') {
                    console.log('Refreshing case list for online mode');
                    get_case_set();
                } else {
                    console.log('get_case_set function not available, reloading page');
                    window.location.reload();
                }
            }, 500);
        }
    }

    async markCaseOffline(caseId) {
        try {
            // Check if we have network connection first
            if (!navigator.onLine) {
                this.showNotification('Network connection required to mark case offline. Please check your internet connection.', 'error');
                throw new Error('No network connection available');
            }
            
            console.log(`Attempting to mark case offline: ${caseId}`);
            
            const response = await fetch(`/api/offline/mark-offline/${caseId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error(`Failed to mark case offline: ${response.status} ${response.statusText}`, errorText);
                throw new Error(`Failed to mark case offline: ${response.status} ${response.statusText}`);
            }
            
            const result = await response.json();
            console.log('Successfully marked case offline:', result);
            this.showNotification('Case marked for offline access', 'success');
            await this.loadOfflineCases();
            
            return result;
        } catch (error) {
            console.error('Error marking case offline:', error);
            this.showNotification('Failed to mark case offline: ' + error.message, 'error');
            throw error;
        }
    }

    async markCaseOnline(caseId) {
        try {
            // Check if we have network connection first
            if (!navigator.onLine) {
                this.showNotification('Network connection required to mark case online. Please check your internet connection.', 'error');
                throw new Error('No network connection available');
            }
            
            console.log(`Attempting to mark case online: ${caseId}`);
            
            const response = await fetch(`/api/offline/mark-online/${caseId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error(`Failed to mark case online: ${response.status} ${response.statusText}`, errorText);
                throw new Error(`Failed to mark case online: ${response.status} ${response.statusText}`);
            }
            
            const result = await response.json();
            console.log('Successfully marked case online:', result);
            this.showNotification('Case marked as online', 'success');
            
            // Remove from offline caches
            this.offlineCases.delete(caseId);
            const cacheKey = `mmria_offline_case_${caseId}`;
            localStorage.removeItem(cacheKey);
            
            // Reload offline cases to update the UI
            await this.loadOfflineCases();
            
            return result;
        } catch (error) {
            console.error('Error marking case online:', error);
            this.showNotification('Failed to mark case online: ' + error.message, 'error');
            throw error;
        }
    }

    cacheOfflineCase(caseData) {
        // Store case data in localStorage for offline access
        const cacheKey = `mmria_offline_case_${caseData._id}`;
        localStorage.setItem(cacheKey, JSON.stringify(caseData));
        this.offlineCases.set(caseData._id, caseData);
    }

    getCachedCase(caseId) {
        const cacheKey = `mmria_offline_case_${caseId}`;
        const cached = localStorage.getItem(cacheKey);
        return cached ? JSON.parse(cached) : null;
    }

    savePendingChange(caseId, changeData) {
        this.pendingChanges.set(caseId, {
            caseId: caseId,
            changeData: changeData,
            timestamp: new Date().toISOString(),
            synced: false
        });
        
        // Update localStorage
        const pendingKey = 'mmria_pending_changes';
        const allPending = Array.from(this.pendingChanges.values());
        localStorage.setItem(pendingKey, JSON.stringify(allPending));
        
        this.updatePendingChangesCount();
        this.renderOfflineCases(); // Update sync status
    }

    loadPendingChanges() {
        const pendingKey = 'mmria_pending_changes';
        const stored = localStorage.getItem(pendingKey);
        
        if (stored) {
            const pendingArray = JSON.parse(stored);
            this.pendingChanges.clear();
            
            pendingArray.forEach(change => {
                if (!change.synced) {
                    this.pendingChanges.set(change.caseId, change);
                }
            });
        }
        
        this.updatePendingChangesCount();
    }

    updatePendingChangesCount() {
        const count = this.pendingChanges.size;
        const countElement = document.getElementById('pending-changes-count');
        if (countElement) {
            countElement.innerHTML = `<i class="fa fa-clock-o"></i> ${count} changes pending sync`;
            countElement.className = `sync-status ${count > 0 ? 'pending' : 'synced'}`;
        }
    }

    async syncAllChanges() {
        if (this.pendingChanges.size === 0) {
            this.showNotification('No changes to sync', 'info');
            return;
        }

        if (!navigator.onLine) {
            this.showNotification('No network connection available for sync', 'error');
            return;
        }

        try {
            const button = document.getElementById('sync-all-btn');
            button.disabled = true;
            button.innerHTML = '<i class="fa fa-spinner fa-spin"></i> Syncing...';

            const changes = Array.from(this.pendingChanges.values()).map(change => change.changeData);
            
            const response = await fetch('/api/offline/sync-offline-changes', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(changes)
            });

            if (!response.ok) throw new Error('Sync failed');

            const results = await response.json();
            let successCount = 0;
            let errorCount = 0;

            results.forEach(result => {
                if (result.status === 'success') {
                    successCount++;
                    this.pendingChanges.delete(result.case_id);
                } else {
                    errorCount++;
                    console.error('Sync error for case', result.case_id, result.error);
                }
            });

            // Update localStorage
            const pendingKey = 'mmria_pending_changes';
            const allPending = Array.from(this.pendingChanges.values());
            localStorage.setItem(pendingKey, JSON.stringify(allPending));

            this.updatePendingChangesCount();
            
            if (successCount > 0) {
                this.showNotification(`Successfully synced ${successCount} changes`, 'success');
            }
            
            if (errorCount > 0) {
                this.showNotification(`Failed to sync ${errorCount} changes`, 'error');
            }

        } catch (error) {
            console.error('Sync error:', error);
            this.showNotification('Failed to sync changes', 'error');
        } finally {
            const button = document.getElementById('sync-all-btn');
            button.disabled = false;
            button.innerHTML = '<i class="fa fa-cloud-upload"></i> Sync All Changes';
        }
    }

    checkPendingChanges() {
        // This is called periodically to update the pending changes count
        this.loadPendingChanges();
    }

    clearOfflineCache() {
        if (confirm('Are you sure you want to clear all offline data? This action cannot be undone.')) {
            // Clear offline cases
            this.offlineCases.forEach((_, caseId) => {
                const cacheKey = `mmria_offline_case_${caseId}`;
                localStorage.removeItem(cacheKey);
            });
            
            // Clear pending changes
            localStorage.removeItem('mmria_pending_changes');
            
            // Clear offline mode
            localStorage.removeItem('mmria_offline_mode');
            
            this.offlineCases.clear();
            this.pendingChanges.clear();
            this.isOfflineMode = false;
            
            this.updateOfflineModeUI();
            this.updatePendingChangesCount();
            this.renderOfflineCases();
            
            this.showNotification('Offline cache cleared', 'success');
        }
    }

    async searchCase() {
        const searchTerm = document.getElementById('case-search').value.trim();
        if (!searchTerm) {
            this.showNotification('Please enter a case ID or record ID to search', 'info');
            return;
        }

        try {
            // This would need to integrate with existing case search functionality
            const response = await fetch(`/api/case_view?search_key=${encodeURIComponent(searchTerm)}&take=10`);
            if (!response.ok) throw new Error('Search failed');
            
            const searchResults = await response.json();
            this.renderSearchResults(searchResults.rows);
            
        } catch (error) {
            console.error('Search error:', error);
            this.showNotification('Failed to search cases', 'error');
        }
    }

    renderSearchResults(results) {
        const container = document.getElementById('case-search-results');
        
        if (!results || results.length === 0) {
            container.innerHTML = '<div class="alert alert-info">No cases found matching your search.</div>';
            return;
        }

        const resultsHtml = results.map(result => {
            const isOffline = this.offlineCases.has(result.id);
            
            return `
                <div class="card mb-2">
                    <div class="card-body">
                        <h6 class="card-title">${result.value.last_name}, ${result.value.first_name} (${result.value.record_id})</h6>
                        <p class="card-text"><small class="text-muted">Case ID: ${result.id}</small></p>
                        ${isOffline ? 
                            '<span class="badge badge-success">Available Offline</span>' :
                            `<button class="btn btn-sm btn-primary" onclick="offlineManager.markCaseOffline('${result.id}')">
                                <i class="fa fa-download"></i> Mark for Offline
                            </button>`
                        }
                    </div>
                </div>
            `;
        }).join('');
        
        container.innerHTML = `<div class="mt-3"><h5>Search Results:</h5>${resultsHtml}</div>`;
    }

    checkNetworkStatus() {
        this.updateNetworkStatus();
    }

    updateNetworkStatus() {
        const isOnline = navigator.onLine;
        // Update UI elements based on network status
        document.querySelectorAll('.requires-network').forEach(element => {
            element.disabled = !isOnline;
        });
    }

    showNotification(message, type = 'info') {
        const container = document.getElementById('notifications');
        const notificationId = 'notification-' + Date.now();
        
        const notification = document.createElement('div');
        notification.id = notificationId;
        notification.className = `notification ${type}`;
        
        // Create the notification content with proper line break handling
        const icon = document.createElement('i');
        icon.className = `fa ${type === 'success' ? 'fa-check-circle' : type === 'error' ? 'fa-exclamation-circle' : 'fa-info-circle'}`;
        
        const messageElement = document.createElement('span');
        messageElement.style.whiteSpace = 'pre-line'; // Preserve line breaks
        messageElement.style.display = 'inline-block';
        messageElement.style.marginLeft = '8px';
        messageElement.style.marginRight = '20px';
        messageElement.textContent = message;
        
        const closeButton = document.createElement('button');
        closeButton.type = 'button';
        closeButton.className = 'close';
        closeButton.style.cssText = 'float: right; background: none; border: none; font-size: 18px; cursor: pointer;';
        closeButton.innerHTML = '&times;';
        closeButton.onclick = function() { this.parentElement.remove(); };
        
        notification.appendChild(icon);
        notification.appendChild(messageElement);
        notification.appendChild(closeButton);
        
        container.appendChild(notification);
        
        // Auto-remove after 10 seconds for longer messages with failed files
        const autoRemoveDelay = message.includes('Failed files:') ? 15000 : 5000;
        setTimeout(() => {
            const element = document.getElementById(notificationId);
            if (element) element.remove();
        }, autoRemoveDelay);
    }

    // Case API interception for offline mode
    getOfflineCase(caseId) {
        if (this.isOfflineMode) {
            // Try to get case from memory first
            if (this.offlineCases.has(caseId)) {
                return this.offlineCases.get(caseId);
            }
            
            // Fall back to localStorage
            const cacheKey = `mmria_offline_case_${caseId}`;
            const cached = localStorage.getItem(cacheKey);
            if (cached) {
                try {
                    return JSON.parse(cached);
                } catch (e) {
                    console.error('Failed to parse cached case data:', e);
                }
            }
        }
        return null;
    }

    async interceptCaseApiCall(caseId) {
        if (this.isOfflineMode) {
            // Return offline data if available
            const offlineCase = this.getOfflineCase(caseId);
            if (offlineCase) {
                console.log('Returning offline case data for:', caseId);
                return offlineCase;
            } else {
                throw new Error('Case not available offline');
            }
        } else {
            // Make normal API call when online
            const response = await fetch(`/api/case?case_id=${caseId}`);
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            return await response.json();
        }
    }

    saveOfflineCase(caseId, caseData) {
        if (this.isOfflineMode) {
            // Update in memory
            this.offlineCases.set(caseId, caseData);
            
            // Update in localStorage
            this.cacheOfflineCase(caseData);
            
            // Mark as pending sync
            this.cacheOfflineChanges(caseId, caseData);
            
            console.log('Case saved offline:', caseId);
            return true;
        }
        return false;
    }

    // API Interception Methods
    
    async interceptOfflineCasesApi() {
        if (this.isOfflineMode && !navigator.onLine) {
            // Return offline cases from memory/localStorage
            console.log('Intercepting offline-cases API call - serving from localStorage');
            
            // First try the pre-cached API response
            const cachedResponse = localStorage.getItem('mmria_cached_offline_cases');
            if (cachedResponse) {
                try {
                    const cases = JSON.parse(cachedResponse);
                    if (Array.isArray(cases) && cases.length > 0) {
                        console.log(`Serving ${cases.length} offline cases from cached API response`);
                        return cases;
                    }
                } catch (e) {
                    console.error('Failed to parse cached offline cases API response:', e);
                }
            }
            
            // Fallback to individual case cache
            const cases = [];
            for (const [caseId, caseDoc] of this.offlineCases) {
                cases.push(caseDoc);
            }
            
            // If no cases in memory, try localStorage
            if (cases.length === 0) {
                const offlineKeys = Object.keys(localStorage).filter(key => key.startsWith('mmria_offline_case_'));
                for (const key of offlineKeys) {
                    try {
                        const caseData = JSON.parse(localStorage.getItem(key));
                        if (caseData) {
                            cases.push(caseData);
                        }
                    } catch (e) {
                        console.error('Failed to parse cached case:', e);
                    }
                }
            }
            
            console.log(`Serving ${cases.length} offline cases from individual cache`);
            return cases;
        }
        return null; // Let normal API call proceed
    }
    
    async interceptJurisdictionTreeApi() {
        if (this.isOfflineMode && !navigator.onLine) {
            // Try to get from localStorage
            console.log('Intercepting jurisdiction_tree API call - serving from localStorage');
            const cached = localStorage.getItem('mmria_cached_jurisdiction_tree');
            if (cached) {
                try {
                    return JSON.parse(cached);
                } catch (e) {
                    console.error('Failed to parse cached jurisdiction tree:', e);
                }
            }
            
            // Return a minimal fallback structure if nothing cached
            console.warn('No cached jurisdiction tree found, returning minimal fallback');
            return [];
        }
        return null; // Let normal API call proceed
    }
    
    async interceptValidationApi(version) {
        if (this.isOfflineMode && !navigator.onLine) {
            const cachedData = localStorage.getItem(`mmria_validation_${version}`);
            if (cachedData) {
                console.log(`Serving validation data for version ${version} from localStorage`);
                return JSON.parse(cachedData);
            }
        }
        return null; // Let normal API call proceed
    }
    
    async interceptCaseViewApi(url) {
        if (this.isOfflineMode && !navigator.onLine) {
            console.log('Intercepting case_view API call - serving offline cases');
            
            // Get offline cases
            const cases = [];
            for (const [caseId, caseDoc] of this.offlineCases) {
                // Convert offline case to case_view format
                const caseViewItem = {
                    id: caseDoc._id,
                    rev: caseDoc._rev,
                    key: caseDoc._id,
                    value: {
                        rev: caseDoc._rev
                    },
                    doc: {
                        _id: caseDoc._id,
                        _rev: caseDoc._rev,
                        date_created: caseDoc.date_created,
                        created_by: caseDoc.created_by,
                        date_last_updated: caseDoc.date_last_updated,
                        last_updated_by: caseDoc.last_updated_by,
                        record_id: caseDoc.record_id,
                        // Add other fields commonly used in case view
                        home_record: caseDoc.home_record,
                        case_narrative: caseDoc.case_narrative
                    }
                };
                cases.push(caseViewItem);
            }
            
            // If no cases in memory, try localStorage
            if (cases.length === 0) {
                const offlineKeys = Object.keys(localStorage).filter(key => key.startsWith('mmria_offline_case_'));
                for (const key of offlineKeys) {
                    try {
                        const caseData = JSON.parse(localStorage.getItem(key));
                        if (caseData) {
                            const caseViewItem = {
                                id: caseData._id,
                                rev: caseData._rev,
                                key: caseData._id,
                                value: {
                                    rev: caseData._rev
                                },
                                doc: {
                                    _id: caseData._id,
                                    _rev: caseData._rev,
                                    date_created: caseData.date_created,
                                    created_by: caseData.created_by,
                                    date_last_updated: caseData.date_last_updated,
                                    last_updated_by: caseData.last_updated_by,
                                    record_id: caseData.record_id,
                                    home_record: caseData.home_record,
                                    case_narrative: caseData.case_narrative
                                }
                            };
                            cases.push(caseViewItem);
                        }
                    } catch (e) {
                        console.error('Failed to parse cached case:', e);
                    }
                }
            }
            
            console.log(`Serving ${cases.length} cases from offline cache for case_view API`);
            
            // Return in the format expected by case_view API
            return {
                total_rows: cases.length,
                offset: 0,
                rows: cases
            };
        }
        return null; // Let normal API call proceed
    }
    
    async interceptCaseApi(caseId) {
        if (this.isOfflineMode && !navigator.onLine) {
            console.log(`Intercepting individual case API call for case ${caseId}`);
            
            // First try memory cache
            if (this.offlineCases.has(caseId)) {
                const caseDoc = this.offlineCases.get(caseId);
                console.log(`Serving case ${caseId} from memory cache`);
                return caseDoc;
            }
            
            // Then try localStorage
            const cachedCase = localStorage.getItem(`mmria_offline_case_${caseId}`);
            if (cachedCase) {
                try {
                    const caseData = JSON.parse(cachedCase);
                    console.log(`Serving case ${caseId} from localStorage`);
                    return caseData;
                } catch (e) {
                    console.error(`Failed to parse cached case ${caseId}:`, e);
                }
            }
            
            console.log(`Case ${caseId} not found in offline cache`);
        }
        return null; // Let normal API call proceed
    }

    // Cache API responses when online
    cacheJurisdictionTree(data) {
        localStorage.setItem('mmria_cached_jurisdiction_tree', JSON.stringify(data));
        console.log('Cached jurisdiction tree data');
    }
    
    cacheValidationData(version, data) {
        const cacheKey = `mmria_cached_validation_${version}`;
        localStorage.setItem(cacheKey, JSON.stringify(data));
        console.log(`Cached validation data for version ${version}`);
    }

    // Service Worker Management
    async registerServiceWorker() {
        if ('serviceWorker' in navigator) {
            try {
                const registration = await navigator.serviceWorker.register('/sw.js');
                console.log('Service Worker registered successfully:', registration.scope);
                
                // Handle service worker updates
                registration.addEventListener('updatefound', () => {
                    const newWorker = registration.installing;
                    if (newWorker) {
                        console.log('Service Worker: New version installing...');
                        newWorker.addEventListener('statechange', () => {
                            if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                                console.log('Service Worker: New version available, will refresh on next page load');
                                // Optionally show a notification to user about update
                                this.showNotification('Application updated. Refresh page for latest version.', 'info');
                            }
                        });
                    }
                });
                
                // Wait for service worker to be ready
                await navigator.serviceWorker.ready;
                this.serviceWorker = navigator.serviceWorker;
                
                // Listen for messages from service worker
                navigator.serviceWorker.addEventListener('message', this.handleServiceWorkerMessage.bind(this));
                
                // Check if we already have cached files
                await this.checkCacheStatus();
                
            } catch (error) {
                console.error('Service Worker registration failed:', error);
            }
        } else {
            console.warn('Service Worker not supported in this browser');
        }
    }

    handleServiceWorkerMessage(event) {
        try {
            const { type, data } = event.data || {};
            
            if (!type) {
                console.warn('Service worker message missing type:', event.data);
                return;
            }
            
            switch (type) {
                case 'CACHE_PROGRESS':
                    this.updateCachingProgress(data || {});
                    break;
                case 'CACHE_COMPLETE':
                    this.onCachingComplete(data || {});
                    break;
                case 'CACHE_ERROR':
                    this.onCachingError(data || {});
                    break;
                case 'CACHE_CLEARED':
                    this.onCacheCleared(data || {});
                    break;
                case 'CACHE_CLEAR_ERROR':
                    this.onCacheClearError(data || {});
                    break;
                case 'CACHE_STATUS':
                    this.onCacheStatus(data || {});
                    break;
                case 'DEBUG_CACHE':
                    this.onDebugCache(data || {});
                    break;
                default:
                    console.warn('Unknown service worker message type:', type);
                    break;
            }
        } catch (error) {
            console.error('Error handling service worker message:', error, event.data);
        }
    }

    async checkCacheStatus() {
        if (this.serviceWorker && this.serviceWorker.controller) {
            const messageChannel = new MessageChannel();
            
            return new Promise((resolve) => {
                messageChannel.port1.onmessage = (event) => {
                    if (event.data.type === 'CACHE_STATUS') {
                        this.onCacheStatus(event.data.data);
                        resolve();
                    }
                };
                
                this.serviceWorker.controller.postMessage(
                    { type: 'GET_CACHE_STATUS' },
                    [messageChannel.port2]
                );
            });
        }
    }

    async startCaching() {
        if (this.serviceWorker && this.serviceWorker.controller && !this.cachingInProgress) {
            this.cachingInProgress = true;
            this.showCachingProgress(0, 0);
            
            // Get the release version from global variable or detect it dynamically
            const releaseVersion = typeof g_release_version !== 'undefined' 
                ? g_release_version 
                : await this.getReleaseVersion();
            
            this.serviceWorker.controller.postMessage({ 
                type: 'CACHE_FILES',
                releaseVersion: releaseVersion
            });
        }
    }

    async clearCache() {
        if (this.serviceWorker && this.serviceWorker.controller) {
            this.serviceWorker.controller.postMessage({ type: 'CLEAR_CACHE' });
        }
    }

    // Helper method to safely parse JSON from response
    async safeParseJsonResponse(response) {
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            try {
                return await response.json();
            } catch (error) {
                const text = await response.text();
                console.error('Failed to parse JSON response:', error);
                console.error('Response text:', text.substring(0, 200));
                throw new Error(`Invalid JSON response: ${error.message}`);
            }
        } else {
            const text = await response.text();
            console.error('Response is not JSON, content-type:', contentType);
            console.error('Response text:', text.substring(0, 200));
            throw new Error(`Expected JSON response but got ${contentType}`);
        }
    }

    // Helper method to handle different response types
    async handleApiResponse(response, url) {
        const contentType = response.headers.get('content-type');
        
        if (contentType && contentType.includes('application/json')) {
            try {
                return await response.json();
            } catch (error) {
                const text = await response.text();
                console.error('Failed to parse JSON response:', error);
                console.error('Response text:', text.substring(0, 200));
                throw new Error(`Invalid JSON response: ${error.message}`);
            }
        } else if (contentType && (contentType.includes('application/javascript') || contentType.includes('text/javascript'))) {
            // Handle JavaScript responses by adding them to the DOM
            const jsCode = await response.text();
            console.log(`Detected JavaScript response from ${url}, adding to DOM`);
            
            try {
                // Create a script element and add the JavaScript code
                const scriptElement = document.createElement('script');
                scriptElement.type = 'text/javascript';
                scriptElement.textContent = jsCode;
                
                // Add a data attribute to identify this as dynamically loaded
                scriptElement.setAttribute('data-source', 'offline-api');
                scriptElement.setAttribute('data-url', url);
                
                // Add to head
                document.head.appendChild(scriptElement);
                
                console.log(`Successfully added JavaScript from ${url} to DOM`);
                return { type: 'javascript', executed: true, url: url };
            } catch (error) {
                console.error(`Error executing JavaScript from ${url}:`, error);
                throw new Error(`Failed to execute JavaScript: ${error.message}`);
            }
        } else {
            const text = await response.text();
            console.warn(`Unexpected content type ${contentType} from ${url}`);
            console.warn('Response text:', text.substring(0, 200));
            return { type: 'text', content: text };
        }
    }

    async getReleaseVersion() {
        try {
            const response = await fetch(`${location.protocol}//${location.host}/api/version/release-version`);
            if (response.ok) {
                return await response.text();
            }
        } catch (error) {
            console.warn('Could not fetch release version:', error);
        }
        // Fallback version if we can't determine it dynamically
        return '25.06.16';
    }

    async debugCache() {
        if (this.serviceWorker && this.serviceWorker.controller) {
            const messageChannel = new MessageChannel();
            
            return new Promise((resolve) => {
                messageChannel.port1.onmessage = (event) => {
                    if (event.data.type === 'DEBUG_CACHE') {
                        resolve(event.data.data);
                    }
                };
                
                this.serviceWorker.controller.postMessage(
                    { type: 'DEBUG_CACHE' },
                    [messageChannel.port2]
                );
            });
        }
        return null;
    }

    onDebugCache(data) {
        console.log('Cache Debug Info:', data);
        if (data.hasValidationApi) {
            console.log('✅ Validation API is cached:', data.validationApis);
        } else {
            console.log('❌ Validation API NOT found in cache');
        }
        console.log('All cached URLs:', data.cachedUrls);
    }

    showCachingProgress(cached, total) {
        const progressHtml = `
            <div id="caching-progress" class="alert alert-info">
                <i class="fa fa-download"></i> Caching files for offline use... 
                ${total > 0 ? `${cached}/${total}` : 'Preparing...'}
                <div class="progress mt-2">
                    <div class="progress-bar" role="progressbar" style="width: ${total > 0 ? (cached/total)*100 : 0}%" 
                         aria-valuenow="${cached}" aria-valuemin="0" aria-valuemax="${total}"></div>
                </div>
            </div>
        `;
        
        const container = document.getElementById('notifications');
        if (container) {
            container.innerHTML = progressHtml;
        }
    }

    updateCachingProgress(data) {
        const { cached, total, completed } = data;
        this.showCachingProgress(cached, total);
        
        if (completed) {
            console.log('Caching progress complete');
        }
    }

    onCachingComplete(data) {
        try {
            console.log('onCachingComplete received data:', data);
            this.cachingInProgress = false;
            const { cached = 0, failed = 0, failedUrls = [] } = data || {};
            
            console.log(`Caching complete: cached=${cached}, failed=${failed}, failedUrls.length=${Array.isArray(failedUrls) ? failedUrls.length : 'not array'}`);
            
            const container = document.getElementById('notifications');
            if (container) {
                container.innerHTML = '';
            }
            
            let message = `Successfully cached ${cached} files for offline use.`;
            if (failed > 0) {
                message += ` ${failed} files failed to cache:`;
                
                // Add the specific failed files to the user message
                if (Array.isArray(failedUrls) && failedUrls.length > 0) {
                    try {
                        const failedFilesList = failedUrls
                            .filter(item => item && (item.url || item.error)) // Filter out invalid items
                            .map(item => {
                                const url = item.url || 'Unknown URL';
                                const error = item.error || 'Unknown error';
                                return `• ${url} (${error})`;
                            })
                            .join('\n');
                        
                        if (failedFilesList) {
                            message += '\n\nFailed files:\n' + failedFilesList;
                        }
                    } catch (mapError) {
                        console.error('Error formatting failed files list:', mapError);
                        message += '\n\nFailed files: Unable to display details (see console for raw data)';
                        console.log('Raw failedUrls data:', failedUrls);
                    }
                }
                
                // Log detailed information about failed files
                console.error('Files that failed to cache:');
                if (Array.isArray(failedUrls)) {
                    failedUrls.forEach((item, index) => {
                        if (item && typeof item === 'object') {
                            console.error(`- ${item.url || 'Unknown URL'}: ${item.error || 'Unknown error'}`);
                        } else {
                            console.error(`- Invalid item at index ${index}:`, item);
                        }
                    });
                } else {
                    console.error('failedUrls is not an array:', failedUrls);
                }
                
                // Show more user-friendly message based on common failures
                try {
                    if (Array.isArray(failedUrls) && failedUrls.some(item => item && item.url && item.url.includes('/api/'))) {
                        message += '\n\nNote: Some API endpoints may not be available. This is normal if they require authentication or don\'t exist yet.';
                    }
                } catch (filterError) {
                    console.error('Error checking for API failures:', filterError);
                }
            }
            
            this.showNotification(message, failed > 0 ? 'warning' : 'success');
            
            // Show detailed error information in console for debugging
            if (failed > 0 && Array.isArray(failedUrls) && failedUrls.length > 0) {
                console.group('Cache Failures Detail:');
                failedUrls.forEach((item, index) => {
                    if (item && typeof item === 'object') {
                        console.warn(`Failed to cache: ${item.url || 'Unknown URL'} - ${item.error || 'Unknown error'}`);
                    } else {
                        console.warn(`Invalid failure item at index ${index}:`, item);
                    }
                });
                console.groupEnd();
            }
            
            // Update UI to show cache status
            this.updateOfflineModeUI();
        } catch (error) {
            console.error('Error in onCachingComplete:', error);
            this.showNotification('Error processing cache completion: ' + error.message, 'error');
            this.cachingInProgress = false;
        }
    }

    onCachingError(data) {
        try {
            this.cachingInProgress = false;
            const container = document.getElementById('notifications');
            if (container) {
                container.innerHTML = '';
            }
            
            const errorMessage = (data && data.error) ? data.error : 'Unknown caching error';
            this.showNotification('Error caching files for offline use: ' + errorMessage, 'error');
            console.error('Caching error data:', data);
        } catch (error) {
            console.error('Error in onCachingError:', error);
            this.showNotification('Critical error during cache error handling', 'error');
            this.cachingInProgress = false;
        }
    }

    onCacheCleared(data) {
        this.showNotification('Offline cache cleared successfully', 'success');
        this.updateOfflineModeUI();
    }

    onCacheClearError(data) {
        this.showNotification('Error clearing offline cache: ' + data.error, 'error');
    }

    onCacheStatus(data) {
        const { hasCachedFiles, cachedFilesCount, totalFilesToCache } = data;
        console.log(`Cache status: ${cachedFilesCount}/${totalFilesToCache} files cached`);
        
        // Update UI based on cache status
        this.updateCacheStatusUI(hasCachedFiles, cachedFilesCount, totalFilesToCache);
    }

    updateCacheStatusUI(hasCachedFiles, cachedCount, totalCount) {
        const description = document.getElementById('offline-mode-description');
        if (description && hasCachedFiles) {
            const percentage = Math.round((cachedCount / totalCount) * 100);
            description.innerHTML = `
                Files cached for offline use: ${cachedCount}/${totalCount} (${percentage}%)<br>
                <small class="text-muted">Click to switch to offline mode. In offline mode, only cached resources and offline cases will be accessible.</small>
            `;
        }
        
        // Also update the dedicated cache status display if it exists
        const cacheStatusDisplay = document.getElementById('cache-status-display');
        if (cacheStatusDisplay) {
            if (hasCachedFiles) {
                const percentage = Math.round((cachedCount / totalCount) * 100);
                cacheStatusDisplay.innerHTML = `
                    <span class="sync-status synced">
                        <i class="fa fa-check-circle"></i> ${cachedCount}/${totalCount} files cached (${percentage}%)
                    </span>
                `;
            } else {
                cacheStatusDisplay.innerHTML = `
                    <span class="sync-status pending">
                        <i class="fa fa-exclamation-circle"></i> No files cached - enter offline mode to cache
                    </span>
                `;
            }
        }
    }

    // Global fetch interceptor for offline mode
    setupFetchInterceptor() {
        if (this.originalFetch) {
            return; // Already set up
        }
        
        this.originalFetch = window.fetch;
        const offlineManager = this;
        
        window.fetch = async function(url, options = {}) {
            // Only intercept GET requests in offline mode
            if (offlineManager.isOfflineMode && !navigator.onLine && (!options.method || options.method === 'GET')) {
                if (typeof url === 'string') {
                    console.log('Intercepting fetch request:', url);
                    
                    // Intercept offline-cases API
                    if (url.includes('/api/offline/offline-cases')) {
                        const interceptedData = await offlineManager.interceptOfflineCasesApi();
                        if (interceptedData !== null) {
                            return new Response(JSON.stringify(interceptedData), {
                                status: 200,
                                headers: { 'Content-Type': 'application/json' }
                            });
                        }
                    }
                    
                    // Intercept jurisdiction_tree API
                    if (url.includes('/api/jurisdiction_tree')) {
                        const interceptedData = await offlineManager.interceptJurisdictionTreeApi();
                        if (interceptedData !== null) {
                            return new Response(JSON.stringify(interceptedData), {
                                status: 200,
                                headers: { 'Content-Type': 'application/json' }
                            });
                        }
                    }
                    
                    // Intercept validation API
                    const validationMatch = url.match(/\/api\/version\/([^\/]+)\/validation/);
                    if (validationMatch) {
                        const version = validationMatch[1];
                        const interceptedData = await offlineManager.interceptValidationApi(version);
                        if (interceptedData !== null) {
                            return new Response(JSON.stringify(interceptedData), {
                                status: 200,
                                headers: { 'Content-Type': 'application/json' }
                            });
                        }
                    }
                    
                    // Intercept case_view API for main case listing
                    if (url.includes('/api/case_view')) {
                        const interceptedData = await offlineManager.interceptCaseViewApi(url);
                        if (interceptedData !== null) {
                            return new Response(JSON.stringify(interceptedData), {
                                status: 200,
                                headers: { 'Content-Type': 'application/json' }
                            });
                        }
                    }
                    
                    // Intercept individual case API calls
                    const caseMatch = url.match(/\/api\/case\?case_id=([^&]+)/);
                    if (caseMatch) {
                        const caseId = caseMatch[1];
                        const interceptedData = await offlineManager.interceptCaseApi(caseId);
                        if (interceptedData !== null) {
                            return new Response(JSON.stringify(interceptedData), {
                                status: 200,
                                headers: { 'Content-Type': 'application/json' }
                            });
                        }
                    }
                }
            }
            
            // Fall back to original fetch
            return offlineManager.originalFetch.call(window, url, options);
        };
        
        console.log('Fetch interceptor set up for offline mode');
    }
    
    restoreFetchInterceptor() {
        if (this.originalFetch) {
            window.fetch = this.originalFetch;
            delete this.originalFetch;
            console.log('Fetch interceptor restored');
        }
    }

    // jQuery AJAX interceptor for offline mode
    setupJQueryInterceptor() {
        if (!window.$ || !window.$.ajax || this.originalAjax) {
            return; // jQuery not available or already set up
        }
        
        this.originalAjax = window.$.ajax;
        const offlineManager = this;
        
        window.$.ajax = function(settings) {
            // Handle both $.ajax(url, settings) and $.ajax(settings) formats
            if (typeof settings === 'string') {
                settings = arguments[1] || {};
                settings.url = arguments[0];
            }
            settings = settings || {};
            
            // Only intercept GET requests in offline mode
            const method = (settings.method || settings.type || 'GET').toUpperCase();
            if (offlineManager.isOfflineMode && !navigator.onLine && method === 'GET') {
                console.log('Intercepting jQuery AJAX request:', settings.url);
                
                // Intercept jurisdiction_tree API
                if (settings.url && settings.url.includes('/api/jurisdiction_tree')) {
                    return new Promise(async (resolve, reject) => {
                        try {
                            const interceptedData = await offlineManager.interceptJurisdictionTreeApi();
                            if (interceptedData !== null) {
                                resolve(interceptedData);
                                return;
                            }
                        } catch (error) {
                            console.error('Error in jQuery interception:', error);
                        }
                        
                        // Fall back to original ajax if interception fails
                        offlineManager.originalAjax.call(window.$, settings).then(resolve).catch(reject);
                    });
                }
                
                // Intercept validation API
                if (settings.url) {
                    const validationMatch = settings.url.match(/\/api\/version\/([^\/]+)\/validation/);
                    if (validationMatch) {
                        const version = validationMatch[1];
                        return new Promise(async (resolve, reject) => {
                            try {
                                const interceptedData = await offlineManager.interceptValidationApi(version);
                                if (interceptedData !== null) {
                                    resolve(interceptedData);
                                    return;
                                }
                            } catch (error) {
                                console.error('Error in jQuery validation interception:', error);
                            }
                            
                            // Fall back to original ajax if interception fails
                            offlineManager.originalAjax.call(window.$, settings).then(resolve).catch(reject);
                        });
                    }
                }
            }
            
            // Fall back to original ajax
            return offlineManager.originalAjax.call(window.$, settings);
        };
        
        console.log('jQuery AJAX interceptor set up for offline mode');
    }
    
    restoreJQueryInterceptor() {
        if (this.originalAjax && window.$) {
            window.$.ajax = this.originalAjax;
            delete this.originalAjax;
            console.log('jQuery AJAX interceptor restored');
        }
    }

    // Enhanced API response caching
    async cacheApiResponsesOnSuccess() {
        // This method should be called when switching to offline mode
        // to pre-cache critical API responses
        
        if (navigator.onLine) {
            try {
                // Use original fetch if available, otherwise use current fetch
                const fetchToUse = this.originalFetch ? 
                    this.originalFetch.bind(window) : 
                    window.fetch.bind(window);
                
                // Cache jurisdiction tree
                console.log('Pre-caching jurisdiction tree...');
                try {
                    const jurisdictionResponse = await fetchToUse('/api/jurisdiction_tree');
                    console.log('Jurisdiction tree response status:', jurisdictionResponse.status);
                    console.log('Jurisdiction tree response headers:', Array.from(jurisdictionResponse.headers.entries()));
                    
                    if (jurisdictionResponse.ok) {
                        const jurisdictionData = await this.handleApiResponse(jurisdictionResponse, '/api/jurisdiction_tree');
                        if (jurisdictionData && typeof jurisdictionData === 'object' && jurisdictionData.type !== 'javascript') {
                            this.cacheJurisdictionTree(jurisdictionData);
                            console.log('Successfully cached jurisdiction tree');
                        } else if (jurisdictionData && jurisdictionData.type === 'javascript') {
                            console.log('JavaScript response handled for jurisdiction tree');
                        }
                    } else {
                        const errorText = await jurisdictionResponse.text();
                        console.error(`Jurisdiction tree API failed: ${jurisdictionResponse.status} ${jurisdictionResponse.statusText}`);
                        console.error('Error response:', errorText.substring(0, 200));
                    }
                } catch (jurisdictionError) {
                    console.error('Error caching jurisdiction tree:', jurisdictionError);
                }
                
                // Cache validation data for current version
                const version = await this.getReleaseVersion();
                console.log(`Pre-caching validation data for version ${version}...`);
                try {
                    const validationResponse = await fetchToUse(`/api/version/${version}/validation`);
                    console.log('Validation response status:', validationResponse.status);
                    console.log('Validation response headers:', Array.from(validationResponse.headers.entries()));
                    
                    if (validationResponse.ok) {
                        const validationData = await this.handleApiResponse(validationResponse, `/api/version/${version}/validation`);
                        if (validationData && typeof validationData === 'object' && validationData.type !== 'javascript') {
                            this.cacheValidationData(version, validationData);
                            console.log('Successfully cached validation data');
                        } else if (validationData && validationData.type === 'javascript') {
                            console.log('JavaScript response handled for validation data');
                        }
                    } else {
                        const errorText = await validationResponse.text();
                        console.error(`Validation API failed: ${validationResponse.status} ${validationResponse.statusText}`);
                        console.error('Error response:', errorText.substring(0, 200));
                    }
                } catch (validationError) {
                    console.error('Error caching validation data:', validationError);
                }
                
                console.log('API response pre-caching completed');
            } catch (error) {
                console.error('Error pre-caching API responses:', error);
            }
        }
    }
}

// Global instance
const offlineManager = new OfflineManager();

// Global functions for UI interaction
function toggleOfflineMode() {
    offlineManager.toggleOfflineMode();
}

function syncAllChanges() {
    offlineManager.syncAllChanges();
}

function checkPendingChanges() {
    offlineManager.checkPendingChanges();
}

function clearOfflineCache() {
    offlineManager.clearOfflineCache();
}

function searchCase() {
    offlineManager.searchCase();
}

function openCase(caseId) {
    window.location.href = `/Case#/find/${caseId}/home_record`;
}

function viewCase(caseId) {
    // Navigate directly to the case page without hash-based routing
    // The case application can handle loading the specific case
    //window.open(`/Case?case_id=${caseId}`, '_blank');
    //window.href = `/Case?case_id=${caseId}`;
    window.location.href ="/Case#/0/home_record"
}

function removeFromOffline(caseId) {
    offlineManager.markCaseOnline(caseId);
}

async function debugOfflineCache() {
    const debug = await offlineManager.debugCache();
    if (debug) {
        console.log('=== Offline Cache Debug ===');
        console.log('Total cached files:', debug.totalCached);
        console.log('Has validation API:', debug.hasValidationApi);
        console.log('Validation APIs:', debug.validationApis);
        console.log('All cached URLs:', debug.cachedUrls);
        
        alert(`Cache Debug:\nTotal Files: ${debug.totalCached}\nValidation API: ${debug.hasValidationApi ? 'YES' : 'NO'}\nCheck console for details.`);
    } else {
        alert('Could not debug cache - service worker not available');
    }
}

// Add keyboard support for search
document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('case-search');
    if (searchInput) {
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                searchCase();
            }
        });
    }
});

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = OfflineManager;
}
