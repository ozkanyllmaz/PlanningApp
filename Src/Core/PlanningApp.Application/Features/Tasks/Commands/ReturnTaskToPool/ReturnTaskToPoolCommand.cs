using System;
using MediatR;

namespace PlanningApp.Application.Features.Tasks.Commands.ReturnTaskToPool;

public sealed record ReturnTaskToPoolCommand(Guid TaskId, double SortOrder) : IRequest;
