using IOTcpServer.Core.Infrastructure;

namespace IOTcpServer.Core.Events;

/// <summary>
/// Внутренние EventArgs для передачи аргументов для события SyncResponseReceived.
/// </summary>
internal class SyncResponseReceivedEventArgs
{
    /// <summary>
    /// Instantiate.
    /// </summary>
    /// <param name="msg">Message.</param>
    /// <param name="data">Data.</param>
    public SyncResponseReceivedEventArgs(Message msg, byte[] data)
    {
        Message = msg;
        Data = data;
    }

    /// <summary>
    /// Message.
    /// </summary>
    public Message Message { get; set; }

    /// <summary>
    /// Data.
    /// </summary>
    public byte[]? Data { get; set; } = null;
}