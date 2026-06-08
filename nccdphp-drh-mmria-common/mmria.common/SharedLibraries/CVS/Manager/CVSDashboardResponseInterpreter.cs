using System;
using System.Linq;
using System.Text.Json;
using mmria.common.SharedLibraries.CVS.Model;

namespace mmria.common.SharedLibraries.CVS.Manager;

internal static class CVSDashboardResponseInterpreter
{
    internal static CVSFileStatusResult Interpret(CVSExternalPostResponse externalResponse)
    {
        var result = new CVSFileStatusResult();

        if (externalResponse == null)
        {
            result.file_status = "unavailable";
            result.message = "The CVS service did not respond.";
            return result;
        }

        if (externalResponse.is_transport_failure)
        {
            ApplyExternalErrorFields(result, externalResponse);
            result.file_status = "unavailable";
            result.message = externalResponse.transport_error_kind == "timeout"
                ? "The CVS service request timed out."
                : "The CVS service did not respond.";
            return result;
        }

        if (!externalResponse.is_success_status_code)
        {
            ApplyExternalErrorFields(result, externalResponse);
            result.file_status = IsTransientHttpStatus(externalResponse.status_code)
                ? "unavailable"
                : "error";
            result.message = IsTransientHttpStatus(externalResponse.status_code)
                ? "The CVS service is temporarily unavailable."
                : "The CVS service returned an error.";
            return result;
        }

        string responseBody = externalResponse.body?.Trim();
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            result.file_status = "unavailable";
            result.message = "The CVS service returned an empty response.";
            return result;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return InterpretSuccessfulJsonResponse(document.RootElement);
        }
        catch (JsonException)
        {
            return InterpretSuccessfulTextResponse(responseBody);
        }
    }

    internal static bool IsTransientHttpStatus(int statusCode)
    {
        return statusCode == 408 ||
            statusCode == 429 ||
            statusCode == 500 ||
            statusCode == 502 ||
            statusCode == 503 ||
            statusCode == 504;
    }

    private static CVSFileStatusResult InterpretSuccessfulJsonResponse(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.String)
        {
            return InterpretSuccessfulTextResponse(root.GetString());
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return CreateControlledError("The CVS service returned an unexpected response.");
        }

        TryGetPropertyIgnoreCase(root, "body", out var bodyElement);
        string body = GetElementString(bodyElement);

        if (TryGetBooleanLikeProperty(root, "isBase64Encoded", out bool isBase64Encoded) && isBase64Encoded)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return CreateControlledError("The CVS service returned an empty PDF response.");
            }

            try
            {
                return new CVSFileStatusResult
                {
                    file_status = "file ready",
                    PdfBytes = Convert.FromBase64String(body)
                };
            }
            catch (FormatException)
            {
                return CreateControlledError("The CVS service returned an invalid PDF response.");
            }
        }

        string responseText = string.IsNullOrWhiteSpace(body) ? root.ToString() : body;
        if (IsGeneratingResponse(responseText))
        {
            return new CVSFileStatusResult
            {
                file_status = "generating",
                message = "The CVS service is preparing the PDF."
            };
        }

        if (LooksLikeUnavailableResponse(responseText))
        {
            return new CVSFileStatusResult
            {
                file_status = "unavailable",
                message = "The CVS service is temporarily unavailable."
            };
        }

        return CreateControlledError("The CVS service returned an unexpected response.");
    }

    private static CVSFileStatusResult InterpretSuccessfulTextResponse(string responseText)
    {
        if (IsGeneratingResponse(responseText))
        {
            return new CVSFileStatusResult
            {
                file_status = "generating",
                message = "The CVS service is preparing the PDF."
            };
        }

        if (LooksLikeUnavailableResponse(responseText))
        {
            return new CVSFileStatusResult
            {
                file_status = "unavailable",
                message = "The CVS service is temporarily unavailable."
            };
        }

        return CreateControlledError("The CVS service returned an unexpected response.");
    }

    private static bool IsGeneratingResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        return body.StartsWith("PDF ", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("PDF is being created", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("PDF creation has been initiated", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("retry API call", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeUnavailableResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        return body.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("temporarily", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("service unavailable", StringComparison.OrdinalIgnoreCase);
    }

    private static CVSFileStatusResult CreateControlledError(string message)
    {
        return new CVSFileStatusResult
        {
            file_status = "error",
            message = message
        };
    }

    private static void ApplyExternalErrorFields(CVSFileStatusResult result, CVSExternalPostResponse externalResponse)
    {
        if (externalResponse.status_code > 0)
        {
            result.external_status_code = externalResponse.status_code;
        }

        result.external_reason_phrase = LimitExternalErrorMessage(externalResponse.reason_phrase);
        result.external_error_message = GetExternalErrorMessage(externalResponse.body);
    }

    private static string GetExternalErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        var trimmedBody = responseBody.Trim();

        try
        {
            using var document = JsonDocument.Parse(trimmedBody);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetStringProperty(root, "message", out var message))
                {
                    return LimitExternalErrorMessage(message);
                }

                if (TryGetStringProperty(root, "error", out var error))
                {
                    return LimitExternalErrorMessage(error);
                }

                if (TryGetStringProperty(root, "detail", out var detail))
                {
                    return LimitExternalErrorMessage(detail);
                }
            }
        }
        catch (JsonException)
        {
        }

        return LimitExternalErrorMessage(trimmedBody);
    }

    private static bool TryGetStringProperty(JsonElement root, string propertyName, out string value)
    {
        value = null;
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var propertyValue))
        {
            return false;
        }

        value = GetElementString(propertyValue);
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetBooleanLikeProperty(JsonElement root, string propertyName, out bool value)
    {
        value = false;
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var propertyValue))
        {
            return false;
        }

        if (propertyValue.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (propertyValue.ValueKind == JsonValueKind.False)
        {
            value = false;
            return true;
        }

        var text = GetElementString(propertyValue);
        if (bool.TryParse(text, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string propertyName, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string GetElementString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.ToString()
        };
    }

    private static string LimitExternalErrorMessage(string value)
    {
        const int maxLength = 1000;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var singleLineValue = new string(value
                .Where(character => !char.IsControl(character))
                .ToArray())
            .Trim();

        if (singleLineValue.Length == 0)
        {
            return null;
        }

        return singleLineValue.Length <= maxLength
            ? singleLineValue
            : singleLineValue.Substring(0, maxLength);
    }
}
