using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using SusPayi.Api.Common;
using SusPayi.Api.Features.Auth;
using SusPayi.Api.Features.Characters;
using SusPayi.Api.Features.Crimes;
using SusPayi.Api.Features.Jail;
using SusPayi.Api.Features.Seasons;
using SusPayi.Domain.Accounts;
using SusPayi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    // Üretimde yapılandırılmış (JSON) log; log toplayıcı alanlara göre arayabilsin.
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GameMetrics>();
builder.Services.AddScoped<GameSession>();
builder.Services.AddSingleton<IPasswordHasher<Account>, PasswordHasher<Account>>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddGameRateLimiting();

// Belirteçler Data Protection ile şifrelenir. Birden fazla sunucuda anahtar halkası
// ortak ve kalıcı olmalı (README: Üretim notları).
builder.Services.AddDataProtection()
    .SetApplicationName("SusPayi")
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeysPath"] ?? "keys"));

builder.Services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken(options =>
    {
        options.BearerTokenExpiration = TimeSpan.FromHours(1);
        options.RefreshTokenExpiration = TimeSpan.FromDays(30);
    });

// Varsayılan olarak her uç kimlik ister; açık olanlar AllowAnonymous ile işaretlenir.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    if (app.Configuration.GetValue("Database:InitializeOnStartup", false))
    {
        await DevelopmentDatabase.InitializeAsync(app);
    }
}
else
{
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();

var api = app.MapGroup("/api/v1");
api.MapAuthEndpoints()
    .MapSeasonEndpoints()
    .MapCharacterEndpoints()
    .MapCrimeEndpoints()
    .MapJailEndpoints();

await app.RunAsync();

/// <summary>Entegrasyon testlerinde WebApplicationFactory için.</summary>
public partial class Program;
