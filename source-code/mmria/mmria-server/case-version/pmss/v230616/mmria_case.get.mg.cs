
using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.case_version.pmss.v230616;

public sealed partial class mmria_case
{


    public string? GetSG_String(string path, int form_index, int grid_index)
    {
        string? result = null;

        try
        {
            result = path.ToLower() switch
            {
    
                _ => null
            };
        }
        catch(Exception)
        {

        }


        
        return result;
    }

    public double? GetSG_Double(string path, int form_index, int grid_index)
    {
        double? result = null;
        try
        {
            result = path.ToLower() switch
            {
    
                _ => null
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }

    public bool? GetSG_Boolean(string path, int form_index, int grid_index)
    {
        bool? result = null;
        try
        {
            result = path.ToLower() switch
            {
    
                _ => null
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }

    public List<double>? GetSG_List_Of_Double(string path, int form_index, int grid_index)
    {
        List<double>? result = null;
        try
        {
            result = path.ToLower() switch
            {
    
                _ => null
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }

    
    public List<string>? GetSG_List_Of_String(string path, int form_index, int grid_index)
    {
        List<string>? result = null;
        try
        {
            result = path.ToLower() switch
            {
    
                _ => null
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }

    public DateTime? GetSG_Datetime(string path, int form_index, int grid_index)
    {
        DateTime? result = null;
        try
        {
            result = path.ToLower() switch
            {
    
                _ => null
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }


    public DateOnly? GetSG_Date_Only(string path, int form_index, int grid_index)
    {
        DateOnly? result = null;
        try
        {
            result = path.ToLower() switch
            {
    
                _ => null
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }


    public TimeOnly? GetSG_Time_Only(string path, int form_index, int grid_index)
    {
        TimeOnly? result = null;
        try
        {
            result = path.ToLower() switch
            {
    
                _ => null
            };
        }
        catch(Exception)
        {

        }

        
        return result;
    }


}