using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanningApp.Domain.Users;

namespace PlanningApp.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        // Id, kimlik kullanıcı Id'si ile aynıdır; veritabanı üretmez
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();

        builder.Property(profile => profile.TimeZoneId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(profile => profile.CreatedAt).HasColumnType("timestamptz");
    }
}
