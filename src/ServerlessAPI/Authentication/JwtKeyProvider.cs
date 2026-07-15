using System.Text;
using AWS.Lambda.Powertools.Parameters;
using Microsoft.IdentityModel.Tokens;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Authentication;

/// <summary>
/// JWT signing key from Secrets Manager. Same SnapStart treatment as the DB password:
/// baking it into the snapshot would make key rotation pointless.
/// </summary>
public sealed class JwtKeyProvider(IConfiguration configuration) : ISecretBackedProvider
{
    /// <summary>HS256 rejects anything shorter.</summary>
    private const int MinKeyBytes = 32;

    private volatile SymmetricSecurityKey? _key;

    public SymmetricSecurityKey Key => _key ?? ResolveAsync().GetAwaiter().GetResult();

    public string Issuer => configuration["Jwt:Issuer"] ?? "san-lorenzo";
    public string Audience => configuration["Jwt:Audience"] ?? "san-lorenzo-app";

    /// <summary>Kept short: there is no revocation list.</summary>
    public static TimeSpan Lifetime => TimeSpan.FromHours(8);

    public async Task WarmAsync() => await ResolveAsync().ConfigureAwait(false);

    public void Clear() => _key = null;

    private async Task<SymmetricSecurityKey> ResolveAsync()
    {
        var secretName = Environment.GetEnvironmentVariable("JWT_SECRET_NAME");

        var material = string.IsNullOrWhiteSpace(secretName)
            ? configuration["Jwt:SigningKey"]
            : await ParametersManager.SecretsProvider
                .ForceFetch()
                .GetAsync(secretName)
                .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(material))
        {
            throw new InvalidOperationException(
                "No JWT signing key. Set JWT_SECRET_NAME (AWS) or Jwt:SigningKey (local).");
        }

        var bytes = Encoding.UTF8.GetBytes(material);

        if (bytes.Length < MinKeyBytes)
        {
            throw new InvalidOperationException(
                $"Signing key is {bytes.Length} bytes; HS256 needs at least {MinKeyBytes}. " +
                "Generate one with: openssl rand -base64 48");
        }

        _key = new SymmetricSecurityKey(bytes);
        return _key;
    }
}
