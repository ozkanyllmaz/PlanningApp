namespace PlanningApp.Application.Common.Exceptions;

// Kimlik doğrulama başarısız (hatalı bilgi, kilitli hesap, geçersiz/süresi dolmuş refresh token).
// Mesaj bilgi sızdırmayacak şekilde geneldir; WebApi'deki global middleware tarafından 401'e çevrilir.
public sealed class AuthenticationFailedException : Exception
{
    public const string InvalidCredentialsMessage = "E-posta veya şifre hatalı.";
    public const string InvalidSessionMessage = "Oturum geçersiz veya süresi dolmuş; lütfen yeniden giriş yapın.";

    public AuthenticationFailedException(string message)
        : base(message)
    {
    }
}
