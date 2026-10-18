namespace SusPayi.Domain.Accounts;

/// <summary>
/// Kalıcı oyuncu hesabı. Sezonlar arasında sıfırlanmaz; karakterler sezona bağlıdır.
/// </summary>
public sealed class Account
{
    public const int EmailMaxLength = 254;
    public const int DisplayNameMinLength = 3;
    public const int DisplayNameMaxLength = 20;

    private Account()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Küçük harfe çevrilmiş e-posta; benzersizlik bu alan üzerinden sağlanır.</summary>
    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static Account Create(string email, string displayName, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new Account
        {
            Id = Guid.CreateVersion7(now),
            Email = NormalizeEmail(email),
            DisplayName = displayName.Trim(),
            CreatedAt = now,
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }
}
