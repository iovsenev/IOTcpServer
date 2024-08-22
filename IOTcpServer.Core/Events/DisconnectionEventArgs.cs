using IOTcpServer.Core.Constants;
using IOTcpServer.Core.Infrastructure;

namespace IOTcpServer.Core.Events;

/// <summary>
/// Аргументы события при обнаружении отключения.
/// </summary>
public class DisconnectionEventArgs : EventArgs
{
    internal DisconnectionEventArgs(ServerClient client, DisconnectReason reason = DisconnectReason.Normal)
    {
        Client = client;
        Reason = reason;
    }

    /// <summary>
    /// Метаданные клиента.
    /// </summary>
    public ServerClient Client { get; }

    /// <summary>
    /// Причина отключения.
    /// </summary>
    public DisconnectReason Reason { get; }
}