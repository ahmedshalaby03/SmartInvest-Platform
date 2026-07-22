using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IContractTypeService
{
    Task<IReadOnlyList<ContractTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ContractTypeDto> CreateAsync(CreateContractTypeDto dto, CancellationToken cancellationToken = default);

    Task<ContractTypeDto> UpdateAsync(int id, UpdateContractTypeDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
