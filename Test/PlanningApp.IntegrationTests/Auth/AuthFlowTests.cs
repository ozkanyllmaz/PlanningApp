using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Features.Auth;
using PlanningApp.Application.Features.Auth.Commands.Login;
using PlanningApp.Application.Features.Auth.Commands.Logout;
using PlanningApp.Application.Features.Auth.Commands.RefreshAccessToken;
using PlanningApp.Application.Features.Auth.Commands.Register;
using PlanningApp.IntegrationTests.Support;

namespace PlanningApp.IntegrationTests.Auth;

// Gerçek Postgres + Application + Persistence + Infrastructure ile uçtan uca kimlik akışı
[Collection(PostgresCollection.Name)]
public class AuthFlowTests : IDisposable
{
    private const string Password = "ornek-parola-123";
    private static readonly DateTimeOffset Now = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _provider;

    public AuthFlowTests(PostgresFixture fixture)
    {
        _provider = TestServices.BuildApplicationProvider(
            TestServices.GetConnectionString(fixture), new FakeTimeProvider(Now));
    }

    public void Dispose() => _provider.Dispose();

    // ---- Register ----

    [Fact]
    public async Task Register_WithValidCommand_ReturnsTokens()
    {
        // Arrange
        var email = NewEmail();

        // Act
        var result = await SendAsync(new RegisterCommand(email, Password, "Europe/Istanbul"));

        // Assert
        result.UserId.Should().NotBeEmpty();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.AccessTokenExpiresAt.Should().Be(Now.AddMinutes(15));
        result.RefreshTokenExpiresAt.Should().Be(Now.AddDays(60));
    }

    [Fact]
    public async Task Register_WithSameEmailTwice_ThrowsValidationExceptionWithEmailError()
    {
        // Arrange
        var email = NewEmail();
        await SendAsync(new RegisterCommand(email, Password, "Europe/Istanbul"));

        // Act
        var act = async () => await SendAsync(new RegisterCommand(email, Password, "Europe/Istanbul"));

        // Assert
        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Should().ContainKey("Email");
    }

    // ---- Login ----

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokensForSameUser()
    {
        // Arrange
        var email = NewEmail();
        var registered = await SendAsync(new RegisterCommand(email, Password, "Europe/Istanbul"));

        // Act
        var result = await SendAsync(new LoginCommand(email, Password));

        // Assert
        result.UserId.Should().Be(registered.UserId);
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBe(registered.RefreshToken);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsAuthenticationFailedExceptionWithGenericMessage()
    {
        // Arrange
        var email = NewEmail();
        await SendAsync(new RegisterCommand(email, Password, "Europe/Istanbul"));

        // Act
        var act = async () => await SendAsync(new LoginCommand(email, "yanlis-parola-999"));

        // Assert
        await act.Should().ThrowAsync<AuthenticationFailedException>()
            .WithMessage("E-posta veya şifre hatalı.");
    }

    // ---- RefreshAccessToken ----

    [Fact]
    public async Task RefreshAccessToken_WithValidToken_ReturnsNewTokensAndInvalidatesOldToken()
    {
        // Arrange
        var registered = await SendAsync(new RegisterCommand(NewEmail(), Password, "Europe/Istanbul"));

        // Act
        var refreshed = await SendAsync(new RefreshAccessTokenCommand(registered.RefreshToken));
        var reuseOldToken = async () => await SendAsync(new RefreshAccessTokenCommand(registered.RefreshToken));

        // Assert
        refreshed.UserId.Should().Be(registered.UserId);
        refreshed.RefreshToken.Should().NotBe(registered.RefreshToken);
        await reuseOldToken.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task RefreshAccessToken_WithNewTokenAfterRotation_Succeeds()
    {
        // Arrange
        var registered = await SendAsync(new RegisterCommand(NewEmail(), Password, "Europe/Istanbul"));
        var refreshed = await SendAsync(new RefreshAccessTokenCommand(registered.RefreshToken));

        // Act
        var secondRefresh = await SendAsync(new RefreshAccessTokenCommand(refreshed.RefreshToken));

        // Assert
        secondRefresh.UserId.Should().Be(registered.UserId);
        secondRefresh.RefreshToken.Should().NotBe(refreshed.RefreshToken);
    }

    // ---- Logout ----

    [Fact]
    public async Task Logout_ThenRefresh_ThrowsAuthenticationFailedException()
    {
        // Arrange
        var registered = await SendAsync(new RegisterCommand(NewEmail(), Password, "Europe/Istanbul"));

        // Act
        await SendAsync(new LogoutCommand(registered.RefreshToken));
        var act = async () => await SendAsync(new RefreshAccessTokenCommand(registered.RefreshToken));

        // Assert
        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Logout_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var registered = await SendAsync(new RegisterCommand(NewEmail(), Password, "Europe/Istanbul"));
        await SendAsync(new LogoutCommand(registered.RefreshToken));

        // Act
        var act = async () => await SendAsync(new LogoutCommand(registered.RefreshToken));

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ---- Test Helpers ----

    private static string NewEmail()
        => $"user-{Guid.NewGuid():N}@example.com";

    // Her Command kendi scope'unda (kendi DbContext'iyle) çalışır: istek başına scope davranışı
    private async Task<AuthResult> SendAsync(IRequest<AuthResult> command)
    {
        await using var scope = _provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command);
    }

    private async Task SendAsync(IRequest command)
    {
        await using var scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command);
    }
}
