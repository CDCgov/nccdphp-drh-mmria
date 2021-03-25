using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;


namespace migrate.set
{

    public class v2_4RaceRecode
    {

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


        public v2_4RaceRecode
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
			this.output_builder.AppendLine($"v2_4RaceRecode Data Migration started at: {DateTime.Now.ToString("o")}");
			DateTime begin_time = System.DateTime.Now;
			
			this.output_builder.AppendLine($"v2_4RaceRecode started at: {begin_time.ToString("o")}");
			
            var gs = new C_Get_Set_Value(this.output_builder);
			try
			{
				//string metadata_url = host_db_url + "/metadata/2016-06-12T13:49:24.759Z";
				string metadata_url = $"https://testdb-mmria.services-dev.cdc.gov/metadata/version_specification-20.12.01/metadata";

				//string metadata_url = $"{host_db_url}/metadata/version_specification-20.12.01/metadata";
				
				cURL metadata_curl = new cURL("GET", null, metadata_url, null, null, null);
				mmria.common.metadata.app metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(await metadata_curl.executeAsync());
            
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

				string url = $"{host_db_url}/{db_name}/_all_docs?include_docs=true";
				var case_curl = new cURL("GET", null, url, null, config_timer_user_name, config_timer_value);
				string responseFromServer = await case_curl.executeAsync();
				
				var race_value_list = this.lookup["lookup/race"];
				var omb_race_recode_list = this.lookup["lookup/omb_race_recode"];
				var value_to_display = new Dictionary<int,string>();
				var display_to_value = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				foreach(var item in race_value_list)
				{
					value_to_display.Add(int.Parse(item.value), item.display);
					
				}

				foreach(var item in omb_race_recode_list)
				{
					display_to_value.Add(item.display, int.Parse(item.value));
				}

				display_to_value.Add("Black or African American", 1);
				display_to_value.Add("American Indian or Alaska Native", 3);

				var case_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<System.Dynamic.ExpandoObject>>(responseFromServer);

				foreach(var case_item in case_response.rows)
				{
					var case_has_changed = false;
					var case_change_count = 0;

					var doc = case_item.doc;
					
					if(doc != null)
					{

						C_Get_Set_Value.get_value_result value_result = gs.get_value(doc, "_id");
						var mmria_id = value_result.result;
						if(mmria_id.IndexOf("_design") > -1)
						{
							continue;
						}



						value_result = gs.get_value(doc, "home_record/record_id");
						var current_record_id = value_result.result;

						C_Get_Set_Value.get_value_result target_value_result = null;

						var record_id_check = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
						{
							"MT-2009-5978",
							"WA-1983-0907" 
						};


						if(record_id_check.Contains(current_record_id))
						{
							System.Console.WriteLine("here");
						}


						if(mmria_id.ToLower() == "52589344-A091-4D96-810D-57E060E9BBE2".ToLower())
						{
							System.Console.WriteLine("here");
						}

						var source_field_path_list = new List<string>()
						{
							"death_certificate/race/race",
							"birth_fetal_death_certificate_parent/race/race_of_mother",
							"birth_fetal_death_certificate_parent/demographic_of_father/race/race_of_father"
						};
						var target_field_path_list = new List<string>()
						{
							"death_certificate/race/omb_race_recode",
							"birth_fetal_death_certificate_parent/race/omb_race_recode",
							"birth_fetal_death_certificate_parent/demographic_of_father/race/omb_race_recode"


						};

						for(int i = 0; i < source_field_path_list.Count; i++)
						{

							try
							{
								var source_field_path = source_field_path_list[i];
								var target_field_path = target_field_path_list[i];

								value_result = gs.get_value(doc, source_field_path);
								target_value_result = gs.get_value(doc, target_field_path);
								if (!value_result.is_error)
								{
									string race_recode = "9999";
									if(value_result.result != null)
									{
										if(value_result.result is IList<object>)
										{
											var object_list = value_result.result as IList<object>;
											var list = new List<string>();

											foreach(var item in object_list)
											{
												int key = -1;
												if(item is string && item != null)
												{
													if(!int.TryParse(item as string, out key))
													{
														list.Add(item as string);
													}
												}
												if(value_to_display.ContainsKey(key))
												{
													list.Add(value_to_display[key]);
												}
											}
											var omb_recode =  calculate_omb_recode(list);
											if(display_to_value.ContainsKey(omb_recode))
											{
												race_recode = display_to_value[omb_recode].ToString();
											}
											else
											{
												race_recode = omb_recode;
											}
											
											if(target_value_result.result == null || target_value_result.result.ToString() != race_recode)
											{
												if(case_change_count == 0)
												{
													case_change_count += 1;
													case_has_changed = true;
												}
												case_has_changed = case_has_changed && gs.set_value(target_field_path, race_recode, doc);
												var output_text = $"item record_id: {mmria_id} RaceRecoded value set to {race_recode}";
												this.output_builder.AppendLine(output_text);
												Console.WriteLine(output_text);
											}
										
										}
										else
										{

										}
							
									}
									else
									{
										if(!target_value_result.is_error)
										{
											if(target_value_result.result != null)
											{
												if(target_value_result.result.ToString() != race_recode)
												{
													if(case_change_count == 0)
													{
														case_change_count += 1;
														case_has_changed = true;
													}
													case_has_changed = case_has_changed && gs.set_value(target_field_path, race_recode, doc);
													var output_text = $"item record_id: {mmria_id} RaceRecoded value set to {race_recode}";
													this.output_builder.AppendLine(output_text);
													Console.WriteLine(output_text);
												}
											}
											else
											{
												if(case_change_count == 0)
												{
													case_change_count += 1;
													case_has_changed = true;
												}
												case_has_changed = case_has_changed && gs.set_value(target_field_path, race_recode, doc);
												var output_text = $"item record_id: {mmria_id} RaceRecoded value set to {race_recode}";
												this.output_builder.AppendLine(output_text);
												Console.WriteLine(output_text);
											}
										}
									}
								}
							
							}
							catch(Exception ex)
							{
								Console.WriteLine(ex);
							}
						}

						if(!is_report_only_mode && case_has_changed)
						{
							var save_result = await save_case(doc);
						}

					}

            
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
			}

			Console.WriteLine($"v2_4RaceRecode Finished {DateTime.Now}");
    	}
        public class Metadata_Node
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

		private async Task<bool> save_case(IDictionary<string, object> case_item)
        {
            bool result = false;
			var gsv = new C_Get_Set_Value(this.output_builder);

            //var case_item  = p_case_item as System.Collections.Generic.Dictionary<string, object>;

            gsv.set_value("date_last_updated", DateTime.UtcNow.ToString("o"), case_item);
            gsv.set_value("last_updated_by", "migration_plan", case_item);


            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(case_item, settings);

            string put_url = $"{host_db_url}/{db_name}/{case_item["_id"]}";
            cURL document_curl = new cURL ("PUT", null, put_url, object_string, config_timer_user_name, config_timer_value);

            try
            {
                var responseFromServer = await document_curl.executeAsync();
                var	put_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

                if(put_result.ok)
                {
                    result = true;
                }
                
            }
            catch(Exception ex)
            {
                //Console.Write("auth_session_token: {0}", auth_session_token);
                Console.WriteLine(ex);
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


        private string calculate_omb_recode(IList<string> p_value_list)
        {
            string result = "(blank)";
            var asian_list = new string[7]{ 
                    "Asian Indian",
                    "Chinese",
                    "Filipino",
                    "Japanese",
                    "Korean",
                    "Vietnamese",
                    "Other Asian"
                };
            var islander_list = new string[4]{
                    "Native Hawaiian",
                    "Guamanian or Chamorro",
                    "Samoan",
                    "Other Pacific Islander"
                };
            if (p_value_list.Count == 0)
            {
            }
            else if (p_value_list.Count == 1)
            {
                if (get_intersection(p_value_list, asian_list)?.Count > 0) {
                    result = "Asian";
                } else if (get_intersection(p_value_list, islander_list)?.Count > 0) {
                    result = "Pacific Islander";
                } else
                {
                    result = p_value_list[0];
                }
            }
            else
            {
                if (p_value_list.Contains("Race Not Specified"))
                {
                    result = "Race Not Specified";
                }
                else
                {
                    var asian_intersection_count = get_intersection(p_value_list, asian_list)?.Count;
                    var is_asian = 0;
                    var islander_intersection_count = get_intersection(p_value_list, islander_list)?.Count;
                    var is_islander = 0;
                    if (asian_intersection_count > 0)
                        is_asian = 1;
                    if (islander_intersection_count > 0)
                        is_islander = 1;
                    var number_not_in_asian_or_islander_categories = p_value_list.Count - asian_intersection_count - islander_intersection_count;
                    var total_unique_items = number_not_in_asian_or_islander_categories + is_asian + is_islander;
                    switch (total_unique_items)
                    {
                        case 1:
                            if (is_asian == 1)
                            {
                                result = "Asian";
                            }
                            else if (is_islander == 1)
                            {
                                result = "Pacific Islander";
                            }
                            else
                            {
                                Console.WriteLine("This should never happen bug");
                            }
                            break;
                        case 2:
                            result = "Bi-Racial";
                            break;
                        default:
                            result = "Multi-Racial";
                            break;
                    }
                }
            }
            return result;
        }

        public IList<string> get_intersection(IList<string> p_list_1, IList<string> p_list_2)
        {
            var result = p_list_1.Intersect(p_list_2)?.ToArray();

            //var a = p_list_1;
            //var b = p_list_2;
            //a.sort();
            //b.sort();
            //var ai = 0, bi = 0;
            //var result = [];
            //while (ai < a.length && bi < b.length)
            //{
            //    if (a[ai] < b[bi])
            //    {
            //        ai++;
            //    }
            //    else if (a[ai] > b[bi])
            //    {
            //        bi++;
            //    }
            //    else
            //    {
            //        result.push(a[ai]);
            //        ai++;
            //        bi++;
            //    }
            //}
            return result;
        }


    }
}

