#if !IS_PMSS_ENHANCED
using System;
using System.Linq;
using mmria.common.metadata;
using Newtonsoft.Json.Linq;

namespace mmria.server.util;

internal static class CaseCompatibilityOracleCanonicalizer
{
    public static JObject Canonicalize(string caseJson, app metadata)
    {
        if (string.IsNullOrWhiteSpace(caseJson))
        {
            throw new ArgumentException("Case JSON cannot be empty.", nameof(caseJson));
        }

        if (metadata == null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        var result = JObject.Parse(caseJson);
        ApplyChildren(result, metadata.children);
        return result;
    }

    private static void ApplyChildren(JObject parent, node[] children)
    {
        if (parent == null || children == null)
        {
            return;
        }

        foreach (var child in children)
        {
            ApplyNode(parent, child);
        }
    }

    private static void ApplyNode(JObject parent, node metadata)
    {
        if (parent == null || metadata == null || string.IsNullOrWhiteSpace(metadata.name))
        {
            return;
        }

        var type = metadata.type?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(type))
        {
            return;
        }

        switch (type)
        {
            case "form":
            case "group":
                ApplyObjectNode(parent, metadata);
                return;

            case "grid":
                ApplyArrayNode(parent, metadata);
                return;

            case "list":
            case "yes_no":
                ApplyListNode(parent, metadata);
                return;

            default:
                ApplyNestedChildrenIfPresent(parent, metadata);
                return;
        }
    }

    private static void ApplyObjectNode(JObject parent, node metadata)
    {
        if (parent.TryGetValue(metadata.name, StringComparison.OrdinalIgnoreCase, out var existing))
        {
            if (existing is JObject existingObject)
            {
                ApplyChildren(existingObject, metadata.children);
                return;
            }

            if (existing is JArray existingArray)
            {
                ApplyArrayItems(existingArray, metadata.children);
            }

            if (existing.Type != JTokenType.Null)
            {
                return;
            }
        }

        var synthesized = new JObject();
        ApplyChildren(synthesized, metadata.children);

        if (synthesized.HasValues)
        {
            parent[metadata.name] = synthesized;
        }
    }

    private static void ApplyArrayNode(JObject parent, node metadata)
    {
        if (!parent.TryGetValue(metadata.name, StringComparison.OrdinalIgnoreCase, out var existing))
        {
            return;
        }

        if (existing is JArray existingArray)
        {
            ApplyArrayItems(existingArray, metadata.children);
        }
    }

    private static void ApplyArrayItems(JArray array, node[] children)
    {
        if (array == null || children == null)
        {
            return;
        }

        foreach (var item in array.OfType<JObject>())
        {
            ApplyChildren(item, children);
        }
    }

    private static void ApplyListNode(JObject parent, node metadata)
    {
        var hasValue = parent.TryGetValue(metadata.name, StringComparison.OrdinalIgnoreCase, out var existing);
        if (hasValue && existing != null && existing.Type != JTokenType.Null)
        {
            return;
        }

        parent[metadata.name] = metadata.is_multiselect == true
            ? new JArray()
            : new JValue(ResolveSingleSelectDefault(metadata));
    }

    private static string ResolveSingleSelectDefault(node metadata)
    {
        return !string.IsNullOrWhiteSpace(metadata.default_value)
            ? metadata.default_value
            : "9999";
    }

    private static void ApplyNestedChildrenIfPresent(JObject parent, node metadata)
    {
        if (metadata.children == null)
        {
            return;
        }

        if (!parent.TryGetValue(metadata.name, StringComparison.OrdinalIgnoreCase, out var existing))
        {
            return;
        }

        if (existing is JObject existingObject)
        {
            ApplyChildren(existingObject, metadata.children);
        }
        else if (existing is JArray existingArray)
        {
            ApplyArrayItems(existingArray, metadata.children);
        }
    }
}
#endif
