using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using aris.BuildingBlocks.Middleware;
using aris.IdentityService.Application.Abstractions;
using aris.IdentityService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace aris.IdentityService.Infrastructure.Security;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly ArisSigningKey _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpiryMinutes;

    public JwtTokenGenerator(ArisSigningKey signingKey, IConfiguration configuration)
    {
        _signingKey = signingKey;

        var jwtSection = configuration.GetSection("Jwt");
        _issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        _audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        _accessTokenExpiryMinutes = jwtSection.GetValue<int?>("AccessTokenExpiryMinutes") ?? 30;
    }

    public GeneratedAccessToken Generate(User user, IReadOnlyCollection<string> roles)
    {
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(_accessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("name", user.DisplayName),
            new(ForcedPasswordChangeMiddleware.MustChangePasswordClaimType, user.MustChangePassword ? "true" : "false"),
        };
        claims.AddRange(roles.Select(role => new Claim("roles", role)));

        var signingCredentials = new SigningCredentials(new RsaSecurityKey(_signingKey.Rsa), SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var expiresInSeconds = (int)(expiresAtUtc - now).TotalSeconds;

        return new GeneratedAccessToken(accessToken, expiresAtUtc, expiresInSeconds);
    }
}
