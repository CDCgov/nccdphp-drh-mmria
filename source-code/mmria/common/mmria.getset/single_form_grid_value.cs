using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate;

public sealed partial class C_Get_Set_Value
{

    private System.Text.StringBuilder output_builder;

    public C_Get_Set_Value(System.Text.StringBuilder p_output_builder)
    {
        output_builder = p_output_builder;
    }

    public sealed class get_grid_value_result
    {
        public get_grid_value_result(bool p_is_error, List<(int, object)> p_result)
        {
            is_error = p_is_error;
            result = p_result;
        }

        public bool is_error { get; set; }
        public List<(int, object)> result { get; set; }

    }
    public get_grid_value_result get_grid_value(System.Dynamic.ExpandoObject p_object, string p_path)
    {
        var is_error = false;

        List<(int, object)> result = new List<(int, object)>();

        //dynamic current = null;

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            object index = null;

            for (int i = 0; i < path.Length; i++)
            {
                
                if(i == 0)
                {
                    IDictionary<string, object> index_dictionary = p_object as IDictionary<string, object>;
                    if
                    (
                        index_dictionary != null &&
                        index_dictionary.ContainsKey(path[0])
                    )
                    {
                        index = index_dictionary[path[0]];
                    }
                    else
                    {
                        return new get_grid_value_result(is_error, result);
                    }

                }
                else if (i == path.Length - 1)
                {
                    if (index is IList<object>)
                    {
                        var grid_list = index as IList<object>;

                        for(int grid_index = 0; grid_index < grid_list.Count; grid_index++)
                        {
                            var grid_row = grid_list[grid_index];

                            if(grid_row is IDictionary<string, object>)
                            {
                                var grid_row_dictionary = grid_row as IDictionary<string, object>;
                                if(grid_row_dictionary.ContainsKey(path[i]))
                                {
                                    result.Add((grid_index, grid_row_dictionary[path[i]]));
                                }
                                else
                                {
                                    result.Add((grid_index, null));
                                }
                            }
                        }		
                    }
                    else
                    {
                        var dictionary = p_object as IDictionary<string,object>;
                        var output = $"get_grid_value: Record does NOT contain Value for. {dictionary["_id"]} - {p_path}";
                        this.output_builder.AppendLine(output);
                        System.Console.WriteLine(output);
                    }

                }
                else if(index is IDictionary<string, object>)
                {
                    if(index != null && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        index = ((IDictionary<string, object>)index)[path[i]];
                    }
                
                }
                else if (index != null)
                {
                    System.Console.WriteLine(index.GetType());
                    /*
                    else if (index != null && index[path[i]].GetType() == typeof(IList<object>))
                    {
                        index = index[path[i]] as IList<object>;
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>) && !((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        //System.Console.WriteLine("Index not found. This should not happen. {0}", p_path);
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>))
                    {
                        index = index[path[i]] as IDictionary<string, object>;
                    }*/
                }
                else
                {
                    is_error = true;
                    //System.Console.WriteLine("This should not happen. {0}", p_path);
                }
            }
        }
        catch (Exception)
        {
            is_error = true;

            if
            (
                p_path != "er_visit_and_hospital_medical_records/onset_of_labor/is_artificial" &&
                p_path != "er_visit_and_hospital_medical_records/onset_of_labor/is_spontaneous"
            )
            {

                var dictionary = p_object as IDictionary<string,object>;
                var output = $"get_grid_value: Record does NOT contain Value for. {dictionary["_id"]} - {p_path}";
                this.output_builder.AppendLine(output);
                System.Console.WriteLine(output);
            }
            //System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }

        return  new get_grid_value_result(is_error, result);

    }


    public bool set_grid_value(System.Dynamic.ExpandoObject p_object, string p_path, List<(int, object)> p_value_list)
    {
        var result = false;

        //List<(int, object)> result = new List<(int, object)>();

        try
        {
            string[] path = p_path.Split('/');

            System.Text.RegularExpressions.Regex number_regex = new System.Text.RegularExpressions.Regex(@"^\d+$");

            //IDictionary<string, object> index = p_object;
            object index = null;

            for (int i = 0; i < path.Length; i++)
            {
                
                if(i == 0)
                {
                    IDictionary<string, object> index_dictionary = p_object as IDictionary<string, object>;
                    if
                    (
                        index_dictionary != null &&
                        index_dictionary.ContainsKey(path[0])
                    )
                    {
                        index = index_dictionary[path[0]];
                    }
                    else
                    {
                        return result;
                    }

                }
                else if (i == path.Length - 1)
                {
                    if (index is IList<object>)
                    {
                        var grid_list = index as IList<object>;
                        result = true;
                        foreach(var tuple in p_value_list)
                        //for(int grid_index = 0; grid_index < grid_list.Count; grid_index++)
                        {

                            int grid_index = tuple.Item1;
                            if(grid_list.Count > grid_index)
                            {
                                var grid_row = grid_list[grid_index];

                                if(grid_row is IDictionary<string, object>)
                                {
                                    var grid_row_dictionary = grid_row as IDictionary<string, object>;
                                    if(grid_row_dictionary.ContainsKey(path[i]))
                                    {
                                        grid_row_dictionary[path[i]] = tuple.Item2;
                                    }
                                    else
                                    {
                                        grid_row_dictionary.Add(path[i].ToLower(), tuple.Item2);
                                    }
                                    
                                    result = result && true;
                                    
                                }
                                else
                                {
                                    result = false;
                                }
                            }
                            else
                            {
                                result = false;
                            }
                        }		
                    }
                    else
                    {
                        var dictionary = p_object as IDictionary<string,object>;
                        System.Console.WriteLine($"set_grid_value: This should not happen. {dictionary["_id"]} {p_path}");
                    }

                }
                else if(index is IDictionary<string, object>)
                {
                    if(index != null && ((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        index = ((IDictionary<string, object>)index)[path[i]];
                    }
                
                }
                else if (index != null)
                {
                    System.Console.WriteLine(index.GetType());
                    /*
                    else if (index != null && index[path[i]].GetType() == typeof(IList<object>))
                    {
                        index = index[path[i]] as IList<object>;
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>) && !((IDictionary<string, object>)index).ContainsKey(path[i]))
                    {
                        //System.Console.WriteLine("Index not found. This should not happen. {0}", p_path);
                    }
                    else if (index != null && index[path[i]].GetType() == typeof(IDictionary<string, object>))
                    {
                        index = index[path[i]] as IDictionary<string, object>;
                    }
                    */
                }
                else
                {
                    //System.Console.WriteLine("This should not happen. {0}", p_path);
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("c_convert_to_report_object.get_value bad mapping {0}\n {1}", p_path, ex);
        }

        return result;

    }

}

