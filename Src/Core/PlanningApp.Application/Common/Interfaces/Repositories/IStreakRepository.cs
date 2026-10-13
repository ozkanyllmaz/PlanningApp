using System;
using PlanningApp.Domain.Gamification;

namespace PlanningApp.Application.Common.Interfaces.Repositories;

public interface IStreakRepository
{
    // Kullanıcının tek bir Streak kaydı olur; henüz yoksa null döner
    Task<Streak?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
