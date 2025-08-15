
using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.case_version.pmss.v230616;

public sealed partial class mmria_case
{


    public string? GetSG_String(string path, int index)
    {
        string? result = null;

        try
        {
            result = path.ToLower() switch
            {
             "data_migration_history/version" => data_migration_history[index].version,
         "data_migration_history/datetime" => data_migration_history[index].datetime,
         "data_migration_history/is_forced_write" => data_migration_history[index].is_forced_write,

                _ => null
            };
        }
        catch(Exception)
        {

        }


        
        return result;
    }

    public double? GetSG_Double(string path, int index)
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

    public bool? GetSG_Boolean(string path, int index)
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

    public List<double>? GetSG_List_Of_Double(string path, int index)
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

    
    public List<string>? GetSG_List_Of_String(string path, int index)
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

    public DateTime? GetSG_Datetime(string path, int index)
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


    public DateOnly? GetSG_Date_Only(string path, int index)
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


    public TimeOnly? GetSG_Time_Only(string path, int index)
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