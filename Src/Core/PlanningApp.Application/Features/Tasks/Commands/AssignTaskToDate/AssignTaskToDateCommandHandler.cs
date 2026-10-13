using System;
using FluentValidation.Results;
using MediatR;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Helpers;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Tasks;
using PlanningApp.Domain.Users;
using PlanningApp.Domain.Weeks;

namespace PlanningApp.Application.Features.Tasks.Commands.AssignTaskToDate;

public sealed class AssignTaskToDateCommandHandler(
    IPlanningTaskRepository taskRepository,
    IStreakRepository streakRepository,
    IPlanningWeekRepository weekRepository,
    IUserProfileRepository userProfileRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<AssignTaskToDateCommand>
{
    public async Task Handle(AssignTaskToDateCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Başka kullanıcının kaydı da repository'den null döner: "bulunamadı" sayılır
        var task = await taskRepository.GetByIdAsync(userId, request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlanningTask), request.TaskId);

        // Idempotency: görev zaten bu güne bu SortOrder ile atanmışsa (offline kuyruk tekrarı) hata verme.
        // Bu kontrol diğer tüm kontrollerden ÖNCE gelir; yoksa başarılı olmuş bir isteğin tekrarı reddedilebilirdi.
        if (task.AssignedDate == request.Date && task.SortOrder == request.SortOrder)
            return;

        // Hedef gün, kullanıcının yerel "bugün"ünden önce olamaz
        var profile = await userProfileRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        var localToday = profile.GetLocalDate(timeProvider.GetUtcNow());
        if (request.Date < localToday)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.Date), "Hedef gün bugünden önce olamaz.")]);
        }

        // Gün kilidi: hem görevin mevcut günü hem de hedef gün değerlendirilmemiş olmalı (streak kaydı yoksa kilit yok)
        var streak = await streakRepository.GetByUserIdAsync(userId, cancellationToken);
        if (task.AssignedDate is { } currentDate)
            streak?.EnsureDayNotEvaluated(currentDate);

        streak?.EnsureDayNotEvaluated(request.Date);

        task.AssignToDate(request.Date, request.SortOrder);

        // Hedef günün haftası yoksa oluştur; hafta Id'sini handler üretir
        var weekStart = WeekCalendar.GetWeekStart(request.Date);
        var week = await weekRepository.GetByStartDateAsync(userId, weekStart, cancellationToken);
        if (week is null)
        {
            week = PlanningWeek.Create(Guid.NewGuid(), userId, weekStart);
            await weekRepository.AddAsync(week, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
