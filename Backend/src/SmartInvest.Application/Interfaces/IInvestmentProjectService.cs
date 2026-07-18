using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IInvestmentProjectService
{
    Task<IReadOnlyList<InvestmentProjectDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<InvestmentProjectDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<InvestmentProjectDto> CreateAsync(CreateInvestmentProjectDto dto, CancellationToken cancellationToken = default);
}
