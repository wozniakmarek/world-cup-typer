using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Infrastructure.Options;
using WorldCupTyper.Infrastructure.Persistence;

namespace WorldCupTyper.Infrastructure.Seeding;

public sealed class DevelopmentDataSeeder
{
    private readonly WorldCupTyperDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly DevelopmentSeedOptions _options;

    public DevelopmentDataSeeder(
        WorldCupTyperDbContext dbContext,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IOptions<DevelopmentSeedOptions> options)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
    }

    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        var rankingNeedsRebuild = false;

        if (!await _dbContext.Users.AnyAsync(cancellationToken))
        {
            await SeedUsersAsync(cancellationToken);
            rankingNeedsRebuild = true;
        }

        if (!await _dbContext.Teams.AnyAsync(cancellationToken))
        {
            await SeedTeamsAsync(cancellationToken);
        }

        if (!await _dbContext.Matches.AnyAsync(cancellationToken))
        {
            await SeedMatchesAsync(cancellationToken);
            rankingNeedsRebuild = true;
        }

        if (!await _dbContext.Predictions.AnyAsync(cancellationToken))
        {
            await SeedPredictionsAsync(cancellationToken);
            rankingNeedsRebuild = true;
        }

        if (!await _dbContext.LeaderboardSnapshots.AnyAsync(cancellationToken)
            && await _dbContext.Matches.AnyAsync(match => match.HomeScore90.HasValue && match.AwayScore90.HasValue, cancellationToken))
        {
            rankingNeedsRebuild = true;
        }

        return rankingNeedsRebuild;
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        var createdAt = _dateTimeProvider.UtcNow.AddDays(-7);
        var users = new[]
        {
            CreateUser(_options.AdminEmail.Trim().ToLowerInvariant(), "Admin", _options.AdminPassword, UserRole.Admin, createdAt),
            CreateUser("marek@typer.local", "Marek", _options.DefaultPlayerPassword, UserRole.Player, createdAt),
            CreateUser("kuba@typer.local", "Kuba", _options.DefaultPlayerPassword, UserRole.Player, createdAt),
            CreateUser("bartek@typer.local", "Bartek", _options.DefaultPlayerPassword, UserRole.Player, createdAt),
            CreateUser("pawel@typer.local", "Paweł", _options.DefaultPlayerPassword, UserRole.Player, createdAt),
            CreateUser("asia@typer.local", "Asia", _options.DefaultPlayerPassword, UserRole.Player, createdAt),
        };

        await _dbContext.Users.AddRangeAsync(users, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedTeamsAsync(CancellationToken cancellationToken)
    {
        var teams = new[]
        {
            CreateTeam("Polska", "POL", "PL", "🇵🇱", "A"),
            CreateTeam("Niemcy", "GER", "DE", "🇩🇪", "A"),
            CreateTeam("Francja", "FRA", "FR", "🇫🇷", "B"),
            CreateTeam("Hiszpania", "ESP", "ES", "🇪🇸", "B"),
            CreateTeam("Brazylia", "BRA", "BR", "🇧🇷", "C"),
            CreateTeam("Argentyna", "ARG", "AR", "🇦🇷", "C"),
            CreateTeam("Anglia", "ENG", "GB", "🏴", "D"),
            CreateTeam("Portugalia", "POR", "PT", "🇵🇹", "D"),
        };

        await _dbContext.Teams.AddRangeAsync(teams, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedMatchesAsync(CancellationToken cancellationToken)
    {
        var teams = await _dbContext.Teams.ToListAsync(cancellationToken);
        var teamByName = teams.ToDictionary(team => team.Name, StringComparer.OrdinalIgnoreCase);
        var now = _dateTimeProvider.UtcNow;

        var matches = new[]
        {
            new Match
            {
                Id = Guid.NewGuid(),
                MatchNumber = 1,
                Phase = MatchPhase.GroupStage,
                GroupName = "A",
                HomeTeamId = teamByName["Polska"].Id,
                AwayTeamId = teamByName["Niemcy"].Id,
                KickoffTimeUtc = now.AddDays(-2).AddHours(-1),
                Venue = "Stadium One",
                Status = MatchStatus.Finished,
                HomeScore90 = 1,
                AwayScore90 = 2,
                CreatedAtUtc = now.AddDays(-10),
            },
            new Match
            {
                Id = Guid.NewGuid(),
                MatchNumber = 2,
                Phase = MatchPhase.GroupStage,
                GroupName = "B",
                HomeTeamId = teamByName["Francja"].Id,
                AwayTeamId = teamByName["Hiszpania"].Id,
                KickoffTimeUtc = now.AddDays(-1).AddHours(-3),
                Venue = "Stadium Two",
                Status = MatchStatus.Finished,
                HomeScore90 = 1,
                AwayScore90 = 1,
                CreatedAtUtc = now.AddDays(-10),
            },
            new Match
            {
                Id = Guid.NewGuid(),
                MatchNumber = 3,
                Phase = MatchPhase.GroupStage,
                GroupName = "C",
                HomeTeamId = teamByName["Brazylia"].Id,
                AwayTeamId = teamByName["Argentyna"].Id,
                KickoffTimeUtc = now.AddDays(1).AddHours(2),
                Venue = "Stadium Three",
                Status = MatchStatus.Scheduled,
                CreatedAtUtc = now.AddDays(-10),
            },
            new Match
            {
                Id = Guid.NewGuid(),
                MatchNumber = 4,
                Phase = MatchPhase.GroupStage,
                GroupName = "D",
                HomeTeamId = teamByName["Anglia"].Id,
                AwayTeamId = teamByName["Portugalia"].Id,
                KickoffTimeUtc = now.AddDays(2).AddHours(5),
                Venue = "Stadium Four",
                Status = MatchStatus.Scheduled,
                CreatedAtUtc = now.AddDays(-10),
            },
        };

        await _dbContext.Matches.AddRangeAsync(matches, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPredictionsAsync(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users.Where(user => user.Role == UserRole.Player).ToListAsync(cancellationToken);
        var usersByName = users.ToDictionary(user => user.DisplayName, StringComparer.OrdinalIgnoreCase);
        var matches = await _dbContext.Matches.OrderBy(match => match.MatchNumber).ToListAsync(cancellationToken);
        var matchOne = matches.Single(match => match.MatchNumber == 1);
        var matchTwo = matches.Single(match => match.MatchNumber == 2);
        var matchThree = matches.Single(match => match.MatchNumber == 3);
        var now = _dateTimeProvider.UtcNow;

        var predictions = new[]
        {
            CreatePrediction(usersByName["Marek"].Id, matchOne.Id, 1, 2, now.AddDays(-2).AddHours(-5)),
            CreatePrediction(usersByName["Kuba"].Id, matchOne.Id, 2, 1, now.AddDays(-2).AddHours(-4)),
            CreatePrediction(usersByName["Bartek"].Id, matchOne.Id, 0, 1, now.AddDays(-2).AddHours(-3)),
            CreatePrediction(usersByName["Paweł"].Id, matchOne.Id, 1, 1, now.AddDays(-2).AddHours(-2)),
            CreatePrediction(usersByName["Asia"].Id, matchOne.Id, 1, 2, now.AddDays(-2).AddHours(-1)),
            CreatePrediction(usersByName["Marek"].Id, matchTwo.Id, 2, 2, now.AddDays(-1).AddHours(-8)),
            CreatePrediction(usersByName["Kuba"].Id, matchTwo.Id, 1, 1, now.AddDays(-1).AddHours(-7)),
            CreatePrediction(usersByName["Bartek"].Id, matchTwo.Id, 2, 1, now.AddDays(-1).AddHours(-6)),
            CreatePrediction(usersByName["Paweł"].Id, matchTwo.Id, 0, 0, now.AddDays(-1).AddHours(-5)),
            CreatePrediction(usersByName["Asia"].Id, matchTwo.Id, 1, 0, now.AddDays(-1).AddHours(-4)),
            CreatePrediction(usersByName["Marek"].Id, matchThree.Id, 2, 1, now.AddHours(-1)),
        };

        await _dbContext.Predictions.AddRangeAsync(predictions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private ApplicationUser CreateUser(string email, string displayName, string password, UserRole role, DateTime createdAtUtc)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            PasswordHash = _passwordHasher.Hash(password),
            Role = role,
            IsActive = true,
            CreatedAtUtc = createdAtUtc,
        };
    }

    private static Team CreateTeam(string name, string shortName, string countryCode, string flagEmoji, string groupName)
    {
        return new Team
        {
            Id = Guid.NewGuid(),
            Name = name,
            ShortName = shortName,
            CountryCode = countryCode,
            FlagEmoji = flagEmoji,
            GroupName = groupName,
        };
    }

    private static Prediction CreatePrediction(Guid userId, Guid matchId, int homeScore, int awayScore, DateTime createdAtUtc)
    {
        return new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MatchId = matchId,
            PredictedHomeScore = homeScore,
            PredictedAwayScore = awayScore,
            CreatedAtUtc = createdAtUtc,
        };
    }
}
