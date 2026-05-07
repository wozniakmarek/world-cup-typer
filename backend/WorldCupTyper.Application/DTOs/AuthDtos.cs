using WorldCupTyper.Domain.Enums;

namespace WorldCupTyper.Application.DTOs;

public sealed record LoginRequest(string Login, string Password);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsActive);

public sealed record AuthResponse(string Token, CurrentUserDto User);
