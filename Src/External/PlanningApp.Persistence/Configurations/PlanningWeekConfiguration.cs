using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanningApp.Domain.Weeks;

namespace PlanningApp.Persistence.Configurations;

public sealed class PlanningWeekConfiguration : IEntityTypeConfiguration<PlanningWeek>
{
    public void Configure(EntityTypeBuilder<PlanningWeek> builder)
    {
        builder.ToTable("planning_weeks");

        builder.HasKey(week => week.Id);
        builder.Property(week => week.Id).ValueGeneratedNever();

        builder.Property(week => week.UserId).IsRequired();
        builder.Property(week => week.WeekStartDate).HasColumnType("date");
        builder.Property(week => week.IsArchived).IsRequired();
        builder.Property(week => week.CompletionPercentage).HasColumnType("numeric(5,2)");
        builder.Property(week => week.ArchivedAt).HasColumnType("timestamptz");

        // WeekEndDate, WeekStartDate'ten türetilir; saklanmaz
        builder.Ignore(week => week.WeekEndDate);

        // Bir kullanıcının aynı haftaya ait tek kaydı olur
        builder.HasIndex(week => new { week.UserId, week.WeekStartDate })
            .HasDatabaseName("IX_planning_weeks_UserId_WeekStartDate")
            .IsUnique();
    }
}
