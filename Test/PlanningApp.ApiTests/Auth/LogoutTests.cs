using System.Net;
using System.Security.Cryptography;
using FluentAssertions;
using PlanningApp.ApiTests.Support;
using static PlanningApp.ApiTests.Support.ApiTestHelpers;

namespace PlanningApp.ApiTests.Auth;

[Collection(ApiCollection.Name)]
public class LogoutTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Default.CreateClient();

    [Fact]
    public async Task Logout_WithValidToken_Returns204AndTokenCannotBeRefreshed()
    {
        // Arrange
        var (_, registered) = await RegisterUserAsync(_client);

        // Act
        var logout = await PostLogoutAsync(_client, registered.RefreshToken);
        var refresh = await PostRefreshAsync(_client, registered.RefreshToken);

        // Assert
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- İdempotent: bilgi sızdırılmaz ----

    [Fact]
    public async Task Logout_WithUnknownToken_Returns204()
    {
        // Arrange
        var randomToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // Act
        var response = await PostLogoutAsync(_client, randomToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
