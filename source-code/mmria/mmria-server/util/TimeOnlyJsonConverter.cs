using System;
using Akka.Util;
using mmria.common.metadata;

namespace mmria.server.utils;
public class TimeOnlyJsonConverter : Newtonsoft.Json.JsonConverter<System.Nullable<TimeOnly>>
{
    const string TimeFormat = "HH:mm:ss.FFFFFFF";
    //const string TimeFormat = "HH:mm";

    public override TimeOnly? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, TimeOnly? existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        TimeOnly? result = null;
        //return TimeOnly.ParseExact((string)reader.Value, TimeFormat, System.Globalization.CultureInfo.InvariantCulture);
        if (TimeOnly.TryParse((string)reader.Value, out var parsed))
        {   
            result = parsed;
        }

        return result;
    }

    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, TimeOnly? value, Newtonsoft.Json.JsonSerializer serializer)
    {
        if(value.HasValue)
        writer.WriteValue(value.Value.ToString(TimeFormat, System.Globalization.CultureInfo.InvariantCulture));
    }
}


public class DateOnlyJsonConverter : Newtonsoft.Json.JsonConverter<System.Nullable<DateOnly>>
{
    const string Format = "yyyy-MM-dd";

    public override DateOnly? ReadJson(Newtonsoft.Json.JsonReader reader,
        Type objectType,
        DateOnly? existingValue,
        bool hasExistingValue,
        Newtonsoft.Json.JsonSerializer serializer)
        {
            //DateOnly.Parse((string)reader.Value, Format, System.Globalization.CultureInfo.InvariantCulture);
            DateOnly? result = null;

            if(DateOnly.TryParse((string)reader.Value, out var temp))
            {
                result = temp;
            }

            return result;
        }
    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, DateOnly? value, Newtonsoft.Json.JsonSerializer serializer)
    {
        if(value.HasValue)
        writer.WriteValue(value.Value.ToString(Format,System.Globalization.CultureInfo.InvariantCulture));
    }        
}