using aris.IdentityService.Application.Abstractions;

namespace aris.IdentityService.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
