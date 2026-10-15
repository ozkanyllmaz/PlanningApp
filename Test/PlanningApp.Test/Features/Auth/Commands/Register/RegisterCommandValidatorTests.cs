using FluentAssertions;
using PlanningApp.Application.Features.Auth.Commands.Register;
using Xunit;

namespace PlanningApp.Test.Features.Auth.Commands.Register;

public class RegisterCommandValidatorTests
{
    private const string EmailSuffix = "@example.com";

    // ---- Geçerli komut ----

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
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
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    public void Validate_WithInvalidEmail_IsInvalid(string email)
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = CreateSampleCommand() with { Email = email };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public void Validate_WithEmailOf256Characters_IsValid()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var email = new string('a', 256 - EmailSuffix.Length) + EmailSuffix;
        var command = CreateSampleCommand() with { Email = email };

        // Act
        var result = validator.Validate(command);

        // Assert
        email.Length.Should().Be(256);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmailLongerThan256Characters_IsInvalid()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var email = new string('a', 257 - EmailSuffix.Length) + EmailSuffix;
        var command = CreateSampleCommand() with { Email = email };

        // Act
        var result = validator.Validate(command);

        // Assert
        email.Length.Should().Be(257);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(RegisterCommand.Email));
    }

    // ---- Password ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPassword_IsInvalid(string password)
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = CreateSampleCommand() with { Password = password };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_WithPasswordOf128Characters_IsValid()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = CreateSampleCommand() with { Password = new string('p', 128) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithPasswordLongerThan128Characters_IsInvalid()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = CreateSampleCommand() with { Password = new string('p', 129) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(RegisterCommand.Password));
    }

    // ---- TimeZoneId ----

    [Theory]
    [InlineData("Mars/Olympus")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidTimeZoneId_IsInvalid(string timeZoneId)
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = CreateSampleCommand() with { TimeZoneId = timeZoneId };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterCommand.TimeZoneId));
    }

    [Theory]
    [InlineData("Europe/Istanbul")]
    [InlineData("America/New_York")]
    [InlineData("UTC")]
    public void Validate_WithValidTimeZoneId_IsValid(string timeZoneId)
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = CreateSampleCommand() with { TimeZoneId = timeZoneId };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // ---- Test Helpers ----

    private static RegisterCommand CreateSampleCommand()
        => new("user@example.com", "ornek-parola-123", "Europe/Istanbul");
}
