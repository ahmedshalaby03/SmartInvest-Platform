using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Enums;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class ChangeRequestService : IChangeRequestService
{
    private readonly IGenericRepository<ProjectAssignmentChangeRequest> _changeRequestRepository;
    private readonly IProjectAssignmentRepository _assignmentRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeRequestService(
        IGenericRepository<ProjectAssignmentChangeRequest> changeRequestRepository,
        IProjectAssignmentRepository assignmentRepository,
        ICurrentUserService currentUser,
        IAuditLogService auditLogService,
        IUnitOfWork unitOfWork)
    {
        _changeRequestRepository = changeRequestRepository;
        _assignmentRepository = assignmentRepository;
        _currentUser = currentUser;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ChangeRequestDto>> GetHistoryAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        // ملاحظة: لا نستخدم GetAssignmentOrThrowAsync هنا لأنها ترفض التعيينات المقفولة، وهذا مسار قراءة فقط
        // (القفل يمنع التعديل لا الاطلاع على السجل). يجب مع ذلك التحقق من الملكية لمنع تسرب بيانات بين الجهات.
        var assignment = await _assignmentRepository.GetWithDetailsAsync(assignmentId, cancellationToken);
        if (assignment == null)
        {
            throw new NotFoundException($"التعيين رقم {assignmentId} غير موجود");
        }

        EnsurePartyOwnership(assignment);

        var requests = await _changeRequestRepository.FindAsync(x => x.AssignmentId == assignmentId, cancellationToken);
        return requests
            .OrderByDescending(x => x.RequestedAt)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ChangeRequestDto> SubmitAsync(int assignmentId, CreateChangeRequestDto dto, CancellationToken cancellationToken = default)
    {
        var assignment = await GetAssignmentOrThrowAsync(assignmentId, cancellationToken);
        EnsurePartyOwnership(assignment);

        var pending = await _changeRequestRepository.FindAsync(
            x => x.AssignmentId == assignmentId && x.Status == ChangeRequestStatus.Pending, cancellationToken);
        if (pending.Count > 0)
        {
            throw new BusinessRuleException("يوجد طلب تعديل قيد الانتظار بالفعل لهذا التعيين، يجب حسمه أولاً");
        }

        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("المستخدم غير معروف");
        var isContractor = _currentUser.Role == Roles.Contractor;

        var changeRequest = new ProjectAssignmentChangeRequest
        {
            AssignmentId = assignmentId,
            RequestedContractValue = dto.RequestedContractValue,
            RequestedExpectedEndDate = dto.RequestedExpectedEndDate,
            RequestedByUserId = userId,
            RequestedAt = DateTime.UtcNow,
            Status = isContractor ? ChangeRequestStatus.Pending : ChangeRequestStatus.Approved,
        };

        if (!isContractor)
        {
            changeRequest.ReviewedByUserId = userId;
            changeRequest.ReviewedAt = DateTime.UtcNow;
        }

        await _changeRequestRepository.AddAsync(changeRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!isContractor)
        {
            await ApplyToAssignmentAsync(assignment, changeRequest, userId, cancellationToken);
        }

        return MapToDto(changeRequest);
    }

    public async Task<ChangeRequestDto> ApproveAsync(int assignmentId, int changeRequestId, ReviewChangeRequestDto dto, CancellationToken cancellationToken = default)
    {
        var assignment = await GetAssignmentOrThrowAsync(assignmentId, cancellationToken);
        EnsureReviewerOwnership(assignment);

        var changeRequest = await GetPendingOrThrowAsync(assignmentId, changeRequestId, cancellationToken);
        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("المستخدم غير معروف");

        changeRequest.Status = ChangeRequestStatus.Approved;
        changeRequest.ReviewedByUserId = userId;
        changeRequest.ReviewedAt = DateTime.UtcNow;
        changeRequest.ReviewNote = dto.ReviewNote;

        _changeRequestRepository.Update(changeRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await ApplyToAssignmentAsync(assignment, changeRequest, userId, cancellationToken);

        return MapToDto(changeRequest);
    }

    public async Task<ChangeRequestDto> RejectAsync(int assignmentId, int changeRequestId, ReviewChangeRequestDto dto, CancellationToken cancellationToken = default)
    {
        var assignment = await GetAssignmentOrThrowAsync(assignmentId, cancellationToken);
        EnsureReviewerOwnership(assignment);

        var changeRequest = await GetPendingOrThrowAsync(assignmentId, changeRequestId, cancellationToken);
        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("المستخدم غير معروف");

        changeRequest.Status = ChangeRequestStatus.Rejected;
        changeRequest.ReviewedByUserId = userId;
        changeRequest.ReviewedAt = DateTime.UtcNow;
        changeRequest.ReviewNote = dto.ReviewNote;

        _changeRequestRepository.Update(changeRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(changeRequest);
    }

    private async Task ApplyToAssignmentAsync(ProjectAssignment assignment, ProjectAssignmentChangeRequest changeRequest, string userId, CancellationToken cancellationToken)
    {
        if (changeRequest.RequestedContractValue.HasValue && changeRequest.RequestedContractValue != assignment.ContractValue)
        {
            await _auditLogService.LogAsync(
                nameof(ProjectAssignment), assignment.AssignmentId, nameof(ProjectAssignment.ContractValue),
                assignment.ContractValue?.ToString(), changeRequest.RequestedContractValue.Value.ToString(), userId, cancellationToken);
            assignment.ContractValue = changeRequest.RequestedContractValue;
        }

        if (changeRequest.RequestedExpectedEndDate.HasValue && changeRequest.RequestedExpectedEndDate != assignment.ExpectedEndDate)
        {
            await _auditLogService.LogAsync(
                nameof(ProjectAssignment), assignment.AssignmentId, nameof(ProjectAssignment.ExpectedEndDate),
                assignment.ExpectedEndDate.ToString("O"), changeRequest.RequestedExpectedEndDate.Value.ToString("O"), userId, cancellationToken);
            assignment.ExpectedEndDate = changeRequest.RequestedExpectedEndDate.Value;
        }

        _assignmentRepository.Update(assignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<ProjectAssignment> GetAssignmentOrThrowAsync(int assignmentId, CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository.GetWithDetailsAsync(assignmentId, cancellationToken);
        if (assignment == null)
        {
            throw new NotFoundException($"التعيين رقم {assignmentId} غير موجود");
        }

        if (assignment.IsLocked && _currentUser.Role != Roles.PlanningManager)
        {
            throw new BusinessRuleException("هذا التعيين مقفول ولا يمكن تعديله (تم تغيير الجهة التنفيذية للمشروع)");
        }

        return assignment;
    }

    private async Task<ProjectAssignmentChangeRequest> GetPendingOrThrowAsync(int assignmentId, int changeRequestId, CancellationToken cancellationToken)
    {
        var changeRequest = await _changeRequestRepository.GetByIdAsync(changeRequestId, cancellationToken);
        if (changeRequest == null || changeRequest.AssignmentId != assignmentId)
        {
            throw new NotFoundException($"طلب التعديل رقم {changeRequestId} غير موجود");
        }

        if (changeRequest.Status != ChangeRequestStatus.Pending)
        {
            throw new BusinessRuleException("تم حسم هذا الطلب بالفعل");
        }

        return changeRequest;
    }

    /// <summary>يسمح بتقديم طلب: تخطيط (تجاوز كامل)، الجهة المسندة، أو المقاول المسند لنفس التعيين.</summary>
    private void EnsurePartyOwnership(ProjectAssignment assignment)
    {
        switch (_currentUser.Role)
        {
            case Roles.ExecutiveAgency:
                if (assignment.SubProject.ExecutiveAgencyId != _currentUser.ExecutiveAgencyId)
                {
                    throw new ForbiddenAccessException("لا يمكنك التعامل مع تعيين مشروع غير مسند لجهتك");
                }

                break;
            case Roles.Contractor:
                if (assignment.ContractorId != _currentUser.ContractorId)
                {
                    throw new ForbiddenAccessException("لا يمكنك التعامل مع تعيين غير مسند إليك");
                }

                break;
        }
    }

    /// <summary>الموافقة/الرفض مقصورة على تخطيط (تجاوز كامل) أو الجهة المسندة — المقاول لا يراجع طلباته.</summary>
    private void EnsureReviewerOwnership(ProjectAssignment assignment)
    {
        if (_currentUser.Role == Roles.ExecutiveAgency && assignment.SubProject.ExecutiveAgencyId != _currentUser.ExecutiveAgencyId)
        {
            throw new ForbiddenAccessException("لا يمكنك مراجعة طلب تعديل لمشروع غير مسند لجهتك");
        }

        if (_currentUser.Role == Roles.Contractor)
        {
            throw new ForbiddenAccessException("لا يحق للمقاول مراجعة طلبات التعديل");
        }
    }

    private static ChangeRequestDto MapToDto(ProjectAssignmentChangeRequest entity)
    {
        return new ChangeRequestDto
        {
            Id = entity.ChangeRequestId,
            AssignmentId = entity.AssignmentId,
            RequestedContractValue = entity.RequestedContractValue,
            RequestedExpectedEndDate = entity.RequestedExpectedEndDate,
            Status = entity.Status,
            RequestedByUserId = entity.RequestedByUserId,
            RequestedAt = entity.RequestedAt,
            ReviewedByUserId = entity.ReviewedByUserId,
            ReviewedAt = entity.ReviewedAt,
            ReviewNote = entity.ReviewNote,
        };
    }
}
