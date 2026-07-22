using Microsoft.EntityFrameworkCore;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;
using SmartInvest.Infrastructure.Data;

namespace SmartInvest.Infrastructure.Repositories;

public class SubProjectRepository : GenericRepository<SubProject>, ISubProjectRepository
{
    public SubProjectRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<SubProject?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.MainProject).ThenInclude(m => m.SubProgram).ThenInclude(sp => sp.MainProgram)
            .Include(x => x.Markaz).ThenInclude(m => m.Governorate)
            .Include(x => x.Priority)
            .Include(x => x.Status)
            .Include(x => x.ExecutiveAgency)
            .Include(x => x.ProjectSpecifications)
            .FirstOrDefaultAsync(x => x.SubProjectId == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<SubProject> Items, int TotalCount)> SearchAsync(
        int? mainProjectId,
        int? mainProgramId,
        int? subProgramId,
        int? markazId,
        int? priorityId,
        int? statusId,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(x => x.MainProject).ThenInclude(m => m.SubProgram).ThenInclude(sp => sp.MainProgram)
            .Include(x => x.Markaz)
            .Include(x => x.Priority)
            .Include(x => x.Status)
            .Include(x => x.ExecutiveAgency)
            .AsQueryable();

        if (mainProjectId.HasValue)
        {
            query = query.Where(x => x.MainProjectId == mainProjectId);
        }

        if (subProgramId.HasValue)
        {
            query = query.Where(x => x.MainProject.SubProgramId == subProgramId);
        }

        if (mainProgramId.HasValue)
        {
            query = query.Where(x => x.MainProject.SubProgram.ProgramId == mainProgramId);
        }

        if (markazId.HasValue)
        {
            query = query.Where(x => x.MarkazId == markazId);
        }

        if (priorityId.HasValue)
        {
            query = query.Where(x => x.PriorityId == priorityId);
        }

        if (statusId.HasValue)
        {
            query = query.Where(x => x.StatusId == statusId);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.SubProjectName.Contains(searchTerm) ||
                (x.SubProjectCode != null && x.SubProjectCode.Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.MainProjectId).ThenBy(x => x.SubProjectId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(x =>
            x.SubProjectCode == code && (excludeId == null || x.SubProjectId != excludeId),
            cancellationToken);
    }
}
