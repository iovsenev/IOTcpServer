using IOTcpServer.Core.Constants;
using IOTcpServer.Core.CustomExceptions;
using IOTcpServer.Core.Events;
using IOTcpServer.Core.Helpers;
using IOTcpServer.Core.Settings;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace IOTcpServer.Core.Infrastructure;

public class ServerClient : IDisposable
{
    private const string Header = ConstantsString.Header;
    private readonly TcpClient _tcpClient;
    private readonly MessageBuilder _messageBuilder;
    private readonly ServerSettings _settings;
    private readonly ServerEvents _events;

    private NetworkStream _networkStream;
    private SslStream? _sslStream;
    private Stream _dataStream;

    private EndPoint _ipPort;

    internal SemaphoreSlim WriteLock = new SemaphoreSlim(1, 1);
    internal SemaphoreSlim ReadLock = new SemaphoreSlim(1, 1);

    internal CancellationTokenSource TokenSource;
    internal CancellationToken Token;

    public ServerClient(
        TcpClient tcpClient,
        MessageBuilder messageBuilder,
        ServerEvents events,
        ServerSettings settings)
    {
        _tcpClient = tcpClient;
        _messageBuilder = messageBuilder;
        _events = events;
        _settings = settings;

        _ipPort = _tcpClient.Client.RemoteEndPoint ?? new IPEndPoint(IPAddress.None, 0);
        _networkStream = _tcpClient.GetStream();
        _dataStream = _networkStream;
        TokenSource = new CancellationTokenSource();
        Token = TokenSource.Token;
    }
    public bool Authenticated { get; set; } = false;
    internal ServerEvents Events { get => _events; }
    internal byte[] SendBuffer { get; set; } = new byte[65536];

    internal Task? DataReceiver { get; set; }

    public Guid Id { get; set; } = Guid.NewGuid();

    public string IpPort
    {
        get
        {
            return _ipPort.ToString() ?? string.Empty;
        }
    }

    public object? Metadata { get; set; } = null;

    internal TcpClient TcpClient
    {
        get
        {
            return _tcpClient;
        }
    }

    internal NetworkStream NetworkStream
    {
        get
        {
            return _networkStream;
        }
        set
        {
            _networkStream = value;
            if (_networkStream != null)
            {
                _dataStream = _networkStream;
            }
        }
    }

    internal SslStream? SslStream
    {
        get
        {
            return _sslStream;
        }
        set
        {
            _sslStream = value;
            if (_sslStream != null)
            {
                _dataStream = _sslStream;
            }
        }
    }

    internal Stream DataStream
    {
        get
        {
            return _dataStream;
        }
    }

    internal async Task<bool> SendInternalAsync(Message msg, long contentLength, Stream? stream, CancellationToken token)
    {
        if (msg == null) throw new ArgumentNullException(nameof(msg));


        if (token == default(CancellationToken))
        {
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, Token);
            token = linkedCts.Token;
        }

        await WriteLock.WaitAsync(token).ConfigureAwait(false);

        try
        {
            await SendHeadersAsync(msg, token).ConfigureAwait(false);
            if (contentLength > 0)
            {
                if (stream == null || !stream.CanRead)
                {
                    throw new ArgumentException("Cannot read from supplied stream.");
                }
                await SendDataStreamAsync(contentLength, stream, token).ConfigureAwait(false);
            }

            _events.HandleClientSentMessage(this, new(contentLength));
            return true;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception e)
        {
            _settings.Logger?.Invoke(Severity.Error, $"{ConstantsString.Header} failed to write message to {this.ToString()}: {e.Message}");
            //_events.HandleExceptionEncountered(this, new ExceptionEventArgs(e));
            return false;
        }
        finally
        {
            WriteLock.Release();
        }
    }
    private async Task SendHeadersAsync(Message msg, CancellationToken token)
    {
        byte[] headerBytes = _messageBuilder.GetHeaderBytes(msg);
        await DataStream.WriteAsync(headerBytes, 0, headerBytes.Length, token).ConfigureAwait(false);
        await DataStream.FlushAsync(token).ConfigureAwait(false);
    }
    private async Task SendDataStreamAsync(long contentLength, Stream stream, CancellationToken token)
    {
        if (contentLength <= 0) return;

        long bytesRemaining = contentLength;
        int bytesRead = 0;

        while (bytesRemaining > 0)
        {
            if (bytesRemaining >= _settings.StreamBufferSize)
            {
                SendBuffer = new byte[_settings.StreamBufferSize];
            }
            else
            {
                SendBuffer = new byte[bytesRemaining];
            }

            bytesRead = await stream.ReadAsync(SendBuffer, 0, SendBuffer.Length, token).ConfigureAwait(false);
            if (bytesRead > 0)
            {
                await DataStream.WriteAsync(SendBuffer, 0, bytesRead, token).ConfigureAwait(false);
                bytesRemaining -= bytesRead;
            }
        }

        await DataStream.FlushAsync(token).ConfigureAwait(false);
    }
    internal async Task<SyncResponse> SendAndWaitInternalAsync(Message msg, int timeoutMs, long contentLength, Stream stream, CancellationToken token)
    {
        if (msg == null) throw new ArgumentNullException(nameof(msg));

        if (contentLength > 0)
        {
            if (stream == null || !stream.CanRead)
            {
                throw new ArgumentException("Cannot read from supplied stream.");
            }
        }

        await WriteLock.WaitAsync();

        SyncResponse? ret = null;
        AutoResetEvent responded = new AutoResetEvent(false);

        // Создать новый обработчик специально для этого сообщения.
        EventHandler<SyncResponseReceivedEventArgs> handler = (sender, e) =>
        {
            if (e.Message.ExpirationUtc == null)
                throw new ArgumentNullException($"{nameof(e.Message.ExpirationUtc)} must be not a null");

            if (e.Message.ConversationGuid == msg.ConversationGuid)
            {
                ret = new SyncResponse(e.Message.ConversationGuid, e.Message.ExpirationUtc.Value, e.Message.Metadata, e.Data);
                responded.Set();
            }
        };

        // Subscribe                
        _events.SyncResponseReceived += handler;

        try
        {
            await SendHeadersAsync(msg, token);
            await SendDataStreamAsync(contentLength, stream, token);
            _settings.Logger?.Invoke(Severity.Debug, $"{ConstantsString.Header} {this.ToString()} synchronous request sent: {msg.ConversationGuid}");

            _events.HandleClientSentMessage(this, new(contentLength));
        }
        catch (Exception e)
        {
            _settings.Logger?.Invoke(Severity.Error, $"{ConstantsString.Header} {ToString()} failed to write message: {e.Message}");
            _events.HandleExceptionEncountered(this, new ExceptionEventArgs(e));
            _events.SyncResponseReceived -= handler;
            throw;
        }
        finally
        {
            WriteLock.Release();
        }

        // Ожидание вызова responded.Set()
        responded.WaitOne(new TimeSpan(0, 0, 0, 0, timeoutMs));

        // Unsubscribe                
        _events.SyncResponseReceived -= handler;

        if (ret != null)
        {
            return ret;
        }
        else
        {
            _settings.Logger?.Invoke(Severity.Error, $"{ConstantsString.Header} synchronous response not received within the timeout window");
            throw new TimeoutException("A response to a synchronous request was not received within the timeout window.");
        }
    }
    internal async Task DataReceiving(CancellationToken token)
    {
        Message msg = await _messageBuilder.BuildFromStream(DataStream);

        if (msg == null)
        {
            await Task.Delay(30, token).ConfigureAwait(false);
            return;
        }

        if (!string.IsNullOrEmpty(_settings.AuthKey))
        {
            if (!Authenticated)
            {
                if (await AuthMessageHandle(msg, token))
                {
                    Authenticated = true;
                    _events.HandleAuthenticationSucceeded(this, new AuthenticationSucceededEventArgs(this));
                    return;
                }
                else
                {
                    _events.HandleAuthenticationFailed(this, new(IpPort));
                    throw new AuthenticatedFailedException($"Authenticate failed for {ToString()}");
                }
            }
            else if (msg.Status == MessageStatus.RegisterClient)
            {
                //_logger?.Invoke(Severity.Debug, $"{Header} client {ToString()} attempting to register GUID {msg.SenderGuid.ToString()}");
                //_clientManager.ReplaceGuid(client.Id, msg.SenderGuid);
                //_settings.Logger?.Invoke(Severity.Debug, $"{Header} updated client GUID from {Id} to {msg.SenderGuid}");

                //client.Id = msg.SenderGuid;
                //_events.HandleClientConnected(this, new ConnectionEventArgs(this));
                //continue;
            }
        }
        if (msg.Status == MessageStatus.Shutdown)
        {
            _settings.Logger?.Invoke(Severity.Debug, $"{Header} client {ToString()} is disconnecting");
            throw new ClientConnectionException("Client {ToString()} is disconnecting");
        }
        else if (msg.Status == MessageStatus.Removed)
        {
            _settings.Logger?.Invoke(Severity.Debug, $"{Header} no authentication material for {ToString()}");
            throw new ClientConnectionException("Client {ToString()} is disconnecting");
        }
        if (msg.SyncRequest)
        {
            _settings.Logger?.Invoke(Severity.Debug, $"{Header} {ToString()} synchronous request received: {msg.ConversationGuid.ToString()}");

            DateTime expiration = Common.GetExpirationTimestamp(msg);

            if (DateTime.UtcNow < expiration)
            {
                await SendSyncResponse(msg, token);
            }
            else
            {
                _settings.Logger?.Invoke(Severity.Debug, $"{Header} expired synchronous request received and discarded from {ToString()}");
            }
        }
        else if (msg.SyncResponse)
        {
            _settings.Logger?.Invoke(Severity.Debug, $"{Header} {ToString()} synchronous response received: {msg.ConversationGuid.ToString()}");
            byte[] msgData = await Common.ReadMessageDataAsync(msg, _settings.StreamBufferSize, token).ConfigureAwait(false);

            if (DateTime.UtcNow < msg.ExpirationUtc)
            {
                _events.HandleSyncResponseReceived(this, new SyncResponseReceivedEventArgs(msg, msgData));
            }
            else
            {
                _settings.Logger?.Invoke(Severity.Debug, Header + "expired synchronous response received and discarded from " + ToString());
            }
        }
        else
        {
            byte[] msgData;

            if (_events.IsUsingMessages)
            {
                msgData = await Common.ReadMessageDataAsync(msg, _settings.StreamBufferSize, token).ConfigureAwait(false);
                MessageReceivedEventArgs mr = new MessageReceivedEventArgs(this, msg.Metadata, msgData);
                await Task.Run(() => _events.HandleMessageReceived(this, mr), token);
            }
            else if (_events.IsUsingStreams)
            {
                StreamReceivedEventArgs sr;
                ServerStream ss;

                if (msg.DataStream == null)
                    throw new ArgumentNullException(nameof(msg.DataStream));

                if (msg.ContentLength >= _settings.MaxProxiedStreamSize)
                {
                    ss = new ServerStream(msg.ContentLength, msg.DataStream);
                    sr = new StreamReceivedEventArgs(this, msg.Metadata, msg.ContentLength, ss);
                    _events.HandleStreamReceived(this, sr);
                }
                else
                {
                    MemoryStream ms = await Common.DataStreamToMemoryStream(msg.ContentLength, msg.DataStream, _settings.StreamBufferSize, token).ConfigureAwait(false);
                    ss = new ServerStream(msg.ContentLength, ms);
                    sr = new StreamReceivedEventArgs(this, msg.Metadata, msg.ContentLength, ss);
                    await Task.Run(() => _events.HandleStreamReceived(this, sr), token);
                }
            }
            else
            {
                _settings.Logger?.Invoke(Severity.Error, Header + "event handler not set for either MessageReceived or StreamReceived");
                throw new ArgumentException("event handler not set for either MessageReceived or StreamReceived");
            }
        }
        _events.HandleClientReceivedMessage(this, new(msg.ContentLength));
    }

    private async Task SendSyncResponse(Message msg, CancellationToken token)
    {
        byte[] msgData = await Common.ReadMessageDataAsync(msg, _settings.StreamBufferSize, token).ConfigureAwait(false);
        Task unawaited = Task.Run(async () =>
        {
            SyncRequest syncReq = new SyncRequest(
                this,
                msg.ConversationGuid,
                msg.ExpirationUtc,
                msg.Metadata,
                msgData);

            SyncResponse? syncResp = null;

            syncResp = await _events.HandleSyncRequestReceivedAsync(syncReq);
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
                await SendInternalAsync(respMsg, contentLength, stream, token).ConfigureAwait(false);
            }
        }, token);
    }

    private async Task<bool> AuthMessageHandle(Message message, CancellationToken token)
    {
        _settings.Logger?.Invoke(Severity.Debug, $"{ConstantsString.Header} message received from unauthenticated endpoint {ToString()}");

        int contentLength = 0;
        Stream? authStream = null;

        if (message.Status == MessageStatus.AuthRequested)
        {
            _events.HandleAuthenticationRequested(this, new AuthenticationRequestedEventArgs(IpPort));

            if (message.AuthKey != null && message.AuthKey.Length > 0)
            {
                string clientPsk = Encoding.UTF8.GetString(message.AuthKey).Trim();
                if (_settings.AuthKey.Trim().Equals(clientPsk))
                {
                    _settings.Logger?.Invoke(Severity.Debug, $"{ConstantsString.Header} client {ToString()} attempting to register GUID {message.SenderGuid.ToString()}");
                    _events.HandleReplaceClientGuid(this, new(Id, message.SenderGuid));
                    _settings.Logger?.Invoke(Severity.Debug, $"{ConstantsString.Header} updated client GUID from {Id} to {message.SenderGuid}");
                    Id = message.SenderGuid;

                    _settings.Logger?.Invoke(Severity.Debug, $"{ConstantsString.Header} Authentication successful for {ToString()}");
                    _events.HandleClientRemoveUnauthenticated(this, new(Id));


                    var data = Encoding.UTF8.GetBytes("Authentication successful");
                    Common.BytesToStream(data, 0, out contentLength, out authStream);
                    var authMsg = _messageBuilder.ConstructNew(contentLength, authStream, false, false, null);
                    authMsg.Status = MessageStatus.AuthSuccess;
                    await SendInternalAsync(authMsg, 0, null, token).ConfigureAwait(false);
                    return true;
                }
                else
                {
                    _settings.Logger?.Invoke(Severity.Warn, $"{ConstantsString.Header} no authentication material for {ToString()}");
                    return false;
                }
            }
        }

        // decline and terminate
        _settings.Logger?.Invoke(Severity.Warn, $"{ConstantsString.Header} no authentication material for {ToString()}");
        return false;
    }

    public void Dispose()
    {
        while (DataReceiver?.Status == TaskStatus.Running)
        {
            Task.Delay(30).Wait();
        }

        Console.WriteLine(DataReceiver?.Status);

        if (TokenSource != null)
        {
            if (!TokenSource.IsCancellationRequested)
            {
                TokenSource.Cancel();
                TokenSource.Dispose();
            }
        }

        _sslStream?.Close();
        _networkStream?.Close();

        if (_tcpClient != null)
        {
            _tcpClient.Close();
            _tcpClient.Dispose();
        }

    }

    public override string ToString()
    {
        string ret = "[";
        ret += Id.ToString() + "|" + IpPort;
        ret += "]";
        return ret;
    }
}