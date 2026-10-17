using System.Text.Json;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Domain.Common;
using PlanningApp.WebApi.ErrorHandling;
using ValidationException = PlanningApp.Application.Common.Exceptions.ValidationException;

namespace PlanningApp.ApiTests.ErrorHandling;

// Exception → HTTP durum kodu eşlemesinin her satırı için bir test (bkz. controller_rules.md)
public class GlobalExceptionHandlerTests
{
    private const string SensitiveText = "SELECT * FROM users WHERE password = 'gizli-ornek'";

    // ---- Eşleme satırları ----

    [Fact]
    public async Task TryHandleAsync_WithValidationException_Returns400WithFieldErrors()
    {
        // Arrange
        var exception = new ValidationException(
        [
            new ValidationFailure("Email", "E-posta boş olamaz."),
            new ValidationFailure("Email", "Geçerli bir e-posta adresi giriniz."),
            new ValidationFailure("Password", "Parola boş olamaz.")
        ]);

        // Act
        var result = await HandleAsync(exception);

        // Assert
        result.Context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.Context.Response.ContentType.Should().StartWith("application/problem+json");
        var errors = result.Body.GetProperty("errors");
        errors.GetProperty("Email").EnumerateArray().Select(item => item.GetString()).Should().HaveCount(2);
        errors.GetProperty("Password").EnumerateArray().Should().ContainSingle();
    }

    [Fact]
    public async Task TryHandleAsync_WithNotFoundException_Returns404()
    {
        // Act
        var result = await HandleAsync(new NotFoundException("PlanningTask", Guid.NewGuid()));

        // Assert
        result.Context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        result.Context.Response.ContentType.Should().StartWith("application/problem+json");
    }

    [Fact]
    public async Task TryHandleAsync_WithAuthenticationFailedException_Returns401WithBearerChallenge()
    {
        // Act
        var result = await HandleAsync(
            new AuthenticationFailedException(AuthenticationFailedException.InvalidCredentialsMessage));

        // Assert
        result.Context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.Context.Response.Headers.WWWAuthenticate.ToString().Should().Be("Bearer");
        result.Context.Response.ContentType.Should().StartWith("application/problem+json");
        result.Body.GetProperty("detail").GetString().Should().Be(AuthenticationFailedException.InvalidCredentialsMessage);
    }

    [Fact]
    public async Task TryHandleAsync_WithDomainException_Returns422()
    {
        // Act
        var result = await HandleAsync(new DomainException("Bu iş kuralı ihlal edildi."));

        // Assert
        result.Context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        result.Context.Response.ContentType.Should().StartWith("application/problem+json");
        result.Body.GetProperty("detail").GetString().Should().Be("Bu iş kuralı ihlal edildi.");
    }

    [Fact]
    public async Task TryHandleAsync_WithDbUpdateConcurrencyException_Returns503WithRetryAfter()
    {
        // Act
        var result = await HandleAsync(new DbUpdateConcurrencyException(SensitiveText));

        // Assert
        result.Context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        result.Context.Response.Headers.RetryAfter.ToString().Should().Be("1");
        result.Context.Response.ContentType.Should().StartWith("application/problem+json");
        result.RawBody.Should().NotContain(SensitiveText);
    }

    [Fact]
    public async Task TryHandleAsync_WithBadHttpRequestException_ReturnsItsStatusCode()
    {
        // Act
        var result = await HandleAsync(new BadHttpRequestException("ham ayrıntı", StatusCodes.Status400BadRequest));

        // Assert
        result.Context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.RawBody.Should().NotContain("ham ayrıntı");
    }

    [Fact]
    public async Task TryHandleAsync_WithUnexpectedException_Returns500WithoutLeakingMessage()
    {
        // Act
        var result = await HandleAsync(new InvalidOperationException(SensitiveText));

        // Assert
        result.Context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Context.Response.ContentType.Should().StartWith("application/problem+json");
        result.Body.GetProperty("detail").GetString().Should().Be(GlobalExceptionHandler.UnexpectedErrorDetail);
        result.RawBody.Should().NotContain(SensitiveText);
        result.RawBody.Should().NotContain("InvalidOperationException");
        result.RawBody.Should().NotContain("StackTrace", because: "stack trace yanıta yazılmaz");
    }

    // ---- Loglama ----

    [Fact]
    public async Task TryHandleAsync_WithUnexpectedException_LogsAtErrorLevel()
    {
        // Act
        var result = await HandleAsync(new InvalidOperationException(SensitiveText));

        // Assert
        result.Logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task TryHandleAsync_WithHandledException_DoesNotLogAtErrorLevel()
    {
        // Act
        var result = await HandleAsync(new NotFoundException("PlanningTask", Guid.NewGuid()));

        // Assert
        result.Logger.Entries.Should().NotContain(entry => entry.Level == LogLevel.Error);
    }

    // ---- Test Helpers ----

    private sealed record HandleResult(DefaultHttpContext Context, JsonElement Body, string RawBody, CapturingLogger Logger);

    private static async Task<HandleResult> HandleAsync(Exception exception)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        await using var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };
        context.Response.Body = new MemoryStream();

        var logger = new CapturingLogger();
        var handler = new GlobalExceptionHandler(
            provider.GetRequiredService<IProblemDetailsService>(), logger);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);
        handled.Should().BeTrue();

        context.Response.Body.Position = 0;
        var raw = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var body = string.IsNullOrEmpty(raw) ? default : JsonDocument.Parse(raw).RootElement.Clone();

        return new HandleResult(context, body, raw, logger);
    }

    private sealed class CapturingLogger : ILogger<GlobalExceptionHandler>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
