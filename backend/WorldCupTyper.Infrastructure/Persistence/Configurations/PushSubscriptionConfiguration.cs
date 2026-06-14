using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Infrastructure.Persistence.Configurations;

public sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("PushSubscriptions");

        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Endpoint).HasMaxLength(500).IsRequired();
        builder.Property(subscription => subscription.P256dh).HasMaxLength(255).IsRequired();
        builder.Property(subscription => subscription.Auth).HasMaxLength(255).IsRequired();
        builder.Property(subscription => subscription.UserAgent).HasMaxLength(500);
        builder.Property(subscription => subscription.CreatedAtUtc).IsRequired();
        builder.Property(subscription => subscription.LastSeenAtUtc).IsRequired();
        builder.Property(subscription => subscription.FailureCount).IsRequired();

        builder.HasIndex(subscription => subscription.Endpoint).IsUnique();
        builder.HasIndex(subscription => subscription.UserId);
        builder.HasIndex(subscription => subscription.RevokedAtUtc);

        builder.HasOne(subscription => subscription.User)
            .WithMany(user => user.PushSubscriptions)
            .HasForeignKey(subscription => subscription.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
