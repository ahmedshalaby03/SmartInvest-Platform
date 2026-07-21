using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class SubProjectMappingProfile : Profile
{
    public SubProjectMappingProfile()
    {
        CreateMap<SubProject, SubProjectListItemDto>()
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.SubProjectCode))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.SubProjectName))
            .ForMember(dest => dest.MainProjectCode, opt => opt.MapFrom(src => src.MainProject.MainProjectCode))
            .ForMember(dest => dest.MainProjectName, opt => opt.MapFrom(src => src.MainProject.MainProjectName))
            .ForMember(dest => dest.VillageName, opt => opt.MapFrom(src => src.Village.VillageName))
            .ForMember(dest => dest.PriorityName, opt => opt.MapFrom(src => src.Priority.Priority))
            .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.StatusName));

        CreateMap<SubProject, SubProjectDetailDto>()
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.SubProjectCode))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.SubProjectName))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.ProjectDescription))
            .ForMember(dest => dest.Goal, opt => opt.MapFrom(src => src.ProjectGoal))
            .ForMember(dest => dest.MainProjectName, opt => opt.MapFrom(src => src.MainProject.MainProjectName))
            .ForMember(dest => dest.VillageName, opt => opt.MapFrom(src => src.Village.VillageName))
            .ForMember(dest => dest.MarkazId, opt => opt.MapFrom(src => src.Village.MarkazId))
            .ForMember(dest => dest.MarkazName, opt => opt.MapFrom(src => src.Village.Markaz.MarkazName))
            .ForMember(dest => dest.GovernorateId, opt => opt.MapFrom(src => src.Village.Markaz.GovernorateId))
            .ForMember(dest => dest.GovernorateName, opt => opt.MapFrom(src => src.Village.Markaz.Governorate.GovernorateName))
            .ForMember(dest => dest.PriorityName, opt => opt.MapFrom(src => src.Priority.Priority))
            .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.StatusName))
            .ForMember(dest => dest.Specifications, opt => opt.MapFrom(src => src.ProjectSpecifications));

        CreateMap<CreateSubProjectDto, SubProject>()
            .ForMember(dest => dest.SubProjectName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ProjectDescription, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ProjectGoal, opt => opt.MapFrom(src => src.Goal));

        CreateMap<UpdateSubProjectDto, SubProject>()
            .ForMember(dest => dest.SubProjectName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ProjectDescription, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ProjectGoal, opt => opt.MapFrom(src => src.Goal));
    }
}