namespace IOTcpServer.Core.Events.ServerEvents;

/// <summary>
/// Аргументы события при возникновении исключения. 
/// </summary>
public class ExceptionEventArgs
{
    internal ExceptionEventArgs(Exception e)
    {
        if (e == null) throw new ArgumentNullException(nameof(e));

        Exception = e;
    }

    /// <summary>
    /// Exception.
    /// </summary>
    public Exception Exception { get; }
}