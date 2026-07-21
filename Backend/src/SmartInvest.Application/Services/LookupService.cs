using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class LookupService : ILookupService
{
    private readonly IGenericRepository<ProjectPriority> _priorityRepository;
    private readonly IGenericRepository<ProjectStatus> _statusRepository;
    private readonly IGenericRepository<MainProgram> _mainProgramRepository;
    private readonly IGenericRepository<SubProgram> _subProgramRepository;
    private readonly IGenericRepository<Governorate> _governorateRepository;
    private readonly IGenericRepository<Markaz> _markazRepository;
    private readonly IGenericRepository<Village> _villageRepository;
    private readonly IMapper _mapper;

    public LookupService(
        IGenericRepository<ProjectPriority> priorityRepository,
        IGenericRepository<ProjectStatus> statusRepository,
        IGenericRepository<MainProgram> mainProgramRepository,
        IGenericRepository<SubProgram> subProgramRepository,
        IGenericRepository<Governorate> governorateRepository,
        IGenericRepository<Markaz> markazRepository,
        IGenericRepository<Village> villageRepository,
        IMapper mapper)
    {
        _priorityRepository = priorityRepository;
        _statusRepository = statusRepository;
        _mainProgramRepository = mainProgramRepository;
        _subProgramRepository = subProgramRepository;
        _governorateRepository = governorateRepository;
        _markazRepository = markazRepository;
        _villageRepository = villageRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<LookupDto>> GetPrioritiesAsync(CancellationToken cancellationToken = default)
    {
        var priorities = await _priorityRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<LookupDto>>(priorities);
    }

    public async Task<IReadOnlyList<LookupDto>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await _statusRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<LookupDto>>(statuses);
    }

    public async Task<IReadOnlyList<LookupDto>> GetMainProgramsAsync(CancellationToken cancellationToken = default)
    {
        var mainPrograms = await _mainProgramRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<LookupDto>>(mainPrograms);
    }

    public async Task<IReadOnlyList<SubProgramLookupDto>> GetSubProgramsAsync(int? mainProgramId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SubProgram> subPrograms;

        if (mainProgramId.HasValue)
        {
            subPrograms = await _subProgramRepository.FindAsync(x => x.ProgramId == mainProgramId.Value, cancellationToken);
        }
        else
        {
            subPrograms = await _subProgramRepository.GetAllAsync(cancellationToken);
        }

        return _mapper.Map<List<SubProgramLookupDto>>(subPrograms);
    }

    public async Task<IReadOnlyList<LookupDto>> GetGovernoratesAsync(CancellationToken cancellationToken = default)
    {
        var governorates = await _governorateRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<LookupDto>>(governorates);
    }

    public async Task<IReadOnlyList<MarkazLookupDto>> GetMarkazAsync(int? governorateId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Markaz> markazList;

        if (governorateId.HasValue)
        {
            markazList = await _markazRepository.FindAsync(x => x.GovernorateId == governorateId.Value, cancellationToken);
        }
        else
        {
            markazList = await _markazRepository.GetAllAsync(cancellationToken);
        }

        return _mapper.Map<List<MarkazLookupDto>>(markazList);
    }

    public async Task<IReadOnlyList<VillageLookupDto>> GetVillagesAsync(int? markazId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Village> villages;

        if (markazId.HasValue)
        {
            villages = await _villageRepository.FindAsync(x => x.MarkazId == markazId.Value, cancellationToken);
        }
        else
        {
            villages = await _villageRepository.GetAllAsync(cancellationToken);
        }

        return _mapper.Map<List<VillageLookupDto>>(villages);
    }
}