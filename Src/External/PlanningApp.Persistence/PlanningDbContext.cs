using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Domain.Tasks;
using PlanningApp.Persistence.Conventions;

namespace PlanningApp.Persistence;

// IUnitOfWork'ü uygular: SaveChangesAsync, DbContext'in kendi metodudur
public sealed class PlanningDbContext(DbContextOptions<PlanningDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<PlanningTask> PlanningTasks => Set<PlanningTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanningDbContext).Assembly);

        // Kolon, anahtar ve index adları snake_case'e çevrilir (tablo adları konfigürasyonlarda açıkça yazılır)
        ApplySnakeCaseNames(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Npgsql timestamptz için yalnızca UTC (offset 0) kabul eder; yazarken UTC'ye çevrilir
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<NullableUtcDateTimeOffsetConverter>();
    }

    private static void ApplySnakeCaseNames(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
                property.SetColumnName(SnakeCase.Convert(property.GetColumnName()));

            foreach (var key in entityType.GetKeys())
                key.SetName(SnakeCase.Convert(key.GetName() ?? string.Empty));

            foreach (var index in entityType.GetIndexes())
                index.SetDatabaseName(SnakeCase.Convert(index.GetDatabaseName() ?? string.Empty));
        }
    }
}
