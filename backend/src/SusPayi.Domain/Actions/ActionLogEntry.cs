namespace SusPayi.Domain.Actions;

/// <summary>
/// Her oyun eyleminin değişmez kaydı. Denge ayarı, hile tespiti ve
/// şeref salonu istatistikleri bu kayıtlardan hesaplanır.
/// </summary>
public sealed class ActionLogEntry
{
    public const int TypeMaxLength = 32;
    public const int SubjectMaxLength = 48;
    public const int OutcomeMaxLength = 24;

    private ActionLogEntry()
    {
    }

    public long Id { get; private set; }

    public Guid SeasonId { get; private set; }

    public Guid CharacterId { get; private set; }

    /// <summary>Eylem türü, örn. "crime", "bust", "bribe".</summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>Eylemin konusu, örn. suç kimliği.</summary>
    public string? Subject { get; private set; }

    /// <summary>Hedef karakter (kurtarma, saldırı gibi eylemlerde).</summary>
    public Guid? TargetCharacterId { get; private set; }

    public string Outcome { get; private set; } = string.Empty;

    public int? ChancePercent { get; private set; }

    public int? Roll { get; private set; }

    public long MoneyDelta { get; private set; }

    public int RankPointsDelta { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static ActionLogEntry Create(
        Guid seasonId,
        Guid characterId,
        string type,
        string outcome,
        DateTimeOffset occurredAt,
        string? subject = null,
        Guid? targetCharacterId = null,
        int? chancePercent = null,
        int? roll = null,
        long moneyDelta = 0,
        int rankPointsDelta = 0) =>
        new()
        {
            SeasonId = seasonId,
            CharacterId = characterId,
            Type = type,
            Outcome = outcome,
            OccurredAt = occurredAt,
            Subject = subject,
            TargetCharacterId = targetCharacterId,
            ChancePercent = chancePercent,
            Roll = roll,
            MoneyDelta = moneyDelta,
            RankPointsDelta = rankPointsDelta,
        };
}
