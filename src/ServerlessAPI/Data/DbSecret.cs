using System.Text.Json.Serialization;

namespace ServerlessAPI.Data;

/// <summary>
/// Shape of the Secrets Manager entry. RDS-managed secrets include host/port/dbname;
/// hand-made ones only carry the credentials and the rest comes from env vars.
/// </summary>
public sealed class DbSecret
{
    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;

    [JsonPropertyName("host")]
    public string? Host { get; init; }

    [JsonPropertyName("port")]
    public int? Port { get; init; }

    [JsonPropertyName("dbname")]
    public string? DbName { get; init; }
}
