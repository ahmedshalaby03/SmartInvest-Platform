using SmartInvest.Domain.Entities;

namespace SmartInvest.Domain.Interfaces;

public interface IProjectAssignmentRepository : IGenericRepository<ProjectAssignment>
{
    Task<ProjectAssignment?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectAssignment>> GetBySubProjectAsync(int subProjectId, CancellationToken cancellationToken = default);
}
