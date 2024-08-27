using IOTcpServer.Core.Constants;
using IOTcpServer.Core.Events.ServerEvents;
using IOTcpServer.Core.Infrastructure;

namespace IOTcpServer.Core.Events.ClientEvents;
internal class ClientEvents
{
    public ClientEvents()
    {

    }

    internal event EventHandler<ClientnInformationEventArgs>? ClientAuthenticationSucceeded;

    internal event EventHandler<ClientnInformationEventArgs>? ClientAuthenticationFailed;

    internal event EventHandler<ClientnInformationEventArgs>? ClientNotConnectedEvent;

    internal event EventHandler<ClientDisconnectEventArgs>? ClientDisconnectEvent;

    internal event EventHandler<ClientSentMessageEventArgs>? ClientSentMessageEvent;

    internal event EventHandler<ClientReceivedMessageEventArgs>? ClientReceivedMessageEvent;

    internal event EventHandler<ClientReplaceIdEventArgs>? ClientReplaceIdEvent;

    internal event EventHandler<ExceptionEventArgs>? ExceptionEncountered;


    internal void HandleClientSentMessage(object sender, ClientSentMessageEventArgs args)
    {
        WrappedEventHandler(() => ClientSentMessageEvent?.Invoke(sender, args), "MessageSent", sender);
    }
   
    internal void HandleClientReceivedMessage(object sender, ClientReceivedMessageEventArgs args)
    {
        WrappedEventHandler(() => ClientReceivedMessageEvent?.Invoke(sender, args), "MessageSent", sender);
    }
    
    internal void HandleClientAuthenticationSucceeded(object sender, ClientnInformationEventArgs args)
    {
        WrappedEventHandler(() => ClientAuthenticationSucceeded?.Invoke(sender, args), "AuthenticationSucceeded", sender);
    }

    internal void HandleClientAuthenticationFailed(object sender, ClientnInformationEventArgs args)
    {
        WrappedEventHandler(() => ClientNotConnectedEvent?.Invoke(sender, args), "AuthenticationFailed", sender);
    }

    internal void HandleClientNotConnected(object sender, ClientnInformationEventArgs args)
    {
        WrappedEventHandler(() => ClientAuthenticationFailed?.Invoke(sender, args), "AuthenticationFailed", sender);
    }

    internal void HandleClientDisconnect(object sender, ClientDisconnectEventArgs args)
    {
        WrappedEventHandler(() => ClientDisconnectEvent?.Invoke(sender, args), "AuthenticationFailed", sender);
    }

    internal void HandleClientReplaceId(object sender, ClientReplaceIdEventArgs args)
    {
        WrappedEventHandler(() => ClientReplaceIdEvent?.Invoke(sender, args), "AuthenticationFailed", sender);
    }

    internal void HandleExceptionEncountered(object sender, ExceptionEventArgs args)
    {
        WrappedEventHandler(() => ExceptionEncountered?.Invoke(sender, args), "ExceptionEncountered", sender);
    }

    internal void WrappedEventHandler(Action action, string handler, object sender)
    {
        if (action == null) return;

        Action<Severity, string>? logger = ((ServerClient)sender).Logger;

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
