using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Akka.Actor;
using System.Globalization;

using mmria.server.Controllers;
using mmria_pmss_client.Models.IJE;
using TinyCsvParser;
using mmria.common.model.couchdb;
using mmria.case_version.pmss.v230616;
using System.Security.Policy;

namespace mmria.pmss.services.vitalsimport;




public sealed class StartPMSSBatchItemMessage
{
    public StartPMSSBatchItemMessage(){}
    //public mmria_pmss_client.Models.IJE.PMSS_Other pmss_other { get; set; }

    public string record_id { get; init; }


    public DateTime ImportDate { get; init; }
    public string ImportFileName { get; init; }
    public List<string> headers { get; init; }
    public List<string> data { get; init; }

}

public sealed class pmss_item_data
{
    public string mmria_path { get; set; }
    public string name {get; set;}

    public string data { get; set; }
}


public sealed class TAMUGeoCode
{

	public mmria.common.texas_am.geocode_response execute
	(
		string geocode_api_key,
		string street_address,
		string city,
		string state,
		string zip
	) 
	{ 

		var result = new common.texas_am.geocode_response();

		string request_string = string.Format ("https://geoservices.tamu.edu/Services/Geocode/WebService/GeocoderWebServiceHttpNonParsed_V04_01.aspx?streetAddress={0}&city={1}&state={2}&zip={3}&apikey={4}&format=json&allowTies=false&tieBreakingStrategy=flipACoin&includeHeader=true&census=true&censusYear=2000|2010&notStore=false&version=4.01", street_address, city, state, zip, geocode_api_key);

		var curl = new mmria.getset.cURL("GET", null, request_string, null);
		try
		{
			string responseFromServer = curl.execute();

			result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.texas_am.geocode_response>(responseFromServer);
		
		}
		catch(Exception ex)
		{
			// do nothing for now
		}

		return result;

	} 


	public async Task<IEnumerable<mmria.common.texas_am.geocode_response>> executeAsync
	(
		string geocode_api_key,
		string street_address,
		string city,
		string state,
		string zip
	) 
	{ 
		
		string request_string = string.Format ("https://geoservices.tamu.edu/Services/Geocode/WebService/GeocoderWebServiceHttpNonParsed_V04_01.aspx?streetAddress={0}&city={1}&state={2}&zip={3}&apikey={4}&format=json&allowTies=false&tieBreakingStrategy=flipACoin&includeHeader=true&census=true&censusYear=2000|2010&notStore=false&version=4.01", street_address, city, state, zip, geocode_api_key);

		var curl = new mmria.getset.cURL("GET", null, request_string, null);
					// Read the content.
		string responseFromServer = await curl.executeAsync();

		var json_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.texas_am.geocode_response>(responseFromServer);



		var result =  new mmria.common.texas_am.geocode_response[] 
		{ 
			json_result

		}; 

		return result;
	} 
}



public sealed class PMSS_ItemProcessor : ReceiveActor
{

    Dictionary<string, mmria.common.metadata.value_node[]> lookup;

    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
    private string config_timer_user_name = null;
    private string config_timer_value = null;

    private string config_couchdb_url = null;
    private string db_prefix = "";

    
    static HashSet<string> ExistingRecordIds = null;

    mmria.common.couchdb.DBConfigurationDetail item_db_info;

    private System.Dynamic.ExpandoObject case_expando_object = null;

    private Dictionary<string, string> StateDisplayToValue;

    private string location_of_residence_latitude = null;
    private string location_of_residence_longitude = null;
    private string facility_of_delivery_location_latitude = null;
    private string facility_of_delivery_location_longitude = null;

    private string death_certificate_place_of_last_residence_latitude = null;
    private string death_certificate_place_of_last_residence_longitude = null;
    private string death_certificate_address_of_death_latitude = null;
    private string death_certificate_address_of_death_longitude = null;

    
    mmria.common.couchdb.OverridableConfiguration configuration;

    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_name;
    public PMSS_ItemProcessor
    (
        mmria.common.couchdb.OverridableConfiguration _configuration,
        string _host_name
    )
    {
        configuration = _configuration;
        host_name = _host_name;

        db_config = configuration.GetDBConfig(host_name);

        Receive<StartPMSSBatchItemMessage>(message =>
        {    
            Console.WriteLine("Message Recieved");
            //Console.WriteLine(JsonConvert.SerializeObject(message));
            Sender.Tell("Message Recieved");
            Process_Message(message);
        });
    }

    private async void Process_Message(StartPMSSBatchItemMessage message)
    {

        //var mor = new mmria_pmss_client.Models.IJE.MOR_Specification();
        //var fet = new mmria_pmss_client.Models.IJE.FET_Specification();
        //var nat = new mmria_pmss_client.Models.IJE.NAT_Specification();

        string metadata_url = $"{db_config.url}/metadata/version_specification-{configuration.GetString("metadata_version", host_name)}/metadata";
        var metadata_curl = new mmria.getset.cURL("GET", null, metadata_url, null, config_timer_user_name, config_timer_value);
        mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_curl.execute());

        lookup = get_look_up(metadata);

        var all_nodes = metadata.Flatten();




        //var new_case = new System.Dynamic.ExpandoObject();
        var new_case = new mmria.case_version.pmss.v230616.mmria_case();

/*
        new_case.data_migration_history = new();
        new_case.amss_tracking = new();
        new_case.amss_tracking.admin_grp = new();
        new_case.amss_tracking.assessment_grp = new();
        new_case.amss_tracking.folder_grp = new();


        new_case.cause_of_death = new();
        new_case.cause_of_death.q28 = new();
        new_case.cause_of_death.q29 = new();
        new_case.cause_of_death.q30 = new();
        new_case.cause_of_death.q31 = new();
        new_case.cause_of_death.q33 = new();




        new_case.committee_review = new();
        new_case.committee_review.agreement_grp = new();
        new_case.committee_review.rev_assessment_grp = new();
        new_case.committee_review.reviewer_grp = new();
   



        new_case.demographic = new();
        new_case.demographic.q12 = new();
        new_case.demographic.q12.ethnicity = new();

        new_case.outcome = new();
        new_case.outcome.q25 = new();
        new_case.outcome.dterm_grp = new();

        new_case.preparer_remarks = new();
        new_case.preparer_remarks.preparer_grp = new();
        new_case.preparer_remarks.remarks_grp = new();
        new_case.preparer_remarks.pdf_grp = new();




        new_case.demographic.q12.group = new ();


        new_case.demographic.q14 = new();
        new_case.demographic.date_of_birth = new();

        
        new_case.tracking = new();
        new_case.tracking.admin_info = new();
        new_case.tracking.date_of_death = new();
        
 
        new_case.tracking.q1 = new();
        new_case.tracking.q7 = new();
        new_case.tracking.q9 = new();

        
        
        new_case.ije_bc = new();
        new_case.ije_bc.file_info = new();
        new_case.ije_bc.delivery_info = new();
        new_case.ije_bc.previous_info = new();
        new_case.ije_bc.residence_mother = new();
        new_case.ije_bc.demog_details = new();

        
        
        new_case.ije_dc = new();
        new_case.ije_dc.file_info = new();
        new_case.ije_dc.death_info = new();
        new_case.ije_dc.cause_details = new();
        new_case.ije_dc.injury_details = new();
        new_case.ije_dc.birthplace_mother = new();
        new_case.ije_dc.residence_mother = new();
        new_case.ije_dc.demog_details = new();
        
        
        new_case.ije_fetaldc = new();
        new_case.ije_fetaldc.file_info = new();
        new_case.ije_fetaldc.delivery_info = new();
        new_case.ije_fetaldc.condition_cause = new();
        new_case.ije_fetaldc.previous_info = new();
        new_case.ije_fetaldc.residence_mother = new();
        new_case.ije_fetaldc.demog_details = new();
        
        
        new_case.vro_case_determination = new();
        new_case.vro_case_determination.cdc_case_matching_results = new();
        new_case.vro_case_determination.vro_update = new();
       */

        

        //mmria.pmss.services.vitalsimport.default_case.create(metadata, new_case);

        var current_date_iso_string = System.DateTime.UtcNow.ToString("o");

        var gs = new migrate.C_Get_Set_Value(new System.Text.StringBuilder());

        string get_string_value(string p_path)
        {
            var result = new_case.GetS_String(p_path);

            return result;
        }

        double? get_number(string p_path)
        {
            var result = new_case.GetS_Double(p_path);

            return result;
        }


        bool set_string_value(string p_path,string value)
        {
            var result = false;


            result = new_case.SetS_String(p_path, value);


            return result;
        }

        bool set_double_value(string p_path,double? value)
        {
            var result = false;


           result = new_case.SetS_Double(p_path, value);


            return result;
        }

        var mmria_id = System.Guid.NewGuid().ToString();

        set_string_value("_id", mmria_id);
        set_string_value("date_created", current_date_iso_string);
        set_string_value("created_by", "pmss-import");
        set_string_value("date_last_updated", current_date_iso_string);
        set_string_value("last_updated_by", "pmss-import");
        set_string_value("version", metadata.version);


        foreach(var has_default_attribute in all_nodes.Where(n => !string.IsNullOrWhiteSpace(n.Node.default_value)))
        {

            string default_string_value = "";
            double default_dobule_value = -1;


            var node = has_default_attribute.Node;
            switch (node.type.ToString().ToLower())
            {
                case "string":
                    default_string_value = node.default_value;
                    new_case.SetS_String
                    (
                        has_default_attribute.path, 
                        default_string_value
                    );

                break;
                case "number":
                    if(double.TryParse(node.default_value, out default_dobule_value))
                    {
                        default_string_value = node.default_value;
                        new_case.SetS_Double
                        (
                            has_default_attribute.path, 
                            default_dobule_value
                        );
                    }
                    break;

                case "list":
                    if
                    (
                        node.is_multiselect != null &&
                        node.is_multiselect.HasValue &&
                        node.is_multiselect.Value
                    )
                    {
                        

                        string[] default_list_of_string;
                        if(string.IsNullOrWhiteSpace(node.data_type))
                        {
                            default_list_of_string = node.default_value.Split("|");
                            List<string> val = new List<string>();
                            foreach(var item in default_list_of_string)
                            {
                                var trimmed_value = item.Trim();
                                if(!string.IsNullOrWhiteSpace(trimmed_value))
                                    val.Add(trimmed_value);
                            }

                            new_case.SetS_List_Of_String
                            (
                                has_default_attribute.path, 
                                val
                            );
                        }
                        else switch(node.data_type.ToLower())
                        {
                            case "number":
                                default_list_of_string = node.default_value.Split("|");
                                List<double> double_list = new List<double>();
                                foreach(var item in default_list_of_string)
                                {
                                    var trimmed_value = item.Trim();
                                    if(has_default_attribute.display_to_value.ContainsKey(trimmed_value))
                                    {
                                        trimmed_value = has_default_attribute.display_to_value[trimmed_value];
                                    }

                                    if(double.TryParse(trimmed_value, out var default_double))
                                        double_list.Add(default_double);
                                }

                                new_case.SetS_List_Of_Double
                                (
                                    has_default_attribute.path, 
                                    double_list
                                );
                            break;
                            case "string":
                                default_list_of_string = node.default_value.Split("|");
                                List<string> val = new List<string>();
                                foreach(var item in default_list_of_string)
                                {
                                    var trimmed_value = item.Trim();
                                    if(!string.IsNullOrWhiteSpace(trimmed_value))
                                        val.Add(trimmed_value);
                                }

                                new_case.SetS_List_Of_String
                                (
                                    has_default_attribute.path, 
                                    val
                                );
                            break;
                            default:
                            System.Console.WriteLine($"default type: {node.type} not found: {has_default_attribute.path}");
                            break;
                        }
                    }
                    else
                    {
                        if(string.IsNullOrWhiteSpace(node.data_type))
                        {
                            default_string_value = node.default_value;
                            new_case.SetS_String
                            (
                                has_default_attribute.path, 
                                default_string_value
                            );
                        }
                        else switch(node.data_type.ToLower())
                        {
                            case "number":
                                if(double.TryParse(node.default_value, out var double_value))
                                {
                                    new_case.SetS_Double
                                    (
                                        has_default_attribute.path, 
                                        double_value
                                    );
                                }
                            break;
                            case "string":
                                default_string_value = node.default_value;
                                new_case.SetS_String
                                (
                                    has_default_attribute.path, 
                                    default_string_value
                                );
                            break;
                            default:
                            System.Console.WriteLine($"default type: {node.type} not found: {has_default_attribute.path}");
                            break;
                        } 
                    }
                    
                break;
                default:
                
                    System.Console.WriteLine($"set default: {node.type} not found: {has_default_attribute.path}");
                break;
            }
        }

    
        var is_valid_file = true;

        var header_to_index = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for(var i = 0; i < message.headers.Count; i++)
        {
            var key = message.headers[i];
            if(header_to_index.ContainsKey(key))
            {
                System.Console.WriteLine($"duplicate header: {key} column: {i+1}");
                is_valid_file = false;
            }
            else
            {
                header_to_index.Add(message.headers[i], i);
            }
        }
            
        
        

        if (! is_valid_file)
        {
            Context.Stop(this.Self);
            return;
        }

        I_PMSS_File_Specification name_to_path;

        if( header_to_index.Count == 156)
        {
            name_to_path = new PMSS_Other_Specification();
        }
        else
        {
            name_to_path = new PMSS_All_Specification();
        }

        foreach(var kvp in header_to_index)
        {

            string[] arr = new string[0];
            
            if(name_to_path.Contains(kvp.Key))
            {
                var mmria_path = name_to_path[kvp.Key];
                var data = message.data[kvp.Value];

                if(mmria_path.ToUpper() == "NOT MAPPED") continue;

                if(mmria_path == "tracking/admin_info/jurisdiction")
                {
                    var state_node = lookup["lookup/state"];

                    var item = state_node.Where (x => int.Parse(x.value) == int.Parse(data)).SingleOrDefault();
                    if(item != null)
                    {
                        //System.Console.WriteLine($"{item.value}");
                        data = item.value;
                    }

                }

                var meta_node = all_nodes.Where(n => n.path.Equals(mmria_path, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();


                if(meta_node == null)
                {
                    System.Console.WriteLine($"Node Not Found path {mmria_path} key:{kvp.Key} value:{data} ");
                    continue;
                }
                //var metadata_node = get_metadata_node(metadata, mmria_path);
                var metadata_node = meta_node.Node;



                var data_type = metadata_node.type.ToLower();
                var is_a_list = false;

                if(data_type == "list")
                {
                    is_a_list = true;

                    if
                    (
                        metadata_node.is_multiselect != null &&
                        metadata_node.is_multiselect.HasValue &&
                        metadata_node.is_multiselect.Value
                    )
                    {
                        data_type = "list_of_";
                    }
                    else
                    {
                        data_type = "";
                    }


                    if(metadata_node.data_type == null)
                    {
                        data_type += "string";
                    }
                    else if(metadata_node.data_type == "number")
                    {
                        data_type += "number";
                    }
                    else if(metadata_node.data_type == "string")
                    {
                        data_type += "string";
                    }
                    else 
                    {
                        data_type += "string";
                    }
                }

                var set_result = false;
                
                var is_ignored = false;

                switch(data_type)
                {
                    case "date":
                        arr = data.Split("/");
                        if(arr.Length > 2)
                        {
                            data = $"{arr[2]}-{arr[0]}-{arr[1]}";
                        }
                        DateOnly date_only_value;
                        //(int.Parse(arr[2]), int.Parse(arr[0]), int.Parse(arr[1]))
                        
                        if(DateOnly.TryParse(data, out date_only_value))
                        {
                            set_result = new_case.SetS_Date_Only
                            (
                                mmria_path, 
                                date_only_value
                            );
                        }
                        else
                        {
                            set_result = new_case.SetS_Date_Only
                            (
                                mmria_path, 
                                null
                            );
                        }
                       
                    break;

                    case "datetime":
                        DateTime datetime_value;
                        if(DateTime.TryParse(data, out datetime_value))
                        {
                            set_result = new_case.SetS_Datetime
                            (
                                mmria_path, 
                                datetime_value
                            );
                        }
                        else
                        {
                            set_result = new_case.SetS_Datetime
                            (
                                mmria_path, 
                                null
                            );
                        }

                       
                    break;

                    case "time":
                        if(string.IsNullOrWhiteSpace(data))
                        {
                            set_result = new_case.SetS_Time_Only
                            (
                                mmria_path, 
                                null
                            );

                            break;
                        }

                        var temp_time = data.PadLeft(4, '0');

                        var temp_time_string = $"{temp_time[..2]}:{temp_time[2..]}";
 
                        TimeOnly time_only_value;

                        if(TimeOnly.TryParse(temp_time_string, out time_only_value))
                        {
                            set_result = new_case.SetS_Time_Only
                            (
                                mmria_path, 
                                time_only_value
                            );
                        }
                        else
                        {
                            set_result = new_case.SetS_Time_Only
                            (
                                mmria_path, 
                                null
                            );
                        }
                    break;
                    case "number":
                        double number_value = -1;
                        if(double.TryParse(data, out number_value))
                        {
                            set_result = new_case.SetS_Double
                            (
                                mmria_path, 
                                number_value
                            );
                        }
                        else
                        {
                            set_result = new_case.SetS_Double
                            (
                                mmria_path, 
                                null
                            );
                        }


                    break;

                    case "list_of_string":
                        List<string> list_of_string = new();
                        arr = data.Split("|");
                        foreach(var item in arr)
                        {
                            var trimmed_item = item.Trim();
                            if(!string.IsNullOrWhiteSpace(trimmed_item))
                            {
                                list_of_string.Add(trimmed_item);
                            }
                        }
                        set_result = new_case.SetS_List_Of_String
                        (                        
                            mmria_path, 
                            list_of_string
                        );
                    break;

                    case "list_of_number":
                        List<double> list_of_double = new();
                        arr = data.Split("|");
                        foreach(var item in arr)
                        {
                            var trimmed_item = item.Trim();

                            if(meta_node.display_to_value.ContainsKey(trimmed_item))
                            {
                                trimmed_item = meta_node.display_to_value[trimmed_item];
                            }

                            if(double.TryParse(trimmed_item, out var double_value))
                            {
                                list_of_double.Add(double_value);
                            }
                        }
                        set_result = new_case.SetS_List_Of_Double
                        (                        
                            mmria_path, 
                            list_of_double
                        );
                    break;

                    case "string":
                    case "textarea":
                        HashSet<string> Padding_Path_List = new(StringComparer.OrdinalIgnoreCase)
                        {
                            "tracking/statdth",
                            "tracking/q9/statres",
                            "tracking/admin_info/jurisdiction",
                            "demographic/q12/matbplc_us"
                        };

                        if(Padding_Path_List.Contains(mmria_path))
                        {
                            if(string.IsNullOrWhiteSpace(data))
                            {
                                set_result = new_case.SetS_String
                                (
                                    mmria_path, 
                                    "9999"
                                );
                            }
                            else if(data.Length < 2)
                            {
                                set_result = new_case.SetS_String
                                (
                                    mmria_path, 
                                    data.PadLeft(2, '0')
                                );
                            }
                            else set_result = new_case.SetS_String
                            (
                                mmria_path, 
                                data
                            );
                        }                        
                        else if(mmria_path == "tracking/q1/amssno")
                        {
                            if(string.IsNullOrWhiteSpace(data))
                            {
                                set_result = new_case.SetS_String
                                (
                                    mmria_path, 
                                    "00000"
                                );
                            }
                            else set_result = new_case.SetS_String
                            (
                                mmria_path, 
                                data.PadLeft(5, '0')
                            );
                        }
                        else set_result = new_case.SetS_String
                        (
                            mmria_path, 
                            data
                        );

                    break;
                    case "group":
                        is_ignored = true;
                    break;
                    default:
                        System.Console.WriteLine($"Not On Set List data_type{data_type} {kvp.Key}:{data} {mmria_path}");
                    break;
                }


                if(!is_ignored)
                if(set_result)
                {
                    //System.Console.WriteLine($"Updated {kvp.Key}:{data} {mmria_path}");
                }
                else
                {
                    System.Console.WriteLine($"Error updating {kvp.Key}:{data} {mmria_path}");
                }
            }
        }

/*

            14a. Decedent's usual occupation         occup    /demographic/q14/occup            

    14b. Kind of business/industry              indust   /demographic/q14/indust
Destination:

    01) industry_code_1 demographic/q14/

    02) industry_code_2

    03) industry_code_3

    04) occupation_code_1

    05) occupation_code_2

    06) occupation_code_3
*/

        pmss_item_data? get_data_item(string name)
        {
            pmss_item_data? result = null;

            if(header_to_index.ContainsKey(name))
            {
                result = new pmss_item_data()
                {
                    name = name,
                    mmria_path = name_to_path[name],
                    data = message.data[header_to_index[name]]
                };



                if(result.mmria_path == "tracking/q9/statres")
                {
                    var state_node = lookup["lookup/state"];

                    var item = state_node.Where (x => int.Parse(x.value) == int.Parse(result.data)).SingleOrDefault();
                    if(item != null)
                    {
                        //System.Console.WriteLine($"{item.value}");
                        //result.data = item.value;
                        result.data = item.display.Substring(item.display.IndexOf("(")+1, 2);
                    }

                }

            }


            return result;
        }

        var occup = get_data_item("occup");
        var occup_string = "";
        var indust = get_data_item("indust");
        var indust_string = "";

        if(occup != null)
        {

            occup_string = occup.data;

        }

        
        if(indust != null)
        {

            indust_string = indust.data;

        }

        
        if(occup != null || indust != null)
        {

            var niosh_result = get_niosh_codes(occup_string, indust_string);
            if(niosh_result.Industry.Count > 0)
            {
                set_string_value
                (
                    "demographic/q14/industry_code_1", 
                    niosh_result.Industry[0].Code
                );
            }

            if(niosh_result.Industry.Count > 1)
            {
                set_string_value
                (
                    "demographic/q14/industry_code_2", 
                    niosh_result.Industry[1].Code
                );
            }

            if(niosh_result.Industry.Count > 2)
            {
                set_string_value
                (
                    "demographic/q14/industry_code_3", 
                    niosh_result.Industry[2].Code
                );
            }

            if(niosh_result.Occupation.Count > 0)
            {
                set_string_value
                (
                    "demographic/q14/occupation_code_1", 
                    niosh_result.Occupation[0].Code
                );
            }

            if(niosh_result.Occupation.Count > 1)
            {
                set_string_value
                (
                    "demographic/q14/occupation_code_2", 
                    niosh_result.Occupation[1].Code
                );
            }

            if(niosh_result.Occupation.Count > 2)
            {
                set_string_value
                (
                    "demographic/q14/occupation_code_3", 
                    niosh_result.Occupation[2].Code
                );
            }

        }


/*
  Q9. State of residence               statres                  /tracking/q9/statres   CODED (00 à NYC,  01 à AL, … 63 à VI)

     9a. Zip code of residence        reszip                    /tracking/q9/reszip    

     9c. County of residence           county                  /tracking/q9/county
Destination:
*/

        var statres = get_data_item("statres");
        var reszip = get_data_item("reszip");
        var county = get_data_item("county");


        if(statres != null && reszip != null)
        {
            var geocode_data = get_geocode_info
            (
                "", // address
                "", // city
                statres.data,
                reszip.data
            );


            Set_Residence_Gecocode
            (
                gs, 
                geocode_data, 
                new_case
            );
        }



    var omb_path = "demographic/q12/group/race_omb";
    string omb_string_value = get_string_value(omb_path);

    if(string.IsNullOrWhiteSpace(omb_string_value))
    {
        string race_white = get_string_value("demographic/q12/group/race_white");
        string race_black = get_string_value("demographic/q12/group/race_black");
        string race_amindalknat = get_string_value("demographic/q12/group/race_amindalknat");
        string race_asianindian = get_string_value("demographic/q12/group/race_asianindian");
        string race_chinese = get_string_value("demographic/q12/group/race_chinese");
        string race_filipino = get_string_value("demographic/q12/group/race_filipino");
        string race_japanese = get_string_value("demographic/q12/group/race_japanese");
        string race_korean = get_string_value("demographic/q12/group/race_korean");
        string race_vietnamese = get_string_value("demographic/q12/group/race_vietnamese");
        string race_otherasian = get_string_value("demographic/q12/group/race_otherasian");
        string race_nativehawaiian = get_string_value("demographic/q12/group/race_nativehawaiian");
        string race_guamcham = get_string_value("demographic/q12/group/race_guamcham");
        string race_samoan = get_string_value("demographic/q12/group/race_samoan");
        string race_otherpacific = get_string_value("demographic/q12/group/race_otherpacific");
        string race_other = get_string_value("demographic/q12/group/race_other");
        string race_notspecified = get_string_value("demographic/q12/group/race_notspecified");
        
        double? omb = calc_race_omb
        (
            race_white,
            race_black,
            race_amindalknat,
            race_asianindian,
            race_chinese,
            race_filipino,
            race_japanese,
            race_korean,
            race_vietnamese,
            race_otherasian,
            race_nativehawaiian,
            race_guamcham,
            race_samoan,
            race_otherpacific,
            race_other,
            race_notspecified
        );

        
        if(omb.HasValue)
        {
            set_double_value(omb_path, omb.Value);
        }
        else
        {
            set_double_value(omb_path, 9999);
        }
            
        
    }



    var agedif_path = "demographic/date_of_birth/agedif";
    var date_of_death_month_path = "tracking/date_of_death/month";
    var date_of_death_day_path = "tracking/date_of_death/day";
    var date_of_death_year_path = "tracking/date_of_death/year	";

    var date_of_birth_month_path = "demographic/date_of_birth/month";
    var date_of_birth_day_path = "demographic/date_of_birth/day";
    var date_of_birth_year_path = "demographic/date_of_birth/year";


    var outcome_month_path = "outcome/dterm_grp/dterm_mo";
    var outcome_day_path = "outcome/dterm_grp/dterm_dy";
    var outcome_year_path = "outcome/dterm_grp/dterm_yr";



    var agedif_number = get_number(agedif_path);

    if(!agedif_number.HasValue)
    {

        var date_of_death_month_double = get_number("tracking/date_of_death/month");
        var date_of_death_day_double = get_number("tracking/date_of_death/day");
        var date_of_death_year_double = get_number("tracking/date_of_death/year");

        var date_of_birth_month_double = get_number("demographic/date_of_birth/month");
        var date_of_birth_day_double = get_number("demographic/date_of_birth/day");
        var date_of_birth_year_double = get_number("demographic/date_of_birth/year");

        var outcome_month_double = get_number("outcome/dterm_grp/dterm_mo");
        var outcome_day_double = get_number("outcome/dterm_grp/dterm_dy");
        var outcome_year_double = get_number("outcome/dterm_grp/dterm_yr");

        var agedif_answer = calc_number_of_years
            (
                date_of_birth_year_double,
                date_of_birth_month_double,
                date_of_birth_day_double,
                date_of_death_year_double,
                date_of_death_month_double,
                date_of_death_day_double
            );

        set_double_value(agedif_path, agedif_answer);


        var daydif_answer = calc_number_of_days
            (
                outcome_year_double,
                outcome_month_double,
                outcome_day_double,
                date_of_death_year_double,
                date_of_death_month_double,
                date_of_death_day_double
            );

        set_double_value("outcome/dterm_grp/daydif", daydif_answer);

        /*

        -	IF AgeDif IS BLANK then Calculate AgeDif		ELSE Transfer AgeDIf to Path: app/demographic/date_of_birth/agedif		[Current value in CSV-file: BLANK]
        -	IF DayDif is BLANK then Calculate DayDIf		ELSE Transfer DayDif to Path app/outcome/dterm_grp/daydif			[Current value in CSV-file: BLANK]	
        -	IF Race_OMB is BLANK then Calculate Race_OMB	ELSE Transfer Race_OMB to Path: app/demographic/q12/group/race_omb 	[Current value in CSV-file: BLANK]	
        -	IF BMI is BLANK then Calculate BMI			ELSE Transfer BMI to Path: app/demographic/bmi				[Current value in CSV-file: BLANK]


        -	IF AgeDif IS BLANK then Calculate AgeDif		ELSE Transfer AgeDIf to Path: app/demographic/date_of_birth/agedif	(DOD-DOB)	
        DESTINATION-FIELD: app/demographic/date_of_birth/agedif
        SOURCE-FIELDS:
        tracking/date_of_death/month		demographic/date_of_birth/month
        tracking/date_of_death/day		demographic/date_of_birth/day
        tracking/date_of_death/year		demographic/date_of_birth/year


        -	IF DayDif IS BLANK then Calculate DayDif		ELSE Transfer DayDif to Path:  app/outcome/dterm_grp/daydif		(DDEL - DOD)	
        DESTINATION-FIELD: : app/outcome/dterm_grp/daydif
        SOURCE-FIELDS
        tracking/date_of_death/month 		outcome/dterm_grp/dterm_mo
        tracking/date_of_death/day 		outcome/dterm_grp/dterm_dy
        tracking/date_of_death/year 		outcome/dterm_grp/dterm_yr
        //CALCULATE NUMBER OF DAYS BETWEEN 2 DATES

        */

    }


    var bmi_path ="demographic/bmi";
    var bmi_double = get_number(bmi_path);
    if(!bmi_double.HasValue)
    {

        var height_path = "demographic/height";
        var weight_path = "demographic/wtpreprg";

        var height_double = get_number(height_path);
        var weight_double = get_number(weight_path);

        var bmi_calc = calc_bmi(height_double, weight_double);

        set_double_value(bmi_path, bmi_calc);

        /*
        -	IF BMI IS BLANK then Calculate BMI			ELSE Transfer BMI to Path: app/demographic/bmi		(WT/HT2)
        DESTINATION-FIELD: app/demographic/bmi
        SOURCE-FIELDS
        demographic/height
        demographic/wtpreprg
        //CALCLATE BMI FROM HEIGHT (IN INCHES)AND WEIGHT (IN POUNDS)
        
        */

    }


    var pmssno_path = "tracking/admin_info/pmssno";
    
    var pmssno = get_string_value(pmssno_path);
    if(string.IsNullOrWhiteSpace(pmssno))
    {
        var jurisdiction_path = "tracking/admin_info/jurisdiction";
        var year_path = "tracking/admin_info/track_year";

        var jurisdiction = get_string_value(jurisdiction_path);
        var year_number = get_number(year_path);

        var year_string = "00";

        if(year_number.HasValue)
        {
             year_string = year_number.ToString().PadLeft(4,'0')[2..];
        }  

        var prefix = $"{jurisdiction}-{year_string}";
        if(!string.IsNullOrWhiteSpace(prefix))
        {
            pmssno = GetNextPMSSNumber(prefix);
            set_string_value(pmssno_path, pmssno);
        }
            
    }

    

/*
        var finished = new mmria.common.ije.BatchItem()
        {
            Status = mmria.common.ije.BatchItem.StatusEnum.NewCaseAdded,
            CDCUniqueID = mor_field_set["SSN"],
            ImportDate = message.ImportDate,
            ImportFileName = message.ImportFileName,
            ReportingState = message.host_state,

            StateOfDeathRecord = mor_field_set["DSTATE"],
            DateOfDeath = $"{mor_field_set["DOD_YR"]}-{mor_field_set["DOD_MO"]}-{mor_field_set["DOD_DY"]}",
            DateOfBirth = $"{mor_field_set["DOB_YR"]}-{mor_field_set["DOB_MO"]}-{mor_field_set["DOB_DY"]}",
            LastName = mor_field_set["LNAME"],
            FirstName = mor_field_set["GNAME"],
            
            mmria_record_id = message.record_id,
            mmria_id = mmria_id,
            StatusDetail = "Added new case"
        };
        */

        string request_string = $"{db_config.url}/{db_config.prefix}mmrds/{mmria_id}";

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(new_case, settings);

        var document_curl = new mmria.getset.cURL("PUT", null, request_string, object_string, db_config.user_name, db_config.user_value);

        var document_put_response = new mmria.common.model.couchdb.document_put_response();

        string responseFromServer = string.Empty;
        try
        {
            responseFromServer = document_curl.execute();
            document_put_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
        }
        catch (Exception ex)
        {

            System.Console.WriteLine(responseFromServer);
            System.Console.WriteLine(ex);
/*
            finished = new mmria.common.ije.BatchItem()
            {
                Status = mmria.common.ije.BatchItem.StatusEnum.ImportFailed,
                CDCUniqueID = mor_field_set["SSN"],
                ImportDate = message.ImportDate,
                ImportFileName = message.ImportFileName,
                ReportingState = message.host_state,

                StateOfDeathRecord = mor_field_set["DSTATE"],
                DateOfDeath = $"{mor_field_set["DOD_YR"]}-{mor_field_set["DOD_MO"]}-{mor_field_set["DOD_DY"]}",
                DateOfBirth = $"{mor_field_set["DOB_YR"]}-{mor_field_set["DOB_MO"]}-{mor_field_set["DOB_DY"]}",
                LastName = mor_field_set["LNAME"],
                FirstName = mor_field_set["GNAME"],
                mmria_record_id = message.record_id,
                mmria_id = mmria_id,
                StatusDetail = "Error\n" + ex.ToString()
            };
            */
        }

        //Sender.Tell(finished);

        Context.Stop(this.Self);

    }

    double? calc_bmi(double? height, double? weight) 
    {



        double? result = null;

        if
        (
            !height.HasValue ||
            height == 99  ||
            height == 999
        )
        {
            return result;
        }

        if
        (
            !weight.HasValue ||
            weight == 666 ||
            weight == 777 ||
            weight == 999 
        )
        {
            return result;
        }


        height /= 39.3700787;
        weight /= 2.20462;
        var temp = Math.Round(weight.Value / Math.Pow(height.Value, 2D) * 10D) / 10D;

        if(!double.IsNaN(temp))
            result = temp;


        return result;
    }

    private double? calc_number_of_days
    (
        double? p_start_year, 
        double? p_start_month,
        double? p_start_day,
        double? p_end_year,
        double? p_end_month,
        double? p_end_day
    )
    {
            double? result = null;
            /*
            int.TryParse(p_start_year, out int start_year);
            int.TryParse(date_of_delivery_month, out int start_month);
            int.TryParse(date_of_delivery_day, out int start_day);
            int.TryParse(date_of_death_year, out int end_year);
            int.TryParse(date_of_death_month, out int end_month);
            int.TryParse(date_of_death_day, out int end_day);
            */


            if 
            (
                DateTime.TryParse
                (   
                    $"{p_start_year}/{p_start_month}/{p_start_day}", 
                    out DateTime startDateTest
                ) == true 
                && 
                DateTime.TryParse
                (
                    $"{p_end_year}/{p_end_month}/{p_end_day}", 
                    out DateTime endDateTest
                ) == true
            ) 
            {
                var time_span = endDateTest - startDateTest;


                var days = time_span.Days;
                result = (double) days;
            }

           return result;
    }



private double? calc_number_of_years
    (
        double? p_start_year, 
        double? p_start_month,
        double? p_start_day,
        double? p_end_year,
        double? p_end_month,
        double? p_end_day
    )
    {
            double? result = null;

            DateTime zeroTime = new DateTime(1, 1, 1);
            /*
            int.TryParse(p_start_year, out int start_year);
            int.TryParse(date_of_delivery_month, out int start_month);
            int.TryParse(date_of_delivery_day, out int start_day);
            int.TryParse(date_of_death_year, out int end_year);
            int.TryParse(date_of_death_month, out int end_month);
            int.TryParse(date_of_death_day, out int end_day);
            */


            if 
            (
                DateTime.TryParse
                (   
                    $"{p_start_year}/{p_start_month}/{p_start_day}", 
                    out DateTime startDateTest
                ) == true 
                && 
                DateTime.TryParse
                (
                    $"{p_end_year}/{p_end_month}/{p_end_day}", 
                    out DateTime endDateTest
                ) == true
            ) 
            {
                var time_span = endDateTest - startDateTest;


                //var days = time_span.Days;
                result = (double) (zeroTime + time_span).Year - 1;
            }

           return result;
    }

    GeocodeTuple get_geocode_info(string street, string city, string state, string zip)
    {

        var result = new GeocodeTuple();

        if (!string.IsNullOrEmpty(state))
        {
            var check_state = state.Split("-");
            state = check_state[0];
        }

        var TAMUGeocoder = new TAMUGeoCode();

        var response = TAMUGeocoder.execute(configuration.GetSharedString("geocode_api_key"), street, city, state, zip);
        
        if(response!= null && response.OutputGeocodes?.Length > 0)
        {
            result.OutputGeocode = response.OutputGeocodes[0].OutputGeocode;

            if(response.OutputGeocodes[0].CensusValues.Count > 0)
            {
                if(response.OutputGeocodes[0].CensusValues[0].ContainsKey("CensusValue1"))
                {
                    result.Census_Value = response.OutputGeocodes[0].CensusValues[0]["CensusValue1"];
                }
                
            }
        }

        return result;
    }

    private void Set_Residence_Gecocode
    (
        migrate.C_Get_Set_Value gs, 
        GeocodeTuple geocode_data, 
        mmria.case_version.pmss.v230616.mmria_case new_case
    )
    {
        string urban_status = null;
        string state_county_fips = null;

        string feature_matching_geography_type = "Unmatchable";
        string latitude = "";
        string longitude = "";
        string naaccr_gis_coordinate_quality_code = "";
        string naaccr_gis_coordinate_quality_type = "";
        string naaccr_census_tract_certainty_code = "";
        string naaccr_census_tract_certainty_type = "";
        string census_state_fips = "";
        string census_county_fips = "";
        string census_tract_fips = "";
        string census_cbsa_fips = "";
        string census_cbsa_micro = "";
        string census_met_div_fips = "";
        urban_status = "";
        state_county_fips = "";

        var outputGeocode_data = geocode_data.OutputGeocode;
        var censusValues_data = geocode_data.Census_Value;
        
        if
        (
            outputGeocode_data != null && 
            outputGeocode_data.FeatureMatchingResultType != null &&
            !outputGeocode_data.FeatureMatchingResultType.Equals("Unmatchable", StringComparison.OrdinalIgnoreCase)
        )
        {
            latitude = outputGeocode_data.Latitude;
            longitude = outputGeocode_data.Longitude;
            feature_matching_geography_type = outputGeocode_data.FeatureMatchingGeographyType;
            naaccr_gis_coordinate_quality_code = outputGeocode_data.NAACCRGISCoordinateQualityCode;
            naaccr_gis_coordinate_quality_type = outputGeocode_data.NAACCRGISCoordinateQualityType;
            naaccr_census_tract_certainty_code = censusValues_data?.NAACCRCensusTractCertaintyCode;
            naaccr_census_tract_certainty_type = censusValues_data?.NAACCRCensusTractCertaintyType;
            census_state_fips = censusValues_data?.CensusStateFips;
            census_county_fips = censusValues_data?.CensusCountyFips;
            census_tract_fips = censusValues_data?.CensusTract;
            census_cbsa_fips = censusValues_data?.CensusCbsaFips;
            census_cbsa_micro = censusValues_data?.CensusCbsaMicro;
            census_met_div_fips = censusValues_data?.CensusMetDivFips;
            // calculate urban_status
            if (censusValues_data != null)
            {
                if
                        (
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                            int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                            censusValues_data?.CensusCbsaFips == ""
                        )
                {
                    urban_status = "Rural";
                }
                else if
                (
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) > 0 &&
                    int.Parse(censusValues_data?.NAACCRCensusTractCertaintyCode) < 7 &&
                    int.Parse(censusValues_data?.CensusCbsaFips) > 0
                )
                {
                    if (!string.IsNullOrEmpty(censusValues_data?.CensusMetDivFips))
                    {
                        urban_status = "Metropolitan Division";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 0)
                    {
                        urban_status = "Metropolitan";
                    }
                    else if (int.Parse(censusValues_data?.CensusCbsaMicro) == 1)
                    {
                        urban_status = "Micropolitan";
                    }
                }
                else
                {
                    urban_status = "Undetermined";
                } 
            }

            // calculate state_county_fips
            if (!String.IsNullOrEmpty(censusValues_data?.CensusStateFips) && !String.IsNullOrEmpty(censusValues_data?.CensusCountyFips))
            {
                state_county_fips = censusValues_data?.CensusStateFips + censusValues_data?.CensusCountyFips;
            }

            facility_of_delivery_location_latitude = latitude;
            facility_of_delivery_location_longitude = longitude;
        }

        new_case.SetS_String("tracking/q9/feature_matching_geography_type", feature_matching_geography_type);
        new_case.SetS_String("tracking/q9/latitude", latitude);
        new_case.SetS_String("tracking/q9/longitude", longitude);
        new_case.SetS_String("tracking/q9/naaccr_gis_coordinate_quality_code", naaccr_gis_coordinate_quality_code);
        new_case.SetS_String("tracking/q9/naaccr_gis_coordinate_quality_type", naaccr_gis_coordinate_quality_type);
        new_case.SetS_String("tracking/q9/naaccr_census_tract_certainty_code", naaccr_census_tract_certainty_code);
        new_case.SetS_String("tracking/q9/naaccr_census_tract_certainty_type", naaccr_census_tract_certainty_type);
        new_case.SetS_String("tracking/q9/census_state_fips", census_state_fips);
        new_case.SetS_String("tracking/q9/census_county_fips", census_county_fips);
        new_case.SetS_String("tracking/q9/census_tract_fips", census_tract_fips);
        new_case.SetS_String("tracking/q9/census_cbsa_fips", census_cbsa_fips);
        new_case.SetS_String("tracking/q9/census_cbsa_micro", census_cbsa_micro);
        new_case.SetS_String("tracking/q9/census_met_div_fips", census_met_div_fips);
        new_case.SetS_String("tracking/q9/urban_status", urban_status);
        new_case.SetS_String("tracking/q9/state_county_fips", state_county_fips);
        
    }


    public sealed class GeocodeTuple
    {
        public GeocodeTuple(){}

        public mmria.common.texas_am.OutputGeocode OutputGeocode {get;set;}
        public mmria.common.texas_am.CensusValue Census_Value {get;set;}

    }

    public sealed class Metadata_Node
    {
        public Metadata_Node(){}
        public bool is_multiform { get; set; }
        public bool is_grid { get; set; }

        public string path {get;set;}

        public string sass_export_name {get;set;}
        public mmria.common.metadata.node Node { get; set; }

        public Dictionary<string,string> display_to_value { get; set; }
        public Dictionary<string,string> value_to_display { get; set; }
    }

    private List<Metadata_Node> get_metadata_node_by_type(mmria.common.metadata.app p_metadata, string p_type)
    {
        var result = new List<Metadata_Node>();
        foreach(var node in p_metadata.children)
        {
            var current_type = node.type.ToLowerInvariant();
            if(current_type == p_type)
            {
                result.Add(new Metadata_Node()
                {
                    is_multiform = false,
                    is_grid = false,
                    path = node.name,
                    Node = node,
                    sass_export_name = node.sass_export_name
                });
            }
            else if(current_type == "form")
            {
                if
                (
                    node.cardinality == "+" ||
                    node.cardinality == "*"
                )
                {
                    get_metadata_node_by_type(ref result, node, p_type, true, false, node.name);
                }
                else
                {
                    get_metadata_node_by_type(ref result, node, p_type, false, false, node.name);
                }
            }
        }
        return result;
    }

private void get_metadata_node_by_type(ref List<Metadata_Node> p_result, mmria.common.metadata.node p_node, string p_type, bool p_is_multiform, bool p_is_grid, string p_path)
    {
        var current_type = p_node.type.ToLowerInvariant();
        if(current_type == p_type)
        {
            var value_to_display = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var display_to_value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if
            (
                current_type == "list"
            )
            {

                if(!string.IsNullOrWhiteSpace(p_node.path_reference))
                {
                    //var key = "lookup/" + p_node.name;
                    var key = p_node.path_reference;
                    if(this.lookup.ContainsKey(key))
                    {
                        var values = this.lookup[key];

                        p_node.values = values;
                    }
                }

                foreach(var value_item in p_node.values)
                {
                    var value = value_item.value;
                    var display = value_item.display;

                    if(!value_to_display.ContainsKey(value))
                    {
                        value_to_display.Add(value, display);
                    }

                    if(!display_to_value.ContainsKey(display))
                    {
                        display_to_value.Add(display, value);
                    }
                }
            }

            p_result.Add(new Metadata_Node()
            {
                is_multiform = p_is_multiform,
                is_grid = p_is_grid,
                path = p_path,
                Node = p_node,
                value_to_display = value_to_display,
                display_to_value = display_to_value,
                sass_export_name = p_node.sass_export_name
            });
        }
        else if(p_node.children != null)
        {
            foreach(var node in p_node.children)
            {
                if(current_type == "grid")
                {
                    get_metadata_node_by_type(ref p_result, node, p_type, p_is_multiform, true, p_path + "/" + node.name);
                }
                else
                {
                    get_metadata_node_by_type(ref p_result, node, p_type, p_is_multiform, p_is_grid, p_path + "/" + node.name);
                }
            }
        }
    }

    private mmria.common.metadata.node get_metadata_node(mmria.common.metadata.app p_metadata, string p_path)
    {
        mmria.common.metadata.node result = null;

        mmria.common.metadata.node current = null;
        
        string[] path = p_path.Split("/");

        for(int i = 0; i < path.Length; i++)
        {
            string current_name = path[i];
            if(i == 0)
            {
                foreach(var child in p_metadata.children)
                {
                    if(child.name.Equals(current_name, StringComparison.OrdinalIgnoreCase))
                    {
                        current = child;
                        break;
                    }
                }
            }

            else
            {

                if(current.children != null)
                {
                    foreach(var child2 in current.children)
                    {
                        if(child2.name.Equals(current_name, StringComparison.OrdinalIgnoreCase))
                        {
                            current = child2;
                            break;
                        }
                    }	
                }
                else
                {
                    return result;
                }

                if(i == path.Length -1)
                {
                    result = current;
                }
            }

        }

        return result;
    }
    struct Result_Struct
    {
        public System.Dynamic.ExpandoObject[] docs;
    }


    private Dictionary<string, mmria.common.metadata.value_node[]> get_look_up(mmria.common.metadata.app p_metadata)
    {
        var result = new Dictionary<string, mmria.common.metadata.value_node[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in p_metadata.lookup)
        {
            result.Add("lookup/" + node.name, node.values);
        }
        return result;
    }


    private  mmria.common.metadata.value_node[] get_metadata_value_node(string search_path, mmria.common.metadata.app p_metadata, string path = "")
    {
        mmria.common.metadata.value_node[] result = null;

        foreach (var node in p_metadata.children)
        {
            result = get_metadata_value_node(search_path, node, node.name);
            if(result != null) break;
        }
        return result;
    }

    private mmria.common.metadata.value_node[] get_metadata_value_node(string search_path, mmria.common.metadata.node p_metadata, string path = "")
    {
        mmria.common.metadata.value_node[] result = null;
        string key = $"{path}/{p_metadata.name}";
        if(search_path.Equals(path, StringComparison.OrdinalIgnoreCase))
        {
            if(! string.IsNullOrWhiteSpace(p_metadata.path_reference))
            {
                //result = lookup[p_metadata.path_reference];
            }
            else
            {
                result = p_metadata.values;
            }
        }
        else if(p_metadata.children!= null)
        {
            foreach (var node in p_metadata.children)
            {
                result = get_metadata_value_node(search_path, node, $"{path}/{node.name}");
                if(result != null) break;
            }
        }
        return result;
    }

    private static string ConvertHHmm_To_MMRIATime(string value)
    {
        string result = value;
        try
        {
            /*
                42 => 00:42:00
                1945 => 19:45:00
                1530 => 15:30:00
                815 => 08:15:00


                42 => 00:42
                1945 => 19:45
                1530 => 15:30
                815 => 08:15
            */
            //Ensure three digit times parse with 4 digits, e.g. 744 becomes 0744 and will be parsed to 7:44 AM
            switch (value.Length)
            {
                case 0:
                    break;
                case 1:
                    result = $"00:0{value}:00";
                    break;
                case 2:
                    result = $"00:{value}:00";
                    break;
                case 3:
                    result = $"0{value[0]}:{value[1..^0]}:00";
                    break;
                case 4:
                    result = $"{value[0..2]}:{value[2..^0]}:00";
                    break;


                default:
                    //result = $"{value.Substring(0,2)}:{value.Substring(2,2)}";
                    System.Console.Write($"ConvertHHmm_To_MMRIATime unable to convert {value}");
                    break;
            }
        }
        catch (Exception ex)
        {
            //Error parsing, eat it and put exact text in as to not lose data on import
            //result = value;
        }

        return result;
    }



    mmria.common.niosh.NioshResult get_niosh_codes(string p_occupation, string p_industry)
    {
        var result = new mmria.common.niosh.NioshResult();
        var builder = new StringBuilder();
        builder.Append("https://wwwn.cdc.gov/nioccs/IOCode?n=3");
        var has_occupation = false;
        var has_industry = false;

        if(!string.IsNullOrWhiteSpace(p_occupation))
        {
            has_occupation = true;
            builder.Append($"&o={p_occupation}");
        }

        if(!string.IsNullOrWhiteSpace(p_industry))
        {
            has_industry = true;
            builder.Append($"&i={p_industry}");
        }

        


        if(has_occupation || has_industry)
        {
            var niosh_url = builder.ToString();

            var niosh_curl = new mmria.getset.cURL("GET", null, niosh_url, null);

            try
            {
                string responseFromServer = niosh_curl.execute();

                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.niosh.NioshResult>(responseFromServer);
            }
            catch
            {
                result.is_error = true;
            }
            
        }
        //{"Industry": [{"Code": "611110","Title": "Elementary and Secondary Schools","Probability": "9.999934E-001"},{"Code": "611310","Title": "Colleges, Universities, and Professional Schools","Probability": "2.598214E-006"},{"Code": "009990","Title": "Insufficient information","Probability": "2.312557E-006"}],"Occupation": [{"Code": "00-9900","Title": "Insufficient Information","Probability": "9.999897E-001"},{"Code": "11-9032","Title": "Education Administrators, Elementary and Secondary School","Probability": "6.550550E-006"},{"Code": "53-3022","Title": "Bus Drivers, School or Special Client","Probability": "4.932875E-007"}],"Scheme": "NAICS 2012 and SOC 2010"}
        return result;

    }


	public (string, mmria.common.cvs.tract_county_result) GetCVSData
    (
        string c_geoid,
		string t_geoid,
		string year,
        mmria.common.couchdb.ConfigurationSet ConfigDB
    ) 
    { 

        mmria.common.cvs.tract_county_result result = null;
        var response_string = string.Empty;

        var base_url = ConfigDB.name_value["cvs_api_url"];

        try
        {
            
            var get_all_data_body = new mmria.common.cvs.get_all_data_post_body()
            {
                id = ConfigDB.name_value["cvs_api_id"],
                secret = ConfigDB.name_value["cvs_api_key"],
                payload = new()
                {
                    
                    c_geoid = c_geoid,
                    t_geoid = t_geoid,
                    year = year
                }
            };

            var body_text =  System.Text.Json.JsonSerializer.Serialize(get_all_data_body);
            var get_all_data_curl = new mmria.getset.cURL("POST", null, base_url, body_text);

            response_string = get_all_data_curl.execute();
            System.Console.WriteLine(response_string);

            result = System.Text.Json.JsonSerializer.Deserialize<mmria.common.cvs.tract_county_result>(response_string);
        
        }
        catch(System.Net.WebException ex)
        {
            System.Console.WriteLine($"cvsAPIController  POST\n{ex}");
            
            /*return Problem(
                type: "/docs/errors/forbidden",
                title: "CVS API Error",
                detail: ex.Message,
                statusCode: (int) ex.Status,
                instance: HttpContext.Request.Path
            );*/

            return ($"Status: {ex.Status} {ex.Message} {response_string}", null);
        }

        return ("success", result);
    }


    public List<int> CVS_Get_Valid_Years(mmria.common.couchdb.ConfigurationSet ConfigDB) 
    { 
        var result = new List<int>()
		{
			2010,
			2011,
			2012,
			2013,
			2014,
			2015,
			2016,
			2017,
			2018,
			2019,
			2020
		};

        var base_url = ConfigDB.name_value["cvs_api_url"];

        try
        {


			var get_year_body = new mmria.common.cvs.get_year_post_body()
			{
				id = ConfigDB.name_value["cvs_api_id"],
				secret = ConfigDB.name_value["cvs_api_key"],
				payload = new()
			};

			var body_text =  System.Text.Json.JsonSerializer.Serialize(get_year_body);
			var get_year_curl = new mmria.getset.cURL("POST", null, base_url, body_text);
			string get_year_response = get_year_curl.execute();
			result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>> (get_year_response);

			System.Console.WriteLine(get_year_response);

    
        }
        catch(System.Net.WebException ex)
        {
            System.Console.WriteLine($"cvsAPIController Get Year POST\n{ex}");
            
            /*return Problem(
                type: "/docs/errors/forbidden",
                title: "CVS API Error",
                detail: ex.Message,
                statusCode: (int) ex.Status,
                instance: HttpContext.Request.Path
            );*/
        }


        return result;
    }	

    bool is_result_quality_in_need_of_checking(mmria.common.cvs.tract_county_result val)
	{

		var over_all_result = false;
		var tract_result = false;
		var county_result = false;

		const float tract_total = 11F;
		const float county_total = 26F;

		float tract_zero_count = 0F;
		float county_zero_count = 0F;

		const float _30_percent_correct = .3F;

		if
		(
			val.tract.pctMOVE == 0  && //cvs_pctmove_tract
			val.tract.pctNOIns_Fem == 0 && //cvs_pctnoins_fem_tract		
			val.county.pctNoVehicle == 0 && //cvs_pctnovehicle_county
			val.tract.pctNoVehicle == 0 && //cvs_pctnovehicle_tract
			val.tract.pctOWNER_OCC == 0 //cvs_pctowner_occ_tract
		)
		{
			over_all_result = true;
		}


		if(val.tract.pctNOIns_Fem == 0) tract_zero_count += 1;
		if(val.tract.MEDHHINC == 0) tract_zero_count += 1;
		if(val.tract.pctNoVehicle == 0) tract_zero_count += 1;
		if(val.tract.pctMOVE == 0) tract_zero_count += 1;
		if(val.tract.pctSPHH == 0) tract_zero_count += 1;
		if(val.tract.pctOVERCROWDHH == 0) tract_zero_count += 1;
		if(val.tract.pctOWNER_OCC == 0) tract_zero_count += 1;
		if(val.tract.pct_less_well == 0) tract_zero_count += 1;
		if(val.tract.NDI_raw == 0) tract_zero_count += 1;
		if(val.tract.pctPOV == 0) tract_zero_count += 1;
		if(val.tract.ICE_INCOME_all == 0) tract_zero_count += 1;



		if(val.county.MDrate == 0) county_zero_count += 1;
		if(val.county.pctNOIns_Fem == 0) county_zero_count += 1;
		if(val.county.pctNoVehicle == 0) county_zero_count += 1;
		if(val.county.pctMOVE == 0) county_zero_count += 1;
		if(val.county.pctSPHH == 0) county_zero_count += 1;
		if(val.county.pctOVERCROWDHH == 0) county_zero_count += 1;
		if(val.county.pctOWNER_OCC == 0) county_zero_count += 1;
		if(val.county.pct_less_well == 0) county_zero_count += 1;
		if(val.county.NDI_raw == 0) county_zero_count += 1;
		if(val.county.pctPOV == 0) county_zero_count += 1;
		if(val.county.ICE_INCOME_all == 0) county_zero_count += 1;
		if(val.county.MEDHHINC == 0) county_zero_count += 1;
		if(val.county.pctOBESE == 0) county_zero_count += 1;
		if(val.county.FI == 0) county_zero_count += 1;
		if(val.county.CNMrate == 0) county_zero_count += 1;
		if(val.county.OBGYNrate == 0) county_zero_count += 1;
		if(val.county.rtTEENBIRTH == 0) county_zero_count += 1;
		if(val.county.rtSTD == 0) county_zero_count += 1;
		if(val.county.rtMHPRACT == 0) county_zero_count += 1;
		if(val.county.rtDRUGODMORTALITY == 0) county_zero_count += 1;
		if(val.county.rtOPIOIDPRESCRIPT == 0) county_zero_count += 1;
		if(val.county.SocCap == 0) county_zero_count += 1;
		if(val.county.rtSocASSOC == 0) county_zero_count += 1;
		if(val.county.pctHOUSE_DISTRESS == 0) county_zero_count += 1;
		if(val.county.rtVIOLENTCR_ICPSR == 0) county_zero_count += 1;
		if(val.county.isolation == 0) county_zero_count += 1;


		if(tract_zero_count / tract_total < _30_percent_correct) tract_result = true;

		if(county_zero_count / county_total < _30_percent_correct) county_result = true;


		return over_all_result || tract_result || county_result;
	}



    static List<List<string>> ParseCsv(ReadOnlySpan<char> csv) 
    {
        var result = new List<List<string>>();
        var row = new List<string>();
        string field = "";
        bool inQuotedField = false;

        for (int i = 0; i < csv.Length; i++) 
        {
            char current = csv[i];
            char next = i == csv.Length - 1 ? ' ' : csv[i + 1];

            if 
            (
                (
                    current != '"' && 
                    current != ',' && 
                    current != '\r' && 
                    current != '\n'
                ) || 
                (
                    current != '"' && 
                    inQuotedField
                )
            ) 
            {
                field += current;
            } 
            else if (current == ' ' || current == '\t') 
            {
                continue;
            } 
            else if (current == '"') 
            {
                if (inQuotedField && next == '"') 
                {
                    i++;
                    field += current;
                } 
                else if (inQuotedField) 
                {
                    row.Add(field);
                    if (next == ',') 
                    {
                        i++;
                    }
                    field = "";
                    inQuotedField = false;
                } 
                else 
                {
                    inQuotedField = true; 
                }
            } 
            else if (current == ',') 
            {
                row.Add(field);
                field = "";
            } 
            else if (current == '\n') 
            {
                row.Add(field);
                result.Add(new List<string>(row));
                field = "";
                row.Clear();
            }
        }

        return result;
    }


    int calc_race_omb
    (
        string p_white, 
        string p_black, 
        string p_amindalknat, 
        string p_asianindian, 
        string p_chinese, 
        string p_filipino, 
        string p_japanese, 
        string p_korean, 
        string p_vietnamese, 
        string p_otherasian, 
        string p_nativehawaiian, 
        string p_guamcham, 
        string p_samoan, 
        string p_otherpacific, 
        string p_other, 
        string p_notspecified
    ) 
    {
        int race_omb = 9999;

        int rW = 0;
        int rB = 0;
        int rA = 0;
        int rAiAn = 0;
        int rPI = 0;
        int rO = 0;
        int rNS = 0;
        
        if 
        (
            p_white == "N" && 
            p_black == "N" && 
            p_amindalknat == "N" && 
            p_asianindian == "N" && 
            p_chinese == "N" && 
            p_filipino == "N" && 
            p_japanese == "N" && 
            p_korean == "N" && 
            p_vietnamese == "N" && 
            p_otherasian == "N" && 
            p_nativehawaiian == "N" && 
            p_guamcham == "N" && 
            p_samoan == "N" && 
            p_otherpacific == "N" && 
            p_other == "N" && 
            p_notspecified == "N"
        ) 
        {
        rNS = 1;
        }
        else
        {
            if (p_notspecified == "Y")
            { 
                rNS = 1;
            }
            if (p_white == "Y") 
            {
                rW = 1;
            }
            if (p_black == "Y") 
            {
                rB = 1;
            }
            if (p_amindalknat == "Y")
            {
                rAiAn = 1;
            }
            if (p_other == "Y") 
            {
                rO = 1;
            }
        
            if (p_asianindian == "Y")
            {
                rA = 1;
            }
            if (p_chinese == "Y")
            {
                rA = 1;
            }
            if (p_filipino == "Y")
            {
                rA = 1;
            }
            if (p_japanese == "Y")
            {
                rA = 1;
            }
            if (p_korean == "Y")
            {
                rA = 1;
            }
            if (p_vietnamese == "Y")
            {
                rA = 1;
            }
            if (p_otherasian == "Y")
            {
                rA = 1;
            }
        
            if (p_nativehawaiian == "Y")
            {
                rPI = 1;
            }
            if (p_guamcham == "Y")
            {
                rPI = 1;
            }
            if (p_samoan == "Y")
            {
                rPI = 1;
            }
            if (p_otherpacific == "Y")
            {
                rPI = 1;
            }
        }
        
        //if ((rW + rB + rAiAn + rA + rPI + rO + rNS) == 1)
        if ((rW + rB + rAiAn + rA + rPI + rO) == 1)
        {
            if (rW == 1)
            {
                race_omb = 1;
            }
            else if (rB == 1)
            {
                race_omb = 2;
            }
            else if (rA == 1) 
            {
                race_omb = 3;
            }
            else if (rAiAn == 1)
            {
                race_omb = 4;
            }
            else if (rPI == 1)
            {
                race_omb = 5;
            }
            else if (rO == 1) 
            {
                race_omb = 8;
            }
            else if (rNS == 1)
            {
                race_omb = 9;
            }
        }
        
        else if ((rW + rB + rAiAn + rA + rPI + rO) == 2)
        {
            race_omb = 6;
        }
        else if ((rW + rB + rAiAn + rA + rPI + rO) > 2)
        {
            race_omb = 7;
        }
        else if (rNS == 1) 
        {
            race_omb = 9;
        }

        return race_omb;
    }


    public string GetNextPMSSNumber
    (
        string prefix
    )
    {
        var result = new List<string>();

        var prefix_array = prefix.Split("-");

        try
        {
            string request_string = $"{db_config.url}/{db_config.prefix}mmrds/_design/sortable/_view/by_pmss_number?skip=0&take=250000";

            var case_view_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = case_view_curl.execute();

            mmria.common.model.couchdb.pmss_case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.pmss_case_view_response>(responseFromServer);

            foreach (mmria.common.model.couchdb.pmss_case_view_item cvi in case_view_response.rows)
            {
                if(cvi.value.pmssno.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(cvi.value.pmssno);
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return $"{prefix}-{(result.Count + 1).ToString().PadLeft(4,'0')}";
    }


}