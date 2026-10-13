using System;
using MediatR;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Application.Features.Tasks.Commands.SetTaskReminder;

public sealed class SetTaskReminderCommandHandler(
    IPlanningTaskRepository taskRepository,
    IStreakRepository streakRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<SetTaskReminderCommand>
{
    public async Task Handle(SetTaskReminderCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Başka kullanıcının kaydı da repository'den null döner: "bulunamadı" sayılır
        var task = await taskRepository.GetByIdAsync(userId, request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlanningTask), request.TaskId);

        // Idempotency: hatırlatma saati zaten aynıysa (offline kuyruk tekrarı) hata verme.
        // Bu kontrol gün kilidinden ÖNCE gelir; yoksa daha önce başarılı olmuş bir isteğin tekrarı reddedilirdi.
        if (task.ReminderTime == request.ReminderTime)
            return;

        // Gün kilidi: görevin atandığı gün değerlendirilmişse değişiklik yapılamaz (streak kaydı yoksa kilit yok)
        if (task.AssignedDate is { } assignedDate)
        {
            var streak = await streakRepository.GetByUserIdAsync(userId, cancellationToken);
            streak?.EnsureDayNotEvaluated(assignedDate);
        }

        task.SetReminder(request.ReminderTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
