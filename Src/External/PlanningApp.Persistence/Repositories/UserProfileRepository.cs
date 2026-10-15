using Microsoft.EntityFrameworkCore;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Users;

namespace PlanningApp.Persistence.Repositories;

public sealed class UserProfileRepository(PlanningDbContext context) : IUserProfileRepository
{
    // UserProfile.Id kimlik kullanıcı Id'sidir; başka kullanıcının Id'si ile kayıt bulunmaz.
    // Takipli (tracked) döner.
    public Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
        => context.UserProfiles
            .FirstOrDefaultAsync(profile => profile.Id == userId, cancellationToken);

    // Kayıt (Register) sırasında yeni profili ekler; SaveChanges handler'daki IUnitOfWork çağrısıyla yapılır
    public async Task AddAsync(UserProfile profile, CancellationToken cancellationToken)
        => await context.UserProfiles.AddAsync(profile, cancellationToken);
}
