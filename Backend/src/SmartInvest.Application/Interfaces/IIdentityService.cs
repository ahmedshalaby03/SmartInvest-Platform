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

    Task<UserDto> CreateAgencyUserAsync(string userName, string email, string? phoneNumber, string password, int executiveAgencyId, CancellationToken cancellationToken = default);

    Task<UserDto> CreateContractorUserAsync(string userName, string email, string? phoneNumber, string password, int contractorId, CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserByExecutiveAgencyIdAsync(int executiveAgencyId, CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserByContractorIdAsync(int contractorId, CancellationToken cancellationToken = default);

    Task ResetPasswordForAgencyAsync(int executiveAgencyId, string newPassword, CancellationToken cancellationToken = default);

    Task ResetPasswordForContractorAsync(int contractorId, string newPassword, CancellationToken cancellationToken = default);

    Task DeleteUserByExecutiveAgencyIdAsync(int executiveAgencyId, CancellationToken cancellationToken = default);

    Task DeleteUserByContractorIdAsync(int contractorId, CancellationToken cancellationToken = default);
}