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
                // Story 29.9 — surface exceptions as an ImportFailed BatchItem so the batch
                // status UI reflects the failure and BatchProcessor.pending_items decrements.
                Console.WriteLine($"Process_Message Exception:\n{ex}");

                var failedItem = BuildImportFailedBatchItem(message, ex);
                var batchProcessor = Context.ActorSelection(message.BatchProcessorPath);
                batchProcessor.Tell(failedItem);
            }
        });
    }

    // Constructs the synthetic BatchItem sent back to BatchProcessor when Process_Message throws.
    // CDCUniqueID must match the value BatchProcessor put in batch_item_set at dispatch time
    // (message.cdc_unique_id) so the completion lookup succeeds. StatusDetail carries only the
    // first line of ex.Message; the full stack trace stays in the console log.
    internal static mmria.common.ije.BatchItem BuildImportFailedBatchItem(
        mmria.common.ije.StartBatchItemMessage message,
        Exception ex)
    {
        var detail = ex?.Message ?? string.Empty;
        var newlineIndex = detail.IndexOf('\n');
        if (newlineIndex >= 0)
        {
            detail = detail.Substring(0, newlineIndex).TrimEnd('\r');
        }

        return new mmria.common.ije.BatchItem
        {
            Status = mmria.common.ije.BatchItem.StatusEnum.ImportFailed,
            CDCUniqueID = message.cdc_unique_id,
            mmria_record_id = message.record_id,
            mmria_id = System.Guid.NewGuid().ToString(),
            ImportDate = message.ImportDate,
            ImportFileName = message.ImportFileName,
            ReportingState = message.host_state,
            case_folder = message.case_folder,
            StatusDetail = $"Processing exception: {detail}"
        };
    }

    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
}
