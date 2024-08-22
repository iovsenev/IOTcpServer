namespace IOTcpServer.Core.Events;

/// <summary>
/// Аргументы события, когда от клиента запрашивается аутентификация.
/// </summary>
public class AuthenticationRequestedEventArgs
{
    public AuthenticationRequestedEventArgs(string ipPort)
    {
        IpPort = ipPort;

    }

    /// <summary>
    /// IP:порт клиента.
    /// </summary>
    public string IpPort { get; }
}