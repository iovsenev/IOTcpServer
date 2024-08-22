namespace IOTcpServer.Core.Constants;
/// <summary>
/// Серьезность сообщения.
/// </summary>
public enum Severity
{
    /// <summary>
    /// Сообщение отладки
    /// </summary>
    Debug = 0,
    /// <summary>
    /// Информационное сообщение
    /// </summary>
    Info = 1,
    /// <summary>
    /// Сообщение предупреждения
    /// </summary>
    Warn = 2,
    /// <summary>
    /// Сообщение ошибки
    /// </summary>
    Error = 3,
    /// <summary>
    /// Сообщение предупреждения.
    /// </summary>
    Alert = 4,
    /// <summary>
    /// Критическое сообщение
    /// </summary>
    Critical = 5,
    /// <summary>
    /// Сообщение о чрезвычайном событии
    /// </summary>
    Emergency = 6
}
