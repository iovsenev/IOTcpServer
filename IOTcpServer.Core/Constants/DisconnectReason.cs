using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace IOTcpServer.Core.Constants;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DisconnectReason
{
    /// <summary>
    /// Нормальное отключение.
    /// </summary>
    [EnumMember(Value = "Normal")]
    Normal = 0,
    /// <summary>
    /// Клиентское соединение было намеренно разорвано программно или сервером.
    /// </summary>
    [EnumMember(Value = "Removed")]
    Removed = 1,
    /// <summary>
    /// Время ожидания клиентского соединения истекло; сервер не получил данные в течение отведенного времени.
    /// </summary>
    [EnumMember(Value = "Timeout")]
    Timeout = 2,
    /// <summary>
    /// Отключение из-за отключения сервера.
    /// </summary>
    [EnumMember(Value = "Shutdown")]
    Shutdown = 3,
    /// <summary>
    /// Отключение из-за сбоя аутентификации.
    /// </summary>
    [EnumMember(Value = "AuthFailure")]
    AuthFailure
}