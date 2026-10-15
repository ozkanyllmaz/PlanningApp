using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlanningApp.Domain.Weeks;
using PlanningApp.Persistence.Repositories;

namespace PlanningApp.IntegrationTests.Persistence.Repositories;

[Collection(PostgresCollection.Name)]
public class PlanningWeekRepositoryTests(PostgresFixture fixture)
{
    // 2026-01-05 Pazartesi
    private static readonly DateOnly WeekStart = new(2026, 1, 5);

    // ---- GetByStartDateAsync ----

    [Fact]
    public async Task GetByStartDateAsync_WithExistingWeek_ReturnsWeek()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var week = CreateWeek(userId, WeekStart);
        await SaveAsync(week);

        // Act
        var result = await ReadAsync(
            repository => repository.GetByStartDateAsync(userId, WeekStart, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(week.Id);
        result.UserId.Should().Be(userId);
        result.WeekStartDate.Should().Be(WeekStart);
    }

    [Fact]
    public async Task GetByStartDateAsync_WithOtherUsersWeek_ReturnsNull()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        await SaveAsync(CreateWeek(ownerId, WeekStart));

        // Act
        var result = await ReadAsync(
            repository => repository.GetByStartDateAsync(Guid.NewGuid(), WeekStart, CancellationToken.None));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByStartDateAsync_WithDifferentStartDate_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SaveAsync(CreateWeek(userId, WeekStart));

        // Act
        var result = await ReadAsync(
            repository => repository.GetByStartDateAsync(userId, WeekStart.AddDays(7), CancellationToken.None));

        // Assert
        result.Should().BeNull();
    }

    // ---- Eşleme ----

    [Fact]
    public async Task SaveAndReload_WithArchivedWeek_RestoresSameValues()
    {
        // Arrange: 2026-01-12 13:30 +03:00 = 10:30 UTC
        var userId = Guid.NewGuid();
        var archivedAt = new DateTimeOffset(2026, 1, 12, 13, 30, 0, TimeSpan.FromHours(3));
        var week = CreateWeek(userId, WeekStart);
        week.Archive(33.33m, new DateOnly(2026, 1, 12), archivedAt);
        await SaveAsync(week);

        // Act
        var result = await ReadAsync(
            repository => repository.GetByStartDateAsync(userId, WeekStart, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(week.Id);
        result.UserId.Should().Be(userId);
        result.WeekStartDate.Should().Be(WeekStart);
        result.WeekEndDate.Should().Be(new DateOnly(2026, 1, 11));
        result.IsArchived.Should().BeTrue();
        result.CompletionPercentage.Should().Be(33.33m);
        result.ArchivedAt.Should().Be(archivedAt); // aynı an (instant)
        result.ArchivedAt!.Value.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task SaveAndReload_WithUnarchivedWeek_RestoresNulls()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SaveAsync(CreateWeek(userId, WeekStart));

        // Act
        var result = await ReadAsync(
            repository => repository.GetByStartDateAsync(userId, WeekStart, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.IsArchived.Should().BeFalse();
        result.CompletionPercentage.Should().BeNull();
        result.ArchivedAt.Should().BeNull();
    }

    // ---- Benzersizlik ----

    [Fact]
    public async Task Add_WithSameUserAndSameWeekStartDate_ThrowsDbUpdateException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SaveAsync(CreateWeek(userId, WeekStart));

        // Act
        var act = async () => await SaveAsync(CreateWeek(userId, WeekStart));

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Add_WithDifferentUsersAndSameWeekStartDate_Succeeds()
    {
        // Arrange
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        await SaveAsync(CreateWeek(firstUserId, WeekStart));

        // Act
        var act = async () => await SaveAsync(CreateWeek(secondUserId, WeekStart));

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ---- Test Helpers ----

    private static PlanningWeek CreateWeek(Guid userId, DateOnly weekStartDate)
        => PlanningWeek.Create(Guid.NewGuid(), userId, weekStartDate);

    // Yazma ayrı bir DbContext ile yapılır
    private async Task SaveAsync(PlanningWeek week)
    {
        await using var context = fixture.CreateDbContext();
        var repository = new PlanningWeekRepository(context);
        await repository.AddAsync(week, CancellationToken.None);

        await context.SaveChangesAsync();
    }

    // Okuma, yazmadan bağımsız yeni bir DbContext ile yapılır
    private async Task<T> ReadAsync<T>(Func<PlanningWeekRepository, Task<T>> read)
    {
        await using var context = fixture.CreateDbContext();
        return await read(new PlanningWeekRepository(context));
    }
}
