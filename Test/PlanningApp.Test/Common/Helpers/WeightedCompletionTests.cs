using FluentAssertions;
using PlanningApp.Application.Common.Helpers;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Common.Helpers;

public class WeightedCompletionTests
{
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");
    private static readonly DateOnly Day = new(2026, 1, 7);
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CompletedAt = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);

    // ---- Calculate ----

    [Fact]
    public void Calculate_WithNoTasks_ReturnsNull()
    {
        // Arrange
        var tasks = Array.Empty<PlanningTask>();

        // Act
        var result = WeightedCompletion.Calculate(tasks);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Calculate_WithAllTasksCompleted_Returns100()
    {
        // Arrange
        var tasks = new[]
        {
            CreateTask(TaskPriority.Normal, completed: true),
            CreateTask(TaskPriority.Critical, completed: true)
        };

        // Act
        var result = WeightedCompletion.Calculate(tasks);

        // Assert
        result.Should().Be(100m);
    }

    [Fact]
    public void Calculate_WithNoTaskCompleted_ReturnsZero()
    {
        // Arrange
        var tasks = new[]
        {
            CreateTask(TaskPriority.Normal, completed: false),
            CreateTask(TaskPriority.Important, completed: false)
        };

        // Act
        var result = WeightedCompletion.Calculate(tasks);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void Calculate_WithCompletedCriticalAndIncompleteNormal_Returns75()
    {
        // Arrange
        var tasks = new[]
        {
            CreateTask(TaskPriority.Critical, completed: true),
            CreateTask(TaskPriority.Normal, completed: false)
        };

        // Act
        var result = WeightedCompletion.Calculate(tasks);

        // Assert
        result.Should().Be(75m);
    }

    [Fact]
    public void Calculate_WithOneOfThreeEqualTasksCompleted_RoundsToTwoDecimals()
    {
        // Arrange
        var tasks = new[]
        {
            CreateTask(TaskPriority.Normal, completed: true),
            CreateTask(TaskPriority.Normal, completed: false),
            CreateTask(TaskPriority.Normal, completed: false)
        };

        // Act
        var result = WeightedCompletion.Calculate(tasks);

        // Assert
        result.Should().Be(33.33m);
    }

    [Fact]
    public void Calculate_WithTwoOfThreeEqualTasksCompleted_RoundsUp()
    {
        // Arrange
        var tasks = new[]
        {
            CreateTask(TaskPriority.Normal, completed: true),
            CreateTask(TaskPriority.Normal, completed: true),
            CreateTask(TaskPriority.Normal, completed: false)
        };

        // Act
        var result = WeightedCompletion.Calculate(tasks);

        // Assert
        result.Should().Be(66.67m);
    }

    [Theory]
    [InlineData(true, false, 66.67)]   // Important tamam (2) / toplam 3
    [InlineData(false, true, 33.33)]   // Normal tamam (1) / toplam 3
    public void Calculate_WithImportantAndNormalMixed_UsesWeights(
        bool importantCompleted, bool normalCompleted, double expected)
    {
        // Arrange
        var tasks = new[]
        {
            CreateTask(TaskPriority.Important, importantCompleted),
            CreateTask(TaskPriority.Normal, normalCompleted)
        };

        // Act
        var result = WeightedCompletion.Calculate(tasks);

        // Assert
        result.Should().Be((decimal)expected);
    }

    [Fact]
    public void Calculate_WithAllThreePriorities_SumsWeightsCorrectly()
    {
        // Arrange: tamamlanan Important(2) + Normal(1) = 3, toplam 1+2+3 = 6
        var tasks = new[]
        {
            CreateTask(TaskPriority.Normal, completed: true),
            CreateTask(TaskPriority.Important, completed: true),
            CreateTask(TaskPriority.Critical, completed: false)
        };

        // Act
        var result = WeightedCompletion.Calculate(tasks);

        // Assert
        result.Should().Be(50m);
    }

    // ---- Test Helpers ----

    private static PlanningTask CreateTask(TaskPriority priority, bool completed)
    {
        var task = PlanningTask.CreateInPool(Guid.NewGuid(), SampleUserId, "Örnek görev", priority, 1000d, CreatedAt);
        task.AssignToDate(Day, 1000d);
        if (completed)
            task.Complete(CompletedAt);

        return task;
    }
}
