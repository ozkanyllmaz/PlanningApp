using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PlanningApp.Application.Common.Behaviors;

namespace PlanningApp.Application;

public static class DependencyInjection
{
    // TimeProvider kaydı burada YAPILMAZ; o WebApi'nin (composition root) sorumluluğundadır.
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
