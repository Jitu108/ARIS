using aris.BuildingBlocks.Logging;
using aris.IdentityService.Application.Abstractions;
using aris.IdentityService.Application.Authentication;
using aris.IdentityService.Application.Users;
using aris.IdentityService.Infrastructure.Persistence;
using aris.IdentityService.Infrastructure.Repositories;
using aris.IdentityService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace aris.IdentityService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("IdentityDb")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAuthAuditEventRepository, AuthAuditEventRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        var refreshTokenExpiryDays = configuration.GetValue<int?>("Jwt:RefreshTokenExpiryDays") ?? 14;
        services.AddScoped<IAuthenticationService>(sp => new AuthenticationService(
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<IRefreshTokenRepository>(),
            sp.GetRequiredService<IPasswordHasher>(),
            sp.GetRequiredService<IJwtTokenGenerator>(),
            sp.GetRequiredService<IPhiSafeLogger<AuthenticationService>>(),
            refreshTokenExpiryDays));

        return services;
    }
}
