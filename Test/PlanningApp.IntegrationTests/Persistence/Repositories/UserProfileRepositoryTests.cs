using FluentAssertions;
using PlanningApp.Domain.Users;
using PlanningApp.Persistence.Repositories;

namespace PlanningApp.IntegrationTests.Persistence.Repositories;

[Collection(PostgresCollection.Name)]
public class UserProfileRepositoryTests(PostgresFixture fixture)
{
    // 2026-01-02 22:30 UTC = 2026-01-03 01:30 +03:00
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 3, 1, 30, 0, TimeSpan.FromHours(3));

    // ---- GetByIdAsync ----

    [Fact]
    public async Task GetByIdAsync_WithExistingProfile_ReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SaveAsync(UserProfile.Create(userId, "Europe/Istanbul", CreatedAt));

        // Act
        var result = await ReadAsync(repository => repository.GetByIdAsync(userId, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetByIdAsync_WithOtherUsersId_ReturnsNull()
    {
        // Arrange
        await SaveAsync(UserProfile.Create(Guid.NewGuid(), "Europe/Istanbul", CreatedAt));

        // Act
        var result = await ReadAsync(repository => repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));

        // Assert
        result.Should().BeNull();
    }

    // ---- Eşleme ----

    [Fact]
    public async Task SaveAndReload_WithAllProperties_RestoresSameValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SaveAsync(UserProfile.Create(userId, "America/New_York", CreatedAt));

        // Act
        var result = await ReadAsync(repository => repository.GetByIdAsync(userId, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.TimeZoneId.Should().Be("America/New_York");
        result.CreatedAt.Should().Be(CreatedAt); // aynı an (instant)
        result.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task SaveAndReload_AfterTimeZoneChange_PersistsNewTimeZone()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SaveAsync(UserProfile.Create(userId, "Europe/Istanbul", CreatedAt));

        // Act
        await using (var context = fixture.CreateDbContext())
        {
            var tracked = await new UserProfileRepository(context).GetByIdAsync(userId, CancellationToken.None);
            tracked!.ChangeTimeZone("Europe/London");
            await context.SaveChangesAsync();
        }

        // Assert
        var result = await ReadAsync(repository => repository.GetByIdAsync(userId, CancellationToken.None));
        result!.TimeZoneId.Should().Be("Europe/London");
    }

    // ---- Test Helpers ----

    // UserProfile için repository'de Add yok (kayıt Authentication akışında oluşur); test doğrudan DbContext kullanır
    private async Task SaveAsync(UserProfile profile)
    {
        await using var context = fixture.CreateDbContext();
        context.UserProfiles.Add(profile);

        await context.SaveChangesAsync();
    }

    // Okuma, yazmadan bağımsız yeni bir DbContext ile yapılır
    private async Task<T> ReadAsync<T>(Func<UserProfileRepository, Task<T>> read)
    {
        await using var context = fixture.CreateDbContext();
        return await read(new UserProfileRepository(context));
    }
}
