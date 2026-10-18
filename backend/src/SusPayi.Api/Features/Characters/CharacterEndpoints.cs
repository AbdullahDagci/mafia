using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SusPayi.Api.Common;
using SusPayi.Domain.Characters;
using SusPayi.Domain.Common;
using SusPayi.Infrastructure.Persistence;

namespace SusPayi.Api.Features.Characters;

internal sealed record CreateCharacterRequest(string Name);

internal static class CharacterEndpoints
{
    public static RouteGroupBuilder MapCharacterEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/seasons/current/characters", Create).WithTags("Karakter");
        api.MapGet("/me/character", GetMine).WithTags("Karakter");
        return api;
    }

    private static async Task<Results<Created<CharacterResponse>, ValidationProblem, ProblemHttpResult>> Create(
        CreateCharacterRequest request,
        ClaimsPrincipal user,
        GameSession session,
        SusPayiDbContext db,
        CancellationToken ct)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (!NameRules.PlayerName().IsMatch(name))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["Karakter adı 3–20 karakter olmalı; harf, rakam, nokta ve alt çizgi kullanılabilir."],
            });
        }

        var season = await session.FindCurrentSeasonAsync(ct);
        if (season is null)
        {
            return GameSession.NoSeason.ToProblem();
        }

        var accountId = user.GetAccountId();
        if (await db.Characters.AnyAsync(c => c.AccountId == accountId && c.SeasonId == season.Id && c.IsAlive, ct))
        {
            return new GameError("character.exists", "Bu sezonda zaten yaşayan bir karakterin var.", GameErrorKind.Conflict).ToProblem();
        }

        var now = session.Now;
        var character = Character.Create(accountId, season.Id, name, season.Rules, now);
        db.Characters.Add(character);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return new GameError("character.name_taken", "Bu ad bu sezonda alınmış ya da zaten bir karakterin var.", GameErrorKind.Conflict).ToProblem();
        }

        return TypedResults.Created("/api/v1/me/character", CharacterResponse.From(character, season.Rules, now));
    }

    private static async Task<Results<Ok<CharacterResponse>, ProblemHttpResult>> GetMine(
        ClaimsPrincipal user,
        GameSession session,
        CancellationToken ct)
    {
        var player = await session.LoadPlayerAsync(user.GetAccountId(), ct);
        if (!player.IsSuccess)
        {
            return player.Error!.ToProblem();
        }

        var (season, character) = player.Value;
        return TypedResults.Ok(CharacterResponse.From(character, season.Rules, session.Now));
    }
}
