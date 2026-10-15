using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlanningApp.Domain.Auth;
using PlanningApp.Persistence.Repositories;

namespace PlanningApp.IntegrationTests.Persistence.Repositories;

[Collection(PostgresCollection.Name)]
public class RefreshTokenRepositoryTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresAt = CreatedAt.AddDays(60);

    // ---- GetByHashAsync ----

    [Fact]
    public async Task GetByHashAsync_WithExistingToken_ReturnsToken()
    {
        // Arrange
        var token = CreateToken(NewHash());
        await SaveAsync(token);

        // Act
        var result = await ReadAsync(repository => repository.GetByHashAsync(token.TokenHash, CancellationToken.None));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(token.Id);
        result.UserId.Should().Be(token.UserId);
        result.TokenHash.Should().Be(token.TokenHash);
        result.CreatedAt.Should().Be(CreatedAt);
        result.ExpiresAt.Should().Be(ExpiresAt);
        result.RevokedAt.Should().BeNull();
        result.ReplacedByTokenId.Should().BeNull();
    }

    [Fact]
    public async Task GetByHashAsync_WithUnknownHash_ReturnsNull()
    {
        // Arrange & Act
        var result = await ReadAsync(repository => repository.GetByHashAsync(NewHash(), CancellationToken.None));

        // Assert
        result.Should().BeNull();
    }

    // ---- Benzersizlik ----

    [Fact]
    public async Task Add_WithSameTokenHashTwice_ThrowsDbUpdateException()
    {
        // Arrange
        var hash = NewHash();
        await SaveAsync(CreateToken(hash));

        // Act
        var act = async () => await SaveAsync(CreateToken(hash));

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // ---- İptal kalıcılığı ----

    [Fact]
    public async Task Revoke_OnTrackedToken_IsPersistedOnSave()
    {
        // Arrange
        var token = CreateToken(NewHash());
        await SaveAsync(token);
        var revokedAt = CreatedAt.AddDays(1);
        var replacedBy = Guid.NewGuid();

        // Act
        await using (var context = fixture.CreateDbContext())
        {
            var tracked = await new RefreshTokenRepository(context)
                .GetByHashAsync(token.TokenHash, CancellationToken.None);
            tracked!.Revoke(revokedAt, replacedBy);
            await context.SaveChangesAsync();
        }

        // Assert
        var result = await ReadAsync(repository => repository.GetByHashAsync(token.TokenHash, CancellationToken.None));
        result.Should().NotBeNull();
        result!.RevokedAt.Should().Be(revokedAt);
        result.ReplacedByTokenId.Should().Be(replacedBy);
        result.IsActive(CreatedAt.AddDays(2)).Should().BeFalse();
    }

    // ---- Eşzamanlılık ----

    [Fact]
    public async Task SaveChanges_WhenSameTokenWasRevokedByAnotherContext_ThrowsDbUpdateConcurrencyException()
    {
        // Arrange: iki ayrı DbContext aynı refresh token'ı yükler
        var token = CreateToken(NewHash());
        await SaveAsync(token);

        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var first = await new RefreshTokenRepository(firstContext)
            .GetByHashAsync(token.TokenHash, CancellationToken.None);
        var second = await new RefreshTokenRepository(secondContext)
            .GetByHashAsync(token.TokenHash, CancellationToken.None);

        first!.Revoke(CreatedAt.AddDays(1), Guid.NewGuid());
        second!.Revoke(CreatedAt.AddDays(1), Guid.NewGuid());
        await firstContext.SaveChangesAsync();

        // Act
        var act = async () => await secondContext.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // ---- Test Helpers ----

    // Özet benzersiz olmalı (UNIQUE index); her test kendi değerini üretir
    private static string NewHash()
        => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    private static RefreshToken CreateToken(string hash)
        => RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), hash, CreatedAt, ExpiresAt);

    // Yazma ayrı bir DbContext ile yapılır
    private async Task SaveAsync(RefreshToken token)
    {
        await using var context = fixture.CreateDbContext();
        var repository = new RefreshTokenRepository(context);
        await repository.AddAsync(token, CancellationToken.None);

        await context.SaveChangesAsync();
    }

    // Okuma, yazmadan bağımsız yeni bir DbContext ile yapılır
    private async Task<T> ReadAsync<T>(Func<RefreshTokenRepository, Task<T>> read)
    {
        await using var context = fixture.CreateDbContext();
        return await read(new RefreshTokenRepository(context));
    }
}
