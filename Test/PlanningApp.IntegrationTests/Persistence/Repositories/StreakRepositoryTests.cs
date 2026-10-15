using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlanningApp.Domain.Gamification;
using PlanningApp.Persistence.Repositories;

namespace PlanningApp.IntegrationTests.Persistence.Repositories;

[Collection(PostgresCollection.Name)]
public class StreakRepositoryTests(PostgresFixture fixture)
{
    private static readonly DateOnly StartDate = new(2026, 1, 5);

    // ---- GetByUserIdAsync ----

    [Fact]
    public async Task GetByUserIdAsync_WithExistingStreak_ReturnsStreak()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var streak = CreateStreak(userId);
        await SaveAsync(streak);

        // Act
        var result = await ReadAsync(repository => repository.GetByUserIdAsync(userId, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(streak.Id);
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithOtherUsersStreak_ReturnsNull()
    {
        // Arrange
        await SaveAsync(CreateStreak(Guid.NewGuid()));

        // Act
        var result = await ReadAsync(
            repository => repository.GetByUserIdAsync(Guid.NewGuid(), CancellationToken.None));

        // Assert
        result.Should().BeNull();
    }

    // ---- Eşleme ----

    [Fact]
    public async Task SaveAndReload_WithAllProperties_RestoresSameValues()
    {
        // Arrange: 3 başarılı gün, 1 freeze, ardından 1 boş gün
        var userId = Guid.NewGuid();
        var streak = CreateStreak(userId);
        streak.EvaluateDay(100m, StartDate);
        streak.EvaluateDay(80m, StartDate.AddDays(1));
        streak.EvaluateDay(70m, StartDate.AddDays(2));
        streak.GrantFreeze(2);
        streak.EvaluateEmptyDay(StartDate.AddDays(3));
        await SaveAsync(streak);

        // Act
        var result = await ReadAsync(repository => repository.GetByUserIdAsync(userId, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(streak.Id);
        result.UserId.Should().Be(userId);
        result.CurrentStreak.Should().Be(3);
        result.LongestStreak.Should().Be(3);
        result.LastEvaluatedDate.Should().Be(StartDate.AddDays(3));
        result.AvailableFreezes.Should().Be(2);
        result.ConsecutiveEmptyDays.Should().Be(1);
    }

    [Fact]
    public async Task SaveAndReload_WithNeverEvaluatedStreak_RestoresNullLastEvaluatedDate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SaveAsync(CreateStreak(userId));

        // Act
        var result = await ReadAsync(repository => repository.GetByUserIdAsync(userId, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.LastEvaluatedDate.Should().BeNull();
        result.CurrentStreak.Should().Be(0);
    }

    // ---- Benzersizlik ----

    [Fact]
    public async Task Add_WithSecondStreakForSameUser_ThrowsDbUpdateException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SaveAsync(CreateStreak(userId));

        // Act
        var act = async () => await SaveAsync(CreateStreak(userId));

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // ---- Eşzamanlılık ----

    [Fact]
    public async Task SaveChanges_WhenSameStreakWasChangedByAnotherContext_ThrowsDbUpdateConcurrencyException()
    {
        // Arrange: iki ayrı DbContext aynı streak'i yükler
        var userId = Guid.NewGuid();
        await SaveAsync(CreateStreak(userId));

        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var first = await new StreakRepository(firstContext).GetByUserIdAsync(userId, CancellationToken.None);
        var second = await new StreakRepository(secondContext).GetByUserIdAsync(userId, CancellationToken.None);

        first!.GrantFreeze(1);
        second!.GrantFreeze(1);
        await firstContext.SaveChangesAsync();

        // Act
        var act = async () => await secondContext.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task SaveChanges_WhenStreakIsChangedByOnlyOneContext_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SaveAsync(CreateStreak(userId));

        // Act
        await using (var context = fixture.CreateDbContext())
        {
            var tracked = await new StreakRepository(context).GetByUserIdAsync(userId, CancellationToken.None);
            tracked!.GrantFreeze(1);
            await context.SaveChangesAsync();
        }

        // Assert
        var result = await ReadAsync(repository => repository.GetByUserIdAsync(userId, CancellationToken.None));
        result!.AvailableFreezes.Should().Be(1);
    }

    // ---- Test Helpers ----

    private static Streak CreateStreak(Guid userId)
        => Streak.Create(Guid.NewGuid(), userId);

    // Yazma ayrı bir DbContext ile yapılır
    private async Task SaveAsync(Streak streak)
    {
        await using var context = fixture.CreateDbContext();
        var repository = new StreakRepository(context);
        await repository.AddAsync(streak, CancellationToken.None);

        await context.SaveChangesAsync();
    }

    // Okuma, yazmadan bağımsız yeni bir DbContext ile yapılır
    private async Task<T> ReadAsync<T>(Func<StreakRepository, Task<T>> read)
    {
        await using var context = fixture.CreateDbContext();
        return await read(new StreakRepository(context));
    }
}
