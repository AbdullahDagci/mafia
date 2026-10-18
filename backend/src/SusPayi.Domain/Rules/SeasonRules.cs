namespace SusPayi.Domain.Rules;

/// <summary>
/// Bir sezonun denge ayarları. Kodda sabit değil, sezon kaydıyla birlikte saklanır;
/// böylece yeni sezon kod değişikliği olmadan farklı değerlerle açılabilir.
/// Varsayılanlar Barafranca'daki sürelere yakın tutuldu.
/// </summary>
public sealed class SeasonRules
{
    /// <summary>İki suç arasındaki bekleme (Barafranca: ~90 sn).</summary>
    public TimeSpan CrimeCooldown { get; init; } = TimeSpan.FromSeconds(90);

    /// <summary>Başarısız suçta yakalanmadan kaçma olasılığı (yüzde).</summary>
    public int EscapeChanceOnFailurePercent { get; init; } = 60;

    /// <summary>İki kurtarma denemesi arasındaki bekleme.</summary>
    public TimeSpan BustCooldown { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Kurtarmanın temel başarı şansı (yüzde).</summary>
    public int BustBaseChancePercent { get; init; } = 55;

    /// <summary>Kurtarma deneyiminden gelebilecek en fazla ek şans (yüzde). Her 100 kurtarma +1.</summary>
    public int BustSkillCapPercent { get; init; } = 20;

    /// <summary>Başarılı kurtarmanın verdiği rütbe puanı (10.000 = bir rütbe).</summary>
    public int BustRankPoints { get; init; } = 300;

    /// <summary>Başarısız kurtarmada kurtarıcının yattığı süre.</summary>
    public TimeSpan BustFailJailTime { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>Rüşvetle hapisten çıkmanın taban ücreti.</summary>
    public long BribeBaseCost { get; init; } = 5_000;

    /// <summary>Her rütbe için rüşvete eklenen ücret.</summary>
    public long BribeCostPerRank { get; init; } = 2_500;

    /// <summary>Yeni karakterin başlangıç parası (Barafranca: 500).</summary>
    public long StartingMoney { get; init; } = 500;

    /// <summary>Yeni karakterin saldırıya kapalı olduğu süre (Barafranca: 48 saat).</summary>
    public TimeSpan NewPlayerProtection { get; init; } = TimeSpan.FromHours(48);
}
