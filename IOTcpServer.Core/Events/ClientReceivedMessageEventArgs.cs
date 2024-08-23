namespace IOTcpServer.Core.Events;

internal class ClientReceivedMessageEventArgs
{
    public ClientReceivedMessageEventArgs(long receivedBytes)
    {
        ReceivedBytes = receivedBytes;
    }

    internal long ReceivedBytes { get; }
}