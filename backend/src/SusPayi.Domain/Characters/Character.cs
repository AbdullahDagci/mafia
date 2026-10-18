using SusPayi.Domain.Common;
using SusPayi.Domain.Rules;

namespace SusPayi.Domain.Characters;

/// <summary>
/// Bir hesabın tek bir sezondaki karakteri. Tüm oyun kuralları burada uygulanır;
/// API katmanı yalnızca yükler, çağırır ve kaydeder.
/// </summary>
public sealed class Character
{
    public const int NameMinLength = 3;
    public const int NameMaxLength = 20;

    private Character()
    {
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid SeasonId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int RankIndex { get; private set; }

    /// <summary>Mevcut rütbedeki ilerleme; 0–10.000 (10.000 = %100).</summary>
    public int RankPoints { get; private set; }

    public long Money { get; private set; }

    public int Health { get; private set; }

    public int Bullets { get; private set; }

    public int Busts { get; private set; }

    public bool IsAlive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ProtectedUntil { get; private set; }

    public DateTimeOffset? JailedUntil { get; private set; }

    public DateTimeOffset? CrimeReadyAt { get; private set; }

    public DateTimeOffset? BustReadyAt { get; private set; }

    /// <summary>İyimser eşzamanlılık belirteci; aynı karakterin paralel isteklerle çift harcanmasını önler.</summary>
    public uint Version { get; private set; }

    public string RankName => Ranks.NameOf(RankIndex);

    public static Character Create(Guid accountId, Guid seasonId, string name, SeasonRules rules, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(rules);

        return new Character
        {
            Id = Guid.CreateVersion7(now),
            AccountId = accountId,
            SeasonId = seasonId,
            Name = name.Trim(),
            RankIndex = 0,
            RankPoints = 0,
            Money = rules.StartingMoney,
            Health = 100,
            Bullets = 0,
            IsAlive = true,
            CreatedAt = now,
            ProtectedUntil = now + rules.NewPlayerProtection,
        };
    }

    public bool IsJailed(DateTimeOffset now) => JailedUntil is { } until && until > now;

    public long BribeCost(SeasonRules rules) => rules.BribeBaseCost + (RankIndex * rules.BribeCostPerRank);

    public Result<CrimeOutcome> CommitCrime(CrimeDefinition crime, SeasonRules rules, DateTimeOffset now, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(crime);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(random);

        if (CheckCanAct(now) is { } blocked)
        {
            return blocked;
        }

        if (CrimeReadyAt is { } ready && ready > now)
        {
            return new GameError("crime.cooldown", "Bir sonraki iş için beklemen gerekiyor.", GameErrorKind.Conflict, ready);
        }

        var chance = crime.SuccessChancePercent(RankIndex);
        var roll = random.NextPercentRoll();
        CrimeReadyAt = now + rules.CrimeCooldown;

        if (roll < chance)
        {
            var reward = random.NextInt64(crime.MinReward, crime.MaxReward + 1);
            Money += reward;
            var promoted = AddRankPoints(crime.RankPoints);
            return new CrimeOutcome(crime.Id, CrimeResultKind.Success, chance, roll, reward, crime.RankPoints, promoted, null, CrimeReadyAt.Value);
        }

        if (random.NextPercentRoll() < rules.EscapeChanceOnFailurePercent)
        {
            return new CrimeOutcome(crime.Id, CrimeResultKind.Escaped, chance, roll, 0, 0, false, null, CrimeReadyAt.Value);
        }

        JailedUntil = now + crime.JailTime;
        return new CrimeOutcome(crime.Id, CrimeResultKind.Caught, chance, roll, 0, 0, false, JailedUntil, CrimeReadyAt.Value);
    }

    public Result<BustOutcome> Bust(Character inmate, SeasonRules rules, DateTimeOffset now, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(inmate);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(random);

        if (inmate.Id == Id)
        {
            return new GameError("bust.self", "Kendini bu yolla kurtaramazsın; rüşvet verebilirsin.", GameErrorKind.Validation);
        }

        if (inmate.SeasonId != SeasonId || !inmate.IsAlive)
        {
            return new GameError("bust.inmate_not_found", "Bu mahkûm bulunamadı.", GameErrorKind.NotFound);
        }

        if (CheckCanAct(now) is { } blocked)
        {
            return blocked;
        }

        if (BustReadyAt is { } ready && ready > now)
        {
            return new GameError("bust.cooldown", "Bir sonraki kurtarma denemesi için beklemen gerekiyor.", GameErrorKind.Conflict, ready);
        }

        if (!inmate.IsJailed(now))
        {
            return new GameError("bust.not_jailed", "Bu kişi artık hapiste değil.", GameErrorKind.Conflict);
        }

        var skillBonus = Math.Min(Busts / 100, rules.BustSkillCapPercent);
        var chance = Math.Clamp(rules.BustBaseChancePercent + skillBonus, 5, 90);
        var roll = random.NextPercentRoll();
        BustReadyAt = now + rules.BustCooldown;

        if (roll < chance)
        {
            inmate.JailedUntil = null;
            Busts++;
            var promoted = AddRankPoints(rules.BustRankPoints);
            return new BustOutcome(inmate.Id, true, chance, roll, rules.BustRankPoints, promoted, null, BustReadyAt.Value);
        }

        JailedUntil = now + rules.BustFailJailTime;
        return new BustOutcome(inmate.Id, false, chance, roll, 0, false, JailedUntil, BustReadyAt.Value);
    }

    public Result<BribeOutcome> BribeOutOfJail(SeasonRules rules, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(rules);

        if (!IsAlive)
        {
            return DeadError();
        }

        if (!IsJailed(now))
        {
            return new GameError("bribe.not_jailed", "Hapiste değilsin.", GameErrorKind.Conflict);
        }

        var cost = BribeCost(rules);
        if (Money < cost)
        {
            return new GameError("bribe.insufficient_funds", "Rüşvet için cebindeki para yetmiyor.", GameErrorKind.Conflict);
        }

        Money -= cost;
        JailedUntil = null;
        return new BribeOutcome(cost);
    }

    private GameError? CheckCanAct(DateTimeOffset now)
    {
        if (!IsAlive)
        {
            return DeadError();
        }

        if (IsJailed(now))
        {
            return new GameError("character.jailed", "Hapisteyken bu işi yapamazsın.", GameErrorKind.Conflict, JailedUntil);
        }

        return null;
    }

    private static GameError DeadError() =>
        new("character.dead", "Bu karakter öldü. Yeni bir karakterle devam edebilirsin.", GameErrorKind.Conflict);

    /// <returns>Rütbe atlandıysa true.</returns>
    private bool AddRankPoints(int points)
    {
        if (points <= 0 || RankIndex == Ranks.Highest && RankPoints >= Ranks.PointsPerRank)
        {
            return false;
        }

        RankPoints += points;
        if (RankPoints < Ranks.PointsPerRank)
        {
            return false;
        }

        if (RankIndex == Ranks.Highest)
        {
            RankPoints = Ranks.PointsPerRank;
            return false;
        }

        RankIndex++;
        RankPoints -= Ranks.PointsPerRank;
        return true;
    }
}
