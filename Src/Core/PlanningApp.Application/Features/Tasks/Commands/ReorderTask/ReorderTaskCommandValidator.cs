using System;
using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Commands.ReorderTask;

public sealed class ReorderTaskCommandValidator : AbstractValidator<ReorderTaskCommand>
{
    public ReorderTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();

        RuleFor(x => x.SortOrder)
            .Must(double.IsFinite)
            .WithMessage("Sıralama değeri geçerli bir sayı olmalıdır.");
    }
}
