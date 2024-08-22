namespace IOTcpServer.Core.Settings;
/// <summary>
/// Настройки проверки активности Сервера. Сервер не реализует проверки активности, а полагается на базовую реализацию в операционной системе и среде выполнения.
/// Проверка активности отправляется после периода простоя, определяемого параметром TcpKeepAliveTime (секунды).
/// Если ответ проверки активности не получен в течение TcpKeepAliveInterval (секунды), будет отправлена ​​последующая проверка активности.
/// Если количество проверок, указанных в параметре TcpKeepAliveRetryCount, не будет завершено, соединение будет разорвано.
/// </summary>
public class KeepaliveSettings
{
    private int _TcpKeepAliveInterval = 5;
    private int _TcpKeepAliveTime = 5;
    private int _TcpKeepAliveRetryCount = 5;

    /// <summary>
    /// Instantiate.
    /// </summary>
    public KeepaliveSettings()
    {

    }

    /// <summary>
    /// Включение или отключение проверок активности на основе TCP.
    /// Сервер не реализует проверки активности, а полагается на базовую реализацию в операционной системе и среде выполнения.
    /// </summary>
    public bool EnableTcpKeepAlives = false;

    /// <summary>
    /// Интервал TCP keepalive, т. е. количество секунд, в течение которых TCP-соединение будет ожидать ответа keepalive перед отправкой другого зонда keepalive.
    /// Сервер не реализует keepalive, а полагается на базовую реализацию в операционной системе и среде выполнения.
    /// Значение по умолчанию — 5 секунд. Значение должно быть больше нуля.
    /// </summary>
    public int TcpKeepAliveInterval
    {
        get
        {
            return _TcpKeepAliveInterval;
        }
        set
        {
            if (value < 1) throw new ArgumentException("TcpKeepAliveInterval must be greater than zero.");
            _TcpKeepAliveInterval = value;
        }
    }

    /// <summary>
    /// Время TCP keepalive, т. е. количество секунд, в течение которых TCP-соединение будет оставаться активным/бездействующим, прежде чем зонды keepalive будут отправлены на удаленный компьютер.
    /// Сервер не реализует keepalive, а полагается на базовую реализацию в операционной системе и среде выполнения.
    /// Значение по умолчанию — 5 секунд. Значение должно быть больше нуля.
    /// </summary>
    public int TcpKeepAliveTime
    {
        get
        {
            return _TcpKeepAliveTime;
        }
        set
        {
            if (value < 1) throw new ArgumentException("TcpKeepAliveTime must be greater than zero.");
            _TcpKeepAliveTime = value;
        }
    }

    /// <summary>
    /// Количество повторных попыток TCP keepalive, т. е. количество раз, когда будет отправлена ​​проверка TCP для проверки соединения.
    /// Сервер не реализует проверки активности, а полагается на базовую реализацию в операционной системе и среде выполнения.
    /// После того, как указанное количество попыток не будет выполнено, соединение будет разорвано.
    /// </summary>
    public int TcpKeepAliveRetryCount
    {
        get
        {
            return _TcpKeepAliveRetryCount;
        }
        set
        {
            if (value < 1) throw new ArgumentException("TcpKeepAliveRetryCount must be greater than zero.");
            _TcpKeepAliveRetryCount = value;
        }
    }
}