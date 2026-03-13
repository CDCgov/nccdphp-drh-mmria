# MMRIA Offline Mode - Technical Documentation

## Overview
Offline Mode enables users to take 0 to many cases into an offline state where they can work without internet connectivity. Cases are stored encrypted at rest using AES-256-GCM encryption with PBKDF2 key derivation. Users can edit existing cases and create new cases while offline, with all changes automatically tracked and synchronized when returning online.

---

## Core Architecture Components

### 1. Client-Side Modules (JavaScript)
Located in `/wwwroot/scripts/offline/`:

| Module | Purpose |
|--------|---------|
| `offline-session-manager.js` | Manages offline session lifecycle and validation |
| `offline-case-manager.js` | Handles case operations (add, remove, create) |
| `offline-sync-manager.js` | Coordinates synchronization with server |
| `offline-transition-manager.js` | Controls online↔offline transitions |
| `offline-change-tracker.js` | Tracks field-level changes for audit trail |
| `offline-network-monitor.js` | Monitors connectivity status |
| `offline-utils.js` | Cryptographic utilities and helper functions |
| `service-worker-manager.js` | Manages service worker lifecycle and messaging |
| `offline-ui-renderer.js` | Renders offline mode UI elements |
| `offline-modals.js` | Modal dialogs for offline operations |
| `offline-status-manager.js` | Manages offline status indicators |
| `offline-session-validator.js` | Validates offline session integrity |
| `offline-logout-button.js` | Handles logout during offline mode |
| `offline-navigation-manager.js` | Routes navigation in offline mode |
| `offline-logger.js` | Debug logging subsystem |
| `offline-debug-modal.js` | Debug console for offline troubleshooting |
| `offline-integrity-validator.js` | Shared offline/session/cache validation used at transition and runtime checkpoints |
| `offline-cache-manifest.js` | Shared cache contract for service worker caching and validator expectations |

### 2. Service Worker (`/wwwroot/service-worker.js`)
- **Size**: 3,573 lines - comprehensive caching and encryption logic
- **Cache Strategy**: Cache-first for offline access with fallback to network
- **Encryption**: AES-256-GCM encryption/decryption in memory (key never persisted)
- **Cache Management**: Versioned caches for static assets, API responses, and case data
- **Request Interception**: Intercepts network requests and serves from cache when offline
- **Version Control**: Automatically detects cache version from server API endpoint

### 2a. Shared Cache Contract (`/wwwroot/scripts/offline/offline-cache-manifest.js`)
- The service worker and the offline integrity validator now share the same cache manifest rather than maintaining separate hard-coded lists.
- The manifest defines three different categories with different validation expectations:
  - `requiredStaticExpectations`: static assets that must be fetched, cached, and usable
  - `requiredRouteExpectations`: HTML routes/pages that must be cached with a valid HTML shell
  - `requiredApiExpectations`: API endpoints that must be cached with route-specific status/content/shape checks
- The manifest also still defines the broader `cachedRoutes` and `cachedApiRoutes` patterns used by the service worker for request routing and cache matching.
- Important distinction:
  - `offline_session_id` is the server-side offline session id stored in localStorage and offline session payloads
  - cache names are keyed by a separate service-worker cache session id
  - the validator resolves the correct cache pair by checking the service-worker session id, cached offline-session payload, and expected case overlap

### 2b. Offline Integrity Validator (`/wwwroot/scripts/offline/offline-integrity-validator.js`)
- This is now the shared integrity gate for offline mode rather than a collection of ad hoc checks.
- It validates:
  - localStorage session artifacts (`mmria_offline_session`, `offline_session_id`, `is_offline`, `has_active_offline_session`, `process_offline_cases`)
  - service worker support/controller/registration state
  - session-specific static/API cache presence
  - cached offline-session payload presence and consistency
  - expected cached case ids
  - required static files, routes, and API endpoints from `offline-cache-manifest.js`
- The validator can recover session context from the cached offline-session payload if `mmria_offline_session` is missing, but that still counts as an integrity failure. Recovery is only for better diagnostics and more accurate blocking behavior.
- The validator uses `offlineLog` only and returns a structured result object including:
  - checkpoint
  - detected lifecycle state
  - offline session id
  - cache session id
  - expected/found cache names
  - expected/found case ids
  - issues, missing artifacts, and warnings
- Current validation checkpoints include:
  - `go_offline_pre_auth`
  - `go_offline_precomplete`
  - `offline_monitor`
  - `case_list_load`
  - `case_detail_load`
  - `go_online_preflight`
- `go_offline_pre_auth` runs after service-worker case/page/metadata caching and before `setup_offline_session_auth()`. This is the "cache readiness before offline auth creation" gate.
- Hosted environments exposed a timing problem here: service-worker cache work can finish noticeably later than localhost, so `go_offline_pre_auth` must run only after explicit cache completion and readiness checks, not after fixed delays or fire-and-forget `postMessage(...)` calls.
- `go_offline_precomplete` runs later in the transition and verifies the fully assembled offline session state.
- `offline_monitor` is the periodic steady-state integrity check while offline.
- `case_list_load` and `case_detail_load` provide narrower diagnostics during normal offline use.
- Steady-state offline failures now automatically trigger `show_go_online_failure_modal()` when:
  - the validator result is invalid
  - `blockAndAlertOnError` is `true`
  - the detected lifecycle state is `offline`
- This is intentionally limited to the time window after `attempt_offline_transition()` has completed and before `go_online_clicked()` begins. It is not used for:
  - `go_offline_pre_auth`
  - `go_offline_precomplete`
  - `go_online_preflight`
- The modal is shown only once per page lifecycle and stops the periodic integrity monitor before recovery starts.
- This gives the user an explicit `OK` acknowledgment path instead of immediately auto-running the invalid-state reset flow.
- Logging is intentionally biased toward high-signal summaries:
  - keep transition milestones, validator pass/fail results, aggregate cache summaries, warnings, and errors
  - avoid per-request routing breadcrumbs and repeated cache-hit success logs unless they represent a user-visible state change or a failure path

### 2c. Go Offline Cache Completion Handshake

The Go Offline transition now uses explicit service-worker acknowledgments and a cache-readiness barrier before `go_offline_pre_auth`.

- `ServiceWorkerManager.prefetchCases()` no longer treats `CACHE_CASE_DATA` as fire-and-forget.
  - each case cache request waits for a `MessageChannel` response from the service worker confirming the case was stored and re-read from cache
- `ServiceWorkerManager.cacheMetadata()` no longer posts `CACHE_METADATA` and sleeps for a fixed delay
  - it now waits for a service-worker response indicating the metadata caching pass finished successfully or failed with an error
- `ServiceWorkerManager.waitForCacheReadiness()` polls the current session caches and waits until:
  - all required static files from `offline-cache-manifest.js` are present
  - required route aliases are present
  - required API entries such as `/api/OfflineCase/cache-version` are present
  - all expected offline case documents are present
  - the cached offline-session payload exists

This barrier exists because cloud-hosted deployments can be slow enough that a validator run immediately after background cache requests will see a partial cache even though the service worker is still working.

### 2d. Case Fetch Rule During Go Offline

`/api/case?case_id=...` must remain network-only until offline mode is fully established.

- During Go Offline setup, there should be no legitimate case reads from cache yet.
- Case prefetch is allowed to write fetched cases into the new session cache, but the service worker must not serve case reads from cache until both:
  - `localStorage["is_offline"] === "true"`
  - `localStorage["has_active_offline_session"] === "true"`
- Before this rule was added, hosted environments exposed a race:
  - the service worker could intercept `/api/case` during setup
  - use cache-first routing too early
  - find an encrypted case response from the wrong session via global `caches.match(request)`
  - fail decryption with `OperationError`
  - return `500 offline_decrypt_failed`
- This was usually hidden on localhost because the transition was fast enough that the incorrect cache-read path rarely won the race.
- The fix is:
  - treat `/api/case?case_id=...` as network-only until steady-state offline mode
  - when steady-state offline mode is active, read case data only from the active session API cache, not from global cache lookup across all caches

This rule is important because offline mode is transactional. A user should not be able to read case data from cache until the offline transition has completed successfully.

### 3. Server-Side Components (C#)

#### Controllers
- **`Controllers/api/OfflineCaseController.cs`** (421 lines)
  - API endpoints for offline case management
  - Handles session creation, retrieval, updates, and deletion
  - Provides cache version endpoint for service worker

#### Utilities
- **`util/OfflineSessionHelper.cs`** - Server-side session management utilities
- **`util/c_db_setup.cs`** - Creates and configures `offline_cases` database

#### Database
- **`offline_cases`** (CouchDB) - Stores offline session documents with case modifications
- Design document: `database-scripts/offline_design_sortable.json`

---

## Security & Encryption

### Key Derivation Process
1. **User Input**: User enters offline key (password/passphrase)
2. **Key Derivation**: PBKDF2 with 100,000 iterations derives 256-bit encryption key
3. **Hash Algorithm**: SHA-256 with per-session salt
4. **Key Storage**: Derived key exists only in service worker memory (never persisted to disk)
5. **Validation**: Key hash stored for login validation (password never transmitted)

### Encryption at Rest
- **Algorithm**: AES-256-GCM (Galois/Counter Mode - provides authenticated encryption)
- **Storage Location**: Cases encrypted in browser's Cache API
- **Key Management**: Encryption key exists only in service worker memory
- **Encryption Header**: Custom `X-Offline-Encrypted: 1` header marks encrypted responses
- **Service Worker Restart**: Requires user to re-enter key to decrypt (key lost on restart)
- **Initialization Vector**: Unique 12-byte IV generated per encryption operation

### Session Validation
- Key validation happens locally against cached hash (no network required for validation)
- Device fingerprinting via User-Agent
- Session-specific salts prevent rainbow table attacks
- Lockout mechanism after failed login attempts (tracked in service worker)
- Cryptographically secure random number generation for salts

---

## Offline Workflow

### Phase 1: Going Offline

**User Actions:**
1. User navigates to home screen and selects cases
2. Clicks "Go Offline" button
3. Enters offline key in modal dialog
4. Confirms action

**System Actions:**
1. Validates case count against configuration limits:
   - `offline_mode_max_existing_cases` (default: 3)
   - `offline_mode_max_new_cases` (default: 3)
2. Performs key derivation (PBKDF2 + SHA-256)
3. Stores key hash for validation
4. Registers service worker if not already registered
5. Service worker caches required resources:
   - Static assets (HTML, CSS, JS, images, fonts)
   - Metadata and form specifications
   - Selected case documents (encrypted with AES-256-GCM)
   - API responses for offline views
   - User roles and jurisdiction data
   - Cache validation now happens in two stages:
     - before offline auth/session setup (`go_offline_pre_auth`)
     - again before the transition is finalized (`go_offline_precomplete`)
6. Service worker validates required cached resources using the shared manifest:
   - required static assets must return usable non-empty responses
   - required HTML routes must return `200` and a valid HTML shell
   - required API routes must satisfy endpoint-specific status/content/shape rules
   - example: `/api/jurisdiction_tree` is expected to be a `200` JSON payload with a `children` array; a `204 No Content` response should be treated as a failure, not a successful cache fill
7. Updates case documents in main database:
   - Sets `is_offline: true`
   - Sets `offline_by: username`
   - Sets `offline_date: ISO8601 timestamp`
7. Creates offline session document in `offline_cases` database:
   ```javascript
   {
     "_id": "unique_session_guid",
     "user_name": "username",
     "offline_state": 1, // 1 = active
     "offline_ids": ["case_id_1", "case_id_2"],
     "case_documents": [],
     "date_created": "ISO8601",
     "device_info": "User-Agent string"
   }
   ```
8. Sets localStorage flags:
   - `is_offline: "true"`
   - `offline_session_id: "session_guid"`
   - `mmria_offline_session: {sessionData}`
9. Reloads page to activate offline mode

### Phase 2: While Offline

**Case Access:**
- Service worker intercepts all HTTP requests
- For case data requests (`/api/case?case_id=X`):
  1. Checks if case is in cache
  2. If encrypted (`X-Offline-Encrypted: 1` header), decrypts using in-memory key
  3. Serves decrypted JSON to application
  4. Falls back to network if not in cache (will fail if truly offline)

**Editing Existing Cases:**
- User opens and edits case forms normally
- On each field change:
  1. `offline-change-tracker.js` captures:
     - Field metadata path (e.g., `/home_record/first_name`)
     - Old value
     - New value
     - Timestamp
     - User name
  2. Change accumulated in `g_offline_changes` Map (in memory)
  3. Updated document sent to service worker for re-encryption and cache update
  4. Change stack persisted to localStorage as backup
- All form validation and business logic continues to work normally

**Creating New Cases:**
- User clicks "Add New Case" button (limited by `offline_mode_max_new_cases`)
- System generates temporary case ID
- Record ID assigned with `-offline` suffix (e.g., `2024-US-001-offline`)
- New case cached and encrypted like edited cases
- Marked with `is_offline: true` and creation metadata

**Persistence Across Sessions:**
- User can close browser completely
- User can restart computer
- User can switch to airplane mode
- On return:
  1. Service worker detects existing session from cache
  2. User must re-enter offline key (key not persisted)
  3. Key validates against stored hash
  4. Cases decrypted and available for continued work

**Network Monitoring:**
- `offline-network-monitor.js` polls connectivity every interval
- Tests endpoint: `/api/OfflineCase/connectivity-check`
- Updates "Go Online" button state:
  - **Enabled**: Network available
  - **Disabled**: No network detected
- Visual indicator shows offline status in UI

**Debug Logging:**
- If `is_offline_logging_enabled: true`:
  - All operations logged to localStorage
  - Max logs: `offline_logging_max_logs` (default: 10,000)
  - Logs include timestamps, module names, and detailed messages
  - Debug modal accessible for troubleshooting
  - Logs synced to server when returning online

**Offline Integrity Monitoring:**
- While offline, the validator periodically re-checks the current offline state (`offline_monitor`).
- This is intended to catch:
  - cache clear/manual cache tampering
  - missing `mmria_offline_session`
  - drift between localStorage state and cached session payload
  - missing/invalid required manifest-backed routes or API responses
- The runtime checks are intentionally stricter than simple "cache exists" checks. A cached response only counts as healthy if it still matches the manifest expectation for that asset/route.
- If a steady-state offline check fails with blocking enabled, the app now surfaces the Go Online failure modal rather than letting the user continue to work in a corrupted state. This is meant to stop users/testers from wasting time once the browser session has drifted or been damaged while still requiring an explicit user acknowledgment.

### Phase 3: Different Browser/Cache Clear Warning

**Scenario**: User attempts to sign in from different browser or after clearing cache

**Warning Modal Displayed:**
> "You have an active offline session. Continuing will cause you to lose your offline session and any unsaved changes. Do you want to continue?"

**User Options:**
1. **Cancel**: Return to current session, don't lose data
2. **Continue**: Acknowledge data loss, proceed with new session

**Technical Details:**
- Detection: Server checks for active session via `/api/OfflineCase/active-user-session`
- Session validation: Compares device fingerprint and session metadata
- Cache check: Service worker verifies presence of encrypted case data
- Data loss: Original encrypted cases become inaccessible without original cache

### Phase 4: Returning Online

**User Actions:**
1. User clicks "Go Online" button (enabled when network available)
2. System shows "Going Online..." modal

**System Actions:**

**Step 1: Pre-flight Checks**
- Network connectivity verified via `/api/OfflineCase/connectivity-check`
- Active session validation
- Modified case count calculated

**Step 2: Save Cases to offline_cases Database**
- Endpoint: `POST /api/OfflineCase/update-cases/{sessionId}`
- Payload includes:
  ```javascript
  {
    "OfflineSessionId": "session_guid",
    "CaseDocuments": [
      {
        "documentId": "case_id",
        "modifiedDocument": { /* full case data */ },
        "syncState": 1, // 1 = pending sync
        "changeStackItems": [
          {
            "_id": "case_id",
            "_rev": "revision",
            "object_path": "home_record.first_name",
            "metadata_path": "/home_record/first_name",
            "old_value": "Jane",
            "new_value": "Janet",
            "dictionary_path": "/home_record/first_name",
            "metadata_type": "string",
            "prompt": "First Name",
            "date_created": "ISO8601",
            "user_name": "username"
          }
          // ... more field changes
        ]
      }
      // ... more cases
    ]
  }
  ```
- Server updates offline session document with all modifications
- Response indicates success and sets `offline_state: 1` (processing)

**Step 3: Transition Service Worker**
- Set localStorage: `process_offline_cases: "true"`
- Clear offline mode flags: `is_offline`, `has_active_offline_session`
- Send message to service worker: `CLEAR_CACHES`
- Wait for cache clearing to complete

**Step 4: Cleanup**
- Unregister service worker
- Clear offline session data from memory
- Clear cached cases from localStorage
- Stop service worker keep-alive interval
- Remove offline mode CSS class from body

**Step 5: Sync Logs (if enabled)**
- Upload debug logs to server
- Endpoint: `POST /api/log/offline-logs`
- Clear local log storage

**Step 6: Redirect**
- Redirect to `/account/login`
- User logs in normally
- System detects `process_offline_cases: "true"` flag
- Loads processing mode UI

### Phase 5: Processing Mode

After returning online, user enters "Processing Offline Cases" mode where they must resolve each modified case.

**UI Display:**
- Special table shows all modified cases
- Each row displays:
  - Case identifier (record ID)
  - Patient name
  - Modification type (Edited vs. Created)
  - Sync status
  - Action buttons

**For Edited Cases (Existing Records):**

**Option 1: Upload (Sync to Server)**
- User clicks "Upload" button
- System performs sync operation:

  1. **Retrieve Modified Document** from offline session
  2. **Fetch Current Document** from server: `GET /api/case?case_id=X`
  3. **Revision Conflict Check**:
     - Compare `_rev` of cached document vs. current server document
     - If mismatch detected (admin unlocked case):
       - Show modal: "This case was modified by an administrator while offline"
       - Automatically abandon changes (syncState: 4)
       - Unlock case in database
       - Exit sync process
     - If match: proceed with sync
  4. **Prepare Save Request**:
     - Clear offline flags: `is_offline: false`, `offline_by: null`, `offline_date: null`
     - Remove `-offline` suffix from `record_id` if present
     - Set `date_last_updated` and `last_updated_by`
     - Include accumulated change stack items from offline tracking
  5. **Save to Server**: `POST /api/case`
     - Payload: `{ Case_Data: {...}, Change_Stack: {...} }`
     - Change stack includes all field-level modifications for audit trail
  6. **Update Sync Status**: `POST /api/OfflineCase/update-sync-status`
     - Set `syncState: 0` (completed/released)
  7. **Remove from Processing Table**
  
- **Success**: Case synced, unlocked, available to all users
- **Failure**: Error modal, case remains in processing queue

**Option 2: Abandon Changes**
- User clicks "Abandon Changes" button
- Confirmation modal: "Are you sure? All offline changes will be lost."
- System performs abandon operation:
  
  1. **Update Sync Status**: `POST /api/OfflineCase/update-sync-status`
     - Set `syncState: 2` (abandoned)
  2. **Fetch Original Document**: `GET /api/case?case_id=X`
  3. **Clear Offline Flags**: 
     - Set `is_offline: false`, `offline_by: null`, `offline_date: null`
  4. **Save Cleared Document**: `POST /api/case`
     - Change stack notes: "Abandoned offline changes"
  5. **Remove from Processing Table**

- **Result**: Case unlocked, all offline changes discarded

**For Created Cases (New Records):**

**Option 1: Upload (Create in Database)**
- User clicks "Upload" button
- System creates new case:

  1. **Retrieve Modified Document** from offline session
  2. **Validate Record ID**:
     - Remove `-offline` suffix
     - Check for duplicates
  3. **Generate Permanent GUID** (if needed)
  4. **Save to Server**: `POST /api/case`
     - Full case data with all field values
     - Change stack notes case creation
  5. **Update Sync Status**: Set `syncState: 0` (completed)
  6. **Remove from Processing Table**

- **Result**: New case permanently saved to database

**Option 2: Delete (Abandon New Case)**
- User clicks "Delete" button
- Confirmation modal: "Delete this new case? This cannot be undone."
- System deletes case:

  1. **Update Sync Status**: Set `syncState: 3` (deleted)
  2. **Remove from Processing Table**
  3. **No database record created**

- **Result**: New case permanently discarded

**Bulk Operations:**
- No bulk upload/abandon buttons (intentional safety measure)
- User must process each case individually
- Prevents accidental mass data loss

**Processing Complete:**
- When all cases resolved (uploaded or abandoned)
- System clears `process_offline_cases` flag
- Deletes offline session document from `offline_cases` database
- User returns to normal operation mode

---

## Sync States Reference

| State | Value | Description | Use Case |
|-------|-------|-------------|----------|
| Released/Abandoned | 0 | Sync completed or session abandoned by system | Normal completion |
| Pending Sync | 1 | Active offline session, changes not yet synced | During offline work |
| Abandoned by User | 2 | User chose to discard changes | Manual abandon |
| Deleted | 3 | New offline case deleted by user | New case abandoned |
| Released by Admin | 4 | Admin unlocked case, causing revision conflict | Conflict resolution |
| No Changes | 5 | Case taken offline but not modified | Unedited case |

---

## Configuration Options

Configuration settings found in `appsettings.json`, accessed via `CaseController.cs`:

```json
{
  "is_offline_mode_enabled": true,
  "offline_mode_max_new_cases": 3,
  "offline_mode_max_existing_cases": 3,
  "is_offline_logging_enabled": true,
  "offline_logging_max_logs": 10000
}
```

**Configuration Details:**

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `is_offline_mode_enabled` | boolean | `false` | Master switch for offline mode feature |
| `offline_mode_max_new_cases` | integer | `3` | Maximum new cases user can create while offline |
| `offline_mode_max_existing_cases` | integer | `3` | Maximum existing cases user can take offline |
| `is_offline_logging_enabled` | boolean | `false` | Enable debug logging subsystem |
| `offline_logging_max_logs` | integer | `10000` | Maximum log entries to store before rotation |

**Configuration Access:**
- Loaded in `CaseController.Index()` method
- Passed to client via `TempData` and `ViewBag`
- Multi-tenant aware via `host_prefix`
- Can be overridden per jurisdiction

---

## Database Schema

### Case Document Fields (Main Database)

Standard case documents extended with offline fields:

```javascript
{
  "_id": "case_guid",
  "_rev": "1-abc123",
  // ... standard case fields ...
  "is_offline": false,           // boolean - case in offline mode
  "offline_date": null,           // ISO8601 or null - when taken offline
  "offline_by": null              // string or null - username
}
```

**CouchDB Design Document:**
- File: `database-scripts/case_design_sortable.json`
- Views updated to include offline fields in emit
- Enables filtering and sorting by offline status

### Offline Session Document (offline_cases Database)

```javascript
{
  "_id": "550e8400-e29b-41d4-a716-446655440000",  // GUID
  "_rev": "2-def456",
  "user_name": "abstractor@jurisdiction.gov",
  "offline_state": 1,              // 0=abandoned/released, 1=processing
  "offline_ids": [                 // Original case IDs taken offline
    "case_guid_1",
    "case_guid_2",
    "case_guid_3"
  ],
  "case_documents": [
    {
      "documentId": "case_guid_1",
      "modifiedDocument": {
        // Full case document with all modifications
        "_id": "case_guid_1",
        "_rev": "3-xyz789",
        "home_record": {
          "first_name": "Janet",  // Modified while offline
          // ... all other fields ...
        },
        // ... rest of case data ...
      },
      "syncState": 1,              // See Sync States Reference above
      "changeStackItems": [
        {
          "_id": "case_guid_1",
          "_rev": "3-xyz789",
          "object_path": "home_record.first_name",
          "metadata_path": "/home_record/first_name",
          "old_value": "Jane",
          "new_value": "Janet",
          "dictionary_path": "/home_record/first_name",
          "metadata_type": "string",
          "prompt": "First Name",
          "date_created": "2024-11-15T14:32:00Z",
          "user_name": "abstractor@jurisdiction.gov"
        },
        // ... more field changes ...
      ]
    },
    // ... more modified cases ...
  ],
  "date_created": "2024-11-15T10:00:00Z",
  "date_last_updated": "2024-11-15T14:32:00Z",
  "device_info": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)..."
}
```

**Field Explanations:**
- `offline_state`: Controls whether session is active (1) or completed (0)
- `offline_ids`: Tracks which cases were originally selected for offline work
- `case_documents`: Array of modified cases with full data and change tracking
- `syncState`: Per-case status during processing (see Sync States Reference)
- `changeStackItems`: Audit trail of field-level changes for each case

---

## API Endpoints

### OfflineCaseController Endpoints

| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| GET | `/api/OfflineCase/cache-version` | Returns cache version for service worker | Yes |
| POST | `/api/OfflineCase` | Creates new offline session | Yes |
| GET | `/api/OfflineCase/by-session/{id}` | Retrieves offline session data | Yes |
| GET | `/api/OfflineCase/active-user-session` | Checks for active offline session for current user | Yes |
| GET | `/api/OfflineCase/all-active-sessions` | Lists all active offline sessions (admin) | Yes |
| GET | `/api/OfflineCase/lightweight-status-only` | Quick status check without full data | Yes |
| POST | `/api/OfflineCase/update-cases/{id}` | Saves modified cases when returning online | Yes |
| POST | `/api/OfflineCase/update-sync-status` | Updates individual case sync state | Yes |
| DELETE | `/api/OfflineCase/{documentId}` | Deletes offline session document | Yes |
| GET | `/api/OfflineCase/connectivity-check` | Network connectivity test (200 OK response) | Yes |

**Authentication:**
- All endpoints require authentication
- Roles: `abstractor`, `data_analyst` (most endpoints)
- Role: `offline_mode` (update-cases endpoint)

**Request/Response Examples:**

**Create Offline Session:**
```http
POST /api/OfflineCase
Content-Type: application/json

{
  "caseIds": ["case_guid_1", "case_guid_2"],
  "offlineKey": "(never transmitted - only hash)",
  "deviceInfo": "User-Agent string"
}

Response:
{
  "ok": true,
  "id": "session_guid",
  "rev": "1-abc123"
}
```

**Update Cases (Return from Offline):**
```http
POST /api/OfflineCase/update-cases/session_guid
Content-Type: application/json

{
  "OfflineSessionId": "session_guid",
  "CaseDocuments": [/* array of modified cases */]
}

Response:
{
  "message": "Case documents saved successfully",
  "offlineCaseId": "session_guid",
  "documentCount": 3,
  "revision": "2-def456",
  "offline_state": 1,
  "shouldSetProcessOffline": true
}
```

---

## User Flows & Edge Cases

### Flow 1: Normal Operation (Happy Path)

1. User selects 3 cases → Goes offline → Works for 2 hours
2. Edits all 3 cases, creates 1 new case
3. Closes browser, goes home
4. Next day: Opens browser → Enters offline key → Continues work
5. Returns online when network available
6. Uploads all 4 cases successfully
7. Returns to normal operation

**Result**: ✅ All changes saved, cases unlocked

### Flow 2: Browser Cache Cleared

1. User goes offline with 2 cases
2. Edits both cases
3. Accidentally clears browser data/cache
4. Attempts to log in
5. System detects active session but no local cache
6. Warning modal: "You will lose your offline session"
7. User clicks Cancel → Cannot proceed
8. User contacts admin to release session

**Result**: ⚠️ Data loss - changes cannot be recovered without cache

### Flow 3: Service Worker Restart

1. User in offline mode, actively editing
2. Browser terminates service worker (memory pressure)
3. User tries to navigate to another case
4. Service worker restarts, loses encryption key from memory
5. System redirects to offline login page
6. User re-enters offline key
7. Key validates, cases decrypt, work continues

**Result**: ✅ No data loss - seamless recovery

### Flow 4: Admin Unlocks Case While User Offline

1. User takes Case A offline
2. Admin needs urgent access
3. Admin manually unlocks Case A (clears offline flags)
4. User returns online, attempts to upload Case A
5. System detects revision mismatch (current rev ≠ cached rev)
6. Shows modal: "Case was modified by administrator"
7. Automatically abandons changes (syncState: 4)
8. Case unlocked, user notified

**Result**: ⚠️ User changes lost (expected behavior for conflict)

### Flow 5: Network Loss During Transition

1. User clicks "Go Online"
2. Network connectivity check passes initially
3. During cache save: network drops
4. API call to `/api/OfflineCase/update-cases` fails
5. System shows error: "Failed to save to server"
6. Go Online button re-enabled
7. User waits for network
8. Tries again successfully

**Result**: ✅ Safe failure - user can retry

### Flow 6: Different Browser Attempt

1. User has active offline session in Chrome
2. Opens Firefox, navigates to app
3. System checks for active session via API
4. Detects existing session for user
5. Shows warning modal: "Active session exists"
6. User clicks Cancel
7. Continues work in Chrome

**Result**: ✅ Data protection - prevents split sessions

### Flow 7: Maximum Case Limits

1. User tries to select 5 existing cases (limit: 3)
2. "Go Offline" button remains disabled
3. UI shows: "Maximum 3 cases allowed"
4. User deselects 2 cases
5. Button enables, user proceeds

**Result**: ✅ Configuration enforced

### Flow 8: Creating Too Many New Cases

1. User offline with 2 existing cases
2. Creates 3 new cases (reaches limit)
3. Attempts to create 4th new case
4. "Add New Case" button disabled
5. UI shows: "Maximum 3 new cases allowed"
6. User must upload or delete before creating more

**Result**: ✅ Configuration enforced

---

## Key Technical Implementation Details

### Change Tracking Mechanism

**In-Memory Tracking (`g_offline_changes`):**
- JavaScript Map object stores changes by case ID
- Structure:
  ```javascript
  g_offline_changes = new Map([
    ["case_id_1", {
      documentId: "case_id_1",
      modifiedDocument: { /* full case */ },
      changeStackItems: [
        { metadata_path: "/home_record/first_name", old_value: "Jane", new_value: "Janet" },
        { metadata_path: "/home_record/date_of_death/year", old_value: "2023", new_value: "2024" }
      ]
    }]
  ]);
  ```

**Duplicate Prevention:**
- Changes tracked by `metadata_path`
- Multiple edits to same field: keeps only most recent
- Accumulates changes across multiple edit sessions
- Example: User changes field 3 times → only records first old_value and final new_value

**Persistence:**
- Changes saved to `localStorage.mmria_offline_changes` as backup
- Synced to service worker encrypted cache on each modification
- Sent to server in batch when returning online

### Record ID Handling

**Problem**: New offline cases need temporary IDs that don't conflict

**Solution**:
1. User creates new case: assigned temporary record ID
2. System appends `-offline` suffix: `2024-US-001-offline`
3. Case saved to cache and tracked with suffix
4. When uploaded: suffix removed before server save
5. Server validates uniqueness of final record ID
6. If conflict: user prompted to resolve

### Service Worker Keep-Alive

**Problem**: Browsers terminate idle service workers, losing encryption key

**Solution**:
1. Set interval: `setInterval(() => ping service worker, 10000)` (every 10 seconds)
2. Ping message: `{ type: 'KEEP_ALIVE' }`
3. Service worker responds, preventing termination
4. Interval cleared when returning online
5. If termination occurs anyway: user redirected to offline login

### Cache Versioning Strategy

**Problem**: Hardcoded cache versions become stale

**Solution**:
1. Server maintains single source of truth: `/api/OfflineCase/cache-version`
2. Returns: `{ baseVersion: "v38-stable", buildTimestamp: "..." }`
3. Service worker fetches on install
4. Cache names include version: `mmria-static-v38-stable-session123`
5. Version mismatch: old caches automatically deleted
6. Ensures consistency across deployments

### Session-Specific Caching

**Problem**: Multiple users on same device could see each other's data

**Solution**:
1. Each offline session gets unique cache set
2. Cache names include session ID: `mmria-api-v38-stable-session_guid`
3. Service worker scopes to current session only
4. Different sessions completely isolated
5. Logout clears session-specific caches only

### Global Variables Used

JavaScript global state (defined in `app.mmria.js`):

```javascript
// Change tracking
window.g_offline_changes = new Map();
window.g_original_offline_documents = new Map();

// Offline mode flags
window.g_network_connected = true;
window.g_offline_operation_in_progress = false;
window.g_processing_operation_in_progress = false;

// Service worker reference
window.g_service_worker_keep_alive_interval = null;

// Case index for navigation
window.g_offline_case_index_map = [];

// Release version for change stack
window.g_release_version = "v38";

// Current user
window.g_user_name = "user@example.com";
```

---

## Testing Offline Mode

### Developer Testing Checklist

**Phase 1: Going Offline**
- [ ] Select cases within configured limits
- [ ] Verify "Go Offline" button enables
- [ ] Enter valid offline key
- [ ] Confirm service worker registers
- [ ] Check cases cached in DevTools → Application → Cache Storage
- [ ] Verify cases marked `is_offline: true` in database
- [ ] Confirm offline session created in `offline_cases` database

**Phase 2: Offline Operations**
- [ ] Disconnect network completely
- [ ] Open cached cases - should load instantly
- [ ] Edit multiple fields in different sections
- [ ] Create new case (check `-offline` suffix)
- [ ] Close browser completely
- [ ] Reopen, enter offline key
- [ ] Verify all changes preserved
- [ ] Check change tracking in localStorage

**Phase 3: Returning Online**
- [ ] Reconnect network
- [ ] Verify "Go Online" button enables
- [ ] Click "Go Online"
- [ ] Confirm cases saved to `offline_cases` DB
- [ ] Check redirect to login
- [ ] Verify `process_offline_cases` flag set

**Phase 4: Processing**
- [ ] Login normally
- [ ] Verify processing mode UI appears
- [ ] Upload edited case - check database updated
- [ ] Abandon edited case - verify changes discarded
- [ ] Upload new case - verify record created
- [ ] Delete new case - verify not in database
- [ ] Confirm all cases cleared from processing queue

**Phase 5: Edge Cases**
- [ ] Try different browser - verify warning
- [ ] Clear cache while offline - verify data loss warning
- [ ] Simulate service worker restart
- [ ] Test network loss during transition
- [ ] Attempt to exceed case limits
- [ ] Try invalid offline key login

### Browser DevTools Inspection

**Check Encrypted Cache:**
```javascript
// In browser console
caches.open('mmria-api-v38-stable-session_guid').then(cache => 
  cache.match('/api/case?case_id=CASE_ID').then(response => 
    console.log('Encrypted:', response.headers.get('X-Offline-Encrypted'))
  )
);
```

**View Change Tracking:**
```javascript
// In browser console
console.log('Changes:', JSON.parse(localStorage.getItem('mmria_offline_changes')));
console.log('Session:', JSON.parse(localStorage.getItem('mmria_offline_session')));
```

**Monitor Service Worker:**
```javascript
// In browser console
navigator.serviceWorker.controller.postMessage({ type: 'DEBUG_STATUS' });
// Check console for service worker logs
```

---

## Troubleshooting Guide

### Issue: "Encryption key missing" error

**Symptoms**: Cannot open cases, error in console about missing `offlineCryptoKey`

**Cause**: Service worker restarted, lost in-memory encryption key

**Solution**:
1. User will be redirected to offline login
2. Re-enter offline key
3. Service worker re-initializes encryption
4. Cases decrypt and become accessible

### Issue: Cases won't upload, revision conflict

**Symptoms**: Upload fails with "Case modified by administrator" message

**Cause**: Admin manually unlocked case while user offline

**Solution**:
1. Changes automatically abandoned (syncState: 4)
2. Case unlocked in database
3. User must re-enter changes after returning online
4. **Prevention**: Communicate before unlocking offline cases

### Issue: "Go Online" button disabled

**Symptoms**: Button grayed out, tooltip says "No network"

**Cause**: Network connectivity check failing

**Solution**:
1. Verify internet connection
2. Check firewall/proxy settings
3. Test endpoint: `curl https://yourserver/api/OfflineCase/connectivity-check`
4. Wait for network monitor to detect connectivity (polls every 30 seconds)

### Issue: Data lost after cache clear

**Symptoms**: Cases not in cache, offline session exists in database

**Cause**: User cleared browser data/cache

**Solution**:
1. **Data cannot be recovered** from browser
2. Check if admin has backup of cases
3. Admin must manually clear offline flags in database:
   ```javascript
   // In CouchDB
   doc.is_offline = false;
   doc.offline_by = null;
   doc.offline_date = null;
   ```
4. **Prevention**: Warn users not to clear cache during offline mode

### Issue: Stuck in processing mode

**Symptoms**: Processing UI won't clear, even after all cases resolved

**Cause**: `process_offline_cases` flag not cleared

**Solution**:
1. Open browser console
2. Run: `localStorage.removeItem('process_offline_cases');`
3. Refresh page
4. Should return to normal mode

### Issue: Cannot create more new cases

**Symptoms**: "Add New Case" button disabled

**Cause**: Reached `offline_mode_max_new_cases` limit

**Solution**:
1. Upload or delete existing offline-created cases
2. Or increase configuration limit (requires admin)
3. Return online to reset counter

---

## Security Considerations

### Threat: Offline Key Brute Force

**Mitigation**:
- PBKDF2 with 100,000 iterations slows attacks
- Lockout after failed attempts (tracked in service worker)
- Key never transmitted over network
- Per-session salts prevent rainbow tables

### Threat: Browser Cache Extraction

**Mitigation**:
- All case data encrypted with AES-256-GCM
- Encryption key exists only in volatile memory
- Key lost on service worker restart
- Authenticated encryption prevents tampering

### Threat: Service Worker Compromise

**Mitigation**:
- Service worker served over HTTPS only
- Content Security Policy (CSP) restricts execution
- Service worker scope limited to app origin
- Regular security audits of service worker code

### Threat: Man-in-the-Middle During Transition

**Mitigation**:
- All API calls over HTTPS/TLS
- Certificate pinning (if configured)
- Data encrypted at rest before transmission
- Change stack provides audit trail

### Threat: Unauthorized Session Access

**Mitigation**:
- Device fingerprinting via User-Agent
- Session-specific cache isolation
- Active session tracking in database
- Warning on different browser attempt

---

## Performance Considerations

### Initial Cache Population
- **Time**: 5-30 seconds depending on case count and size
- **Size**: ~2-5 MB per case (encrypted)
- **Network**: Downloads metadata, specifications, cases in parallel
- **Optimization**: Cases cached on-demand, not all at once

### Cache Storage Limits
- **Quota**: Browser-dependent (typically 50% of available disk)
- **Monitoring**: Service worker tracks cache size
- **Eviction**: Oldest caches deleted if quota exceeded
- **Recommendation**: Limit to 10-15 cases per session

### Service Worker Lifecycle
- **Install**: 1-3 seconds
- **Activation**: Immediate (skipWaiting)
- **Termination**: After ~30 seconds idle (browser-dependent)
- **Keep-Alive**: Ping every 10 seconds prevents termination

### Encryption Performance
- **Algorithm**: AES-GCM hardware-accelerated in modern browsers
- **Overhead**: <100ms per case encrypt/decrypt
- **Memory**: Negligible (streaming encryption)
- **CPU**: Minimal impact on battery life

---

## Future Enhancements (Potential)

- **Sync Conflict Resolution UI**: Show diff between offline and server versions
- **Partial Case Upload**: Upload specific forms instead of entire case
- **Background Sync API**: Auto-sync when network returns (Service Worker API)
- **IndexedDB Storage**: Alternative to Cache API for structured data
- **Progressive Web App (PWA)**: Install as native app
- **Multi-Device Sync**: Share offline session across devices (complex security implications)
- **Compression**: Compress case data before encryption (reduce cache size)
- **Offline-First Default**: Make offline mode default, sync in background

---

## Related Documentation

- [MMRIA Background Jobs Documentation](./MMRIA_Background_Jobs_Documentation.md) - Background services and scheduled tasks
- [AI Context](./AI_CONTEXT.md) - AI-assisted development guidelines
- Database Scripts: `database-scripts/offline_design_sortable.json`
- Configuration: `appsettings.json` - Offline mode settings

---

## Glossary

| Term | Definition |
|------|------------|
| **Service Worker** | JavaScript proxy that intercepts network requests, enables offline functionality |
| **Cache API** | Browser API for storing HTTP responses in cache |
| **PBKDF2** | Password-Based Key Derivation Function 2 - industry standard key derivation |
| **AES-GCM** | Advanced Encryption Standard in Galois/Counter Mode - authenticated encryption |
| **Sync State** | Enumeration tracking status of offline case during processing (0-5) |
| **Change Stack** | Audit trail of field-level changes made to case |
| **Offline Session** | Document in `offline_cases` DB tracking user's offline work |
| **Revision Conflict** | Case modified on server while user has offline copy |
| **Keep-Alive** | Periodic message to prevent service worker termination |
| **Cache Version** | String identifying cache generation (e.g., "v38-stable") |

---

**Document Version**: 1.0  
**Last Updated**: 2026-02-10  
**Maintained By**: Development Team  
**Related Code Paths**:
- `/wwwroot/scripts/offline/` - Client-side modules
- `/wwwroot/service-worker.js` - Service worker implementation
- `/Controllers/api/OfflineCaseController.cs` - API endpoints
- `/util/OfflineSessionHelper.cs` - Server utilities
