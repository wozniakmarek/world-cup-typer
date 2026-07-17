using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Services.Interfaces;

public interface IFinalSummaryService
{
    Task<FinalSummaryAvailabilityDto> GetFinalSummaryAvailabilityAsync(CancellationToken cancellationToken = default);
    Task<FinalSummaryResponseDto> GetFinalSummaryAsync(Guid? currentUserId = null, CancellationToken cancellationToken = default);
    Task<PersonalFinalSummaryResponseDto> GetPersonalFinalSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
