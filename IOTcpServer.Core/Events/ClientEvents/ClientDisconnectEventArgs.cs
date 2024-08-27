using IOTcpServer.Core.Constants;
using IOTcpServer.Core.Infrastructure;

namespace IOTcpServer.Core.Events.ClientEvents;

internal class ClientDisconnectEventArgs
{
    public ClientDisconnectEventArgs(ServerClient client, MessageStatus status)
    {
        Client = client;
        Status = status;
    }

    public ServerClient Client { get; }
    public MessageStatus Status { get; }
}