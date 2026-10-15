using FluentValidation;

namespace PlanningApp.Application.Features.Auth.Commands.RefreshAccessToken;

public sealed class RefreshAccessTokenCommandValidator : AbstractValidator<RefreshAccessTokenCommand>
{
    public RefreshAccessTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Yenileme token'ı boş olamaz.");
    }
}
