namespace IOTcpServer.Core.CustomExceptions;
internal class AuthenticatedFailedException : Exception
{
    public AuthenticatedFailedException(string message) : base(message)
    {
    }
}
