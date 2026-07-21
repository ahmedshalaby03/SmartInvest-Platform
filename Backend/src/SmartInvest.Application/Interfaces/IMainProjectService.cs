using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IMainProjectService
{
    Task<IReadOnlyList<MainProjectListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<MainProjectDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<MainProjectDetailDto> CreateAsync(CreateMainProjectDto dto, CancellationToken cancellationToken = default);

    Task<MainProjectDetailDto> UpdateAsync(int id, UpdateMainProjectDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}