using IOTcpServer.Core.Constants;
using System.Net;
using System.Reflection.Metadata;

namespace IOTcpServer.Core.Settings;

public class ServerSettings
{
    private IPAddress _listenIp = IPAddress.Parse("127.0.0.1");
    private int _listenPort = 9000;

    private int _streamBufferSize = 65536;
    private int _maxProxiedStreamSize = 67108864;

    private int _maxConnections = 4096;
    private int _idleClientTimeoutSeconds = 0;

    private TlsVersion _tlsVersion = TlsVersion.Tls12;
    private List<string> _permittedIPs = new List<string>();
    private List<string> _blockedIPs = new List<string>();

    private KeepaliveSettings _keepAliveSettings = new KeepaliveSettings();
    private ServerSslConfiguration _sslConfiguration = new ServerSslConfiguration();

    public ServerSettings()
    {
    }

    public Guid ServerId { get; } = Guid.NewGuid();
    public string AuthKey { get; set; } = string.Empty;
    public KeepaliveSettings KeepAliveSettings
    {
        get => _keepAliveSettings;
        set => _keepAliveSettings = value;
    }
    public ServerSslConfiguration SslConfiguration
    {
        get => _sslConfiguration;
        set => _sslConfiguration = value;
    }

    public IPAddress ListenIp
    {
        get => _listenIp;
        set => _listenIp = value;
    }
    public int ListenPort
    {
        get => _listenPort;
        set
        {
            if (value < 0)
                throw new ArgumentException($"{nameof(ListenPort)} must be greater than zero.");
            _listenPort = value;
        }
    }
    public int StreamBufferSize
    {
        get => _streamBufferSize;
        set
        {
            if (value < 0)
                throw new ArgumentException($"{nameof(StreamBufferSize)} must be greater than zero.");
            _streamBufferSize = value;
        }
    }
    public int MaxProxiedStreamSize
    {
        get => _maxProxiedStreamSize;
        set
        {
            if (value < 0)
                throw new ArgumentException($"{nameof(MaxProxiedStreamSize)} must be greater than zero.");
            _maxProxiedStreamSize = value;
        }
    }
    public int MaxConnections
    {
        get => _maxConnections; set
        {
            if (value < 0)
                throw new ArgumentException($"{nameof(MaxConnections)} must be greater than zero.");
            _maxProxiedStreamSize = value;
        }
    }
    public int IdleClientTimeoutSeconds
    {
        get => _idleClientTimeoutSeconds;
        set
        {
            if (value < 0)
                _idleClientTimeoutSeconds = 0;
            _idleClientTimeoutSeconds = value;
        }
    }
    public bool NoDelay { get; set; } = true;


    public bool IsSsl { get; set; } = false;
    public TlsVersion TlsVersion
    {
        get => _tlsVersion;
        set => _tlsVersion = value;
    }
    public string CertFilePath { get; set; } = "";
    public string CertPass { get; set; } = "";
    public bool AcceptInvalidCertificates { get; set; } = true;
    public bool MutuallyAuthenticate { get; set; } = false;


    public List<string> PermittedIPs { get => _permittedIPs; set => _permittedIPs = value; }
    public List<string> BlockedIPs { get => _blockedIPs; set => _blockedIPs = value; }

    public Action<Severity, string> Logger { get; set; } = (severity, message) => { Console.WriteLine($"[--{severity}--] ==> {message}"); };
}