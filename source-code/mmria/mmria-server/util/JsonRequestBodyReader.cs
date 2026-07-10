using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using mmria.common.utils;
using Newtonsoft.Json;

namespace mmria.server.util;

internal static class JsonRequestBodyReader
{
    private static readonly ConcurrentDictionary<Type, bool> CaseAwareTypeCache = new();

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
            if (RequiresCaseAwareDeserialization(typeof(T)))
            {
                return JsonConvert.DeserializeObject<T>(body, CaseJsonSerialization.CreateNewtonsoftSerializerSettings());
            }

            return JsonConvert.DeserializeObject<T>(body);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static bool RequiresCaseAwareDeserialization(Type type)
    {
        return CaseAwareTypeCache.GetOrAdd(type, static t => ContainsTypedCase(t, new HashSet<Type>()));
    }

    private static bool ContainsTypedCase(Type type, HashSet<Type> visited)
    {
        if (type == null)
        {
            return false;
        }

        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(mmria.case_version.v260615.mmria_case))
        {
            return true;
        }

        if (type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(Guid) ||
            type == typeof(TimeOnly) ||
            type == typeof(DateOnly))
        {
            return false;
        }

        if (!visited.Add(type))
        {
            return false;
        }

        if (type.IsArray)
        {
            return ContainsTypedCase(type.GetElementType(), visited);
        }

        if (type.IsGenericType)
        {
            foreach (var genericArgument in type.GetGenericArguments())
            {
                if (ContainsTypedCase(genericArgument, visited))
                {
                    return true;
                }
            }
        }

        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (ContainsTypedCase(property.PropertyType, visited))
            {
                return true;
            }
        }

        return false;
    }
}
