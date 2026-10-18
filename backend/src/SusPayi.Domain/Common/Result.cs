namespace SusPayi.Domain.Common;

/// <summary>
/// Oyun kuralı ihlali. Beklenen bir durumdur (bekleme süresi dolmadı, hapiste vb.),
/// bu yüzden exception yerine sonuç olarak döner.
/// </summary>
/// <param name="Code">İstemcinin karar verebilmesi için sabit kod, örn. "crime.cooldown".</param>
/// <param name="Message">Oyuncuya gösterilebilecek Türkçe açıklama.</param>
/// <param name="Kind">API katmanının HTTP durumuna çevirmesi için kategori.</param>
/// <param name="RetryAt">Bekleme gerektiren hatalarda işlemin yeniden denenebileceği an.</param>
public sealed record GameError(string Code, string Message, GameErrorKind Kind, DateTimeOffset? RetryAt = null);

public enum GameErrorKind
{
    /// <summary>Girdi geçersiz (400).</summary>
    Validation,

    /// <summary>Kaynak bulunamadı (404).</summary>
    NotFound,

    /// <summary>Oyun durumu bu işleme izin vermiyor (409).</summary>
    Conflict,
}

public readonly struct Result<T>
{
    private readonly T? _value;

    private Result(T value)
    {
        _value = value;
        Error = null;
    }

    private Result(GameError error)
    {
        _value = default;
        Error = error;
    }

    public GameError? Error { get; }

    public bool IsSuccess => Error is null;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Başarısız sonucun değeri okunamaz: {Error!.Code}");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(GameError error) => new(error);
}
