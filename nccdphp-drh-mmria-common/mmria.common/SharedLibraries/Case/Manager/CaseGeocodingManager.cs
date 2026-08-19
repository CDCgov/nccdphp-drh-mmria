using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using mmria.common.SharedLibraries.Geocoding;

namespace mmria.common.SharedLibraries.Case.Manager;

// Applies a GeocodeResult to a specific location inside a case document.
// Pure document mutation — no CouchDB access. Safe as a DI singleton.
public sealed class CaseGeocodingManager
{
    public void Apply_DC_PlaceOfLastResidence_Geocode(ExpandoObject caseDoc, GeocodeResult result)
        => ApplyStatic(caseDoc, result, "death_certificate/place_of_last_residence");

    public void Apply_DC_AddressOfInjury_Geocode(ExpandoObject caseDoc, GeocodeResult result)
        => ApplyStatic(caseDoc, result, "death_certificate/address_of_injury");

    public void Apply_DC_AddressOfDeath_Geocode(ExpandoObject caseDoc, GeocodeResult result)
        => ApplyStatic(caseDoc, result, "death_certificate/address_of_death");

    public void Apply_BC_FacilityOfDelivery_Geocode(ExpandoObject caseDoc, GeocodeResult result)
        => ApplyStatic(caseDoc, result, "birth_fetal_death_certificate_parent/facility_of_delivery_location");

    public void Apply_BC_LocationOfResidence_Geocode(ExpandoObject caseDoc, GeocodeResult result)
        => ApplyStatic(caseDoc, result, "birth_fetal_death_certificate_parent/location_of_residence");

    public void Apply_PC_PrimaryCareFacility_Geocode(ExpandoObject caseDoc, GeocodeResult result)
        => ApplyStatic(caseDoc, result, "prenatal_care_record/location_of_primary_prenatal_care_facility");

    public void Apply_ERH_Location_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)
        => ApplyList(caseDoc, result, "er_visit_and_hospital_medical_records", listIndex, "location");

    public void Apply_OMV_LocationOfCare_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)
        => ApplyList(caseDoc, result, "other_medical_office_visits", listIndex, "location_of_medical_care_facility");

    public void Apply_MT_OriginAddress_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)
        => ApplyList(caseDoc, result, "medical_transport", listIndex, "origin_information/address");

    public void Apply_MT_DestinationAddress_Geocode(ExpandoObject caseDoc, GeocodeResult result, int listIndex)
        => ApplyList(caseDoc, result, "medical_transport", listIndex, "destination_information/address");

    private static void ApplyStatic(ExpandoObject caseDoc, GeocodeResult result, string basePath)
    {
        if (caseDoc == null || result == null)
        {
            return;
        }

        var gs = new migrate.C_Get_Set_Value(new StringBuilder());
        foreach (var (fieldName, value) in ResolveFieldValues(result))
        {
            gs.set_value($"{basePath}/{fieldName}", value, caseDoc);
        }
    }

    private static void ApplyList(
        ExpandoObject caseDoc,
        GeocodeResult result,
        string listPath,
        int listIndex,
        string subPath)
    {
        if (caseDoc == null || result == null || listIndex < 0)
        {
            return;
        }

        IDictionary<string, object> root = caseDoc;
        if (!root.TryGetValue(listPath, out var listObj) || listObj is not IList<object> list)
        {
            return;
        }

        if (listIndex >= list.Count || list[listIndex] is not IDictionary<string, object> item)
        {
            return;
        }

        var gs = new migrate.C_Get_Set_Value(new StringBuilder());
        foreach (var (fieldName, value) in ResolveFieldValues(result))
        {
            gs.set_value($"{subPath}/{fieldName}", value, item);
        }
    }

    private static IEnumerable<(string FieldName, string Value)> ResolveFieldValues(GeocodeResult result)
    {
        // Unmatchable / empty-result guard: mirrors the JS unmatchable path — write "" for every field.
        bool unmatched =
            string.IsNullOrEmpty(result.FeatureMatchingGeographyType) ||
            string.Equals(result.FeatureMatchingGeographyType, "Unmatchable", System.StringComparison.OrdinalIgnoreCase);

        string V(string success) => unmatched ? "" : (success ?? "");

        yield return ("latitude", V(result.Latitude));
        yield return ("longitude", V(result.Longitude));
        yield return ("feature_matching_geography_type", V(result.FeatureMatchingGeographyType));
        yield return ("naaccr_gis_coordinate_quality_code", V(result.NAACCRGISCoordinateQualityCode));
        yield return ("naaccr_gis_coordinate_quality_type", V(result.NAACCRGISCoordinateQualityType));
        yield return ("naaccr_census_tract_certainty_code", V(result.NAACCRCensusTractCertaintyCode));
        yield return ("naaccr_census_tract_certainty_type", V(result.NAACCRCensusTractCertaintyType));
        yield return ("census_state_fips", V(result.CensusStateFips));
        yield return ("census_county_fips", V(result.CensusCountyFips));
        yield return ("census_tract_fips", V(result.CensusTractFips));
        yield return ("census_cbsa_fips", V(result.CensusCbsaFips));
        yield return ("census_cbsa_micro", V(result.CensusCbsaMicro));
        yield return ("census_met_div_fips", V(result.CensusMetDivFips));
        yield return ("urban_status", V(result.UrbanStatus));
        yield return ("state_county_fips", V(result.StateCountyFips));
    }
}
