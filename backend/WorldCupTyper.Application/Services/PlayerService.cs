using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Mappers;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;

namespace WorldCupTyper.Application.Services;

public sealed class PlayerService : IPlayerService
{
    private readonly IAppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PlayerService(IAppDbContext dbContext, IPasswordHasher passwordHasher, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync(CancellationToken cancellationToken = default)
    {
        var players = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsActive)
            .OrderByDescending(user => user.Role == UserRole.Admin)
            .ThenBy(user => user.DisplayName)
            .ToListAsync(cancellationToken);

        return players.Select(player => player.ToDto()).ToList();
    }

    public async Task<PlayerDto> CreatePlayerAsync(CreatePlayerRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var displayName = NormalizeDisplayName(request.DisplayName);
        await EnsurePlayerIsUniqueAsync(normalizedEmail, displayName, null, cancellationToken);

        var password = string.IsNullOrWhiteSpace(request.Password) ? GenerateTemporaryPassword() : request.Password.Trim();
        EnsurePassword(password);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            DisplayName = displayName,
            PasswordHash = _passwordHasher.Hash(password),
            Role = request.Role,
            IsActive = true,
            RequiresPasswordChange = true,
            CreatedAtUtc = _dateTimeProvider.UtcNow,
        };

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }

    public async Task<PlayerDto> UpdatePlayerAsync(Guid playerId, UpdatePlayerRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(candidate => candidate.Id == playerId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Nie znaleziono gracza.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var displayName = NormalizeDisplayName(request.DisplayName);
        await EnsurePlayerIsUniqueAsync(normalizedEmail, displayName, playerId, cancellationToken);

        user.Email = normalizedEmail;
        user.DisplayName = displayName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user.ToDto();
    }

    public async Task DeactivatePlayerAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(candidate => candidate.Id == playerId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Nie znaleziono gracza.");
        }

        user.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(Guid playerId, ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(candidate => candidate.Id == playerId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Nie znaleziono gracza.");
        }

        var password = string.IsNullOrWhiteSpace(request.NewPassword) ? GenerateTemporaryPassword() : request.NewPassword.Trim();
        EnsurePassword(password);

        user.PasswordHash = _passwordHasher.Hash(password);
        user.RequiresPasswordChange = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResponse(user.Id, password);
    }

    private async Task EnsurePlayerIsUniqueAsync(string email, string displayName, Guid? currentUserId, CancellationToken cancellationToken)
    {
        var emailTaken = await _dbContext.Users.AnyAsync(
            user => user.Email == email && user.Id != currentUserId,
            cancellationToken);

        if (emailTaken)
        {
            throw new ConflictException("Użytkownik z takim adresem e-mail już istnieje.");
        }

        var displayNameLower = displayName.ToLowerInvariant();
        var displayNameTaken = await _dbContext.Users.AnyAsync(
            user => user.DisplayName.ToLower() == displayNameLower && user.Id != currentUserId,
            cancellationToken);

        if (displayNameTaken)
        {
            throw new ConflictException("Użytkownik z taką nazwą wyświetlaną już istnieje.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BusinessRuleException("Adres e-mail jest wymagany.");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new BusinessRuleException("Nazwa wyświetlana jest wymagana.");
        }

        return displayName.Trim();
    }

    private static void EnsurePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new BusinessRuleException("Hasło musi mieć co najmniej 8 znaków.");
        }
    }

    private static string GenerateTemporaryPassword()
    {
        return $"Temp{RandomNumberGenerator.GetInt32(100000, 999999)}!";
    }
}
