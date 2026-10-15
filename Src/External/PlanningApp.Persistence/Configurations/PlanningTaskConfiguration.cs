using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Persistence.Configurations;

public sealed class PlanningTaskConfiguration : IEntityTypeConfiguration<PlanningTask>
{
    public void Configure(EntityTypeBuilder<PlanningTask> builder)
    {
        builder.ToTable("planning_tasks");

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).ValueGeneratedNever();

        builder.Property(task => task.UserId).IsRequired();

        builder.Property(task => task.Title)
            .IsRequired()
            .HasMaxLength(200);

        // Enum değerleri sabittir; int olarak saklanır
        builder.Property(task => task.Priority).HasConversion<int>();
        builder.Property(task => task.FocusColor).HasConversion<int?>();

        builder.Property(task => task.SortOrder).IsRequired();

        // DateOnly → date, TimeOnly → time, DateTimeOffset → timestamptz (UTC converter DbContext'te)
        builder.Property(task => task.AssignedDate).HasColumnType("date");
        builder.Property(task => task.ReminderTime).HasColumnType("time");
        builder.Property(task => task.CreatedAt).HasColumnType("timestamptz");
        builder.Property(task => task.CompletedAt).HasColumnType("timestamptz");
        builder.Property(task => task.DeletedAt).HasColumnType("timestamptz");

        // Günlük plan ve aralık sorguları; yalnızca silinmemiş kayıtlar indekslenir
        builder.HasIndex(task => new { task.UserId, task.AssignedDate })
            .HasDatabaseName("IX_planning_tasks_UserId_AssignedDate")
            .HasFilter("is_deleted = false");

        // Havuz sorgusu
        builder.HasIndex(task => task.UserId)
            .HasDatabaseName("IX_planning_tasks_UserId")
            .HasFilter("is_deleted = false");
    }
}
