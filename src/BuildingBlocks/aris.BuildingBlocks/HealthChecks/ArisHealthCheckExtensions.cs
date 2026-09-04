using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace aris.BuildingBlocks.HealthChecks;

/// <summary>
/// Standard /health/live + /health/ready contract every service implements identically.
/// Tag a dependency check (e.g. DB connectivity) with <see cref="ReadyTag"/> so it counts
/// toward readiness without affecting liveness.
/// </summary>
public static class ArisHealthCheckExtensions
{
    public const string ReadyTag = "ready";

    public static IHealthChecksBuilder AddArisHealthChecks(this IServiceCollection services)
    {
        return services.AddHealthChecks();
    }

    public static IEndpointRouteBuilder MapArisHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
        }).AllowAnonymous();

        return endpoints;
    }
}
