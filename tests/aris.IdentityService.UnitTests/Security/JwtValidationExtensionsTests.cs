using aris.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace aris.IdentityService.UnitTests.Security;

public class JwtValidationExtensionsTests
{
    [Fact] // FR-2.1: the shared JWT wiring every service calls must default every unattributed endpoint to authenticated-only.
    public void AddArisJwtBearerValidation_ConfiguresFallbackPolicyToRequireAuthenticatedUser()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "ARIS.IdentityService",
                ["Jwt:Audience"] = "ARIS",
            })
            .Build();
        var services = new ServiceCollection();

        services.AddArisJwtBearerValidation(configuration, new FakeDevelopmentEnvironment());

        var authorizationOptions = services.BuildServiceProvider()
            .GetRequiredService<IOptions<AuthorizationOptions>>()
            .Value;

        Assert.NotNull(authorizationOptions.FallbackPolicy);
        Assert.Contains(
            authorizationOptions.FallbackPolicy!.Requirements,
            requirement => requirement is DenyAnonymousAuthorizationRequirement);
    }

    private sealed class FakeDevelopmentEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = nameof(JwtValidationExtensionsTests);
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
