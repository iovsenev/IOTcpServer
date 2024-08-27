using IOTcpServer.Core.Constants;

namespace IOTcpServer.Core.Events.ServerEvents;

/// <summary>
/// TCP сервер ивенты
/// </summary>
public class ServerEvents : IDisposable
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public ServerEvents()
    {

    }

    /// <summary>
    /// Событие, срабатывающее при успешной аутентификации клиента.
    /// </summary>
    public event EventHandler<AuthenticationSucceededEventArgs>? AuthenticationSucceeded;

    /// <summary>
    /// Событие, срабатывающее при неудачной аутентификации клиента.
    /// </summary>
    public event EventHandler<AuthenticationFailedEventArgs>? AuthenticationFailed;

    /// <summary>
    /// Событие, срабатывающее при подключении клиента к серверу.
    /// IP:port клиента передается в аргументах.
    /// </summary>
    public event EventHandler<ConnectionEventArgs>? ClientConnected;

    /// <summary>
    /// Событие, которое срабатывает при отключении клиента от сервера.
    /// IP:port передается в аргументах вместе с причиной отключения.
    /// </summary>
    public event EventHandler<DisconnectionEventArgs>? ClientDisconnected;

    /// <summary>
    /// Это событие срабатывает, когда от клиента получено сообщение и требуется, чтобы сервер передал массив байтов, содержащий полезную нагрузку сообщения.
    /// Если задано MessageReceived, StreamReceived использоваться не будет.
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public bool IsUsingMessage => MessageReceived != null && MessageReceived.GetInvocationList().Length > 0;

    /// <summary>
    /// Это событие вызывается при запуске сервера.
    /// </summary>
    public event EventHandler? ServerStarted;

    /// <summary>
    /// Это событие вызывается при остановке сервера.
    /// </summary>
    public event EventHandler? ServerStopped;

    /// <summary>
    /// Это событие вызывается при возникновении исключения.
    /// </summary>
    public event EventHandler<ExceptionEventArgs>? ExceptionEncountered;


    internal void HandleAuthenticationSucceeded(object sender, AuthenticationSucceededEventArgs args)
    {
        WrappedEventHandler(() => AuthenticationSucceeded?.Invoke(sender, args), "AuthenticationSucceeded", sender);
    }

    internal void HandleAuthenticationFailed(object sender, AuthenticationFailedEventArgs args)
    {
        WrappedEventHandler(() => AuthenticationFailed?.Invoke(sender, args), "AuthenticationFailed", sender);
    }

    internal void HandleClientConnected(object sender, ConnectionEventArgs args)
    {
        WrappedEventHandler(() => ClientConnected?.Invoke(sender, args), "ClientConnected", sender);
    }

    internal void HandleClientDisconnected(object sender, DisconnectionEventArgs args)
    {
        WrappedEventHandler(() => ClientDisconnected?.Invoke(sender, args), "ClientDisconnected", sender);
    }

    internal void HandleMessageReceived(object sender, MessageReceivedEventArgs args)
    {
        WrappedEventHandler(() => MessageReceived?.Invoke(sender, args), "MessageReceived", sender);
    }

    internal void HandleServerStarted(object sender, EventArgs args)
    {
        WrappedEventHandler(() => ServerStarted?.Invoke(sender, args), "ServerStarted", sender);
    }

    internal void HandleServerStopped(object sender, EventArgs args)
    {
        WrappedEventHandler(() => ServerStopped?.Invoke(sender, args), "ServerStopped", sender);
    }

    internal void HandleExceptionEncountered(object sender, ExceptionEventArgs args)
    {
        WrappedEventHandler(() => ExceptionEncountered?.Invoke(sender, args), "ExceptionEncountered", sender);
    }

    internal void WrappedEventHandler(Action action, string handler, object sender)
    {
        if (action == null) return;

        Action<Severity, string>? logger = ((IoTcpServer)sender).Settings.Logger;

        try
        {
            action.Invoke();
        }
        catch (Exception e)
        {
            logger?.Invoke(Severity.Error, "Event handler exception in " + handler + ": " + Environment.NewLine + e.ToString());
        }
    }

    public void Dispose()
    {
        AuthenticationSucceeded = null;
        AuthenticationFailed = null;
        ClientConnected = null;
        ClientDisconnected = null;
        MessageReceived = null;
        ServerStarted = null;
        ServerStopped = null;
        ExceptionEncountered = null;
    }
}