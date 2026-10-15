using FluentAssertions;
using PlanningApp.Application.Features.Auth.Commands.Login;
using Xunit;

namespace PlanningApp.Test.Features.Auth.Commands.Login;

public class LoginCommandValidatorTests
{
    private const string EmailSuffix = "@example.com";

    // ---- Geçerli komut ----

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = CreateSampleCommand();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // ---- Email ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_IsInvalid(string email)
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = CreateSampleCommand() with { Email = email };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public void Validate_WithEmailLongerThan256Characters_IsInvalid()
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var email = new string('a', 257 - EmailSuffix.Length) + EmailSuffix;
        var command = CreateSampleCommand() with { Email = email };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(LoginCommand.Email));
    }

    // ---- Password ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPassword_IsInvalid(string password)
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = CreateSampleCommand() with { Password = password };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(LoginCommand.Password));
    }

    [Fact]
    public void Validate_WithPasswordLongerThan128Characters_IsInvalid()
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = CreateSampleCommand() with { Password = new string('p', 129) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(LoginCommand.Password));
    }

    // ---- Test Helpers ----

    private static LoginCommand CreateSampleCommand()
        => new("user@example.com", "ornek-parola-123");
}
