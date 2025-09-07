# MMRIA Offline Mode - Complete Implementation

## Overview
This document describes the complete implementation of offline mode for MMRIA, including service worker integration, case caching, UI updates, and navigation handling.

## Key Components

### 1. Service Worker (`wwwroot/service-worker.js`)
- **Purpose**: Handles caching of static files, API responses, and case data for offline access
- **Features**:
  - Caches static assets (CSS, JS, images, fonts)
  - Caches API endpoints for user data, metadata, and case information
  - Provides fallback responses for critical API calls when offline
  - Handles font files with cache-busting query parameters
  - Implements custom `/api/case_view/offline-documents` endpoint for offline case list

### 2. Client Application (`wwwroot/scripts/editor/page_renderer/app.mmria.js`)
- **Purpose**: Main client-side logic for offline/online transitions and UI management
- **Features**:
  - Service worker registration/unregistration
  - Pre-fetching of selected offline cases
  - Conditional rendering based on offline mode
  - Dynamic DOM manipulation to hide/show elements
  - Offline case list rendering with correct data mapping

### 3. Navigation System (`wwwroot/scripts/case/index.js`)
- **Purpose**: Handles routing and navigation for both online and offline modes
- **Features**:
  - Route parsing for offline case URLs (`#/offline-case/{caseId}/home_record`)
  - Offline case loading from service worker cache
  - Read-only mode for offline cases
  - Fallback handling for missing offline cases

## Implementation Details

### Service Worker Caching Strategy

#### Static Files Cached:
- HTML pages (Case, Print pages)
- CSS stylesheets
- JavaScript files
- Images and icons
- Font files (with query parameter handling)

#### API Endpoints Cached:
- `/api/user` - User information
- `/api/user/role-list` - User roles
- `/api/metadata` - Application metadata
- `/api/metadata/version_specification` - Metadata version
- `/api/case?case_id=*` - Individual case data

#### API Endpoints Excluded:
- PDF generation (`/api/case_view/print_version`)
- Address validation (`/api/geocode`)
- Geography context (`/api/geojson`)
- Save operations (POST, PUT, DELETE requests)

### Offline Case Navigation

#### URL Format:
```
#/offline-case/{caseId}/home_record
```

#### Navigation Flow:
1. User clicks offline case link
2. URL monitor parses route as `["offline-case", caseId, "home_record"]`
3. `get_offline_case()` function loads case data from cache
4. Case renders in read-only mode

### UI Behavior

#### Online Mode:
- Full case listing with filters displayed
- Add/remove offline functionality available
- All navigation and editing features enabled

#### Offline Mode:
- Case listing and filters hidden
- Only "Cases Selected for Offline Work" section visible
- Cases loaded from cache with read-only access
- Save functionality disabled

#### Transitions:
- Going offline: Service worker registers, cases pre-cached, UI updated
- Going online: Service worker unregisters, full UI restored

## Data Mapping

### Offline Case List Format:
Each offline case document includes:
- `id`: Case ID
- `created_by`: Creator information
- `date_created`: Creation date
- `date_last_updated`: Last update date
- `last_updated_by`: Last updater
- `case_status`: Current status
- Case information fields (first/last name, age, etc.)

### Service Worker Response Format:
```javascript
{
  "total_rows": count,
  "offset": 0, 
  "rows": [
    {
      "id": "case-id",
      "value": {
        // Case data with proper field mapping
      }
    }
  ]
}
```

## Error Handling

### Network Disconnection:
- Service worker provides fallback responses for critical endpoints
- Client gracefully handles cache misses
- User feedback for unavailable resources

### Missing Offline Cases:
- Alert notification if case not found in cache
- Automatic redirect to summary page
- Console logging for debugging

## Testing and Validation

### Offline Mode Testing:
1. Enable offline mode for specific cases
2. Disconnect network
3. Verify case list displays correctly
4. Test navigation to individual cases
5. Confirm read-only behavior

### Cache Validation:
1. Check service worker cache contents
2. Verify API responses match expected format
3. Test font file handling with query parameters
4. Validate fallback responses

## Files Modified

### Core Files:
- `wwwroot/service-worker.js` - Service worker implementation
- `wwwroot/scripts/editor/page_renderer/app.mmria.js` - Client offline logic
- `wwwroot/scripts/case/index.js` - Navigation and routing

### Supporting Files:
- Various view files checked for script references
- Documentation files created

## Configuration

### Service Worker Registration:
- Only registered when entering offline mode
- Automatically unregistered when going online
- Handles existing registrations gracefully

### Cache Names:
- Primary cache: `'mmria-cache'`
- Versioned for cache invalidation
- Automatic cleanup of old caches

## Security Considerations

### Read-Only Access:
- Offline cases cannot be modified
- Save operations disabled in offline mode
- Data integrity maintained

### Cache Scope:
- Only authorized user data cached
- No sensitive information exposed
- Proper cleanup on mode changes

## Performance Optimizations

### Selective Caching:
- Only user-selected cases cached
- Essential metadata pre-cached
- Lazy loading for non-critical resources

### Cache Management:
- Automatic cache cleanup
- Size limits respected
- Efficient storage utilization

## Future Enhancements

### Potential Improvements:
- Background sync for offline edits
- Partial case updates
- Enhanced cache management
- Offline case search functionality

### Known Limitations:
- No offline editing capability
- Limited to pre-selected cases
- Requires initial online setup

## Troubleshooting

### Common Issues:
1. **Cases not appearing offline**: Check service worker registration and cache contents
2. **Navigation errors**: Verify URL format matches expected pattern
3. **UI elements not hiding**: Check offline mode detection logic
4. **Font loading issues**: Verify font file cache handling

### Debug Information:
- Browser Developer Tools → Application → Service Workers
- Console logs for cache operations
- Network tab for request interception

## Conclusion

The offline mode implementation provides a robust solution for accessing critical case data when network connectivity is unavailable. The system maintains data integrity while providing essential functionality for field work and emergency situations.
