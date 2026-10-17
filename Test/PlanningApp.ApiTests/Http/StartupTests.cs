using FluentAssertions;
using Microsoft.Extensions.Options;
using PlanningApp.ApiTests.Support;

namespace PlanningApp.ApiTests.Http;

[Collection(ApiCollection.Name)]
public class StartupTests(ApiFixture fixture)
{
    [Fact]
    public void Startup_WithoutSigningKey_Fails()
    {
        // Arrange
        using var factory = fixture.CreateFactory(includeSigningKey: false);

        // Act
        var act = () => factory.CreateClient();

        // Assert: varsayılan anahtar YOKTUR, uygulama başlamaz
        act.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("Jwt:SigningKey");
    }

    [Fact]
    public void Startup_WithTooShortSigningKey_FailsWithoutLeakingTheKey()
    {
        // Arrange: 16 bayt, 32 baytlık alt sınırın altındadır
        var shortKey = ApiFixture.NewSigningKey(byteLength: 16);
        using var factory = fixture.CreateFactory(signingKey: shortKey);

        // Act
        var act = () => factory.CreateClient();

        // Assert
        act.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("Jwt:SigningKey").And.NotContain(shortKey);
    }
}
