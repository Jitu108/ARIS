using aris.BuildingBlocks.HealthChecks;
using aris.BuildingBlocks.Logging;
using aris.BuildingBlocks.Middleware;
using aris.BuildingBlocks.Security;
using aris.IdentityService.Infrastructure;
using aris.IdentityService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddPhiSafeLogging();

builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services
    .AddArisHealthChecks()
    .AddDbContextCheck<IdentityDbContext>("identity-db", tags: new[] { ArisHealthCheckExtensions.ReadyTag });

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
