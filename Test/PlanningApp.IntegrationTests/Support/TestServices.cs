using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanningApp.Application;
using PlanningApp.Infrastructure;
using PlanningApp.Persistence;

namespace PlanningApp.IntegrationTests.Support;

// Entegrasyon testleri için DI/yapılandırma yardımcıları. İmzalama anahtarı her çağrıda rastgele
// üretilir; repoda sabit anahtar BULUNMAZ.
internal static class TestServices
{
    // 32 baytlık rastgele anahtar, base64
    public static string NewSigningKey(int byteLength = 32)
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength));

    public static string GetConnectionString(PostgresFixture fixture)
    {
        using var context = fixture.CreateDbContext();
        return context.Database.GetConnectionString()!;
    }

    public static IConfiguration BuildConfiguration(string? connectionString, string? signingKey)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "planning-app-tests",
            ["Jwt:Audience"] = "planning-app-tests-clients",
            ["Jwt:SigningKey"] = signingKey
        };

        if (connectionString is not null)
            values["ConnectionStrings:PlanningDb"] = connectionString;

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    // Yalnızca Persistence (IdentityService vb. için)
    public static ServiceProvider BuildPersistenceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPersistence(BuildConfiguration(connectionString, NewSigningKey()));

        return services.BuildServiceProvider(validateScopes: true);
    }

    // Tüm katmanlar: AuthFlow testleri için
    public static ServiceProvider BuildApplicationProvider(string connectionString, TimeProvider timeProvider)
    {
        var configuration = BuildConfiguration(connectionString, NewSigningKey());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(timeProvider);
        services.AddApplication();
        services.AddPersistence(configuration);
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider(validateScopes: true);
    }
}
