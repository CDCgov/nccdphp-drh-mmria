using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using System;
using System.IO;
using System.Net.Http;

using Microsoft.AspNetCore.Authorization;
using System.Net;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.common.steve;

namespace mmria.server.Controllers;

[Authorize]
[Route("api/[controller]")]

public sealed class steveController : ControllerBase
{
    private ActorSystem _actorSystem;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public steveController
    (
        ActorSystem actorSystem, 
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        _actorSystem = actorSystem;
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }


    [HttpGet]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<mmria.common.metadata.Populate_CDC_Instance_Record> ReadMessage()
    {

        mmria.common.metadata.Populate_CDC_Instance_Record result = new ();
        var processor = _actorSystem.ActorSelection("user/populate-cdc-instance-supervisor");

        result = await processor.Ask(DateTime.Now) as mmria.common.metadata.Populate_CDC_Instance_Record;

        System.Console.WriteLine("here");

        return result;

    }


    [HttpPut]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<mmria.common.metadata.Populate_CDC_Instance_Record> ReadMessage([FromBody] mmria.common.metadata.Populate_CDC_Instance body)
    {
        mmria.common.metadata.Populate_CDC_Instance_Record result = new (); 

        var processor = _actorSystem.ActorSelection("user/populate-cdc-instance-supervisor");

        result = await processor.Ask(body) as mmria.common.metadata.Populate_CDC_Instance_Record;
        
        System.Console.WriteLine("here");

        return result;

        var AuthRequestBody = new AuthRequestBody()
        {
            seaBucketKMSKey = configuration.GetSharedString("steve_api:sea_bucket_kms_key"),
            clientName = configuration.GetSharedString("steve_api:client_name"),
            clientSecretKey = configuration.GetSharedString("steve_api:client_secret_key")
        };

        var base_url = configuration.GetSharedString("steve_api:base_url");
        var auth_url = $"{base_url}/auth";

        string jsonString = System.Text.Json.JsonSerializer.Serialize(AuthRequestBody);
        var curl = new cURL("POST", null, auth_url, jsonString, null, null);
        var response = await curl.executeAsync();

        //System.Console.WriteLine(response);

        var auth_reponse = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(response);

        var list_mailboxes_url = $"{base_url}/mailbox";
        var mail_box_curl = new cURL("GET", null, list_mailboxes_url, null, null, null);        
        mail_box_curl.AddHeader("Authorization","Bearer " + auth_reponse.token); 
        response = await mail_box_curl.executeAsync();
        //System.Console.WriteLine(response);

        var GetMailboxListResult = System.Text.Json.JsonSerializer.Deserialize<GetMailboxListResult>(response);
        
        foreach(var mail_box in GetMailboxListResult.mailboxes)
        {

/*
•	count (number)
•	fromDate (in YYYY-MM-DD format)
•	toDate (in YYYY-MM-DD format)
All three are optional. For example, the route to use for getting all (both read and unread) messages from November 1 to present would be:
/mailbox/{mailboxId}/all?fromDate=2022-11-01

To get only unread messages for the month of October, retrieving only 25 results maximum would be:
/mailbox/{mailboxId}/unread?count=25&fromDate=2022-10-01&toDate=2022-10-31

//toDate
//fromDate
*/
            var mailbox_unread_url = $"{base_url}/mailbox/{mail_box.mailboxId}/all";
            var mailbox_unread_curl = new cURL("GET", null, mailbox_unread_url, null, null, null);        
            mailbox_unread_curl.AddHeader("Authorization","Bearer " + auth_reponse.token); 
            response = await mailbox_unread_curl.executeAsync();

            var UnreadMessageResult = System.Text.Json.JsonSerializer.Deserialize<MailBoxMessageResult>(response);
            //if(UnreadMessageResult.unreadMessages?.Length > 0)
             //   System.Console.WriteLine(response);
        }

        var message_id = "9ddfd4b0-797c-45e8-8302-286b1c822546";
        //var message_id = "fc3f22a0-1206-486d-8ecf-fd5b81e18973";
        //var message_id = "ee19a81c-e13e-438a-b909-3c49571f9029";
        var download_message_url = $"{base_url}/file/{message_id}";
        var download_message_curl = new cURL("GET", null, download_message_url, null, null, null);        
        download_message_curl.AddHeader("Authorization","Bearer " + auth_reponse.token); 
        response = await download_message_curl.executeAsync();
        System.Console.WriteLine(response);

        System.Console.WriteLine("here");

    }
}
