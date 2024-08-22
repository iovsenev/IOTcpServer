namespace IOTcpServer.Core.Helpers;

internal interface ISerializationHelper
{
    /// <summary>
    /// Десериализация из JSON в объект указанного типа.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    /// <param name="json">JSON string.</param>
    /// <returns>Instance.</returns>
    T? DeserializeJson<T>(string json);

    /// <summary>
    /// Сериализация из объекта в JSON.
    /// </summary>
    /// <param name="obj">Object.</param>
    /// <param name="pretty">Pretty print.</param>
    /// <returns>JSON.</returns>
    string SerializeJson(object obj, bool pretty = true);
}