using System;
using MediatR;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Application.Features.Tasks.Commands.ReturnTaskToPool;

public sealed class ReturnTaskToPoolCommandHandler(
    IPlanningTaskRepository taskRepository,
    IStreakRepository streakRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<ReturnTaskToPoolCommand>
{
    public async Task Handle(ReturnTaskToPoolCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Başka kullanıcının kaydı da repository'den null döner: "bulunamadı" sayılır
        var task = await taskRepository.GetByIdAsync(userId, request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlanningTask), request.TaskId);

        // Idempotency: görev zaten havuzdaysa ve SortOrder aynıysa (offline kuyruk tekrarı) hata verme.
        // Bu kontrol gün kilidinden ÖNCE gelir; yoksa daha önce başarılı olmuş bir isteğin tekrarı reddedilirdi.
        if (task.AssignedDate is null && task.SortOrder == request.SortOrder)
            return;

        // Gün kilidi: görevin atandığı gün değerlendirilmişse değişiklik yapılamaz (streak kaydı yoksa kilit yok)
        if (task.AssignedDate is { } assignedDate)
        {
            var streak = await streakRepository.GetByUserIdAsync(userId, cancellationToken);
            streak?.EnsureDayNotEvaluated(assignedDate);
        }

        task.ReturnToPool(request.SortOrder);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
