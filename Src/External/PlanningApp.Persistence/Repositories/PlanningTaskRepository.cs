using Microsoft.EntityFrameworkCore;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Persistence.Repositories;

// Repository'ler SaveChanges ÇAĞIRMAZ; kaydetme handler'daki IUnitOfWork çağrısıyla yapılır.
// Silinmişlik filtresi her sorguda açıkça yazılır (global query filter kullanılmaz).
public sealed class PlanningTaskRepository(PlanningDbContext context) : IPlanningTaskRepository
{
    // Tek kayıt getirenler takipli (tracked) döner: komutlar değiştirip kaydeder
    public Task<PlanningTask?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        => context.PlanningTasks
            .FirstOrDefaultAsync(
                task => task.UserId == userId && task.Id == id && !task.IsDeleted,
                cancellationToken);

    public Task<PlanningTask?> GetByIdIncludingDeletedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        => context.PlanningTasks
            .FirstOrDefaultAsync(
                task => task.UserId == userId && task.Id == id,
                cancellationToken);

    // Liste okumaları AsNoTracking kullanır; sıralama handler'da yapılır
    public async Task<IReadOnlyList<PlanningTask>> GetByDateAsync(
        Guid userId, DateOnly date, CancellationToken cancellationToken)
        => await context.PlanningTasks
            .AsNoTracking()
            .Where(task => task.UserId == userId && task.AssignedDate == date && !task.IsDeleted)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PlanningTask>> GetPoolAsync(Guid userId, CancellationToken cancellationToken)
        => await context.PlanningTasks
            .AsNoTracking()
            .Where(task => task.UserId == userId && task.AssignedDate == null && !task.IsDeleted)
            .ToListAsync(cancellationToken);

    // [from, to] aralığı, to dahildir
    public async Task<IReadOnlyList<PlanningTask>> GetByDateRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
        => await context.PlanningTasks
            .AsNoTracking()
            .Where(task => task.UserId == userId
                && task.AssignedDate >= from
                && task.AssignedDate <= to
                && !task.IsDeleted)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PlanningTask task, CancellationToken cancellationToken)
        => await context.PlanningTasks.AddAsync(task, cancellationToken);
}
