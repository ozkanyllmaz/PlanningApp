using FluentValidation;

namespace PlanningApp.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola boş olamaz.")
            .MaximumLength(128).WithMessage("Parola en fazla 128 karakter olabilir.");
    }
}
