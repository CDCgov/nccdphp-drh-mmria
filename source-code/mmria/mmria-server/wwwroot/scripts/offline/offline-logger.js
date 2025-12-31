/**
 * Offline Logger Module
 * Provides logging functions that save to IndexedDB for later review and server sync
 * Replaces console.log/warn/error in offline code
 */

(function() {
    'use strict';
    
    // Configuration - will be set from server-side ViewBag
    let isLoggingEnabled = false;
    let isConsoleOutputEnabled = true; // Always output to console for development

    // IndexedDB configuration
    const DB_NAME = 'mmria_offline_logs';
    const DB_VERSION = 1;
    const LOG_STORE_NAME = 'logs';
    const MAX_LOGS = 1000; // Maximum number of logs to store before rotation

    let db = null;

    /**
     * Initialize the logging system
     * @param {boolean} loggingEnabled - Whether IndexedDB logging is enabled
     */
    function initializeLogger(loggingEnabled) {
        isLoggingEnabled = loggingEnabled;
        
        if (isLoggingEnabled) {
            openDatabase();
        }
    }

/**
 * Open IndexedDB connection
 */
function openDatabase() {
    if (!('indexedDB' in window)) {
        console.warn('IndexedDB not supported - offline logging disabled');
        isLoggingEnabled = false;
        return;
    }
    
    const request = indexedDB.open(DB_NAME, DB_VERSION);
    
    request.onerror = function(event) {
        console.error('Failed to open offline log database:', event.target.error);
        isLoggingEnabled = false;
    };
    
    request.onsuccess = function(event) {
        db = event.target.result;
        console.log('Offline log database opened successfully');
    };
    
    request.onupgradeneeded = function(event) {
        const db = event.target.result;
        
        // Create object store for logs
        if (!db.objectStoreNames.contains(LOG_STORE_NAME)) {
            const objectStore = db.createObjectStore(LOG_STORE_NAME, { keyPath: 'id', autoIncrement: true });
            objectStore.createIndex('timestamp', 'timestamp', { unique: false });
            objectStore.createIndex('level', 'level', { unique: false });
        }
    };
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
        const transaction = db.transaction([LOG_STORE_NAME], 'readwrite');
        const objectStore = transaction.objectStore(LOG_STORE_NAME);
        
        // Convert args to strings
        const message = args.map(arg => {
            if (typeof arg === 'object') {
                try {
                    return JSON.stringify(arg);
                } catch (e) {
                    return String(arg);
                }
            }
            return String(arg);
        }).join(' ');
        
        const logEntry = {
            timestamp: new Date().toISOString(),
            level: level,
            context: context,
            message: message
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
 * Rotate logs if we exceed MAX_LOGS
 */
function rotateLogs() {
    if (!db) return;
    
    try {
        const transaction = db.transaction([LOG_STORE_NAME], 'readonly');
        const objectStore = transaction.objectStore(LOG_STORE_NAME);
        const countRequest = objectStore.count();
        
        countRequest.onsuccess = function() {
            const count = countRequest.result;
            
            if (count > MAX_LOGS) {
                // Delete oldest logs
                const deleteCount = count - MAX_LOGS;
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
window.offlineLog = {
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

console.log('Offline Logger module loaded');

})(); // End of IIFE
