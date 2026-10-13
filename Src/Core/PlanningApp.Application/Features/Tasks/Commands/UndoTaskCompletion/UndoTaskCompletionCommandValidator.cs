using System;
using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Commands.UndoTaskCompletion;

public sealed class UndoTaskCompletionCommandValidator : AbstractValidator<UndoTaskCompletionCommand>
{
    public UndoTaskCompletionCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
    }
}
