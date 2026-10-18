using System.Security.Claims;

namespace SusPayi.Api.Common;

internal static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Kimliği doğrulanmış kullanıcının hesap kimliği. Yetkilendirme varsayılan olarak
    /// her uçta zorunlu olduğundan, kimliksiz istek buraya ulaşamaz.
    /// </summary>
    public static Guid GetAccountId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("Kimlik belirtecinde hesap kimliği yok.");
    }
}
