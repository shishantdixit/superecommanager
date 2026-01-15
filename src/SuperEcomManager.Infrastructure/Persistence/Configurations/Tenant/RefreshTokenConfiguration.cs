using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Identity;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(rt => rt.RevokedReason)
            .HasMaxLength(256);

        builder.Property(rt => rt.ReplacedByToken)
            .HasMaxLength(512);

        builder.HasIndex(rt => rt.Token).IsUnique();
        builder.HasIndex(rt => rt.UserId);
        builder.HasIndex(rt => new { rt.UserId, rt.RevokedAt });

        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
