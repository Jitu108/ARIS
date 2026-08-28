using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace aris.BuildingBlocks.Security;

/// <summary>
/// Wires RS256 JWT bearer validation the same way in every service (Technical Documentation
/// §5.1/§5.2) — the gateway forwarding a token is never treated as validation, so each service
/// validates signature/issuer/audience/expiry itself using the shared signing key's public half.
/// </summary>
public static class JwtValidationExtensions
{
    public static IServiceCollection AddArisJwtBearerValidation(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

        var publicKeyPem = jwtSection["PublicKeyPem"];
        RSA rsa;
        if (!string.IsNullOrWhiteSpace(publicKeyPem))
        {
            rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
        }
        else if (environment.IsDevelopment())
        {
            // Dev-only fallback so the service boots without manual key setup. Tokens signed with
            // this ephemeral key are not valid across restarts and no other service can validate
            // them — never used outside Development. Real keys come from env vars/Docker secrets,
            // never source control (§5.1).
            rsa = RSA.Create(2048);
        }
        else
        {
            throw new InvalidOperationException(
                "Jwt:PublicKeyPem is not configured. Set it via environment variable / Docker secret — it must not come from source control.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                    RoleClaimType = "roles",
                    NameClaimType = "sub",
                };
            });

        services.AddAuthorization();

        return services;
    }
}
