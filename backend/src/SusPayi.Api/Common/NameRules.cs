using System.Text.RegularExpressions;

namespace SusPayi.Api.Common;

internal static partial class NameRules
{
    /// <summary>Harf (Türkçe dahil), rakam, alt çizgi ve nokta; 3–20 karakter.</summary>
    [GeneratedRegex(@"^[\p{L}\p{N}_.]{3,20}$", RegexOptions.CultureInvariant)]
    public static partial Regex PlayerName();
}
