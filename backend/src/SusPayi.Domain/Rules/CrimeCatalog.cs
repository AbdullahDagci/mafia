namespace SusPayi.Domain.Rules;

/// <param name="Id">Kalıcı kimlik; API'de ve olay kaydında kullanılır, değiştirilmez.</param>
/// <param name="Name">Oyuncuya gösterilen ad.</param>
/// <param name="BaseChancePercent">Çaylak rütbesindeki başarı şansı.</param>
/// <param name="ChancePerRankPercent">Her rütbede eklenen şans.</param>
/// <param name="MinReward">Başarıda en az kazanç.</param>
/// <param name="MaxReward">Başarıda en çok kazanç.</param>
/// <param name="RankPoints">Başarıda kazanılan rütbe puanı (10.000 = bir rütbe).</param>
/// <param name="JailTime">Yakalanınca hapis süresi.</param>
public sealed record CrimeDefinition(
    string Id,
    string Name,
    int BaseChancePercent,
    int ChancePerRankPercent,
    long MinReward,
    long MaxReward,
    int RankPoints,
    TimeSpan JailTime)
{
    public const int MinChancePercent = 1;
    public const int MaxChancePercent = 95;

    public int SuccessChancePercent(int rankIndex) =>
        Math.Clamp(BaseChancePercent + (rankIndex * ChancePerRankPercent), MinChancePercent, MaxChancePercent);
}

public static class CrimeCatalog
{
    public static IReadOnlyList<CrimeDefinition> All { get; } =
    [
        new("pickpocket", "Pazarda yankesicilik", 56, 3, 300, 900, 500, TimeSpan.FromMinutes(2)),
        new("corner-shop", "Mahalle bakkalını soy", 42, 3, 1_200, 3_400, 700, TimeSpan.FromMinutes(3)),
        new("car-park", "Otoparkta bagaj soy", 30, 3, 4_000, 9_000, 900, TimeSpan.FromMinutes(4)),
        new("jeweller", "Kuyumcu vitrinini kır", 16, 3, 15_000, 42_000, 1_300, TimeSpan.FromMinutes(6)),
        new("customs", "Gümrükçüye rüşvet", 4, 3, 60_000, 150_000, 2_000, TimeSpan.FromMinutes(10)),
    ];

    public static CrimeDefinition? Find(string id) =>
        All.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal));
}
