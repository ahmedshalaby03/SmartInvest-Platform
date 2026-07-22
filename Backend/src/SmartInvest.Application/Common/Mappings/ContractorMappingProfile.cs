using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class ContractorMappingProfile : Profile
{
    public ContractorMappingProfile()
    {
        CreateMap<Contractor, ContractorDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ContractorId))
            .ForMember(dest => dest.UserName, opt => opt.Ignore());
    }
}
