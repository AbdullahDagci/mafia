using Microsoft.AspNetCore.Http.HttpResults;
using SusPayi.Api.Common;

namespace SusPayi.Api.Features.Seasons;

internal sealed record SeasonResponse(
    Guid Id,
    int Number,
    string Name,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset FinalPhaseStartsAt,
    int Day,
    int TotalDays,
    bool IsFinalPhase);

internal static class SeasonEndpoints
{
    public static RouteGroupBuilder MapSeasonEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/seasons").WithTags("Sezon");
        group.MapGet("/current", GetCurrent);
        return api;
    }

    private static async Task<Results<Ok<SeasonResponse>, ProblemHttpResult>> GetCurrent(GameSession session, CancellationToken ct)
    {
        var season = await session.FindCurrentSeasonAsync(ct);
        if (season is null)
        {
            return GameSession.NoSeason.ToProblem();
        }

        var now = session.Now;
        return TypedResults.Ok(new SeasonResponse(
            season.Id,
            season.Number,
            season.Name,
            season.StartsAt,
            season.EndsAt,
            season.FinalPhaseStartsAt,
            season.DayNumber(now),
            (int)(season.EndsAt - season.StartsAt).TotalDays,
            season.IsInFinalPhase(now)));
    }
}
