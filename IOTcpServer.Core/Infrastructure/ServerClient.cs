using IOTcpServer.Core.Constants;
using IOTcpServer.Core.CustomExceptions;
using IOTcpServer.Core.Events.ClientEvents;
using IOTcpServer.Core.Events.ServerEvents;
using IOTcpServer.Core.Helpers;
using IOTcpServer.Core.Settings;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace IOTcpServer.Core.Infrastructure;

public class ServerClient : IDisposable
{
    private const string Header = ConstantsString.Header;
    private readonly TcpClient _tcpClient;

    private readonly ServerSettings _settings;
    private readonly ClientEvents _events;

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
        ServerSettings settings)
    {
        _tcpClient = tcpClient;

        _events = new();
        _settings = settings;
        Logger = _settings.Logger;
        SendBuffer = new byte[_settings.StreamBufferSize];

        _ipPort = _tcpClient.Client.RemoteEndPoint ?? new IPEndPoint(IPAddress.None, 0);
        _networkStream = _tcpClient.GetStream();
        _dataStream = _networkStream;
        TokenSource = new CancellationTokenSource();
        Token = TokenSource.Token;
    }
    public bool Authenticated { get; set; } = false;
    internal ClientEvents Events { get => _events; }
    internal byte[] SendBuffer { get; set; }

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

    public Action<Severity, string> Logger { get; }

    internal Stream DataStream
    {
        get
        {
            return _dataStream;
        }
    }

    internal async Task<bool> SendInternalAsync(Message msg, CancellationToken token)
    {
        var contentLength = msg.ContentLength;
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
                if (msg.DataStream == null || !msg.DataStream.CanRead)
                {
                    throw new ArgumentException("Cannot read from supplied stream.");
                }

                await SendDataStreamAsync(contentLength, msg.DataStream, token).ConfigureAwait(false);
            }

            _events.HandleClientSentMessage(this, new(this, msg));
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
        byte[] headerBytes = MessageBuilder.GetHeaderBytes(msg);
        if (msg.DataStream == null) msg.DataStream = new MemoryStream(Array.Empty<byte>());
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


    internal async Task DataReceive(CancellationToken token)
    {
        while (true)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (!IsClientConnected())
                {
                    _events.HandleClientNotConnected(this, new(this));
                    Dispose();
                    break;
                }
                Message msg = await MessageBuilder.BuildFromStream(DataStream);

                if (msg == null)
                {
                    await Task.Delay(30, token).ConfigureAwait(false);
                    continue;
                }
                await DataReceiving(msg, token);
            }
            catch (ObjectDisposedException ode)
            {
                HandleException(ode, "object disposed exception encountered");
                _events.HandleClientDisconnect(this, new(this, MessageStatus.Removed));
                break;
            }
            catch (TaskCanceledException tce)
            {
                HandleException(tce, "task canceled exception encountered");
                _events.HandleClientDisconnect(this, new(this, MessageStatus.Removed));
                break;
            }
            catch (OperationCanceledException oce)
            {
                HandleException(oce, "operation canceled exception encountered");
                _events.HandleClientDisconnect(this, new(this, MessageStatus.Removed));
                break;
            }
            catch (IOException ioe)
            {
                HandleException(ioe, "IO exception encountered");
                _events.HandleClientDisconnect(this, new(this, MessageStatus.Removed));
                break;
            }
            catch (Exception e)
            {
                HandleException(e, $"data receiver exception for  {ToString()}: {e.Message}");
                _events.HandleClientDisconnect(this, new(this, MessageStatus.Removed));
                break;
            }
        }
    }
    internal async Task DataReceiving(Message message, CancellationToken token)
    {
        if (!string.IsNullOrEmpty(_settings.AuthKey)
            && !Authenticated
            && message.Status != MessageStatus.AuthRequested)
        {
            _events.HandleClientAuthenticationFailed(this, new(this));
            return;
        }
        switch (message.Status)
        {
            case MessageStatus.AuthRequested:
                var isSuccess = await ReceiveAuthMessage(message, token);

                if (!isSuccess)
                    _events.HandleClientAuthenticationFailed(this, new(this));
                Authenticated = true;
                _events.HandleClientAuthenticationSucceeded(this, new ClientnInformationEventArgs(this));
                break;

            case MessageStatus.RegisterClient:
                break;

            case MessageStatus.Normal:
                await ReceiveNormalMessage(message, token);
                break;

            case MessageStatus.Timeout:
                _settings.Logger?.Invoke(Severity.Debug, $"{Header} no authentication material for {ToString()}");
                _events.HandleClientDisconnect(this, new(this,MessageStatus.Timeout));
                break;

            case MessageStatus.Failure:
                break;

            case MessageStatus.Shutdown:
                _settings.Logger?.Invoke(Severity.Debug, $"{Header} client {ToString()} is disconnecting");
                _events.HandleClientDisconnect(this, new(this, MessageStatus.Shutdown));
                break;

            case MessageStatus.Heartbeat:
                break;

            case MessageStatus.Success:
                break;

            case MessageStatus.Removed:
                _settings.Logger?.Invoke(Severity.Debug, $"{Header} no authentication material for {ToString()}");
                throw new ClientConnectionException("Client {ToString()} is disconnecting");
            default:
                Logger?.Invoke(Severity.Debug, $"{Header} not identified message status ");
                break;
        }


        _events.HandleClientReceivedMessage(this, new(this, message));
    }

    private async Task<bool> ReceiveAuthMessage(Message message, CancellationToken token)
    {
        if (string.IsNullOrEmpty(_settings.AuthKey))
            return true;
        if (Authenticated)
            return true;
        _settings.Logger?.Invoke(Severity.Debug, $"{Header} message received from unauthenticated endpoint {ToString()}");
        int contentLength = 0;
        Stream? authStream = null;

        if (message.AuthKey == null
            || message.AuthKey.Length == 0
            || !_settings.AuthKey.Trim().Equals(Encoding.UTF8.GetString(message.AuthKey).Trim()))
        {
            _settings.Logger?.Invoke(Severity.Warn, $"{Header} no authentication material for {ToString()}");
            return false;
        }

        _settings.Logger?.Invoke(Severity.Debug, $"{Header} client {ToString()} attempting to register GUID {message.SenderGuid.ToString()}");
        _settings.Logger?.Invoke(Severity.Debug, $"{Header} updated client GUID from {Id} to {message.SenderGuid}");
        _events.HandleClientReplaceId(this, new(Id, message.SenderGuid));
        Id = message.SenderGuid;
        _settings.Logger?.Invoke(Severity.Debug, $"{Header} Authentication successful for {ToString()}");

        var data = Encoding.UTF8.GetBytes("Authentication successful");
        authStream = Common.BytesToStream(data, 0);
        contentLength = data.Length;
        var authMsg = MessageBuilder.ConstructNew(contentLength, authStream, null);
        authMsg.Status = MessageStatus.AuthSuccess;
        await SendInternalAsync(authMsg, token).ConfigureAwait(false);
        return true;
    }

    private async Task ReceiveNormalMessage(Message message, CancellationToken token)
    {
        byte[] msgData;
        msgData = await Common.ReadMessageDataAsync(message, _settings.StreamBufferSize, token).ConfigureAwait(false);
        message.Data = msgData;
        ClientReceivedMessageEventArgs mr = new(this, message);
        await Task.Run(() => _events.HandleClientReceivedMessage(this, mr), token);
    }


    private bool IsClientConnected()
    {
        if (TcpClient != null)
        {
            var state = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                    .FirstOrDefault(x =>
                        x.LocalEndPoint.Equals(TcpClient.Client.LocalEndPoint)
                        && x.RemoteEndPoint.Equals(TcpClient.Client.RemoteEndPoint));

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
                WriteLock.Wait();
                TcpClient.Client.Send(tmp, 0, 0);
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
                WriteLock.Release();
            }

            if (success) return true;

            try
            {
                WriteLock.Wait();

                if (TcpClient.Client.Poll(0, SelectMode.SelectWrite)
                    && (!TcpClient.Client.Poll(0, SelectMode.SelectError)))
                {
                    byte[] buffer = new byte[1];
                    if (TcpClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
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
                WriteLock.Release();
            }
        }
        else
        {
            return false;
        }
    }

    private void HandleException(Exception ode, string message)
    {
        _settings.Logger?.Invoke(Severity.Debug, $"{Header} {message}");
        _events.HandleExceptionEncountered(this, new ExceptionEventArgs(ode));
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