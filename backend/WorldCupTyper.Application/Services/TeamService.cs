using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Mappers;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Application.Services;

public sealed class TeamService : ITeamService
{
    private readonly IAppDbContext _dbContext;

    public TeamService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<TeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        var teams = await _dbContext.Teams
            .AsNoTracking()
            .OrderBy(team => team.Name)
            .ToListAsync(cancellationToken);

        return teams.Select(team => team.ToDto()).ToList();
    }

    public async Task<TeamDto> CreateTeamAsync(UpsertTeamRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureUniqueAsync(request.Name, request.ShortName, null, cancellationToken);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = NormalizeRequired(request.Name, "Nazwa drużyny jest wymagana."),
            ShortName = NormalizeRequired(request.ShortName, "Skrót drużyny jest wymagany."),
            CountryCode = NormalizeRequired(request.CountryCode, "Kod kraju jest wymagany.").ToUpperInvariant(),
            FlagEmoji = request.FlagEmoji?.Trim(),
            GroupName = request.GroupName?.Trim(),
        };

        await _dbContext.Teams.AddAsync(team, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return team.ToDto();
    }

    public async Task<TeamDto> UpdateTeamAsync(Guid teamId, UpsertTeamRequest request, CancellationToken cancellationToken = default)
    {
        var team = await _dbContext.Teams.FirstOrDefaultAsync(candidate => candidate.Id == teamId, cancellationToken);
        if (team is null)
        {
            throw new NotFoundException("Nie znaleziono drużyny.");
        }

        await EnsureUniqueAsync(request.Name, request.ShortName, teamId, cancellationToken);

        team.Name = NormalizeRequired(request.Name, "Nazwa drużyny jest wymagana.");
        team.ShortName = NormalizeRequired(request.ShortName, "Skrót drużyny jest wymagany.");
        team.CountryCode = NormalizeRequired(request.CountryCode, "Kod kraju jest wymagany.").ToUpperInvariant();
        team.FlagEmoji = request.FlagEmoji?.Trim();
        team.GroupName = request.GroupName?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return team.ToDto();
    }

    private async Task EnsureUniqueAsync(string name, string shortName, Guid? currentTeamId, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeRequired(name, "Nazwa drużyny jest wymagana.");
        var normalizedShortName = NormalizeRequired(shortName, "Skrót drużyny jest wymagany.");

        var nameTaken = await _dbContext.Teams.AnyAsync(
            team => team.Name.ToLower() == normalizedName.ToLower() && team.Id != currentTeamId,
            cancellationToken);

        if (nameTaken)
        {
            throw new ConflictException("Drużyna o tej nazwie już istnieje.");
        }

        var shortNameTaken = await _dbContext.Teams.AnyAsync(
            team => team.ShortName.ToLower() == normalizedShortName.ToLower() && team.Id != currentTeamId,
            cancellationToken);

        if (shortNameTaken)
        {
            throw new ConflictException("Drużyna o tym skrócie już istnieje.");
        }
    }

    private static string NormalizeRequired(string value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleException(errorMessage);
        }

        return value.Trim();
    }
}
