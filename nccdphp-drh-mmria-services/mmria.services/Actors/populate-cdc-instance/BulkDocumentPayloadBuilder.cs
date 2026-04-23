using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace mmria.server.utils;

/// <summary>
/// mmria.services-side copy of the mmria-server <c>BulkDocumentPayloadBuilder</c>.
/// Issue H in <c>docs/ai/performance_risk_review.md</c>: builds CouchDB
/// <c>_bulk_docs</c> payloads by concatenating already-serialized document JSON
/// strings rather than round-tripping through Newtonsoft <c>JObject</c>/<c>JArray</c>.
/// The two assemblies cannot share a single helper because mmria.services does
/// not (and should not) take a project reference on mmria-server.
/// </summary>
internal static class BulkDocumentPayloadBuilder
{
    public static string GetIdFromDocumentJson(string documentJson)
    {
        if (string.IsNullOrWhiteSpace(documentJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(documentJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (doc.RootElement.TryGetProperty("_id", out var idElement) &&
                idElement.ValueKind == JsonValueKind.String)
            {
                return idElement.GetString();
            }
        }
        catch (JsonException)
        {
            // Malformed JSON: caller decides what to do; we report no id.
        }

        return null;
    }

    public static string RewriteDocumentJson(string documentJson, string setRev, bool stripRevWhenNoSet)
    {
        if (string.IsNullOrWhiteSpace(documentJson))
        {
            return documentJson;
        }

        if (setRev == null && !stripRevWhenNoSet)
        {
            return documentJson;
        }

        using var doc = JsonDocument.Parse(documentJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return documentJson;
        }

        using var ms = new MemoryStream(documentJson.Length + 64);
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            bool wroteRev = false;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.NameEquals("_rev"))
                {
                    if (setRev != null && !wroteRev)
                    {
                        writer.WriteString("_rev", setRev);
                        wroteRev = true;
                    }
                    continue;
                }

                prop.WriteTo(writer);
            }

            if (setRev != null && !wroteRev)
            {
                writer.WriteString("_rev", setRev);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
    }

    public static string BuildBulkDocsPayload(IReadOnlyList<string> documentJsonList)
    {
        if (documentJsonList == null || documentJsonList.Count == 0)
        {
            return "{\"docs\":[]}";
        }

        long estimate = 16;
        for (int i = 0; i < documentJsonList.Count; i++)
        {
            estimate += (documentJsonList[i]?.Length ?? 0) + 1;
        }
        int initialCapacity = estimate > int.MaxValue ? int.MaxValue : (int)estimate;

        var sb = new StringBuilder(initialCapacity);
        sb.Append("{\"docs\":[");

        bool first = true;
        for (int i = 0; i < documentJsonList.Count; i++)
        {
            string dj = documentJsonList[i];
            if (string.IsNullOrWhiteSpace(dj))
            {
                continue;
            }

            // mmria.services bulk path historically did not strip _rev before the
            // _bulk_docs POST, so preserve that behavior and concatenate as-is.
            if (!first)
            {
                sb.Append(',');
            }
            sb.Append(dj);
            first = false;
        }

        sb.Append("]}");
        return sb.ToString();
    }
}
