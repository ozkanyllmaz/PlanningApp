using FluentValidation.Results;

namespace PlanningApp.Application.Common.Exceptions;

// Application katmanının kendi doğrulama hatası; WebApi'deki global middleware tarafından 400'e çevrilir.
public sealed class ValidationException : Exception
{
    // Anahtar: property adı, değer: o property'ye ait hata mesajları
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("Bir veya daha fazla doğrulama hatası oluştu.")
    {
        Errors = failures
            .GroupBy(failure => failure.PropertyName, failure => failure.ErrorMessage)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }
}
