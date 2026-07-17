using FluentAssertions;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Infrastructure.Persistence;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class FinalSummaryServiceTests
{
    [Fact]
    public async Task GetFinalSummaryAsync_ShouldReturnActivePlayerPositionSeriesSortedByFinalPosition()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var tomek, out var inactive);
        inactive.IsActive = false;
        var matchOne = AddSettledMatch(dbContext, 1, "POL", "GER", DateTime.UtcNow.AddDays(-2));
        var matchTwo = AddSettledMatch(dbContext, 2, string.Empty, "ESP", DateTime.UtcNow.AddDays(-1));
        AddSnapshot(dbContext, matchOne.Id, marek.Id, totalPoints: 3, exact: 1, outcome: 1, predictions: 1, position: 2, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, marek.Id, totalPoints: 9, exact: 3, outcome: 3, predictions: 2, position: 1, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchOne.Id, tomek.Id, totalPoints: 6, exact: 2, outcome: 2, predictions: 1, position: 1, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, tomek.Id, totalPoints: 7, exact: 2, outcome: 3, predictions: 2, position: 2, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, inactive.Id, totalPoints: 99, exact: 33, outcome: 33, predictions: 2, position: 1, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddPrediction(dbContext, marek.Id, matchOne.Id, 1, 0, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, marek.Id, matchTwo.Id, 0, 0, points: 1, exact: false, outcome: true);
        AddPrediction(dbContext, tomek.Id, matchOne.Id, 0, 1, points: 0, exact: false, outcome: false);
        AddPrediction(dbContext, tomek.Id, matchTwo.Id, 1, 0, points: 3, exact: true, outcome: true);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var summary = await service.GetFinalSummaryAsync(marek.Id);

        summary.Stats.SettledMatchesCount.Should().Be(2);
        summary.Stats.ActivePlayersCount.Should().Be(2);
        summary.Stats.FinalLeaderUserId.Should().Be(marek.Id);
        summary.Stats.FinalLeaderDisplayName.Should().Be("Marek");
        summary.PositionSeries.Select(series => series.DisplayName).Should().Equal("Marek", "Tomek");
        summary.PositionSeries.Single(series => series.UserId == marek.Id).IsCurrentUser.Should().BeTrue();
        var marekPoints = summary.PositionSeries.Single(series => series.UserId == marek.Id).Points.ToList();
        marekPoints.Select(point => point.Position).Should().Equal(2, 1);
        marekPoints.Select(point => point.MatchId).Should().Equal(matchOne.Id, matchTwo.Id);
        marekPoints.Select(point => point.MatchNumber).Should().Equal(1, 2);
        marekPoints.Select(point => point.MatchLabel).Should().Equal("POL-GER", "M2");
        marekPoints.Select(point => point.SnapshotAtUtc).Should().Equal(matchOne.KickoffTimeUtc.AddHours(2), matchTwo.KickoffTimeUtc.AddHours(2));
        marekPoints.Select(point => point.TotalPoints).Should().Equal(3, 9);
        summary.FinalTop.Select(entry => entry.DisplayName).Should().Equal("Marek", "Tomek");
        var marekFinalTop = summary.FinalTop.Single(entry => entry.UserId == marek.Id);
        marekFinalTop.TotalPoints.Should().Be(4);
        marekFinalTop.ExactScoreHits.Should().Be(1);
        marekFinalTop.CorrectOutcomeHits.Should().Be(2);
        marekFinalTop.PredictionsCount.Should().Be(2);
        marekFinalTop.IsCurrentUser.Should().BeTrue();
        var tomekFinalTop = summary.FinalTop.Single(entry => entry.UserId == tomek.Id);
        tomekFinalTop.TotalPoints.Should().Be(3);
        tomekFinalTop.ExactScoreHits.Should().Be(1);
        tomekFinalTop.CorrectOutcomeHits.Should().Be(1);
        tomekFinalTop.PredictionsCount.Should().Be(2);
        tomekFinalTop.IsCurrentUser.Should().BeFalse();
    }

    [Fact]
    public async Task GetFinalSummaryAsync_ShouldReturnGlobalFactsFromSnapshotsAndPredictions()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var tomek, out _);
        var matchOne = AddSettledMatch(dbContext, 1, "MEX", "RSA", DateTime.UtcNow.AddDays(-2), homeScore: 2, awayScore: 0);
        var matchTwo = AddSettledMatch(dbContext, 2, "FRA", "ESP", DateTime.UtcNow.AddDays(-1), homeScore: 1, awayScore: 1);
        AddSnapshot(dbContext, matchOne.Id, marek.Id, totalPoints: 3, exact: 1, outcome: 1, predictions: 1, position: 8, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, marek.Id, totalPoints: 10, exact: 2, outcome: 4, predictions: 2, position: 1, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchOne.Id, tomek.Id, totalPoints: 6, exact: 2, outcome: 2, predictions: 1, position: 1, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, tomek.Id, totalPoints: 7, exact: 2, outcome: 3, predictions: 2, position: 4, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddPrediction(dbContext, marek.Id, matchOne.Id, 2, 0, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, tomek.Id, matchOne.Id, 2, 0, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, marek.Id, matchTwo.Id, 1, 1, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, tomek.Id, matchTwo.Id, 0, 0, points: 1, exact: false, outcome: true);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var summary = await service.GetFinalSummaryAsync();

        summary.GlobalFacts.Should().Contain(fact => fact.Id == "biggest-climb" && fact.RelatedUserIds.Contains(marek.Id));
        summary.GlobalFacts.Should().Contain(fact => fact.Id == "biggest-drop" && fact.RelatedUserIds.Contains(tomek.Id));
        summary.GlobalFacts.Should().Contain(fact => fact.Id == "most-exact-match" && fact.RelatedMatchIds.Contains(matchOne.Id));
        summary.GlobalFacts.Single(fact => fact.Id == "biggest-climb").Label.Should().Be("Największy awans");
        summary.GlobalFacts.Single(fact => fact.Id == "most-exact-match").Label.Should().Be("Najwięcej dokładnych wyników");
        summary.GlobalFacts.Single(fact => fact.Id == "most-exact-match").Description.Should().Contain("dokładnych typów");
        var drawSpecialist = summary.GlobalFacts.Single(fact => fact.Id == "draw-specialist");
        drawSpecialist.RelatedUserIds.Should().Equal(marek.Id, tomek.Id);
        drawSpecialist.RelatedMatchIds.Should().Equal(matchTwo.Id);
        summary.GlobalFacts.Count.Should().BeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task GetFinalSummaryAsync_ShouldOrderPositionPointsByKickoffThenMatchNumberBeforeSnapshotTime()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out _, out var inactive);
        inactive.IsActive = false;
        var kickoffTimeUtc = DateTime.UtcNow.AddDays(-1);
        var matchOne = AddSettledMatch(dbContext, 1, "POL", "GER", kickoffTimeUtc);
        var matchTwo = AddSettledMatch(dbContext, 2, "FRA", "ESP", kickoffTimeUtc);
        AddSnapshot(dbContext, matchOne.Id, marek.Id, totalPoints: 3, exact: 1, outcome: 1, predictions: 1, position: 2, createdAtUtc: kickoffTimeUtc.AddHours(4));
        AddSnapshot(dbContext, matchTwo.Id, marek.Id, totalPoints: 4, exact: 1, outcome: 2, predictions: 2, position: 1, createdAtUtc: kickoffTimeUtc.AddHours(2));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var summary = await service.GetFinalSummaryAsync(marek.Id);

        var points = summary.PositionSeries.Single(series => series.UserId == marek.Id).Points.ToList();
        points.Select(point => point.MatchNumber).Should().Equal(1, 2);
        points.Select(point => point.MatchId).Should().Equal(matchOne.Id, matchTwo.Id);
        points.Select(point => point.SnapshotAtUtc).Should().Equal(kickoffTimeUtc.AddHours(4), kickoffTimeUtc.AddHours(2));
    }

    [Fact]
    public async Task GetFinalSummaryAsync_ShouldIncludeActivePredictionLeaderWithoutSnapshots()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var tomek, out var inactive);
        inactive.IsActive = false;
        var ola = CreateUser("Ola");
        dbContext.Users.Add(ola);
        var matchOne = AddSettledMatch(dbContext, 1, "MEX", "RSA", DateTime.UtcNow.AddDays(-2), homeScore: 2, awayScore: 0);
        var matchTwo = AddSettledMatch(dbContext, 2, "FRA", "ESP", DateTime.UtcNow.AddDays(-1), homeScore: 1, awayScore: 1);
        AddSnapshot(dbContext, matchOne.Id, marek.Id, totalPoints: 3, exact: 1, outcome: 1, predictions: 1, position: 1, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, marek.Id, totalPoints: 4, exact: 1, outcome: 2, predictions: 2, position: 2, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddPrediction(dbContext, marek.Id, matchOne.Id, 2, 0, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, marek.Id, matchTwo.Id, 0, 0, points: 1, exact: false, outcome: true);
        AddPrediction(dbContext, ola.Id, matchOne.Id, 2, 0, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, ola.Id, matchTwo.Id, 1, 1, points: 3, exact: true, outcome: true);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var summary = await service.GetFinalSummaryAsync(ola.Id);
        var personal = await service.GetPersonalFinalSummaryAsync(ola.Id);

        summary.PositionSeries.Select(series => series.UserId).Should().Equal(marek.Id);
        summary.Stats.ActivePlayersCount.Should().Be(3);
        summary.Stats.FinalLeaderUserId.Should().Be(ola.Id);
        summary.Stats.FinalLeaderDisplayName.Should().Be("Ola");
        summary.FinalTop.Select(entry => entry.UserId).Should().Equal(ola.Id, marek.Id, tomek.Id);
        var olaFinalTop = summary.FinalTop.Single(entry => entry.UserId == ola.Id);
        olaFinalTop.FinalPosition.Should().Be(1);
        olaFinalTop.TotalPoints.Should().Be(6);
        olaFinalTop.ExactScoreHits.Should().Be(2);
        olaFinalTop.CorrectOutcomeHits.Should().Be(2);
        olaFinalTop.PredictionsCount.Should().Be(2);
        olaFinalTop.IsCurrentUser.Should().BeTrue();
        personal.FinalPosition.Should().Be(1);
        personal.TotalPoints.Should().Be(6);
        personal.ExactScoreHits.Should().Be(2);
        personal.CorrectOutcomeHits.Should().Be(2);
        personal.PredictionsCount.Should().Be(2);
    }

    [Fact]
    public async Task GetFinalSummaryAsync_ShouldUseActiveUserDisplayNameForDrawSpecialistWithoutSnapshots()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out _, out var inactive);
        inactive.IsActive = false;
        var ola = CreateUser("Ola");
        dbContext.Users.Add(ola);
        var drawMatch = AddSettledMatch(dbContext, 1, "FRA", "ESP", DateTime.UtcNow.AddDays(-1), homeScore: 1, awayScore: 1);
        AddSnapshot(dbContext, drawMatch.Id, marek.Id, totalPoints: 0, exact: 0, outcome: 0, predictions: 0, position: 1, createdAtUtc: drawMatch.KickoffTimeUtc.AddHours(2));
        AddPrediction(dbContext, ola.Id, drawMatch.Id, 1, 1, points: 3, exact: true, outcome: true);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var summary = await service.GetFinalSummaryAsync();

        summary.PositionSeries.Select(series => series.UserId).Should().NotContain(ola.Id);
        var drawSpecialist = summary.GlobalFacts.Single(fact => fact.Id == "draw-specialist");
        drawSpecialist.RelatedUserIds.Should().Equal(ola.Id);
        drawSpecialist.RelatedMatchIds.Should().Equal(drawMatch.Id);
        $"{drawSpecialist.Title} {drawSpecialist.Description}".Should().Contain("Ola").And.NotContain("Gracz");
    }

    [Fact]
    public async Task GetPersonalFinalSummaryAsync_ShouldReturnAtLeastThreeFactsForPlayerWithData()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var tomek, out _);
        var matchOne = AddSettledMatch(dbContext, 1, "POL", "GER", DateTime.UtcNow.AddDays(-3), homeScore: 2, awayScore: 1);
        var matchTwo = AddSettledMatch(dbContext, 2, "FRA", "ESP", DateTime.UtcNow.AddDays(-2), homeScore: 1, awayScore: 1);
        var matchThree = AddSettledMatch(dbContext, 3, "BRA", "ARG", DateTime.UtcNow.AddDays(-1), homeScore: 3, awayScore: 0);
        AddSnapshot(dbContext, matchOne.Id, marek.Id, totalPoints: 1, exact: 0, outcome: 1, predictions: 1, position: 4, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, marek.Id, totalPoints: 4, exact: 1, outcome: 2, predictions: 2, position: 2, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchThree.Id, marek.Id, totalPoints: 7, exact: 2, outcome: 3, predictions: 3, position: 1, createdAtUtc: matchThree.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchThree.Id, tomek.Id, totalPoints: 6, exact: 1, outcome: 3, predictions: 3, position: 2, createdAtUtc: matchThree.KickoffTimeUtc.AddHours(2));
        AddPrediction(dbContext, marek.Id, matchOne.Id, 2, 0, points: 1, exact: false, outcome: true);
        AddPrediction(dbContext, marek.Id, matchTwo.Id, 1, 1, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, marek.Id, matchThree.Id, 3, 0, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, tomek.Id, matchThree.Id, 2, 0, points: 1, exact: false, outcome: true);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var recap = await service.GetPersonalFinalSummaryAsync(marek.Id);

        recap.DisplayName.Should().Be("Marek");
        recap.FinalPosition.Should().Be(1);
        recap.PersonalFacts.Count.Should().BeGreaterThanOrEqualTo(3);
        recap.PersonalFacts.Should().Contain(fact => fact.Id == "personal-best-match");
        recap.PersonalFacts.Should().Contain(fact => fact.Id == "personal-biggest-climb");
        recap.PersonalFacts.Single(fact => fact.Id == "personal-final-rank").Description.Should().Contain("kończy").And.Contain("dokładnymi");
        recap.PersonalFacts.Single(fact => fact.Id == "personal-biggest-climb").Description.Should().Contain("przesunął się");
        recap.HighlightedMatchIds.Should().Contain(matchThree.Id);
    }

    [Fact]
    public async Task GetFinalSummaryAsync_ShouldPreferEightInterestingGlobalFactsWhenDataAllows()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var tomek, out _);
        var matchOne = AddSettledMatch(dbContext, 1, "POL", "GER", DateTime.UtcNow.AddDays(-4), homeScore: 2, awayScore: 1);
        var matchTwo = AddSettledMatch(dbContext, 2, "FRA", "ESP", DateTime.UtcNow.AddDays(-3), homeScore: 1, awayScore: 1);
        var matchThree = AddSettledMatch(dbContext, 3, "BRA", "ARG", DateTime.UtcNow.AddDays(-2), homeScore: 3, awayScore: 0);
        var matchFour = AddSettledMatch(dbContext, 4, "USA", "JPN", DateTime.UtcNow.AddDays(-1), homeScore: 0, awayScore: 0);
        foreach (var match in new[] { matchOne, matchTwo, matchThree, matchFour })
        {
            AddSnapshot(dbContext, match.Id, marek.Id, totalPoints: match.MatchNumber * 3, exact: match.MatchNumber, outcome: match.MatchNumber + 1, predictions: match.MatchNumber, position: Math.Max(1, 5 - match.MatchNumber), createdAtUtc: match.KickoffTimeUtc.AddHours(2));
            AddSnapshot(dbContext, match.Id, tomek.Id, totalPoints: match.MatchNumber, exact: 0, outcome: match.MatchNumber, predictions: match.MatchNumber, position: match.MatchNumber + 1, createdAtUtc: match.KickoffTimeUtc.AddHours(2));
            AddPrediction(dbContext, marek.Id, match.Id, match.HomeScore90!.Value, match.AwayScore90!.Value, points: 3, exact: true, outcome: true);
            AddPrediction(dbContext, tomek.Id, match.Id, match.HomeScore90.Value, match.AwayScore90.Value + 1, points: match.HomeScore90 == match.AwayScore90 ? 0 : 1, exact: false, outcome: match.HomeScore90 != match.AwayScore90);
        }
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var summary = await service.GetFinalSummaryAsync();

        summary.GlobalFacts.Count.Should().BeGreaterThanOrEqualTo(8);
        summary.GlobalFacts.Select(fact => fact.Id).Should().Contain(new[]
        {
            "biggest-climb",
            "biggest-drop",
            "most-exact-match",
            "draw-specialist",
            "strongest-finish",
            "scoreline-magnet",
            "most-consistent",
            "one-goal-away",
        });
    }

    [Fact]
    public async Task GetFinalSummaryAsync_ShouldSurfaceTriviaPatternsFromPredictions()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var tomek, out _);
        var ola = CreateUser("Ola");
        dbContext.Users.Add(ola);

        var trapMatch = AddSettledMatch(dbContext, 1, "ESP", "CPV", DateTime.UtcNow.AddDays(-5), homeScore: 0, awayScore: 0);
        var everyoneKnewMatch = AddSettledMatch(dbContext, 2, "GER", "CUW", DateTime.UtcNow.AddDays(-4), homeScore: 7, awayScore: 1);
        var soloOne = AddSettledMatch(dbContext, 3, "KOR", "CZE", DateTime.UtcNow.AddDays(-3), homeScore: 2, awayScore: 1);
        var soloTwo = AddSettledMatch(dbContext, 4, "GHA", "PAN", DateTime.UtcNow.AddDays(-2), homeScore: 1, awayScore: 0);

        AddPrediction(dbContext, marek.Id, trapMatch.Id, 2, 1, points: 0, exact: false, outcome: false);
        AddPrediction(dbContext, tomek.Id, trapMatch.Id, 1, 0, points: 0, exact: false, outcome: false);
        AddPrediction(dbContext, ola.Id, trapMatch.Id, 0, 2, points: 0, exact: false, outcome: false);

        AddPrediction(dbContext, marek.Id, everyoneKnewMatch.Id, 3, 0, points: 1, exact: false, outcome: true);
        AddPrediction(dbContext, tomek.Id, everyoneKnewMatch.Id, 4, 1, points: 1, exact: false, outcome: true);
        AddPrediction(dbContext, ola.Id, everyoneKnewMatch.Id, 2, 0, points: 1, exact: false, outcome: true);

        AddPrediction(dbContext, marek.Id, soloOne.Id, 2, 1, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, tomek.Id, soloOne.Id, 1, 0, points: 1, exact: false, outcome: true);
        AddPrediction(dbContext, ola.Id, soloOne.Id, 0, 1, points: 0, exact: false, outcome: false);

        AddPrediction(dbContext, marek.Id, soloTwo.Id, 1, 0, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, tomek.Id, soloTwo.Id, 2, 0, points: 1, exact: false, outcome: true);
        AddPrediction(dbContext, ola.Id, soloTwo.Id, 0, 1, points: 0, exact: false, outcome: false);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var summary = await service.GetFinalSummaryAsync();

        var trap = summary.GlobalFacts.Single(fact => fact.Id == "biggest-trap");
        trap.Title.Should().Contain("ESP-CPV").And.Contain("3");
        trap.Description.Should().Contain("wszyscy").And.Contain("złą stronę");
        trap.RelatedMatchIds.Should().Equal(trapMatch.Id);

        var noExact = summary.GlobalFacts.Single(fact => fact.Id == "no-exact-perfect-direction");
        noExact.Title.Should().Contain("GER-CUW").And.Contain("3").And.Contain("0 dokładnych");
        noExact.Description.Should().Contain("kierunek").And.Contain("rozmiaru");
        noExact.RelatedMatchIds.Should().Equal(everyoneKnewMatch.Id);

        var soloKing = summary.GlobalFacts.Single(fact => fact.Id == "solo-exact-king");
        soloKing.Title.Should().Contain("Marek").And.Contain("2");
        soloKing.Description.Should().Contain("samotnie").And.Contain("KOR-CZE").And.Contain("GHA-PAN");
        soloKing.RelatedUserIds.Should().Equal(marek.Id);
        soloKing.RelatedMatchIds.Should().Equal(soloOne.Id, soloTwo.Id);

        var streak = summary.GlobalFacts.Single(fact => fact.Id == "longest-exact-streak");
        streak.Title.Should().Contain("Marek").And.Contain("2");
        streak.Description.Should().Contain("dokładne").And.Contain("z rzędu");
        streak.RelatedMatchIds.Should().Equal(soloOne.Id, soloTwo.Id);
    }

    [Fact]
    public async Task GetFinalSummaryAsync_ShouldCountAllNonExactSettledPredictionsForOneGoalAwayFact()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var tomek, out _);
        var matchOne = AddSettledMatch(dbContext, 1, "POL", "GER", DateTime.UtcNow.AddDays(-4), homeScore: 2, awayScore: 1);
        var matchTwo = AddSettledMatch(dbContext, 2, "FRA", "ESP", DateTime.UtcNow.AddDays(-3), homeScore: 1, awayScore: 1);
        var matchThree = AddSettledMatch(dbContext, 3, "BRA", "ARG", DateTime.UtcNow.AddDays(-2), homeScore: 3, awayScore: 0);
        var matchFour = AddSettledMatch(dbContext, 4, "USA", "JPN", DateTime.UtcNow.AddDays(-1), homeScore: 0, awayScore: 0);
        foreach (var match in new[] { matchOne, matchTwo, matchThree, matchFour })
        {
            AddSnapshot(dbContext, match.Id, marek.Id, totalPoints: match.MatchNumber, exact: 0, outcome: match.MatchNumber, predictions: match.MatchNumber, position: 1, createdAtUtc: match.KickoffTimeUtc.AddHours(2));
            AddSnapshot(dbContext, match.Id, tomek.Id, totalPoints: match.MatchNumber, exact: 0, outcome: match.MatchNumber, predictions: match.MatchNumber, position: 2, createdAtUtc: match.KickoffTimeUtc.AddHours(2));
        }
        AddPrediction(dbContext, marek.Id, matchOne.Id, 0, 4, points: 0, exact: false, outcome: false);
        AddPrediction(dbContext, marek.Id, matchTwo.Id, 4, 0, points: 0, exact: false, outcome: false);
        AddPrediction(dbContext, marek.Id, matchThree.Id, 0, 3, points: 0, exact: false, outcome: false);
        AddPrediction(dbContext, tomek.Id, matchFour.Id, 0, 1, points: 0, exact: false, outcome: false);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var summary = await service.GetFinalSummaryAsync();

        var oneGoalAway = summary.GlobalFacts.Single(fact => fact.Id == "one-goal-away");
        oneGoalAway.RelatedUserIds.Should().Equal(marek.Id);
        oneGoalAway.RelatedMatchIds.Should().Equal(matchOne.Id, matchTwo.Id, matchThree.Id);
        oneGoalAway.Title.Should().Contain("3");
        $"{oneGoalAway.Label} {oneGoalAway.Title} {oneGoalAway.Description}".Should().NotContain("Jedna bramka").And.NotContain("jedna bramke");
        $"{oneGoalAway.Label} {oneGoalAway.Title} {oneGoalAway.Description}".Should().Contain("Najwięcej").And.Contain("niedokładnych typów").And.Contain("dokładnego wyniku");
    }

    [Fact]
    public async Task GetPersonalFinalSummaryAsync_ShouldReturnMovementAndPredictionFactsForPlayerWhoDrops()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var tomek, out _);
        var matchOne = AddSettledMatch(dbContext, 1, "POL", "GER", DateTime.UtcNow.AddDays(-3), homeScore: 2, awayScore: 1);
        var matchTwo = AddSettledMatch(dbContext, 2, "FRA", "ESP", DateTime.UtcNow.AddDays(-2), homeScore: 1, awayScore: 1);
        var matchThree = AddSettledMatch(dbContext, 3, "BRA", "ARG", DateTime.UtcNow.AddDays(-1), homeScore: 3, awayScore: 0);
        AddSnapshot(dbContext, matchOne.Id, marek.Id, totalPoints: 3, exact: 1, outcome: 1, predictions: 1, position: 1, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, marek.Id, totalPoints: 3, exact: 1, outcome: 1, predictions: 2, position: 3, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchThree.Id, marek.Id, totalPoints: 3, exact: 1, outcome: 1, predictions: 3, position: 4, createdAtUtc: matchThree.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchThree.Id, tomek.Id, totalPoints: 9, exact: 3, outcome: 3, predictions: 3, position: 1, createdAtUtc: matchThree.KickoffTimeUtc.AddHours(2));
        AddPrediction(dbContext, marek.Id, matchOne.Id, 2, 1, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, marek.Id, matchTwo.Id, 4, 0, points: 0, exact: false, outcome: false);
        AddPrediction(dbContext, marek.Id, matchThree.Id, 0, 3, points: 0, exact: false, outcome: false);
        AddPrediction(dbContext, tomek.Id, matchThree.Id, 3, 0, points: 3, exact: true, outcome: true);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var recap = await service.GetPersonalFinalSummaryAsync(marek.Id);

        recap.DisplayName.Should().Be("Marek");
        recap.PersonalFacts.Count.Should().BeGreaterThanOrEqualTo(3);
        recap.PersonalFacts.Should().Contain(fact => fact.Id == "personal-biggest-drop");
        recap.PersonalFacts.Should().Contain(fact => fact.Id == "personal-non-exact-count");
    }

    [Fact]
    public async Task GetPersonalFinalSummaryAsync_ShouldIncludeSoloExactAndExactStreakFacts()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var tomek, out _);
        var ola = CreateUser("Ola");
        dbContext.Users.Add(ola);
        var matchOne = AddSettledMatch(dbContext, 1, "KOR", "CZE", DateTime.UtcNow.AddDays(-3), homeScore: 2, awayScore: 1);
        var matchTwo = AddSettledMatch(dbContext, 2, "GHA", "PAN", DateTime.UtcNow.AddDays(-2), homeScore: 1, awayScore: 0);
        var matchThree = AddSettledMatch(dbContext, 3, "ESP", "CPV", DateTime.UtcNow.AddDays(-1), homeScore: 0, awayScore: 0);

        AddPrediction(dbContext, marek.Id, matchOne.Id, 2, 1, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, tomek.Id, matchOne.Id, 1, 0, points: 1, exact: false, outcome: true);
        AddPrediction(dbContext, ola.Id, matchOne.Id, 0, 1, points: 0, exact: false, outcome: false);

        AddPrediction(dbContext, marek.Id, matchTwo.Id, 1, 0, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, tomek.Id, matchTwo.Id, 2, 0, points: 1, exact: false, outcome: true);
        AddPrediction(dbContext, ola.Id, matchTwo.Id, 0, 1, points: 0, exact: false, outcome: false);

        AddPrediction(dbContext, marek.Id, matchThree.Id, 2, 1, points: 0, exact: false, outcome: false);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var recap = await service.GetPersonalFinalSummaryAsync(marek.Id);

        var solo = recap.PersonalFacts.Single(fact => fact.Id == "personal-solo-exacts");
        solo.Title.Should().Contain("2");
        solo.Description.Should().Contain("tylko Marek").And.Contain("KOR-CZE").And.Contain("GHA-PAN");
        solo.RelatedMatchIds.Should().Equal(matchOne.Id, matchTwo.Id);

        var streak = recap.PersonalFacts.Single(fact => fact.Id == "personal-exact-streak");
        streak.Title.Should().Contain("2");
        streak.Description.Should().Contain("dokładne").And.Contain("z rzędu");
        streak.RelatedMatchIds.Should().Equal(matchOne.Id, matchTwo.Id);
    }

    private static FinalSummaryService CreateService(WorldCupTyperDbContext dbContext)
    {
        return new FinalSummaryService(dbContext);
    }

    private static void SeedUsers(WorldCupTyperDbContext dbContext, out ApplicationUser marek, out ApplicationUser tomek, out ApplicationUser inactive)
    {
        marek = CreateUser("Marek");
        tomek = CreateUser("Tomek");
        inactive = CreateUser("Inactive");
        dbContext.Users.AddRange(marek, tomek, inactive);
    }

    private static ApplicationUser CreateUser(string displayName)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"{displayName.ToLowerInvariant()}@test.local",
            DisplayName = displayName,
            PasswordHash = "hash",
            Role = UserRole.Player,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    private static Match AddSettledMatch(WorldCupTyperDbContext dbContext, int matchNumber, string homeShortName, string awayShortName, DateTime kickoffTimeUtc, int homeScore = 1, int awayScore = 0)
    {
        var homeTeam = new Team { Id = Guid.NewGuid(), Name = homeShortName, ShortName = homeShortName, CountryCode = homeShortName };
        var awayTeam = new Team { Id = Guid.NewGuid(), Name = awayShortName, ShortName = awayShortName, CountryCode = awayShortName };
        var match = new Match
        {
            Id = Guid.NewGuid(),
            MatchNumber = matchNumber,
            Phase = MatchPhase.GroupStage,
            HomeTeamId = homeTeam.Id,
            HomeTeam = homeTeam,
            AwayTeamId = awayTeam.Id,
            AwayTeam = awayTeam,
            KickoffTimeUtc = kickoffTimeUtc,
            Status = MatchStatus.Settled,
            HomeScore90 = homeScore,
            AwayScore90 = awayScore,
            IsSettled = true,
            CreatedAtUtc = kickoffTimeUtc.AddDays(-1),
        };

        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Matches.Add(match);
        return match;
    }

    private static void AddSnapshot(WorldCupTyperDbContext dbContext, Guid matchId, Guid userId, int totalPoints, int exact, int outcome, int predictions, int position, DateTime createdAtUtc)
    {
        dbContext.LeaderboardSnapshots.Add(new LeaderboardSnapshot
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            UserId = userId,
            TotalPoints = totalPoints,
            ExactScoreHits = exact,
            CorrectOutcomeHits = outcome,
            PredictionsCount = predictions,
            Position = position,
            CreatedAtUtc = createdAtUtc,
        });
    }

    private static void AddPrediction(WorldCupTyperDbContext dbContext, Guid userId, Guid matchId, int predictedHome, int predictedAway, int points, bool exact, bool outcome)
    {
        dbContext.Predictions.Add(new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MatchId = matchId,
            PredictedHomeScore = predictedHome,
            PredictedAwayScore = predictedAway,
            CreatedAtUtc = DateTime.UtcNow,
            Result = new PredictionResult
            {
                Id = Guid.NewGuid(),
                Points = points,
                IsExactScore = exact,
                IsCorrectOutcome = outcome,
                CalculatedAtUtc = DateTime.UtcNow,
            },
        });
    }
}
