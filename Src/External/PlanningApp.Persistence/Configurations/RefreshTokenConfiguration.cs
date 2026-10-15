using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanningApp.Domain.Auth;

namespace PlanningApp.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();

        builder.Property(token => token.UserId).IsRequired();

        // Düz token SAKLANMAZ; yalnızca SHA-256 özeti tutulur
        builder.Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(token => token.CreatedAt).HasColumnType("timestamptz");
        builder.Property(token => token.ExpiresAt).HasColumnType("timestamptz");
        builder.Property(token => token.RevokedAt).HasColumnType("timestamptz");
        builder.Property(token => token.ReplacedByTokenId);

        // İyimser eşzamanlılık: aynı token ile eşzamanlı iki yenilemeden yalnızca biri başarılı olur.
        // Npgsql, uint "xmin" için IsRowVersion'ı sistem kolonu olarak tanır.
        builder.Property<uint>("xmin").IsRowVersion();

        // Özetle arama ve benzersizlik
        builder.HasIndex(token => token.TokenHash)
            .HasDatabaseName("IX_refresh_tokens_TokenHash")
            .IsUnique();

        builder.HasIndex(token => token.UserId)
            .HasDatabaseName("IX_refresh_tokens_UserId");
    }
}
