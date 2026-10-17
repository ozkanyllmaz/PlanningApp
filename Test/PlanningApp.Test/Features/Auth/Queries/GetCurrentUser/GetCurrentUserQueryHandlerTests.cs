using FluentAssertions;
using Moq;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Models;
using PlanningApp.Application.Features.Auth.Queries.GetCurrentUser;
using Xunit;

namespace PlanningApp.Test.Features.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandlerTests
{
    private static readonly Guid UserId = new("11111111-1111-1111-1111-111111111111");

    // ---- Kullanıcı var ----

    [Fact]
    public async Task Handle_WithExistingUser_ReturnsUserIdAndEmail()
    {
        // Arrange
        var fixture = new Fixture(new AppUserInfo(UserId, "user@example.com"));

        // Act
        var result = await fixture.Handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        result.Should().Be(new CurrentUserDto(UserId, "user@example.com"));
    }

    [Fact]
    public async Task Handle_Always_LooksUpUserWithCurrentUserId()
    {
        // Arrange
        var fixture = new Fixture(new AppUserInfo(UserId, "user@example.com"));

        // Act
        await fixture.Handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        fixture.IdentityService.Verify(
            service => service.FindByIdAsync(UserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Kullanıcı yok ----

    [Fact]
    public async Task Handle_WithMissingUser_ThrowsAuthenticationFailedException()
    {
        // Arrange
        var fixture = new Fixture(null);

        // Act
        var act = async () => await fixture.Handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticationFailedException>()
            .WithMessage(AuthenticationFailedException.InvalidSessionMessage);
    }

    // ---- Test Helpers ----

    private sealed class Fixture
    {
        public Mock<IIdentityService> IdentityService { get; } = new();

        public GetCurrentUserQueryHandler Handler { get; }

        public Fixture(AppUserInfo? user)
        {
            IdentityService
                .Setup(service => service.FindByIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(service => service.UserId).Returns(UserId);

            Handler = new GetCurrentUserQueryHandler(IdentityService.Object, currentUser.Object);
        }
    }
}
