# Offline Mode Refactoring Plan

## Overview
Extracting offline functionality from `app.mmria.js` (4,909 lines) into modular files.

## Phase 1: High-Impact Extractions (Largest, Most Isolated)

### 1. offline-sync-manager.js (~600 lines)
**Functions to extract:**
- `sync_offline_changes()` (225 lines)
- `abandon_offline_changes()` (157 lines)
- `delete_offline_changes()` (155 lines)
- `abandon_offline_session()` (38 lines)
- `SaveCaseAndReleaseOfflineLock()` (58 lines)
- `clear_offline_processing_mode()` (47 lines)
- `update_cached_case_document()` (28 lines)

**Dependencies:**
- Needs: `g_user_name`, `g_release_version`, `g_data`, `$mmria.get_new_guid()`, `g_ui`
- Needs: `show_message()`, `get_case_set()`, `get_offline_cases_by_session()`
- Needs: `show_revision_mismatch_modal()`, `g_offline_changes`, `save_offline_changes_to_storage()`

**Global exports:**
```javascript
window.OfflineSyncManager = {
    sync_offline_changes,
    abandon_offline_changes,
    delete_offline_changes,
    abandon_offline_session,
    clear_offline_processing_mode,
    update_cached_case_document
};
```

### 2. offline-modals.js (~400 lines)
**Functions to extract:**
- `show_revision_mismatch_modal()` / `close_revision_mismatch_modal()`
- `show_case_already_offline_modal()` / `close_case_already_offline_modal()`
- `show_case_already_online_modal()` / `close_case_already_online_modal()`
- `show_go_online_modal()` / `close_go_online_modal()` / `go_online_clicked()`
- `show_go_offline_modal()` / `close_go_offline_modal()`
- `show_set_offline_key_modal()` / `close_set_offline_key_modal()`
- `show_moving_to_offline_modal()` / `close_moving_to_offline_modal()` / `update_offline_modal_status()`
- `show_moving_to_online_modal()` / `close_moving_to_online_modal()`
- `show_abandon_case_modal()` / `close_abandon_case_modal()` / `confirm_abandon_case()`
- `handle_key_input()` / `validate_key_realtime()` / `validate_offline_key()`
- `enable_offline_cancel_button()` / `cancel_offline_transition()`

**Dependencies:**
- Needs: Modal creation utilities
- Needs: Key validation functions
- Needs: Various action handlers

**Global exports:**
```javascript
window.OfflineModals = {
    showRevisionMismatch,
    showCaseAlreadyOffline,
    showCaseAlreadyOnline,
    showGoOnline,
    showGoOffline,
    showSetOfflineKey,
    showMovingToOffline,
    showMovingToOnline,
    showAbandonCase,
    // ... all modal functions
};
```

### 3. offline-transition-manager.js (~800 lines)
**Functions to extract:**
- `go_offline_clicked()` (10 lines)
- `go_offline_final()` (36 lines)
- `attempt_offline_transition()` (306 lines - HUGE!)
- `setup_offline_session_auth()` (18 lines)
- `save_cached_cases_to_database()` (116 lines)
- `continue_to_set_key()` (7 lines)
- Crypto functions:
  - `generateSecureOfflineKeySalt()` (19 lines)
  - `deriveOfflineKeyHash()` (28 lines)
- `unregister_service_worker()` (27 lines)
- `clear_all_cached_data()` (48 lines)

**Dependencies:**
- Needs: ServiceWorkerManager
- Needs: Modal functions
- Needs: Crypto API
- Needs: All offline-related globals

**Global exports:**
```javascript
window.OfflineTransitionManager = {
    goOffline,
    goOnline,
    attemptOfflineTransition,
    cancelTransition,
    // ... transition functions
};
```

## Phase 2: Medium Impact

### 4. offline-change-tracker.js (~350 lines)
**Functions to extract:**
- `initialize_offline_change_tracking()`
- `track_offline_document_change()`
- `fetchAndStoreOriginalDocument()`
- `save_offline_changes_to_storage()`
- `get_all_offline_changes()`
- `clear_offline_changes()`
- State: `g_offline_changes`, `g_original_offline_documents`

### 5. offline-case-manager.js (~300 lines)
**Functions to extract:**
- `toggle_offline_status()`
- `remove_from_offline_list()`
- `refresh_offline_documents_list()`
- `get_offline_documents()`
- `get_offline_cases_by_session()`
- State: `g_offline_case_index_map`

## Phase 3: Low Impact

### 6. Service Worker Manager Enhancements
**Functions to move:**
- `fetchCacheVersionFromServer()` (from app.mmria.js)
- `getActualApiCacheName()` (from app.mmria.js)

### 7. offline-network-monitor.js (~150 lines)
**Functions to extract:**
- `check_network_connectivity()`
- `update_go_online_button_state()`
- `handle_network_status_change()`
- `initialize_network_monitoring()`
- State: `g_network_connected`

### 8. offline-session-validator.js (~100 lines)
**Functions to extract:**
- `get_offline_session_data()`
- `validate_offline_key_against_session()`
- `is_offline_mode()`

## Functions to Keep in app.mmria.js

**UI Rendering (must stay):**
- `app_render()` - Main entry point
- `render_offline_processing_item()`
- `render_offline_only_document_item()`
- `render_offline_document_item()`
- `hideOnlineCaseListingElements()`
- `showOnlineCaseListingElements()`
- All case listing rendering functions
- All search/filter UI functions

## Critical Global Variables

These need careful management:
- `g_user_name` - Used everywhere
- `g_release_version` - Used in sync
- `g_data` - Case data
- `g_ui` - UI state (including offline case lists)
- `g_offline_changes` - Will move to offline-change-tracker
- `g_original_offline_documents` - Will move to offline-change-tracker
- `g_offline_case_index_map` - Will move to offline-case-manager
- `g_network_connected` - Will move to offline-network-monitor

## Script Loading Order (Critical!)

```html
<!-- Core dependencies first -->
<script src="/scripts/service-worker-manager.js"></script>

<!-- Offline modules (can load in parallel after SW manager) -->
<script src="/scripts/offline/offline-change-tracker.js"></script>
<script src="/scripts/offline/offline-case-manager.js"></script>
<script src="/scripts/offline/offline-sync-manager.js"></script>
<script src="/scripts/offline/offline-modals.js"></script>
<script src="/scripts/offline/offline-transition-manager.js"></script>
<script src="/scripts/offline/offline-network-monitor.js"></script>
<script src="/scripts/offline/offline-session-validator.js"></script>

<!-- Finally, the main app -->
<script src="/scripts/editor/page_renderer/app.mmria.js"></script>
```

## Testing Checklist

After refactoring:
- [ ] Go offline works
- [ ] Go online works
- [ ] Sync offline changes works
- [ ] Abandon offline changes works
- [ ] Delete offline cases works
- [ ] New case creation offline works
- [ ] Edit existing case offline works
- [ ] Revision mismatch detection works
- [ ] Network monitoring works
- [ ] All modals display correctly
- [ ] No console errors
- [ ] Offline session persists across page refresh

## Risks & Mitigation

**Risk 1:** Breaking function dependencies
- **Mitigation:** Carefully track all cross-module dependencies, expose via window globals

**Risk 2:** Script loading order issues
- **Mitigation:** Document load order, use DOMContentLoaded for initialization

**Risk 3:** Global variable conflicts
- **Mitigation:** Namespace everything under window.Offline* objects

**Risk 4:** Lost functionality
- **Mitigation:** Test each feature after extraction, keep detailed checklist

## Success Criteria

1. app.mmria.js reduced from 4,909 lines to ~1,500-2,000 lines
2. All offline functionality works identically
3. Code is more maintainable and testable
4. Build succeeds without errors
5. All tests pass
