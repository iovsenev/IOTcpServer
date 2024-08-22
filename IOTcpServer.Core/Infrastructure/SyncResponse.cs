using System.Text;

namespace IOTcpServer.Core.Infrastructure;

/// <summary>
/// Ответ на синхронный запрос.
/// </summary>
public class SyncResponse
{
    /// <summary>
    /// Instantiate.
    /// </summary>
    /// <param name="req">Синхронный запрос, для которого предназначен этот ответ.</param>
    /// <param name="data">Данные для отправки в качестве ответа.</param>
    public SyncResponse(SyncRequest req, string data)
    {
        if (req == null) throw new ArgumentNullException(nameof(req));
        ExpirationUtc = req.ExpirationUtc;
        ConversationGuid = req.ConversationGuid;

        if (String.IsNullOrEmpty(data)) Data = Array.Empty<byte>();
        else Data = Encoding.UTF8.GetBytes(data);
    }

    /// <summary>
    /// Instantiate.
    /// </summary>
    /// <param name="req">Синхронный запрос, для которого предназначен этот ответ.</param>
    /// <param name="data">Данные для отправки в качестве ответа.</param>
    public SyncResponse(SyncRequest req, byte[] data)
    {
        if (req == null) throw new ArgumentNullException(nameof(req));
        ExpirationUtc = req.ExpirationUtc;
        ConversationGuid = req.ConversationGuid;
        Data = data;
    }

    /// <summary>
    /// Instantiate.
    /// </summary>
    /// <param name="req">Синхронный запрос, для которого предназначен этот ответ.</param>
    /// <param name="metadata">Метаданные для присоединения к ответу.</param>
    /// <param name="data">Данные для отправки в качестве ответа.</param>
    public SyncResponse(SyncRequest req, Dictionary<string, object> metadata, string data)
    {
        if (req == null) throw new ArgumentNullException(nameof(req));
        ExpirationUtc = req.ExpirationUtc;
        ConversationGuid = req.ConversationGuid;

        Metadata = metadata;

        if (String.IsNullOrEmpty(data))
        {
            Data = Array.Empty<byte>();
        }
        else
        {
            Data = Encoding.UTF8.GetBytes(data);
        }
    }

    /// <summary>
    /// Instantiate.
    /// </summary> 
    /// <param name="req">Синхронный запрос, для которого предназначен этот ответ.</param>
    /// <param name="metadata">Метаданные для присоединения к ответу.</param>
    /// <param name="data">Данные для отправки в качестве ответа.</param>
    public SyncResponse(SyncRequest req, Dictionary<string, object> metadata, byte[] data)
    {
        if (req == null) throw new ArgumentNullException(nameof(req));
        ExpirationUtc = req.ExpirationUtc;
        ConversationGuid = req.ConversationGuid;

        Metadata = metadata;
        Data = data;
    }

    /// <summary>
    /// Instantiate.
    /// </summary>
    /// <param name="convGuid"></param>
    /// <param name="expirationUtc"></param>
    /// <param name="metadata"></param>
    /// <param name="data"></param>
    public SyncResponse(Guid convGuid, DateTime expirationUtc, Dictionary<string, object> metadata, byte[] data)
    {
        ConversationGuid = convGuid;
        ExpirationUtc = expirationUtc;
        Metadata = metadata;
        Data = data;
    }

    /// <summary>
    /// Метаданные для присоединения к ответу.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

    /// <summary>
    /// Данные для присоединения к ответу.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// GUID разговора.
    /// </summary>
    public Guid ConversationGuid { get; } = Guid.NewGuid();

    /// <summary>
    /// Время истечения срока действия запроса.
    /// </summary>
    public DateTime ExpirationUtc { get; }
}