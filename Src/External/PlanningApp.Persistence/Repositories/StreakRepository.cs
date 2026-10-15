using Microsoft.EntityFrameworkCore;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Gamification;

namespace PlanningApp.Persistence.Repositories;

// Repository'ler SaveChanges ÇAĞIRMAZ; kaydetme handler'daki IUnitOfWork çağrısıyla yapılır
public sealed class StreakRepository(PlanningDbContext context) : IStreakRepository
{
    // Takipli (tracked) döner; kullanıcının tek Streak kaydı olur
    public Task<Streak?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        => context.Streaks
            .FirstOrDefaultAsync(streak => streak.UserId == userId, cancellationToken);

    public async Task AddAsync(Streak streak, CancellationToken cancellationToken)
        => await context.Streaks.AddAsync(streak, cancellationToken);
}
