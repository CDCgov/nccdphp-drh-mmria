using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using mmria.services.vitalsimport.Actors.VitalsImport;
using mmria.services.vitalsimport.Messages;
using System;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using mmria.common.SharedLibraries.VitalImport;
using mmria.common.couchdb;

namespace mmria.services.vitalsimport.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public sealed class VitalNotificationController : ControllerBase
{
    private ActorSystem _actorSystem;
    private mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly IVitalImportRepository? _vitalImportRepository;

    public VitalNotificationController(ActorSystem actorSystem, mmria.common.getset.CouchDbHttpClient couchDbHttpClient, IVitalImportRepository? vitalImportRepository = null)
    {
        _actorSystem = actorSystem;
        _couchDbHttpClient = couchDbHttpClient;
        _vitalImportRepository = vitalImportRepository;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<List<mmria.common.ije.Batch>> Get()
    {
        var  result = new List<mmria.common.ije.Batch>();

        try
        {
            var dbConfig = new DBConfigurationDetail
            {
                url = mmria.services.vitalsimport.Program.couchdb_url,
                user_name = mmria.services.vitalsimport.Program.timer_user_name,
                user_value = mmria.services.vitalsimport.Program.timer_value
            };
            var alldocs = await _vitalImportRepository!.GetAllBatchesAsync(dbConfig);

            foreach(var item in alldocs.rows)
            {
                result.Add(item.doc);
            }
        }
        catch(Exception ex)
        {
            //Console.Write("auth_session_token: {0}", auth_session_token);
            Console.WriteLine(ex);
        }



        return result;
    }


    [HttpDelete]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<bool> Delete()
    {
        var  result = true;

        var  batch_list = new List<mmria.common.ije.Batch>();

        try
        {
            var dbConfig = new DBConfigurationDetail
            {
                url = mmria.services.vitalsimport.Program.couchdb_url,
                user_name = mmria.services.vitalsimport.Program.timer_user_name,
                user_value = mmria.services.vitalsimport.Program.timer_value
            };
            var alldocs = await _vitalImportRepository!.GetAllBatchesAsync(dbConfig);

            foreach(var item in alldocs.rows)
            {
                batch_list.Add(item.doc);
            }
        }
        catch(Exception ex)
        {
            //Console.Write("auth_session_token: {0}", auth_session_token);
            Console.WriteLine(ex);
        }

        foreach(var item in batch_list)
        {
            var message = new mmria.common.ije.BatchRemoveDataMessage()
            {
                id = item.id,
                date_of_removal = DateTime.Now
            };

            var bsr = _actorSystem.ActorSelection("user/batch-supervisor");
            bsr.Tell(message);
        }


        return result;
    }

}
