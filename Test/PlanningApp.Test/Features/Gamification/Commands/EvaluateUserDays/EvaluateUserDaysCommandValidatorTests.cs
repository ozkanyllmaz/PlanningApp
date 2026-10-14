using FluentAssertions;
using PlanningApp.Application.Features.Gamification.Commands.EvaluateUserDays;
using Xunit;

namespace PlanningApp.Test.Features.Gamification.Commands.EvaluateUserDays;

public class EvaluateUserDaysCommandValidatorTests
{
    // ---- UserId ----

    [Fact]
    public void Validate_WithValidUserId_IsValid()
    {
        // Arrange
        var validator = new EvaluateUserDaysCommandValidator();
        var command = new EvaluateUserDaysCommand(Guid.NewGuid());

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyUserId_IsInvalid()
    {
        // Arrange
        var validator = new EvaluateUserDaysCommandValidator();
        var command = new EvaluateUserDaysCommand(Guid.Empty);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(EvaluateUserDaysCommand.UserId));
    }
}
