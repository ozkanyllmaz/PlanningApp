using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Application.Common.Interfaces.Repositories;
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

        return services;
    }
}
