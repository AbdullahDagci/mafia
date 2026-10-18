using Microsoft.EntityFrameworkCore;
using SusPayi.Domain.Rules;
using SusPayi.Domain.Seasons;
using SusPayi.Infrastructure.Persistence;

namespace SusPayi.Api.Common;

/// <summary>
/// Yalnızca geliştirme ortamında: migration'ları uygular ve açık sezon yoksa Sezon 1'i açar.
/// Üretimde migration'lar dağıtım adımında idempotent SQL betiğiyle uygulanır.
/// </summary>
internal static partial class DevelopmentDatabase
{
    public static async Task InitializeAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SusPayiDbContext>();
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentDatabase");

        if (!db.Database.GetMigrations().Any())
        {
            LogNoMigrations(logger);
            return;
        }

        await db.Database.MigrateAsync();

        var now = time.GetUtcNow();
        if (await db.Seasons.AnyAsync(s => s.StartsAt <= now && s.EndsAt > now))
        {
            return;
        }

        var nextNumber = (await db.Seasons.MaxAsync(s => (int?)s.Number) ?? 0) + 1;
        var season = Season.Create(
            nextNumber,
            "İstanbul Kuruluşu",
            now,
            length: TimeSpan.FromDays(84),
            finalPhaseLength: TimeSpan.FromDays(14),
            new SeasonRules());
        db.Seasons.Add(season);
        await db.SaveChangesAsync();
        LogSeasonCreated(logger, season.Number, season.Name);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Hiç migration yok. README'deki 'dotnet ef migrations add InitialCreate' adımını çalıştır.")]
    private static partial void LogNoMigrations(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Geliştirme için Sezon {Number} açıldı: {Name}")]
    private static partial void LogSeasonCreated(ILogger logger, int number, string name);
}
