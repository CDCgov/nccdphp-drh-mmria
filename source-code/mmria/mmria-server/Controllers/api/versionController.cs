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
using mmria.common.utils;
using Newtonsoft.Json.Linq;

using  mmria.server.extension; 
using mmria.server.util;
namespace mmria.server;

[Route("api/[controller]")]
public sealed class versionController: ControllerBase
{ 

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager _metadataVersionManager;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.SharedLibraries.MetadataVersion.IMetadataRepository _metadataRepository;
    public Dictionary<string, string> formName = new Dictionary<string, string>();
    public versionController
(
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.MetadataVersion.Manager.MetadataVersionManager metadataVersionManager,
        mmria.common.SharedLibraries.MetadataVersion.IMetadataRepository metadataRepository
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        _metadataVersionManager = metadataVersionManager;
        _metadataRepository = metadataRepository;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
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
            result = await _metadataVersionManager.ListVersionSpecificationsAsync(db_config);
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
            result = await _metadataVersionManager.GetVersionSpecificationMetadataAsync(version_specification_id, db_config);
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
            result = await _metadataVersionManager.GetValidatorAsync(db_config);
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
    public async Task<string> export_all_generate_name_map
    (
        string version_specification_id,
        string type = "all"
    )
    {

        var export_all_generate_name_map = new mmria.server.utils.export_all_generate_name_map(db_config, _metadataRepository);

        var result = await export_all_generate_name_map.ExecuteAsync(version_specification_id, type);

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
            string responseString = await _metadataVersionManager.GetVersionDocumentAsync(version_specification_id, document_name, db_config);
            if (string.Equals(document_name, "metadata", StringComparison.OrdinalIgnoreCase))
            {
                responseString = ApplyOmbExpirationDateToMetadata(responseString);
            }

            string type="javascript";
            if(!string.IsNullOrWhiteSpace(document_name))
            switch(document_name.ToLower())
            {
                case "metadata":
                case "ui_specification":
                    type="json";
                    break;
            }

            byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(responseString);
            result = File(responseBytes, $"application/{type}", "validator");


            

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    } 

    private string ApplyOmbExpirationDateToMetadata(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return metadataJson;
        }

        try
        {
            var metadata = JToken.Parse(metadataJson);
            ApplyOmbExpirationDateToMetadataToken(metadata, GetOmbExpirationDate());
            return metadata.ToString(Newtonsoft.Json.Formatting.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying OMB expiration date to version metadata: {ex}");
            return metadataJson;
        }
    }

    private void ApplyOmbExpirationDateToMetadataToken(JToken token, string ombExpirationDate)
    {
        if (token is JObject metadataObject)
        {
            if (string.Equals(
                metadataObject.Value<string>("name"),
                "omb_expiration_label",
                StringComparison.OrdinalIgnoreCase))
            {
                metadataObject["prompt"] = $"Exp. Date {ombExpirationDate}";
            }

            foreach (var property in metadataObject.Properties().ToList())
            {
                ApplyOmbExpirationDateToMetadataToken(property.Value, ombExpirationDate);
            }
        }
        else if (token is JArray metadataArray)
        {
            foreach (var item in metadataArray)
            {
                ApplyOmbExpirationDateToMetadataToken(item, ombExpirationDate);
            }
        }
    }

    private string GetOmbExpirationDate()
    {
        return configuration.GetString("omb_expiration_date", host_prefix) ?? "05/31/2026";
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
    // POST api/values 
    [Authorize(Roles  = "form_designer")]
    [Route("save")]
    [HttpPost]
    [HttpPut]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post() 
    { 
        var p_Version_Specification = await JsonRequestBodyReader.ReadAsync<mmria.common.metadata.Version_Specification>(Request);
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();
        var sanitizedVersionSpecification = DocumentPayloadCloneHelper.CloneVersionSpecification(p_Version_Specification, GetCurrentUserName());

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
            if (sanitizedVersionSpecification == null)
            {
                return result;
            }

            result = await _metadataVersionManager.SaveVersionSpecificationAsync(sanitizedVersionSpecification, db_config);
            if (result == null || !result.ok)
            {
                var revisionHandling = CouchDbRevisionHelper.DescribeRevisionHandling(p_Version_Specification?._rev, null);
                Console.WriteLine(
                    $"Version specification save failed for {sanitizedVersionSpecification._id}: rev={revisionHandling}; response={result?.error_description}");
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



        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

            try
            {


                mmria.common.metadata.Add_Attachement add_attachement = null;

                System.IO.Stream dataStream0 = this.Request.Body;

                //dataStream0.Seek(0, System.IO.SeekOrigin.Begin);
                System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);

                var document_content = await reader0.ReadToEndAsync ();
                add_attachement = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Add_Attachement>(document_content);
                var sanitizedAttachment = DocumentPayloadCloneHelper.CloneAddAttachment(add_attachement);
                if (sanitizedAttachment == null)
                {
                    return result;
                }

                result = await _metadataVersionManager.SaveVersionAttachmentAsync(sanitizedAttachment, db_config, false);
                if (result == null || !result.ok)
                {
                    var revisionHandling = CouchDbRevisionHelper.DescribeRevisionHandling(add_attachement?._rev, null);
                    Console.WriteLine(
                        $"Version attachment save failed for {sanitizedAttachment._id}: rev={revisionHandling}; response={result?.error_description}");
                }

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

    private string GetCurrentUserName()
    {
        if (User?.Identities?.Any(u => u.IsAuthenticated) == true)
        {
            return User.Identities.First(
                u => u.IsAuthenticated &&
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                .FindFirst(System.Security.Claims.ClaimTypes.Name)
                .Value;
        }

        return null;
    }


} 


