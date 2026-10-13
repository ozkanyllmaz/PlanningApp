using System;
using FluentValidation;
using PlanningApp.Application.Common.Validation;

namespace PlanningApp.Application.Features.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Priority).IsInEnum();

        RuleFor(x => x.SortOrder)
            .Must(double.IsFinite)
            .WithMessage("Sıralama değeri geçerli bir sayı olmalıdır.");

        RuleFor(x => x.CreatedAt)
            .MustNotBeInFuture(timeProvider, "Oluşturulma zamanı gelecekte olamaz.");
    }
}
