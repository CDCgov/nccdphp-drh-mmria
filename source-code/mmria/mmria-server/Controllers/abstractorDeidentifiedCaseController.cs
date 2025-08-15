using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server.Controllers;

[Authorize(Roles  = "abstractor,data_analyst")]
public sealed class abstractorDeidentifiedCaseController : Controller
{
    public class DuplicateMultiformResult
    {
        
        public string _id {get;set;}
        public System.Collections.Generic.HashSet<string> field_list{ get; set;}
    }

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public abstractorDeidentifiedCaseController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }
        
    public IActionResult Index()
    {

        TempData["metadata_version"] = configuration.GetString("metadata_version", host_prefix);
        return View();
    }

    [HttpGet]
    public async Task<JsonResult> GetDuplicateMultiFormList()
    {
        var result = new DuplicateMultiformResult();

        try
        {
            string request_string = $"{db_config.url}/metadata/duplicate-multiform-list";

            var case_view_curl = new mmria.server.cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            string responseFromServer = await case_view_curl.executeAsync();

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<DuplicateMultiformResult>(responseFromServer);

        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex);
        }


        return Json(result);
    }

}
