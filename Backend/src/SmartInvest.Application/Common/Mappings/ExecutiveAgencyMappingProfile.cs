using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class ExecutiveAgencyMappingProfile : Profile
{
    public ExecutiveAgencyMappingProfile()
    {
        CreateMap<ExecutiveAgency, ExecutiveAgencyDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ExecutiveAgencyId))
            .ForMember(dest => dest.UserName, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());
    }
}
