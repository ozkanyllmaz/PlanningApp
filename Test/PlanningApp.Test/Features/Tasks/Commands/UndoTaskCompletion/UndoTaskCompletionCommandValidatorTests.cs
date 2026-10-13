using FluentAssertions;
using PlanningApp.Application.Features.Tasks.Commands.UndoTaskCompletion;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.UndoTaskCompletion;

public class UndoTaskCompletionCommandValidatorTests
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
            .Which.PropertyName.Should().Be(nameof(UndoTaskCompletionCommand.TaskId));
    }

    // ---- Test Helpers ----

    private static UndoTaskCompletionCommandValidator CreateValidator()
        => new();

    private static UndoTaskCompletionCommand CreateSampleCommand()
        => new(Guid.NewGuid());
}
