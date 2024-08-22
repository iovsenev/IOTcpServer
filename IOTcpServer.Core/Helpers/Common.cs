﻿using IOTcpServer.Core.Infrastructure;
using System.Text;

namespace IOTcpServer.Core.Helpers;

internal class Common
{
    internal static string ByteArrayToHex(byte[] data)
    {
        StringBuilder hex = new StringBuilder(data.Length * 2);
        foreach (byte b in data) hex.AppendFormat("{0:x2}", b);
        return hex.ToString();
    }

    internal static byte[] AppendBytes(byte[] head, byte[] tail)
    {
        byte[] arrayCombined = new byte[head.Length + tail.Length];
        Array.Copy(head, 0, arrayCombined, 0, head.Length);
        Array.Copy(tail, 0, arrayCombined, head.Length, tail.Length);
        return arrayCombined;
    }
    internal static void BytesToStream(byte[] data, int start, out int contentLength, out Stream stream)
    {
        contentLength = 0;
        stream = new MemoryStream(Array.Empty<byte>());

        if (data != null && data.Length > 0)
        {
            contentLength = data.Length - start;
            stream = new MemoryStream();
            stream.Write(data, start, contentLength);
            stream.Seek(0, SeekOrigin.Begin);
        }
    }

    internal static async Task<byte[]> ReadMessageDataAsync(Message msg, int bufferLen, CancellationToken token)
    {
        if (msg == null) throw new ArgumentNullException(nameof(msg));
        if (msg.DataStream == null) throw new ArgumentNullException(nameof(msg.DataStream));
        if (msg.ContentLength == 0) return Array.Empty<byte>();

        byte[] msgData = Array.Empty<byte>();

        try
        {
            msgData = await ReadFromStreamAsync(msg.DataStream, msg.ContentLength, bufferLen, token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // exception may be thrown if a client disconnects
            // immediately after sending a message
        }
        return msgData;
    }

    internal static async Task<byte[]> ReadFromStreamAsync(Stream stream, long count, int bufferLen, CancellationToken token)
    {
        if (count <= 0) return Array.Empty<byte>();
        if (bufferLen <= 0) throw new ArgumentException("Buffer must be greater than zero bytes.");
        byte[] buffer = new byte[bufferLen];

        int read = 0;
        long bytesRemaining = count;

        using (MemoryStream ms = new MemoryStream())
        {
            try
            {
                while (bytesRemaining > 0)
                {
                    if (bufferLen > bytesRemaining) buffer = new byte[bytesRemaining];

                    read = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                    if (read > 0)
                    {
                        await ms.WriteAsync(buffer, 0, read, token).ConfigureAwait(false);
                        bytesRemaining -= read;
                    }
                    else
                    {
                        throw new IOException("Could not read from supplied stream.");
                    }
                }
            }
            catch (Exception)
            {
                // exception may be thrown if a client disconnects
                // immediately after sending a message
            }

            ms.Seek(0, SeekOrigin.Begin);
            return ms.ToArray();
        }
    }

    internal static async Task<MemoryStream> DataStreamToMemoryStream(long contentLength, Stream stream, int bufferLen, CancellationToken token)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (contentLength <= 0) return new MemoryStream(Array.Empty<byte>());
        if (bufferLen <= 0) throw new ArgumentException("Buffer must be greater than zero bytes.");
        byte[] buffer = new byte[bufferLen];

        int read = 0;
        long bytesRemaining = contentLength;
        MemoryStream ms = new MemoryStream();

        while (bytesRemaining > 0)
        {
            if (bufferLen > bytesRemaining) buffer = new byte[bytesRemaining];

            read = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
            if (read > 0)
            {
                await ms.WriteAsync(buffer, 0, read, token).ConfigureAwait(false);
                bytesRemaining -= read;
            }
            else
            {
                throw new IOException("Could not read from supplied stream.");
            }
        }

        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    internal static DateTime GetExpirationTimestamp(Message msg)
    {
        //
        // TimeSpan will be negative if sender timestamp is earlier than now or positive if sender timestamp is later than now
        // Goal #1: if sender has a later timestamp, decrease expiration by the difference between sender time and our time
        // Goal #2: if sender has an earlier timestamp, increase expiration by the difference between sender time and our time
        // 
        // E.g. If sender time is 10:40 and receiver time is 10:45 and expiration is 1 minute, so 10:41.
        // ts = 10:45 - 10:40 = 5 minutes
        // expiration = 10:41 + 5 = 10:46 which is 1 minute later than when receiver received the message
        //
        TimeSpan ts = DateTime.UtcNow - msg.TimestampUtc;

        if (msg.ExpirationUtc is null)
            return DateTime.MaxValue;
        return msg.ExpirationUtc.Value.AddMilliseconds(ts.TotalMilliseconds);
    }
}