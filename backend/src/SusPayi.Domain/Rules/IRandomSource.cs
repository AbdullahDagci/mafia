namespace SusPayi.Domain.Rules;

/// <summary>
/// Tüm zar atışları bu arayüzden geçer. Üretimde kriptografik üreteç kullanılır,
/// testlerde sonucu sabitlenmiş bir sahte kaynak verilir.
/// </summary>
public interface IRandomSource
{
    /// <summary>0 ile 99 arasında (ikisi dahil) bir sayı döner.</summary>
    int NextPercentRoll();

    /// <summary><paramref name="minInclusive"/> ile <paramref name="maxExclusive"/> arasında bir sayı döner.</summary>
    long NextInt64(long minInclusive, long maxExclusive);
}
