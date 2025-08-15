using Akka.Actor;
using Newtonsoft.Json;
using mmria.services.vitalsimport.Messages;
using mmria.services.vitalsimport.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace mmria.services.vitalsimport.Actors.VitalsImport;

public sealed class Recieve_Import_Actor : ReceiveActor
{
    protected override void PreStart() => Console.WriteLine("Recieve_Import_Actor started");
    protected override void PostStop() => Console.WriteLine("Recieve_Import_Actor stopped");

    public Recieve_Import_Actor()
    {
        Receive<RecordUpload_Message>(message =>
        {
            Console.WriteLine("Message Recieved");
            //Console.WriteLine(JsonConvert.SerializeObject(message));
            Sender.Tell("Message Recieved");
            Process_Message(message);
        });

        Receive<mmria.common.ije.NewIJESet_Message>(message =>
        {
            Console.WriteLine("Message Recieved");
            //Console.WriteLine(JsonConvert.SerializeObject(message));
            Sender.Tell("Message Recieved");
            Process_Message(message);
        });

        Receive<string>(message =>
        {
            Console.WriteLine("Message Recieved");
            //Console.WriteLine(JsonConvert.SerializeObject(message));
            Sender.Tell("Message Recieved");

            try
            {
                var convertedMessage = JsonConvert.DeserializeObject<RecordUpload_Message>(message);
                Process_Message(convertedMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception has occured converting message: {JsonConvert.SerializeObject(ex)}");
                throw;
            }
        });
    }

    private void Process_Message(mmria.common.ije.NewIJESet_Message message)
    {

    }
    
    private void Process_Message(RecordUpload_Message message)
    {
        Console.WriteLine("Message Process kicked off");
        try
        {
            if (message?.filenames != null && message.filenames.Any())
            {
                var now = DateTime.Now.ToString("MMddyy_Hmmss");

                foreach (var name in message.filenames)
                {
                    string filename = Path.Combine(message.location, name);

                    using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Delete))
                    {
                        if (filename.ToLower().Contains(".fet"))
                        {
                            var totalRecordLength = FET_File_Record.Record_Length + 2; //Taken From Spreadsheet provided + 2 to account for /r/n

                            ExtractFileAndConvert<FET_File_Record>(fs, totalRecordLength, "FET Extract Results", filename, now);
                        }
                        else if (filename.ToLower().Contains(".mor"))
                        {
                            var totalRecordLength = MOR_File_Record.Record_Length + 2; //Taken From Spreadsheet provided + 2 to account for /r/n

                            ExtractFileAndConvert<MOR_File_Record>(fs, totalRecordLength, "MOR Extract Results", filename, now);
                        }
                        else if (filename.ToLower().Contains(".nat"))
                        {
                            var totalRecordLength = NAT_File_Record.Record_Length + 2; //Taken From Spreadsheet provided + 2 to account for /r/n

                            ExtractFileAndConvert<NAT_File_Record>(fs, totalRecordLength, "NAT Extract Results", filename, now);
                        }
                        else
                        {
                            Console.WriteLine($"File {name}: This file type has no processor associated with it. Will be deleted.");

                        }

                        Console.WriteLine($"File {name}: Processed");
                        File.Delete(filename);
                        Console.WriteLine($"File {name}: Deleted");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Message Contained no files to process");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An exception has occured: {JsonConvert.SerializeObject(ex)}");
            throw;
        }
    }

    private void ExtractFileAndConvert<T>(FileStream fs, int recordLength, string extractResultsMessageText, string filename, string now) where T : class
    {
        var lengthOfFile = fs.Length;
        var totalRecordLength = recordLength;
        var totalCompleteRecords = lengthOfFile / totalRecordLength;
        var records = new List<T>();

        for (int i = 0; i < totalCompleteRecords; i++)
        {
            var segment = new MemoryStream();

            //Set the postition of the file to the next record in the case of an incomplete read
            fs.Position = i * totalRecordLength;

            //Work only with the file segment where the stream is at.
            fs.CopyTo(segment);

            var record = (T)Activator.CreateInstance(typeof(T));

            //Put the file segment into the reader
            var flr = new FixedLengthReader(segment);

            //Read teh segment into an object
            flr.read(record);

            //Add the object to our list 
            records.Add(record);
        }


        //Debugging only on ECPaaS, can probably remove
        Console.WriteLine(extractResultsMessageText);
        //Debugging only on ECPaaS, can probably remove
        Console.WriteLine(JsonConvert.SerializeObject(records));

        //For each Record create a CSV for it
        for (int i = 0; i < records.Count; i++)
        {
            string desitanationFileName = Path.Combine(Environment.GetEnvironmentVariable("CSV_DESTINATION"), $"{Path.GetFileName(filename)}_{now}_{i+1}_of_{records.Count}.csv");
            File.WriteAllText(desitanationFileName, CsvConverter.ToCSV(records[i]));
        }
    }
}

