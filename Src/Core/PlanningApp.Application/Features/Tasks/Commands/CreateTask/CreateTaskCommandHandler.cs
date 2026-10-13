using System;
using MediatR;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Application.Features.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandHandler(
    IPlanningTaskRepository taskRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateTaskCommand, Guid>
{
    public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Idempotency: offline kuyruk aynı isteği tekrar gönderebilir.
        // Aynı Id bu kullanıcıda zaten varsa (silinmiş olsa da) yeni kayıt açmadan başarılı dön;
        // aksi halde silinmiş görevin tekrar gelen isteği birincil anahtar çakışması yaratırdı.
        var existing = await taskRepository.GetByIdIncludingDeletedAsync(userId, request.Id, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var task = PlanningTask.CreateInPool(
    request.Id, userId, request.Title, request.Priority, request.SortOrder, request.CreatedAt);

        await taskRepository.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return task.Id;
    }
}
