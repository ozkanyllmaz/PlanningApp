namespace PlanningApp.Application.Common.Models;

// Errors: alan adı → mesajlar (e-posta çakışması "Email", parola kuralı ihlali "Password")
public sealed record IdentityOperationResult(bool Succeeded, IReadOnlyDictionary<string, string[]> Errors)
{
    public static IdentityOperationResult Success()
        => new(true, new Dictionary<string, string[]>());

    public static IdentityOperationResult Failure(IReadOnlyDictionary<string, string[]> errors)
        => new(false, errors);
}
