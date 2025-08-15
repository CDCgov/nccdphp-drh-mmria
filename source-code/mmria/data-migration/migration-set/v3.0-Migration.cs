using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.set;

public sealed class v3_0_Migration
{


	private Microsoft.Extensions.Configuration.IConfiguration configuration;

	public string host_db_url;
	public string db_name;
	public string config_timer_user_name;
	public string config_timer_value;

	public bool is_report_only_mode;

	public System.Text.StringBuilder output_builder;
	private Dictionary<string,mmria.common.metadata.value_node[]> lookup;

	List<Metadata_Node> all_list_set;

	List<Metadata_Node> single_form_value_set;
	List<Metadata_Node> single_form_multi_value_set;
	List<Metadata_Node> single_form_grid_value_set;
	List<Metadata_Node> single_form_grid_multi_value_list_set;
	List<Metadata_Node> multiform_value_set;
	List<Metadata_Node> multiform_multi_value_set;
	List<Metadata_Node> multiform_grid_value_set;

	List<Metadata_Node> multiform_grid_multi_value_set;

	public Dictionary<string, HashSet<string>> summary_value_dictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

	public bool is_data_correction = false;

	mmria.common.couchdb.ConfigurationSet configDB;

	migrate.common.CVS_API CVS_API = null;


	public v3_0_Migration
	(
		string p_host_db_url, 
		string p_db_name, 
		string p_config_timer_user_name, 
		string p_config_timer_value,
		System.Text.StringBuilder p_output_builder,
		Dictionary<string, HashSet<string>> p_summary_value_dictionary,
		bool p_is_report_only_mode
	) 
	{

		host_db_url = p_host_db_url;
		db_name = p_db_name;
		config_timer_user_name = p_config_timer_user_name;
		config_timer_value = p_config_timer_value;
		output_builder = p_output_builder;
		summary_value_dictionary = p_summary_value_dictionary;
		is_report_only_mode = p_is_report_only_mode;

		 
	}


	public async Task execute()
	{
		this.output_builder.AppendLine($"v3.0 Data Migration started at: {DateTime.Now.ToString("o")}");
		DateTime begin_time = System.DateTime.Now;
		
		this.output_builder.AppendLine($"v3_0_Migration started at: {begin_time.ToString("o")}");
		
		var gs = new C_Get_Set_Value(this.output_builder);

		

		try
		{

			//string metadata_url = host_db_url + "/metadata/2016-06-12T13:49:24.759Z";
			string metadata_url = $"https://couchdb-test-mmria.apps.ecpaas-dev.cdc.gov/metadata/version_specification-22.09.13/metadata";

			//string metadata_url = $"{host_db_url}/metadata/version_specification-20.12.01/metadata";
			
			cURL metadata_curl = new cURL("GET", null, metadata_url, null, null, null);
			mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(await metadata_curl.executeAsync());
		
			//var Valid_CVS_Years = await CVS_Get_Valid_Years();

			this.lookup = get_look_up(metadata);

			all_list_set = get_metadata_node_by_type(metadata, "list");

			single_form_value_set = all_list_set.Where(o=> o.is_multiform == false && o.is_grid == false && o.Node.is_multiselect == null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();
			single_form_multi_value_set = all_list_set.Where(o=> o.is_multiform == false && o.is_grid == false && o.Node.is_multiselect != null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();

			single_form_grid_value_set = all_list_set.Where(o=> o.is_multiform == false && o.is_grid == true && o.Node.is_multiselect == null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();
			single_form_grid_multi_value_list_set = all_list_set.Where(o=> o.is_multiform == false && o.is_grid == true && o.Node.is_multiselect != null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();

			multiform_value_set = all_list_set.Where(o=> o.is_multiform == true && o.is_grid == false && o.Node.is_multiselect == null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();
			multiform_multi_value_set = all_list_set.Where(o=> o.is_multiform == true && o.is_grid == false && o.Node.is_multiselect != null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();

			multiform_grid_value_set = all_list_set.Where(o=> o.is_multiform == true && o.is_grid == true && o.Node.is_multiselect == null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();
			multiform_grid_multi_value_set = all_list_set.Where(o=> o.is_multiform == true && o.is_grid == true && o.Node.is_multiselect != null && (o.Node.control_style == null || !o.Node.control_style.Equals("editable",StringComparison.OrdinalIgnoreCase))).ToList();


			var total_count = single_form_value_set.Count + single_form_grid_value_set.Count + multiform_value_set.Count + multiform_grid_value_set.Count + single_form_multi_value_set.Count + single_form_grid_multi_value_list_set.Count + multiform_multi_value_set.Count + multiform_grid_multi_value_set.Count;
			System.Console.WriteLine($"all_list_set.Count: {all_list_set.Count} total_count: {total_count}");
			System.Console.WriteLine($"is count the same: {all_list_set.Count == single_form_value_set.Count + single_form_grid_value_set.Count + multiform_value_set.Count + multiform_grid_value_set.Count + single_form_multi_value_set.Count + single_form_grid_multi_value_list_set.Count + multiform_multi_value_set.Count + multiform_grid_multi_value_set.Count}");




			const string occupationImpacted_path = "social_and_environmental_profile/socio_economic_characteristics/occupationImpacted";

			const string sep_m_industry_code_1_path = "social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_1";
			const string sep_m_industry_code_2_path = "social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_2";
			const string sep_m_industry_code_3_path = "social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_3";
			const string sep_m_occupation_code_1_path = "social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_1";
			const string sep_m_occupation_code_2_path = "social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_2";
			const string sep_m_occupation_code_3_path = "social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_3";




			var ExistingRecordIds = await GetExistingRecordIds();

			string url = $"{host_db_url}/{db_name}/_all_docs?include_docs=true";
			var case_curl = new cURL("GET", null, url, null, config_timer_user_name, config_timer_value);
			string responseFromServer = await case_curl.executeAsync();
			var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<System.Dynamic.ExpandoObject>>(responseFromServer);
			
			foreach(var case_item in case_response.rows)
			{
				var case_has_changed = false;
				var case_change_count = 0;

				var doc = case_item.doc;
				
				if(doc != null)
				{

					C_Get_Set_Value.get_value_result value_result = gs.get_value(doc, "_id");
					var mmria_id = value_result.result.ToString();
					
					
					value_result = gs.get_value(doc, "home_record/record_id");
					var mmria_record_id = "";
					
					if(value_result.result != null)
						mmria_record_id = value_result.result.ToString();

					if(mmria_id.IndexOf("_design") > -1)
					{
						continue;
					}


					string primary_occupation = null;
					string business_industry = null;

					value_result = gs.get_value(doc, "social_and_environmental_profile/socio_economic_characteristics/occupation");
					if
					(
						!value_result.is_error && 
						value_result.result != null &&
						!string.IsNullOrWhiteSpace(value_result.result.ToString())
					)
					{
						primary_occupation = value_result.result.ToString();
					}

					var niosh_result = get_niosh_codes
					(
						primary_occupation,
						business_industry
					);

					case_has_changed = gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_1", "", doc);
					case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_2",  "", doc);
					case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_3",  "", doc);
					case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_1",  "", doc);
					case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_2", "", doc);
					case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_3", "", doc);


					if
					(
						!niosh_result.is_error && 
						(
							niosh_result.Industry.Count > 0 ||
							niosh_result.Occupation.Count > 0 
						)
					)
					{   
						if(niosh_result.Industry.Count > 0)                      
						case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_1", niosh_result.Industry[0].Code, doc);
						if(niosh_result.Industry.Count > 1)
						case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_2",  niosh_result.Industry[1].Code, doc);
						if(niosh_result.Industry.Count > 2)
						case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_industry_code_3",  niosh_result.Industry[2].Code, doc);
						if(niosh_result.Occupation.Count > 0)
						case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_1",  niosh_result.Occupation[0].Code, doc);
						if(niosh_result.Occupation.Count > 1)
						case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_2", niosh_result.Occupation[1].Code, doc);
						if(niosh_result.Occupation.Count > 2)
						case_has_changed = case_has_changed && gs.set_value("social_and_environmental_profile/socio_economic_characteristics/sep_m_occupation_code_3", niosh_result.Occupation[2].Code, doc);
					}

					var output_text = $"item id: {mmria_id} case_has_changed: Niosh updated: {case_has_changed} record_id:{mmria_record_id} ";
					this.output_builder.AppendLine(output_text);
					Console.WriteLine(output_text);



/*
				if(mmria_id == "")
				{
					var save_result = await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(doc as IDictionary<string, object>,"v3.0");
				}
				else*/ 
				
				if(!is_report_only_mode && case_has_changed)
				{
					var save_result = await new SaveRecord(this.host_db_url, this.db_name, this.config_timer_user_name, this.config_timer_value, this.output_builder).save_case(doc as IDictionary<string, object>,"v3.0");
				}

			}

		
		}
	}
	catch(Exception ex)
	{
		Console.WriteLine(ex);
	}

	Console.WriteLine($"v3_0_Migration Finished {DateTime.Now}");
}

	bool isInNeedOfConversion(string p_value)
	{
		var result = true;

		if(p_value != null)
		{
			if
			(
				p_value.Trim() != "1"
			)
			{
				result = false;
			}
		}


		return result;
	}



	string ConvertToStandardTime(string p_value)
	{
		var result = p_value;

		if(p_value != null)
		{
			var upper = p_value.Trim().ToUpper();
			if
			(
				upper.Contains("AM")
			)
			{
				var data = upper.Replace(" AM","").Split(":");
				if(data[0].Contains("12"))
				{
					data[0] = "0";
				}
				result =string.Join(':',data);
			}
			else if(upper.Contains("PM"))
			{
				var data = upper.Replace(" PM","").Split(":");
				if(int.TryParse(data[0], out var hour))
				{
					var new_hour = hour + 12;
					if(hour == 12)
					{
						new_hour = 12;
					}

					if(new_hour < 24)
					{
						data[0] = new_hour.ToString();
						result =string.Join(':',data);
					}
					else
					{
						System.Console.WriteLine("Hour greater than 24...I should never happen");
					}
				}
				else
				{
					System.Console.WriteLine("unable to parse data[0] should never happen");
				}
		
			}
			else
			{
				System.Console.WriteLine("I should never happen");
			}
		}


		return result;
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
	private Dictionary<string,mmria.common.metadata.value_node[]> get_look_up(mmria.common.metadata.app p_metadata)
	{
		var result = new Dictionary<string,mmria.common.metadata.value_node[]>(StringComparer.OrdinalIgnoreCase);

		foreach(var node in p_metadata.lookup)
		{
			result.Add("lookup/" + node.name, node.values);
		}
		return result;
	}	


	public async Task<HashSet<string>> GetExistingRecordIds()
	{
		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


		try
		{        
			string request_string = $"{host_db_url}/{db_name}/_design/sortable/_view/by_date_created?skip=0&take=25000";

			var case_view_curl = new cURL("GET", null, request_string, null, config_timer_user_name, config_timer_value);
			string responseFromServer = await case_view_curl.executeAsync();

			mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

			foreach(mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
			{
				result.Add(cvi.value.record_id);

			}
		}
		catch(Exception ex) 
		{
			Console.WriteLine (ex);
		}

		return result;
	} 

	mmria.common.niosh.NioshResult get_niosh_codes(string p_occupation, string p_industry)
    {
        var result = new mmria.common.niosh.NioshResult();
        var builder = new System.Text.StringBuilder();
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

            var niosh_curl = new cURL("GET", null, niosh_url, null);

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

	public void SetConfiguration(Microsoft.Extensions.Configuration.IConfiguration value)
	{
		configuration = value;

	}


	public void SetConfigDB(mmria.common.couchdb.ConfigurationSet value)
	{
		configDB = value;
		
	}

}
