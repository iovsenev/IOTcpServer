namespace IOTcpServer.Core.Events;

internal class ClientUpdateLastSeenEventArgs
{
    public ClientUpdateLastSeenEventArgs(Guid clientId)
    {
        ClientId = clientId;
    }

    public Guid ClientId { get; }
}