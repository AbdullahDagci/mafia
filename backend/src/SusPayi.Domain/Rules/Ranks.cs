namespace SusPayi.Domain.Rules;

/// <summary>
/// 16 basamaklı rütbe merdiveni. Barafranca'daki merdivenle aynı uzunlukta,
/// adlar Türkiye temasına göre özgün.
/// </summary>
public static class Ranks
{
    /// <summary>Bir rütbeyi tamamlamak için gereken puan (10.000 = %100).</summary>
    public const int PointsPerRank = 10_000;

    private static readonly string[] Names =
    [
        "Çaylak",
        "Kurye",
        "Çırak",
        "Dükkân Hırsızı",
        "Yankesici",
        "Hırsız",
        "Tayfa",
        "Külhanbeyi",
        "Fedai",
        "Dolandırıcı",
        "Tetikçi",
        "Semt Ağası",
        "Ağa",
        "Kabadayı",
        "Reis",
        "Baba",
    ];

    public static int Count => Names.Length;

    public static int Highest => Names.Length - 1;

    public static string NameOf(int rankIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rankIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rankIndex, Highest);
        return Names[rankIndex];
    }
}
