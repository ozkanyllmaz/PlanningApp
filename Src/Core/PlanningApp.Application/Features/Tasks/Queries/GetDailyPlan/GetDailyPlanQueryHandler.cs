using System;
using System.Linq;
using MediatR;
using PlanningApp.Application.Common.Helpers;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Features.Tasks.Dtos;

namespace PlanningApp.Application.Features.Tasks.Queries.GetDailyPlan;

public sealed class GetDailyPlanQueryHandler(
    IPlanningTaskRepository taskRepository,
    IStreakRepository streakRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetDailyPlanQuery, DailyPlanDto>
{
    public async Task<DailyPlanDto> Handle(GetDailyPlanQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var tasks = await taskRepository.GetByDateAsync(userId, request.Date, cancellationToken);

        // Sıralama repository'de değil burada yapılır: SortOrder, CreatedAt, Id (artan).
        // Tamamlananları alta almak istemcinin işidir, burada sıra bozulmaz.
        var orderedTasks = tasks
            .OrderBy(task => task.SortOrder)
            .ThenBy(task => task.CreatedAt)
            .ThenBy(task => task.Id)
            .ToList();

        // Streak kaydı yoksa gün kilitli değildir
        var streak = await streakRepository.GetByUserIdAsync(userId, cancellationToken);
        var isLocked = streak?.IsDayEvaluated(request.Date) ?? false;

        var percentage = WeightedCompletion.Calculate(orderedTasks);

        return new DailyPlanDto(
            request.Date,
            isLocked,
            percentage,
            orderedTasks.Select(TaskDto.From).ToList());
    }
}
