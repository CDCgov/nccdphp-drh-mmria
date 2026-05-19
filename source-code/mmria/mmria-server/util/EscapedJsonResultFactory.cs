using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

    public static ActionResult Create(object value) =>
        new EscapedJsonActionResult(value);

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

    public sealed class EscapedJsonActionResult : ActionResult
    {
        public EscapedJsonActionResult(object value)
        {
            Value = value;
        }

        public object Value { get; }

        public string SerializedValue => Serialize(Value);

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var response = context.HttpContext.Response;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = JsonContentType;
            await response.WriteAsync(SerializedValue, context.HttpContext.RequestAborted);
        }
    }
}
