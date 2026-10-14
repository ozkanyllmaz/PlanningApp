using FluentAssertions;
using Moq;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Features.Tasks.Queries.GetDailyPlan;
using PlanningApp.Domain.Gamification;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Queries.GetDailyPlan;

public class GetDailyPlanQueryHandlerTests
{
    private static readonly Guid UserId = new("11111111-1111-1111-1111-111111111111");
    private static readonly DateOnly Day = new(2026, 1, 7);
    private static readonly DateTimeOffset BaseCreatedAt = new(2026, 1, 5, 9, 0, 0, TimeSpan.FromHours(3));
    private static readonly DateTimeOffset CompletedAt = new(2026, 1, 7, 10, 0, 0, TimeSpan.FromHours(3));

    // ---- Sıralama ----

    [Fact]
    public async Task Handle_WithUnsortedTasks_ReturnsTasksOrderedBySortOrder()
    {
        // Arrange
        var first = CreateTask(sortOrder: 1000d);
        var second = CreateTask(sortOrder: 2000d);
        var third = CreateTask(sortOrder: 3000d);
        var fixture = CreateFixture(tasks: [third, first, second]);

        // Act
        var result = await fixture.Handler.Handle(new GetDailyPlanQuery(Day), CancellationToken.None);

        // Assert
        result.Tasks.Select(task => task.Id).Should().Equal(first.Id, second.Id, third.Id);
    }

    [Fact]
    public async Task Handle_WithEqualSortOrder_OrdersByCreatedAt()
    {
        // Arrange
        var older = CreateTask(sortOrder: 1000d, createdAt: BaseCreatedAt);
        var newer = CreateTask(sortOrder: 1000d, createdAt: BaseCreatedAt.AddMinutes(1));
        var fixture = CreateFixture(tasks: [newer, older]);

        // Act
        var result = await fixture.Handler.Handle(new GetDailyPlanQuery(Day), CancellationToken.None);

        // Assert
        result.Tasks.Select(task => task.Id).Should().Equal(older.Id, newer.Id);
    }

    [Fact]
    public async Task Handle_WithEqualSortOrderAndCreatedAt_OrdersById()
    {
        // Arrange
        var smallerId = CreateTask(id: new Guid("00000000-0000-0000-0000-000000000001"), sortOrder: 1000d);
        var largerId = CreateTask(id: new Guid("00000000-0000-0000-0000-000000000002"), sortOrder: 1000d);
        var fixture = CreateFixture(tasks: [largerId, smallerId]);

        // Act
        var result = await fixture.Handler.Handle(new GetDailyPlanQuery(Day), CancellationToken.None);

        // Assert
        result.Tasks.Select(task => task.Id).Should().Equal(smallerId.Id, largerId.Id);
    }

    [Fact]
    public async Task Handle_WithCompletedTask_KeepsItsPositionInList()
    {
        // Arrange: tamamlanan görev SortOrder'ı en küçük olan; alta düşmemeli
        var completed = CreateTask(sortOrder: 1000d, completed: true);
        var pending = CreateTask(sortOrder: 2000d);
        var fixture = CreateFixture(tasks: [pending, completed]);

        // Act
        var result = await fixture.Handler.Handle(new GetDailyPlanQuery(Day), CancellationToken.None);

        // Assert
        result.Tasks.Select(task => task.Id).Should().Equal(completed.Id, pending.Id);
        result.Tasks[0].IsCompleted.Should().BeTrue();
        result.Tasks[0].SortOrder.Should().Be(1000d);
    }

    // ---- Boş gün / yüzde ----

    [Fact]
    public async Task Handle_WithNoTasks_ReturnsEmptyListAndNullPercentage()
    {
        // Arrange
        var fixture = CreateFixture(tasks: []);

        // Act
        var result = await fixture.Handler.Handle(new GetDailyPlanQuery(Day), CancellationToken.None);

        // Assert
        result.Date.Should().Be(Day);
        result.Tasks.Should().BeEmpty();
        result.WeightedCompletionPercentage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithMixedTasks_CalculatesWeightedPercentage()
    {
        // Arrange: Critical(3) tamam, Normal(1) tamamlanmamış → 75
        var fixture = CreateFixture(tasks:
        [
            CreateTask(sortOrder: 1000d, priority: TaskPriority.Critical, completed: true),
            CreateTask(sortOrder: 2000d, priority: TaskPriority.Normal)
        ]);

        // Act
        var result = await fixture.Handler.Handle(new GetDailyPlanQuery(Day), CancellationToken.None);

        // Assert
        result.WeightedCompletionPercentage.Should().Be(75m);
    }

    // ---- Kullanıcı / tarih ----

    [Fact]
    public async Task Handle_Always_QueriesRepositoryWithCurrentUserIdAndRequestedDate()
    {
        // Arrange
        var fixture = CreateFixture(tasks: []);

        // Act
        await fixture.Handler.Handle(new GetDailyPlanQuery(Day), CancellationToken.None);

        // Assert
        fixture.TaskRepository.Verify(
            repository => repository.GetByDateAsync(UserId, Day, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.StreakRepository.Verify(
            repository => repository.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithTask_MapsTaskFieldsToDto()
    {
        // Arrange
        var task = CreateTask(sortOrder: 1500d, priority: TaskPriority.Important);
        task.SetFocusColor(FocusColor.Blue);
        task.SetReminder(new TimeOnly(9, 30));
        var fixture = CreateFixture(tasks: [task]);

        // Act
        var result = await fixture.Handler.Handle(new GetDailyPlanQuery(Day), CancellationToken.None);

        // Assert
        var dto = result.Tasks.Should().ContainSingle().Subject;
        dto.Id.Should().Be(task.Id);
        dto.Title.Should().Be(task.Title);
        dto.Priority.Should().Be(TaskPriority.Important);
        dto.FocusColor.Should().Be(FocusColor.Blue);
        dto.SortOrder.Should().Be(1500d);
        dto.IsCompleted.Should().BeFalse();
        dto.AssignedDate.Should().Be(Day);
        dto.ReminderTime.Should().Be(new TimeOnly(9, 30));
        dto.CreatedAt.Should().Be(task.CreatedAt);
        dto.CompletedAt.Should().BeNull();
    }

    // ---- IsLocked ----

    [Fact]
    public async Task Handle_WithNoStreakRecord_ReturnsIsLockedFalse()
    {
        // Arrange
        var fixture = CreateFixture(tasks: [], streak: null);

        // Act
        var result = await fixture.Handler.Handle(new GetDailyPlanQuery(Day), CancellationToken.None);

        // Assert
        result.IsLocked.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)] // değerlendirme tam o gün yapılmış
    [InlineData(1)] // sonraki bir gün değerlendirilmiş, o gün de kilitli
    public async Task Handle_WhenDayIsEvaluated_ReturnsIsLockedTrue(int evaluatedDaysAfterRequestedDay)
    {
        // Arrange
        var streak = Streak.Create(Guid.NewGuid(), UserId);
        streak.EvaluateDay(80m, Day.AddDays(evaluatedDaysAfterRequestedDay));
        var fixture = CreateFixture(tasks: [], streak: streak);

        // Act
        var result = await fixture.Handler.Handle(new GetDailyPlanQuery(Day), CancellationToken.None);

        // Assert
        result.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenDayIsNotEvaluated_ReturnsIsLockedFalse()
    {
        // Arrange: yalnızca bir önceki gün değerlendirilmiş
        var streak = Streak.Create(Guid.NewGuid(), UserId);
        streak.EvaluateDay(80m, Day.AddDays(-1));
        var fixture = CreateFixture(tasks: [], streak: streak);

        // Act
        var result = await fixture.Handler.Handle(new GetDailyPlanQuery(Day), CancellationToken.None);

        // Assert
        result.IsLocked.Should().BeFalse();
    }

    // ---- Test Helpers ----

    private sealed record Fixture(
        GetDailyPlanQueryHandler Handler,
        Mock<IPlanningTaskRepository> TaskRepository,
        Mock<IStreakRepository> StreakRepository);

    private static Fixture CreateFixture(IReadOnlyList<PlanningTask> tasks, Streak? streak = null)
    {
        var taskRepository = new Mock<IPlanningTaskRepository>();
        taskRepository
            .Setup(repository => repository.GetByDateAsync(UserId, Day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var streakRepository = new Mock<IStreakRepository>();
        streakRepository
            .Setup(repository => repository.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streak);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.UserId).Returns(UserId);

        var handler = new GetDailyPlanQueryHandler(taskRepository.Object, streakRepository.Object, currentUser.Object);
        return new Fixture(handler, taskRepository, streakRepository);
    }

    private static PlanningTask CreateTask(
        double sortOrder,
        Guid? id = null,
        DateTimeOffset? createdAt = null,
        TaskPriority priority = TaskPriority.Normal,
        bool completed = false)
    {
        var task = PlanningTask.CreateInPool(
            id ?? Guid.NewGuid(), UserId, "Örnek görev", priority, sortOrder, createdAt ?? BaseCreatedAt);
        task.AssignToDate(Day, sortOrder);
        if (completed)
            task.Complete(CompletedAt);

        return task;
    }
}
