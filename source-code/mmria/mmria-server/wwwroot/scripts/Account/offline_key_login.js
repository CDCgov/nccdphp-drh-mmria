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

// Constants for password hash verification (matching service worker values)
const KEY_DERIVATION_ITERATIONS = 100000;
const HASH_ALGORITHM = 'SHA-256';
const KEY_LENGTH = 256; // bits

var offline_error_messages = [];



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

// Function to create session-specific salt
function createSessionSalt(sessionId, timestamp, deviceInfo) {
    const randomBytes = new Uint8Array(16); // 128 bits of entropy
    crypto.getRandomValues(randomBytes);
    const randomHex = Array.from(randomBytes, byte => byte.toString(16).padStart(2, '0')).join('');
    return `${sessionId}-${timestamp}-${deviceInfo}-${randomHex}`;
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
            // Field has a value - actual key validation will be done by service worker
            // to ensure we're checking against the authoritative cached session data
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
            const enteredKey = offline_key_element?.value || '';

            localStorage.setItem('has_active_offline_session', 'true');

            // Initialize crypto in SW + decrypt cached cases (best-effort, non-blocking if it fails)
            try {
                await initializeOfflineCryptoAfterLogin(enteredKey);
            } catch (e) {
                console.error('Error during offline crypto initialization:', e);
            }

            // Notify service worker of status change
            if (window.ServiceWorkerManager) {
                window.ServiceWorkerManager.notifyActiveOfflineSessionChange();
            }
            
            console.log('Offline login successful - redirecting to application');
            window.location.href = '/Home/Index';
        } else {
            console.log('Offline login failed - invalid key or account locked');
            // Error message already shown by validate_key_against_service_worker
            // via show_offline_lockout_error or show_offline_key_error
        }
    });
}

if (offline_key_element) {
    // Clear error styling when user starts typing (improves UX)
    offline_key_element.addEventListener('input', () => {
        offline_key_element.classList.remove('error-text');
        offline_key_error_message_element.classList.add('d-none');
        offline_login_error_message_element.classList.add('d-none');
    });
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
            console.log('Validating key locally - fetching session data from service worker...');
            
            // Step 1: Get session data FROM service worker (includes stored hash and lockout status)
            const sessionData = await requestSessionDataFromServiceWorker();
            if (!sessionData) {
                console.error('Failed to retrieve session data from service worker');
                return validate_key_with_fallback_methods(enteredKey);
            }
            
            // Step 2: Check lockout status from service worker
            if (sessionData.isLockedOut) {
                const remainingMinutes = sessionData.remainingMinutes || 0;
                console.log(`Account locked out. ${remainingMinutes} minutes remaining.`);
                show_offline_lockout_error(0, true, remainingMinutes);
                return false;
            }
            
            // Step 3: Derive key and compare in main thread (no password data transmitted)
            if (!sessionData.keySalt || !sessionData.derivedKeyHash) {
                console.error('Session data missing required fields for validation');
                return false;
            }
            
            const enteredKeyHash = await deriveKeyFromPassword(enteredKey, sessionData.keySalt);
            const isValid = enteredKeyHash === sessionData.derivedKeyHash;
            
            console.log('Local validation result:', isValid ? 'valid' : 'invalid');
            
            // Step 4: Notify service worker of result for lockout tracking (no password data sent)
            // const lockoutResponse = await notifyServiceWorkerOfLoginAttempt(
            //     isValid, 
            //     sessionData.offlineSessionId
            // );
            
            // Step 5: Handle validation result
            if (isValid) {
                console.log('Key validation successful');
                return true;
            } else {
                // Failed validation - show generic error
                // TODO: Re-implement lockout tracking in a safe way
                show_offline_key_error('Invalid offline access key. Please check your key and try again.');
                return false;
            }            

            //commented this out. will need to revisit this and implement in a safe way
            // Step 5: Handle validation result and lockout state
            // if (isValid) {
            //     console.log('Key validation successful');
            //     return true;
            // } else {
            //     // Failed validation - show appropriate error
            //     if (lockoutResponse && lockoutResponse.isLockedOut) {
            //         const remainingMinutes = lockoutResponse.remainingMinutes || 0;
            //         console.log(`Failed attempt resulted in lockout. ${remainingMinutes} minutes remaining.`);
            //         show_offline_lockout_error(0, true, remainingMinutes);
            //     } else if (lockoutResponse && typeof lockoutResponse.attemptsRemaining === 'number') {
            //         console.log(`Key validation failed. ${lockoutResponse.attemptsRemaining} attempts remaining.`);
            //         show_offline_lockout_error(lockoutResponse.attemptsRemaining, false, 0);
            //     } else {
            //         show_offline_key_error('Invalid offline access key. Please check your key and try again.');
            //     }
            //     return false;
            // }
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

// // Notify service worker of login attempt result for lockout tracking (no password data transmitted)
// async function notifyServiceWorkerOfLoginAttempt(isValid, sessionId) {
//     return new Promise((resolve) => {
//         const messageChannel = new MessageChannel();
        
//         messageChannel.port1.onmessage = (event) => {
//             if (event.data.type === 'LOGIN_ATTEMPT_RECORDED') {
//                 resolve(event.data);
//             } else {
//                 resolve(null);
//             }
//         };
        
//         navigator.serviceWorker.controller.postMessage({
//             type: 'RECORD_LOGIN_ATTEMPT',
//             isValid: isValid,
//             sessionId: sessionId
//         }, [messageChannel.port2]);
        
//         // Timeout after 3 seconds
//         setTimeout(() => resolve(null), 3000);
//     });
// }

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



// Helper function to request session data from service worker
async function requestSessionDataFromServiceWorker() {
    return new Promise((resolve) => {
        const messageChannel = new MessageChannel();
        
        messageChannel.port1.onmessage = (event) => {
            if (event.data.type === 'OFFLINE_SESSION_DATA_RESPONSE') {
                if (event.data.success && event.data.sessionData) {
                    // Include lockout status in the returned data
                    const sessionData = event.data.sessionData;
                    sessionData.isLockedOut = event.data.isLockedOut || false;
                    sessionData.remainingMinutes = event.data.remainingMinutes || 0;
                    resolve(sessionData);
                } else {
                    resolve(null);
                }
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

// Send the derived AES key to the service worker so it can encrypt/decrypt cached cases
async function sendOfflineKeyToServiceWorker(aesKey) {
    if (!('serviceWorker' in navigator)) return false;

    const registration = await navigator.serviceWorker.ready;
    if (!registration.active) return false;

    const keyBytes = await crypto.subtle.exportKey('raw', aesKey);

    return new Promise(resolve => {
        const messageChannel = new MessageChannel();

        messageChannel.port1.onmessage = (event) => {
            resolve(event.data && event.data.success === true);
        };

        registration.active.postMessage(
            {
                type: 'SET_OFFLINE_ENCRYPTION_KEY',
                keyBytes
            },
            [messageChannel.port2, keyBytes] // transfer port + key bytes
        );
    });
}

// Tell the service worker to decrypt all cached case responses
async function requestDecryptCachedCases() {
    if (!('serviceWorker' in navigator)) return;

    const registration = await navigator.serviceWorker.ready;
    if (!registration.active) return;

    return new Promise(resolve => {
        const messageChannel = new MessageChannel();

        messageChannel.port1.onmessage = () => {
            resolve();
        };

        registration.active.postMessage(
            { type: 'OFFLINE_LOGIN_DECRYPT_CASES' },
            [messageChannel.port2]
        );
    });
}

// After offline login success, initialize crypto in the SW and decrypt cached cases
async function initializeOfflineCryptoAfterLogin(enteredKey) {
    try {
        if (!('serviceWorker' in navigator) || !navigator.serviceWorker.controller) {
            return; // nothing to do if no SW controlling this page
        }

        const sessionData = await getSessionDataForValidation();
        if (!sessionData || !sessionData.keySalt) {
            console.warn('No sessionData / keySalt available for AES key derivation');
            return;
        }

        const keySet = await ServiceWorkerManager.setOfflineKey(enteredKey, sessionData.keySalt);
        if (!keySet) {
            console.warn('Failed to set offline AES key in service worker');
            return;
        }

        // Now tell SW to decrypt any encrypted /api/case responses
        //await requestDecryptCachedCases();
        console.log('Offline cached cases decrypted in service worker');
    } catch (err) {
        console.error('Error initializing offline crypto after login:', err);
    }
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

// Function to show lockout error message with attempts remaining or lockout time
function show_offline_lockout_error(attemptsRemaining, isLockedOut, remainingMinutes) {
    if (offline_login_error_message_element) {
        const messageSpan = offline_login_error_message_element.querySelector('.margin-pagealert');
        if (messageSpan) {
            if (isLockedOut) {
                // User is locked out - show lockout message
                const hours = Math.floor(remainingMinutes / 60);
                const minutes = remainingMinutes % 60;
                let timeString = '';
                if (hours > 0) {
                    timeString = `${hours} hour${hours > 1 ? 's' : ''}`;
                    if (minutes > 0) {
                        timeString += ` and ${minutes} minute${minutes > 1 ? 's' : ''}`;
                    }
                } else {
                    timeString = `${minutes} minute${minutes > 1 ? 's' : ''}`;
                }
                
                messageSpan.innerHTML = `You have entered an incorrect key. <b>Your account will be locked for 2 hours after 3 failed attempts.</b><br><br>` +
                    `<b>Account is currently locked. Please try again in ${timeString}.</b><br><br>` +
                    `Please contact your jurisdiction administrator for further offline key assistance if needed.`;
            } else if (attemptsRemaining > 0) {
                // User has attempts remaining - show warning
                messageSpan.innerHTML = `You have entered an incorrect key. <b>Your account will be locked for 2 hours after 3 failed attempts.</b><br><br>` +
                    `<b>Attempts Remaining: ${attemptsRemaining}</b><br><br>` +
                    `Please contact your jurisdiction administrator for further offline key assistance if needed.`;
            } else {
                // Fallback generic message
                messageSpan.textContent = 'Invalid offline access key. Please check your key and try again.';
            }
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
        
        messageChannel.port1.onmessage = async (event) => {
            if (event.data.type === 'OFFLINE_SESSION_DATA_RESPONSE') {
                if (event.data.success && event.data.sessionData) {
                    console.log('Offline session data loaded from service worker:', {
                        sessionId: event.data.sessionData.offlineSessionId,
                        hasKey: !!event.data.sessionData.offlineKey,
                        hasKeySalt: !!event.data.sessionData.keySalt,
                        hasDerivedKeyHash: !!event.data.sessionData.derivedKeyHash,
                        dateCreated: event.data.sessionData.dateCreated
                    });
                    
                    // Verify localStorage and service worker cache are in sync
                    try {
                        const localData = localStorage.getItem('mmria_offline_session');
                        if (localData) {
                            const localSession = JSON.parse(localData);
                            const swSession = event.data.sessionData;
                            
                            // Check if session data matches
                            if (localSession.offlineSessionId !== swSession.offlineSessionId) {
                                console.warn('WARNING: Session ID mismatch between localStorage and service worker cache!');
                                console.warn('localStorage sessionId:', localSession.offlineSessionId);
                                console.warn('Service worker sessionId:', swSession.offlineSessionId);
                            }
                            
                            if (localSession.keySalt !== swSession.keySalt) {
                                console.warn('WARNING: Key salt mismatch between localStorage and service worker cache!');
                                console.warn('This will cause login failures. Session data is out of sync.');
                            }
                            
                            if (localSession.derivedKeyHash !== swSession.derivedKeyHash) {
                                console.warn('WARNING: Derived key hash mismatch between localStorage and service worker cache!');
                                console.warn('This will cause login failures. Session data is out of sync.');
                            }
                        }
                    } catch (error) {
                        console.error('Error verifying session data sync:', error);
                    }
                    
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