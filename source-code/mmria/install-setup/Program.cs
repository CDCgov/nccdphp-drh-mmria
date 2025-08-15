using System;
using System.IO;
using Microsoft.Extensions.Configuration;


namespace install_setup
{
    class Program
    {

		static string major_version = "18.05.30";
		static string minor_version = "26fda10";
		static string current_version;

        static string build_directory_path;
		static string input_directory_path;
		static string output_directory_path;
		static string mmria_server_binary_directory_path;
		static string mmria_console_binary_directory_path;
		static string mmria_server_html_directory_path;

        static IConfiguration configuration = null;

        static void Main(string[] args)
        {



            configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(),"appsettings.json"), true, true)
            .Build();


			if (args.Length > 0) 
			{
				for (var i = 1; i < args.Length; i++) 
				{
					string arg = args [i];
					int index = arg.IndexOf (':');
					string val = arg.Substring (index + 1, arg.Length - (index + 1)).Trim (new char [] { '\"' });

					if (arg.ToLower ().StartsWith ("major_version")) 
					{
						major_version = val;
					}
					else if (arg.ToLower ().StartsWith ("minor_version")) 
					{
						minor_version = val;
					}
					else if (arg.ToLower ().StartsWith ("build_directory_path")) 
					{
						build_directory_path = val;
					}
					else if (arg.ToLower ().StartsWith ("input_directory_path")) 
					{
						input_directory_path = val;
					}
					else if (arg.ToLower ().StartsWith ("output_directory_path")) 
					{
						output_directory_path = val;
					}
					else if (arg.ToLower ().StartsWith ("mmria_server_binary_directory_path")) 
					{
						mmria_server_binary_directory_path = val;
					}
					else if (arg.ToLower ().StartsWith ("mmria_console_binary_directory_path")) 
					{
						mmria_console_binary_directory_path = val;
					}
					else if (arg.ToLower ().StartsWith ("mmria_server_html_directory_path")) 
					{
						mmria_server_html_directory_path = val;
					}
				}
			}

			if(string.IsNullOrWhiteSpace (build_directory_path)) build_directory_path = configuration["mmria_settings:build_directory_path"];
			if(string.IsNullOrWhiteSpace (input_directory_path)) input_directory_path = configuration["mmria_settings:input_directory_path"];
			if(string.IsNullOrWhiteSpace (output_directory_path)) output_directory_path = configuration["mmria_settings:output_directory_path"];
			if(string.IsNullOrWhiteSpace (mmria_server_binary_directory_path)) mmria_server_binary_directory_path = configuration["mmria_settings:mmria_server_binary_directory_path"];
			if(string.IsNullOrWhiteSpace (mmria_console_binary_directory_path)) mmria_console_binary_directory_path = configuration["mmria_settings:mmria_console_binary_directory_path"];
			if(string.IsNullOrWhiteSpace (mmria_server_html_directory_path)) mmria_server_html_directory_path = configuration["mmria_settings:mmria_server_html_directory_path"];


			current_version = $"{major_version} v({minor_version})";

			bool win7Only = false;

			string[] publish_version_set = new string[]
			{
				//"win10-x64"//, 
				"ubuntu.16.10-x64",
				//"win7-x86"
			};
			
			string root_dir = Directory.GetCurrentDirectory().Replace("install-setup","");
			var NoErrors = new System.Text.RegularExpressions.Regex("0 Error\\(s\\)");
			string output = null;
			string mmria_server_project_file = Path.Combine(root_dir,"mmria-server","mmria-server.csproj");
			string mmria_console_project_file = Path.Combine(root_dir,"mmria-console","mmria-console.csproj");
			string mmria_service_project_file = Path.Combine(root_dir,"mmria-server","mmria-service.csproj");


			InitZipDirectory();

			if(win7Only)
			{
				string mmria_server_publish_directory = Path.Combine(root_dir,"mmria-server","bin","Release","net471", "win7-x64","publish");
				if(Directory.Exists(mmria_server_publish_directory))
				{
					RecursiveDirectoryDelete(new DirectoryInfo(mmria_server_publish_directory));
				}

				output = RunShell("dotnet", $"build {mmria_service_project_file} /p:Configuration=Release /t:Clean ");
				if(NoErrors.IsMatch(output))
				{
					output = RunShell("dotnet", $"publish {mmria_service_project_file} --framework net471 -c Release -r win7-x64 -v d");
					
					if(NoErrors.IsMatch(output))
					{
						Console.WriteLine($"go server win7-x64");

						ProcessServerPublish("win7-x64");

						//ubuntu.16.10-x64
					}
					else
					{
						Console.WriteLine($"no go server win7-x64");
						Console.WriteLine(output);
					}
				}
			}
			else
			{

				output = RunShell("dotnet", $"build {mmria_server_project_file} /p:Configuration=Release /t:Clean ");
				if(NoErrors.IsMatch(output))
				foreach(string publish_version in publish_version_set)
				{
					output = RunShell("dotnet", $"publish {mmria_server_project_file} --framework netcoreapp2.1 -c Release -r {publish_version} -v d");
					
					if(NoErrors.IsMatch(output))
					{
						Console.WriteLine($"go server {publish_version}");
						ProcessServerPublish(publish_version);

						//ubuntu.16.10-x64
					}
					else
					{
						Console.WriteLine($"no go {publish_version}");
						Console.WriteLine(output);
					}
				}

			}

			output = RunShell("dotnet", $"build {mmria_console_project_file} /p:Configuration=Release /t:Clean ");
			if(NoErrors.IsMatch(output))
			foreach(string publish_version in publish_version_set)
			{
				output = RunShell("dotnet", $"publish {mmria_console_project_file} --framework netcoreapp2.1 -c Release -r {publish_version} -v d");
				if(NoErrors.IsMatch(output))
				{
					Console.WriteLine($"go console {publish_version}");
					ProcessConsolePublish(publish_version);
				}
				else
				{
					Console.WriteLine($"no go {publish_version}");
					Console.WriteLine(output);
				}
			}

			//CopyFolder.CopyDirectory(input_directory_path, output_directory_path);
        }


		static void InitZipDirectory()
		{
			string root_dir = Directory.GetCurrentDirectory().Replace("install-setup","");
			string zip_directory = Path.Combine(root_dir,"install-setup","bin");
			foreach(string file_name in Directory.GetFiles(zip_directory))
			{
				if(file_name.EndsWith(".zip"))
				{
					File.Delete(file_name);
				}
			}
		}

		static void ProcessServerPublish(string p_publish_version)
		{
			string root_dir = Directory.GetCurrentDirectory().Replace("install-setup","");
							// version number -- Start
			System.Text.RegularExpressions.Regex version_tag = new System.Text.RegularExpressions.Regex ("<\\%=version\\%>");
			string mmria_server_publish_directory = Path.Combine(root_dir,"mmria-server","bin","Release","netcoreapp2.1", p_publish_version,"publish");
			if(p_publish_version=="win7-x64")
			{
				mmria_server_publish_directory = Path.Combine(root_dir,"mmria-server","bin","Release","net471", p_publish_version,"publish");
			}
			
			string profile_text = System.IO.File.ReadAllText (mmria_server_publish_directory + "/wwwroot/scripts/profile.js");
			System.IO.File.WriteAllText(mmria_server_publish_directory + "/wwwroot/scripts/profile.js", version_tag.Replace(profile_text, current_version));

			string index_text = System.IO.File.ReadAllText (mmria_server_publish_directory + "/wwwroot/index.html");
			System.IO.File.WriteAllText(mmria_server_publish_directory + "/wwwroot/index.html", version_tag.Replace(index_text, current_version));
			
			string instruction_text = System.IO.File.ReadAllText(Path.Combine(root_dir,"install-setup","Install-Instructions.txt"));
			System.IO.File.WriteAllText(mmria_server_publish_directory + "/Install-Instructions.txt", version_tag.Replace(instruction_text, current_version));

			// version number -- End

			// move database scripts -- start
			string mmria_server_database_scripts_directory = Path.Combine(root_dir,"mmria-server","database-scripts");

			System.IO.File.Copy(Path.Combine(mmria_server_database_scripts_directory, "metadata_design_auth.json"), Path.Combine(mmria_server_publish_directory, "database-scripts", "metadata_design_auth.json"), true);
			System.IO.File.Copy (Path.Combine (mmria_server_database_scripts_directory, "metadata.json"), Path.Combine(mmria_server_publish_directory, "database-scripts", "metadata.json"), true);
			System.IO.File.Copy (Path.Combine (mmria_server_database_scripts_directory, "MMRIA_calculations.js"), Path.Combine(mmria_server_publish_directory, "database-scripts", "MMRIA_calculations.js"), true);
			System.IO.File.Copy (Path.Combine (mmria_server_database_scripts_directory, "mmria-check-code.js"), Path.Combine(mmria_server_publish_directory, "database-scripts", "mmria-check-code.js"), true);

			System.IO.File.Copy (Path.Combine (mmria_server_database_scripts_directory, "case_design_sortable.json"), Path.Combine(mmria_server_publish_directory, "database-scripts", "case_design_sortable.json"), true);
			System.IO.File.Copy (Path.Combine (mmria_server_database_scripts_directory, "case_store_design_auth.json"), Path.Combine(mmria_server_publish_directory, "database-scripts", "case_store_design_auth.json"), true);

			//System.IO.File.Copy (Path.Combine (mmria_server_database_scripts_directory, "case_store_design_auth.json"), Path.Combine(mmria_server_publish_directory, "database-scripts", "case_store_design_auth.json"), true);



			// move database scripts -- end

			// remove unneeded files -- start
			if (System.IO.Directory.Exists (mmria_server_publish_directory + "/wwwroot/metadata")) 
			{
				System.IO.Directory.Delete(mmria_server_publish_directory + "/wwwroot/metadata", true);	
			}

			if (File.Exists (mmria_server_publish_directory + "/wwwroot/grid-test-1.html"))
			{
				File.Delete (mmria_server_publish_directory + "/wwwroot/grid-test-1.html");
			}

			if (File.Exists (mmria_server_publish_directory + "/wwwroot/grid-test-2.html"))
			{
				File.Delete (mmria_server_publish_directory + "/wwwroot/grid-test-2.html");
			}

			if (File.Exists (mmria_server_publish_directory + "/wwwroot/grid-test-3.html"))
			{
				File.Delete (mmria_server_publish_directory + "/wwwroot/grid-test-3.html");
			}

			if (File.Exists (mmria_server_publish_directory + "/wwwroot/socket-test.html"))
			{
				File.Delete (mmria_server_publish_directory + "/wwwroot/socket-test.html");
			}
							
			if (File.Exists (mmria_server_publish_directory + "/wwwroot/socket-test2.html"))
			{
				File.Delete (mmria_server_publish_directory + "/wwwroot/socket-test2.html");
			}

			if (File.Exists (mmria_server_publish_directory + "/appsettings.Development.json"))
			{
				File.Delete (mmria_server_publish_directory + "/appsettings.Development.json");
			}

			if (File.Exists (mmria_server_publish_directory + "/appsettings.json"))
			{
				File.Delete (mmria_server_publish_directory + "/appsettings.json");
			}

			// remove uneeded files -- end

			File.Copy(Path.Combine(root_dir,"install-setup","mmria-server.appsettings.json"), mmria_server_publish_directory + "/appsettings.json");


			string mmria_server_zip_file_name = Path.Combine(root_dir,"install-setup","bin", $"MMRIA-server-{p_publish_version}-{current_version}.zip");
			cFolderCompressor folder_compressor = new cFolderCompressor ();

			if(File.Exists(mmria_server_zip_file_name))
			{
				File.Delete(mmria_server_zip_file_name);
			}

			folder_compressor.Compress
			(
				System.IO.Path.Combine (mmria_server_zip_file_name),
				null,// string password 
				System.IO.Path.Combine (mmria_server_publish_directory)
			);

		}

		static void ProcessConsolePublish(string p_publish_version)
		{
			string root_dir = Directory.GetCurrentDirectory().Replace("install-setup","");

			string mmria_console_zip_file_name = Path.Combine(root_dir,"install-setup","bin", $"MMRIA-console-{p_publish_version}-{current_version}.zip");
			string mmria_console_publish_folder = Path.Combine(root_dir,"mmria-console","bin","Release","netcoreapp2.1",p_publish_version,"publish");
			cFolderCompressor folder_compressor = new cFolderCompressor ();

			if(File.Exists(mmria_console_zip_file_name))
			{
				File.Delete(mmria_console_zip_file_name);
			}

			folder_compressor.Compress
			(
				System.IO.Path.Combine (mmria_console_zip_file_name),
				null,// string password 
				System.IO.Path.Combine (mmria_console_publish_folder)
			);
		}

		static string RunShell(string p_file_path, string p_arguments = null)
		{
			System.Text.StringBuilder result = new System.Text.StringBuilder();
			
			System.Diagnostics.Process proc = new System.Diagnostics.Process 
			{
				StartInfo = new System.Diagnostics.ProcessStartInfo 
				{
					FileName = p_file_path,
					Arguments = p_arguments,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};


			proc.Start();
			while (!proc.StandardOutput.EndOfStream) 
			{
				result.AppendLine(proc.StandardOutput.ReadLine());
			}

			return result.ToString();
		}

		static void RecursiveDirectoryDelete(System.IO.DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
                return;

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                RecursiveDirectoryDelete(dir);
            }
            baseDir.Delete(true);
        }
    }
}
