using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IProjectSpecificationService
{
    Task<IReadOnlyList<ProjectSpecificationDto>> GetBySubProjectAsync(int subProjectId, CancellationToken cancellationToken = default);

    Task<ProjectSpecificationDto> CreateAsync(int subProjectId, CreateProjectSpecificationDto dto, CancellationToken cancellationToken = default);

    Task<ProjectSpecificationDto> UpdateAsync(int id, UpdateProjectSpecificationDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}