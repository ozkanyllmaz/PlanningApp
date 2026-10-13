using System;
using MediatR;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Application.Features.Tasks.Commands.CreateTask;

// Id ve CreatedAt istemciden gelir (offline-first). UserId bilerek YOK: ICurrentUserService'ten alınır.
public sealed record CreateTaskCommand(
    Guid Id,
    string Title,
    TaskPriority Priority,
    double SortOrder,
    DateTimeOffset CreatedAt) : IRequest<Guid>;

