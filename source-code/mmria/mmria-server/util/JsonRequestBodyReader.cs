using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace mmria.server.util;

internal static class JsonRequestBodyReader
{
    public static async Task<T> ReadAsync<T>(HttpRequest request)
    {
        if (request?.Body == null)
        {
            return default;
        }

        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();

        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(body);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
