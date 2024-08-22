namespace IOTcpServer.Core.Infrastructure;

public class ServerClientsManager : IDisposable
{
    private readonly object _UnauthenticatedClientsLock = new object();
    private Dictionary<Guid, DateTime> _UnauthenticatedClients = new Dictionary<Guid, DateTime>();

    private readonly object _ClientsLock = new object();
    private Dictionary<Guid, ServerClient> _Clients = new Dictionary<Guid, ServerClient>();

    private readonly object _ClientsLastSeenLock = new object();
    private Dictionary<Guid, DateTime> _ClientsLastSeen = new Dictionary<Guid, DateTime>();

    private readonly object _ClientsKickedLock = new object();
    private Dictionary<Guid, DateTime> _ClientsKicked = new Dictionary<Guid, DateTime>();

    private readonly object _ClientsTimedoutLock = new object();
    private Dictionary<Guid, DateTime> _ClientsTimedout = new Dictionary<Guid, DateTime>();

    public ServerClientsManager()
    {

    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    /// <param name="disposing">Indicate if resources should be disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _UnauthenticatedClients = new();
            _Clients = new();
            _ClientsLastSeen = new();
            _ClientsKicked = new();
            _ClientsTimedout = new();
        }
    }

    internal void Reset()
    {

    }

    internal void ReplaceGuid(Guid original, Guid replace)
    {
        ReplaceUnauthenticatedClient(original, replace);
        ReplaceClient(original, replace);
        ReplaceClientLastSeen(original, replace);
        ReplaceClientKicked(original, replace);
        ReplaceClientTimedout(original, replace);
    }

    internal void Remove(Guid guid)
    {
        RemoveUnauthenticatedClient(guid);
        RemoveClient(guid);
        RemoveClientLastSeen(guid);
        RemoveClientKicked(guid);
        RemoveClientTimedout(guid);
    }

    internal void AddUnauthenticatedClient(Guid guid)
    {
        lock (_UnauthenticatedClientsLock)
        {
            _UnauthenticatedClients.Add(guid, DateTime.UtcNow);
        }
    }

    internal void RemoveUnauthenticatedClient(Guid guid)
    {
        lock (_UnauthenticatedClientsLock)
        {
            if (_UnauthenticatedClients.ContainsKey(guid))
                _UnauthenticatedClients.Remove(guid);
        }
    }

    internal bool ExistsUnauthenticatedClient(Guid guid)
    {
        lock (_UnauthenticatedClientsLock)
        {
            return _UnauthenticatedClients.ContainsKey(guid);
        }
    }

    internal void ReplaceUnauthenticatedClient(Guid original, Guid update)
    {
        lock (_UnauthenticatedClientsLock)
        {
            if (_UnauthenticatedClients.ContainsKey(original))
            {
                DateTime dt = _UnauthenticatedClients[original];
                _UnauthenticatedClients.Remove(original);
                _UnauthenticatedClients.Add(update, dt);
            }
        }
    }

    internal Dictionary<Guid, DateTime> AllUnauthenticatedClients()
    {
        lock (_UnauthenticatedClientsLock)
        {
            return new Dictionary<Guid, DateTime>(_UnauthenticatedClients);
        }
    }

    internal void AddClient(Guid guid, ServerClient client)
    {
        lock (_ClientsLock)
        {
            _Clients.Add(guid, client);
        }
    }

    internal ServerClient? GetClient(Guid guid)
    {
        lock (_ClientsLock)
        {
            if (_Clients.ContainsKey(guid)) return _Clients[guid];
            return null;
        }
    }

    internal void RemoveClient(Guid guid)
    {
        lock (_ClientsLock)
        {
            if (_Clients.ContainsKey(guid))
                _Clients.Remove(guid);
        }
    }

    internal bool ExistsClient(Guid guid)
    {
        lock (_ClientsLock)
        {
            return _Clients.ContainsKey(guid);
        }
    }

    internal void ReplaceClient(Guid original, Guid update)
    {
        lock (_ClientsLock)
        {
            if (_Clients.ContainsKey(original))
            {
                ServerClient md = _Clients[original];
                _Clients.Remove(original);
                _Clients.Add(update, md);
            }
        }
    }

    internal Dictionary<Guid, ServerClient> AllClients()
    {
        lock (_ClientsLock)
        {
            return new Dictionary<Guid, ServerClient>(_Clients);
        }
    }

    internal void AddClientLastSeen(Guid guid)
    {
        lock (_ClientsLastSeenLock)
        {
            _ClientsLastSeen.Add(guid, DateTime.UtcNow);
        }
    }

    internal void RemoveClientLastSeen(Guid guid)
    {
        lock (_ClientsLastSeenLock)
        {
            if (_ClientsLastSeen.ContainsKey(guid))
                _ClientsLastSeen.Remove(guid);
        }
    }

    internal bool ExistsClientLastSeen(Guid guid)
    {
        lock (_ClientsLastSeenLock)
        {
            return _ClientsLastSeen.ContainsKey(guid);
        }
    }

    internal void ReplaceClientLastSeen(Guid original, Guid update)
    {
        lock (_ClientsLastSeenLock)
        {
            if (_ClientsLastSeen.ContainsKey(original))
            {
                DateTime dt = _ClientsLastSeen[original];
                _ClientsLastSeen.Remove(original);
                _ClientsLastSeen.Add(update, dt);
            }
        }
    }

    internal void UpdateClientLastSeen(Guid guid, DateTime dt)
    {
        lock (_ClientsLastSeenLock)
        {
            if (_ClientsLastSeen.ContainsKey(guid))
            {
                _ClientsLastSeen.Remove(guid);
                _ClientsLastSeen.Add(guid, dt.ToUniversalTime());
            }
        }
    }

    internal Dictionary<Guid, DateTime> AllClientsLastSeen()
    {
        lock (_ClientsLastSeenLock)
        {
            return new Dictionary<Guid, DateTime>(_ClientsLastSeen);
        }
    }

    internal void AddClientKicked(Guid guid)
    {
        lock (_ClientsKickedLock)
        {
            _ClientsKicked.Add(guid, DateTime.UtcNow);
        }
    }

    internal void RemoveClientKicked(Guid guid)
    {
        lock (_ClientsKickedLock)
        {
            if (_ClientsKicked.ContainsKey(guid))
                _ClientsKicked.Remove(guid);
        }
    }

    internal bool ExistsClientKicked(Guid guid)
    {
        lock (_ClientsKickedLock)
        {
            return _ClientsKicked.ContainsKey(guid);
        }
    }

    internal void ReplaceClientKicked(Guid original, Guid update)
    {
        lock (_ClientsKickedLock)
        {
            if (_ClientsKicked.ContainsKey(original))
            {
                DateTime dt = _ClientsKicked[original];
                _ClientsKicked.Remove(original);
                _ClientsKicked.Add(update, dt);
            }
        }
    }

    internal void UpdateClientKicked(Guid guid, DateTime dt)
    {
        lock (_ClientsKickedLock)
        {
            if (_ClientsKicked.ContainsKey(guid))
            {
                _ClientsKicked.Remove(guid);
                _ClientsKicked.Add(guid, dt.ToUniversalTime());
            }
        }
    }

    internal Dictionary<Guid, DateTime> AllClientsKicked()
    {
        lock (_ClientsKickedLock)
        {
            return new Dictionary<Guid, DateTime>(_ClientsKicked);
        }
    }

    internal void AddClientTimedout(Guid guid)
    {
        lock (_ClientsTimedoutLock)
        {
            _ClientsTimedout.Add(guid, DateTime.UtcNow);
        }
    }

    internal void RemoveClientTimedout(Guid guid)
    {
        lock (_ClientsTimedoutLock)
        {
            if (_ClientsTimedout.ContainsKey(guid))
                _ClientsTimedout.Remove(guid);
        }
    }

    internal bool ExistsClientTimedout(Guid guid)
    {
        lock (_ClientsTimedoutLock)
        {
            return _ClientsTimedout.ContainsKey(guid);
        }
    }

    internal void ReplaceClientTimedout(Guid original, Guid update)
    {
        lock (_ClientsTimedoutLock)
        {
            if (_ClientsTimedout.ContainsKey(original))
            {
                DateTime dt = _ClientsTimedout[original];
                _ClientsTimedout.Remove(original);
                _ClientsTimedout.Add(update, dt);
            }
        }
    }

    internal void UpdateClientTimeout(Guid guid, DateTime dt)
    {
        lock (_ClientsTimedoutLock)
        {
            if (_ClientsTimedout.ContainsKey(guid))
            {
                _ClientsTimedout.Remove(guid);
                _ClientsTimedout.Add(guid, dt.ToUniversalTime());
            }
        }
    }

    internal Dictionary<Guid, DateTime> AllClientsTimedout()
    {
        lock (_ClientsTimedoutLock)
        {
            return new Dictionary<Guid, DateTime>(_ClientsTimedout);
        }
    }
}