using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace mmria.server.util;

public static class EscapedJsonResultFactory
{
    private const string JsonContentType = "application/json; charset=utf-8";

    private static readonly JsonSerializerSettings HtmlEscapingSerializerSettings = new()
    {
        StringEscapeHandling = StringEscapeHandling.EscapeHtml
    };

    public static ContentResult Create(object value) =>
        new()
        {
            Content = Serialize(value),
            ContentType = JsonContentType,
            StatusCode = 200
        };

    public static string Serialize(object value) =>
        JsonConvert.SerializeObject(value, HtmlEscapingSerializerSettings);
}
