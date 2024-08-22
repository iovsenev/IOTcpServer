using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace IOTcpServer.Core.Settings;
/// <summary>
/// Сохраняет параметры для <see cref="SslStream"/> используется серверами.
/// </summary>
public class ServerSslConfiguration
{
    private bool _clientCertRequired = true;
    private RemoteCertificateValidationCallback _clientCertValidationCallback;

    /// <summary>
    /// Initializes a new instance of <see cref="ServerSslConfiguration"/>.
    /// </summary>
    public ServerSslConfiguration()
    {
        _clientCertValidationCallback = DefaultValidateClientCertificate;
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ServerSslConfiguration"/>
    /// класс, в котором хранятся параметры, скопированные из другой конфигурации.
    /// </summary>
    /// <param name="configuration">
    ///  <see cref="ServerSslConfiguration"/> откуда копировать.
    /// </param>
    /// <exception cref="ArgumentNullException"/>
    public ServerSslConfiguration(ServerSslConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException("Can not copy from null server SSL configuration");

        _clientCertRequired = configuration._clientCertRequired;
        _clientCertValidationCallback = configuration._clientCertValidationCallback;
    }

    /// <summary>
    /// Получает или задает значение, указывающее, запрашивается ли у клиента
    /// сертификат для аутентификации.
    /// </summary>
    public bool ClientCertificateRequired
    {
        get
        {
            return _clientCertRequired;
        }

        set
        {
            _clientCertRequired = value;
        }
    }

    /// <summary>
    /// Получает или задает <see cref="RemoteCertificateValidationCallback"/> делегировать ответственность
    /// для проверки сертификата, предоставленного удаленной стороной.
    /// </summary>
    /// <remarks>
    /// Делегат по умолчанию возвращает true для всех сертификатов
    /// </remarks>
    public RemoteCertificateValidationCallback ClientCertificateValidationCallback
    {
        get
        {
            return _clientCertValidationCallback;
        }

        set
        {
            _clientCertValidationCallback = value;
        }
    }

    private static bool DefaultValidateClientCertificate(
          object sender,
          X509Certificate? certificate,
          X509Chain? chain,
          SslPolicyErrors sslPolicyErrors
        )
    {
        return true;
    }
}
