using FluentAssertions;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class MatchSettlementServiceTests
{
    [Fact]
    public async Task SettleMatchAsync_ShouldNotifyRankingUpdatedAfterSavingSnapshots()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider { UtcNow = new DateTime(2026, 6, 15, 20, 0, 0, DateTimeKind.Utc) };
        var homeTeam = CreateTeam("Poland", "POL");
        var awayTeam = CreateTeam("Germany", "GER");
        var user = CreateUser();
        var match = new Match
        {
            Id = Guid.NewGuid(),
            MatchNumber = 1,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            KickoffTimeUtc = dateTimeProvider.UtcNow.AddHours(-2),
            HomeScore90 = 2,
            AwayScore90 = 1,
            Status = MatchStatus.Finished,
            CreatedAtUtc = dateTimeProvider.UtcNow.AddDays(-1),
        };
        var prediction = new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            MatchId = match.Id,
            PredictedHomeScore = 2,
            PredictedAwayScore = 1,
            CreatedAtUtc = dateTimeProvider.UtcNow.AddDays(-1),
        };
        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Users.Add(user);
        dbContext.Matches.Add(match);
        dbContext.Predictions.Add(prediction);
        await dbContext.SaveChangesAsync();

        var notificationService = new FakeNotificationService();
        var service = new MatchSettlementService(
            dbContext,
            new ScoringService(),
            new LeaderboardBuilder(dbContext),
            dateTimeProvider,
            notificationService);

        await service.SettleMatchAsync(match.Id);

        dbContext.LeaderboardSnapshots.Should().ContainSingle(snapshot => snapshot.MatchId == match.Id);
        notificationService.RankingUpdatedMatchIds.Should().ContainSingle().Which.Should().Be(match.Id);
    }

    private static Team CreateTeam(string name, string shortName) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            ShortName = shortName,
            CountryCode = shortName,
        };

    private static ApplicationUser CreateUser() =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = "player@test.local",
            DisplayName = "Player",
            PasswordHash = "hash",
            Role = UserRole.Player,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

    private sealed class FakeNotificationService : INotificationService
    {
        public List<Guid> RankingUpdatedMatchIds { get; } = [];

        public Task NotifyPredictionsClosingSoonAsync(Guid matchId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task NotifyRankingUpdatedAsync(Guid matchId, CancellationToken cancellationToken = default)
        {
            RankingUpdatedMatchIds.Add(matchId);
            return Task.CompletedTask;
        }
    }
}
