
using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.case_version.pmss.v230616;

public sealed partial class mmria_case
{


    public bool SetSG_String(string path, int index, string value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
                case "data_migration_history/version":
                    data_migration_history[index].version = value;
                    result = true;
            break;
            case "data_migration_history/datetime":
                    data_migration_history[index].datetime = value;
                    result = true;
            break;
            case "data_migration_history/is_forced_write":
                    data_migration_history[index].is_forced_write = value;
                    result = true;
            break;

                default:
                break;
            };
        }
        catch(Exception)
        {

        }


        
        return result;
    }

    public bool SetSG_Double(string path, int index, double? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }

    public bool SetSG_Boolean(string path, int index, bool? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }

    public bool SetSG_List_Of_Double(string path, int index, List<double> value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }

    
    public bool SetSG_List_Of_String(string path, int index, List<string> value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }

    public bool SetSG_Datetime(string path, int index, DateTime? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }


    public bool SetSG_Date_Only(string path, int index, DateOnly? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }


    public bool SetSG_Time_Only(string path, int index, TimeOnly? value)
    {
        bool result = false;
        try
        {
            switch(path.ToLower())
            {
    
                default:
                break;
            };
        }
        catch(Exception)
        {

        }
        

        
        return result;
    }


}