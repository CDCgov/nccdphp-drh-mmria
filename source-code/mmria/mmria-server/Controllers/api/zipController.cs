using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server;

[Authorize(Roles  = "abstractor,data_analyst")]
[Route("api/[controller]")]
public sealed class zipController: ControllerBase
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public zipController 
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    

    [HttpGet("{id}")]
    public async System.Threading.Tasks.Task<FileResult> Get (string id)
    {
        string file_name = null;

        var get_item_curl = new cURL ("GET", null, db_config.url + $"/{db_config.prefix}export_queue/" + id, null, db_config.user_name, db_config.user_value);
        string responseFromServer = await get_item_curl.executeAsync ();
        export_queue_item export_queue_item = Newtonsoft.Json.JsonConvert.DeserializeObject<export_queue_item> (responseFromServer);

        file_name = export_queue_item.file_name;

        var path = System.IO.Path.Combine (configuration.GetString("export_directory", host_prefix), export_queue_item.file_name);

        export_queue_item.status = "Downloaded";

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        string object_string = Newtonsoft.Json.JsonConvert.SerializeObject (export_queue_item, settings); 
        var set_item_curl = new cURL ("PUT", null, db_config.url + $"/{db_config.prefix}export_queue/" + export_queue_item._id, object_string, db_config.user_name, db_config.user_value);
        responseFromServer = await set_item_curl.executeAsync ();
        
        byte[] fileBytes = GetFile(path);
        return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, file_name);

    }


    byte[] GetFile(string s)
    {
        byte[] data;
        int br;
        int fs_length;

        using(FileStream fs = new FileStream (s, FileMode.Open, FileAccess.Read))
        {
            fs_length = (int) fs.Length;
            data = new byte[fs.Length];
            br = fs.Read(data, 0, data.Length);
        }
        if (br != (int) fs_length)
            throw new System.IO.IOException(s);
        return data;
    }

}




