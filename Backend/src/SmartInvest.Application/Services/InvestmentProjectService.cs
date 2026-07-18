using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class InvestmentProjectService : IInvestmentProjectService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public InvestmentProjectService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<InvestmentProjectDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _unitOfWork.Repository<InvestmentProject>().GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyList<InvestmentProjectDto>>(projects);
    }

    public async Task<InvestmentProjectDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await _unitOfWork.Repository<InvestmentProject>().GetByIdAsync(id, cancellationToken);
        return project is null ? null : _mapper.Map<InvestmentProjectDto>(project);
    }

    public async Task<InvestmentProjectDto> CreateAsync(CreateInvestmentProjectDto dto, CancellationToken cancellationToken = default)
    {
        var project = _mapper.Map<InvestmentProject>(dto);

        await _unitOfWork.Repository<InvestmentProject>().AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InvestmentProjectDto>(project);
    }
}
