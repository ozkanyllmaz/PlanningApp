using FluentAssertions;
using PlanningApp.Application.Features.Auth;
using PlanningApp.Application.Features.Auth.Commands.Login;
using PlanningApp.Application.Features.Auth.Commands.Logout;
using PlanningApp.Application.Features.Auth.Commands.RefreshAccessToken;
using PlanningApp.Application.Features.Auth.Commands.Register;
using Xunit;

namespace PlanningApp.Test.Features.Auth;

// Parola ve token log'a/exception'a düşmesin: ToString hassas alanları göstermemeli
public class SensitiveCommandToStringTests
{
    private const string SecretPassword = "cok-gizli-parola-123";
    private const string SecretToken = "cok-gizli-refresh-token-xyz";

    [Fact]
    public void ToString_OnRegisterCommand_DoesNotContainPassword()
    {
        // Arrange
        var command = new RegisterCommand("user@example.com", SecretPassword, "Europe/Istanbul");

        // Act
        var text = command.ToString();

        // Assert
        text.Should().NotContain(SecretPassword);
        text.Should().Contain("***");
        text.Should().Contain("user@example.com");
    }

    [Fact]
    public void ToString_OnLoginCommand_DoesNotContainPassword()
    {
        // Arrange
        var command = new LoginCommand("user@example.com", SecretPassword);

        // Act
        var text = command.ToString();

        // Assert
        text.Should().NotContain(SecretPassword);
        text.Should().Contain("***");
        text.Should().Contain("user@example.com");
    }

    [Fact]
    public void ToString_OnRefreshAccessTokenCommand_DoesNotContainToken()
    {
        // Arrange
        var command = new RefreshAccessTokenCommand(SecretToken);

        // Act
        var text = command.ToString();

        // Assert
        text.Should().NotContain(SecretToken);
        text.Should().Contain("***");
    }

    [Fact]
    public void ToString_OnLogoutCommand_DoesNotContainToken()
    {
        // Arrange
        var command = new LogoutCommand(SecretToken);

        // Act
        var text = command.ToString();

        // Assert
        text.Should().NotContain(SecretToken);
        text.Should().Contain("***");
    }

    [Fact]
    public void ToString_OnAuthResult_DoesNotContainTokens()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);
        var result = new AuthResult(Guid.NewGuid(), "access-secret", now, SecretToken, now.AddDays(60));

        // Act
        var text = result.ToString();

        // Assert
        text.Should().NotContain("access-secret");
        text.Should().NotContain(SecretToken);
    }
}
