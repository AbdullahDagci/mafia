using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SusPayi.Api.Common;

/// <summary>
/// Beklenmeyen hatalar tek yerde Problem Details yanıtına çevrilir.
/// 500 yanıtlarında hata mesajı veya yığın izi istemciye dönmez; yalnızca izleme kimliği döner.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int status;
        string title;
        string? code = null;

        switch (exception)
        {
            case DbUpdateConcurrencyException:
                // Aynı karakter için iki istek aynı anda geldi; biri kazandı, diğeri yeniden denemeli.
                status = StatusCodes.Status409Conflict;
                title = "Aynı anda başka bir işlem yapıldı, lütfen tekrar dene.";
                code = "concurrency.conflict";
                LogConcurrencyConflict(logger, httpContext.Request.Path);
                break;
            case BadHttpRequestException badRequest:
                status = badRequest.StatusCode;
                title = "İstek okunamadı.";
                break;
            default:
                status = StatusCodes.Status500InternalServerError;
                title = "Beklenmeyen bir hata oluştu.";
                LogUnhandled(logger, exception, httpContext.Request.Path);
                break;
        }

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = title };
        if (code is not null)
        {
            problem.Extensions["code"] = code;
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "İşlenmeyen hata: {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Eşzamanlılık çakışması: {Path}")]
    private static partial void LogConcurrencyConflict(ILogger logger, string path);
}
