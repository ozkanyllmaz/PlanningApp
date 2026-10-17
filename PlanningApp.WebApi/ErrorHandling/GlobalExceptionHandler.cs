using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Domain.Common;

namespace PlanningApp.WebApi.ErrorHandling;

// Tüm exception'ların TEK yakalama noktası: katman exception'ları uygun HTTP durum koduna çevrilir.
// Yanıtlar application/problem+json (RFC 9457) olarak yazılır.
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public const string UnexpectedErrorDetail = "İsteğiniz işlenirken beklenmeyen bir hata oluştu.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = CreateProblem(exception);

        if (exception is AuthenticationFailedException)
            httpContext.Response.Headers.WWWAuthenticate = "Bearer";

        if (exception is DbUpdateConcurrencyException)
            httpContext.Response.Headers.RetryAfter = "1";

        // Beklenmeyen hatalar Error düzeyinde loglanır; istek/yanıt gövdesi LOGLANMAZ (parola ve token içerir)
        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "İstek işlenirken beklenmeyen bir hata oluştu.");

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });

        return true;
    }

    private static ProblemDetails CreateProblem(Exception exception)
        => exception switch
        {
            ValidationException validation => new ValidationProblemDetails(
                validation.Errors.ToDictionary(error => error.Key, error => error.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bir veya daha fazla doğrulama hatası oluştu."
            },
            NotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Kayıt bulunamadı.",
                Detail = "İstenen kayıt bulunamadı."
            },
            AuthenticationFailedException authentication => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Kimlik doğrulama başarısız.",
                Detail = authentication.Message
            },
            DomainException domain => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "İş kuralı ihlali.",
                Detail = domain.Message
            },
            DbUpdateConcurrencyException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Hizmet geçici olarak kullanılamıyor.",
                Detail = "Eşzamanlı bir değişiklik algılandı; lütfen isteği tekrar deneyin."
            },
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Geçersiz istek.",
                Detail = "İstek okunamadı."
            },
            // Exception mesajı, stack trace, SQL ve kullanıcı verisi yanıta ASLA yazılmaz
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Beklenmeyen bir hata oluştu.",
                Detail = UnexpectedErrorDetail
            }
        };
}
