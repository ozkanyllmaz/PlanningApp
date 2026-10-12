using FluentValidation;
using MediatR;
using ValidationException = PlanningApp.Application.Common.Exceptions.ValidationException;

namespace PlanningApp.Application.Common.Behaviors;

// Handler'a girmeden önce isteğe ait tüm validator'ları çalıştırır; hata varsa ValidationException fırlatır.
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Bu istek için validator tanımlanmamışsa doğrudan sonraki adıma geç
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next(cancellationToken);
    }
}
