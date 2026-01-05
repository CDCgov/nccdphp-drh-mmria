/**
 * Offline Debug Modal Module
 * Provides modal UI for viewing, filtering, and exporting offline logs
 * Keyboard shortcut: Ctrl+Shift+L to toggle modal
 */

(function() {
    'use strict';

    // Module state
    let isModalVisible = false;
    let allLogs = [];
    let filteredLogs = [];
    let currentFilters = {
        level: 'all',
        module: 'all',
        sessionId: 'all',
        searchText: '',
        startDate: null,
        endDate: null
    };

    // Create modal HTML structure
    function createModalHTML() {
        const modalHtml = `
            <div id="offline-debug-modal" class="modal fade" tabindex="-1" role="dialog" style="z-index: 1050;">
                <div class="modal-dialog" role="document" style="max-width: 95%; width: 1400px;">
                    <div class="modal-content" style="height: 90vh; display: flex; flex-direction: column;">
                        <div class="modal-header" style="background-color: #7b2d8e; color: white; padding: 7px;">
                            <h4 class="modal-title" style="margin: 0; font-weight: 600; font-size:17px;">Offline Debug Logs</h4>
                            <button type="button" class="close" onclick="window.OfflineDebugModal.hide()" style="color: white; opacity: 1; font-size: 28px; background: none; border: none; cursor: pointer;">
                                <span aria-hidden="true">&times;</span>
                            </button>
                        </div>
                        
                        <div class="modal-body" style="padding: 20px; overflow: hidden; display: flex; flex-direction: column; flex: 1;">
                            <!-- Filter Controls -->
                            <div style="background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 4px; padding: 15px; margin-bottom: 15px;">
                                <div style="display: flex; gap: 15px; margin-bottom: 10px; flex-wrap: wrap;">
                                    <div style="flex: 1; min-width: 150px;">
                                        <label for="debug-level-filter" style="display: block; font-weight: 600; font-size: 13px; margin-bottom: 5px;">Level:</label>
                                        <select id="debug-level-filter" class="form-control" style="font-size: 14px;">
                                            <option value="all">All</option>
                                            <option value="log">Log</option>
                                            <option value="info">Info</option>
                                            <option value="warn">Warning</option>
                                            <option value="error">Error</option>
                                        </select>
                                    </div>
                                    
                                    <div style="flex: 1; min-width: 150px;">
                                        <label for="debug-module-filter" style="display: block; font-weight: 600; font-size: 13px; margin-bottom: 5px;">Module:</label>
                                        <select id="debug-module-filter" class="form-control" style="font-size: 14px;">
                                            <option value="all">All</option>
                                        </select>
                                    </div>
                                    
                                    <div style="flex: 1; min-width: 200px;">
                                        <label for="debug-session-filter" style="display: block; font-weight: 600; font-size: 13px; margin-bottom: 5px;">Session ID:</label>
                                        <select id="debug-session-filter" class="form-control" style="font-size: 14px;">
                                            <option value="all">All</option>
                                        </select>
                                    </div>
                                    
                                    <div style="flex: 2; min-width: 200px;">
                                        <label for="debug-search-filter" style="display: block; font-weight: 600; font-size: 13px; margin-bottom: 5px;">Search:</label>
                                        <input type="text" id="debug-search-filter" class="form-control" placeholder="Search logs..." style="font-size: 14px;" />
                                    </div>
                                </div>
                                
                                <div style="display: flex; gap: 15px; flex-wrap: wrap; align-items: flex-end;">
                                    <div style="flex: 1; min-width: 150px;">
                                        <label for="debug-start-date" style="display: block; font-weight: 600; font-size: 13px; margin-bottom: 5px;">Start Date:</label>
                                        <input type="datetime-local" id="debug-start-date" class="form-control" style="font-size: 14px;" />
                                    </div>
                                    
                                    <div style="flex: 1; min-width: 150px;">
                                        <label for="debug-end-date" style="display: block; font-weight: 600; font-size: 13px; margin-bottom: 5px;">End Date:</label>
                                        <input type="datetime-local" id="debug-end-date" class="form-control" style="font-size: 14px;" />
                                    </div>
                                    
                                    <div style="display: flex; gap: 8px;">
                                        <button type="button" id="debug-apply-filters" class="btn btn-primary" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 6px 16px;">Apply Filters</button>
                                        <button type="button" id="debug-reset-filters" class="btn btn-light" style="padding: 6px 16px;">Reset</button>
                                    </div>
                                </div>
                            </div>
                            
                            <!-- Log Statistics -->
                            <div style="display: flex; gap: 20px; margin-bottom: 10px; font-size: 13px; color: #666;">
                                <span id="debug-total-count" style="font-weight: 600;">Total: 0</span>
                                <span id="debug-filtered-count" style="font-weight: 600;">Showing: 0</span>
                            </div>
                            
                            <!-- Log Display -->
                            <div style="flex: 1; overflow: auto; border: 1px solid #dee2e6; border-radius: 4px; background-color: #fff;">
                                <table class="table table-sm table-hover" style="margin-bottom: 0; font-size: 13px;">
                                    <thead style="position: sticky; top: 0; background-color: #f8f9fa; z-index: 1;">
                                        <tr>
                                            <th style="width: 140px; padding: 10px; border-bottom: 2px solid #dee2e6;">Timestamp</th>
                                            <th style="width: 80px; padding: 10px; border-bottom: 2px solid #dee2e6;">Level</th>
                                            <th style="width: 100px; padding: 10px; border-bottom: 2px solid #dee2e6;">Status</th>
                                            <th style="width: 130px; padding: 10px; border-bottom: 2px solid #dee2e6;">Module</th>
                                            <th style="width: 140px; padding: 10px; border-bottom: 2px solid #dee2e6;">Location</th>
                                            <th style="width: 120px; padding: 10px; border-bottom: 2px solid #dee2e6;">Function</th>
                                            <th style="padding: 10px; border-bottom: 2px solid #dee2e6;">Message</th>
                                        </tr>
                                    </thead>
                                    <tbody id="debug-logs-tbody">
                                        <tr>
                                            <td colspan="7" style="text-align: center; padding: 20px; color: #666;">
                                                No logs to display
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                        
                        <div class="modal-footer" style="padding: 20px 30px; background-color: #f8f9fa; border-top: 1px solid #dee2e6;">
                            <button type="button" id="debug-export-json" class="btn btn-light" style="padding: 8px 20px; margin-right: 10px;">Export as JSON</button>
                            <button type="button" id="debug-export-csv" class="btn btn-light" style="padding: 8px 20px; margin-right: 10px;">Export as CSV</button>
                            <button type="button" id="debug-clear-logs" class="btn btn-danger" style="padding: 8px 20px; margin-right: 10px;">Clear All Logs</button>
                            <button type="button" id="debug-close" class="btn btn-primary" style="background-color: #7b2d8e; border-color: #7b2d8e; padding: 8px 20px;">Close</button>
                        </div>
                    </div>
                </div>
            </div>
            <div id="offline-debug-backdrop" class="modal-backdrop fade" style="z-index: 1040;"></div>
        `;
        
        // Inject custom CSS for log level badges
        const styleHTML = `
            <style>
                .offline-debug-log-level {
                    padding: 2px 8px;
                    border-radius: 3px;
                    font-weight: 600;
                    font-size: 11px;
                    text-transform: uppercase;
                    display: inline-block;
                }
                
                .offline-debug-log-level-log {
                    background-color: #e7f3ff;
                    color: #004085;
                }
                
                .offline-debug-log-level-info {
                    background-color: #d1ecf1;
                    color: #0c5460;
                }
                
                .offline-debug-log-level-warn {
                    background-color: #fff3cd;
                    color: #856404;
                }
                
                .offline-debug-log-level-error {
                    background-color: #f8d7da;
                    color: #721c24;
                }
                
                #debug-logs-tbody td {
                    vertical-align: top;
                    word-break: break-word;
                }
            </style>
        `;
        
        // Append to document
        document.head.insertAdjacentHTML('beforeend', styleHTML);
        document.body.insertAdjacentHTML('beforeend', modalHtml);
    }

    // Initialize modal
    function initialize() {
        if (document.getElementById('offline-debug-modal')) {
            return; // Already initialized
        }
        
        createModalHTML();
        attachEventListeners();       
       
    }

    // Attach event listeners
    function attachEventListeners() {
        // Keyboard shortcut (Ctrl+Shift+L)
        document.addEventListener('keydown', (e) => {
            if (e.ctrlKey && e.shiftKey && e.key === 'L') {
                e.preventDefault();
                toggle();
            }
            // Escape key to close
            if (e.key === 'Escape' && isModalVisible) {
                hide();
            }
        });
        
        // Filter controls
        document.getElementById('debug-level-filter').addEventListener('change', (e) => {
            currentFilters.level = e.target.value;
        });
        
        document.getElementById('debug-module-filter').addEventListener('change', (e) => {
            currentFilters.module = e.target.value;
        });
        
        document.getElementById('debug-session-filter').addEventListener('change', (e) => {
            currentFilters.sessionId = e.target.value;
        });
        
        document.getElementById('debug-search-filter').addEventListener('input', (e) => {
            currentFilters.searchText = e.target.value.toLowerCase();
        });
        
        document.getElementById('debug-start-date').addEventListener('change', (e) => {
            currentFilters.startDate = e.target.value ? new Date(e.target.value) : null;
        });
        
        document.getElementById('debug-end-date').addEventListener('change', (e) => {
            currentFilters.endDate = e.target.value ? new Date(e.target.value) : null;
        });
        
        // Buttons
        document.getElementById('debug-apply-filters').addEventListener('click', applyFilters);
        document.getElementById('debug-reset-filters').addEventListener('click', resetFilters);
        document.getElementById('debug-export-json').addEventListener('click', exportAsJSON);
        document.getElementById('debug-export-csv').addEventListener('click', exportAsCSV);
        document.getElementById('debug-clear-logs').addEventListener('click', clearLogs);
        document.getElementById('debug-close').addEventListener('click', hide);
        
        // Close when clicking outside modal
        document.getElementById('offline-debug-backdrop').addEventListener('click', hide);
    }

    // Show modal
    async function show() {
        const modal = document.getElementById('offline-debug-modal');
        const backdrop = document.getElementById('offline-debug-backdrop');
        
        if (!modal) {
            initialize();
            return show(); // Retry after initialization
        }
        
        modal.style.display = 'block';
        backdrop.style.display = 'block';
        
        // Trigger fade-in effect
        setTimeout(() => {
            modal.classList.add('show');
            backdrop.classList.add('show');
        }, 10);
        
        isModalVisible = true;
        
        await loadLogs();       
      
    }

    // Hide modal
    function hide() {
        const modal = document.getElementById('offline-debug-modal');
        const backdrop = document.getElementById('offline-debug-backdrop');
        
        if (modal && backdrop) {
            modal.classList.remove('show');
            backdrop.classList.remove('show');
            
            setTimeout(() => {
                modal.style.display = 'none';
                backdrop.style.display = 'none';
            }, 150);
        }
        
        isModalVisible = false;        

    }

    // Toggle modal visibility
    function toggle() {
        if (isModalVisible) {
            hide();
        } else {
            show();
        }
    }

    // Load logs from IndexedDB
    async function loadLogs() {
        try {
            allLogs = await offlineLog.getAllLogs();
            populateModuleFilter();
            populateSessionFilter();
            applyFilters();           
           
        } catch (error) {
            offlineLog.error('OfflineDebugModal', 'Error loading logs:', error);
        }
    }

    // Populate module filter dropdown with unique modules
    function populateModuleFilter() {
        const modules = new Set();
        allLogs.forEach(log => {
            const moduleName = log.module || log.context;
            if (moduleName) {
                modules.add(moduleName);
            }
        });
        
        const moduleFilter = document.getElementById('debug-module-filter');
        const currentValue = moduleFilter.value;
        
        // Clear existing options except "All"
        moduleFilter.innerHTML = '<option value="all">All</option>';
        
        // Add sorted module options
        Array.from(modules).sort().forEach(module => {
            const option = document.createElement('option');
            option.value = module;
            option.textContent = module;
            moduleFilter.appendChild(option);
        });
        
        // Restore previous selection if still valid
        if (currentValue !== 'all' && modules.has(currentValue)) {
            moduleFilter.value = currentValue;
        }
    }

    // Populate session ID filter dropdown with unique sessions and their first date
    function populateSessionFilter() {
        const sessions = new Map(); // Map<sessionId, firstTimestamp>
        
        allLogs.forEach(log => {
            const sessionId = log.offline_session_id;
            if (sessionId && sessionId !== 'null') {
                if (!sessions.has(sessionId)) {
                    sessions.set(sessionId, log.timestamp);
                }
            }
        });
        
        const sessionFilter = document.getElementById('debug-session-filter');
        const currentValue = sessionFilter.value;
        
        // Clear existing options except "All"
        sessionFilter.innerHTML = '<option value="all">All</option>';
        
        // Add sorted session options with first date
        Array.from(sessions.entries())
            .sort((a, b) => new Date(b[1]) - new Date(a[1])) // Sort by date, newest first
            .forEach(([sessionId, timestamp]) => {
                const option = document.createElement('option');
                option.value = sessionId;
                const date = new Date(timestamp).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric',
                    hour: '2-digit',
                    minute: '2-digit'
                });
                option.textContent = `${sessionId.substring(0, 8)}... (${date})`;
                sessionFilter.appendChild(option);
            });
        
        // Restore previous selection if still valid
        if (currentValue !== 'all' && sessions.has(currentValue)) {
            sessionFilter.value = currentValue;
        }
    }

    // Apply filters to logs
    function applyFilters() {
        filteredLogs = allLogs.filter(log => {
            // Level filter
            if (currentFilters.level !== 'all' && log.level !== currentFilters.level) {
                return false;
            }
            
            // Module filter
            const moduleName = log.module || log.context;
            if (currentFilters.module !== 'all' && moduleName !== currentFilters.module) {
                return false;
            }
            
            // Session ID filter
            if (currentFilters.sessionId !== 'all') {
                const logSessionId = log.offline_session_id;
                if (!logSessionId || logSessionId !== currentFilters.sessionId) {
                    return false;
                }
            }
            
            // Search text filter
            if (currentFilters.searchText) {
                const searchIn = `${log.module} ${log.message} ${JSON.stringify(log.data || '')}`.toLowerCase();
                if (!searchIn.includes(currentFilters.searchText)) {
                    return false;
                }
            }
            
            // Date range filter
            const logDate = new Date(log.timestamp);
            if (currentFilters.startDate && logDate < currentFilters.startDate) {
                return false;
            }
            if (currentFilters.endDate && logDate > currentFilters.endDate) {
                return false;
            }
            
            return true;
        });
        
        renderLogs();
        updateStats();
    }

    // Reset all filters
    function resetFilters() {
        currentFilters = {
            level: 'all',
            module: 'all',
            sessionId: 'all',
            searchText: '',
            startDate: null,
            endDate: null
        };
        
        document.getElementById('debug-level-filter').value = 'all';
        document.getElementById('debug-module-filter').value = 'all';
        document.getElementById('debug-session-filter').value = 'all';
        document.getElementById('debug-search-filter').value = '';
        document.getElementById('debug-start-date').value = '';
        document.getElementById('debug-end-date').value = '';
        
        applyFilters();
        
        offlineLog.log('OfflineDebugModal', 'Filters reset');
    }

    // Get offline status indicator
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
        const tbody = document.getElementById('debug-logs-tbody');
        
        if (filteredLogs.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="7" style="text-align: center; padding: 20px; color: #666;">
                        No logs to display
                    </td>
                </tr>
            `;
            return;
        }
        
        tbody.innerHTML = filteredLogs.map(log => {
            const timestamp = new Date(log.timestamp).toLocaleString('en-US', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: false
            });
            
            const levelClass = `offline-debug-log-level offline-debug-log-level-${log.level}`;
            const message = escapeHtml(log.message);
            const dataStr = log.data ? escapeHtml(JSON.stringify(log.data)) : '';
            const fullMessage = dataStr ? `${message} ${dataStr}` : message;
            const moduleName = log.module || log.context || '';
            
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
            const rowId = `log-${log.id}`;
            const stackRowId = `stack-${log.id}`;
            const statusBadge = getOfflineStatus(log);
            
            let html = `
                <tr id="${rowId}" ${hasStackTrace ? `style="cursor: pointer;" onclick="window.OfflineDebugModal.toggleStackTrace('${stackRowId}')"` : ''}>
                    <td style="padding: 8px 10px;">${timestamp}</td>
                    <td style="padding: 8px 10px;"><span class="${levelClass}">${log.level}</span></td>
                    <td style="padding: 8px 10px;">${statusBadge}</td>
                    <td style="padding: 8px 10px;">${escapeHtml(moduleName)}</td>
                    <td style="padding: 8px 10px; font-family: monospace; font-size: 12px;">${escapeHtml(location)}</td>
                    <td style="padding: 8px 10px; font-family: monospace; font-size: 12px;">${escapeHtml(functionInfo)}</td>
                    <td style="padding: 8px 10px;">${fullMessage} ${hasStackTrace ? '<span style="color: #7b2d8e; font-weight: 600; margin-left: 8px;">📋 Stack</span>' : ''}</td>
                </tr>
            `;
            
            // Add hidden stack trace row
            if (hasStackTrace) {
                const stackHtml = escapeHtml(log.stackTrace).replace(/\n/g, '<br>');
                html += `
                    <tr id="${stackRowId}" style="display: none; background-color: #f8f9fa;">
                        <td colspan="7" style="padding: 12px; font-family: monospace; font-size: 11px; white-space: pre-wrap; border-top: 1px dashed #dee2e6;">
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
        document.getElementById('debug-total-count').textContent = `Total: ${allLogs.length}`;
        document.getElementById('debug-filtered-count').textContent = `Showing: ${filteredLogs.length}`;
    }

    // Export logs as JSON
    function exportAsJSON() {
        const dataStr = JSON.stringify(filteredLogs, null, 2);
        const dataBlob = new Blob([dataStr], { type: 'application/json' });
        downloadFile(dataBlob, `offline-logs-${Date.now()}.json`);
        
        offlineLog.log('OfflineDebugModal', `Exported ${filteredLogs.length} logs as JSON`);
    }

    // Export logs as CSV
    function exportAsCSV() {
        const headers = ['Timestamp', 'Level', 'Module', 'FileName', 'LineNumber', 'ColumnNumber', 'FunctionName', 'ErrorType', 'Message', 'StackTrace'];
        const csvRows = [headers.join(',')];
        
        filteredLogs.forEach(log => {
            const moduleName = log.module || log.context || '';
            const row = [
                new Date(log.timestamp).toISOString(),
                log.level,
                escapeCsv(moduleName),
                escapeCsv(log.fileName || ''),
                log.lineNumber || '',
                log.columnNumber || '',
                escapeCsv(log.functionName || ''),
                escapeCsv(log.errorType || ''),
                escapeCsv(log.message),
                escapeCsv(log.stackTrace || '')
            ];
            csvRows.push(row.join(','));
        });
        
        const csvContent = csvRows.join('\n');
        const dataBlob = new Blob([csvContent], { type: 'text/csv' });
        downloadFile(dataBlob, `offline-logs-${Date.now()}.csv`);
        
        offlineLog.log('OfflineDebugModal', `Exported ${filteredLogs.length} logs as CSV`);
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

    // Clear all logs
    async function clearLogs() {
        if (!confirm('Are you sure you want to clear all logs? This action cannot be undone.')) {
            return;
        }
        
        try {
            await offlineLog.clearLogs();
            allLogs = [];
            filteredLogs = [];
            renderLogs();
            updateStats();
            populateModuleFilter();
            
            offlineLog.log('OfflineDebugModal', 'All logs cleared');
            alert('All logs have been cleared.');
        } catch (error) {
            offlineLog.error('OfflineDebugModal', 'Error clearing logs:', error);
            alert('Error clearing logs. Please try again.');
        }
    }

    // Escape HTML to prevent XSS
    function escapeHtml(text) {
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
    const OfflineDebugModal = {
        initialize,
        show,
        hide,
        toggle,
        toggleStackTrace
    };

    // Expose to global scope
    if (typeof window !== 'undefined') {
        window.OfflineDebugModal = OfflineDebugModal;
    }

    // Auto-initialize on DOM ready
    if (typeof document !== 'undefined') {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initialize);
        } else {
            initialize();
        }
    } 

})();
