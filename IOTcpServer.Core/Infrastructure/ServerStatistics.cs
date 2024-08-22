namespace IOTcpServer.Core.Infrastructure;

public class ServerStatistics
{
    private DateTime _startTime = DateTime.UtcNow;
    private long _receivedBytes = 0;
    private long _receivedMessages = 0;
    private long _sentBytes = 0;
    private long _sentMessages = 0;

    public ServerStatistics()
    {

    }

    public DateTime StartTime { get => _startTime; }

    public TimeSpan UpTime { get => DateTime.UtcNow - _startTime; }
    public long ReceivedBytes { get => _receivedBytes; internal set => _receivedBytes = value; }
    public long ReceivedMessages { get => _receivedMessages; internal set => _receivedMessages = value; }
    public long SentBytes { get => _sentBytes; internal set => _sentBytes = value; }
    public long SentMessages { get => _sentMessages; internal set => _sentMessages = value; }

    public int ReceivedMessageSizeAverage
    {
        get
        {
            if (_receivedBytes > 0 && _receivedMessages > 0)
            {
                return (int)(_receivedBytes / _receivedMessages);
            }
            else
            {
                return 0;
            }
        }
    }

    public decimal SentMessageSizeAverage
    {
        get
        {
            if (_sentBytes > 0 && _sentMessages > 0)
            {
                return (int)(_sentBytes / _sentMessages);
            }
            else
            {
                return 0;
            }
        }
    }

    public override string ToString()
    {
        string ret =
            "--- Statistics ---" + Environment.NewLine +
            "    Started     : " + _startTime.ToString() + Environment.NewLine +
            "    Uptime      : " + UpTime.ToString() + Environment.NewLine +
            "    Received    : " + Environment.NewLine +
            "       Bytes    : " + ReceivedBytes + Environment.NewLine +
            "       Messages : " + ReceivedMessages + Environment.NewLine +
            "       Average  : " + ReceivedMessageSizeAverage + " bytes" + Environment.NewLine +
            "    Sent        : " + Environment.NewLine +
            "       Bytes    : " + SentBytes + Environment.NewLine +
            "       Messages : " + SentMessages + Environment.NewLine +
            "       Average  : " + SentMessageSizeAverage + " bytes" + Environment.NewLine;
        return ret;
    }

    /// <summary>
    /// Сбросить статистику, отличную от StartTime и UpTime.
    /// </summary>
    public void Reset()
    {
        _receivedBytes = 0;
        _receivedMessages = 0;
        _sentBytes = 0;
        _sentMessages = 0;
    }

    internal void IncrementReceivedMessages()
    {
        _receivedMessages = Interlocked.Increment(ref _receivedMessages);
    }

    internal void IncrementSentMessages()
    {
        _sentMessages = Interlocked.Increment(ref _sentMessages);
    }

    internal void AddReceivedBytes(long bytes)
    {
        _receivedBytes = Interlocked.Add(ref _receivedBytes, bytes);
    }

    internal void AddSentBytes(long bytes)
    {
        _sentBytes = Interlocked.Add(ref _sentBytes, bytes);
    }
}