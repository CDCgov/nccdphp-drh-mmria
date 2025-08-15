using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mmria.common.model;
using System.Net.Http;
using Serilog;
using Serilog.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server;

[Route("api/[controller]")]
public sealed class versionController: ControllerBase
{ 

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public Dictionary<string, string> formName = new Dictionary<string, string>();
    public versionController
(
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
        formName.Add("(none)", "(none)");
        formName.Add("home_record", "Home Record");
        formName.Add("death_certificate", "Death Certificate");
        formName.Add("birth_fetal_death_certificate_parent", "Birth/Fetal Death Certificate - Parent Section");
        formName.Add("birth_certificate_infant_fetal_section", "Birth/Fetal Death Certificate - Infant/Fetal Section");
        formName.Add("autopsy_report", "Autopsy Report");
        formName.Add("prenatal", "Prenatal Care Record");
        formName.Add("er_visit_and_hospital_medical_records", "ER Visits & Hospitalizations");
        formName.Add("other_medical_office_visits", "Other Medical Office Visits");
        formName.Add("medical_transport", "Medical Transport");
        formName.Add("social_and_environmental_profile", "Social & Environment Profile");
        formName.Add("mental_health_profile", "Mental Health Profile");
        formName.Add("informant_interviews", "Informant Interviews");
        formName.Add("case_narrative", "Case Narrative");
        formName.Add("committee_review", "Committee Decisions");
        formName.Add("cvs", "Community Vital Signs");
        formName.Add("data_migration_history", "Data Migration History");
    }

    [Route("list")]
    [AllowAnonymous] 
    [HttpGet]
    public async System.Threading.Tasks.Task<List<mmria.common.metadata.Version_Specification>> List()
    {
        Log.Information  ("Recieved message.");
        var result = new List<mmria.common.metadata.Version_Specification>();

        try
        {
            string version_specification_url = db_config.url + $"/metadata/_all_docs?include_docs=true";

            var curl = new cURL("GET", null, version_specification_url, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await curl.executeAsync();

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings{
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    MissingMemberHandling =  Newtonsoft.Json.MissingMemberHandling.Ignore
            };
            var version_specification_list = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<mmria.common.metadata.Version_Specification>> (responseFromServer, settings);

            foreach(var row in version_specification_list.rows)
            {
                var version_specification = row.doc;
                if
                (
                    version_specification.data_type == null || 
                    version_specification.data_type != "version-specification"|| 
                    version_specification._id == "2016-06-12T13:49:24.759Z" ||
                    version_specification._id == "de-identified-list"
                )
                {
                    continue;
                }
                result.Add(row.doc);
                    
            }

        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }

    [Route("GetVersionSpecificationMetadata")]
    [AllowAnonymous] 
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.metadata.Version_Specification> GetVersionSpecificationMetadata(string version_specification_id)
    {
        Log.Information  ("Recieved message.");
        mmria.common.metadata.Version_Specification result = null;

        try
        {
            string version_specification_url = db_config.url + $"/metadata/version_specification-{version_specification_id}";

            var curl = new cURL("GET", null, version_specification_url, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await curl.executeAsync();

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Version_Specification>(responseFromServer);
        }
        catch(Exception ex) 
        {
            Log.Information ($"{ex}");
        }

        return result;
    }

    
    [AllowAnonymous] 
    [Route("release-version")]
    [HttpGet]
    public string release_version()
    {
        return configuration.GetString("metadata_version", host_prefix);
    }

    [AllowAnonymous] 
    [HttpGet]
    public async Task<string> Get()
    {
        string result = null;

        try
        {
            //"2016-06-12T13:49:24.759Z"
            string request_string = db_config.url + $"/metadata/2016-06-12T13:49:24.759Z/validator.js";

            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));
            request.Method = "GET";
            request.PreAuthenticate = false;

            System.Net.WebResponse response = (System.Net.HttpWebResponse) await request.GetResponseAsync();
            System.IO.Stream dataStream = response.GetResponseStream();
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            result = await reader.ReadToEndAsync ();

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    } 

    

    [AllowAnonymous] 
    [Route("export-names/{version_specification_id}/{type}")]
    [HttpGet]
    public string export_all_generate_name_map
    (
        string version_specification_id,
        string type = "all"
    )
    {

        var export_all_generate_name_map = new mmria.server.utils.export_all_generate_name_map(db_config);

        var result = export_all_generate_name_map.Execute(version_specification_id, type);

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(result, settings);

        return object_string;
    }


    [AllowAnonymous] 
    [HttpGet]
    [Route("{version_specification_id}/{document_name}")]
    public async Task<FileResult> Get_Version_Document(string version_specification_id, string document_name = "")
    {
        FileResult result = null;

        try
        {
            string request_string = db_config.url + $"/metadata/version_specification-{version_specification_id}/{document_name}";

            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));
            request.Method = "GET";
            request.PreAuthenticate = false;

            System.Net.WebResponse response = (System.Net.HttpWebResponse) await request.GetResponseAsync();

            using(System.IO.Stream dataStream = response.GetResponseStream())
            {
                //System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
                string type="javascript";
                if(!string.IsNullOrWhiteSpace(document_name))
                switch(document_name.ToLower())
                {
                    case "metadata":
                    case "ui_specification":
                        type="json";
                        break;
                }

                result = File(ReadFully(dataStream),$"application/{type}", "validator");
            }


            

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    } 

    public static byte[] ReadFully(System.IO.Stream input)
    {
        byte[] buffer = new byte[16*1024];
        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        {
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }
    }
/*
    [AllowAnonymous] 
    [HttpGet]
    [Route("{p_Version_Specification_Id}")]
    public async Task<mmria.common.metadata.Version_Specification> Get_Version_Specification(string p_Version_Specification_Id, string path = "")
    {
        mmria.common.metadata.Version_Specification result = null;

        try
        {
            //"2016-06-12T13:49:24.759Z"
            string request_string = db_config.url + $"/metadata/2016-06-12T13:49:24.759Z/validator.js";

            System.Net.WebRequest request = System.Net.WebRequest.Create(new Uri(request_string));
            request.Method = "GET";
            request.PreAuthenticate = false;

            System.Net.WebResponse response = (System.Net.HttpWebResponse) await request.GetResponseAsync();
            System.IO.Stream dataStream = response.GetResponseStream();
            System.IO.StreamReader reader = new System.IO.StreamReader (dataStream);
            result = await reader.ReadToEndAsync ();
            

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    } */

    // POST api/values 
    [Authorize(Roles  = "form_designer")]
    [Route("save")]
    [HttpPost]
    [HttpPut]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (
        [FromBody] mmria.common.metadata.Version_Specification p_Version_Specification
    ) 
    { 
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

/*
        System.IO.Stream dataStream0 = this.Request.Body;
        // Open the stream using a StreamReader for easy access.
        //dataStream0.Seek(0, System.IO.SeekOrigin.Begin);
        System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);
        // Read the content.
        var object_string = await reader0.ReadToEndAsync ();


        var p_Version_Specification = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Version_Specification>(object_string); 
*/
        //if(!string.IsNullOrWhiteSpace(json))
        try
        {
            string id_val = p_Version_Specification._id;


            string check_url = db_config.url + "/metadata/"  + id_val;
            cURL check_document_curl = new cURL ("Get", null, check_url, null, db_config.user_name, db_config.user_value);

            bool save_document = false;

            if(!string.IsNullOrWhiteSpace(p_Version_Specification._rev))
            {
                try
                {
                    string responseFromServer = await check_document_curl.executeAsync();
                    var check_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Version_Specification>(responseFromServer);

                    if
                    (
                        !string.IsNullOrWhiteSpace(check_result.data_type) &&
                        check_result.data_type == "version-specification" 
                    )
                    {
                        if(string.IsNullOrWhiteSpace(check_result.data_type))
                        {
                            save_document = true;
                        }
                        else if(check_result.publish_status != common.metadata.publish_status_enum.final)
                        {
                            save_document = true;
                        }
                        
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }


            if(save_document)
            {
                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(p_Version_Specification, settings);


                
                string metadata_url = db_config.url + "/metadata/"  + id_val;
                cURL document_curl = new cURL ("PUT", null, metadata_url, object_string, db_config.user_name, db_config.user_value);

                try
                {
                    string responseFromServer = await document_curl.executeAsync();
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        }
/*
        this.Response.Clear();
        this.Response.ClearHeaders();
        this.Response.AddHeader("Content-Type", "text/plain");
*/
        return result;
    }


// POST api/values 
    [Authorize(Roles  = "form_designer")]
    [HttpPost]
    [HttpPut]
    public mmria.common.model.couchdb.document_put_response SetValue
    (
        [FromBody] string id, string name, string value
    ) 
    { 
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        //if(!string.IsNullOrWhiteSpace(json))
        try
        {
            string id_val = id;

/*
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(p_Version_Specification, settings);


            
            string metadata_url = db_config.url + "/metadata/"  + id_val;
            cURL document_curl = new cURL ("PUT", null, metadata_url, object_string, db_config.user_name, db_config.user_value);

            try
            {
                string responseFromServer = await document_curl.executeAsync();
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
*/
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        }
/*
        this.Response.Clear();
        this.Response.ClearHeaders();
        this.Response.AddHeader("Content-Type", "text/plain");
*/
        return result;
    }

    async Task<string> GenerateFileAsync(string schemaJson)
    {
            string result = null;

            var schema = await NJsonSchema.JsonSchema.FromJsonAsync(schemaJson);
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

    [Authorize(Roles  = "form_designer")]
    [Route("add_attachement")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Add_Attachment
    (
        
        //[FromBody] mmria.common.metadata.Add_Attachement add_attachement
    ) 
    { 



        string document_content;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

            try
            {


                mmria.common.metadata.Add_Attachement add_attachement = null;

                System.IO.Stream dataStream0 = this.Request.Body;

                //dataStream0.Seek(0, System.IO.SeekOrigin.Begin);
                System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);

                document_content = await reader0.ReadToEndAsync ();


                add_attachement = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Add_Attachement>(document_content);

                if
                (
                    //p_version_specification.data_type == null ||
                    //p_version_specification.data_type != "version-specification" || 
                    add_attachement._id =="default_version_specification" ||
                    add_attachement._id == "2016-06-12T13:49:24.759Z" ||
                    add_attachement._id == "de-identified-list"

                )
                {
                    return null;
                }


                

                string metadata_url = db_config.url + $"/metadata/{add_attachement._id}/{add_attachement.doc_name}";

                var put_curl = new cURL("PUT", null, metadata_url, add_attachement.document_content, db_config.user_name, db_config.user_value, "text/*");
                put_curl.AddHeader("If-Match",  add_attachement._rev);

                string responseFromServer = await put_curl.executeAsync();

                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

                if (!result.ok) 
                {

                }

            }
            catch(Exception ex) 
            {
                Console.WriteLine (ex);
            }
            
        return result;
    } 


} 


