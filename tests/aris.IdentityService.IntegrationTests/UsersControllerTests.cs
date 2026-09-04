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

    [Fact] // IT-ID-13 / FR-6.7: an Administrator can browse a paginated, filtered list of accounts.
    public async Task ListUsers_AsAdministrator_ReturnsPaginatedResults()
    {
        var client = await CreateAuthenticatedAdminClientAsync();
        await client.PostAsJsonAsync(
            "/identity/users",
            new CreateUserRequestDto("listuser1", "listuser1@aris.local", "P@ssword123", "List User One", new[] { "Coder" }));

        var response = await client.GetAsync("/identity/users?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListUsersResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.Page);
        Assert.Equal(20, body.PageSize);
        Assert.Contains(body.Items, item => item.Username == "listuser1" && item.Roles.Contains("Coder"));
    }

    [Fact] // IT-ID-13 / FR-6.7: a non-matching query is a correct empty result, not an error.
    public async Task ListUsers_WithNonMatchingQuery_ReturnsEmptyResult()
    {
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync("/identity/users?query=no-such-account-exists");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListUsersResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(0, body!.TotalCount);
        Assert.Empty(body.Items);
    }

    [Fact] // IT-ID-13 / FR-6.6: a non-Administrator cannot browse the user list, enforced server-side regardless of the UI.
    public async Task ListUsers_AsNonAdministrator_ReturnsForbidden()
    {
        var adminClient = await CreateAuthenticatedAdminClientAsync();
        await adminClient.PostAsJsonAsync(
            "/identity/users",
            new CreateUserRequestDto("plainlistcoder", "plainlistcoder@aris.local", "P@ssword123", "Plain List Coder", new[] { "Coder" }));

        var coderClient = _factory.CreateClient();
        var loginResponse = await coderClient.PostAsJsonAsync("/identity/login", new LoginRequestDto("plainlistcoder", "P@ssword123"));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        coderClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        var response = await coderClient.GetAsync("/identity/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // IT-ID-13 / FR-6.6: an unauthenticated caller cannot browse the user list.
    public async Task ListUsers_WithoutBearerToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/identity/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact] // IT-ID-11 / FR-6.3: an Administrator can retrieve a specific user's account/role details by id.
    public async Task GetUser_AsAdministrator_ReturnsUserWithRoles()
    {
        var client = await CreateAuthenticatedAdminClientAsync();
        var createResponse = await client.PostAsJsonAsync(
            "/identity/users",
            new CreateUserRequestDto("detailuser1", "detailuser1@aris.local", "P@ssword123", "Detail User One", new[] { "Coder" }));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponseDto>();

        var response = await client.GetAsync($"/identity/users/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto>();
        Assert.NotNull(body);
        Assert.Equal("detailuser1", body!.Username);
        Assert.Equal(new[] { "Coder" }, body.Roles);
    }

    [Fact] // IT-ID-11 / FR-6.3: an unknown user id is a 404.
    public async Task GetUser_WithUnknownId_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync($"/identity/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact] // IT-ID-11 / FR-6.6: a non-Administrator cannot retrieve a user's account/role details, enforced server-side regardless of the UI.
    public async Task GetUser_AsNonAdministrator_ReturnsForbidden()
    {
        var adminClient = await CreateAuthenticatedAdminClientAsync();
        var createResponse = await adminClient.PostAsJsonAsync(
            "/identity/users",
            new CreateUserRequestDto("plaindetailcoder", "plaindetailcoder@aris.local", "P@ssword123", "Plain Detail Coder", new[] { "Coder" }));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponseDto>();

        var coderClient = _factory.CreateClient();
        var loginResponse = await coderClient.PostAsJsonAsync("/identity/login", new LoginRequestDto("plaindetailcoder", "P@ssword123"));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        coderClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        var response = await coderClient.GetAsync($"/identity/users/{created!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // IT-ID-12 / FR-6.2: an Administrator updates a user's roles, and a subsequent GET reflects the change.
    public async Task ChangeUserRoles_AsAdministrator_UpdatesRolesAndPersists()
    {
        var client = await CreateAuthenticatedAdminClientAsync();
        var createResponse = await client.PostAsJsonAsync(
            "/identity/users",
            new CreateUserRequestDto("rolechangeuser1", "rolechangeuser1@aris.local", "P@ssword123", "Role Change User One", new[] { "Coder" }));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponseDto>();

        var putResponse = await client.PutAsJsonAsync(
            $"/identity/users/{created!.Id}/roles",
            new ChangeUserRolesRequestDto(new[] { "Coder", "RiskAnalyst" }));

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var putBody = await putResponse.Content.ReadFromJsonAsync<UserSummaryDto>();
        Assert.NotNull(putBody);
        Assert.Equal(new[] { "Coder", "RiskAnalyst" }, putBody!.Roles.OrderBy(role => role));

        var getResponse = await client.GetAsync($"/identity/users/{created.Id}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<UserSummaryDto>();
        Assert.Equal(new[] { "Coder", "RiskAnalyst" }, getBody!.Roles.OrderBy(role => role));
    }

    [Fact] // IT-ID-12 / FR-6.2: an unrecognized role name is rejected.
    public async Task ChangeUserRoles_WithUnknownRole_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedAdminClientAsync();
        var createResponse = await client.PostAsJsonAsync(
            "/identity/users",
            new CreateUserRequestDto("rolechangeuser2", "rolechangeuser2@aris.local", "P@ssword123", "Role Change User Two", new[] { "Coder" }));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponseDto>();

        var response = await client.PutAsJsonAsync(
            $"/identity/users/{created!.Id}/roles",
            new ChangeUserRolesRequestDto(new[] { "NotARole" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] // IT-ID-12 / FR-6.2: changing roles for a non-existent user is a 404.
    public async Task ChangeUserRoles_WithUnknownUserId_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/identity/users/{Guid.NewGuid()}/roles",
            new ChangeUserRolesRequestDto(new[] { "Coder" }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact] // IT-ID-12 / FR-6.6: a non-Administrator cannot change a user's roles, enforced server-side regardless of the UI.
    public async Task ChangeUserRoles_AsNonAdministrator_ReturnsForbidden()
    {
        var adminClient = await CreateAuthenticatedAdminClientAsync();
        var createResponse = await adminClient.PostAsJsonAsync(
            "/identity/users",
            new CreateUserRequestDto("plainrolechangecoder", "plainrolechangecoder@aris.local", "P@ssword123", "Plain Role Change Coder", new[] { "Coder" }));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponseDto>();

        var coderClient = _factory.CreateClient();
        var loginResponse = await coderClient.PostAsJsonAsync("/identity/login", new LoginRequestDto("plainrolechangecoder", "P@ssword123"));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        coderClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        var response = await coderClient.PutAsJsonAsync(
            $"/identity/users/{created!.Id}/roles",
            new ChangeUserRolesRequestDto(new[] { "RiskAnalyst" }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // IT-ID-14 / FR-6.8: deactivate sets the account inactive, revokes its outstanding refresh token(s), and a subsequent login returns the same generic 401 as any invalid login.
    public async Task DeactivateUser_AsAdministrator_RevokesSessionsAndBlocksFutureLogin()
    {
        var client = await CreateAuthenticatedAdminClientAsync();
        var createResponse = await client.PostAsJsonAsync(
            "/identity/users",
            new CreateUserRequestDto("deactivateuser1", "deactivateuser1@aris.local", "P@ssword123", "Deactivate User One", new[] { "Coder" }));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponseDto>();

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/identity/login",
            new LoginRequestDto("deactivateuser1", "P@ssword123"));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        var deactivateResponse = await client.PostAsync($"/identity/users/{created!.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        var refreshResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/identity/refresh",
            new RefreshRequestDto(loginBody!.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);

        var secondLoginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/identity/login",
            new LoginRequestDto("deactivateuser1", "P@ssword123"));
        Assert.Equal(HttpStatusCode.Unauthorized, secondLoginResponse.StatusCode);
    }

    [Fact] // IT-ID-15 / FR-6.8: deactivating an already-inactive account is a conflict.
    public async Task DeactivateUser_OnAlreadyInactiveAccount_ReturnsConflict()
    {
        var client = await CreateAuthenticatedAdminClientAsync();
        var createResponse = await client.PostAsJsonAsync(
            "/identity/users",
            new CreateUserRequestDto("deactivateuser2", "deactivateuser2@aris.local", "P@ssword123", "Deactivate User Two", new[] { "Coder" }));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponseDto>();
        await client.PostAsync($"/identity/users/{created!.Id}/deactivate", null);

        var response = await client.PostAsync($"/identity/users/{created.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact] // IT-ID-15 / FR-6.6: a non-Administrator cannot deactivate a user, enforced server-side regardless of the UI.
    public async Task DeactivateUser_AsNonAdministrator_ReturnsForbidden()
    {
        var adminClient = await CreateAuthenticatedAdminClientAsync();
        var createResponse = await adminClient.PostAsJsonAsync(
            "/identity/users",
            new CreateUserRequestDto("plaindeactivatecoder", "plaindeactivatecoder@aris.local", "P@ssword123", "Plain Deactivate Coder", new[] { "Coder" }));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateUserResponseDto>();

        var coderClient = _factory.CreateClient();
        var loginResponse = await coderClient.PostAsJsonAsync("/identity/login", new LoginRequestDto("plaindeactivatecoder", "P@ssword123"));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        coderClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        var response = await coderClient.PostAsync($"/identity/users/{created!.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // IT-ID-15 / FR-6.8: deactivating an unknown user id is a 404.
    public async Task DeactivateUser_WithUnknownUserId_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.PostAsync($"/identity/users/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
