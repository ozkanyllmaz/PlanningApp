using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Common.Models;
using PlanningApp.Application.Features.Auth.Commands.Login;
using PlanningApp.Domain.Auth;
using Xunit;

namespace PlanningApp.Test.Features.Auth.Commands.Login;

public class LoginCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid SampleUserId = new("11111111-1111-1111-1111-111111111111");
    private static readonly RefreshTokenMaterial RefreshMaterial = new("plain-refresh", "hash-refresh", Now.AddDays(60));
    private static readonly AccessTokenResult AccessToken = new("access-token", Now.AddMinutes(15));

    // ---- Başarılı giriş ----

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAuthResultWithTokens()
    {
        // Arrange
        var fixture = new Fixture(CredentialCheckResultFor(SampleUserId, "user@example.com"));

        // Act
        var result = await fixture.Handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        result.UserId.Should().Be(SampleUserId);
        result.AccessToken.Should().Be("access-token");
        result.AccessTokenExpiresAt.Should().Be(AccessToken.ExpiresAt);
        result.RefreshToken.Should().Be("plain-refresh");
        result.RefreshTokenExpiresAt.Should().Be(RefreshMaterial.ExpiresAt);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_AddsRefreshTokenAndSavesOnce()
    {
        // Arrange
        var fixture = new Fixture(CredentialCheckResultFor(SampleUserId, "user@example.com"));

        // Act
        await fixture.Handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        var token = fixture.AddedRefreshToken;
        token.Should().NotBeNull();
        token!.UserId.Should().Be(SampleUserId);
        token.TokenHash.Should().Be("hash-refresh");
        token.CreatedAt.Should().Be(Now);
        token.ExpiresAt.Should().Be(RefreshMaterial.ExpiresAt);
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Başarısız giriş ----

    [Fact]
    public async Task Handle_WithInvalidCredentials_ThrowsAuthenticationFailedExceptionWithGenericMessage()
    {
        // Arrange
        var fixture = new Fixture(new CredentialCheckResult(false, null, null));

        // Act
        var act = async () => await fixture.Handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticationFailedException>()
            .WithMessage("E-posta veya şifre hatalı.");
    }

    [Fact]
    public async Task Handle_WithInvalidCredentials_CreatesNoTokenAndDoesNotSave()
    {
        // Arrange
        var fixture = new Fixture(new CredentialCheckResult(false, null, null));

        // Act
        var act = async () => await fixture.Handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticationFailedException>();
        fixture.TokenService.Verify(service => service.CreateRefreshToken(It.IsAny<DateTimeOffset>()), Times.Never);
        fixture.TokenService.Verify(
            service => service.CreateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()),
            Times.Never);
        fixture.RefreshTokenRepository.Verify(
            repository => repository.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Test Helpers ----

    private static LoginCommand CreateCommand()
        => new("user@example.com", "ornek-parola-123");

    private static CredentialCheckResult CredentialCheckResultFor(Guid userId, string email)
        => new(true, userId, email);

    private sealed class Fixture
    {
        public Mock<IIdentityService> IdentityService { get; } = new();
        public Mock<ITokenService> TokenService { get; } = new();
        public Mock<IRefreshTokenRepository> RefreshTokenRepository { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();

        public LoginCommandHandler Handler { get; }

        public RefreshToken? AddedRefreshToken { get; private set; }

        public Fixture(CredentialCheckResult checkResult)
        {
            IdentityService
                .Setup(service => service.CheckCredentialsAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(checkResult);

            TokenService
                .Setup(service => service.CreateRefreshToken(Now))
                .Returns(RefreshMaterial);
            TokenService
                .Setup(service => service.CreateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), Now))
                .Returns(AccessToken);

            RefreshTokenRepository
                .Setup(repository => repository.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
                .Callback<RefreshToken, CancellationToken>((token, _) => AddedRefreshToken = token)
                .Returns(Task.CompletedTask);

            Handler = new LoginCommandHandler(
                IdentityService.Object,
                TokenService.Object,
                RefreshTokenRepository.Object,
                UnitOfWork.Object,
                new FakeTimeProvider(Now));
        }
    }
}
