using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.PlanningManager)]
public class UsersController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public UsersController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _identityService.GetUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateEmployee(CreateEmployeeDto dto, CancellationToken cancellationToken)
    {
        var user = await _identityService.CreateEmployeeAsync(dto, cancellationToken);
        return Ok(user);
    }

    [HttpPut("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        await _identityService.ResetPasswordAsync(id, dto.NewPassword, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/activate")]
    public async Task<IActionResult> Activate(string id, CancellationToken cancellationToken)
    {
        await _identityService.SetActiveStatusAsync(id, true, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
    {
        await _identityService.SetActiveStatusAsync(id, false, cancellationToken);
        return NoContent();
    }
}