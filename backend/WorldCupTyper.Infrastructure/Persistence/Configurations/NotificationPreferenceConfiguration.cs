using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Infrastructure.Persistence.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        builder.HasKey(preference => preference.UserId);
        builder.Property(preference => preference.MorningDigestEnabled).IsRequired();
        builder.Property(preference => preference.MissingPrediction2hEnabled).IsRequired();
        builder.Property(preference => preference.MissingPrediction30mEnabled).IsRequired();
        builder.Property(preference => preference.RankingUpdatedEnabled).IsRequired();
        builder.Property(preference => preference.UpdatedAtUtc).IsRequired();

        builder.HasOne(preference => preference.User)
            .WithOne(user => user.NotificationPreference)
            .HasForeignKey<NotificationPreference>(preference => preference.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
