
using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.case_version.v231108;

public sealed partial class mmria_case
{


    public bool SetS_String(string path, string value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "version":
                version = value;
                result = true;
            break;
            case "created_by":
                created_by = value;
                result = true;
            break;
            case "last_updated_by":
                last_updated_by = value;
                result = true;
            break;
            case "last_checked_out_by":
                last_checked_out_by = value;
                result = true;
            break;
            case "host_state":
                host_state = value;
                result = true;
            break;
            case "addquarter":
                addquarter = value;
                result = true;
            break;
            case "cmpquarter":
                cmpquarter = value;
                result = true;
            break;
            case "home_record/first_name":
                home_record.first_name = value;
                result = true;
            break;
            case "home_record/middle_name":
                home_record.middle_name = value;
                result = true;
            break;
            case "home_record/last_name":
                home_record.last_name = value;
                result = true;
            break;
            case "home_record/state_of_death_record":
                home_record.state_of_death_record = value;
                result = true;
            break;
            case "home_record/record_id":
                home_record.record_id = value;
                result = true;
            break;
            case "home_record/agency_case_id":
                home_record.agency_case_id = value;
                result = true;
            break;
            case "home_record/specify_other_multiple_sources":
                home_record.specify_other_multiple_sources = value;
                result = true;
            break;
            case "home_record/primary_abstractor":
                home_record.primary_abstractor = value;
                result = true;
            break;
            case "home_record/jurisdiction_id":
                home_record.jurisdiction_id = value;
                result = true;
            break;
            case "home_record/overall_assessment_of_timing_of_death/hr_prg_outcome_othsp":
                home_record.overall_assessment_of_timing_of_death.hr_prg_outcome_othsp = value;
                result = true;
            break;
            case "home_record/automated_vitals_group/vital_report":
                home_record.automated_vitals_group.vital_report = value;
                result = true;
            break;
            case "home_record/automated_vitals_group/vro_status":
                home_record.automated_vitals_group.vro_status = value;
                result = true;
            break;
            case "home_record/automated_vitals_group/import_date":
                home_record.automated_vitals_group.import_date = value;
                result = true;
            break;
            case "death_certificate/certificate_identification/local_file_number":
                death_certificate.certificate_identification.local_file_number = value;
                result = true;
            break;
            case "death_certificate/certificate_identification/state_file_number":
                death_certificate.certificate_identification.state_file_number = value;
                result = true;
            break;
            case "death_certificate/certificate_identification/dmaiden":
                death_certificate.certificate_identification.dmaiden = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/street":
                death_certificate.place_of_last_residence.street = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/apartment":
                death_certificate.place_of_last_residence.apartment = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/city":
                death_certificate.place_of_last_residence.city = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/state":
                death_certificate.place_of_last_residence.state = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/country_of_last_residence":
                death_certificate.place_of_last_residence.country_of_last_residence = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/zip_code":
                death_certificate.place_of_last_residence.zip_code = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/county":
                death_certificate.place_of_last_residence.county = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/feature_matching_geography_type":
                death_certificate.place_of_last_residence.feature_matching_geography_type = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/latitude":
                death_certificate.place_of_last_residence.latitude = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/longitude":
                death_certificate.place_of_last_residence.longitude = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/naaccr_gis_coordinate_quality_code":
                death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_code = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/naaccr_gis_coordinate_quality_type":
                death_certificate.place_of_last_residence.naaccr_gis_coordinate_quality_type = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/naaccr_census_tract_certainty_code":
                death_certificate.place_of_last_residence.naaccr_census_tract_certainty_code = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/naaccr_census_tract_certainty_type":
                death_certificate.place_of_last_residence.naaccr_census_tract_certainty_type = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/state_county_fips":
                death_certificate.place_of_last_residence.state_county_fips = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/census_state_fips":
                death_certificate.place_of_last_residence.census_state_fips = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/census_county_fips":
                death_certificate.place_of_last_residence.census_county_fips = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/census_tract_fips":
                death_certificate.place_of_last_residence.census_tract_fips = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/urban_status":
                death_certificate.place_of_last_residence.urban_status = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/census_met_div_fips":
                death_certificate.place_of_last_residence.census_met_div_fips = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/census_cbsa_fips":
                death_certificate.place_of_last_residence.census_cbsa_fips = value;
                result = true;
            break;
            case "death_certificate/place_of_last_residence/census_cbsa_micro":
                death_certificate.place_of_last_residence.census_cbsa_micro = value;
                result = true;
            break;
            case "death_certificate/demographics/city_of_birth":
                death_certificate.demographics.city_of_birth = value;
                result = true;
            break;
            case "death_certificate/demographics/state_of_birth":
                death_certificate.demographics.state_of_birth = value;
                result = true;
            break;
            case "death_certificate/demographics/country_of_birth":
                death_certificate.demographics.country_of_birth = value;
                result = true;
            break;
            case "death_certificate/demographics/primary_occupation":
                death_certificate.demographics.primary_occupation = value;
                result = true;
            break;
            case "death_certificate/demographics/occupation_business_industry":
                death_certificate.demographics.occupation_business_industry = value;
                result = true;
            break;
            case "death_certificate/demographics/is_of_hispanic_origin_other_specify":
                death_certificate.demographics.is_of_hispanic_origin_other_specify = value;
                result = true;
            break;
            case "death_certificate/demographics/dc_m_industry_code_1":
                death_certificate.demographics.dc_m_industry_code_1 = value;
                result = true;
            break;
            case "death_certificate/demographics/dc_m_industry_code_2":
                death_certificate.demographics.dc_m_industry_code_2 = value;
                result = true;
            break;
            case "death_certificate/demographics/dc_m_industry_code_3":
                death_certificate.demographics.dc_m_industry_code_3 = value;
                result = true;
            break;
            case "death_certificate/demographics/dc_m_occupation_code_1":
                death_certificate.demographics.dc_m_occupation_code_1 = value;
                result = true;
            break;
            case "death_certificate/demographics/dc_m_occupation_code_2":
                death_certificate.demographics.dc_m_occupation_code_2 = value;
                result = true;
            break;
            case "death_certificate/demographics/dc_m_occupation_code_3":
                death_certificate.demographics.dc_m_occupation_code_3 = value;
                result = true;
            break;
            case "death_certificate/citizen_of_what_country":
                death_certificate.citizen_of_what_country = value;
                result = true;
            break;
            case "death_certificate/race/other_race":
                death_certificate.race.other_race = value;
                result = true;
            break;
            case "death_certificate/race/other_asian":
                death_certificate.race.other_asian = value;
                result = true;
            break;
            case "death_certificate/race/other_pacific_islander":
                death_certificate.race.other_pacific_islander = value;
                result = true;
            break;
            case "death_certificate/race/principle_tribe":
                death_certificate.race.principle_tribe = value;
                result = true;
            break;
            case "death_certificate/injury_associated_information/place_of_injury":
                death_certificate.injury_associated_information.place_of_injury = value;
                result = true;
            break;
            case "death_certificate/injury_associated_information/transport_related_other_specify":
                death_certificate.injury_associated_information.transport_related_other_specify = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/street":
                death_certificate.address_of_injury.street = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/apartment":
                death_certificate.address_of_injury.apartment = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/city":
                death_certificate.address_of_injury.city = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/state":
                death_certificate.address_of_injury.state = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/zip_code":
                death_certificate.address_of_injury.zip_code = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/county":
                death_certificate.address_of_injury.county = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/feature_matching_geography_type":
                death_certificate.address_of_injury.feature_matching_geography_type = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/latitude":
                death_certificate.address_of_injury.latitude = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/longitude":
                death_certificate.address_of_injury.longitude = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/naaccr_gis_coordinate_quality_code":
                death_certificate.address_of_injury.naaccr_gis_coordinate_quality_code = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/naaccr_gis_coordinate_quality_type":
                death_certificate.address_of_injury.naaccr_gis_coordinate_quality_type = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/naaccr_census_tract_certainty_code":
                death_certificate.address_of_injury.naaccr_census_tract_certainty_code = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/naaccr_census_tract_certainty_type":
                death_certificate.address_of_injury.naaccr_census_tract_certainty_type = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/state_county_fips":
                death_certificate.address_of_injury.state_county_fips = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/census_state_fips":
                death_certificate.address_of_injury.census_state_fips = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/census_county_fips":
                death_certificate.address_of_injury.census_county_fips = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/census_tract_fips":
                death_certificate.address_of_injury.census_tract_fips = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/urban_status":
                death_certificate.address_of_injury.urban_status = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/census_met_div_fips":
                death_certificate.address_of_injury.census_met_div_fips = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/census_cbsa_fips":
                death_certificate.address_of_injury.census_cbsa_fips = value;
                result = true;
            break;
            case "death_certificate/address_of_injury/census_cbsa_micro":
                death_certificate.address_of_injury.census_cbsa_micro = value;
                result = true;
            break;
            case "death_certificate/death_information/other_death_outside_of_hospital":
                death_certificate.death_information.other_death_outside_of_hospital = value;
                result = true;
            break;
            case "death_certificate/address_of_death/place_of_death":
                death_certificate.address_of_death.place_of_death = value;
                result = true;
            break;
            case "death_certificate/address_of_death/street":
                death_certificate.address_of_death.street = value;
                result = true;
            break;
            case "death_certificate/address_of_death/apartment":
                death_certificate.address_of_death.apartment = value;
                result = true;
            break;
            case "death_certificate/address_of_death/city":
                death_certificate.address_of_death.city = value;
                result = true;
            break;
            case "death_certificate/address_of_death/state":
                death_certificate.address_of_death.state = value;
                result = true;
            break;
            case "death_certificate/address_of_death/zip_code":
                death_certificate.address_of_death.zip_code = value;
                result = true;
            break;
            case "death_certificate/address_of_death/county":
                death_certificate.address_of_death.county = value;
                result = true;
            break;
            case "death_certificate/address_of_death/feature_matching_geography_type":
                death_certificate.address_of_death.feature_matching_geography_type = value;
                result = true;
            break;
            case "death_certificate/address_of_death/latitude":
                death_certificate.address_of_death.latitude = value;
                result = true;
            break;
            case "death_certificate/address_of_death/longitude":
                death_certificate.address_of_death.longitude = value;
                result = true;
            break;
            case "death_certificate/address_of_death/naaccr_gis_coordinate_quality_code":
                death_certificate.address_of_death.naaccr_gis_coordinate_quality_code = value;
                result = true;
            break;
            case "death_certificate/address_of_death/naaccr_gis_coordinate_quality_type":
                death_certificate.address_of_death.naaccr_gis_coordinate_quality_type = value;
                result = true;
            break;
            case "death_certificate/address_of_death/naaccr_census_tract_certainty_code":
                death_certificate.address_of_death.naaccr_census_tract_certainty_code = value;
                result = true;
            break;
            case "death_certificate/address_of_death/naaccr_census_tract_certainty_type":
                death_certificate.address_of_death.naaccr_census_tract_certainty_type = value;
                result = true;
            break;
            case "death_certificate/address_of_death/state_county_fips":
                death_certificate.address_of_death.state_county_fips = value;
                result = true;
            break;
            case "death_certificate/address_of_death/census_state_fips":
                death_certificate.address_of_death.census_state_fips = value;
                result = true;
            break;
            case "death_certificate/address_of_death/census_county_fips":
                death_certificate.address_of_death.census_county_fips = value;
                result = true;
            break;
            case "death_certificate/address_of_death/census_tract_fips":
                death_certificate.address_of_death.census_tract_fips = value;
                result = true;
            break;
            case "death_certificate/address_of_death/urban_status":
                death_certificate.address_of_death.urban_status = value;
                result = true;
            break;
            case "death_certificate/address_of_death/census_met_div_fips":
                death_certificate.address_of_death.census_met_div_fips = value;
                result = true;
            break;
            case "death_certificate/address_of_death/census_cbsa_fips":
                death_certificate.address_of_death.census_cbsa_fips = value;
                result = true;
            break;
            case "death_certificate/address_of_death/census_cbsa_micro":
                death_certificate.address_of_death.census_cbsa_micro = value;
                result = true;
            break;
            case "death_certificate/reviewer_note":
                death_certificate.reviewer_note = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/vital_summary_text":
                death_certificate.vitals_import_group.vital_summary_text = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/cod1a":
                death_certificate.vitals_import_group.cod1a = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/interval1a":
                death_certificate.vitals_import_group.interval1a = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/cod1b":
                death_certificate.vitals_import_group.cod1b = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/interval1b":
                death_certificate.vitals_import_group.interval1b = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/cod1c":
                death_certificate.vitals_import_group.cod1c = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/interval1c":
                death_certificate.vitals_import_group.interval1c = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/cod1d":
                death_certificate.vitals_import_group.cod1d = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/interfval1d":
                death_certificate.vitals_import_group.interfval1d = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/othercondition":
                death_certificate.vitals_import_group.othercondition = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/man_uc":
                death_certificate.vitals_import_group.man_uc = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/acme_uc":
                death_certificate.vitals_import_group.acme_uc = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/eac":
                death_certificate.vitals_import_group.eac = value;
                result = true;
            break;
            case "death_certificate/vitals_import_group/rac":
                death_certificate.vitals_import_group.rac = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/other_maternal_level_of_care":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.other_maternal_level_of_care = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/facility_npi_number":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.facility_npi_number = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/facility_name":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.facility_name = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/other_attendant_type":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.other_attendant_type = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/attendant_npi":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.attendant_npi = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/transferred_from_where":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.transferred_from_where = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/street":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.street = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/apartment":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.apartment = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/city":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.city = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/state":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.state = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/zip_code":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.zip_code = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/county":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.county = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/latitude":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.latitude = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/longitude":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.longitude = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/feature_matching_geography_type":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.feature_matching_geography_type = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_gis_coordinate_quality_code":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_code = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_gis_coordinate_quality_type":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_gis_coordinate_quality_type = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_census_tract_certainty_code":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_code = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/naaccr_census_tract_certainty_type":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.naaccr_census_tract_certainty_type = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/urban_status":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.urban_status = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/state_county_fips":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.state_county_fips = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/census_state_fips":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.census_state_fips = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/census_county_fips":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.census_county_fips = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/census_tract_fips":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.census_tract_fips = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/census_met_div_fips":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.census_met_div_fips = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/census_cbsa_fips":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_fips = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_location/census_cbsa_micro":
                birth_fetal_death_certificate_parent.facility_of_delivery_location.census_cbsa_micro = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/city_of_birth":
                birth_fetal_death_certificate_parent.demographic_of_father.city_of_birth = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/state_of_birth":
                birth_fetal_death_certificate_parent.demographic_of_father.state_of_birth = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/father_country_of_birth":
                birth_fetal_death_certificate_parent.demographic_of_father.father_country_of_birth = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/primary_occupation":
                birth_fetal_death_certificate_parent.demographic_of_father.primary_occupation = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/occupation_business_industry":
                birth_fetal_death_certificate_parent.demographic_of_father.occupation_business_industry = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/is_father_of_hispanic_origin_other_specify":
                birth_fetal_death_certificate_parent.demographic_of_father.is_father_of_hispanic_origin_other_specify = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/race/other_race":
                birth_fetal_death_certificate_parent.demographic_of_father.race.other_race = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/race/other_asian":
                birth_fetal_death_certificate_parent.demographic_of_father.race.other_asian = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/race/other_pacific_islander":
                birth_fetal_death_certificate_parent.demographic_of_father.race.other_pacific_islander = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/race/principle_tribe":
                birth_fetal_death_certificate_parent.demographic_of_father.race.principle_tribe = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/race/omb_race_recode":
                birth_fetal_death_certificate_parent.demographic_of_father.race.omb_race_recode = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_1":
                birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_industry_code_1 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_2":
                birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_industry_code_2 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_industry_code_3":
                birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_industry_code_3 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_1":
                birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_occupation_code_1 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_2":
                birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_occupation_code_2 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/bcdcp_f_occupation_code_3":
                birth_fetal_death_certificate_parent.demographic_of_father.bcdcp_f_occupation_code_3 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/record_identification/first_name":
                birth_fetal_death_certificate_parent.record_identification.first_name = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/record_identification/middle_name":
                birth_fetal_death_certificate_parent.record_identification.middle_name = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/record_identification/last_name":
                birth_fetal_death_certificate_parent.record_identification.last_name = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/record_identification/maiden_name":
                birth_fetal_death_certificate_parent.record_identification.maiden_name = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/record_identification/medical_record_number":
                birth_fetal_death_certificate_parent.record_identification.medical_record_number = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/city_of_birth":
                birth_fetal_death_certificate_parent.demographic_of_mother.city_of_birth = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/state_of_birth":
                birth_fetal_death_certificate_parent.demographic_of_mother.state_of_birth = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/country_of_birth":
                birth_fetal_death_certificate_parent.demographic_of_mother.country_of_birth = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/primary_occupation":
                birth_fetal_death_certificate_parent.demographic_of_mother.primary_occupation = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/occupation_business_industry":
                birth_fetal_death_certificate_parent.demographic_of_mother.occupation_business_industry = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin_other_specify":
                birth_fetal_death_certificate_parent.demographic_of_mother.is_of_hispanic_origin_other_specify = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_1":
                birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_industry_code_1 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_2":
                birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_industry_code_2 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_industry_code_3":
                birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_industry_code_3 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_1":
                birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_occupation_code_1 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_2":
                birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_occupation_code_2 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/bcdcp_m_occupation_code_3":
                birth_fetal_death_certificate_parent.demographic_of_mother.bcdcp_m_occupation_code_3 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/street":
                birth_fetal_death_certificate_parent.location_of_residence.street = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/apartment":
                birth_fetal_death_certificate_parent.location_of_residence.apartment = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/city":
                birth_fetal_death_certificate_parent.location_of_residence.city = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/state":
                birth_fetal_death_certificate_parent.location_of_residence.state = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/zip_code":
                birth_fetal_death_certificate_parent.location_of_residence.zip_code = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/county":
                birth_fetal_death_certificate_parent.location_of_residence.county = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/feature_matching_geography_type":
                birth_fetal_death_certificate_parent.location_of_residence.feature_matching_geography_type = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/latitude":
                birth_fetal_death_certificate_parent.location_of_residence.latitude = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/longitude":
                birth_fetal_death_certificate_parent.location_of_residence.longitude = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/naaccr_gis_coordinate_quality_code":
                birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_code = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/naaccr_gis_coordinate_quality_type":
                birth_fetal_death_certificate_parent.location_of_residence.naaccr_gis_coordinate_quality_type = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/naaccr_census_tract_certainty_code":
                birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_code = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/naaccr_census_tract_certainty_type":
                birth_fetal_death_certificate_parent.location_of_residence.naaccr_census_tract_certainty_type = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/state_county_fips":
                birth_fetal_death_certificate_parent.location_of_residence.state_county_fips = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/census_state_fips":
                birth_fetal_death_certificate_parent.location_of_residence.census_state_fips = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/census_county_fips":
                birth_fetal_death_certificate_parent.location_of_residence.census_county_fips = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/census_tract_fips":
                birth_fetal_death_certificate_parent.location_of_residence.census_tract_fips = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/urban_status":
                birth_fetal_death_certificate_parent.location_of_residence.urban_status = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/census_met_div_fips":
                birth_fetal_death_certificate_parent.location_of_residence.census_met_div_fips = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/census_cbsa_fips":
                birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_fips = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/census_cbsa_micro":
                birth_fetal_death_certificate_parent.location_of_residence.census_cbsa_micro = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/race/other_race":
                birth_fetal_death_certificate_parent.race.other_race = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/race/other_asian":
                birth_fetal_death_certificate_parent.race.other_asian = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/race/other_pacific_islander":
                birth_fetal_death_certificate_parent.race.other_pacific_islander = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/race/principle_tribe":
                birth_fetal_death_certificate_parent.race.principle_tribe = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/race/omb_race_recode":
                birth_fetal_death_certificate_parent.race.omb_race_recode = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/pregnancy_history/pregnancy_interval":
                birth_fetal_death_certificate_parent.pregnancy_history.pregnancy_interval = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/specify_if_greater_than_3":
                birth_fetal_death_certificate_parent.prenatal_care.specify_if_greater_than_3 = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/specify_other_payor":
                birth_fetal_death_certificate_parent.prenatal_care.specify_other_payor = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/specify_other_infection":
                birth_fetal_death_certificate_parent.specify_other_infection = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/reviewer_note":
                birth_fetal_death_certificate_parent.reviewer_note = value;
                result = true;
            break;
            case "cvs/cvs_used_other_sp":
                cvs.cvs_used_other_sp = value;
                result = true;
            break;
            case "cvs/reviewer_note":
                cvs.reviewer_note = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/source_of_income_other_specify":
                social_and_environmental_profile.socio_economic_characteristics.source_of_income_other_specify = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/employment_status_other_specify":
                social_and_environmental_profile.socio_economic_characteristics.employment_status_other_specify = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/occupation":
                social_and_environmental_profile.socio_economic_characteristics.occupation = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/religious_preference":
                social_and_environmental_profile.socio_economic_characteristics.religious_preference = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/country_of_birth":
                social_and_environmental_profile.socio_economic_characteristics.country_of_birth = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_1":
                social_and_environmental_profile.socio_economic_characteristics.sep_m_occupation_code_1 = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_2":
                social_and_environmental_profile.socio_economic_characteristics.sep_m_occupation_code_2 = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_3":
                social_and_environmental_profile.socio_economic_characteristics.sep_m_occupation_code_3 = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_1":
                social_and_environmental_profile.socio_economic_characteristics.sep_m_industry_code_1 = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_2":
                social_and_environmental_profile.socio_economic_characteristics.sep_m_industry_code_2 = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_3":
                social_and_environmental_profile.socio_economic_characteristics.sep_m_industry_code_3 = value;
                result = true;
            break;
            case "social_and_environmental_profile/gender_identity/sep_genid_source_othersp":
                social_and_environmental_profile.gender_identity.sep_genid_source_othersp = value;
                result = true;
            break;
            case "social_and_environmental_profile/gender_identity/sep_genid_source_terms":
                social_and_environmental_profile.gender_identity.sep_genid_source_terms = value;
                result = true;
            break;
            case "social_and_environmental_profile/health_care_access/barriers_to_health_care_access_other_specify":
                social_and_environmental_profile.health_care_access.barriers_to_health_care_access_other_specify = value;
                result = true;
            break;
            case "social_and_environmental_profile/health_care_access/comments":
                social_and_environmental_profile.health_care_access.comments = value;
                result = true;
            break;
            case "social_and_environmental_profile/communications/barriers_to_communications_other_specify":
                social_and_environmental_profile.communications.barriers_to_communications_other_specify = value;
                result = true;
            break;
            case "social_and_environmental_profile/communications/comments":
                social_and_environmental_profile.communications.comments = value;
                result = true;
            break;
            case "social_and_environmental_profile/social_or_emotional_stress/specify_other_evidence_stress":
                social_and_environmental_profile.social_or_emotional_stress.specify_other_evidence_stress = value;
                result = true;
            break;
            case "social_and_environmental_profile/social_or_emotional_stress/explain_further":
                social_and_environmental_profile.social_or_emotional_stress.explain_further = value;
                result = true;
            break;
            case "social_and_environmental_profile/health_care_system/specify_other_reason":
                social_and_environmental_profile.health_care_system.specify_other_reason = value;
                result = true;
            break;
            case "social_and_environmental_profile/health_care_system/comments":
                social_and_environmental_profile.health_care_system.comments = value;
                result = true;
            break;
            case "social_and_environmental_profile/reviewer_note":
                social_and_environmental_profile.reviewer_note = value;
                result = true;
            break;
            case "autopsy_report/reporter_characteristics/other_specify":
                autopsy_report.reporter_characteristics.other_specify = value;
                result = true;
            break;
            case "autopsy_report/reporter_characteristics/jurisdiction":
                autopsy_report.reporter_characteristics.jurisdiction = value;
                result = true;
            break;
            case "autopsy_report/icd_code_version":
                autopsy_report.icd_code_version = value;
                result = true;
            break;
            case "autopsy_report/reviewer_note":
                autopsy_report.reviewer_note = value;
                result = true;
            break;
            case "prenatal/prenatal_care_record_no":
                prenatal.prenatal_care_record_no = value;
                result = true;
            break;
            case "prenatal/primary_prenatal_care_facility/other_place_type":
                prenatal.primary_prenatal_care_facility.other_place_type = value;
                result = true;
            break;
            case "prenatal/primary_prenatal_care_facility/specify_other_provider_type":
                prenatal.primary_prenatal_care_facility.specify_other_provider_type = value;
                result = true;
            break;
            case "prenatal/primary_prenatal_care_facility/other_payment_source":
                prenatal.primary_prenatal_care_facility.other_payment_source = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/street":
                prenatal.location_of_primary_prenatal_care_facility.street = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/apartment":
                prenatal.location_of_primary_prenatal_care_facility.apartment = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/city":
                prenatal.location_of_primary_prenatal_care_facility.city = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/state":
                prenatal.location_of_primary_prenatal_care_facility.state = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/zip_code":
                prenatal.location_of_primary_prenatal_care_facility.zip_code = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/county":
                prenatal.location_of_primary_prenatal_care_facility.county = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/feature_matching_geography_type":
                prenatal.location_of_primary_prenatal_care_facility.feature_matching_geography_type = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/latitude":
                prenatal.location_of_primary_prenatal_care_facility.latitude = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/longitude":
                prenatal.location_of_primary_prenatal_care_facility.longitude = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/naaccr_gis_coordinate_quality_code":
                prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_code = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/naaccr_gis_coordinate_quality_type":
                prenatal.location_of_primary_prenatal_care_facility.naaccr_gis_coordinate_quality_type = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/naaccr_census_tract_certainty_code":
                prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_code = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/naaccr_census_tract_certainty_type":
                prenatal.location_of_primary_prenatal_care_facility.naaccr_census_tract_certainty_type = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/state_county_fips":
                prenatal.location_of_primary_prenatal_care_facility.state_county_fips = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/census_state_fips":
                prenatal.location_of_primary_prenatal_care_facility.census_state_fips = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/census_county_fips":
                prenatal.location_of_primary_prenatal_care_facility.census_county_fips = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/census_tract_fips":
                prenatal.location_of_primary_prenatal_care_facility.census_tract_fips = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/urban_status":
                prenatal.location_of_primary_prenatal_care_facility.urban_status = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/census_met_div_fips":
                prenatal.location_of_primary_prenatal_care_facility.census_met_div_fips = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/census_cbsa_fips":
                prenatal.location_of_primary_prenatal_care_facility.census_cbsa_fips = value;
                result = true;
            break;
            case "prenatal/location_of_primary_prenatal_care_facility/census_cbsa_micro":
                prenatal.location_of_primary_prenatal_care_facility.census_cbsa_micro = value;
                result = true;
            break;
            case "prenatal/intendedenes/pi_wp_plann_sp":
                prenatal.intendedenes.pi_wp_plann_sp = value;
                result = true;
            break;
            case "prenatal/intendedenes/was_patient_using_birth_control_other_specify":
                prenatal.intendedenes.was_patient_using_birth_control_other_specify = value;
                result = true;
            break;
            case "prenatal/infertility_treatment/specify_other_art_type":
                prenatal.infertility_treatment.specify_other_art_type = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on_ultrasound":
                prenatal.current_pregnancy.estimated_date_of_confinement.estimate_based_on_ultrasound = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on_lmp":
                prenatal.current_pregnancy.estimated_date_of_confinement.estimate_based_on_lmp = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/intended_birthing_facility":
                prenatal.current_pregnancy.intended_birthing_facility = value;
                result = true;
            break;
            case "prenatal/reviewer_note":
                prenatal.reviewer_note = value;
                result = true;
            break;
            case "mental_health_profile/other_prior_to_pregnancy":
                mental_health_profile.other_prior_to_pregnancy = value;
                result = true;
            break;
            case "mental_health_profile/other_during_pregnancy":
                mental_health_profile.other_during_pregnancy = value;
                result = true;
            break;
            case "mental_health_profile/other_after_pregnancy":
                mental_health_profile.other_after_pregnancy = value;
                result = true;
            break;
            case "mental_health_profile/reviewer_note":
                mental_health_profile.reviewer_note = value;
                result = true;
            break;
            case "case_narrative/case_opening_overview":
                case_narrative.case_opening_overview = value;
                result = true;
            break;
            case "committee_review/pmss_mm":
                committee_review.pmss_mm = value;
                result = true;
            break;
            case "committee_review/pmss_mm_secondary":
                committee_review.pmss_mm_secondary = value;
                result = true;
            break;
            case "committee_review/specify_other_means_fatal_injury":
                committee_review.specify_other_means_fatal_injury = value;
                result = true;
            break;
            case "committee_review/specify_other_relationship":
                committee_review.specify_other_relationship = value;
                result = true;
            break;
            case "committee_review/cr_add_recs":
                committee_review.cr_add_recs = value;
                result = true;
            break;
            case "committee_review/notes_about_key_circumstances_surrounding_death":
                committee_review.notes_about_key_circumstances_surrounding_death = value;
                result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }

    public bool SetS_Double(string path, double? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "home_record/date_of_death/month":
                home_record.date_of_death.month = value;
                result = true;
            break;
            case "home_record/date_of_death/day":
                home_record.date_of_death.day = value;
                result = true;
            break;
            case "home_record/date_of_death/year":
                home_record.date_of_death.year = value;
                result = true;
            break;
            case "home_record/case_status/overall_case_status":
                home_record.case_status.overall_case_status = value;
                result = true;
            break;
            case "home_record/overall_assessment_of_timing_of_death/abstrator_assigned_status":
                home_record.overall_assessment_of_timing_of_death.abstrator_assigned_status = value;
                result = true;
            break;
            case "home_record/overall_assessment_of_timing_of_death/number_of_days_after_end_of_pregnancey":
                home_record.overall_assessment_of_timing_of_death.number_of_days_after_end_of_pregnancey = value;
                result = true;
            break;
            case "home_record/overall_assessment_of_timing_of_death/hr_prg_outcome":
                home_record.overall_assessment_of_timing_of_death.hr_prg_outcome = value;
                result = true;
            break;
            case "home_record/case_progress_report/death_certificate":
                home_record.case_progress_report.death_certificate = value;
                result = true;
            break;
            case "home_record/case_progress_report/autopsy_report":
                home_record.case_progress_report.autopsy_report = value;
                result = true;
            break;
            case "home_record/case_progress_report/birth_certificate_parent_section":
                home_record.case_progress_report.birth_certificate_parent_section = value;
                result = true;
            break;
            case "home_record/case_progress_report/birth_certificate_infant_or_fetal_death_section":
                home_record.case_progress_report.birth_certificate_infant_or_fetal_death_section = value;
                result = true;
            break;
            case "home_record/case_progress_report/community_vital_signs":
                home_record.case_progress_report.community_vital_signs = value;
                result = true;
            break;
            case "home_record/case_progress_report/social_and_psychological_profile":
                home_record.case_progress_report.social_and_psychological_profile = value;
                result = true;
            break;
            case "home_record/case_progress_report/prenatal_care_record":
                home_record.case_progress_report.prenatal_care_record = value;
                result = true;
            break;
            case "home_record/case_progress_report/er_visits_and_hospitalizations":
                home_record.case_progress_report.er_visits_and_hospitalizations = value;
                result = true;
            break;
            case "home_record/case_progress_report/other_medical_visits":
                home_record.case_progress_report.other_medical_visits = value;
                result = true;
            break;
            case "home_record/case_progress_report/medical_transport":
                home_record.case_progress_report.medical_transport = value;
                result = true;
            break;
            case "home_record/case_progress_report/mental_health_profile":
                home_record.case_progress_report.mental_health_profile = value;
                result = true;
            break;
            case "home_record/case_progress_report/informant_interviews":
                home_record.case_progress_report.informant_interviews = value;
                result = true;
            break;
            case "home_record/case_progress_report/case_narrative":
                home_record.case_progress_report.case_narrative = value;
                result = true;
            break;
            case "home_record/case_progress_report/committe_review_worksheet":
                home_record.case_progress_report.committe_review_worksheet = value;
                result = true;
            break;
            case "home_record/automated_vitals_group/bc_det_match":
                home_record.automated_vitals_group.bc_det_match = value;
                result = true;
            break;
            case "home_record/automated_vitals_group/fdc_det_match":
                home_record.automated_vitals_group.fdc_det_match = value;
                result = true;
            break;
            case "home_record/automated_vitals_group/bc_prob_match":
                home_record.automated_vitals_group.bc_prob_match = value;
                result = true;
            break;
            case "home_record/automated_vitals_group/fdc_prob_match":
                home_record.automated_vitals_group.fdc_prob_match = value;
                result = true;
            break;
            case "home_record/automated_vitals_group/icd10_match":
                home_record.automated_vitals_group.icd10_match = value;
                result = true;
            break;
            case "home_record/automated_vitals_group/pregcb_match":
                home_record.automated_vitals_group.pregcb_match = value;
                result = true;
            break;
            case "home_record/automated_vitals_group/literalcod_match":
                home_record.automated_vitals_group.literalcod_match = value;
                result = true;
            break;
            case "death_certificate/demographics/date_of_birth/month":
                death_certificate.demographics.date_of_birth.month = value;
                result = true;
            break;
            case "death_certificate/demographics/date_of_birth/day":
                death_certificate.demographics.date_of_birth.day = value;
                result = true;
            break;
            case "death_certificate/demographics/date_of_birth/year":
                death_certificate.demographics.date_of_birth.year = value;
                result = true;
            break;
            case "death_certificate/demographics/age":
                death_certificate.demographics.age = value;
                result = true;
            break;
            case "death_certificate/demographics/age_on_death_certificate":
                death_certificate.demographics.age_on_death_certificate = value;
                result = true;
            break;
            case "death_certificate/demographics/marital_status":
                death_certificate.demographics.marital_status = value;
                result = true;
            break;
            case "death_certificate/demographics/ever_in_us_armed_forces":
                death_certificate.demographics.ever_in_us_armed_forces = value;
                result = true;
            break;
            case "death_certificate/demographics/is_of_hispanic_origin":
                death_certificate.demographics.is_of_hispanic_origin = value;
                result = true;
            break;
            case "death_certificate/demographics/education_level":
                death_certificate.demographics.education_level = value;
                result = true;
            break;
            case "death_certificate/race/omb_race_recode":
                death_certificate.race.omb_race_recode = value;
                result = true;
            break;
            case "death_certificate/injury_associated_information/date_of_injury/month":
                death_certificate.injury_associated_information.date_of_injury.month = value;
                result = true;
            break;
            case "death_certificate/injury_associated_information/date_of_injury/day":
                death_certificate.injury_associated_information.date_of_injury.day = value;
                result = true;
            break;
            case "death_certificate/injury_associated_information/date_of_injury/year":
                death_certificate.injury_associated_information.date_of_injury.year = value;
                result = true;
            break;
            case "death_certificate/injury_associated_information/was_injury_at_work":
                death_certificate.injury_associated_information.was_injury_at_work = value;
                result = true;
            break;
            case "death_certificate/injury_associated_information/transportation_related_injury":
                death_certificate.injury_associated_information.transportation_related_injury = value;
                result = true;
            break;
            case "death_certificate/injury_associated_information/were_seat_belts_in_use":
                death_certificate.injury_associated_information.were_seat_belts_in_use = value;
                result = true;
            break;
            case "death_certificate/death_information/death_occured_in_hospital":
                death_certificate.death_information.death_occured_in_hospital = value;
                result = true;
            break;
            case "death_certificate/death_information/death_outside_of_hospital":
                death_certificate.death_information.death_outside_of_hospital = value;
                result = true;
            break;
            case "death_certificate/death_information/manner_of_death":
                death_certificate.death_information.manner_of_death = value;
                result = true;
            break;
            case "death_certificate/death_information/was_autopsy_performed":
                death_certificate.death_information.was_autopsy_performed = value;
                result = true;
            break;
            case "death_certificate/death_information/was_autopsy_used_for_death_coding":
                death_certificate.death_information.was_autopsy_used_for_death_coding = value;
                result = true;
            break;
            case "death_certificate/death_information/pregnancy_status":
                death_certificate.death_information.pregnancy_status = value;
                result = true;
            break;
            case "death_certificate/death_information/did_tobacco_contribute_to_death":
                death_certificate.death_information.did_tobacco_contribute_to_death = value;
                result = true;
            break;
            case "death_certificate/address_of_death/estimated_death_distance_from_residence":
                death_certificate.address_of_death.estimated_death_distance_from_residence = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.month = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.day = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.date_of_delivery.year = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/type_of_place":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.type_of_place = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/was_home_delivery_planned":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.was_home_delivery_planned = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/maternal_level_of_care":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.maternal_level_of_care = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/attendant_type":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.attendant_type = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/was_mother_transferred":
                birth_fetal_death_certificate_parent.facility_of_delivery_demographics.was_mother_transferred = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth/month":
                birth_fetal_death_certificate_parent.demographic_of_father.date_of_birth.month = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/date_of_birth/year":
                birth_fetal_death_certificate_parent.demographic_of_father.date_of_birth.year = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/age":
                birth_fetal_death_certificate_parent.demographic_of_father.age = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/education_level":
                birth_fetal_death_certificate_parent.demographic_of_father.education_level = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/is_father_of_hispanic_origin":
                birth_fetal_death_certificate_parent.demographic_of_father.is_father_of_hispanic_origin = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/month":
                birth_fetal_death_certificate_parent.demographic_of_mother.date_of_birth.month = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/day":
                birth_fetal_death_certificate_parent.demographic_of_mother.date_of_birth.day = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/year":
                birth_fetal_death_certificate_parent.demographic_of_mother.date_of_birth.year = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/age":
                birth_fetal_death_certificate_parent.demographic_of_mother.age = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/mother_married":
                birth_fetal_death_certificate_parent.demographic_of_mother.mother_married = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital":
                birth_fetal_death_certificate_parent.demographic_of_mother.If_mother_not_married_has_paternity_acknowledgement_been_signed_in_the_hospital = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/ever_in_us_armed_forces":
                birth_fetal_death_certificate_parent.demographic_of_mother.ever_in_us_armed_forces = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin":
                birth_fetal_death_certificate_parent.demographic_of_mother.is_of_hispanic_origin = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_mother/education_level":
                birth_fetal_death_certificate_parent.demographic_of_mother.education_level = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/location_of_residence/estimated_distance_from_residence":
                birth_fetal_death_certificate_parent.location_of_residence.estimated_distance_from_residence = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/month":
                birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_live_birth.month = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/day":
                birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_live_birth.day = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_live_birth/year":
                birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_live_birth.year = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/pregnancy_history/live_birth_interval":
                birth_fetal_death_certificate_parent.pregnancy_history.live_birth_interval = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/pregnancy_history/number_of_previous_live_births":
                birth_fetal_death_certificate_parent.pregnancy_history.number_of_previous_live_births = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/pregnancy_history/now_living":
                birth_fetal_death_certificate_parent.pregnancy_history.now_living = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/pregnancy_history/now_dead":
                birth_fetal_death_certificate_parent.pregnancy_history.now_dead = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/pregnancy_history/other_outcomes":
                birth_fetal_death_certificate_parent.pregnancy_history.other_outcomes = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/month":
                birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_other_outcome.month = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/day":
                birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_other_outcome.day = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/pregnancy_history/date_of_last_other_outcome/year":
                birth_fetal_death_certificate_parent.pregnancy_history.date_of_last_other_outcome.year = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/maternal_biometrics/height_feet":
                birth_fetal_death_certificate_parent.maternal_biometrics.height_feet = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/maternal_biometrics/height_inches":
                birth_fetal_death_certificate_parent.maternal_biometrics.height_inches = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/maternal_biometrics/pre_pregnancy_weight":
                birth_fetal_death_certificate_parent.maternal_biometrics.pre_pregnancy_weight = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/maternal_biometrics/weight_at_delivery":
                birth_fetal_death_certificate_parent.maternal_biometrics.weight_at_delivery = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/maternal_biometrics/weight_gain":
                birth_fetal_death_certificate_parent.maternal_biometrics.weight_gain = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/maternal_biometrics/bmi":
                birth_fetal_death_certificate_parent.maternal_biometrics.bmi = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/month":
                birth_fetal_death_certificate_parent.prenatal_care.date_of_last_normal_menses.month = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/day":
                birth_fetal_death_certificate_parent.prenatal_care.date_of_last_normal_menses.day = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_normal_menses/year":
                birth_fetal_death_certificate_parent.prenatal_care.date_of_last_normal_menses.year = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit/month":
                birth_fetal_death_certificate_parent.prenatal_care.date_of_1st_prenatal_visit.month = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit/day":
                birth_fetal_death_certificate_parent.prenatal_care.date_of_1st_prenatal_visit.day = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/date_of_1st_prenatal_visit/year":
                birth_fetal_death_certificate_parent.prenatal_care.date_of_1st_prenatal_visit.year = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/month":
                birth_fetal_death_certificate_parent.prenatal_care.date_of_last_prenatal_visit.month = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/day":
                birth_fetal_death_certificate_parent.prenatal_care.date_of_last_prenatal_visit.day = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/date_of_last_prenatal_visit/year":
                birth_fetal_death_certificate_parent.prenatal_care.date_of_last_prenatal_visit.year = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation":
                birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/calculated_gestation_days":
                birth_fetal_death_certificate_parent.prenatal_care.calculated_gestation_days = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/obsteric_estimate_of_gestation":
                birth_fetal_death_certificate_parent.prenatal_care.obsteric_estimate_of_gestation = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/plurality":
                birth_fetal_death_certificate_parent.prenatal_care.plurality = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/was_wic_used":
                birth_fetal_death_certificate_parent.prenatal_care.was_wic_used = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/principal_source_of_payment_for_this_delivery":
                birth_fetal_death_certificate_parent.prenatal_care.principal_source_of_payment_for_this_delivery = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/trimester_of_1st_prenatal_care_visit":
                birth_fetal_death_certificate_parent.prenatal_care.trimester_of_1st_prenatal_care_visit = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/prenatal_care/number_of_visits":
                birth_fetal_death_certificate_parent.prenatal_care.number_of_visits = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/cigarette_smoking/prior_3_months":
                birth_fetal_death_certificate_parent.cigarette_smoking.prior_3_months = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/cigarette_smoking/prior_3_months_type":
                birth_fetal_death_certificate_parent.cigarette_smoking.prior_3_months_type = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/cigarette_smoking/trimester_1st":
                birth_fetal_death_certificate_parent.cigarette_smoking.trimester_1st = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/cigarette_smoking/trimester_1st_type":
                birth_fetal_death_certificate_parent.cigarette_smoking.trimester_1st_type = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/cigarette_smoking/trimester_2nd":
                birth_fetal_death_certificate_parent.cigarette_smoking.trimester_2nd = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/cigarette_smoking/trimester_2nd_type":
                birth_fetal_death_certificate_parent.cigarette_smoking.trimester_2nd_type = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/cigarette_smoking/trimester_3rd":
                birth_fetal_death_certificate_parent.cigarette_smoking.trimester_3rd = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/cigarette_smoking/trimester_3rd_type":
                birth_fetal_death_certificate_parent.cigarette_smoking.trimester_3rd_type = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/cigarette_smoking/none_or_not_specified":
                birth_fetal_death_certificate_parent.cigarette_smoking.none_or_not_specified = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/risk_factors/number_of_c_sections":
                birth_fetal_death_certificate_parent.risk_factors.number_of_c_sections = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/length_between_child_birth_and_death_of_mother":
                birth_fetal_death_certificate_parent.length_between_child_birth_and_death_of_mother = value;
                result = true;
            break;
            case "cvs/cvs_used":
                cvs.cvs_used = value;
                result = true;
            break;
            case "cvs/cvs_used_how":
                cvs.cvs_used_how = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/source_of_income":
                social_and_environmental_profile.socio_economic_characteristics.source_of_income = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/employment_status":
                social_and_environmental_profile.socio_economic_characteristics.employment_status = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/immigration_status":
                social_and_environmental_profile.socio_economic_characteristics.immigration_status = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/time_in_the_us":
                social_and_environmental_profile.socio_economic_characteristics.time_in_the_us = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/time_in_the_us_units":
                social_and_environmental_profile.socio_economic_characteristics.time_in_the_us_units = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements":
                social_and_environmental_profile.socio_economic_characteristics.current_living_arrangements = value;
                result = true;
            break;
            case "social_and_environmental_profile/gender_identity/sep_genid_is_nonfemale":
                social_and_environmental_profile.gender_identity.sep_genid_is_nonfemale = value;
                result = true;
            break;
            case "social_and_environmental_profile/health_care_system/no_prenatal_care":
                social_and_environmental_profile.health_care_system.no_prenatal_care = value;
                result = true;
            break;
            case "social_and_environmental_profile/had_military_service":
                social_and_environmental_profile.had_military_service = value;
                result = true;
            break;
            case "social_and_environmental_profile/was_there_bereavement_support":
                social_and_environmental_profile.was_there_bereavement_support = value;
                result = true;
            break;
            case "social_and_environmental_profile/documented_substance_use":
                social_and_environmental_profile.documented_substance_use = value;
                result = true;
            break;
            case "autopsy_report/was_there_an_autopsy_referral":
                autopsy_report.was_there_an_autopsy_referral = value;
                result = true;
            break;
            case "autopsy_report/type_of_autopsy_or_examination":
                autopsy_report.type_of_autopsy_or_examination = value;
                result = true;
            break;
            case "autopsy_report/is_autopsy_or_exam_report_available":
                autopsy_report.is_autopsy_or_exam_report_available = value;
                result = true;
            break;
            case "autopsy_report/was_toxicology_performed":
                autopsy_report.was_toxicology_performed = value;
                result = true;
            break;
            case "autopsy_report/is_toxicology_report_available":
                autopsy_report.is_toxicology_report_available = value;
                result = true;
            break;
            case "autopsy_report/completeness_of_autopsy_information":
                autopsy_report.completeness_of_autopsy_information = value;
                result = true;
            break;
            case "autopsy_report/reporter_characteristics/reporter_type":
                autopsy_report.reporter_characteristics.reporter_type = value;
                result = true;
            break;
            case "autopsy_report/reporter_characteristics/date_of_autopsy/month":
                autopsy_report.reporter_characteristics.date_of_autopsy.month = value;
                result = true;
            break;
            case "autopsy_report/reporter_characteristics/date_of_autopsy/day":
                autopsy_report.reporter_characteristics.date_of_autopsy.day = value;
                result = true;
            break;
            case "autopsy_report/reporter_characteristics/date_of_autopsy/year":
                autopsy_report.reporter_characteristics.date_of_autopsy.year = value;
                result = true;
            break;
            case "autopsy_report/biometrics/mother/height/feet":
                autopsy_report.biometrics.mother.height.feet = value;
                result = true;
            break;
            case "autopsy_report/biometrics/mother/height/inches":
                autopsy_report.biometrics.mother.height.inches = value;
                result = true;
            break;
            case "autopsy_report/biometrics/mother/weight":
                autopsy_report.biometrics.mother.weight = value;
                result = true;
            break;
            case "autopsy_report/biometrics/mother/bmi":
                autopsy_report.biometrics.mother.bmi = value;
                result = true;
            break;
            case "autopsy_report/biometrics/fetus/fetal_weight_uom":
                autopsy_report.biometrics.fetus.fetal_weight_uom = value;
                result = true;
            break;
            case "autopsy_report/biometrics/fetus/fetal_weight":
                autopsy_report.biometrics.fetus.fetal_weight = value;
                result = true;
            break;
            case "autopsy_report/biometrics/fetus/fetal_weight_ounce_value":
                autopsy_report.biometrics.fetus.fetal_weight_ounce_value = value;
                result = true;
            break;
            case "autopsy_report/biometrics/fetus/fetal_length_uom":
                autopsy_report.biometrics.fetus.fetal_length_uom = value;
                result = true;
            break;
            case "autopsy_report/biometrics/fetus/fetal_length":
                autopsy_report.biometrics.fetus.fetal_length = value;
                result = true;
            break;
            case "autopsy_report/biometrics/fetus/gestational_age_estimate":
                autopsy_report.biometrics.fetus.gestational_age_estimate = value;
                result = true;
            break;
            case "autopsy_report/was_drug_toxicology_positive":
                autopsy_report.was_drug_toxicology_positive = value;
                result = true;
            break;
            case "prenatal/number_of_pnc_sources":
                prenatal.number_of_pnc_sources = value;
                result = true;
            break;
            case "prenatal/primary_prenatal_care_facility/place_type":
                prenatal.primary_prenatal_care_facility.place_type = value;
                result = true;
            break;
            case "prenatal/primary_prenatal_care_facility/primary_provider_type":
                prenatal.primary_prenatal_care_facility.primary_provider_type = value;
                result = true;
            break;
            case "prenatal/primary_prenatal_care_facility/principal_source_of_payment":
                prenatal.primary_prenatal_care_facility.principal_source_of_payment = value;
                result = true;
            break;
            case "prenatal/primary_prenatal_care_facility/is_use_wic":
                prenatal.primary_prenatal_care_facility.is_use_wic = value;
                result = true;
            break;
            case "prenatal/had_pre_existing_conditions":
                prenatal.had_pre_existing_conditions = value;
                result = true;
            break;
            case "prenatal/were_there_documented_mental_health_conditions":
                prenatal.were_there_documented_mental_health_conditions = value;
                result = true;
            break;
            case "prenatal/evidence_of_substance_use":
                prenatal.evidence_of_substance_use = value;
                result = true;
            break;
            case "prenatal/pregnancy_history/gravida":
                prenatal.pregnancy_history.gravida = value;
                result = true;
            break;
            case "prenatal/pregnancy_history/para":
                prenatal.pregnancy_history.para = value;
                result = true;
            break;
            case "prenatal/pregnancy_history/abortions":
                prenatal.pregnancy_history.abortions = value;
                result = true;
            break;
            case "prenatal/intendedenes/date_birth_control_was_discontinued/month":
                prenatal.intendedenes.date_birth_control_was_discontinued.month = value;
                result = true;
            break;
            case "prenatal/intendedenes/date_birth_control_was_discontinued/day":
                prenatal.intendedenes.date_birth_control_was_discontinued.day = value;
                result = true;
            break;
            case "prenatal/intendedenes/date_birth_control_was_discontinued/year":
                prenatal.intendedenes.date_birth_control_was_discontinued.year = value;
                result = true;
            break;
            case "prenatal/intendedenes/was_pregnancy_planned":
                prenatal.intendedenes.was_pregnancy_planned = value;
                result = true;
            break;
            case "prenatal/intendedenes/was_patient_using_birth_control":
                prenatal.intendedenes.was_patient_using_birth_control = value;
                result = true;
            break;
            case "prenatal/infertility_treatment/was_pregnancy_result_of_infertility_treatment":
                prenatal.infertility_treatment.was_pregnancy_result_of_infertility_treatment = value;
                result = true;
            break;
            case "prenatal/infertility_treatment/fertility_enhanding_drugs":
                prenatal.infertility_treatment.fertility_enhanding_drugs = value;
                result = true;
            break;
            case "prenatal/infertility_treatment/assisted_reproductive_technology":
                prenatal.infertility_treatment.assisted_reproductive_technology = value;
                result = true;
            break;
            case "prenatal/infertility_treatment/art_type":
                prenatal.infertility_treatment.art_type = value;
                result = true;
            break;
            case "prenatal/infertility_treatment/cycle_number":
                prenatal.infertility_treatment.cycle_number = value;
                result = true;
            break;
            case "prenatal/infertility_treatment/embryos_transferred":
                prenatal.infertility_treatment.embryos_transferred = value;
                result = true;
            break;
            case "prenatal/infertility_treatment/embryos_growing":
                prenatal.infertility_treatment.embryos_growing = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_last_normal_menses/month":
                prenatal.current_pregnancy.date_of_last_normal_menses.month = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_last_normal_menses/day":
                prenatal.current_pregnancy.date_of_last_normal_menses.day = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_last_normal_menses/year":
                prenatal.current_pregnancy.date_of_last_normal_menses.year = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/estimated_date_of_confinement/month":
                prenatal.current_pregnancy.estimated_date_of_confinement.month = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/estimated_date_of_confinement/day":
                prenatal.current_pregnancy.estimated_date_of_confinement.day = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/estimated_date_of_confinement/year":
                prenatal.current_pregnancy.estimated_date_of_confinement.year = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/estimated_date_of_confinement/estimate_based_on":
                prenatal.current_pregnancy.estimated_date_of_confinement.estimate_based_on = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_1st_prenatal_visit/month":
                prenatal.current_pregnancy.date_of_1st_prenatal_visit.month = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_1st_prenatal_visit/day":
                prenatal.current_pregnancy.date_of_1st_prenatal_visit.day = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_1st_prenatal_visit/year":
                prenatal.current_pregnancy.date_of_1st_prenatal_visit.year = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_weeks":
                prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_weeks = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_1st_prenatal_visit/gestational_age_days":
                prenatal.current_pregnancy.date_of_1st_prenatal_visit.gestational_age_days = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_1st_ultrasound/month":
                prenatal.current_pregnancy.date_of_1st_ultrasound.month = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_1st_ultrasound/day":
                prenatal.current_pregnancy.date_of_1st_ultrasound.day = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_1st_ultrasound/year":
                prenatal.current_pregnancy.date_of_1st_ultrasound.year = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_1st_ultrasound/gestational_age_at_first_ultrasound":
                prenatal.current_pregnancy.date_of_1st_ultrasound.gestational_age_at_first_ultrasound = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_1st_ultrasound/gestational_age_at_first_ultrasound_days":
                prenatal.current_pregnancy.date_of_1st_ultrasound.gestational_age_at_first_ultrasound_days = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_last_prenatal_visit/month":
                prenatal.current_pregnancy.date_of_last_prenatal_visit.month = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_last_prenatal_visit/day":
                prenatal.current_pregnancy.date_of_last_prenatal_visit.day = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_last_prenatal_visit/year":
                prenatal.current_pregnancy.date_of_last_prenatal_visit.year = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit":
                prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/date_of_last_prenatal_visit/gestational_age_at_last_prenatal_visit_days":
                prenatal.current_pregnancy.date_of_last_prenatal_visit.gestational_age_at_last_prenatal_visit_days = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/height/feet":
                prenatal.current_pregnancy.height.feet = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/height/inches":
                prenatal.current_pregnancy.height.inches = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/pre_pregnancy_weight":
                prenatal.current_pregnancy.pre_pregnancy_weight = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/bmi":
                prenatal.current_pregnancy.bmi = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/weight_at_1st_visit":
                prenatal.current_pregnancy.weight_at_1st_visit = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/weight_at_last_visit":
                prenatal.current_pregnancy.weight_at_last_visit = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/weight_gain":
                prenatal.current_pregnancy.weight_gain = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/total_number_of_visits":
                prenatal.current_pregnancy.total_number_of_visits = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/trimester_of_first_pnc_visit":
                prenatal.current_pregnancy.trimester_of_first_pnc_visit = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/number_of_fetuses":
                prenatal.current_pregnancy.number_of_fetuses = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/was_home_delivery_planned":
                prenatal.current_pregnancy.was_home_delivery_planned = value;
                result = true;
            break;
            case "prenatal/current_pregnancy/attended_prenatal_visits_alone":
                prenatal.current_pregnancy.attended_prenatal_visits_alone = value;
                result = true;
            break;
            case "prenatal/highest_blood_pressure/systolic":
                prenatal.highest_blood_pressure.systolic = value;
                result = true;
            break;
            case "prenatal/highest_blood_pressure/diastolic":
                prenatal.highest_blood_pressure.diastolic = value;
                result = true;
            break;
            case "prenatal/lowest_hematocrit":
                prenatal.lowest_hematocrit = value;
                result = true;
            break;
            case "prenatal/were_there_problems_identified":
                prenatal.were_there_problems_identified = value;
                result = true;
            break;
            case "prenatal/were_there_adverse_reactions":
                prenatal.were_there_adverse_reactions = value;
                result = true;
            break;
            case "prenatal/were_there_pre_delivery_hospitalizations":
                prenatal.were_there_pre_delivery_hospitalizations = value;
                result = true;
            break;
            case "prenatal/were_medical_referrals_to_others":
                prenatal.were_medical_referrals_to_others = value;
                result = true;
            break;
            case "mental_health_profile/were_there_documented_preexisting_mental_health_conditions":
                mental_health_profile.were_there_documented_preexisting_mental_health_conditions = value;
                result = true;
            break;
            case "committee_review/pregnancy_relatedness":
                committee_review.pregnancy_relatedness = value;
                result = true;
            break;
            case "committee_review/estimate_degree_relevant_information_available":
                committee_review.estimate_degree_relevant_information_available = value;
                result = true;
            break;
            case "committee_review/does_committee_agree_with_cod_on_death_certificate":
                committee_review.does_committee_agree_with_cod_on_death_certificate = value;
                result = true;
            break;
            case "committee_review/did_obesity_contribute_to_the_death":
                committee_review.did_obesity_contribute_to_the_death = value;
                result = true;
            break;
            case "committee_review/did_discrimination_contribute_to_the_death":
                committee_review.did_discrimination_contribute_to_the_death = value;
                result = true;
            break;
            case "committee_review/did_mental_health_conditions_contribute_to_the_death":
                committee_review.did_mental_health_conditions_contribute_to_the_death = value;
                result = true;
            break;
            case "committee_review/did_substance_use_disorder_contribute_to_the_death":
                committee_review.did_substance_use_disorder_contribute_to_the_death = value;
                result = true;
            break;
            case "committee_review/was_this_death_a_sucide":
                committee_review.was_this_death_a_sucide = value;
                result = true;
            break;
            case "committee_review/was_this_death_a_homicide":
                committee_review.was_this_death_a_homicide = value;
                result = true;
            break;
            case "committee_review/means_of_fatal_injury":
                committee_review.means_of_fatal_injury = value;
                result = true;
            break;
            case "committee_review/if_homicide_relationship_of_perpetrator":
                committee_review.if_homicide_relationship_of_perpetrator = value;
                result = true;
            break;
            case "committee_review/was_this_death_preventable":
                committee_review.was_this_death_preventable = value;
                result = true;
            break;
            case "committee_review/chance_to_alter_outcome":
                committee_review.chance_to_alter_outcome = value;
                result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }

    public bool SetS_Boolean(string path, bool? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }

    public bool SetS_List_Of_Double(string path, List<double> value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "home_record/how_was_this_death_identified":
                home_record.how_was_this_death_identified = value;
                result = true;
            break;
            case "death_certificate/race/race":
                death_certificate.race.race = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/demographic_of_father/race/race_of_father":
                birth_fetal_death_certificate_parent.demographic_of_father.race.race_of_father = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/race/race_of_mother":
                birth_fetal_death_certificate_parent.race.race_of_mother = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/risk_factors/risk_factors_in_this_pregnancy":
                birth_fetal_death_certificate_parent.risk_factors.risk_factors_in_this_pregnancy = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/infections_present_or_treated_during_pregnancy":
                birth_fetal_death_certificate_parent.infections_present_or_treated_during_pregnancy = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/onset_of_labor":
                birth_fetal_death_certificate_parent.onset_of_labor = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/obstetric_procedures":
                birth_fetal_death_certificate_parent.obstetric_procedures = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/characteristics_of_labor_and_delivery":
                birth_fetal_death_certificate_parent.characteristics_of_labor_and_delivery = value;
                result = true;
            break;
            case "birth_fetal_death_certificate_parent/maternal_morbidity":
                birth_fetal_death_certificate_parent.maternal_morbidity = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/homelessness":
                social_and_environmental_profile.socio_economic_characteristics.homelessness = value;
                result = true;
            break;
            case "social_and_environmental_profile/socio_economic_characteristics/unstable_housing":
                social_and_environmental_profile.socio_economic_characteristics.unstable_housing = value;
                result = true;
            break;
            case "social_and_environmental_profile/gender_identity/sep_genid_source":
                social_and_environmental_profile.gender_identity.sep_genid_source = value;
                result = true;
            break;
            case "social_and_environmental_profile/previous_or_current_incarcerations":
                social_and_environmental_profile.previous_or_current_incarcerations = value;
                result = true;
            break;
            case "social_and_environmental_profile/was_decedent_ever_arrested":
                social_and_environmental_profile.was_decedent_ever_arrested = value;
                result = true;
            break;
            case "social_and_environmental_profile/health_care_access/barriers_to_health_care_access":
                social_and_environmental_profile.health_care_access.barriers_to_health_care_access = value;
                result = true;
            break;
            case "social_and_environmental_profile/communications/barriers_to_communications":
                social_and_environmental_profile.communications.barriers_to_communications = value;
                result = true;
            break;
            case "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress":
                social_and_environmental_profile.social_or_emotional_stress.evidence_of_social_or_emotional_stress = value;
                result = true;
            break;
            case "social_and_environmental_profile/health_care_system/reasons_for_missed_appointments":
                social_and_environmental_profile.health_care_system.reasons_for_missed_appointments = value;
                result = true;
            break;
            case "mental_health_profile/mental_health_conditions_prior_to_the_most_recent_pregnancy":
                mental_health_profile.mental_health_conditions_prior_to_the_most_recent_pregnancy = value;
                result = true;
            break;
            case "mental_health_profile/mental_health_conditions_during_the_most_recent_pregnancy":
                mental_health_profile.mental_health_conditions_during_the_most_recent_pregnancy = value;
                result = true;
            break;
            case "mental_health_profile/mental_health_conditions_after_the_most_recent_pregnancy":
                mental_health_profile.mental_health_conditions_after_the_most_recent_pregnancy = value;
                result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }

    
    public bool SetS_List_Of_String(string path, List<string> value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
            

        }
       
        catch(Exception)
        {

        }
        
        return result;
    }

    public bool SetS_Datetime(string path, DateTime? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "date_created":
                date_created = value;
                result = true;
            break;
            case "date_last_updated":
                date_last_updated = value;
                result = true;
            break;
            case "date_last_checked_out":
                date_last_checked_out = value;
                result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }


    public bool SetS_Date_Only(string path, DateOnly? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "home_record/case_status/abstraction_begin_date":
                home_record.case_status.abstraction_begin_date = value;
                result = true;
            break;
            case "home_record/case_status/abstraction_complete_date":
                home_record.case_status.abstraction_complete_date = value;
                result = true;
            break;
            case "home_record/case_status/projected_review_date":
                home_record.case_status.projected_review_date = value;
                result = true;
            break;
            case "home_record/case_status/committee_review_date":
                home_record.case_status.committee_review_date = value;
                result = true;
            break;
            case "home_record/case_status/case_locked_date":
                home_record.case_status.case_locked_date = value;
                result = true;
            break;
            case "committee_review/date_of_review":
                committee_review.date_of_review = value;
                result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }


    public bool SetS_Time_Only(string path, TimeOnly? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "death_certificate/certificate_identification/time_of_death":
                death_certificate.certificate_identification.time_of_death = value;
                result = true;
            break;
            case "death_certificate/injury_associated_information/time_of_injury":
                death_certificate.injury_associated_information.time_of_injury = value;
                result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }


}