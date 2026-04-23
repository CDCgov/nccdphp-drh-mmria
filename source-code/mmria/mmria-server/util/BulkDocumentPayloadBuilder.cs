#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace mmria.server.utils;

/// <summary>
/// Allocation-light helpers for building CouchDB <c>_bulk_docs</c> payloads.
/// Replaces the prior Newtonsoft <c>JObject</c> / <c>JArray</c> graph that was
/// being built and torn down once per chunk in <see cref="c_document_sync_all"/>.
/// Issue H in <c>docs/ai/performance_risk_review.md</c>.
/// </summary>
public static class BulkDocumentPayloadBuilder
{
    /// <summary>
    /// Read-only extraction of the top-level <c>_id</c> string from a document JSON.
    /// Returns <c>null</c> when the JSON is empty, malformed, not an object, or has
    /// no string <c>_id</c> property.
    /// </summary>
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

    /// <summary>
    /// Returns a copy of <paramref name="documentJson"/> with the top-level <c>_rev</c>
    /// property either replaced/inserted (<paramref name="setRev"/> non-null) or removed
    /// (<paramref name="setRev"/> null and <paramref name="stripRevWhenNoSet"/> true).
    /// When neither applies, the original string is returned unchanged.
    /// </summary>
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
                    // Strip case: simply skip writing this property.
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

    /// <summary>
    /// Build a CouchDB <c>{"keys":[...]}</c> lookup payload without allocating any
    /// intermediate <c>JObject</c>/<c>JArray</c> graph.
    /// </summary>
    public static string BuildKeysPayload(IEnumerable<string> ids)
    {
        if (ids == null)
        {
            return "{\"keys\":[]}";
        }

        using var ms = new MemoryStream(256);
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("keys");
            writer.WriteStartArray();
            foreach (var id in ids)
            {
                if (id == null)
                {
                    continue;
                }
                writer.WriteStringValue(id);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
    }

    /// <summary>
    /// Build a CouchDB <c>{"docs":[...]}</c> payload by string-concatenating already
    /// serialized document JSONs, applying <c>_rev</c> rewrites only where required.
    /// Replaces the prior pattern of materializing every doc as a <c>JObject</c>.
    /// </summary>
    /// <param name="documentJsonList">Per-document JSON strings (top-level objects).</param>
    /// <param name="idToRev">
    /// Optional map of document id -> existing CouchDB revision. When non-null this
    /// indicates the "hydrate existing revisions" mode: for any document whose id is
    /// in the map the existing <c>_rev</c> is replaced with the mapped value; for
    /// documents not in the map the original JSON (including any <c>_rev</c>) is
    /// preserved unchanged. When the map is null, every document has its <c>_rev</c>
    /// stripped.
    /// </param>
    public static string BuildBulkDocsPayload(
        IReadOnlyList<string> documentJsonList,
        IReadOnlyDictionary<string, string> idToRev)
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

            string transformed;
            if (idToRev != null)
            {
                string id = GetIdFromDocumentJson(dj);
                if (id != null && idToRev.TryGetValue(id, out var rev) && !string.IsNullOrWhiteSpace(rev))
                {
                    transformed = RewriteDocumentJson(dj, setRev: rev, stripRevWhenNoSet: false);
                }
                else
                {
                    // Hydrate-mode but id wasn't in the existing-rev map: preserve
                    // the original document (including any prior _rev). This
                    // matches the legacy JObject-based behavior.
                    transformed = dj;
                }
            }
            else
            {
                transformed = RewriteDocumentJson(dj, setRev: null, stripRevWhenNoSet: true);
            }

            if (!first)
            {
                sb.Append(',');
            }
            sb.Append(transformed);
            first = false;
        }

        sb.Append("]}");
        return sb.ToString();
    }
}
#endif
