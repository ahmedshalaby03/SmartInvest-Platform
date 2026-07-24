using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IPlanService
{
    Task<IReadOnlyList<PlanDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PlanDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PlanDto> CreateAsync(CreatePlanDto dto, CancellationToken cancellationToken = default);

    Task<PlanDto> UpdateAsync(int id, UpdatePlanDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<PlanDetailDto> AddSuggestedProjectAsync(int planId, int subProjectId, CancellationToken cancellationToken = default);

    Task RemoveSuggestedProjectAsync(int planId, int subProjectId, CancellationToken cancellationToken = default);

    Task<PlanDto> ApproveAsync(int planId, DateTime approvalDate, CancellationToken cancellationToken = default);
}
