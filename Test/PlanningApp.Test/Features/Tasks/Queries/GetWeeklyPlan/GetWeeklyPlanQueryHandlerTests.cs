using FluentAssertions;
using Moq;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Features.Tasks.Queries.GetWeeklyPlan;
using PlanningApp.Domain.Gamification;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Queries.GetWeeklyPlan;

public class GetWeeklyPlanQueryHandlerTests
{
    private static readonly Guid UserId = new("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset BaseCreatedAt = new(2026, 1, 1, 9, 0, 0, TimeSpan.FromHours(3));

    // 2026-01-05 Pazartesi
    private static readonly DateOnly Monday = new(2026, 1, 5);

    // ---- Hafta aralığı ----

    [Theory]
    [InlineData(0)] // Pazartesi
    [InlineData(2)] // Çarşamba
    [InlineData(6)] // Pazar
    public async Task Handle_WithAnyDayOfWeek_ReturnsSevenDaysStartingFromMonday(int daysAfterMonday)
    {
        // Arrange
        var fixture = CreateFixture(Monday, tasks: []);

        // Act
        var result = await fixture.Handler.Handle(
            new GetWeeklyPlanQuery(Monday.AddDays(daysAfterMonday)), CancellationToken.None);

        // Assert
        result.Select(day => day.Date).Should().Equal(
            Enumerable.Range(0, 7).Select(offset => Monday.AddDays(offset)));
    }

    [Fact]
    public async Task Handle_WithYearEndDate_StartsWeekOnPreviousMonday()
    {
        // Arrange: 2026-12-31 Perşembe → hafta 2026-12-28'de başlar
        var weekStart = new DateOnly(2026, 12, 28);
        var fixture = CreateFixture(weekStart, tasks: []);

        // Act
        var result = await fixture.Handler.Handle(
            new GetWeeklyPlanQuery(new DateOnly(2026, 12, 31)), CancellationToken.None);

        // Assert
        result.Should().HaveCount(7);
        result[0].Date.Should().Be(new DateOnly(2026, 12, 28));
        result[6].Date.Should().Be(new DateOnly(2027, 1, 3));
    }

    // ---- Gruplama ----

    [Fact]
    public async Task Handle_WithTasksOnDifferentDays_GroupsTasksIntoCorrectDay()
    {
        // Arrange
        var mondayTask = CreateTask(Monday, sortOrder: 1000d);
        var wednesdayTask1 = CreateTask(Monday.AddDays(2), sortOrder: 1000d);
        var wednesdayTask2 = CreateTask(Monday.AddDays(2), sortOrder: 2000d);
        var sundayTask = CreateTask(Monday.AddDays(6), sortOrder: 1000d);
        var fixture = CreateFixture(Monday, tasks: [sundayTask, wednesdayTask2, mondayTask, wednesdayTask1]);

        // Act
        var result = await fixture.Handler.Handle(new GetWeeklyPlanQuery(Monday), CancellationToken.None);

        // Assert
        result[0].Tasks.Select(task => task.Id).Should().Equal(mondayTask.Id);
        result[2].Tasks.Select(task => task.Id).Should().Equal(wednesdayTask1.Id, wednesdayTask2.Id);
        result[6].Tasks.Select(task => task.Id).Should().Equal(sundayTask.Id);
        result.Sum(day => day.Tasks.Count).Should().Be(4);
    }

    [Fact]
    public async Task Handle_WithEqualSortOrder_OrdersByCreatedAtThenId()
    {
        // Arrange
        var older = CreateTask(Monday, sortOrder: 1000d, createdAt: BaseCreatedAt);
        var newerSmallerId = CreateTask(
            Monday, sortOrder: 1000d, createdAt: BaseCreatedAt.AddMinutes(1),
            id: new Guid("00000000-0000-0000-0000-000000000001"));
        var newerLargerId = CreateTask(
            Monday, sortOrder: 1000d, createdAt: BaseCreatedAt.AddMinutes(1),
            id: new Guid("00000000-0000-0000-0000-000000000002"));
        var fixture = CreateFixture(Monday, tasks: [newerLargerId, newerSmallerId, older]);

        // Act
        var result = await fixture.Handler.Handle(new GetWeeklyPlanQuery(Monday), CancellationToken.None);

        // Assert
        result[0].Tasks.Select(task => task.Id).Should().Equal(older.Id, newerSmallerId.Id, newerLargerId.Id);
    }

    [Fact]
    public async Task Handle_WithDayWithoutTasks_ReturnsEmptyListAndNullPercentage()
    {
        // Arrange: yalnızca Pazartesi'de görev var
        var fixture = CreateFixture(Monday, tasks: [CreateTask(Monday, sortOrder: 1000d)]);

        // Act
        var result = await fixture.Handler.Handle(new GetWeeklyPlanQuery(Monday), CancellationToken.None);

        // Assert
        result[1].Tasks.Should().BeEmpty();
        result[1].WeightedCompletionPercentage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithTasks_CalculatesPercentagePerDay()
    {
        // Arrange: Pazartesi 75, Salı 0, Çarşamba 100
        var fixture = CreateFixture(Monday, tasks:
        [
            CreateTask(Monday, sortOrder: 1000d, priority: TaskPriority.Critical, completed: true),
            CreateTask(Monday, sortOrder: 2000d, priority: TaskPriority.Normal),
            CreateTask(Monday.AddDays(1), sortOrder: 1000d),
            CreateTask(Monday.AddDays(2), sortOrder: 1000d, completed: true)
        ]);

        // Act
        var result = await fixture.Handler.Handle(new GetWeeklyPlanQuery(Monday), CancellationToken.None);

        // Assert
        result[0].WeightedCompletionPercentage.Should().Be(75m);
        result[1].WeightedCompletionPercentage.Should().Be(0m);
        result[2].WeightedCompletionPercentage.Should().Be(100m);
    }

    // ---- IsLocked ----

    [Fact]
    public async Task Handle_WithNoStreakRecord_ReturnsAllDaysUnlocked()
    {
        // Arrange
        var fixture = CreateFixture(Monday, tasks: [], streak: null);

        // Act
        var result = await fixture.Handler.Handle(new GetWeeklyPlanQuery(Monday), CancellationToken.None);

        // Assert
        result.Should().OnlyContain(day => !day.IsLocked);
    }

    [Fact]
    public async Task Handle_WithLastEvaluatedDateOnWednesday_LocksOnlyDaysUpToWednesday()
    {
        // Arrange
        var streak = Streak.Create(Guid.NewGuid(), UserId);
        streak.EvaluateDay(80m, Monday.AddDays(2));
        var fixture = CreateFixture(Monday, tasks: [], streak: streak);

        // Act
        var result = await fixture.Handler.Handle(new GetWeeklyPlanQuery(Monday), CancellationToken.None);

        // Assert
        result.Select(day => day.IsLocked).Should().Equal(
            true, true, true, false, false, false, false);
    }

    [Fact]
    public async Task Handle_WhenWholeWeekIsEvaluated_LocksAllDays()
    {
        // Arrange: değerlendirme haftanın sonrasındaki bir güne kadar yapılmış
        var streak = Streak.Create(Guid.NewGuid(), UserId);
        streak.EvaluateDay(80m, Monday.AddDays(10));
        var fixture = CreateFixture(Monday, tasks: [], streak: streak);

        // Act
        var result = await fixture.Handler.Handle(new GetWeeklyPlanQuery(Monday), CancellationToken.None);

        // Assert
        result.Should().OnlyContain(day => day.IsLocked);
    }

    // ---- Repository çağrıları ----

    [Fact]
    public async Task Handle_Always_QueriesTaskRepositoryOnceWithWeekRange()
    {
        // Arrange
        var fixture = CreateFixture(Monday, tasks: []);

        // Act
        await fixture.Handler.Handle(new GetWeeklyPlanQuery(Monday.AddDays(3)), CancellationToken.None);

        // Assert
        fixture.TaskRepository.Verify(
            repository => repository.GetByDateRangeAsync(
                UserId, Monday, Monday.AddDays(6), It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.TaskRepository.Verify(
            repository => repository.GetByDateAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Always_QueriesStreakRepositoryOnlyOnce()
    {
        // Arrange
        var fixture = CreateFixture(Monday, tasks: []);

        // Act
        await fixture.Handler.Handle(new GetWeeklyPlanQuery(Monday), CancellationToken.None);

        // Assert
        fixture.StreakRepository.Verify(
            repository => repository.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Test Helpers ----

    private sealed record Fixture(
        GetWeeklyPlanQueryHandler Handler,
        Mock<IPlanningTaskRepository> TaskRepository,
        Mock<IStreakRepository> StreakRepository);

    private static Fixture CreateFixture(
        DateOnly weekStart, IReadOnlyList<PlanningTask> tasks, Streak? streak = null)
    {
        var taskRepository = new Mock<IPlanningTaskRepository>();
        taskRepository
            .Setup(repository => repository.GetByDateRangeAsync(
                UserId, weekStart, weekStart.AddDays(6), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var streakRepository = new Mock<IStreakRepository>();
        streakRepository
            .Setup(repository => repository.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.UserId).Returns(UserId);

        var handler = new GetWeeklyPlanQueryHandler(taskRepository.Object, streakRepository.Object, currentUser.Object);
        return new Fixture(handler, taskRepository, streakRepository);
    }

    private static PlanningTask CreateTask(
        DateOnly date,
        double sortOrder,
        Guid? id = null,
        DateTimeOffset? createdAt = null,
        TaskPriority priority = TaskPriority.Normal,
        bool completed = false)
    {
        var task = PlanningTask.CreateInPool(
            id ?? Guid.NewGuid(), UserId, "Örnek görev", priority, sortOrder, createdAt ?? BaseCreatedAt);
        task.AssignToDate(date, sortOrder);
        if (completed)
        {
            // İstemci tamamlama zamanını kendi yerel offset'iyle gönderir
            var completedAt = new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)), TimeSpan.FromHours(3));
            task.Complete(completedAt);
        }

        return task;
    }
}
