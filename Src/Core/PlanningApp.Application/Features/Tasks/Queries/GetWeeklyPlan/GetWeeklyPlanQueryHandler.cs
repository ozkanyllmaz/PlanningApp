using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using PlanningApp.Application.Common.Helpers;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Features.Tasks.Dtos;
using PlanningApp.Application.Features.Tasks.Queries.GetDailyPlan;

namespace PlanningApp.Application.Features.Tasks.Queries.GetWeeklyPlan;

public sealed class GetWeeklyPlanQueryHandler(
    IPlanningTaskRepository taskRepository,
    IStreakRepository streakRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetWeeklyPlanQuery, IReadOnlyList<DailyPlanDto>>
{
    private const int DaysInWeek = 7;

    public async Task<IReadOnlyList<DailyPlanDto>> Handle(GetWeeklyPlanQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var weekStart = WeekCalendar.GetWeekStart(request.Date);
        var weekEnd = weekStart.AddDays(DaysInWeek - 1);

        // Haftanın tüm görevleri tek sorguda çekilir, güne gruplama burada yapılır
        var tasks = await taskRepository.GetByDateRangeAsync(userId, weekStart, weekEnd, cancellationToken);
        var tasksByDate = tasks
            .Where(task => task.AssignedDate is not null)
            .ToLookup(task => task.AssignedDate!.Value);

        // Streak kaydına hafta için yalnızca bir kez gidilir; streak yoksa hiçbir gün kilitli değildir
        var streak = await streakRepository.GetByUserIdAsync(userId, cancellationToken);

        var days = new List<DailyPlanDto>(DaysInWeek);
        for (var offset = 0; offset < DaysInWeek; offset++)
        {
            var date = weekStart.AddDays(offset);

            // Sıralama: SortOrder, CreatedAt, Id (artan); tamamlananlar alta alınmaz
            var dayTasks = tasksByDate[date]
                .OrderBy(task => task.SortOrder)
                .ThenBy(task => task.CreatedAt)
                .ThenBy(task => task.Id)
                .ToList();

            days.Add(new DailyPlanDto(
                date,
                streak?.IsDayEvaluated(date) ?? false,
                WeightedCompletion.Calculate(dayTasks),
                dayTasks.Select(TaskDto.From).ToList()));
        }

        return days;
    }
}
