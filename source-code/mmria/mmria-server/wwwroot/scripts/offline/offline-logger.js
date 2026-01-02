/**
 * Offline Logger Module
 * Provides logging functions that save to IndexedDB for later review and server sync
 * Replaces console.log/warn/error in offline code
 */

(function() {
    'use strict';
    
    // Configuration - will be set from server-side ViewBag
    let isLoggingEnabled = false;
    let isConsoleOutputEnabled = false; // Always output to console for development

    // IndexedDB configuration
    const DB_NAME = 'mmria_offline_logs';
    const DB_VERSION = 2; // Updated to add stack trace fields
    const LOG_STORE_NAME = 'logs';
    let maxLogs = 10000; // Default - will be overridden by configuration

    let db = null;

    /**
     * Initialize the logging system
     * @param {boolean} loggingEnabled - Whether IndexedDB logging is enabled
     * @param {number} maxLogsConfig - Maximum number of logs to store before rotation
     */
    function initializeLogger(loggingEnabled, maxLogsConfig) {
        isLoggingEnabled = loggingEnabled;
        if (maxLogsConfig) {
            maxLogs = maxLogsConfig;
        }
        
        if (isLoggingEnabled) {
            openDatabase();
        }
    }

/**
 * Open IndexedDB connection
 */
function openDatabase() {
    const globalScope = typeof self !== 'undefined' ? self : window;
    if (!('indexedDB' in globalScope)) {
        console.warn('IndexedDB not supported - offline logging disabled');
        isLoggingEnabled = false;
        return;
    }
    
    const request = indexedDB.open(DB_NAME, DB_VERSION);
    
    request.onerror = function(event) {
        //console.error('Failed to open offline log database:', event.target.error);
        isLoggingEnabled = false;
    };
    
    request.onsuccess = function(event) {
        db = event.target.result;
        //console.log('Offline log database opened successfully');
    };
    
    request.onupgradeneeded = function(event) {
        const db = event.target.result;
        
        // Clear existing logs when upgrading (no backward compatibility needed)
        if (db.objectStoreNames.contains(LOG_STORE_NAME)) {
            db.deleteObjectStore(LOG_STORE_NAME);
        }
        
        // Create object store for logs with enhanced fields
        const objectStore = db.createObjectStore(LOG_STORE_NAME, { keyPath: 'id', autoIncrement: true });
        objectStore.createIndex('timestamp', 'timestamp', { unique: false });
        objectStore.createIndex('level', 'level', { unique: false });
        objectStore.createIndex('context', 'context', { unique: false });
        objectStore.createIndex('fileName', 'fileName', { unique: false });
        objectStore.createIndex('errorType', 'errorType', { unique: false });
    };
}

/**
 * Parse stack trace to extract caller information
 * @param {string} stack - Error stack trace
 * @param {number} skipFrames - Number of frames to skip (to get actual caller)
 * @returns {Object} Caller information {fileName, lineNumber, columnNumber, functionName}
 */
function parseStackTrace(stack, skipFrames = 3) {
    if (!stack) return { fileName: null, lineNumber: null, columnNumber: null, functionName: null };
    
    try {
        const lines = stack.split('\n');
        // Skip the first few frames (Error creation, parseStackTrace, saveLogToIndexedDB)
        const relevantLine = lines[skipFrames];
        
        if (!relevantLine) return { fileName: null, lineNumber: null, columnNumber: null, functionName: null };
        
        // Chrome format: "    at functionName (file.js:line:col)" or "    at file.js:line:col"
        const chromeMatch = relevantLine.match(/at\s+(?:(.+?)\s+\()?(.+?):(\d+):(\d+)\)?$/);
        
        if (chromeMatch) {
            const functionName = chromeMatch[1] || 'anonymous';
            const fullPath = chromeMatch[2];
            const lineNumber = parseInt(chromeMatch[3], 10);
            const columnNumber = parseInt(chromeMatch[4], 10);
            
            // Extract just the filename from the full path
            const fileName = fullPath.split('/').pop().split('?')[0];
            
            return { fileName, lineNumber, columnNumber, functionName };
        }
    } catch (e) {
        console.error('Error parsing stack trace:', e);
    }
    
    return { fileName: null, lineNumber: null, columnNumber: null, functionName: null };
}

/**
 * Serialize an Error object to preserve stack trace and properties
 * @param {any} arg - Argument to serialize
 * @returns {Object} Serialized error information or null
 */
function serializeError(arg) {
    if (arg instanceof Error) {
        return {
            isError: true,
            name: arg.name,
            message: arg.message,
            stack: arg.stack,
            // Include any additional properties
            ...Object.getOwnPropertyNames(arg).reduce((acc, key) => {
                if (!['name', 'message', 'stack'].includes(key)) {
                    acc[key] = arg[key];
                }
                return acc;
            }, {})
        };
    }
    return null;
}

/**
 * Save a log entry to IndexedDB
 * @param {string} level - Log level (log, info, warn, error)
 * @param {string} context - Context/module where log originated
 * @param {Array} args - Log arguments
 */
function saveLogToIndexedDB(level, context, args) {
    if (!isLoggingEnabled || !db) {
        return;
    }
    
    try {
        // Capture stack trace for caller information
        const stack = new Error().stack;
        const callerInfo = parseStackTrace(stack, 3);
        
        // Find first Error object in args for error type and stack
        let errorInfo = null;
        let errorType = null;
        let errorStack = null;
        
        for (const arg of args) {
            if (arg instanceof Error) {
                errorInfo = serializeError(arg);
                errorType = arg.name;
                errorStack = arg.stack;
                break;
            }
        }
        
        const transaction = db.transaction([LOG_STORE_NAME], 'readwrite');
        const objectStore = transaction.objectStore(LOG_STORE_NAME);
        
        // Convert args to strings, preserving Error information
        const message = args.map(arg => {
            const serialized = serializeError(arg);
            if (serialized) {
                // Format error message nicely
                return `${serialized.name}: ${serialized.message}`;
            }
            if (typeof arg === 'object') {
                try {
                    return JSON.stringify(arg);
                } catch (e) {
                    return String(arg);
                }
            }
            return String(arg);
        }).join(' ');
        
        // Read offline-related localStorage values (only available in window context, not service worker)
        let isOffline = null;
        let processOfflineCases = null;
        let offlineSessionId = null;
        
        try {
            // Check if we're in a service worker context
            if (typeof self !== 'undefined' && self.constructor.name === 'ServiceWorkerGlobalScope') {
                // In service worker - use service worker global variables
                isOffline = true
                offlineSessionId = (typeof self.OFFLINE_SESSION_ID !== 'undefined' && self.OFFLINE_SESSION_ID) ? self.OFFLINE_SESSION_ID : null;
                // Note: process_offline_cases not tracked in service worker, leave as null
            } else if (typeof localStorage !== 'undefined') {
                // In window context - use localStorage
                isOffline = localStorage.getItem('is_offline') || null;
                processOfflineCases = localStorage.getItem('process_offline_cases') || null;
                offlineSessionId = localStorage.getItem('offline_session_id') || null;
            }
        } catch (e) {
            // Access not available
        }
        
        const logEntry = {
            timestamp: new Date().toISOString(),
            level: level,
            context: context,
            message: message,
            // New fields for enhanced debugging
            fileName: callerInfo.fileName,
            lineNumber: callerInfo.lineNumber,
            columnNumber: callerInfo.columnNumber,
            functionName: callerInfo.functionName,
            stackTrace: errorStack || null,
            errorType: errorType || null,
            // Offline context fields
            is_offline: isOffline,
            process_offline_cases: processOfflineCases,
            offline_session_id: offlineSessionId
        };
        
        const request = objectStore.add(logEntry);
        
        request.onsuccess = function() {
            // Check if we need to rotate logs
            rotateLogs();
        };
        
        request.onerror = function(event) {
            console.error('Failed to save log to IndexedDB:', event.target.error);
        };
        
    } catch (error) {
        console.error('Error saving log to IndexedDB:', error);
    }
}

/**
 * Rotate logs if we exceed maxLogs
 */
function rotateLogs() {
    if (!db) return;
    
    try {
        const transaction = db.transaction([LOG_STORE_NAME], 'readonly');
        const objectStore = transaction.objectStore(LOG_STORE_NAME);
        const countRequest = objectStore.count();
        
        countRequest.onsuccess = function() {
            const count = countRequest.result;
            
            if (count > maxLogs) {
                // Delete oldest logs
                const deleteCount = count - maxLogs;
                deleteOldestLogs(deleteCount);
            }
        };
    } catch (error) {
        console.error('Error checking log count:', error);
    }
}

/**
 * Delete oldest logs
 * @param {number} count - Number of logs to delete
 */
function deleteOldestLogs(count) {
    if (!db) return;
    
    try {
        const transaction = db.transaction([LOG_STORE_NAME], 'readwrite');
        const objectStore = transaction.objectStore(LOG_STORE_NAME);
        const index = objectStore.index('timestamp');
        const request = index.openCursor();
        
        let deleted = 0;
        
        request.onsuccess = function(event) {
            const cursor = event.target.result;
            if (cursor && deleted < count) {
                cursor.delete();
                deleted++;
                cursor.continue();
            }
        };
        
    } catch (error) {
        console.error('Error deleting old logs:', error);
    }
}

/**
 * Get all logs from IndexedDB
 * @returns {Promise<Array>} Array of log entries
 */
function getAllLogs() {
    return new Promise((resolve, reject) => {
        if (!db) {
            resolve([]);
            return;
        }
        
        try {
            const transaction = db.transaction([LOG_STORE_NAME], 'readonly');
            const objectStore = transaction.objectStore(LOG_STORE_NAME);
            const request = objectStore.getAll();
            
            request.onsuccess = function() {
                resolve(request.result);
            };
            
            request.onerror = function() {
                reject(request.error);
            };
            
        } catch (error) {
            reject(error);
        }
    });
}

/**
 * Clear all logs from IndexedDB
 * @returns {Promise<void>}
 */
function clearAllLogs() {
    return new Promise((resolve, reject) => {
        if (!db) {
            resolve();
            return;
        }
        
        try {
            const transaction = db.transaction([LOG_STORE_NAME], 'readwrite');
            const objectStore = transaction.objectStore(LOG_STORE_NAME);
            const request = objectStore.clear();
            
            request.onsuccess = function() {
                resolve();
            };
            
            request.onerror = function() {
                reject(request.error);
            };
            
        } catch (error) {
            reject(error);
        }
    });
}

/**
 * Offline Logger API
 */
const offlineLog = {
    /**
     * Log a message
     * @param {string} context - Context/module name
     * @param  {...any} args - Arguments to log
     */
    log: function(context, ...args) {
        if (isConsoleOutputEnabled) {
            console.log(context, ...args);
        }
        if (isLoggingEnabled) {
            saveLogToIndexedDB('log', context, args);
        }
    },
    
    /**
     * Log an info message
     * @param {string} context - Context/module name
     * @param  {...any} args - Arguments to log
     */
    info: function(context, ...args) {
        if (isConsoleOutputEnabled) {
            console.info(context, ...args);
        }
        if (isLoggingEnabled) {
            saveLogToIndexedDB('info', context, args);
        }
    },
    
    /**
     * Log a warning message
     * @param {string} context - Context/module name
     * @param  {...any} args - Arguments to log
     */
    warn: function(context, ...args) {
        if (isConsoleOutputEnabled) {
            console.warn(context, ...args);
        }
        if (isLoggingEnabled) {
            saveLogToIndexedDB('warn', context, args);
        }
    },
    
    /**
     * Log an error message
     * @param {string} context - Context/module name
     * @param  {...any} args - Arguments to log
     */
    error: function(context, ...args) {
        if (isConsoleOutputEnabled) {
            console.error(context, ...args);
        }
        if (isLoggingEnabled) {
            saveLogToIndexedDB('error', context, args);
        }
    },
    
    /**
     * Initialize the logger with configuration
     * @param {boolean} loggingEnabled - Whether IndexedDB logging is enabled
     */
    initialize: initializeLogger,
    
    /**
     * Get all stored logs
     * @returns {Promise<Array>} Array of log entries
     */
    getAllLogs: getAllLogs,
    
    /**
     * Clear all stored logs
     * @returns {Promise<void>}
     */
    clearLogs: clearAllLogs,
    
    /**
     * Check if logging is enabled
     * @returns {boolean}
     */
    isEnabled: function() {
        return isLoggingEnabled;
    }
};

// Export to both window (main thread) and self (service worker)
const globalScope = typeof window !== 'undefined' ? window : self;
globalScope.offlineLog = offlineLog;

// Set up global error handlers (only in window context, not service worker)
if (typeof window !== 'undefined') {
    // Catch uncaught exceptions
    window.addEventListener('error', function(event) {
        if (isLoggingEnabled) {
            const errorInfo = {
                message: event.message,
                filename: event.filename ? event.filename.split('/').pop() : 'unknown',
                lineno: event.lineno,
                colno: event.colno,
                error: event.error
            };
            
            offlineLog.error('UncaughtError', 
                `${event.message} at ${errorInfo.filename}:${event.lineno}:${event.colno}`,
                event.error
            );
        }
    });
    
    // Catch unhandled promise rejections
    window.addEventListener('unhandledrejection', function(event) {
        if (isLoggingEnabled) {
            const reason = event.reason;
            let errorMsg = 'Unhandled Promise Rejection';
            
            if (reason instanceof Error) {
                errorMsg = `${reason.name}: ${reason.message}`;
            } else if (typeof reason === 'string') {
                errorMsg = reason;
            } else {
                try {
                    errorMsg = JSON.stringify(reason);
                } catch (e) {
                    errorMsg = String(reason);
                }
            }
            
            offlineLog.error('UnhandledRejection', errorMsg, reason);
        }
    });
}

//console.log('Offline Logger module loaded');

})(); // End of IIFE
