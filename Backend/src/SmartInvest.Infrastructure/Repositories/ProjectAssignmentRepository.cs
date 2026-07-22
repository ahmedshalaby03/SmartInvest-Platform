using Microsoft.EntityFrameworkCore;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;
using SmartInvest.Infrastructure.Data;

namespace SmartInvest.Infrastructure.Repositories;

public class ProjectAssignmentRepository : GenericRepository<ProjectAssignment>, IProjectAssignmentRepository
{
    public ProjectAssignmentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<ProjectAssignment?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.SubProject).ThenInclude(s => s.ExecutiveAgency)
            .Include(x => x.Contractor)
            .Include(x => x.ContractType)
            .FirstOrDefaultAsync(x => x.AssignmentId == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectAssignment>> GetBySubProjectAsync(int subProjectId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.SubProject).ThenInclude(s => s.ExecutiveAgency)
            .Include(x => x.Contractor)
            .Include(x => x.ContractType)
            .Where(x => x.SubProjectId == subProjectId)
            .OrderByDescending(x => x.AssignmentDate)
            .ToListAsync(cancellationToken);
    }
}
