using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mmria.common.SharedLibraries.VitalImport.Manager;

namespace mmria.services.vitalsimport.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public sealed class VitalNotificationController : ControllerBase
{
    private readonly ActorSystem _actorSystem;
    private readonly VitalImportManager _vitalImportManager;

    public VitalNotificationController(ActorSystem actorSystem, VitalImportManager vitalImportManager)
    {
        _actorSystem = actorSystem;
        _vitalImportManager = vitalImportManager;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<List<mmria.common.ije.Batch>> Get()
    {
        var result = new List<mmria.common.ije.Batch>();

        try
        {
            var alldocs = await _vitalImportManager.GetBatchSetAsync(CreateVitalImportDbConfig());
            foreach (var item in alldocs?.rows ?? Array.Empty<mmria.common.model.couchdb.alldoc_item<mmria.common.ije.Batch>>())
            {
                result.Add(item.doc);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    [HttpDelete]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<bool> Delete()
    {
        var result = true;
        var batchList = new List<mmria.common.ije.Batch>();

        try
        {
            var alldocs = await _vitalImportManager.GetBatchSetAsync(CreateVitalImportDbConfig());
            foreach (var item in alldocs?.rows ?? Array.Empty<mmria.common.model.couchdb.alldoc_item<mmria.common.ije.Batch>>())
            {
                batchList.Add(item.doc);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        foreach (var item in batchList)
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

    private static mmria.common.couchdb.DBConfigurationDetail CreateVitalImportDbConfig()
    {
        return new mmria.common.couchdb.DBConfigurationDetail
        {
            url = mmria.services.vitalsimport.Program.couchdb_url,
            prefix = string.Empty,
            user_name = mmria.services.vitalsimport.Program.timer_user_name,
            user_value = mmria.services.vitalsimport.Program.timer_value
        };
    }
}
