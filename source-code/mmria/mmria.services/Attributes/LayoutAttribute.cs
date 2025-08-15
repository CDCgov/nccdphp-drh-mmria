using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mmria.services.vitalsimport;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class LayoutAttribute : Attribute
{
    private int _index;
    private int _length;

    public int index
    {
        get { return _index; }
    }

    public int length
    {
        get { return _length; }
    }

    public LayoutAttribute(int index, int length)
    {
        this._index = index;
        this._length = length;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
internal sealed class MMRIA_PathAttribute : Attribute
{
    private string _MMRIA_Path_Name;
    private string _MMRIA_Field_Name;

    public string MMRIA_Path_Name
    {
        get { return _MMRIA_Path_Name; }
    }

    public string MMRIA_Field_Name
    {
        get { return _MMRIA_Field_Name; }
    }

    public MMRIA_PathAttribute(string mmria_path_name, string mmria_field_name)
    {
        this._MMRIA_Path_Name = mmria_path_name;
        this._MMRIA_Field_Name = mmria_field_name;
    }
}

[AttributeUsage(AttributeTargets.Field)]
internal sealed class IJE_Name : Attribute
{
    private string _name;

    public string Name
    {
        get { return _name; }
    }

    public IJE_Name(string name)
    {
        this._name = name;
    }
}

