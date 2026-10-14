using FluentAssertions;
using PlanningApp.Application.Features.Tasks.Queries.GetWeeklyPlan;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Queries.GetWeeklyPlan;

public class GetWeeklyPlanQueryValidatorTests
{
    // ---- Date ----

    [Fact]
    public void Validate_WithValidDate_IsValid()
    {
        // Arrange
        var validator = new GetWeeklyPlanQueryValidator();
        var query = new GetWeeklyPlanQuery(new DateOnly(2026, 1, 7));

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithDefaultDate_IsInvalid()
    {
        // Arrange
        var validator = new GetWeeklyPlanQueryValidator();
        var query = new GetWeeklyPlanQuery(default);

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(GetWeeklyPlanQuery.Date));
    }
}
