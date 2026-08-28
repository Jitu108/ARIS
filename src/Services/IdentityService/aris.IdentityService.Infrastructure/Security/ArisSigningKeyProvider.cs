using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace aris.IdentityService.Infrastructure.Security;

/// <summary>
/// Resolves the RSA key pair IdentityService signs access tokens with. Must be resolved and its
/// derived public key written back into <c>Jwt:PublicKeyPem</c> configuration *before*
/// <c>aris.BuildingBlocks.Security.JwtValidationExtensions.AddArisJwtBearerValidation</c> runs —
/// otherwise, in Development, each side would generate its own unrelated ephemeral key and
/// IdentityService would be unable to validate the tokens it just issued.
/// </summary>
public sealed class ArisSigningKey : IDisposable
{
    public required RSA Rsa { get; init; }
    public required string PublicKeyPem { get; init; }

    public void Dispose()
    {
        Rsa.Dispose();
    }
}

public static class ArisSigningKeyProvider
{
    public static ArisSigningKey Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var privateKeyPem = configuration["Jwt:PrivateKeyPem"];

        RSA rsa;
        if (!string.IsNullOrWhiteSpace(privateKeyPem))
        {
            rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);
        }
        else if (environment.IsDevelopment())
        {
            // Dev-only fallback so the service boots without manual key setup. Not valid across
            // restarts and never used outside Development — real keys come from env vars/Docker
            // secrets, never source control (Technical Documentation §5.1).
            rsa = RSA.Create(2048);
        }
        else
        {
            throw new InvalidOperationException(
                "Jwt:PrivateKeyPem is not configured. Set it via environment variable / Docker secret — it must not come from source control.");
        }

        return new ArisSigningKey
        {
            Rsa = rsa,
            PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem(),
        };
    }
}
