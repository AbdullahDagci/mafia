namespace SusPayi.Domain.Characters;

public enum CrimeResultKind
{
    /// <summary>İş tuttu: para ve rütbe puanı kazanıldı.</summary>
    Success,

    /// <summary>İş yattı ama polis görmedi.</summary>
    Escaped,

    /// <summary>Yakalandı, hapis süresi başladı.</summary>
    Caught,
}

public sealed record CrimeOutcome(
    string CrimeId,
    CrimeResultKind Result,
    int ChancePercent,
    int Roll,
    long Reward,
    int RankPointsGained,
    bool Promoted,
    DateTimeOffset? JailedUntil,
    DateTimeOffset NextCrimeAt);

public sealed record BustOutcome(
    Guid InmateId,
    bool Freed,
    int ChancePercent,
    int Roll,
    int RankPointsGained,
    bool Promoted,
    DateTimeOffset? BusterJailedUntil,
    DateTimeOffset NextBustAt);

public sealed record BribeOutcome(long Cost);
