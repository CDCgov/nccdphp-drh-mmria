using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace mmria.server.util;

public static class EscapedJsonResultFactory
{
    private const string NoSniffHeaderName = "X-Content-Type-Options";
    private const string NoSniffHeaderValue = "nosniff";

    // System.Text.Json default options use JavaScriptEncoder.Default, which Unicode-escapes
    // <, >, &, ', " in all string values (\u003c, \u003e, \u0026, \u0027, \u0022).
    // This eliminates the XSS taint path: value is stored as JsonResult.Value and serialized
    // by the framework rather than being placed directly into a ContentResult.Content string.
    private static readonly JsonSerializerOptions HtmlSafeJsonOptions = new()
    {
        WriteIndented = false
    };

    // Used by Serialize() for callers that need a raw JSON string (e.g. binary file payloads).
    private static readonly JsonSerializerSettings HtmlEscapingSerializerSettings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        StringEscapeHandling = StringEscapeHandling.EscapeHtml,
        TypeNameHandling = TypeNameHandling.None
    };

    public static JsonResult Create(object value) =>
        new SecureJsonResult(value, HtmlSafeJsonOptions);

    public static string Serialize(object value)
    {
        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var jsonWriter = new JsonTextWriter(stringWriter)
        {
            CloseOutput = false,
            Formatting = Formatting.None,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml
        };

        Newtonsoft.Json.JsonSerializer.Create(HtmlEscapingSerializerSettings).Serialize(jsonWriter, value);
        jsonWriter.Flush();
        return stringWriter.ToString();
    }

    private sealed class SecureJsonResult : JsonResult
    {
        public SecureJsonResult(object value, JsonSerializerOptions options)
            : base(value, options) { }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.Headers[NoSniffHeaderName] = NoSniffHeaderValue;
            return base.ExecuteResultAsync(context);
        }
    }
}
