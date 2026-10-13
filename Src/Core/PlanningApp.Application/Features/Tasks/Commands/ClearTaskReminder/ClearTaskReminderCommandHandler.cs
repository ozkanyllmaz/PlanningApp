using System;
using MediatR;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Application.Features.Tasks.Commands.ClearTaskReminder;

public sealed class ClearTaskReminderCommandHandler(
    IPlanningTaskRepository taskRepository,
    IStreakRepository streakRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<ClearTaskReminderCommand>
{
    public async Task Handle(ClearTaskReminderCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Başka kullanıcının kaydı da repository'den null döner: "bulunamadı" sayılır
        var task = await taskRepository.GetByIdAsync(userId, request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlanningTask), request.TaskId);

        // Idempotency: hatırlatıcı zaten yoksa (offline kuyruk tekrarı) hata verme.
        // Bu kontrol gün kilidinden ÖNCE gelir; yoksa daha önce başarılı olmuş bir isteğin tekrarı reddedilirdi.
        if (task.ReminderTime is null)
            return;

        // Gün kilidi: görevin atandığı gün değerlendirilmişse değişiklik yapılamaz (streak kaydı yoksa kilit yok)
        if (task.AssignedDate is { } assignedDate)
        {
            var streak = await streakRepository.GetByUserIdAsync(userId, cancellationToken);
            streak?.EnsureDayNotEvaluated(assignedDate);
        }

        task.ClearReminder();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
