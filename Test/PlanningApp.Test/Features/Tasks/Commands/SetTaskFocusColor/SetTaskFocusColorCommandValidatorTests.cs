using FluentAssertions;
using PlanningApp.Application.Features.Tasks.Commands.SetTaskFocusColor;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.SetTaskFocusColor;

public class SetTaskFocusColorCommandValidatorTests
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
            .Which.PropertyName.Should().Be(nameof(SetTaskFocusColorCommand.TaskId));
    }

    // ---- FocusColor ----

    [Theory]
    [InlineData(null)] // renk kaldırma isteği
    [InlineData(FocusColor.Red)]
    [InlineData(FocusColor.Purple)]
    public void Validate_WithNullOrDefinedColor_IsValid(FocusColor? color)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { FocusColor = color };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(99)]
    public void Validate_WithUndefinedColor_IsInvalid(int invalidColor)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { FocusColor = (FocusColor)invalidColor };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(SetTaskFocusColorCommand.FocusColor));
    }

    // ---- Test Helpers ----

    private static SetTaskFocusColorCommandValidator CreateValidator()
        => new();

    private static SetTaskFocusColorCommand CreateSampleCommand()
        => new(Guid.NewGuid(), FocusColor.Red);
}
