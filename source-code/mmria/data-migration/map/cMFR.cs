using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace migrate.map;

public sealed class cMapFilter 
{

    private List<System.Dynamic.ExpandoObject> list;
    private mmria.common.metadata.app metadata;


    public cMapFilter(List<System.Dynamic.ExpandoObject> p_list, mmria.common.metadata.app p_metadata)
    {
        list = p_list;
        metadata = p_metadata;
    }



    public delegate System.Dynamic.ExpandoObject MapDelegate(System.Dynamic.ExpandoObject that);
    public delegate bool FilterDelegate(System.Dynamic.ExpandoObject that);

    public cMapFilter map(List<System.Dynamic.ExpandoObject> p_List, MapDelegate p_function)
    {
        var result = new List<System.Dynamic.ExpandoObject>();

        foreach(var item in p_List)
        {
            var new_item = p_function(item);
            result.Add(new_item);
        }
        return new cMapFilter(result, metadata);
    }

    public cMapFilter filter(List<System.Dynamic.ExpandoObject> p_List, FilterDelegate p_function)
    {
        var result = new List<System.Dynamic.ExpandoObject>();

        foreach(var item in p_List)
        {
            if(p_function(item))
            {
                result.Add(item);
            }
            
        }

        return new cMapFilter(result, metadata);
    }
}

