using System;
using MediatR;

namespace PlanningApp.Application.Features.Tasks.Commands.UndoTaskCompletion;

public sealed record UndoTaskCompletionCommand(Guid TaskId) : IRequest;
