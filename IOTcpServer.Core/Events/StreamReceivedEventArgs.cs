using IOTcpServer.Core.Infrastructure;
using System.Runtime.CompilerServices;

namespace IOTcpServer.Core.Events;

/// <summary>
/// Аргументы события при получении потока.
/// </summary>
public class StreamReceivedEventArgs : EventArgs
{
    private Dictionary<string, object> _Metadata = new Dictionary<string, object>();
    private byte[]? _data = null;
    private int _bufferSize = 65536;

    internal StreamReceivedEventArgs(ServerClient client, Dictionary<string, object>? metadata, long contentLength, Stream stream)
    {
        Client = client;
        Metadata = metadata;
        ContentLength = contentLength;
        DataStream = stream;
    }

    /// <summary>
    /// Метаданные клиента.
    /// </summary>
    public ServerClient Client { get; }

    /// <summary>
    /// Метаданные, полученные от конечной точки.
    /// </summary>
    public Dictionary<string, object>? Metadata
    {
        get
        {
            return _Metadata;
        }
        set
        {
            if (value == null) _Metadata = new Dictionary<string, object>();
            else _Metadata = value;
        }
    }

    /// <summary>
    /// Количество байтов данных, которые следует прочитать из DataStream.
    /// </summary>
    public long ContentLength { get; }

    /// <summary>
    /// Поток, содержащий данные сообщения.
    /// </summary>
    public Stream DataStream { get; }

    /// <summary>
    /// Данные из DataStream.
    /// Использование Data полностью прочитает содержимое DataStream.
    /// </summary>
    public byte[]? Data
    {
        get
        {
            if (_data != null) return _data;
            if (ContentLength <= 0) return null;
            _data = ReadFromStream(DataStream, ContentLength);
            return _data;
        }
    }

    private byte[] ReadFromStream(Stream stream, long count)
    {
        if (count <= 0) throw new ArgumentException("Count data read must be greater than zero.");
        byte[] buffer = new byte[_bufferSize];

        int read = 0;
        long bytesRemaining = count;
        MemoryStream ms = new MemoryStream();

        while (bytesRemaining > 0)
        {
            if (_bufferSize > bytesRemaining) buffer = new byte[bytesRemaining];

            read = stream.Read(buffer, 0, buffer.Length);
            if (read > 0)
            {
                ms.Write(buffer, 0, read);
                bytesRemaining -= read;
            }
            else
            {
                throw new IOException("Could not read from supplied stream.");
            }
        }

        byte[] data = ms.ToArray();
        return data;
    }
}