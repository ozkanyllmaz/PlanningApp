using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Common.Models;
using PlanningApp.Application.Features.Auth.Commands.RefreshAccessToken;
using PlanningApp.Domain.Auth;
using Xunit;

namespace PlanningApp.Test.Features.Auth.Commands.RefreshAccessToken;

public class RefreshAccessTokenCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");
    private static readonly RefreshTokenMaterial NewRefreshMaterial = new("new-plain-refresh", "new-hash", Now.AddDays(60));
    private static readonly AccessTokenResult AccessToken = new("access-token", Now.AddMinutes(15));

    // ---- Başarılı yenileme ----

    [Fact]
    public async Task Handle_WithActiveToken_RevokesOldTokenWithReplacementId()
    {
        // Arrange
        var existing = CreateActiveToken();
        var fixture = new Fixture(existing, new AppUserInfo(SampleUserId, "user@example.com"));

        // Act
        await fixture.Handler.Handle(new RefreshAccessTokenCommand("old-plain"), CancellationToken.None);

        // Assert
        fixture.AddedRefreshToken.Should().NotBeNull();
        existing.RevokedAt.Should().Be(Now);
        existing.ReplacedByTokenId.Should().Be(fixture.AddedRefreshToken!.Id);
    }

    [Fact]
    public async Task Handle_WithActiveToken_AddsNewTokenAndSavesOnce()
    {
        // Arrange
        var fixture = new Fixture(CreateActiveToken(), new AppUserInfo(SampleUserId, "user@example.com"));

        // Act
        await fixture.Handler.Handle(new RefreshAccessTokenCommand("old-plain"), CancellationToken.None);

        // Assert
        var added = fixture.AddedRefreshToken;
        added.Should().NotBeNull();
        added!.UserId.Should().Be(SampleUserId);
        added.TokenHash.Should().Be("new-hash");
        added.CreatedAt.Should().Be(Now);
        added.ExpiresAt.Should().Be(NewRefreshMaterial.ExpiresAt);
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithActiveToken_ReturnsNewTokens()
    {
        // Arrange
        var fixture = new Fixture(CreateActiveToken(), new AppUserInfo(SampleUserId, "user@example.com"));

        // Act
        var result = await fixture.Handler.Handle(new RefreshAccessTokenCommand("old-plain"), CancellationToken.None);

        // Assert
        result.UserId.Should().Be(SampleUserId);
        result.AccessToken.Should().Be("access-token");
        result.AccessTokenExpiresAt.Should().Be(AccessToken.ExpiresAt);
        result.RefreshToken.Should().Be("new-plain-refresh");
        result.RefreshTokenExpiresAt.Should().Be(NewRefreshMaterial.ExpiresAt);
    }

    [Fact]
    public async Task Handle_Always_LooksUpTokenByHashOfPlainToken()
    {
        // Arrange
        var fixture = new Fixture(CreateActiveToken(), new AppUserInfo(SampleUserId, "user@example.com"));

        // Act
        await fixture.Handler.Handle(new RefreshAccessTokenCommand("old-plain"), CancellationToken.None);

        // Assert
        fixture.RefreshTokenRepository.Verify(
            repository => repository.GetByHashAsync("hash-of-old-plain", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Geçersiz token ----

    [Fact]
    public async Task Handle_WithUnknownToken_ThrowsAuthenticationFailedExceptionWithoutSaving()
    {
        // Arrange
        var fixture = new Fixture(existingToken: null, new AppUserInfo(SampleUserId, "user@example.com"));

        // Act
        var act = async () => await fixture.Handler.Handle(
            new RefreshAccessTokenCommand("old-plain"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticationFailedException>();
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithRevokedToken_ThrowsAuthenticationFailedExceptionWithoutSaving()
    {
        // Arrange
        var revoked = CreateActiveToken();
        revoked.Revoke(Now.AddDays(-1));
        var fixture = new Fixture(revoked, new AppUserInfo(SampleUserId, "user@example.com"));

        // Act
        var act = async () => await fixture.Handler.Handle(
            new RefreshAccessTokenCommand("old-plain"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticationFailedException>();
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        fixture.RefreshTokenRepository.Verify(
            repository => repository.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ThrowsAuthenticationFailedExceptionWithoutSaving()
    {
        // Arrange: süresi tam şu anda doluyor (IsActive tam ExpiresAt anında false)
        var expired = RefreshToken.Create(
            Guid.NewGuid(), SampleUserId, "hash-of-old-plain", Now.AddDays(-60), Now);
        var fixture = new Fixture(expired, new AppUserInfo(SampleUserId, "user@example.com"));

        // Act
        var act = async () => await fixture.Handler.Handle(
            new RefreshAccessTokenCommand("old-plain"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticationFailedException>();
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsAuthenticationFailedExceptionAndKeepsOldTokenActive()
    {
        // Arrange
        var existing = CreateActiveToken();
        var fixture = new Fixture(existing, user: null);

        // Act
        var act = async () => await fixture.Handler.Handle(
            new RefreshAccessTokenCommand("old-plain"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticationFailedException>();
        existing.RevokedAt.Should().BeNull();
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Test Helpers ----

    private static RefreshToken CreateActiveToken()
        => RefreshToken.Create(
            Guid.NewGuid(), SampleUserId, "hash-of-old-plain", Now.AddDays(-1), Now.AddDays(59));

    private sealed class Fixture
    {
        public Mock<IIdentityService> IdentityService { get; } = new();
        public Mock<ITokenService> TokenService { get; } = new();
        public Mock<IRefreshTokenRepository> RefreshTokenRepository { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();

        public RefreshAccessTokenCommandHandler Handler { get; }

        public RefreshToken? AddedRefreshToken { get; private set; }

        public Fixture(RefreshToken? existingToken, AppUserInfo? user)
        {
            TokenService
                .Setup(service => service.HashRefreshToken(It.IsAny<string>()))
                .Returns<string>(plain => $"hash-of-{plain}");
            TokenService
                .Setup(service => service.CreateRefreshToken(Now))
                .Returns(NewRefreshMaterial);
            TokenService
                .Setup(service => service.CreateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), Now))
                .Returns(AccessToken);

            RefreshTokenRepository
                .Setup(repository => repository.GetByHashAsync("hash-of-old-plain", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingToken);
            RefreshTokenRepository
                .Setup(repository => repository.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
                .Callback<RefreshToken, CancellationToken>((token, _) => AddedRefreshToken = token)
                .Returns(Task.CompletedTask);

            IdentityService
                .Setup(service => service.FindByIdAsync(SampleUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            Handler = new RefreshAccessTokenCommandHandler(
                IdentityService.Object,
                TokenService.Object,
                RefreshTokenRepository.Object,
                UnitOfWork.Object,
                new FakeTimeProvider(Now));
        }
    }
}
