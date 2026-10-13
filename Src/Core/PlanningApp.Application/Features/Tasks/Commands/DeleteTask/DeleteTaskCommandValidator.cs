using System;
using FluentValidation;
using PlanningApp.Application.Common.Validation;

namespace PlanningApp.Application.Features.Tasks.Commands.DeleteTask;

public sealed class DeleteTaskCommandValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.TaskId).NotEmpty();

        RuleFor(x => x.DeletedAt)
            .MustNotBeInFuture(timeProvider, "Silinme zamanı gelecekte olamaz.");
    }
}
