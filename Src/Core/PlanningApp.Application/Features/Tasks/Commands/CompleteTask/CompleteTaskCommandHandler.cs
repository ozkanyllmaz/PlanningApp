using System;
using MediatR;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Application.Features.Tasks.Commands.CompleteTask;

public sealed class CompleteTaskCommandHandler(
    IPlanningTaskRepository taskRepository,
    IStreakRepository streakRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<CompleteTaskCommand>
{
    public async Task Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Başka kullanıcının kaydı da repository'den null döner: "bulunamadı" sayılır
        var task = await taskRepository.GetByIdAsync(userId, request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlanningTask), request.TaskId);

        // Idempotency: hedef durum zaten sağlanmışsa (offline kuyruk tekrarı) hata verme.
        // Bu kontrol gün-değerlendirme kontrolünden ÖNCE gelir; yoksa daha önce başarılı
        // olmuş bir isteğin tekrarı yanlışlıkla reddedilirdi.
        if (task.IsCompleted)
            return;

        if (task.AssignedDate is { } assignedDate)
        {
            var streak = await streakRepository.GetByUserIdAsync(userId, cancellationToken);
            streak?.EnsureDayNotEvaluated(assignedDate);
        }

        task.Complete(request.CompletedAt);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
