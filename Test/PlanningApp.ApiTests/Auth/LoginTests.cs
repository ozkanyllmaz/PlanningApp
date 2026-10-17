using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlanningApp.ApiTests.Support;
using PlanningApp.Application.Features.Auth;
using static PlanningApp.ApiTests.Support.ApiTestHelpers;

namespace PlanningApp.ApiTests.Auth;

[Collection(ApiCollection.Name)]
public class LoginTests(ApiFixture fixture)
{
    private const string WrongPassword = "yanlis-parola-456";

    private readonly HttpClient _client = fixture.Default.CreateClient();

    // ---- Başarılı giriş ----

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        // Arrange
        var (email, registered) = await RegisterUserAsync(_client);

        // Act
        var response = await PostLoginAsync(_client, email);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResult>();
        auth!.UserId.Should().Be(registered.UserId);
        auth.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    // ---- Hesap varlığı sızdırılmaz ----

    [Fact]
    public async Task Login_WithWrongPasswordOrUnknownEmail_Returns401WithIdenticalBody()
    {
        // Arrange
        var (knownEmail, _) = await RegisterUserAsync(_client);

        // Act
        var wrongPassword = await PostLoginAsync(_client, knownEmail, WrongPassword);
        var unknownEmail = await PostLoginAsync(_client, NewEmail(), WrongPassword);

        // Assert
        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        wrongPassword.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
        unknownEmail.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
        wrongPassword.Headers.WwwAuthenticate.ToString().Should().StartWith("Bearer");

        var wrongPasswordBody = await ReadProblemWithoutTraceIdAsync(wrongPassword);
        var unknownEmailBody = await ReadProblemWithoutTraceIdAsync(unknownEmail);
        wrongPasswordBody.Should().Be(unknownEmailBody);
        wrongPasswordBody.Should().NotContain(knownEmail).And.NotContain(WrongPassword);
    }

    // ---- Kilitleme ----

    [Fact]
    public async Task Login_AfterFiveWrongAttempts_Returns401EvenWithCorrectPassword()
    {
        // Arrange
        var (email, _) = await RegisterUserAsync(_client);
        for (var attempt = 0; attempt < 5; attempt++)
            (await PostLoginAsync(_client, email, WrongPassword)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act
        var response = await PostLoginAsync(_client, email);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- Doğrulama ----

    [Fact]
    public async Task Login_WithEmptyBodyObject_Returns400()
    {
        // Act
        var response = await _client.PostAsync("/api/v1/auth/login", JsonContent.Create(new { }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
    }
}
