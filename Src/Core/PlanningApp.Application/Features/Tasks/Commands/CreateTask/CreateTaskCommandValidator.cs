using System;
using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    // İstemci saati ile sunucu saati arasındaki küçük farka izin verilen tolerans
    private static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromMinutes(5);

    public CreateTaskCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Priority).IsInEnum();

        RuleFor(x => x.CreatedAt)
            .Must(createdAt => createdAt <= timeProvider.GetUtcNow() + ClockSkewTolerance)
            .WithMessage("Oluşturulma zamanı gelecekte olamaz.");
    }
}
