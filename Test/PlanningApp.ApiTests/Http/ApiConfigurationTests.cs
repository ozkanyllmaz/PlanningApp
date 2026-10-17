using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using PlanningApp.ApiTests.Support;
using PlanningApp.Infrastructure.Auth;
using static PlanningApp.ApiTests.Support.ApiTestHelpers;

namespace PlanningApp.ApiTests.Http;

// UseSetting ile verilen değerlerin uygulamaya gerçekten ulaştığını kanıtlar (bkz. api_test_rules.md)
[Collection(ApiCollection.Name)]
public class ApiConfigurationTests(ApiFixture fixture)
{
    [Fact]
    public async Task UseSetting_Values_ReachApplicationConfiguration()
    {
        // Arrange
        using var client = fixture.Default.CreateClient();
        var (_, registered) = await RegisterUserAsync(client);

        // Act
        var configuration = fixture.Default.Services.GetRequiredService<IConfiguration>();
        var jwtOptions = fixture.Default.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var token = new JsonWebToken(registered.AccessToken);

        // Assert: appsettings.json'daki değerler değil, UseSetting ile verilenler geçerlidir
        configuration["RateLimiting:Auth:PermitLimit"].Should().Be("100000");
        jwtOptions.Issuer.Should().Be(ApiFixture.Issuer);
        jwtOptions.Audience.Should().Be(ApiFixture.Audience);
        token.Issuer.Should().Be(ApiFixture.Issuer);
        token.Audiences.Should().ContainSingle().Which.Should().Be(ApiFixture.Audience);
    }

    [Fact]
    public async Task UseSetting_ConnectionString_ReachesPersistence()
    {
        // Arrange: kayıt başarılıysa uygulama test container'ındaki veritabanına bağlanmıştır
        using var client = fixture.Default.CreateClient();

        // Act
        var response = await PostRegisterAsync(client, NewEmail());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
