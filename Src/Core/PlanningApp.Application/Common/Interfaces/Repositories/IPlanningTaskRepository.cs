using System;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Application.Common.Interfaces.Repositories;

public interface IPlanningTaskRepository
{
    // Sorgu her zaman UserId ile filtrelenir; başka kullanıcının kaydı "bulunamadı" sayılır
    Task<PlanningTask?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    Task AddAsync(PlanningTask task, CancellationToken cancellationToken);

}
