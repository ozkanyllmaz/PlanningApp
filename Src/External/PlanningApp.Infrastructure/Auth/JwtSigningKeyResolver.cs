using Microsoft.IdentityModel.Tokens;

namespace PlanningApp.Infrastructure.Auth;

// İmzalama anahtarı çözümlemesinin TEK noktası: JwtTokenService (imzalama) ve WebApi'nin JWT doğrulaması
// aynı kod yolunu kullanır. Anahtar değeri hiçbir hata mesajına yazılmaz.
public static class JwtSigningKeyResolver
{
    public static SymmetricSecurityKey Resolve(JwtOptions options)
        => new(Convert.FromBase64String(options.SigningKey));
}
