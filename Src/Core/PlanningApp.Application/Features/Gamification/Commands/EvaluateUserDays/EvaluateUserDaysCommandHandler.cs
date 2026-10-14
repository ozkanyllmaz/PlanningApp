using System;
using System.Linq;
using MediatR;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Helpers;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Gamification;
using PlanningApp.Domain.Users;

namespace PlanningApp.Application.Features.Gamification.Commands.EvaluateUserDays;

public sealed class EvaluateUserDaysCommandHandler(
    IUserProfileRepository userProfileRepository,
    IStreakRepository streakRepository,
    IPlanningTaskRepository taskRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<EvaluateUserDaysCommand, EvaluateUserDaysResult>
{
    public async Task<EvaluateUserDaysResult> Handle(EvaluateUserDaysCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        var profile = await userProfileRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        // Değerlendirilebilir son gün: (şimdi - bekleme süresi) anının yerel günü - 1
        var lastEvaluableDate = profile
            .GetLocalDate(timeProvider.GetUtcNow() - DayEvaluationRules.GracePeriod)
            .AddDays(-1);

        var streak = await streakRepository.GetByUserIdAsync(userId, cancellationToken);

        // Kayıt veya hiç değerlendirme yoksa başlangıç, kayıt gününün yerel tarihidir
        var startDate = streak?.LastEvaluatedDate is { } lastEvaluatedDate
            ? lastEvaluatedDate.AddDays(1)
            : profile.GetLocalDate(profile.CreatedAt);

        // Değerlendirilecek gün yoksa görev sorgusuna ve yazıma gidilmez (idempotent)
        if (startDate > lastEvaluableDate)
            return new EvaluateUserDaysResult(0, streak?.CurrentStreak ?? 0, streak?.LongestStreak ?? 0);

        // Tüm günlerin görevleri tek aralık sorgusuyla çekilir, güne göre gruplanır
        var tasks = await taskRepository.GetByDateRangeAsync(userId, startDate, lastEvaluableDate, cancellationToken);
        var tasksByDate = tasks
            .Where(task => task.AssignedDate is not null)
            .ToLookup(task => task.AssignedDate!.Value);

        if (streak is null)
        {
            streak = Streak.Create(Guid.NewGuid(), userId);
            await streakRepository.AddAsync(streak, cancellationToken);
        }

        // Gün atlanmaz: sırayla gezilir; görevi olan gün EvaluateDay, boş gün EvaluateEmptyDay
        var evaluatedDayCount = 0;
        for (var date = startDate; date <= lastEvaluableDate; date = date.AddDays(1))
        {
            var dayTasks = tasksByDate[date].ToList();
            if (dayTasks.Count > 0)
                streak.EvaluateDay(WeightedCompletion.Calculate(dayTasks)!.Value, date);
            else
                streak.EvaluateEmptyDay(date);

            evaluatedDayCount++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new EvaluateUserDaysResult(evaluatedDayCount, streak.CurrentStreak, streak.LongestStreak);
    }
}
