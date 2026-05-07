using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches");

        builder.HasKey(match => match.Id);
        builder.Property(match => match.ExternalId).HasMaxLength(100);
        builder.Property(match => match.Phase).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(match => match.GroupName).HasMaxLength(20);
        builder.Property(match => match.HomeSlotRule).HasMaxLength(50);
        builder.Property(match => match.AwaySlotRule).HasMaxLength(50);
        builder.Property(match => match.Venue).HasMaxLength(150);
        builder.Property(match => match.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(match => match.CreatedAtUtc).IsRequired();

        builder.HasIndex(match => match.MatchNumber).IsUnique();
        builder.HasIndex(match => match.KickoffTimeUtc);

        builder.HasOne(match => match.HomeTeam)
            .WithMany(team => team.HomeMatches)
            .HasForeignKey(match => match.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(match => match.AwayTeam)
            .WithMany(team => team.AwayMatches)
            .HasForeignKey(match => match.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(match => match.WinnerTeam)
            .WithMany()
            .HasForeignKey(match => match.WinnerTeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
