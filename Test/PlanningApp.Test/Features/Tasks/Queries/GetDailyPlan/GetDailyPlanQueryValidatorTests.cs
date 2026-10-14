using FluentAssertions;
using PlanningApp.Application.Features.Tasks.Queries.GetDailyPlan;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Queries.GetDailyPlan;

public class GetDailyPlanQueryValidatorTests
{
    // ---- Date ----

    [Fact]
    public void Validate_WithValidDate_IsValid()
    {
        // Arrange
        var validator = new GetDailyPlanQueryValidator();
        var query = new GetDailyPlanQuery(new DateOnly(2026, 1, 7));

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithDefaultDate_IsInvalid()
    {
        // Arrange
        var validator = new GetDailyPlanQueryValidator();
        var query = new GetDailyPlanQuery(default);

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(GetDailyPlanQuery.Date));
    }
}
