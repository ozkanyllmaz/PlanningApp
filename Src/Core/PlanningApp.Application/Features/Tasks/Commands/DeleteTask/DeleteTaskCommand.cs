using System;
using MediatR;

namespace PlanningApp.Application.Features.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskCommand(Guid TaskId, DateTimeOffset DeletedAt) : IRequest;
