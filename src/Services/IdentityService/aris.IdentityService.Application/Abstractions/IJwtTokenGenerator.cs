using aris.IdentityService.Domain.Entities;

namespace aris.IdentityService.Application.Abstractions;

public interface IJwtTokenGenerator
{
    GeneratedAccessToken Generate(User user, IReadOnlyCollection<string> roles);
}

public sealed record GeneratedAccessToken(string Token, DateTime ExpiresAtUtc, int ExpiresInSeconds);
