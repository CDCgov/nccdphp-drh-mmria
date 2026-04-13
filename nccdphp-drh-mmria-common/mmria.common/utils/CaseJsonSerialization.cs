using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using mmria.case_version.v260120;
using Newtonsoft.Json;

namespace mmria.common.utils;

public static class CaseJsonSerialization
{
    public static JsonSerializerSettings CreateNewtonsoftSerializerSettings(bool ignoreNulls = false)
    {
        return new JsonSerializerSettings
        {
            NullValueHandling = ignoreNulls ? NullValueHandling.Ignore : NullValueHandling.Include,
            Converters =
            {
                new TimeOnlyJsonConverter(),
                new DateOnlyJsonConverter()
            }
        };
    }

    public static mmria_case DeserializeMmriaCase(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Case JSON cannot be null or empty.", nameof(json));
        }

        try
        {
            return DeserializeStrict(json);
        }
        catch (Newtonsoft.Json.JsonSerializationException)
        {
            return DeserializeWithCompatibilityFallback(json);
        }
        catch (Newtonsoft.Json.JsonReaderException)
        {
            return DeserializeWithCompatibilityFallback(json);
        }
    }

    public static string SerializeMmriaCase(mmria_case caseDoc)
    {
        if (caseDoc == null)
        {
            throw new ArgumentNullException(nameof(caseDoc));
        }

        return JsonConvert.SerializeObject(caseDoc, CreateNewtonsoftSerializerSettings(ignoreNulls: true));
    }

    private static mmria_case DeserializeStrict(string json)
    {
        var result = JsonConvert.DeserializeObject<mmria_case>(json, CreateNewtonsoftSerializerSettings());
        return result ?? throw new JsonSerializationException("Typed case deserialization returned null.");
    }

    private static mmria_case DeserializeWithCompatibilityFallback(string json)
    {
        var options = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        using var jsonDoc = System.Text.Json.JsonSerializer.Deserialize<JsonDocument>(json, options)
            ?? throw new System.Text.Json.JsonException("Compatibility fallback could not parse case JSON.");

        var result = new mmria_case();
        result.Convert(jsonDoc.RootElement);
        return result;
    }
}
