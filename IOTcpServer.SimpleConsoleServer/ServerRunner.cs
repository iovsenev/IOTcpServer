using System.Diagnostics;
using System.Text;
using IOTcpServer.Core.Constants;
using IOTcpServer.Core.Infrastructure;
using IOTcpServer.Core;
using System.Text.Json;
using IOTcpServer.Core.Events.ServerEvents;

namespace IOTcpServer.SimpleConsoleServer;
public class ServerRunner
{
    private static string _ServerIp = "";
    private static int _ServerPort = 0;
    private static bool _Ssl = false;
    private static IoTcpServer _server = new IoTcpServer(new ());
    private static string _CertFile = "";
    private static string _CertPass = "";
    private static bool _DebugMessages = true;
    private static bool _AcceptInvalidCerts = true;
    private static bool _MutualAuth = true;
    private static Guid _LastGuid = Guid.Empty;

    public ServerRunner()
    {
    }

    public async Task Run(string[] args)
    {
        try
        {
            if (!_Ssl)
            {

            }
            else
            {
                _CertFile = InputHalper.GetString("Certificate file:", "test.pfx");
                _CertPass = InputHalper.GetString("Certificate password:", "password");
                _AcceptInvalidCerts = InputHalper.GetBoolean("Accept invalid certs:", true);
                _MutualAuth = InputHalper.GetBoolean("Mutually authenticate:", false);

                _server.Settings.AcceptInvalidCertificates = _AcceptInvalidCerts;
                _server.Settings.MutuallyAuthenticate = _MutualAuth;
            }

            _server.Events.ClientConnected += ClientConnected;
            _server.Events.ClientDisconnected += ClientDisconnected;
            _server.Events.MessageReceived += MessageReceived;
            _server.Events.ServerStarted += ServerStarted;
            _server.Events.ServerStopped += ServerStopped;
            _server.Events.ExceptionEncountered += ExceptionEncountered;


            //_Server.Settings.IdleClientTimeoutSeconds = 10;
            _server.Settings.AuthKey = "0000000000000000";
            _server.Settings.Logger = Logger;
            _server.Settings.DebugMessages = _DebugMessages;
            _server.Settings.NoDelay = true;

            _server.Settings.KeepAliveSettings.EnableTcpKeepAlives = true;
            _server.Settings.KeepAliveSettings.TcpKeepAliveInterval = 1;
            _server.Settings.KeepAliveSettings.TcpKeepAliveTime = 1;
            _server.Settings.KeepAliveSettings.TcpKeepAliveRetryCount = 3;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return;
        }

        _server.Start();

        bool runForever = true;
        List<ServerClient> clients;
        Guid guid;
        MessageStatus reason = MessageStatus.Removed;
        Dictionary<string, object> metadata;

        while (runForever)
        {
            string userInput = InputHalper.GetString("Command [? for help]:", null);
            if (_server is null)
                break;
            switch (userInput)
            {
                case "?":
                    bool listening = _server != null ? _server.IsListening : false;
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("  ?                   help (this menu)");
                    Console.WriteLine("  q                   quit");
                    Console.WriteLine("  cls                 clear screen");
                    Console.WriteLine("  start               start listening for connections (listening: " + listening.ToString() + ")");
                    Console.WriteLine("  stop                stop listening for connections  (listening: " + listening.ToString() + ")");
                    Console.WriteLine("  list                list clients");
                    Console.WriteLine("  dispose             dispose of the server");
                    Console.WriteLine("  send                send message to client");
                    Console.WriteLine("  send offset         send message to client with offset");
                    Console.WriteLine("  send md             send message with metadata to client");
                    Console.WriteLine("  sendandwait         send message and wait for a response");
                    Console.WriteLine("  sendempty           send empty message with metadata");
                    Console.WriteLine("  sendandwait empty   send empty message with metadata and wait for a response");
                    Console.WriteLine("  remove              disconnect client");
                    Console.WriteLine("  remove all          disconnect all clients");
                    Console.WriteLine("  psk                 set preshared key");
                    Console.WriteLine("  stats               display server statistics");
                    Console.WriteLine("  stats reset         reset statistics other than start time and uptime");
                    Console.WriteLine("  debug               enable/disable debug");
                    break;

                case "q":
                    runForever = false;
                    break;

                case "cls":
                    Console.Clear();
                    break;

                case "start":
                    _server.Start();
                    break;

                case "stop":
                    _server.Stop();
                    break;

                case "list":
                    clients = _server.ListClients().ToList();
                    if (clients != null && clients.Count > 0)
                    {
                        Console.WriteLine("");
                        Console.WriteLine("Clients");
                        Console.WriteLine("-------");
                        foreach (ServerClient curr in clients)
                        {
                            Console.WriteLine(curr.Id.ToString() + ": " + curr.IpPort);
                        }
                        Console.WriteLine("");
                    }
                    else
                    {
                        Console.WriteLine("None");
                    }
                    break;

                case "dispose":
                    _server.Dispose();
                    break;

                case "send":
                    guid = Guid.Parse(InputHalper.GetString("GUID:", _LastGuid.ToString()));
                    userInput = InputHalper.GetString("Data:", null);
                    if (!await _server.SendAsync(guid, userInput))
                        Console.WriteLine("Failed");
                    break;

                case "send offset":
                    guid = Guid.Parse(InputHalper.GetString("GUID:", _LastGuid.ToString()));
                    userInput = InputHalper.GetString("Data:", null );
                    int offset = InputHalper.GetInteger("Offset:", 0, true, true);
                    if (!await _server.SendAsync(guid, Encoding.UTF8.GetBytes(userInput), null, offset))
                        Console.WriteLine("Failed");
                    break;

                case "send10":
                    guid = Guid.Parse(InputHalper.GetString("GUID:", _LastGuid.ToString()));
                    userInput = InputHalper.GetString("Data:", null);
                    for (int i = 0; i < 10; i++)
                    {
                        Console.WriteLine("Sending " + i);
                        if (!await _server.SendAsync(guid, userInput + "[" + i.ToString() + "]"))
                            Console.WriteLine("Failed");
                    }
                    break;

                case "send md":
                    guid = Guid.Parse(InputHalper.GetString("GUID:", _LastGuid.ToString()));
                    userInput = InputHalper.GetString("Data:", null);
                    metadata = InputHalper.GetDictionary<object>("enter key", "enter value");
                    if (!await _server.SendAsync(guid, userInput, metadata))
                        Console.WriteLine("Failed");
                    break;

                case "send md large":
                    guid = Guid.Parse(InputHalper.GetString("GUID:", _LastGuid.ToString()));
                    metadata = new Dictionary<string, object>();
                    for (int i = 0; i < 100000; i++) metadata.Add(i.ToString(), i);
                    if (!await _server.SendAsync(guid, "Hello!", metadata))
                        Console.WriteLine("Failed");
                    break;

                case "remove":
                    guid = Guid.Parse(InputHalper.GetString("GUID:", _LastGuid.ToString()));
                    Console.WriteLine("Valid disconnect reasons: Removed, Normal, Shutdown, Timeout");
                    reason = (MessageStatus)Enum.Parse(typeof(MessageStatus), InputHalper.GetString("Disconnect reason:", "Removed"));
                    await _server.DisconnectClientAsync(guid, reason);
                    break;

                case "remove all":
                    await _server.DisconnectAllClientsAsync();
                    break;

                case "psk":
                    _server.Settings.AuthKey = InputHalper.GetString("Preshared key:", "1234567812345678");
                    break;

                case "stats":
                    Console.WriteLine(_server.Statistics.ToString());
                    break;

                case "stats reset":
                    _server.Statistics.Reset();
                    break;

                case "debug":
                    _server.Settings.DebugMessages = !_server.Settings.DebugMessages;
                    Console.WriteLine("Debug set to: " + _server.Settings.DebugMessages);
                    break;

                default:
                    break;
            }
        }
    }

   

    private static void ExceptionEncountered(object? sender, ExceptionEventArgs e)
    {
        Console.WriteLine(JsonSerializer.Serialize(e));
    }

    private static void ClientConnected(object? sender, ConnectionEventArgs args)
    {
        _LastGuid = args.Client.Id;
        Console.WriteLine("Client connected: " + args.Client.ToString());
    }

    private static void ClientDisconnected(object? sender, DisconnectionEventArgs args)
    {
        Console.WriteLine("Client disconnected: " + args.Client.ToString() + ": " + args.Reason.ToString());
    }

    private static void MessageReceived(object? sender, MessageReceivedEventArgs args)
    {
        _LastGuid = args.Client.Id;
        Console.Write(args.Data.Length + " byte message from " + args.Client.ToString() + ": ");
        if (args.Data != null) Console.WriteLine(Encoding.UTF8.GetString(args.Data));
        else Console.WriteLine("[null]");

        if (args.Metadata != null)
        {
            Console.Write("Metadata: ");
            if (args.Metadata.Count < 1)
            {
                Console.WriteLine("(none)");
            }
            else
            {
                Console.WriteLine(args.Metadata.Count);
                foreach (KeyValuePair<string, object> curr in args.Metadata)
                {
                    Console.WriteLine("  " + curr.Key.ToString() + ": " + curr.Value.ToString());
                }
            }
        }
    }

    private static void ServerStarted(object? sender, EventArgs args)
    {
        Console.WriteLine("Server started");
    }

    private static void ServerStopped(object? sender, EventArgs args)
    {
        Console.WriteLine("Server stopped");
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async Task<SyncResponse> SyncRequestReceived(SyncRequest req)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        _LastGuid = req.Client.Id;
        Console.Write("Synchronous request received from " + req.Client.ToString() + ": ");
        if (req.Data != null) Console.WriteLine(Encoding.UTF8.GetString(req.Data));
        else Console.WriteLine("[null]");

        if (req.Metadata != null && req.Metadata.Count > 0)
        {
            Console.WriteLine("Metadata:");
            foreach (KeyValuePair<string, object> curr in req.Metadata)
            {
                Console.WriteLine("  " + curr.Key.ToString() + ": " + curr.Value.ToString());
            }
        }

        Dictionary<string, object> retMetadata = new Dictionary<string, object>();
        retMetadata.Add("foo", "bar");
        retMetadata.Add("bar", "baz");

        // Uncomment to test timeout
        // Task.Delay(10000).Wait();
        Console.WriteLine("Sending synchronous response");
        return new SyncResponse(req, retMetadata, "Here is your response!");
    }

    

    private static void Logger(Severity sev, string msg)
    {
        Console.WriteLine("[" + sev.ToString().PadRight(9) + "] " + msg);
    }
}
