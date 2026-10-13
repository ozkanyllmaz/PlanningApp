using System;
using FluentValidation;
using PlanningApp.Application.Common.Validation;

namespace PlanningApp.Application.Features.Tasks.Commands.CopyTaskToDate;

// "Hedef gün bugünden önce olamaz" kuralı kullanıcının saat dilimine bağlıdır; handler'da kontrol edilir.
public sealed class CopyTaskToDateCommandValidator : AbstractValidator<CopyTaskToDateCommand>
{
    public CopyTaskToDateCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.NewId).NotEmpty();

        RuleFor(x => x.SourceTaskId).NotEmpty();

        RuleFor(x => x.TargetDate).NotEmpty();

        RuleFor(x => x.SortOrder)
            .Must(double.IsFinite)
            .WithMessage("Sıralama değeri geçerli bir sayı olmalıdır.");

        RuleFor(x => x.CreatedAt)
            .MustNotBeInFuture(timeProvider, "Oluşturulma zamanı gelecekte olamaz.");
    }
}
