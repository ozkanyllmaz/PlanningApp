using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Infrastructure;
using PlanningApp.Infrastructure.Auth;
using PlanningApp.IntegrationTests.Support;

namespace PlanningApp.IntegrationTests.Infrastructure.Auth;

// Veritabanı gerektirmez; JwtTokenService davranışları burada test edilir
public class JwtTokenServiceTests
{
    private const string Issuer = "planning-app-tests";
    private const string Audience = "planning-app-tests-clients";

    // Saniye altı kısım yok: iat/exp claim'leri saniye çözünürlüğündedir
    private static readonly DateTimeOffset IssuedAt = new(2026, 1, 7, 10, 0, 0, TimeSpan.Zero);

    // ---- Access token ----

    [Fact]
    public async Task CreateAccessToken_ValidatesWithSameKey()
    {
        // Arrange
        var key = TestServices.NewSigningKey();
        var service = CreateService(key);

        // Act
        var result = service.CreateAccessToken(Guid.NewGuid(), "user@example.com", IssuedAt);
        var validation = await ValidateAsync(result.Token, key);

        // Assert
        validation.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAccessToken_ContainsSubAndEmailClaims()
    {
        // Arrange
        var key = TestServices.NewSigningKey();
        var service = CreateService(key);
        var userId = Guid.NewGuid();

        // Act
        var result = service.CreateAccessToken(userId, "user@example.com", IssuedAt);
        var validation = await ValidateAsync(result.Token, key);

        // Assert
        var claims = validation.ClaimsIdentity.Claims.ToList();
        claims.Should().Contain(claim => claim.Type == "sub" && claim.Value == userId.ToString());
        claims.Should().Contain(claim => claim.Type == "email" && claim.Value == "user@example.com");
        claims.Should().Contain(claim => claim.Type == "jti");
    }

    [Fact]
    public void CreateAccessToken_ExpiresFifteenMinutesAfterIssuedAt()
    {
        // Arrange
        var service = CreateService(TestServices.NewSigningKey());

        // Act
        var result = service.CreateAccessToken(Guid.NewGuid(), "user@example.com", IssuedAt);

        // Assert
        result.ExpiresAt.Should().Be(IssuedAt.AddMinutes(15));
        var parsed = new JsonWebToken(result.Token);
        new DateTimeOffset(parsed.ValidTo, TimeSpan.Zero).Should().Be(IssuedAt.AddMinutes(15));
        new DateTimeOffset(parsed.IssuedAt, TimeSpan.Zero).Should().Be(IssuedAt);
    }

    [Fact]
    public async Task CreateAccessToken_WhenSignatureIsTampered_IsInvalid()
    {
        // Arrange
        var key = TestServices.NewSigningKey();
        var service = CreateService(key);
        var token = service.CreateAccessToken(Guid.NewGuid(), "user@example.com", IssuedAt).Token;

        // İmza bölümünün ilk karakteri değiştirilir (6 anlamlı bit içerir)
        var parts = token.Split('.');
        parts[2] = (parts[2][0] == 'A' ? 'B' : 'A') + parts[2][1..];
        var tampered = string.Join('.', parts);

        // Act
        var validation = await ValidateAsync(tampered, key);

        // Assert
        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAccessToken_ValidatedWithDifferentKey_IsInvalid()
    {
        // Arrange
        var service = CreateService(TestServices.NewSigningKey());
        var token = service.CreateAccessToken(Guid.NewGuid(), "user@example.com", IssuedAt).Token;

        // Act
        var validation = await ValidateAsync(token, TestServices.NewSigningKey());

        // Assert
        validation.IsValid.Should().BeFalse();
    }

    // ---- Refresh token ----

    [Fact]
    public void CreateRefreshToken_Produces32ByteBase64UrlToken()
    {
        // Arrange
        var service = CreateService(TestServices.NewSigningKey());

        // Act
        var material = service.CreateRefreshToken(IssuedAt);

        // Assert
        Base64Url.DecodeFromChars(material.PlainToken).Should().HaveCount(32);
        material.PlainToken.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void CreateRefreshToken_HashEqualsSha256OfPlainToken()
    {
        // Arrange
        var service = CreateService(TestServices.NewSigningKey());

        // Act
        var material = service.CreateRefreshToken(IssuedAt);

        // Assert
        var expected = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material.PlainToken)));
        material.TokenHash.Should().Be(expected);
        material.TokenHash.Should().NotBe(material.PlainToken);
    }

    [Fact]
    public void HashRefreshToken_IsDeterministicAndMatchesCreatedHash()
    {
        // Arrange
        var service = CreateService(TestServices.NewSigningKey());
        var material = service.CreateRefreshToken(IssuedAt);

        // Act
        var first = service.HashRefreshToken(material.PlainToken);
        var second = service.HashRefreshToken(material.PlainToken);

        // Assert
        first.Should().Be(second);
        first.Should().Be(material.TokenHash);
    }

    [Fact]
    public void CreateRefreshToken_CalledTwice_ProducesDifferentTokens()
    {
        // Arrange
        var service = CreateService(TestServices.NewSigningKey());

        // Act
        var first = service.CreateRefreshToken(IssuedAt);
        var second = service.CreateRefreshToken(IssuedAt);

        // Assert
        first.PlainToken.Should().NotBe(second.PlainToken);
        first.TokenHash.Should().NotBe(second.TokenHash);
    }

    [Fact]
    public void CreateRefreshToken_ExpiresSixtyDaysAfterIssuedAt()
    {
        // Arrange
        var service = CreateService(TestServices.NewSigningKey());

        // Act
        var material = service.CreateRefreshToken(IssuedAt);

        // Assert
        material.ExpiresAt.Should().Be(IssuedAt.AddDays(60));
    }

    // ---- Seçenek doğrulaması (ValidateOnStart) ----

    [Fact]
    public void AddInfrastructure_WithKeyShorterThan32Bytes_FailsOptionsValidation()
    {
        // Arrange
        var configuration = TestServices.BuildConfiguration(connectionString: null, TestServices.NewSigningKey(16));
        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IOptions<JwtOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void AddInfrastructure_WithNonBase64Key_FailsOptionsValidation()
    {
        // Arrange
        var configuration = TestServices.BuildConfiguration(connectionString: null, "bu-base64-degil-!!!");
        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IOptions<JwtOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void AddInfrastructure_WithValidKey_ResolvesTokenService()
    {
        // Arrange
        var configuration = TestServices.BuildConfiguration(connectionString: null, TestServices.NewSigningKey());
        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        // Act
        var tokenService = provider.GetRequiredService<ITokenService>();

        // Assert
        tokenService.Should().BeOfType<JwtTokenService>();
    }

    // ---- Test Helpers ----

    private static JwtTokenService CreateService(string signingKey)
        => new(Options.Create(new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = signingKey
        }));

    // Süre doğrulaması kapalıdır: sabit geçmiş bir an kullanılıyor, exp ayrıca ayrıştırılarak denetlenir
    private static Task<TokenValidationResult> ValidateAsync(string token, string signingKey)
        => new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
            ValidateLifetime = false
        });
}
