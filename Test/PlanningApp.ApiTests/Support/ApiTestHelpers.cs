using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PlanningApp.Application.Features.Auth;

namespace PlanningApp.ApiTests.Support;

// Testler arası ortak yardımcılar. Parola değeri yer tutucudur, gerçek bir gizli değer DEĞİLDİR.
internal static class ApiTestHelpers
{
    public const string ValidPassword = "ornek-parola-123";
    public const string TimeZoneId = "Europe/Istanbul";
    public const string ProblemJsonContentType = "application/problem+json";

    // Her test kendi benzersiz e-postasıyla çalışır
    public static string NewEmail()
        => $"user-{Guid.NewGuid():N}@example.com";

    public static Task<HttpResponseMessage> PostRegisterAsync(HttpClient client, string email, string password = ValidPassword)
        => client.PostAsJsonAsync("/api/v1/auth/register", new { email, password, timeZoneId = TimeZoneId });

    public static Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string email, string password = ValidPassword)
        => client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    public static Task<HttpResponseMessage> PostRefreshAsync(HttpClient client, string refreshToken)
        => client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });

    public static Task<HttpResponseMessage> PostLogoutAsync(HttpClient client, string refreshToken)
        => client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken });

    public static Task<HttpResponseMessage> GetMeAsync(HttpClient client, string? accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        if (accessToken is not null)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        return client.SendAsync(request);
    }

    // Yeni kullanıcı kaydeder ve dönen oturumu verir
    public static async Task<(string Email, AuthResult Auth)> RegisterUserAsync(HttpClient client)
    {
        var email = NewEmail();
        var response = await PostRegisterAsync(client, email);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var auth = await response.Content.ReadFromJsonAsync<AuthResult>();
        return (email, auth!);
    }

    // Problem yanıtını JSON olarak okur; "traceId" isteğe özgü olduğu için karşılaştırmalardan çıkarılır
    public static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    public static async Task<string> ReadProblemWithoutTraceIdAsync(HttpResponseMessage response)
    {
        var problem = await ReadProblemAsync(response);
        var properties = problem.EnumerateObject()
            .Where(property => property.Name != "traceId")
            .Select(property => $"{property.Name}={property.Value.GetRawText()}");

        return string.Join(";", properties);
    }
}
