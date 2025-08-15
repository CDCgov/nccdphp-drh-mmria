using System.Text.Json;
using System.Text.Json.Serialization;
public class NullToEmptyStringConverter : JsonConverter<DateTime?>
{
    // Override default null handling
    public override bool HandleNull => true;
    // Check the type
    public override bool CanConvert(Type typeToConvert)
    {
    	return typeToConvert == typeof(DateTime?);
    }
    // Ignore for this exampke
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
    	DateTime? result = null;

        var test_value = reader.GetString();

        if(!string.IsNullOrWhiteSpace(test_value))
        {
            if(DateTime.TryParse(test_value, out var new_value))
            {
                result = new_value;
            }
        }

        return result;
    }
    // 
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
    	throw new NotImplementedException();
    }
}