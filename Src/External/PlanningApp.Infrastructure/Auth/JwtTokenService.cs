using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Models;

namespace PlanningApp.Infrastructure.Auth;

// Veritabanına dokunmaz. Zaman, çağıranın verdiği issuedAt parametresiyle gelir (TimeProvider Application'dadır).
public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private const int RefreshTokenByteLength = 32;

    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AccessTokenResult CreateAccessToken(Guid userId, string email, DateTimeOffset issuedAt)
    {
        var expiresAt = issuedAt.AddMinutes(_options.AccessTokenMinutes);

        // Claim'ler: sub, email, jti (iat ve exp tanımlayıcıdan gelir); rol veya yetki claim'i YOK
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(_options.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new AccessTokenResult(_tokenHandler.CreateToken(descriptor), expiresAt);
    }

    public RefreshTokenMaterial CreateRefreshToken(DateTimeOffset issuedAt)
    {
        // 32 bayt kriptografik rastgele, base64url
        var plainToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RefreshTokenByteLength));

        return new RefreshTokenMaterial(
            plainToken,
            HashRefreshToken(plainToken),
            issuedAt.AddDays(_options.RefreshTokenDays));
    }

    // SHA-256 özeti, küçük harfli hex
    public string HashRefreshToken(string plainToken)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken)));
}
