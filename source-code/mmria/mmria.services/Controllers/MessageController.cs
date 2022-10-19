﻿using Akka.Actor;
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

namespace mmria.services.vitalsimport.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public sealed class MessageController : ControllerBase
{
    private ActorSystem _actorSystem;
    private IConfiguration _configurationSet;

    public MessageController(ActorSystem actorSystem, IConfiguration configurationSet)
    {
        _actorSystem = actorSystem;
        _configurationSet = configurationSet;
    }


    [HttpGet("_health")]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public ObjectResult _health()
    {
        string health = string.Empty;

        object status = new
        {
            alive = true,
            //RabbitMQ_Alive = IsRabbitMQConnectionAlive()
        };

        health = JsonConvert.SerializeObject(status);

        return Ok(health);
    }

    [HttpPost("Read")]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public void ReadMessage([FromBody]RecordUpload_Message body)
    {
        var processor = _actorSystem.ActorOf<Recieve_Import_Actor>();

        processor.Tell(body);

        //string rabbitMQHostName = Environment.GetEnvironmentVariable("RABBITMQ_HOSTNAME");
        //Console.WriteLine($"RabbitMQ Host = {rabbitMQHostName}");

        //var factory = new ConnectionFactory() { HostName = rabbitMQHostName };

        //using (var connection = factory.CreateConnection())
        //using (var channel = connection.CreateModel())
        //{
        //    channel.QueueDeclare(queue: "hello",
        //                         durable: false,
        //                         exclusive: false,
        //                         autoDelete: false,
        //                         arguments: null);

        //    //var consumer = new EventingBasicConsumer(channel);
        //    //consumer.Received += (model, ea) =>
        //    //{
        //    //    var body = ea.Body.ToArray();
        //    //    var message = Encoding.UTF8.GetString(body);
        //    //    Console.WriteLine(" [x] Received {0}", message);
        //    //};
        //    //channel.BasicConsume(queue: "hello",
        //    //                     autoAck: true,
        //    //                     consumer: consumer);

        //    var consumer = new EventingBasicConsumer(channel);
        //    consumer.Received += (model, ea) =>
        //    {
        //        var body = ea.Body.ToArray();
        //        var message = Encoding.UTF8.GetString(body);
        //        Console.WriteLine(" [x] Received {0}", message);

        //        int dots = message.Split('.').Length - 1;

        //        Console.WriteLine(" [x] Done");
        //    };
        //    channel.BasicConsume(queue: "task_queue", autoAck: true, consumer: consumer);

        //    Console.WriteLine("Message Read Complete");
        //}
    }


    [HttpPut("IJESet")]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public mmria.common.ije.NewIJESet_MessageResponse ReadMessage([FromBody] mmria.common.ije.NewIJESet_MessageDTO body)
    {
        var processor = _actorSystem.ActorSelection("user/batch-supervisor");


        var NewIJESet_Message = new mmria.common.ije.NewIJESet_Message()
        {
            batch_id = System.Guid.NewGuid().ToString(),
            mor = body.mor,
            nat = body.nat,
            fet = body.fet,
            mor_file_name = body.mor_file_name,
            nat_file_name = body.nat_file_name,
            fet_file_name = body.fet_file_name
        };

        var result = new mmria.common.ije.NewIJESet_MessageResponse()
        {
            batch_id = NewIJESet_Message.batch_id,
            ok = true
        };

        processor.Tell(NewIJESet_Message);

        return result;
    }
}
