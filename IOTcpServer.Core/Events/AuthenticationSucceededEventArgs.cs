using IOTcpServer.Core.Infrastructure;

namespace IOTcpServer.Core.Events;

public class AuthenticationSucceededEventArgs
{
    /// <summary>
    /// Создание.
    /// </summary>
    /// <param name="client">Client metadata.</param>
    public AuthenticationSucceededEventArgs(ServerClient client)
    {
        Client = client;
    }

    /// <summary>
    /// Метаданные клиента
    /// </summary>
    public ServerClient Client { get; }
}