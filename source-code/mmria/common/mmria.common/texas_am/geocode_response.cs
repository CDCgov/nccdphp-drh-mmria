using System;
using System.Collections.Generic;

namespace mmria.common.texas_am;

public sealed class OutputGeocodeItem
{


    public OutputGeocodeItem()
    {
        CensusValues = new List<Dictionary<string, CensusValue>>();
    }

    public OutputGeocode OutputGeocode {get; set;}

    public List<Dictionary<string, CensusValue>> CensusValues {get;set;}
}
public sealed class CensusValue
{
    public CensusValue(){}
    public string CensusYear { get;set;}
    public string CensusTimeTaken { get;set;}
    public string NAACCRCensusTractCertaintyCode { get;set;}
    public string NAACCRCensusTractCertaintyType { get;set;}
    public string CensusBlock { get;set;}
    public string CensusBlockGroup { get;set;}
    public string CensusTract { get;set;}
    public string CensusCountyFips { get;set;}
    public string CensusStateFips { get;set;}
    public string CensusCbsaFips { get;set;}
    public string CensusCbsaMicro { get;set;}
    public string CensusMcdFips { get;set;}
    public string CensusMetDivFips { get;set;}
    public string CensusMsaFips { get;set;}
    public string CensusPlaceFips { get;set;}
    public string ExceptionOccured { get;set;}
    public string Exception { get;set;}
    public string ErrorMessage { get;set;}
}
public sealed class OutputGeocode
{
    public OutputGeocode(){}

    public string Latitude { get;set;}
    public string Longitude { get;set;}
    public string NAACCRGISCoordinateQualityCode { get;set;}
    public string NAACCRGISCoordinateQualityType { get;set;}
    public string MatchScore { get;set;}
    public string MatchType { get;set;}
    public string FeatureMatchingResultType { get;set;}
    public string FeatureMatchingResultCount { get;set;}
    public string FeatureMatchingGeographyType { get;set;}
    public string RegionSize { get;set;}
    public string RegionSizeUnits { get;set;}
    public string MatchedLocationType { get;set;}
    public string ExceptionOccured { get;set;}
    public string Exception { get;set;}
    public string ErrorMessage { get;set;}

}
public sealed class geocode_response
{
    public geocode_response ()
    {
    }

    //public string version { get; set; }
    public string TransactionId { get; set; }
    public string Version { get; set; }
    public string QueryStatusCodeValue { get; set; }
    public string FeatureMatchingResultType { get; set; }
    public string FeatureMatchingResultCount { get; set; }
    public string TimeTaken { get; set; }
    public string ExceptionOccured { get; set; }
    public string Exception { get; set; }
    public InputAddress InputAddress { get; set; }
    public  OutputGeocodeItem[] OutputGeocodes { get; set; }


}


