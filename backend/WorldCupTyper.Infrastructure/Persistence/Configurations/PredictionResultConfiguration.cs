using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Infrastructure.Persistence.Configurations;

public sealed class PredictionResultConfiguration : IEntityTypeConfiguration<PredictionResult>
{
    public void Configure(EntityTypeBuilder<PredictionResult> builder)
    {
        builder.ToTable("PredictionResults");

        builder.HasKey(result => result.Id);
        builder.Property(result => result.Points).IsRequired();
        builder.Property(result => result.IsExactScore).IsRequired();
        builder.Property(result => result.IsCorrectOutcome).IsRequired();
        builder.Property(result => result.CalculatedAtUtc).IsRequired();
    }
}
