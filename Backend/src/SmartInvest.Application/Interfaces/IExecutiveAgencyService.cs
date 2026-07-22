using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IExecutiveAgencyService
{
    Task<IReadOnlyList<ExecutiveAgencyDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ExecutiveAgencyDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ExecutiveAgencyDto> CreateAsync(CreateExecutiveAgencyDto dto, CancellationToken cancellationToken = default);

    Task<ExecutiveAgencyDto> UpdateAsync(int id, UpdateExecutiveAgencyDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default);
}
