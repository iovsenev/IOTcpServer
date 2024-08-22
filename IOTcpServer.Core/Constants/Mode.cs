﻿using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace IOTcpServer.Core.Constants;

/// <summary>
/// Mode.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum Mode
{
    /// <summary>
    /// Tcp.
    /// </summary>
    [EnumMember(Value = "Tcp")]
    Tcp = 0,
    /// <summary>
    /// Ssl.
    /// </summary>
    [EnumMember(Value = "Ssl")]
    Ssl = 1
}