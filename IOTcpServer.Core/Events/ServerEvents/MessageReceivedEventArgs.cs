using IOTcpServer.Core.Infrastructure;

namespace IOTcpServer.Core.Events.ServerEvents;

/// <summary>
/// Аргументы события при получении сообщения.
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    internal MessageReceivedEventArgs(ServerClient client, Dictionary<string, object>? metadata, byte[] data)
    {
        Client = client;
        Metadata = metadata;
        Data = data;
    }
    /// <summary>
    /// Метаданные клиента.
    /// </summary>
    public ServerClient Client { get; }

    /// <summary>
    /// Метаданные, полученные от конечной точки.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; }

    /// <summary>
    /// Данные, полученные от конечной точки.
    /// </summary>
    public byte[] Data { get; }
}