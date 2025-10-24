const offline_login_button = document.getElementById('offline_login_button');
const offline_key_element = document.getElementById('offline_login_key');
const offline_key_error_message_element = document.getElementById('offline_key_error_message');
const offline_login_error_message_element = document.getElementById('offline_login_error_message');
const offline_show_hide_key_button = document.getElementById('offline_show_hide_key');

// Add the is-active class to make the login form visible (similar to mmria-custom.js)
const offlineUserLogin = document.getElementById('offline_user_login');
if (offlineUserLogin) {
    setTimeout(() => {
        offlineUserLogin.classList.add('is-active');
    }, 150);
}

const OFFLINE_KEY_ERROR = 'OFFLINE_KEY_ERROR';

var offline_error_messages = [];

// Secure key derivation constants
const KEY_DERIVATION_ITERATIONS = 100000; // PBKDF2 iterations
const HASH_ALGORITHM = 'SHA-256';
const KEY_LENGTH = 256; // bits

// Function to generate a cryptographically secure salt
async function generateSecureSalt() {
    const array = new Uint8Array(32); // 256 bits
    crypto.getRandomValues(array);
    return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
}

// Function to derive key using PBKDF2
async function deriveKeyFromPassword(password, salt, iterations = KEY_DERIVATION_ITERATIONS) {
    try {
        const encoder = new TextEncoder();
        const keyMaterial = await crypto.subtle.importKey(
            'raw',
            encoder.encode(password),
            { name: 'PBKDF2' },
            false,
            ['deriveBits']
        );
        
        const derivedBits = await crypto.subtle.deriveBits(
            {
                name: 'PBKDF2',
                salt: encoder.encode(salt),
                iterations: iterations,
                hash: HASH_ALGORITHM
            },
            keyMaterial,
            KEY_LENGTH
        );
        
        // Convert to hex string for comparison
        const hashArray = Array.from(new Uint8Array(derivedBits));
        return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
    } catch (error) {
        console.error('Error deriving key:', error);
        throw new Error('Failed to derive key');
    }
}

// Function to create session-specific salt (combines multiple entropy sources)
function createSessionSalt(sessionId, timestamp, deviceInfo) {
    return `${sessionId}-${timestamp}-${deviceInfo}-${Math.random().toString(36).substring(2)}`;
}

function show_hide_offline_key(field_id) {
    const keyField = document.getElementById(field_id);
    const button = keyField.nextElementSibling.querySelector('button');
    const icon = button.querySelector('span');

    if (keyField.type === 'password') {
        keyField.type = 'text';
        icon.classList.remove('cdc-icon-eye-solid');
        icon.classList.remove('x22');
        icon.classList.add('x24');
        icon.classList.add('cdc-icon-minus');
    } else {
        keyField.type = 'password';
        icon.classList.remove('cdc-icon-minus');
        icon.classList.remove('x24');
        icon.classList.add('x22');
        icon.classList.add('cdc-icon-eye-solid');
    }
}

// Helper functions to manage global error state
function add_offline_error(code) {
    if (!offline_error_messages.includes(code)) offline_error_messages.push(code);
}
function remove_offline_error(code) {
    const i = offline_error_messages.indexOf(code);
    if (i > -1) offline_error_messages.splice(i, 1);
}

// Function to get offline session data (from app.mmria.js)
function get_offline_session_data_for_login() {
    // Check if the global function exists
    if (typeof window.get_offline_session_data === 'function') {
        return window.get_offline_session_data();
    }
    
    // Fallback to localStorage
    try {
        const storedData = localStorage.getItem('mmria_offline_session');
        if (storedData) {
            return JSON.parse(storedData);
        }
    } catch (error) {
        console.error('Error parsing offline session data from localStorage:', error);
    }
    
    return null;
}

// Function to validate offline key against cached session data using secure derivation
async function validate_offline_key_locally(inputKey) {
    try {
        const sessionData = get_offline_session_data_for_login();
        
        if (!sessionData) {
            console.warn('No offline session data found for key validation');
            return false;
        }
        
        // Use secure derived key comparison if available
        if (sessionData.keySalt && sessionData.derivedKeyHash) {
            const inputKeyHash = await deriveKeyFromPassword(inputKey, sessionData.keySalt);
            const isValid = inputKeyHash === sessionData.derivedKeyHash;
            console.log(isValid ? 'Key validated using secure derivation' : 'Key validation failed - hash mismatch');
            return isValid;
        }
        
        // Legacy fallback for old plaintext keys
        if (sessionData.offlineKey) {
            console.warn('Using legacy plaintext key validation - should be upgraded');
            return sessionData.offlineKey === inputKey;
        }
        
        console.warn('No valid key data found in session');
        return false;
    } catch (error) {
        console.error('Error in local key validation:', error);
        return false;
    }
}

async function validate_offline_login_fields() {
    // Run validators (each updates global offline_error_messages)
    const key_valid = await set_offline_key_validation();
    show_offline_login_error_message();
    return key_valid;
}



async function set_offline_key_validation() {
    let is_valid = true;
    if (offline_key_element) {
        if (!offline_key_element.value) {
            offline_key_element.classList.add('error-text');
            is_valid = false;
        } else {
            offline_key_element.classList.remove('error-text');
            
            // Validate against cached offline session data when available
            const isOfflineMode = localStorage.getItem('is_offline') === 'true';
            if (isOfflineMode) {
                try {
                    const keyMatches = await validate_offline_key_locally(offline_key_element.value);
                    if (!keyMatches) {
                        offline_key_element.classList.add('error-text');
                        is_valid = false;
                        console.log('Offline key validation failed - key does not match cached session data');
                    }
                } catch (error) {
                    console.error('Error validating key locally:', error);
                    offline_key_element.classList.add('error-text');
                    is_valid = false;
                }
            }
        }
    }
    is_valid ? remove_offline_error(OFFLINE_KEY_ERROR) : add_offline_error(OFFLINE_KEY_ERROR);
    return is_valid;
}

function show_offline_login_error_message() {
    if (offline_error_messages.length >= 0) 
    {
        offline_login_error_message_element.classList.add('d-none');
    }

    if (offline_error_messages.includes(OFFLINE_KEY_ERROR)) {
        offline_key_error_message_element.classList.remove('d-none');
    } else {
        offline_key_error_message_element.classList.add('d-none');
    }
}

// Event wiring
if (offline_login_button) {
    offline_login_button.addEventListener('click', async e => {
        e.preventDefault(); // Always prevent default form submission
        
        const validationResult = await validate_offline_login_fields();
        if (!validationResult) {
            return; // Stop if basic validation fails
        }
        
        console.log('Offline login attempt - validating key against service worker cache...');
        
        // Validate against cached service worker data (primary method for offline mode)
        const isValidKey = await validate_key_against_service_worker();
        
        if (isValidKey) {
            localStorage.setItem('has_active_offline_session', 'true');
            console.log('Offline login successful - redirecting to application');
            // Key is valid, redirect to the application
            const returnUrl = document.querySelector('input[name="returnUrl"]')?.value;
            if (returnUrl) {
                window.location.href = returnUrl;
            } else {
                window.location.href = '/Home/Index';
            }
        } else {
            console.log('Offline login failed - invalid key');
            // Show error message
            show_offline_key_error('Invalid offline access key. Please check your key and try again.');
        }
    });
}

if (offline_key_element) {
    const offlineKeyHandlers = async () => { 
        await set_offline_key_validation(); 
        show_offline_login_error_message(); 
    };
    offline_key_element.addEventListener('input', offlineKeyHandlers);
}

if (offline_show_hide_key_button) {
    offline_show_hide_key_button.addEventListener('click', () => {
        show_hide_offline_key('offline_login_key');
    });
}

// Function to validate key against service worker cached data (primary method for offline)
async function validate_key_against_service_worker() {
    try {
        const enteredKey = offline_key_element?.value;
        if (!enteredKey) {
            console.log('No key entered for validation');
            return false;
        }

        // For offline mode, prioritize service worker validation since it works when completely disconnected
        if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
            console.log('Attempting service worker key validation for offline mode...');
            
            return new Promise(async (resolve) => {
                const messageChannel = new MessageChannel();
                
                messageChannel.port1.onmessage = (event) => {
                    const { type, isValid } = event.data;
                    if (type === 'OFFLINE_KEY_VALIDATION_RESPONSE') {
                        console.log('Service worker key validation result:', isValid);
                        resolve(isValid);
                    } else {
                        console.warn('Unexpected response from service worker:', event.data);
                        resolve(false);
                    }
                };
                
                try {
                    // Derive key hash for secure validation (never send plain key)
                    const sessionData = await getSessionDataForValidation();
                    if (!sessionData || !sessionData.keySalt) {
                        console.error('No session salt found for key derivation');
                        resolve(false);
                        return;
                    }
                    
                    const derivedKeyHash = await deriveKeyFromPassword(enteredKey, sessionData.keySalt);
                    
                    // Send derived hash to service worker (never the original key)
                    navigator.serviceWorker.controller.postMessage({
                        type: 'VALIDATE_OFFLINE_KEY',
                        derivedKeyHash: derivedKeyHash,
                        sessionId: sessionData.offlineSessionId
                    }, [messageChannel.port2]);
                } catch (error) {
                    console.error('Error deriving key for validation:', error);
                    resolve(false);
                }
                
                // Timeout after 10 seconds (longer for offline scenarios)
                setTimeout(() => {
                    console.warn('Service worker key validation timeout');
                    resolve(false);
                }, 10000);
            });
        } else {
            console.warn('Service worker not available for key validation - trying fallback methods');
            return validate_key_with_fallback_methods(enteredKey);
        }
    } catch (error) {
        console.error('Error validating key against service worker:', error);
        // Try fallback methods if service worker fails
        return validate_key_with_fallback_methods(enteredKey);
    }
}

// Fallback validation methods (for cases where service worker isn't available)
async function validate_key_with_fallback_methods(enteredKey) {
    try {
        const sessionData = await getSessionDataForValidation();
        if (!sessionData) {
            console.warn('No session data available for fallback validation');
            return false;
        }
        
        // Derive key hash and compare with stored hash
        if (sessionData.keySalt && sessionData.derivedKeyHash) {
            try {
                const enteredKeyHash = await deriveKeyFromPassword(enteredKey, sessionData.keySalt);
                const isValid = enteredKeyHash === sessionData.derivedKeyHash;
                
                if (isValid) {
                    console.log('Key validated successfully using fallback derived key comparison');
                } else {
                    console.log('Key validation failed in fallback - hash mismatch');
                }
                
                return isValid;
            } catch (error) {
                console.error('Error deriving key in fallback validation:', error);
                return false;
            }
        }
        
        // Legacy fallback for old plaintext keys (will be phased out)
        console.warn('Using legacy plaintext key validation - this should be upgraded');
        if (window.mmria_offline_session_data && 
            window.mmria_offline_session_data.offlineKey && 
            window.mmria_offline_session_data.offlineKey === enteredKey) {
            console.log('Key validated using legacy global variable (plaintext)');
            return true;
        }
        
        return false;
    } catch (error) {
        console.error('Error in fallback validation methods:', error);
        return false;
    }
}

// Helper function to get session data for validation
async function getSessionDataForValidation() {
    // Try service worker first
    if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
        try {
            const sessionData = await requestSessionDataFromServiceWorker();
            if (sessionData) {
                return sessionData;
            }
        } catch (error) {
            console.warn('Failed to get session data from service worker:', error);
        }
    }
    
    // Fallback to global variable
    if (window.mmria_offline_session_data) {
        return window.mmria_offline_session_data;
    }
    
    // Last resort: localStorage
    try {
        const storedData = localStorage.getItem('mmria_offline_session');
        if (storedData) {
            return JSON.parse(storedData);
        }
    } catch (error) {
        console.warn('localStorage not available for session data:', error);
    }
    
    return null;
}

// Helper function to request session data from service worker
async function requestSessionDataFromServiceWorker() {
    return new Promise((resolve) => {
        const messageChannel = new MessageChannel();
        
        messageChannel.port1.onmessage = (event) => {
            if (event.data.type === 'OFFLINE_SESSION_DATA_RESPONSE') {
                resolve(event.data.success ? event.data.sessionData : null);
            } else {
                resolve(null);
            }
        };
        
        navigator.serviceWorker.controller.postMessage({
            type: 'GET_OFFLINE_SESSION_DATA'
        }, [messageChannel.port2]);
        
        // Timeout after 3 seconds for validation requests
        setTimeout(() => resolve(null), 3000);
    });
}

// Function to show offline key error
function show_offline_key_error(message) {
    if (offline_login_error_message_element) {
        const messageSpan = offline_login_error_message_element.querySelector('.margin-pagealert');
        if (messageSpan) {
            messageSpan.textContent = message;
        }
        offline_login_error_message_element.classList.remove('d-none');
    }
    
    if (offline_key_element) {
        offline_key_element.classList.add('error-text');
    }
}

// Initialize offline session data on page load
document.addEventListener('DOMContentLoaded', async () => {
    console.log('Offline login page loaded - checking for offline mode data...');
    
    // Check if user should be redirected to regular login
    try {
        const isOfflineMode = localStorage.getItem('is_offline') === 'true';
        if (!isOfflineMode) {
            console.log('User not in offline mode, redirecting to regular login');
            window.location.href = '/Account/Login';
            return;
        }
    } catch (error) {
        console.error('Error checking offline mode, redirecting to regular login:', error);
        window.location.href = '/Account/Login';
        return;
    }
    
    // For offline login, we should always be in offline mode, but check multiple sources
    let isOfflineMode = false;
    
    // Check multiple indicators for offline mode
    try {
        isOfflineMode = localStorage.getItem('is_offline') === 'true';
    } catch (error) {
        console.warn('localStorage not accessible:', error);
    }
    
    // Also check if service worker is controlling (indicates offline mode)
    if (!isOfflineMode && 'serviceWorker' in navigator && navigator.serviceWorker.controller) {
        console.log('Service worker detected - likely in offline mode');
        isOfflineMode = true;
    }
    
    if (isOfflineMode) {
        console.log('Offline login page detected offline mode - loading session data...');
        
        // Try to get session data from service worker first (most reliable for offline)
        if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
            try {
                await preload_session_data_from_service_worker();
            } catch (error) {
                console.warn('Failed to preload from service worker:', error);
                // Fall back to other methods
                preload_session_data_fallback();
            }
        } else {
            preload_session_data_fallback();
        }
    } else {
        console.warn('Offline login page loaded but offline mode not detected - this may indicate a setup issue');
    }
});

// Function to preload session data from service worker
async function preload_session_data_from_service_worker() {
    return new Promise((resolve, reject) => {
        const messageChannel = new MessageChannel();
        
        messageChannel.port1.onmessage = (event) => {
            if (event.data.type === 'OFFLINE_SESSION_DATA_RESPONSE') {
                if (event.data.success && event.data.sessionData) {
                    console.log('Offline session data loaded from service worker:', {
                        sessionId: event.data.sessionData.offlineSessionId,
                        hasKey: !!event.data.sessionData.offlineKey,
                        dateCreated: event.data.sessionData.dateCreated
                    });
                    resolve(event.data.sessionData);
                } else {
                    console.warn('No offline session data found in service worker');
                    reject(new Error('No session data in service worker'));
                }
            }
        };
        
        // Request session data from service worker
        navigator.serviceWorker.controller.postMessage({
            type: 'GET_OFFLINE_SESSION_DATA'
        }, [messageChannel.port2]);
        
        // Timeout after 5 seconds
        setTimeout(() => {
            reject(new Error('Service worker session data request timeout'));
        }, 5000);
    });
}

// Fallback method to preload session data
function preload_session_data_fallback() {
    // Check global window variable first
    if (window.mmria_offline_session_data) {
        console.log('Offline session data loaded from global variable:', {
            sessionId: window.mmria_offline_session_data.offlineSessionId,
            hasKey: !!window.mmria_offline_session_data.offlineKey,
            dateCreated: window.mmria_offline_session_data.dateCreated
        });
        return;
    }
    
    // Fall back to localStorage if available
    try {
        const offlineSessionDataString = localStorage.getItem('mmria_offline_session');
        if (offlineSessionDataString) {
            const sessionData = JSON.parse(offlineSessionDataString);
            console.log('Offline session data loaded from localStorage fallback:', {
                sessionId: sessionData.offlineSessionId,
                hasKey: !!sessionData.offlineKey,
                dateCreated: sessionData.dateCreated
            });
        } else {
            console.warn('No offline session data found in any source - user may need to set up offline mode first');
        }
    } catch (error) {
        console.error('Error loading offline session data from fallback methods:', error);
    }
}