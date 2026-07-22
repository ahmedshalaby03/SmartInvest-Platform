using AutoMapper;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class ExecutiveAgencyService : IExecutiveAgencyService
{
    private readonly IGenericRepository<ExecutiveAgency> _agencyRepository;
    private readonly ISubProjectRepository _subProjectRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ExecutiveAgencyService(
        IGenericRepository<ExecutiveAgency> agencyRepository,
        ISubProjectRepository subProjectRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _agencyRepository = agencyRepository;
        _subProjectRepository = subProjectRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ExecutiveAgencyDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var agencies = await _agencyRepository.GetAllAsync(cancellationToken);
        var result = new List<ExecutiveAgencyDto>();
        foreach (var agency in agencies)
        {
            result.Add(await MapWithUserAsync(agency, cancellationToken));
        }

        return result;
    }

    public async Task<ExecutiveAgencyDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var agency = await GetOrThrowAsync(id, cancellationToken);
        return await MapWithUserAsync(agency, cancellationToken);
    }

    public async Task<ExecutiveAgencyDto> CreateAsync(CreateExecutiveAgencyDto dto, CancellationToken cancellationToken = default)
    {
        var agency = new ExecutiveAgency
        {
            AgencyName = dto.AgencyName,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
        };

        await _agencyRepository.AddAsync(agency, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _identityService.CreateAgencyUserAsync(
                dto.UserName, dto.Email, dto.Phone, dto.Password, agency.ExecutiveAgencyId, cancellationToken);
        }
        catch
        {
            _agencyRepository.Remove(agency);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }

        return await MapWithUserAsync(agency, cancellationToken);
    }

    public async Task<ExecutiveAgencyDto> UpdateAsync(int id, UpdateExecutiveAgencyDto dto, CancellationToken cancellationToken = default)
    {
        var agency = await GetOrThrowAsync(id, cancellationToken);

        agency.AgencyName = dto.AgencyName;
        agency.Phone = dto.Phone;
        agency.Email = dto.Email;
        agency.Address = dto.Address;

        _agencyRepository.Update(agency);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapWithUserAsync(agency, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var agency = await GetOrThrowAsync(id, cancellationToken);

        var linkedSubProjects = await _subProjectRepository.FindAsync(x => x.ExecutiveAgencyId == id, cancellationToken);
        if (linkedSubProjects.Count > 0)
        {
            throw new BusinessRuleException("لا يمكن حذف الجهة لوجود مشروعات فرعية مسندة إليها");
        }

        await _identityService.DeleteUserByExecutiveAgencyIdAsync(id, cancellationToken);

        _agencyRepository.Remove(agency);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default)
    {
        await GetOrThrowAsync(id, cancellationToken);
        await _identityService.ResetPasswordForAgencyAsync(id, newPassword, cancellationToken);
    }

    private async Task<ExecutiveAgency> GetOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var agency = await _agencyRepository.GetByIdAsync(id, cancellationToken);
        if (agency == null)
        {
            throw new NotFoundException($"الجهة التنفيذية رقم {id} غير موجودة");
        }

        return agency;
    }

    private async Task<ExecutiveAgencyDto> MapWithUserAsync(ExecutiveAgency agency, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<ExecutiveAgencyDto>(agency);
        var user = await _identityService.GetUserByExecutiveAgencyIdAsync(agency.ExecutiveAgencyId, cancellationToken);
        dto.UserName = user?.UserName ?? string.Empty;
        dto.IsActive = user?.IsActive ?? false;
        return dto;
    }
}
