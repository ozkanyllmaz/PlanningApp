using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using PlanningApp.WebApi.Authentication;

namespace PlanningApp.ApiTests.Authentication;

public class HttpContextCurrentUserServiceTests
{
    // ---- sub claim'i okunur ----

    [Fact]
    public void UserId_WithValidSubClaim_ReturnsGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var service = CreateService(new Claim("sub", userId.ToString()));

        // Act
        var result = service.UserId;

        // Assert
        result.Should().Be(userId);
    }

    // ---- Geçersiz durumlar ----

    [Fact]
    public void UserId_WithoutSubClaim_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService(new Claim("email", "user@example.com"));

        // Act
        var act = () => service.UserId;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UserId_WithNonGuidSubClaim_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService(new Claim("sub", "guid-degil"));

        // Act
        var act = () => service.UserId;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UserId_WithoutHttpContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new HttpContextCurrentUserService(new HttpContextAccessor { HttpContext = null });

        // Act
        var act = () => service.UserId;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ---- Test Helpers ----

    private static HttpContextCurrentUserService CreateService(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"))
        };

        return new HttpContextCurrentUserService(new HttpContextAccessor { HttpContext = context });
    }
}
