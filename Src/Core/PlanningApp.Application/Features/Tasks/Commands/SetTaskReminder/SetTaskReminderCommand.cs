using System;
using MediatR;

namespace PlanningApp.Application.Features.Tasks.Commands.SetTaskReminder;

public sealed record SetTaskReminderCommand(Guid TaskId, TimeOnly ReminderTime) : IRequest;
