using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace mmria.server.util;

public static class EscapedJsonResultFactory
{
    private const string JsonContentType = "application/json; charset=utf-8";
    private const string NoSniffHeaderName = "X-Content-Type-Options";
    private const string NoSniffHeaderValue = "nosniff";

    private static readonly JsonSerializerSettings HtmlEscapingSerializerSettings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        StringEscapeHandling = StringEscapeHandling.EscapeHtml,
        TypeNameHandling = TypeNameHandling.None
    };

    public static JsonResult Create(object value) =>
        new SecureEscapedJsonResult
        {
            Value = value,
            SerializerSettings = HtmlEscapingSerializerSettings,
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

    private sealed class SecureEscapedJsonResult : JsonResult
    {
        public SecureEscapedJsonResult() : base(value: null)
        {
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.Headers[NoSniffHeaderName] = NoSniffHeaderValue;
            return base.ExecuteResultAsync(context);
        }
    }
}
