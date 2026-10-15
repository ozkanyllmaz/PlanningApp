using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Common.Models;
using PlanningApp.Application.Features.Auth.Commands.Register;
using PlanningApp.Domain.Auth;
using PlanningApp.Domain.Users;
using Xunit;

namespace PlanningApp.Test.Features.Auth.Commands.Register;

public class RegisterCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly RefreshTokenMaterial RefreshMaterial = new("plain-refresh", "hash-refresh", Now.AddDays(60));
    private static readonly AccessTokenResult AccessToken = new("access-token", Now.AddMinutes(15));

    // ---- Başarılı kayıt ----

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsAuthResultWithTokens()
    {
        // Arrange
        var fixture = new Fixture();

        // Act
        var result = await fixture.Handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        result.UserId.Should().NotBeEmpty();
        result.UserId.Should().Be(fixture.CreatedUserId);
        result.AccessToken.Should().Be("access-token");
        result.AccessTokenExpiresAt.Should().Be(AccessToken.ExpiresAt);
        result.RefreshToken.Should().Be("plain-refresh");
        result.RefreshTokenExpiresAt.Should().Be(RefreshMaterial.ExpiresAt);
    }

    [Fact]
    public async Task Handle_WithValidCommand_AddsProfileWithCorrectUserIdAndTimeZone()
    {
        // Arrange
        var fixture = new Fixture();

        // Act
        await fixture.Handler.Handle(CreateCommand(timeZoneId: "Europe/Istanbul"), CancellationToken.None);

        // Assert
        var profile = fixture.AddedProfile;
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(fixture.CreatedUserId);
        profile.TimeZoneId.Should().Be("Europe/Istanbul");
        profile.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_WithValidCommand_AddsRefreshTokenWithServiceHash()
    {
        // Arrange
        var fixture = new Fixture();

        // Act
        await fixture.Handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert: düz token değil, ITokenService'in verdiği özet saklanır
        var token = fixture.AddedRefreshToken;
        token.Should().NotBeNull();
        token!.UserId.Should().Be(fixture.CreatedUserId);
        token.TokenHash.Should().Be("hash-refresh");
        token.CreatedAt.Should().Be(Now);
        token.ExpiresAt.Should().Be(RefreshMaterial.ExpiresAt);
    }

    [Fact]
    public async Task Handle_WithValidCommand_SavesChangesOnceAndDoesNotDeleteUser()
    {
        // Arrange
        var fixture = new Fixture();

        // Act
        await fixture.Handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        fixture.IdentityService.Verify(
            service => service.DeleteUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Kimlik hatası ----

    [Fact]
    public async Task Handle_WhenIdentityCreationFails_ThrowsValidationExceptionWithFieldErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Bu e-posta zaten kayıtlı."],
            ["Password"] = ["Parola en az 8 karakter olmalıdır."]
        };
        var fixture = new Fixture(identityResult: IdentityOperationResult.Failure(errors));

        // Act
        var act = async () => await fixture.Handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors["Email"].Should().Equal("Bu e-posta zaten kayıtlı.");
        exception.Errors["Password"].Should().Equal("Parola en az 8 karakter olmalıdır.");
    }

    [Fact]
    public async Task Handle_WhenIdentityCreationFails_WritesNothingAndDoesNotDeleteUser()
    {
        // Arrange
        var errors = new Dictionary<string, string[]> { ["Email"] = ["Bu e-posta zaten kayıtlı."] };
        var fixture = new Fixture(identityResult: IdentityOperationResult.Failure(errors));

        // Act
        var act = async () => await fixture.Handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        fixture.UserProfileRepository.Verify(
            repository => repository.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.RefreshTokenRepository.Verify(
            repository => repository.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        fixture.IdentityService.Verify(
            service => service.DeleteUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Telafi (compensation) ----

    [Fact]
    public async Task Handle_WhenSaveChangesFails_DeletesCreatedUserAndRethrows()
    {
        // Arrange
        var fixture = new Fixture(saveException: new InvalidOperationException("veritabanı hatası"));

        // Act
        var act = async () => await fixture.Handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("veritabanı hatası");
        fixture.IdentityService.Verify(
            service => service.DeleteUserAsync(fixture.CreatedUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSaveChangesAndCompensationBothFail_RethrowsOriginalSaveChangesException()
    {
        // Arrange
        var saveException = new InvalidOperationException("veritabanı hatası");
        var fixture = new Fixture(saveException: saveException);
        fixture.IdentityService
            .Setup(service => service.DeleteUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("telafi hatası"));

        // Act
        var act = async () => await fixture.Handler.Handle(CreateCommand(), CancellationToken.None);

        // Assert: telafi hatası yutulur, asıl hata (aynı nesne) yükselir
        var thrown = (await act.Should().ThrowAsync<Exception>()).Which;
        thrown.Should().BeSameAs(saveException);
        fixture.IdentityService.Verify(
            service => service.DeleteUserAsync(fixture.CreatedUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Test Helpers ----

    private static RegisterCommand CreateCommand(string timeZoneId = "Europe/Istanbul")
        => new("user@example.com", "ornek-parola-123", timeZoneId);

    // Tüm mock'ları kurar; handler'ın ürettiği kullanıcı kimliği ve eklenen kayıtlar buradan okunur
    private sealed class Fixture
    {
        public Mock<IIdentityService> IdentityService { get; } = new();
        public Mock<ITokenService> TokenService { get; } = new();
        public Mock<IUserProfileRepository> UserProfileRepository { get; } = new();
        public Mock<IRefreshTokenRepository> RefreshTokenRepository { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();

        public RegisterCommandHandler Handler { get; }

        public Guid CreatedUserId { get; private set; }
        public UserProfile? AddedProfile { get; private set; }
        public RefreshToken? AddedRefreshToken { get; private set; }

        public Fixture(IdentityOperationResult? identityResult = null, Exception? saveException = null)
        {
            IdentityService
                .Setup(service => service.CreateUserAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, string, string, CancellationToken>((userId, _, _, _) => CreatedUserId = userId)
                .ReturnsAsync(identityResult ?? IdentityOperationResult.Success());

            TokenService
                .Setup(service => service.CreateRefreshToken(Now))
                .Returns(RefreshMaterial);
            TokenService
                .Setup(service => service.CreateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), Now))
                .Returns(AccessToken);

            UserProfileRepository
                .Setup(repository => repository.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
                .Callback<UserProfile, CancellationToken>((profile, _) => AddedProfile = profile)
                .Returns(Task.CompletedTask);
            RefreshTokenRepository
                .Setup(repository => repository.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
                .Callback<RefreshToken, CancellationToken>((token, _) => AddedRefreshToken = token)
                .Returns(Task.CompletedTask);

            if (saveException is not null)
            {
                UnitOfWork
                    .Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(saveException);
            }

            Handler = new RegisterCommandHandler(
                IdentityService.Object,
                TokenService.Object,
                UserProfileRepository.Object,
                RefreshTokenRepository.Object,
                UnitOfWork.Object,
                new FakeTimeProvider(Now));
        }
    }
}
