using FluentAssertions;
using Moq;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Features.Tasks.Queries.GetPool;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Features.Tasks.Queries.GetPool;

public class GetPoolQueryHandlerTests
{
    private static readonly Guid UserId = new("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset BaseCreatedAt = new(2026, 1, 5, 9, 0, 0, TimeSpan.FromHours(3));

    // ---- Sıralama ----

    [Fact]
    public async Task Handle_WithUnsortedTasks_ReturnsTasksOrderedBySortOrder()
    {
        // Arrange
        var first = CreateTask(sortOrder: 1000d);
        var second = CreateTask(sortOrder: 2000d);
        var third = CreateTask(sortOrder: 3000d);
        var fixture = CreateFixture([third, first, second]);

        // Act
        var result = await fixture.Handler.Handle(new GetPoolQuery(), CancellationToken.None);

        // Assert
        result.Select(task => task.Id).Should().Equal(first.Id, second.Id, third.Id);
    }

    [Fact]
    public async Task Handle_WithEqualSortOrder_OrdersByCreatedAt()
    {
        // Arrange
        var older = CreateTask(sortOrder: 1000d, createdAt: BaseCreatedAt);
        var newer = CreateTask(sortOrder: 1000d, createdAt: BaseCreatedAt.AddMinutes(1));
        var fixture = CreateFixture([newer, older]);

        // Act
        var result = await fixture.Handler.Handle(new GetPoolQuery(), CancellationToken.None);

        // Assert
        result.Select(task => task.Id).Should().Equal(older.Id, newer.Id);
    }

    [Fact]
    public async Task Handle_WithEqualSortOrderAndCreatedAt_OrdersById()
    {
        // Arrange
        var smallerId = CreateTask(sortOrder: 1000d, id: new Guid("00000000-0000-0000-0000-000000000001"));
        var largerId = CreateTask(sortOrder: 1000d, id: new Guid("00000000-0000-0000-0000-000000000002"));
        var fixture = CreateFixture([largerId, smallerId]);

        // Act
        var result = await fixture.Handler.Handle(new GetPoolQuery(), CancellationToken.None);

        // Assert
        result.Select(task => task.Id).Should().Equal(smallerId.Id, largerId.Id);
    }

    // ---- Boş havuz / eşleme / kullanıcı ----

    [Fact]
    public async Task Handle_WithEmptyPool_ReturnsEmptyList()
    {
        // Arrange
        var fixture = CreateFixture([]);

        // Act
        var result = await fixture.Handler.Handle(new GetPoolQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithTask_MapsTaskFieldsToDto()
    {
        // Arrange
        var task = CreateTask(sortOrder: 1500d, priority: TaskPriority.Critical);
        task.SetFocusColor(FocusColor.Green);
        var fixture = CreateFixture([task]);

        // Act
        var result = await fixture.Handler.Handle(new GetPoolQuery(), CancellationToken.None);

        // Assert
        var dto = result.Should().ContainSingle().Subject;
        dto.Id.Should().Be(task.Id);
        dto.Title.Should().Be(task.Title);
        dto.Priority.Should().Be(TaskPriority.Critical);
        dto.FocusColor.Should().Be(FocusColor.Green);
        dto.SortOrder.Should().Be(1500d);
        dto.IsCompleted.Should().BeFalse();
        dto.AssignedDate.Should().BeNull();
        dto.ReminderTime.Should().BeNull();
        dto.CreatedAt.Should().Be(task.CreatedAt);
        dto.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Always_QueriesRepositoryOnceWithCurrentUserId()
    {
        // Arrange
        var fixture = CreateFixture([]);

        // Act
        await fixture.Handler.Handle(new GetPoolQuery(), CancellationToken.None);

        // Assert
        fixture.TaskRepository.Verify(
            repository => repository.GetPoolAsync(UserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Test Helpers ----

    private sealed record Fixture(
        GetPoolQueryHandler Handler,
        Mock<IPlanningTaskRepository> TaskRepository);

    private static Fixture CreateFixture(IReadOnlyList<PlanningTask> tasks)
    {
        var taskRepository = new Mock<IPlanningTaskRepository>();
        taskRepository
            .Setup(repository => repository.GetPoolAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.UserId).Returns(UserId);

        return new Fixture(new GetPoolQueryHandler(taskRepository.Object, currentUser.Object), taskRepository);
    }

    private static PlanningTask CreateTask(
        double sortOrder,
        Guid? id = null,
        DateTimeOffset? createdAt = null,
        TaskPriority priority = TaskPriority.Normal)
        => PlanningTask.CreateInPool(
            id ?? Guid.NewGuid(), UserId, "Örnek görev", priority, sortOrder, createdAt ?? BaseCreatedAt);
}
