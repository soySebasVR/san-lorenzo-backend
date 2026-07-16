using System.Text.Json;
using AWS.Lambda.Powertools.Parameters;
using Microsoft.Data.SqlClient;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Data;

/// <summary>Proveedor de cadena de conexión desde Secrets Manager.</summary>
public sealed class ConnectionStringProvider(IConfiguration configuration) : ISecretBackedProvider
{
    private volatile string? _connectionString;

    /// <summary>
    /// Falls back to a blocking resolve if no hook warmed it. Happens at most once per
    /// container, and Lambda runs one invocation at a time, so there is no race to guard.
    /// </summary>
    public string Value => _connectionString ?? ResolveAsync().GetAwaiter().GetResult();

    public async Task WarmAsync() => await ResolveAsync().ConfigureAwait(false);

    private async Task<string> ResolveAsync()
    {
        var secretName = Environment.GetEnvironmentVariable("DB_SECRET_NAME");

        // Desarrollo local si no hay secreto configurado.
        if (string.IsNullOrWhiteSpace(secretName))
        {
            var local = configuration.GetConnectionString("SanLorenzo");

            if (string.IsNullOrWhiteSpace(local))
            {
                throw new InvalidOperationException(
                    "No database credentials. Set DB_SECRET_NAME (AWS) or ConnectionStrings:SanLorenzo (local).");
            }

            _connectionString = local;
            return local;
        }

        var json = await ParametersManager.SecretsProvider
            .ForceFetch()
            .GetAsync(secretName)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException($"Secret '{secretName}' is empty or missing.");

        var secret = JsonSerializer.Deserialize<DbSecret>(json);

        if (secret is null || string.IsNullOrWhiteSpace(secret.Username))
            throw new InvalidOperationException($"Secret '{secretName}' has no valid credentials.");

        var host = secret.Host ?? Environment.GetEnvironmentVariable("DB_HOST");
        var database = secret.DbName ?? Environment.GetEnvironmentVariable("DB_NAME");
        var port = secret.Port ?? int.Parse(Environment.GetEnvironmentVariable("DB_PORT") ?? "1433");

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("Missing DB_HOST or DB_NAME.");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{host},{port}",
            InitialCatalog = database,
            UserID = secret.Username,
            Password = secret.Password,

            Encrypt = true,
            // Traffic is encrypted but the chain is not verified: doing so needs the
            // rds-ca-rsa2048-g1 bundle shipped with the function.
            TrustServerCertificate = true,

            // Process-wide pool, reused across warm invocations.
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = 10,

            ConnectTimeout = 30,
            CommandTimeout = 30,
            ApplicationName = "san-lorenzo-api",
        };

        _connectionString = builder.ConnectionString;
        return _connectionString;
    }
}
