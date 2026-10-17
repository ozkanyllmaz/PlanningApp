using System.Net;
using System.Text;
using FluentAssertions;
using PlanningApp.ApiTests.Support;
using static PlanningApp.ApiTests.Support.ApiTestHelpers;

namespace PlanningApp.ApiTests.Http;

[Collection(ApiCollection.Name)]
public class ErrorResponseTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Default.CreateClient();

    // ---- Bozuk istek / bilinmeyen rota ----

    [Fact]
    public async Task Register_WithMalformedJson_Returns400WithProblemJson()
    {
        // Arrange
        var content = new StringContent("{ \"email\": ", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
    }

    [Fact]
    public async Task UnknownRoute_WhenAuthenticated_Returns404WithProblemJson()
    {
        // Arrange
        var (_, registered) = await RegisterUserAsync(_client);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bilinmeyen-rota");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registered.AccessToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
    }

    // Fallback policy eşleşen ucu olmayan isteklere de uygulanır: anonim istek rota varlığını öğrenemez
    [Fact]
    public async Task UnknownRoute_WhenAnonymous_Returns401WithProblemJson()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/bilinmeyen-rota");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
    }

    // ---- Parola ve hash yanıtlarda yer almaz ----

    [Fact]
    public async Task AuthResponses_NeverContainPasswordOrHash()
    {
        // Arrange
        var email = NewEmail();
        var responses = new List<HttpResponseMessage>
        {
            await PostRegisterAsync(_client, email),
            await PostRegisterAsync(_client, email),
            await PostLoginAsync(_client, email),
            await PostLoginAsync(_client, email, "yanlis-parola-456")
        };
        var loginBody = await responses[2].Content.ReadAsStringAsync();
        var refreshToken = System.Text.Json.JsonDocument.Parse(loginBody).RootElement.GetProperty("refreshToken").GetString()!;
        responses.Add(await PostRefreshAsync(_client, refreshToken));
        responses.Add(await GetMeAsync(_client, accessToken: null));

        // Act
        var bodies = new List<string>();
        foreach (var response in responses)
            bodies.Add(await response.Content.ReadAsStringAsync());

        // Assert
        foreach (var body in bodies)
        {
            body.Should().NotContain(ValidPassword);
            body.Should().NotContain("yanlis-parola-456");
            body.Should().NotContainEquivalentOf("passwordHash");
            body.Should().NotContainEquivalentOf("tokenHash");
            body.Should().NotContainEquivalentOf("securityStamp");
        }
    }
}
