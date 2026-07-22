using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IProjectAssignmentService
{
    Task<IReadOnlyList<ProjectAssignmentDto>> GetBySubProjectAsync(int subProjectId, CancellationToken cancellationToken = default);

    Task<ProjectAssignmentDto> CreateAsync(int subProjectId, CreateProjectAssignmentDto dto, CancellationToken cancellationToken = default);

    Task<ProjectAssignmentDto> UpdateGeneralAsync(int subProjectId, int id, UpdateProjectAssignmentDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int subProjectId, int id, CancellationToken cancellationToken = default);
}
