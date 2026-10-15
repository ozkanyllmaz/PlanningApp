using FluentAssertions;
using PlanningApp.Application.Features.Auth.Commands.RefreshAccessToken;
using Xunit;

namespace PlanningApp.Test.Features.Auth.Commands.RefreshAccessToken;

public class RefreshAccessTokenCommandValidatorTests
{
    // ---- RefreshToken ----

    [Fact]
    public void Validate_WithValidToken_IsValid()
    {
        // Arrange
        var validator = new RefreshAccessTokenCommandValidator();
        var command = new RefreshAccessTokenCommand("plain-refresh-token");

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
        var validator = new RefreshAccessTokenCommandValidator();
        var command = new RefreshAccessTokenCommand(refreshToken);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(RefreshAccessTokenCommand.RefreshToken));
    }
}
