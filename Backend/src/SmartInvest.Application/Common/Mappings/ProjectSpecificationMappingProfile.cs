using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class ProjectSpecificationMappingProfile : Profile
{
    public ProjectSpecificationMappingProfile()
    {
        CreateMap<ProjectSpecification, ProjectSpecificationDto>();

        CreateMap<CreateProjectSpecificationDto, ProjectSpecification>();

        CreateMap<UpdateProjectSpecificationDto, ProjectSpecification>();
    }
}