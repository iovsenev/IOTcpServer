using IOTcpServer.Core.Infrastructure;

namespace IOTcpServer.Core.Events;

internal class ServerCallbacks
{
    private Func<SyncRequest, SyncResponse>? _SyncRequestReceived = null;
    private Func<SyncRequest, Task<SyncResponse>>? _SyncRequestReceivedAsync = null;

    /// <summary>
    /// Instantiate.
    /// </summary>
    public ServerCallbacks()
    {

    }

    /// <summary>
    /// Обратный вызов при получении синхронного запроса, требующего ответа.
    /// </summary>
    [Obsolete("Please migrate to async methods.")]
    public Func<SyncRequest, SyncResponse>? SyncRequestReceived
    {
        get
        {
            return _SyncRequestReceived;
        }
        set
        {
            _SyncRequestReceived = value;
        }
    }

    /// <summary>
    /// Обратный вызов при получении синхронного запроса, требующего ответа.
    /// </summary>
    public Func<SyncRequest, Task<SyncResponse>>? SyncRequestReceivedAsync
    {
        get
        {
            return _SyncRequestReceivedAsync;
        }
        set
        {
            _SyncRequestReceivedAsync = value;
        }
    }

    internal SyncResponse HandleSyncRequestReceived(SyncRequest req)
    {
        SyncResponse? ret = null;

#pragma warning disable CS0618 // Type or member is obsolete
        if (SyncRequestReceived != null)
        {
            try
            {
                ret = SyncRequestReceived(req);
            }
            catch (Exception)
            {

            }
        }
#pragma warning restore CS0618 // Type or member is obsolete

        return ret;
    }

    internal async Task<SyncResponse> HandleSyncRequestReceivedAsync(SyncRequest req)
    {
        SyncResponse ret = null;

        if (SyncRequestReceivedAsync != null)
        {
            try
            {
                ret = await SyncRequestReceivedAsync(req);
            }
            catch (Exception)
            {

            }
        }

        return ret;
    }
}