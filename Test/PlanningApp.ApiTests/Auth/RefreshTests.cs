using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using FluentAssertions;
using PlanningApp.ApiTests.Support;
using PlanningApp.Application.Features.Auth;
using static PlanningApp.ApiTests.Support.ApiTestHelpers;

namespace PlanningApp.ApiTests.Auth;

[Collection(ApiCollection.Name)]
public class RefreshTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Default.CreateClient();

    // ---- Başarılı yenileme ----

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var (_, registered) = await RegisterUserAsync(_client);

        // Act
        var response = await PostRefreshAsync(_client, registered.RefreshToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await response.Content.ReadFromJsonAsync<AuthResult>();
        refreshed!.UserId.Should().Be(registered.UserId);
        refreshed.RefreshToken.Should().NotBe(registered.RefreshToken);
        refreshed.AccessToken.Should().NotBe(registered.AccessToken);
    }

    // ---- Yeniden kullanım ----

    [Fact]
    public async Task Refresh_WithAlreadyUsedToken_Returns401()
    {
        // Arrange
        var (_, registered) = await RegisterUserAsync(_client);
        (await PostRefreshAsync(_client, registered.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await PostRefreshAsync(_client, registered.RefreshToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
    }

    [Fact]
    public async Task Refresh_WithRandomToken_Returns401()
    {
        // Arrange
        var randomToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // Act
        var response = await PostRefreshAsync(_client, randomToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
    }
}
