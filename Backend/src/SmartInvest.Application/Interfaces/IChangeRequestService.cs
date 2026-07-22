using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IChangeRequestService
{
    Task<IReadOnlyList<ChangeRequestDto>> GetHistoryAsync(int assignmentId, CancellationToken cancellationToken = default);

    Task<ChangeRequestDto> SubmitAsync(int assignmentId, CreateChangeRequestDto dto, CancellationToken cancellationToken = default);

    Task<ChangeRequestDto> ApproveAsync(int assignmentId, int changeRequestId, ReviewChangeRequestDto dto, CancellationToken cancellationToken = default);

    Task<ChangeRequestDto> RejectAsync(int assignmentId, int changeRequestId, ReviewChangeRequestDto dto, CancellationToken cancellationToken = default);
}
