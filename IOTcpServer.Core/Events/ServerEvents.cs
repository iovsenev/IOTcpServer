using IOTcpServer.Core.Constants;

namespace IOTcpServer.Core.Events;

/// <summary>
/// TCP сервер ивенты
/// </summary>
public class ServerEvents
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public ServerEvents()
    {

    }
    /// <summary>
    /// Событие, срабатывающее при запросе аутентификации от клиента.
    /// </summary>
    public event EventHandler<AuthenticationRequestedEventArgs>? AuthenticationRequested;

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

    /// <summary> 
    /// Это событие запускается, когда от клиента получен поток и требуется, чтобы WatsonTcp передал поток, содержащий полезную нагрузку сообщения, в ваше приложение.
    /// Если задано MessageReceived, StreamReceived использоваться не будет.
    /// </summary>
    public event EventHandler<StreamReceivedEventArgs>? StreamReceived;

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

    internal bool IsUsingMessages
    {
        get => MessageReceived != null && MessageReceived.GetInvocationList().Length > 0;
    }

    internal bool IsUsingStreams
    {
        get => StreamReceived != null && StreamReceived.GetInvocationList().Length > 0;
    }

    internal void HandleAuthenticationRequested(object sender, AuthenticationRequestedEventArgs args)
    {
        WrappedEventHandler(() => AuthenticationRequested?.Invoke(sender, args), "AuthenticationRequested", sender);
    }

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

    internal void HandleStreamReceived(object sender, StreamReceivedEventArgs args)
    {
        WrappedEventHandler(() => StreamReceived?.Invoke(sender, args), "StreamReceived", sender);
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
}