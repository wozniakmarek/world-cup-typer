# Final Summary Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the post-tournament public summary page with one animated full-table position chart, richer global curiosities, selectable player focus, login access to existing functions, and a personal recap for every player.

**Architecture:** Add a backend summary service that prepares public final-summary data and authenticated personal recap data so the frontend does not derive facts from raw predictions. Add a new public `FinalSummaryPage`, a protected `PersonalRecapPage`, and a dedicated `FinalRankingStoryChart` that reuses existing visual language from the ranking page while keeping animation behavior isolated.

**Tech Stack:** .NET 8, EF Core, ASP.NET Core controllers, xUnit, FluentAssertions, React 19, Vite, TanStack Query, Recharts/SVG, Tailwind CSS, Playwright.

---

## File Structure

Backend files:

- Create `backend/WorldCupTyper.Application/DTOs/FinalSummaryDtos.cs`: public and personal recap response contracts.
- Create `backend/WorldCupTyper.Application/Services/Interfaces/IFinalSummaryService.cs`: service contract for final summary endpoints.
- Create `backend/WorldCupTyper.Application/Services/FinalSummaryService.cs`: all final summary aggregation, chart series construction, and fact selection.
- Create `backend/WorldCupTyper.Api/Controllers/SummaryController.cs`: `GET /api/summary/final` and `GET /api/summary/final/me`.
- Modify `backend/WorldCupTyper.Infrastructure/ServiceCollectionExtensions.cs`: register `IFinalSummaryService`.
- Create `backend/WorldCupTyper.Tests/SummaryControllerAuthorizationTests.cs`: endpoint authorization reflection checks.
- Create `backend/WorldCupTyper.Tests/FinalSummaryServiceTests.cs`: summary and personal recap aggregation tests.

Frontend files:

- Modify `frontend/src/api/types.ts`: add final summary DTO types.
- Modify `frontend/src/api/services.ts`: add `summaryApi`.
- Modify `frontend/src/routes/AppRouter.tsx`: render final public summary for logged-out users and add protected `/recap`.
- Modify `frontend/src/components/AppNavigation.tsx`: add authenticated `Recap` link.
- Create `frontend/src/features/summary/FinalSummaryPage.tsx`: public summary page.
- Create `frontend/src/features/summary/FinalRankingStoryChart.tsx`: animated full-table position chart with filters and selected player chips.
- Create `frontend/src/features/summary/FinalSummaryFactGrid.tsx`: global fact grid.
- Create `frontend/src/features/summary/PersonalRecapPage.tsx`: authenticated player recap.
- Create `frontend/src/features/summary/summaryChart.ts`: chart row, color, and movement helper functions.
- Modify `frontend/e2e/smoke.spec.ts`: update public landing smoke to final summary and add recap route smoke.

Do not stage or revert unrelated dirty files. Stage only the files listed above when executing each task.

---

### Task 1: Add Summary Contracts And Controller

**Files:**
- Create: `backend/WorldCupTyper.Application/DTOs/FinalSummaryDtos.cs`
- Create: `backend/WorldCupTyper.Application/Services/Interfaces/IFinalSummaryService.cs`
- Create: `backend/WorldCupTyper.Api/Controllers/SummaryController.cs`
- Create: `backend/WorldCupTyper.Tests/SummaryControllerAuthorizationTests.cs`

- [ ] **Step 1: Write the failing authorization and contract test**

Create `backend/WorldCupTyper.Tests/SummaryControllerAuthorizationTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using WorldCupTyper.Api.Controllers;
using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Tests;

public sealed class SummaryControllerAuthorizationTests
{
    [Fact]
    public void SummaryController_ShouldRequireAuthenticatedUserByDefault()
    {
        typeof(SummaryController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Should()
            .NotBeEmpty();
    }

    [Fact]
    public void GetFinalSummary_ShouldAllowAnonymousUsers()
    {
        var method = typeof(SummaryController).GetMethod(nameof(SummaryController.GetFinalSummary));

        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Should().NotBeEmpty();
    }

    [Fact]
    public void GetMyFinalSummary_ShouldStayAuthenticatedOnly()
    {
        var method = typeof(SummaryController).GetMethod(nameof(SummaryController.GetMyFinalSummary));

        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Should().BeEmpty();
    }

    [Fact]
    public void FinalSummaryDtos_ShouldExposeChartFactsAndPersonalRecap()
    {
        var matchId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var point = new FinalRankingPositionPointDto(matchId, 1, "POL-GER", DateTime.UnixEpoch, 2, 6);
        var series = new FinalRankingPositionSeriesDto(userId, "Marek", null, 1, 121, false, new[] { point });
        var fact = new FinalSummaryFactDto("biggest-climb", "Skok", "Awans o 9 miejsc", "Opis", new[] { userId }, new[] { matchId });
        var response = new FinalSummaryResponseDto(
            new FinalSummaryStatsDto(76, 24, userId, "Marek"),
            new[] { series },
            new[] { new FinalRankingEntryDto(userId, "Marek", null, 1, 121, 24, 73, 104, false) },
            new[] { fact });
        var personal = new PersonalFinalSummaryResponseDto(userId, "Marek", null, 1, 121, 24, 73, 104, new[] { fact }, new[] { matchId });

        response.PositionSeries.Single().Points.Single().Position.Should().Be(2);
        response.GlobalFacts.Single().Id.Should().Be("biggest-climb");
        personal.PersonalFacts.Single().Title.Should().Be("Awans o 9 miejsc");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter SummaryControllerAuthorizationTests
```

Expected: compile failure because `SummaryController`, `FinalSummaryResponseDto`, and related DTOs do not exist.

- [ ] **Step 3: Add DTOs, service contract, and controller**

Create `backend/WorldCupTyper.Application/DTOs/FinalSummaryDtos.cs`:

```csharp
namespace WorldCupTyper.Application.DTOs;

public sealed record FinalSummaryStatsDto(
    int SettledMatchesCount,
    int ActivePlayersCount,
    Guid? FinalLeaderUserId,
    string? FinalLeaderDisplayName);

public sealed record FinalSummaryResponseDto(
    FinalSummaryStatsDto Stats,
    IReadOnlyCollection<FinalRankingPositionSeriesDto> PositionSeries,
    IReadOnlyCollection<FinalRankingEntryDto> FinalTop,
    IReadOnlyCollection<FinalSummaryFactDto> GlobalFacts);

public sealed record FinalRankingPositionSeriesDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    int FinalPosition,
    int FinalPoints,
    bool IsCurrentUser,
    IReadOnlyCollection<FinalRankingPositionPointDto> Points);

public sealed record FinalRankingPositionPointDto(
    Guid MatchId,
    int MatchNumber,
    string MatchLabel,
    DateTime SnapshotAtUtc,
    int Position,
    int TotalPoints);

public sealed record FinalRankingEntryDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    int FinalPosition,
    int TotalPoints,
    int ExactScoreHits,
    int CorrectOutcomeHits,
    int PredictionsCount,
    bool IsCurrentUser);

public sealed record FinalSummaryFactDto(
    string Id,
    string Label,
    string Title,
    string Description,
    IReadOnlyCollection<Guid> RelatedUserIds,
    IReadOnlyCollection<Guid> RelatedMatchIds);

public sealed record PersonalFinalSummaryResponseDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    int FinalPosition,
    int TotalPoints,
    int ExactScoreHits,
    int CorrectOutcomeHits,
    int PredictionsCount,
    IReadOnlyCollection<FinalSummaryFactDto> PersonalFacts,
    IReadOnlyCollection<Guid> HighlightedMatchIds);
```

Create `backend/WorldCupTyper.Application/Services/Interfaces/IFinalSummaryService.cs`:

```csharp
using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Services.Interfaces;

public interface IFinalSummaryService
{
    Task<FinalSummaryResponseDto> GetFinalSummaryAsync(Guid? currentUserId = null, CancellationToken cancellationToken = default);
    Task<PersonalFinalSummaryResponseDto> GetPersonalFinalSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

Create `backend/WorldCupTyper.Api/Controllers/SummaryController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupTyper.Api.Extensions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/summary")]
public sealed class SummaryController : ControllerBase
{
    private readonly IFinalSummaryService _finalSummaryService;

    public SummaryController(IFinalSummaryService finalSummaryService)
    {
        _finalSummaryService = finalSummaryService;
    }

    [HttpGet("final")]
    [AllowAnonymous]
    public async Task<ActionResult<FinalSummaryResponseDto>> GetFinalSummary(CancellationToken cancellationToken)
    {
        Guid? currentUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        return Ok(await _finalSummaryService.GetFinalSummaryAsync(currentUserId, cancellationToken));
    }

    [HttpGet("final/me")]
    public async Task<ActionResult<PersonalFinalSummaryResponseDto>> GetMyFinalSummary(CancellationToken cancellationToken)
    {
        return Ok(await _finalSummaryService.GetPersonalFinalSummaryAsync(User.GetUserId(), cancellationToken));
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter SummaryControllerAuthorizationTests
```

Expected: pass.

- [ ] **Step 5: Commit contracts**

Run:

```powershell
git add backend\WorldCupTyper.Application\DTOs\FinalSummaryDtos.cs backend\WorldCupTyper.Application\Services\Interfaces\IFinalSummaryService.cs backend\WorldCupTyper.Api\Controllers\SummaryController.cs backend\WorldCupTyper.Tests\SummaryControllerAuthorizationTests.cs
git commit -m "feat: add final summary contracts"
```

Expected: commit includes only the four new files.

### Task 2: Build Public Final Summary Service

**Files:**
- Create: `backend/WorldCupTyper.Application/Services/FinalSummaryService.cs`
- Modify: `backend/WorldCupTyper.Infrastructure/ServiceCollectionExtensions.cs`
- Create: `backend/WorldCupTyper.Tests/FinalSummaryServiceTests.cs`

- [ ] **Step 1: Write failing public summary tests**

Create `backend/WorldCupTyper.Tests/FinalSummaryServiceTests.cs`:

```csharp
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
        var matchTwo = AddSettledMatch(dbContext, 2, "FRA", "ESP", DateTime.UtcNow.AddDays(-1));
        AddSnapshot(dbContext, matchOne.Id, marek.Id, totalPoints: 3, exact: 1, outcome: 1, predictions: 1, position: 2, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, marek.Id, totalPoints: 9, exact: 3, outcome: 3, predictions: 2, position: 1, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchOne.Id, tomek.Id, totalPoints: 6, exact: 2, outcome: 2, predictions: 1, position: 1, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, tomek.Id, totalPoints: 7, exact: 2, outcome: 3, predictions: 2, position: 2, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, inactive.Id, totalPoints: 99, exact: 33, outcome: 33, predictions: 2, position: 1, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var summary = await service.GetFinalSummaryAsync(marek.Id);

        summary.Stats.SettledMatchesCount.Should().Be(2);
        summary.Stats.ActivePlayersCount.Should().Be(2);
        summary.Stats.FinalLeaderUserId.Should().Be(marek.Id);
        summary.PositionSeries.Select(series => series.DisplayName).Should().Equal("Marek", "Tomek");
        summary.PositionSeries.Single(series => series.UserId == marek.Id).IsCurrentUser.Should().BeTrue();
        summary.PositionSeries.Single(series => series.UserId == marek.Id).Points.Select(point => point.Position).Should().Equal(2, 1);
        summary.FinalTop.Select(entry => entry.DisplayName).Should().Equal("Marek", "Tomek");
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
        summary.GlobalFacts.Should().Contain(fact => fact.Id == "draw-specialist");
        summary.GlobalFacts.Count.Should().BeGreaterThanOrEqualTo(4);
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FinalSummaryServiceTests
```

Expected: compile failure because `FinalSummaryService` does not exist.

- [ ] **Step 3: Implement public summary service and DI**

Create `backend/WorldCupTyper.Application/Services/FinalSummaryService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Application.Services;

public sealed class FinalSummaryService : IFinalSummaryService
{
    private readonly IAppDbContext _dbContext;

    public FinalSummaryService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FinalSummaryResponseDto> GetFinalSummaryAsync(Guid? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var series = await BuildPositionSeriesAsync(currentUserId, cancellationToken);
        var finalTop = await BuildFinalTopAsync(series, currentUserId, cancellationToken);
        var leader = finalTop.OrderBy(entry => entry.FinalPosition).FirstOrDefault();
        var settledMatchesCount = await _dbContext.Matches
            .AsNoTracking()
            .CountAsync(match => match.IsSettled, cancellationToken);
        var activePlayersCount = await _dbContext.Users
            .AsNoTracking()
            .CountAsync(user => user.IsActive, cancellationToken);
        var facts = await BuildGlobalFactsAsync(series, cancellationToken);

        return new FinalSummaryResponseDto(
            new FinalSummaryStatsDto(settledMatchesCount, activePlayersCount, leader?.UserId, leader?.DisplayName),
            series,
            finalTop.Take(5).ToList(),
            facts);
    }

    public async Task<PersonalFinalSummaryResponseDto> GetPersonalFinalSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var finalTop = await BuildFinalTopAsync(await BuildPositionSeriesAsync(userId, cancellationToken), userId, cancellationToken);
        var entry = finalTop.FirstOrDefault(candidate => candidate.UserId == userId);
        if (entry is null)
        {
            throw new NotFoundException("Nie znaleziono podsumowania zawodnika.");
        }

        return new PersonalFinalSummaryResponseDto(
            entry.UserId,
            entry.DisplayName,
            entry.AvatarUrl,
            entry.FinalPosition,
            entry.TotalPoints,
            entry.ExactScoreHits,
            entry.CorrectOutcomeHits,
            entry.PredictionsCount,
            Array.Empty<FinalSummaryFactDto>(),
            Array.Empty<Guid>());
    }

    private async Task<List<FinalRankingPositionSeriesDto>> BuildPositionSeriesAsync(Guid? currentUserId, CancellationToken cancellationToken)
    {
        var snapshots = await _dbContext.LeaderboardSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.User.IsActive)
            .Include(snapshot => snapshot.User)
            .Include(snapshot => snapshot.Match)
            .ThenInclude(match => match.HomeTeam)
            .Include(snapshot => snapshot.Match)
            .ThenInclude(match => match.AwayTeam)
            .OrderBy(snapshot => snapshot.Match.KickoffTimeUtc)
            .ThenBy(snapshot => snapshot.CreatedAtUtc)
            .ThenBy(snapshot => snapshot.Match.MatchNumber)
            .ThenBy(snapshot => snapshot.Position)
            .ToListAsync(cancellationToken);

        return snapshots
            .GroupBy(snapshot => snapshot.UserId)
            .Select(group =>
            {
                var ordered = group
                    .OrderBy(snapshot => snapshot.Match.KickoffTimeUtc)
                    .ThenBy(snapshot => snapshot.CreatedAtUtc)
                    .ThenBy(snapshot => snapshot.Match.MatchNumber)
                    .ToList();
                var latest = ordered.Last();
                return new FinalRankingPositionSeriesDto(
                    latest.UserId,
                    latest.User.DisplayName,
                    latest.User.AvatarUrl,
                    latest.Position,
                    latest.TotalPoints,
                    currentUserId.HasValue && currentUserId.Value == latest.UserId,
                    ordered.Select(snapshot => new FinalRankingPositionPointDto(
                        snapshot.MatchId,
                        snapshot.Match.MatchNumber,
                        BuildMatchLabel(snapshot),
                        snapshot.CreatedAtUtc,
                        snapshot.Position,
                        snapshot.TotalPoints))
                        .ToList());
            })
            .OrderBy(series => series.FinalPosition)
            .ThenBy(series => series.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<List<FinalRankingEntryDto>> BuildFinalTopAsync(IReadOnlyCollection<FinalRankingPositionSeriesDto> series, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var userIds = series.Select(item => item.UserId).ToList();
        var predictionStats = await _dbContext.Predictions
            .AsNoTracking()
            .Where(prediction => userIds.Contains(prediction.UserId) && prediction.Match.IsSettled && prediction.Result != null)
            .GroupBy(prediction => prediction.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                ExactScoreHits = group.Count(prediction => prediction.Result!.IsExactScore),
                CorrectOutcomeHits = group.Count(prediction => prediction.Result!.IsCorrectOutcome),
                PredictionsCount = group.Count(),
            })
            .ToListAsync(cancellationToken);

        var statsByUser = predictionStats.ToDictionary(item => item.UserId);

        return series
            .OrderBy(item => item.FinalPosition)
            .Select(item =>
            {
                statsByUser.TryGetValue(item.UserId, out var stats);
                return new FinalRankingEntryDto(
                    item.UserId,
                    item.DisplayName,
                    item.AvatarUrl,
                    item.FinalPosition,
                    item.FinalPoints,
                    stats?.ExactScoreHits ?? 0,
                    stats?.CorrectOutcomeHits ?? 0,
                    stats?.PredictionsCount ?? 0,
                    currentUserId.HasValue && currentUserId.Value == item.UserId);
            })
            .ToList();
    }

    private async Task<List<FinalSummaryFactDto>> BuildGlobalFactsAsync(IReadOnlyCollection<FinalRankingPositionSeriesDto> series, CancellationToken cancellationToken)
    {
        var facts = new List<FinalSummaryFactDto>();
        AddMovementFacts(facts, series);
        await AddPredictionFactsAsync(facts, cancellationToken);
        return facts.Take(12).ToList();
    }

    private static void AddMovementFacts(List<FinalSummaryFactDto> facts, IReadOnlyCollection<FinalRankingPositionSeriesDto> series)
    {
        var movements = series
            .Select(item =>
            {
                var ordered = item.Points.OrderBy(point => point.SnapshotAtUtc).ThenBy(point => point.MatchNumber).ToList();
                var first = ordered.FirstOrDefault();
                var last = ordered.LastOrDefault();
                return new { Series = item, First = first, Last = last, Climb = first is null || last is null ? 0 : first.Position - last.Position, Drop = first is null || last is null ? 0 : last.Position - first.Position };
            })
            .ToList();

        var biggestClimb = movements.OrderByDescending(item => item.Climb).FirstOrDefault(item => item.Climb > 0);
        if (biggestClimb is not null)
        {
            facts.Add(new FinalSummaryFactDto(
                "biggest-climb",
                "Najwiekszy skok",
                $"{biggestClimb.Series.DisplayName}: +{biggestClimb.Climb} miejsc",
                $"Od #{biggestClimb.First!.Position} do #{biggestClimb.Last!.Position} w finalnej historii tabeli.",
                new[] { biggestClimb.Series.UserId },
                Array.Empty<Guid>()));
        }

        var biggestDrop = movements.OrderByDescending(item => item.Drop).FirstOrDefault(item => item.Drop > 0);
        if (biggestDrop is not null)
        {
            facts.Add(new FinalSummaryFactDto(
                "biggest-drop",
                "Najwiekszy spadek",
                $"{biggestDrop.Series.DisplayName}: -{biggestDrop.Drop} miejsc",
                $"Linia zaczela przy #{biggestDrop.First!.Position}, a skonczyla przy #{biggestDrop.Last!.Position}.",
                new[] { biggestDrop.Series.UserId },
                Array.Empty<Guid>()));
        }
    }

    private async Task AddPredictionFactsAsync(List<FinalSummaryFactDto> facts, CancellationToken cancellationToken)
    {
        var predictions = await _dbContext.Predictions
            .AsNoTracking()
            .Where(prediction => prediction.Match.IsSettled && prediction.Result != null)
            .Include(prediction => prediction.Match)
            .ThenInclude(match => match.HomeTeam)
            .Include(prediction => prediction.Match)
            .ThenInclude(match => match.AwayTeam)
            .Include(prediction => prediction.User)
            .ToListAsync(cancellationToken);

        var matchPredictionStats = predictions
            .GroupBy(prediction => prediction.MatchId)
            .Select(group => new
            {
                Match = group.First().Match,
                ExactCount = group.Count(prediction => prediction.Result!.IsExactScore),
                CorrectOutcomeCount = group.Count(prediction => prediction.Result!.IsCorrectOutcome),
            })
            .ToList();

        var mostExact = matchPredictionStats
            .OrderByDescending(item => item.ExactCount)
            .ThenBy(item => item.Match.MatchNumber)
            .FirstOrDefault(item => item.ExactCount > 0);
        if (mostExact is not null)
        {
            facts.Add(new FinalSummaryFactDto(
                "most-exact-match",
                "Najbardziej trafiony mecz",
                $"{BuildMatchLabel(mostExact.Match)}: {mostExact.ExactCount} dokladnych",
                "To byl wspolny jackpot kolejki.",
                Array.Empty<Guid>(),
                new[] { mostExact.Match.Id }));
        }

        var drawSpecialist = predictions
            .Where(prediction => prediction.User.IsActive && prediction.Match.HomeScore90 == prediction.Match.AwayScore90 && prediction.Result!.IsCorrectOutcome)
            .GroupBy(prediction => prediction.UserId)
            .Select(group => new { User = group.First().User, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.User.DisplayName)
            .FirstOrDefault();
        if (drawSpecialist is not null)
        {
            facts.Add(new FinalSummaryFactDto(
                "draw-specialist",
                "Remisowy prorok",
                $"{drawSpecialist.User.DisplayName}: {drawSpecialist.Count} trafionych remisow",
                "Osobna chwala za czucie meczow na styku.",
                new[] { drawSpecialist.User.Id },
                Array.Empty<Guid>()));
        }
    }

    private static string BuildMatchLabel(WorldCupTyper.Domain.Entities.LeaderboardSnapshot snapshot)
    {
        return BuildMatchLabel(snapshot.Match);
    }

    private static string BuildMatchLabel(WorldCupTyper.Domain.Entities.Match match)
    {
        var home = match.HomeTeam.ShortName.Trim();
        var away = match.AwayTeam.ShortName.Trim();
        return string.IsNullOrWhiteSpace(home) || string.IsNullOrWhiteSpace(away)
            ? $"M{match.MatchNumber}"
            : $"{home}-{away}";
    }
}
```

Modify `backend/WorldCupTyper.Infrastructure/ServiceCollectionExtensions.cs` by adding this registration near the other application services:

```csharp
services.AddScoped<IFinalSummaryService, FinalSummaryService>();
```

- [ ] **Step 4: Run public summary tests**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FinalSummaryServiceTests
```

Expected: pass.

- [ ] **Step 5: Commit public service**

Run:

```powershell
git add backend\WorldCupTyper.Application\Services\FinalSummaryService.cs backend\WorldCupTyper.Infrastructure\ServiceCollectionExtensions.cs backend\WorldCupTyper.Tests\FinalSummaryServiceTests.cs
git commit -m "feat: build final summary data"
```

Expected: commit includes only service, DI registration, and tests.

### Task 3: Expand Global Facts And Personal Recap

**Files:**
- Modify: `backend/WorldCupTyper.Application/Services/FinalSummaryService.cs`
- Modify: `backend/WorldCupTyper.Tests/FinalSummaryServiceTests.cs`

- [ ] **Step 1: Add failing personal recap and expanded fact tests**

Append these tests to `backend/WorldCupTyper.Tests/FinalSummaryServiceTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter "FinalSummaryServiceTests"
```

Expected: personal recap facts are empty and expanded global fact IDs are missing.

- [ ] **Step 3: Add expanded facts and personal recap builders**

Modify `FinalSummaryService` by adding these helper methods and calling them:

```csharp
private async Task<List<FinalSummaryFactDto>> BuildPersonalFactsAsync(Guid userId, CancellationToken cancellationToken)
{
    var facts = new List<FinalSummaryFactDto>();
    var series = (await BuildPositionSeriesAsync(userId, cancellationToken)).FirstOrDefault(item => item.UserId == userId);
    if (series is not null)
    {
        AddPersonalMovementFacts(facts, series);
    }

    await AddPersonalPredictionFactsAsync(facts, userId, cancellationToken);
    return facts
        .GroupBy(fact => fact.Id)
        .Select(group => group.First())
        .Take(5)
        .ToList();
}

private static void AddPersonalMovementFacts(List<FinalSummaryFactDto> facts, FinalRankingPositionSeriesDto series)
{
    var points = series.Points.OrderBy(point => point.SnapshotAtUtc).ThenBy(point => point.MatchNumber).ToList();
    if (points.Count == 0)
    {
        return;
    }

    var first = points.First();
    var last = points.Last();
    if (first.Position > last.Position)
    {
        facts.Add(new FinalSummaryFactDto(
            "personal-biggest-climb",
            "Twoj skok",
            $"+{first.Position - last.Position} miejsc w tabeli",
            $"Zaczynales przy #{first.Position}, a finalnie skonczyles przy #{last.Position}.",
            new[] { series.UserId },
            Array.Empty<Guid>()));
    }

    facts.Add(new FinalSummaryFactDto(
        "personal-final-rank",
        "Final",
        $"#{series.FinalPosition} i {series.FinalPoints} pkt",
        "To Twoja koncowa karta turnieju.",
        new[] { series.UserId },
        Array.Empty<Guid>()));
}

private async Task AddPersonalPredictionFactsAsync(List<FinalSummaryFactDto> facts, Guid userId, CancellationToken cancellationToken)
{
    var predictions = await _dbContext.Predictions
        .AsNoTracking()
        .Where(prediction => prediction.UserId == userId && prediction.Match.IsSettled && prediction.Result != null)
        .Include(prediction => prediction.Match)
        .ThenInclude(match => match.HomeTeam)
        .Include(prediction => prediction.Match)
        .ThenInclude(match => match.AwayTeam)
        .OrderBy(prediction => prediction.Match.KickoffTimeUtc)
        .ThenBy(prediction => prediction.Match.MatchNumber)
        .ToListAsync(cancellationToken);

    var bestPrediction = predictions
        .OrderByDescending(prediction => prediction.Result!.Points)
        .ThenBy(prediction => prediction.Match.MatchNumber)
        .FirstOrDefault();
    if (bestPrediction is not null)
    {
        facts.Add(new FinalSummaryFactDto(
            "personal-best-match",
            "Najlepszy moment",
            $"{BuildMatchLabel(bestPrediction.Match)}: {bestPrediction.Result!.Points} pkt",
            $"Typ {bestPrediction.PredictedHomeScore}:{bestPrediction.PredictedAwayScore} dal Twoj najlepszy pojedynczy wynik.",
            new[] { userId },
            new[] { bestPrediction.MatchId }));
    }

    var favoriteScoreline = predictions
        .GroupBy(prediction => $"{prediction.PredictedHomeScore}:{prediction.PredictedAwayScore}")
        .Select(group => new { Scoreline = group.Key, Count = group.Count() })
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Scoreline)
        .FirstOrDefault();
    if (favoriteScoreline is not null)
    {
        facts.Add(new FinalSummaryFactDto(
            "personal-favorite-scoreline",
            "Twoj podpis",
            $"{favoriteScoreline.Scoreline} typowane {favoriteScoreline.Count} razy",
            "Kazdy typer ma swoj ulubiony wynik.",
            new[] { userId },
            Array.Empty<Guid>()));
    }
}
```

Change `GetPersonalFinalSummaryAsync` so `PersonalFacts` and `HighlightedMatchIds` come from the facts:

```csharp
var personalFacts = await BuildPersonalFactsAsync(userId, cancellationToken);
return new PersonalFinalSummaryResponseDto(
    entry.UserId,
    entry.DisplayName,
    entry.AvatarUrl,
    entry.FinalPosition,
    entry.TotalPoints,
    entry.ExactScoreHits,
    entry.CorrectOutcomeHits,
    entry.PredictionsCount,
    personalFacts,
    personalFacts.SelectMany(fact => fact.RelatedMatchIds).Distinct().ToList());
```

Add global helpers and call them from `BuildGlobalFactsAsync` after `AddPredictionFactsAsync`:

```csharp
AddFinishFact(facts, series);
await AddScorelineAndConsistencyFactsAsync(facts, cancellationToken);
```

```csharp
private static void AddFinishFact(List<FinalSummaryFactDto> facts, IReadOnlyCollection<FinalRankingPositionSeriesDto> series)
{
    var strongestFinish = series
        .Select(item =>
        {
            var points = item.Points.OrderBy(point => point.SnapshotAtUtc).ThenBy(point => point.MatchNumber).ToList();
            var halfway = points.Count / 2;
            var secondHalfGain = points.Count == 0 ? 0 : points.Last().TotalPoints - points[Math.Max(0, halfway - 1)].TotalPoints;
            return new { Series = item, Gain = secondHalfGain };
        })
        .OrderByDescending(item => item.Gain)
        .FirstOrDefault(item => item.Gain > 0);

    if (strongestFinish is not null)
    {
        facts.Add(new FinalSummaryFactDto(
            "strongest-finish",
            "Finisz turnieju",
            $"{strongestFinish.Series.DisplayName}: +{strongestFinish.Gain} pkt w drugiej polowie",
            "Bohater koncowki, nawet jesli nie zawsze byl na podium.",
            new[] { strongestFinish.Series.UserId },
            Array.Empty<Guid>()));
    }
}

private async Task AddScorelineAndConsistencyFactsAsync(List<FinalSummaryFactDto> facts, CancellationToken cancellationToken)
{
    var scoreline = await _dbContext.Predictions
        .AsNoTracking()
        .Where(prediction => prediction.Match.IsSettled)
        .GroupBy(prediction => $"{prediction.PredictedHomeScore}:{prediction.PredictedAwayScore}")
        .Select(group => new { Scoreline = group.Key, Count = group.Count() })
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Scoreline)
        .FirstOrDefaultAsync(cancellationToken);
    if (scoreline is not null)
    {
        facts.Add(new FinalSummaryFactDto(
            "scoreline-magnet",
            "Wynik-magnes",
            $"{scoreline.Scoreline} typowane {scoreline.Count} razy",
            "Najbardziej popularny wzorzec turnieju.",
            Array.Empty<Guid>(),
            Array.Empty<Guid>()));
    }

    var consistent = await _dbContext.Predictions
        .AsNoTracking()
        .Where(prediction => prediction.User.IsActive && prediction.Match.IsSettled)
        .GroupBy(prediction => new { prediction.UserId, prediction.User.DisplayName })
        .Select(group => new { group.Key.UserId, group.Key.DisplayName, Count = group.Count() })
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.DisplayName)
        .FirstOrDefaultAsync(cancellationToken);
    if (consistent is not null)
    {
        facts.Add(new FinalSummaryFactDto(
            "most-consistent",
            "Stabilny typer",
            $"{consistent.DisplayName}: {consistent.Count} typow",
            "Regularnosc tez zasluguje na medal.",
            new[] { consistent.UserId },
            Array.Empty<Guid>()));
    }

    var oneGoalAway = await _dbContext.Predictions
        .AsNoTracking()
        .Where(prediction => prediction.User.IsActive && prediction.Match.IsSettled && prediction.Result != null && !prediction.Result.IsExactScore)
        .GroupBy(prediction => new { prediction.UserId, prediction.User.DisplayName })
        .Select(group => new { group.Key.UserId, group.Key.DisplayName, Count = group.Count() })
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.DisplayName)
        .FirstOrDefaultAsync(cancellationToken);
    if (oneGoalAway is not null)
    {
        facts.Add(new FinalSummaryFactDto(
            "one-goal-away",
            "Prawie dokladnie",
            $"{oneGoalAway.DisplayName}: {oneGoalAway.Count} bliskich typow",
            "Kategoria pecha opowiedziana lekko, bez zawstydzania.",
            new[] { oneGoalAway.UserId },
            Array.Empty<Guid>()));
    }
}
```

- [ ] **Step 4: Run expanded tests**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter "FinalSummaryServiceTests"
```

Expected: pass.

- [ ] **Step 5: Commit expanded facts**

Run:

```powershell
git add backend\WorldCupTyper.Application\Services\FinalSummaryService.cs backend\WorldCupTyper.Tests\FinalSummaryServiceTests.cs
git commit -m "feat: add final recap facts"
```

Expected: commit contains service and test updates only.

### Task 4: Add Frontend API Types And Public Page Route

**Files:**
- Modify: `frontend/src/api/types.ts`
- Modify: `frontend/src/api/services.ts`
- Create: `frontend/src/features/summary/FinalSummaryFactGrid.tsx`
- Create: `frontend/src/features/summary/FinalSummaryPage.tsx`
- Modify: `frontend/src/routes/AppRouter.tsx`
- Modify: `frontend/e2e/smoke.spec.ts`

- [ ] **Step 1: Write failing Playwright smoke for public final summary**

In `frontend/e2e/smoke.spec.ts`, replace the public home test with this test:

```ts
test('publiczny home pokazuje finalne podsumowanie turnieju', async ({ page }) => {
  test.skip(!isLocalPreview, 'Publiczny final summary z mockowanym API sprawdzamy lokalnie')

  await page.route('**/api/summary/final', async (route) =>
    route.fulfill({
      json: {
        stats: {
          settledMatchesCount: 76,
          activePlayersCount: 24,
          finalLeaderUserId: 'user-1',
          finalLeaderDisplayName: 'Marek',
        },
        positionSeries: [
          {
            userId: 'user-1',
            displayName: 'Marek',
            avatarUrl: null,
            finalPosition: 1,
            finalPoints: 121,
            isCurrentUser: false,
            points: [
              { matchId: 'match-1', matchNumber: 1, matchLabel: 'POL-GER', snapshotAtUtc: '2026-06-11T20:00:00Z', position: 2, totalPoints: 3 },
              { matchId: 'match-2', matchNumber: 2, matchLabel: 'FRA-ESP', snapshotAtUtc: '2026-06-12T20:00:00Z', position: 1, totalPoints: 6 },
            ],
          },
          {
            userId: 'user-2',
            displayName: 'Tomek',
            avatarUrl: null,
            finalPosition: 2,
            finalPoints: 117,
            isCurrentUser: false,
            points: [
              { matchId: 'match-1', matchNumber: 1, matchLabel: 'POL-GER', snapshotAtUtc: '2026-06-11T20:00:00Z', position: 1, totalPoints: 3 },
              { matchId: 'match-2', matchNumber: 2, matchLabel: 'FRA-ESP', snapshotAtUtc: '2026-06-12T20:00:00Z', position: 2, totalPoints: 4 },
            ],
          },
        ],
        finalTop: [
          { userId: 'user-1', displayName: 'Marek', avatarUrl: null, finalPosition: 1, totalPoints: 121, exactScoreHits: 24, correctOutcomeHits: 73, predictionsCount: 104, isCurrentUser: false },
          { userId: 'user-2', displayName: 'Tomek', avatarUrl: null, finalPosition: 2, totalPoints: 117, exactScoreHits: 22, correctOutcomeHits: 71, predictionsCount: 104, isCurrentUser: false },
        ],
        globalFacts: [
          { id: 'biggest-climb', label: 'Najwiekszy skok', title: 'Marek: +7 miejsc', description: 'Najmocniejszy ruch tabeli.', relatedUserIds: ['user-1'], relatedMatchIds: [] },
          { id: 'most-exact-match', label: 'Najbardziej trafiony mecz', title: 'POL-GER: 8 dokladnych', description: 'Wspolny jackpot kolejki.', relatedUserIds: [], relatedMatchIds: ['match-1'] },
        ],
      },
    }),
  )

  await page.goto('/')

  await expect(page.getByRole('heading', { name: 'Cala tabela, mecz po meczu' })).toBeVisible()
  await expect(page.getByText('Animowana pelna tabela')).toBeVisible()
  await expect(page.getByText('Najwiekszy skok')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Zaloguj sie po swoj recap' })).toHaveAttribute('href', '/login')
})
```

- [ ] **Step 2: Run smoke test to verify it fails**

Run:

```powershell
cd frontend
npm run test:e2e:smoke -- --grep "publiczny home pokazuje finalne podsumowanie turnieju"
```

Expected: fail because `/api/summary/final` is not called and the old public home renders.

- [ ] **Step 3: Add API types and service**

Append to `frontend/src/api/types.ts`:

```ts
export interface FinalSummaryStats {
  settledMatchesCount: number
  activePlayersCount: number
  finalLeaderUserId?: string | null
  finalLeaderDisplayName?: string | null
}

export interface FinalRankingPositionPoint {
  matchId: string
  matchNumber: number
  matchLabel: string
  snapshotAtUtc: string
  position: number
  totalPoints: number
}

export interface FinalRankingPositionSeries {
  userId: string
  displayName: string
  avatarUrl?: string | null
  finalPosition: number
  finalPoints: number
  isCurrentUser: boolean
  points: FinalRankingPositionPoint[]
}

export interface FinalRankingEntry {
  userId: string
  displayName: string
  avatarUrl?: string | null
  finalPosition: number
  totalPoints: number
  exactScoreHits: number
  correctOutcomeHits: number
  predictionsCount: number
  isCurrentUser: boolean
}

export interface FinalSummaryFact {
  id: string
  label: string
  title: string
  description: string
  relatedUserIds: string[]
  relatedMatchIds: string[]
}

export interface FinalSummaryResponse {
  stats: FinalSummaryStats
  positionSeries: FinalRankingPositionSeries[]
  finalTop: FinalRankingEntry[]
  globalFacts: FinalSummaryFact[]
}

export interface PersonalFinalSummaryResponse {
  userId: string
  displayName: string
  avatarUrl?: string | null
  finalPosition: number
  totalPoints: number
  exactScoreHits: number
  correctOutcomeHits: number
  predictionsCount: number
  personalFacts: FinalSummaryFact[]
  highlightedMatchIds: string[]
}
```

Update imports in `frontend/src/api/services.ts` to include `FinalSummaryResponse` and `PersonalFinalSummaryResponse`, then add:

```ts
export const summaryApi = {
  getFinal: async () => (await apiClient.get<FinalSummaryResponse>('/summary/final')).data,
  getMine: async () => (await apiClient.get<PersonalFinalSummaryResponse>('/summary/final/me')).data,
}
```

- [ ] **Step 4: Add public page and fact grid**

Create `frontend/src/features/summary/FinalSummaryFactGrid.tsx`:

```tsx
import type { FinalSummaryFact } from '../../api/types'

export const FinalSummaryFactGrid = ({ facts }: { facts: FinalSummaryFact[] }) => {
  return (
    <section className="border-t border-white/10 px-4 py-8 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-7xl">
        <p className="font-display text-sm uppercase tracking-[0.28em] text-emerald-300">Ciekawostki</p>
        <h2 className="mt-2 font-display text-3xl uppercase text-white sm:text-4xl">Historie z turnieju</h2>
        <div className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {facts.map((fact) => (
            <article key={fact.id} className="rounded-3xl border border-white/10 bg-slate-950/55 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-emerald-300">{fact.label}</p>
              <p className="mt-3 font-display text-xl uppercase text-white">{fact.title}</p>
              <p className="mt-2 text-sm leading-6 text-slate-300">{fact.description}</p>
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}
```

Create `frontend/src/features/summary/FinalSummaryPage.tsx`:

```tsx
import { useQuery } from '@tanstack/react-query'
import { ArrowRight, RotateCcw, Sparkles } from 'lucide-react'
import { Link } from 'react-router-dom'
import { getErrorMessage } from '../../api/client'
import { summaryApi } from '../../api/services'
import { buttonClassName, secondaryButtonClassName } from '../../styles/ui'
import { FinalSummaryFactGrid } from './FinalSummaryFactGrid'

export const FinalSummaryPage = () => {
  const summaryQuery = useQuery({ queryKey: ['summary', 'final'], queryFn: summaryApi.getFinal })
  const summary = summaryQuery.data

  return (
    <main className="min-h-screen overflow-hidden bg-pitch-950 text-slate-100">
      <section className="relative isolate overflow-hidden">
        <div className="absolute inset-0 -z-20 bg-stadium opacity-95" />
        <div className="absolute inset-x-0 bottom-0 -z-10 h-36 bg-gradient-to-t from-pitch-950 to-transparent" />
        <div className="mx-auto flex max-w-7xl flex-col px-4 py-5 sm:px-6 lg:px-8">
          <header className="flex items-center justify-between gap-4 border-b border-white/10 pb-5">
            <Link to="/" className="font-display text-base font-bold uppercase tracking-[0.22em] text-white sm:text-xl">
              Typer MS
            </Link>
            <Link to="/login" className={secondaryButtonClassName}>
              Logowanie
            </Link>
          </header>
          <div className="grid gap-8 py-8 lg:grid-cols-[minmax(0,0.82fr)_minmax(25rem,1.18fr)] lg:items-start">
            <div>
              <p className="font-display text-sm uppercase tracking-[0.3em] text-emerald-300">Koniec turnieju 2026</p>
              <h1 className="mt-4 font-display text-5xl font-bold uppercase leading-none text-white sm:text-6xl">
                Cala tabela, mecz po meczu
              </h1>
              <p className="mt-5 max-w-xl text-lg leading-8 text-slate-300">
                Finalne podsumowanie rywalizacji: animowana historia miejsc, ciekawostki i wejscie po personalny recap.
              </p>
              <div className="mt-7 flex flex-col gap-3 sm:flex-row">
                <a href="#animated-table" className={buttonClassName}>
                  Odtworz animacje tabeli
                  <RotateCcw className="ml-2 h-4 w-4" aria-hidden="true" />
                </a>
                <Link to="/login" className={secondaryButtonClassName}>
                  Zaloguj sie po swoj recap
                  <ArrowRight className="ml-2 h-4 w-4" aria-hidden="true" />
                </Link>
              </div>
              <div className="mt-6 grid gap-3 sm:grid-cols-2">
                <div className="glass-card rounded-3xl p-4">
                  <p className="text-xs uppercase tracking-[0.24em] text-slate-400">Mecze</p>
                  <p className="mt-2 font-display text-3xl text-white">{summary?.stats.settledMatchesCount ?? '-'}</p>
                </div>
                <div className="glass-card rounded-3xl p-4">
                  <p className="text-xs uppercase tracking-[0.24em] text-slate-400">Gracze</p>
                  <p className="mt-2 font-display text-3xl text-white">{summary?.stats.activePlayersCount ?? '-'}</p>
                </div>
              </div>
            </div>
            <section id="animated-table" className="glass-card rounded-[2rem] p-5">
              <p className="font-display text-sm uppercase tracking-[0.24em] text-emerald-300">Animowana pelna tabela</p>
              <p className="mt-3 text-sm text-slate-400">
                {summaryQuery.isLoading
                  ? 'Ladowanie historii tabeli...'
                  : summaryQuery.isError
                    ? getErrorMessage(summaryQuery.error)
                    : 'Wykres miejsc pojawi sie tutaj w kolejnym kroku.'}
              </p>
              <div className="mt-5 flex min-h-80 items-center justify-center rounded-3xl border border-white/10 bg-slate-950/50">
                <Sparkles className="h-8 w-8 text-emerald-300" aria-hidden="true" />
              </div>
            </section>
          </div>
        </div>
      </section>
      {summary ? <FinalSummaryFactGrid facts={summary.globalFacts} /> : null}
    </main>
  )
}
```

Modify `frontend/src/routes/AppRouter.tsx`:

```tsx
import { FinalSummaryPage } from '../features/summary/FinalSummaryPage'
```

Then change the logged-out branch in `RootRoute`:

```tsx
if (!isAuthenticated) {
  return <FinalSummaryPage />
}
```

- [ ] **Step 5: Run smoke test**

Run:

```powershell
cd frontend
npm run test:e2e:smoke -- --grep "publiczny home pokazuje finalne podsumowanie turnieju"
```

Expected: pass.

- [ ] **Step 6: Commit public page shell**

Run:

```powershell
git add frontend\src\api\types.ts frontend\src\api\services.ts frontend\src\features\summary\FinalSummaryFactGrid.tsx frontend\src\features\summary\FinalSummaryPage.tsx frontend\src\routes\AppRouter.tsx frontend\e2e\smoke.spec.ts
git commit -m "feat: add final summary page shell"
```

Expected: commit includes only frontend API, page shell, route, and smoke test changes.

### Task 5: Implement Animated Full-Table Chart And Filters

**Files:**
- Create: `frontend/src/features/summary/summaryChart.ts`
- Create: `frontend/src/features/summary/FinalRankingStoryChart.tsx`
- Modify: `frontend/src/features/summary/FinalSummaryPage.tsx`
- Modify: `frontend/e2e/smoke.spec.ts`

- [ ] **Step 1: Add failing smoke assertions for chart filters**

Append these assertions to the final summary smoke test after the heading assertion:

```ts
await expect(page.getByRole('button', { name: 'Podium' })).toBeVisible()
await expect(page.getByRole('button', { name: 'Wybrani zawodnicy' })).toBeVisible()
await page.getByRole('button', { name: 'Tomek' }).click()
await expect(page.getByRole('button', { name: 'Tomek' })).toHaveAttribute('aria-pressed', 'true')
await expect(page.locator('[data-testid="final-ranking-story-chart"]')).toBeVisible()
await expect(page.locator('[data-testid="static-final-ranking-table"]')).toHaveCount(0)
```

- [ ] **Step 2: Run smoke test to verify it fails**

Run:

```powershell
cd frontend
npm run test:e2e:smoke -- --grep "publiczny home pokazuje finalne podsumowanie turnieju"
```

Expected: fail because chart buttons and `data-testid="final-ranking-story-chart"` are missing.

- [ ] **Step 3: Add chart helpers**

Create `frontend/src/features/summary/summaryChart.ts`:

```ts
import type { FinalRankingPositionSeries } from '../../api/types'

export const finalChartColors = [
  '#32d583',
  '#38bdf8',
  '#fb923c',
  '#f472b6',
  '#a78bfa',
  '#facc15',
  '#22d3ee',
  '#34d399',
  '#fb7185',
  '#818cf8',
  '#14b8a6',
  '#f97316',
  '#84cc16',
  '#60a5fa',
  '#e879f9',
  '#c084fc',
  '#2dd4bf',
  '#f59e0b',
  '#4ade80',
  '#f87171',
]

export type FinalChartRow = {
  matchId: string
  matchNumber: number
  matchLabel: string
  [userId: string]: string | number | null
}

export const buildFinalChartRows = (series: FinalRankingPositionSeries[]) => {
  const rowsByMatch = new Map<string, FinalChartRow>()

  for (const player of series) {
    for (const point of player.points) {
      const existing = rowsByMatch.get(point.matchId)
      const row = existing ?? {
        matchId: point.matchId,
        matchNumber: point.matchNumber,
        matchLabel: point.matchLabel,
      }

      row[player.userId] = point.position
      rowsByMatch.set(point.matchId, row)
    }
  }

  return [...rowsByMatch.values()].sort((first, second) => first.matchNumber - second.matchNumber)
}

export const getBiggestClimbUserIds = (series: FinalRankingPositionSeries[]) => {
  const ranked = series
    .map((player) => {
      const ordered = [...player.points].sort((first, second) => first.matchNumber - second.matchNumber)
      const first = ordered[0]
      const last = ordered.at(-1)
      return { userId: player.userId, movement: first && last ? first.position - last.position : 0 }
    })
    .sort((first, second) => second.movement - first.movement)

  return ranked.filter((entry) => entry.movement > 0).slice(0, 3).map((entry) => entry.userId)
}

export const getBiggestDropUserIds = (series: FinalRankingPositionSeries[]) => {
  const ranked = series
    .map((player) => {
      const ordered = [...player.points].sort((first, second) => first.matchNumber - second.matchNumber)
      const first = ordered[0]
      const last = ordered.at(-1)
      return { userId: player.userId, movement: first && last ? last.position - first.position : 0 }
    })
    .sort((first, second) => second.movement - first.movement)

  return ranked.filter((entry) => entry.movement > 0).slice(0, 3).map((entry) => entry.userId)
}
```

- [ ] **Step 4: Add `FinalRankingStoryChart`**

Create `frontend/src/features/summary/FinalRankingStoryChart.tsx`:

```tsx
import { useMemo, useState } from 'react'
import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { FinalRankingPositionSeries } from '../../api/types'
import { UserAvatar } from '../../components/UserAvatar'
import {
  buildFinalChartRows,
  finalChartColors,
  getBiggestClimbUserIds,
  getBiggestDropUserIds,
} from './summaryChart'

type FilterMode = 'all' | 'podium' | 'mine' | 'climb' | 'drop' | 'selected'

export const FinalRankingStoryChart = ({ series }: { series: FinalRankingPositionSeries[] }) => {
  const [filterMode, setFilterMode] = useState<FilterMode>('all')
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([])
  const chartRows = useMemo(() => buildFinalChartRows(series), [series])
  const biggestClimbUserIds = useMemo(() => getBiggestClimbUserIds(series), [series])
  const biggestDropUserIds = useMemo(() => getBiggestDropUserIds(series), [series])
  const currentUserId = series.find((player) => player.isCurrentUser)?.userId
  const podiumUserIds = series.filter((player) => player.finalPosition <= 3).map((player) => player.userId)

  const focusedUserIds = useMemo(() => {
    if (filterMode === 'podium') return podiumUserIds
    if (filterMode === 'mine') return currentUserId ? [currentUserId] : []
    if (filterMode === 'climb') return biggestClimbUserIds
    if (filterMode === 'drop') return biggestDropUserIds
    if (filterMode === 'selected') return selectedUserIds
    return []
  }, [biggestClimbUserIds, biggestDropUserIds, currentUserId, filterMode, podiumUserIds, selectedUserIds])

  const focusedSet = new Set(focusedUserIds)
  const hasFocus = focusedUserIds.length > 0
  const maxPosition = Math.max(1, ...series.map((player) => player.finalPosition), ...series.flatMap((player) => player.points.map((point) => point.position)))

  const toggleSelectedPlayer = (userId: string) => {
    setFilterMode('selected')
    setSelectedUserIds((current) =>
      current.includes(userId)
        ? current.filter((selectedUserId) => selectedUserId !== userId)
        : [...current, userId],
    )
  }

  return (
    <section data-testid="final-ranking-story-chart" className="glass-card rounded-[2rem] p-5">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <p className="font-display text-sm uppercase tracking-[0.24em] text-emerald-300">Animowana pelna tabela</p>
          <h2 className="mt-2 font-display text-3xl uppercase text-white">Pozycje po meczach</h2>
          <p className="mt-2 text-sm text-slate-400">#1 jest u gory. Filtry wzmacniaja wybrane linie, a reszta zostaje jako kontekst.</p>
        </div>
        <button type="button" className="rounded-full border border-emerald-300/40 bg-emerald-300/10 px-4 py-2 text-sm font-semibold text-emerald-100">
          Odtworz
        </button>
      </div>

      <div className="mt-5 h-[28rem] overflow-x-auto">
        <div className="h-full min-w-[44rem]">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={chartRows} margin={{ top: 28, right: 36, bottom: 72, left: 8 }}>
              <CartesianGrid stroke="rgba(148, 163, 184, 0.16)" strokeDasharray="4 4" />
              <XAxis dataKey="matchLabel" angle={-58} height={88} interval="preserveStartEnd" tick={{ fill: 'rgb(148 163 184)', fontSize: 11 }} />
              <YAxis reversed allowDecimals={false} width={42} domain={[1, maxPosition]} tick={{ fill: 'rgb(148 163 184)', fontSize: 12 }} />
              <Tooltip />
              {series.map((player, index) => {
                const isFocused = focusedSet.has(player.userId)
                const isDimmed = hasFocus && !isFocused
                return (
                  <Line
                    key={player.userId}
                    type="monotone"
                    dataKey={player.userId}
                    name={player.displayName}
                    stroke={finalChartColors[index % finalChartColors.length]}
                    strokeWidth={isFocused ? 4 : player.finalPosition <= 3 ? 3 : 1.4}
                    strokeOpacity={isDimmed ? 0.18 : player.finalPosition <= 3 ? 0.96 : 0.42}
                    dot={false}
                    activeDot={{ r: 5 }}
                    connectNulls
                    isAnimationActive
                    animationDuration={2200}
                  />
                )
              })}
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="mt-4 flex flex-wrap gap-2">
        {[
          ['all', 'Wszyscy'],
          ['podium', 'Podium'],
          ['mine', 'Moj przebieg'],
          ['climb', 'Najwiekszy skok'],
          ['drop', 'Najwiekszy spadek'],
          ['selected', 'Wybrani zawodnicy'],
        ].map(([mode, label]) => (
          <button
            key={mode}
            type="button"
            aria-pressed={filterMode === mode}
            className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
              filterMode === mode ? 'bg-emerald-400 text-slate-950' : 'bg-white/5 text-slate-300 hover:bg-white/10 hover:text-white'
            }`}
            onClick={() => setFilterMode(mode as FilterMode)}
          >
            {label}
          </button>
        ))}
      </div>

      <div className="mt-3 flex flex-wrap gap-2">
        {series.map((player) => {
          const isSelected = selectedUserIds.includes(player.userId)
          return (
            <button
              key={player.userId}
              type="button"
              aria-pressed={isSelected}
              className={`inline-flex max-w-full items-center gap-2 rounded-full border px-3 py-2 text-sm transition ${
                isSelected ? 'border-emerald-300/70 bg-emerald-300/12 text-white' : 'border-white/10 bg-white/5 text-slate-300 hover:border-white/25 hover:text-white'
              }`}
              onClick={() => toggleSelectedPlayer(player.userId)}
            >
              <UserAvatar displayName={player.displayName} avatarUrl={player.avatarUrl} size="sm" />
              <span className="truncate">{player.displayName}</span>
            </button>
          )
        })}
      </div>
    </section>
  )
}
```

- [ ] **Step 5: Use chart in `FinalSummaryPage`**

Update imports:

```tsx
import { FinalRankingStoryChart } from './FinalRankingStoryChart'
```

Replace the placeholder chart section with:

```tsx
{summary ? (
  <FinalRankingStoryChart series={summary.positionSeries} />
) : (
  <section id="animated-table" className="glass-card rounded-[2rem] p-5">
    <p className="font-display text-sm uppercase tracking-[0.24em] text-emerald-300">Animowana pelna tabela</p>
    <p className="mt-3 text-sm text-slate-400">
      {summaryQuery.isLoading ? 'Ladowanie historii tabeli...' : summaryQuery.isError ? getErrorMessage(summaryQuery.error) : 'Brak danych podsumowania.'}
    </p>
  </section>
)}
```

- [ ] **Step 6: Run smoke test and lint**

Run:

```powershell
cd frontend
npm run test:e2e:smoke -- --grep "publiczny home pokazuje finalne podsumowanie turnieju"
npm run lint
```

Expected: smoke passes, lint passes.

- [ ] **Step 7: Commit chart**

Run:

```powershell
git add frontend\src\features\summary\summaryChart.ts frontend\src\features\summary\FinalRankingStoryChart.tsx frontend\src\features\summary\FinalSummaryPage.tsx frontend\e2e\smoke.spec.ts
git commit -m "feat: animate final ranking table"
```

Expected: commit contains chart and test changes only.

### Task 6: Add Personal Recap Route And Navigation

**Files:**
- Create: `frontend/src/features/summary/PersonalRecapPage.tsx`
- Modify: `frontend/src/routes/AppRouter.tsx`
- Modify: `frontend/src/components/AppNavigation.tsx`
- Modify: `frontend/e2e/smoke.spec.ts`

- [ ] **Step 1: Add failing authenticated recap smoke**

Add this test to `frontend/e2e/smoke.spec.ts`:

```ts
test('zalogowany gracz widzi personalny recap', async ({ page }) => {
  test.skip(!isLocalPreview, 'Personalny recap z mockowanym API sprawdzamy lokalnie')

  await page.addInitScript(() => {
    window.localStorage.setItem('typer.auth.token', 'cached-token')
    window.localStorage.setItem(
      'typer.auth.user',
      JSON.stringify({
        id: 'player-1',
        email: 'player@test.local',
        displayName: 'Tomek',
        role: 'Player',
        isActive: true,
        requiresPasswordChange: false,
        avatarUrl: null,
      }),
    )
  })
  await page.route('**/api/auth/me', async (route) =>
    route.fulfill({
      json: {
        id: 'player-1',
        email: 'player@test.local',
        displayName: 'Tomek',
        role: 'Player',
        isActive: true,
        requiresPasswordChange: false,
        avatarUrl: null,
      },
    }),
  )
  await page.route('**/api/summary/final/me', async (route) =>
    route.fulfill({
      json: {
        userId: 'player-1',
        displayName: 'Tomek',
        avatarUrl: null,
        finalPosition: 2,
        totalPoints: 117,
        exactScoreHits: 22,
        correctOutcomeHits: 71,
        predictionsCount: 104,
        highlightedMatchIds: ['match-1'],
        personalFacts: [
          { id: 'personal-best-match', label: 'Najlepszy moment', title: 'POL-GER: 3 pkt', description: 'Twoj najlepszy pojedynczy typ.', relatedUserIds: ['player-1'], relatedMatchIds: ['match-1'] },
          { id: 'personal-favorite-scoreline', label: 'Twoj podpis', title: '2:1 typowane 12 razy', description: 'Ulubiony wynik turnieju.', relatedUserIds: ['player-1'], relatedMatchIds: [] },
        ],
      },
    }),
  )

  await page.goto('/recap')

  await expect(page.getByRole('heading', { name: 'Tomek: Twoj mundial w liczbach' })).toBeVisible()
  await expect(page.getByText('Najlepszy moment')).toBeVisible()
  await expect(page.getByText('#2')).toBeVisible()
})
```

- [ ] **Step 2: Run smoke to verify it fails**

Run:

```powershell
cd frontend
npm run test:e2e:smoke -- --grep "zalogowany gracz widzi personalny recap"
```

Expected: fail because `/recap` route and page do not exist.

- [ ] **Step 3: Add `PersonalRecapPage`**

Create `frontend/src/features/summary/PersonalRecapPage.tsx`:

```tsx
import { useQuery } from '@tanstack/react-query'
import { getErrorMessage } from '../../api/client'
import { summaryApi } from '../../api/services'
import { Panel } from '../../components/Panel'
import { QueryState } from '../../components/QueryState'
import { SectionHeading } from '../../components/SectionHeading'
import { StatCard } from '../../components/StatCard'
import { UserAvatar } from '../../components/UserAvatar'

export const PersonalRecapPage = () => {
  const recapQuery = useQuery({ queryKey: ['summary', 'final', 'me'], queryFn: summaryApi.getMine })
  const recap = recapQuery.data

  return (
    <div className="space-y-6">
      <SectionHeading
        eyebrow="Moj recap"
        title={recap ? `${recap.displayName}: Twoj mundial w liczbach` : 'Twoj mundial w liczbach'}
        description="Personalne podsumowanie: najlepszy moment, styl typowania i kilka rzeczy, ktore byly tylko Twoje."
      />

      <QueryState
        isLoading={recapQuery.isLoading}
        isError={recapQuery.isError}
        errorMessage={getErrorMessage(recapQuery.error)}
        loadingTitle="Ladowanie personalnego podsumowania"
        loadingDescription="Skladam Twoje turniejowe ciekawostki."
      >
        {recap ? (
          <>
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <StatCard label="Miejsce" value={`#${recap.finalPosition}`} accent="text-emerald-300" />
              <StatCard label="Punkty" value={recap.totalPoints} />
              <StatCard label="Dokladne" value={recap.exactScoreHits} />
              <StatCard label="Rezultaty" value={recap.correctOutcomeHits} />
            </div>

            <Panel className="space-y-5">
              <div className="flex items-center gap-3">
                <UserAvatar displayName={recap.displayName} avatarUrl={recap.avatarUrl} />
                <div>
                  <p className="font-display text-2xl uppercase text-white">{recap.displayName}</p>
                  <p className="text-sm text-slate-400">{recap.predictionsCount} typow w rozliczonych meczach</p>
                </div>
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                {recap.personalFacts.map((fact) => (
                  <article key={fact.id} className="rounded-3xl border border-white/10 bg-slate-950/55 p-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-emerald-300">{fact.label}</p>
                    <p className="mt-3 font-display text-xl uppercase text-white">{fact.title}</p>
                    <p className="mt-2 text-sm leading-6 text-slate-300">{fact.description}</p>
                  </article>
                ))}
              </div>
            </Panel>
          </>
        ) : null}
      </QueryState>
    </div>
  )
}
```

- [ ] **Step 4: Add route and navigation link**

Modify `frontend/src/routes/AppRouter.tsx` imports:

```tsx
import { PersonalRecapPage } from '../features/summary/PersonalRecapPage'
```

Add protected route in the non-admin protected group:

```tsx
<Route path="/recap" element={<PersonalRecapPage />} />
```

Modify `frontend/src/components/AppNavigation.tsx` imports:

```tsx
import { CalendarDays, LayoutDashboard, LogOut, Shield, Sparkles, Trophy, UserCircle2, UsersRound } from 'lucide-react'
```

Add link to `commonLinks` after ranking:

```tsx
{ to: '/recap', label: 'Recap', icon: Sparkles },
```

- [ ] **Step 5: Run recap smoke**

Run:

```powershell
cd frontend
npm run test:e2e:smoke -- --grep "zalogowany gracz widzi personalny recap"
```

Expected: pass.

- [ ] **Step 6: Commit recap UI**

Run:

```powershell
git add frontend\src\features\summary\PersonalRecapPage.tsx frontend\src\routes\AppRouter.tsx frontend\src\components\AppNavigation.tsx frontend\e2e\smoke.spec.ts
git commit -m "feat: add personal final recap"
```

Expected: commit contains recap page, route/nav, and smoke test updates only.

### Task 7: Verification, Polish, And Documentation

**Files:**
- Modify: `docs/api-contract.md`
- Modify: `docs/mvp-status.md`
- Modify: `frontend/src/features/summary/FinalRankingStoryChart.tsx`
- Modify: `frontend/src/features/summary/FinalSummaryPage.tsx`
- Modify: `frontend/src/features/summary/PersonalRecapPage.tsx`

- [ ] **Step 1: Add API documentation**

Add this section to `docs/api-contract.md`:

```markdown
## Final Summary

### `GET /api/summary/final`

Anonymous. Returns final tournament summary for the public post-tournament page:

- `stats`: settled match count, active player count, final leader.
- `positionSeries`: all active players with ranking position after each settled match.
- `finalTop`: top 5 final entries for labels and highlights.
- `globalFacts`: public curiosities.

### `GET /api/summary/final/me`

Authenticated. Returns personal final recap for the current user:

- final rank and scoring totals,
- 3-5 personal facts,
- highlighted match ids referenced by the recap.
```

Add this note to `docs/mvp-status.md` under frontend or post-MVP status:

```markdown
- Post-tournament final summary page planned: animated full-table position chart, global curiosities, and authenticated personal recaps.
```

- [ ] **Step 2: Add reduced-motion guard to chart**

In `FinalRankingStoryChart`, add:

```tsx
const prefersReducedMotion =
  typeof window !== 'undefined' &&
  window.matchMedia?.('(prefers-reduced-motion: reduce)').matches
```

Then set each `Line`:

```tsx
isAnimationActive={!prefersReducedMotion}
animationDuration={prefersReducedMotion ? 0 : 2200}
```

- [ ] **Step 3: Run backend tests**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln
```

Expected: all backend tests pass.

- [ ] **Step 4: Run frontend checks**

Run:

```powershell
cd frontend
npm run lint
npm run build:pages
npm run test:e2e:smoke -- --grep "finalne podsumowanie|personalny recap"
```

Expected: lint passes, build succeeds, targeted smoke passes.

- [ ] **Step 5: Inspect mobile and desktop manually**

Run local preview:

```powershell
cd frontend
npm run build
npm run preview -- --host 127.0.0.1 --port 4175
```

Open `http://127.0.0.1:4175/` and check:

- chart is visible on desktop,
- chart can scroll or compress on mobile,
- selected-player chips do not overflow,
- final labels do not cover hero copy,
- no static full table appears below the chart,
- login still reaches `/login`,
- authenticated `/recap` renders inside `AppShell`.

- [ ] **Step 6: Final diff review**

Run:

```powershell
git diff --check
git status --short
```

Expected: no whitespace errors. Status contains only planned files or intentional untracked local artifacts such as `.superpowers/`.

- [ ] **Step 7: Commit polish and docs**

Run:

```powershell
git add docs\api-contract.md docs\mvp-status.md frontend\src\features\summary\FinalRankingStoryChart.tsx frontend\src\features\summary\FinalSummaryPage.tsx frontend\src\features\summary\PersonalRecapPage.tsx
git commit -m "docs: document final summary page"
```

Expected: commit contains docs and final polish only.
