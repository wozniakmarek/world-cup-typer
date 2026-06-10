using WorldCupTyper.Domain.Enums;

namespace WorldCupTyper.Application.DTOs;

public sealed record PlayerDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsActive,
    bool RequiresPasswordChange,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    string? AvatarUrl);

public sealed record CreatePlayerRequest(
    string Email,
    string DisplayName,
    string? Password,
    UserRole Role = UserRole.Player);

public sealed record UpdatePlayerRequest(
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsActive);

public sealed record ResetPasswordRequest(string? NewPassword);

public sealed record ResetPasswordResponse(Guid PlayerId, string TemporaryPassword);
