using Microsoft.AspNetCore.Builder;

namespace aris.BuildingBlocks.Middleware;

public static class RequestPipelineExtensions
{
    /// <summary>Correlation-id capture and unhandled-exception mapping. Call first, before auth.</summary>
    public static IApplicationBuilder UseArisRequestPipeline(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }

    /// <summary>Forced-password-change gate. Call after UseAuthentication(), before UseAuthorization()/endpoints.</summary>
    public static IApplicationBuilder UseArisForcedPasswordChangeGate(this IApplicationBuilder app)
    {
        app.UseMiddleware<ForcedPasswordChangeMiddleware>();
        return app;
    }
}
