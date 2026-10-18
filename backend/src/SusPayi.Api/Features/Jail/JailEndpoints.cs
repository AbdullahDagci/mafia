using System.Buffers.Text;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SusPayi.Api.Common;
using SusPayi.Api.Features.Characters;
using SusPayi.Domain.Actions;
using SusPayi.Domain.Common;
using SusPayi.Domain.Rules;
using SusPayi.Infrastructure.Persistence;

namespace SusPayi.Api.Features.Jail;

internal sealed record InmateResponse(Guid Id, string Name, string RankName, DateTimeOffset JailedUntil, bool IsMe);

internal sealed record InmatePage(IReadOnlyList<InmateResponse> Items, string? NextCursor);

internal sealed record BustResponse(Guid InmateId, bool Freed, int ChancePercent, bool Promoted, DateTimeOffset? JailedUntil, DateTimeOffset NextBustAt, CharacterResponse Character);

internal sealed record BribeResponse(long Cost, CharacterResponse Character);

internal static class JailEndpoints
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;

    public static RouteGroupBuilder MapJailEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/jail").WithTags("Hapishane");
        group.MapGet("/", List);
        group.MapPost("/{inmateId:guid}/bust", Bust).RequireRateLimiting(RateLimitPolicies.GameAction);
        group.MapPost("/bribe", Bribe).RequireRateLimiting(RateLimitPolicies.GameAction);
        return api;
    }

    /// <summary>
    /// Hapisteki karakterler, çıkış zamanı yakın olandan uzağa. Anahtar tabanlı sayfalama:
    /// liste sürekli değiştiği için Skip/Take kayan sonuçlar verirdi.
    /// </summary>
    private static async Task<Results<Ok<InmatePage>, ValidationProblem, ProblemHttpResult>> List(
        ClaimsPrincipal user,
        GameSession session,
        SusPayiDbContext db,
        string? cursor,
        int? limit,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(limit ?? DefaultPageSize, 1, MaxPageSize);
        if (!TryDecodeCursor(cursor, out var after))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["cursor"] = ["Geçersiz sayfa imleci."] });
        }

        var season = await session.FindCurrentSeasonAsync(ct);
        if (season is null)
        {
            return GameSession.NoSeason.ToProblem();
        }

        var now = session.Now;
        var accountId = user.GetAccountId();
        var query = db.Characters
            .AsNoTracking()
            .Where(c => c.SeasonId == season.Id && c.IsAlive && c.JailedUntil > now);

        if (after is { } position)
        {
            query = query.Where(c => c.JailedUntil > position.JailedUntil
                || (c.JailedUntil == position.JailedUntil && c.Id.CompareTo(position.Id) > 0));
        }

        var rows = await query
            .OrderBy(c => c.JailedUntil)
            .ThenBy(c => c.Id)
            .Take(pageSize + 1)
            .Select(c => new { c.Id, c.Name, c.RankIndex, JailedUntil = c.JailedUntil!.Value, c.AccountId })
            .ToListAsync(ct);

        var items = rows
            .Take(pageSize)
            .Select(r => new InmateResponse(r.Id, r.Name, Ranks.NameOf(r.RankIndex), r.JailedUntil, r.AccountId == accountId))
            .ToList();

        var nextCursor = rows.Count > pageSize ? EncodeCursor(items[^1].JailedUntil, items[^1].Id) : null;
        return TypedResults.Ok(new InmatePage(items, nextCursor));
    }

    private static async Task<Results<Ok<BustResponse>, ProblemHttpResult>> Bust(
        Guid inmateId,
        ClaimsPrincipal user,
        GameSession session,
        SusPayiDbContext db,
        IRandomSource random,
        GameMetrics metrics,
        CancellationToken ct)
    {
        var player = await session.LoadPlayerAsync(user.GetAccountId(), ct);
        if (!player.IsSuccess)
        {
            return player.Error!.ToProblem();
        }

        var (season, buster) = player.Value;
        var inmate = await db.Characters.FirstOrDefaultAsync(c => c.Id == inmateId && c.SeasonId == season.Id, ct);
        if (inmate is null)
        {
            return new GameError("bust.inmate_not_found", "Bu mahkûm bulunamadı.", GameErrorKind.NotFound).ToProblem();
        }

        var now = session.Now;
        var result = buster.Bust(inmate, season.Rules, now, random);
        if (!result.IsSuccess)
        {
            return result.Error!.ToProblem();
        }

        var outcome = result.Value;
        db.ActionLog.Add(ActionLogEntry.Create(
            season.Id,
            buster.Id,
            type: "bust",
            outcome: outcome.Freed ? "freed" : "failed",
            occurredAt: now,
            targetCharacterId: inmate.Id,
            chancePercent: outcome.ChancePercent,
            roll: outcome.Roll,
            rankPointsDelta: outcome.RankPointsGained));

        // İki kişi aynı mahkûmu aynı anda kurtarmaya çalışırsa sürüm belirteci birini reddeder.
        await db.SaveChangesAsync(ct);

        metrics.BustAttempted(outcome.Freed);
        if (outcome.Promoted)
        {
            metrics.Promoted();
        }

        return TypedResults.Ok(new BustResponse(
            inmate.Id,
            outcome.Freed,
            outcome.ChancePercent,
            outcome.Promoted,
            outcome.BusterJailedUntil,
            outcome.NextBustAt,
            CharacterResponse.From(buster, season.Rules, now)));
    }

    private static async Task<Results<Ok<BribeResponse>, ProblemHttpResult>> Bribe(
        ClaimsPrincipal user,
        GameSession session,
        SusPayiDbContext db,
        CancellationToken ct)
    {
        var player = await session.LoadPlayerAsync(user.GetAccountId(), ct);
        if (!player.IsSuccess)
        {
            return player.Error!.ToProblem();
        }

        var (season, character) = player.Value;
        var now = session.Now;
        var result = character.BribeOutOfJail(season.Rules, now);
        if (!result.IsSuccess)
        {
            return result.Error!.ToProblem();
        }

        db.ActionLog.Add(ActionLogEntry.Create(
            season.Id,
            character.Id,
            type: "bribe",
            outcome: "paid",
            occurredAt: now,
            moneyDelta: -result.Value.Cost));
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new BribeResponse(result.Value.Cost, CharacterResponse.From(character, season.Rules, now)));
    }

    private readonly record struct CursorPosition(DateTimeOffset JailedUntil, Guid Id);

    private static string EncodeCursor(DateTimeOffset jailedUntil, Guid id) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(
            string.Create(CultureInfo.InvariantCulture, $"{jailedUntil.UtcTicks}:{id:N}")));

    private static bool TryDecodeCursor(string? cursor, out CursorPosition? position)
    {
        position = null;
        if (string.IsNullOrEmpty(cursor))
        {
            return true;
        }

        try
        {
            var raw = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(cursor));
            var parts = raw.Split(':');
            if (parts.Length == 2
                && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
                && Guid.TryParseExact(parts[1], "N", out var id))
            {
                position = new CursorPosition(new DateTimeOffset(ticks, TimeSpan.Zero), id);
                return true;
            }
        }
        catch (FormatException)
        {
            // Geçersiz base64; aşağıda false dönülür.
        }

        return false;
    }
}
