using System;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Application.Common.Interfaces.Repositories;

public interface IPlanningTaskRepository
{
    // Sorgu her zaman UserId ile filtrelenir; başka kullanıcının kaydı "bulunamadı" sayılır
    Task<PlanningTask?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    // Silinmiş kayıtları da döner; SADECE idempotency kontrolleri ve DeleteTask kullanır.
    // Sorgu yine UserId ile filtrelenir.
    Task<PlanningTask?> GetByIdIncludingDeletedAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    // O güne atanmış, silinmemiş görevler; sıralamasızdır (sıralama handler'da yapılır)
    Task<IReadOnlyList<PlanningTask>> GetByDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken);

    Task AddAsync(PlanningTask task, CancellationToken cancellationToken);

}
