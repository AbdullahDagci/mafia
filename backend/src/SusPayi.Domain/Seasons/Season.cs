using SusPayi.Domain.Rules;

namespace SusPayi.Domain.Seasons;

/// <summary>
/// 12 haftalık oyun dönemi. Sezonluk tüm tablolar SeasonId taşır;
/// sıfırlama veri silmek değil, yeni sezon açmaktır.
/// </summary>
public sealed class Season
{
    public const int NameMaxLength = 80;

    private Season()
    {
    }

    public Guid Id { get; private set; }

    public int Number { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Final fazının başladığı an: yeni aile kurulamaz, aile değiştirilemez.</summary>
    public DateTimeOffset FinalPhaseStartsAt { get; private set; }

    public SeasonRules Rules { get; private set; } = new();

    public static Season Create(int number, string name, DateTimeOffset startsAt, TimeSpan length, TimeSpan finalPhaseLength, SeasonRules rules)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(finalPhaseLength, length);
        ArgumentNullException.ThrowIfNull(rules);

        var endsAt = startsAt + length;
        return new Season
        {
            Id = Guid.CreateVersion7(startsAt),
            Number = number,
            Name = name.Trim(),
            StartsAt = startsAt,
            EndsAt = endsAt,
            FinalPhaseStartsAt = endsAt - finalPhaseLength,
            Rules = rules,
        };
    }

    public bool IsRunning(DateTimeOffset now) => now >= StartsAt && now < EndsAt;

    public bool IsInFinalPhase(DateTimeOffset now) => IsRunning(now) && now >= FinalPhaseStartsAt;

    public int DayNumber(DateTimeOffset now) =>
        now < StartsAt ? 0 : (int)Math.Min((now - StartsAt).TotalDays + 1, (EndsAt - StartsAt).TotalDays);
}
