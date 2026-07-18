using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<InvestmentProject, InvestmentProjectDto>();
        CreateMap<CreateInvestmentProjectDto, InvestmentProject>();
    }
}
