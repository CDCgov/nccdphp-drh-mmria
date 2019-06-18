﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Threading.Tasks;

namespace mmria.console
{
	public class convert
	{
		private string auth_token = null;
		private string user_name = null;
		private string password = null;
		private string source_file_path = null;
		private string mmria_url = null;

		//import user_name:user1 password:password database_file_path:mapping-file-set/Maternal_Mortality.mdb url:http://localhost:12345

		public convert()
		{
			

		}
		public async Task Execute(string[] args)
		{

			if (args.Length > 1)
			{
				for (var i = 1; i < args.Length; i++)
				{
					string arg = args[i];
					int index = arg.IndexOf(':');
					string val = arg.Substring(index + 1, arg.Length - (index + 1)).Trim(new char[] { '\"' });

					if (arg.ToLower().StartsWith("auth_token"))
					{
						this.auth_token = val;
					}
					else if (arg.ToLower().StartsWith("user_name"))
					{
						this.user_name = val;
					}
					else if (arg.ToLower().StartsWith("password"))
					{
						this.password = val;
					}
					else if (arg.ToLower().StartsWith("source_file_path"))
					{
						
						this.source_file_path = val;
					}
					else if (arg.ToLower().StartsWith("url"))
					{
						this.mmria_url = val;
					}
				}
			}

/*
			if (string.IsNullOrWhiteSpace(this.source_file_path))
			{
				System.Console.WriteLine("missing source_file_path");
				System.Console.WriteLine(" form source_file_path:[file path]");
				System.Console.WriteLine(" example 1 source_file_path:c:/temp/");
				System.Console.WriteLine(" example 2 source_file_path:\"c:/temp folder/\"");
				System.Console.WriteLine(" example 3 source_file_path:mapping-file-set\\\"");
				System.Console.WriteLine(" mmria-console.exe export user_name:user1 password:secret url:http://localhost:12345 source_file_path:\"c:\\temp folder\\\"");

				return;
			}

			if (string.IsNullOrWhiteSpace(this.mmria_url))
			{
				System.Console.WriteLine("missing url");
				System.Console.WriteLine(" form url:[website_url]");
				System.Console.WriteLine(" example url:http://localhost:12345");

				return;
			}

			if (string.IsNullOrWhiteSpace(this.user_name))
			{
				System.Console.WriteLine("missing user_name");
				System.Console.WriteLine(" form user_name:[user_name]");
				System.Console.WriteLine(" example user_name:user1");
				return;
			}

			if (string.IsNullOrWhiteSpace(this.password))
			{
				System.Console.WriteLine("missing password");
				System.Console.WriteLine(" form password:[password]");
				System.Console.WriteLine(" example password:secret");
				return;
			}



			if (!Directory.Exists(this.source_file_path))
			{
				System.Console.WriteLine($"source_file_path does NOT exist: {this.source_file_path}");
				return;
			}
 */


			var metadata_response = await ReadMetadataAsync();
			Console.Write(metadata_response);

			var metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app>(metadata_response);

			var new_schema = await GetSchema(metadata);


			var location = Directory.GetCurrentDirectory();
			var output_schema_path = Path.Combine(location, @"schema/GeneratedSchema");
			var new_schema_json = new_schema.ToJson();

			var json_schema_path = $"{output_schema_path}/generated_schema.json";
			File.WriteAllText(json_schema_path, new_schema_json);


			//var filename = "address";
			//var filename = "multivalue-list-example";
			var generate_code_file = true;

			if(generate_code_file)
			{

				var filename = "generated_from_schema";
				
				var output_path = Path.Combine(location, @"schema/GeneratedCode");

				//var path = Path.Combine(location, @"schema/template");
				//var schemaJson = File.ReadAllText($"{path}/{filename}.json");

				var generatedFile = await GenerateFileAsync(filename, new_schema_json);

				var cs_file_name = $"{output_path}/{filename}.cs";

				File.WriteAllText(cs_file_name, generatedFile);



				var dll_name = "my";
				var dll_file_name = dll_name + ".dll";

				if(System.IO.File.Exists(dll_file_name))
				{
					System.IO.File.Delete(dll_file_name);
				}
				var genlib = new mmria.console.schema.Generatelib();


				genlib.Execute(dll_name, cs_file_name, json_schema_path);





			}


			Console.WriteLine("Convert Finished");

		}

		static async Task<string> ReadMetadataAsync()
		{
				// ... Target page.
				string page = "http://demo.mmria.org/api/metadata";

				// ... Use HttpClient.
				using (HttpClient client = new HttpClient())
				using (HttpResponseMessage response = await client.GetAsync(page))
				using (HttpContent content = response.Content)
				{
						// ... Read the string.
						string result = await content.ReadAsStringAsync();

						return result;
				}
		}





		static async Task<string> GenerateFileAsync(string filename, string schemaJson)
		{
				string result = null;

				var schema = await NJsonSchema.JsonSchema4.FromJsonAsync(schemaJson);
				var settings = new NJsonSchema.CodeGeneration.CSharp.CSharpGeneratorSettings()
				{
					Namespace = "AwesomeSauce.v1",
					//ClassStyle = NJsonSchema.CodeGeneration.CSharp.CSharpClassStyle.Inpc 
					ClassStyle = NJsonSchema.CodeGeneration.CSharp.CSharpClassStyle.Poco,
					GenerateJsonMethods = true,
					GenerateDataAnnotations = true
				};

				var generator = new NJsonSchema.CodeGeneration.CSharp.CSharpGenerator(schema, settings);
				result = generator.GenerateFile();

//NJsonSchema.CodeGeneration.CSharp.CSharpClassStyle.
				return result;
		}

		static async Task<NJsonSchema.JsonSchema4> GetSchema(mmria.common.metadata.app p_app)
		{
				//https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Schema_JsonSchema.htm
						
			var schema = new NJsonSchema.JsonSchema4();
			
			schema.Type =  NJsonSchema.JsonObjectType.Object;
			schema.Title = "mmria_case";
			schema.Description = "Here is a case for your ...!";
			
			schema.SchemaVersion = "http://json-schema.org/draft-06/schema#";

			schema.Properties.Add("_id", new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.String});
			//schema.Properties.Add("name", new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.String});
			//schema.Properties.Add("prompt", new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.String});

			

			try

			{
				foreach(var child in p_app.lookup)
				{
						Set_LookUp(schema.Definitions, child);
				}

			}
			catch(Exception ex)
			{
				System.Console.Write($"Set_LookUp Exception: {ex}");
			}


			try

			{
				foreach(var child in p_app.children)
				{
						await GetSchema(schema.Definitions, schema, child);
				}

			}
			catch(Exception ex)
			{
				System.Console.Write($"GetSchema Exception: {ex}");
			}
			return schema;
		}


		static async Task<NJsonSchema.JsonSchema4> GetSchema(IDictionary<string, NJsonSchema.JsonSchema4> p_lookup, NJsonSchema.JsonSchema4 p_parent, mmria.common.metadata.node p_node)
		{
				//https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Schema_JsonSchema.htm

			/*
			var schema = new NJsonSchema.JsonSchema4();
			
			schema.Type =  NJsonSchema.JsonObjectType.Object;
			schema.Title = "mmria_case";
			schema.Description = "Here is a case for your ...!";
			
			schema.SchemaVersion = "http://json-schema.org/draft-06/schema#";
 			*/			


			NJsonSchema.JsonProperty property = null;
			//schema.Properties.Add("name", new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.String});
			//schema.Properties.Add("prompt", new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.String});

			try
			{

				switch(p_node.type.ToLower())
				{
						case "app":
						case "form":
						case "group":
						case "grid":

						if(p_node.type.ToLower() == "form" && p_node.cardinality == "*")
						{
								property = new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.Array};
						}
						else if(p_node.type.ToLower() == "grid")
						{
								property = new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.Array};
						}
						else
						{
							property = new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.Object};
						}

						p_parent.Properties.Add(p_node.name, property);
						
						foreach(var child in p_node.children)
						{
								await GetSchema(p_lookup, property, child);
						}

						break;
						case "textarea":
						case "hidden":
						case "string":
									var string_property = new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.String};
							
									if(p_node.default_value != null)
									{
										string_property.Default = p_node.default_value;
									}

									if(p_node.is_required.HasValue && p_node.is_required.Value)
									{
										string_property.IsRequired = true;
									}

									p_parent.Properties.Add(p_node.name, string_property);
									break;
						case "datetime":
						case "date":
						case "time":
									var date_property = new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.String, Format = "date-time" };
									if(p_node.is_required.HasValue && p_node.is_required.Value)
									{
										date_property.IsRequired = true;
									}
									p_parent.Properties.Add(p_node.name, date_property);
									break;
						case "number":
									var number_property = new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.Number };
									if(p_node.default_value != null)
									{
										decimal decimal_value;
										if(decimal.TryParse(p_node.default_value, out decimal_value))
										{
											number_property.Default = decimal_value;
										}
									}

									if(p_node.is_required.HasValue && p_node.is_required.Value)
									{
										number_property.IsRequired = true;
									}

									if(p_node.min_value != null)
									{
										decimal number_value;
										if(decimal.TryParse(p_node.min_value, out number_value))
										{
											number_property.Minimum = number_value;
										}
									}

									if(p_node.max_value != null)
									{
										decimal number_value;
										if(decimal.TryParse(p_node.max_value, out number_value))
										{
											number_property.Maximum = number_value;
										}
									}

									p_parent.Properties.Add(p_node.name, number_property);
									break;
						case "boolean":
									var boolean_property = new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.Boolean };
									if(p_node.is_required.HasValue && p_node.is_required.Value)
									{
										boolean_property.IsRequired = true;
									}

									if(p_node.default_value != null)
									{
										bool bool_value;

										if(bool.TryParse(p_node.default_value, out bool_value))
										{
											boolean_property.Default = bool_value;
										}
										
									}
									p_parent.Properties.Add(p_node.name,boolean_property);
									break;								
						case "list":
								if(p_node.is_multiselect.HasValue && p_node.is_multiselect.Value == true)
								{
									property = new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.Array};
									p_parent.Properties.Add(p_node.name, property);

									if(!string.IsNullOrWhiteSpace(p_node.path_reference))
									{
										property.Reference = p_lookup[p_node.path_reference.Replace("lookup/", "")];
										//p_parent.Properties.Add(p_node.name, property);
									}
									else
									{
										foreach(var value in p_node.values)
										{
											property.EnumerationNames.Add(value.value);
										}
									}
									
								}
								else
								{
									property = new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.String };
									p_parent.Properties.Add(p_node.name, property);

									if(!string.IsNullOrWhiteSpace(p_node.path_reference))
									{
										property.Reference = p_lookup[p_node.path_reference.Replace("lookup/", "")];
										//p_parent.Properties.Add(p_node.name, property);

									}
									else
									{

										foreach(var value in p_node.values)
										{
											property.EnumerationNames.Add(value.value);
										}
									}
								}


								break;
						case "button":
						case "chart":
						case "label":
								break;
						default:
							System.Console.Write($"Convert.cs.GetSchema.switch.Missing: {p_node.type}");
							break;
				}

			}
			catch(Exception ex)
			{
				System.Console.Write($"GetSchemaGetSchema(p_parent, p_node) Exception: {ex}");
			}
			return p_parent;
		}

		static void Set_LookUp(IDictionary<string, NJsonSchema.JsonSchema4> p_parent, mmria.common.metadata.node p_node)
		{
//https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Schema_JsonSchema.htm

			/*
			var schema = new NJsonSchema.JsonSchema4();
			
			schema.Type =  NJsonSchema.JsonObjectType.Object;
			schema.Title = "mmria_case";
			schema.Description = "Here is a case for your ...!";
			
			schema.SchemaVersion = "http://json-schema.org/draft-06/schema#";
 			*/			


			NJsonSchema.JsonProperty property = null;
			//schema.Properties.Add("name", new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.String});
			//schema.Properties.Add("prompt", new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.String});

			try
			{

				switch(p_node.type.ToLower())
				{						
						case "list":
								if(p_node.is_multiselect.HasValue && p_node.is_multiselect.Value == true)
								{
									property = new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.Array};
									foreach(var value in p_node.values)
									{
										property.EnumerationNames.Add(value.value);
									}
								}
								else
								{
									property = new NJsonSchema.JsonProperty(){ Type = NJsonSchema.JsonObjectType.String };
									p_parent.Add(p_node.name, property);

									foreach(var value in p_node.values)
									{
										property.EnumerationNames.Add(value.value);
									}
								}


								break;
								
				}

			}
			catch(Exception ex)
			{
				System.Console.Write($"GetSchemaGetSchema(p_parent, p_node) Exception: {ex}");
			}
			//return p_parent;

		}

		private static void addDoubleQuotes(System.Text.StringBuilder p_builder, string p_value)
		{
			p_builder.Append("\"");
			p_builder.Append(p_value);
			p_builder.Append("\"");
		}
	}
}
