using System;
using FluentValidation;

namespace PlanningApp.Application.Features.Tasks.Commands.AssignTaskToDate;

// "Hedef gün bugünden önce olamaz" kuralı kullanıcının saat dilimine bağlıdır; handler'da kontrol edilir.
public sealed class AssignTaskToDateCommandValidator : AbstractValidator<AssignTaskToDateCommand>
{
    public AssignTaskToDateCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();

        RuleFor(x => x.Date).NotEmpty();

        RuleFor(x => x.SortOrder)
            .Must(double.IsFinite)
            .WithMessage("Sıralama değeri geçerli bir sayı olmalıdır.");
    }
}
