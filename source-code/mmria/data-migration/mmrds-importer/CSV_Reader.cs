using System;
using System.IO;
using System.Collections.Generic;

namespace mmria.mmrds.util
{

	public class csv_Data
	{
		
		public System.Data.DataTable get_datatable (string p_file_path)
		{
			//https://github.com/Spintronic/CsvReader

			Dictionary<string, int> column_name_to_column_index_map = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
			// open a cCommaSeparatedReader
			//LumenWorks.Framework.IO.Csv.CachedCsvReader csv = new LumenWorks.Framework.IO.Csv.CachedCsvReader (new System.IO.StreamReader (@"mapping-file-set/MMRDS-Mapping-NO-GRIDS-lookup-values.csv"), true);

			LumenWorks.Framework.IO.Csv.CachedCsvReader  csv_reader = new LumenWorks.Framework.IO.Csv.CachedCsvReader (new System.IO.StreamReader(p_file_path), true);

			// csv_reader 

			System.Data.DataTable result = new System.Data.DataTable ();
			// begin create columns
			// for each field in first csv_row of the csv_reader
			string [] header_array = csv_reader.GetFieldHeaders ();
			for (var i = 0; i < header_array.Length; i++)
			{
				string column_name = header_array [i];
				column_name_to_column_index_map.Add (column_name, i);
				// add columns to the data_table coloumn based on the csv columns

				System.Data.DataColumn data_column = new System.Data.DataColumn (column_name, typeof(string));
				result.Columns.Add (data_column);

			}
			// end create columns

			while (csv_reader.ReadNextRecord ()) 
			{

				System.Data.DataRow datatable_row = result.NewRow ();


				foreach ( KeyValuePair<string, int> kvp in column_name_to_column_index_map)
				{
					string column_name = kvp.Key; 
					int column_index = kvp.Value;
					if (csv_reader [column_index] != null) 
					{
						datatable_row [column_name] = csv_reader [column_index];
					}
				}


				result.Rows.Add (datatable_row);


			}
			return result;
		}


	}

	public class csv_Row : List<string>
	{
		public string LineText { get; set; }
	}
		

	/// <summary>
	/// Class to read data from a CSV file
	/// </summary>
	public class CSV_Reader : StreamReader
	{
		public CSV_Reader(Stream stream)
			: base(stream)
		{
		}

		public CSV_Reader(string filename)
			: base(filename)
		{
		}

		/// <summary>
		/// Reads a row of data from a CSV file
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		public bool ReadRow(csv_Row row)
		{
			row.LineText = ReadLine();
			if (String.IsNullOrEmpty(row.LineText))
				return false;

			if (row.LineText.IndexOf ("home_record/case_progress_report/birth_certificate_parent_section") > -1) 
			{
				System.Console.Write ("Break");
			}

			int pos = 0;
			int rows = 0;

			while (pos < row.LineText.Length)
			{
				string value;

				// Special handling for quoted field
				if (row.LineText[pos] == '"')
				{
					// Skip initial quote
					pos++;

					// Parse quoted value
					int start = pos;
					while (pos < row.LineText.Length)
					{
						// Test for quote character
						if (row.LineText[pos] == '"')
						{
							// Found one
							pos++;

							// If two quotes together, keep one
							// Otherwise, indicates end of value
							if (pos >= row.LineText.Length || row.LineText[pos] != '"')
							{
								pos--;
								break;
							}
						}
						pos++;
					}
					value = row.LineText.Substring(start, pos - start);
					value = value.Replace("\"\"", "\"");
				}
				else
				{
					// Parse unquoted value
					int start = pos;
					while (pos < row.LineText.Length && row.LineText[pos] != ',')
						pos++;
					value = row.LineText.Substring(start, pos - start);
				}

				// Add field to list
				if (rows < row.Count)
					row[rows] = value;
				else
					row.Add(value);
				rows++;

				// Eat up to and including next comma
				while (pos < row.LineText.Length && row.LineText[pos] != ',')
					pos++;
				if (pos < row.LineText.Length)
					pos++;
			}
			// Delete any unused items
			//while (row.Count > rows)
			//	row.RemoveAt(rows);

			// Return true if any columns read
			return (row.Count > 0);
		}
		
	}

}

