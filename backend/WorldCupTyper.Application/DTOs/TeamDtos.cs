namespace WorldCupTyper.Application.DTOs;

public sealed record TeamDto(
    Guid Id,
    string Name,
    string ShortName,
    string CountryCode,
    string? FlagEmoji,
    string? GroupName);

public sealed record UpsertTeamRequest(
    string Name,
    string ShortName,
    string CountryCode,
    string? FlagEmoji,
    string? GroupName);
