using Microsoft.AspNetCore.Identity;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Models;

namespace PlanningApp.Persistence.Identity;

// UserManager kullanır, SignInManager KULLANMAZ (HTTP bağımlılığı).
// UserManager işlemleri kendi SaveChanges'ini çağırır; "repository SaveChanges çağırmaz"
// kuralının tek istisnası budur (bu yüzden Application kayıtta telafi uygular).
public sealed class IdentityService(UserManager<ApplicationUser> userManager, PlanningDbContext context)
    : IIdentityService
{
    private const string EmailField = "Email";
    private const string PasswordField = "Password";
    private const string GeneralField = "General";

    public async Task<IdentityOperationResult> CreateUserAsync(
        Guid userId, string email, string password, CancellationToken cancellationToken)
    {
        // E-posta kullanıcı adıdır
        var user = new ApplicationUser { Id = userId, UserName = email, Email = email };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            return IdentityOperationResult.Success();

        return IdentityOperationResult.Failure(MapErrors(result.Errors));
    }

    public async Task<CredentialCheckResult> CheckCredentialsAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var failed = new CredentialCheckResult(false, null, null);

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return failed;

        // Kilitli hesap doğru parolayla bile giriş yapamaz
        if (await userManager.IsLockedOutAsync(user))
            return failed;

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            // Hatalı deneme sayacını artırır; eşik aşılınca hesabı kilitler
            await userManager.AccessFailedAsync(user);
            return failed;
        }

        // Başarılı girişte hatalı deneme sayacı sıfırlanır
        await userManager.ResetAccessFailedCountAsync(user);

        return new CredentialCheckResult(true, user.Id, user.Email);
    }

    public async Task<AppUserInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        return user is null || user.Email is null
            ? null
            : new AppUserInfo(user.Id, user.Email);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Başarısız bir SaveChanges bağlamda bekleyen (Added) entity'ler bırakabilir; silme işlemi
        // kendi SaveChanges'ini çağırdığında onları yeniden denemesin diye izleyici temizlenir.
        context.ChangeTracker.Clear();

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return;

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var descriptions = string.Join("; ", result.Errors.Select(error => error.Code));
            throw new InvalidOperationException($"Kullanıcı silinemedi: {descriptions}");
        }
    }

    // Identity hata kodlarını spec'teki alan adlarına ("Email", "Password") ve Türkçe mesajlara eşler
    private static Dictionary<string, string[]> MapErrors(IEnumerable<IdentityError> errors)
    {
        return errors
            .Select(error => (Field: ResolveField(error.Code), Message: ResolveMessage(error)))
            .GroupBy(item => item.Field, item => item.Message)
            .ToDictionary(group => group.Key, group => group.Distinct().ToArray());
    }

    private static string ResolveField(string code)
    {
        if (code.StartsWith("Password", StringComparison.Ordinal))
            return PasswordField;

        if (code.Contains("Email", StringComparison.Ordinal) || code.Contains("UserName", StringComparison.Ordinal))
            return EmailField;

        return GeneralField;
    }

    private static string ResolveMessage(IdentityError error)
        => error.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" => "Bu e-posta adresi zaten kayıtlı.",
            "InvalidEmail" or "InvalidUserName" => "Geçerli bir e-posta adresi giriniz.",
            "PasswordTooShort" => "Parola en az 8 karakter olmalıdır.",
            _ => error.Description
        };
}
