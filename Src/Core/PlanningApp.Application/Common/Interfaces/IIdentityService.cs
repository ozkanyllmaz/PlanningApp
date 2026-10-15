using System;
using PlanningApp.Application.Common.Models;

namespace PlanningApp.Application.Common.Interfaces;

// Identity (kullanıcı deposu) erişimi; somut implementasyon Persistence'tadır
public interface IIdentityService
{
    // Hata alan adlarıyla döner: e-posta çakışması "Email", parola kuralı ihlali "Password"
    Task<IdentityOperationResult> CreateUserAsync(
        Guid userId, string email, string password, CancellationToken cancellationToken);

    // Hatalı deneme sayacı ve kilitleme burada yönetilir
    Task<CredentialCheckResult> CheckCredentialsAsync(
        string email, string password, CancellationToken cancellationToken);

    Task<AppUserInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    // Kayıt sırasında telafi (compensation) için kullanılır
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken);
}
