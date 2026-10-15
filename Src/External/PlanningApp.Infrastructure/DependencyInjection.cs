using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Infrastructure.Auth;

namespace PlanningApp.Infrastructure;

public static class DependencyInjection
{
    // TimeProvider kaydı burada YAPILMAZ; o WebApi'nin (composition root) sorumluluğundadır.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Geçersiz Jwt yapılandırmasında (kısa/bozuk anahtar vb.) uygulama başlangıçta hata verir
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
