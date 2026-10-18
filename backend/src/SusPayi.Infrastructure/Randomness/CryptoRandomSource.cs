using System.Security.Cryptography;
using SusPayi.Domain.Rules;

namespace SusPayi.Infrastructure.Randomness;

/// <summary>
/// Zar atışları tahmin edilemez olmalı; aksi halde istemci tarafında sonuç önceden hesaplanabilir.
/// </summary>
public sealed class CryptoRandomSource : IRandomSource
{
    public int NextPercentRoll() => RandomNumberGenerator.GetInt32(0, 100);

    public long NextInt64(long minInclusive, long maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);

        var range = (ulong)(maxExclusive - minInclusive);
        if (range <= int.MaxValue)
        {
            return minInclusive + RandomNumberGenerator.GetInt32(0, (int)range);
        }

        // Büyük aralıklar için modulo yanlılığı olmadan reddetme örneklemesi.
        var limit = ulong.MaxValue - (ulong.MaxValue % range);
        Span<byte> buffer = stackalloc byte[8];
        ulong value;
        do
        {
            RandomNumberGenerator.Fill(buffer);
            value = BitConverter.ToUInt64(buffer);
        }
        while (value >= limit);

        return minInclusive + (long)(value % range);
    }
}
