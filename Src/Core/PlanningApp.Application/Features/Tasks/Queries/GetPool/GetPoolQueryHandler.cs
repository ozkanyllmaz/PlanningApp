using System.Collections.Generic;
using System.Linq;
using MediatR;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Application.Features.Tasks.Dtos;

namespace PlanningApp.Application.Features.Tasks.Queries.GetPool;

public sealed class GetPoolQueryHandler(
    IPlanningTaskRepository taskRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetPoolQuery, IReadOnlyList<TaskDto>>
{
    public async Task<IReadOnlyList<TaskDto>> Handle(GetPoolQuery request, CancellationToken cancellationToken)
    {
        var tasks = await taskRepository.GetPoolAsync(currentUser.UserId, cancellationToken);

        // Sıralama repository'de değil burada yapılır: SortOrder, CreatedAt, Id (artan)
        return tasks
            .OrderBy(task => task.SortOrder)
            .ThenBy(task => task.CreatedAt)
            .ThenBy(task => task.Id)
            .Select(TaskDto.From)
            .ToList();
    }
}
