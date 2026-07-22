using AutoMapper;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class ContractorService : IContractorService
{
    private readonly IGenericRepository<Contractor> _contractorRepository;
    private readonly IGenericRepository<ProjectAssignment> _assignmentRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ContractorService(
        IGenericRepository<Contractor> contractorRepository,
        IGenericRepository<ProjectAssignment> assignmentRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _contractorRepository = contractorRepository;
        _assignmentRepository = assignmentRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ContractorDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var contractors = await _contractorRepository.GetAllAsync(cancellationToken);
        var result = new List<ContractorDto>();
        foreach (var contractor in contractors)
        {
            result.Add(await MapWithUserAsync(contractor, cancellationToken));
        }

        return result;
    }

    public async Task<ContractorDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var contractor = await GetOrThrowAsync(id, cancellationToken);
        return await MapWithUserAsync(contractor, cancellationToken);
    }

    public async Task<ContractorDto> CreateAsync(CreateContractorDto dto, CancellationToken cancellationToken = default)
    {
        var contractor = new Contractor
        {
            ContractorName = dto.ContractorName,
            CompanyType = dto.CompanyType,
            NationalIdOrCommercialRegister = dto.NationalIdOrCommercialRegister,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Category = dto.Category,
            IsActive = true,
        };

        await _contractorRepository.AddAsync(contractor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _identityService.CreateContractorUserAsync(
                dto.UserName, dto.Email, dto.PhoneNumber, dto.Password, contractor.ContractorId, cancellationToken);
        }
        catch
        {
            _contractorRepository.Remove(contractor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }

        return await MapWithUserAsync(contractor, cancellationToken);
    }

    public async Task<ContractorDto> UpdateAsync(int id, UpdateContractorDto dto, CancellationToken cancellationToken = default)
    {
        var contractor = await GetOrThrowAsync(id, cancellationToken);

        contractor.ContractorName = dto.ContractorName;
        contractor.CompanyType = dto.CompanyType;
        contractor.NationalIdOrCommercialRegister = dto.NationalIdOrCommercialRegister;
        contractor.PhoneNumber = dto.PhoneNumber;
        contractor.Email = dto.Email;
        contractor.Address = dto.Address;
        contractor.Category = dto.Category;
        contractor.IsActive = dto.IsActive;

        _contractorRepository.Update(contractor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapWithUserAsync(contractor, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var contractor = await GetOrThrowAsync(id, cancellationToken);

        var linkedAssignments = await _assignmentRepository.FindAsync(x => x.ContractorId == id, cancellationToken);
        if (linkedAssignments.Count > 0)
        {
            throw new BusinessRuleException("لا يمكن حذف المقاول لوجود تعيينات مرتبطة به");
        }

        await _identityService.DeleteUserByContractorIdAsync(id, cancellationToken);

        _contractorRepository.Remove(contractor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default)
    {
        await GetOrThrowAsync(id, cancellationToken);
        await _identityService.ResetPasswordForContractorAsync(id, newPassword, cancellationToken);
    }

    private async Task<Contractor> GetOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var contractor = await _contractorRepository.GetByIdAsync(id, cancellationToken);
        if (contractor == null)
        {
            throw new NotFoundException($"المقاول رقم {id} غير موجود");
        }

        return contractor;
    }

    private async Task<ContractorDto> MapWithUserAsync(Contractor contractor, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<ContractorDto>(contractor);
        var user = await _identityService.GetUserByContractorIdAsync(contractor.ContractorId, cancellationToken);
        dto.UserName = user?.UserName ?? string.Empty;
        return dto;
    }
}
