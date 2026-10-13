using System;
using PlanningApp.Domain.Weeks;

namespace PlanningApp.Application.Common.Interfaces.Repositories;

public interface IPlanningWeekRepository
{
    // Sorgu her zaman UserId ile filtrelenir; haftanın başlangıcı (Pazartesi) ile aranır
    Task<PlanningWeek?> GetByStartDateAsync(Guid userId, DateOnly weekStartDate, CancellationToken cancellationToken);

    Task AddAsync(PlanningWeek week, CancellationToken cancellationToken);
}
