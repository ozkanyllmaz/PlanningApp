namespace PlanningApp.WebApi.RateLimiting;

public static class RateLimitPolicies
{
    // register, login, refresh ve logout uçlarına uygulanır
    public const string Auth = "auth";

    public const string AuthPermitLimitKey = "RateLimiting:Auth:PermitLimit";
    public const int DefaultAuthPermitLimit = 20;
}
