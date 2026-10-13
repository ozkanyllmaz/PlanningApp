using System;
using MediatR;

namespace PlanningApp.Application.Features.Tasks.Commands.ReorderTask;

public sealed record ReorderTaskCommand(Guid TaskId, double SortOrder) : IRequest;
