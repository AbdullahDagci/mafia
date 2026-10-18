using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SusPayi.Api.Common;
using SusPayi.Api.Features.Characters;
using SusPayi.Domain.Actions;
using SusPayi.Domain.Characters;
using SusPayi.Domain.Common;
using SusPayi.Domain.Rules;
using SusPayi.Infrastructure.Persistence;

namespace SusPayi.Api.Features.Crimes;

internal sealed record CrimeResponse(
    string Id,
    string Name,
    int ChancePercent,
    long MinReward,
    long MaxReward,
    double RankProgressPercent,
    int JailMinutes);

internal sealed record CrimeAttemptResponse(
    string CrimeId,
    string Result,
    int ChancePercent,
    long Reward,
    bool Promoted,
    DateTimeOffset? JailedUntil,
    DateTimeOffset NextCrimeAt,
    CharacterResponse Character);

internal static class CrimeEndpoints
{
    public static RouteGroupBuilder MapCrimeEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/crimes").WithTags("Suçlar");
        group.MapGet("/", List);
        group.MapPost("/{crimeId}/attempts", Attempt).RequireRateLimiting(RateLimitPolicies.GameAction);
        return api;
    }

    private static async Task<Results<Ok<IReadOnlyList<CrimeResponse>>, ProblemHttpResult>> List(
        ClaimsPrincipal user,
        GameSession session,
        CancellationToken ct)
    {
        var player = await session.LoadPlayerAsync(user.GetAccountId(), ct);
        if (!player.IsSuccess)
        {
            return player.Error!.ToProblem();
        }

        var rank = player.Value.Character.RankIndex;
        IReadOnlyList<CrimeResponse> crimes = CrimeCatalog.All
            .Select(c => new CrimeResponse(
                c.Id,
                c.Name,
                c.SuccessChancePercent(rank),
                c.MinReward,
                c.MaxReward,
                c.RankPoints / (double)Ranks.PointsPerRank * 100,
                (int)c.JailTime.TotalMinutes))
            .ToList();

        return TypedResults.Ok(crimes);
    }

    private static async Task<Results<Ok<CrimeAttemptResponse>, ProblemHttpResult>> Attempt(
        string crimeId,
        ClaimsPrincipal user,
        GameSession session,
        SusPayiDbContext db,
        IRandomSource random,
        GameMetrics metrics,
        CancellationToken ct)
    {
        var crime = CrimeCatalog.Find(crimeId);
        if (crime is null)
        {
            return new GameError("crime.not_found", "Böyle bir iş yok.", GameErrorKind.NotFound).ToProblem();
        }

        var player = await session.LoadPlayerAsync(user.GetAccountId(), ct);
        if (!player.IsSuccess)
        {
            return player.Error!.ToProblem();
        }

        var (season, character) = player.Value;
        var now = session.Now;
        var result = character.CommitCrime(crime, season.Rules, now, random);
        if (!result.IsSuccess)
        {
            return result.Error!.ToProblem();
        }

        var outcome = result.Value;
        var resultName = outcome.Result.ToString().ToLowerInvariant();
        db.ActionLog.Add(ActionLogEntry.Create(
            season.Id,
            character.Id,
            type: "crime",
            outcome: resultName,
            occurredAt: now,
            subject: crime.Id,
            chancePercent: outcome.ChancePercent,
            roll: outcome.Roll,
            moneyDelta: outcome.Reward,
            rankPointsDelta: outcome.RankPointsGained));

        // Karakterin sürüm belirteci, aynı anda gelen ikinci isteğin bekleme süresini atlamasını engeller.
        await db.SaveChangesAsync(ct);

        metrics.CrimeAttempted(crime.Id, resultName);
        if (outcome.Promoted)
        {
            metrics.Promoted();
        }

        return TypedResults.Ok(new CrimeAttemptResponse(
            crime.Id,
            resultName,
            outcome.ChancePercent,
            outcome.Reward,
            outcome.Promoted,
            outcome.JailedUntil,
            outcome.NextCrimeAt,
            CharacterResponse.From(character, season.Rules, now)));
    }
}
