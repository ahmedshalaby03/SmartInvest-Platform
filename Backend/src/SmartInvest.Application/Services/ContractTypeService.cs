using AutoMapper;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class ContractTypeService : IContractTypeService
{
    private readonly IGenericRepository<ContractType> _contractTypeRepository;
    private readonly IGenericRepository<ProjectAssignment> _assignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ContractTypeService(
        IGenericRepository<ContractType> contractTypeRepository,
        IGenericRepository<ProjectAssignment> assignmentRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _contractTypeRepository = contractTypeRepository;
        _assignmentRepository = assignmentRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ContractTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var contractTypes = await _contractTypeRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<ContractTypeDto>>(contractTypes);
    }

    public async Task<ContractTypeDto> CreateAsync(CreateContractTypeDto dto, CancellationToken cancellationToken = default)
    {
        var contractType = new ContractType { ContractName = dto.ContractName };

        await _contractTypeRepository.AddAsync(contractType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ContractTypeDto>(contractType);
    }

    public async Task<ContractTypeDto> UpdateAsync(int id, UpdateContractTypeDto dto, CancellationToken cancellationToken = default)
    {
        var contractType = await _contractTypeRepository.GetByIdAsync(id, cancellationToken);
        if (contractType == null)
        {
            throw new NotFoundException($"نوع العقد رقم {id} غير موجود");
        }

        contractType.ContractName = dto.ContractName;

        _contractTypeRepository.Update(contractType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ContractTypeDto>(contractType);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var contractType = await _contractTypeRepository.GetByIdAsync(id, cancellationToken);
        if (contractType == null)
        {
            throw new NotFoundException($"نوع العقد رقم {id} غير موجود");
        }

        var linkedAssignments = await _assignmentRepository.FindAsync(x => x.ContractTypeId == id, cancellationToken);
        if (linkedAssignments.Count > 0)
        {
            throw new BusinessRuleException("لا يمكن حذف نوع العقد لوجود تعيينات مرتبطة به");
        }

        _contractTypeRepository.Remove(contractType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
