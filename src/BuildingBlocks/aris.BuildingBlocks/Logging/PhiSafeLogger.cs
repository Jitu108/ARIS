using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace aris.BuildingBlocks.Logging;

/// <summary>
/// Structured-logging entry point every service uses instead of ILogger&lt;T&gt; directly.
/// Callers pass entity IDs and other non-identifying values as template arguments — never
/// PHI-shaped fields (name, DOB, MRN) — per the PHI-safe logging convention (Technical
/// Documentation §5.4). This wrapper forces templated logging; it does not itself inspect
/// argument content, so the convention still has to be honored at call sites.
/// </summary>
public interface IPhiSafeLogger<out T>
{
    void LogInformation(string messageTemplate, params object?[] args);
    void LogWarning(string messageTemplate, params object?[] args);
    void LogError(Exception? exception, string messageTemplate, params object?[] args);
}

internal sealed class PhiSafeLogger<T> : IPhiSafeLogger<T>
{
    private readonly ILogger<T> _logger;

    public PhiSafeLogger(ILogger<T> logger)
    {
        _logger = logger;
    }

    public void LogInformation(string messageTemplate, params object?[] args) =>
        _logger.LogInformation(messageTemplate, args);

    public void LogWarning(string messageTemplate, params object?[] args) =>
        _logger.LogWarning(messageTemplate, args);

    public void LogError(Exception? exception, string messageTemplate, params object?[] args) =>
        _logger.LogError(exception, messageTemplate, args);
}

public static class PhiSafeLoggingExtensions
{
    public static IServiceCollection AddPhiSafeLogging(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IPhiSafeLogger<>), typeof(PhiSafeLogger<>));
        return services;
    }
}
