using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Application.Mappers;

public static class DtoMapper
{
    public static TeamDto ToDto(this Team team) =>
        new(
            team.Id,
            team.Name,
            team.ShortName,
            team.CountryCode,
            team.FlagEmoji,
            team.GroupName);

    public static PredictionSummaryDto ToSummaryDto(this Prediction prediction, DateTime nowUtc, DateTime matchKickoffUtc)
    {
        var lockedAtUtc = prediction.LockedAtUtc;
        if (lockedAtUtc is null && matchKickoffUtc <= nowUtc)
        {
            lockedAtUtc = matchKickoffUtc;
        }

        return new PredictionSummaryDto(
            prediction.Id,
            prediction.PredictedHomeScore,
            prediction.PredictedAwayScore,
            prediction.CreatedAtUtc,
            prediction.UpdatedAtUtc,
            lockedAtUtc,
            prediction.Result?.Points,
            prediction.Result?.IsExactScore,
            prediction.Result?.IsCorrectOutcome);
    }

    public static MatchSummaryDto ToMatchSummaryDto(this Match match, Guid currentUserId, DateTime nowUtc)
    {
        var myPrediction = match.Predictions.FirstOrDefault(x => x.UserId == currentUserId);
        return new MatchSummaryDto(
            match.Id,
            match.MatchNumber,
            match.Phase,
            match.GroupName,
            match.KickoffTimeUtc,
            match.Venue,
            match.Status,
            match.IsSettled,
            match.HomeScore90,
            match.AwayScore90,
            match.HomeTeam.ToDto(),
            match.AwayTeam.ToDto(),
            myPrediction?.ToSummaryDto(nowUtc, match.KickoffTimeUtc),
            myPrediction?.Result?.Points,
            match.CanAcceptPredictions(nowUtc));
    }

    public static MatchDetailsDto ToMatchDetailsDto(this Match match, Guid currentUserId, bool canViewPredictions, DateTime nowUtc)
    {
        var myPrediction = match.Predictions.FirstOrDefault(x => x.UserId == currentUserId);
        return new MatchDetailsDto(
            match.Id,
            match.MatchNumber,
            match.Phase,
            match.GroupName,
            match.KickoffTimeUtc,
            match.Venue,
            match.Status,
            match.IsSettled,
            match.HomeScore90,
            match.AwayScore90,
            match.HomeScoreFinal,
            match.AwayScoreFinal,
            match.HomeTeam.ToDto(),
            match.AwayTeam.ToDto(),
            myPrediction?.ToSummaryDto(nowUtc, match.KickoffTimeUtc),
            myPrediction?.Result?.Points,
            match.CanAcceptPredictions(nowUtc),
            canViewPredictions);
    }

    public static AdminMatchDto ToAdminDto(this Match match) =>
        new(
            match.Id,
            match.MatchNumber,
            match.Phase,
            match.GroupName,
            match.KickoffTimeUtc,
            match.Venue,
            match.Status,
            match.IsSettled,
            match.Predictions.Count,
            match.HomeScore90,
            match.AwayScore90,
            match.HomeTeam.ToDto(),
            match.AwayTeam.ToDto());

    public static PlayerDto ToDto(this ApplicationUser user) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            user.IsActive,
            user.CreatedAtUtc,
            user.LastLoginAtUtc,
            user.AvatarUrl);
}
