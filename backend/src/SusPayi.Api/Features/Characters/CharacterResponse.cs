using SusPayi.Domain.Characters;
using SusPayi.Domain.Rules;

namespace SusPayi.Api.Features.Characters;

internal sealed record CharacterResponse(
    Guid Id,
    string Name,
    int RankIndex,
    string RankName,
    double RankProgressPercent,
    long Money,
    int Health,
    int Bullets,
    int Busts,
    bool IsAlive,
    bool IsJailed,
    DateTimeOffset? JailedUntil,
    long BribeCost,
    DateTimeOffset? CrimeReadyAt,
    DateTimeOffset? BustReadyAt,
    DateTimeOffset ProtectedUntil)
{
    public static CharacterResponse From(Character character, SeasonRules rules, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(character);
        var jailed = character.IsJailed(now);

        return new CharacterResponse(
            character.Id,
            character.Name,
            character.RankIndex,
            character.RankName,
            Math.Round(character.RankPoints / (double)Ranks.PointsPerRank * 100, 2),
            character.Money,
            character.Health,
            character.Bullets,
            character.Busts,
            character.IsAlive,
            jailed,
            jailed ? character.JailedUntil : null,
            character.BribeCost(rules),
            character.CrimeReadyAt > now ? character.CrimeReadyAt : null,
            character.BustReadyAt > now ? character.BustReadyAt : null,
            character.ProtectedUntil);
    }
}
