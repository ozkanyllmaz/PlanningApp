using System;
using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Commands.SetTaskFocusColor;

public sealed class SetTaskFocusColorCommandValidator : AbstractValidator<SetTaskFocusColorCommand>
{
    public SetTaskFocusColorCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();

        // null = renk kaldırılır; değer verilmişse tanımlı bir renk olmalı
        RuleFor(x => x.FocusColor).IsInEnum();
    }
}
