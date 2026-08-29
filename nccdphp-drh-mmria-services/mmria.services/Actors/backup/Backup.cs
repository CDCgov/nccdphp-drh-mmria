using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.services.Utilities;

namespace mmria.services.backup;

public sealed class Backup
{
	public sealed class BackupResultMessage
	{
		public BackupResultMessage() {}

		public string Status { get; set;}

		public string Detail {get;set;}

		public int Doc_ID_Count { get; set;}
		public int SuccessCount { get; set;}
		public int ErrorCount { get; set; }
	}

	private HashSet<string> id_list = null;
	private string auth_token = null;
	private string user_name = null;
	private string password = null;
	private string backup_file_path = null;
	private string database_url = null;
	private string mmria_url = null;
	private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

	public Backup(mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
	{
		_couchDbHttpClient = couchDbHttpClient ?? throw new ArgumentNullException(nameof(couchDbHttpClient));
	}
	public async Task<BackupResultMessage> Execute (string [] args)
	{
		var result = new BackupResultMessage();
		string export_directory = null;

		if (args.Length > 1) 
		{
			for (var i = 1; i < args.Length; i++) 
			{
				string arg = args [i];
				int index = arg.IndexOf (':');
				string val = arg.Substring (index + 1, arg.Length - (index + 1)).Trim (new char [] { '\"' });

				if (arg.ToLower ().StartsWith ("auth_token")) 
				{
					this.auth_token = val;
				} 
				else if (arg.ToLower ().StartsWith ("user_name"))
				{
					this.user_name = val;
				}
				else if (arg.ToLower ().StartsWith ("password")) 
				{
					this.password = val;
				}
				else if (arg.ToLower ().StartsWith ("database_url"))
				{
					this.database_url = val;
				}
				else if (arg.ToLower ().StartsWith ("backup_file_path"))
				{
					this.backup_file_path = val;
				}
				else if (arg.ToLower ().StartsWith ("url"))
				{
					this.mmria_url = val;
				}
				else if(arg.ToLower().StartsWith("export_directory"))
				{
					export_directory = val;
				}
			}
		}

		if (string.IsNullOrWhiteSpace (this.database_url)) 
		{
			result.Status = "Validation Error";
			result.Detail = "missing database_url";
			return result;
		}

		if (string.IsNullOrWhiteSpace (this.user_name)) 
		{
			result.Status = "Validation Error";
			result.Detail = "missing user_name";
			return result;
		}

		if (string.IsNullOrWhiteSpace (this.password)) 
		{
			result.Status = "Validation Error";
			result.Detail = "missing password";
			return result;
		}


		try 
		{
	
id_list = await GetIdList();

		result.Doc_ID_Count = id_list.Count;


		var (SuccessCount, ErrorCount) = await GetDocumentList ();

			result.SuccessCount = SuccessCount;
			result.ErrorCount = ErrorCount;
			if(ErrorCount > 0)
			{
				result.Status = "Partial";
				result.Detail = $"{ErrorCount} document(s) failed while backing up {this.database_url}.";
			}
			else
			{
				result.Status = "Success";
				result.Detail = null;
			}
			return result;

		}
		catch (Exception ex) 
		{
			result.Status = $"Error";
			result.Detail = $"{ex}";

			return result;
		}




	}


	private async Task<HashSet<string>> GetIdList ()
	{

		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		string URL = string.Format("{0}/_all_docs", this.database_url);
		var curl_result = await _couchDbHttpClient.ExecuteAsync("GET", URL, null, this.user_name, this.password);
		var all_cases = System.Text.Json.JsonSerializer.Deserialize<mmria.common.model.couchdb.alldocs_response<System.Dynamic.ExpandoObject>> (curl_result);
		var all_cases_rows = all_cases?.rows;

		if(all_cases_rows == null)
		{
			throw new InvalidOperationException($"Unable to enumerate documents for {this.database_url}. Response: {GetResponsePreview(curl_result)}");
		}

		foreach (var row in all_cases_rows) 
		{
			result.Add(row.id);
		}
		return result;
	}

	


	private async Task<(int SuccessCount, int ErrorCount)> GetDocumentList ()
	{
		int SuccessCount = 0;
		int ErrorCount = 0;

		foreach(var id in id_list)
		{
			try
			{
				string URL = $"{this.database_url}/{id}";
				string curl_result = await _couchDbHttpClient.ExecuteAsync("GET", URL, null, this.user_name, this.password);

				dynamic case_row = System.Text.Json.JsonSerializer.Deserialize<System.Dynamic.ExpandoObject> (curl_result);

				IDictionary<string, object> case_doc = case_row as IDictionary<string, object>;
				if(case_doc == null)
				{
					throw new InvalidOperationException($"Unable to deserialize document {id} from {this.database_url}.");
				}
				case_doc.Remove("_rev");

				var case_json = System.Text.Json.JsonSerializer.Serialize(case_doc);

				var backup_file_path = this.backup_file_path;

				if(this.database_url.EndsWith("/metadata"))
				{
					var new_id = PathSanitizer.SanitizeDocumentId(id);
					var file_path = System.IO.Path.Combine(backup_file_path, new_id);
					System.IO.Directory.CreateDirectory($"{file_path}/_attachments");

					file_path = System.IO.Path.Combine(file_path, $"{new_id}.json");
					if (!System.IO.File.Exists (file_path)) 
					{
						System.IO.File.WriteAllText (file_path, case_json);
					}
				}
				else
				{

					var file_path = System.IO.Path.Combine(backup_file_path, $"{PathSanitizer.SanitizeDocumentId(id)}.json");
					if (!System.IO.File.Exists (file_path)) 
					{
						System.IO.File.WriteAllText(file_path, case_json);
					}
				}

				if(this.database_url.EndsWith("/metadata"))
				{
					if(case_doc.ContainsKey("_attachments"))
					{
						var attachment_set = case_doc["_attachments"] as IDictionary<string,object>;
						if(attachment_set != null)
						{
							var new_id = PathSanitizer.SanitizeDocumentId(id);
							var attachment_path = System.IO.Path.Combine(backup_file_path, new_id, "_attachments");
							

							foreach(var kvp in attachment_set)
							{
								var attachment_url = $"{URL}/{kvp.Key}";
						string attachment_doc_json = await _couchDbHttpClient.ExecuteAsync("GET", attachment_url, null, this.user_name, this.password);

                        var attachment_file_path = System.IO.Path.Combine(attachment_path, System.IO.Path.GetFileName(kvp.Key));
                        if (!System.IO.File.Exists (attachment_file_path))
                        {
                            System.IO.File.WriteAllText(attachment_file_path, attachment_doc_json);
						}
					}
				}
			}
		}

				SuccessCount+= 1;
			}
			catch(Exception)
			{
				ErrorCount += 1;
			}


			
		}

		return (SuccessCount, ErrorCount);
	}

	private static string GetResponsePreview(string responseText)
	{
		if(string.IsNullOrWhiteSpace(responseText))
		{
			return "(empty response)";
		}

		var normalized = responseText
			.Replace("\r", " ")
			.Replace("\n", " ")
			.Trim();

		if(normalized.Length <= 512)
		{
			return normalized;
		}

		return normalized.Substring(0, 512);
	}

}

