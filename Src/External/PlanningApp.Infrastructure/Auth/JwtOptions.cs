namespace PlanningApp.Infrastructure.Auth;

// "Jwt" yapılandırma bölümü. SigningKey kodda/appsettings'te YOKTUR (user-secrets veya ortam değişkeni).
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    // Base64 kodlu; çözülmüş hali en az 32 bayt olmalıdır
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 60;
}
