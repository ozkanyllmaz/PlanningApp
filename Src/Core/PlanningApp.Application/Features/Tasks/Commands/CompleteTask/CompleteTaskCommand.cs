using System;
using MediatR;

namespace PlanningApp.Application.Features.Tasks.Commands.CompleteTask;

// CompletedAt istemcinin yerel offset'iyle gelir (gün hesabı buna göre yapılır)
public sealed record CompleteTaskCommand(Guid TaskId, DateTimeOffset CompletedAt) : IRequest;
