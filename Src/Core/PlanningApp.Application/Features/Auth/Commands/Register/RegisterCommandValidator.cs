using System;
using FluentValidation;

namespace PlanningApp.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .MaximumLength(256).WithMessage("E-posta en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        // Parola karmaşıklık kuralı Identity seçeneklerindedir (tek kaynak); burada yalnızca boşluk ve üst sınır
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola boş olamaz.")
            .MaximumLength(128).WithMessage("Parola en fazla 128 karakter olabilir.");

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("Saat dilimi boş olamaz.")
            .Must(BeValidTimeZone).WithMessage("Geçerli bir saat dilimi kimliği verilmelidir.");
    }

    private static bool BeValidTimeZone(string? timeZoneId)
        => !string.IsNullOrWhiteSpace(timeZoneId)
           && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out _);
}
