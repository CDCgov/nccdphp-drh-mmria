using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace mmria.common.couchdb;

/// <summary>
/// Custom JsonConverter for Dictionary&lt;string, Dictionary&lt;string, string&gt;&gt; that handles
/// nested JSON objects as inner dictionary values by storing them as raw JSON strings.
/// This allows OverridableConfiguration.string_keys to hold entries like vital_sign_range
/// whose CouchDB value is a nested object rather than a flat string.
/// </summary>
public sealed class NestedStringDictionaryConverter : JsonConverter<Dictionary<string, Dictionary<string, string>>>
{
    public override Dictionary<string, Dictionary<string, string>> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject for outer dictionary.");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName in outer dictionary.");

            string outerKey = reader.GetString();
            reader.Read();

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Expected StartObject for inner dictionary at key '{outerKey}'.");

            var innerDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected PropertyName in inner dictionary.");

                string innerKey = reader.GetString();
                reader.Read();

                string innerValue = reader.TokenType switch
                {
                    JsonTokenType.String => reader.GetString(),
                    JsonTokenType.Null => null,
                    JsonTokenType.True => "true",
                    JsonTokenType.False => "false",
                    JsonTokenType.Number => reader.GetDecimal().ToString(),
                    JsonTokenType.StartObject or JsonTokenType.StartArray =>
                        JsonDocument.ParseValue(ref reader).RootElement.GetRawText(),
                    _ => throw new JsonException(
                        $"Unexpected token type '{reader.TokenType}' for inner value of key '{innerKey}'.")
                };

                innerDict[innerKey] = innerValue;
            }

            result[outerKey] = innerDict;
        }

        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        Dictionary<string, Dictionary<string, string>> value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var outerEntry in value)
        {
            writer.WritePropertyName(outerEntry.Key);
            writer.WriteStartObject();
            foreach (var innerEntry in outerEntry.Value)
            {
                writer.WritePropertyName(innerEntry.Key);
                writer.WriteStringValue(innerEntry.Value);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }
}
