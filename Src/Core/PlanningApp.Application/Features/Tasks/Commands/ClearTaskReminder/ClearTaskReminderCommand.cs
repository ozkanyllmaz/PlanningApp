using System;
using MediatR;

namespace PlanningApp.Application.Features.Tasks.Commands.ClearTaskReminder;

public sealed record ClearTaskReminderCommand(Guid TaskId) : IRequest;
