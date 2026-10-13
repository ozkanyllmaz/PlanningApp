using System;
using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Commands.ChangeTaskPriority;

public sealed class ChangeTaskPriorityCommandValidator : AbstractValidator<ChangeTaskPriorityCommand>
{
    public ChangeTaskPriorityCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();

        RuleFor(x => x.Priority).IsInEnum();
    }
}
