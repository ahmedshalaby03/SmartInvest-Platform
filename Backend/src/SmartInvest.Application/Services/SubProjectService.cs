using AutoMapper;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.DTOs.Common;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class SubProjectService : ISubProjectService
{
    private readonly ISubProjectRepository _subProjectRepository;
    private readonly IMainProjectRepository _mainProjectRepository;
    private readonly IGenericRepository<Village> _villageRepository;
    private readonly IGenericRepository<ProjectPriority> _priorityRepository;
    private readonly IGenericRepository<ProjectStatus> _statusRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SubProjectService(
        ISubProjectRepository subProjectRepository,
        IMainProjectRepository mainProjectRepository,
        IGenericRepository<Village> villageRepository,
        IGenericRepository<ProjectPriority> priorityRepository,
        IGenericRepository<ProjectStatus> statusRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _subProjectRepository = subProjectRepository;
        _mainProjectRepository = mainProjectRepository;
        _villageRepository = villageRepository;
        _priorityRepository = priorityRepository;
        _statusRepository = statusRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<SubProjectListItemDto>> SearchAsync(int? mainProjectId, int? mainProgramId, int? subProgramId, int? markazId, int? villageId, int? priorityId, int? statusId, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var result = await _subProjectRepository.SearchAsync(mainProjectId, mainProgramId, subProgramId, markazId, villageId,
            priorityId, statusId, searchTerm, page, pageSize, cancellationToken);

        var pagedResult = new PagedResultDto<SubProjectListItemDto>
        {
            Items = _mapper.Map<List<SubProjectListItemDto>>(result.Items),
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize
        };

        return pagedResult;
    }

    public async Task<SubProjectDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var subProject = await _subProjectRepository.GetWithDetailsAsync(id, cancellationToken);
        if (subProject == null)
        {
            throw new NotFoundException($"المشروع الفرعي رقم {id} غير موجود");
        }

        return _mapper.Map<SubProjectDetailDto>(subProject);
    }

    public async Task<SubProjectDetailDto> CreateAsync(CreateSubProjectDto dto, CancellationToken cancellationToken = default)
    {
        await ValidateReferencesAsync(dto.MainProjectId, dto.VillageId, dto.PriorityId, dto.StatusId, cancellationToken);

        var subProject = _mapper.Map<SubProject>(dto);

        await _subProjectRepository.AddAsync(subProject, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _subProjectRepository.GetWithDetailsAsync(subProject.SubProjectId, cancellationToken);
        return _mapper.Map<SubProjectDetailDto>(created);
    }

    public async Task<SubProjectDetailDto> UpdateAsync(int id, UpdateSubProjectDto dto, CancellationToken cancellationToken = default)
    {
        var subProject = await _subProjectRepository.GetByIdAsync(id, cancellationToken);
        if (subProject == null)
        {
            throw new NotFoundException($"المشروع الفرعي رقم {id} غير موجود");
        }

        await ValidateReferencesAsync(subProject.MainProjectId, dto.VillageId, dto.PriorityId, dto.StatusId, cancellationToken);

        subProject.SubProjectName = dto.Name;
        subProject.ProjectLevel = dto.ProjectLevel;
        subProject.ComponentType = dto.ComponentType;
        subProject.AccountingUnit = dto.AccountingUnit;
        subProject.ProjectNature = dto.ProjectNature;
        subProject.VillageId = dto.VillageId;
        subProject.PriorityId = dto.PriorityId;
        subProject.StatusId = dto.StatusId;
        subProject.BankFunding = dto.BankFunding;
        subProject.SelfFunding = dto.SelfFunding;
        subProject.Latitude = dto.Latitude;
        subProject.Longitude = dto.Longitude;
        subProject.ProjectDescription = dto.Description;
        subProject.ProjectGoal = dto.Goal;
        subProject.SocialImpact = dto.SocialImpact;
        subProject.EconomicImpact = dto.EconomicImpact;
        subProject.EnvironmentalImpact = dto.EnvironmentalImpact;
        subProject.GreenInvestmentLink = dto.GreenInvestmentLink;

        _subProjectRepository.Update(subProject);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _subProjectRepository.GetWithDetailsAsync(id, cancellationToken);
        return _mapper.Map<SubProjectDetailDto>(updated);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var subProject = await _subProjectRepository.GetByIdAsync(id, cancellationToken);
        if (subProject == null)
        {
            throw new NotFoundException($"المشروع الفرعي رقم {id} غير موجود");
        }

        _subProjectRepository.Remove(subProject);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateReferencesAsync(int mainProjectId, int villageId, int priorityId, int statusId, CancellationToken cancellationToken)
    {
        var mainProject = await _mainProjectRepository.GetByIdAsync(mainProjectId, cancellationToken);
        if (mainProject == null)
        {
            throw new NotFoundException("المشروع الرئيسي المحدد غير موجود");
        }

        var village = await _villageRepository.GetByIdAsync(villageId, cancellationToken);
        if (village == null)
        {
            throw new NotFoundException("القرية المحددة غير موجودة");
        }

        var priority = await _priorityRepository.GetByIdAsync(priorityId, cancellationToken);
        if (priority == null)
        {
            throw new NotFoundException("الأولوية المحددة غير موجودة");
        }

        var status = await _statusRepository.GetByIdAsync(statusId, cancellationToken);
        if (status == null)
        {
            throw new NotFoundException("حالة المشروع المحددة غير موجودة");
        }
    }
}