using System.IO;
using System.Text.Json;
using mmria.services.vitalsimport;
public class IJEReader
{
    string path = "//cdc.gov/project/CCHP_NCCD_DRH/PregMort2/STEVE_NAPHSIS/Import_Into_MMRIA/";

    public IJEReader(string p_path)
    {
        path = p_path;
    }


    public void execute()
    {

        var result = new List<string>();
        var file_list = new List<string>();
        var dir_list = new List<string>();
        var file_info_List = new List<FileInfo>();
        var dir_info_List = new List<DirectoryInfo>();


        foreach(var file_path in System.IO.Directory.GetFiles(path))
        {
            var fileInfo = new FileInfo(file_path);
            file_info_List.Add(fileInfo);

        }

        foreach(var dir_path in System.IO.Directory.GetDirectories(path))
        {
             var dirInfo = new DirectoryInfo(dir_path);
             dir_info_List.Add(dirInfo);
        }

        var extension_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mor",
            ".nat",
            ".fet"
        };

        file_info_List = file_info_List.Where( x=>  extension_set.Contains(x.Extension)).OrderByDescending( x => x.CreationTime ).ToList();
        //dir_info_List = dir_info_List.Where( x=> (DateTime.Now - x.CreationTime).Days > over_number_of_days).OrderByDescending( x => x.CreationTime ).ToList();
        dir_info_List = dir_info_List.OrderByDescending( x => x.CreationTime ).ToList();

        long total_length = 0;

        foreach(var fileInfo in file_info_List)
        {
            total_length += fileInfo.Length;
            var size = 0.0;
            
            if(fileInfo.Length > 1_000_000)
            {
                size = fileInfo.Length / 1_000_000.0;

                file_list.Add($"rm -rf {fileInfo.Name}");
            }
            else if(fileInfo.Length > 1_000)
            {
                size = fileInfo.Length / 1_000.0;;
                file_list.Add($"rm -rf {fileInfo.Name}");
            }
            else
            {
                size = fileInfo.Length;
                file_list.Add($"rm -rf {fileInfo.Name}");
            }

            fileInfo.Delete();
            
        }

        foreach(var dirInfo in dir_info_List)
        {
            

            dir_list.Add($"rm -rf {dirInfo.Name}");
            foreach(var fileInfo in dirInfo.GetFiles())
            {
                total_length += fileInfo.Length;
            }

            //dirInfo.Delete(true);
        }

    }
    private void Process_Message(mmria.common.ije.NewIJESet_Message message)
    {

    }
    
    private void Process_Message
    (
        mmria.services.vitalsimport.Messages.RecordUpload_Message message
    )
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
            Console.WriteLine($"An exception has occured: {System.Text.Json.JsonSerializer.Serialize(ex)}");
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
            var flr = new mmria.services.vitalsimport.Utilities.FixedLengthReader(segment);

            //Read teh segment into an object
            flr.read(record);

            //Add the object to our list 
            records.Add(record);
        }


        //Debugging only on ECPaaS, can probably remove
        Console.WriteLine(extractResultsMessageText);
        //Debugging only on ECPaaS, can probably remove
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(records));

        //For each Record create a CSV for it
        for (int i = 0; i < records.Count; i++)
        {
            string desitanationFileName = Path.Combine(Environment.GetEnvironmentVariable("CSV_DESTINATION"), $"{Path.GetFileName(filename)}_{now}_{i+1}_of_{records.Count}.csv");
            File.WriteAllText(desitanationFileName, mmria.services.vitalsimport.Utilities.CsvConverter.ToCSV(records[i]));
        }
    }
}