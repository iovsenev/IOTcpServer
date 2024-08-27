using System.Runtime.Serialization;
using System.Text;
using IOTcpServer.Core.Helpers;

namespace IOTcpServer.Core.Infrastructure;

public static class MessageBuilder
{
    private static ISerializationHelper _serializationHelper = new DefaultSerializationHelper();
    private static int _readStreamBuffer = 65536;


    internal static ISerializationHelper SerializationHelper
    {
        get => _serializationHelper;
        set
        {
            if (value == null) throw new ArgumentNullException(nameof(SerializationHelper));
            _serializationHelper = value;
        }
    }

    internal static int ReadStreamBuffer
    {
        get => _readStreamBuffer;
        set
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(ReadStreamBuffer));
            _readStreamBuffer = value;
        }
    }

    /// <summary>
    /// Создание нового сообщения для отправки
    /// </summary>
    /// <param name="contentLength">Количество байтов, включенных в поток.</param>
    /// <param name="stream">Поток, содержащий данные.</param>
    /// <param name="syncRequest">Укажите, является ли сообщение синхронным запросом сообщения.</param>
    /// <param name="syncResponse">Укажите, является ли сообщение синхронным ответом на сообщение.</param>
    /// <param name="expirationUtc">Время UTC, по истечении которого сообщение должно истечь (действительно только для синхронных запросов сообщений).</param>
    /// <param name="metadata">Метаданные для прикрепления к сообщению.</param>
    internal static Message ConstructNew(
        long contentLength,
        Stream? stream,
        Dictionary<string, object>? metadata = null)
    {
        Message msg;
        if (contentLength < 0) throw new ArgumentException("Content length must be zero or greater.");
        if (contentLength > 0)
        {
            if (stream == null || !stream.CanRead)
            {
                throw new ArgumentException("Cannot read from supplied stream.");
            }
            msg = new(stream);
        }
        else
        {
            msg = new();
        }
        msg.ContentLength = contentLength;
        msg.DataStream = stream;
        msg.Metadata = metadata;

        return msg;
    }

    /// <summary>
    /// Чтение с потока и создание сообщения
    /// </summary>
    /// <param name="stream">Поток.</param>
    /// <param name="token">Cancellation token.</param>
    internal static async Task<Message> BuildFromStream(Stream stream, CancellationToken token = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead) throw new ArgumentException("Cannot read from stream.");

        // {"len":0,"s":"Normal"}\r\n\r\n
        byte[] headerBytes = new byte[24];

        await stream.ReadAsync(headerBytes, 0, 24, token).ConfigureAwait(false);
        byte[] headerBuffer = new byte[1];

        while (true)
        {
            byte[] endCheck = headerBytes.Skip(headerBytes.Length - 4).Take(4).ToArray();

            if (endCheck[3] == 0
                && endCheck[2] == 0
                && endCheck[1] == 0
                && endCheck[0] == 0)
            {
                throw new IOException("Null header data indicates peer disconnected.");
            }

            if (endCheck[3] == 10
                && endCheck[2] == 13
                && endCheck[1] == 10
                && endCheck[0] == 13)
            {
                break;
            }

            await stream.ReadAsync(headerBuffer, 0, 1, token).ConfigureAwait(false);
            headerBytes = Common.AppendBytes(headerBytes, headerBuffer);
        }

        var msg = _serializationHelper.DeserializeJson<Message>(Encoding.UTF8.GetString(headerBytes));

        if (msg == null)
            throw new SerializationException("not serialized");

        msg.DataStream = stream;

        return msg;
    }

    /// <summary>
    /// Извлечение байтов заголовков сообщения.
    /// </summary>
    /// <param name="msg">Сообщение <see cref="Message"/></param>
    /// <returns>Header bytes.</returns>
    internal static byte[] GetHeaderBytes(Message msg)
    {
        string jsonStr = _serializationHelper.SerializeJson(msg, false);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonStr);
        byte[] end = Encoding.UTF8.GetBytes("\r\n\r\n");
        byte[] final = Common.AppendBytes(jsonBytes, end);
        return final;
    }
}