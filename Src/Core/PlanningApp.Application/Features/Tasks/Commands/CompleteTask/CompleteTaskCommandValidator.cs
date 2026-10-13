using System;
using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Commands.CompleteTask;

public sealed class CompleteTaskCommandValidator : AbstractValidator<CompleteTaskCommand>
{
    // CreateTask validator'ındaki toleransla aynı; üçüncü kullanımda Common'a çıkarılacak
    private static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromMinutes(5);

    public CompleteTaskCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.TaskId).NotEmpty();

        RuleFor(x => x.CompletedAt)
            .Must(completedAt => completedAt <= timeProvider.GetUtcNow() + ClockSkewTolerance)
            .WithMessage("Tamamlanma zamanı gelecekte olamaz.");
    }
}
