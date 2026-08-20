namespace mmria.common.SharedLibraries.Geocoding;

public sealed class GeocodeResult
{
    public string Latitude { get; set; } = "";
    public string Longitude { get; set; } = "";
    public string FeatureMatchingGeographyType { get; set; } = "";
    public string NAACCRGISCoordinateQualityCode { get; set; } = "";
    public string NAACCRGISCoordinateQualityType { get; set; } = "";
    public string NAACCRCensusTractCertaintyCode { get; set; } = "";
    public string NAACCRCensusTractCertaintyType { get; set; } = "";
    public string CensusStateFips { get; set; } = "";
    public string CensusCountyFips { get; set; } = "";
    public string CensusTractFips { get; set; } = "";
    public string CensusCbsaFips { get; set; } = "";
    public string CensusCbsaMicro { get; set; } = "";
    public string CensusMetDivFips { get; set; } = "";
    public string UrbanStatus { get; set; } = "Undetermined";
    public string StateCountyFips { get; set; } = "";
}
