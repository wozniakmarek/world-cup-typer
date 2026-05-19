# Football Data Automation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a fully automated football-data.org sync that imports World Cup matches, updates statuses and 90-minute scores, and settles finished matches automatically.

**Architecture:** Keep provider-specific code in `WorldCupTyper.Infrastructure`. The API and application layers depend only on `IScheduleImportService`, `IMatchSettlementService`, and DTOs that describe sync summaries. The background worker and manual admin endpoint use the same idempotent sync service.

**Tech Stack:** .NET 8, EF Core, ASP.NET Core hosted services, `HttpClient`, xUnit, FluentAssertions, EF Core InMemory tests.

---

### Task 1: Add Sync Contracts And FootballData Options

**Files:**
- Modify: `backend/WorldCupTyper.Application/Abstractions/IScheduleImportService.cs`
- Create: `backend/WorldCupTyper.Application/DTOs/ScheduleSyncDtos.cs`
- Create: `backend/WorldCupTyper.Infrastructure/Options/FootballDataOptions.cs`
- Modify: `backend/WorldCupTyper.Api/appsettings.json`
- Modify: `backend/WorldCupTyper.Api/appsettings.Development.json`

- [ ] **Step 1: Write the failing contract compile test**

Add this test to a new `backend/WorldCupTyper.Tests/FootballDataOptionsTests.cs`:

```csharp
using FluentAssertions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Tests;

public sealed class FootballDataOptionsTests
{
    [Fact]
    public void Defaults_ShouldKeepAutomationDisabledAndUseWorldCupCompetition()
    {
        var options = new FootballDataOptions();

        options.Enabled.Should().BeFalse();
        options.CompetitionCode.Should().Be("WC");
        options.SyncIntervalMinutes.Should().Be(30);
        options.SettleAutomatically.Should().BeTrue();
    }

    [Fact]
    public void ScheduleSyncSummary_ShouldExposeAllCounters()
    {
        var summary = new ScheduleSyncSummaryDto(1, 2, 3, 4, 5);

        summary.ImportedMatches.Should().Be(1);
        summary.UpdatedMatches.Should().Be(2);
        summary.SkippedMatches.Should().Be(3);
        summary.SettledMatches.Should().Be(4);
        summary.FailedMatches.Should().Be(5);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FootballDataOptionsTests
```

Expected: compile failure because `FootballDataOptions` and `ScheduleSyncSummaryDto` do not exist.

- [ ] **Step 3: Add minimal contracts and config**

`ScheduleSyncDtos.cs`:

```csharp
namespace WorldCupTyper.Application.DTOs;

public sealed record ScheduleSyncSummaryDto(
    int ImportedMatches,
    int UpdatedMatches,
    int SkippedMatches,
    int SettledMatches,
    int FailedMatches);
```

Change `IScheduleImportService`:

```csharp
using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Abstractions;

public interface IScheduleImportService
{
    Task<ScheduleSyncSummaryDto> ImportScheduleAsync(CancellationToken cancellationToken = default);
}
```

`FootballDataOptions.cs`:

```csharp
namespace WorldCupTyper.Infrastructure.Options;

public sealed class FootballDataOptions
{
    public const string SectionName = "FootballData";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://api.football-data.org/v4/";
    public string ApiToken { get; set; } = string.Empty;
    public string CompetitionCode { get; set; } = "WC";
    public int SyncIntervalMinutes { get; set; } = 30;
    public int LookbackDays { get; set; } = 2;
    public int LookaheadDays { get; set; } = 370;
    public bool SettleAutomatically { get; set; } = true;
}
```

Add the `FootballData` section to both appsettings files with `Enabled=false` and blank `ApiToken`.

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FootballDataOptionsTests
```

Expected: pass.

### Task 2: Add Team ExternalId Persistence

**Files:**
- Modify: `backend/WorldCupTyper.Domain/Entities/Team.cs`
- Modify: `backend/WorldCupTyper.Infrastructure/Persistence/Configurations/TeamConfiguration.cs`
- Modify: `backend/WorldCupTyper.Infrastructure/Persistence/Configurations/MatchConfiguration.cs`
- Create: EF migration under `backend/WorldCupTyper.Infrastructure/Persistence/Migrations/`

- [ ] **Step 1: Write failing persistence test**

Add this test to `backend/WorldCupTyper.Tests/FootballDataPersistenceTests.cs`:

```csharp
using FluentAssertions;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class FootballDataPersistenceTests
{
    [Fact]
    public async Task Team_ShouldPersistExternalId()
    {
        using var dbContext = TestDbContextFactory.Create();
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Poland",
            ShortName = "POL",
            CountryCode = "POL",
            ExternalId = "football-data:794",
        };

        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();
        var saved = dbContext.Teams.Single(candidate => candidate.Id == team.Id);
        saved.ExternalId.Should().Be("football-data:794");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FootballDataPersistenceTests
```

Expected: compile failure because `Team.ExternalId` does not exist.

- [ ] **Step 3: Add field, indexes, and migration**

Add to `Team`:

```csharp
public string? ExternalId { get; set; }
```

Configure:

```csharp
builder.Property(team => team.ExternalId).HasMaxLength(100);
builder.HasIndex(team => team.ExternalId).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");
```

Configure `Match.ExternalId` unique index:

```csharp
builder.HasIndex(match => match.ExternalId).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");
```

Create migration:

```powershell
dotnet ef migrations add AddFootballDataExternalIds --project backend\WorldCupTyper.Infrastructure --startup-project backend\WorldCupTyper.Api --output-dir Persistence\Migrations
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FootballDataPersistenceTests
```

Expected: pass.

### Task 3: Implement Provider DTOs And Mapper

**Files:**
- Create: `backend/WorldCupTyper.Infrastructure/FootballData/FootballDataDtos.cs`
- Create: `backend/WorldCupTyper.Infrastructure/FootballData/FootballDataSyncModels.cs`
- Create: `backend/WorldCupTyper.Infrastructure/FootballData/FootballDataMatchMapper.cs`
- Create: `backend/WorldCupTyper.Tests/FootballDataMatchMapperTests.cs`

- [ ] **Step 1: Write failing mapper tests**

Cover these tests:

```csharp
[Fact]
public void Map_WithScheduledStatus_ShouldMapScheduled()

[Fact]
public void Map_WithInPlayStatus_ShouldMapInProgress()

[Fact]
public void Map_WithRegularTimeScore_ShouldUseRegularTimeForScore90()

[Fact]
public void Map_WithRegularDurationAndNoRegularTime_ShouldUseFullTimeForScore90()

[Fact]
public void Map_WithExtraTimeAndNoRegularTime_ShouldLeaveScore90Empty()

[Fact]
public void Map_WithUnknownStatus_ShouldReturnNull()
```

Each test builds a `FootballDataMatchDto`, calls `FootballDataMatchMapper.Map(match)`, and asserts the mapped `MatchStatus`, `HomeScore90`, `AwayScore90`, `ExternalId`, and `CanSettle`.

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FootballDataMatchMapperTests
```

Expected: compile failure because mapper types do not exist.

- [ ] **Step 3: Add provider DTOs and mapper**

Use `JsonPropertyName` DTOs for response shape:

```csharp
public sealed record FootballDataMatchesResponseDto(
    [property: JsonPropertyName("matches")] IReadOnlyCollection<FootballDataMatchDto> Matches);
```

Mapper outputs a provider-neutral model with:

```csharp
public sealed record FootballDataMatchSyncModel(
    string ExternalId,
    int MatchNumber,
    MatchPhase Phase,
    string? GroupName,
    FootballDataTeamSyncModel HomeTeam,
    FootballDataTeamSyncModel AwayTeam,
    DateTime KickoffTimeUtc,
    string? Venue,
    MatchStatus Status,
    int? HomeScore90,
    int? AwayScore90,
    int? HomeScoreFinal,
    int? AwayScoreFinal);
```

Implement score fallback exactly as the design describes.

- [ ] **Step 4: Run mapper tests**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FootballDataMatchMapperTests
```

Expected: pass.

### Task 4: Implement FootballData Client

**Files:**
- Create: `backend/WorldCupTyper.Infrastructure/FootballData/IFootballDataClient.cs`
- Create: `backend/WorldCupTyper.Infrastructure/FootballData/FootballDataClient.cs`
- Create: `backend/WorldCupTyper.Tests/FootballDataClientTests.cs`

- [ ] **Step 1: Write failing client test with fake handler**

Test that client calls `competitions/WC/matches`, sends `X-Auth-Token`, and deserializes one match.

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FootballDataClientTests
```

Expected: compile failure because client does not exist.

- [ ] **Step 3: Implement client**

Use constructor dependencies:

```csharp
public FootballDataClient(HttpClient httpClient, IOptions<FootballDataOptions> options)
```

Throw a controlled `BusinessRuleException` if token is blank for manual sync.

- [ ] **Step 4: Run client tests**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FootballDataClientTests
```

Expected: pass.

### Task 5: Implement Import Service With Settlement

**Files:**
- Delete: `backend/WorldCupTyper.Infrastructure/Services/StubScheduleImportService.cs`
- Create: `backend/WorldCupTyper.Infrastructure/Services/FootballDataScheduleImportService.cs`
- Create: `backend/WorldCupTyper.Tests/FootballDataScheduleImportServiceTests.cs`

- [ ] **Step 1: Write failing service tests**

Cover:

```csharp
[Fact]
public async Task ImportScheduleAsync_WithNewFinishedMatch_ShouldCreateTeamsMatchAndSettlePredictions()

[Fact]
public async Task ImportScheduleAsync_RunTwice_ShouldNotDuplicateTeamsOrMatches()

[Fact]
public async Task ImportScheduleAsync_WithFinishedExtraTimeMatchWithoutRegularTime_ShouldNotSettle()
```

Use a fake `IFootballDataClient` returning sync models and the real `MatchSettlementService` with test users/predictions.

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FootballDataScheduleImportServiceTests
```

Expected: compile failure because service does not exist.

- [ ] **Step 3: Implement service**

Core algorithm:

```csharp
var providerMatches = await _client.GetCompetitionMatchesAsync(cancellationToken);
foreach (var providerMatch in providerMatches)
{
    var homeTeam = await UpsertTeamAsync(providerMatch.HomeTeam, cancellationToken);
    var awayTeam = await UpsertTeamAsync(providerMatch.AwayTeam, cancellationToken);
    var match = await UpsertMatchAsync(providerMatch, homeTeam.Id, awayTeam.Id, cancellationToken);
    if (_options.Value.SettleAutomatically && CanAutoSettle(match))
    {
        await _settlementService.SettleMatchAsync(match.Id, cancellationToken);
    }
}
```

`CanAutoSettle` requires `Status == Finished`, `!IsSettled`, and both 90-minute scores present.

- [ ] **Step 4: Run service tests**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FootballDataScheduleImportServiceTests
```

Expected: pass.

### Task 6: Wire DI, Admin Endpoint, And Worker

**Files:**
- Modify: `backend/WorldCupTyper.Infrastructure/ServiceCollectionExtensions.cs`
- Create: `backend/WorldCupTyper.Infrastructure/FootballData/FootballDataSyncWorker.cs`
- Modify: `backend/WorldCupTyper.Api/Controllers/AdminMatchesController.cs`
- Create: `backend/WorldCupTyper.Tests/FootballDataSyncWorkerTests.cs`

- [ ] **Step 1: Write failing worker disabled test**

Assert that worker does not call `IScheduleImportService` when `FootballData.Enabled=false` or token is blank.

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FootballDataSyncWorkerTests
```

Expected: compile failure because worker does not exist.

- [ ] **Step 3: Wire services**

Register:

```csharp
services.Configure<FootballDataOptions>(configuration.GetSection(FootballDataOptions.SectionName));
services.AddHttpClient<IFootballDataClient, FootballDataClient>();
services.AddScoped<IScheduleImportService, FootballDataScheduleImportService>();
services.AddHostedService<FootballDataSyncWorker>();
```

Add admin action:

```csharp
[HttpPost("sync-football-data")]
public async Task<ActionResult<ScheduleSyncSummaryDto>> SyncFootballData(CancellationToken cancellationToken)
{
    return Ok(await _scheduleImportService.ImportScheduleAsync(cancellationToken));
}
```

- [ ] **Step 4: Run worker test**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln --filter FootballDataSyncWorkerTests
```

Expected: pass.

### Task 7: Documentation And Full Verification

**Files:**
- Modify: `docs/local-development.md`
- Modify: `docs/api-contract.md`
- Modify: `docs/football-api-research.md` if implementation details change materially.

- [ ] **Step 1: Document config and endpoint**

Add `FootballData__...` environment variables, explain disabled default, manual endpoint, and staging-first rollout.

- [ ] **Step 2: Run full backend tests**

Run:

```powershell
dotnet test backend\WorldCupTyper.sln
```

Expected: all tests pass.

- [ ] **Step 3: Final diff review**

Run:

```powershell
git diff --check
git status --short
```

Expected: no whitespace errors and only planned files changed.
