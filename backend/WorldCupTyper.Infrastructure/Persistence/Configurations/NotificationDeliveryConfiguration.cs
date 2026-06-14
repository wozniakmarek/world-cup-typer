using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Infrastructure.Persistence.Configurations;

public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("NotificationDeliveries");

        builder.HasKey(delivery => delivery.Id);
        builder.Property(delivery => delivery.SubjectKey).HasMaxLength(120).IsRequired();
        builder.Property(delivery => delivery.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(delivery => delivery.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(delivery => delivery.ErrorCode).HasMaxLength(100);
        builder.Property(delivery => delivery.ScheduledForUtc).IsRequired();
        builder.Property(delivery => delivery.CreatedAtUtc).IsRequired();

        builder.HasIndex(delivery => new
        {
            delivery.UserId,
            delivery.PushSubscriptionId,
            delivery.Type,
            delivery.SubjectKey,
            delivery.ScheduledForUtc,
        }).IsUnique();

        builder.HasOne(delivery => delivery.User)
            .WithMany(user => user.NotificationDeliveries)
            .HasForeignKey(delivery => delivery.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(delivery => delivery.PushSubscription)
            .WithMany(subscription => subscription.NotificationDeliveries)
            .HasForeignKey(delivery => delivery.PushSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(delivery => delivery.Match)
            .WithMany()
            .HasForeignKey(delivery => delivery.MatchId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
