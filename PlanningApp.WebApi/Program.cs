using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PlanningApp.Application;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Infrastructure;
using PlanningApp.Infrastructure.Auth;
using PlanningApp.Persistence;
using PlanningApp.WebApi.Authentication;
using PlanningApp.WebApi.ErrorHandling;
using PlanningApp.WebApi.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();

// Eksik alan FluentValidation tarafından yakalanır; böylece tek bir hata biçimi kalır
builder.Services.AddControllers(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

// JWT doğrulaması JwtOptions'tan (tek kaynak) beslenir; imzalama anahtarı JwtTokenService ile aynı yardımcıyla çözülür
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;

        bearerOptions.MapInboundClaims = false;
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtSigningKeyResolver.Resolve(jwt),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// Varsayılan: kimliği doğrulanmış kullanıcı. Anonim uçlar [AllowAnonymous] ile AÇIKÇA işaretlenir.
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// "auth" politikası: IP başına sabit pencere (1 dk), kuyruk yok. Limit yapılandırmadan, tüm kaynaklar
// yüklendikten sonra okunur.
builder.Services.AddRateLimiter(_ => { });
builder.Services
    .AddOptions<RateLimiterOptions>()
    .Configure<IConfiguration, IProblemDetailsService>((rateLimiterOptions, configuration, problemDetailsService) =>
    {
        var permitLimit = configuration.GetValue<int?>(RateLimitPolicies.AuthPermitLimitKey)
            ?? RateLimitPolicies.DefaultAuthPermitLimit;

        rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        rateLimiterOptions.AddPolicy(RateLimitPolicies.Auth, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
        {
            var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? (int)Math.Ceiling(retryAfter.TotalSeconds)
                : 60;

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.Headers.RetryAfter =
                retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context.HttpContext,
                ProblemDetails =
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Çok fazla istek.",
                    Detail = "İstek sınırı aşıldı; lütfen bir süre sonra tekrar deneyin."
                }
            });
        };
    });

if (builder.Environment.IsDevelopment())
    builder.Services.AddOpenApi();

var app = builder.Build();

// Yerel kolaylık: yalnızca Development'ta şema açılışta uygulanır. Diğer ortamlarda migration dağıtım adımıdır.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<PlanningDbContext>().Database.MigrateAsync();

    app.MapOpenApi().AllowAnonymous();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// ApiTests'in WebApplicationFactory<Program> kullanabilmesi için
public partial class Program;
