namespace SmartInvest.Application.DTOs;

public class UserDto
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class CreateEmployeeDto
{
    public string FullName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}