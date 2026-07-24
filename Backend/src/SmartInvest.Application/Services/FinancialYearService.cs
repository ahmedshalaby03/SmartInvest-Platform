using AutoMapper;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class FinancialYearService : IFinancialYearService
{
    private readonly IGenericRepository<FinancialYear> _financialYearRepository;
    private readonly IGenericRepository<SubProjectFinancialYear> _subProjectFinancialYearRepository;
    private readonly IGenericRepository<Plan> _planRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FinancialYearService(
        IGenericRepository<FinancialYear> financialYearRepository,
        IGenericRepository<SubProjectFinancialYear> subProjectFinancialYearRepository,
        IGenericRepository<Plan> planRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _financialYearRepository = financialYearRepository;
        _subProjectFinancialYearRepository = subProjectFinancialYearRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<FinancialYearDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var years = await _financialYearRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<FinancialYearDto>>(years);
    }

    public async Task<FinancialYearDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var year = await GetOrThrowAsync(id, cancellationToken);
        return _mapper.Map<FinancialYearDto>(year);
    }

    public async Task<FinancialYearDto> CreateAsync(CreateFinancialYearDto dto, CancellationToken cancellationToken = default)
    {
        var year = new FinancialYear
        {
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsClosed = false,
            Budget = dto.Budget,
        };

        await _financialYearRepository.AddAsync(year, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<FinancialYearDto>(year);
    }

    public async Task<FinancialYearDto> UpdateAsync(int id, UpdateFinancialYearDto dto, CancellationToken cancellationToken = default)
    {
        var year = await GetOrThrowAsync(id, cancellationToken);

        year.Name = dto.Name;
        year.StartDate = dto.StartDate;
        year.EndDate = dto.EndDate;
        year.IsClosed = dto.IsClosed;
        year.Budget = dto.Budget;

        _financialYearRepository.Update(year);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<FinancialYearDto>(year);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var year = await GetOrThrowAsync(id, cancellationToken);

        var linkedSubProjects = await _subProjectFinancialYearRepository.FindAsync(x => x.FinancialYearId == id, cancellationToken);
        if (linkedSubProjects.Count > 0)
        {
            throw new BusinessRuleException("لا يمكن حذف السنة المالية لوجود مشروعات فرعية مرتبطة بها");
        }

        var linkedPlans = await _planRepository.FindAsync(x => x.FinancialYearId == id, cancellationToken);
        if (linkedPlans.Count > 0)
        {
            throw new BusinessRuleException("لا يمكن حذف السنة المالية لوجود خطط مرتبطة بها");
        }

        _financialYearRepository.Remove(year);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<FinancialYear> GetOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var year = await _financialYearRepository.GetByIdAsync(id, cancellationToken);
        if (year == null)
        {
            throw new NotFoundException($"السنة المالية رقم {id} غير موجودة");
        }

        return year;
    }
}
