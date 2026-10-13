using FluentAssertions;
using PlanningApp.Application.Features.Tasks.Commands.ChangeTaskPriority;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.ChangeTaskPriority;

public class ChangeTaskPriorityCommandValidatorTests
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
            .Which.PropertyName.Should().Be(nameof(ChangeTaskPriorityCommand.TaskId));
    }

    // ---- Priority ----

    [Theory]
    [InlineData(TaskPriority.Normal)]
    [InlineData(TaskPriority.Important)]
    [InlineData(TaskPriority.Critical)]
    public void Validate_WithDefinedPriority_IsValid(TaskPriority priority)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { Priority = priority };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Validate_WithUndefinedPriority_IsInvalid(int invalidPriority)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { Priority = (TaskPriority)invalidPriority };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(ChangeTaskPriorityCommand.Priority));
    }

    // ---- Test Helpers ----

    private static ChangeTaskPriorityCommandValidator CreateValidator()
        => new();

    private static ChangeTaskPriorityCommand CreateSampleCommand()
        => new(Guid.NewGuid(), TaskPriority.Normal);
}
