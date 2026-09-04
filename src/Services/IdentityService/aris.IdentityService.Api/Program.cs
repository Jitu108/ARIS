using aris.BuildingBlocks.HealthChecks;
using aris.BuildingBlocks.Logging;
using aris.BuildingBlocks.Middleware;
using aris.BuildingBlocks.Security;
using aris.IdentityService.Infrastructure;
using aris.IdentityService.Infrastructure.Persistence;
using aris.IdentityService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddPhiSafeLogging();

builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services
    .AddArisHealthChecks()
    .AddDbContextCheck<IdentityDbContext>("identity-db", tags: new[] { ArisHealthCheckExtensions.ReadyTag });

// Resolve the signing key and seed Jwt:PublicKeyPem from it *before* AddArisJwtBearerValidation
// runs, so IdentityService's own token validation uses the exact key it just signed with
// (see ArisSigningKeyProvider remarks — otherwise Development would generate two unrelated keys).
var signingKey = ArisSigningKeyProvider.Resolve(builder.Configuration, builder.Environment);
builder.Configuration["Jwt:PublicKeyPem"] = signingKey.PublicKeyPem;
builder.Services.AddSingleton(signingKey);

builder.Services.AddArisJwtBearerValidation(builder.Configuration, builder.Environment);

var app = builder.Build();

// Applies pending migrations (and HasData seeding) before the app accepts any requests, so
// `docker compose up` produces a working, seeded database with no manual `dotnet ef database
// update` step. Phase 1 has no real production deployment target to gate this against (that's
// Phase 6) — a real migration strategy belongs with whichever later phase introduces one.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    // Migrations are generated for SqlServer (the only real deployment target); skip when a
    // different provider is configured, e.g. the integration test suite's SQLite substitution,
    // which manages its own schema via Database.EnsureCreated() instead.
    if (dbContext.Database.IsSqlServer())
    {
        dbContext.Database.Migrate();
    }
}

app.UseArisRequestPipeline();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseArisForcedPasswordChangeGate();
app.UseAuthorization();

app.MapControllers();
app.MapArisHealthChecks();

app.Run();

// Exposes the top-level statements' generated entry point as a public type so
// WebApplicationFactory<Program> is usable from the integration test project.
public partial class Program;
