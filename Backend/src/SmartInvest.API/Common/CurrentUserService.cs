using System.Security.Claims;
using SmartInvest.Application.Interfaces;

namespace SmartInvest.API.Common;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public int? ExecutiveAgencyId => ParseIntClaim("executiveAgencyId");

    public int? ContractorId => ParseIntClaim("contractorId");

    private int? ParseIntClaim(string claimType)
    {
        var value = User?.FindFirstValue(claimType);
        return int.TryParse(value, out var id) ? id : null;
    }
}
