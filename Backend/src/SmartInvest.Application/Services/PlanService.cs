using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class PlanService : IPlanService
{
    private readonly IGenericRepository<Plan> _planRepository;
    private readonly IGenericRepository<PlanProject> _planProjectRepository;
    private readonly IGenericRepository<FinancialYear> _financialYearRepository;
    private readonly IGenericRepository<SubProject> _subProjectRepository;
    private readonly IGenericRepository<MainProject> _mainProjectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PlanService(
        IGenericRepository<Plan> planRepository,
        IGenericRepository<PlanProject> planProjectRepository,
        IGenericRepository<FinancialYear> financialYearRepository,
        IGenericRepository<SubProject> subProjectRepository,
        IGenericRepository<MainProject> mainProjectRepository,
        IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _planProjectRepository = planProjectRepository;
        _financialYearRepository = financialYearRepository;
        _subProjectRepository = subProjectRepository;
        _mainProjectRepository = mainProjectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PlanDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _planRepository.FindAsync(_ => true, cancellationToken);
        var result = new List<PlanDto>();

        foreach (var plan in plans)
        {
            result.Add(await MapPlanAsync(plan, cancellationToken));
        }

        return result;
    }

    public async Task<PlanDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var plan = await GetOrThrowAsync(id, cancellationToken);
        return await MapPlanDetailAsync(plan, cancellationToken);
    }

    public async Task<PlanDto> CreateAsync(CreatePlanDto dto, CancellationToken cancellationToken = default)
    {
        var year = await _financialYearRepository.GetByIdAsync(dto.FinancialYearId, cancellationToken);
        if (year == null)
        {
            throw new NotFoundException("السنة المالية المحددة غير موجودة");
        }

        var plan = new Plan
        {
            PlanName = dto.PlanName,
            PlanStatus = "مسودة",
            SuggestionDate = DateTime.UtcNow,
            FinancialYearId = dto.FinancialYearId,
        };

        await _planRepository.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapPlanAsync(plan, cancellationToken);
    }

    public async Task<PlanDto> UpdateAsync(int id, UpdatePlanDto dto, CancellationToken cancellationToken = default)
    {
        var plan = await GetOrThrowAsync(id, cancellationToken);

        plan.PlanName = dto.PlanName;

        _planRepository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapPlanAsync(plan, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var plan = await GetOrThrowAsync(id, cancellationToken);

        var suggestedProjects = await _planProjectRepository.FindAsync(x => x.PlanId == id, cancellationToken);
        foreach (var suggested in suggestedProjects)
        {
            _planProjectRepository.Remove(suggested);
        }

        _planRepository.Remove(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlanDetailDto> AddSuggestedProjectAsync(int planId, int subProjectId, CancellationToken cancellationToken = default)
    {
        var plan = await GetOrThrowAsync(planId, cancellationToken);

        var subProject = await _subProjectRepository.GetByIdAsync(subProjectId, cancellationToken);
        if (subProject == null)
        {
            throw new NotFoundException($"المشروع الفرعي رقم {subProjectId} غير موجود");
        }

        var existing = await _planProjectRepository.FindAsync(
            x => x.PlanId == planId && x.SubProjectId == subProjectId, cancellationToken);
        if (existing.Count > 0)
        {
            throw new BusinessRuleException("المشروع الفرعي مضاف بالفعل لقائمة المشروعات المقترحة في هذه الخطة");
        }

        await _planProjectRepository.AddAsync(new PlanProject { PlanId = planId, SubProjectId = subProjectId }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapPlanDetailAsync(plan, cancellationToken);
    }

    public async Task RemoveSuggestedProjectAsync(int planId, int subProjectId, CancellationToken cancellationToken = default)
    {
        await GetOrThrowAsync(planId, cancellationToken);

        var link = (await _planProjectRepository.FindAsync(
            x => x.PlanId == planId && x.SubProjectId == subProjectId, cancellationToken))
            .FirstOrDefault();

        if (link == null)
        {
            throw new NotFoundException("المشروع الفرعي غير موجود في قائمة المقترحات لهذه الخطة");
        }

        _planProjectRepository.Remove(link);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlanDto> ApproveAsync(int planId, DateTime approvalDate, CancellationToken cancellationToken = default)
    {
        var plan = await GetOrThrowAsync(planId, cancellationToken);

        if (plan.ApprovalDate.HasValue)
        {
            throw new BusinessRuleException("تم اعتماد هذه الخطة بالفعل");
        }

        plan.ApprovalDate = approvalDate;
        plan.PlanStatus = "معتمدة";

        _planRepository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapPlanAsync(plan, cancellationToken);
    }

    private async Task<Plan> GetOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(id, cancellationToken);
        if (plan == null)
        {
            throw new NotFoundException($"الخطة رقم {id} غير موجودة");
        }

        return plan;
    }

    private async Task<PlanDto> MapPlanAsync(Plan plan, CancellationToken cancellationToken)
    {
        var year = await _financialYearRepository.GetByIdAsync(plan.FinancialYearId, cancellationToken);
        return new PlanDto
        {
            Id = plan.PlanId,
            PlanName = plan.PlanName,
            PlanStatus = plan.PlanStatus,
            SuggestionDate = plan.SuggestionDate,
            ApprovalDate = plan.ApprovalDate,
            FinancialYearId = plan.FinancialYearId,
            FinancialYearName = year?.Name ?? string.Empty,
        };
    }

    private async Task<PlanDetailDto> MapPlanDetailAsync(Plan plan, CancellationToken cancellationToken)
    {
        var year = await _financialYearRepository.GetByIdAsync(plan.FinancialYearId, cancellationToken);
        var links = await _planProjectRepository.FindAsync(x => x.PlanId == plan.PlanId, cancellationToken);

        var suggested = new List<PlanSuggestedProjectDto>();
        foreach (var link in links)
        {
            var subProject = await _subProjectRepository.GetByIdAsync(link.SubProjectId, cancellationToken);
            if (subProject == null)
            {
                continue;
            }

            var mainProject = await _mainProjectRepository.GetByIdAsync(subProject.MainProjectId, cancellationToken);

            suggested.Add(new PlanSuggestedProjectDto
            {
                SubProjectId = subProject.SubProjectId,
                SubProjectName = subProject.SubProjectName,
                SubProjectCode = subProject.SubProjectCode,
                MainProjectName = mainProject?.MainProjectName ?? string.Empty,
                BankFunding = subProject.BankFunding,
                SelfFunding = subProject.SelfFunding,
                TotalCost = subProject.TotalCost,
            });
        }

        return new PlanDetailDto
        {
            Id = plan.PlanId,
            PlanName = plan.PlanName,
            PlanStatus = plan.PlanStatus,
            SuggestionDate = plan.SuggestionDate,
            ApprovalDate = plan.ApprovalDate,
            FinancialYearId = plan.FinancialYearId,
            FinancialYearName = year?.Name ?? string.Empty,
            SuggestedProjects = suggested,
        };
    }
}
