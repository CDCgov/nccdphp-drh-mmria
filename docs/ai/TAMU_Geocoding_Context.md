# TAMU Geocoding Service Integration

- Status: Active
- Scope: TAMU geocoding fields, current API controller location, and case-generator guardrails for TAMU-backed values.
- When to use: Read this before changing TAMU geocoding, address-derived case fields, or test-data generation that touches geocode fields.
- Last verified: 2026-03-24
- Related docs: [AI Context Index](./AI_CONTEXT.md), [CVS Community Vital Signs Context](./CVS_Community_Vital_Signs_Context.md), [Strongly Typed Case Generator Workflow](./strongly_typed_case_generator.md)

## Overview

TAMU (Texas A&M Geocoding Service) provides geocoding data for address fields throughout MMRIA. It converts addresses into coordinates plus related census and geography values.

## Case-generator rule

TAMU-backed fields should not be populated with fake values during case generation. Leave them blank so the real geocoding workflow can populate them later.

## Backend and client locations

### API controller

- [Controllers/api/tamuGeoCodeController.cs](../../source-code/mmria/mmria-server/Controllers/api/tamuGeoCodeController.cs) is the current server-side controller for TAMU geocoding requests.

### JavaScript callers

- [wwwroot/scripts/mmria.js](../../source-code/mmria/mmria-server/wwwroot/scripts/mmria.js)
- [wwwroot/scripts/mmria.committee_member.js](../../source-code/mmria/mmria-server/wwwroot/scripts/mmria.committee_member.js)
- [wwwroot/scripts/validator.js](../../source-code/mmria/mmria-server/wwwroot/scripts/validator.js)

## Standard field groups

Most geocode locations populate these field groups:

- Core location fields such as `latitude` and `longitude`
- Match-quality fields such as `feature_matching_result_type`
- NAACCR GIS fields
- Census FIPS fields
- Derived fields such as `urban_status` and `state_county_fips`

## Geocode locations in the case model

Single-form locations include:

- `death_certificate/place_of_last_residence/`
- `death_certificate/address_of_injury/`
- `death_certificate/address_of_death/`
- `birth_fetal_death_certificate_parent/facility_of_delivery_location/`
- `birth_fetal_death_certificate_parent/location_of_residence/`
- `prenatal/location_of_primary_prenatal_care_facility/`

Grid-backed locations include:

- `er_visit_and_hospital_medical_records[index]/name_and_location_facility/`
- `other_medical_office_visits[index]/location_of_medical_care_facility/`
- `medical_transport[index]/origin_information/address/`
- `medical_transport[index]/destination_information/address/`

## Why blank test data matters

Fake TAMU data creates invalid coordinates, invalid census-derived values, and misleading downstream analysis. Leaving the fields blank keeps generated cases compatible with the real geocoding workflow.

## Related code and models

- [geocode_response.cs](../../nccdphp-drh-mmria-common/mmria.common/texas_am/geocode_response.cs)
- [Census_Variables.cs](../../nccdphp-drh-mmria-common/mmria.common/texas_am/Census_Variables.cs)
- [validator.js](../../source-code/mmria/mmria-server/wwwroot/scripts/validator.js)
- [MMRIA_calculations.js](../../source-code/mmria/mmria-server/database-scripts/MMRIA_calculations.js)


