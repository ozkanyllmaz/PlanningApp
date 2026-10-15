using Microsoft.AspNetCore.Identity;

namespace PlanningApp.Persistence.Identity;

// Identity kullanıcısı SADECE Persistence'ta yaşar; Domain ve Application'a sızmaz.
// Id, UserProfile.Id ile aynıdır.
public sealed class ApplicationUser : IdentityUser<Guid>
{
}
