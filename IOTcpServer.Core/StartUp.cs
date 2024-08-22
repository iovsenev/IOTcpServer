
using IOTcpServer.Core.Events;
using System.Net;

namespace IOTcpServer.Core;
internal class StartUp
{
    public void Run()
    {
        IoTcpServer server = new(new()
        {
            ListenIp = IPAddress.Any,
            ListenPort = 9000,
            KeepAliveSettings =
            {
                EnableTcpKeepAlives = true,

            },
        });

        server.Events.AuthenticationFailed += auth;
    }

    private void auth(object? sender, AuthenticationFailedEventArgs e)
    {
        throw new NotImplementedException();
    }
}
