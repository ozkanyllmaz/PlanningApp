using FluentValidation;

namespace PlanningApp.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Yenileme token'ı boş olamaz.");
    }
}
