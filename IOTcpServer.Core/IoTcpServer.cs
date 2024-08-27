using IOTcpServer.Core.Constants;
using IOTcpServer.Core.CustomExceptions;
using IOTcpServer.Core.Events.ClientEvents;
using IOTcpServer.Core.Events.ServerEvents;
using IOTcpServer.Core.Extensions;
using IOTcpServer.Core.Helpers;
using IOTcpServer.Core.Infrastructure;
using IOTcpServer.Core.Settings;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace IOTcpServer.Core;
public class IoTcpServer :  ITcpServer
{
    private const string Header = "[IOTcpServer]";

    private readonly ServerSettings _settings;

    private ServerEvents _events;
    private ServerStatistics _statistics;

    private ServerClientsManager _clientManager;

    private int _connections = 0;
    private bool _isListening = false;

    private Mode _mode;
    private TcpListener _listener;

    private X509Certificate2? _sslCertificate;

    private CancellationTokenSource _tokenSource;
    private CancellationToken _token;

    private Task? _acceptConnections = null;
    private Task? _monitorClients = null;

    public IoTcpServer(ServerSettings serverSettings)
    {
        _settings = serverSettings;
        _events = new();
        _statistics = new();
        _clientManager = new();
        _tokenSource = new();
        _token = _tokenSource.Token;

        if (_settings.IsSsl)
        {
            _mode = Mode.Ssl;
            if (string.IsNullOrEmpty(_settings.CertPass))
            {
                _sslCertificate = new X509Certificate2(_settings.CertFilePath);
            }
            else
            {
                _sslCertificate = new X509Certificate2(_settings.CertFilePath, _settings.CertPass);
            }
        }
        else _mode = Mode.Tcp;
        _listener = new TcpListener(_settings.ListenIp, _settings.ListenPort);
    }



    public ServerSettings Settings => _settings;
    public ServerEvents Events { get => _events; }
    public ServerStatistics Statistics { get => _statistics; }
    public int Connections { get => _connections; }
    public bool IsListening { get => _isListening; }

    public void Start()
    {
        if (_isListening) throw new InvalidOperationException("Server is already running.");

        if (_events.IsUsingMessage )
            throw new InvalidOperationException("One of either 'MessageReceived' or 'StreamReceived' events must first be set.");

        if (_mode == Mode.Tcp)
        {
            _settings.Logger?.Invoke(Severity.Info, Header + "starting on " + Settings.ListenIp + ":" + Settings.ListenPort);
        }
        else if (_mode == Mode.Ssl)
        {
            _settings.Logger?.Invoke(Severity.Info, Header + "starting with SSL on " + Settings.ListenIp + ":" + Settings.ListenPort);
        }
        else
        {
            throw new ArgumentException("Unknown mode: " + _mode.ToString());
        }

        _listener.Start();

        _acceptConnections = Task.Run(() => AcceptConnections(_token), _token); // sets _IsListening
        if (Settings.IdleClientTimeoutSeconds > 0)
            _monitorClients = Task.Run(() => MonitorForIdleClients(_token), _token);
        _events.HandleServerStarted(this, EventArgs.Empty);
    }

    public void Stop()
    {
        if (!_isListening) throw new InvalidOperationException("WatsonTcpServer is not running.");

        try
        {
            _isListening = false;
            _listener.Stop();
            _tokenSource.Cancel();

            _settings.Logger?.Invoke(Severity.Info, Header + "stopped");
            _events.HandleServerStopped(this, EventArgs.Empty);
        }
        catch (Exception e)
        {
            _events.HandleExceptionEncountered(this, new ExceptionEventArgs(e));
            throw;
        }
    }

    public async Task<bool> SendAsync(
        Guid ClientId,
        string data,
        Dictionary<string, object>? metadata = null,
        int start = 0,
        CancellationToken token = default)
    {
        byte[] bytes = Array.Empty<byte>();
        if (!String.IsNullOrEmpty(data)) bytes = Encoding.UTF8.GetBytes(data);
        return await SendAsync(ClientId, bytes, metadata, start, token).ConfigureAwait(false);
    }

    public async Task<bool> SendAsync(
        Guid ClientId,
        byte[] data,
        Dictionary<string, object>? metadata = null,
        int start = 0,
        CancellationToken token = default)
    {
        if (data == null) data = Array.Empty<byte>();

        var contentLength = data.Length > 0 ? data.Length - start : 0;
        Stream stream = Common.BytesToStream(data, start);
        return await SendAsync(ClientId, contentLength, stream, metadata, token).ConfigureAwait(false);
    }

    public async Task<bool> SendAsync(
        Guid ClientId,
        long contentLength,
        Stream stream,
        Dictionary<string, object>? metadata = null,
        CancellationToken token = default)
    {
        if (contentLength < 0) throw new ArgumentException("Content length must be zero or greater.");
        if (token == default(CancellationToken)) token = _token;
        ServerClient? client = _clientManager.GetClient(ClientId);
        if (client == null)
        {
            _settings.Logger?.Invoke(Severity.Error, $"{Header} unable to find client {ClientId.ToString()}");
            throw new KeyNotFoundException("Unable to find client " + ClientId.ToString() + ".");
        }

        if (stream == null) stream = new MemoryStream(Array.Empty<byte>());
        Message msg = MessageBuilder.ConstructNew(
            contentLength,
            stream,
            metadata);

        var isSend = await client.SendInternalAsync(msg, token).ConfigureAwait(false);

        if (!isSend)
            return false;
        return true;
    }

    public IEnumerable<ServerClient> ListClients()
    {
        Dictionary<Guid, ServerClient> clients = _clientManager.AllClients();
        if (clients != null && clients.Count > 0)
        {
            foreach (KeyValuePair<Guid, ServerClient> client in clients)
            {
                yield return client.Value;
            }
        }
    }

    #region Connection client
    public bool IsClientConnected(Guid guid)
    {
        return _clientManager.ExistsClient(guid);
    }

    public async Task DisconnectAllClientsAsync(MessageStatus status = MessageStatus.Removed, bool sendNotice = true, CancellationToken token = default)
    {
        Dictionary<Guid, ServerClient> clients = _clientManager.AllClients();
        if (clients != null && clients.Count > 0)
        {
            foreach (KeyValuePair<Guid, ServerClient> client in clients)
            {
                await DisconnectClientAsync(client.Key, status, sendNotice, token).ConfigureAwait(false);
            }
        }
    }

    public async Task DisconnectClientAsync(Guid guid, MessageStatus status = MessageStatus.Removed, bool sendNotice = true, CancellationToken token = default)
    {
        ServerClient? client = _clientManager.GetClient(guid);
        if (client == null)
        {
            _settings.Logger?.Invoke(Severity.Error, Header + "unable to find client " + guid.ToString());
        }
        else
        {
            if (!_clientManager.ExistsClientTimedout(guid)) _clientManager.AddClientKicked(guid);

            if (sendNotice)
            {
                Message removeMsg = MessageBuilder.ConstructNew(
                    0,
                    null);
                removeMsg.Status = status;
                await client.SendInternalAsync(removeMsg, token).ConfigureAwait(false);
            }
            client.Events.ClientSentMessageEvent -= SentMessage;
            client.Events.ClientReceivedMessageEvent -= ReceivedMessage;
            client.Events.ClientAuthenticationSucceeded -= ClientAuthenticateSucceeded;
            client.Events.ClientAuthenticationFailed -= ClientAuthenticationFailed;
            client.Events.ClientDisconnectEvent -= ClientDisconnect;
            client.Events.ClientNotConnectedEvent -= ClientNotConnected;
            client.Events.ExceptionEncountered -= ExceptionEncountered;
            client.Events.ClientReplaceIdEvent -= ReplaceId;

            client.Dispose();
            _clientManager.Remove(guid);
        }
    }

    private async Task MonitorForIdleClients(CancellationToken token)
    {
        try
        {
            Dictionary<Guid, DateTime>? lastSeen = null;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                await Task.Delay(5000, _token).ConfigureAwait(false);

                if (_settings.IdleClientTimeoutSeconds > 0)
                {
                    lastSeen = _clientManager.AllClientsLastSeen();

                    if (lastSeen != null && lastSeen.Count > 0)
                    {
                        DateTime idleTimestamp = DateTime.UtcNow.AddSeconds(-1 * _settings.IdleClientTimeoutSeconds);

                        foreach (KeyValuePair<Guid, DateTime> curr in lastSeen)
                        {
                            if (curr.Value < idleTimestamp)
                            {
                                _clientManager.AddClientTimedout(curr.Key);
                                _settings.Logger?.Invoke(Severity.Debug, $"{Header} disconnecting client {curr.Key} due to idle timeout");
                                await DisconnectClientAsync(curr.Key, MessageStatus.Timeout, true);
                            }
                        }
                    }
                }
            }
        }
        catch (TaskCanceledException)
        {

        }
        catch (OperationCanceledException)
        {

        }
    }

    private async Task AcceptConnections(CancellationToken token)
    {
        _isListening = true;

        while (true)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (!_isListening && (_connections >= _settings.MaxConnections))
                {
                    await Task.Delay(100);
                    continue;
                }
                else if (!_isListening)
                {
                    _listener.Start();
                    _isListening = true;
                }

                var client = await AcceptAndValidate();
                if (client == null)
                    continue;
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_token, client.Token);

                IncrementConnection();

                InitializeClient(client, linkedCts);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                _settings.Logger?.Invoke(Severity.Error, Header + "listener exception: " + e.Message);
                _events.HandleExceptionEncountered(this, new ExceptionEventArgs(e));
                break;
            }
        }
    }

    private async Task<ServerClient?> AcceptAndValidate()
    {
        TcpClient tcpClient = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
        tcpClient.LingerState = new LingerOption(false, 10);
        tcpClient.NoDelay = _settings.NoDelay;

        if (_settings.KeepAliveSettings.EnableTcpKeepAlives) EnableKeepalives(tcpClient);
        var remoteEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
        if (remoteEndPoint == null)
        {
            _settings.Logger?.Invoke(Severity.Warn, $"{Header} Remote Ip address must be not null");
            return null;
        }
        string clientIp = remoteEndPoint.Address.ToString();

        if (_settings.PermittedIPs.Count > 0 && !_settings.PermittedIPs.Contains(clientIp))
        {
            _settings.Logger?.Invoke(Severity.Info, $"{Header} rejecting connection from {clientIp} (not permitted)");
            tcpClient.Close();
            return null;
        }

        if (_settings.BlockedIPs.Count > 0 && _settings.BlockedIPs.Contains(clientIp))
        {
            _settings.Logger?.Invoke(Severity.Info, $"{Header} rejecting connection from {clientIp} (blocked)");
            tcpClient.Close();
            return null;
        }

        ServerClient client = new ServerClient(tcpClient, _settings);
        client.Events.ClientSentMessageEvent += SentMessage;
        client.Events.ClientReceivedMessageEvent += ReceivedMessage;
        client.Events.ClientAuthenticationSucceeded += ClientAuthenticateSucceeded;
        client.Events.ClientAuthenticationFailed += ClientAuthenticationFailed;
        client.Events.ClientDisconnectEvent += ClientDisconnect;
        client.Events.ClientNotConnectedEvent += ClientNotConnected;
        client.Events.ExceptionEncountered += ExceptionEncountered;
        client.Events.ClientReplaceIdEvent += ReplaceId;


        _clientManager.AddClient(client.Id, client);
        _clientManager.AddClientLastSeen(client.Id);

        return client;
    }


    private void IncrementConnection()
    {
        Interlocked.Increment(ref _connections);

        if (_connections >= _settings.MaxConnections)
        {
            _settings.Logger?.Invoke(Severity.Info, Header + "maximum connections "
                + _settings.MaxConnections + " met (currently " + _connections + " connections), pausing");
            _isListening = false;
            _listener.Stop();
        }
    }

    private void InitializeClient(ServerClient client, CancellationTokenSource linkedCts)
    {
        Task unawaited;

        if (_mode == Mode.Tcp)
        {
            unawaited = Task.Run(() => FinalizeConnection(client, linkedCts.Token), linkedCts.Token);
        }
        else if (_mode == Mode.Ssl)
        {
            unawaited = ConnectWithSSL(client, linkedCts);
        }
        else
        {
            throw new ArgumentException("Unknown mode: " + _mode.ToString());
        }

        _settings.Logger?.Invoke(Severity.Debug, Header + "accepted connection from " + client.ToString());
    }

    private async Task FinalizeConnection(ServerClient client, CancellationToken token)
    {
        if (!String.IsNullOrEmpty(_settings.AuthKey))
        {

            _settings.Logger?.Invoke(Severity.Debug, $" {Header} requesting authentication material from {client.ToString()}");
            _clientManager.AddUnauthenticatedClient(client.Id);

            Message authMsg = MessageBuilder.ConstructNew(
                0,
                null
                );
            authMsg.Status = MessageStatus.AuthRequired;

            var isSent = await client.SendInternalAsync(authMsg, token).ConfigureAwait(false);
            if (!isSent) throw new OperationCanceledException($"Auth message do not sent to {client.ToString()}");
        }

        _settings.Logger?.Invoke(Severity.Debug, $"{Header} starting data receiver for {client.ToString()}");
        client.DataReceiver = Task.Run(() => client.DataReceive( token), token);
    }

    private Task ConnectWithSSL(ServerClient client, CancellationTokenSource linkedCts)
    {
        Task unawaited;
        if (_settings.AcceptInvalidCertificates)
        {
            client.SslStream = new SslStream(client.NetworkStream, false, _settings.SslConfiguration.ClientCertificateValidationCallback);
        }
        else
        {
            client.SslStream = new SslStream(client.NetworkStream, false);
        }

        unawaited = Task.Run(async () =>
        {
            bool success = await StartTls(client, linkedCts.Token).ConfigureAwait(false);
            if (success)
            {
                await FinalizeConnection(client, linkedCts.Token).ConfigureAwait(false);
            }
            else
            {
                _clientManager.RemoveClient(client.Id);
                _clientManager.RemoveClientLastSeen(client.Id);

                client.Dispose();
            }

        }, linkedCts.Token);

        return unawaited;
    }

    private async Task<bool> StartTls(ServerClient client, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            if (client.SslStream == null)
                throw new InvalidOperationException("SslStream is not actived");
            if (_sslCertificate == null)
                throw new ArgumentNullException(nameof(_sslCertificate));

            await client.SslStream.AuthenticateAsServerAsync(_sslCertificate,
                _settings.SslConfiguration.ClientCertificateRequired,
                _settings.TlsVersion.ToSslProtocols(),
                !_settings.AcceptInvalidCertificates).ConfigureAwait(false);

            if (!client.SslStream.IsEncrypted)
            {
                _settings.Logger?.Invoke(Severity.Error, $"{Header} stream from {client.ToString()} not encrypted");
                client.Dispose();
                Interlocked.Decrement(ref _connections);
                return false;
            }

            if (!client.SslStream.IsAuthenticated)
            {
                _settings.Logger?.Invoke(Severity.Error, $"{Header} stream from {client.ToString()} not authenticated");
                client.Dispose();
                Interlocked.Decrement(ref _connections);
                return false;
            }

            if (_settings.MutuallyAuthenticate && !client.SslStream.IsMutuallyAuthenticated)
            {
                _settings.Logger?.Invoke(Severity.Error, $"{Header} mutual authentication with {client.ToString()} ({_settings.TlsVersion}) failed");
                client.Dispose();
                Interlocked.Decrement(ref _connections);
                return false;
            }
        }
        catch (Exception e)
        {
            _settings.Logger?.Invoke(Severity.Error, $"{Header} disconnected during SSL/TLS establishment with {client.ToString()} ({_settings.TlsVersion}): {e.Message}");
            _events.HandleExceptionEncountered(this, new ExceptionEventArgs(e));

            client.Dispose();
            Interlocked.Decrement(ref _connections);
            return false;
        }

        return true;
    }

    private void EnableKeepalives(TcpClient client)
    {
        try
        {
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, false);
        }
        catch (Exception)
        {
            _settings.Logger?.Invoke(Severity.Error, $" -[{Header}]- keepalives not supported on this platform, disabled");
            _settings.KeepAliveSettings.EnableTcpKeepAlives = false;
        }
    }


    #endregion Connection Client


    private void HandleException(Exception ode, string message)
    {
        _settings.Logger?.Invoke(Severity.Debug, $"{Header} {message}");
        _events.HandleExceptionEncountered(this, new ExceptionEventArgs(ode));
    }

    #region Client event methods
    private void ExceptionEncountered(object? sender, ExceptionEventArgs e)
    {
        _events.HandleExceptionEncountered(this, new(e.Exception));
    }
    private void ClientNotConnected(object? sender, ClientnInformationEventArgs e)
    {
        _clientManager.Remove(e.Client.Id);
        _settings.Logger?.Invoke(Severity.Info, $"{Header} Client: {e.Client.ToString()} lost connection");
        e.Client.Events.ClientSentMessageEvent -= SentMessage;
        e.Client.Events.ClientReceivedMessageEvent -= ReceivedMessage;
        e.Client.Events.ClientAuthenticationSucceeded -= ClientAuthenticateSucceeded;
        e.Client.Events.ClientAuthenticationFailed -= ClientAuthenticationFailed;
        e.Client.Events.ClientDisconnectEvent -= ClientDisconnect;
        e.Client.Events.ClientNotConnectedEvent -= ClientNotConnected;
        e.Client.Events.ExceptionEncountered -= ExceptionEncountered;
        e.Client.Events.ClientReplaceIdEvent -= ReplaceId;
        _events.HandleClientDisconnected(this,new(e.Client, DisconnectReason.Shutdown));
        e.Client.Dispose();
    }
    private void SentMessage(object? sender, ClientSentMessageEventArgs e)
    {
        _statistics.IncrementSentMessages();
        _statistics.AddSentBytes(e.Message.ContentLength);
    }
    private void ReceivedMessage(object? sender, ClientReceivedMessageEventArgs e)
    {
        _statistics.IncrementReceivedMessages();
        _statistics.AddReceivedBytes(e.Message.ContentLength);
        _events.HandleMessageReceived(this,new(e.Client, e.Message.Metadata, e.Message.Data));
    }
    private void ReplaceId(object? sender, ClientReplaceIdEventArgs e)
    {
        _clientManager.ReplaceGuid(e.OldGuid, e.NewGuid);
    }
    private void ClientAuthenticateSucceeded(object? sender, ClientnInformationEventArgs e)
    {
       _clientManager.RemoveUnauthenticatedClient(e.Client.Id);
    }
    private async void ClientDisconnect(object? sender, ClientDisconnectEventArgs e)
    {
        await DisconnectClientAsync(e.Client.Id, e.Status);
    }
    private async void ClientAuthenticationFailed(object? sender, ClientnInformationEventArgs e)
    {
        _settings.Logger?.Invoke(Severity.Warn, $"{Header} Client {e.Client.ToString()} is not authenticated.");
        await DisconnectClientAsync(e.Client.Id, MessageStatus.AuthFailure);
    }
    #endregion Client event methods

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _settings.Logger?.Invoke(Severity.Info, Header + "disposing");

            if (_isListening) Stop();

            DisconnectAllClientsAsync(MessageStatus.Shutdown).Wait();

            if (_listener != null)
            {
                if (_listener.Server != null)
                {
                    _listener.Server.Close();
                    _listener.Server.Dispose();
                }
                _listener.Dispose();
            }

            if (_sslCertificate != null)
            {
                _sslCertificate.Dispose();
            }

            if (_clientManager != null)
            {
                _clientManager.Dispose();
            }

            _events.Dispose();
            _sslCertificate = null;
            _tokenSource.Dispose();
            _acceptConnections = null;
            _monitorClients = null;
            _isListening = false;
        }
    }
}