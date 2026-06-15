using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace mmria.server.util;

public sealed class VitalSignRangeEntry
{
    [JsonPropertyName("min")]
    public double Min { get; set; }

    [JsonPropertyName("max")]
    public double Max { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }
}

public static class VitalSignRangeHelper
{
    public static Dictionary<string, VitalSignRangeEntry> GetVitalSignRangeConfig(
        mmria.common.couchdb.OverridableConfiguration configuration,
        string hostPrefix)
    {
        var normalizedPrefix = string.IsNullOrWhiteSpace(hostPrefix) ? "shared" : hostPrefix.Trim();

        var rawJson = configuration?.GetString("vital_sign_range", normalizedPrefix);

        if (!string.IsNullOrWhiteSpace(rawJson))
        {
            var parsed = TryParseVitalSignRange(rawJson);
            if (parsed != null)
            {
                return parsed;
            }
        }

        return GetDefaults();
    }

    private static Dictionary<string, VitalSignRangeEntry> TryParseVitalSignRange(string rawJson)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Try nested per-field format first: { "temperature": { "min": 0, "max": 110, "label": "..." }, ... }
            var nested = JsonSerializer.Deserialize<Dictionary<string, VitalSignRangeEntry>>(rawJson, options);
            if (nested != null && nested.Count > 0)
            {
                return nested;
            }
        }
        catch (Exception)
        {
            // fall through to flat key attempt
        }

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Attempt flat key format: { "temperature_fahrenheit_min": "0", "temperature_fahrenheit_max": "110", ... }
            var flat = JsonSerializer.Deserialize<Dictionary<string, string>>(rawJson, options);
            if (flat != null && flat.Count > 0)
            {
                return BuildFromFlatKeys(flat);
            }
        }
        catch (Exception)
        {
            // fall through to defaults
        }

        return null;
    }

    // Maps flat DB keys (e.g. "temperature_fahrenheit_min") to per-field entries keyed by input name attribute.
    private static Dictionary<string, VitalSignRangeEntry> BuildFromFlatKeys(Dictionary<string, string> flat)
    {
        static double Parse(string v) => double.TryParse(v, out var d) ? d : 0;

        var result = new Dictionary<string, VitalSignRangeEntry>(StringComparer.OrdinalIgnoreCase);

        if (flat.TryGetValue("temperature_fahrenheit_min", out var tMin) &&
            flat.TryGetValue("temperature_fahrenheit_max", out var tMax))
        {
            result["temperature"] = new VitalSignRangeEntry { Min = Parse(tMin), Max = Parse(tMax), Label = "Temperature" };
        }

        if (flat.TryGetValue("heart_rate_min", out var hrMin) &&
            flat.TryGetValue("heart_rate_max", out var hrMax))
        {
            result["pulse"] = new VitalSignRangeEntry { Min = Parse(hrMin), Max = Parse(hrMax), Label = "Heart Rate" };
        }

        if (flat.TryGetValue("respiration_min", out var rMin) &&
            flat.TryGetValue("respiration_max", out var rMax))
        {
            result["respiration"] = new VitalSignRangeEntry { Min = Parse(rMin), Max = Parse(rMax), Label = "Respiration" };
        }

        if (flat.TryGetValue("systolic_blood_pressure_min", out var sbpMin) &&
            flat.TryGetValue("systolic_blood_pressure_max", out var sbpMax))
        {
            result["bp_systolic"] = new VitalSignRangeEntry { Min = Parse(sbpMin), Max = Parse(sbpMax), Label = "Systolic BP" };
        }

        if (flat.TryGetValue("diastolic_blood_pressure_min", out var dbpMin) &&
            flat.TryGetValue("diastolic_blood_pressure_max", out var dbpMax))
        {
            result["bp_diastolic"] = new VitalSignRangeEntry { Min = Parse(dbpMin), Max = Parse(dbpMax), Label = "Diastolic BP" };
        }

        return result.Count > 0 ? result : null;
    }

    private static Dictionary<string, VitalSignRangeEntry> GetDefaults()
    {
        return new Dictionary<string, VitalSignRangeEntry>(StringComparer.OrdinalIgnoreCase)
        {
            ["temperature"]  = new VitalSignRangeEntry { Min = 0, Max = 110, Label = "Temperature" },
            ["pulse"]        = new VitalSignRangeEntry { Min = 0, Max = 400, Label = "Heart Rate" },
            ["respiration"]  = new VitalSignRangeEntry { Min = 0, Max = 60,  Label = "Respiration" },
            ["bp_systolic"]  = new VitalSignRangeEntry { Min = 0, Max = 300, Label = "Systolic BP" },
            ["bp_diastolic"] = new VitalSignRangeEntry { Min = 0, Max = 300, Label = "Diastolic BP" }
        };
    }
}
