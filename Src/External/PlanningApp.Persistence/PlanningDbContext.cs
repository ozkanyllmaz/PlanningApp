using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Domain.Auth;
using PlanningApp.Domain.Gamification;
using PlanningApp.Domain.Tasks;
using PlanningApp.Domain.Users;
using PlanningApp.Domain.Weeks;
using PlanningApp.Persistence.Conventions;
using PlanningApp.Persistence.Identity;

namespace PlanningApp.Persistence;

// Rolsüz Identity bağlamından türer; IUnitOfWork'ü uygular (SaveChangesAsync DbContext'in kendi metodudur)
public sealed class PlanningDbContext(DbContextOptions<PlanningDbContext> options)
    : IdentityUserContext<ApplicationUser, Guid>(options), IUnitOfWork
{
    public DbSet<PlanningTask> PlanningTasks => Set<PlanningTask>();
    public DbSet<PlanningWeek> PlanningWeeks => Set<PlanningWeek>();
    public DbSet<Streak> Streaks => Set<Streak>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Önce Identity şeması kurulur, sonra bizim konfigürasyonlar tablo adlarını ezer
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanningDbContext).Assembly);

        // Kolon, anahtar, index ve FK adları snake_case'e çevrilir (tablo adları konfigürasyonlarda açıkça yazılır)
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

            foreach (var foreignKey in entityType.GetForeignKeys())
                foreignKey.SetConstraintName(SnakeCase.Convert(foreignKey.GetConstraintName() ?? string.Empty));
        }
    }
}
