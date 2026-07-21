using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class LookupMappingProfile : Profile
{
    public LookupMappingProfile()
    {
        CreateMap<ProjectPriority, LookupDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Priority));

        CreateMap<ProjectStatus, LookupDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.StatusName));

        CreateMap<MainProgram, LookupDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ProgramName));

        CreateMap<Governorate, LookupDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.GovernorateName));

        CreateMap<SubProgram, SubProgramLookupDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.SubProgramName))
            .ForMember(dest => dest.MainProgramId, opt => opt.MapFrom(src => src.ProgramId));

        CreateMap<Markaz, MarkazLookupDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.MarkazName));

        CreateMap<Village, VillageLookupDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.VillageName));
    }
}