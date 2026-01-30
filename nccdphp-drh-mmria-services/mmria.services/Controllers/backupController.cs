using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
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

namespace mmria.services.vitalsimport.Controllers;

[Authorize]
[Route("api/[controller]/[action]")]
[ApiController]
public sealed class backupController : Controller
{
    private ActorSystem _actorSystem;

    public backupController
    (
        ActorSystem actorSystem
    )
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


    [HttpGet]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<IActionResult> PerformCompression()
    {
        var  message = new mmria.services.backup.BackupSupervisor.PerformBackupMessage()
        {
            type = "compress",
            DateStarted = DateTime.Now
        };


        var bsr = _actorSystem.ActorSelection("user/backup-supervisor");
        bsr.Tell(message); 


        return Ok();
    }


    [HttpGet]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<IActionResult> GetFileList()
    {
        string root_folder = Program.DbConfigSet.name_value["backup_storage_root_folder"];

        var result = new List<string>();
        var file_list = new List<string>();
        var dir_list = new List<string>();
        var file_info_List = new List<FileInfo>();
        var dir_info_List = new List<DirectoryInfo>();

        foreach(var file_path in System.IO.Directory.GetFiles(root_folder))
        {
            var fileInfo = new FileInfo(file_path);
            file_info_List.Add(fileInfo);

        }

        foreach(var dir_path in System.IO.Directory.GetDirectories(root_folder))
        {
             var dirInfo = new DirectoryInfo(dir_path);
             dir_info_List.Add(dirInfo);
        }

        file_info_List = file_info_List.OrderByDescending( x => x.CreationTime ).ToList();
        dir_info_List = dir_info_List.OrderByDescending( x => x.CreationTime ).ToList();

        long total_length = 0;

        foreach(var fileInfo in file_info_List)
        {
            total_length += fileInfo.Length;
            var size = 0.0;
            
            if(fileInfo.Length > 1_000_000)
            {
                size = fileInfo.Length / 1_000_000.0;
                file_list.Add($"--- {fileInfo.Name} : {size:#0.00} Mb");
            }
            else if(fileInfo.Length > 1_000)
            {
                size = fileInfo.Length / 1_000.0;;
                file_list.Add($"--- {fileInfo.Name} : {size:#0.00} Kb");
            }
            else
            {
                size = fileInfo.Length;
                file_list.Add($"--- {fileInfo.Name} : {size:#0.00} bytes");
            }
        }

        foreach(var dirInfo in dir_info_List)
        {
            dir_list.Add($"d-- {dirInfo.Name}");
            foreach(var fileInfo in dirInfo.GetFiles())
            {
                total_length += fileInfo.Length;
            }
            
        }

        var total_size =  0.0;
        if(total_length > 1_000_000_000)
        {
            total_size = total_length / 1_000_000_000.0;
            result.Add($"total file size: {total_size:##0.00} Gb");
        }
        else if(total_length > 1_000_000)
        {
            total_size = total_length / 1_000_000.0;
            result.Add($"total file size: {total_size:##0.00} Mb");
        }
        else if(total_length > 1_000)
        {
            total_size = total_length / 1_000.0;
            result.Add($"total file size: {total_size:##0.00} Kb");
        }
        else 
        {
            total_size = total_length;
            result.Add($"total file size: {total_size:##0.00} bytes");
        }


        
        result.AddRange(file_list);
        result.AddRange(dir_list);


        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<IActionResult> GetSubFolderFileList(string id)
    {
        string root_folder = Program.DbConfigSet.name_value["backup_storage_root_folder"];

        if(!string.IsNullOrWhiteSpace(id))
        {
            root_folder = System.IO.Path.Combine(root_folder, id);
        }

        var result = new List<string>();
        var file_list = new List<string>();
        var dir_list = new List<string>();
        var file_info_List = new List<FileInfo>();
        var dir_info_List = new List<DirectoryInfo>();

        foreach(var file_path in System.IO.Directory.GetFiles(root_folder))
        {
            var fileInfo = new FileInfo(file_path);
            file_info_List.Add(fileInfo);

        }

        foreach(var dir_path in System.IO.Directory.GetDirectories(root_folder))
        {
             var dirInfo = new DirectoryInfo(dir_path);
             dir_info_List.Add(dirInfo);
        }

        file_info_List = file_info_List.OrderByDescending( x => x.CreationTime ).ToList();
        dir_info_List = dir_info_List.OrderByDescending( x => x.CreationTime ).ToList();

        long total_length = 0;

        foreach(var fileInfo in file_info_List)
        {
            total_length += fileInfo.Length;
            var size = 0.0;
            
            if(fileInfo.Length > 1_000_000)
            {
                size = fileInfo.Length / 1_000_000.0;
                file_list.Add($"--- {fileInfo.Name} : {size:#0.00} Mb");
            }
            else if(fileInfo.Length > 1_000)
            {
                size = fileInfo.Length / 1_000.0;;
                file_list.Add($"--- {fileInfo.Name} : {size:#0.00} Kb");
            }
            else
            {
                size = fileInfo.Length;
                file_list.Add($"--- {fileInfo.Name} : {size:#0.00} bytes");
            }
        }

        foreach(var dirInfo in dir_info_List)
        {
            dir_list.Add($"d-- {dirInfo.Name}");
        }

        var total_size =  0.0;
        if(total_length > 1_000_000_000)
        {
            total_size = total_length / 1_000_000_000.0;
            result.Add($"total file size: {total_size:##0.00} Gb");
        }
        else if(total_length > 1_000_000)
        {
            total_size = total_length / 1_000_000.0;
            result.Add($"total file size: {total_size:##0.00} Mb");
        }
        else if(total_length > 1_000)
        {
            total_size = total_length / 1_000.0;
            result.Add($"total file size: {total_size:##0.00} Kb");
        }
        else 
        {
            total_size = total_length;
            result.Add($"total file size: {total_size:##0.00} bytes");
        }


        
        result.AddRange(file_list);
        result.AddRange(dir_list);


        return Ok(result);
    }


    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<IActionResult> GetFile(string id)
    {
        string root_folder = Program.DbConfigSet.name_value["backup_storage_root_folder"];
        var file_path = System.IO.Path.Combine(root_folder, id);

        if(System.IO.File.Exists(file_path))
        {
            return new PhysicalFileResult
            (
                file_path, 
                "application/octet-stream"
            ) 
            { 
                FileDownloadName = id 
            };
        }
        else
        {
            return NotFound();
        }


    }

    [HttpGet("{folder}/{file_name}")]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<IActionResult> GetSubFolderFile(string folder, string file_name)
    {
        string root_folder = Program.DbConfigSet.name_value["backup_storage_root_folder"];
        var file_path = System.IO.Path.Combine(root_folder, folder, file_name);

        if(System.IO.File.Exists(file_path))
        {
            return new PhysicalFileResult
            (
                file_path, 
                "application/octet-stream"
            ) 
            { 
                FileDownloadName = file_name 
            };
        }
        else
        {
            return NotFound();
        }


    }



    [HttpGet("{over_number_of_days}")]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<IActionResult> GetRemoveFileList(int over_number_of_days)
    {
        string root_folder = Program.DbConfigSet.name_value["backup_storage_root_folder"];

        var result = new List<string>();
        var file_list = new List<string>();
        var dir_list = new List<string>();
        var file_info_List = new List<FileInfo>();
        var dir_info_List = new List<DirectoryInfo>();

        if(over_number_of_days < 30)
        {
            return Ok(result);
        }

        foreach(var file_path in System.IO.Directory.GetFiles(root_folder))
        {
            var fileInfo = new FileInfo(file_path);
            file_info_List.Add(fileInfo);

        }

        foreach(var dir_path in System.IO.Directory.GetDirectories(root_folder))
        {
             var dirInfo = new DirectoryInfo(dir_path);
             dir_info_List.Add(dirInfo);
        }

        file_info_List = file_info_List.Where( x=> (DateTime.Now - x.CreationTime).Days > over_number_of_days).OrderByDescending( x => x.CreationTime ).ToList();
        dir_info_List = dir_info_List.Where( x=> (DateTime.Now - x.CreationTime).Days > over_number_of_days).OrderByDescending( x => x.CreationTime ).ToList();

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
        }

        foreach(var dirInfo in dir_info_List)
        {
            dir_list.Add($"rm -rf {dirInfo.Name}");
            foreach(var fileInfo in dirInfo.GetFiles())
            {
                total_length += fileInfo.Length;
            }
            
        }

        var total_size =  0.0;
        if(total_length > 1_000_000_000)
        {
            total_size = total_length / 1_000_000_000.0;
            result.Add($"total file size: {total_size:##0.00} Gb");
        }
        else if(total_length > 1_000_000)
        {
            total_size = total_length / 1_000_000.0;
            result.Add($"total file size: {total_size:##0.00} Mb");
        }
        else if(total_length > 1_000)
        {
            total_size = total_length / 1_000.0;
            result.Add($"total file size: {total_size:##0.00} Kb");
        }
        else 
        {
            total_size = total_length;
            result.Add($"total file size: {total_size:##0.00} bytes");
        }


        
        result.AddRange(file_list);
        result.AddRange(dir_list);


        return Ok(result);
    }



    [HttpGet("{over_number_of_days}")]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public async Task<IActionResult> RemoveFiles(int over_number_of_days)
    {
        string root_folder = Program.DbConfigSet.name_value["backup_storage_root_folder"];

        var result = new List<string>();
        var file_list = new List<string>();
        var dir_list = new List<string>();
        var file_info_List = new List<FileInfo>();
        var dir_info_List = new List<DirectoryInfo>();

        if(over_number_of_days < 30)
        {
            return Ok(result);
        }

        foreach(var file_path in System.IO.Directory.GetFiles(root_folder))
        {
            var fileInfo = new FileInfo(file_path);
            file_info_List.Add(fileInfo);

        }

        foreach(var dir_path in System.IO.Directory.GetDirectories(root_folder))
        {
             var dirInfo = new DirectoryInfo(dir_path);
             dir_info_List.Add(dirInfo);
        }

        file_info_List = file_info_List.Where( x=> (DateTime.Now - x.CreationTime).Days > over_number_of_days).OrderByDescending( x => x.CreationTime ).ToList();
        dir_info_List = dir_info_List.Where( x=> (DateTime.Now - x.CreationTime).Days > over_number_of_days).OrderByDescending( x => x.CreationTime ).ToList();

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

            dirInfo.Delete(true);
        }

        var total_size =  0.0;
        if(total_length > 1_000_000_000)
        {
            total_size = total_length / 1_000_000_000.0;
            result.Add($"total file size: {total_size:##0.00} Gb");
        }
        else if(total_length > 1_000_000)
        {
            total_size = total_length / 1_000_000.0;
            result.Add($"total file size: {total_size:##0.00} Mb");
        }
        else if(total_length > 1_000)
        {
            total_size = total_length / 1_000.0;
            result.Add($"total file size: {total_size:##0.00} Kb");
        }
        else 
        {
            total_size = total_length;
            result.Add($"total file size: {total_size:##0.00} bytes");
        }


        
        result.AddRange(file_list);
        result.AddRange(dir_list);


        return Ok(result);
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

    async Task<byte[]> ReadFile(string s)
    {
        byte[] data;
        int br;
        int fs_length;

        using(FileStream fs = new FileStream (s, FileMode.Open, FileAccess.Read))
        {
            fs_length = (int) fs.Length;
            data = new byte[fs.Length];
            br = await fs.ReadAsync(data, 0, data.Length);
        }
        if (br != (int) fs_length)
            throw new System.IO.IOException(s);
        return data;
    }

    char[] Base64EncodeBytes(byte[] inputBytes) 
    {
        // Each 3-byte sequence in inputBytes must be converted to a 4-byte 
        // sequence 
        long arrLength = (long)(4.0d * inputBytes.Length / 3.0d);
        if ((arrLength  % 4) != 0) 
        {
            // increment the array length to the next multiple of 4 
            //    if it is not already divisible by 4
            arrLength += 4 - (arrLength % 4);
        }

        char[] encodedCharArray = new char[arrLength];
        Convert.ToBase64CharArray(inputBytes, 0, inputBytes.Length, encodedCharArray, 0);

        return encodedCharArray;
    }

}
