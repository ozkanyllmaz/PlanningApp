using System;
using MediatR;

namespace PlanningApp.Application.Features.Tasks.Commands.RenameTask;

public sealed record RenameTaskCommand(Guid TaskId, string Title) : IRequest;
