using System.Security.Claims;
using System.Threading.RateLimiting;

namespace SusPayi.Api.Common;

/// <summary>
/// Botlara ve kaba kuvvet denemelerine karşı ilk savunma hattı. Oyun içi bekleme süreleri
/// asıl sınırdır; bunlar yalnızca istek seli için üst sınırdır.
/// </summary>
internal static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string GameAction = "game-action";

    public static IServiceCollection AddGameRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Kimlik uçları: IP başına dakikada 10 istek.
            options.AddPolicy(Auth, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

            // Oyun eylemleri: hesap başına dakikada 60 istek.
            options.AddPolicy(GameAction, context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });

        return services;
    }
}
