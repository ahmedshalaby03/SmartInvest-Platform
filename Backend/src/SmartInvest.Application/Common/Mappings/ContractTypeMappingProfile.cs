using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class ContractTypeMappingProfile : Profile
{
    public ContractTypeMappingProfile()
    {
        CreateMap<ContractType, ContractTypeDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ContractTypeId));
    }
}
