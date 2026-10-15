using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
using PlanningApp.Persistence.Identity;
using PlanningApp.Persistence.Repositories;

namespace PlanningApp.Persistence;

public static class DependencyInjection
{
    private const string ConnectionStringName = "PlanningDb";

    // Bağlantı dizesi yapılandırmadan okunur (ConnectionStrings:PlanningDb); kodda sabit değer yoktur
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"'{ConnectionStringName}' bağlantı dizesi yapılandırmada bulunamadı.");

        services.AddDbContext<PlanningDbContext>(options => options.UseNpgsql(connectionString));

        // Aynı scope içinde handler ve repository aynı DbContext örneğini paylaşır
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<PlanningDbContext>());
        services.AddScoped<IPlanningTaskRepository, PlanningTaskRepository>();
        services.AddScoped<IPlanningWeekRepository, PlanningWeekRepository>();
        services.AddScoped<IStreakRepository, StreakRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Identity seçenekleri tek kaynaktır (bkz. Authentication.md): rolsüz, yalnızca kullanıcı deposu
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<PlanningDbContext>();

        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }
}
