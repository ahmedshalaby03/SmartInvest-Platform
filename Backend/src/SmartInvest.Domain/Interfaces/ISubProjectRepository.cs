using SmartInvest.Domain.Entities;

namespace SmartInvest.Domain.Interfaces;

public interface ISubProjectRepository : IGenericRepository<SubProject>
{
    Task<SubProject?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SubProject> Items, int TotalCount)> SearchAsync(int? mainProjectId, int? mainProgramId, int? subProgramId, int? markazId, int? villageId, int? priorityId, int? statusId, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}