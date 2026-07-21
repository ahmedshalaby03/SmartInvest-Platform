using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface ILookupService
{
    Task<IReadOnlyList<LookupDto>> GetPrioritiesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupDto>> GetStatusesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupDto>> GetMainProgramsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubProgramLookupDto>> GetSubProgramsAsync(int? mainProgramId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupDto>> GetGovernoratesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarkazLookupDto>> GetMarkazAsync(int? governorateId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VillageLookupDto>> GetVillagesAsync(int? markazId, CancellationToken cancellationToken = default);
}