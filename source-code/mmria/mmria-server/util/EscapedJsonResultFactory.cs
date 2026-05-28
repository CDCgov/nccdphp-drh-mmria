using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace mmria.server.util;

public static class EscapedJsonResultFactory
{
    private const string JsonContentType = "application/json; charset=utf-8";

    private static readonly JsonSerializerSettings HtmlEscapingSerializerSettings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        StringEscapeHandling = StringEscapeHandling.EscapeHtml,
        TypeNameHandling = TypeNameHandling.None
    };

    public static ContentResult Create(object value) =>
        new()
        {
            Content = Serialize(value),
            ContentType = JsonContentType,
            StatusCode = 200
        };

    public static string Serialize(object value)
    {
        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var jsonWriter = new JsonTextWriter(stringWriter)
        {
            CloseOutput = false,
            Formatting = Formatting.None,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml
        };

        JsonSerializer.Create(HtmlEscapingSerializerSettings).Serialize(jsonWriter, value);
        jsonWriter.Flush();
        return stringWriter.ToString();
    }
}
