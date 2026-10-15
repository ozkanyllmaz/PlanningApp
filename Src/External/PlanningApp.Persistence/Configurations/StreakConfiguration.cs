using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanningApp.Domain.Gamification;

namespace PlanningApp.Persistence.Configurations;

public sealed class StreakConfiguration : IEntityTypeConfiguration<Streak>
{
    public void Configure(EntityTypeBuilder<Streak> builder)
    {
        builder.ToTable("streaks");

        builder.HasKey(streak => streak.Id);
        builder.Property(streak => streak.Id).ValueGeneratedNever();

        builder.Property(streak => streak.UserId).IsRequired();
        builder.Property(streak => streak.CurrentStreak).IsRequired();
        builder.Property(streak => streak.LongestStreak).IsRequired();
        builder.Property(streak => streak.LastEvaluatedDate).HasColumnType("date");
        builder.Property(streak => streak.AvailableFreezes).IsRequired();
        builder.Property(streak => streak.ConsecutiveEmptyDays).IsRequired();

        // İyimser eşzamanlılık: PostgreSQL xmin sistem kolonu shadow property olarak kullanılır
        // (Domain'de alan yok). Çakışmada DbUpdateConcurrencyException yükselir.
        // Npgsql, uint "xmin" için IsRowVersion'ı sistem kolonu olarak tanır (migration'a kolon eklenmez).
        builder.Property<uint>("xmin").IsRowVersion();

        // Bir kullanıcının tek bir Streak kaydı olur
        builder.HasIndex(streak => streak.UserId)
            .HasDatabaseName("IX_streaks_UserId")
            .IsUnique();
    }
}
