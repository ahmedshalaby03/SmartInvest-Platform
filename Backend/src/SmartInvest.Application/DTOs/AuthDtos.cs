namespace SmartInvest.Application.DTOs;

public class LoginDto
{
    public string UsernameOrEmail { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class AuthResultDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}