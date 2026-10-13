using FluentAssertions;
using PlanningApp.Application.Features.Tasks.Commands.RenameTask;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.RenameTask;

public class RenameTaskCommandValidatorTests
{
    // ---- Geçerli komut ----

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // ---- TaskId ----

    [Fact]
    public void Validate_WithEmptyTaskId_IsInvalid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { TaskId = Guid.Empty };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(RenameTaskCommand.TaskId));
    }

    // ---- Title ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullTitle_IsInvalid(string? invalidTitle)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { Title = invalidTitle! };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().OnlyContain(error => error.PropertyName == nameof(RenameTaskCommand.Title));
    }

    [Fact]
    public void Validate_WithTitleOfMaxLength_IsValid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { Title = new string('a', 200) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithTitleLongerThan200Characters_IsInvalid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { Title = new string('a', 201) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(RenameTaskCommand.Title));
    }

    // ---- Test Helpers ----

    private static RenameTaskCommandValidator CreateValidator()
        => new();

    private static RenameTaskCommand CreateSampleCommand()
        => new(Guid.NewGuid(), "Örnek görev");
}
