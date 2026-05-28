using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace mmria.services.vitalsimport.Utilities;
public static class CsvConverter
{
    public static string ToCSV<T>(T record) where T : class
    {
        var kvpList = new List<KeyValuePair<string, string>>();

        //Itterate through each property
        foreach (var prop in record.GetType().GetFields())
        {
            foreach (object attr in prop.GetCustomAttributes())
            {
                if (attr is MMRIA_PathAttribute)
                {
                    var mmria_pathAttribute = (MMRIA_PathAttribute)attr;

                    if (mmria_pathAttribute == null)
                    {
                    }
                    else
                    {
                        //Extract path from property and its associated value
                        kvpList.Add(new KeyValuePair<string, string>($"{mmria_pathAttribute.MMRIA_Path_Name}/{mmria_pathAttribute.MMRIA_Field_Name}", prop.GetValue(record)?.ToString()));
                    }
                }
            }
        }

        //Create a comma seperated list from the kvp
        var strings = kvpList.Select(kvp => $"{kvp.Key?.Replace(",", " ")},{kvp.Value}");

        //Return the new line seperated csv
        return string.Join(Environment.NewLine, strings);
    }
}

