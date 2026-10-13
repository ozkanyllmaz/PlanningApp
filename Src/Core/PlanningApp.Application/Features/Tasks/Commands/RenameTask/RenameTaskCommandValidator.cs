using System;
using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Commands.RenameTask;

public sealed class RenameTaskCommandValidator : AbstractValidator<RenameTaskCommand>
{
    public RenameTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);
    }
}
