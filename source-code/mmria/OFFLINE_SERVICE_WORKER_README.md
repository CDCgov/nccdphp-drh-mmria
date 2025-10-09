# MMRIA Offline Mode Service Worker Implementation

## Overview
This document describes the enhanced offline functionality for MMRIA that uses a Service Worker for improved caching and offline experience.

## Key Features
- **Service Worker-based caching**: Replaces localStorage with proper service worker caching for better performance and reliability
- **On-demand registration**: Service worker is only registered when user activates offline mode
- **Selective caching**: Only caches required routes and excludes PDF generation, printing, and validation services
- **Pre-fetch offline cases**: Uses `/api/case?case_id=` endpoint to cache selected cases

## File Changes

### New Files
- `/wwwroot/service-worker.js` - Main service worker implementation

### Modified Files
- `/wwwroot/scripts/editor/page_renderer/app.mmria.js` - Updated offline functionality

## How It Works

### 1. Offline Mode Activation
When user clicks "Go Offline" and sets an offline key in `go_offline_final()`:
1. Service worker is registered (`/service-worker.js`)
2. Selected cases are pre-fetched using `/api/case?case_id=` endpoint
3. Static resources and metadata are cached
4. Application enters offline mode

### 2. Service Worker Caching Strategy
The service worker implements different caching strategies:

#### Static Files (cache-first)
- CSS files: `/css/index.css`, `/css/bootstrap.min.css`
- JavaScript files: `/scripts/jquery.min.js`, `/scripts/bootstrap.min.js`, etc.
- Images: `/img/icon_pin.png`, `/img/icon_unpin.png`, etc.

#### API Requests (network-first with cache fallback)
- Case data: `/api/case?case_id=`
- Metadata: `/api/metadata`, `/api/metadata/version_specification`
- User roles: `/api/user_role_jurisdiction_view/my-roles`

#### Page Routes (network-first with cache fallback)
- Case summary: `/Case/{id}/summary`, `/Case/summary`
- Case forms: `/Case/{id}/0/home_record`, `/Case/{id}/0/death_certificate`, etc.

### 3. Excluded Routes
The service worker explicitly excludes:
- PDF generation routes (any URL containing "pdf")
- Print routes (any URL containing "print")
- Address validation (any URL containing "validate" and "address")
- Geography context (any URL containing "geography" and "context")

### 4. Offline to Online Transition
When user clicks "Go Online" in `go_online_clicked()`:
1. Service worker is unregistered
2. All caches are cleared
3. Local storage is cleaned up
4. Page is refreshed to return to full online mode

## Cached Routes

### Case Navigation Routes
- `/Case#/summary` - Case summary page
- `/Case/summary` - General case summary
- `/Case#/0/home_record` - Home record form
- `/Case#/0/death_certificate` - Death certificate form
- `/Case#/0/birth_fetal_death_certificate_parent` - Birth/fetal death certificate
- `/Case#/0/birth_certificate_infant_fetal_section` - Birth certificate infant section
- `/Case#/0/cvs` - CVS form
- `/Case#/0/social_and_environmental_profile` - Social and environmental profile
- `/Case#/0/autopsy_report` - Autopsy report
- `/Case#/0/prenatal` - Prenatal form
- `/Case#/0/er_visit_and_hospital_medical_records` - ER visit records
- `/Case#/0/other_medical_office_visits` - Other medical visits
- `/Case#/0/medical_transport` - Medical transport
- `/Case#/0/mental_health_profile` - Mental health profile
- `/Case#/0/informant_interviews` - Informant interviews
- `/Case#/0/case_narrative` - Case narrative
- `/Case#/0/committee_review` - Committee review

Note: `#` represents the case ID placeholder in actual URLs.

## Browser Compatibility
- Service Workers are supported in all modern browsers
- Fallback handling included for browsers without service worker support
- Cache API used for efficient resource management

## Development Notes
- Service worker only registers during offline mode activation
- Debug information available in browser dev tools under "Application" > "Service Workers"
- Cache status can be monitored in browser dev tools under "Application" > "Storage"
- Console logging available for troubleshooting cache operations

## Security Considerations
- Service worker only caches explicitly defined routes
- No sensitive data cached beyond necessary case information
- Service worker unregisters when returning to online mode
- All cached data is cleared when exiting offline mode
