using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace IOTcpServer.Core.Constants;

/// <summary>
/// Статус сообщения
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageStatus
{
    /// <summary>
    /// Нормальное
    /// </summary>
    [EnumMember(Value = "Normal")]
    Normal = 0,
    /// <summary>
    /// Выполнено
    /// </summary>
    [EnumMember(Value = "Success")]
    Success = 1,
    /// <summary>
    /// Провал
    /// </summary>
    [EnumMember(Value = "Failure")]
    Failure = 2,
    /// <summary>
    /// Требуется аутентификация
    /// </summary>
    [EnumMember(Value = "AuthRequired")]
    AuthRequired = 3,
    /// <summary>
    /// Запрошена аутентификация
    /// </summary>
    [EnumMember(Value = "AuthRequested")]
    AuthRequested = 4,
    /// <summary>
    /// Аутентификация пройдена
    /// </summary>
    [EnumMember(Value = "AuthSuccess")]
    AuthSuccess = 5,
    /// <summary>
    /// Аутентификация провалена
    /// </summary>
    [EnumMember(Value = "AuthFailure")]
    AuthFailure = 6,
    /// <summary>
    /// Удалено
    /// </summary>
    [EnumMember(Value = "Removed")]
    Removed = 7,
    /// <summary>
    /// Неисправно
    /// </summary>
    [EnumMember(Value = "Shutdown")]
    Shutdown = 8,
    /// <summary>
    /// Heartbeat
    /// </summary>
    [EnumMember(Value = "Heartbeat")]
    Heartbeat = 9,
    /// <summary>
    /// Задержка
    /// </summary>
    [EnumMember(Value = "Timeout")]
    Timeout = 10,
    /// <summary>
    /// Регистрация клиента
    /// </summary>
    [EnumMember(Value = "RegisterClient")]
    RegisterClient = 11
}