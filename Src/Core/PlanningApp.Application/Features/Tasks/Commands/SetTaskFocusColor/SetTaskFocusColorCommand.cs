using System;
using MediatR;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Application.Features.Tasks.Commands.SetTaskFocusColor;

public sealed record SetTaskFocusColorCommand(Guid TaskId, FocusColor? FocusColor) : IRequest;
