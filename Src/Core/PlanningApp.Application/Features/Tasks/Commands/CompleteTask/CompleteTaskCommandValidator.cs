using System;
using FluentValidation;
using PlanningApp.Application.Common.Validation;

namespace PlanningApp.Application.Features.Tasks.Commands.CompleteTask;

public sealed class CompleteTaskCommandValidator : AbstractValidator<CompleteTaskCommand>
{
    public CompleteTaskCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.TaskId).NotEmpty();

        RuleFor(x => x.CompletedAt)
            .MustNotBeInFuture(timeProvider, "Tamamlanma zamanı gelecekte olamaz.");
    }
}
