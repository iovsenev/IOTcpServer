namespace IOTcpServer.Core.Events;

internal class ClientRemoveUnauthenticatedEventArgs
{
    public ClientRemoveUnauthenticatedEventArgs(Guid clientId)
    {
        ClientId = clientId;
    }
    public Guid ClientId { get; }
}