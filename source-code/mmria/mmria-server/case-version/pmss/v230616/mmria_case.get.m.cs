
using System;
using System.Collections.Generic;
using System.Linq;

namespace mmria.case_version.pmss.v230616;

public sealed partial class mmria_case
{


    public string? GetM_String(string path, int index)
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

    public double? GetM_Double(string path, int index)
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

    public bool? GetM_Boolean(string path, int index)
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

    public List<double>? GetM_List_Of_Double(string path, int index)
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

    
    public List<string>? GetM_List_Of_String(string path, int index)
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

    public DateTime? GetM_Datetime(string path, int index)
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


    public DateOnly? GetM_Date_Only(string path, int index)
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


    public TimeOnly? GetM_Time_Only(string path, int index)
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