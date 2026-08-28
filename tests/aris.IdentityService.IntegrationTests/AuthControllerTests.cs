using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using aris.IdentityService.Application.Authentication;
using aris.IdentityService.Infrastructure.Persistence;
using aris.IdentityService.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace aris.IdentityService.IntegrationTests;

public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    [Fact] // IT-ID-01: POST /identity/login with seeded valid credentials returns 200 with a usable access + refresh token.
    public async Task Login_WithSeededValidCredentials_ReturnsOkWithUsableTokens()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/identity/login", new LoginRequestDto("admin", "Admin@12345"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.Equal("System Administrator", body.User.DisplayName);
        Assert.Contains("Administrator", body.User.Roles);
        Assert.False(body.MustChangePassword);

        // "Usable" means the app's own JWT bearer validation accepts a token this response issued —
        // proves the signing key resolved at startup matches the validation key it was seeded with
        // (no [Authorize] endpoint exists yet in this slice to exercise that through HTTP, so this
        // validates the token directly against the same signing key the running app resolved).
        var signingKey = _factory.Services.GetRequiredService<ArisSigningKey>();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "ARIS.IdentityService",
            ValidateAudience = true,
            ValidAudience = "ARIS",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(signingKey.Rsa),
        };

        // Validate the raw JWT (not via ClaimsPrincipal, whose default inbound claim-type mapping
        // would obscure the literal "sub" claim name) to confirm the signature and claims are correct.
        new JwtSecurityTokenHandler().ValidateToken(body.AccessToken, validationParameters, out var validatedToken);
        var jwt = (JwtSecurityToken)validatedToken;
        Assert.Equal(body.User.Id.ToString(), jwt.Claims.Single(c => c.Type == "sub").Value);
    }

    [Fact] // IT-ID-02 (courtesy, FR-1.2): invalid credentials return 401 with the generic problem-details message.
    public async Task Login_WithWrongPassword_ReturnsGenericUnauthorizedProblemDetails()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/identity/login", new LoginRequestDto("admin", "wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid username or password.", body);
    }

    [Fact] // FR-1.2: unknown username produces the exact same response as a wrong password (anti-enumeration).
    public async Task Login_WithUnknownUsername_ReturnsSameGenericResponseAsWrongPassword()
    {
        var client = _factory.CreateClient();

        var wrongPasswordResponse = await client.PostAsJsonAsync("/identity/login", new LoginRequestDto("admin", "wrong"));
        var unknownUserResponse = await client.PostAsJsonAsync("/identity/login", new LoginRequestDto("nosuchuser", "whatever"));

        Assert.Equal(wrongPasswordResponse.StatusCode, unknownUserResponse.StatusCode);

        // Compare everything except traceId, which legitimately differs per request.
        var wrongPasswordProblem = await wrongPasswordResponse.Content.ReadFromJsonAsync<JsonElement>();
        var unknownUserProblem = await unknownUserResponse.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var field in new[] { "type", "title", "status", "detail" })
        {
            Assert.Equal(
                wrongPasswordProblem.GetProperty(field).ToString(),
                unknownUserProblem.GetProperty(field).ToString());
        }
    }

    [Fact] // FR-1.4: logout with no bearer token is rejected — the endpoint requires an authenticated caller.
    public async Task Logout_WithoutBearerToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/identity/logout", new LogoutRequestDto("whatever"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact] // FR-1.4: logging out with the refresh token issued by login revokes that token; reusing it then fails.
    public async Task Logout_WithValidBearerAndOwnRefreshToken_RevokesTokenAndReturnsNoContent()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new LoginRequestDto("admin", "Admin@12345"));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        var logoutResponse = await client.PostAsJsonAsync("/identity/logout", new LogoutRequestDto(loginBody.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(loginBody.RefreshToken)));
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var revokedToken = dbContext.RefreshTokens.Single(token => token.TokenHash == tokenHash);
        Assert.NotNull(revokedToken.RevokedAtUtc);
    }

    [Fact] // FR-1.4: an unrecognized refresh token is a silent no-op (204), not an error — logout must not leak token validity.
    public async Task Logout_WithUnknownRefreshToken_StillReturnsNoContent()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new LoginRequestDto("admin", "Admin@12345"));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        var logoutResponse = await client.PostAsJsonAsync("/identity/logout", new LogoutRequestDto("not-a-real-refresh-token"));

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
    }
}
