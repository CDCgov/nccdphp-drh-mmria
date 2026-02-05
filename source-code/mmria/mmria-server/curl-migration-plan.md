# cURL Migration Plan - mmria-server

**Analysis Date:** February 3, 2026  
**Status:** Current accurate assessment after filtering already-migrated files

## Executive Summary

### Statistics
- **Total Files with Active cURL Usage:** 14 files
- **Total Active cURL Instantiations:** 69 calls
- **Sync vs Async:** ~90% async (.executeAsync) / ~10% sync (.execute)
- **Controllers with cURL:** 2 files (3 calls) ⚠️ **CRITICAL - Architecture violation**

### Migration Effort Estimate
- **Total Estimated Hours:** 83 hours (~10 weeks for 1 developer at 8 hrs/week)
- **Priority 1 (Critical):** 4 hours
- **Priority 2 (High):** 23 hours
- **Priority 3 (Medium):** 52.5 hours
- **Priority 4 (Low):** 3.5 hours

---

## Detailed File Analysis

### Priority 1: CRITICAL - Auth & Controllers

High-traffic files that affect all user requests.

| File Path | cURL Count | Sync/Async | Complexity | Effort | Notes |
|-----------|------------|------------|------------|--------|-------|
| `CustomAuthHandler.cs` | 2 | Sync | Medium | 3 hrs | Lines 87, 154: Session GET/PUT - Auth handler affects all requests ⚠️ |
| `Controllers/api/versionController.cs` | 1 | Sync | Simple | 1 hr | Line 408: Metadata PUT - Already has _couchDbHttpClient DI ✅ |
| `Controllers/vitalsController.cs` | 0 | N/A | N/A | 0 hrs | Line 77: Already commented out ✅ |

**Subtotal:** 2 files, 3 calls, **4 hours**

**Risk:** CustomAuthHandler is highest priority - affects every authenticated request  
**Note:** Not refactoring controller architecture - just replacing cURL calls

---

### Priority 2: HIGH - Core Utilities (Infrastructure Backbone)

These utilities are called during critical operations like case saves and report generation.

| File Path | cURL Count | Sync/Async | Complexity | Effort | Notes |
|-----------|------------|------------|------------|--------|-------|
| `util/c_sync_document.cs` | 7 | Both | Complex | 10 hrs | Lines 76, 176, 217, 265, 312, 365, 421: Case sync, de-id, aggregates, reports |
| `util/c_sync_document.pmss.cs` | 7 | Both | Complex | 10 hrs | Lines 73, 173, 226, 266, 314, 361, 414: PMSS variant of above |
| `model/actor/quartz/Process_Central_Pull_list.cs` | 2 | Async | Medium | 3 hrs | Lines 203, 372: Central replication pull operations |

**Subtotal:** 3 files, 16 calls, **23 hours**

**Risk:** c_sync_document runs on EVERY case save - must be thoroughly tested

---

### Priority 3: MEDIUM - Export & Background Jobs

Export utilities run as background jobs and are less critical but heavily used.

| File Path | cURL Count | Sync/Async | Complexity | Effort | Notes |
|-----------|------------|------------|------------|--------|-------|
| `util/exporter/exporter.cs` | 11 | Both | Complex | 12 hrs | Lines 151, 155, 167, 404, 427, 1360, 1370, 1384, 1393: Main export engine |
| `util/exporter/mmrds_exporter.cs` | 8 | Both | Complex | 10 hrs | Lines 148, 297, 327, 1828, 1838, 1852, 1861: MMRDS format export |
| `util/core_element_export/core_element_exporter.cs` | 6 | Both | Medium | 6 hrs | Lines 121, 202, 235, 255, 798, 807: Core element export |
| `util/exporter/export_all_generate_name_map.cs` | 1 | Sync | Simple | 0.5 hrs | Line 49: Name mapping helper |
| `model/actor/quartz/Vital_Import_Synchronizer.cs` | 2 | Sync | Medium | 3 hrs | Lines 103, 252: Vital import batch sync |
| `model/actor/quartz/vital-import/BatchItemProcessor.cs` | 7 | Both | Complex | 8 hrs | Lines 80, 857, 9033, 9088, 9314, 9435, 9493: Individual item processing (note: some are external APIs) |
| `model/actor/quartz/vital-import/BatchProcessor.cs` | 3 | Async | Medium | 3 hrs | Lines 807, 836, 863: Batch coordination |
| `model/actor/quartz/vital-import/PMSS_ItemProcessor.cs` | 9 | Both | Complex | 10 hrs | Lines 62, 92, 177, 1235, 1879, 1930, 1988, 2350: PMSS data processing (note: some are external APIs) |

**Subtotal:** 8 files, 47 calls, **52.5 hours**

**Note:** Vital import actors use both `new cURL` and `new mmria.getset.cURL` patterns. Some calls are to external APIs (NIOSH/CVS) and should use HttpClient instead.

---

### Priority 4: LOW - One-off Utilities

| File Path | cURL Count | Sync/Async | Complexity | Effort | Notes |
|-----------|------------|------------|------------|--------|-------|
| `util/c_de_identifier.cs` | 0 (commented) | N/A | N/A | 0 hrs | Line 70: Already commented out ✅ |
| `util/JurisdictionAuthorizationRequirement.cs` | 0 (commented) | N/A | N/A | 0 hrs | Line 54: Already commented out ✅ |

**Subtotal:** 0 active calls, **0 hours**

---

## Category Breakdown

### By File Type
- **Controllers:** 2 files, 3 calls (2.1%)
- **Auth Handlers:** 1 file, 2 calls (2.9%)
- **Core Utilities:** 2 files, 14 calls (20.3%)
- **Export Utilities:** 4 files, 26 calls (37.7%)
- **Actor Files:** 5 files, 25 calls (36.2%)
- **Helpers:** 1 file, 1 call (1.4%)

### By Execution Pattern
- **Async Only (.executeAsync):** ~35 calls (50.7%)
- **Sync Only (.execute):** ~28 calls (40.6%)
- **Mixed (both patterns):** ~6 files (8.7%)

---

## Migration Strategy

### Phase 1: Auth & Controllers (Week 1)
**Goal:** Replace cURL calls in authentication and controllers  
**Files:** CustomAuthHandler.cs, versionController.cs  
**Effort:** 4 hours

**Actions:**
1. CustomAuthHandler.cs - Add CouchDbHttpClient via DI, replace 2 cURL calls (lines 87, 154)
2. versionController.cs - Already has _couchDbHttpClient, replace 1 cURL call (line 408)
3. vitalsController.cs - No action needed, cURL already commented out ✅

**Testing:** 
- Login/logout flows
- Session management and timeout
- Metadata version operations

---Replace cURL in case save operations  
**Files:** c_sync_document.cs, c_sync_document.pmss.cs, Process_Central_Pull_list.cs  
**Effort:** 23 hours

**Actions:**
1. c_sync_document.cs - Replace 7 cURL calls with _couchDbHttpClient (constructor already takes db_config)
2. c_sync_document.pmss.cs - Replace 7 cURL calls with _couchDbHttpClient
3. Process_Central_Pull_list.cs - Replace 2 cURL calls, inject CouchDbHttpClient

**Testing:** 
- Case create/edit/save workflows
- De-identified data generation
- Aggregate report updates (opioid, DQR, frequency)
- PMSS case synchronization
- Central replication pullskflows
- De-identified data generation
- Aggregate report updates (opioid, DQR, frequency)
- PMSS case synchronization

**Critical:** This touches EVERY case save - requires full regression testing
Replace cURL in export background jobs  
**Files:** exporter.cs, mmrds_exporter.cs, core_element_exporter.cs, export_all_generate_name_map.cs  
**Effort:** 28.5 hours

**Actions:**
1. exporter.cs - Inject CouchDbHttpClient, replace 11 cURL calls
2. mmrds_exporter.cs - Inject CouchDbHttpClient, replace 8 cURL calls
3. core_element_exporter.cs - Inject CouchDbHttpClient, replace 6 cURL calls
4. export_all_generate_name_map.cs - Replace 1 cURL call

**Testing:**
- Export queue processing
- All export format outputs (CSV, MMRDS, Core Elements)(CSV, MMRDS, Core Elements)

**Testing:**
- Export queue processing
- All export format outputs
- Export status tracking
- Error handling and retries

---

### Phase Replace cURL in vital import batch processing  
**Files:** Vital_Import_Synchronizer.cs, BatchItemProcessor.cs, BatchProcessor.cs, PMSS_ItemProcessor.cs  
**Effort:** 24 hours

**Actions:**
1. Vital_Import_Synchronizer.cs - Replace 2 cURL calls with CouchDbHttpClient
2. BatchItemProcessor.cs - Replace 7 calls (4 CouchDB → CouchDbHttpClient, 3 external APIs → HttpClient)
3. BatchProcessor.cs - Replace 3 cURL calls with CouchDbHttpClient
4. PMSS_ItemProcessor.cs - Replace 9 calls (6 CouchDB → CouchDbHttpClient, 3 external APIs → HttpClient)

**Note:** Actors don't use traditional DI - pass CouchDbHttpClient via message passing or Propth HttpClient (external APIs)
4. Test batch processing workflows

**Testing:**
- Vital import file processing
- STEVE/NCHS batch operations
- PMSS data integration
- Error recovery and batch status

---Cleanup & Verification (Week 10)
**Goal:** Final verification and documentation  
**Effort:** 3.5 hours

**Actions:**
1. Search entire codebase for any remaining cURL usage
2. Verify all commented-out cURL code can be removed
3. Run full build and test suite
4. Update AI_CONTEXT.md with migration completion

**Note:** NOT removing cURL class yet - it's still used in mmria-utilities and data-migration projectstset if fully deprecated
3. Update AI_CONTEXT.md with migration completion
4. Final build and regression test

---

## Risk Assessment

### High Risk Areas
1. **CustomAuthHandler.cs** ⚠️
   - Affects ALL authenticated requests
   - Session management is critical
   - Must test login/logout/session timeout scenarios

2. **c_sync_document.cs** ⚠️
   - Runs on EVERY case save
   - De-identification logic is sensitive
   - Report aggregation affects analytics

3. **Vital Import Actors** ⚠️
   - Akka.NET actors - must avoid blocking calls
   - Batch processing handles thousands of records
   - External API calls to NIOSH/CVS need HttpClient not cURL

### Medium Risk Areas
- Export utilities (background jobs, can be retried)
- Replication operations (eventual consistency model)

---

## Testing Requirements

### Unit Tests
- Create CouchDbHttpClient mock for each migrated class
- Test all HTTP methods: GET, PUT, POST, DELETE
- Test error handling paths

### Integration Tests
- Test against actual CouchDB instance
- Verify de-identification logic
- Verify report generation accuracy

### Regression Tests
- Full case workflow (create, edit, save, export)
- Authentication flows
- Batch import processing
- Multi-tenant operations

---

## Migration Checklist

- [ ] Phase 1: Controllers (8 hrs)
  - [ ] CustomAuthHandler.cs (4 hrs)
  - [ ] vitalsController.cs (2 hrs)
  - [ ] versionController.cs (2 hrs)

- [ ] Phase 2: Core Sync (24 hrs)
  - [ ] c_sync_document.cs (12 hrs)
  - [ ] c_sync_document.pmss.cs (12 hrs)

- [ ] Phase 3: Exports (37 hrs)
  - [ ] exporter.cs (16 hrs)
  - [ ] mmrds_exporter.cs (12 hrs)
  - [ ] core_element_exporter.cs (8 hrs)
  - [ ] export_Auth & Controllers (4 hrs)
  - [ ] CustomAuthHandler.cs (3 hrs) - 2 calls
  - [ ] versionController.cs (1 hr) - 1 call

- [ ] Phase 2: Core Sync (23 hrs)
  - [ ] c_sync_document.cs (10 hrs) - 7 calls
  - [ ] c_sync_document.pmss.cs (10 hrs) - 7 calls
  - [ ] Process_Central_Pull_list.cs (3 hrs) - 2 calls

- [ ] Phase 3: Exports (28.5 hrs)
  - [ ] exporter.cs (12 hrs) - 11 calls
  - [ ] mmrds_exporter.cs (10 hrs) - 8 calls
  - [ ] core_element_exporter.cs (6 hrs) - 6 calls
  - [ ] export_all_generate_name_map.cs (0.5 hrs) - 1 call

- [ ] Phase 4: Vital Import (24 hrs)
  - [ ] Vital_Import_Synchronizer.cs (3 hrs) - 2 calls
  - [ ] BatchItemProcessor.cs (8 hrs) - 7 calls (4 CouchDB, 3 external)
  - [ ] BatchProcessor.cs (3 hrs) - 3 calls
  - [ ] PMSS_ItemProcessor.cs (10 hrs) - 9 calls (6 CouchDB, 3 external)

- [ ] Phase 5: Cleanup (3.5 hrs)
  - [ ] Final grep search verification
  - [ ] Remove commented-out cURL code
  - [ ] Documentation updates
  - [ ] Full regression test
Some cURL calls in BatchItemProcessor and PMSS_ItemProcessor are to **external APIs** (NIOSH, CVS):
- Lines with `niosh_url` → Use HttpClient (not CouchDbHttpClient)
- Lines with CVS API → Use HttpClient (already fixed in services)

### Commented Out Code
Several files have cURL code already commented out - these can be removed entirely:
- `util/c_de_identifier.cs` line 70
- `util/JurisdictionAuthorizationRequirement.cs` line 54
- `Controllers/vitalsController.cs` line 77 (GetFolderList method)
- `Controllers/api/ije_messageController.cs` line 142
- `Controllers/api/populate_cdc_instanceController.cs` lines 95, 196

---

## Recommended Approach

**START HERE:** 
1. CustomAuthHandler.cs (affects all users)
2. versionController.cs (simple, 1 call)
3. c_sync_document.cs (critical path)

**Then:** Work through exports and actors in parallel if multiple developers available.

**End Goal:** Complete deprecation of cURL class, using:
- `CouchDbHttpClient` for all CouchDB operations
- `HttpClient` via SimpleHttpClientFactory for external APIs
versionController.cs (simplest, already has _couchDbHttpClient DI) - 1 hour
2. CustomAuthHandler.cs (affects all users) - 3 hours
3. c_sync_document.cs + pmss variant (critical path) - 20 hours

**Then:** Work through exports and actors in parallel if multiple developers available.

**Focus:** Simple mechanical replacement of `new cURL(...)` with `_couchDbHttpClient.ExecuteAsync(...)`. No architectural refactoring.

**End Goal:** Remove all cURL usage from mmria-server, using:
- `CouchDbHttpClient` for all CouchDB operations
- `HttpClient` via SimpleHttpClientFactory for external APIs (NIOSH, CVS)