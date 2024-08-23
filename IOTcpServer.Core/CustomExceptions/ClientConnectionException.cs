namespace IOTcpServer.Core.CustomExceptions;
internal class ClientConnectionException : Exception
{
    public ClientConnectionException(string message) : base(message)
    {
        
    }
}
