using aris.IdentityService.Infrastructure.Security;

namespace aris.IdentityService.UnitTests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact] // UT-ID-03: accepts a matching password.
    public void Verify_WithMatchingPassword_ReturnsTrue()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Admin@12345");

        Assert.True(_sut.Verify("Admin@12345", hash));
    }

    [Fact] // UT-ID-03: rejects a non-matching password.
    public void Verify_WithNonMatchingPassword_ReturnsFalse()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Admin@12345");

        Assert.False(_sut.Verify("wrong-password", hash));
    }
}
