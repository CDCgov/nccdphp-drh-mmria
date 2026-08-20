using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using mmria.common.SharedLibraries.Case;
using mmria.common.SharedLibraries.Case.Manager;
using mmria.common.SharedLibraries.Geocoding;
using mmria.common.SharedLibraries.Geocoding.Manager;

using mmria.server.extension;

namespace mmria.server;

// Story 30.3: unified server-side endpoint — geocode + apply + optional CVS + save in one request.
// Client POSTs address + locationKey; server writes the case doc and returns { ok: true } so the
// client can reload the full case rather than reconstructing fields from the response.
[Authorize]
[Route("api/case-geocode")]
public sealed class CaseGeocodeController : ControllerBase
{
    // Derived from CaseGeocodingManager.LocationRegistry — single source of truth for supported location keys.
    private static readonly HashSet<string> _validKeys =
        new(CaseGeocodingManager.LocationRegistry.Keys, StringComparer.Ordinal);

    private static readonly HashSet<string> _listKeys = new(
        CaseGeocodingManager.LocationRegistry.Where(kv => kv.Value.IsList).Select(kv => kv.Key),
        StringComparer.Ordinal);

    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly mmria.common.couchdb.DBConfigurationDetail _dbConfig;
    private readonly string _hostPrefix;
    private readonly ICaseRepository _caseRepository;
    private readonly GeocodingManager _geocodingManager;
    private readonly CaseGeocodingManager _caseGeocodingManager;
    private readonly mmria.common.SharedLibraries.CVS.Manager.CVSManager _cvsManager;
    private readonly ILogger<CaseGeocodeController> _logger;

    public CaseGeocodeController(
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        ICaseRepository caseRepository,
        GeocodingManager geocodingManager,
        CaseGeocodingManager caseGeocodingManager,
        mmria.common.SharedLibraries.CVS.Manager.CVSManager cvsManager,
        ILogger<CaseGeocodeController> logger)
    {
        _configuration = tenantRuntime.RequireConfiguration();
        _dbConfig = tenantRuntime.RequireDbConfig();
        _hostPrefix = tenantRuntime.EffectiveHostPrefix;
        _caseRepository = caseRepository;
        _geocodingManager = geocodingManager;
        _caseGeocodingManager = caseGeocodingManager;
        _cvsManager = cvsManager;
        _logger = logger;
    }

    public sealed class Geocode_Request
    {
        public string street { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
        public string censusYear { get; set; }
        public int? listIndex { get; set; }
    }

    [Authorize(Roles = "abstractor")]
    [HttpPost("{caseId}/{locationKey}")]
    public async Task<IActionResult> Post(string caseId, string locationKey)
    {
        var safeCaseId = SanitizeSingleLineText(caseId, 256);
        var safeLocationKey = SanitizeSingleLineText(locationKey, 64);

        if (string.IsNullOrWhiteSpace(safeCaseId))
        {
            return BadRequest(new { error = "caseId is required." });
        }

        if (!_validKeys.Contains(safeLocationKey))
        {
            return BadRequest(new { error = $"Invalid locationKey: '{safeLocationKey}'." });
        }

        Geocode_Request request;
        try
        {
            request = await mmria.server.util.JsonRequestBodyReader.ReadAsync<Geocode_Request>(Request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "case-geocode: failed to parse request body for case {CaseId}", safeCaseId);
            return BadRequest(new { error = "Invalid request body." });
        }

        if (request == null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        if (_listKeys.Contains(safeLocationKey) && !request.listIndex.HasValue)
        {
            return BadRequest(new { error = $"listIndex is required for locationKey '{safeLocationKey}'." });
        }

        // Load case doc
        string caseJson;
        try
        {
            caseJson = await _caseRepository.GetCaseDocumentJsonAsync(safeCaseId, _dbConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "case-geocode: failed to load case {CaseId}", safeCaseId);
            return StatusCode(500, new { error = "Failed to load case document." });
        }

        if (string.IsNullOrWhiteSpace(caseJson) || caseJson.Contains("\"not_found\"", StringComparison.Ordinal))
        {
            return NotFound(new { error = $"Case not found: {safeCaseId}" });
        }

        ExpandoObject caseDoc;
        try
        {
            caseDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(caseJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "case-geocode: failed to parse case {CaseId}", safeCaseId);
            return StatusCode(500, new { error = "Failed to parse case document." });
        }

        if (caseDoc == null)
        {
            return StatusCode(500, new { error = "Case document is empty." });
        }

        // Resolve census year — request body wins, otherwise derive from home_record.date_of_death.year.
        var censusYear = SanitizeSingleLineText(request.censusYear, 4);
        if (string.IsNullOrWhiteSpace(censusYear))
        {
            censusYear = TryGetDateOfDeathYear(caseDoc) ?? "";
        }

        // Fetch geocode. GeocodingManager never throws — returns empty result on failure.
        GeocodeResult geocodeResult;
        try
        {
            var geocodeApiKey = _configuration.GetSharedString("geocode_api_key");
            geocodeResult = _geocodingManager.FetchGeocode(
                geocodeApiKey,
                SanitizeSingleLineText(request.street, 200),
                SanitizeSingleLineText(request.city, 100),
                SanitizeSingleLineText(request.state, 64),
                SanitizeSingleLineText(request.zip, 10),
                censusYear);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "case-geocode: geocode call failed for case {CaseId}", safeCaseId);
            return StatusCode(500, new { error = "Geocode lookup failed." });
        }

        if (geocodeResult == null)
        {
            geocodeResult = new GeocodeResult();
        }

        _caseGeocodingManager.Apply(caseDoc, safeLocationKey, geocodeResult, request.listIndex ?? 0);

        // Server-side CVS lookup — only for dc_place_of_last_residence when the geocode matched.
        if (string.Equals(safeLocationKey, "dc_place_of_last_residence", StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(geocodeResult.FeatureMatchingGeographyType) &&
            !string.Equals(geocodeResult.FeatureMatchingGeographyType, "Unmatchable", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await TryRunCvsAsync(caseDoc, safeCaseId, geocodeResult);
            }
            catch (Exception ex)
            {
                // CVS failures are non-fatal — the geocode has already been applied.
                _logger.LogWarning(ex, "case-geocode: CVS lookup failed for case {CaseId} — continuing", safeCaseId);
            }
        }

        // Save
        try
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            };
            var updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(caseDoc, settings);
            await _caseRepository.PutCaseDocumentJsonAsync(safeCaseId, updatedJson, _dbConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "case-geocode: failed to save case {CaseId}", safeCaseId);
            return StatusCode(500, new { error = "Failed to save case document." });
        }

        return Ok(new { ok = true });
    }

    // Server-side replacement for the client's CVS "data" round-trip.
    // Field mapping mirrors mmria.js callback_cvs_data_success (cvs.cvs_grid[0]).
    private async Task TryRunCvsAsync(ExpandoObject caseDoc, string caseId, GeocodeResult result)
    {
        var stateCountyFips = result.StateCountyFips ?? "";
        var tractDigits = (result.CensusTractFips ?? "").Replace(".", "");
        if (string.IsNullOrWhiteSpace(stateCountyFips) || string.IsNullOrWhiteSpace(tractDigits))
        {
            return;
        }

        var year = TryGetDateOfDeathYear(caseDoc);
        if (string.IsNullOrWhiteSpace(year))
        {
            return;
        }

        var cGeoid = stateCountyFips;
        var tGeoid = stateCountyFips + tractDigits.PadLeft(6, '0');

        var cvsConfig = _configuration.GetCVSConfigurationDetail();
        var cvsResult = await _cvsManager.GetAllDataAsync(
            new mmria.common.cvs.post_payload
            {
                c_geoid = cGeoid,
                t_geoid = tGeoid,
                year = year,
                id = caseId
            },
            cvsConfig);

        var gridItem = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["cvs_api_request_url"] = "/api/case-geocode",
            ["cvs_api_request_date_time"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            ["cvs_api_request_c_geoid"] = cGeoid,
            ["cvs_api_request_t_geoid"] = tGeoid,
            ["cvs_api_request_year"] = year,
            ["cvs_api_request_result_message"] =
                (cvsResult == null || cvsResult.tract == null || cvsResult.county == null)
                    ? "CVS data unavailable."
                    : "Data request successful."
        };

        if (cvsResult != null && cvsResult.tract != null && cvsResult.county != null)
        {
            var t = cvsResult.tract;
            var c = cvsResult.county;
            gridItem["cvs_pctnoins_fem_county"] = Fmt(c.pctNOIns_Fem);
            gridItem["cvs_pctnoins_fem_tract"] = Fmt(t.pctNOIns_Fem);
            gridItem["cvs_pctnovehicle_county"] = Fmt(c.pctNoVehicle);
            gridItem["cvs_pctnovehicle_tract"] = Fmt(t.pctNoVehicle);
            gridItem["cvs_pctmove_county"] = Fmt(c.pctMOVE);
            gridItem["cvs_pctmove_tract"] = Fmt(t.pctMOVE);
            gridItem["cvs_pctsphh_county"] = Fmt(c.pctSPHH);
            gridItem["cvs_pctsphh_tract"] = Fmt(t.pctSPHH);
            gridItem["cvs_pctovercrowdhh_county"] = Fmt(c.pctOVERCROWDHH);
            gridItem["cvs_pctovercrowdhh_tract"] = Fmt(t.pctOVERCROWDHH);
            gridItem["cvs_pctowner_occ_county"] = Fmt(c.pctOWNER_OCC);
            gridItem["cvs_pctowner_occ_tract"] = Fmt(t.pctOWNER_OCC);
            gridItem["cvs_pct_less_well_county"] = Fmt(c.pct_less_well);
            gridItem["cvs_pct_less_well_tract"] = Fmt(t.pct_less_well);
            gridItem["cvs_ndi_raw_county"] = Fmt(c.NDI_raw);
            gridItem["cvs_ndi_raw_tract"] = Fmt(t.NDI_raw);
            gridItem["cvs_pctpov_county"] = Fmt(c.pctPOV);
            gridItem["cvs_pctpov_tract"] = Fmt(t.pctPOV);
            gridItem["cvs_ice_income_all_county"] = Fmt(c.ICE_INCOME_all);
            gridItem["cvs_ice_income_all_tract"] = Fmt(t.ICE_INCOME_all);
            gridItem["cvs_medhhinc_county"] = Fmt(c.MEDHHINC);
            gridItem["cvs_medhhinc_tract"] = Fmt(t.MEDHHINC);
            gridItem["cvs_pctobese_county"] = Fmt(c.pctOBESE);
            gridItem["cvs_fi_county"] = Fmt(c.FI);
            gridItem["cvs_obgynrate_county"] = Fmt(c.OBGYNrate);
            gridItem["cvs_rtteenbirth_county"] = Fmt(c.rtTEENBIRTH);
            gridItem["cvs_rtstd_county"] = Fmt(c.rtSTD);
            // Client mmria.js maps cvs_rtmhpract_county <- county.mhcenteRrate (MHCENTERrate).
            gridItem["cvs_rtmhpract_county"] = Fmt(c.MHCENTERrate);
            gridItem["cvs_rtdrugodmortality_county"] = Fmt(c.rtDRUGODMORTALITY);
            gridItem["cvs_rtopioidprescript_county"] = Fmt(c.rtOPIOIDPRESCRIPT);
            gridItem["cvs_soccap_county"] = Fmt(c.SocCap);
            gridItem["cvs_rtsocassoc_county"] = Fmt(c.rtSocASSOC);
            gridItem["cvs_pcthouse_distress_county"] = Fmt(c.pctHOUSE_DISTRESS);
            // Client mmria.js maps cvs_cnmrate_county <- county.midwiveSrate (MIDWIVESrate).
            gridItem["cvs_cnmrate_county"] = Fmt(c.MIDWIVESrate);
            gridItem["cvs_isolation_county"] = Fmt(c.segregation);
            // Client mmria.js maps cvs_mdrate_county <- county.pcPrate (PCPrate).
            gridItem["cvs_mdrate_county"] = Fmt(c.PCPrate);
            gridItem["cvs_rtviolentcr_icpsr_county"] = Fmt(c.rtVIOLENTCR);
            gridItem["cvs_pctrural"] = Fmt(c.pctRural);
            gridItem["cvs_mhproviderrate"] = Fmt(c.MHPROVIDERrate);
            gridItem["cvs_racialized_pov"] = Fmt(c.Racialized_pov);
        }

        IDictionary<string, object> root = caseDoc;
        if (!root.TryGetValue("cvs", out var cvsObj) || cvsObj is not IDictionary<string, object> cvsDict)
        {
            var newCvs = new ExpandoObject();
            IDictionary<string, object> newCvsDict = newCvs;
            newCvsDict["cvs_used"] = "9999";
            newCvsDict["cvs_used_how"] = "9999";
            newCvsDict["cvs_used_other_sp"] = "";
            root["cvs"] = newCvs;
            cvsDict = newCvsDict;
        }
        cvsDict["cvs_grid"] = new List<object> { gridItem };
    }

    private static string Fmt(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static string TryGetDateOfDeathYear(ExpandoObject caseDoc)
    {
        IDictionary<string, object> root = caseDoc;
        if (root.TryGetValue("home_record", out var hrObj) && hrObj is IDictionary<string, object> hr &&
            hr.TryGetValue("date_of_death", out var dodObj) && dodObj is IDictionary<string, object> dod &&
            dod.TryGetValue("year", out var y) && y != null)
        {
            return y.ToString();
        }
        return null;
    }

    private static string SanitizeSingleLineText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(value.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return sanitized.Length > maxLength ? sanitized[..maxLength] : sanitized;
    }
}
