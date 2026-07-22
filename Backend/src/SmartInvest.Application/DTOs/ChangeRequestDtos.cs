using SmartInvest.Domain.Enums;

namespace SmartInvest.Application.DTOs;

public class ChangeRequestDto
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public decimal? RequestedContractValue { get; set; }
    public DateTime? RequestedExpectedEndDate { get; set; }
    public ChangeRequestStatus Status { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

public class CreateChangeRequestDto
{
    public decimal? RequestedContractValue { get; set; }
    public DateTime? RequestedExpectedEndDate { get; set; }
}

public class ReviewChangeRequestDto
{
    public string? ReviewNote { get; set; }
}
