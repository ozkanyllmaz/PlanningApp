using FluentAssertions;
using PlanningApp.Domain.Auth;
using PlanningApp.Domain.Common;
using Xunit;

namespace PlanningApp.Test.Auth;

public class RefreshTokenTests
{
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset SampleCreatedAt = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SampleExpiresAt = SampleCreatedAt.AddDays(60);

    // ---- Create ----

    [Fact]
    public void Create_WithValidValues_SetsProperties()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var token = RefreshToken.Create(id, SampleUserId, "hash-value", SampleCreatedAt, SampleExpiresAt);

        // Assert
        token.Id.Should().Be(id);
        token.UserId.Should().Be(SampleUserId);
        token.TokenHash.Should().Be("hash-value");
        token.CreatedAt.Should().Be(SampleCreatedAt);
        token.ExpiresAt.Should().Be(SampleExpiresAt);
        token.RevokedAt.Should().BeNull();
        token.ReplacedByTokenId.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        // Arrange & Act
        var act = () => RefreshToken.Create(Guid.NewGuid(), Guid.Empty, "hash-value", SampleCreatedAt, SampleExpiresAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Kullanıcı kimliği boş olamaz.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTokenHash_ThrowsDomainException(string tokenHash)
    {
        // Arrange & Act
        var act = () => RefreshToken.Create(Guid.NewGuid(), SampleUserId, tokenHash, SampleCreatedAt, SampleExpiresAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Token özeti boş olamaz.");
    }

    [Theory]
    [InlineData(0)]   // ExpiresAt == CreatedAt
    [InlineData(-60)] // ExpiresAt, CreatedAt'ten önce
    public void Create_WithExpiresAtNotAfterCreatedAt_ThrowsDomainException(int secondsAfterCreatedAt)
    {
        // Arrange
        var expiresAt = SampleCreatedAt.AddSeconds(secondsAfterCreatedAt);

        // Act
        var act = () => RefreshToken.Create(Guid.NewGuid(), SampleUserId, "hash-value", SampleCreatedAt, expiresAt);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Token bitiş zamanı oluşturulma zamanından sonra olmalıdır.");
    }

    // ---- IsActive ----

    [Fact]
    public void IsActive_BeforeExpiry_ReturnsTrue()
    {
        // Arrange
        var token = CreateSampleToken();

        // Act
        var result = token.IsActive(SampleExpiresAt.AddSeconds(-1));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ExactlyAtExpiry_ReturnsFalse()
    {
        // Arrange
        var token = CreateSampleToken();

        // Act
        var result = token.IsActive(SampleExpiresAt);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsActive_AfterExpiry_ReturnsFalse()
    {
        // Arrange
        var token = CreateSampleToken();

        // Act
        var result = token.IsActive(SampleExpiresAt.AddSeconds(1));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsActive_AfterRevoke_ReturnsFalse()
    {
        // Arrange
        var token = CreateSampleToken();
        token.Revoke(SampleCreatedAt.AddDays(1));

        // Act
        var result = token.IsActive(SampleCreatedAt.AddDays(2));

        // Assert
        result.Should().BeFalse();
    }

    // ---- Revoke ----

    [Fact]
    public void Revoke_WithReplacementId_SetsRevokedAtAndReplacedByTokenId()
    {
        // Arrange
        var token = CreateSampleToken();
        var revokedAt = SampleCreatedAt.AddDays(1);
        var replacedBy = Guid.NewGuid();

        // Act
        token.Revoke(revokedAt, replacedBy);

        // Assert
        token.RevokedAt.Should().Be(revokedAt);
        token.ReplacedByTokenId.Should().Be(replacedBy);
    }

    [Fact]
    public void Revoke_WithoutReplacementId_LeavesReplacedByTokenIdNull()
    {
        // Arrange
        var token = CreateSampleToken();
        var revokedAt = SampleCreatedAt.AddDays(1);

        // Act
        token.Revoke(revokedAt);

        // Assert
        token.RevokedAt.Should().Be(revokedAt);
        token.ReplacedByTokenId.Should().BeNull();
    }

    [Fact]
    public void Revoke_OnAlreadyRevokedToken_ThrowsDomainException()
    {
        // Arrange
        var token = CreateSampleToken();
        token.Revoke(SampleCreatedAt.AddDays(1));

        // Act
        var act = () => token.Revoke(SampleCreatedAt.AddDays(2));

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Token zaten iptal edilmiş.");
    }

    // ---- Test Helpers ----

    private static RefreshToken CreateSampleToken()
        => RefreshToken.Create(Guid.NewGuid(), SampleUserId, "hash-value", SampleCreatedAt, SampleExpiresAt);
}
