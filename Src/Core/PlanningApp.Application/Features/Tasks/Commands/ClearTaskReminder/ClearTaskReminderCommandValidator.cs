using System;
using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Commands.ClearTaskReminder;

public sealed class ClearTaskReminderCommandValidator : AbstractValidator<ClearTaskReminderCommand>
{
    public ClearTaskReminderCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
    }
}
