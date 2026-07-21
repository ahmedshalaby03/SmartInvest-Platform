using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class MainProjectMappingProfile : Profile
{
    public MainProjectMappingProfile()
    {
        CreateMap<MainProject, MainProjectListItemDto>()
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.MainProjectCode))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.MainProjectName))
            .ForMember(dest => dest.SubProgramName, opt => opt.MapFrom(src => src.SubProgram.SubProgramName))
            .ForMember(dest => dest.MainProgramName, opt => opt.MapFrom(src => src.SubProgram.MainProgram.ProgramName))
            .ForMember(dest => dest.SubProjectsCount, opt => opt.MapFrom(src => src.SubProjects.Count))
            .ForMember(dest => dest.TotalBankFunding, opt => opt.MapFrom(src => src.SubProjects.Sum(sp => sp.BankFunding)))
            .ForMember(dest => dest.TotalSelfFunding, opt => opt.MapFrom(src => src.SubProjects.Sum(sp => sp.SelfFunding)));

        CreateMap<MainProject, MainProjectDetailDto>()
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.MainProjectCode))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.MainProjectName))
            .ForMember(dest => dest.SubProgramName, opt => opt.MapFrom(src => src.SubProgram.SubProgramName))
            .ForMember(dest => dest.MainProgramName, opt => opt.MapFrom(src => src.SubProgram.MainProgram.ProgramName))
            .ForMember(dest => dest.SubProjects, opt => opt.MapFrom(src => src.SubProjects));

        CreateMap<CreateMainProjectDto, MainProject>()
            .ForMember(dest => dest.MainProjectCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.MainProjectName, opt => opt.MapFrom(src => src.Name));

        CreateMap<UpdateMainProjectDto, MainProject>()
            .ForMember(dest => dest.MainProjectCode, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.MainProjectName, opt => opt.MapFrom(src => src.Name));
    }
}