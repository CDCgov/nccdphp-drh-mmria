﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQ.Client;
using mmria.services.vitalsimport.Actors.VitalsImport;
using mmria.services.vitalsimport.Messages;
using System;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Net;

namespace mmria.services.vitalsimport.Controllers
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class backupController : Controller
    {
        private ActorSystem _actorSystem;

        public backupController(ActorSystem actorSystem)
        {
            _actorSystem = actorSystem;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "BasicAuthentication")]
        public async Task<IActionResult> PerformHotBackup()
        {
            var  message = new mmria.services.backup.BackupSupervisor.PerformBackupMessage()
            {
                type = "hot",
                DateStarted = DateTime.Now
            };


            var bsr = _actorSystem.ActorSelection("user/backup-supervisor");
            bsr.Tell(message); 


            return Ok();
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "BasicAuthentication")]
        public async Task<IActionResult> PerformColdBackup()
        {
            var  message = new mmria.services.backup.BackupSupervisor.PerformBackupMessage()
            {
                type = "cold",
                DateStarted = DateTime.Now
            };


            var bsr = _actorSystem.ActorSelection("user/backup-supervisor");
            bsr.Tell(message); 


            return Ok();
        }


        [HttpDelete]
        [Authorize(AuthenticationSchemes = "BasicAuthentication")]
        public async Task<bool> Delete()
        {
            var  result = true;

/*
            var  batch_list = new List<mmria.common.ije.Batch>();

            string url = $"{mmria.services.vitalsimport.Program.couchdb_url}/vital_import/_all_docs?include_docs=true";
            var document_curl = new mmria.getset.cURL ("GET", null, url, null, mmria.services.vitalsimport.Program.timer_user_name, mmria.services.vitalsimport.Program.timer_value);
            try
            {
                var responseFromServer = await document_curl.executeAsync();
                var alldocs = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch>>(responseFromServer);
    
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

*/
            return result;
        }

    }
}