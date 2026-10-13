using FluentValidation;

namespace PlanningApp.Application.Common.Validation;

public static class ValidatorExtensions
{
    // İstemciden gelen zaman damgası sunucu saatinden en fazla ClockSkew.Tolerance kadar ileride olabilir
    public static IRuleBuilderOptions<T, DateTimeOffset> MustNotBeInFuture<T>(
        this IRuleBuilder<T, DateTimeOffset> ruleBuilder,
        TimeProvider timeProvider,
        string message)
        => ruleBuilder
            .Must(value => value <= timeProvider.GetUtcNow() + ClockSkew.Tolerance)
            .WithMessage(message);
}
