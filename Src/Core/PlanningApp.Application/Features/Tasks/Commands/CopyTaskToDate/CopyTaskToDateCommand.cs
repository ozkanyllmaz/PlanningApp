using System;
using MediatR;

namespace PlanningApp.Application.Features.Tasks.Commands.CopyTaskToDate;

// NewId ve CreatedAt istemciden gelir (offline-first); yeni görevin Id'si döner.
public sealed record CopyTaskToDateCommand(
    Guid NewId,
    Guid SourceTaskId,
    DateOnly TargetDate,
    double SortOrder,
    DateTimeOffset CreatedAt) : IRequest<Guid>;
