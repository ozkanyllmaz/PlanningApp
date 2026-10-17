using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace PlanningApp.ApiTests.Support;

// "me" ucunun reddetmesi gereken token'ları üretir. Anahtarlar her çağrıda rastgele üretilir.
internal static class TestTokens
{
    private static readonly JsonWebTokenHandler Handler = new();

    public static string Create(
        Guid userId,
        string email,
        string signingKey,
        string issuer = ApiFixture.Issuer,
        string audience = ApiFixture.Audience,
        DateTime? expires = null)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            Issuer = issuer,
            Audience = audience,
            IssuedAt = now.AddHours(-2),
            NotBefore = now.AddHours(-2),
            Expires = expires ?? now.AddMinutes(15),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return Handler.CreateToken(descriptor);
    }

    // İmzasız (alg = none) token: imza bölümü boştur
    public static string CreateUnsigned(Guid userId, string email)
    {
        var header = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));
        var exp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
        var payload = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(
            $$"""{"sub":"{{userId}}","email":"{{email}}","iss":"{{ApiFixture.Issuer}}","aud":"{{ApiFixture.Audience}}","exp":{{exp}}}"""));

        return $"{header}.{payload}.";
    }
}
