using SusPayi.Domain.Characters;
using SusPayi.Domain.Rules;

namespace SusPayi.Domain.Tests;

/// <summary>Sırayla verilen zar sonuçlarını döndüren sahte kaynak.</summary>
internal sealed class ScriptedRandom(params int[] percentRolls) : IRandomSource
{
    private readonly Queue<int> _rolls = new(percentRolls);

    public long? FixedAmount { get; init; }

    public int NextPercentRoll() =>
        _rolls.Count > 0 ? _rolls.Dequeue() : throw new InvalidOperationException("Senaryoda yeterli zar yok.");

    public long NextInt64(long minInclusive, long maxExclusive) => FixedAmount ?? minInclusive;
}

internal static class Fixtures
{
    public static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    public static SeasonRules DefaultRules { get; } = new();

    public static Guid SeasonId { get; } = Guid.CreateVersion7(Now);

    public static Character NewCharacter(string name = "Sessiz") =>
        Character.Create(Guid.CreateVersion7(Now), SeasonId, name, DefaultRules, Now);

    public static CrimeDefinition Pickpocket => CrimeCatalog.Find("pickpocket")!;
}
