using System.Net;
using FluentAssertions;
using PlanningApp.ApiTests.Support;
using static PlanningApp.ApiTests.Support.ApiTestHelpers;

namespace PlanningApp.ApiTests.Http;

// Düşük limitli AYRI bir uygulama örneği kullanılır; diğer testler etkilenmez
[Collection(ApiCollection.Name)]
public class RateLimitingTests(ApiFixture fixture)
{
    private const int PermitLimit = 3;

    [Fact]
    public async Task AuthEndpoint_WhenLimitExceeded_Returns429WithRetryAfterAndProblemJson()
    {
        // Arrange
        await using var factory = fixture.CreateFactory(permitLimit: PermitLimit);
        using var client = factory.CreateClient();
        for (var request = 0; request < PermitLimit; request++)
        {
            var allowed = await PostLoginAsync(client, NewEmail());
            allowed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // Act
        var response = await PostLoginAsync(client, NewEmail());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
        response.Headers.RetryAfter.Should().NotBeNull();
        response.Headers.RetryAfter!.Delta!.Value.TotalSeconds.Should().BeGreaterThan(0);

        var problem = await ReadProblemAsync(response);
        problem.GetProperty("status").GetInt32().Should().Be(429);
    }

    [Fact]
    public async Task AuthEndpoints_ShareTheSameLimitPerIp()
    {
        // Arrange
        await using var factory = fixture.CreateFactory(permitLimit: PermitLimit);
        using var client = factory.CreateClient();
        await PostLoginAsync(client, NewEmail());
        await PostRefreshAsync(client, "rastgele-token");
        await PostLogoutAsync(client, "rastgele-token");

        // Act
        var response = await PostRegisterAsync(client, NewEmail());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.Should().NotBeNull();
    }
}
