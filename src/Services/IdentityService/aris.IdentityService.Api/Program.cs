using aris.BuildingBlocks.HealthChecks;
using aris.BuildingBlocks.Logging;
using aris.BuildingBlocks.Middleware;
using aris.BuildingBlocks.Security;
using aris.IdentityService.Infrastructure;
using aris.IdentityService.Infrastructure.Persistence;
using aris.IdentityService.Infrastructure.Security;

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

app.UseArisRequestPipeline();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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
