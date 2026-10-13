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

namespace PlanningApp.Application.Features.Tasks.Commands.CopyTaskToDate;

public sealed class CopyTaskToDateCommandHandler(
    IPlanningTaskRepository taskRepository,
    IStreakRepository streakRepository,
    IPlanningWeekRepository weekRepository,
    IUserProfileRepository userProfileRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CopyTaskToDateCommand, Guid>
{
    public async Task<Guid> Handle(CopyTaskToDateCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Idempotency: aynı NewId ile görev zaten varsa (silinmiş olsa da) yeni kayıt açmadan mevcut Id'yi dön.
        // Bu kontrol diğer tüm kontrollerden ÖNCE gelir; kaynak görev sonradan silinmiş olsa bile tekrar başarılı olur.
        var existing = await taskRepository.GetByIdIncludingDeletedAsync(userId, request.NewId, cancellationToken);
        if (existing is not null)
            return existing.Id;

        // Başka kullanıcının veya silinmiş kaynak da "bulunamadı" sayılır
        var source = await taskRepository.GetByIdAsync(userId, request.SourceTaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlanningTask), request.SourceTaskId);

        // Hedef gün, kullanıcının yerel "bugün"ünden önce olamaz
        var profile = await userProfileRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        var localToday = profile.GetLocalDate(timeProvider.GetUtcNow());
        if (request.TargetDate < localToday)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.TargetDate), "Hedef gün bugünden önce olamaz.")]);
        }

        // Gün kilidi: yalnızca hedef gün değerlendirilmemiş olmalı; kaynak görev değişmediği için kaynağın günü kontrol edilmez
        var streak = await streakRepository.GetByUserIdAsync(userId, cancellationToken);
        streak?.EnsureDayNotEvaluated(request.TargetDate);

        var copy = source.CopyToDate(request.NewId, request.TargetDate, request.SortOrder, request.CreatedAt);

        // Hedef günün haftası yoksa oluştur; hafta Id'sini handler üretir
        var weekStart = WeekCalendar.GetWeekStart(request.TargetDate);
        var week = await weekRepository.GetByStartDateAsync(userId, weekStart, cancellationToken);
        if (week is null)
        {
            week = PlanningWeek.Create(Guid.NewGuid(), userId, weekStart);
            await weekRepository.AddAsync(week, cancellationToken);
        }

        await taskRepository.AddAsync(copy, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return copy.Id;
    }
}
