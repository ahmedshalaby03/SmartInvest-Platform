using SmartInvest.Domain.Enums;

namespace SmartInvest.Application.DTOs;

public record InvestmentProjectDto(
    int Id,
    string Title,
    string Description,
    decimal TargetAmount,
    string Sector,
    string Location,
    ProjectStatus Status);

public record CreateInvestmentProjectDto(
    string Title,
    string Description,
    decimal TargetAmount,
    string Sector,
    string Location);
