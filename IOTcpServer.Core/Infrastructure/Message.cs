using System.Text.Json.Serialization;
using IOTcpServer.Core.Constants;
using IOTcpServer.Core.Helpers;

namespace IOTcpServer.Core.Infrastructure;

public class Message
{
    private string _DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fffzzz"; // 32 bytes
    private byte[] _authKey = Array.Empty<byte>();
    public Message() { }

    public Message(Stream stream)
    {
        DataStream = stream;
    }

    /// <summary>
    /// Длина данных
    /// </summary>
    [JsonPropertyName("len")]
    public long ContentLength { get; set; }

    /// <summary>
    /// Предварительно предоставленный ключ для аутентификации соединения.
    /// </summary>
    [JsonPropertyName("aut")]
    public byte[] AuthKey
    {
        get
        {
            return _authKey;
        }
        set
        {
            if (value == null)
            {
                _authKey = Array.Empty<byte>();
            }
            else
            {
                if (value.Length != 16) throw new ArgumentException("Authentication key must be 16 bytes.");

                _authKey = new byte[16];
                Buffer.BlockCopy(value, 0, _authKey, 0, 16);
            }
        }
    }

    /// <summary>
    /// Статус сообщения   
    /// </summary>
    [JsonPropertyName("status")]
    public MessageStatus Status { get; set; } = MessageStatus.Normal;

    /// <summary>
    /// Словарь метаданных; содержит метаданные, предоставленные пользователем.
    /// </summary>
    [JsonPropertyName("md")]
    public Dictionary<string, object>? Metadata
    {
        get;
        set;
    }

    /// <summary>
    /// Указывает текущее время, воспринимаемое отправителем; полезно для определения сроков действия.
    /// </summary>
    [JsonPropertyName("ts")]
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;


    /// <summary>
    /// Указывает GUID беседы сообщения. 
    /// </summary>
    [JsonPropertyName("convguid")]
    public Guid ConversationGuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Отправителя GUID.
    /// </summary>
    public Guid SenderGuid { get; set; } = default;

    /// <summary>
    /// Поток, содержащий данные сообщения.
    /// </summary>
    [JsonIgnore]
    public Stream? DataStream { get; set; }
    [JsonIgnore]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Строковая версия объекта, понятная человеку.
    /// </summary>
    /// <returns>String.</returns>
    public override string ToString()
    {
        string ret = "---" + Environment.NewLine;
        ret += "  Preshared key     : " + (AuthKey != null ? Common.ByteArrayToHex(AuthKey) : "null") + Environment.NewLine;
        ret += "  Status            : " + Status.ToString() + Environment.NewLine;
        //ret += "  ExpirationUtc     : " + (ExpirationUtc != null ? ExpirationUtc.Value.ToString(_DateTimeFormat) : "null") + Environment.NewLine;
        ret += "  ConversationGuid  : " + ConversationGuid.ToString() + Environment.NewLine;

        if (Metadata != null && Metadata.Count > 0)
        {
            ret += "  Metadata          : " + Metadata.Count + " entries" + Environment.NewLine;
        }

        if (DataStream != null)
            ret += "  DataStream        : present, " + ContentLength + " bytes" + Environment.NewLine;

        return ret;
    }
}