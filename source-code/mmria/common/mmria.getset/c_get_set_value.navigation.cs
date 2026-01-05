using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate;

public sealed partial class C_Get_Set_Value
{
/*
    public System.Type get_item_type(object p_object)
    {
        System.Type result = null;

        try
        {
            if(p_object != null)
            {
                switch(p_object.GetType())
                {
                    case typeof(IDictionary<string, object>):
                        result = IDictionary<string, object>;
                        break;
                    case IList<object>:
                        result = IList<object>;
                        break;
                    case IList<IDictionary<string, object>>:
                        result = IList<IDictionary<string, object>>;
                        break;
                    case typeof(string):
                        result = typeof(string);
                        break;
                    case typeof(string):
                        result = typeof(string);
                        break;
                }
            }

        }
        catch(System.Exception ex)
        {
            // do nothing
        }

        return result;
    }

*/

    public IDictionary<string, object> get_form(IDictionary<string, object> p_object, string p_name)
    {
        IDictionary<string, object> result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                result = p_object[p_name] as IDictionary<string, object>;
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }

    public IDictionary<string, object> get_group(IDictionary<string, object> p_object, string p_name)
    {
        IDictionary<string, object> result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                result = p_object[p_name] as IDictionary<string, object>;
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }

    public IList<IDictionary<string, object>> get_multi_form(IDictionary<string, object> p_object, string p_name)
    {
        IList<IDictionary<string, object>> result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                result = p_object[p_name] as IList<IDictionary<string, object>>;
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }

    public IList<object> get_grid(IDictionary<string, object> p_object, string p_name)
    {
        IList<object> result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                result = p_object[p_name] as IList<object>;
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }

    public string get_string(IDictionary<string, object> p_object, string p_name)
    {
        string result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                result = p_object[p_name] as string;
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }


    public double? get_number(IDictionary<string, object> p_object, string p_name)
    {
        double? result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                if(p_object[p_name] is string)
                {
                    if(double.TryParse(p_object[p_name].ToString(), out var test_double))
                    {
                        result = test_double;
                    }
                }
                else
                {
                    result = p_object[p_name] as double?;
                }
                

                
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }

    public string get_date(IDictionary<string, object> p_object, string p_name)
    {
        string result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                result = p_object[p_name] as string;
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }

    public TimeSpan? get_time(IDictionary<string, object> p_object, string p_name)
    {
        TimeSpan? result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                result = p_object[p_name] as TimeSpan?;
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }

    public DateTime? get_datetime(IDictionary<string, object> p_object, string p_name)
    {
        DateTime? result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                result = p_object[p_name] as DateTime?;
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }

    public string get_string_list(IDictionary<string, object> p_object, string p_name)
    {
        string result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                result = p_object[p_name] as string;
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }

    public double? get_number_list(IDictionary<string, object> p_object, string p_name)
    {
        double? result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                result = p_object[p_name] as double?;
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }


    public IList<string> get_multi_string_list(IDictionary<string, object> p_object, string p_name)
    {
        IList<string> result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                result = p_object[p_name] as IList<string>;
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }
    public IList<double?> get_multi_number_list(IDictionary<string, object> p_object, string p_name)
    {
        IList<double?> result = null;

        try
        {
            if(p_object.ContainsKey(p_name))
            {
                result = p_object[p_name] as IList<double?>;
            }

        }
        catch(System.Exception)
        {
            // do nothing
        }

        return result;
    }

}

