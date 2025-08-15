using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;

namespace strongcase;

internal class Program
{

    enum RunEnum
    {
        Generate_From_Metadata,
        Test_Output_Load_Json
    }

    static RunEnum Run_Type = RunEnum.Generate_From_Metadata;
    static public async Task Main(string[] args)
    {
        if(Run_Type == RunEnum.Generate_From_Metadata)
        {
            Console.WriteLine("Executinng: Generate_From_Metadata");

            var di = new System.IO.DirectoryInfo("output");

            foreach (System.IO.FileInfo file in di.GetFiles())
            {
                file.Delete(); 
            }


            /*

            using System.Net.Http.Json;

            //Get JSON
            var stock = await httpClient.GetFromJsonAsync<Stock>($"https://localhost:12345/stocks/{symbol}");

            stock.Price = 121.10m;

            //Send JSON
            await httpClient.PostAsJsonAsync<Stock>("https://localhost:12345/stocks/", stock);

            */


        //string metadata_url = $"http://localhost:5984/metadata/version_specification-20.12.01/metadata";

        //string metadata_url = $"{host_db_url}/metadata/version_specification-20.12.01/metadata";
        //var metadata_client = new HttpClient { BaseAddress = new Uri(metadata_url) };
        
        
        //var metadata_url = "http://localhost:5984/metadata/mmria-pmss-builder";
        mmria.common.metadata.app metadata = null;


        //var metadata_url = $"http://localhost:5984/metadata/version_specification-23.01.03/metadata";
        //var metadata_url = $"http://localhost:5984/metaata/version_specification-23.11.08/metadata";


//https://couchdb-test-mmria.apps.ecpaas-dev.cdc.gov/
//https://couchdb-231-mmria.apps.ecpaas-dev.cdc.gov/

        var metadata_list = new List<string>()
        {
            "23.11.08",
            "24.03.01",
            "24.06.16",
            "24.10.01",
            "25.02.13",
            "25.06.16"
        };

        var metadata_index = 5;

        //var metadata_url = $"https://couchdb-231-mmria.apps.ecpaas-dev.cdc.gov/metadata/version_specification-23.06.16/metadata"; // pmss

        //var metadata_url = $"https://couchdb-test-mmria.apps.ecpaas-dev.cdc.gov/metadata/version_specification-23.11.08/metadata"; // mmria

        var metadata_version = metadata_list[metadata_index];
        var metadata_url = $"https://couchdb-test-mmria.apps.ecpaas-dev.cdc.gov/metadata/version_specification-{metadata_version}/metadata"; // mmria
        //24.03.01

        //24.06.16

        //var metadata_url = $"http://localhost:5984/metadata/version_specification-{metadata_version}/metadata"; // mmria

        using(var metadata_client = new HttpClient ())
        {
            metadata = await metadata_client.GetFromJsonAsync<mmria.common.metadata.app>(metadata_url);

            Console.WriteLine($"node name: {metadata.name} prompt: {metadata.prompt} type: {metadata.type}");
        }

        var name_space_version = $"v{metadata_version.Replace(".", "")}";
        var metadata_mgr = new metadata_mgr(metadata, name_space_version);
        var total_count = metadata_mgr.single_form_value_set.Count + 
            metadata_mgr.single_form_grid_value_set.Count + 
            metadata_mgr.multiform_value_set.Count + 
            metadata_mgr.multiform_grid_value_set.Count + 
            metadata_mgr.single_form_multi_value_set.Count + 
            metadata_mgr.single_form_grid_multi_value_list_set.Count + 
            metadata_mgr.multiform_multi_value_set.Count + 
            metadata_mgr.multiform_grid_multi_value_set.Count;
        
        var is_count_same = metadata_mgr.all_list_set.Count == metadata_mgr.single_form_value_set.Count + 
            metadata_mgr.single_form_grid_value_set.Count + 
            metadata_mgr.multiform_value_set.Count + 
            metadata_mgr.multiform_grid_value_set.Count + 
            metadata_mgr.single_form_multi_value_set.Count + 
            metadata_mgr.single_form_grid_multi_value_list_set.Count + 
            metadata_mgr.multiform_multi_value_set.Count + 
            metadata_mgr.multiform_grid_multi_value_set.Count;

        System.Console.WriteLine($"all_list_set.Count: {metadata_mgr.all_list_set.Count} total_count: {total_count}");
        System.Console.WriteLine($"is count the same: {is_count_same}");



        System.Console.WriteLine($"Single Form Count: {metadata_mgr.SingleformList.Count}");
        System.Console.WriteLine($"Multi Form Count: {metadata_mgr.MultifFormList.Count}");
        System.Console.WriteLine($"Grid Count: {metadata_mgr.GridList.Count}");
        System.Console.WriteLine($"Group Count: {metadata_mgr.GroupList.Count}");
        System.Console.WriteLine($"MultivaluedList Count: {metadata_mgr.MultivaluedList.Count}");

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        /*builder.AppendLine(@"
using System;
using System.Collections.Generic;

namespace mmria.case_version.v241001;");*/
		while(metadata_mgr.source_code_builder_stack.Count > 0)
        {
            var current = metadata_mgr.source_code_builder_stack.Pop();

            builder.AppendLine(current.ToString());

        }
        
        System.Console.WriteLine($"source code: {builder.ToString()}");

        System.IO.File.WriteAllText("output/mmria_case.cs", builder.ToString());

        var mmria_case_convert_template = System.IO.File.ReadAllText("mmria_case.convert.cs").Replace(".v241001;",$".{name_space_version};");
        System.IO.File.WriteAllText("output/mmria_case.convert.cs", mmria_case_convert_template);

// S

        var S_Get = new Template_Writer_S_Get(metadata_mgr.dictionary_set, name_space_version);
        await S_Get.Execute();
        
//  SG

        var SG_Get = new Template_Writer_SG_Get(metadata_mgr.dictionary_set, name_space_version);
        await SG_Get.Execute();

        var S_Set = new Template_Writer_S_Set(metadata_mgr.dictionary_set, name_space_version);
        await S_Set.Execute();


        var SG_Set = new Template_Writer_SG_Set(metadata_mgr.dictionary_set, name_space_version);
        await SG_Set.Execute();

        var M_Get = new Template_Writer_M_Get(metadata_mgr.dictionary_set, name_space_version);
        await M_Get.Execute();

        var MG_Get = new Template_Writer_MG_Get(metadata_mgr.dictionary_set, name_space_version);
        await MG_Get.Execute();

        var M_Set = new Template_Writer_M_Set(metadata_mgr.dictionary_set, name_space_version);
        await M_Set.Execute();

        var MG_Set = new Template_Writer_MG_Set(metadata_mgr.dictionary_set, name_space_version);
        await MG_Set.Execute();
        
        /*
            print("Single Form", metadata_mgr.SingleformList);
            print("Multi Form", metadata_mgr.MultifFormList);
            print("Grid", metadata_mgr.GridList);
            print("Group", metadata_mgr.GroupList);
        */
        }
        else if(Run_Type == RunEnum.Test_Output_Load_Json)
        {
            Console.WriteLine("Executinng: Test_Output_Load_Json");

/*            mmria.case_version.v1.mmria_case test_case = new mmria.case_version.v1.mmria_case();
            try
            {


                System.Text.Json.JsonDocumentOptions options = new()
                {
                    WriteIndented = true
                };
                * /

                using System.IO.FileStream stream = System.IO.File.OpenRead("json-convert-test/23.11.08.json");
                //test_case = await System.Text.Json.JsonSerializer.DeserializeAsync<mmria.case_version.v1.mmria_case>(stream);
                //var json = System.IO.File.ReadAllText("json-convert-test/23.11.08.json");
                var json_element = await System.Text.Json.JsonSerializer.DeserializeAsync<System.Text.Json.JsonDocument>(stream);
                //test_case.Convert(json_element.RootElement);
                

                if(test_case == null)
                {
                    System.Console.WriteLine($"unable to deserialize case version: 23.11.08");
                 
                }
                //stream.Close();
                //await stream.DisposeAsync();
                
                //System.Console.WriteLine($"case version: {test_case.version}");

                var test_case_json = System.Text.Json.JsonSerializer.Serialize(test_case);
                //System.Console.WriteLine(test_case_json);

                System.IO.File.WriteAllText("output/file-01.json", JsonPrettify(test_case_json));

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    //PropertyNamingPolicy = new UpperCaseNamingPolicy(),
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                    WriteIndented = true
                };

                //using System.IO.FileStream stream2 = System.IO.File.OpenRead("json-convert-test/23.11.08.json");
                //var test_case2 = await System.Text.Json.JsonSerializer.DeserializeAsync<mmria.case_version.v1.mmria_case>(stream2, options);

                var test_case2 = System.Text.Json.JsonSerializer.Deserialize<mmria.case_version.v1.mmria_case>(test_case_json, options);
                if(test_case2 != null)
                {
                    //System.Console.WriteLine($"case version: {test_case2.version}");

                    //System.Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(test_case));
                }
                else
                {
                    System.Console.WriteLine($"unable to deserialize case version: 23.11.08");
                }

                //test_case.Convert()

                

            }
            catch(Exception ex)
            {
                System.Console.WriteLine(ex);
            }
            */

        }

    }

    public static async Task<T> ReadAsync<T>(string filePath)
    {
        using System.IO.FileStream stream = System.IO.File.OpenRead(filePath);
        return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream);
    }

    static void print(string list_name, List<mmria.common.metadata.Metadata_Node> list)
    {
        System.Console.WriteLine($"List: {list_name} Count: {list.Count}");
        foreach(var item in list)
        {
            System.Console.WriteLine($"\n: {item.Node.name}");
        }
    }


    public static string JsonPrettify(string json)
    {
        using var jDoc = System.Text.Json.JsonDocument.Parse(json);
        return System.Text.Json.JsonSerializer.Serialize(jDoc, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }



    public void gather_info
    (
        mmria.common.metadata.app p_metadata,
        string p_path
    )
	{

		foreach(var node in p_metadata.children)
		{
			switch (node.type.ToLowerInvariant())


			{
                case "form":
				if
				(
					node.cardinality == "+" ||
					node.cardinality == "*"
				)
				{
					
				}
				else
				{
					
				}
                break;
			}
		}
		
	}


    public void write_class
    (
        mmria.common.metadata.app p_metadata,
        string p_path
    )
	{

		foreach(var node in p_metadata.children)
		{
			switch (node.type.ToLowerInvariant())


			{
                case "form":
				if
				(
					node.cardinality == "+" ||
					node.cardinality == "*"
				)
				{
					
				}
				else
				{
					
				}
                break;
			}
		}
		
	}
	
}