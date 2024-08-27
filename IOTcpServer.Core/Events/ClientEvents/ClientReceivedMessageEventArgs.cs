using IOTcpServer.Core.Infrastructure;

namespace IOTcpServer.Core.Events.ClientEvents;

internal class ClientReceivedMessageEventArgs
{
    public ClientReceivedMessageEventArgs(ServerClient client, Message message)
    {
        Client = client;
        Message = message;
    }

    internal ServerClient Client { get; }
    internal Message Message { get; }
}