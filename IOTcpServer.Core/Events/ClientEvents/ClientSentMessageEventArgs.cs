using IOTcpServer.Core.Infrastructure;

namespace IOTcpServer.Core.Events.ClientEvents;

internal class ClientSentMessageEventArgs
{
    public ClientSentMessageEventArgs(ServerClient client, Message msg)
    {
        Client = client;
        Message = msg;
    }

    public ServerClient Client { get; }
    public Message Message { get; }
}