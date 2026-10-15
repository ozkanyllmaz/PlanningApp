using Microsoft.EntityFrameworkCore;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Weeks;

namespace PlanningApp.Persistence.Repositories;

// Repository'ler SaveChanges ÇAĞIRMAZ; kaydetme handler'daki IUnitOfWork çağrısıyla yapılır
public sealed class PlanningWeekRepository(PlanningDbContext context) : IPlanningWeekRepository
{
    // Tek kayıt getirenler takipli (tracked) döner: komutlar değiştirip kaydeder
    public Task<PlanningWeek?> GetByStartDateAsync(
        Guid userId, DateOnly weekStartDate, CancellationToken cancellationToken)
        => context.PlanningWeeks
            .FirstOrDefaultAsync(
                week => week.UserId == userId && week.WeekStartDate == weekStartDate,
                cancellationToken);

    public async Task AddAsync(PlanningWeek week, CancellationToken cancellationToken)
        => await context.PlanningWeeks.AddAsync(week, cancellationToken);
}
