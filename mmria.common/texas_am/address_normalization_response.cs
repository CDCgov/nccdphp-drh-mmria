using System;

namespace mmria.common.texas_am;

public sealed class address_normalization_response
{
    

    public address_normalization_response ()
    {
    }


    public string Number { get; set; }
    public string NumberFractional { get; set; }
    public string PreDirectional { get; set; }
    public string PreQualifier { get; set; }
    public string PreType { get; set; }
    public string PreArticle { get; set; }
    public string StreetName { get; set; }
    public string Suffix { get; set; }
    public string PostArticle { get; set; }
    public string PostQualifier { get; set; }
    public string PostDirectional { get; set; }
    public string SuiteType { get; set; }
    public string SuiteNumber { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZIP { get; set; }
    public string ZIPPlus1 { get; set; }
    public string ZIPPlus2 { get; set; }
    public string ZIPPlus3 { get; set; }
    public string ZIPPlus4 { get; set; }
    public string ZIPPlus5 { get; set; }
    public string PostOfficeBoxType { get; set; }
    public string PostOfficeBoxNumber { get; set; }


    /*
    {
        "TransactionId" : "0744d967-764e-4d16-86eb-e7262f311812",
        "Version"   : "4.01",
        "QueryStatusCode" : "Success",
        "StreetAddresses" : 
        [
            {
                "Number" : "123",
                "NumberFractional" : "",
                "PreDirectional" : "",
                "PreQualifier" : "",
                "PreType" : "",
                "PreArticle" : "",
                "StreetName" : "OLD US 25",
                "Suffix" : "",
                "PostArticle" : "",
                "PostQualifier" : "",
                "PostDirectional" : "",
                "SuiteType" : "",
                "SuiteNumber" : "",
                "City" : "LOS ANGELES",
                "State" : "CA",
                "ZIP" : "90089",
                "ZIPPlus1" : "",
                "ZIPPlus2" : "",
                "ZIPPlus3" : "",
                "ZIPPlus4" : "0255",
                "ZIPPlus5" : "",
                "PostOfficeBoxType" : "",
                "PostOfficeBoxNumber" : ""
            }
        ]
    }*/

}


