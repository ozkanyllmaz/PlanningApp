using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PlanningApp.ApiTests.Support;
using PlanningApp.Application.Features.Auth;
using static PlanningApp.ApiTests.Support.ApiTestHelpers;

namespace PlanningApp.ApiTests.Auth;

[Collection(ApiCollection.Name)]
public class RegisterTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Default.CreateClient();

    // ---- Başarılı kayıt ----

    [Fact]
    public async Task Register_WithValidRequest_Returns201WithTokens()
    {
        // Arrange
        var email = NewEmail();

        // Act
        var response = await PostRegisterAsync(_client, email);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResult>();
        auth!.UserId.Should().NotBeEmpty();
        auth.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.RefreshToken.Should().NotBeNullOrWhiteSpace();
        auth.AccessTokenExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        auth.RefreshTokenExpiresAt.Should().BeAfter(auth.AccessTokenExpiresAt);
    }

    // ---- E-posta çakışması ----

    [Fact]
    public async Task Register_WithSameEmailInDifferentCase_Returns400WithEmailError()
    {
        // Arrange
        var email = NewEmail();
        (await PostRegisterAsync(_client, email)).StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await PostRegisterAsync(_client, email.ToUpperInvariant());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
        var problem = await ReadProblemAsync(response);
        problem.GetProperty("errors").TryGetProperty("Email", out _).Should().BeTrue();
    }

    // ---- Eksik / geçersiz alanlar ----

    [Fact]
    public async Task Register_WithEmptyBodyObject_Returns400WithFieldErrors()
    {
        // Arrange
        var content = JsonContent.Create(new { });

        // Act
        var response = await _client.PostAsync("/api/v1/auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
        var errors = (await ReadProblemAsync(response)).GetProperty("errors");
        errors.TryGetProperty("Email", out _).Should().BeTrue();
        errors.TryGetProperty("Password", out _).Should().BeTrue();
        errors.TryGetProperty("TimeZoneId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithInvalidTimeZone_Returns400WithTimeZoneError()
    {
        // Arrange
        var body = new { email = NewEmail(), password = ValidPassword, timeZoneId = "Mars/Olympus_Mons" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
        var errors = (await ReadProblemAsync(response)).GetProperty("errors");
        errors.TryGetProperty("TimeZoneId", out _).Should().BeTrue();
        errors.EnumerateObject().Select(property => property.Name).Should().NotContain(["Email", "Password"]);
    }
}
