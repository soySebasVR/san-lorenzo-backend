using System.Text.Json.Serialization;

namespace ServerlessAPI.Data;

/// <summary>Modelo del secreto en Secrets Manager.</summary>
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
