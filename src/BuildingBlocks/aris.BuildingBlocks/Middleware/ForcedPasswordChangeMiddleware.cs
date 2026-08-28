using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace aris.BuildingBlocks.Middleware;

/// <summary>
/// Runs after JWT validation in every service. If the authenticated user's
/// MustChangePassword claim is true, every route is rejected except the fixed
/// allow-list — this is what stops a user in that state from reaching any
/// service by going around IdentityService (Technical Documentation §5.2).
/// </summary>
public sealed class ForcedPasswordChangeMiddleware
{
    public const string MustChangePasswordClaimType = "must_change_password";

    private static readonly string[] AllowListedPaths =
    {
        "/identity/change-password",
        "/identity/me",
        "/identity/logout",
        "/identity/refresh",
    };

    private readonly RequestDelegate _next;

    public ForcedPasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        var mustChangePassword = user.Identity?.IsAuthenticated == true
            && string.Equals(user.FindFirst(MustChangePasswordClaimType)?.Value, "true", StringComparison.OrdinalIgnoreCase);

        if (mustChangePassword && !IsAllowListed(context.Request.Path))
        {
            var problem = new ProblemDetails
            {
                Type = "https://aris.dev/problems/password-change-required",
                Title = "Password change required.",
                Status = StatusCodes.Status403Forbidden,
                Detail = "This account must change its password before continuing.",
            };
            problem.Extensions["traceId"] = context.GetCorrelationId();

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(problem);
            return;
        }

        await _next(context);
    }

    private static bool IsAllowListed(PathString path)
    {
        return AllowListedPaths.Any(allowed => path.StartsWithSegments(allowed, StringComparison.OrdinalIgnoreCase));
    }
}
