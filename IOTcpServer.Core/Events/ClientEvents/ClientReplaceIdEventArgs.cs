namespace IOTcpServer.Core.Events.ClientEvents;

internal class ClientReplaceIdEventArgs
{
    public ClientReplaceIdEventArgs(Guid oldGuid, Guid newGuid)
    {
        OldGuid = oldGuid;
        NewGuid = newGuid;
    }

    public Guid OldGuid { get; }
    public Guid NewGuid { get; }
}