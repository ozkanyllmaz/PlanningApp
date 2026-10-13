using System;
using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Commands.SetTaskReminder;

public sealed class SetTaskReminderCommandValidator : AbstractValidator<SetTaskReminderCommand>
{
    public SetTaskReminderCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();

        // TimeOnly'nin her değeri geçerli bir saattir; ek kural yok. Görevin güne atanmış olması Domain'de kontrol edilir.
    }
}
