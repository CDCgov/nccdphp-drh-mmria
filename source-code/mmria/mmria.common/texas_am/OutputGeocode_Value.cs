﻿using System;
namespace mmria.common
{
	public class OutputGeocode_Value
	{
		public OutputGeocode_Value()
		{
		}

		public string Latitude { get; set; }
		public string Longitude { get; set; }
		public string NAACCRGISCoordinateQualityCode { get; set; }
		public string NAACCRGISCoordinateQualityType { get; set; }
		public string MatchScore { get; set; }
		public string MatchType { get; set; }
		public string FeatureMatchingResultType { get; set; }
		public string FeatureMatchingResultCount { get; set; }
		public string FeatureMatchingGeographyType { get; set; }
		public string RegionSize { get; set; }
		public string RegionSizeUnits { get; set; }
		public string MatchedLocationType { get; set; }
		public string ExceptionOccured { get; set; }
		public string Exception { get; set; }
		public string ErrorMessage { get; set; }
		public System.Collections.Generic.KeyValuePair<string, census_value>[] CensusValues { get; set; }


	}
}
