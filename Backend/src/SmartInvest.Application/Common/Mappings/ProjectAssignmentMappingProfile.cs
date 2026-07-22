using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class ProjectAssignmentMappingProfile : Profile
{
    public ProjectAssignmentMappingProfile()
    {
        CreateMap<ProjectAssignment, ProjectAssignmentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.AssignmentId))
            .ForMember(dest => dest.ExecutiveAgencyId, opt => opt.MapFrom(src => src.SubProject.ExecutiveAgencyId ?? 0))
            .ForMember(dest => dest.ExecutiveAgencyName, opt => opt.MapFrom(src => src.SubProject.ExecutiveAgency != null ? src.SubProject.ExecutiveAgency.AgencyName : string.Empty))
            .ForMember(dest => dest.ContractorName, opt => opt.MapFrom(src => src.Contractor != null ? src.Contractor.ContractorName : null))
            .ForMember(dest => dest.ContractTypeName, opt => opt.MapFrom(src => src.ContractType.ContractName));
    }
}
