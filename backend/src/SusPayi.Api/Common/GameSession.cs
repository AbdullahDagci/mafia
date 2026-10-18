using Microsoft.EntityFrameworkCore;
using SusPayi.Domain.Characters;
using SusPayi.Domain.Common;
using SusPayi.Domain.Seasons;
using SusPayi.Infrastructure.Persistence;

namespace SusPayi.Api.Common;

/// <summary>
/// Uçların ortak ihtiyacı: şu anki sezonu ve oyuncunun o sezondaki canlı karakterini yüklemek.
/// </summary>
internal sealed class GameSession(SusPayiDbContext db, TimeProvider time)
{
    public DateTimeOffset Now => time.GetUtcNow();

    public Task<Season?> FindCurrentSeasonAsync(CancellationToken ct)
    {
        var now = Now;
        return db.Seasons
            .AsNoTracking()
            .Where(s => s.StartsAt <= now && s.EndsAt > now)
            .OrderBy(s => s.StartsAt)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>Karakteri değişiklik takibiyle yükler; çağıran SaveChanges ile kaydeder.</summary>
    public async Task<Result<PlayerContext>> LoadPlayerAsync(Guid accountId, CancellationToken ct)
    {
        var season = await FindCurrentSeasonAsync(ct);
        if (season is null)
        {
            return NoSeason;
        }

        var character = await db.Characters
            .FirstOrDefaultAsync(c => c.AccountId == accountId && c.SeasonId == season.Id && c.IsAlive, ct);

        return character is null
            ? new GameError("character.none", "Bu sezonda karakterin yok. Önce bir karakter oluştur.", GameErrorKind.NotFound)
            : new PlayerContext(season, character);
    }

    public static GameError NoSeason { get; } =
        new("season.none", "Şu anda açık bir sezon yok.", GameErrorKind.NotFound);
}

internal sealed record PlayerContext(Season Season, Character Character);
