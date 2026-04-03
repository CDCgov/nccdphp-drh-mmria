using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace mmria.server.util;

public static class EscapedJsonResultFactory
{
    private static readonly JsonSerializerSettings HtmlEscapingSerializerSettings = new()
    {
        StringEscapeHandling = StringEscapeHandling.EscapeHtml
    };

    public static JsonResult Create(object value) =>
        new(value, HtmlEscapingSerializerSettings);

    public static string Serialize(object value) =>
        JsonConvert.SerializeObject(value, HtmlEscapingSerializerSettings);
}
