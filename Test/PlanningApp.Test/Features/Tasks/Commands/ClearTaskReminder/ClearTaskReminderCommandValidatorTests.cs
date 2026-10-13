using FluentAssertions;
using PlanningApp.Application.Features.Tasks.Commands.ClearTaskReminder;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.ClearTaskReminder;

public class ClearTaskReminderCommandValidatorTests
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
            .Which.PropertyName.Should().Be(nameof(ClearTaskReminderCommand.TaskId));
    }

    // ---- Test Helpers ----

    private static ClearTaskReminderCommandValidator CreateValidator()
        => new();

    private static ClearTaskReminderCommand CreateSampleCommand()
        => new(Guid.NewGuid());
}
