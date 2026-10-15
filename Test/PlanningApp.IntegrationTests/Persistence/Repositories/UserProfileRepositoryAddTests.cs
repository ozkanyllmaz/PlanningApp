using FluentAssertions;
using PlanningApp.Domain.Users;
using PlanningApp.Persistence.Repositories;

namespace PlanningApp.IntegrationTests.Persistence.Repositories;

[Collection(PostgresCollection.Name)]
public class UserProfileRepositoryAddTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);

    // ---- AddAsync ----

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Europe/Istanbul", CreatedAt);

        // Act
        await using (var writeContext = fixture.CreateDbContext())
        {
            await new UserProfileRepository(writeContext).AddAsync(profile, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        // Assert
        await using var readContext = fixture.CreateDbContext();
        var result = await new UserProfileRepository(readContext).GetByIdAsync(userId, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.TimeZoneId.Should().Be("Europe/Istanbul");
        result.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public async Task AddAsync_WithoutSaveChanges_DoesNotPersistProfile()
    {
        // Arrange: repository SaveChanges ÇAĞIRMAZ
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, "Europe/Istanbul", CreatedAt);

        // Act
        await using (var writeContext = fixture.CreateDbContext())
        {
            await new UserProfileRepository(writeContext).AddAsync(profile, CancellationToken.None);
        }

        // Assert
        await using var readContext = fixture.CreateDbContext();
        var result = await new UserProfileRepository(readContext).GetByIdAsync(userId, CancellationToken.None);
        result.Should().BeNull();
    }
}
