using FluentAssertions;
using PlanningApp.Application.Features.Tasks.Commands.SetTaskReminder;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.SetTaskReminder;

public class SetTaskReminderCommandValidatorTests
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
            .Which.PropertyName.Should().Be(nameof(SetTaskReminderCommand.TaskId));
    }

    // ---- ReminderTime ----

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9, 30)]
    [InlineData(23, 59)]
    public void Validate_WithAnyTimeOfDay_IsValid(int hour, int minute)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { ReminderTime = new TimeOnly(hour, minute) };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // ---- Test Helpers ----

    private static SetTaskReminderCommandValidator CreateValidator()
        => new();

    private static SetTaskReminderCommand CreateSampleCommand()
        => new(Guid.NewGuid(), new TimeOnly(9, 0));
}
