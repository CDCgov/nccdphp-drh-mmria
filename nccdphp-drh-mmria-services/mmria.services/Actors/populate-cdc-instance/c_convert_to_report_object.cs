using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.server.utils;

public sealed partial class c_convert_to_report_object
{
    string source_json;

    private System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> List_Look_Up;


    common.couchdb.DBConfigurationDetail connection;

    string metadata_release_version_name;
    private enum deaths_by_age_enum
    {
        blank,
        age_less_than_20,
        age_20_to_24,
        age_25_to_29,
        age_30_to_34,
        age_35_to_44,
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
        other
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

    public c_convert_to_report_object (string p_source_json, common.couchdb.DBConfigurationDetail p_connection, string p_metadata_release_version_name)
    {

        source_json = p_source_json;
        connection = p_connection;
        metadata_release_version_name = p_metadata_release_version_name;
    }



    public string execute ()
    {
        string result = null;
        //Get_Value_Result value_result = null;

        string metadata_url = connection.url + $"/metadata/version_specification-{metadata_release_version_name}/metadata";
        var metadata_curl = new mmria.getset.cURL("GET", null, metadata_url, null, connection.user_name, connection.user_value);
        mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_curl.execute());


        List_Look_Up = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach(var child in metadata.children)
        {
            Get_List_Look_Up(List_Look_Up, metadata.lookup, child, "/" + child.name);
        }



        mmria.server.model.c_report_object report_object;

        System.Dynamic.ExpandoObject source_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (source_json);
        //dynamic source_object = Newtonsoft.Json.Linq.JObject.Parse(source_json);

        report_object = new mmria.server.model.c_report_object ();
        report_object._id = get_value (source_object, "_id").result?.ToString();


        


        /*
        if (report_object._id == "02279162-6be3-49e4-930f-42eed7cd4706")
        {
            System.Console.Write("break");
        }*/

        object val = null;

        try
        {
            val = get_value(source_object, "home_record/date_of_death/year").result;
            if(val != null && val.ToString() != "")
            {
                report_object.year_of_death = System.Convert.ToInt32(val);
            }
        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }


        try
        {
            val = get_value(source_object, "committee_review/date_of_review").result;
            if(val != null && val.ToString() != "")
            {
                report_object.year_of_case_review = System.Convert.ToDateTime(val).Year;
                report_object.month_of_case_review = System.Convert.ToDateTime(val).Month;
            }
        }
        catch(Exception)
        {
            //System.Console.WriteLine (ex);
        }


        this.popluate_total_number_of_cases_by_pregnancy_relatedness (ref report_object, source_object);
        this.popluate_total_number_of_pregnancy_related_deaths_by_ethnicity(ref report_object, source_object);
        this.popluate_total_number_of_pregnancy_associated_deaths_by_ethnicity (ref report_object, source_object);
        
        this.popluate_pregnancy_deaths_by_age(ref report_object, source_object);
        this.popluate_pregnancy_deaths_by_pregnant_at_time_of_death(ref report_object, source_object);

        //this.popluate_distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm(ref report_object, source_object);
        this.popluate_list(ref report_object.distribution_of_underlying_cause_of_pregnancy_related_death_pmss_mm, ref report_object, source_object, "committee_review/pmss_mm", true);



        //this.popluate_pregnancy_related_determined_to_be_preventable(ref report_object, source_object);
        //this.popluate_pregnancy_associated_determined_to_be_preventable(ref report_object, source_object);
        this.popluate_list(ref report_object.total_pregnancy_related_determined_to_be_preventable, ref report_object, source_object, "committee_review/was_this_death_preventable", true);
        this.popluate_list(ref report_object.total_pregnancy_associated_determined_to_be_preventable, ref report_object, source_object, "committee_review/was_this_death_preventable", false);


        //this.popluate_pregnancy_related_obesity_contributed_to_the_death(ref report_object, source_object);
        //this.popluate_pregnancy_associated_obesity_contributed_to_the_death(ref report_object, source_object);
        this.popluate_list(ref report_object.total_pregnancy_related_obesity_contributed_to_the_death, ref report_object, source_object, "committee_review/did_obesity_contribute_to_the_death", true);
        this.popluate_list(ref report_object.total_pregnancy_associated_obesity_contributed_to_the_death, ref report_object, source_object, "committee_review/did_obesity_contribute_to_the_death", false);


        //this.popluate_pregnancy_related_mental_health_conditions_contributed_to_death(ref report_object, source_object);
        //this.popluate_pregnancy_associated_mental_health_conditions_contributed_to_death(ref report_object, source_object);
        this.popluate_list(ref report_object.total_pregnancy_related_mental_health_conditions_contributed_to_death, ref report_object, source_object, "committee_review/did_mental_health_conditions_contribute_to_the_death", true);
        this.popluate_list(ref report_object.total_pregnancy_associated_mental_health_conditions_contributed_to_death, ref report_object, source_object, "committee_review/did_mental_health_conditions_contribute_to_the_death", false);



        //this.popluate_pregnancy_related_substance_use_disorder_contributed_to_death(ref report_object, source_object);
        //this.popluate_pregnancy_associated_substance_use_disorder_contributed_to_death(ref report_object, source_object);
        this.popluate_list(ref report_object.total_pregnancy_related_substance_use_disorder_contributed_to_death, ref report_object, source_object, "committee_review/did_substance_use_disorder_contribute_to_the_death", true);
        this.popluate_list(ref report_object.total_pregnancy_associated_substance_use_disorder_contributed_to_death, ref report_object, source_object, "committee_review/did_substance_use_disorder_contribute_to_the_death", false);


        //this.popluate_pregnancy_related_is_suicide(ref report_object, source_object);
        //this.popluate_pregnancy_associated_is_suicide(ref report_object, source_object);
        this.popluate_list(ref report_object.total_pregnancy_related_is_suicide, ref report_object, source_object, "committee_review/was_this_death_a_sucide", true);
        this.popluate_list(ref report_object.total_pregnancy_associated_is_suicide, ref report_object, source_object, "committee_review/was_this_death_a_sucide", false);

        //this.popluate_pregnancy_related_is_homocide(ref report_object, source_object);
        //this.popluate_pregnancy_associated_is_homocide(ref report_object, source_object);
        this.popluate_list(ref report_object.total_pregnancy_related_is_homocide, ref report_object, source_object, "committee_review/was_this_death_a_homicide", true);
        this.popluate_list(ref report_object.total_pregnancy_associated_is_homocide, ref report_object, source_object, "committee_review/was_this_death_a_homicide", false);


        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        //settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        result = Newtonsoft.Json.JsonConvert.SerializeObject(report_object, settings);

        return result;
    }


    private class Get_Value_Result
    {
        public Get_Value_Result(){}

        public object result { get; set;}

        public bool is_erorr { get; set;}
    }

    private Get_Value_Result get_value(System.Dynamic.ExpandoObject p_object, string p_path, string p_data_type = "string")
    {
        object result = null;
        bool is_error = false;

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
                            return new Get_Value_Result(){ result = result, is_erorr = is_error};;
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
                            return new Get_Value_Result(){ result = result, is_erorr = is_error};;
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
                    }
                    */
                }
                else
                {
                    //System.Console.WriteLine("This should not happen. {0}", p_path);
                }
            }
        }
        catch (Exception ex)
        {
            is_error = true;
            System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }

        if(is_error)
        {
            return new Get_Value_Result(){ result = null, is_erorr = is_error};
        }
        else
        {

            return new Get_Value_Result(){ result = result, is_erorr = is_error};
        }

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


        

    private pregnant_at_time_of_death_enum get_pregnant_at_time_of_death_classifier (System.Dynamic.ExpandoObject p_source_object)
    {

        pregnant_at_time_of_death_enum result = pregnant_at_time_of_death_enum.blank;

        
/*
        pregnant_at_the_time_of_death,
birth_fetal_death_certificate_parent/length_between_child_birth_and_death_of_mother = 0; 
OR death_certificate/pregnancy_status = pregnant at time of death

        pregnant_within_42_days_of_death,
birth_fetal_death_certificate_parent/length_between_child_birth_and_death_of_mother = 1-42;
OR death_certificate/pregnancy_status = Pregnant within 42 days of death

        pregnant_within_43_to_365_days_of_death

birth_fetal_death_certificate_parent/length_between_child_birth_and_death_of_mother = 43-365; 
OR death_certificate/pregnancy_status = Pregnant 43 to 365 days of death

        blank,
        pregnant_at_the_time_of_death,
        pregnant_within_42_days_of_death,
        pregnant_within_43_to_365_days_of_death
*/

        var get_value_result = get_value (p_source_object, "birth_fetal_death_certificate_parent/length_between_child_birth_and_death_of_mother");
        object length = null;

        if(!get_value_result.is_erorr)
        {
            length = get_value_result.result;
        }
        
        int length_test = -1;

        if (length != null) int.TryParse (length.ToString (), out length_test);

        string status = null;
        get_value_result = get_value (p_source_object, "death_certificate/pregnancy_status");
        if(!get_value_result.is_erorr)
        {
            status = get_value_result.result?.ToString();
        }

        if (status == null) status = "";

        if 
        (
            (length_test == 0) || 
            status.Equals ("pregnant at time of death", StringComparison.InvariantCultureIgnoreCase)
        )
        {
            result = pregnant_at_time_of_death_enum.pregnant_at_the_time_of_death;
        }
        else if
        (
            (length_test >= 1 && length_test <= 42) ||
            status.Equals ("pregnant within 42 days of death", StringComparison.InvariantCultureIgnoreCase)
        )
        {
            result = pregnant_at_time_of_death_enum.pregnant_within_42_days_of_death;
        }
        else if
        (
            (length_test >= 43 && length_test <= 365) ||
            status.Equals ("Pregnant 43 to 365 days of death", StringComparison.InvariantCultureIgnoreCase)
        )
        {
            result = pregnant_at_time_of_death_enum.pregnant_within_43_to_365_days_of_death;
        }
        

        return result;
    }

    private deaths_by_age_enum get_age_classifier (System.Dynamic.ExpandoObject p_source_object)
    {

        deaths_by_age_enum result = deaths_by_age_enum.blank;
        
        object val = null;

        var get_value_result = get_value (p_source_object, "death_certificate/demographics/age");
        if(!get_value_result.is_erorr)
        {
            val = get_value_result.result;
        }

        int value_test = 0;
        if (val != null && int.TryParse (val.ToString (), out value_test))
        {
            if(value_test < 20) result = deaths_by_age_enum.age_less_than_20;
            else if(value_test < 20) result = deaths_by_age_enum.age_less_than_20;
            else if(value_test >= 20 && value_test <= 24) result = deaths_by_age_enum.age_20_to_24;
            else if(value_test >= 25 && value_test <= 29)  result = deaths_by_age_enum.age_25_to_29;
            else if(value_test >= 30 && value_test <= 34)  result = deaths_by_age_enum.age_30_to_34;
            else if(value_test >= 35 && value_test <= 44)  result = deaths_by_age_enum.age_35_to_44;
            else if(value_test >= 45)  result = deaths_by_age_enum.age_45_and_above;
        }

        return result;
    }


    private HashSet<ethnicity_enum> get_ethnicity_classifier (System.Dynamic.ExpandoObject p_source_object)
    {
        HashSet<ethnicity_enum> result = new HashSet<ethnicity_enum> ();

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


        var get_value_result = get_value (p_source_object, "birth_fetal_death_certificate_parent/demographic_of_mother/is_of_hispanic_origin");
        if(! get_value_result.is_erorr)
        {
            val = get_value_result.result?.ToString();
        }

        if (val != null)
        {
            if (bc_hispanic_origin.Contains (val))
            {
                result.Add (ethnicity_enum.hispanic);
            }
            else 
            {
                get_value_result = get_value (p_source_object, "death_certificate/demographics/is_of_hispanic_origin");
                if(! get_value_result.is_erorr)
                {
                    val = get_value_result.result?.ToString();
                }
                
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


    private void popluate_pregnancy_deaths_by_pregnant_at_time_of_death(ref mmria.server.model.c_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

/*

1
committee_review/pregnancy_relatedness = Pregnancy Related; 
AND
birth_fetal_death_certificate_parent/
length_between_child_birth_and_death_of_mother = 0;
OR death_certificate/pregnancy_status = pregnant at time of death



2
committee_review/pregnancy_relatedness = Pregnancy Related; 
AND
birth_fetal_death_certificate_parent/
length_between_child_birth_and_death_of_mother = 1-42;
OR death_certificate/pregnancy_status = Pregnant within 42 days of death

3
committee_review/pregnancy_relatedness = Pregnancy Related; 
AND
birth_fetal_death_certificate_parent/
length_between_child_birth_and_death_of_mother = 43-365;
OR death_certificate/pregnancy_status = Pregnant 43 to 365 days of death


length_between_child_birth_and_death_of_mother <- number field
length_between_child_birth_and_death_of_mother = 0
length_between_child_birth_and_death_of_mother = 1-42
length_between_child_birth_and_death_of_mother = 43-365


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

        object length_between_child_birth_and_death_of_mother_dynamic = null;

        var get_value_result = get_value(p_source_object, "birth_fetal_death_certificate_parent/length_between_child_birth_and_death_of_mother");
        
        if(! get_value_result.is_erorr)
        {
            length_between_child_birth_and_death_of_mother_dynamic = get_value_result.result;
        }
        
        int length_between_child_birth_and_death_of_mother =  -1;
        if(length_between_child_birth_and_death_of_mother_dynamic is string)
        {
            string length_between_child_birth_and_death_of_mother_string = length_between_child_birth_and_death_of_mother_dynamic as string;
            if(!int.TryParse(length_between_child_birth_and_death_of_mother_string, out length_between_child_birth_and_death_of_mother))
            {
                length_between_child_birth_and_death_of_mother = -1;
            }
        }
        else if(length_between_child_birth_and_death_of_mother_dynamic is Int64)
        {
            length_between_child_birth_and_death_of_mother = (int) length_between_child_birth_and_death_of_mother_dynamic;
        }

        
        if(length_between_child_birth_and_death_of_mother < -1)
        {
            length_between_child_birth_and_death_of_mother = -1;
        }

        string pregnancy_status_string = null;
        get_value_result = get_value(p_source_object, "death_certificate/death_information/pregnancy_status");
        if(! get_value_result.is_erorr)
        {
            pregnancy_status_string = get_value_result.result?.ToString();
        }
        
        
        int pregnancy_status = -1;
        if(string.IsNullOrWhiteSpace(pregnancy_status_string) || !int.TryParse(pregnancy_status_string, out pregnancy_status))
        {
            pregnancy_status = -1;
        }


        if
        (
            length_between_child_birth_and_death_of_mother == 0 
        )
        {
            if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1)
            {
                p_report_object.total_number_pregnancy_related_at_time_of_death.pregnant_at_the_time_of_death = 1;
            }
            else if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
            {
                p_report_object.total_number_pregnancy_associated_at_time_of_death.pregnant_at_the_time_of_death = 1;
            }
        }
        else if
        (
            length_between_child_birth_and_death_of_mother == -1 &&
            pregnancy_status == 1

        )
        {
            if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1)
            {
                p_report_object.total_number_pregnancy_related_at_time_of_death.pregnant_at_the_time_of_death = 1;
            }
            else if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
            {
                p_report_object.total_number_pregnancy_associated_at_time_of_death.pregnant_at_the_time_of_death = 1;
            }
        }

        else if
        (
            (
                length_between_child_birth_and_death_of_mother >= 1 &&
                length_between_child_birth_and_death_of_mother <= 42 
            )
        )
        {
            if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1)
            {
                p_report_object.total_number_pregnancy_related_at_time_of_death.pregnant_within_42_days_of_death = 1;
            }
            else if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
            {
                p_report_object.total_number_pregnancy_associated_at_time_of_death.pregnant_within_42_days_of_death = 1;
            }
        }
        else if
        (
            length_between_child_birth_and_death_of_mother == -1 &&
            pregnancy_status == 2

        )
        {
            if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1)
            {
                p_report_object.total_number_pregnancy_related_at_time_of_death.pregnant_within_42_days_of_death = 1;
            }
            else if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
            {
                p_report_object.total_number_pregnancy_associated_at_time_of_death.pregnant_within_42_days_of_death = 1;
            }
        }
        else if
        (
            (
                length_between_child_birth_and_death_of_mother >= 43 &&
                length_between_child_birth_and_death_of_mother <= 365
            )
        )
        {
            if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1)
            {
                p_report_object.total_number_pregnancy_related_at_time_of_death.pregnant_within_43_to_365_days_of_death = 1;
            }
            else if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
            {
                p_report_object.total_number_pregnancy_associated_at_time_of_death.pregnant_within_43_to_365_days_of_death = 1;
            }
        }
        else if
        (
            length_between_child_birth_and_death_of_mother == -1 &&
            pregnancy_status == 3

        )
        {
            if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1)
            {
                p_report_object.total_number_pregnancy_related_at_time_of_death.pregnant_within_43_to_365_days_of_death = 1;
            }
            else if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
            {
                p_report_object.total_number_pregnancy_associated_at_time_of_death.pregnant_within_43_to_365_days_of_death = 1;
            }
        }
        else if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1)
        {
            p_report_object.total_number_pregnancy_related_at_time_of_death.blank = 1;
        }
        else if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
        {
            p_report_object.total_number_pregnancy_associated_at_time_of_death.blank = 1;
        }


/*
        pregnant_at_time_of_death_enum time_of_death_enum = get_pregnant_at_time_of_death_classifier (p_source_object);
        switch (time_of_death_enum) 
        {
            case pregnant_at_time_of_death_enum.pregnant_at_the_time_of_death:
            if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1) 
            {
                p_report_object.total_number_pregnancy_related_at_time_of_death.pregnant_at_the_time_of_death = 1;
            } 
            else if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
            {
                p_report_object.total_number_pregnancy_associated_at_time_of_death.pregnant_at_the_time_of_death = 1;
            }


            break;
        case pregnant_at_time_of_death_enum.pregnant_within_42_days_of_death:
            if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1) 
            {
                p_report_object.total_number_pregnancy_related_at_time_of_death.pregnant_within_42_days_of_death = 1;
            } 
            else if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
            {
                p_report_object.total_number_pregnancy_associated_at_time_of_death.pregnant_within_42_days_of_death = 1;
            }

            break;
        case pregnant_at_time_of_death_enum.pregnant_within_43_to_365_days_of_death:
            if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1) 
            {
                p_report_object.total_number_pregnancy_related_at_time_of_death.pregnant_within_43_to_365_days_of_death = 1;
            } 
            else if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
            {
                p_report_object.total_number_pregnancy_associated_at_time_of_death.pregnant_within_43_to_365_days_of_death = 1;
            }

            break;
            case pregnant_at_time_of_death_enum.blank:
            default:
            if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1) 
            {
                p_report_object.total_number_pregnancy_related_at_time_of_death.blank = 1;
            } 
            else if(p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
            {
                p_report_object.total_number_of_pregnancy_associated_deaths_by_age.blank = 1;
            }

            break;
        }
*/
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


private void popluate_pregnancy_deaths_by_age (ref mmria.server.model.c_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
{
deaths_by_age_enum age_enum = get_age_classifier (p_source_object);
switch (age_enum) 
{
    case deaths_by_age_enum.age_less_than_20:
        if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_related_deaths_by_age.age_less_than_20 = 1;
        } 
        else if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_associated_deaths_by_age.age_less_than_20 = 1;
        }
        break;
    case deaths_by_age_enum.age_20_to_24:
        if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_related_deaths_by_age.age_20_to_24 = 1;
        } 
        else if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_associated_deaths_by_age.age_20_to_24 = 1;
        }

        break;
    case deaths_by_age_enum.age_25_to_29:
        if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_related_deaths_by_age.age_25_to_29 = 1;
        }
        else if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_associated_deaths_by_age.age_25_to_29 = 1;
        }

        break;
    case deaths_by_age_enum.age_30_to_34:
        if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_related_deaths_by_age.age_30_to_34 = 1;
        } 
        else if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_associated_deaths_by_age.age_30_to_34 = 1;
        }

        break;
    case deaths_by_age_enum.age_35_to_44:
        if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_related_deaths_by_age.age_35_to_44 = 1;
        } 
        else if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_associated_deaths_by_age.age_35_to_44 = 1;
        }

        break;
    case deaths_by_age_enum.age_45_and_above:
        if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_related_deaths_by_age.age_45_and_above = 1;
        }
        else if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_associated_deaths_by_age.age_45_and_above = 1;
        }
        break;
    case deaths_by_age_enum.blank:
    default:
        if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_related_deaths_by_age.blank = 1;
        }
        else if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1) 
        {
            p_report_object.total_number_of_pregnancy_associated_deaths_by_age.blank = 1;
        }

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


    private void popluate_total_number_of_pregnancy_related_deaths_by_ethnicity (ref mmria.server.model.c_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {
        if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1)
        {
            HashSet<ethnicity_enum> ethnicity_set = get_ethnicity_classifier (p_source_object);

            
            if (ethnicity_set.Count() == 0)
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.blank = 1;
                return;
            }

            
            bool is_blank = true;
            
            if (ethnicity_set.Contains(ethnicity_enum.hispanic))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.hispanic  = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.non_hispanic_black))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.non_hispanic_black  = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.non_hispanic_white))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.non_hispanic_white = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.american_indian_alaska_native))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.american_indian_alaska_native = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.native_hawaiian))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.native_hawaiian = 1;
                is_blank = false;
            }

        
            if (ethnicity_set.Contains (ethnicity_enum.guamanian_or_chamorro))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.guamanian_or_chamorro = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.samoan))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.samoan = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.other_pacific_islander))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.other_pacific_islander = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.asian_indian))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.asian_indian = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.filipino))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.filipino = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.korean))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity .korean = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.other_asian))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.other_asian = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.chinese))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.chinese = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.japanese))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.japanese = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.vietnamese))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.vietnamese = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.other))
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.other = 1;
                is_blank = false;
            }

            if (is_blank)
            {
                p_report_object.total_number_of_pregnancy_related_deaths_by_ethnicity.blank = 1;
                return;
            }
            
            //System.Console.WriteLine ("break");
        }
    }


    private void popluate_total_number_of_pregnancy_associated_deaths_by_ethnicity (ref mmria.server.model.c_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {
        if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
        {
            HashSet<ethnicity_enum> ethnicity_set = get_ethnicity_classifier (p_source_object);

            bool is_blank = true;
            
            if (ethnicity_set.Count() == 0)
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.blank = 1;
                return;
            }

            if (ethnicity_set.Contains(ethnicity_enum.hispanic))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.hispanic  = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.non_hispanic_black))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.non_hispanic_black  = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.non_hispanic_white))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.non_hispanic_white = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.american_indian_alaska_native))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.american_indian_alaska_native = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.native_hawaiian))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.native_hawaiian = 1;
                is_blank = false;
            }

        
            if (ethnicity_set.Contains (ethnicity_enum.guamanian_or_chamorro))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.guamanian_or_chamorro = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.samoan))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.samoan = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.other_pacific_islander))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.other_pacific_islander = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.asian_indian))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.asian_indian = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.filipino))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.filipino = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.korean))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity .korean = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.other_asian))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.other_asian = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.chinese))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.chinese = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.japanese))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.japanese = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.vietnamese))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.vietnamese = 1;
                is_blank = false;
            }
        
            if (ethnicity_set.Contains (ethnicity_enum.other))
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.other = 1;
                is_blank = false;
            }

            
            if(is_blank)
            {
                p_report_object.total_number_of_pregnancy_associated_by_ethnicity.blank = 1;
            }
            
            //System.Console.WriteLine ("break");
        }
    }


    private void popluate_total_number_of_cases_by_pregnancy_relatedness (ref mmria.server.model.c_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object)
    {

        try
        {	
            
            var list = List_Look_Up["/committee_review/pregnancy_relatedness"];

            string val = null;
            var get_value_result = get_value(p_source_object, "committee_review/pregnancy_relatedness");
            if(! get_value_result.is_erorr)
            {
                val = get_value_result.result?.ToString();
            }

            if(val != null)
            {
                switch(val)
                {
                    case "Pregnancy-Related":
                    case "Pregnancy Related":
                    case "1":
                        p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related = 1;
                    break;
                    case "Pregnancy-Associated but NOT Related":
                    case "Pregnancy-Associated, but NOT -Related":
                    case "0":
                        p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related = 1;
                    break;
                    case "Not Pregnancy Related or Associated (i.e. False Positive)":
                    case "Not Pregnancy-Related or -Associated (i.e. False Positive)":
                    case "99":
                        p_report_object.total_number_of_cases_by_pregnancy_relatedness.not_pregnancy_related_or_associated = 1;
                    break;
                    case "Pregnancy-Associated but Unable to Determine Pregnancy-Relatedness":
                    case "Unable to Determine if Pregnancy Related or Associated":
                    case "2":
                        p_report_object.total_number_of_cases_by_pregnancy_relatedness.unable_to_determine = 1;
                    break;
                    default:
                        p_report_object.total_number_of_cases_by_pregnancy_relatedness.blank = 1;
                    break;
                }

            }
            else
            {
                p_report_object.total_number_of_cases_by_pregnancy_relatedness.blank = 1;
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


    private void popluate_list (ref System.Collections.Generic.Dictionary<string, int> p_result, ref mmria.server.model.c_report_object p_report_object, System.Dynamic.ExpandoObject p_source_object, string p_mmria_path, bool p_is_pregnance_related)
    {
        var list = List_Look_Up["/" + p_mmria_path];

        //p_result = new System.Collections.Generic.Dictionary<string, int> (StringComparer.OrdinalIgnoreCase);

        foreach(var kvp in list)
        {
            p_result.Add(kvp.Key, 0);
        }

        if(p_is_pregnance_related)
        {
            if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_related == 1)
            {
                try
                {	
                    string val = null;
                    var get_value_result = get_value(p_source_object, p_mmria_path);
                    if(! get_value_result.is_erorr)
                    {
                        val = get_value_result.result?.ToString();
                    }

                    if(val != null && p_result.ContainsKey(val))
                    {
                        p_result[val] = 1;
                    }
                    else
                    {
                        p_result["9999"] = 1;
                    }
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine (ex);
                }
            }
        }
        else if (p_report_object.total_number_of_cases_by_pregnancy_relatedness.pregnancy_associated_but_not_related == 1)
        {
            try
            {	
                string val = null;
                var get_value_result = get_value(p_source_object, p_mmria_path);
                if(! get_value_result.is_erorr)
                {
                    val = get_value_result.result?.ToString();
                }

                if(val != null && p_result.ContainsKey(val))
                {
                    p_result[val] = 1;
                }
                else
                {
                    p_result["9999"] = 1;
                }
            }
            catch(Exception ex)
            {
                System.Console.WriteLine (ex);
            }
        }
    }

}


