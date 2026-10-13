using System;
using MediatR;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Application.Features.Tasks.Commands.ChangeTaskPriority;

public sealed record ChangeTaskPriorityCommand(Guid TaskId, TaskPriority Priority) : IRequest;
