using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IIdentityService
{
    Task<AuthResultDto> LoginAsync(string usernameOrEmail, string password, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    Task<UserDto> CreateEmployeeAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default);

    Task SetActiveStatusAsync(string userId, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
}