using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using mmria.common.SharedLibraries.Geocoding;

namespace mmria.common.SharedLibraries.Case.Manager;

// Declarative case-document target for a geocode location key.
// Static → mutations under BasePath. List → mutations under ListPath[listIndex]/SubPath.
public readonly record struct GeocodeTarget(bool IsList, string BasePath, string ListPath, string SubPath)
{
    public static GeocodeTarget Static(string basePath) => new(false, basePath, string.Empty, string.Empty);
    public static GeocodeTarget List(string listPath, string subPath) => new(true, string.Empty, listPath, subPath);
}

// Applies a GeocodeResult to a specific location inside a case document.
// Pure document mutation — no CouchDB access. Safe as a DI singleton.
public sealed class CaseGeocodingManager
{
    // Single source of truth for location key → case-document target mapping.
    // Adding a new geocode-enabled location is a single-entry addition here — no other file changes required.
    public static readonly IReadOnlyDictionary<string, GeocodeTarget> LocationRegistry =
        new Dictionary<string, GeocodeTarget>(System.StringComparer.Ordinal)
        {
            ["dc_place_of_last_residence"] = GeocodeTarget.Static("death_certificate/place_of_last_residence"),
            ["dc_address_of_injury"]       = GeocodeTarget.Static("death_certificate/address_of_injury"),
            ["dc_address_of_death"]        = GeocodeTarget.Static("death_certificate/address_of_death"),
            ["bc_facility_of_delivery"]    = GeocodeTarget.Static("birth_fetal_death_certificate_parent/facility_of_delivery_location"),
            ["bc_location_of_residence"]   = GeocodeTarget.Static("birth_fetal_death_certificate_parent/location_of_residence"),
            ["pc_primary_care_facility"]   = GeocodeTarget.Static("prenatal/location_of_primary_prenatal_care_facility"),
            ["erh_location"]               = GeocodeTarget.List("er_visit_and_hospital_medical_records", "name_and_location_facility"),
            ["omv_location_of_care"]       = GeocodeTarget.List("other_medical_office_visits", "location_of_medical_care_facility"),
            ["mt_origin_address"]          = GeocodeTarget.List("medical_transport", "origin_information/address"),
            ["mt_destination_address"]     = GeocodeTarget.List("medical_transport", "destination_information/address"),
        };

    // Unknown keys are ignored so batch callers can invoke without pre-validation.
    public void Apply(ExpandoObject caseDoc, string locationKey, GeocodeResult result, int listIndex = 0)
    {
        if (locationKey == null || !LocationRegistry.TryGetValue(locationKey, out var target))
        {
            return;
        }

        if (target.IsList)
        {
            ApplyList(caseDoc, result, target.ListPath, listIndex, target.SubPath);
        }
        else
        {
            ApplyStatic(caseDoc, result, target.BasePath);
        }
    }

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
