using FluentAssertions;
using PlanningApp.Application.Features.Auth.Commands.Logout;
using Xunit;

namespace PlanningApp.Test.Features.Auth.Commands.Logout;

public class LogoutCommandValidatorTests
{
    // ---- RefreshToken ----

    [Fact]
    public void Validate_WithValidToken_IsValid()
    {
        // Arrange
        var validator = new LogoutCommandValidator();
        var command = new LogoutCommand("plain-refresh-token");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyToken_IsInvalid(string refreshToken)
    {
        // Arrange
        var validator = new LogoutCommandValidator();
        var command = new LogoutCommand(refreshToken);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(LogoutCommand.RefreshToken));
    }
}
