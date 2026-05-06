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
                var (completion, batchItem) = await _batchItemProcessingService.Process_Message(message);

                var batchProcessor = Context.ActorSelection(message.BatchProcessorPath);
                batchProcessor.Tell(completion);
                batchProcessor.Tell(batchItem);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Process_Message Exception:\n{ex}");
                var batchProcessor = Context.ActorSelection(message.BatchProcessorPath);
                batchProcessor.Tell(new mmria.common.ije.BatchItemComplete
                {
                    cdc_unique_id = message.cdc_unique_id,
                    success = false,
                    error_message = "IJE item import failed before completion."
                });
                batchProcessor.Tell(new mmria.common.ije.BatchItem
                {
                    Status = mmria.common.ije.BatchItem.StatusEnum.ImportFailed,
                    CDCUniqueID = message.cdc_unique_id,
                    ImportDate = message.ImportDate,
                    ImportFileName = message.ImportFileName,
                    ReportingState = message.host_state,
                    mmria_record_id = message.record_id,
                    case_folder = message.case_folder,
                    StatusDetail = "IJE item import failed before completion."
                });
            }
        });
    }

    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
}
