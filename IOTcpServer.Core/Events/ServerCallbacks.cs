using IOTcpServer.Core.Infrastructure;

namespace IOTcpServer.Core.Events;

internal class ServerCallbacks
{
    private Func<SyncRequest, Task<SyncResponse>>? _syncRequestReceivedAsync = null;

    /// <summary>
    /// Instantiate.
    /// </summary>
    public ServerCallbacks()
    {

    }

    /// <summary>
    /// Обратный вызов при получении синхронного запроса, требующего ответа.
    /// </summary>
    public Func<SyncRequest, Task<SyncResponse>>? SyncRequestReceivedAsync
    {
        get
        {
            return _syncRequestReceivedAsync;
        }
        set
        {
            _syncRequestReceivedAsync = value;
        }
    }

    internal async Task<SyncResponse> HandleSyncRequestReceivedAsync(SyncRequest req)
    {
        SyncResponse ret;
        if (SyncRequestReceivedAsync == null)
            throw new InvalidOperationException(nameof(SyncRequestReceivedAsync));

        try
        {
            ret = await SyncRequestReceivedAsync(req);
            return ret;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(nameof(SyncRequestReceivedAsync));
        }

    }
}