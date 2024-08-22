namespace IOTcpServer.Core.Events;

/// <summary>
/// Аргументы события для случая, когда клиент не проходит аутентификацию.
/// </summary>
public class AuthenticationFailedEventArgs
{
    internal AuthenticationFailedEventArgs(string ipPort)
    {
        IpPort = ipPort;
    }

    /// <summary>
    /// IP:порт клиента.
    /// </summary>
    public string IpPort { get; }
}