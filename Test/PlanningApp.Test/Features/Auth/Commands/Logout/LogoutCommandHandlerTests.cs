using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Features.Auth.Commands.Logout;
using PlanningApp.Domain.Auth;
using Xunit;

namespace PlanningApp.Test.Features.Auth.Commands.Logout;

public class LogoutCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");

    // ---- Aktif token ----

    [Fact]
    public async Task Handle_WithActiveToken_RevokesTokenAndSavesOnce()
    {
        // Arrange
        var token = CreateToken();
        var fixture = new Fixture(token);

        // Act
        await fixture.Handler.Handle(new LogoutCommand("plain"), CancellationToken.None);

        // Assert
        token.RevokedAt.Should().Be(Now);
        token.ReplacedByTokenId.Should().BeNull();
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Always_LooksUpTokenByHashOfPlainToken()
    {
        // Arrange
        var fixture = new Fixture(CreateToken());

        // Act
        await fixture.Handler.Handle(new LogoutCommand("plain"), CancellationToken.None);

        // Assert
        fixture.RefreshTokenRepository.Verify(
            repository => repository.GetByHashAsync("hash-of-plain", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Idempotency ----

    [Fact]
    public async Task Handle_WithUnknownToken_DoesNothingAndDoesNotSave()
    {
        // Arrange
        var fixture = new Fixture(existingToken: null);

        // Act
        var act = async () => await fixture.Handler.Handle(new LogoutCommand("plain"), CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAlreadyRevokedToken_DoesNothingAndDoesNotSave()
    {
        // Arrange
        var token = CreateToken();
        var firstRevokedAt = Now.AddDays(-1);
        token.Revoke(firstRevokedAt);
        var fixture = new Fixture(token);

        // Act
        var act = async () => await fixture.Handler.Handle(new LogoutCommand("plain"), CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        token.RevokedAt.Should().Be(firstRevokedAt);
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Test Helpers ----

    private static RefreshToken CreateToken()
        => RefreshToken.Create(Guid.NewGuid(), SampleUserId, "hash-of-plain", Now.AddDays(-1), Now.AddDays(59));

    private sealed class Fixture
    {
        public Mock<ITokenService> TokenService { get; } = new();
        public Mock<IRefreshTokenRepository> RefreshTokenRepository { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();

        public LogoutCommandHandler Handler { get; }

        public Fixture(RefreshToken? existingToken)
        {
            TokenService
                .Setup(service => service.HashRefreshToken(It.IsAny<string>()))
                .Returns<string>(plain => $"hash-of-{plain}");

            RefreshTokenRepository
                .Setup(repository => repository.GetByHashAsync("hash-of-plain", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingToken);

            Handler = new LogoutCommandHandler(
                TokenService.Object,
                RefreshTokenRepository.Object,
                UnitOfWork.Object,
                new FakeTimeProvider(Now));
        }
    }
}
