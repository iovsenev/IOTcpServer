using IOTcpServer.Core.Constants;
using IOTcpServer.Core.Events;
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
public class IoTcpServer : IDisposable
{
    private const string Header = "[IOTcpServer]";

    private readonly ServerSettings _settings;

    private ServerCallbacks _callbacks;
    private ServerEvents _events;
    private ServerStatistics _statistics;

    private MessageBuilder _messageBuilder;
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
        _messageBuilder = new();
        _clientManager = new();
        _tokenSource = new();
        _callbacks = new();
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

        if (!_events.IsUsingMessages && !_events.IsUsingStreams)
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

    #region Connection client
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
                Message removeMsg = new Message(client.DataStream);
                removeMsg.Status = status;
                await client.SendInternalAsync(removeMsg, 0, null, token).ConfigureAwait(false);
            }

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
        string clientIp = remoteEndPoint is not null ? remoteEndPoint.Address.ToString() : string.Empty;
        if (_settings.PermittedIPs.Count > 0 && !_settings.PermittedIPs.Contains(clientIp))
        {
            _settings.Logger?.Invoke(Severity.Info, Header + "rejecting connection from " + clientIp + " (not permitted)");
            tcpClient.Close();
            return null;
        }

        if (_settings.BlockedIPs.Count > 0 && _settings.BlockedIPs.Contains(clientIp))
        {
            _settings.Logger?.Invoke(Severity.Info, Header + "rejecting connection from " + clientIp + " (blocked)");
            tcpClient.Close();
            return null;
        }

        ServerClient client = new ServerClient(tcpClient, _messageBuilder, _settings.StreamBufferSize, _settings.Logger);
        client.SendBuffer = new byte[_settings.StreamBufferSize];

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

            byte[] data = Encoding.UTF8.GetBytes("Authentication required");
            Message authMsg = new Message(client.DataStream);
            authMsg.Status = MessageStatus.AuthRequired;
            var sendBytes = await client.SendInternalAsync(authMsg, 0, null, token).ConfigureAwait(false);
            _statistics.IncrementSentMessages();
            _statistics.AddSentBytes(sendBytes);
        }

        _settings.Logger?.Invoke(Severity.Debug, $"{Header} starting data receiver for {client.ToString()}");
        client.DataReceiver = Task.Run(() => DataReceiver(client, token), token);
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

    private bool IsClientConnected(ServerClient client)
    {
        if (client != null && client.TcpClient != null)
        {
            var state = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                    .FirstOrDefault(x =>
                        x.LocalEndPoint.Equals(client.TcpClient.Client.LocalEndPoint)
                        && x.RemoteEndPoint.Equals(client.TcpClient.Client.RemoteEndPoint));

            if (state == default(TcpConnectionInformation)
                || state.State == TcpState.Unknown
                || state.State == TcpState.FinWait1
                || state.State == TcpState.FinWait2
                || state.State == TcpState.Closed
                || state.State == TcpState.Closing
                || state.State == TcpState.CloseWait)
            {
                return false;
            }

            byte[] tmp = new byte[1];
            bool success = false;

            try
            {
                client.WriteLock.Wait();
                client.TcpClient.Client.Send(tmp, 0, 0);
                success = true;
            }
            catch (SocketException se)
            {
                if (se.NativeErrorCode.Equals(10035)) success = true;
            }
            catch (Exception)
            {
            }
            finally
            {
                if (client != null)
                {
                    client.WriteLock.Release();
                }
            }

            if (success) return true;

            try
            {
                client.WriteLock.Wait();

                if ((client.TcpClient.Client.Poll(0, SelectMode.SelectWrite))
                    && (!client.TcpClient.Client.Poll(0, SelectMode.SelectError)))
                {
                    byte[] buffer = new byte[1];
                    if (client.TcpClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (client != null) client.WriteLock.Release();
            }
        }
        else
        {
            return false;
        }
    }

    #endregion Connection Client

    private async Task DataReceiver(ServerClient client, CancellationToken token)
    {
        while (true)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (!IsClientConnected(client)) break;

                Message msg = await _messageBuilder.BuildFromStream(client.DataStream);

                if (msg == null)
                {
                    await Task.Delay(30, token).ConfigureAwait(false);
                    continue;
                }

                if (!string.IsNullOrEmpty(_settings.AuthKey))
                {
                    if (_clientManager.ExistsUnauthenticatedClient(client.Id))
                    {
                        if (await AuthMessageHandle(msg, client, token))
                            continue;
                        else
                            break;
                    }
                    else if (msg.Status == MessageStatus.RegisterClient)
                    {
                        _settings.Logger?.Invoke(Severity.Debug, $"{Header} client {client.ToString()} attempting to register GUID {msg.SenderGuid.ToString()}");
                        _clientManager.ReplaceGuid(client.Id, msg.SenderGuid);
                        _settings.Logger?.Invoke(Severity.Debug, $"{Header} updated client GUID from {client.Id} to {msg.SenderGuid}");

                        client.Id = msg.SenderGuid;
                        _events.HandleClientConnected(this, new ConnectionEventArgs(client));
                        continue;
                    }
                }
                if (msg.Status == MessageStatus.Shutdown)
                {
                    _settings.Logger?.Invoke(Severity.Debug, $"{Header} client {client.ToString()} is disconnecting");
                    _clientManager.Remove(client.Id);
                    break;
                }
                else if (msg.Status == MessageStatus.Removed)
                {
                    _settings.Logger?.Invoke(Severity.Debug, $"{Header} no authentication material for {client.ToString()}");
                    _clientManager.Remove(client.Id);
                    break;
                }
                if (msg.SyncRequest)
                {
                    _settings.Logger?.Invoke(Severity.Debug, $"{Header} {client.ToString()} synchronous request received: {msg.ConversationGuid.ToString()}");

                    DateTime expiration = Common.GetExpirationTimestamp(msg);
                    byte[] msgData = await Common.ReadMessageDataAsync(msg, _settings.StreamBufferSize, token).ConfigureAwait(false);

                    if (DateTime.UtcNow < expiration)
                    {
                        Task unawaited = Task.Run(async () =>
                        {
                            SyncRequest syncReq = new SyncRequest(
                                client,
                                msg.ConversationGuid,
                                msg.ExpirationUtc,
                                msg.Metadata,
                                msgData);

                            SyncResponse? syncResp = null;

                            syncResp = await _callbacks.HandleSyncRequestReceivedAsync(syncReq);
                            if (syncResp != null)
                            {
                                Common.BytesToStream(syncResp.Data, 0, out int contentLength, out Stream stream);
                                Message respMsg = _messageBuilder.ConstructNew(
                                     contentLength,
                                     stream,
                                     false,
                                     true,
                                     msg.ExpirationUtc,
                                     syncResp.Metadata);

                                respMsg.ConversationGuid = msg.ConversationGuid;
                                await client.SendInternalAsync(respMsg, contentLength, stream, token).ConfigureAwait(false);
                            }
                        }, token);
                    }
                    else
                    {
                        _settings.Logger?.Invoke(Severity.Debug, Header + "expired synchronous request received and discarded from " + client.ToString());
                    }
                }
                else if (msg.SyncResponse)
                {
                    // Не нужно изменять срок действия сообщения; он копируется из запроса, который был установлен этим узлом
                    // DateTime expiration = Common.GetExpirationTimestamp(msg);
                    _settings.Logger?.Invoke(Severity.Debug, Header + client.ToString() + " synchronous response received: " + msg.ConversationGuid.ToString());
                    byte[] msgData = await Common.ReadMessageDataAsync(msg, _settings.StreamBufferSize, token).ConfigureAwait(false);

                    if (DateTime.UtcNow < msg.ExpirationUtc)
                    {
                        lock (_SyncResponseLock)
                        {
                            _SyncResponseReceived?.Invoke(this, new SyncResponseReceivedEventArgs(msg, msgData));
                        }
                    }
                    else
                    {
                        _settings.Logger?.Invoke(Severity.Debug, Header + "expired synchronous response received and discarded from " + client.ToString());
                    }
                }
                else
                {
                    byte[] msgData;

                    if (_events.IsUsingMessages)
                    {
                        msgData = await Common.ReadMessageDataAsync(msg, _settings.StreamBufferSize, token).ConfigureAwait(false);
                        MessageReceivedEventArgs mr = new MessageReceivedEventArgs(client, msg.Metadata, msgData);
                        await Task.Run(() => _events.HandleMessageReceived(this, mr), token);
                    }
                    else if (_events.IsUsingStreams)
                    {
                        StreamReceivedEventArgs sr;
                        ServerStream ss;

                        if (msg.ContentLength >= _settings.MaxProxiedStreamSize)
                        {
                            ss = new ServerStream(msg.ContentLength, msg.DataStream);
                            sr = new StreamReceivedEventArgs(client, msg.Metadata, msg.ContentLength, ss);
                            _events.HandleStreamReceived(this, sr);
                        }
                        else
                        {
                            MemoryStream ms = await Common.DataStreamToMemoryStream(msg.ContentLength, msg.DataStream, _settings.StreamBufferSize, token).ConfigureAwait(false);
                            ss = new ServerStream(msg.ContentLength, ms);
                            sr = new StreamReceivedEventArgs(client, msg.Metadata, msg.ContentLength, ss);
                            await Task.Run(() => _events.HandleStreamReceived(this, sr), token);
                        }
                    }
                    else
                    {
                        _settings.Logger?.Invoke(Severity.Error, Header + "event handler not set for either MessageReceived or StreamReceived");
                        break;
                    }
                }

                _statistics.IncrementReceivedMessages();
                _statistics.AddReceivedBytes(msg.ContentLength);
                _clientManager.UpdateClientLastSeen(client.Id, DateTime.UtcNow);
            }
            catch (ObjectDisposedException ode)
            {
                HandleException(ode, "object disposed exception encountered");
                await DisconnectClientAsync(client.Id, MessageStatus.Failure, false, token).ConfigureAwait(false);
                break;
            }
            catch (TaskCanceledException tce)
            {
                HandleException(tce, "task canceled exception encountered");
                await DisconnectClientAsync(client.Id, MessageStatus.Failure, false, token).ConfigureAwait(false);
                break;
            }
            catch (OperationCanceledException oce)
            {
                HandleException(oce, "operation canceled exception encountered");
                await DisconnectClientAsync(client.Id, MessageStatus.Failure, false, token).ConfigureAwait(false);
                break;
            }
            catch (IOException ioe)
            {
                HandleException(ioe, "IO exception encountered");
                await DisconnectClientAsync(client.Id, MessageStatus.Failure, false, token).ConfigureAwait(false);
                break;
            }
            catch (Exception e)
            {
                HandleException(e, $"data receiver exception for  {client.ToString()}: {e.Message}");
                await DisconnectClientAsync(client.Id, MessageStatus.Failure, false, token).ConfigureAwait(false);
                break;
            }
        }
    }


    private async Task<bool> AuthMessageHandle(Message message, ServerClient client, CancellationToken token)
    {
        _settings.Logger?.Invoke(Severity.Debug, $"{ConstantsString.Header} message received from unauthenticated endpoint {client.ToString()}");

        int contentLength = 0;
        Stream? authStream = null;

        if (message.Status == MessageStatus.AuthRequested)
        {
            _events.HandleAuthenticationRequested(this, new AuthenticationRequestedEventArgs(client.IpPort));

            if (message.AuthKey != null && message.AuthKey.Length > 0)
            {
                string clientPsk = Encoding.UTF8.GetString(message.AuthKey).Trim();
                if (_settings.AuthKey.Trim().Equals(clientPsk))
                {
                    _settings.Logger?.Invoke(Severity.Debug, $"{ConstantsString.Header} client {client.ToString()} attempting to register GUID {message.SenderGuid.ToString()}");
                    _clientManager.ReplaceGuid(client.Id, message.SenderGuid);
                    _settings.Logger?.Invoke(Severity.Debug, $"{ConstantsString.Header} updated client GUID from {client.Id} to {message.SenderGuid}");
                    client.Id = message.SenderGuid;

                    _settings.Logger?.Invoke(Severity.Debug, $"{ConstantsString.Header} no authentication material for {client.ToString()}");
                    _clientManager.RemoveUnauthenticatedClient(client.Id);
                    _events.HandleAuthenticationSucceeded(this, new AuthenticationSucceededEventArgs(client));

                    var data = Encoding.UTF8.GetBytes("Authentication successful");
                    Common.BytesToStream(data, 0, out contentLength, out authStream);
                    var authMsg = _messageBuilder.ConstructNew(contentLength, authStream, false, false, null);
                    authMsg.Status = MessageStatus.AuthSuccess;
                    await client.SendInternalAsync(authMsg, 0, null, token).ConfigureAwait(false);
                    return true;
                }
                else
                {
                    _settings.Logger?.Invoke(Severity.Warn, $"{ConstantsString.Header} no authentication material for {client.ToString()}");
                    await DisconnectClientAsync(client.Id, MessageStatus.AuthFailure, false, token).ConfigureAwait(false);
                    return false;
                }
            }
        }

        // decline and terminate
        _settings.Logger?.Invoke(Severity.Warn, $"{ConstantsString.Header} no authentication material for {client.ToString()}");
        await DisconnectClientAsync(client.Id, MessageStatus.AuthFailure, false, token).ConfigureAwait(false);
        return false;
    }
    private void HandleException(Exception ode, string message)
    {
        _settings.Logger?.Invoke(Severity.Debug, $"{Header} {message}");
        _events.HandleExceptionEncountered(this, new ExceptionEventArgs(ode));
    }

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

            DisconnectClientsAsync(MessageStatus.Shutdown).Wait();

            if (_listener != null)
            {
                if (_listener.Server != null)
                {
                    _listener.Server.Close();
                    _listener.Server.Dispose();
                }
            }

            if (_sslCertificate != null)
            {
                _sslCertificate.Dispose();
            }

            if (_clientManager != null)
            {
                _clientManager.Dispose();
            }

            _events = null;
            _callbacks = null;
            _statistics = null;

            _listener = null;

            _sslCertificate = null;

            _tokenSource = null;

            _acceptConnections = null;
            _monitorClients = null;

            _isListening = false;
        }
    }
}