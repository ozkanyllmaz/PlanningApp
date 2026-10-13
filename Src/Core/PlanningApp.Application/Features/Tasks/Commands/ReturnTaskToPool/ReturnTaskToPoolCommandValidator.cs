using System;
using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Commands.ReturnTaskToPool;

public sealed class ReturnTaskToPoolCommandValidator : AbstractValidator<ReturnTaskToPoolCommand>
{
    public ReturnTaskToPoolCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();

        RuleFor(x => x.SortOrder)
            .Must(double.IsFinite)
            .WithMessage("Sıralama değeri geçerli bir sayı olmalıdır.");
    }
}
