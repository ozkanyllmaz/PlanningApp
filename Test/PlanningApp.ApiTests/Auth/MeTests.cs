using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlanningApp.ApiTests.Support;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Features.Auth.Queries.GetCurrentUser;
using Microsoft.Extensions.DependencyInjection;
using static PlanningApp.ApiTests.Support.ApiTestHelpers;

namespace PlanningApp.ApiTests.Auth;

[Collection(ApiCollection.Name)]
public class MeTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Default.CreateClient();

    // ---- Kimlik doğrulama gerekir ----

    [Fact]
    public async Task Me_WithoutToken_Returns401WithProblemJson()
    {
        // Act
        var response = await GetMeAsync(_client, accessToken: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
        response.Headers.WwwAuthenticate.ToString().Should().StartWith("Bearer");
    }

    [Fact]
    public async Task Me_WithValidToken_Returns200WithCurrentUser()
    {
        // Arrange
        var (email, registered) = await RegisterUserAsync(_client);

        // Act
        var response = await GetMeAsync(_client, registered.AccessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        user.Should().Be(new CurrentUserDto(registered.UserId, email));
    }

    // ---- Reddedilmesi gereken token'lar ----

    [Theory]
    [InlineData("wrong-key")]
    [InlineData("expired")]
    [InlineData("wrong-audience")]
    [InlineData("wrong-issuer")]
    [InlineData("unsigned")]
    public async Task Me_WithInvalidToken_Returns401(string scenario)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = NewEmail();
        var token = scenario switch
        {
            "wrong-key" => TestTokens.Create(userId, email, ApiFixture.NewSigningKey()),
            "expired" => TestTokens.Create(userId, email, fixture.SigningKey, expires: DateTime.UtcNow.AddHours(-1)),
            "wrong-audience" => TestTokens.Create(userId, email, fixture.SigningKey, audience: "baska-audience"),
            "wrong-issuer" => TestTokens.Create(userId, email, fixture.SigningKey, issuer: "baska-issuer"),
            "unsigned" => TestTokens.CreateUnsigned(userId, email),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        // Act
        var response = await GetMeAsync(_client, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
    }

    // ---- Kullanıcısı silinmiş geçerli token ----

    [Fact]
    public async Task Me_WithValidTokenOfDeletedUser_Returns401()
    {
        // Arrange
        var (_, registered) = await RegisterUserAsync(_client);
        await using (var scope = fixture.Default.Services.CreateAsyncScope())
        {
            var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            await identityService.DeleteUserAsync(registered.UserId, CancellationToken.None);
        }

        // Act
        var response = await GetMeAsync(_client, registered.AccessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJsonContentType);
    }
}
