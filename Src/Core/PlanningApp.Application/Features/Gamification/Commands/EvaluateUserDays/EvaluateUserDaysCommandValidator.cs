using FluentValidation;

namespace PlanningApp.Application.Features.Gamification.Commands.EvaluateUserDays;

public sealed class EvaluateUserDaysCommandValidator : AbstractValidator<EvaluateUserDaysCommand>
{
    public EvaluateUserDaysCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("Kullanıcı kimliği boş olamaz.");
    }
}
