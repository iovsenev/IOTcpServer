using IOTcpServer.Core.Constants;
using IOTcpServer.Core.Events.ServerEvents;
using IOTcpServer.Core.Infrastructure;
using IOTcpServer.Core.Settings;

namespace IOTcpServer.Core;
public interface ITcpServer :IDisposable
{
    int Connections { get; }
    ServerEvents Events { get; }
    bool IsListening { get; }
    ServerSettings Settings { get; }
    ServerStatistics Statistics { get; }
    IEnumerable<ServerClient> ListClients();

    void Start();
    void Stop();
    Task DisconnectAllClientsAsync(MessageStatus status = MessageStatus.Removed, bool sendNotice = true, CancellationToken token = default);
    Task DisconnectClientAsync(Guid guid, MessageStatus status = MessageStatus.Removed, bool sendNotice = true, CancellationToken token = default);
    bool IsClientConnected(Guid guid);
    Task<bool> SendAsync(Guid ClientId, byte[] data, Dictionary<string, object>? metadata = null, int start = 0, CancellationToken token = default);
    Task<bool> SendAsync(Guid ClientId, long contentLength, Stream stream, Dictionary<string, object>? metadata = null, CancellationToken token = default);
    Task<bool> SendAsync(Guid ClientId, string data, Dictionary<string, object>? metadata = null, int start = 0, CancellationToken token = default);
    void Dispose();
}