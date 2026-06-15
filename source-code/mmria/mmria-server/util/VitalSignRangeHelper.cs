using System;
using System.Collections.Generic;
using System.Text.Json;

namespace mmria.server.util;

public static class VitalSignRangeHelper
{
    public static Dictionary<string, string> GetVitalSignRangeConfig(
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

    private static Dictionary<string, string> TryParseVitalSignRange(string rawJson)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(rawJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static Dictionary<string, string> GetDefaults()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["temperature_fahrenheit_min"] = "0",
            ["temperature_fahrenheit_max"] = "110",
            ["heart_rate_min"] = "0",
            ["heart_rate_max"] = "400",
            ["respiration_min"] = "0",
            ["respiration_max"] = "60",
            ["systolic_blood_pressure_min"] = "0",
            ["systolic_blood_pressure_max"] = "300",
            ["diastolic_blood_pressure_min"] = "0",
            ["diastolic_blood_pressure_max"] = "300",
            ["oxygen_saturation_min"] = "0",
            ["oxygen_saturation_max"] = "100"
        };
    }
}
