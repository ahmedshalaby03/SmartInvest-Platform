using AutoMapper;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class ProjectSpecificationService : IProjectSpecificationService
{
    private readonly IGenericRepository<ProjectSpecification> _specificationRepository;
    private readonly ISubProjectRepository _subProjectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectSpecificationService(
        IGenericRepository<ProjectSpecification> specificationRepository,
        ISubProjectRepository subProjectRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _specificationRepository = specificationRepository;
        _subProjectRepository = subProjectRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ProjectSpecificationDto>> GetBySubProjectAsync(int subProjectId, CancellationToken cancellationToken = default)
    {
        var specifications = await _specificationRepository.FindAsync(x => x.SubProjectId == subProjectId, cancellationToken);
        return _mapper.Map<List<ProjectSpecificationDto>>(specifications);
    }

    public async Task<ProjectSpecificationDto> CreateAsync(int subProjectId, CreateProjectSpecificationDto dto, CancellationToken cancellationToken = default)
    {
        var subProject = await _subProjectRepository.GetByIdAsync(subProjectId, cancellationToken);
        if (subProject == null)
        {
            throw new NotFoundException($"المشروع الفرعي رقم {subProjectId} غير موجود");
        }

        var specification = _mapper.Map<ProjectSpecification>(dto);
        specification.SubProjectId = subProjectId;

        await _specificationRepository.AddAsync(specification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProjectSpecificationDto>(specification);
    }

    public async Task<ProjectSpecificationDto> UpdateAsync(int id, UpdateProjectSpecificationDto dto, CancellationToken cancellationToken = default)
    {
        var specification = await _specificationRepository.GetByIdAsync(id, cancellationToken);
        if (specification == null)
        {
            throw new NotFoundException($"المواصفة رقم {id} غير موجودة");
        }

        specification.SpecificationName = dto.SpecificationName;
        specification.SpecificationValue = dto.SpecificationValue;
        specification.Unit = dto.Unit;

        _specificationRepository.Update(specification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProjectSpecificationDto>(specification);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var specification = await _specificationRepository.GetByIdAsync(id, cancellationToken);
        if (specification == null)
        {
            throw new NotFoundException($"المواصفة رقم {id} غير موجودة");
        }

        _specificationRepository.Remove(specification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}