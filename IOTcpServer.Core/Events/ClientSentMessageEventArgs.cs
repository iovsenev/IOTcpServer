namespace IOTcpServer.Core.Events;

internal class ClientSentMessageEventArgs
{
    public ClientSentMessageEventArgs(long sendBytes)
    {
        SendBytes = sendBytes;
    }

    public long SendBytes { get; }
}