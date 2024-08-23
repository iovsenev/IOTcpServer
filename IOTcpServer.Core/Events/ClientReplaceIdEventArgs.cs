namespace IOTcpServer.Core.Events;

internal class ClientReplaceIdEventArgs
{
    public ClientReplaceIdEventArgs(Guid lastClientId, Guid newClientId)
    {
        LastClientId = lastClientId;
        NewClientId = newClientId;
    }

    public Guid LastClientId { get; }
    public Guid NewClientId { get; }
}