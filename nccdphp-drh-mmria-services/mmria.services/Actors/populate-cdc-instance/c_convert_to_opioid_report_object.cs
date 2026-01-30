using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.server.utils;

public sealed partial class c_convert_to_opioid_report_object
{
    
    //static HashSet<string> BlankIdSet = new HashSet<string>();
    Dictionary<string, mmria.server.model.opioid_report_value_struct> indicators;

    string source_json;

    common.couchdb.DBConfigurationDetail connection;
    string metadata_release_version_name;
    string report_type = "overdose";

    private System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> List_Look_Up;

    private int blank_value = 9999;


    private enum deaths_by_age_enum
    {
        blank,
        age_less_than_20,
        age_20_to_24,
        age_25_to_29,
        age_30_to_34,
        age_35_to_39,
        age_40_to_44,
        age_45_and_above
    }

    private enum pregnant_at_time_of_death_enum
    {
        blank,
        pregnant_at_the_time_of_death,
        pregnant_within_42_days_of_death,
        pregnant_within_43_to_365_days_of_death
    }

    private enum ethnicity_enum
    {
        hispanic,
        non_hispanic_black,
        non_hispanic_white,
        american_indian_alaska_native,
        native_hawaiian,
        guamanian_or_chamorro,
        samoan,
        other_pacific_islander,
        asian_indian,
        filipino,
        korean,
        other_asian,
        chinese,
        japanese,
        vietnamese,
        other,
        not_specified
    }

    string temp = 

    @"
    'id': doc._id,
    'hr_date_of_death_year': doc.home_record.date_of_death.year,
    'dc_date_of_death': doc.death_certificate.certificate_identification.date_of_death,
    'date_of_review': doc.committee_review.date_of_review,
    'was_this_death_preventable': doc.committee_review.was_this_death_preventable,
    'pregnancy_relatedness': doc.committee_review.pregnancy_relatedness,
    'bc_is_of_hispanic_origin': doc.birth_fetal_death_certificate_parent.demographic_of_mother.is_of_hispanic_origin,
    'dc_is_of_hispanic_origin': doc.death_certificate.demographics.is_of_hispanic_origin,
    'age': doc.death_certificate.demographics.age,
    'pmss': doc.committee_review.pmss_mm,
    'pmss_mm_secondary': doc.committee_review.pmss_mm_secondary,
    'did_obesity_contribute_to_the_death': doc.committee_review.did_obesity_contribute_to_the_death,
    'did_mental_health_conditions_contribute_to_the_death': doc.committee_review.did_mental_health_conditions_contribute_to_the_death,
    'did_substance_use_disorder_contribute_to_the_death': doc.committee_review.did_substance_use_disorder_contribute_to_the_death,
    'was_this_death_a_sucide': doc.committee_review.was_this_death_a_sucide,
    'was_this_death_a_homicide': doc.committee_review.homicide_relatedness.was_this_death_a_homicide,
    'dc_race': doc.death_certificate.race.race,
    'bc_race': doc.birth_fetal_death_certificate_parent.race.race_of_mother

";

    static HashSet<string> aggregator_set = new HashSet<string>()
    {
        "_id",
        "home_record/date_of_death/year",
        "home_record/date_of_death/month",
        "home_record/date_of_death/day",
        "committee_review/date_of_review",
        "committee_review/was_this_death_preventable",
        "committee_review/pregnancy_relatedness",
        "birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin",
        "death_certificate/demographics/is_of_hispanic_origin",
        "death_certificate/demographics/age",
        "committee_review/pmss_mm",
        "committee_review/pmss_mm_secondary",
        "committee_review/did_obesity_contribute_to_the_death",
        "committee_review/did_mental_health_conditions_contribute_to_the_death",
        "committee_review/did_substance_use_disorder_contribute_to_the_death",
        "committee_review/was_this_death_a_sucide",
        "committee_review/homicide_relatedness/was_this_death_a_homicide",
        "death_certificate/race/race",
        "birth_fetal_death_certificate_parent/race/race_of_mother"
    };

    public c_convert_to_opioid_report_object (string p_source_json, common.couchdb.DBConfigurationDetail p_connection, string p_metadata_release_version_name, string p_type = "overdose")
    {

        source_json = p_source_json;
        connection = p_connection;
        metadata_release_version_name = p_metadata_release_version_name;
        this.report_type = p_type;
    }



    private Dictionary<string, mmria.server.model.opioid_report_value_struct> get_zero_indicators(mmria.server.model.opioid_report_value_struct p_header)
    {

        mmria.server.model.opioid_report_value_struct get_new_struct(string p_indicator_field_id)
        {
            var result = new mmria.server.model.opioid_report_value_struct();
            var keys = p_indicator_field_id.Split(" ");

            if(keys.Length > 1)
            {
                result.indicator_id = keys[0];
                result.field_id = keys[1];
            }
            result.value = 0;

            result.year_of_death = p_header.year_of_death;
            result.month_of_death = p_header.month_of_death;
            result.day_of_death = p_header.day_of_death;

            result.case_review_year = p_header.case_review_year;
            result.case_review_month = p_header.case_review_month;
            result.case_review_day = p_header.case_review_day;

            result.pregnancy_related = p_header.pregnancy_related;
            result.host_state = p_header.host_state;

            result.jurisdiction_id = p_header.jurisdiction_id;

            return result;
        }

        var result = new Dictionary<string, mmria.server.model.opioid_report_value_struct>(StringComparer.OrdinalIgnoreCase); 

        result.Add("mPregRelated MPregRel1", get_new_struct("mPregRelated MPregRel1"));
        result.Add("mPregRelated MPregRel2", get_new_struct("mPregRelated MPregRel2"));
        result.Add("mPregRelated MPregRel3", get_new_struct("mPregRelated MPregRel3"));
        result.Add("mPregRelated MPregRel4", get_new_struct("mPregRelated MPregRel4"));
        result.Add("mPregRelated MPregRel5", get_new_struct("mPregRelated MPregRel5"));
        result.Add("mDeathsbyRaceEth MRaceEth3", get_new_struct("mDeathsbyRaceEth MRaceEth3"));
        result.Add("mDeathsbyRaceEth MRaceEth4", get_new_struct("mDeathsbyRaceEth MRaceEth4"));
        result.Add("mDeathsbyRaceEth MRaceEth5", get_new_struct("mDeathsbyRaceEth MRaceEth5"));
        result.Add("mDeathsbyRaceEth MRaceEth6", get_new_struct("mDeathsbyRaceEth MRaceEth6"));
        result.Add("mDeathsbyRaceEth MRaceEth7", get_new_struct("mDeathsbyRaceEth MRaceEth7"));
        result.Add("mDeathsbyRaceEth MRaceEth8", get_new_struct("mDeathsbyRaceEth MRaceEth8"));
        result.Add("mDeathsbyRaceEth MRaceEth9", get_new_struct("mDeathsbyRaceEth MRaceEth9"));
        result.Add("mDeathsbyRaceEth MRaceEth10", get_new_struct("mDeathsbyRaceEth MRaceEth10"));
        result.Add("mDeathsbyRaceEth MRaceEth11", get_new_struct("mDeathsbyRaceEth MRaceEth11"));
        result.Add("mDeathsbyRaceEth MRaceEth12", get_new_struct("mDeathsbyRaceEth MRaceEth12"));
        result.Add("mDeathsbyRaceEth MRaceEth13", get_new_struct("mDeathsbyRaceEth MRaceEth13"));
        result.Add("mDeathsbyRaceEth MRaceEth14", get_new_struct("mDeathsbyRaceEth MRaceEth14"));
        result.Add("mDeathsbyRaceEth MRaceEth15", get_new_struct("mDeathsbyRaceEth MRaceEth15"));
        result.Add("mDeathsbyRaceEth MRaceEth16", get_new_struct("mDeathsbyRaceEth MRaceEth16"));
        result.Add("mDeathsbyRaceEth MRaceEth17", get_new_struct("mDeathsbyRaceEth MRaceEth17"));
        result.Add("mDeathsbyRaceEth MRaceEth18", get_new_struct("mDeathsbyRaceEth MRaceEth18"));
        result.Add("mDeathsbyRaceEth MRaceEth1", get_new_struct("mDeathsbyRaceEth MRaceEth1"));
        result.Add("mDeathsbyRaceEth MRaceEth2", get_new_struct("mDeathsbyRaceEth MRaceEth2"));
        result.Add("mDeathsbyRaceEth MRaceEth20", get_new_struct("mDeathsbyRaceEth MRaceEth20"));
        result.Add("mDeathsbyRaceEth MRaceEth19", get_new_struct("mDeathsbyRaceEth MRaceEth19"));
        result.Add("mTimingofDeath MTimeD1", get_new_struct("mTimingofDeath MTimeD1"));
        result.Add("mTimingofDeath MTimeD2", get_new_struct("mTimingofDeath MTimeD2"));
        result.Add("mTimingofDeath MTimeD3", get_new_struct("mTimingofDeath MTimeD3"));
        result.Add("mTimingofDeath MTimeD4", get_new_struct("mTimingofDeath MTimeD4"));
        result.Add("mAgeatDeath MAgeD1", get_new_struct("mAgeatDeath MAgeD1"));
        result.Add("mAgeatDeath MAgeD2", get_new_struct("mAgeatDeath MAgeD2"));
        result.Add("mAgeatDeath MAgeD3", get_new_struct("mAgeatDeath MAgeD3"));
        result.Add("mAgeatDeath MAgeD4", get_new_struct("mAgeatDeath MAgeD4"));
        result.Add("mAgeatDeath MAgeD5", get_new_struct("mAgeatDeath MAgeD5"));
        result.Add("mAgeatDeath MAgeD6", get_new_struct("mAgeatDeath MAgeD6"));
        result.Add("mAgeatDeath MAgeD7", get_new_struct("mAgeatDeath MAgeD7"));
        //result.Add("mAgeatDeath MAgeD8", get_new_struct("mAgeatDeath MAgeD8"));
        result.Add("mDeathCause MCauseD1", get_new_struct("mDeathCause MCauseD1"));
        result.Add("mDeathCause MCauseD2", get_new_struct("mDeathCause MCauseD2"));
        result.Add("mDeathCause MCauseD3", get_new_struct("mDeathCause MCauseD3"));
        result.Add("mDeathCause MCauseD4", get_new_struct("mDeathCause MCauseD4"));
        result.Add("mDeathCause MCauseD5", get_new_struct("mDeathCause MCauseD5"));
        result.Add("mDeathCause MCauseD6", get_new_struct("mDeathCause MCauseD6"));
        result.Add("mDeathCause MCauseD7", get_new_struct("mDeathCause MCauseD7"));
        result.Add("mDeathCause MCauseD8", get_new_struct("mDeathCause MCauseD8"));
        result.Add("mDeathCause MCauseD9", get_new_struct("mDeathCause MCauseD9"));
        result.Add("mDeathCause MCauseD10", get_new_struct("mDeathCause MCauseD10"));
        result.Add("mDeathCause MCauseD11", get_new_struct("mDeathCause MCauseD11"));
        result.Add("mDeathCause MCauseD12", get_new_struct("mDeathCause MCauseD12"));
        result.Add("mDeathCause MCauseD13", get_new_struct("mDeathCause MCauseD13"));
        result.Add("mDeathCause MCauseD14", get_new_struct("mDeathCause MCauseD14"));
        result.Add("mDeathCause MCauseD15", get_new_struct("mDeathCause MCauseD15"));
        result.Add("mSubstAutop MSubAuto1", get_new_struct("mSubstAutop MSubAuto1"));
        result.Add("mSubstAutop MSubAuto2", get_new_struct("mSubstAutop MSubAuto2"));
        result.Add("mSubstAutop MSubAuto3", get_new_struct("mSubstAutop MSubAuto3"));
        result.Add("mSubstAutop MSubAuto4", get_new_struct("mSubstAutop MSubAuto4"));
        result.Add("mSubstAutop MSubAuto5", get_new_struct("mSubstAutop MSubAuto5"));
        result.Add("mSubstAutop MSubAuto6", get_new_struct("mSubstAutop MSubAuto6"));
        result.Add("mSubstAutop MSubAuto7", get_new_struct("mSubstAutop MSubAuto7"));
        result.Add("mSubstAutop MSubAuto8", get_new_struct("mSubstAutop MSubAuto8"));
        result.Add("mSubstAutop MSubAuto9", get_new_struct("mSubstAutop MSubAuto9"));
        result.Add("mSubstAutop MSubAuto10", get_new_struct("mSubstAutop MSubAuto10"));
        result.Add("mDeathSubAbuseEvi MEviSub1", get_new_struct("mDeathSubAbuseEvi MEviSub1"));
        result.Add("mDeathSubAbuseEvi MEviSub2", get_new_struct("mDeathSubAbuseEvi MEviSub2"));
        result.Add("mDeathSubAbuseEvi MEviSub3", get_new_struct("mDeathSubAbuseEvi MEviSub3"));
        result.Add("mDeathSubAbuseEvi MEviSub4", get_new_struct("mDeathSubAbuseEvi MEviSub4"));
        result.Add("mHxofSubAbu MHxSub1", get_new_struct("mHxofSubAbu MHxSub1"));
        result.Add("mHxofSubAbu MHxSub2", get_new_struct("mHxofSubAbu MHxSub2"));
        result.Add("mHxofSubAbu MHxSub3", get_new_struct("mHxofSubAbu MHxSub3"));
        result.Add("mHxofSubAbu MHxSub4", get_new_struct("mHxofSubAbu MHxSub4"));
        result.Add("mLivingArrange MLivD1", get_new_struct("mLivingArrange MLivD1"));
        result.Add("mLivingArrange MLivD2", get_new_struct("mLivingArrange MLivD2"));
        result.Add("mLivingArrange MLivD3", get_new_struct("mLivingArrange MLivD3"));
        result.Add("mLivingArrange MLivD4", get_new_struct("mLivingArrange MLivD4"));
        result.Add("mLivingArrange MLivD5", get_new_struct("mLivingArrange MLivD5"));
        result.Add("mLivingArrange MLivD6", get_new_struct("mLivingArrange MLivD6"));
        result.Add("mLivingArrange MLivD7", get_new_struct("mLivingArrange MLivD7"));
        result.Add("mHomeless MHomeless1", get_new_struct("mHomeless MHomeless1"));
        result.Add("mHomeless MHomeless2", get_new_struct("mHomeless MHomeless2"));
        result.Add("mHomeless MHomeless3", get_new_struct("mHomeless MHomeless3"));
        result.Add("mHomeless MHomeless4", get_new_struct("mHomeless MHomeless4"));
        result.Add("mHomeless MHomeless5", get_new_struct("mHomeless MHomeless5"));
        result.Add("mHomeless MHomeless6", get_new_struct("mHomeless MHomeless6"));
        result.Add("mHomeless MHomeless7", get_new_struct("mHomeless MHomeless7"));
        result.Add("mHomeless MHomeless8", get_new_struct("mHomeless MHomeless8"));
        result.Add("mHomeless MHomeless9", get_new_struct("mHomeless MHomeless9"));
        result.Add("mIncarHx MHxIncar1", get_new_struct("mIncarHx MHxIncar1"));
        result.Add("mIncarHx MHxIncar2", get_new_struct("mIncarHx MHxIncar2"));
        result.Add("mIncarHx MHxIncar3", get_new_struct("mIncarHx MHxIncar3"));
        result.Add("mIncarHx MHxIncar4", get_new_struct("mIncarHx MHxIncar4"));
        result.Add("mIncarHx MHxIncar5", get_new_struct("mIncarHx MHxIncar5"));
        result.Add("mIncarHx MHxIncar6", get_new_struct("mIncarHx MHxIncar6"));
        result.Add("mIncarHx MHxIncar7", get_new_struct("mIncarHx MHxIncar7"));
        result.Add("mIncarHx MHxIncar8", get_new_struct("mIncarHx MHxIncar8"));
        result.Add("mIncarHx MHxIncar9", get_new_struct("mIncarHx MHxIncar9"));
        result.Add("mIncarHx MHxIncar10", get_new_struct("mIncarHx MHxIncar10"));
        
        result.Add("mHxofEmoStress MEmoStress1", get_new_struct("mHxofEmoStress MEmoStress1"));
        result.Add("mHxofEmoStress MEmoStress2", get_new_struct("mHxofEmoStress MEmoStress2"));
        result.Add("mHxofEmoStress MEmoStress3", get_new_struct("mHxofEmoStress MEmoStress3"));
        result.Add("mHxofEmoStress MEmoStress4", get_new_struct("mHxofEmoStress MEmoStress4"));
        result.Add("mHxofEmoStress MEmoStress5", get_new_struct("mHxofEmoStress MEmoStress5"));
        result.Add("mHxofEmoStress MEmoStress6", get_new_struct("mHxofEmoStress MEmoStress6"));
        result.Add("mHxofEmoStress MEmoStress7", get_new_struct("mHxofEmoStress MEmoStress7"));
        result.Add("mHxofEmoStress MEmoStress8", get_new_struct("mHxofEmoStress MEmoStress8"));
        result.Add("mHxofEmoStress MEmoStress9", get_new_struct("mHxofEmoStress MEmoStress9"));
        result.Add("mHxofEmoStress MEmoStress10", get_new_struct("mHxofEmoStress MEmoStress10"));
        result.Add("mHxofEmoStress MEmoStress11", get_new_struct("mHxofEmoStress MEmoStress11"));
        result.Add("mHxofEmoStress MEmoStress12", get_new_struct("mHxofEmoStress MEmoStress12"));
        result.Add("mHxofEmoStress MEmoStress13", get_new_struct("mHxofEmoStress MEmoStress13"));
        result.Add("mHxofEmoStress MEmoStress14", get_new_struct("mHxofEmoStress MEmoStress14"));
        result.Add("mMHTxTiming MMHTx1", get_new_struct("mMHTxTiming MMHTx1"));
        result.Add("mMHTxTiming MMHTx2", get_new_struct("mMHTxTiming MMHTx2"));
        result.Add("mMHTxTiming MMHTx3", get_new_struct("mMHTxTiming MMHTx3"));
        result.Add("mMHTxTiming MMHTx4", get_new_struct("mMHTxTiming MMHTx4"));

        result.Add("mEducation MEduc1", get_new_struct("mEducation MEduc1"));
        result.Add("mEducation MEduc2", get_new_struct("mEducation MEduc2"));
        result.Add("mEducation MEduc3", get_new_struct("mEducation MEduc3"));
        result.Add("mEducation MEduc4", get_new_struct("mEducation MEduc4"));
        result.Add("mEducation MEduc5", get_new_struct("mEducation MEduc5"));

        
/*
mUndCofDeath MUndCofDeath21 21
mDeathCause  MCauseD30 30
mDeathPrevent MDeathPrevent3 3
mOMBRaceRcd  MOMBRaceRcd10 10
mDeathbyRace  MDeathbyRace17 17

*/

        for(var i = 0; i < 22; i++)
        result.Add($"mUndCofDeath MUndCofDeath{i+1}", get_new_struct($"mUndCofDeath MUndCofDeath{i+1}"));

        for(var i = 15; i < 30; i++)
        result.Add($"mDeathCause MCauseD{i+1}", get_new_struct($"mDeathCause MCauseD{i+1}"));

        for(var i = 0; i < 4; i++)
        result.Add($"mDeathPrevent MDeathPrevent{i+1}", get_new_struct($"mDeathPrevent MDeathPrevent{i+1}"));

        for(var i = 0; i < 10; i++)
        result.Add($"mOMBRaceRcd MOMBRaceRcd{i+1}", get_new_struct($"mOMBRaceRcd MOMBRaceRcd{i+1}"));

        for(var i = 0; i < 17; i++)
        result.Add($"mDeathbyRace MDeathbyRace{i+1}", get_new_struct($"mDeathbyRace MDeathbyRace{i+1}"));



        return result;

    }

    public string execute ()
    {
        string result = null;
        
        string metadata_url = connection.url + $"/metadata/version_specification-{metadata_release_version_name}/metadata";
        var metadata_curl = new mmria.getset.cURL("GET", null, metadata_url, null, connection.user_name, connection.user_value);
        mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_curl.execute());


        List_Look_Up = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach(var child in metadata.children)
        {
            Get_List_Look_Up(List_Look_Up, metadata.lookup, child, "/" + child.name);
        }



        mmria.server.model.c_opioid_report_object report_object;

        System.Dynamic.ExpandoObject source_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (source_json);
        int means_of_fatal_injury = 9999;

        if(report_type == "overdose")
        {
            try
            {
                var filter_check_string = get_value(source_object, "committee_review/means_of_fatal_injury");
                int int_check = 0;
                if
                (
                    filter_check_string == null ||
                    string.IsNullOrWhiteSpace(filter_check_string.ToString())
                )
                {
                    return result;
                }
                
                int Overdose_Poisioning = 3;
                if(int.TryParse(filter_check_string.ToString(), out int_check))
                {
                    if(int_check != Overdose_Poisioning)
                    {
                        return result;
                    }
                }
                else
                {
                    return result;
                }

            }
            catch(Exception)
            {
                //System.Console.WriteLine (ex);
            }
        }
        else
        {
            try
            {
                var filter_check_string = get_value(source_object, "committee_review/means_of_fatal_injury");
                int int_check = 0;
                if
                (
                    filter_check_string == null ||
                    string.IsNullOrWhiteSpace(filter_check_string.ToString())
                )
                {
                    means_of_fatal_injury = 9999;
                }
                else if(int.TryParse(filter_check_string.ToString(), out int_check))
                {
                    means_of_fatal_injury = int_check;
                }
                else
                {
                    means_of_fatal_injury = 9999;
                }

            }
            catch(Exception)
            {
                //System.Console.WriteLine (ex);
            }
        }

        //dynamic source_object = Newtonsoft.Json.Linq.JObject.Parse(source_json);


        

        report_object = new mmria.server.model.c_opioid_report_object (this.report_type);
        report_object._id = get_value (source_object, "_id")?.ToString();
        report_object.means_of_fatal_injury = means_of_fatal_injury;

        var opioid_report_value_header = new mmria.server.model.opioid_report_value_struct();

        /*
        if (report_object._id == "02279162-6be3-49e4-930f-42eed7cd4706")
        {
            System.Console.Write("break");
        }*/

        object val = null;

        try
        {
            val = get_value(source_object, "home_record/date_of_death/year");
            if(val != null && val.ToString() != "")
            {
                report_object.year_of_death = System.Convert.ToInt32(val);
                opioid_report_value_header.year_of_death = report_object.year_of_death;
            }
        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }

        try
        {
            val = get_value(source_object, "home_record/date_of_death/month");
            if(val != null && val.ToString() != "")
            {
                opioid_report_value_header.month_of_death = System.Convert.ToInt32(val);

            }
        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }


        try
        {
            val = get_value(source_object, "home_record/date_of_death/day");
            if(val != null && val.ToString() != "")
            {
                opioid_report_value_header.day_of_death = System.Convert.ToInt32(val);

            }
        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }


        try
        {
            val = get_value(source_object, "committee_review/date_of_review");
            if(val != null && val.ToString() != "")
            {
                report_object.year_of_case_review = System.Convert.ToDateTime(val).Year;
                report_object.month_of_case_review = System.Convert.ToDateTime(val).Month;
                report_object.day_of_case_review = System.Convert.ToDateTime(val).Day;

                opioid_report_value_header.case_review_year = report_object.year_of_case_review;
                opioid_report_value_header.case_review_month = report_object.month_of_case_review;
                opioid_report_value_header.case_review_day = report_object.day_of_case_review;
                
                
            }
        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }


        try
        {
            val = get_value(source_object, "host_state");
            if(val != null && val.ToString() != "")
            {
                opioid_report_value_header.host_state = val.ToString();
            }
        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }


        try
        {
            val = get_value(source_object, "home_record/jurisdiction_id");
            if(val != null && val.ToString() != "")
            {
                report_object.jurisdiction_id = val.ToString();
                opioid_report_value_header.jurisdiction_id = report_object.jurisdiction_id;
            }
        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }



        

        mmria.server.model.opioid_report_value_struct work_item = initialize_opioid_report_value_struct(opioid_report_value_header);
        

        this.popluate_total_number_of_cases_by_pregnancy_relatedness (ref work_item, ref report_object, source_object);
        
        opioid_report_value_header.pregnancy_related = work_item.pregnancy_related;

        this.indicators = get_zero_indicators(opioid_report_value_header);
        this.indicators[$"{work_item.indicator_id} {work_item.field_id}"] = work_item;


        work_item = initialize_opioid_report_value_struct(opioid_report_value_header);
        
        this.popluate_total_number_of_pregnancy_related_deaths_by_ethnicity(ref work_item, source_object, opioid_report_value_header.pregnancy_related == 1);
        this.indicators[$"{work_item.indicator_id} {work_item.field_id}"] = work_item;


        //this.popluate_total_number_of_pregnancy_associated_deaths_by_ethnicity (ref report_object, source_object);
        
        work_item = initialize_opioid_report_value_struct(opioid_report_value_header);

        var idset = new HashSet<string>(StringComparer.OrdinalIgnoreCase){"3e155616-1c2f-4b70-848f-276471d907ac"};

        if(idset.Contains(report_object._id))
        {
            Console.WriteLine("here");
        }
/**/

        this.popluate_pregnancy_deaths_by_age(ref work_item, ref report_object, source_object);
        this.indicators[$"{work_item.indicator_id} {work_item.field_id}"] = work_item;

        this.popluate_death_cause(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);
        this.popluate_mDeathSubAbuseEvi(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);

        this.popluate_mEducation(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);
        this.popluate_mHomeless(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);
        this.popluate_mHxofEmoStress(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);
        this.popluate_mHxofSubAbu(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);
        this.popluate_mIncarHx(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);
        this.popluate_mLivingArrange(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);
        this.popluate_mMHTxTiming(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);

        this.popluate_mTimingofDeath(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);
    
        this.popluate_mSubstAutop(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);

        this.popluate_mUndCofDeath(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);
        this.popluate_mDeathPrevent(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);	
        this.popluate_mOMBRaceRcd(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);
        this.popluate_mDeathbyRace(ref report_object.data, ref opioid_report_value_header, ref report_object, source_object);

        foreach(var indicator_item in this.indicators)
        {
            report_object.data.Add(indicator_item.Value);
        }


        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        //settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        result = Newtonsoft.Json.JsonConvert.SerializeObject(report_object, settings);

        return result;
    }

    public mmria.server.model.opioid_report_value_struct initialize_opioid_report_value_struct(mmria.server.model.opioid_report_value_struct p_header)
    {
        var result = new mmria.server.model.opioid_report_value_struct();

        result.year_of_death = p_header.year_of_death;
        result.month_of_death = p_header.month_of_death;
        result.day_of_death = p_header.day_of_death;

        result.case_review_year = p_header.case_review_year;
        result.case_review_month = p_header.case_review_month;
        result.case_review_day = p_header.case_review_day;

        result.pregnancy_related = p_header.pregnancy_related;
        result.host_state = p_header.host_state;

        result.jurisdiction_id = p_header.jurisdiction_id;

        return result;
    }
    public object get_value(System.Dynamic.ExpandoObject p_object, string p_path, string p_data_type = "string")
    {
        object result = null;

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            object index = p_object;

            /*
            if (path[1] == "abnormal_conditions_of_newborn")
            {
                System.Console.WriteLine("break");
            }*/


            for (int i = 0; i < path.Length; i++)
            {
                
                if(i == 0)
                {

                    if (i == path.Length - 1 && index is IDictionary<string, object>)
                    {
                        
                        IDictionary<string, object> dictionary_object = index as IDictionary<string, object>;

                        if(dictionary_object == null)
                        {
                            result = null;
                            return result;
                        }

                        object val = null;

                        if(dictionary_object.ContainsKey(path[i]))
                        {
                            val = dictionary_object[path[i]]; 
                        }

                        if(val != null)
                        {
                            if(val.GetType() == typeof(System.DateTime))
                            {
                                System.DateTime? temp_date_time = val as System.DateTime?;
                                result = temp_date_time.Value;	
                            }
                            else if(val.GetType() == typeof(string))
                            {
                                result = val.ToString();	
                            }

                            else if(val.GetType() == typeof(System.Collections.Generic.List<string>))
                            {
                                
                                result = val as System.Collections.Generic.List<string>;	
                            }
                            else if(val.GetType() == typeof(System.Collections.Generic.List<object>))
                            {

                                result = val as System.Collections.Generic.List<object>;	
                            }
                            else
                            {
                                result = val;	
                            }
                        }
                        else
                        {
                            result = null;	
                        }
                    }
                    else
                    {
                        index = ((IDictionary<string, object>)p_object)[path[i]];
                    }
                }
                else if (i == path.Length - 1)
                {
                    if (index is IDictionary<string, object>)
                    {

                        IDictionary<string, object> dictionary_object = index as IDictionary<string, object>;

                        if(dictionary_object == null)
                        {
                            result = null;
                            return result;
                        }

                        object val = null;

                        if(dictionary_object.ContainsKey(path[i]))
                        {
                            val = dictionary_object[path[i]]; 
                        }


                        if(val != null)
                        {
                            if(val.GetType() == typeof(System.DateTime))
                            {
                                System.DateTime? temp_date_time = val as System.DateTime?;
                                result = temp_date_time.Value;	
                            }
                            else if(val.GetType() == typeof(string))
                            {
                                result = val.ToString();	
                            }

                            else if(val.GetType() == typeof(System.Collections.Generic.List<string>))
                            {

                                result = val as System.Collections.Generic.List<string>;	
                            }
                            else if(val.GetType() == typeof(System.Collections.Generic.List<object>))
                            {

                                result = val as System.Collections.Generic.List<object>;	
                            }
                            else
                            {
                                result = val;	
                            }

                        }
                        else
                        {
                            result = null;	
                        }
                    }
                    else
                    {
                        //System.Console.WriteLine("break");
                    }
                }
                else if(index is IDictionary<string, object>)
                {
                    if(index != null && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        index = ((IDictionary<string, object>)index)[path[i]];
                    }
                
                }
                else if (index != null)
                {
                    System.Console.WriteLine(index.GetType());
                    /*
                    else if (index != null && index[path[i]].GetType() == typeof(IList<object>))
                    {
                        index = index[path[i]] as IList<object>;
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>) && !((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        //System.Console.WriteLine("Index not found. This should not happen. {0}", p_path);
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>))
                    {
                        index = index[path[i]] as IDictionary<string, object>;
                    }*/
                }
                else
                {
                    //System.Console.WriteLine("This should not happen. {0}", p_path);
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }

        return result;

    }

    public List<(int, object)> get_grid_value(System.Dynamic.ExpandoObject p_object, string p_path)
    {
        List<(int, object)> result = new List<(int, object)>();

        //dynamic current = null;

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            object index = null;

            for (int i = 0; i < path.Length; i++)
            {
                
                if(i == 0)
                {
                    IDictionary<string, object> index_dictionary = p_object as IDictionary<string, object>;
                    if
                    (
                        index_dictionary != null &&
                        index_dictionary.ContainsKey(path[0])
                    )
                    {
                        index = index_dictionary[path[0]];
                    }
                    else
                    {
                        return result;
                    }

                }
                else if (i == path.Length - 1)
                {
                    if (index is IList<object>)
                    {
                        var grid_list = index as IList<object>;
                        if(grid_list != null)
                        {
                            for(int grid_index = 0; grid_index < grid_list.Count; grid_index++)
                            {
                                var grid_row = grid_list[grid_index];

                                if(grid_row is IDictionary<string, object>)
                                {
                                    var grid_row_dictionary = grid_row as IDictionary<string, object>;
                                    if(grid_row_dictionary.ContainsKey(path[i]))
                                    {
                                        result.Add((grid_index, grid_row_dictionary[path[i]]));
                                    }
                                }
                            }
                        }		
                    }
                    else
                    {
                        System.Console.WriteLine("This should not happen. {0}", p_path);
                    }

                }
                else if(index is IDictionary<string, object>)
                {
                    if(index != null && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        index = ((IDictionary<string, object>)index)[path[i]];
                    }
                
                }
                else if (index != null)
                {
                    System.Console.WriteLine(index.GetType());
                    /*
                    else if (index != null && index[path[i]].GetType() == typeof(IList<object>))
                    {
                        index = index[path[i]] as IList<object>;
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>) && !((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        //System.Console.WriteLine("Index not found. This should not happen. {0}", p_path);
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>))
                    {
                        index = index[path[i]] as IDictionary<string, object>;
                    }*/
                }
                else
                {
                    //System.Console.WriteLine("This should not happen. {0}", p_path);
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }

        return result;

    }



    private string get_race_ethnicity (System.Dynamic.ExpandoObject p_source_object)
    {
        //string result = "9999";

        string val = null;
        object val_object = null;
        string race_name = "blank";


//Hispanic
        
        HashSet<string> bc_hispanic_origin = new HashSet<string> (StringComparer.InvariantCultureIgnoreCase);
        HashSet<string> dc_hispanic_origin = new HashSet<string> (StringComparer.InvariantCultureIgnoreCase);

//birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin 
        // Yes, Mexican, Mexican American, Chicano 
        // Yes, Puerto Rican 
        // Yes, Cuban 
        // Yes, Other Spanish/Hispanic/Latino 
        //Yes, Origin Unknown

/*
9999 (blank)
0 No, Not Spanish/Hispanic/Latino
1 Yes, Mexican, Mexican American, Chicano
2 Yes, Puerto Rican
3 Yes, Cuban
4 Yes, Other Spanish/Hispanic/Latino
5 Yes, Origin Unknown
8888 Not Specified


        bc_hispanic_origin.Add ("Yes, Mexican, Mexican American, Chicano");
        bc_hispanic_origin.Add ("Yes, Puerto Rican");
        bc_hispanic_origin.Add ("Yes, Cuban");
        bc_hispanic_origin.Add ("Yes, Other Spanish/Hispanic/Latino");
        bc_hispanic_origin.Add ("Yes, Origin Unknown");
*/
        bc_hispanic_origin.Add ("1");
        bc_hispanic_origin.Add ("2");
        bc_hispanic_origin.Add ("3");
        bc_hispanic_origin.Add ("4");
        bc_hispanic_origin.Add ("5");



//IF NO BC present:
//death_certificate/demographics/is_of_hispanic_origin
        //Yes, Mexican, Mexican American, Chicano
        //Yes, Puerto Rican 
        //Yes, Cuban
        //Yes, Other Spanish/Hispanic/Latino 
        //Yes, Origin Unknown

        dc_hispanic_origin.Add ("1");
        dc_hispanic_origin.Add ("2");
        dc_hispanic_origin.Add ("3");
        dc_hispanic_origin.Add ("4");
        dc_hispanic_origin.Add ("5");

        bool is_hispanic = false;
        bool is_hispanic_blank = true;
        

        var val_dynamic = get_value (p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin");
        if(val_dynamic != null)
        {
            val = val_dynamic.ToString();
        }

        if (val != null)
        {
            if
            (
                string.IsNullOrWhiteSpace(val.ToString()) ||
                val.ToString() == "9999" ||
                val.ToString() == "7777" ||
                val.ToString() == "8888"

            )
            {
                is_hispanic_blank = true;
            }
            else if (bc_hispanic_origin.Contains (val))
            {
                //result.Add (ethnicity_enum.hispanic);
                is_hispanic = true;
                is_hispanic_blank = false;
            }
            else if 
            (
                //"No, not Spanish/ Hispanic/ Latino".Equals(val.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                //"No, not Spanish/Hispanic/Latino".Equals(val.ToString(), StringComparison.InvariantCultureIgnoreCase)
                "0".Equals(val.ToString(), StringComparison.InvariantCultureIgnoreCase) 
            )
            {
                is_hispanic = false;
                is_hispanic_blank = false;
            }

            if(!is_hispanic_blank)
            {
                val_object = get_value (p_source_object, "birth_fetal_death_certificate_parent/race/race_of_mother");
                if (val_object != null)
                {
                    
                    HashSet<string> ethnicity_set = new HashSet<string> (StringComparer.InvariantCultureIgnoreCase);
                    var val_list = val_object as IList<object>;

                    if(val_list != null)
                    {
                        if(val_list.Count == 1)
                        {
                            if(val_list[0]!= null)
                            switch(val_list[0].ToString().ToLower())
                            {

                                case "0": //white":
                                    race_name ="white";
                                    break;
                                case "1"://"black":
                                    race_name = "black";
                                    break;
                                case "9999":
                                case "8888":
                                case "7777":
                                case "":
                                    race_name = "blank";
                                    break;
                                default:
                                    race_name = "other";
                                    break;
                            }
                        }
                        else if(val_list.Count > 1)
                        {
                            race_name = "other";
                            foreach(object item in val_list)
                            {

                                if(item!= null)
                                {
                                    string item_value = item.ToString().ToLower();
                                    if
                                    (
                                        item_value == "9999" ||
                                        item_value == "8888" ||
                                        item_value == "7777" ||
                                        item_value == ""
                                    )
                                    {
                                            race_name = "blank";
                                            break;
                                    }
                                    else
                                    {
                                        race_name = "other";
                                    }
                                }
                                
                            }
                        }
                        

                    }
                }
            }				

        }

        if(is_hispanic_blank || race_name == "blank")
        {
            val = get_value (p_source_object, "death_certificate/demographics/is_of_hispanic_origin")?.ToString();
            if
            (
                string.IsNullOrWhiteSpace(val.ToString()) ||
                val.ToString() == "9999" ||
                val.ToString() == "7777" ||
                val.ToString() == "8888"

            )
            {
                is_hispanic_blank = true;
            }
            else if (dc_hispanic_origin.Contains (val))
            {
                //result.Add (ethnicity_enum.hispanic);
                is_hispanic = true;
                is_hispanic_blank = false;
            }
            else if 
            (
                //"No, not Spanish/ Hispanic/ Latino".Equals(val.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                //"No, not Spanish/Hispanic/Latino".Equals(val.ToString(), StringComparison.InvariantCultureIgnoreCase)
                "0".Equals(val.ToString(), StringComparison.InvariantCultureIgnoreCase) 
            )
            {
                is_hispanic = false;
                is_hispanic_blank = false;
            }


            if(!is_hispanic_blank)
            {
                race_name = "blank";
                
                val_object = get_value (p_source_object, "death_certificate/race/race");

                
                if (val_object != null)
                {
                    
                    HashSet<string> ethnicity_set = new HashSet<string> (StringComparer.InvariantCultureIgnoreCase);
                    var val_list = val_object as IList<object>;

                    if(val_list != null)
                    {
                        if(val_list.Count == 1)
                        {
                            if(val_list[0]!= null)
                            switch(val_list[0].ToString().ToLower())
                            {


                                case "0": //white":
                                    race_name ="white";
                                    break;
                                case "1"://"black":
                                    race_name = "black";
                                    break;
                                case "9999":
                                case "8888":
                                case "7777":
                                case "":
                                    race_name = "blank";
                                    break;
                                default:
                                    race_name = "other";
                                    break;
                            }
                        }
                        else if(val_list.Count > 1)
                        {
                            race_name = "other";
                            foreach(object item in val_list)
                            {

                                if(item!= null)
                                {
                                    string item_value = item.ToString().ToLower();
                                    if
                                    (
                                        item_value == "9999" ||
                                        item_value == "8888" ||
                                        item_value == "7777" ||
                                        item_value == ""
                                    )
                                    {
                                            race_name = "blank";
                                            break;
                                    }
                                    else
                                    {
                                        race_name = "other";
                                    }
                                }
                                
                            }
                        }
                        

                    }
                }
            }
        }


        if(is_hispanic_blank)
        {
            return "9999";
        }
        else
        {
            if(is_hispanic)
            {
                if(race_name == "blank") 
                {
                    return "9999";
                }
                else
                {
                    return "hispanic";
                }
                
            }
            else
            {
                if(race_name == "blank")
                {
                    return "9999";
                }
                else
                {
                    if(race_name == "black" || race_name == "white")
                    {
                        return race_name;
                    }
                    else
                    {
                        return "other";
                    }						
                }
            }

        }

        //return result;

        
    }


    private bool is_non_hispanic (string p_ethnicity, System.Dynamic.ExpandoObject p_source_object)
    {
        bool result = false;

        HashSet<string> bc_ethinicity = new HashSet<string> (StringComparer.InvariantCultureIgnoreCase);
        HashSet<string> dc_ethinicity = new HashSet<string> (StringComparer.InvariantCultureIgnoreCase);

        bc_ethinicity.Add(p_ethnicity);
        dc_ethinicity.Add(p_ethnicity);

        object val = get_value (p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin");
        if (val != null)
        {
            if 
            (
                //"No, not Spanish/ Hispanic/ Latino".Equals(val.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                //"No, not Spanish/Hispanic/Latino".Equals(val.ToString(), StringComparison.InvariantCultureIgnoreCase)
                "0".Equals(val.ToString(), StringComparison.InvariantCultureIgnoreCase) 
            )
            {
                val = get_value (p_source_object, "birth_fetal_death_certificate_parent/race/race_of_mother");


                if (val != null)
                {
                    
                    HashSet<string> ethnicity_set = new HashSet<string> (StringComparer.InvariantCultureIgnoreCase);
                    var val_list = val as IList<object>;

                    if(val_list != null)
                    foreach(string item in val_list) ethnicity_set.Add(item);
                        
                    if (ethnicity_set.Intersect (bc_ethinicity).Count() > 0)
                    {
                        result = true;
                    }
                }
            }
            else 
            {
                val = get_value (p_source_object, "death_certificate/demographics/is_of_hispanic_origin");
                
                if
                (
                    val != null && 
                    (
                        //"No, not Spanish/ Hispanic/ Latino".Equals(val.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                        //"No, not Spanish/Hispanic/Latino".Equals(val.ToString(), StringComparison.InvariantCultureIgnoreCase) 
                        "0".Equals(val.ToString(), StringComparison.InvariantCultureIgnoreCase)
                    )
                )
                {
                    val = get_value (p_source_object, "death_certificate/race/race");
                    if (val != null)
                    {
                        HashSet<string> ethnicity_set = new HashSet<string> (StringComparer.InvariantCultureIgnoreCase);
                        var val_list = val as IList<object>;
                    
                        if(val_list != null)
                        foreach(string item in val_list) ethnicity_set.Add(item);
                        if (ethnicity_set.Intersect (bc_ethinicity).Count() > 0)
                        {
                            result = true;
                        }
                    }
                }
            }
        }

        return result;

    }

    private deaths_by_age_enum get_age_classifier (System.Dynamic.ExpandoObject p_source_object)
    {
            int? age_of_mother = null;

            int calculate_age(DateTime date_of_birth, DateTime date_of_death)  
            {  
                int age = 0;  
                age = date_of_death.Year - date_of_birth.Year;  
                if (date_of_death.DayOfYear < date_of_birth.DayOfYear)  
                {
                    age = age - 1;  
                }
            
                return age;  
            }  


            object val1 = get_value (p_source_object, "death_certificate/demographics/age");
            if
            (
                val1 != null && 
                !string.IsNullOrWhiteSpace(val1.ToString())
            )
            {
                int temp;

                if(int.TryParse(val1.ToString(), out temp))
                {
                    age_of_mother = temp;
                }
            }


            if(!age_of_mother.HasValue)
            {
                //CALCULATE MOTHERS AGE AT DEATH ON DC
                /*
                path=death_certificate/demographics/age
                event=onfocus
                */

                DateTime? Convert(object year, object month, object day)
                {
                    DateTime? result = null;

                    int start_year;
                    int start_month;
                    int start_day;

                    if
                    (
                        year!= null && !string.IsNullOrWhiteSpace(year.ToString()) &&
                        month!= null && !string.IsNullOrWhiteSpace(month.ToString()) &&
                        day!= null && !string.IsNullOrWhiteSpace(day.ToString()) &&
                        int.TryParse(year.ToString(), out start_year) &&
                        int.TryParse(month.ToString(), out start_month) &&
                        int.TryParse(day.ToString(), out start_day)
                    )
                    {

                        if
                        (
                            start_year != 9999 &&
                            start_month != 9999 &&
                            start_day != 9999
                        )
                        {
                            try
                            {
                                result = new DateTime(start_year, start_month, start_day);
                            }
                            catch(Exception)
                            {
                                
                            }
                        }
                        else if
                        (
                            start_year != 9999 &&
                            start_month != 9999 &&
                            start_day == 9999
                        )
                        {
                            try
                            {
                                result = new DateTime(start_year, start_month, 1);
                            }
                            catch(Exception)
                            {
                                
                            }
                        }
                        else if
                        (
                            start_year != 9999 &&
                            start_month == 9999 &&
                            start_day != 9999
                        )
                        {
                            try
                            {
                                result = new DateTime(start_year, 1, start_day);
                            }
                            catch(Exception)
                            {
                                
                            }
                        }
                    }


                    return result;
                }

                ///birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month
                ///home_record/date_of_death/month

                val1 = get_value (p_source_object, "death_certificate/demographics/date_of_birth/year");
                object val2 = get_value (p_source_object, "death_certificate/demographics/date_of_birth/month");
                object val3 = get_value (p_source_object, "death_certificate/demographics/date_of_birth/day");


                var date_of_birth = Convert(val1, val2, val3);

                val1 = get_value (p_source_object, "home_record/date_of_death/year");
                val2 = get_value (p_source_object, "home_record/date_of_death/month");
                val3 = get_value (p_source_object, "home_record/date_of_death/day");

                var date_of_death = Convert(val1, val2, val3);

                if(date_of_birth.HasValue && date_of_death.HasValue)
                {
                    age_of_mother = calculate_age(date_of_birth.Value, date_of_death.Value);
                }
                
                if(!age_of_mother.HasValue)
                {
                    //CALCULATE MOTHERS AGE AT DELIVERY ON BC
                    /*
                    path=birth_fetal_death_certificate_parent/demographic_of_mother/age
                    event=onfocus
                    */

                    val1 = get_value (p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/age");

                    if
                    (
                        val1 != null && 
                        !string.IsNullOrWhiteSpace(val1.ToString())
                    )
                    {
                        int temp;

                        if(int.TryParse(val1.ToString(), out temp))
                        {
                            age_of_mother = temp;
                        }
                    }

                    if(!age_of_mother.HasValue)
                    {
                        val1 = get_value (p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/year");
                        val2 = get_value (p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/month");
                        val3 = get_value (p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/date_of_birth/day");


                        date_of_birth = Convert(val1, val2, val3);

                        val1 = get_value (p_source_object, "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year");
                        val2 = get_value (p_source_object, "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month");
                        val3 = get_value (p_source_object, "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day");

                        date_of_death = Convert(val1, val2, val3);

                        if(date_of_birth.HasValue && date_of_death.HasValue)
                        {
                            age_of_mother = calculate_age(date_of_birth.Value, date_of_death.Value);
                        }
                    }
                }



            }

        deaths_by_age_enum result = deaths_by_age_enum.blank;;
        
        if(age_of_mother.HasValue)	
        {
            var value_test = age_of_mother.Value;

            if(value_test <= 0) result = deaths_by_age_enum.blank;
            else if(value_test < 20) result = deaths_by_age_enum.age_less_than_20;
            else if(value_test >= 20 && value_test <= 24) result = deaths_by_age_enum.age_20_to_24;
            else if(value_test >= 25 && value_test <= 29)  result = deaths_by_age_enum.age_25_to_29;
            else if(value_test >= 30 && value_test <= 34)  result = deaths_by_age_enum.age_30_to_34;
            else if(value_test >= 35 && value_test <= 39)  result = deaths_by_age_enum.age_35_to_39;
            else if(value_test >= 40 && value_test <= 44)  result = deaths_by_age_enum.age_40_to_44;
            else if(value_test >= 45)  result = deaths_by_age_enum.age_45_and_above;
        }

        return result;
    }


    private HashSet<ethnicity_enum> get_ethnicity_classifier (System.Dynamic.ExpandoObject p_source_object)
    {
        HashSet<ethnicity_enum> result = new HashSet<ethnicity_enum> ();

        object val_object = null;
        string val = null;


//Hispanic
        
        HashSet<string> bc_hispanic_origin = new HashSet<string> (StringComparer.InvariantCultureIgnoreCase);
        HashSet<string> dc_hispanic_origin = new HashSet<string> (StringComparer.InvariantCultureIgnoreCase);

//birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin 
        // Yes, Mexican, Mexican American, Chicano 
        // Yes, Puerto Rican 
        // Yes, Cuban 
        // Yes, Other Spanish/Hispanic/Latino 
        //Yes, Origin Unknown

/*
9999 (blank)
0 No, Not Spanish/Hispanic/Latino
1 Yes, Mexican, Mexican American, Chicano
2 Yes, Puerto Rican
3 Yes, Cuban
4 Yes, Other Spanish/Hispanic/Latino
5 Yes, Origin Unknown
8888 Not Specified


        bc_hispanic_origin.Add ("Yes, Mexican, Mexican American, Chicano");
        bc_hispanic_origin.Add ("Yes, Puerto Rican");
        bc_hispanic_origin.Add ("Yes, Cuban");
        bc_hispanic_origin.Add ("Yes, Other Spanish/Hispanic/Latino");
        bc_hispanic_origin.Add ("Yes, Origin Unknown");
*/
        bc_hispanic_origin.Add ("1");
        bc_hispanic_origin.Add ("2");
        bc_hispanic_origin.Add ("3");
        bc_hispanic_origin.Add ("4");
        bc_hispanic_origin.Add ("5");



//IF NO BC present:
//death_certificate/demographics/is_of_hispanic_origin
        //Yes, Mexican, Mexican American, Chicano
        //Yes, Puerto Rican 
        //Yes, Cuban
        //Yes, Other Spanish/Hispanic/Latino 
        //Yes, Origin Unknown

        dc_hispanic_origin.Add ("1");
        dc_hispanic_origin.Add ("2");
        dc_hispanic_origin.Add ("3");
        dc_hispanic_origin.Add ("4");
        dc_hispanic_origin.Add ("5");


        val_object = get_value (p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin");
        if(val_object != null)
        {
            val = val_object.ToString();
        }

        if (val != null)
        {
            if (bc_hispanic_origin.Contains (val))
            {
                result.Add (ethnicity_enum.hispanic);
            }
            else 
            {
                val = get_value (p_source_object, "death_certificate/demographics/is_of_hispanic_origin")?.ToString();
                if (dc_hispanic_origin.Contains (val))
                {
                    result.Add (ethnicity_enum.hispanic);
                }
            }
        }


//Non-Hispanic Black
//birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = 
//No, not Spanish/ Hispanic/ Latino; AND 
//birth_fetal_death_certificate_parent/race_of_mother = Black
//IF NO BC PRESENT:
//death_certificate/demographics/is_of_hispanic_origin = 
//No, not Spanish/ Hispanic/ Latino; AND
// death_certificate/Race/race = Black
        //if (is_non_hispanic("Black", p_source_object))
        if (is_non_hispanic("1", p_source_object))
        {
            result.Add (ethnicity_enum.non_hispanic_black);
        }



/*

Non-Hispanic White

birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; 
AND birth_fetal_death_certificate_parent/race_of_mother = White

IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; 
AND death_certificate/Race/race = White
*/

        //if (is_non_hispanic("White", p_source_object))
        if (is_non_hispanic("0", p_source_object))
        {
            result.Add (ethnicity_enum.non_hispanic_white);
        }


/*
American Indian / Alaska Native

birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND
birth_fetal_death_certificate_parent/race_of_mother = American Indian / AK Native

IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND
death_certificate/Race/race = American Indian / AK Native
*/
        //if (is_non_hispanic("American Indian/Alaska Native", p_source_object))
        if (is_non_hispanic("2", p_source_object))
        {
            result.Add (ethnicity_enum.american_indian_alaska_native);
        }
/*
Native Hawaiian

birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
birth_fetal_death_certificate_parent/race_of_mother = Native Hawaiian

IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
death_certificate/Race/race = Native Hawaiian
*/
        //if (is_non_hispanic("Native Hawaiian", p_source_object))
        if (is_non_hispanic("3", p_source_object))
        {
            result.Add (ethnicity_enum.native_hawaiian);
        }
/*
Guamanian or Chamorro

birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND
birth_fetal_death_certificate_parent/race_of_mother = Guamanian or Chamorro

IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND
death_certificate/Race/race = Guamanian or Chamorro
*/
        //if (is_non_hispanic("Guamanian or Chamorro", p_source_object))
        if (is_non_hispanic("4", p_source_object))
        {
            result.Add (ethnicity_enum.guamanian_or_chamorro);
        }
/*
Samoan

birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND
birth_fetal_death_certificate_parent/race_of_mother = Samoan

IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
death_certificate/Race/race = Samoan
*/
        //if (is_non_hispanic("Samoan", p_source_object))
        if (is_non_hispanic("5", p_source_object))
        {
            result.Add (ethnicity_enum.samoan);
        }
/*

Other Pacific Islander

birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
birth_fetal_death_certificate_parent/race_of_mother = Other Pacific Islander
IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
death_certificate/Race/race = Other Pacific Islander
*/
        //if (is_non_hispanic("Other Pacific Islander", p_source_object))
        if (is_non_hispanic("6", p_source_object))
        {
            result.Add (ethnicity_enum.other_pacific_islander);
        }
/*
Asian Indian

birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
birth_fetal_death_certificate_parent/race_of_mother = Asian Indian

IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND
death_certificate/Race/race = Asian Indian
*/
        //if (is_non_hispanic("Asian Indian", p_source_object))
        if (is_non_hispanic("7", p_source_object))
        {
            result.Add (ethnicity_enum.asian_indian);
        }
/*
Filipino

birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
birth_fetal_death_certificate_parent/race_of_mother = Filipino

IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
death_certificate/Race/race = Filipino
*/
        //if (is_non_hispanic("Filipino", p_source_object))
        if (is_non_hispanic("9", p_source_object))
        {
            result.Add (ethnicity_enum.filipino);
        }
/*
Korean

birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
birth_fetal_death_certificate_parent/race_of_mother = Korean

IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
death_certificate/Race/race = Korean
*/
        //if (is_non_hispanic("Korean", p_source_object))
        if (is_non_hispanic("11", p_source_object))
        {
            result.Add (ethnicity_enum.korean);
        }
/*
Other Asian

birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
birth_fetal_death_certificate_parent/race_of_mother = Other Asian

IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
death_certificate/Race/race = Other Asian
*/
        //if (is_non_hispanic("Other Asian", p_source_object))
        if (is_non_hispanic("13", p_source_object))
        {
            result.Add (ethnicity_enum.other_asian);
        }
/*
Chinese

birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
birth_fetal_death_certificate_parent/race_of_mother = Chinese
IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
death_certificate/Race/race = Chinese
*/
        //if (is_non_hispanic("Chinese", p_source_object))
        if (is_non_hispanic("8", p_source_object))
        {
            result.Add (ethnicity_enum.chinese);
        }
/*
Japanese


birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
birth_fetal_death_certificate_parent/race_of_mother = Japanese

IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
death_certificate/Race/race = Japanese
*/
        //if (is_non_hispanic("Japanese", p_source_object))
        if (is_non_hispanic("10", p_source_object))
        {
            result.Add (ethnicity_enum.japanese);
        }
/*
Vietnamese

birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
birth_fetal_death_certificate_parent/race_of_mother = Vietnamese
IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
death_certificate/Race/race = Vietnamese
*/
        //if (is_non_hispanic("Vietnamese", p_source_object))
        if (is_non_hispanic("12", p_source_object))
        {
            result.Add (ethnicity_enum.vietnamese);
        }
/*
Other


birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
birth_fetal_death_certificate_parent/race_of_mother = Other
IF NO BC PRESENT:
death_certificate/demographics/is_of_hispanic_origin = No, not Spanish/ Hispanic/ Latino; AND 
death_certificate/Race/race = Other


        if 
        (
            is_non_hispanic("Other", p_source_object) ||
            is_non_hispanic("Other Race", p_source_object) 
        )
*/
        if 
        (
            is_non_hispanic("13", p_source_object) ||
            is_non_hispanic("14", p_source_object) 
        )

        {
            result.Add (ethnicity_enum.other);
        }
        return result;

        
    }


    private void popluate_mTimingofDeath(ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {
        DateTime? Convert(object year, object month, object day)
        {
            DateTime? result = null;

            int start_year;
            int start_month;
            int start_day;

            if
            (
                year!= null && !string.IsNullOrWhiteSpace(year.ToString()) &&
                month!= null && !string.IsNullOrWhiteSpace(month.ToString()) &&
                day!= null && !string.IsNullOrWhiteSpace(day.ToString()) &&
                int.TryParse(year.ToString(), out start_year) && start_year != 9999 &&
                int.TryParse(month.ToString(), out start_month) && start_month != 9999  &&
                int.TryParse(day.ToString(), out start_day) && start_day != 9999 
            )
            {
                try
                {
                    result = new DateTime(start_year, start_month, start_day);
                }
                catch(Exception)
                {
                    
                }
                
            }
            else
            {

            }


            return result;
        }

        var _id = get_value(p_source_object, "_id");

        var bfdcpfodddod_year = get_value(p_source_object, "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/year");
        var bfdcpfodddod_month = get_value(p_source_object, "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/month");
        var bfdcpfodddod_day = get_value(p_source_object, "birth_fetal_death_certificate_parent/facility_of_delivery_demographics/date_of_delivery/day");
        var hrdod_year = get_value(p_source_object, "home_record/date_of_death/year");
        var hrdod_month = get_value(p_source_object, "home_record/date_of_death/month");
        var hrdod_day = get_value(p_source_object, "home_record/date_of_death/day");

        var hr_abs_dth_timing_dynamic = get_value(p_source_object, "home_record/overall_assessment_of_timing_of_death/abstrator_assigned_status");
        var hr_abs_dth_days_dynamic = get_value(p_source_object, "home_record/overall_assessment_of_timing_of_death/number_of_days_after_end_of_pregnancey");
        var dcdi_p_statu_dynamic = get_value(p_source_object, "death_certificate/death_information/pregnancy_status");

        int hr_abs_dth_timing = -1;
        if
        (
            hr_abs_dth_timing_dynamic != null &&
            !string.IsNullOrWhiteSpace(hr_abs_dth_timing_dynamic.ToString())
        )
        {
            int.TryParse(hr_abs_dth_timing_dynamic.ToString(), out hr_abs_dth_timing);
        } 
        
        int hr_abs_dth_days = -1;
        
        if
        (
            hr_abs_dth_days_dynamic != null &&
            !string.IsNullOrWhiteSpace(hr_abs_dth_days_dynamic.ToString())
        )
        {
                int.TryParse(hr_abs_dth_days_dynamic.ToString(), out hr_abs_dth_days);
        }
        
        int dcdi_p_statu = -1;
        if
        (
            dcdi_p_statu_dynamic != null &&
            !string.IsNullOrWhiteSpace(dcdi_p_statu_dynamic.ToString())
        )
        {
            int.TryParse(dcdi_p_statu_dynamic.ToString(), out dcdi_p_statu);
        }

/*
    if
    (
        bfdcpfodddod_month ne 9999 AND 
        bfdcpfodddod_day ne 9999 AND 
        bfdcpfodddod_year ne 9999 AND 
        hrdod_month NE 9999 AND 
        hrdod_day NE 9999 AND 
        hrdod_year NE 9999
    )
    {
        
    delivery_date_clean=mdy(bfdcpfodddod_month,bfdcpfodddod_day,bfdcpfodddod_year);
    death_date_clean=mdy(hrdod_month,hrdod_day,hrdod_year);
    timing_calc_clean=int(death_date_clean-delivery_date_clean);
    
}*/

var delivery_date = Convert(bfdcpfodddod_year, bfdcpfodddod_month, bfdcpfodddod_day);
var death_date = Convert(hrdod_year, hrdod_month, hrdod_day);
int? timing_calc_clean = null;
if(delivery_date.HasValue && death_date.HasValue)
{
    var interval = (death_date - delivery_date).Value;

    System.Console.WriteLine($"{interval.Days} - {interval.TotalDays}");
    timing_calc_clean = (int) interval.TotalDays;
}
        
int? timing_clean = null;
if(timing_calc_clean.HasValue)
{
    if ( timing_calc_clean.Value <= 0) timing_clean=0; 
    if (0 < timing_calc_clean.Value && timing_calc_clean.Value <43) timing_clean=1;
    if ( 42 < timing_calc_clean.Value && timing_calc_clean.Value <=365) timing_clean=2;
} 

//If the calculation for # of days between date of delivery and date of death is missing, use abstractor-assigned timing of death
if ( ! timing_calc_clean.HasValue)
{
    /*
        if .<hr_abs_dth_days<=0 then timing_clean=0;
        if 0<hr_abs_dth_days<43 then timing_clean=1;
        if 42<hr_abs_dth_days<=365 then timing_clean=2;
    */
    timing_clean = hr_abs_dth_timing switch 
    {
        1 => timing_clean = 0,
        2 =>  timing_clean = 1,
        3 =>  timing_clean = 2,
        _ => null
    };
    
}

    //If timing of death still missing, use abstractor-assigned days between end of pregnancy and death
    if ( ! timing_clean.HasValue)
    {

        /*
        if .<hr_abs_dth_days<=0 then timing_clean=0;
        if 0<hr_abs_dth_days<43 then timing_clean=1;
        if 42<hr_abs_dth_days<=365 then timing_clean=2;
        */
        timing_clean = hr_abs_dth_days switch 
        {
            < 0 => null,
            0 => timing_clean = 0,
            < 43 =>  timing_clean = 1,
            <= 365 =>  timing_clean = 2,
            _ => null
        };
    }
    
    //If timing of death still missing, use the death certificate pregnancy checkbox
    if ( ! timing_clean.HasValue)
    { /*
        if dcdi_p_statu=1 then timing_clean=0;
        else if dcdi_p_statu=2 then timing_clean=1;
        else if dcdi_p_statu=3 then timing_clean=2;
    */
        timing_clean = dcdi_p_statu switch 
        {
            1 => timing_clean = 0,
            2 =>  timing_clean = 1,
            3 =>  timing_clean = 2,
            _ => null
        };
    }
/*
MTimeD1=0
MTimeD2=1
MTimeD3=2
MTimeD4=missing/blank
*/

var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
curr.indicator_id = "mTimingofDeath";
curr.value = 1;
switch(timing_clean)
{
    case 0:
        curr.field_id = "MTimeD1";
        break;
    case 1:
        curr.field_id = "MTimeD2";
        break;
    case 2:
        curr.field_id = "MTimeD3";
        break;
    default:
        curr.field_id = "MTimeD4";
        break;
}
this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;


/*
        string val_1 = get_value(p_source_object, "death_certificate/death_information/pregnancy_status");

        int test_int;

        try
        {	



            if(timing_calc_clean.HasValue)
            {

                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mTimingofDeath";
                curr.value = 1;
                
                if(timing_calc_clean.Value <= 0)
                {
                    curr.field_id = "MTimeD1";
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                else if
                (
                    timing_calc_clean.Value > 0 && 
                    timing_calc_clean.Value <= 42
                )
                {
                    curr.field_id = "MTimeD2";
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;

                }
                else if
                (
                    timing_calc_clean.Value >= 43 &&
                    timing_calc_clean.Value <= 365
                )
                {
                    curr.field_id = "MTimeD3";
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                else
                {
                    timing_calc_clean = null;
                }
            }
            
            if(!timing_calc_clean.HasValue)
            {


                if
                (
                    val_1 != null && 
                    int.TryParse(val_1, out test_int) &&
                    test_int == 1
                )
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mTimingofDeath";
                    curr.field_id = "MTimeD1";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                else if
                (
                    val_1 != null && 
                    int.TryParse(val_1, out test_int) && 
                    test_int == 2
                )
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mTimingofDeath";
                    curr.field_id = "MTimeD2";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                else if
                (
                    val_1 != null && 
                    int.TryParse(val_1, out test_int) && 
                    test_int == 3
                )
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mTimingofDeath";
                    curr.field_id = "MTimeD3";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                else if
                (
                    string.IsNullOrWhiteSpace(val_1) || 
                    !int.TryParse(val_1, out test_int) ||
                    (
                        test_int == blank_value || 
                        test_int == 0 ||
                        test_int == 88 ||
                        test_int == 4 ||
                        test_int == 8888 ||
                        test_int == 7777
                    )
                )
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mTimingofDeath";
                    curr.field_id = "MTimeD4";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                else
                {

                }
            }
            

        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
*/

    return;
/*



pregnancy_status <- list field
9999 (blank)
0 Not pregnant within last year
1 Pregnant at the time of death
2 Pregnant within 42 days of death
3 Pregnant 43 to 365 days of death
88 Unknown if pregnant in last year
4 Not pregnant, but pregnant withing past year (time unknown)
8888 Not Specififed


*/

    }


private void popluate_pregnancy_deaths_by_age (ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
{

/*
mAgeatDeath	MAgeD1	<20
mAgeatDeath	MAgeD2	20-24
mAgeatDeath	MAgeD3	25-29
mAgeatDeath	MAgeD4	30-34
mAgeatDeath	MAgeD5	35-44
mAgeatDeath	MAgeD6	45+

*/

p_opioid_report_value.indicator_id = "mAgeatDeath";
p_opioid_report_value.value = 1;


deaths_by_age_enum age_enum = get_age_classifier (p_source_object);

switch (age_enum) 
{
    case deaths_by_age_enum.age_less_than_20:
        p_opioid_report_value.field_id = "MAgeD1";
        break;
    case deaths_by_age_enum.age_20_to_24:
        p_opioid_report_value.field_id = "MAgeD2";
        break;
    case deaths_by_age_enum.age_25_to_29:
        p_opioid_report_value.field_id = "MAgeD3";
        break;
    case deaths_by_age_enum.age_30_to_34:
        p_opioid_report_value.field_id = "MAgeD4";	
        break;
    case deaths_by_age_enum.age_35_to_39:
        p_opioid_report_value.field_id = "MAgeD5";
        break;
    case deaths_by_age_enum.age_40_to_44:
        p_opioid_report_value.field_id = "MAgeD6";
        break;
    case deaths_by_age_enum.age_45_and_above:
        p_opioid_report_value.field_id = "MAgeD7";
        break;
    case deaths_by_age_enum.blank:
    default:
        //BlankIdSet.Add(p_mmria_id);
        p_opioid_report_value.field_id = "MAgeD8";
        break;
}

/*			
age_less_than_20,
age_20_to_24,
age_25_to_29,
age_30_to_34,
age_35_to_44,
age_45_and_above
blank,
*/

}


    private void popluate_total_number_of_pregnancy_related_deaths_by_ethnicity (ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, System.Dynamic.ExpandoObject p_source_object, bool p_is_pregnancy_related)
    {
/*
mDeathsbyRaceEth	MRaceEth3	Hispanic
mDeathsbyRaceEth	MRaceEth4	Non-Hispanic Black
mDeathsbyRaceEth	MRaceEth5	Non-Hispanic White
mDeathsbyRaceEth	MRaceEth6	American Indian / Alaska Native
mDeathsbyRaceEth	MRaceEth7	Native Hawaiian
mDeathsbyRaceEth	MRaceEth8	Guamanian or Chamorro
mDeathsbyRaceEth	MRaceEth9	Samoan
mDeathsbyRaceEth	MRaceEth10	Other Pacific Islander
mDeathsbyRaceEth	MRaceEth11	Asian Indian
mDeathsbyRaceEth	MRaceEth12	Filipino
mDeathsbyRaceEth	MRaceEth13	Korean
mDeathsbyRaceEth	MRaceEth14	Other Asian
mDeathsbyRaceEth	MRaceEth15	Chinese
mDeathsbyRaceEth	MRaceEth16	Japanese
mDeathsbyRaceEth	MRaceEth17	Vietnamese
mDeathsbyRaceEth	MRaceEth18	Other Race
mDeathsbyRaceEth	MRaceEth1	White
mDeathsbyRaceEth	MRaceEth2	Black
mDeathsbyRaceEth	MRaceEth20	(Blank)
mDeathsbyRaceEth	MRaceEth19	Race Not Specified
*/
        p_opioid_report_value.indicator_id = "mDeathsbyRaceEth";
        p_opioid_report_value.value = 1;

        var race_ethnicity_result = get_race_ethnicity(p_source_object);

        switch(race_ethnicity_result)
        {
            case "9999":
                p_opioid_report_value.field_id = "MRaceEth20";
            break;
            case "hispanic":
                p_opioid_report_value.field_id = "MRaceEth3";
            break;
            case "black":
                p_opioid_report_value.field_id = "MRaceEth4";
            break;
            case "white":
                p_opioid_report_value.field_id = "MRaceEth5";
            break;
            case "other":
                p_opioid_report_value.field_id = "MRaceEth18";
            break;
        }


    }


    private void popluate_total_number_of_cases_by_pregnancy_relatedness (ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {
/*
MPregRel1	Pregnancy Related
MPregRel2	Pregnancy Associated, but NOT Related
MPregRel3	Pregnancy-Associated but Unable to Determine Pregnancy-Relatedness
MPregRel4	Not Pregnancy-Related or -Associated (i.e. False Positive)
MPregRel5	(Blank)
*/

        p_opioid_report_value.indicator_id = "mPregRelated";
        p_opioid_report_value.value = 1;
        p_opioid_report_value.pregnancy_related = 0;


        try
        {	
            
            var list = List_Look_Up["/committee_review/pregnancy_relatedness"];

            var val_dynamic = get_value(p_source_object, "committee_review/pregnancy_relatedness");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null)
            {
                switch(val)
                {
                    case "Pregnancy-Related":
                    case "Pregnancy Related":
                    case "1":
                        //p_report_object.mPregRelated.pregnancy_related = 1;
                        p_opioid_report_value.field_id = "MPregRel1";
                        p_opioid_report_value.pregnancy_related = 1;

                    break;
                    case "Pregnancy-Associated but NOT Related":
                    case "Pregnancy-Associated, but NOT -Related":
                    case "0":
                        //p_report_object.mPregRelated.pregnancy_associated_but_not_related = 1;
                        p_opioid_report_value.field_id = "MPregRel2";
                        p_opioid_report_value.pregnancy_related = 0;
                    break;
                    case "Not Pregnancy Related or Associated (i.e. False Positive)":
                    case "Not Pregnancy-Related or -Associated (i.e. False Positive)":
                    case "99":
                        //p_report_object.mPregRelated.not_pregnancy_related_or_associated = 1;
                        p_opioid_report_value.field_id = "MPregRel4";
                        p_opioid_report_value.pregnancy_related = 99;
                    break;
                    case "Pregnancy-Associated but Unable to Determine Pregnancy-Relatedness":
                    case "Unable to Determine if Pregnancy Related or Associated":
                    case "2":
                        //p_report_object.mPregRelated.unable_to_determine = 1;
                        p_opioid_report_value.field_id = "MPregRel3";
                        p_opioid_report_value.pregnancy_related = 2;
                    break;
                    default:
                        //p_report_object.mPregRelated.blank = 1;
                        p_opioid_report_value.field_id = "MPregRel5";
                        p_opioid_report_value.pregnancy_related = blank_value;
                    break;
                }

            }
            else
            {
                //p_report_object.mPregRelated.blank = 1;
                p_opioid_report_value.field_id = "MPregRel5";
                p_opioid_report_value.pregnancy_related = blank_value;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
    }

    private void Get_List_Look_Up
    (
        System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> p_result,
        mmria.common.metadata.node[] p_lookup,
        mmria.common.metadata.node p_metadata,
        string p_path
    )
    {
            switch(p_metadata.type.ToLower())
            {
                case "form":
                case "group":
                case "grid":
                    foreach(mmria.common.metadata.node node in p_metadata.children)
                    {
                        Get_List_Look_Up(p_result, p_lookup, node, p_path + "/" + node.name.ToLower());
                    }
                    break;
                case "list":

                    if
                    (
                        p_metadata.control_style!= null &&
                        p_metadata.control_style.ToLower() == "editable"
                    )
                    {
                        break;
                    }

                    p_result.Add(p_path, new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

                    var value_node_list = p_metadata.values;
                    if
                    (
                        !string.IsNullOrWhiteSpace(p_metadata.path_reference)
                    )
                    {
                        var name = p_metadata.path_reference.Replace("lookup/", "");
                        foreach(var item in p_lookup)
                        {
                            if(item.name.ToLower() == name.ToLower())
                            {
                                value_node_list = item.values;
                                break;
                            }
                        }
                    }

                    foreach(var value in value_node_list)
                    {
                        p_result[p_path].Add(value.value, value.display);
                    }

                    //p_result[file_name].Add(p_path, field_name);

                    break;
                default:
                    break;
            }
    }

    private void popluate_mDeathSubAbuseEvi (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        int test_int;

        var mmria_id = get_value(p_source_object, "_id");
        
        //mDeathSubAbuseEvi	Deaths with Evidence of Substance Use in Prenatal Records	MEviSub1	Yes	1	prenatal/evidence_of_substance_use=Yes	prenatal/evidence_of_substance_use = 1

        try
        {	

            var val_dynamic = get_value(p_source_object, "prenatal/evidence_of_substance_use");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }

            if(val != null && int.TryParse(val, out test_int) && test_int == 1)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathSubAbuseEvi";
                curr.field_id = "MEviSub1";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

        //mDeathSubAbuseEvi	Deaths with Evidence of Substance Use in Prenatal Records	MEviSub2	No	2	prenatal/evidence_of_substance_use=No	prenatal/evidence_of_substance_use = 0

        try
        {	

            var val_dynamic = get_value(p_source_object, "prenatal/evidence_of_substance_use");
            
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }

            if(val != null && int.TryParse(val, out test_int) && test_int == 0)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathSubAbuseEvi";
                curr.field_id = "MEviSub2";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }


        try
        {	

            var val_dynamic = get_value(p_source_object, "prenatal/evidence_of_substance_use");
            
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }

            if
            (
                val == null || 
                string.IsNullOrWhiteSpace(val) || 
                (val != null && int.TryParse(val, out test_int) && test_int == blank_value)
            )
            {
                
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathSubAbuseEvi";
                curr.field_id = "MEviSub3";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

        try
        {	

            var val_dynamic = get_value(p_source_object, "prenatal/evidence_of_substance_use");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            
            if(val != null && int.TryParse(val, out test_int) && test_int == 7777)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathSubAbuseEvi";
                curr.field_id = "MEviSub4";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

    }

    private void popluate_death_cause (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

            int test_int;


//mDeathCause	MCauseD1	Mental Health Conditions - Yes	1	committee_review/did_mental_health_conditions_contribute_to_the_death = yes	committee_review/did_mental_health_conditions_contribute_to_the_death = 1

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/did_mental_health_conditions_contribute_to_the_death");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null && int.TryParse(val, out test_int) && test_int == 1)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD1";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }



//mDeathCause	MCauseD2	Mental Health Conditions - No	2	committee_review/did_mental_health_conditions_contribute_to_the_death = No	committee_review/did_mental_health_conditions_contribute_to_the_death = 0

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/did_mental_health_conditions_contribute_to_the_death");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null && int.TryParse(val, out test_int) && test_int == 0)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD2";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }



//mDeathCause	MCauseD3	Mental Health Conditions - Possibly	3	committee_review/did_mental_health_conditions_contribute_to_the_death = Probably	committee_review/did_mental_health_conditions_contribute_to_the_death = 2

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/did_mental_health_conditions_contribute_to_the_death");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null && int.TryParse(val, out test_int) && test_int == 2)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD3";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }



//mDeathCause	MCauseD4	Mental Health Conditions - Unknown	4	committee_review/did_mental_health_conditions_contribute_to_the_death = Unknown	committee_review/did_mental_health_conditions_contribute_to_the_death = 7777

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/did_mental_health_conditions_contribute_to_the_death");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null && int.TryParse(val, out test_int) && test_int == 7777)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD4";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }



//mDeathCause	MCauseD5	Mental Health Conditions - Blank	5	committee_review/did_mental_health_conditions_contribute_to_the_death = Blank	committee_review/did_mental_health_conditions_contribute_to_the_death = 9999

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/did_mental_health_conditions_contribute_to_the_death");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val == null || string.IsNullOrWhiteSpace(val) ||  (val != null && int.TryParse(val, out test_int) && test_int == blank_value))
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD5";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }


//mDeathCause	MCauseD6	Substance Use Disorder - Yes	6	committee_review/did_substance_use_disorder_contribute_to_the_death = Yes	committee_review/did_substance_use_disorder_contribute_to_the_death = 1

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/did_substance_use_disorder_contribute_to_the_death");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null && int.TryParse(val, out test_int) && test_int == 1)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD6";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

//mDeathCause	MCauseD7	Substance Use Disorder - No	7	committee_review/did_substance_use_disorder_contribute_to_the_death = No	committee_review/did_substance_use_disorder_contribute_to_the_death = 0

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/did_substance_use_disorder_contribute_to_the_death");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null && int.TryParse(val, out test_int) && test_int == 0)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD7";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }


//mDeathCause	MCauseD8	Mental Health Conditions-Possibly - Possibly	8	committee_review/did_substance_use_disorder_contribute_to_the_death = Probably	committee_review/did_substance_use_disorder_contribute_to_the_death = 2

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/did_substance_use_disorder_contribute_to_the_death");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null && int.TryParse(val, out test_int) && test_int == 2)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD8";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

//mDeathCause	MCauseD9	Mental Health Conditions - Unknown	9	committee_review/did_substance_use_disorder_contribute_to_the_death  = Unknown	committee_review/did_substance_use_disorder_contribute_to_the_death  = 7777

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/did_substance_use_disorder_contribute_to_the_death");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null && int.TryParse(val, out test_int) && test_int == 7777)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD9";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }


//mDeathCause	MCauseD10	Mental Health Conditions - Blank	10	committee_review/did_substance_use_disorder_contribute_to_the_death  = Blank	committee_review/did_substance_use_disorder_contribute_to_the_death  = 9999

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/did_substance_use_disorder_contribute_to_the_death");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val == null || string.IsNullOrWhiteSpace(val) || (val != null && int.TryParse(val, out test_int) && test_int == blank_value))
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD10";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }



//mDeathCause	MCauseD11	Suicide - Yes	11	committee_review/was_this_death_a_sucide = Yes	committee_review/was_this_death_a_sucde = 1

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/was_this_death_a_sucide");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null && int.TryParse(val, out test_int) && test_int == 1)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD11";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }


//mDeathCause	MCauseD12	Suicide - No	12	committee_review/was_this_death_a_sucide = No	committee_review/was_this_death_a_sucide = 0

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/was_this_death_a_sucide");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null && int.TryParse(val, out test_int) && test_int == 0)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD12";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }


//mDeathCause	MCauseD13	Suicide - Possibly	13	committee_review/was_this_death_a_sucide = Probably	committee_review/was_this_death_a_sucide = 2

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/was_this_death_a_sucide");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null && int.TryParse(val, out test_int) && test_int == 2)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD13";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }


//mDeathCause	MCauseD14	Suicide - Unknown	14	committee_review/was_this_death_a_sucide  = Unknown	committee_review/was_this_death_a_sucide  = 7777

        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/was_this_death_a_sucide");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val != null && int.TryParse(val, out test_int) && test_int == 7777)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD14";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

//mDeathCause	MCauseD15	Suicide - Blank	15	committee_review/was_this_death_a_sucide  = Blank	committee_review/was_this_death_a_sucide  = 9999


        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/was_this_death_a_sucide");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            if(val == null || string.IsNullOrWhiteSpace(val) ||  (val != null && int.TryParse(val, out test_int) && test_int == blank_value))
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD15";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }



        try
        {	


            var val_dynamic = get_value(p_source_object, "committee_review/did_obesity_contribute_to_the_death");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }
            
            // MCauseD16 committee_review/did_obesity_contribute_to_the_death=1
            if(!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out test_int) && test_int == 1)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD16";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            // MCauseD17 committee_review/did_obesity_contribute_to_the_death=0
            if(!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out test_int) && test_int == 0)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD17";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            // MCauseD18 committee_review/did_obesity_contribute_to_the_death=2
            if(!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out test_int) && test_int == 2)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD18";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            // MCauseD19 committee_review/did_obesity_contribute_to_the_death=7777
            if(!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out test_int) && test_int == 7777)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD19";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            // MCauseD20 committee_review/did_obesity_contribute_to_the_death=9999 (include missing)
            if(val == null || string.IsNullOrWhiteSpace(val) ||  (val != null && int.TryParse(val, out test_int) && test_int == blank_value))
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD20";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }


        try
        {	

            var val_dynamic = get_value(p_source_object, "committee_review/did_discrimination_contribute_to_the_death");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }

            // MCauseD21 committee_review/did_discrimination_contribute_to_the_death=1
            if(!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out test_int) && test_int == 1)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD21";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            // MCauseD22 committee_review/did_discrimination_contribute_to_the_death=0
            if(!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out test_int) && test_int == 0)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD22";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            // MCauseD23 committee_review/did_discrimination_contribute_to_the_death=2
            if(!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out test_int) && test_int == 2)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD23";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            // MCauseD24 committee_review/did_discrimination_contribute_to_the_death=7777
            if(!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out test_int) && test_int == 7777)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD24";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            // MCauseD25 committee_review/did_discrimination_contribute_to_the_death=9999 (include missing)
            if(val == null || string.IsNullOrWhiteSpace(val) ||  (val != null && int.TryParse(val, out test_int) && test_int == blank_value))
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD25";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

        try
        {	
            var val_dynamic = get_value(p_source_object, "committee_review/was_this_death_a_homicide");
            string val = null;
            if(val_dynamic != null)
            {
                val = val_dynamic.ToString();
            }

            // MCauseD26 committee_review/was_this_death_a_homicide=1
            if(!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out test_int) && test_int == 1)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD26";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            // MCauseD27 committee_review/was_this_death_a_homicide=0
            if(!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out test_int) && test_int == 0)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD27";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            // MCauseD28 committee_review/was_this_death_a_homicide=2
            if(!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out test_int) && test_int == 2)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD28";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            // MCauseD29 committee_review/was_this_death_a_homicide=7777
            if(!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out test_int) && test_int == 7777)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD29";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            // MCauseD30 committee_review/was_this_death_a_homicide=9999 (include missing)
            if(val == null || string.IsNullOrWhiteSpace(val) ||  (val != null && int.TryParse(val, out test_int) && test_int == blank_value))
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathCause";
                curr.field_id = "MCauseD30";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }


    }


    private void popluate_mEducation (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        int test_int;

/*
birth_fetal_death_certificate_parent/demographic_of_mother/education_level = '8th Grade or Less' or '9th-12th Grade; No Diploma' or 'High School Grad or GED Completed'
OR
death_certificate/demographics/education_level = '8th Grade or Less' or '9th-12th Grade; No Diploma' or 'High School Grad or GED Completed'
*/



//mEducation	Education	MEduc1	Completed High School or less	1	birth_fetal_death_certificate_parent/demographic_of_mother/education_level = 
//'8th Grade or Less' or '9th-12th Grade; No Diploma' or 'High School Grad or GED Completed' OR death_certificate/demographics/education_level = '8th Grade or Less' or 
//'9th-12th Grade; No Diploma' or 'High School Grad or GED Completed'	birth_fetal_death_certificate_parent/demographic_of_mother/education_level in (0, 1, 2) 
// OR death_certificate/demographics/education_level in (0, 1, 2)
        try
        {	
            string val_1 = get_value(p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/education_level")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && !(test_int > 7 && test_int <= blank_value))
            {
                if( test_int>=0 && test_int <= 2)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mEducation";
                    curr.field_id = "MEduc1";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
            }
            else if(val_1 == null || string.IsNullOrWhiteSpace(val_1) || int.TryParse(val_1, out test_int) && (test_int > 7 && test_int <= blank_value))
            {
                string val_2 = get_value(p_source_object, "death_certificate/demographics/education_level")?.ToString();
                if(val_2 != null && int.TryParse(val_2, out test_int) && test_int >=0 && test_int <= 2)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mEducation";
                    curr.field_id = "MEduc1";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

//mEducation	Education	MEduc2	Completed Some College	2	
//birth_fetal_death_certificate_parent/demographic_of_mother/education_level = 'Some College; No Degree' OR 
//death_certificate/demographics/education_level =  'Some College; No Degree'	
//birth_fetal_death_certificate_parent/demographic_of_mother/education_level = 3 OR death_certificate/demographics/education_level =  3

        try
        {	
            string val_1 = get_value(p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/education_level")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && !(test_int > 7 && test_int <= blank_value))
            {
                if(test_int >=3 && test_int <= 3)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mEducation";
                    curr.field_id = "MEduc2";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
            }
            else if(val_1 == null || string.IsNullOrWhiteSpace(val_1) || int.TryParse(val_1, out test_int) && (test_int > 7 && test_int <= blank_value))
            {
                string val_2 = get_value(p_source_object, "death_certificate/demographics/education_level")?.ToString();
                if(val_2 != null && int.TryParse(val_2, out test_int) && test_int >=3 && test_int <= 3)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mEducation";
                    curr.field_id = "MEduc2";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

//mEducation	Education	MEduc3	College Graduate	3	birth_fetal_death_certificate_parent/demographic_of_mother/education_level = 'Associate Degree' or 'Bachelor's Degree'  OR death_certificate/demographics/education_level = 'Associate Degree' or 'Bachelor's Degree'	birth_fetal_death_certificate_parent/demographic_of_mother/education_level in (4, 5) OR death_certificate/demographics/education_level in (4, 5)
        try
        {	
            string val_1 = get_value(p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/education_level")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && !(test_int > 7 && test_int <= blank_value))
            {
                if(test_int >=4 && test_int <= 5)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mEducation";
                    curr.field_id = "MEduc3";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
            }
            else if(val_1 == null || string.IsNullOrWhiteSpace(val_1) || int.TryParse(val_1, out test_int) && (test_int > 7 && test_int <= blank_value))
            {
                string val_2 = get_value(p_source_object, "death_certificate/demographics/education_level")?.ToString();
                if(val_2 != null && int.TryParse(val_2, out test_int))
                {
                    if(test_int >=4 && test_int <= 5)
                    {
                        var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                        curr.indicator_id = "mEducation";
                        curr.field_id = "MEduc3";
                        curr.value = 1;
                        this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    }
                }
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

//mEducation	Education	MEduc4	Completed Advanced Degree	4	birth_fetal_death_certificate_parent/demographic_of_mother/education_level = 'Master's Degree' or 'Doctorate or Professional Degree' OR death_certificate/demographics/education_level = 'Master's Degree' or 'Doctorate or Professional Degree'	birth_fetal_death_certificate_parent/demographic_of_mother/education_level in (6, 7) OR death_certificate/demographics/education_levelin (6, 7)
        try
        {	
            string val_1 = get_value(p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/education_level")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && !(test_int > 7 && test_int <= blank_value))
            {
                if(test_int >=6 && test_int <= 7)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mEducation";
                    curr.field_id = "MEduc4";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    
                }
            }
            else if(val_1 == null || string.IsNullOrWhiteSpace(val_1) || int.TryParse(val_1, out test_int) && (test_int > 7 && test_int <= blank_value))
            {
                string val_2 = get_value(p_source_object, "death_certificate/demographics/education_level")?.ToString();
                if(val_2 != null && int.TryParse(val_2, out test_int) && test_int >=6 && test_int <= 7)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mEducation";
                    curr.field_id = "MEduc4";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }


        try
        {	
            string val_1 = get_value(p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/education_level")?.ToString();
            string val_2 = get_value(p_source_object, "death_certificate/demographics/education_level")?.ToString();

            var record_id = get_value(p_source_object, "_id");
            if(val_1 != null && int.TryParse(val_1, out test_int) && (test_int > 7 && test_int <= blank_value))
            {
                if(string.IsNullOrWhiteSpace(val_2) || (int.TryParse(val_2, out test_int) && test_int > 7 && test_int <= blank_value))
                {
                    //System.Console.WriteLine ($"MEduc5 blank death_certificate id: {record_id}");
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mEducation";
                    curr.field_id = "MEduc5";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
            }
            else if(val_1 == null || string.IsNullOrWhiteSpace(val_1))
            {
                
                if(string.IsNullOrWhiteSpace(val_2) || (int.TryParse(val_2, out test_int) && test_int > 7 && test_int <= blank_value))
                {
                    //System.Console.WriteLine ($"MEduc5 blank death_certificate id: {record_id}");
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mEducation";
                    curr.field_id = "MEduc5";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
    }
    private void popluate_mHomeless (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        int test_int;

/*
social_and_environmental_profile/socio_economic_characteristics/homelessness
*/
        object dynamic_val = get_value(p_source_object, "social_and_environmental_profile/socio_economic_characteristics/homelessness");

        if(dynamic_val != null && !(dynamic_val is IList<object>))
        {
            return;
        }

        var object_list = dynamic_val as IList<object>;

        if(object_list!= null)
        foreach(var object_val in object_list)
        {
            string val_1 = null;

            if(object_val != null)
            {
                val_1 = object_val.ToString();
            }
//mHomeless	Homelessness - Housing Arrangement at time of Death	MHomeless1	Never	1	social_and_environmental_profile/socio_economic_characteristics/homelessness = Never	social_and_environmental_profile/socio_economic_characteristics/homelessness = 0

            try
            {	
                
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 0)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHomeless";
                    curr.field_id = "MHomeless1";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
//mHomeless	Homelessness - Housing Arrangement at time of Death	MHomeless2	Yes, in last 12 monhts	2	social_and_environmental_profile/socio_economic_characteristics/homelessness =Yes, in last 12 months	social_and_environmental_profile/socio_economic_characteristics/homelessness = 1

            try
            {	
                
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 1)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHomeless";
                    curr.field_id = "MHomeless2";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
//mHomeless	Homelessness - Housing Arrangement at time of Death	MHomeless3	Yes, but more than 12 months ago	3	social_and_environmental_profile/socio_economic_characteristics/homelessness = Yes, but more than 12 months ago	social_and_environmental_profile/socio_economic_characteristics/homelessness =  2

            try
            {	
                
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 2)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHomeless";
                    curr.field_id = "MHomeless3";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
//mHomeless	Homelessness - Housing Arrangement at time of Death	MHomeless4	Unknown/Not Specified	4	social_and_environmental_profile/socio_economic_characteristics/homelessness = Unknown or Not Specified	social_and_environmental_profile/socio_economic_characteristics/homelessness in (7777, 8888)

            try
            {	
                
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && (test_int == 7777 || test_int == 8888))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHomeless";
                    curr.field_id = "MHomeless4";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

            try
            {	
                
                
                if(val_1 == null || string.IsNullOrWhiteSpace(val_1) || (val_1 != null && int.TryParse(val_1, out test_int) && test_int == blank_value))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHomeless";
                    curr.field_id = "MHomeless5";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

            try
            {	
                
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 3)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHomeless";
                    curr.field_id = "MHomeless6";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

            try
            {	
                
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 4)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHomeless";
                    curr.field_id = "MHomeless7";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

            try
            {	
                
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 5)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHomeless";
                    curr.field_id = "MHomeless8";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

            try
            {	
                
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 6)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHomeless";
                    curr.field_id = "MHomeless9";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
        }

    }

    private void popluate_mHxofEmoStress (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        int test_int;

/*
social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress
*/

List<object> val_list = get_value(p_source_object, "social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress") as List<object>;

List<string> val_string_list = new List<string>();

foreach(var item in val_list)
{
val_string_list.Add(item.ToString());
}


//mHxofEmoStress	History of Social or Emotional Stress	MEmoStress1	History of domestic violence	1	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=History of domestic violence	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=0
        try
        {	
            
            
            foreach(string val_1 in val_string_list)
            {
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 0)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress1";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mHxofEmoStress	History of Social or Emotional Stress	MEmoStress2	History of psychiatric hospitalizations or treatment	2	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=History of psychiatric hospitalizations or treatment	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=1
        try
        {	
            foreach(string val_1 in val_string_list)
            {
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 1)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress2";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mHxofEmoStress	History of Social or Emotional Stress	MEmoStress3	Child Protective Services involvement	3	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=Child Protective Services involvement	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=2
        try
        {	
            foreach(string val_1 in val_string_list)
            {
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 2)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress3";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mHxofEmoStress	History of Social or Emotional Stress	MEmoStress4	History of substance use	4	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=History of substance use	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=3
        try
        {	
            foreach(string val_1 in val_string_list)
            {
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 3)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress4";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mHxofEmoStress	History of Social or Emotional Stress	MEmoStress5	Unemployment	5	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=Unemployment	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=4
        try
        {	
            foreach(string val_1 in val_string_list)
            {
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 4)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress5";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mHxofEmoStress	History of Social or Emotional Stress	MEmoStress6	History of substance use treatment	6	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=History of substance use treatment	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=5
        try
        {	
            foreach(string val_1 in val_string_list)
            {
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 5)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress6";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mHxofEmoStress	History of Social or Emotional Stress	MEmoStress7	Pregnancy Unwanted	7	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=Pregnancy Unwanted	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=6
        try
        {	
            foreach(string val_1 in val_string_list)
            {
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 6)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress7";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mHxofEmoStress	History of Social or Emotional Stress	MEmoStress8	Recent Trauma	8	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=Recent Trauma	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=7
        try
        {	
            foreach(string val_1 in val_string_list)
            {
            
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 7)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress8";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mHxofEmoStress	History of Social or Emotional Stress	MEmoStress9	History of childhood trauma	9	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=History of childhood trauma	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=8
        try
        {	
            foreach(string val_1 in val_string_list)
            {
            
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 8)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress9";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mHxofEmoStress	History of Social or Emotional Stress	MEmoStress10	Prior suicice attempts	10	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=Prior suicide attempts	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=9
        try
        {	
            foreach(string val_1 in val_string_list)
            {
            
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 9)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress10";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }				
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mHxofEmoStress	History of Social or Emotional Stress	MEmoStress11	Other	11	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=Other	social_and_environmental_profile/social_or_emotional_stress/evidence_of_social_or_emotional_stress=10
        try
        {	
            foreach(string val_1 in val_string_list)
            {
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 10)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress11";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

        try
        {	
            foreach(string val_1 in val_string_list)
            {
                if(val_1 == null || string.IsNullOrWhiteSpace(val_1) || (val_1 != null && int.TryParse(val_1, out test_int) && test_int == blank_value))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress12";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

        try
        {	
            foreach(string val_1 in val_string_list)
            {
                if(val_1 == null || (val_1 != null && int.TryParse(val_1, out test_int) && test_int == 7777))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress13";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

        try
        {	
            foreach(string val_1 in val_string_list)
            {
                if(val_1 == null || (val_1 != null && int.TryParse(val_1, out test_int) && test_int == 11))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mHxofEmoStress";
                    curr.field_id = "MEmoStress14";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

    }

    private void popluate_mHxofSubAbu (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        int test_int;

//social_and_environmental_profile/documented_substance_use

//mHxofSubAbu	History of Documented Substance Use	MHxSub1	Yes	1	social_and_environmental_profile/documented_substance_use=Yes	social_and_environmental_profile/documented_substance_use = 1
        try
        {	
            string val_1 = get_value(p_source_object, "social_and_environmental_profile/documented_substance_use")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 1)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mHxofSubAbu";
                curr.field_id = "MHxSub1";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
    

//mHxofSubAbu	History of Documented Substance Use	MHxSub2	No	2	social_and_environmental_profile/documented_substance_use=No	social_and_environmental_profile/documented_substance_use = 0
        try
        {	
            string val_1 = get_value(p_source_object, "social_and_environmental_profile/documented_substance_use")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 0)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mHxofSubAbu";
                curr.field_id = "MHxSub2";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
    

//mHxofSubAbu	History of Documented Substance Use	MHxSub3	Not Specified	3	social_and_environmental_profile/documented_substance_use=Not Specified	social_and_environmental_profile/documented_substance_use = 8888

        try
        {	
            string val_1 = get_value(p_source_object, "social_and_environmental_profile/documented_substance_use")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 7777)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mHxofSubAbu";
                curr.field_id = "MHxSub3";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

        try
        {	
            string val_1 = get_value(p_source_object, "social_and_environmental_profile/documented_substance_use")?.ToString();
            
            if(val_1 == null || string.IsNullOrWhiteSpace(val_1) || (val_1 != null && int.TryParse(val_1, out test_int) && test_int == blank_value))
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mHxofSubAbu";
                curr.field_id = "MHxSub4";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
    }

    private void popluate_mIncarHx (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        int test_int;

//social_and_environmental_profile/previous_or_current_incarcerations

        object val_list = get_value(p_source_object, "social_and_environmental_profile/previous_or_current_incarcerations");

        if(val_list != null && !(val_list is IList<object>))
        {
            return;

        }

        foreach(var val_object in (IList<object>)val_list)
        {
            string val_1 = null;

            if(val_object != null)
            {
                val_1 = val_object.ToString();
            }
//mIncarHx	Incarceration History	MHxIncar1	Never Incarcerated	1	social_and_environmental_profile/previous_or_current_incarcerations=Never	social_and_environmental_profile/previous_or_current_incarcerations=0
            try
            {	
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 0)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mIncarHx";
                    curr.field_id = "MHxIncar1";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
//mIncarHx	Incarceration History	MHxIncar2	Was Incarcerated	2	social_and_environmental_profile/previous_or_current_incarcerations=Before pregnancy or During Pregnancy or After Pregnancy	social_and_environmental_profile/previous_or_current_incarcerations in (1, 2, 3)
            /*
            try
            {	
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int >= 1 && test_int <= 3)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mIncarHx";
                    curr.field_id = "MHxIncar2";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }*/
//mIncarHx	Incarceration History	MHxIncar3	Incarcerated Before Current Pregnancy	3	social_and_environmental_profile/previous_or_current_incarcerations=Before pregnancy	social_and_environmental_profile/previous_or_current_incarcerations=1
            try
            {	
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 1)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mIncarHx";
                    curr.field_id = "MHxIncar3";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
//mIncarHx	Incarceration History	MHxIncar4	Incarcerated During Current Pregnancy	4	social_and_environmental_profile/previous_or_current_incarcerations=During Pregnancy	social_and_environmental_profile/previous_or_current_incarcerations=2
            try
            {	
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 2)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mIncarHx";
                    curr.field_id = "MHxIncar4";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
//mIncarHx	Incarceration History	MHxIncar5	Incarcerated After Current Pregnancy	5	social_and_environmental_profile/previous_or_current_incarcerations=After Pregnancy	social_and_environmental_profile/previous_or_current_incarcerations=3
            try
            {	
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 3)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mIncarHx";
                    curr.field_id = "MHxIncar5";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

            try
            {	
                
                if(val_1 == null || string.IsNullOrWhiteSpace(val_1) || (val_1 != null && int.TryParse(val_1, out test_int) && test_int == blank_value))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mIncarHx";
                    curr.field_id = "MHxIncar7";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
/*
mIncarHx	Number of deaths by mother’s incarceration history in relation to pregnancy	Mother’s Incarceration History	MHxIncar1	Never incarcerated
mIncarHx	Number of deaths by mother’s incarceration history in relation to pregnancy	Mother’s Incarceration History	MHxIncar8	More than 1 year prior to pregnancy
mIncarHx	Number of deaths by mother’s incarceration history in relation to pregnancy	Mother’s Incarceration History	MHxIncar9	Within 1 year prior to pregnancy
mIncarHx	Number of deaths by mother’s incarceration history in relation to pregnancy	Mother’s Incarceration History	MHxIncar4	During current pregnancy
mIncarHx	Number of deaths by mother’s incarceration history in relation to pregnancy	Mother’s Incarceration History	MHxIncar5	After current pregnancy
mIncarHx	Number of deaths by mother’s incarceration history in relation to pregnancy	Mother’s Incarceration History	MHxIncar3	Before current pregnancy (obsolete)
mIncarHx	Number of deaths by mother’s incarceration history in relation to pregnancy	Mother’s Incarceration History	MHxIncar10	Unknown
mIncarHx	Number of deaths by mother’s incarceration history in relation to pregnancy	Mother’s Incarceration History	MHxIncar7	(blank)
*/

            try
            {
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 4)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mIncarHx";
                    curr.field_id = "MHxIncar8";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

                try
            {
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 5)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mIncarHx";
                    curr.field_id = "MHxIncar9";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }


                try
            {
                
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 7777)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mIncarHx";
                    curr.field_id = "MHxIncar10";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
            

        }
    }

    private void popluate_mLivingArrange (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        int test_int;

//social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements

//mLivingArrange	Living Arrangements at time of death	MLivD1	Own	1	social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements=Own	social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements=0
        try
        {	
            string val_1 = get_value(p_source_object, "social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 0)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mLivingArrange";
                curr.field_id = "MLivD1";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mLivingArrange	Living Arrangements at time of death	MLivD2	Rent	2	social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements=Rent	social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements=1
        try
        {	
            string val_1 = get_value(p_source_object, "social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 1)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mLivingArrange";
                curr.field_id = "MLivD2";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mLivingArrange	Living Arrangements at time of death	MLivD3	Public Housing	3	social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements=Public Housing	social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements=2
        try
        {	
            string val_1 = get_value(p_source_object, "social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 2)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mLivingArrange";
                curr.field_id = "MLivD3";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mLivingArrange	Living Arrangements at time of death	MLivD4	Live with relative	4	social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements=Live with relative	social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements=3
        try
        {	
            string val_1 = get_value(p_source_object, "social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 3)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mLivingArrange";
                curr.field_id = "MLivD4";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mLivingArrange	Living Arrangements at time of death	MLivD5	Homeless	5	social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements=Homeless	social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements=4
        try
        {	
            string val_1 = get_value(p_source_object, "social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && test_int == 4)
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mLivingArrange";
                curr.field_id = "MLivD5";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
//mLivingArrange	Living Arrangements at time of death	MLivD6	Other or Unknown or Not specified	6	social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements=Other or Unknown or Not specified	social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements in (5, 7777, 8888)

        try
        {	
            string val_1 = get_value(p_source_object, "social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements")?.ToString();
            
            if(val_1 != null && int.TryParse(val_1, out test_int) && (test_int == 5 || test_int == 7777 || test_int == 8888))
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mLivingArrange";
                curr.field_id = "MLivD6";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

        try
        {	
            string val_1 = get_value(p_source_object, "social_and_environmental_profile/socio_economic_characteristics/current_living_arrangements")?.ToString();
            
            if(val_1 == null || string.IsNullOrWhiteSpace(val_1)|| (val_1 != null && int.TryParse(val_1, out test_int) && test_int == blank_value ))
            {
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mLivingArrange";
                curr.field_id = "MLivD7";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
    }


    private void popluate_mMHTxTiming (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        int test_int;

//mMHTxTiming	Number of Deaths by Mental Health Treatment Timing	MMHTx1	Prior to Most Recent Pregnancy	1	mental_health_profile/mental_health_conditions_prior_to_the_most_recent_pregnancy = Yes	mental_health_profile/mental_health_conditions_prior_to_the_most_recent_pregnancy in (0, 1, 2, 3, 4)
        try
        {	
            List<object> val_list = get_value(p_source_object, "mental_health_profile/mental_health_conditions_prior_to_the_most_recent_pregnancy") as List<object>;

            List<string> val_string_list = new List<string>();

            foreach(var item in val_list)
            {
                val_string_list.Add(item.ToString());
            }

            foreach(string val_1 in val_string_list)
            {
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int >= 0 && test_int <= 5)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mMHTxTiming";
                    curr.field_id = "MMHTx1";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

//mMHTxTiming	Number of Deaths by Mental Health Treatment Timing	MMHTx2	Was During Most Recent Pregnancy	2	mental_health_profile/mental_health_conditions_during_the_most_recent_pregnancy = Yes	mental_health_profile/mental_health_conditions_during_the_most_recent_pregnancy in (0, 1, 2, 3, 4)
        try
        {	
            List<object> val_list = get_value(p_source_object, "mental_health_profile/mental_health_conditions_during_the_most_recent_pregnancy") as List<object>;

            List<string> val_string_list = new List<string>();

            foreach(var item in val_list)
            {
                val_string_list.Add(item.ToString());
            }

            foreach(string val_1 in val_string_list)
            {
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int >= 0 && test_int <= 5)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mMHTxTiming";
                    curr.field_id = "MMHTx2";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

//mMHTxTiming	Number of Deaths by Mental Health Treatment Timing	MMHTx3	Mental Health Treatment After Most Recent Pregnancy	3	mental_health_profile/mental_health_conditions_after_the_most_recent_pregnancy = yes	mental_health_profile/mental_health_conditions_after_the_most_recent_pregnancy in (0, 1, 2, 3, 4)

        try
        {	
            List<object> val_list = get_value(p_source_object, "mental_health_profile/mental_health_conditions_after_the_most_recent_pregnancy") as List<object>;

            List<string> val_string_list = new List<string>();

            foreach(var item in val_list)
            {
                val_string_list.Add(item.ToString());
            }
            foreach(string val_1 in val_string_list)
            {
                if(val_1 != null && int.TryParse(val_1, out test_int) && test_int >= 0 && test_int <= 5)
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mMHTxTiming";
                    curr.field_id = "MMHTx3";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
/*
        try
        {	
            List<object> val_list = get_value(p_source_object, "mental_health_profile/mental_health_conditions_after_the_most_recent_pregnancy");

            List<string> val_string_list = new List<string>();

            foreach(var item in val_list)
            {
                val_string_list.Add(item.ToString());
            }
            foreach(string val_1 in val_string_list)
            {
                if(val_1 == null || string.IsNullOrWhiteSpace(val_1) || (val_1 != null && int.TryParse(val_1, out test_int) && test_int == blank_value))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mMHTxTiming";
                    curr.field_id = "MMHTx4";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                    break;
                }
            }
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }
*/
    }

    private void popluate_mSubstAutop (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        //autopsy_report/toxicology/substance

        //mSubstAutop	MSubAuto1	Alcohol	1	autopsy_report/toxicology/substance= Substance that are mapped to ‘Alcohol‘ category in the substance categorization table below	autopsy_report/toxicology/substance = 'Alcohol'
        //mSubstAutop	MSubAuto2	Amphetamine	2	autopsy_report/toxicology/substance= Substance that are mapped to ‘Amphetamine‘ category in the substance categorization table below	autopsy_report/toxicology/substance in ('Amphetamines, 'Methamphetamine')
        //mSubstAutop	MSubAuto3	Benzodiazepine	3	autopsy_report/toxicology/substance= Substance that are mapped to ‘Benzodiazepine‘ category  in the substance categorization table below	autopsy_report/toxicology/substance in ('Alprazolam (Xanax)', 'Aminoclonazepam', 'Chlordiazepoxide (Librium)', 'Clonazepam (Klonopin or Rivotril)', 'Diazepam (Valium)', 'Lorazepam (Ativan)', 'Temazepam (Restoril)', 'Zolpidem (Ambien)')
        //mSubstAutop	MSubAuto4	Buprenorphine/Methadone	4	autopsy_report/toxicology/substance= Substance that are mapped to ‘Buprenorphine/Methadone‘ category  in the substance categorization table below	autopsy_report/toxicology/substance in ('Buprenorphine', 'Methadone Hydrochloride')
        //mSubstAutop	MSubAuto5	Cocaine	5	autopsy_report/toxicology/substance= Substance that are mapped to ‘Cocaine‘category in the substance categorization table below	autopsy_report/toxicology/substance = 'Cocaine'
        //mSubstAutop	MSubAuto6	Opioid (excl Buprenorphine/Methadone)	6	autopsy_report/toxicology/substance= Substance that are mapped to ‘Opioid (excl Buprenorphine/Methadone)‘  category  in the substance categorization table below	autopsy_report/toxicology/substance in ('Fentanyl', 'Heroin', 'Hydromorphone (Dilaudid)', 'Morphine Sulfate', 'Oxycodone Hydrochloride', 'Oxymorphone Hydrochloride (Opana)')
        //mSubstAutop	MSubAuto7	Substance with Other Chemical Classification	7	autopsy_report/toxicology/substance= Substance that are mapped to ‘Substance with Other Chemical Classification‘  category  in the substance categorization table below	autopsy_report/toxicology/substance in ('Acetaminophen', 'Acetazolamide (Diamox)', 'Aripiprazole (Abilify)', 'Carbamazepine (Neurontin)', 'Citalopram (Celexa)', 'Doxepin (Silenor, Zonalon, Prudoxin), 'Duloxetine (Cymbalta)', 'Felbamate (Felbatol)', 'Fluoxetine/Olanzapine (Symbyax)', 'Lurasidone (Latuda)', 'Meprobamate (Equanil)', 'Midazolam (Versed)', 'Pregabalin (Lyrica)', 'Quetiapine (Seroquel)', 'Sertraline (Zoloft)', 'Trazadone (Oleptro)')
        //mSubstAutop	Substances at Autopsy	MSubAuto9	Cannabinoid	5	autopsy_report/toxicology/substance= Substance that are mapped to ‘Marijuana‘ category  in the substance categorization table below	autopsy_report/toxicology/substance in ('Marijuana')
        //mSubstAutop	Substances at Autopsy	MSubAuto10	Other	9	autopsy_report/toxicology/substance= Substance that are mapped to ‘Other‘  category  in the substance categorization table below	autopsy_report/toxicology/substance in ('Other')



        var is_Alcohol = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var is_Amphetamine = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var is_Benzodiazepine = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var is_Buprenorphine_Methadone = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var is_Cannabinoid = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var is_Cocaine = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var is_Opioid = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var is_Other_Substance = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        is_Alcohol.Add("Alcohol");

        is_Amphetamine.Add("Amphetamines");
        is_Amphetamine.Add("Methamphetamine");
/*
        is_Benzodiazepine.Add("Alprazolam (Xanax)");
        is_Benzodiazepine.Add("Aminoclonazepam");
        is_Benzodiazepine.Add("Chlordiazepoxide (Librium)");
        is_Benzodiazepine.Add("Clonazepam (Klonopin or Rivotril)");
        is_Benzodiazepine.Add("Diazepam (Valium)");
        is_Benzodiazepine.Add("Lorazepam (Ativan)");
        is_Benzodiazepine.Add("Temazepam (Restoril)");
        is_Benzodiazepine.Add("Zolpidem (Ambien)");
*/

    is_Benzodiazepine.Add("Alprazolam (Xanax)");
    is_Benzodiazepine.Add("Aminoclonazepam");
    is_Benzodiazepine.Add("Chlordiazepoxide (Librium)");
    is_Benzodiazepine.Add("Clonazepam (Klonopin or Rivotril)");
    is_Benzodiazepine.Add("Diazepam (Valium)");
    is_Benzodiazepine.Add("Lorazepam (Ativan)");
    is_Benzodiazepine.Add("Midazolam (Versed)");
    is_Benzodiazepine.Add("Temazepam (Restoril)");

        is_Buprenorphine_Methadone.Add("Buprenorphine");
        is_Buprenorphine_Methadone.Add("Methadone");
        is_Buprenorphine_Methadone.Add("Methadone Hydrochloride");

        is_Cannabinoid.Add("Marijuana");

        is_Cocaine.Add("Cocaine");

        is_Opioid.Add("Fentanyl");
        is_Opioid.Add("Heroin");
        is_Opioid.Add("Hydromorphone (Dilaudid)");
        is_Opioid.Add("Morphine Sulfate");
        is_Opioid.Add("Oxycodone Hydrochloride");
        is_Opioid.Add("Oxymorphone Hydrochloride (Opana)");

        is_Other_Substance.Add("Acetaminophen");
        is_Other_Substance.Add("Acetazolamide (Diamox)");
        is_Other_Substance.Add("Aripiprazole (Abilify)");
        is_Other_Substance.Add("Carbamazepine (Tegretol)");
        is_Other_Substance.Add("Citalopram (Celexa)");
        is_Other_Substance.Add("Doxepin (Silenor, Zonalon, Prudoxin)");
        is_Other_Substance.Add("Duloxetine (Cymbalta)");
        is_Other_Substance.Add("Felbamate (Felbatol)");
        is_Other_Substance.Add("Fluoxetine/Olanzapine (Symbyax)");
        is_Other_Substance.Add("Lurasidone (Latuda)");
        is_Other_Substance.Add("Meprobamate (Equanil)");
        is_Other_Substance.Add("Pregabalin (Lyrica)");
        is_Other_Substance.Add("Quetiapine (Seroquel)");
        is_Other_Substance.Add("Sertraline (Zoloft)");
        is_Other_Substance.Add("Trazadone (Oleptro)");
        is_Other_Substance.Add("Zolpidem (Ambien)");






        var result_list = get_grid_value(p_source_object, "autopsy_report/toxicology/substance");



        foreach(var tuple in result_list)
        {


                string val_1 = null;

                if(!string.IsNullOrWhiteSpace(tuple.Item2?.ToString()))
                {
                    val_1 = tuple.Item2?.ToString();
                }
//mSubstAutop	MSubAuto1	Alcohol	1	autopsy_report/toxicology/substance= Substance that are mapped to ‘Alcohol‘ category in the substance categorization table below	autopsy_report/toxicology/substance = 'Alcohol'

            try
            {	
                //string val_1 = get_value(p_source_object, "autopsy_report/toxicology/substance");
                
                if(is_Alcohol.Contains(val_1))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mSubstAutop";
                    curr.field_id = "MSubAuto1";
                    curr.value = 1;

                    var key = $"{curr.indicator_id} {curr.field_id}";
                    if(this.indicators.ContainsKey(key) && this.indicators[key].value == 0)
                    {

                        this.indicators[key] = curr;
                    }

                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

//mSubstAutop	MSubAuto2	Amphetamine	2	autopsy_report/toxicology/substance= Substance that are mapped to ‘Amphetamine‘ category in the substance categorization table below	autopsy_report/toxicology/substance in ('Amphetamines, 'Methamphetamine')

            try
            {	
                //string val_1 = get_value(p_source_object, "autopsy_report/toxicology/substance");
                
                if(is_Amphetamine.Contains(val_1))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mSubstAutop";
                    curr.field_id = "MSubAuto2";
                    curr.value = 1;
                    var key = $"{curr.indicator_id} {curr.field_id}";
                    if(this.indicators.ContainsKey(key) && this.indicators[key].value == 0)
                    {

                        this.indicators[key] = curr;
                    }

                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

//mSubstAutop	MSubAuto3	Benzodiazepine	3	autopsy_report/toxicology/substance= Substance that are mapped to ‘Benzodiazepine‘ category  in the substance categorization table below	autopsy_report/toxicology/substance in ('Alprazolam (Xanax)', 'Aminoclonazepam', 'Chlordiazepoxide (Librium)', 'Clonazepam (Klonopin or Rivotril)', 'Diazepam (Valium)', 'Lorazepam (Ativan)', 'Temazepam (Restoril)', 'Zolpidem (Ambien)')

            try
            {	
                //string val_1 = get_value(p_source_object, "autopsy_report/toxicology/substance");
                
                if(is_Benzodiazepine.Contains(val_1))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mSubstAutop";
                    curr.field_id = "MSubAuto3";
                    curr.value = 1;
                    var key = $"{curr.indicator_id} {curr.field_id}";
                    if(this.indicators.ContainsKey(key) && this.indicators[key].value == 0)
                    {

                        this.indicators[key] = curr;
                    }

                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

//mSubstAutop	MSubAuto4	Buprenorphine/Methadone	4	autopsy_report/toxicology/substance= Substance that are mapped to ‘Buprenorphine/Methadone‘ category  in the substance categorization table below	autopsy_report/toxicology/substance in ('Buprenorphine', 'Methadone Hydrochloride')

            try
            {	
                //string val_1 = get_value(p_source_object, "autopsy_report/toxicology/substance");
                
                if(is_Buprenorphine_Methadone.Contains(val_1))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mSubstAutop";
                    curr.field_id = "MSubAuto4";
                    curr.value = 1;
                    var key = $"{curr.indicator_id} {curr.field_id}";
                    if(this.indicators.ContainsKey(key) && this.indicators[key].value == 0)
                    {

                        this.indicators[key] = curr;
                    }

                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

//mSubstAutop	MSubAuto5	Cocaine	5	autopsy_report/toxicology/substance= Substance that are mapped to ‘Cocaine‘category in the substance categorization table below	autopsy_report/toxicology/substance = 'Cocaine'

            try
            {	
                //string val_1 = get_value(p_source_object, "autopsy_report/toxicology/substance");
                
                if(is_Cocaine.Contains(val_1))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mSubstAutop";
                    curr.field_id = "MSubAuto5";
                    curr.value = 1;
                    var key = $"{curr.indicator_id} {curr.field_id}";
                    if(this.indicators.ContainsKey(key) && this.indicators[key].value == 0)
                    {

                        this.indicators[key] = curr;
                    }

                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

//mSubstAutop	MSubAuto6	Opioid (excl Buprenorphine/Methadone)	6	autopsy_report/toxicology/substance= Substance that are mapped to ‘Opioid (excl Buprenorphine/Methadone)‘  category  in the substance categorization table below	autopsy_report/toxicology/substance in ('Fentanyl', 'Heroin', 'Hydromorphone (Dilaudid)', 'Morphine Sulfate', 'Oxycodone Hydrochloride', 'Oxymorphone Hydrochloride (Opana)')

            try
            {	
                //string val_1 = get_value(p_source_object, "autopsy_report/toxicology/substance");
                
                if(is_Opioid.Contains(val_1))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mSubstAutop";
                    curr.field_id = "MSubAuto6";
                    curr.value = 1;
                    var key = $"{curr.indicator_id} {curr.field_id}";
                    if(this.indicators.ContainsKey(key) && this.indicators[key].value == 0)
                    {

                        this.indicators[key] = curr;
                    }

                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

//mSubstAutop	MSubAuto7	Substance with Other Chemical Classification	7	autopsy_report/toxicology/substance= Substance that are mapped to ‘Substance with Other Chemical Classification‘  category  in the substance categorization table below	autopsy_report/toxicology/substance in ('Acetaminophen', 'Acetazolamide (Diamox)', 'Aripiprazole (Abilify)', 'Carbamazepine (Neurontin)', 'Citalopram (Celexa)', 'Doxepin (Silenor, Zonalon, Prudoxin), 'Duloxetine (Cymbalta)', 'Felbamate (Felbatol)', 'Fluoxetine/Olanzapine (Symbyax)', 'Lurasidone (Latuda)', 'Meprobamate (Equanil)', 'Midazolam (Versed)', 'Pregabalin (Lyrica)', 'Quetiapine (Seroquel)', 'Sertraline (Zoloft)', 'Trazadone (Oleptro)')



            try
            {	
                //string val_1 = get_value(p_source_object, "autopsy_report/toxicology/substance");
                
                if(is_Other_Substance.Contains(val_1))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mSubstAutop";
                    curr.field_id = "MSubAuto7";
                    curr.value = 1;
                    var key = $"{curr.indicator_id} {curr.field_id}";
                    if(this.indicators.ContainsKey(key) && this.indicators[key].value == 0)
                    {

                        this.indicators[key] = curr;
                    }

                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

            try
            {	
                int test_int;
                
                //string val_1 = get_value(p_source_object, "autopsy_report/toxicology/substance");
                
                if(val_1 == null || string.IsNullOrWhiteSpace(val_1) ||  (val_1 != null && (string.IsNullOrWhiteSpace(val_1) || int.TryParse(val_1, out test_int) && test_int == blank_value )))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mSubstAutop";
                    curr.field_id = "MSubAuto8";
                    curr.value = 1;
                    var key = $"{curr.indicator_id} {curr.field_id}";
                    if(this.indicators.ContainsKey(key) && this.indicators[key].value == 0)
                    {

                        this.indicators[key] = curr;
                    }

                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

            try
            {	
                //string val_1 = get_value(p_source_object, "autopsy_report/toxicology/substance");
                
                if(val_1 != null && val_1.ToLower() == "Cannabinoid".ToLower() || is_Cannabinoid.Contains(val_1))
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mSubstAutop";
                    curr.field_id = "MSubAuto9";
                    curr.value = 1;
                    var key = $"{curr.indicator_id} {curr.field_id}";
                    if(this.indicators.ContainsKey(key) && this.indicators[key].value == 0)
                    {

                        this.indicators[key] = curr;
                    }

                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

            try
            {	
                int test_int;
                //string val_1 = get_value(p_source_object, "autopsy_report/toxicology/substance");
                
                if
                (
                    //val_1 == null ||
                    val_1 != null && 
                    !string.IsNullOrWhiteSpace(val_1) &&
                    !(int.TryParse(val_1, out test_int) && test_int == blank_value) &&
                    //val_1.ToLower() == "Other".ToLower() &&
                    !is_Alcohol.Contains(val_1) &&
                    !is_Amphetamine.Contains(val_1) &&
                    !is_Benzodiazepine.Contains(val_1) &&
                    !is_Buprenorphine_Methadone.Contains(val_1) &&
                    !is_Cannabinoid.Contains(val_1) &&
                    !is_Cocaine.Contains(val_1) &&
                    !is_Opioid.Contains(val_1) &&
                    !is_Other_Substance.Contains(val_1)

                )
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mSubstAutop";
                    curr.field_id = "MSubAuto10";
                    curr.value = 1;
                    var key = $"{curr.indicator_id} {curr.field_id}";
                    if(this.indicators.ContainsKey(key) && this.indicators[key].value == 0)
                    {

                        this.indicators[key] = curr;
                    }
                    
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

        }
    }


    private void popluate_mUndCofDeath (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        double test_double;

        var current_id = get_value(p_source_object, "_id").ToString();


        //mUndCofDeath MUndCofDeath22 22
/*
MUndCofDeath1 If /committee_review/pmss_mm= 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7, 10.8, 10.9, or 10.10
MUndCofDeath2 If /committee_review/pmss_mm= 20.1, 20.2, 20.4, 20.5, 20.6, 20.7, 20.8, 20.9, 20.10, or 20.11
MUndCofDeath3 If /committee_review/pmss_mm= 30.1 or 30.9
MUndCofDeath4 If /committee_review/pmss_mm=31.1
MUndCofDeath5 If /committee_review/pmss_mm=40.1, 50.1, or 60.1
MUndCofDeath6 If /committee_review/pmss_mm=70.1
MUndCofDeath7 If /committee_review/pmss_mm=80.1, 80.2, or 80.9
MUndCofDeath8 If /committee_review/pmss_mm=82.1 or 82.9
MUndCofDeath9 If /committee_review/pmss_mm=83.1 or 83.9
MUndCofDeath10 If /committee_review/pmss_mm=85.1
MUndCofDeath11 If /committee_review/pmss_mm=88.1, 88.2, or 88.9
MUndCofDeath12 If /committee_review/pmss_mm=89.1, 89.3, 89.9
MUndCofDeath13 If /committee_review/pmss_mm=90.1, 90.2, 90.3, 90.4, 90.5, 90.6, 90.7, 90.8, or 90.9
MUndCofDeath14 If /committee_review/pmss_mm=91.1, 91.2, 91.3, or 91.9
MUndCofDeath15 If /committee_review/pmss_mm=92.1 or 92.9
MUndCofDeath16 If /committee_review/pmss_mm=93.1 or 93.9
MUndCofDeath17 If /committee_review/pmss_mm=95.1
MUndCofDeath18 If /committee_review/pmss_mm=96.1, 96.2, or 96.9
MUndCofDeath19 If /committee_review/pmss_mm=97.1, 97.2, or  97.9
MUndCofDeath20 If /committee_review/pmss_mm=100.1, 100.2, 100.3, 100.4, 100.5, or 100.9
MUndCofDeath21 If /committee_review/pmss_mm=999.1
*/

HashSet<double> MUndCofDeath1 = new HashSet<double>() { 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7, 10.8, 10.9, 10.10};
HashSet<double> MUndCofDeath2 = new HashSet<double>() { 20.1, 20.2, 20.4, 20.5, 20.6, 20.7, 20.8, 20.9, 20.10, 20.11};
HashSet<double> MUndCofDeath3 = new HashSet<double>() { 30.1,30.9};
HashSet<double> MUndCofDeath4 = new HashSet<double>() {31.1};
HashSet<double> MUndCofDeath5 = new HashSet<double>() {40.1, 50.1, 60.1};
HashSet<double> MUndCofDeath6 = new HashSet<double>() {70.1};
HashSet<double> MUndCofDeath7 = new HashSet<double>() {80.1, 80.2,  80.9};
HashSet<double> MUndCofDeath8 = new HashSet<double>() {82.1 , 82.9};
HashSet<double> MUndCofDeath9 = new HashSet<double>() {83.1 , 83.9};
HashSet<double> MUndCofDeath10 = new HashSet<double>(){85.1};
HashSet<double> MUndCofDeath11 = new HashSet<double>(){88.1, 88.2,  88.9};
HashSet<double> MUndCofDeath12 = new HashSet<double>(){89.1, 89.3, 89.9};
HashSet<double> MUndCofDeath13 = new HashSet<double>(){90.1, 90.2, 90.3, 90.4, 90.5, 90.6, 90.7, 90.8, 90.9};
HashSet<double> MUndCofDeath14 = new HashSet<double>(){91.1, 91.2, 91.3, 91.9};
HashSet<double> MUndCofDeath15 = new HashSet<double>(){92.1, 92.9};
HashSet<double> MUndCofDeath16 = new HashSet<double>(){93.1, 93.9};
HashSet<double> MUndCofDeath17 = new HashSet<double>(){95.1};
HashSet<double> MUndCofDeath18 = new HashSet<double>(){96.1, 96.2, 96.9};
HashSet<double> MUndCofDeath19 = new HashSet<double>(){97.1, 97.2, 97.9};
HashSet<double> MUndCofDeath20 = new HashSet<double>(){100.1, 100.2, 100.3, 100.4, 100.5, 100.9};
HashSet<double> MUndCofDeath21 = new HashSet<double>(){999.1};

        var pmss_mm_string = string.Empty;
        double pmss_mm = double.NaN;
    
        object dynamic_val = get_value(p_source_object, "committee_review/pmss_mm");

        if(dynamic_val != null )
        {
            var object_val = dynamic_val as object;
            if(object_val!=null)
                pmss_mm_string = object_val.ToString();
        }

        if(double.TryParse(pmss_mm_string, out test_double))
        {
            pmss_mm = test_double;
        }
        
        try
        {	
            var (indicator_id, field_id) = pmss_mm switch
            {
                double v when MUndCofDeath1.Contains(v) => ("mUndCofDeath", "MUndCofDeath1"),
                double v when MUndCofDeath2.Contains(v) => ("mUndCofDeath", "MUndCofDeath2"),
                double v when MUndCofDeath3.Contains(v) => ("mUndCofDeath", "MUndCofDeath3"),
                double v when MUndCofDeath4.Contains(v) => ("mUndCofDeath", "MUndCofDeath4"),
                double v when MUndCofDeath5.Contains(v) => ("mUndCofDeath", "MUndCofDeath5"),
                double v when MUndCofDeath6.Contains(v) => ("mUndCofDeath", "MUndCofDeath6"),
                double v when MUndCofDeath7.Contains(v) => ("mUndCofDeath", "MUndCofDeath7"),
                double v when MUndCofDeath8.Contains(v) => ("mUndCofDeath", "MUndCofDeath8"),
                double v when MUndCofDeath9.Contains(v) => ("mUndCofDeath", "MUndCofDeath9"),
                double v when MUndCofDeath10.Contains(v) => ("mUndCofDeath", "MUndCofDeath10"),
                double v when MUndCofDeath11.Contains(v) => ("mUndCofDeath", "MUndCofDeath11"),
                double v when MUndCofDeath12.Contains(v) => ("mUndCofDeath", "MUndCofDeath12"),
                double v when MUndCofDeath13.Contains(v) => ("mUndCofDeath", "MUndCofDeath13"),
                double v when MUndCofDeath14.Contains(v) => ("mUndCofDeath", "MUndCofDeath14"),
                double v when MUndCofDeath15.Contains(v) => ("mUndCofDeath", "MUndCofDeath15"),
                double v when MUndCofDeath16.Contains(v) => ("mUndCofDeath", "MUndCofDeath16"),
                double v when MUndCofDeath17.Contains(v) => ("mUndCofDeath", "MUndCofDeath17"),
                double v when MUndCofDeath18.Contains(v) => ("mUndCofDeath", "MUndCofDeath18"),
                double v when MUndCofDeath19.Contains(v) => ("mUndCofDeath", "MUndCofDeath19"),
                double v when MUndCofDeath20.Contains(v) => ("mUndCofDeath", "MUndCofDeath20"),
                double v when MUndCofDeath21.Contains(v) => ("mUndCofDeath", "MUndCofDeath21"),
                9999D => ("mUndCofDeath", "MUndCofDeath22"),
                double.NaN => ("mUndCofDeath", "MUndCofDeath22"),
                _ =>  ("", "")
            };


            if(!string.IsNullOrWhiteSpace(indicator_id))     
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = indicator_id;
                curr.field_id = field_id;
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }



    } 

    private void popluate_mDeathPrevent (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        int test_int;

        //mDeathPrevent MDeathPrevent4 4

        var was_this_death_preventable = string.Empty;
        var chance_to_alter_outcome = -1;;

        object dynamic_val = get_value(p_source_object, "committee_review/was_this_death_preventable");

        if(dynamic_val != null )
        {
            var object_val = dynamic_val as object;
            if(object_val!=null)
                was_this_death_preventable = object_val.ToString();
        }

        dynamic_val = get_value(p_source_object, "committee_review/chance_to_alter_outcome");

        if(dynamic_val != null )
        {
            var object_val = dynamic_val as object;
            if
            (
                object_val != null &&
                int.TryParse(object_val.ToString(), out test_int)
            )
            {
                chance_to_alter_outcome = test_int;
            }
        }

        if(string.IsNullOrEmpty(was_this_death_preventable))
        {
            was_this_death_preventable = "9999";
        }
            
/*
committee_review/was_this_death_preventable=1 (Yes) OR 
committee_review/chance_to_alter_outcome=0 (Good Chance) OR 
committee_review/chance_to_alter_outcome=1 (Some Chance
*/

        var is_preventable = false;

            try
            {	
                if
                (
                    was_this_death_preventable == "1" || 
                    chance_to_alter_outcome == 0 ||
                    chance_to_alter_outcome == 1
                )
                {
                    is_preventable = true;
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mDeathPrevent";
                    curr.field_id = "MDeathPrevent1";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
/*
committee_review/was_this_death_preventable=0 (No) OR
committee_review/chance_to_alter_outcome=2 (No Chance)
*/
            try
            {	
                if
                (
                    !is_preventable &&
                    (
                        was_this_death_preventable == "0" || 
                        chance_to_alter_outcome == 2
                    )
                )
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mDeathPrevent";
                    curr.field_id = "MDeathPrevent2";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
/*
committee_review/was_this_death_preventable=9999 (Blank) AND 
committee_review/chance_to_alter_outcome=3 (Unable to Determine)
*/

            try
            {	
                if
                (
                    was_this_death_preventable == "9999" && 
                    chance_to_alter_outcome == 3
                )
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mDeathPrevent";
                    curr.field_id = "MDeathPrevent3";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
        
/*
/committee_review/was_this_death_preventable=9999 (Blank) AND 
/committee_review/chance_to_alter_outcome=9999 (Blank)
*/
        
            try
            {	
                if
                (
                    was_this_death_preventable == "9999" && 
                    (
                        chance_to_alter_outcome == 9999 ||
                        chance_to_alter_outcome == -1
                    )
                )
                {
                    var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                    curr.indicator_id = "mDeathPrevent";
                    curr.field_id = "MDeathPrevent4";
                    curr.value = 1;
                    this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
                }
                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }

    }

    private void popluate_mOMBRaceRcd (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        int test_int;


//birth_fetal_death_certificate_parent/race/omb_race_recode=0 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 0)
//birth_fetal_death_certificate_parent/race/omb_race_recode=1 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 1)
//birth_fetal_death_certificate_parent/race/omb_race_recode=2 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 2)
//birth_fetal_death_certificate_parent/race/omb_race_recode=3 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 3)
//birth_fetal_death_certificate_parent/race/omb_race_recode=4 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 4)
//birth_fetal_death_certificate_parent/race/omb_race_recode=5 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 5)
//birth_fetal_death_certificate_parent/race/omb_race_recode=6 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 6)
//birth_fetal_death_certificate_parent/race/omb_race_recode=14 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 14)
//birth_fetal_death_certificate_parent/race/omb_race_recode=8888 AND death_certificate/race/omb_race_recode = 8888
//birth_fetal_death_certificate_parent/race/omb_race_recode=9999 AND death_certificate/race/omb_race_recode = 9999


        var birth_fetal_death_certificate_parent_race_omb_race_recode_string = string.Empty;
        var death_certificate_race_race_string = string.Empty;

        var birth_fetal_death_certificate_parent_race_race_of_mother = 9999;
        var death_certificate_race_race = 9999;

        object dynamic_val = get_value(p_source_object, "birth_fetal_death_certificate_parent/race/omb_race_recode");
        
        if(dynamic_val != null)
        {
            var object_val = dynamic_val as object;
            if ( object_val!=null )
                birth_fetal_death_certificate_parent_race_omb_race_recode_string = object_val.ToString();
        }

        dynamic_val = get_value(p_source_object, "death_certificate/race/omb_race_recode");
        if(dynamic_val != null)
        {
            var object_val = dynamic_val as object;
            if ( object_val!= null )
                death_certificate_race_race_string = object_val.ToString();
        }

        if(!string.IsNullOrWhiteSpace(birth_fetal_death_certificate_parent_race_omb_race_recode_string))
        {
            if(int.TryParse(birth_fetal_death_certificate_parent_race_omb_race_recode_string, out test_int))
            {
                birth_fetal_death_certificate_parent_race_race_of_mother = test_int;
            }
            /*
            else
            {
                birth_fetal_death_certificate_parent_race_race_of_mother = 9999;
            }*/
            
        }

        if(!string.IsNullOrWhiteSpace(death_certificate_race_race_string))
        {
            if(int.TryParse(death_certificate_race_race_string, out test_int))
            {
                death_certificate_race_race = test_int;
            }
            /*
            else
            {
                death_certificate_race_race = 9999;
            }*/
            
        }

/*

Omb Recode Race:

if bfdcpr_or_recod=0 or (bfdcpr_or_recod in (8888,9999) and dcr_or_recod=0) then OMB='White';
    if bfdcpr_or_recod=1 or (bfdcpr_or_recod in (8888,9999) and dcr_or_recod=1) then OMB='Black or African American';
    if bfdcpr_or_recod=2 or (bfdcpr_or_recod in (8888,9999) and dcr_or_recod=2) then OMB='American Indian or Alaska Native';
    if bfdcpr_or_recod=3 or (bfdcpr_or_recod in (8888,9999) and dcr_or_recod=3) then OMB='Native Hawaiian or Pacific Islander';
    if bfdcpr_or_recod=4 or (bfdcpr_or_recod in (8888,9999) and dcr_or_recod=4) then OMB='Asian';
    if bfdcpr_or_recod=5 or (bfdcpr_or_recod in (8888,9999) and dcr_or_recod=5) then OMB='Bi-Racial';
    if bfdcpr_or_recod=6 or (bfdcpr_or_recod in (8888,9999) and dcr_or_recod=6) then OMB='Multi-Racial';
    if bfdcpr_or_recod=14 or (bfdcpr_or_recod in (8888,9999) and dcr_or_recod=14) then OMB='Other Race';
    if bfdcpr_or_recod=8888 and dcr_or_recod=8888 then OMB='Race Not Specified';



*/



        try
        {	
            var (indicator_id, field_id) = (birth_fetal_death_certificate_parent_race_race_of_mother, death_certificate_race_race) switch
            {
                //birth_fetal_death_certificate_parent/race/omb_race_recode=0 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 0)
                (int b, int d) when b==0 || ((b == 8888 || b == 9999) && d== 0 ) => ("mOMBRaceRcd", "MOMBRaceRcd1"),
                //birth_fetal_death_certificate_parent/race/omb_race_recode=1 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 1)
                (int b, int d) when b==1 || ((b == 8888 || b == 9999) && d== 1 ) => ("mOMBRaceRcd", "MOMBRaceRcd2"),
                //birth_fetal_death_certificate_parent/race/omb_race_recode=2 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 2)
                (int b, int d) when b==2 || ((b == 8888 || b == 9999) && d== 2 ) => ("mOMBRaceRcd", "MOMBRaceRcd3"),
                //birth_fetal_death_certificate_parent/race/omb_race_recode=3 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 3)
                (int b, int d) when b==3 || ((b == 8888 || b == 9999) && d== 3 ) => ("mOMBRaceRcd", "MOMBRaceRcd4"),
                //birth_fetal_death_certificate_parent/race/omb_race_recode=4 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 4)
                (int b, int d) when b==4 || ((b == 8888 || b == 9999) && d== 4 ) => ("mOMBRaceRcd", "MOMBRaceRcd5"),
                //birth_fetal_death_certificate_parent/race/omb_race_recode=5 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 5)
                (int b, int d) when b==5 || ((b == 8888 || b == 9999) && d== 5 ) => ("mOMBRaceRcd", "MOMBRaceRcd6"),
                //birth_fetal_death_certificate_parent/race/omb_race_recode=6 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 6)
                (int b, int d) when b==6 || ((b == 8888 || b == 9999) && d== 6 ) => ("mOMBRaceRcd", "MOMBRaceRcd7"),
                //birth_fetal_death_certificate_parent/race/omb_race_recode=14 OR (birth_fetal_death_certificate_parent/race/omb_race_recode in (8888, 9999) AND death_certificate/race/omb_race_recode = 14)
                (int b, int d) when b==14 || ((b == 8888 || b == 9999) && d== 14 ) => ("mOMBRaceRcd", "MOMBRaceRcd8"),
                //birth_fetal_death_certificate_parent/race/omb_race_recode=8888 AND death_certificate/race/omb_race_recode = 8888
                (int b, int d) when b==8888 && d== 8888 => ("mOMBRaceRcd", "MOMBRaceRcd9"),
                //birth_fetal_death_certificate_parent/race/omb_race_recode=9999 AND death_certificate/race/omb_race_recode = 9999
                (int b, int d) when b==9999 && d== 9999 => ("mOMBRaceRcd", "MOMBRaceRcd10"),

            _ =>  ("", "")
            };

            if(!string.IsNullOrWhiteSpace(indicator_id))     
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = indicator_id;
                curr.field_id = field_id;
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
        
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

    }

    private void popluate_mDeathbyRace (ref List<mmria.server.model.opioid_report_value_struct> p_opioid_report_value_list, ref mmria.server.model.opioid_report_value_struct p_opioid_report_value, ref mmria.server.model.c_opioid_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        int test_int;


        var birth_fetal_death_certificate_parent_race_race_of_mother_object_list = new List<object>();
        var death_certificate_race_race_object_list = new List<object>();

        var birth_fetal_death_certificate_parent_race_race_of_mother = new HashSet<int>();
        var death_certificate_race_race = new HashSet<int>();

        /*

        string current_id = get_value(p_source_object, "_id").ToString();
        
var id_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
"22ddb666-7a43-49d5-94a5-22475ced0593",
"3851677b-91d2-46e9-af35-9bf9c0e53d72",
"3937b3bd-0746-4d24-8bf4-8ca63ce0e153",
"983deae6-361b-4295-89fb-fc0de82f16cb",
"a53c843b-e7f9-4ea6-9237-0c9a00105fe6",
"d662697f-b0e3-4624-b653-4c7f3808e775",
};

        if(id_set.Contains(current_id))
        {

        }
*/



        object dynamic_val = get_value(p_source_object, "birth_fetal_death_certificate_parent/race/race_of_mother");
        

        if(dynamic_val != null)
        {
            var object_val = dynamic_val as object;
            birth_fetal_death_certificate_parent_race_race_of_mother_object_list = object_val as List<object>;
        }

        dynamic_val = get_value(p_source_object, "death_certificate/race/race");
        if(dynamic_val != null)
        {
            var object_val = dynamic_val as object;
            death_certificate_race_race_object_list = object_val as List<object>;
        }

        if(birth_fetal_death_certificate_parent_race_race_of_mother_object_list!=null)
        {
            foreach(var item in birth_fetal_death_certificate_parent_race_race_of_mother_object_list)
            {
                if
                (
                    item != null &&
                    !string.IsNullOrWhiteSpace(item.ToString()) &&
                    int.TryParse(item.ToString(), out test_int)                      
                )
                {
                    birth_fetal_death_certificate_parent_race_race_of_mother.Add(test_int);
                }
            }
            
        }

        if(death_certificate_race_race_object_list!= null)
        {
            foreach(var item in death_certificate_race_race_object_list)
            {
                if
                (
                    item != null &&
                    !string.IsNullOrWhiteSpace(item.ToString()) &&
                    int.TryParse(item.ToString(), out test_int)                      
                )
                {
                    death_certificate_race_race.Add(test_int);
                }
            }
            
            
        }

/*
mDeathbyRace MDeathbyRace17

/birth_fetal_death_certificate_parent/race/race_of_mother=0 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 0)
/birth_fetal_death_certificate_parent/race/race_of_mother=1 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 1)
/birth_fetal_death_certificate_parent/race/race_of_mother=2 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 2)
/birth_fetal_death_certificate_parent/race/race_of_mother=3 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 3)
/birth_fetal_death_certificate_parent/race/race_of_mother=4 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 4)
/birth_fetal_death_certificate_parent/race/race_of_mother=5 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 5)
/birth_fetal_death_certificate_parent/race/race_of_mother=6 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 6)
/birth_fetal_death_certificate_parent/race/race_of_mother=7 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 7)
/birth_fetal_death_certificate_parent/race/race_of_mother=8 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 8)
/birth_fetal_death_certificate_parent/race/race_of_mother=9 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 9)
/birth_fetal_death_certificate_parent/race/race_of_mother=10 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 10)
/birth_fetal_death_certificate_parent/race/race_of_mother=11 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 11)
/birth_fetal_death_certificate_parent/race/race_of_mother=12 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 12)
/birth_fetal_death_certificate_parent/race/race_of_mother=13 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 13)
/birth_fetal_death_certificate_parent/race/race_of_mother=14 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 14)
/birth_fetal_death_certificate_parent/race/race_of_mother=8888 AND /death_certificate/race/race= 8888
/birth_fetal_death_certificate_parent/race/race_of_mother=9999 AND /death_certificate/race/race= 9999



            Race: mDeathbyRace


if bfdcpr_ro_mothe='White' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='White') then race='White';
if bfdcpr_ro_mothe='Black or African American' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Black or African American') then race='Black or African American';
if bfdcpr_ro_mothe='American Indian or Alaska Native' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='American Indian or Alaska Native') then race='American Indian or Alaska Native';
    if bfdcpr_ro_mothe='Native Hawaiian' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Native Hawaiian') then race='Native Hawaiian';
    if bfdcpr_ro_mothe='Guamanian or Chamorro' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Guamanian or Chamorro') then race='Guamanian or Chamorro';
    if bfdcpr_ro_mothe='Samoan' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Samoan') then race='Samoan';
    if bfdcpr_ro_mothe='Other Pacific Islander' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Other Pacific Islander') then race='Other Pacific Islander';
    if bfdcpr_ro_mothe='Asian Indian' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Asian Indian') then race='Asian Indian';
    if bfdcpr_ro_mothe='Chinese' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Chinese') then race='Chinese';
    if bfdcpr_ro_mothe='Filipino' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Filipino') then race='Filipino';
    if bfdcpr_ro_mothe='Japanese' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Japanese') then race='Japanese';
    if bfdcpr_ro_mothe='Korean' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Korean') then race='Korean';
    if bfdcpr_ro_mothe='Vietnamese' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Vietnamese') then race='Vietnamese';
    if bfdcpr_ro_mothe='Other Asian' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Other Asian') then race='Other Asian';
    if bfdcpr_ro_mothe='Other Race' or (bfdcpr_ro_mothe in ('Race Not Specified','(blank)') and dcr_race='Other Race') then race='Other Race';
    if bfdcpr_ro_mothe='Race Not Specified' and dcr_race='Race Not Specified' then race='Race Not Specified';

*/



        
        try
        {	
            var (b, d) = (birth_fetal_death_certificate_parent_race_race_of_mother, death_certificate_race_race);
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=0 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 0)
            if(b.Contains(0) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(0)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace1";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
    

            //birth_fetal_death_certificate_parent/race/race_of_mother=1 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 1)
            if(b.Contains(1) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(1)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace2";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=2 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 2)
            if(b.Contains(2) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(2)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace3";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            //birth_fetal_death_certificate_parent/race/race_of_mother=3 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 3)
            if(b.Contains(3) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(3)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace4";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=4 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 4)
            if(b.Contains(4) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(4)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace5";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=5 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 5)
            if(b.Contains(5) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(5)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace6";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=6 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 6)
            if(b.Contains(6) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(6)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace7";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=7 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 7)
            if(b.Contains(7) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(7)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace8";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=8 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 8)
            if(b.Contains(8) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(8)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace9";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            //birth_fetal_death_certificate_parent/race/race_of_mother=9 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 9)
            if(b.Contains(9) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(9)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace10";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=10 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 10)
            if(b.Contains(10) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(10)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace11";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=11 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 11)
            if(b.Contains(11) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(11)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace12";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=12 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 12)
            if(b.Contains(12) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(12)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace13";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=13 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 13)
            if(b.Contains(13) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(13)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id =  "MDeathbyRace14";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=14 OR (/birth_fetal_death_certificate_parent/race/race_of_mother in (8888, 9999) AND /death_certificate/race/race= 14)
            if(b.Contains(14) || ((b.Count == 0 || b.Contains(8888) || b.Contains(9999)) && d.Contains(14)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace15";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

            //birth_fetal_death_certificate_parent/race/race_of_mother=8888 AND /death_certificate/race/race= 8888
            if(b.Contains(8888) && d.Contains(8888))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace16";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }
            
            //birth_fetal_death_certificate_parent/race/race_of_mother=9999 AND /death_certificate/race/race= 9999
            if((b.Count == 0 || b.Contains(9999)) && (d.Count == 0 || d.Contains(9999)))
            {           
                var  curr = initialize_opioid_report_value_struct(p_opioid_report_value);
                curr.indicator_id = "mDeathbyRace";
                curr.field_id = "MDeathbyRace17";
                curr.value = 1;
                this.indicators[$"{curr.indicator_id} {curr.field_id}"] = curr;
            }

    
            
        }
        catch(Exception ex)
        {
            System.Console.WriteLine (ex);
        }

    }       

}


