using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Infrastructure.Persistence.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        builder.HasKey(team => team.Id);
        builder.Property(team => team.Name).HasMaxLength(100).IsRequired();
        builder.Property(team => team.ShortName).HasMaxLength(20).IsRequired();
        builder.Property(team => team.CountryCode).HasMaxLength(3).IsRequired();
        builder.Property(team => team.FlagEmoji).HasMaxLength(10);
        builder.Property(team => team.GroupName).HasMaxLength(20);

        builder.HasIndex(team => team.Name).IsUnique();
        builder.HasIndex(team => team.ShortName).IsUnique();
    }
}
