namespace IOTcpServer.Core.Infrastructure;

public class ServerStream : Stream
{
    private readonly object _Lock = new object();
    private Stream _Stream;
    private long _Length = 0;
    private long _Position = 0;
    private long _BytesRemaining
    {
        get
        {
            return _Length - _Position;
        }
    }

    internal ServerStream(long contentLength, Stream stream)
    {
        if (contentLength < 0) throw new ArgumentException("Content length must be zero or greater.");
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead) throw new ArgumentException("Cannot read from supplied stream.");

        _Length = contentLength;
        _Stream = stream;
    }

    /// <summary>
    /// Указывает, доступен ли поток для чтения.
    /// Это значение всегда будет истинным.
    /// </summary>
    public override bool CanRead => true;

    /// <summary>
    /// Указывает, поддерживаются ли операции поиска.
    /// Это значение всегда будет ложным.
    /// </summary>
    public override bool CanSeek => false;

    /// <summary>
    /// Указывает, доступен ли поток для записи.
    /// Это значение всегда будет ложным.
    /// </summary>
    public override bool CanWrite => false;

    /// <summary>
    /// Количество байтов, оставшихся в потоке.
    /// </summary>
    public override long Length
    {
        get
        {
            return _Length;
        }
    }

    /// <summary>
    /// Текущая позиция в потоке.
    /// </summary>
    public override long Position
    {
        get
        {
            return _Position;
        }
        set
        {
            throw new InvalidOperationException("Position may not be modified.");
        }
    }

    /// <summary>
    /// Сбрасывает данные, ожидающие в потоке.
    /// </summary>
    public override void Flush()
    {
    }

    /// <summary>
    /// Чтение данных из потока.
    /// </summary>
    /// <param name="buffer">Буфер, в который следует считывать данные.</param>
    /// <param name="offset">Смещение внутри буфера, где должны начинаться данные.</param>
    /// <param name="count">Количество байтов для чтения.</param>
    /// <returns>Количество прочитанных байтов.</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentException("Offset must be zero or greater.");
        if (offset >= buffer.Length) throw new IndexOutOfRangeException("Offset must be less than the buffer length of " + buffer.Length + ".");
        if (count < 0) throw new ArgumentException("Count must be zero or greater.");
        if (count == 0) return 0;
        if (count + offset > buffer.Length) throw new ArgumentException("Offset and count must sum to a value less than the buffer length of " + buffer.Length + ".");

        lock (_Lock)
        {
            byte[] temp;

            if (_BytesRemaining == 0) return 0;

            if (count > _BytesRemaining) temp = new byte[_BytesRemaining];
            else temp = new byte[count];

            int bytesRead = _Stream.Read(temp, 0, temp.Length);
            Buffer.BlockCopy(temp, 0, buffer, offset, bytesRead);
            _Position += bytesRead;

            return bytesRead;

        }
    }

    /// <summary>
    /// Не поддерживается.
    /// Поиск определенной позиции в потоке.  
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="origin"></param>
    /// <returns></returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new InvalidOperationException("Seek operations are not supported.");
    }

    /// <summary>
    /// Не поддерживается.
    /// Установите длину потока.
    /// </summary>
    /// <param name="value">Length.</param>
    public override void SetLength(long value)
    {
        throw new InvalidOperationException("Length may not be modified.");
    }

    /// <summary>
    /// Не поддерживается.
    /// Запись в поток.
    /// </summary>
    /// <param name="buffer">The buffer containing the data that should be written to the stream.</param>
    /// <param name="offset">The offset within the buffer from which data should be read.</param>
    /// <param name="count">The number of bytes to read.</param>
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new InvalidOperationException("Stream is not writeable.");
    }
}