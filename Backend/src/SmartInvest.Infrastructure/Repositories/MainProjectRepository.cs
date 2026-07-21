using Microsoft.EntityFrameworkCore;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;
using SmartInvest.Infrastructure.Data;

namespace SmartInvest.Infrastructure.Repositories;

public class MainProjectRepository : GenericRepository<MainProject>, IMainProjectRepository
{
    public MainProjectRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<MainProject?> GetWithSubProjectsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.SubProgram).ThenInclude(sp => sp.MainProgram)
            .Include(x => x.SubProjects).ThenInclude(sp => sp.Priority)
            .Include(x => x.SubProjects).ThenInclude(sp => sp.Status)
            .FirstOrDefaultAsync(x => x.MainProjectId == id, cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(x =>
            x.MainProjectCode == code && (excludeId == null || x.MainProjectId != excludeId),
            cancellationToken);
    }
}