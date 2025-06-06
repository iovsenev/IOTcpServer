﻿using System.Text.Json.Serialization;
using System.Text.Json;

namespace IOTcpServer.Core.Helpers;
internal class DefaultSerializationHelper : ISerializationHelper
{
    public T? DeserializeJson<T>(string json)
    {
        var result = JsonSerializer.Deserialize<T>(json);
        return result;
    }

    /// <summary>
    /// Сериализация из объекта в JSON.
    /// </summary>
    /// <param name="obj">Object.</param>
    /// <param name="pretty">Pretty print.</param>
    /// <returns>JSON.</returns>
    public string SerializeJson(object obj, bool pretty = true)
    {
        if (obj == null) return string.Empty;

        JsonSerializerOptions options = new JsonSerializerOptions();
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        //// see https://github.com/dotnet/runtime/issues/43026
        //options.Converters.Add(_ExceptionConverter);
        //options.Converters.Add(_NameValueCollectionConverter);

        options.WriteIndented = pretty;
        return JsonSerializer.Serialize(obj, options);
    }
}