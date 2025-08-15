using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Akka.Actor;

namespace migrate;


struct Result_Struct
{
    public System.Dynamic.ExpandoObject[] docs;
}

struct Selector_Struc
{
    public System.Collections.Generic.Dictionary<string,System.Collections.Generic.Dictionary<string,string>> selector;
    public string[] fields;

    public int limit;
}

class Program
{
        static private IConfiguration Configuration;

        static private mmria.common.couchdb.ConfigurationSet ConfigurationSet;

    static private string config_timer_user_name;
    static private string config_timer_value;
    static private string config_metadata_user_name;
    static private string config_metadata_value;

    static private string config_couchdb_url;

    static public string config_metadata_version;

    static List<string> run_list;


    static HashSet<string> has_been_done_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {

    };

    static List<string> test_list = new List<string>()
    {
        "qa"
//"localhost",
//"fl_dev",
//"test"
/*
"as",
"cnmi",
"vi",
"aa",
"anthc",
"cat",
"gl",
"gp",
"ica",
"nn",
"nwp",
"rm",
"sp",
"uset",
"chickasaw",
"gu",      
"afd",
"dc",
"hi",
"md",
"me",
"nd",
"vt",
"pr",
"ak",
"az",

ma
mo
fl


"ma",*/
//IA, MI, MO, PA, SC ,
//AK, CO, KS, MS
/*"
"al",
"ga",
"ia",
"ma",
"ms",
"ny",
"wa",
"wi",
//"ny",
ma",
"wa",
"ms",
"mn"
//"localhost",

"ia",
"mi",
"mo",
"pa",
"sc",*/
//"fl",
//"wa",
//"ks",
//"ga",
//"co",

//"ny"
        //"hi",
        //"al",
        //"ak",
        //"az",
        //"md",
        //"me",
        //"ar",
        //"ga",
        //"fl",
        
        /*"fl_dev",*/
        //"uat",
        //"localhost",
        //"qa",
        //"test",
        //"fl_dev",
        /*"az",
                "ma",
                "nc",
                "oh",
                "tn",
                "ut",
                "wi"
        
        //"fl_dev"
        
        "localhost",
        "afd",
"dc",
//"ga",
"hi",
"md",
"me",
"nd",
"vt",
"pr",
        //"test",
        "qa",
        "uat"*/

        /*"test",
        "ga"
        "hi",
        "qa",
        "uat",
        "cdc",
        "tn",*/
        /*"nh",
        "wa",
        "ma",
        "wi",
        "nc"*/
    };

/**/
    static HashSet<string> prefix_list = new HashSet<string>()
    {
            "mp",
            "mh",
            "pw",
            "as",
            "cnmi",
            "vi",
                "aa",
                "anthc",
                "cat",
                "gl",
                "gp",
                "ica",
                "nn",
                "nwp",
                "rm",
                "sp",
                "uset",
                "chickasaw",
                "gu",
        
"afd",
"dc",
"ga",
"hi",
"md",
"me",
"nd",
"vt",
"pr",

        

        
        "al",
        "ak",
        "az",
        "ar",
        "ca",
        "ct",
        //"cdc",
        "co",
        "de",
        "demo",
        "fl",
        "hi",
        "ia",
        "id",
        "in",
        "il",
        "ks",
        "ky",
        "la",
        "ma",
        "mi",
        "mn",
        "mo",
        "ms",
        "mt",
        "nc",
        "ne",
        "nh",
        "nj",
        "nm",
        "ny",
        "nv",
        "oh",
        "ok",
        "or",
        "pa",
        "ri",
        "sc",
        "sd",
        "tn",
        "tx",
        "ut",
        "va",
        "wa",
        "wi",
        "wv",
        "wy"
        

    };

    /*

    static Dictionary<string,bool> central_db = new Dictionary<string,bool>(StringComparer.OrdinalIgnoreCase)
    {
        
        {"afd", true},
        {"dc", true},
        {"ga", true},
        {"hi", true},
        {"md", true},
        {"me", true},
        {"nd", true},
        {"vt", true},
        {"pr", true},

{"ok", true},
{"tx", true},
{"mt", true},
{"ak", true},
{"ar", true},
{"ca", true},
{"ia", true},
{"id", true},
{"ky", true},
{"nv", true},
{"or", true},
{"ri", true},
{"sd", true}

    };*/

    enum RunTypeEnum
    {
        OnBoarding,
        DataMigration,

        OneTime,

        MMRDSImport
    }

    static async Task Main(string[] args)
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .AddUserSecrets<Program>()
            .Build();

        var config_id = Configuration["data_migration:config_id"];
        config_couchdb_url = Configuration["data_migration:couchdb_url"];
        config_timer_user_name = Configuration["data_migration:timer_user_name"];
        config_timer_value = Configuration["data_migration:timer_value"];
        
        ConfigurationSet = GetConfiguration(config_id);

        
        config_metadata_version = Configuration["data_migration:metadata_version"];
        /*config_metadata_user_name = Configuration["mmria_settings:metadata_timer_user_name"];
        config_metadata_value = Configuration["mmria_settings:metadata_timer_password"];
        */

        bool is_test_list = true;
        
        bool is_report_only_mode = true;


        RunTypeEnum MigrationType = RunTypeEnum.DataMigration;

        

        string path = System.IO.Directory.GetCurrentDirectory();
        string target = System.IO.Path.Combine(path, "output");
            
        var dt = DateTime.Now;

        var directory_name = $"{dt.Year}-{dt.Month}-{dt.Day}T{dt.Hour}-{dt.Minute}-{dt.Second}";
        var file_name = $"{directory_name}.txt";

        string target_directory = System.IO.Path.Combine(target, directory_name);
        string backup_directory = System.IO.Path.Combine(target_directory, "backup");


        Console.WriteLine("The current directory is {0}", path);


        

        if (!System.IO.Directory.Exists(backup_directory)) 
        {
            System.IO.Directory.CreateDirectory(backup_directory);
        }


        var summary_value_dictionary = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
        //var state_value_dictionary = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
        var output_string_builder = new Dictionary<string, Dictionary<string, System.Text.StringBuilder>>(StringComparer.OrdinalIgnoreCase);

        output_string_builder.Add("main", new Dictionary<string, System.Text.StringBuilder>(StringComparer.OrdinalIgnoreCase));
        output_string_builder["main"].Add("main", new System.Text.StringBuilder());
        output_string_builder["main"]["main"].AppendLine($"Run Startedt at: {dt.ToString("o")}");
        output_string_builder["main"]["main"].AppendLine($"is test mode? {is_test_list}");
        output_string_builder["main"]["main"].AppendLine($"is report_only_mode mode? {is_report_only_mode}");

        output_string_builder.Add("committee_review_pregnancy_relatedness", new Dictionary<string, System.Text.StringBuilder>(StringComparer.OrdinalIgnoreCase));
        output_string_builder.Add("editable_list", new Dictionary<string, System.Text.StringBuilder>(StringComparer.OrdinalIgnoreCase));
        output_string_builder.Add("Manual_Migration", new Dictionary<string, System.Text.StringBuilder>(StringComparer.OrdinalIgnoreCase));
        output_string_builder.Add("Process_Migrate_Charactor_to_Numeric", new Dictionary<string, System.Text.StringBuilder>(StringComparer.OrdinalIgnoreCase));

        output_string_builder.Add("summary", new Dictionary<string, System.Text.StringBuilder>(StringComparer.OrdinalIgnoreCase));

        summary_value_dictionary.Add("summary", new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase));

        if(is_test_list)
        {
            run_list = test_list;
        }
        else
        {
            run_list = prefix_list.ToList();
        }


        if(MigrationType == RunTypeEnum.MMRDSImport)
        {
            var mmrds_importer = new mmria.mmrds.import.mmrds_importer();
            await mmrds_importer.Execute
            (
                new string[1]{ $"database_file_path={Configuration["data_migration:database_path"]}" }
            );
            return;
        }
                

        foreach(var prefix in run_list)
        {
            if(has_been_done_set.Contains(prefix)) 
            {
                continue;
            }

            mmria.common.couchdb.DBConfigurationDetail db_config = null;

            if(ConfigurationSet.detail_list.ContainsKey(prefix))
            {
                    db_config = ConfigurationSet.detail_list[prefix];
            }

            if(db_config == null)
            {
                continue;
            }

            var db_name = "mmrds";

            config_couchdb_url = db_config.url; 
            
            config_timer_user_name = db_config.user_name; 
            config_timer_value = db_config.user_value;
            if(! string.IsNullOrWhiteSpace(db_config.prefix))
            {
                db_name = $"{db_config.prefix}mmrds";
            }


            if (!is_test_list && !is_report_only_mode)
            {
                //await backup(backup_directory, prefix, db_name);
            }

            //output_string_builder["main"].AppendLine($"Database UrL: {url}/{db_name}");

            output_string_builder["committee_review_pregnancy_relatedness"].Add(prefix, new System.Text.StringBuilder());
            output_string_builder["editable_list"].Add(prefix, new System.Text.StringBuilder());
            output_string_builder["Manual_Migration"].Add(prefix, new System.Text.StringBuilder());
            output_string_builder["Process_Migrate_Charactor_to_Numeric"].Add(prefix, new System.Text.StringBuilder());
            output_string_builder["summary"].Add(prefix, new System.Text.StringBuilder());

            summary_value_dictionary.Add(prefix, new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase));

            var output_text = $"Database UrL: {config_couchdb_url}/{db_name}";

            output_string_builder["main"]["main"].AppendLine(output_text);
            //if(false)
            try
            {
                Console.WriteLine($"Process_Migrate_Data Begin {System.DateTime.Now}");
                
                switch(MigrationType)
                {
                    case RunTypeEnum.OnBoarding:
                    Console.WriteLine("This is an OnBoarding data migration.");
                    output_string_builder["main"]["main"].AppendLine("This is an OnBoarding data migration.");
                    break;

                    case RunTypeEnum.DataMigration:
                    Console.WriteLine("This is a Version upgrade data migration");
                    output_string_builder["main"]["main"].AppendLine("This is a Version upgrade data migration");
                    break;

                    case RunTypeEnum.OneTime:	
                    Console.WriteLine("This is a one time only data migration");
                    output_string_builder["main"]["main"].AppendLine("This is a one time only data migration");
                    break;
                    case RunTypeEnum.MMRDSImport:
                    Console.WriteLine("This is a MMRDS to MMRIA data migration");
                    output_string_builder["main"]["main"].AppendLine("This is a MMRDS to MMRIA data migration");
                    break;
                }

                 if(MigrationType == RunTypeEnum.OnBoarding)
                {
                    var crpr = new migrate.set.committee_review_pregnancy_relatedness(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["committee_review_pregnancy_relatedness"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await crpr.execute();

                    var el = new migrate.set.editable_list(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["editable_list"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await el.execute();
                    
                    var mm = new migrate.set.Manual_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Manual_Migration"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await mm.execute();
                    
                    var pctn = new migrate.set.Process_Migrate_Charactor_to_Numeric(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, config_metadata_user_name, config_metadata_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await pctn.execute();

                    var v2_3 = new migrate.set.v2_3_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await v2_3.execute();

                    var v2_4 = new migrate.set.v2_4_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await v2_4.execute();

                    var v2_4RaceRecode = new migrate.set.v2_4RaceRecode(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await v2_4RaceRecode.execute();

                    var v2_5 = new migrate.set.v2_5_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await v2_5.execute();

                    var v2_6 = new migrate.set.v2_6_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, prefix);
                    await v2_6.execute();

                    var v2_6_2 = new migrate.set.v2_6_2_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await v2_6_2.execute();

                    var v2_7 = new migrate.set.v2_7_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await v2_7.execute();

                    var SubstanceMigration = new migrate.set.SubstanceMigration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, config_metadata_user_name, config_metadata_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await SubstanceMigration.execute();

                    var v2_8 = new migrate.set.v2_8_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await v2_8.execute();

                    var v2_8_1 = new migrate.set.v2_8_1_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await v2_8_1.execute();

                    var v2_9 = new migrate.set.v2_9_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await v2_9.execute();

                    var v2_10 = new migrate.set.v2_10_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await v2_10.execute();

                    var CVS_Migration = new migrate.set.CVS_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, ConfigurationSet);
                    await CVS_Migration.execute();

                    var v3_0_Migration = new migrate.set.v3_0_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await v3_0_Migration.execute();

                    var v3_0_1_Migration = new migrate.set.v3_0_1_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await v3_0_1_Migration.execute();

                    var v3_3_Migration = new migrate.set.v3_3_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, ConfigurationSet);
                    await v3_3_Migration.execute();


                }
                else if(MigrationType == RunTypeEnum.DataMigration)
                {
                    
                    var v3_7_Migration = new migrate.set.v3_7_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, ConfigurationSet);
                    await v3_7_Migration.execute();

                    //var CVS_Migration = new migrate.set.CVS_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, ConfigurationSet);
                    //await CVS_Migration.execute();

                    var SubstanceMigration = new migrate.set.SubstanceMigration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, config_metadata_user_name, config_metadata_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    await SubstanceMigration.execute();
                }
                else if(MigrationType == RunTypeEnum.OneTime)
                {

                    var v3_7_Migration = new migrate.set.v3_7_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, ConfigurationSet);
                    await v3_7_Migration.execute();

                    //var v3_4_PreUpgrade = new migrate.set.v3_4_PreUpgrade(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, ConfigurationSet);
                    //await v3_4_PreUpgrade.execute();

                    //var v3_4_TimeField = new migrate.set.v3_4_TimeField(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, ConfigurationSet);
                    //await v3_4_TimeField.execute();

                   //var v3_3_3_Migration = new migrate.set.v3_3_3_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, ConfigurationSet);
                   //await v3_3_3_Migration.execute();



                   // var v3_3_1_Migration = new migrate.set.v3_3_1_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, ConfigurationSet);
                   // await v3_3_1_Migration.execute();

                    

                    /*
                    var v2_10_1 = new migrate.set.v2_10_1_CertaintyHotfix(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    v2_10_1.SetConfiguration(Configuration);
                    v2_10_1.SetConfigDB(ConfigurationSet);
                    await v2_10_1.execute();
                    */

                   // var v2_10_1_CVS_Migration = new migrate.set.v2_10_1_CVS_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, ConfigurationSet);
                   // await v2_10_1_CVS_Migration.execute();

                    //var MMRDS_CS_Narrative_Migration = new migrate.set.MMRDS_CS_Narrative_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    //await MMRDS_CS_Narrative_Migration.execute();


                    //var v2_10 = new migrate.set.v2_10_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    ///await v2_10.execute();

                    //var CVS_Migration = new migrate.set.CVS_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, ConfigurationSet);
                    //await CVS_Migration.execute();


                    //var v2_8_1 = new migrate.set.v2_8_1_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    //await v2_8_1.execute();

                    //var v2_6_2 = new migrate.set.v2_6_2_Migration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    //await v2_6_2.execute();

                    //var sep_data_fix = new migrate.set.vSEP_DataFix(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, prefix);
                    //await sep_data_fix.execute();


                    //var VitalsMigration01 = new migrate.set.VitalsMigration01(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode, prefix, ConfigurationSet);
                    //await VitalsMigration01.execute();


                    //var Fix_American_Indian_Recode = new migrate.set.Fix_American_Indian_Recode(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    //await Fix_American_Indian_Recode.execute();
                        
                                        
                    //var SubstanceMigration = new migrate.set.SubstanceMigration(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, config_metadata_user_name, config_metadata_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    //await SubstanceMigration.execute();

                    //var GA_One_Time = new migrate.set.GA_One_Time(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    //await GA_One_Time.execute();

                    //var v2_4RaceRecode = new migrate.set.v2_4RaceRecode(config_couchdb_url, db_name, config_timer_user_name, config_timer_value, output_string_builder["Process_Migrate_Charactor_to_Numeric"][prefix], summary_value_dictionary[prefix], is_report_only_mode);
                    //await v2_4RaceRecode.execute();
                }

                foreach(var kvp in summary_value_dictionary[prefix])
                {
                    foreach(var kvp2 in summary_value_dictionary[prefix])
                    {
                        var p_path = kvp2.Key;

                        foreach(var item in kvp2.Value)
                        {
                            output_string_builder["summary"][prefix].AppendLine($"{p_path}: {item}");
                            if(!summary_value_dictionary["summary"].ContainsKey(p_path))
                            {
                                summary_value_dictionary["summary"].Add(p_path, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                            }
                            summary_value_dictionary["summary"][p_path].Add(item);

                        }
                    }
                }

                string state_output_file = System.IO.Path.Combine(target_directory, $"{prefix}-{directory_name}.txt");
                using (System.IO.FileStream fs = System.IO.File.OpenWrite(state_output_file)) 
                {
                    foreach(var kvp in output_string_builder)
                    {
                        foreach(var kvp2 in kvp.Value)
                        {
                            if(kvp2.Key == prefix)
                            {
                                output_string_builder["main"]["main"].AppendLine(kvp2.Value.ToString());

                                Byte[] info = 
                                new System.Text.UTF8Encoding(true).GetBytes(kvp2.Value.ToString());
                                fs.Write(info, 0, info.Length);
                            }
                        }
                    }
                }


                Console.WriteLine($"Process_Migrate_Data End {System.DateTime.Now}");
            
            }
            catch(Exception ex)
            {
                output_string_builder["main"]["main"].AppendLine($"exception: {ex}");
                Console.WriteLine($"Process_Migrate_Data exception {System.DateTime.Now}\n{ex}");
            }
        }


        var edt = DateTime.Now;
        var elapsed_time_in_minutes = (edt-dt).TotalMinutes;

        output_string_builder["main"]["main"].AppendLine($"Run ended at: {edt.ToString("o")}");
        output_string_builder["main"]["main"].AppendLine($"elapsed time in minutes:{elapsed_time_in_minutes}");
        output_string_builder["main"]["main"].AppendLine($"is test mode:{is_test_list}");
        output_string_builder["main"]["main"].AppendLine($"is report_only_mode mode? {is_report_only_mode}");


        
        output_string_builder["summary"].Add("summary", new System.Text.StringBuilder());


        output_string_builder["summary"]["summary"].AppendLine();
        output_string_builder["summary"]["summary"].AppendLine();
        output_string_builder["summary"]["summary"].AppendLine("************ summary ********");
        foreach(var kvp in summary_value_dictionary["summary"])
        {
            var p_path = kvp.Key;

            foreach(var item in kvp.Value)
            {
                output_string_builder["summary"]["summary"].AppendLine($"{p_path}: {item}");
            }
        }
        


        string output_file = System.IO.Path.Combine(target_directory, file_name);
        using (System.IO.FileStream fs = System.IO.File.OpenWrite(output_file)) 
        {
            Byte[] info = null;
            //foreach(var kvp in )
            //{
                info = 
                new System.Text.UTF8Encoding(true).GetBytes(output_string_builder["main"]["main"].ToString());
                fs.Write(info, 0, info.Length);
            //}


            //foreach(var kvp in output_string_builder["main"])
            //{
                info = 
                new System.Text.UTF8Encoding(true).GetBytes(output_string_builder["summary"]["summary"].ToString());
                fs.Write(info, 0, info.Length);
            //}

/*
            foreach(var kvp in summary_value_dictionary["summary"])
            {
                var p_path = kvp.Key;

                Byte[] info = 
                new System.Text.UTF8Encoding(true).GetBytes($"{p_path}: {kvp.Value}");
                fs.Write(info, 0, info.Length);
            }*/

        }
        


        
        string line = null;
        string substance_mapping_log_path = System.IO.Path.Combine(target_directory, "substance-mapping-log.txt");
        

        using System.IO.StreamReader input_file = new System.IO.StreamReader(output_file); 
        using (System.IO.StreamWriter substance_mapping_log_file = new System.IO.StreamWriter(substance_mapping_log_path))
        { 
            string db_url = null;
            //08c16c1b-a9e1-4d22-aa76-dc5d6dc6aef8
            //22680d11-4291-76d5-a1531937205616573
            var pattern = "[a-fA-F0-9]+";
            var regex = new System.Text.RegularExpressions.Regex($"({pattern}-{pattern}-{pattern}-{pattern}-{pattern})");

            
            while((line = input_file.ReadLine()) != null)  
            {  
                System.Console.WriteLine(line);  

                if (line.StartsWith("Database UrL: "))
                {
                    db_url = line.Replace("Database UrL: ","");
                }

                if (line.StartsWith("applied substance mapping"))
                {
                    var record_id = regex.Match(line).Groups[0]; 
                    if(string.IsNullOrWhiteSpace(record_id.ToString()))
                    {
                        var regex2 = new System.Text.RegularExpressions.Regex($"({pattern}-{pattern}-{pattern}-{pattern})");
                        record_id = regex2.Match(line).Groups[0]; 
                        System.Console.WriteLine(line);  
                    }

                    if(string.IsNullOrWhiteSpace(record_id.ToString()))
                    {
                        System.Console.WriteLine(line);  
                    }

                    substance_mapping_log_file.WriteLine($"{db_url}\t{record_id}\t{line}");
                }
            }  
        }


        Console.WriteLine("fin.");


    }

    static private async Task backup(string root_folder, string p_prefix, string p_db)
    {
        string[] get_execute_url
        (
            string uid,
            string pwd,
            string prefix,
            string db = "mmrds",
            string directory = "C:/temp"
        )
        {
            var server_url = config_couchdb_url.Replace("{prefix}",prefix);
                return new[]
                {
                    "backup",
                    "user_name:" + uid,
                    "password:" + pwd,
                    $"database_url:{server_url}/{db}",
                    $"backup_file_path:{directory}/{prefix}-mmria-{db}-db.json"
                };
        }

        var date_string = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var target_folder = System.IO.Path.Combine(root_folder, date_string);

        System.IO.Directory.CreateDirectory(target_folder);

        var b = new Backup();


        await b.Execute(get_execute_url(config_timer_user_name, config_timer_value, directory:target_folder, prefix:p_prefix, db:p_db));

    }

    private void process_node(ref bool is_changed, mmria.common.metadata.node p_metadata, object p_value)
    {
        IDictionary<string,object> object_dictionary = null;
        switch(p_metadata.type.ToLowerInvariant())
        {
            case "group":
                object_dictionary = p_value as IDictionary<string,object>;
                
                if(object_dictionary != null)
                foreach(var child in p_metadata.children)
                {
                    if(object_dictionary.ContainsKey(child.name))
                    {
                        try
                        {
                            process_node(ref is_changed, child, object_dictionary[child.name]);
                        }
                        catch(Exception)
                        {
                            Console.WriteLine("unable to process" + child.name + " : " + child.type);
                        }
                    }

                }
            break;				
            case "grid":
                var object_array = p_value as IList<object>;

                if(object_array != null)
                foreach(var object_item in object_array)
                {
                    object_dictionary = object_item as IDictionary<string,object>;
                    if(object_dictionary != null)
                    foreach(var child in p_metadata.children)
                    {
                        if(object_dictionary.ContainsKey(child.name))
                        {
                            try
                            {
                                process_node(ref is_changed, child, object_dictionary[child.name]);
                            }
                            catch(Exception)
                            {
                                Console.WriteLine("unable to process" + child.name + " : " + child.type);
                            }
                        }

                    }
                }
            break;
            case "form":
                if
                (
                    p_metadata.cardinality == "+" ||
                    p_metadata.cardinality == "*"
                )
                {
                    var form_array = p_value as IList<object>;
                    if(form_array != null)
                    foreach(var form in form_array)
                    {
                        object_dictionary = form as IDictionary<string,object>;
                        
                        if(object_dictionary != null)
                        foreach(var child in p_metadata.children)
                        {
                            if(object_dictionary.ContainsKey(child.name))
                            {
                                try
                                {
                                    process_node(ref is_changed, child, object_dictionary[child.name]);
                                }
                                catch(Exception)
                                {
                                    Console.WriteLine("unable to process" + child.name + " : " + child.type);
                                }
                            }
                            

                        }		
                    }
                }
                else
                {
                    object_dictionary = p_value as IDictionary<string,object>;

                    if(object_dictionary != null)
                    foreach(var child in p_metadata.children)
                    {
                        if(object_dictionary.ContainsKey(child.name))
                        {
                            try
                            {
                                process_node(ref is_changed, child, object_dictionary[child.name]);
                            }
                            catch(Exception)
                            {
                                Console.WriteLine("unable to process" + child.name + " : " + child.type);
                            }
                        }
                    }	
                }

            break;
            case "list":
                try
                {
                    //process_list(ref is_changed, this.lookup,  p_metadata, p_value);
                }
                catch(Exception)
                {
                    Console.WriteLine("unable to process" + p_metadata.name + " : " + p_metadata.type);
                }
            break;
            default:
                break;
        }
    }

    private static Dictionary<string,mmria.common.metadata.value_node[]> get_look_up(mmria.common.metadata.app p_metadata)
    {
        var result = new Dictionary<string,mmria.common.metadata.value_node[]>(StringComparer.OrdinalIgnoreCase);

        foreach(var node in p_metadata.lookup)
        {
            result.Add("lookup/" + node.name, node.values);
        }
        return result;
    }
    static string Base64Encode(string plainText) 
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    private static mmria.common.couchdb.ConfigurationSet GetConfiguration(string config_id)
    {
        var result = new mmria.common.couchdb.ConfigurationSet();
        
        try
        {
            string request_string = $"{config_couchdb_url}/configuration/{config_id}";
            var case_curl = new cURL("GET", null, request_string, null, config_timer_user_name, config_timer_value);
            string responseFromServer = case_curl.execute();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.couchdb.ConfigurationSet> (responseFromServer);

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return result;
    }

}



