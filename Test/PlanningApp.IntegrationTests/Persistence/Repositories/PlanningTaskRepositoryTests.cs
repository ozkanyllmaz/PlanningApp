using FluentAssertions;
using PlanningApp.Domain.Tasks;
using PlanningApp.Persistence.Repositories;

namespace PlanningApp.IntegrationTests.Persistence.Repositories;

[Collection(PostgresCollection.Name)]
public class PlanningTaskRepositoryTests(PostgresFixture fixture)
{
    private static readonly DateOnly Day = new(2026, 1, 7);
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

    // ---- GetByIdAsync ----

    [Fact]
    public async Task GetByIdAsync_WithExistingTask_ReturnsTask()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = CreatePoolTask(userId);
        await SaveAsync(task);

        // Act
        var result = await ReadAsync(repository => repository.GetByIdAsync(userId, task.Id, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetByIdAsync_WithOtherUsersTask_ReturnsNull()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var task = CreatePoolTask(ownerId);
        await SaveAsync(task);

        // Act
        var result = await ReadAsync(
            repository => repository.GetByIdAsync(Guid.NewGuid(), task.Id, CancellationToken.None));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithDeletedTask_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = CreatePoolTask(userId);
        task.Delete(CreatedAt.AddDays(1));
        await SaveAsync(task);

        // Act
        var result = await ReadAsync(repository => repository.GetByIdAsync(userId, task.Id, CancellationToken.None));

        // Assert
        result.Should().BeNull();
    }

    // ---- GetByIdIncludingDeletedAsync ----

    [Fact]
    public async Task GetByIdIncludingDeletedAsync_WithDeletedTask_ReturnsTask()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = CreatePoolTask(userId);
        task.Delete(CreatedAt.AddDays(1));
        await SaveAsync(task);

        // Act
        var result = await ReadAsync(
            repository => repository.GetByIdIncludingDeletedAsync(userId, task.Id, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdIncludingDeletedAsync_WithOtherUsersDeletedTask_ReturnsNull()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var task = CreatePoolTask(ownerId);
        task.Delete(CreatedAt.AddDays(1));
        await SaveAsync(task);

        // Act
        var result = await ReadAsync(
            repository => repository.GetByIdIncludingDeletedAsync(Guid.NewGuid(), task.Id, CancellationToken.None));

        // Assert
        result.Should().BeNull();
    }

    // ---- GetByDateAsync ----

    [Fact]
    public async Task GetByDateAsync_ReturnsOnlyThatDaysNonDeletedTasksOfUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var included = CreateAssignedTask(userId, Day);
        var otherDay = CreateAssignedTask(userId, Day.AddDays(1));
        var deleted = CreateAssignedTask(userId, Day);
        deleted.Delete(CreatedAt.AddDays(1));
        var pooled = CreatePoolTask(userId);
        var otherUsers = CreateAssignedTask(Guid.NewGuid(), Day);
        await SaveAsync(included, otherDay, deleted, pooled, otherUsers);

        // Act
        var result = await ReadAsync(repository => repository.GetByDateAsync(userId, Day, CancellationToken.None));

        // Assert
        result.Select(task => task.Id).Should().BeEquivalentTo([included.Id]);
    }

    // ---- GetPoolAsync ----

    [Fact]
    public async Task GetPoolAsync_ReturnsOnlyNonDeletedPoolTasksOfUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var pooled = CreatePoolTask(userId);
        var assigned = CreateAssignedTask(userId, Day);
        var deletedPooled = CreatePoolTask(userId);
        deletedPooled.Delete(CreatedAt.AddDays(1));
        var otherUsers = CreatePoolTask(Guid.NewGuid());
        await SaveAsync(pooled, assigned, deletedPooled, otherUsers);

        // Act
        var result = await ReadAsync(repository => repository.GetPoolAsync(userId, CancellationToken.None));

        // Assert
        result.Select(task => task.Id).Should().BeEquivalentTo([pooled.Id]);
    }

    // ---- GetByDateRangeAsync ----

    [Fact]
    public async Task GetByDateRangeAsync_IncludesBoundaryDaysAndExcludesOutsideDays()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var from = Day;
        var to = Day.AddDays(6);
        var dayBefore = CreateAssignedTask(userId, from.AddDays(-1));
        var onFrom = CreateAssignedTask(userId, from);
        var inside = CreateAssignedTask(userId, from.AddDays(3));
        var onTo = CreateAssignedTask(userId, to);
        var dayAfter = CreateAssignedTask(userId, to.AddDays(1));
        var deletedInside = CreateAssignedTask(userId, from.AddDays(2));
        deletedInside.Delete(CreatedAt.AddDays(1));
        var otherUsers = CreateAssignedTask(Guid.NewGuid(), from.AddDays(1));
        await SaveAsync(dayBefore, onFrom, inside, onTo, dayAfter, deletedInside, otherUsers);

        // Act
        var result = await ReadAsync(
            repository => repository.GetByDateRangeAsync(userId, from, to, CancellationToken.None));

        // Assert
        result.Select(task => task.Id).Should().BeEquivalentTo([onFrom.Id, inside.Id, onTo.Id]);
    }

    // ---- Eşleme ----

    [Fact]
    public async Task SaveAndReload_WithAllProperties_RestoresSameValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var completedAt = new DateTimeOffset(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);
        var deletedAt = new DateTimeOffset(2026, 1, 7, 12, 0, 0, TimeSpan.Zero);
        var task = PlanningTask.CreateInPool(
            Guid.NewGuid(), userId, "  Tüm alanlar  ", TaskPriority.Critical, 1234.5d, CreatedAt);
        task.SetFocusColor(FocusColor.Purple);
        task.AssignToDate(Day, 2345.25d);
        task.SetReminder(new TimeOnly(9, 30));
        task.Complete(completedAt);
        task.Delete(deletedAt);
        await SaveAsync(task);

        // Act
        var result = await ReadAsync(
            repository => repository.GetByIdIncludingDeletedAsync(userId, task.Id, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.UserId.Should().Be(userId);
        result.Title.Should().Be("Tüm alanlar");
        result.Priority.Should().Be(TaskPriority.Critical);
        result.FocusColor.Should().Be(FocusColor.Purple);
        result.SortOrder.Should().Be(2345.25d);
        result.AssignedDate.Should().Be(Day);
        result.ReminderTime.Should().Be(new TimeOnly(9, 30));
        result.IsCompleted.Should().BeTrue();
        result.CreatedAt.Should().Be(CreatedAt);
        result.CompletedAt.Should().Be(completedAt);
        result.IsDeleted.Should().BeTrue();
        result.DeletedAt.Should().Be(deletedAt);
    }

    [Fact]
    public async Task SaveAndReload_WithNullableValuesUnset_RestoresNulls()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = CreatePoolTask(userId);
        await SaveAsync(task);

        // Act
        var result = await ReadAsync(repository => repository.GetByIdAsync(userId, task.Id, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.FocusColor.Should().BeNull();
        result.AssignedDate.Should().BeNull();
        result.ReminderTime.Should().BeNull();
        result.CompletedAt.Should().BeNull();
        result.DeletedAt.Should().BeNull();
        result.IsCompleted.Should().BeFalse();
        result.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAndReload_WithOffsetCompletedAt_PreservesInstantAndReturnsZeroOffset()
    {
        // Arrange: 2026-01-07 01:30 +03:00 = 2026-01-06 22:30 UTC (UTC günü farklı)
        var userId = Guid.NewGuid();
        var completedAt = new DateTimeOffset(2026, 1, 7, 1, 30, 0, TimeSpan.FromHours(3));
        var task = CreateAssignedTask(userId, Day);
        task.Complete(completedAt);
        await SaveAsync(task);

        // Act
        var result = await ReadAsync(repository => repository.GetByIdAsync(userId, task.Id, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.CompletedAt.Should().NotBeNull();
        result.CompletedAt!.Value.Should().Be(completedAt); // aynı an (instant)
        result.CompletedAt.Value.UtcDateTime.Should().Be(new DateTime(2026, 1, 6, 22, 30, 0, DateTimeKind.Utc));
        result.CompletedAt.Value.Offset.Should().Be(TimeSpan.Zero);
    }

    // ---- Takipli entity üzerinden güncelleme ----

    [Fact]
    public async Task Uncomplete_OnTrackedCompletedTask_IsPersistedOnSave()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = CreateAssignedTask(userId, Day);
        task.Complete(new DateTimeOffset(2026, 1, 7, 10, 0, 0, TimeSpan.FromHours(3)));
        await SaveAsync(task);

        // Act
        await using (var updateContext = fixture.CreateDbContext())
        {
            var repository = new PlanningTaskRepository(updateContext);
            var tracked = await repository.GetByIdAsync(userId, task.Id, CancellationToken.None);
            tracked!.Uncomplete();
            await updateContext.SaveChangesAsync();
        }

        // Assert
        var result = await ReadAsync(repository => repository.GetByIdAsync(userId, task.Id, CancellationToken.None));
        result.Should().NotBeNull();
        result!.IsCompleted.Should().BeFalse();
        result.CompletedAt.Should().BeNull();
    }

    // ---- Test Helpers ----

    private static PlanningTask CreatePoolTask(Guid userId)
        => PlanningTask.CreateInPool(Guid.NewGuid(), userId, "Örnek görev", TaskPriority.Normal, 1000d, CreatedAt);

    private static PlanningTask CreateAssignedTask(Guid userId, DateOnly date)
    {
        var task = CreatePoolTask(userId);
        task.AssignToDate(date, 1000d);
        return task;
    }

    // Yazma ayrı bir DbContext ile yapılır
    private async Task SaveAsync(params PlanningTask[] tasks)
    {
        await using var context = fixture.CreateDbContext();
        var repository = new PlanningTaskRepository(context);
        foreach (var task in tasks)
            await repository.AddAsync(task, CancellationToken.None);

        await context.SaveChangesAsync();
    }

    // Okuma, yazmadan bağımsız yeni bir DbContext ile yapılır
    private async Task<T> ReadAsync<T>(Func<PlanningTaskRepository, Task<T>> read)
    {
        await using var context = fixture.CreateDbContext();
        return await read(new PlanningTaskRepository(context));
    }
}
