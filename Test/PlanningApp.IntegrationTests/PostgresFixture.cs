using Microsoft.EntityFrameworkCore;
using PlanningApp.Persistence;
using Testcontainers.PostgreSql;

namespace PlanningApp.IntegrationTests;

// Tüm testler için tek paylaşılan PostgreSQL container'ı; şema migration'lardan kurulur
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // EnsureCreated değil: migration'ların kendisi de test edilir
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
        => await _container.DisposeAsync();

    // Her çağrı yeni bir DbContext verir; yazma ve okuma ayrı örneklerle yapılır (tracking etkisini dışlamak için)
    public PlanningDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new PlanningDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
