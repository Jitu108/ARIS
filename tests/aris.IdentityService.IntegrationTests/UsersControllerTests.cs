using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using aris.IdentityService.Application.Authentication;
using aris.IdentityService.Application.Users;

namespace aris.IdentityService.IntegrationTests;

public class UsersControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UsersControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseCreated();
    }

    [Fact] // FR-6.1 / FR-6.5: an Administrator creates a user, and that user can log in immediately with no separate activation step.
    public async Task CreateUser_AsAdministrator_ReturnsCreatedAndNewUserCanImmediatelyLogIn()
    {
        var client = await CreateAuthenticatedAdminClientAsync();
        var request = new CreateUserRequestDto("newcoder", "newcoder@aris.local", "P@ssword123", "New Coder", new[] { "Coder" });

        var response = await client.PostAsJsonAsync("/identity/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateUserResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("newcoder", body!.Username);
        Assert.True(body.IsActive);
        Assert.Equal(new[] { "Coder" }, body.Roles);

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/identity/login",
            new LoginRequestDto("newcoder", "P@ssword123"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact] // FR-6.4: a duplicate username/email is rejected with a specific (non-generic) reason.
    public async Task CreateUser_WithUsernameAlreadyInUse_ReturnsConflict()
    {
        var client = await CreateAuthenticatedAdminClientAsync();
        var request = new CreateUserRequestDto("admin", "someoneelse@aris.local", "P@ssword123", "Someone Else", new[] { "Coder" });

        var response = await client.PostAsJsonAsync("/identity/users", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Username or email already in use.", body);
    }

    [Fact] // FR-6.1: an unrecognized role name is rejected.
    public async Task CreateUser_WithUnknownRole_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedAdminClientAsync();
        var request = new CreateUserRequestDto("newuser", "newuser@aris.local", "P@ssword123", "New User", new[] { "NotARole" });

        var response = await client.PostAsJsonAsync("/identity/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] // FR-6.6: an unauthenticated caller cannot create a user.
    public async Task CreateUser_WithoutBearerToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new CreateUserRequestDto("newuser", "newuser@aris.local", "P@ssword123", "New User", new[] { "Coder" });

        var response = await client.PostAsJsonAsync("/identity/users", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact] // FR-6.6: a non-Administrator's credentials cannot perform this action, enforced server-side regardless of the UI.
    public async Task CreateUser_AsNonAdministrator_ReturnsForbidden()
    {
        var adminClient = await CreateAuthenticatedAdminClientAsync();
        var coderCreateRequest = new CreateUserRequestDto("plaincoder", "plaincoder@aris.local", "P@ssword123", "Plain Coder", new[] { "Coder" });
        await adminClient.PostAsJsonAsync("/identity/users", coderCreateRequest);

        var coderClient = _factory.CreateClient();
        var loginResponse = await coderClient.PostAsJsonAsync("/identity/login", new LoginRequestDto("plaincoder", "P@ssword123"));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        coderClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        var response = await coderClient.PostAsJsonAsync(
            "/identity/users",
            new CreateUserRequestDto("shouldnotexist", "shouldnotexist@aris.local", "P@ssword123", "Should Not Exist", new[] { "Coder" }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/identity/login", new LoginRequestDto("admin", "Admin@12345"));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
        return client;
    }
}
