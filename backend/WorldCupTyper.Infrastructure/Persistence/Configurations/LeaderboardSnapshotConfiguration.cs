using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Infrastructure.Persistence.Configurations;

public sealed class LeaderboardSnapshotConfiguration : IEntityTypeConfiguration<LeaderboardSnapshot>
{
    public void Configure(EntityTypeBuilder<LeaderboardSnapshot> builder)
    {
        builder.ToTable("LeaderboardSnapshots");

        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.CreatedAtUtc).IsRequired();

        builder.HasIndex(snapshot => new { snapshot.MatchId, snapshot.UserId }).IsUnique();

        builder.HasOne(snapshot => snapshot.Match)
            .WithMany(match => match.LeaderboardSnapshots)
            .HasForeignKey(snapshot => snapshot.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(snapshot => snapshot.User)
            .WithMany(user => user.LeaderboardSnapshots)
            .HasForeignKey(snapshot => snapshot.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
