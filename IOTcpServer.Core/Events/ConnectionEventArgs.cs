using IOTcpServer.Core.Infrastructure;

namespace IOTcpServer.Core.Events;

/// <summary>
/// Аргументы события при установлении соединения.
/// </summary>
public class ConnectionEventArgs : EventArgs
{
    internal ConnectionEventArgs(ServerClient client)
    {
        Client = client;
    }

    /// <summary>
    /// Метаданные клиента.
    /// </summary>
    public ServerClient Client { get; }
}