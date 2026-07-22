using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartInvest.Application.Common;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartInvest.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;

    public IdentityService(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtOptions)
    {
        _userManager = userManager;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthResultDto> LoginAsync(string usernameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(usernameOrEmail);
        if (user == null)
        {
            user = await _userManager.FindByEmailAsync(usernameOrEmail);
        }

        if (user == null)
        {
            throw new BusinessRuleException("بيانات الدخول غير صحيحة");
        }

        if (!user.IsActive)
        {
            throw new BusinessRuleException("هذا الحساب معطّل، برجاء التواصل مع مدير التخطيط");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            throw new BusinessRuleException("بيانات الدخول غير صحيحة");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);
        var token = GenerateJwtToken(user, role, expiresAt);

        var result = new AuthResultDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = role
        };

        return result;
    }

    public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("المستخدم غير موجود");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(" - ", result.Errors.Select(e => e.Description));
            throw new BusinessRuleException(errors);
        }
    }

    public async Task<UserDto> CreateEmployeeAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Role != Roles.PlanningEmployee && dto.Role != Roles.PlanningManager)
        {
            throw new BusinessRuleException("الدور الوظيفي غير صحيح");
        }

        var user = new ApplicationUser
        {
            UserName = dto.UserName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            FullName = dto.FullName,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" - ", createResult.Errors.Select(e => e.Description));
            throw new BusinessRuleException(errors);
        }

        await _userManager.AddToRoleAsync(user, dto.Role);

        var userDto = new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = dto.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };

        return userDto;
    }

    public async Task ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("المستخدم غير موجود");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(" - ", result.Errors.Select(e => e.Description));
            throw new BusinessRuleException(errors);
        }
    }

    public async Task SetActiveStatusAsync(string userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("المستخدم غير موجود");
        }

        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            result.Add(userDto);
        }

        return result;
    }

    private string GenerateJwtToken(ApplicationUser user, string role, DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.ExecutiveAgencyId.HasValue)
        {
            claims.Add(new Claim("executiveAgencyId", user.ExecutiveAgencyId.Value.ToString()));
        }

        if (user.ContractorId.HasValue)
        {
            claims.Add(new Claim("contractorId", user.ContractorId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(token);

        return tokenString;
    }
}