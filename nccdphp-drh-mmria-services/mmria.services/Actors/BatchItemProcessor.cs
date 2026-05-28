using System;
using System.Threading.Tasks;
using Akka.Actor;
using RecordsProcessor_Worker.Services;

namespace RecordsProcessor_Worker.Actors;

public sealed class BatchItemProcessor : ReceiveActor
{
    private readonly BatchItemProcessingService _batchItemProcessingService;

    public BatchItemProcessor(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _batchItemProcessingService = new BatchItemProcessingService(couchDbHttpClient);

        ReceiveAsync<mmria.common.ije.StartBatchItemMessage>(async message =>
        {
            Console.WriteLine("Message Received");
            try
            {
                var (_, batchItem) = await _batchItemProcessingService.Process_Message(message);

                var batchProcessor = Context.ActorSelection(message.BatchProcessorPath);
                batchProcessor.Tell(batchItem);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Process_Message Exception:\n{ex}");
            }
        });
    }

    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
}
