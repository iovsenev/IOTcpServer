namespace IOTcpServer.Core.Infrastructure;

public class ServerClientsManager : IDisposable
{
    private readonly object _unauthenticatedClientsLock = new object();
    private Dictionary<Guid, DateTime> _unauthenticatedClients = new Dictionary<Guid, DateTime>();

    private readonly object _clientsLock = new object();
    private Dictionary<Guid, ServerClient> _clients = new Dictionary<Guid, ServerClient>();

    private readonly object _clientsLastSeenLock = new object();
    private Dictionary<Guid, DateTime> _clientsLastSeen = new Dictionary<Guid, DateTime>();

    private readonly object _clientsKickedLock = new object();
    private Dictionary<Guid, DateTime> _clientsKicked = new Dictionary<Guid, DateTime>();

    private readonly object _clientsTimedoutLock = new object();
    private Dictionary<Guid, DateTime> _clientsTimedout = new Dictionary<Guid, DateTime>();

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
            _unauthenticatedClients = new();
            _clients = new();
            _clientsLastSeen = new();
            _clientsKicked = new();
            _clientsTimedout = new();
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
        lock (_unauthenticatedClientsLock)
        {
            _unauthenticatedClients.Add(guid, DateTime.UtcNow);
        }
    }

    internal void RemoveUnauthenticatedClient(Guid guid)
    {
        lock (_unauthenticatedClientsLock)
        {
            if (_unauthenticatedClients.ContainsKey(guid))
                _unauthenticatedClients.Remove(guid);
        }
    }

    internal bool ExistsUnauthenticatedClient(Guid guid)
    {
        lock (_unauthenticatedClientsLock)
        {
            return _unauthenticatedClients.ContainsKey(guid);
        }
    }

    internal void ReplaceUnauthenticatedClient(Guid original, Guid update)
    {
        lock (_unauthenticatedClientsLock)
        {
            if (_unauthenticatedClients.ContainsKey(original))
            {
                DateTime dt = _unauthenticatedClients[original];
                _unauthenticatedClients.Remove(original);
                _unauthenticatedClients.Add(update, dt);
            }
        }
    }

    internal Dictionary<Guid, DateTime> AllUnauthenticatedClients()
    {
        lock (_unauthenticatedClientsLock)
        {
            return new Dictionary<Guid, DateTime>(_unauthenticatedClients);
        }
    }

    internal void AddClient(Guid guid, ServerClient client)
    {
        lock (_clientsLock)
        {
            _clients.Add(guid, client);
        }
    }

    internal ServerClient? GetClient(Guid guid)
    {
        lock (_clientsLock)
        {
            if (_clients.ContainsKey(guid)) return _clients[guid];
            return null;
        }
    }

    internal void RemoveClient(Guid guid)
    {
        lock (_clientsLock)
        {
            if (_clients.ContainsKey(guid))
                _clients.Remove(guid);
        }
    }

    internal bool ExistsClient(Guid guid)
    {
        lock (_clientsLock)
        {
            return _clients.ContainsKey(guid);
        }
    }

    internal void ReplaceClient(Guid original, Guid update)
    {
        lock (_clientsLock)
        {
            if (_clients.ContainsKey(original))
            {
                ServerClient md = _clients[original];
                _clients.Remove(original);
                _clients.Add(update, md);
            }
        }
    }

    internal Dictionary<Guid, ServerClient> AllClients()
    {
        lock (_clientsLock)
        {
            return new Dictionary<Guid, ServerClient>(_clients);
        }
    }

    internal void AddClientLastSeen(Guid guid)
    {
        lock (_clientsLastSeenLock)
        {
            _clientsLastSeen.Add(guid, DateTime.UtcNow);
        }
    }

    internal void RemoveClientLastSeen(Guid guid)
    {
        lock (_clientsLastSeenLock)
        {
            if (_clientsLastSeen.ContainsKey(guid))
                _clientsLastSeen.Remove(guid);
        }
    }

    internal bool ExistsClientLastSeen(Guid guid)
    {
        lock (_clientsLastSeenLock)
        {
            return _clientsLastSeen.ContainsKey(guid);
        }
    }

    internal void ReplaceClientLastSeen(Guid original, Guid update)
    {
        lock (_clientsLastSeenLock)
        {
            if (_clientsLastSeen.ContainsKey(original))
            {
                DateTime dt = _clientsLastSeen[original];
                _clientsLastSeen.Remove(original);
                _clientsLastSeen.Add(update, dt);
            }
        }
    }

    internal void UpdateClientLastSeen(Guid guid, DateTime dt)
    {
        lock (_clientsLastSeenLock)
        {
            if (_clientsLastSeen.ContainsKey(guid))
            {
                _clientsLastSeen.Remove(guid);
                _clientsLastSeen.Add(guid, dt.ToUniversalTime());
            }
        }
    }

    internal Dictionary<Guid, DateTime> AllClientsLastSeen()
    {
        lock (_clientsLastSeenLock)
        {
            return new Dictionary<Guid, DateTime>(_clientsLastSeen);
        }
    }

    internal void AddClientKicked(Guid guid)
    {
        lock (_clientsKickedLock)
        {
            _clientsKicked.Add(guid, DateTime.UtcNow);
        }
    }

    internal void RemoveClientKicked(Guid guid)
    {
        lock (_clientsKickedLock)
        {
            if (_clientsKicked.ContainsKey(guid))
                _clientsKicked.Remove(guid);
        }
    }

    internal bool ExistsClientKicked(Guid guid)
    {
        lock (_clientsKickedLock)
        {
            return _clientsKicked.ContainsKey(guid);
        }
    }

    internal void ReplaceClientKicked(Guid original, Guid update)
    {
        lock (_clientsKickedLock)
        {
            if (_clientsKicked.ContainsKey(original))
            {
                DateTime dt = _clientsKicked[original];
                _clientsKicked.Remove(original);
                _clientsKicked.Add(update, dt);
            }
        }
    }

    internal void UpdateClientKicked(Guid guid, DateTime dt)
    {
        lock (_clientsKickedLock)
        {
            if (_clientsKicked.ContainsKey(guid))
            {
                _clientsKicked.Remove(guid);
                _clientsKicked.Add(guid, dt.ToUniversalTime());
            }
        }
    }

    internal Dictionary<Guid, DateTime> AllClientsKicked()
    {
        lock (_clientsKickedLock)
        {
            return new Dictionary<Guid, DateTime>(_clientsKicked);
        }
    }

    internal void AddClientTimedout(Guid guid)
    {
        lock (_clientsTimedoutLock)
        {
            _clientsTimedout.Add(guid, DateTime.UtcNow);
        }
    }

    internal void RemoveClientTimedout(Guid guid)
    {
        lock (_clientsTimedoutLock)
        {
            if (_clientsTimedout.ContainsKey(guid))
                _clientsTimedout.Remove(guid);
        }
    }

    internal bool ExistsClientTimedout(Guid guid)
    {
        lock (_clientsTimedoutLock)
        {
            return _clientsTimedout.ContainsKey(guid);
        }
    }

    internal void ReplaceClientTimedout(Guid original, Guid update)
    {
        lock (_clientsTimedoutLock)
        {
            if (_clientsTimedout.ContainsKey(original))
            {
                DateTime dt = _clientsTimedout[original];
                _clientsTimedout.Remove(original);
                _clientsTimedout.Add(update, dt);
            }
        }
    }

    internal void UpdateClientTimeout(Guid guid, DateTime dt)
    {
        lock (_clientsTimedoutLock)
        {
            if (_clientsTimedout.ContainsKey(guid))
            {
                _clientsTimedout.Remove(guid);
                _clientsTimedout.Add(guid, dt.ToUniversalTime());
            }
        }
    }

    internal Dictionary<Guid, DateTime> AllClientsTimedout()
    {
        lock (_clientsTimedoutLock)
        {
            return new Dictionary<Guid, DateTime>(_clientsTimedout);
        }
    }
}