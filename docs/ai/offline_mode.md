# MMRIA Offline Mode - Technical Documentation

- Status: Active
- Scope: Offline architecture, service worker caching, local encrypted storage, session integrity, and online/offline transition behavior.
- When to use: Read this before changing offline session handling, sync flows, service worker logic, or cached case behavior.
- Last verified: 2026-04-14


## Overview
Offline Mode enables users to take 0 to many cases into an offline state where they can work without internet connectivity. Cases are stored encrypted at rest using AES-256-GCM encryption with PBKDF2 key derivation. Users can edit existing cases and create new cases while offline, with all changes automatically tracked and synchronized when returning online.

---

## Core Architecture Components

### 1. Client-Side Modules (JavaScript)
Located in `/wwwroot/scripts/offline/`:

| Area | Primary modules | Why they matter |
|--------|---------|---------|
| Session lifecycle | `offline-session-manager.js`, `offline-session-validator.js`, `offline-inactivity-manager.js`, `offline-navigation-manager.js` | Own the offline session state machine, idle timeout enforcement, and re-auth routing. |
| Transition and sync | `offline-transition-manager.js`, `offline-sync-manager.js`, `offline-case-manager.js`, `offline-change-tracker.js` | Drive Go Offline / Go Online flow, case selection, local change capture, and upload preparation. |
| Service-worker bridge | `service-worker-manager.js`, `offline-integrity-validator.js`, `offline-cache-manifest.js` | Coordinate cache population, cache validation, and encrypted case access. |
| UI and diagnostics | `offline-ui-renderer.js`, `offline-modals.js`, `offline-status-manager.js`, `offline-logout-button.js`, `offline-network-monitor.js`, `offline-logger.js`, `offline-debug-modal.js`, `offline-utils.js` | Surface offline state, connectivity, debugging, and crypto/helper behavior to the user and developers. |

### 2. Service Worker (`/wwwroot/service-worker.js`)
- Handles offline cache population, encrypted case storage, cache-version checks, and request interception.
- Keeps the crypto key only in service-worker memory; a restart requires offline re-auth before cached cases can be decrypted again.
- Must distinguish between session-scoped case caches and broader static/API caches so one session cannot read another session's encrypted payloads.

### 2a. Shared Cache Contract (`/wwwroot/scripts/offline/offline-cache-manifest.js`)
- The service worker and integrity validator share one manifest instead of maintaining separate hard-coded expectations.
- The manifest distinguishes required static assets, HTML routes, and API routes because each category has different validity checks.
- Keep the naming distinction straight:
  - `offline_session_id` identifies the offline session document and localStorage state.
  - cache names use a separate service-worker cache session id.
  - the validator matches them by checking session payload, cache naming, and expected case overlap together.

### 2b. Offline Integrity Validator (`/wwwroot/scripts/offline/offline-integrity-validator.js`)
- This is the main integrity gate for offline mode. It validates localStorage state, service-worker state, session-specific caches, cached session payloads, expected case ids, and manifest-backed required artifacts.
- It returns a structured diagnostic object rather than ad hoc booleans, which is why it is the right place to add new offline health checks.
- Important checkpoints:

| Checkpoint | Purpose |
| --- | --- |
| `go_offline_pre_auth` | Verifies cache readiness before offline auth/session creation. This guards the hosted-environment timing race where cache writes finish later than localhost. |
| `go_offline_precomplete` | Verifies the fully assembled offline session state before the transition is finalized. |
| `offline_monitor` | Periodic steady-state integrity check while offline. |
| `offline_login` | Blocks offline login if cached session artifacts are missing or corrupted. |
| `case_list_load`, `case_detail_load` | Narrower diagnostics during normal offline browsing and editing. |
| `go_online_preflight` | Validates the session before Go Online processing starts. |

- When `offline_monitor` finds a blocking failure in steady-state offline mode, the client now shows `show_go_online_failure_modal()` once and stops the monitor before recovery begins.

### 2c. Offline Inactivity Re-Auth

- Offline mode enforces a client-side inactivity timeout using `session_idle_timeout_minutes` while the browser is offline and actively authenticated.
- Activity is tracked through `localStorage["mmria_offline_last_activity_at"]`, which lets one active tab keep the offline session alive for the browser profile.
- When the timeout is exceeded, the client asks the service worker to drop the in-memory key, marks the session as no longer actively authenticated, and redirects to `/Account/OfflineLogin`.
- This is intentionally separate from the narrow offline server auth token, which exists only for the initial Go Online handoff before the browser returns to normal `/Account/AutoLogin` authentication.
- Logging should stay high-signal: milestone summaries, validator outcomes, warnings, and errors are valuable; per-request noise is not.

### 2d. Go Offline Cache Completion Handshake

The Go Offline transition now uses explicit service-worker acknowledgments and a cache-readiness barrier before `go_offline_pre_auth`.

- `ServiceWorkerManager.prefetchCases()` waits for an acknowledgment that each case was written and re-read from cache.
- `ServiceWorkerManager.cacheMetadata()` waits for an explicit success/failure response instead of sleeping for a fixed delay.
- `ServiceWorkerManager.waitForCacheReadiness()` blocks until required static assets, route aliases, API entries, expected case documents, and the cached offline-session payload are all present.

This barrier exists because cloud-hosted deployments can be slow enough that a validator run immediately after background cache requests will see a partial cache even though the service worker is still working.

### 2e. Case Fetch Rule During Go Offline

`/api/case?case_id=...` must remain network-only until offline mode is fully established.

- During Go Offline setup there should be no legitimate case reads from cache yet.
- Case prefetch may populate the future session cache, but the service worker must not serve case reads from cache until both `is_offline` and `has_active_offline_session` are true.
- This rule prevents the hosted-environment race where an early cache-first `/api/case` read can hit an encrypted response from the wrong session and fail with `offline_decrypt_failed`.
- Once steady-state offline mode is active, case reads must come from the active session API cache only, not from global cache lookup across all caches.

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
- Offline login validation is enforced by the service worker via `VALIDATE_OFFLINE_KEY`
- Lockout mechanism after 3 failed login attempts is tracked per offline session in the service worker cache
- Lockout duration is 2 hours, and the offline login page shows remaining attempts or remaining lockout time inline
- After the 2-hour lockout window expires, the failed-attempt counter resets and the user gets a fresh set of attempts
- When the offline login page loads during an active lockout, it immediately shows the lockout banner before another submit
- When cached offline cases exist but the in-memory offline crypto key is missing, the service worker now returns `401 { error: "offline_key_required" }` for case-list reads instead of `200` with an empty list.
- Offline case-list and case-detail clients redirect that state to `/Account/OfflineLogin` so cached encrypted cases do not silently disappear behind a blank list.
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
   - Sets `offline_lock_type: 1` for the pre-session soft lock
   - Sets `offline_by_tab_id` to the current browser tab id
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
   - Session creation now also carries the initiating browser tab id internally so the server can enforce the one-tab offline rule without changing the public response contract.
8. Sets localStorage flags:
   - `is_offline: "true"`
   - `offline_session_id: "session_guid"`
   - `mmria_offline_session: {sessionData}`
9. Reloads page to activate offline mode

### One-Tab Offline Rule
- Soft locks for offline mode are enforced per browser tab, not just per user.
- `POST /api/case/toggle-offline/{caseId}` add operations require a `tab_id`.
- The server rejects a soft-lock add when the same user already owns any offline soft lock from another tab.
- `goOfflineFinal()` posts the current `tab_id` to `POST /api/OfflineCase`.
- The server rejects offline-session creation when the same user:
  - has offline soft locks owned by another tab, or
  - already has an active offline session owned by another tab.
- Case editing is also tab-scoped for offline-owned cases:
  - the client blocks `Enable Edit` when `is_offline == true`, `offline_by == current user`, and `offline_by_tab_id` belongs to another tab
  - the server blocks `SaveCaseAsync` for the same condition so the rule still applies if client checks are bypassed
- Unload cleanup (`/api/case/finalize-unload`) removes offline soft locks only when `offline_by_tab_id` matches the current tab.
- UI behavior reuses `show_locked_case_modal()`:
  - `show_add_offline_softlock_tab_conflict_modal(caseId)` for add-to-offline conflicts
  - `show_go_offline_tab_conflict_modal()` for go-offline transition conflicts
  - `show_edit_offline_case_tab_conflict_modal(caseId)` for edit/save conflicts on offline-owned cases from another tab

### Phase 2: While Offline

| Concern | Current behavior |
| --- | --- |
| Case reads | The service worker intercepts requests and serves decrypted case JSON only from the active session cache; a cache miss falls back to network and will fail if the browser is truly offline. |
| Editing existing cases | Field changes are captured into `g_offline_changes`, mirrored to local backup state, and written back through the service worker so the cached case stays encrypted and current. |
| Creating new cases | New offline cases receive temporary record ids with the `-offline` suffix and stay in the same encrypted/session-scoped workflow as edited cases. |
| Resume after browser restart | The cached session can survive browser close or machine restart, but the user must re-enter the offline key because the decryption key is never persisted. |
| Connectivity state | `offline-network-monitor.js` polls `/api/OfflineCase/connectivity-check` and controls whether Go Online actions are enabled. |
| Debug logging | When enabled, offline logs are stored locally, shown in the debug modal, and uploaded during Go Online cleanup. |
| Integrity monitoring | `offline_monitor` periodically validates localStorage, session cache state, expected case ids, and manifest-backed responses. Blocking failures surface the Go Online failure modal instead of allowing continued work in a corrupted session. |

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

| Step | Current behavior |
| --- | --- |
| Pre-flight | Verify connectivity, confirm the active offline session, and calculate the modified case count. |
| Persist offline work | `POST /api/OfflineCase/update-cases/{sessionId}` saves the modified documents plus change stacks into the `offline_cases` session document and marks the session as processing. |
| Transition runtime state | Set `process_offline_cases`, clear active offline flags, ask the service worker to clear caches, and wait for cleanup to finish. |
| Cleanup | Unregister the service worker, clear runtime-only local state, stop keep-alive timers, and remove offline UI state. |
| Optional log upload | If offline logging is enabled, upload logs before clearing local log state. |
| Return to normal auth | Redirect through `/Account/AutoLogin`; after login, the app detects processing mode and shows the offline-processing UI. |

### Phase 5: Processing Mode

After returning online, user enters "Processing Offline Cases" mode where they must resolve each modified case.

Authentication boundary during processing mode:
- The narrowed offline server session is only allowed to call `POST /api/OfflineCase/update-cases/{sessionId}`.
- The remaining processing APIs (`active-user-session`, `sync-case`, `update-sync-status`, `update-offline-state`, `release-case-locks`, `recover-softlocks`, and generic `/api/case` reads/writes) are expected to run only after normal login is restored.
- If the browser is still carrying the narrow offline token when processing-mode pages or recovery flows load, the client redirects through `/Account/AutoLogin` before those APIs are called.

| Case type | Action | Current behavior |
| --- | --- | --- |
| Existing case | Upload | Load the cached modified document, compare revisions with the current server copy, and save through the normal case pipeline if no admin/conflict mismatch is found. On success, clear offline flags and mark the processing item complete. |
| Existing case | Abandon | Mark the item abandoned, reload the server copy, clear offline fields, and discard the cached offline edits. |
| New offline case | Upload | Remove the `-offline` suffix, validate the final identifier, create the record in the main database, and mark the processing item complete. |
| New offline case | Delete | Mark the processing item deleted and discard the offline-only record without creating a server-side case. |

- Bulk actions remain intentionally unsupported because offline-processing mode is designed to favor explicit per-case decisions over fast mass operations.
- When all items are resolved, the app clears `process_offline_cases`, deletes the session document, and returns to normal runtime state.

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

Standard case documents are extended with offline ownership and lock fields such as `is_offline`, `offline_date`, `offline_by`, `offline_lock_type`, and `offline_by_tab_id` so offline selection, cleanup, and cross-tab enforcement can be coordinated against the normal case record.

**CouchDB Design Document:**
- File: `database-scripts/case_design_sortable.json`
- Views updated to include offline fields in emit
- Enables filtering and sorting by offline status

### Offline Session Document (offline_cases Database)

The `offline_cases` document is the durable session envelope. It stores:

- session identity and ownership (`_id`, `user_name`, `device_info`)
- overall session state (`offline_state`)
- the original selected case ids (`offline_ids`)
- per-case modified documents plus `syncState`
- accumulated `changeStackItems` used for audit and upload processing
- created/updated timestamps used for monitoring and cleanup

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
| GET | `/api/OfflineCase/connectivity-check` | Network connectivity test (200 OK response) | No |

**Authentication:**
- All protected endpoints require authentication
- Roles: `abstractor`, `data_analyst` (most endpoints)
- Role: `offline_mode` is intentionally limited to `POST /api/OfflineCase/update-cases/{id}`
- `GET /api/OfflineCase/connectivity-check` is anonymous

---

## User Flows & Edge Cases

| Scenario | Expected outcome |
| --- | --- |
| Normal offline session, restart, and upload | User can re-enter the offline key, continue work, and later upload or abandon each case in processing mode. |
| Browser cache cleared or different browser used | The server may still know about the active session, but the encrypted browser cache is gone; the user gets a warning and the session cannot be resumed from that browser state. |
| Service worker restart | The key is lost, the user is redirected to offline login, and work resumes after re-auth if the cache is intact. |
| Admin unlock or server-side revision drift | Upload detects the mismatch and abandons the offline edits for that case rather than forcing an unsafe merge. |
| Network loss during Go Online | The transition fails safely and can be retried once connectivity returns. |
| Case-count limits reached | Go Offline or Add New Case remains disabled until the selection/count is back within configured bounds. |

---

## Key Technical Implementation Details

### Change Tracking Mechanism

- `g_offline_changes` is the client-side source for per-case modified documents plus field-level change stacks.
- Change deduplication is metadata-path based: repeated edits to the same field preserve the original `old_value` and latest `new_value`.
- The same change state is mirrored into localStorage and encrypted cache so browser refreshes and offline resumes can reconstruct the working set.

### Record ID Handling

- New offline cases use temporary record ids with the `-offline` suffix so they cannot collide with real server-side records during disconnected work.
- The suffix is removed only when the user uploads the case back to the server and the final identifier passes normal uniqueness checks.

### Service Worker Keep-Alive

- Browsers can terminate an idle service worker and thereby drop the in-memory crypto key.
- A keep-alive ping reduces that risk, but restart/re-auth still has to be treated as normal recovery behavior rather than something the client can prevent with certainty.

### Cache Versioning Strategy

- `/api/OfflineCase/cache-version` is the canonical source for the current cache generation.
- Cache names include both the base version and session identity so stale deployments can be cleaned up without mixing session data.

### Session-Specific Caching

- Each offline session gets its own cache namespace.
- The service worker should serve case data only from the active session cache, which is the main guardrail against cross-session data exposure on the same device.

### Global Variables Used

Key global state includes the change maps, offline/processing in-progress flags, connectivity state, service-worker keep-alive handle, offline case index map, and the current user/release markers used in change-stack generation.

---

## Testing Offline Mode

### Developer Testing Checklist

| Test area | Minimum checks |
| --- | --- |
| Go Offline | Select cases within limits, verify service-worker registration, confirm cached assets/cases, and verify offline flags/session creation in CouchDB. |
| Offline editing | Edit multiple fields, create a new offline case, restart the browser, re-enter the offline key, and confirm cached work resumes. |
| Go Online | Reconnect network, save modified cases into `offline_cases`, verify redirect through login, and confirm processing mode appears. |
| Processing mode | Upload and abandon an edited case, upload and delete a new case, and confirm the queue clears correctly. |
| Edge cases | Test different browser, cache clear, service-worker restart, transition-time network loss, case-count limits, invalid key attempts, and lockout-expiry behavior. |

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

| Issue | Likely cause | Response |
| --- | --- | --- |
| Missing encryption key / `offlineCryptoKey` error | Service worker restarted and lost the in-memory key | Redirect to offline login and re-enter the offline key. |
| Upload blocked by revision conflict | Admin or another server-side process changed the case while the user was offline | The case is abandoned with conflict state `4`; the user must re-enter the changes online if needed. |
| Go Online button disabled | Connectivity check is failing | Verify the network path to `/api/OfflineCase/connectivity-check` and wait for the monitor to re-enable the action. |
| Offline data lost after cache clear | Browser cache/storage was cleared | The encrypted browser copy is gone; clear the server-side offline flags/session and recover operationally, not by expecting the browser data back. |
| Stuck in processing mode | `process_offline_cases` flag or session cleanup did not finish | Verify processing completion, then clear the local flag and reload if the runtime state is stale. |
| Cannot create more offline cases | Configured new/existing-case limit reached | Upload, delete, or reduce selection count, or raise the configured limit if policy allows. |

---

## Security Considerations

| Threat | Mitigation summary |
| --- | --- |
| Offline key brute force | PBKDF2, per-session salts, and the offline-login lockout window slow repeated guessing attempts. |
| Browser cache extraction | Case data stays AES-256-GCM encrypted and the decryption key remains only in volatile service-worker memory. |
| Service worker compromise | HTTPS delivery, CSP restrictions, and origin scoping reduce the execution surface. |
| Transition-time interception | Go Offline / Go Online API traffic remains on HTTPS/TLS and the change stack preserves auditability. |
| Unauthorized session access | Session-specific cache isolation, active-session tracking, device/browser checks, and warning flows reduce split-session exposure. |

---

## Performance Considerations

| Area | Operational note |
| --- | --- |
| Initial cache population | Usually dominated by case count, case size, and hosted-environment latency. |
| Browser storage quota | Browser-dependent; session count and case volume should stay conservative to reduce quota and eviction risk. |
| Service-worker lifecycle | Keep-alive reduces termination risk, but restart/re-auth still has to be treated as normal recovery behavior. |
| Encryption overhead | Modern browsers generally keep AES-GCM overhead small relative to the network/cache work. |

---

## Related Documentation

- [MMRIA Background Jobs Documentation](./MMRIA_Background_Jobs_Documentation.md) - Background services and scheduled tasks
- [AI Context Index](./AI_CONTEXT.md) - AI-assisted development guidelines
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

## Recent Locking Changes

| Change | Why it matters |
| --- | --- |
| One-tab offline rule | Offline add/edit/session-entry flows are tab-scoped, so another tab for the same user cannot quietly take over a live offline lock. |
| Go-online cleanup for unchanged cases | Unchanged-case cleanup now uses an offline-session-specific release path instead of pushing full case saves through the normal `/api/case` pipeline. |
| Offline resume sync across tabs/browsers | `POST /api/OfflineCase/sync-case` supports the real recovery flow where the same user resumes processing from another tab/browser with the same valid offline session. |
| Failed go-offline and damaged-state recovery | `POST /api/OfflineCase/recover-softlocks` preserves or restores soft locks so bad transitions and damaged caches do not strand cases in hard-lock state. |
