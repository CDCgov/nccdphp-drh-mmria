/**
 * Offline Utils Module
 * Utility functions for offline mode operations
 */

// Cryptographic constants - exposed globally for use by app.mmria.js
const OFFLINE_KEY_DERIVATION_ITERATIONS = 100000;
const OFFLINE_HASH_ALGORITHM = 'SHA-256';
const OFFLINE_KEY_LENGTH = 256;

// Function to fetch cache version from server
async function fetchCacheVersionFromServer() {
    try {
        const response = await fetch('/api/values/cache-version');
        if (response.ok) {
            const data = await response.json();
            return data.version || 'v1';
        }
    } catch (error) {
        console.warn('Failed to fetch cache version from server:', error);
    }
    return 'v1';
}

// Function to get actual API cache name
async function getActualApiCacheName() {
    const version = await fetchCacheVersionFromServer();
    return `mmria-api-cache-${version}`;
}

// Function to generate a secure salt for offline key derivation
async function generateSecureOfflineKeySalt(sessionId, timestamp) {
    try {
        const randomArray = new Uint8Array(32);
        crypto.getRandomValues(randomArray);
        const randomHex = Array.from(randomArray, byte => byte.toString(16).padStart(2, '0')).join('');
        
        const compositeSalt = `${sessionId}-${timestamp}-${randomHex}-${navigator.userAgent.length}`;
        
        const encoder = new TextEncoder();
        const saltBuffer = await crypto.subtle.digest(OFFLINE_HASH_ALGORITHM, encoder.encode(compositeSalt));
        const saltArray = Array.from(new Uint8Array(saltBuffer));
        return saltArray.map(b => b.toString(16).padStart(2, '0')).join('');
    } catch (error) {
        console.error('Error generating secure offline key salt:', error);
        return `${sessionId}-${timestamp}-${Math.random().toString(36).substring(2)}`;
    }
}

// Function to derive offline key hash using PBKDF2
async function deriveOfflineKeyHash(password, salt, iterations = OFFLINE_KEY_DERIVATION_ITERATIONS) {
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
                hash: OFFLINE_HASH_ALGORITHM
            },
            keyMaterial,
            OFFLINE_KEY_LENGTH
        );
        
        const hashArray = Array.from(new Uint8Array(derivedBits));
        return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
    } catch (error) {
        console.error('Error deriving offline key hash:', error);
        throw new Error('Failed to derive offline key hash');
    }
}

// Expose the offline utils API to the global scope
window.OfflineUtils = {
    fetchCacheVersion: fetchCacheVersionFromServer,
    getApiCacheName: getActualApiCacheName,
    generateKeySalt: generateSecureOfflineKeySalt,
    deriveKeyHash: deriveOfflineKeyHash
};

console.log('Offline Utils module loaded');
