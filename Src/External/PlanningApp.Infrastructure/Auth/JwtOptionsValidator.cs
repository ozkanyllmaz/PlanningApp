using Microsoft.Extensions.Options;

namespace PlanningApp.Infrastructure.Auth;

// ValidateOnStart ile çalışır: geçersiz yapılandırmada uygulama BAŞLAMAZ
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public const int MinimumSigningKeyBytes = 32;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
            failures.Add("Jwt:Issuer boş olamaz.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            failures.Add("Jwt:Audience boş olamaz.");

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            failures.Add("Jwt:SigningKey boş olamaz.");
        }
        else
        {
            // Değer hata mesajına YAZILMAZ
            var buffer = new byte[options.SigningKey.Length];
            if (!Convert.TryFromBase64String(options.SigningKey, buffer, out var bytesWritten))
                failures.Add("Jwt:SigningKey geçerli bir base64 değeri olmalıdır.");
            else if (bytesWritten < MinimumSigningKeyBytes)
                failures.Add($"Jwt:SigningKey çözülmüş haliyle en az {MinimumSigningKeyBytes} bayt olmalıdır.");
        }

        if (options.AccessTokenMinutes <= 0)
            failures.Add("Jwt:AccessTokenMinutes pozitif olmalıdır.");

        if (options.RefreshTokenDays <= 0)
            failures.Add("Jwt:RefreshTokenDays pozitif olmalıdır.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
