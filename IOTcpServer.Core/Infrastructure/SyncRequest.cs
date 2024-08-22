namespace IOTcpServer.Core.Infrastructure;

/// <summary>
/// Запрос, требующий ответа в течение определенного времени ожидания.
/// </summary>
public class SyncRequest
{
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="client">Метаданные клиента.</param>
    /// <param name="convGuid">GUID соединения.</param>
    /// <param name="expirationUtc">Срок действия метки времени UTC.</param>
    /// <param name="metadata">Metadata.</param>
    /// <param name="data">Data.</param>
    public SyncRequest(ServerClient client, Guid convGuid, DateTime? expirationUtc, Dictionary<string, object>? metadata, byte[] data)
    {
        Client = client;
        ConversationGuid = convGuid;
        ExpirationUtc = expirationUtc;
        Metadata = metadata;

        if (data != null)
        {
            Data = new byte[data.Length];
            Buffer.BlockCopy(data, 0, Data, 0, data.Length);
        }
    }

    /// <summary>
    /// Метаданные клиента.
    /// </summary>
    public ServerClient Client { get; }

    /// <summary>
    /// Время истечения срока действия запроса.
    /// </summary>
    public DateTime? ExpirationUtc { get; }

    /// <summary>
    /// Метаданные, прикрепленные к запросу.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; }

    /// <summary>
    /// Запрос данных.
    /// </summary>
    public byte[]? Data { get; }

    /// <summary>
    /// GUID соединения.
    /// </summary>
    public Guid ConversationGuid { get; } = Guid.NewGuid();
}