
using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.case_version.mmria.v240616;

public sealed partial class mmria_case
{


    public string? GetM_String(string path, int index)
    {
        string? result = null;
        try
        {
            result = path.ToLower() switch
            {
             "birth_certificate_infant_fetal_section/record_identification/state_file_number" => birth_certificate_infant_fetal_section[index].record_identification.state_file_number,
         "birth_certificate_infant_fetal_section/record_identification/local_file_number" => birth_certificate_infant_fetal_section[index].record_identification.local_file_number,
         "birth_certificate_infant_fetal_section/record_identification/newborn_medical_record_number" => birth_certificate_infant_fetal_section[index].record_identification.newborn_medical_record_number,
         "birth_certificate_infant_fetal_section/biometrics_and_demographics/facility_city_state" => birth_certificate_infant_fetal_section[index].biometrics_and_demographics.facility_city_state,
         "birth_certificate_infant_fetal_section/method_of_delivery/other_presentation" => birth_certificate_infant_fetal_section[index].method_of_delivery.other_presentation,
         "birth_certificate_infant_fetal_section/icd_version" => birth_certificate_infant_fetal_section[index].icd_version,
         "birth_certificate_infant_fetal_section/reviewer_note" => birth_certificate_infant_fetal_section[index].reviewer_note,
         "birth_certificate_infant_fetal_section/vitals_import_group/summary_text" => birth_certificate_infant_fetal_section[index].vitals_import_group.summary_text,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a1" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a1,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a2" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a2,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a3" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a3,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a4" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a4,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a5" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a5,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a6" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a6,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a7" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a7,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a8" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a8,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a9" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a9,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a10" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a10,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a11" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a11,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a12" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a12,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a13" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a13,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18a14" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18a14,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b1" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b1,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b2" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b2,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b3" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b3,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b4" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b4,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b5" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b5,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b6" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b6,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b7" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b7,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b8" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b8,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b9" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b9,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b10" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b10,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b11" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b11,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b12" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b12,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b13" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b13,
         "birth_certificate_infant_fetal_section/vitals_import_group/cod18b14" => birth_certificate_infant_fetal_section[index].vitals_import_group.cod18b14,
         "birth_certificate_infant_fetal_section/vitals_import_group/icod" => birth_certificate_infant_fetal_section[index].vitals_import_group.icod,
         "birth_certificate_infant_fetal_section/vitals_import_group/ocod1" => birth_certificate_infant_fetal_section[index].vitals_import_group.ocod1,
         "birth_certificate_infant_fetal_section/vitals_import_group/ocod2" => birth_certificate_infant_fetal_section[index].vitals_import_group.ocod2,
         "birth_certificate_infant_fetal_section/vitals_import_group/ocod3" => birth_certificate_infant_fetal_section[index].vitals_import_group.ocod3,
         "birth_certificate_infant_fetal_section/vitals_import_group/ocod4" => birth_certificate_infant_fetal_section[index].vitals_import_group.ocod4,
         "birth_certificate_infant_fetal_section/vitals_import_group/ocod5" => birth_certificate_infant_fetal_section[index].vitals_import_group.ocod5,
         "birth_certificate_infant_fetal_section/vitals_import_group/ocod6" => birth_certificate_infant_fetal_section[index].vitals_import_group.ocod6,
         "birth_certificate_infant_fetal_section/vitals_import_group/ocod7" => birth_certificate_infant_fetal_section[index].vitals_import_group.ocod7,
         "er_visit_and_hospital_medical_records/maternal_record_identification/medical_record_no" => er_visit_and_hospital_medical_records[index].maternal_record_identification.medical_record_no,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/admission_status_other" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.admission_status_other,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/admission_reason_other" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.admission_reason_other,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/principle_source_of_payment_other_specify" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.principle_source_of_payment_other_specify,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/from_where" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.from_where,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/to_where" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.to_where,
         "er_visit_and_hospital_medical_records/name_and_location_facility/facility_name" => er_visit_and_hospital_medical_records[index].name_and_location_facility.facility_name,
         "er_visit_and_hospital_medical_records/name_and_location_facility/type_of_facility_other_specify" => er_visit_and_hospital_medical_records[index].name_and_location_facility.type_of_facility_other_specify,
         "er_visit_and_hospital_medical_records/name_and_location_facility/facility_npi_no" => er_visit_and_hospital_medical_records[index].name_and_location_facility.facility_npi_no,
         "er_visit_and_hospital_medical_records/name_and_location_facility/other_maternal_level_of_care" => er_visit_and_hospital_medical_records[index].name_and_location_facility.other_maternal_level_of_care,
         "er_visit_and_hospital_medical_records/name_and_location_facility/street" => er_visit_and_hospital_medical_records[index].name_and_location_facility.street,
         "er_visit_and_hospital_medical_records/name_and_location_facility/apartment" => er_visit_and_hospital_medical_records[index].name_and_location_facility.apartment,
         "er_visit_and_hospital_medical_records/name_and_location_facility/city" => er_visit_and_hospital_medical_records[index].name_and_location_facility.city,
         "er_visit_and_hospital_medical_records/name_and_location_facility/state" => er_visit_and_hospital_medical_records[index].name_and_location_facility.state,
         "er_visit_and_hospital_medical_records/name_and_location_facility/zip_code" => er_visit_and_hospital_medical_records[index].name_and_location_facility.zip_code,
         "er_visit_and_hospital_medical_records/name_and_location_facility/county" => er_visit_and_hospital_medical_records[index].name_and_location_facility.county,
         "er_visit_and_hospital_medical_records/name_and_location_facility/feature_matching_geography_type" => er_visit_and_hospital_medical_records[index].name_and_location_facility.feature_matching_geography_type,
         "er_visit_and_hospital_medical_records/name_and_location_facility/latitude" => er_visit_and_hospital_medical_records[index].name_and_location_facility.latitude,
         "er_visit_and_hospital_medical_records/name_and_location_facility/longitude" => er_visit_and_hospital_medical_records[index].name_and_location_facility.longitude,
         "er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_gis_coordinate_quality_code" => er_visit_and_hospital_medical_records[index].name_and_location_facility.naaccr_gis_coordinate_quality_code,
         "er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_gis_coordinate_quality_type" => er_visit_and_hospital_medical_records[index].name_and_location_facility.naaccr_gis_coordinate_quality_type,
         "er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_census_tract_certainty_code" => er_visit_and_hospital_medical_records[index].name_and_location_facility.naaccr_census_tract_certainty_code,
         "er_visit_and_hospital_medical_records/name_and_location_facility/naaccr_census_tract_certainty_type" => er_visit_and_hospital_medical_records[index].name_and_location_facility.naaccr_census_tract_certainty_type,
         "er_visit_and_hospital_medical_records/name_and_location_facility/state_county_fips" => er_visit_and_hospital_medical_records[index].name_and_location_facility.state_county_fips,
         "er_visit_and_hospital_medical_records/name_and_location_facility/census_state_fips" => er_visit_and_hospital_medical_records[index].name_and_location_facility.census_state_fips,
         "er_visit_and_hospital_medical_records/name_and_location_facility/census_county_fips" => er_visit_and_hospital_medical_records[index].name_and_location_facility.census_county_fips,
         "er_visit_and_hospital_medical_records/name_and_location_facility/census_tract_fips" => er_visit_and_hospital_medical_records[index].name_and_location_facility.census_tract_fips,
         "er_visit_and_hospital_medical_records/name_and_location_facility/urban_status" => er_visit_and_hospital_medical_records[index].name_and_location_facility.urban_status,
         "er_visit_and_hospital_medical_records/name_and_location_facility/census_met_div_fips" => er_visit_and_hospital_medical_records[index].name_and_location_facility.census_met_div_fips,
         "er_visit_and_hospital_medical_records/name_and_location_facility/census_cbsa_fips" => er_visit_and_hospital_medical_records[index].name_and_location_facility.census_cbsa_fips,
         "er_visit_and_hospital_medical_records/name_and_location_facility/census_cbsa_micro" => er_visit_and_hospital_medical_records[index].name_and_location_facility.census_cbsa_micro,
         "er_visit_and_hospital_medical_records/name_and_location_facility/mode_of_transportation_to_facility_other" => er_visit_and_hospital_medical_records[index].name_and_location_facility.mode_of_transportation_to_facility_other,
         "er_visit_and_hospital_medical_records/name_and_location_facility/origin_of_travel_other" => er_visit_and_hospital_medical_records[index].name_and_location_facility.origin_of_travel_other,
         "er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/duration_of_labor_prior_to_arrival" => er_visit_and_hospital_medical_records[index].onset_of_labor.date_of_onset_of_labor.duration_of_labor_prior_to_arrival,
         "er_visit_and_hospital_medical_records/onset_of_labor/is_artificial" => er_visit_and_hospital_medical_records[index].onset_of_labor.is_artificial,
         "er_visit_and_hospital_medical_records/onset_of_labor/is_spontaneous" => er_visit_and_hospital_medical_records[index].onset_of_labor.is_spontaneous,
         "er_visit_and_hospital_medical_records/onset_of_labor/pregnancy_outcome_other_specify" => er_visit_and_hospital_medical_records[index].onset_of_labor.pregnancy_outcome_other_specify,
         "er_visit_and_hospital_medical_records/patient_blood_type" => er_visit_and_hospital_medical_records[index].patient_blood_type,
         "er_visit_and_hospital_medical_records/reviewer_note" => er_visit_and_hospital_medical_records[index].reviewer_note,
         "other_medical_office_visits/visit/visit_type_other_specify" => other_medical_office_visits[index].visit.visit_type_other_specify,
         "other_medical_office_visits/visit/medical_record_no" => other_medical_office_visits[index].visit.medical_record_no,
         "other_medical_office_visits/visit/reason_for_visit_or_chief_complaint" => other_medical_office_visits[index].visit.reason_for_visit_or_chief_complaint,
         "other_medical_office_visits/medical_care_facility/specify_other_place_type" => other_medical_office_visits[index].medical_care_facility.specify_other_place_type,
         "other_medical_office_visits/medical_care_facility/specify_other_provider_type" => other_medical_office_visits[index].medical_care_facility.specify_other_provider_type,
         "other_medical_office_visits/medical_care_facility/other_payment_source" => other_medical_office_visits[index].medical_care_facility.other_payment_source,
         "other_medical_office_visits/location_of_medical_care_facility/street" => other_medical_office_visits[index].location_of_medical_care_facility.street,
         "other_medical_office_visits/location_of_medical_care_facility/apartment" => other_medical_office_visits[index].location_of_medical_care_facility.apartment,
         "other_medical_office_visits/location_of_medical_care_facility/city" => other_medical_office_visits[index].location_of_medical_care_facility.city,
         "other_medical_office_visits/location_of_medical_care_facility/state" => other_medical_office_visits[index].location_of_medical_care_facility.state,
         "other_medical_office_visits/location_of_medical_care_facility/zip_code" => other_medical_office_visits[index].location_of_medical_care_facility.zip_code,
         "other_medical_office_visits/location_of_medical_care_facility/county" => other_medical_office_visits[index].location_of_medical_care_facility.county,
         "other_medical_office_visits/location_of_medical_care_facility/feature_matching_geography_type" => other_medical_office_visits[index].location_of_medical_care_facility.feature_matching_geography_type,
         "other_medical_office_visits/location_of_medical_care_facility/latitude" => other_medical_office_visits[index].location_of_medical_care_facility.latitude,
         "other_medical_office_visits/location_of_medical_care_facility/longitude" => other_medical_office_visits[index].location_of_medical_care_facility.longitude,
         "other_medical_office_visits/location_of_medical_care_facility/naaccr_gis_coordinate_quality_code" => other_medical_office_visits[index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_code,
         "other_medical_office_visits/location_of_medical_care_facility/naaccr_gis_coordinate_quality_type" => other_medical_office_visits[index].location_of_medical_care_facility.naaccr_gis_coordinate_quality_type,
         "other_medical_office_visits/location_of_medical_care_facility/naaccr_census_tract_certainty_code" => other_medical_office_visits[index].location_of_medical_care_facility.naaccr_census_tract_certainty_code,
         "other_medical_office_visits/location_of_medical_care_facility/naaccr_census_tract_certainty_type" => other_medical_office_visits[index].location_of_medical_care_facility.naaccr_census_tract_certainty_type,
         "other_medical_office_visits/location_of_medical_care_facility/state_county_fips" => other_medical_office_visits[index].location_of_medical_care_facility.state_county_fips,
         "other_medical_office_visits/location_of_medical_care_facility/census_state_fips" => other_medical_office_visits[index].location_of_medical_care_facility.census_state_fips,
         "other_medical_office_visits/location_of_medical_care_facility/census_county_fips" => other_medical_office_visits[index].location_of_medical_care_facility.census_county_fips,
         "other_medical_office_visits/location_of_medical_care_facility/census_tract_fips" => other_medical_office_visits[index].location_of_medical_care_facility.census_tract_fips,
         "other_medical_office_visits/location_of_medical_care_facility/urban_status" => other_medical_office_visits[index].location_of_medical_care_facility.urban_status,
         "other_medical_office_visits/location_of_medical_care_facility/census_met_div_fips" => other_medical_office_visits[index].location_of_medical_care_facility.census_met_div_fips,
         "other_medical_office_visits/location_of_medical_care_facility/census_cbsa_fips" => other_medical_office_visits[index].location_of_medical_care_facility.census_cbsa_fips,
         "other_medical_office_visits/location_of_medical_care_facility/census_cbsa_micro" => other_medical_office_visits[index].location_of_medical_care_facility.census_cbsa_micro,
         "other_medical_office_visits/reviewer_note" => other_medical_office_visits[index].reviewer_note,
         "medical_transport/reason_for_transport" => medical_transport[index].reason_for_transport,
         "medical_transport/maternal_conditions" => medical_transport[index].maternal_conditions,
         "medical_transport/other_transport_manager" => medical_transport[index].other_transport_manager,
         "medical_transport/other_transport_vehicle" => medical_transport[index].other_transport_vehicle,
         "medical_transport/origin_information/place_of_origin" => medical_transport[index].origin_information.place_of_origin,
         "medical_transport/origin_information/place_of_origin_other" => medical_transport[index].origin_information.place_of_origin_other,
         "medical_transport/origin_information/address/street" => medical_transport[index].origin_information.address.street,
         "medical_transport/origin_information/address/apartment" => medical_transport[index].origin_information.address.apartment,
         "medical_transport/origin_information/address/city" => medical_transport[index].origin_information.address.city,
         "medical_transport/origin_information/address/state" => medical_transport[index].origin_information.address.state,
         "medical_transport/origin_information/address/country" => medical_transport[index].origin_information.address.country,
         "medical_transport/origin_information/address/zip_code" => medical_transport[index].origin_information.address.zip_code,
         "medical_transport/origin_information/address/county" => medical_transport[index].origin_information.address.county,
         "medical_transport/origin_information/address/feature_matching_geography_type" => medical_transport[index].origin_information.address.feature_matching_geography_type,
         "medical_transport/origin_information/address/latitude" => medical_transport[index].origin_information.address.latitude,
         "medical_transport/origin_information/address/longitude" => medical_transport[index].origin_information.address.longitude,
         "medical_transport/origin_information/address/naaccr_gis_coordinate_quality_code" => medical_transport[index].origin_information.address.naaccr_gis_coordinate_quality_code,
         "medical_transport/origin_information/address/naaccr_gis_coordinate_quality_type" => medical_transport[index].origin_information.address.naaccr_gis_coordinate_quality_type,
         "medical_transport/origin_information/address/naaccr_census_tract_certainty_code" => medical_transport[index].origin_information.address.naaccr_census_tract_certainty_code,
         "medical_transport/origin_information/address/naaccr_census_tract_certainty_type" => medical_transport[index].origin_information.address.naaccr_census_tract_certainty_type,
         "medical_transport/origin_information/address/state_county_fips" => medical_transport[index].origin_information.address.state_county_fips,
         "medical_transport/origin_information/address/census_state_fips" => medical_transport[index].origin_information.address.census_state_fips,
         "medical_transport/origin_information/address/census_county_fips" => medical_transport[index].origin_information.address.census_county_fips,
         "medical_transport/origin_information/address/census_tract_fips" => medical_transport[index].origin_information.address.census_tract_fips,
         "medical_transport/origin_information/address/urban_status" => medical_transport[index].origin_information.address.urban_status,
         "medical_transport/origin_information/address/census_met_div_fips" => medical_transport[index].origin_information.address.census_met_div_fips,
         "medical_transport/origin_information/address/census_cbsa_fips" => medical_transport[index].origin_information.address.census_cbsa_fips,
         "medical_transport/origin_information/address/census_cbsa_micro" => medical_transport[index].origin_information.address.census_cbsa_micro,
         "medical_transport/origin_information/other_trauma_level_of_care" => medical_transport[index].origin_information.other_trauma_level_of_care,
         "medical_transport/origin_information/other_maternal_level_of_care" => medical_transport[index].origin_information.other_maternal_level_of_care,
         "medical_transport/origin_information/comments" => medical_transport[index].origin_information.comments,
         "medical_transport/procedures_before_transport" => medical_transport[index].procedures_before_transport,
         "medical_transport/procedures_during_transport" => medical_transport[index].procedures_during_transport,
         "medical_transport/mental_status_of_patient_during_transport" => medical_transport[index].mental_status_of_patient_during_transport,
         "medical_transport/documented_pertinent_oral_statements_made_by_patient_and_other_on_scene" => medical_transport[index].documented_pertinent_oral_statements_made_by_patient_and_other_on_scene,
         "medical_transport/destination_information/place_of_destination" => medical_transport[index].destination_information.place_of_destination,
         "medical_transport/destination_information/destination_type" => medical_transport[index].destination_information.destination_type,
         "medical_transport/destination_information/place_of_origin_other" => medical_transport[index].destination_information.place_of_origin_other,
         "medical_transport/destination_information/address/street" => medical_transport[index].destination_information.address.street,
         "medical_transport/destination_information/address/apartment" => medical_transport[index].destination_information.address.apartment,
         "medical_transport/destination_information/address/city" => medical_transport[index].destination_information.address.city,
         "medical_transport/destination_information/address/state" => medical_transport[index].destination_information.address.state,
         "medical_transport/destination_information/address/country_of_last_residence" => medical_transport[index].destination_information.address.country_of_last_residence,
         "medical_transport/destination_information/address/zip_code" => medical_transport[index].destination_information.address.zip_code,
         "medical_transport/destination_information/address/county" => medical_transport[index].destination_information.address.county,
         "medical_transport/destination_information/address/feature_matching_geography_type" => medical_transport[index].destination_information.address.feature_matching_geography_type,
         "medical_transport/destination_information/address/latitude" => medical_transport[index].destination_information.address.latitude,
         "medical_transport/destination_information/address/longitude" => medical_transport[index].destination_information.address.longitude,
         "medical_transport/destination_information/address/naaccr_gis_coordinate_quality_code" => medical_transport[index].destination_information.address.naaccr_gis_coordinate_quality_code,
         "medical_transport/destination_information/address/naaccr_gis_coordinate_quality_type" => medical_transport[index].destination_information.address.naaccr_gis_coordinate_quality_type,
         "medical_transport/destination_information/address/naaccr_census_tract_certainty_code" => medical_transport[index].destination_information.address.naaccr_census_tract_certainty_code,
         "medical_transport/destination_information/address/naaccr_census_tract_certainty_type" => medical_transport[index].destination_information.address.naaccr_census_tract_certainty_type,
         "medical_transport/destination_information/address/state_county_fips" => medical_transport[index].destination_information.address.state_county_fips,
         "medical_transport/destination_information/address/census_state_fips" => medical_transport[index].destination_information.address.census_state_fips,
         "medical_transport/destination_information/address/census_county_fips" => medical_transport[index].destination_information.address.census_county_fips,
         "medical_transport/destination_information/address/census_tract_fips" => medical_transport[index].destination_information.address.census_tract_fips,
         "medical_transport/destination_information/address/urban_status" => medical_transport[index].destination_information.address.urban_status,
         "medical_transport/destination_information/address/census_met_div_fips" => medical_transport[index].destination_information.address.census_met_div_fips,
         "medical_transport/destination_information/address/census_cbsa_fips" => medical_transport[index].destination_information.address.census_cbsa_fips,
         "medical_transport/destination_information/address/census_cbsa_micro" => medical_transport[index].destination_information.address.census_cbsa_micro,
         "medical_transport/destination_information/other_trauma_level_of_care" => medical_transport[index].destination_information.other_trauma_level_of_care,
         "medical_transport/destination_information/other_maternal_level_of_care" => medical_transport[index].destination_information.other_maternal_level_of_care,
         "medical_transport/destination_information/comments" => medical_transport[index].destination_information.comments,
         "medical_transport/reviewer_note" => medical_transport[index].reviewer_note,
         "informant_interviews/other_interview_type" => informant_interviews[index].other_interview_type,
         "informant_interviews/other_relationship" => informant_interviews[index].other_relationship,
         "informant_interviews/interview_narrative" => informant_interviews[index].interview_narrative,
         "informant_interviews/reviewer_note" => informant_interviews[index].reviewer_note,

                _ => null
            };
        }
        catch(Exception)
        {
            
        }
        return result;
    }

    public double? GetM_Double(string path, int index)
    {
        double? result = null;
        try
        {
            result = path.ToLower() switch
            {
             "birth_certificate_infant_fetal_section/record_type" => birth_certificate_infant_fetal_section[index].record_type,
         "birth_certificate_infant_fetal_section/is_multiple_gestation" => birth_certificate_infant_fetal_section[index].is_multiple_gestation,
         "birth_certificate_infant_fetal_section/birth_order" => birth_certificate_infant_fetal_section[index].birth_order,
         "birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/unit_of_measurement" => birth_certificate_infant_fetal_section[index].biometrics_and_demographics.birth_weight.unit_of_measurement,
         "birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/grams_or_pounds" => birth_certificate_infant_fetal_section[index].biometrics_and_demographics.birth_weight.grams_or_pounds,
         "birth_certificate_infant_fetal_section/biometrics_and_demographics/birth_weight/ounces" => birth_certificate_infant_fetal_section[index].biometrics_and_demographics.birth_weight.ounces,
         "birth_certificate_infant_fetal_section/biometrics_and_demographics/gender" => birth_certificate_infant_fetal_section[index].biometrics_and_demographics.gender,
         "birth_certificate_infant_fetal_section/biometrics_and_demographics/apgar_scores/minute_5" => birth_certificate_infant_fetal_section[index].biometrics_and_demographics.apgar_scores.minute_5,
         "birth_certificate_infant_fetal_section/biometrics_and_demographics/apgar_scores/minute_10" => birth_certificate_infant_fetal_section[index].biometrics_and_demographics.apgar_scores.minute_10,
         "birth_certificate_infant_fetal_section/biometrics_and_demographics/is_infant_living_at_time_of_report" => birth_certificate_infant_fetal_section[index].biometrics_and_demographics.is_infant_living_at_time_of_report,
         "birth_certificate_infant_fetal_section/biometrics_and_demographics/is_infant_being_breastfed_at_discharge" => birth_certificate_infant_fetal_section[index].biometrics_and_demographics.is_infant_being_breastfed_at_discharge,
         "birth_certificate_infant_fetal_section/biometrics_and_demographics/was_infant_transferred_within_24_hours" => birth_certificate_infant_fetal_section[index].biometrics_and_demographics.was_infant_transferred_within_24_hours,
         "birth_certificate_infant_fetal_section/method_of_delivery/was_delivery_with_forceps_attempted_but_unsuccessful" => birth_certificate_infant_fetal_section[index].method_of_delivery.was_delivery_with_forceps_attempted_but_unsuccessful,
         "birth_certificate_infant_fetal_section/method_of_delivery/was_delivery_with_vacuum_extration_attempted_but_unsuccessful" => birth_certificate_infant_fetal_section[index].method_of_delivery.was_delivery_with_vacuum_extration_attempted_but_unsuccessful,
         "birth_certificate_infant_fetal_section/method_of_delivery/fetal_delivery" => birth_certificate_infant_fetal_section[index].method_of_delivery.fetal_delivery,
         "birth_certificate_infant_fetal_section/method_of_delivery/final_route_and_method_of_delivery" => birth_certificate_infant_fetal_section[index].method_of_delivery.final_route_and_method_of_delivery,
         "birth_certificate_infant_fetal_section/method_of_delivery/if_cesarean_was_trial_of_labor_attempted" => birth_certificate_infant_fetal_section[index].method_of_delivery.if_cesarean_was_trial_of_labor_attempted,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/month" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_arrival.month,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/day" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_arrival.day,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/year" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_arrival.year,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_weeks" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_arrival.gestational_age_weeks,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/gestational_age_days" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_arrival.gestational_age_days,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/days_postpartum" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_arrival.days_postpartum,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/month" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_admission.month,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/day" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_admission.day,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/year" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_admission.year,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/gestational_age_weeks" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_weeks,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/gestational_age_days" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_admission.gestational_age_days,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/days_postpartum" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_admission.days_postpartum,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/admission_condition" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.admission_condition,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/admission_status" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.admission_status,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/admission_reason" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.admission_reason,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/principle_source_of_payment" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.principle_source_of_payment,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/was_recieved_from_another_hospital" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.was_recieved_from_another_hospital,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/was_transferred_to_another_hospital" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.was_transferred_to_another_hospital,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/month" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_discharge.month,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/day" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_discharge.day,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/year" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_discharge.year,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/gestational_age_weeks" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_weeks,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/gestational_age_days" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_discharge.gestational_age_days,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/days_postpartum" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_discharge.days_postpartum,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/discharge_pregnancy_status" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.discharge_pregnancy_status,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/deceased_at_discharge" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.deceased_at_discharge,
         "er_visit_and_hospital_medical_records/name_and_location_facility/type_of_facility" => er_visit_and_hospital_medical_records[index].name_and_location_facility.type_of_facility,
         "er_visit_and_hospital_medical_records/name_and_location_facility/maternal_level_of_care" => er_visit_and_hospital_medical_records[index].name_and_location_facility.maternal_level_of_care,
         "er_visit_and_hospital_medical_records/name_and_location_facility/mode_of_transportation_to_facility" => er_visit_and_hospital_medical_records[index].name_and_location_facility.mode_of_transportation_to_facility,
         "er_visit_and_hospital_medical_records/name_and_location_facility/origin_of_travel" => er_visit_and_hospital_medical_records[index].name_and_location_facility.origin_of_travel,
         "er_visit_and_hospital_medical_records/name_and_location_facility/travel_time_to_hospital/value" => er_visit_and_hospital_medical_records[index].name_and_location_facility.travel_time_to_hospital.value,
         "er_visit_and_hospital_medical_records/name_and_location_facility/travel_time_to_hospital/unit" => er_visit_and_hospital_medical_records[index].name_and_location_facility.travel_time_to_hospital.unit,
         "er_visit_and_hospital_medical_records/maternal_biometrics/height/feet" => er_visit_and_hospital_medical_records[index].maternal_biometrics.height.feet,
         "er_visit_and_hospital_medical_records/maternal_biometrics/height/inches" => er_visit_and_hospital_medical_records[index].maternal_biometrics.height.inches,
         "er_visit_and_hospital_medical_records/maternal_biometrics/height/bmi" => er_visit_and_hospital_medical_records[index].maternal_biometrics.height.bmi,
         "er_visit_and_hospital_medical_records/maternal_biometrics/admission_weight" => er_visit_and_hospital_medical_records[index].maternal_biometrics.admission_weight,
         "er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/month" => er_visit_and_hospital_medical_records[index].onset_of_labor.date_of_onset_of_labor.month,
         "er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/day" => er_visit_and_hospital_medical_records[index].onset_of_labor.date_of_onset_of_labor.day,
         "er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/year" => er_visit_and_hospital_medical_records[index].onset_of_labor.date_of_onset_of_labor.year,
         "er_visit_and_hospital_medical_records/onset_of_labor/date_of_rupture/month" => er_visit_and_hospital_medical_records[index].onset_of_labor.date_of_rupture.month,
         "er_visit_and_hospital_medical_records/onset_of_labor/date_of_rupture/day" => er_visit_and_hospital_medical_records[index].onset_of_labor.date_of_rupture.day,
         "er_visit_and_hospital_medical_records/onset_of_labor/date_of_rupture/year" => er_visit_and_hospital_medical_records[index].onset_of_labor.date_of_rupture.year,
         "er_visit_and_hospital_medical_records/onset_of_labor/final_delivery_route" => er_visit_and_hospital_medical_records[index].onset_of_labor.final_delivery_route,
         "er_visit_and_hospital_medical_records/onset_of_labor/onset_of_labor_was" => er_visit_and_hospital_medical_records[index].onset_of_labor.onset_of_labor_was,
         "er_visit_and_hospital_medical_records/onset_of_labor/multiple_gestation" => er_visit_and_hospital_medical_records[index].onset_of_labor.multiple_gestation,
         "er_visit_and_hospital_medical_records/onset_of_labor/pregnancy_outcome" => er_visit_and_hospital_medical_records[index].onset_of_labor.pregnancy_outcome,
         "er_visit_and_hospital_medical_records/highest_bp/systolic_bp" => er_visit_and_hospital_medical_records[index].highest_bp.systolic_bp,
         "er_visit_and_hospital_medical_records/highest_bp/diastolic_bp" => er_visit_and_hospital_medical_records[index].highest_bp.diastolic_bp,
         "er_visit_and_hospital_medical_records/were_there_complications_of_anesthesia" => er_visit_and_hospital_medical_records[index].were_there_complications_of_anesthesia,
         "er_visit_and_hospital_medical_records/any_adverse_reactions" => er_visit_and_hospital_medical_records[index].any_adverse_reactions,
         "er_visit_and_hospital_medical_records/any_surgical_procedures" => er_visit_and_hospital_medical_records[index].any_surgical_procedures,
         "er_visit_and_hospital_medical_records/any_blood_transfusions" => er_visit_and_hospital_medical_records[index].any_blood_transfusions,
         "other_medical_office_visits/visit/date_of_medical_office_visit/month" => other_medical_office_visits[index].visit.date_of_medical_office_visit.month,
         "other_medical_office_visits/visit/date_of_medical_office_visit/day" => other_medical_office_visits[index].visit.date_of_medical_office_visit.day,
         "other_medical_office_visits/visit/date_of_medical_office_visit/year" => other_medical_office_visits[index].visit.date_of_medical_office_visit.year,
         "other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_weeks" => other_medical_office_visits[index].visit.date_of_medical_office_visit.gestational_age_weeks,
         "other_medical_office_visits/visit/date_of_medical_office_visit/gestational_age_days" => other_medical_office_visits[index].visit.date_of_medical_office_visit.gestational_age_days,
         "other_medical_office_visits/visit/date_of_medical_office_visit/days_postpartum" => other_medical_office_visits[index].visit.date_of_medical_office_visit.days_postpartum,
         "other_medical_office_visits/visit/visit_type" => other_medical_office_visits[index].visit.visit_type,
         "other_medical_office_visits/medical_care_facility/place_type" => other_medical_office_visits[index].medical_care_facility.place_type,
         "other_medical_office_visits/medical_care_facility/provider_type" => other_medical_office_visits[index].medical_care_facility.provider_type,
         "other_medical_office_visits/medical_care_facility/payment_source" => other_medical_office_visits[index].medical_care_facility.payment_source,
         "other_medical_office_visits/medical_care_facility/pregnancy_status" => other_medical_office_visits[index].medical_care_facility.pregnancy_status,
         "other_medical_office_visits/medical_care_facility/was_this_provider_her_primary_prenatal_care_provider" => other_medical_office_visits[index].medical_care_facility.was_this_provider_her_primary_prenatal_care_provider,
         "medical_transport/date_of_transport/month" => medical_transport[index].date_of_transport.month,
         "medical_transport/date_of_transport/day" => medical_transport[index].date_of_transport.day,
         "medical_transport/date_of_transport/year" => medical_transport[index].date_of_transport.year,
         "medical_transport/date_of_transport/gestational_age_weeks" => medical_transport[index].date_of_transport.gestational_age_weeks,
         "medical_transport/date_of_transport/gestational_age_days" => medical_transport[index].date_of_transport.gestational_age_days,
         "medical_transport/date_of_transport/days_postpartum" => medical_transport[index].date_of_transport.days_postpartum,
         "medical_transport/who_managed_the_transport" => medical_transport[index].who_managed_the_transport,
         "medical_transport/transport_vehicle" => medical_transport[index].transport_vehicle,
         "medical_transport/origin_information/trauma_level_of_care" => medical_transport[index].origin_information.trauma_level_of_care,
         "medical_transport/origin_information/maternal_level_of_care" => medical_transport[index].origin_information.maternal_level_of_care,
         "medical_transport/destination_information/address/estimated_distance" => medical_transport[index].destination_information.address.estimated_distance,
         "medical_transport/destination_information/trauma_level_of_care" => medical_transport[index].destination_information.trauma_level_of_care,
         "medical_transport/destination_information/maternal_level_of_care" => medical_transport[index].destination_information.maternal_level_of_care,
         "informant_interviews/date_of_interview/month" => informant_interviews[index].date_of_interview.month,
         "informant_interviews/date_of_interview/day" => informant_interviews[index].date_of_interview.day,
         "informant_interviews/date_of_interview/year" => informant_interviews[index].date_of_interview.year,
         "informant_interviews/interview_type" => informant_interviews[index].interview_type,
         "informant_interviews/age_group" => informant_interviews[index].age_group,
         "informant_interviews/relationship_to_deceased" => informant_interviews[index].relationship_to_deceased,

                _ => null
            };        
        }
        catch(Exception)
        {
            
        }


        
        return result;
    }

    public bool? GetM_Boolean(string path, int index)
    {
        bool? result = null;
        try
        {
            result = path.ToLower() switch
            {
    
                _ => null
            };
        }
        catch(Exception)
        {
            
        }

        
        return result;
    }

    public List<double>? GetM_List_Of_Double(string path, int index)
    {
        List<double>? result = null;
        try
        {
            result = path.ToLower() switch
            {
             "birth_certificate_infant_fetal_section/abnormal_conditions_of_newborn" => birth_certificate_infant_fetal_section[index].abnormal_conditions_of_newborn,
         "birth_certificate_infant_fetal_section/congenital_anomalies" => birth_certificate_infant_fetal_section[index].congenital_anomalies,

                _ => null
            };        
        }
        catch(Exception)
        {
            
        }


        
        return result;
    }

    
    public List<string>? GetM_List_Of_String(string path, int index)
    {
        List<string>? result = null;
        try
        {
            result = path.ToLower() switch
            {
    
                _ => null
            };        
        }
        catch(Exception)
        {
            
        }

        
        return result;
    }

    public DateTime? GetM_Datetime(string path, int index)
    {
        DateTime? result = null;
        try
        {
            result = path.ToLower() switch
            {
             "medical_transport/timing_of_transport/call_received" => medical_transport[index].timing_of_transport.call_received,
         "medical_transport/timing_of_transport/depart_for_patient_origin" => medical_transport[index].timing_of_transport.depart_for_patient_origin,
         "medical_transport/timing_of_transport/arrive_at_patient_origin" => medical_transport[index].timing_of_transport.arrive_at_patient_origin,
         "medical_transport/timing_of_transport/patient_contact" => medical_transport[index].timing_of_transport.patient_contact,
         "medical_transport/timing_of_transport/depart_for_referring_facility" => medical_transport[index].timing_of_transport.depart_for_referring_facility,
         "medical_transport/timing_of_transport/arrive_at_referring_facility" => medical_transport[index].timing_of_transport.arrive_at_referring_facility,

                _ => null
            };        
        }
        catch(Exception)
        {
            
        }

        
        return result;
    }


    public DateOnly? GetM_Date_Only(string path, int index)
    {
        DateOnly? result = null;
        try
        {
            result = path.ToLower() switch
            {
             "birth_certificate_infant_fetal_section/record_identification/date_of_delivery" => birth_certificate_infant_fetal_section[index].record_identification.date_of_delivery,

                _ => null
            };
        }
        catch(Exception)
        {
            
        }

        
        return result;
    }


    public TimeOnly? GetM_Time_Only(string path, int index)
    {
        TimeOnly? result = null;
        try
        {
            result = path.ToLower() switch
            {
             "birth_certificate_infant_fetal_section/record_identification/time_of_delivery" => birth_certificate_infant_fetal_section[index].record_identification.time_of_delivery,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/time_of_arrival" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_arrival.time_of_arrival,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_admission/time_of_admission" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_admission.time_of_admission,
         "er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_hospital_discharge/time_of_discharge" => er_visit_and_hospital_medical_records[index].basic_admission_and_discharge_information.date_of_hospital_discharge.time_of_discharge,
         "er_visit_and_hospital_medical_records/onset_of_labor/date_of_onset_of_labor/time_of_onset_of_labor" => er_visit_and_hospital_medical_records[index].onset_of_labor.date_of_onset_of_labor.time_of_onset_of_labor,
         "er_visit_and_hospital_medical_records/onset_of_labor/date_of_rupture/time_of_rupture" => er_visit_and_hospital_medical_records[index].onset_of_labor.date_of_rupture.time_of_rupture,
         "other_medical_office_visits/visit/date_of_medical_office_visit/arrival_time" => other_medical_office_visits[index].visit.date_of_medical_office_visit.arrival_time,

                _ => null
            };
                    
        }
        catch(Exception)
        {
            
        }

        return result;
    }


}