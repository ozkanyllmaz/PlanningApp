using Microsoft.IdentityModel.JsonWebTokens;
using PlanningApp.Application.Common.Interfaces;

namespace PlanningApp.WebApi.Authentication;

// JWT'deki "sub" claim'inden kullanıcı kimliğini okur (MapInboundClaims kapalıdır, claim adı değişmez).
// Handler'lara yalnızca yetkili istek ulaşır; kimliksiz ya da geçersiz durum bir programlama hatasıdır.
public sealed class HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (!Guid.TryParse(subject, out var userId))
                throw new InvalidOperationException("Geçerli bir kullanıcı kimliği (sub claim'i) bulunamadı.");

            return userId;
        }
    }
}
