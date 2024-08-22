using IOTcpServer.Core.Constants;
using IOTcpServer.Core.Constants;
using IOTcpServer.Core.Events;
using IOTcpServer.Core.Helpers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime;
using System.Text;

namespace IOTcpServer.Core.Infrastructure;

public class ServerClient : IDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly MessageBuilder _messageBuilder;
    private NetworkStream _networkStream;
    private SslStream? _sslStream;
    private Stream _dataStream;
    private EndPoint _ipPort ;

    private readonly int _serverStreamBufferSize;
    private readonly Action<Severity, string>? _logger;
    internal SemaphoreSlim WriteLock = new SemaphoreSlim(1, 1);
    internal SemaphoreSlim ReadLock = new SemaphoreSlim(1, 1);

    internal CancellationTokenSource TokenSource;
    internal CancellationToken Token;
    public ServerClient(
        TcpClient tcpClient, 
        MessageBuilder messageBuilder, 
        int serverStreamBufferSize,
        Action<Severity,string>? logger)
    {
        _tcpClient = tcpClient;
        _messageBuilder = messageBuilder;
        _serverStreamBufferSize = serverStreamBufferSize;
        _logger = logger;
        _ipPort = _tcpClient.Client.RemoteEndPoint ?? new IPEndPoint(IPAddress.None,0);
        _networkStream = _tcpClient.GetStream();
        _dataStream = _networkStream;
        TokenSource = new CancellationTokenSource();
        Token = TokenSource.Token;
    }

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

    internal async Task<long> SendInternalAsync(Message msg, long contentLength, Stream? stream, CancellationToken token)
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
            await SendHeadersAsync( msg, token).ConfigureAwait(false);
            if (contentLength > 0)
            {
                if (stream == null || !stream.CanRead)
                {
                    throw new ArgumentException("Cannot read from supplied stream.");
                }
                await SendDataStreamAsync( contentLength, stream, token).ConfigureAwait(false);
            }

            return contentLength;
        }
        catch (TaskCanceledException)
        {
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception e)
        {
            _logger?.Invoke(Severity.Error, $"{ConstantsString.Header} failed to write message to {this.ToString()}: {e.Message}");
            //_events.HandleExceptionEncountered(this, new ExceptionEventArgs(e));
            return 0;
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
            if (bytesRemaining >= _serverStreamBufferSize)
            {
                SendBuffer = new byte[_serverStreamBufferSize];
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