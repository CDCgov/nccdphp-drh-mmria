/**
 * Server Log Viewer Module
 * Provides interface for viewing, filtering, and exporting server logs from CouchDB
 * Loads on page initialization
 */

(function() {
    'use strict';

    // Module state
    let allLogs = [];
    let filteredLogs = [];
    let metadata = {
        modules: [],
        sessionIds: [],
        userNames: []
    };
    let currentFilters = {
        level: 'all',
        module: 'all',
        sessionId: 'all',
        userName: 'all',
        searchText: '',
        startDate: null,
        endDate: null
    };
    let isLoading = false;

    // Initialize on page load
    function initialize() {
        console.log('Server Log Viewer: Initializing...');
        
        buildInterface();
        attachEventListeners();
        loadMetadata();
        
        console.log('Server Log Viewer: Initialized');
    }

    // Build the log viewer interface
    function buildInterface() {
        const container = document.getElementById('log-viewer-container');
        if (!container) {
            console.error('Server Log Viewer: Container #log-viewer-container not found');
            return;
        }

        container.innerHTML = `
            <!-- Filter Controls -->
            <div class="card mb-3">
                <div class="card-body">
                    <div class="horizontal-control">
                        <div class="vertical-control col-md-2">
                            <label for="log-level-filter">Level:</label>
                            <select id="log-level-filter" class="form-control">
                                <option value="all">All</option>
                                <option value="log">Log</option>
                                <option value="info">Info</option>
                                <option value="warn">Warning</option>
                                <option value="error">Error</option>
                            </select>
                        </div>
                        
                        <div class="vertical-control col-md-3">
                            <label for="log-module-filter">Module:</label>
                            <select id="log-module-filter" class="form-control">
                                <option value="all">All</option>
                            </select>
                        </div>
                        
                        <div class="vertical-control col-md-3">
                            <label for="log-user-filter">User:</label>
                            <select id="log-user-filter" class="form-control">
                                <option value="all">All</option>
                            </select>
                        </div>
                    </div>
                    
                    <div class="horizontal-control">
                        <div class="vertical-control col-md-4">
                            <label for="log-session-filter">Session ID:</label>
                            <select id="log-session-filter" class="form-control">
                                <option value="all">All</option>
                            </select>
                        </div>
                        
                        <div class="vertical-control col-md-8">
                            <label for="log-search-filter">Search:</label>
                            <input type="text" id="log-search-filter" class="form-control" placeholder="Search messages..." />
                        </div>
                    </div>
                    
                    <div class="horizontal-control align-items-end">
                        <div class="vertical-control col-md-3">
                            <label for="log-start-date">Start Date:</label>
                            <input type="datetime-local" id="log-start-date" class="form-control" />
                        </div>
                        
                        <div class="vertical-control col-md-3">
                            <label for="log-end-date">End Date:</label>
                            <input type="datetime-local" id="log-end-date" class="form-control" />
                        </div>
                        
                        <div class="vertical-control col-md-6">
                            <button type="button" id="log-apply-filters" class="btn btn-primary mr-2">Apply Filters</button>
                            <button type="button" id="log-reset-filters" class="btn btn-outline-secondary mr-2">Reset</button>
                            <button type="button" id="log-refresh" class="btn btn-outline-secondary">🔄 Refresh</button>
                        </div>
                    </div>
                </div>
            </div>
            
            <!-- Log Statistics -->
            <div class="d-flex mb-2 text-muted small">
                <span id="log-total-count" class="fw-semibold mr-3">Total: 0</span>
                <span id="log-filtered-count" class="fw-semibold mr-3">Showing: 0</span>
                <span id="log-loading-indicator" class="text-primary fw-semibold d-none">⏳ Loading...</span>
            </div>
            
            <!-- Log Display -->
            <div class="vertical-control col-md-12 mb-4">
                <table class="table">
                    <thead>
                        <tr class="header-level-top-white">
                            <th colspan="8">Server Logs</th>
                        </tr>
                        <tr class="header-level-2">
                            <th>Timestamp</th>
                            <th>Level</th>
                            <th>Status</th>
                            <th>Module</th>
                            <th>User</th>
                            <th>Location</th>
                            <th>Function</th>
                            <th>Message</th>
                        </tr>
                    </thead>
                    <tbody id="log-tbody">
                        <tr>
                            <td colspan="8" class="text-center py-4 text-muted">
                               Click apply filters or refresh to load logs
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
            
            <!-- Export Actions -->
            <div class="mt-3">
                <button type="button" id="log-export-json" class="btn btn-outline-secondary mr-2">Export as JSON</button>
                <button type="button" id="log-export-csv" class="btn btn-outline-secondary">Export as CSV</button>
            </div>
            
            <style>
                .log-level-badge {
                    padding: 2px 8px;
                    border-radius: 3px;
                    font-weight: 600;
                    font-size: 11px;
                    text-transform: uppercase;
                    display: inline-block;
                }
                
                .log-level-log {
                    background-color: #e7f3ff;
                    color: #004085;
                }
                
                .log-level-info {
                    background-color: #d1ecf1;
                    color: #0c5460;
                }
                
                .log-level-warn {
                    background-color: #fff3cd;
                    color: #856404;
                }
                
                .log-level-error {
                    background-color: #f8d7da;
                    color: #721c24;
                }
                
                #log-tbody td {
                    vertical-align: top;
                    word-break: break-word;
                }
                
                #log-tbody tr {
                    cursor: pointer;
                }
                
                #log-tbody tr.stack-row {
                    cursor: default;
                }
            </style>
        `;
    }

    // Attach event listeners
    function attachEventListeners() {
        // Filter change handlers
        document.getElementById('log-level-filter').addEventListener('change', (e) => {
            currentFilters.level = e.target.value;
        });
        
        document.getElementById('log-module-filter').addEventListener('change', (e) => {
            currentFilters.module = e.target.value;
        });
        
        document.getElementById('log-user-filter').addEventListener('change', (e) => {
            currentFilters.userName = e.target.value;
        });
        
        document.getElementById('log-session-filter').addEventListener('change', (e) => {
            currentFilters.sessionId = e.target.value;
            
            // Auto-fill related fields when a specific session is chosen
            if (e.target.value !== 'all') {
                const chosen = e.target.options[e.target.selectedIndex];
                const sessionData = chosen.dataset;
                
                // // Populate start date field
                // if (sessionData.dateCreated) {
                //     const startInput = document.getElementById('log-start-date');
                //     startInput.value = convertToInputFormat(new Date(sessionData.dateCreated));
                //     currentFilters.startDate = startInput.value;
                // }
                
                // // Populate end date field
                // if (sessionData.dateUpdated) {
                //     const endInput = document.getElementById('log-end-date');
                //     endInput.value = convertToInputFormat(new Date(sessionData.dateUpdated));
                //     currentFilters.endDate = endInput.value;
                // }
                
            }
        });
        
        document.getElementById('log-search-filter').addEventListener('input', (e) => {
            currentFilters.searchText = e.target.value.toLowerCase();
        });
        
        document.getElementById('log-start-date').addEventListener('change', (e) => {
            currentFilters.startDate = e.target.value || null;
        });
        
        document.getElementById('log-end-date').addEventListener('change', (e) => {
            currentFilters.endDate = e.target.value || null;
        });
        
        // Button handlers
        document.getElementById('log-apply-filters').addEventListener('click', applyFiltersAndLoad);
        document.getElementById('log-reset-filters').addEventListener('click', resetFilters);
        document.getElementById('log-refresh').addEventListener('click', () => {
            loadMetadata();
            loadLogs();
        });
        document.getElementById('log-export-json').addEventListener('click', exportAsJSON);
        document.getElementById('log-export-csv').addEventListener('click', exportAsCSV);
    }

    // Load metadata (distinct lists for dropdowns)
    async function loadMetadata() {
        try {
            showLoading(true);
            
            const response = await fetch('/api/logger/metadata');
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            metadata = await response.json();
            
            populateDropdowns();
            loadLogs();
            
            console.log('Server Log Viewer: Metadata loaded', metadata);
        } catch (error) {
            console.error('Server Log Viewer: Error loading metadata', error);
            showError('Failed to load metadata: ' + error.message);
        } finally {
            showLoading(false);
        }
    }

    // Populate filter dropdowns with metadata
    function populateDropdowns() {
        // Modules
        const moduleFilter = document.getElementById('log-module-filter');
        const currentModule = moduleFilter.value;
        moduleFilter.innerHTML = '<option value="all">All</option>';
        metadata.modules.forEach(module => {
            const option = document.createElement('option');
            option.value = module;
            option.textContent = module;
            moduleFilter.appendChild(option);
        });
        if (metadata.modules.includes(currentModule)) {
            moduleFilter.value = currentModule;
        }
        
        // Users
        const userFilter = document.getElementById('log-user-filter');
        const currentUser = userFilter.value;
        userFilter.innerHTML = '<option value="all">All</option>';
        metadata.userNames.forEach(userName => {
            const option = document.createElement('option');
            option.value = userName;
            option.textContent = userName;
            userFilter.appendChild(option);
        });
        if (metadata.userNames.includes(currentUser)) {
            userFilter.value = currentUser;
        }
        
        // Session IDs
        const sessionFilter = document.getElementById('log-session-filter');
        const currentSession = sessionFilter.value;
        sessionFilter.innerHTML = '<option value="all">All</option>';
        metadata.sessionIds.forEach(sessionItem => {
            const option = document.createElement('option');
            option.value = sessionItem.value;
            option.textContent = sessionItem.name;
            
            // Store metadata using dataset API
            option.dataset.dateCreated = sessionItem.dateCreated || '';
            option.dataset.dateUpdated = sessionItem.dateLastUpdated || '';
            option.dataset.state = sessionItem.offlineState || '';
            option.dataset.hasLogs = sessionItem.hasLogData || false;
            // Make option bold if it has log data
            if (sessionItem.hasLogData === true) {
                option.style.fontWeight = 'bold';
            }            
            sessionFilter.appendChild(option);
        });
        // Check if current session is still in the list
        const sessionValues = metadata.sessionIds.map(s => s.value);
        if (sessionValues.includes(currentSession)) {
            sessionFilter.value = currentSession;
        }
    }

    // Load logs from server with current filters
    async function loadLogs() {
        if (isLoading) return;
        
        try {
            showLoading(true);
            
            // Build query parameters
            const params = new URLSearchParams();
            if (currentFilters.level !== 'all') params.append('level', currentFilters.level);
            if (currentFilters.module !== 'all') params.append('context', currentFilters.module);
            if (currentFilters.userName !== 'all') params.append('userName', currentFilters.userName);
            if (currentFilters.sessionId !== 'all') params.append('sessionId', currentFilters.sessionId);
            if (currentFilters.searchText) params.append('search', currentFilters.searchText);
            if (currentFilters.startDate) params.append('startDate', currentFilters.startDate);
            if (currentFilters.endDate) params.append('endDate', currentFilters.endDate);
            //params.append('limit', '1000');
            
            const response = await fetch('/api/logger/get-logs?' + params.toString());
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
        const data = await response.json();
        allLogs = data.logs || [];
        
        // Sort logs by timestamp in descending order (newest first)
        allLogs.sort((a, b) => {
            const dateA = a.timestamp ? new Date(a.timestamp) : new Date(0);
            const dateB = b.timestamp ? new Date(b.timestamp) : new Date(0);
            return dateA - dateB; // Ascending order
        });
        
        filteredLogs = allLogs; // Server-side filtering
        
            renderLogs();
            updateStats();
            
            console.log('Server Log Viewer: Loaded', allLogs.length, 'logs');
        } catch (error) {
            console.error('Server Log Viewer: Error loading logs', error);
            showError('Failed to load logs: ' + error.message);
        } finally {
            showLoading(false);
        }
    }

    // Apply filters and reload logs
    function applyFiltersAndLoad() {
        loadLogs();
    }

    // Reset all filters
    function resetFilters() {
        currentFilters = {
            level: 'all',
            module: 'all',
            sessionId: 'all',
            userName: 'all',
            searchText: '',
            startDate: null,
            endDate: null
        };
        
        document.getElementById('log-level-filter').value = 'all';
        document.getElementById('log-module-filter').value = 'all';
        document.getElementById('log-user-filter').value = 'all';
        document.getElementById('log-session-filter').value = 'all';
        document.getElementById('log-search-filter').value = '';
        document.getElementById('log-start-date').value = '';
        document.getElementById('log-end-date').value = '';
        
        loadLogs();
        
        console.log('Server Log Viewer: Filters reset');
    }

    // Get offline status badge HTML
    function getOfflineStatus(log) {
        const isOffline = log.is_offline === 'true';
        const isProcessing = log.process_offline_cases === 'true';
        
        if (!isOffline) {
            return '<span style="padding: 2px 8px; border-radius: 3px; font-weight: 600; font-size: 11px; background-color: #d4edda; color: #155724; display: inline-block;">ONLINE</span>';
        } else if (isProcessing) {
            return '<span style="padding: 2px 8px; border-radius: 3px; font-weight: 600; font-size: 11px; background-color: #fff3cd; color: #856404; display: inline-block;">PROCESSING</span>';
        } else {
            return '<span style="padding: 2px 8px; border-radius: 3px; font-weight: 600; font-size: 11px; background-color: #cce5ff; color: #004085; display: inline-block;">OFFLINE</span>';
        }
    }

    // Render logs in table
    function renderLogs() {
        const tbody = document.getElementById('log-tbody');
        
        if (filteredLogs.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="8" style="text-align: center; padding: 20px; color: #666;">
                        No logs to display
                    </td>
                </tr>
            `;
            return;
        }
        
        tbody.innerHTML = filteredLogs.map((log, index) => {
            const timestamp = log.timestamp ? new Date(log.timestamp).toLocaleString('en-US', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: false
            }) : '';
            
            const levelClass = `log-level-badge log-level-${log.level || 'log'}`;
            const message = escapeHtml(log.message || '');
            const moduleName = log.context || '';
            const userName = log.user_name || '';
            
            // Format location (file:line:column)
            let location = '';
            if (log.fileName) {
                location = log.fileName;
                if (log.lineNumber) {
                    location += `:${log.lineNumber}`;
                    if (log.columnNumber) {
                        location += `:${log.columnNumber}`;
                    }
                }
            }
            
            // Format function name with error type if present
            let functionInfo = log.functionName || '';
            if (log.errorType && log.errorType !== 'null') {
                functionInfo = log.errorType + (functionInfo ? ` (${functionInfo})` : '');
            }
            
            // Create expandable row for stack trace if present
            const hasStackTrace = log.stackTrace && log.stackTrace !== 'null';
            const rowId = `log-${index}`;
            const stackRowId = `stack-${index}`;
            const statusBadge = getOfflineStatus(log);
            
            let html = `
                <tr id="${rowId}" ${hasStackTrace ? `onclick="window.ServerLogViewer.toggleStackTrace('${stackRowId}')"` : ''}>
                    <td style="padding: 8px 10px;">${timestamp}</td>
                    <td style="padding: 8px 10px;"><span class="${levelClass}">${log.level || 'log'}</span></td>
                    <td style="padding: 8px 10px;">${statusBadge}</td>
                    <td style="padding: 8px 10px;">${escapeHtml(moduleName)}</td>
                    <td style="padding: 8px 10px;">${escapeHtml(userName)}</td>
                    <td style="padding: 8px 10px; font-family: monospace; font-size: 12px;">${escapeHtml(location)}</td>
                    <td style="padding: 8px 10px; font-family: monospace; font-size: 12px;">${escapeHtml(functionInfo)}</td>
                    <td style="padding: 8px 10px;">${message} ${hasStackTrace ? '<span style="color: #7b2d8e; font-weight: 600; margin-left: 8px;">📋 Stack</span>' : ''}</td>
                </tr>
            `;
            
            // Add hidden stack trace row
            if (hasStackTrace) {
                const stackHtml = escapeHtml(log.stackTrace).replace(/\n/g, '<br>');
                html += `
                    <tr id="${stackRowId}" class="stack-row" style="display: none; background-color: #f8f9fa;">
                        <td colspan="8" style="padding: 12px; font-family: monospace; font-size: 11px; white-space: pre-wrap; border-top: 1px dashed #dee2e6;">
                            <strong style="color: #721c24;">Stack Trace:</strong><br>${stackHtml}
                        </td>
                    </tr>
                `;
            }
            
            return html;
        }).join('');
    }

    // Toggle stack trace visibility
    function toggleStackTrace(stackRowId) {
        const stackRow = document.getElementById(stackRowId);
        if (stackRow) {
            stackRow.style.display = stackRow.style.display === 'none' ? '' : 'none';
        }
    }

    // Update statistics display
    function updateStats() {
        document.getElementById('log-total-count').textContent = `Total: ${allLogs.length}`;
        document.getElementById('log-filtered-count').textContent = `Showing: ${filteredLogs.length}`;
    }

    // Show/hide loading indicator
    function showLoading(loading) {
        isLoading = loading;
        const indicator = document.getElementById('log-loading-indicator');
        if (indicator) {
            if (loading) {
                indicator.classList.remove('d-none');
            } else {
                indicator.classList.add('d-none');
            }
        }
    }

    // Show error message
    function showError(message) {
        const container = document.getElementById('log-viewer-container');
        if (container) {
            const errorDiv = document.createElement('div');
            errorDiv.className = 'error-message';
            errorDiv.textContent = message;
            container.insertBefore(errorDiv, container.firstChild);
            
            setTimeout(() => {
                errorDiv.remove();
            }, 5000);
        }
    }

    // Export logs as JSON
    function exportAsJSON() {
        const dataStr = JSON.stringify(filteredLogs, null, 2);
        const dataBlob = new Blob([dataStr], { type: 'application/json' });
        downloadFile(dataBlob, `server-logs-${Date.now()}.json`);
        
        console.log('Server Log Viewer: Exported', filteredLogs.length, 'logs as JSON');
    }

    // Export logs as CSV
    function exportAsCSV() {
        const headers = ['Timestamp', 'Level', 'Module', 'User', 'FileName', 'LineNumber', 'ColumnNumber', 'FunctionName', 'ErrorType', 'Message', 'StackTrace'];
        const csvRows = [headers.join(',')];
        
        filteredLogs.forEach(log => {
            const row = [
                log.timestamp ? new Date(log.timestamp).toISOString() : '',
                log.level || '',
                escapeCsv(log.context || ''),
                escapeCsv(log.user_name || ''),
                escapeCsv(log.fileName || ''),
                log.lineNumber || '',
                log.columnNumber || '',
                escapeCsv(log.functionName || ''),
                escapeCsv(log.errorType || ''),
                escapeCsv(log.message || ''),
                escapeCsv(log.stackTrace || '')
            ];
            csvRows.push(row.join(','));
        });
        
        const csvContent = csvRows.join('\n');
        const dataBlob = new Blob([csvContent], { type: 'text/csv' });
        downloadFile(dataBlob, `server-logs-${Date.now()}.csv`);
        
        console.log('Server Log Viewer: Exported', filteredLogs.length, 'logs as CSV');
    }

    // Download file helper
    function downloadFile(blob, filename) {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }

    // Convert Date object to datetime-local input format
    function convertToInputFormat(dateObj) {
        const pad = (num) => String(num).padStart(2, '0');
        return `${dateObj.getFullYear()}-${pad(dateObj.getMonth() + 1)}-${pad(dateObj.getDate())}T${pad(dateObj.getHours())}:${pad(dateObj.getMinutes())}`;
    }
    
    // Escape HTML to prevent XSS
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    
    // Escape CSV fields
    function escapeCsv(text) {
        if (text == null) return '';
        const str = String(text);
        // Enclose in quotes if contains comma, newline, or quotes
        if (str.includes(',') || str.includes('\n') || str.includes('"')) {
            return `"${str.replace(/"/g, '""')}"`;
        }
        return str;
    }

    // Export public API
    const ServerLogViewer = {
        initialize,
        loadLogs,
        toggleStackTrace,
        exportAsJSON,
        exportAsCSV
    };

    // Expose to global scope
    if (typeof window !== 'undefined') {
        window.ServerLogViewer = ServerLogViewer;
    }

    // Auto-initialize on DOM ready
    if (typeof document !== 'undefined') {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initialize);
        } else {
            initialize();
        }
    }

    console.log('Server Log Viewer: Module loaded');

})();
