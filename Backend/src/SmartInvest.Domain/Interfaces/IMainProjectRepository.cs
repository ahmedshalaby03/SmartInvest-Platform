using SmartInvest.Domain.Entities;

namespace SmartInvest.Domain.Interfaces;

public interface IMainProjectRepository : IGenericRepository<MainProject>
{
    Task<MainProject?> GetWithSubProjectsAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}