using SmartInvest.Application.DTOs;
using SmartInvest.Application.DTOs.Common;

namespace SmartInvest.Application.Interfaces;

public interface ISubProjectService
{
    Task<PagedResultDto<SubProjectListItemDto>> SearchAsync(int? mainProjectId, int? mainProgramId, int? subProgramId, int? markazId, int? priorityId, int? statusId, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<SubProjectDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<SubProjectDetailDto> CreateAsync(CreateSubProjectDto dto, CancellationToken cancellationToken = default);

    Task<SubProjectDetailDto> UpdateAsync(int id, UpdateSubProjectDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<SubProjectDetailDto> AssignExecutiveAgencyAsync(int id, int executiveAgencyId, CancellationToken cancellationToken = default);
}