using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Infrastructure.Persistence.Configurations;

public sealed class PredictionConfiguration : IEntityTypeConfiguration<Prediction>
{
    public void Configure(EntityTypeBuilder<Prediction> builder)
    {
        builder.ToTable("Predictions");

        builder.HasKey(prediction => prediction.Id);
        builder.Property(prediction => prediction.PredictedHomeScore).IsRequired();
        builder.Property(prediction => prediction.PredictedAwayScore).IsRequired();
        builder.Property(prediction => prediction.CreatedAtUtc).IsRequired();

        builder.HasIndex(prediction => new { prediction.UserId, prediction.MatchId }).IsUnique();

        builder.HasOne(prediction => prediction.User)
            .WithMany(user => user.Predictions)
            .HasForeignKey(prediction => prediction.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(prediction => prediction.Match)
            .WithMany(match => match.Predictions)
            .HasForeignKey(prediction => prediction.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(prediction => prediction.Result)
            .WithOne(result => result.Prediction)
            .HasForeignKey<PredictionResult>(result => result.PredictionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
