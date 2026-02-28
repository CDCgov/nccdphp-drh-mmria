using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.Model.AggregateReport;

namespace mmria.common.Manager;

public sealed class AggregateReportManager
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public AggregateReportManager(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    /// <summary>
    /// Retrieves all aggregate reports from CouchDB and filters for valid entries.
    /// </summary>
    public async Task<IList<c_report_object>> GetReportsAsync(DBConfigurationDetail dbConfig)
    {
        var result = new List<c_report_object>();

        string request_string = dbConfig.Get_Prefix_DB_Url("report/_all_docs?include_docs=true");

            string responseFromServer = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                request_string,
                null,
                dbConfig.user_name,
                dbConfig.user_value
            );

            // Parse the JSON response
            using (JsonDocument doc = JsonDocument.Parse(responseFromServer))
            {
                var root = doc.RootElement;
                
                if (root.TryGetProperty("rows", out JsonElement rowsElement) &&
                    rowsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var rowItem in rowsElement.EnumerateArray())
                    {
                        if (rowItem.TryGetProperty("doc", out JsonElement docElement))
                        {
                            var convert_result = Convert(docElement);

                            if (convert_result.Key)
                            {
                                var item = convert_result.Value;

                                // Filter: year_of_death != 9999 and year_of_case_review is present
                                if (item.year_of_death.HasValue &&
                                    item.year_of_death.Value != 9999 &&
                                    item.year_of_case_review.HasValue)
                                {
                                    result.Add(item);
                                }
                            }
                        }
                    }
                }
            }

        return result;
    }

    /// <summary>
    /// Converts a raw CouchDB document (as JsonElement) into a strongly-typed c_report_object.
    /// </summary>
    private KeyValuePair<bool, c_report_object> Convert(JsonElement docElement)
    {
        var temp = new c_report_object();
        bool is_complete_conversion = true;

        try
        {
            // Extract _id
            if (docElement.TryGetProperty("_id", out JsonElement idElement))
            {
                temp._id = idElement.GetString();
            }

            // Extract numeric fields
            if (docElement.TryGetProperty("year_of_death", out JsonElement yearOfDeathElement) &&
                yearOfDeathElement.TryGetInt32(out int yearOfDeath))
            {
                temp.year_of_death = yearOfDeath;
            }

            if (docElement.TryGetProperty("year_of_case_review", out JsonElement yearOfCaseReviewElement) &&
                yearOfCaseReviewElement.TryGetInt32(out int yearOfCaseReview))
            {
                temp.year_of_case_review = yearOfCaseReview;
            }

            if (docElement.TryGetProperty("month_of_case_review", out JsonElement monthOfCaseReviewElement) &&
                monthOfCaseReviewElement.TryGetInt32(out int monthOfCaseReview))
            {
                temp.month_of_case_review = monthOfCaseReview;
            }

            // Total number of cases by pregnancy relatedness
            if (docElement.TryGetProperty("total_number_of_cases_by_pregnancy_relatedness", out JsonElement pregRelElement))
            {
                PopulatePregnancyRelatednessStruct(ref temp.total_number_of_cases_by_pregnancy_relatedness, pregRelElement);
            }

            // Total number of pregnancy related deaths by ethnicity
            if (docElement.TryGetProperty("total_number_of_pregnancy_related_deaths_by_ethnicity", out JsonElement pregRelEthnicElement))
            {
                PopulateEthnicityStruct(ref temp.total_number_of_pregnancy_related_deaths_by_ethnicity, pregRelEthnicElement);
            }

            // Total number of pregnancy associated by ethnicity
            if (docElement.TryGetProperty("total_number_of_pregnancy_associated_by_ethnicity", out JsonElement pregAssocEthnicElement))
            {
                PopulateEthnicityStruct(ref temp.total_number_of_pregnancy_associated_by_ethnicity, pregAssocEthnicElement);
            }

            // Total number of pregnancy related deaths by age
            if (docElement.TryGetProperty("total_number_of_pregnancy_related_deaths_by_age", out JsonElement pregRelAgeElement))
            {
                PopulateAgeStruct(ref temp.total_number_of_pregnancy_related_deaths_by_age, pregRelAgeElement);
            }

            // Total number of pregnancy associated deaths by age
            if (docElement.TryGetProperty("total_number_of_pregnancy_associated_deaths_by_age", out JsonElement pregAssocAgeElement))
            {
                PopulateAgeStruct(ref temp.total_number_of_pregnancy_associated_deaths_by_age, pregAssocAgeElement);
            }

            // Total number pregnancy related at time of death
            if (docElement.TryGetProperty("total_number_pregnancy_related_at_time_of_death", out JsonElement pregRelTimeElement))
            {
                PopulateTimingOfDeathStruct(ref temp.total_number_pregnancy_related_at_time_of_death, pregRelTimeElement);
            }

            // Total number pregnancy associated at time of death
            if (docElement.TryGetProperty("total_number_pregnancy_associated_at_time_of_death", out JsonElement pregAssocTimeElement))
            {
                PopulateTimingOfDeathStruct(ref temp.total_number_pregnancy_associated_at_time_of_death, pregAssocTimeElement);
            }

            // Dictionary fields
            PopulateList(ref temp.distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm,
                docElement, "distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm");

            PopulateList(ref temp.total_pregnancy_related_determined_to_be_preventable,
                docElement, "total_pregnancy_related_determined_to_be_preventable");
            PopulateList(ref temp.total_pregnancy_associated_determined_to_be_preventable,
                docElement, "total_pregnancy_associated_determined_to_be_preventable");

            PopulateList(ref temp.total_pregnancy_related_obesity_contributed_to_the_death,
                docElement, "total_pregnancy_related_obesity_contributed_to_the_death");
            PopulateList(ref temp.total_pregnancy_associated_obesity_contributed_to_the_death,
                docElement, "total_pregnancy_associated_obesity_contributed_to_the_death");

            PopulateList(ref temp.total_pregnancy_related_mental_health_conditions_contributed_to_death,
                docElement, "total_pregnancy_related_mental_health_conditions_contributed_to_death");
            PopulateList(ref temp.total_pregnancy_associated_mental_health_conditions_contributed_to_death,
                docElement, "total_pregnancy_associated_mental_health_conditions_contributed_to_death");

            PopulateList(ref temp.total_pregnancy_related_substance_use_disorder_contributed_to_death,
                docElement, "total_pregnancy_related_substance_use_disorder_contributed_to_death");
            PopulateList(ref temp.total_pregnancy_associated_substance_use_disorder_contributed_to_death,
                docElement, "total_pregnancy_associated_substance_use_disorder_contributed_to_death");

            PopulateList(ref temp.total_pregnancy_related_is_suicide,
                docElement, "total_pregnancy_related_is_suicide");
            PopulateList(ref temp.total_pregnancy_associated_is_suicide,
                docElement, "total_pregnancy_associated_is_suicide");

            PopulateList(ref temp.total_pregnancy_related_is_homocide,
                docElement, "total_pregnancy_related_is_homocide");
            PopulateList(ref temp.total_pregnancy_associated_is_homocide,
                docElement, "total_pregnancy_associated_is_homocide");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            is_complete_conversion = false;
        }

        return new KeyValuePair<bool, c_report_object>(is_complete_conversion, temp);
    }

    /// <summary>
    /// Populates a pregnancy relatedness struct from a JsonElement.
    /// </summary>
    private void PopulatePregnancyRelatednessStruct(ref total_number_of_cases_by_pregnancy_relatedness_struct target, JsonElement source)
    {
        target.pregnancy_related = GetIntValue(source, "pregnancy_related");
        target.pregnancy_associated_but_not_related = GetIntValue(source, "pregnancy_associated_but_not_related");
        target.not_pregnancy_related_or_associated = GetIntValue(source, "not_pregnancy_related_or_associated");
        target.unable_to_determine = GetIntValue(source, "unable_to_determine");
        target.blank = GetIntValue(source, "blank");
    }

    /// <summary>
    /// Populates an ethnicity_struct from a JsonElement.
    /// </summary>
    private void PopulateEthnicityStruct(ref ethnicity_struct target, JsonElement source)
    {
        target.blank = GetIntValue(source, "blank");
        target.hispanic = GetIntValue(source, "hispanic");
        target.non_hispanic_black = GetIntValue(source, "non_hispanic_black");
        target.non_hispanic_white = GetIntValue(source, "non_hispanic_white");
        target.american_indian_alaska_native = GetIntValue(source, "american_indian_alaska_native");
        target.native_hawaiian = GetIntValue(source, "native_hawaiian");
        target.guamanian_or_chamorro = GetIntValue(source, "guamanian_or_chamorro");
        target.samoan = GetIntValue(source, "samoan");
        target.other_pacific_islander = GetIntValue(source, "other_pacific_islander");
        target.asian_indian = GetIntValue(source, "asian_indian");
        target.filipino = GetIntValue(source, "filipino");
        target.korean = GetIntValue(source, "korean");
        target.other_asian = GetIntValue(source, "other_asian");
        target.chinese = GetIntValue(source, "chinese");
        target.japanese = GetIntValue(source, "japanese");
        target.vietnamese = GetIntValue(source, "vietnamese");
        target.other = GetIntValue(source, "other");
    }

    /// <summary>
    /// Populates an age_at_death_struct from a JsonElement.
    /// </summary>
    private void PopulateAgeStruct(ref age_at_death_struct target, JsonElement source)
    {
        target.age_less_than_20 = GetIntValue(source, "age_less_than_20");
        target.age_20_to_24 = GetIntValue(source, "age_20_to_24");
        target.age_25_to_29 = GetIntValue(source, "age_25_to_29");
        target.age_30_to_34 = GetIntValue(source, "age_30_to_34");
        target.age_35_to_44 = GetIntValue(source, "age_35_to_44");
        target.age_45_and_above = GetIntValue(source, "age_45_and_above");
        target.blank = GetIntValue(source, "blank");
    }

    /// <summary>
    /// Populates a timing_of_death_in_relation_to_pregnancy_struct from a JsonElement.
    /// </summary>
    private void PopulateTimingOfDeathStruct(ref timing_of_death_in_relation_to_pregnancy_struct target, JsonElement source)
    {
        target.pregnant_at_the_time_of_death = GetIntValue(source, "pregnant_at_the_time_of_death");
        target.pregnant_within_42_days_of_death = GetIntValue(source, "pregnant_within_42_days_of_death");
        target.pregnant_within_43_to_365_days_of_death = GetIntValue(source, "pregnant_within_43_to_365_days_of_death");
        target.blank = GetIntValue(source, "blank");
    }

    /// <summary>
    /// Populates a dictionary with integer values from a JsonElement.
    /// </summary>
    private void PopulateList(ref System.Collections.Generic.Dictionary<string, int> target,
        JsonElement docElement, string propertyName)
    {
        if (!docElement.TryGetProperty(propertyName, out JsonElement sourceElement))
            return;

        if (sourceElement.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in sourceElement.EnumerateObject())
        {
            if (property.Value.TryGetInt32(out int value))
            {
                target.Add(property.Name, value);
            }
        }
    }

    /// <summary>
    /// Helper method to safely extract an int value from a JsonElement property.
    /// </summary>
    private int GetIntValue(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement valueElement) &&
            valueElement.TryGetInt32(out int value))
        {
            return value;
        }
        return 0;
    }
}
