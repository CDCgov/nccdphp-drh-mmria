using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Akka.Actor;
using RecordsProcessor_Worker.Actors;
using mmria.services.vitalsimport.Actors.VitalsImport;

namespace mmria.services.vitalsimport;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private string _queueName = "vitals_import_queue";
    private ActorSystem _actorSystem;


    public Worker(ILogger<Worker> logger, ActorSystem actorSystem)
    {
        _logger = logger;
        _actorSystem = actorSystem;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }
}

