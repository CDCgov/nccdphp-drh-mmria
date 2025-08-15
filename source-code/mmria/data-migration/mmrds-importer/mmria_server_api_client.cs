using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace mmria.mmrds
{
	public class mmria_server_api_client
	{
		private string database_path;
		private string database_url;
		private string mmria_url;
		private string auth_token;
		private List<string> roles;
		private string user_name;
		private string password;


		public mmria_server_api_client(string p_mmria_url)
		{
			this.mmria_url = p_mmria_url;
			this.roles = new List<string>();
		}


		public string Database_Path
		{
			get { return this.database_path; }
			set { this.database_path = value; }
		}


		public string Database_Url
		{
			get { return this.database_url; }
			set { this.database_url = value; }
		}

		public mmria_server_api_client(string p_database_path,  string p_mmria_url)
		{
			this.database_path = p_database_path;
			this.mmria_url =p_mmria_url;
		}

		public mmria.common.metadata.app get_metadata()
		{
			mmria.common.metadata.app result = null;//"

			string metadata_url = $"{this.mmria_url}/metadata/version_specification-19.10.18/metadata";
			
			string URL =  $"{this.mmria_url}/metadata/version_specification-19.10.18/metadata";;
			//string urlParameters = "?api_key=123";
			string urlParameters = "";

			var curl = new migrate.cURL ("GET", null, URL, null, null);

			try
			{
				string json_result = curl.execute ();
				result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.app> (json_result);

			}
			catch(Exception ex)
			{
				Console.WriteLine("{0}", ex);
			}

			return result;
		}

		public mmria.common.model.couchdb.session_response get_session()
		{
			mmria.common.model.couchdb.session_response result = null;

			string URL = this.mmria_url + "/api/sessionDB";
			//string urlParameters = "?api_key=123";
			string urlParameters = "";

			HttpClient client = new HttpClient();
			client.BaseAddress = new Uri(URL);


			var curl = new migrate.cURL ("GET", null, URL, null, null);

			try 
			{
				string json_result = curl.execute ();
				result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.session_response> (json_result);

			}
			catch (Exception ex) 
			{
				Console.WriteLine ("{0}", ex);
			}


			return result;
		}

		public IEnumerable<mmria.common.model.couchdb.login_response> login(string p_user_id, string p_password)
		{
			this.user_name = p_user_id;
			this.password = p_password;


			string login_url = this.mmria_url + "/api/sessionDB/";
			var login_data = new { userid = this.user_name, password = this.password };
			var login_curl = new migrate.cURL("POST", null, login_url, Newtonsoft.Json.JsonConvert.SerializeObject(login_data));
			string login_result_string = null;
			dynamic json_response = null;
			try
			{
				login_result_string = login_curl.execute();
				System.Console.WriteLine(login_result_string);
				json_response = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<mmria.common.model.couchdb.login_response>>(login_result_string);

			}
			catch (Exception ex)
			{
				System.Console.WriteLine(ex);
			}


			if (json_response[0].ok == true)
			{
				if (json_response[0].name != null)
				{
					this.roles.AddRange(json_response[0].roles as IList<string>);
					this.auth_token = json_response[0].auth_session;
				}
			}


			return null;

		}

		public mmria.common.model.couchdb.document_put_response set_case(string case_json)
		{
			mmria.common.model.couchdb.document_put_response result = null;

			string URL = this.mmria_url + "/api/case";
			dynamic json_response = null;

			var curl = new migrate.cURL ("GET", null, URL, null, null);

			var byteArray = System.Text.Encoding.ASCII.GetBytes (string.Format ("{0}:{1}", this.user_name, this.password));
			curl.AddHeader ("Basic", Convert.ToBase64String (byteArray));

			//curl.AddHeader ("AuthSession", this.auth_token);

			try 
			{
				string json_result = curl.execute ();
				json_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response> (json_result);

			}
			catch (Exception ex) 
			{
				Console.WriteLine ("{0}", ex);
			}




			if (json_response.ok == true)
			{
				if (json_response.auth_session != null && !string.IsNullOrWhiteSpace(json_response.auth_session))
				{
					this.auth_token = json_response.auth_session;
				}
			}
			else
			{

			}

			return json_response;
		}

		public mmria.common.model.couchdb.document_put_response set_case2(string case_json)
		{
			mmria.common.model.couchdb.document_put_response result = null;

			string URL = this.mmria_url + "/api/metadata";
			//string urlParameters = "?api_key=123";
			string urlParameters = "";

			HttpClient client = new HttpClient();
			client.BaseAddress = new Uri(URL);

			// Add an Accept header for JSON format.
			client.DefaultRequestHeaders.Accept.Add(
			new MediaTypeWithQualityHeaderValue("application/json"));

			// List data response.
			HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call!
			if (response.IsSuccessStatusCode)
			{
				// Parse the response body. Blocking!
				//result = response.Content.ReadAsAsync<mmria.common.model.couchdb.document_put_response>().Result;
			}
			else
			{
				Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
			}

			return result;
		}

		public object get_all_cases(string p_database_url)
		{
			bool is_offline_mode = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["is_offline_mode"]);

			if (is_offline_mode)
			{
				return get_all_cases();
			}

			this.database_url = p_database_url;
			/*
			var credential = new System.Net.NetworkCredential
			{
				UserName = "user1",
				Password = "password"
			};

			var httpClientHandler = new System.Net.Http.HttpClientHandler
			{
				Credentials = credential,
				PreAuthenticate = false,
				Proxy = new WebProxy("http://127.0.0.1:8888"),
                UseProxy = true,
			};

			HttpClient client = new HttpClient(httpClientHandler);
			*/

			System.Dynamic.ExpandoObject result = null;
			string URL = this.database_url + "/mmrds/_all_docs";
			string urlParameters = "?include_docs=true";



			var curl = new migrate.cURL ("GET", null, URL, null, null);

			var byteArray = System.Text.Encoding.ASCII.GetBytes (string.Format ("{0}:{1}", this.user_name, this.password));
			curl.AddHeader ("Basic", Convert.ToBase64String (byteArray));
			//curl.AddHeader ("AuthSession", this.auth_token);

			try
			{
				string json_result = curl.execute ();
				result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (json_result);

			}
			catch (Exception ex) 
			{
				Console.WriteLine ("{0}", ex);
			}
			/*

			HttpClient client = new HttpClient();
			client.BaseAddress = new Uri(URL);

			var byteArray = System.Text.Encoding.ASCII.GetBytes(string.Format("{0}:{1}", this.user_name, this.password));
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));


			// Add an Accept header for JSON format.
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			// List data response.
			HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call!
			if (response.IsSuccessStatusCode)
			{
				// Parse the response body. Blocking!
				result = response.Content.ReadAsAsync<System.Dynamic.ExpandoObject>().Result;
			}
			else
			{
				Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
			}*/

			return result;
		}

		public object get_all_cases()
		{
			dynamic result = new List<System.Dynamic.ExpandoObject>();

			string import_directory = System.Configuration.ConfigurationManager.AppSettings["import_directory"];

			foreach (string file_name in System.IO.Directory.GetFiles(import_directory, "*.json"))
			{
				/*
				if (file_name != "d0e08da8-d306-4a9a-a5ff-9f1d54702091.json")
				{
					continue;
				}*/
			

				try
				{
					string json_string = System.IO.File.OpenText(file_name).ReadToEnd();
					System.Dynamic.ExpandoObject case_data = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(json_string);
					result.Add(case_data);
				}
				catch (Exception ex)
				{
					// do nothing
				}

			}

			return result;
		}

	private string get_revision (string p_document_url)
	{

		string result = null;

			var document_curl = new migrate.cURL ("GET", null, p_document_url, null, this.user_name, this.password);
		string document_json = null;

		try 
		{

			document_json = document_curl.execute ();
			var request_result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (document_json);
			IDictionary<string, object> updater = request_result as IDictionary<string, object>;
			result = updater ["_rev"].ToString ();
		} 
		catch (Exception ex) 
		{
			if (!(ex.Message.IndexOf ("(404) Object Not Found") > -1)) 
			{
				//System.Console.WriteLine ("c_sync_document.get_revision");
				//System.Console.WriteLine (ex);
			}
		}

			return result;
	}

	}
}
