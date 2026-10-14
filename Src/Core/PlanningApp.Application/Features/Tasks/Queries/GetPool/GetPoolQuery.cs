using System.Collections.Generic;
using MediatR;
using PlanningApp.Application.Features.Tasks.Dtos;

namespace PlanningApp.Application.Features.Tasks.Queries.GetPool;

// Girdisi yoktur; kullanıcı kimliği ICurrentUserService'ten alınır
public sealed record GetPoolQuery : IRequest<IReadOnlyList<TaskDto>>;
