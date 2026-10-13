using FluentAssertions;
using PlanningApp.Application.Features.Tasks.Commands.AssignTaskToDate;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Commands.AssignTaskToDate;

public class AssignTaskToDateCommandValidatorTests
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
            .Which.PropertyName.Should().Be(nameof(AssignTaskToDateCommand.TaskId));
    }

    // ---- Date ----

    [Fact]
    public void Validate_WithDefaultDate_IsInvalid()
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { Date = default };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(AssignTaskToDateCommand.Date));
    }

    // ---- SortOrder ----

    [Theory]
    [InlineData(0, true)]
    [InlineData(1000.5, true)]
    [InlineData(double.NaN, false)]
    [InlineData(double.PositiveInfinity, false)]
    [InlineData(double.NegativeInfinity, false)]
    public void Validate_WithSortOrder_ReturnsExpectedValidity(double sortOrder, bool expectedValid)
    {
        // Arrange
        var validator = CreateValidator();
        var command = CreateSampleCommand() with { SortOrder = sortOrder };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Errors.Should().ContainSingle()
                .Which.Should().Match<FluentValidation.Results.ValidationFailure>(error =>
                    error.PropertyName == nameof(AssignTaskToDateCommand.SortOrder) &&
                    error.ErrorMessage == "Sıralama değeri geçerli bir sayı olmalıdır.");
        }
    }

    // ---- Test Helpers ----

    private static AssignTaskToDateCommandValidator CreateValidator()
        => new();

    private static AssignTaskToDateCommand CreateSampleCommand()
        => new(Guid.NewGuid(), new DateOnly(2026, 1, 9), 1000d);
}
