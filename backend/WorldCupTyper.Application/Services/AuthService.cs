using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IAppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthService(
        IAppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAppException("Nieprawidłowy login lub hasło.");
        }

        var normalizedLogin = request.Login.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                candidate => candidate.Email == normalizedLogin || candidate.DisplayName.ToLower() == normalizedLogin,
                cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAppException("Nieprawidłowy login lub hasło.");
        }

        user.LastLoginAtUtc = _dateTimeProvider.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            _jwtTokenService.GenerateToken(user),
            ToCurrentUserDto(user));
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == userId && candidate.IsActive, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAppException("Użytkownik nie istnieje lub jest nieaktywny.");
        }

        return ToCurrentUserDto(user);
    }

    public async Task<CurrentUserDto> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId && candidate.IsActive, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAppException("Użytkownik nie istnieje lub jest nieaktywny.");
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            !_passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            throw new UnauthorizedAppException("Obecne hasło jest nieprawidłowe.");
        }

        var newPassword = request.NewPassword?.Trim() ?? string.Empty;
        EnsurePassword(newPassword);

        if (_passwordHasher.Verify(user.PasswordHash, newPassword))
        {
            throw new BusinessRuleException("Nowe hasło musi różnić się od obecnego.");
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.RequiresPasswordChange = false;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToCurrentUserDto(user);
    }

    public async Task<CurrentUserDto> UpdateAvatarAsync(Guid userId, UpdateAvatarRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId && candidate.IsActive, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAppException("Uzytkownik nie istnieje lub jest nieaktywny.");
        }

        user.AvatarUrl = NormalizeAvatarUrl(request.AvatarUrl);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToCurrentUserDto(user);
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private static CurrentUserDto ToCurrentUserDto(WorldCupTyper.Domain.Entities.ApplicationUser user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role, user.IsActive, user.RequiresPasswordChange, user.AvatarUrl);

    private static void EnsurePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new BusinessRuleException("Hasło musi mieć co najmniej 8 znaków.");
        }
    }

    private static string? NormalizeAvatarUrl(string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return null;
        }

        var normalized = avatarUrl.Trim();
        if (normalized.Length > 500)
        {
            throw new BusinessRuleException("Adres avatara moze miec maksymalnie 500 znakow.");
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new BusinessRuleException("Adres avatara musi byc pelnym adresem http lub https.");
        }

        return normalized;
    }
}
