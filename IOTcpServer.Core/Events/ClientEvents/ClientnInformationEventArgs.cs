using IOTcpServer.Core.Infrastructure;

namespace IOTcpServer.Core.Events.ClientEvents;

internal class ClientnInformationEventArgs
{
    public ClientnInformationEventArgs(ServerClient client)
    {
        Client = client;
    }

    public ServerClient Client { get; }

}