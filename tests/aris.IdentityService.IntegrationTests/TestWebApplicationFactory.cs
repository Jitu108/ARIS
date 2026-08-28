using aris.IdentityService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace aris.IdentityService.IntegrationTests;

/// <summary>
/// Boots the real Api composition root (Program.cs) against an in-memory SQLite database instead
/// of SQL Server, so the full HTTP pipeline — middleware, controller, JWT issuance/validation via
/// the app's own signing key — is exercised without requiring a live SQL Server instance.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public TestWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // AddDbContext registers more than just DbContextOptions<IdentityDbContext> (e.g.
            // IDbContextOptionsConfiguration<IdentityDbContext>, added additively) — removing only
            // that one leaves Program.cs's SqlServer configuration merged alongside this Sqlite one,
            // which EF rejects as two providers on the same context. Strip everything keyed to
            // IdentityDbContext before re-adding.
            foreach (var descriptor in services
                .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(IdentityDbContext)))
                .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<IdentityDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public void EnsureDatabaseCreated()
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.EnsureCreated();
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }
}
