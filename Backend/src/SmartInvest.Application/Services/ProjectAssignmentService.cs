using AutoMapper;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class ProjectAssignmentService : IProjectAssignmentService
{
    private readonly IProjectAssignmentRepository _assignmentRepository;
    private readonly ISubProjectRepository _subProjectRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectAssignmentService(
        IProjectAssignmentRepository assignmentRepository,
        ISubProjectRepository subProjectRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _assignmentRepository = assignmentRepository;
        _subProjectRepository = subProjectRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ProjectAssignmentDto>> GetBySubProjectAsync(int subProjectId, CancellationToken cancellationToken = default)
    {
        var subProject = await GetSubProjectOrThrowAsync(subProjectId, cancellationToken);
        EnsureAgencyOwnership(subProject);

        var assignments = await _assignmentRepository.GetBySubProjectAsync(subProjectId, cancellationToken);
        return _mapper.Map<List<ProjectAssignmentDto>>(assignments);
    }

    public async Task<ProjectAssignmentDto> CreateAsync(int subProjectId, CreateProjectAssignmentDto dto, CancellationToken cancellationToken = default)
    {
        var subProject = await GetSubProjectOrThrowAsync(subProjectId, cancellationToken);
        EnsureAgencyOwnership(subProject);

        var assignment = new ProjectAssignment
        {
            SubProjectId = subProjectId,
            ContractorId = dto.ContractorId,
            ContractTypeId = dto.ContractTypeId,
            ContractNumber = dto.ContractNumber,
            ContractValue = dto.ContractValue,
            ExpectedStartDate = dto.ExpectedStartDate,
            ExpectedEndDate = dto.ExpectedEndDate,
            Notes = dto.Notes,
            AssignmentDate = DateTime.UtcNow,
            IsLocked = false,
        };

        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _assignmentRepository.GetWithDetailsAsync(assignment.AssignmentId, cancellationToken);
        return _mapper.Map<ProjectAssignmentDto>(created);
    }

    public async Task<ProjectAssignmentDto> UpdateGeneralAsync(int subProjectId, int id, UpdateProjectAssignmentDto dto, CancellationToken cancellationToken = default)
    {
        var subProject = await GetSubProjectOrThrowAsync(subProjectId, cancellationToken);
        EnsureAgencyOwnership(subProject);

        var assignment = await GetAssignmentOrThrowAsync(subProjectId, id, cancellationToken);
        if (assignment.IsLocked && _currentUser.Role != Roles.PlanningManager)
        {
            throw new BusinessRuleException("هذا التعيين مقفول ولا يمكن تعديله (تم تغيير الجهة التنفيذية للمشروع)");
        }

        assignment.ContractorId = dto.ContractorId;
        assignment.ContractTypeId = dto.ContractTypeId;
        assignment.ContractNumber = dto.ContractNumber;
        assignment.Notes = dto.Notes;

        _assignmentRepository.Update(assignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _assignmentRepository.GetWithDetailsAsync(id, cancellationToken);
        return _mapper.Map<ProjectAssignmentDto>(updated);
    }

    public async Task DeleteAsync(int subProjectId, int id, CancellationToken cancellationToken = default)
    {
        await GetSubProjectOrThrowAsync(subProjectId, cancellationToken);
        var assignment = await GetAssignmentOrThrowAsync(subProjectId, id, cancellationToken);

        _assignmentRepository.Remove(assignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<SubProject> GetSubProjectOrThrowAsync(int subProjectId, CancellationToken cancellationToken)
    {
        var subProject = await _subProjectRepository.GetByIdAsync(subProjectId, cancellationToken);
        if (subProject == null)
        {
            throw new NotFoundException($"المشروع الفرعي رقم {subProjectId} غير موجود");
        }

        return subProject;
    }

    private async Task<ProjectAssignment> GetAssignmentOrThrowAsync(int subProjectId, int id, CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(id, cancellationToken);
        if (assignment == null || assignment.SubProjectId != subProjectId)
        {
            throw new NotFoundException($"التعيين رقم {id} غير موجود");
        }

        return assignment;
    }

    /// <summary>
    /// مدير التخطيط وموظف التخطيط لهم تجاوز كامل. الجهة التنفيذية مقصورة على مشروعاتها فقط.
    /// </summary>
    private void EnsureAgencyOwnership(SubProject subProject)
    {
        if (_currentUser.Role != Roles.ExecutiveAgency)
        {
            return;
        }

        if (subProject.ExecutiveAgencyId == null || subProject.ExecutiveAgencyId != _currentUser.ExecutiveAgencyId)
        {
            throw new ForbiddenAccessException("لا يمكنك التعامل مع تعيينات مشروع غير مسند لجهتك");
        }
    }
}
