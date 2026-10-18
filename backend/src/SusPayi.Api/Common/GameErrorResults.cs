using Microsoft.AspNetCore.Http.HttpResults;
using SusPayi.Domain.Common;

namespace SusPayi.Api.Common;

internal static class GameErrorResults
{
    /// <summary>
    /// Oyun kuralı hatasını Problem Details yanıtına çevirir. İstemci "code" alanına göre karar verir,
    /// "retryAt" varsa geri sayım gösterir.
    /// </summary>
    public static ProblemHttpResult ToProblem(this GameError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var status = error.Kind switch
        {
            GameErrorKind.Validation => StatusCodes.Status400BadRequest,
            GameErrorKind.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status409Conflict,
        };

        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };
        if (error.RetryAt is { } retryAt)
        {
            extensions["retryAt"] = retryAt;
        }

        return TypedResults.Problem(
            detail: error.Message,
            statusCode: status,
            title: error.Code,
            extensions: extensions);
    }
}
