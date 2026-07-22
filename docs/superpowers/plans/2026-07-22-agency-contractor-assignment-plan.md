# Agency/Contractor Assignment System (المرحلة 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let sub-projects be assigned to one executive agency, let agencies assign one or more contractors under a contract type, and lock down every mutation with role- and ownership-scoped authorization — backend only.

**Architecture:** .NET 10 Onion architecture (Domain → Application → Infrastructure → API), matching the existing SmartInvest codebase exactly. Controllers stay thin (`[Authorize(Roles=...)]` only); ownership/scope checks live in Application-layer services via a new `ICurrentUserService`. Generic repository (`IGenericRepository<T>` via `UnitOfWork`) is used for simple CRUD; bespoke repositories only where custom queries (includes/filters) are needed, matching the existing `ISubProjectRepository` pattern.

**Tech Stack:** ASP.NET Core 10 Web API, EF Core 10 (SQL Server), ASP.NET Core Identity + JWT, AutoMapper, FluentValidation. No test project exists in this repo (verified: no `.Tests.csproj` anywhere) — verification per task is `dotnet build` + manual HTTP calls via `Backend/src/SmartInvest.API/SmartInvest.API.http` or Swagger UI, matching the codebase's existing (test-free) convention. Do not introduce a new test project as part of this plan — that would be unrequested scope.

## Global Constraints

- Backend only. Do not touch anything under `Frontend/` in this plan.
- All user-facing strings (validation messages, exceptions) must be Arabic, matching every existing message in the codebase (see `CreateSubProjectDtoValidator`, `SubProjectService`, `IdentityService`).
- Follow the existing file/namespace conventions exactly: `SmartInvest.Domain.Entities`, `SmartInvest.Domain.Common`, `SmartInvest.Application.DTOs`, `SmartInvest.Application.Interfaces`, `SmartInvest.Application.Services`, `SmartInvest.Application.Validators`, `SmartInvest.Application.Common.Mappings`, `SmartInvest.Infrastructure.Data.Configurations`, `SmartInvest.Infrastructure.Repositories`, `SmartInvest.API.Controllers`.
- `PlanningManager` has full override on every operation in this plan, regardless of what the per-operation table says.
- Currency fields use `[Column(TypeName = "decimal(18,2)")]` (matches `SubProject.BankFunding`/`SelfFunding`), not `HasColumnType` in Fluent config, to match how new entities are already annotated in this codebase.
- Run `dotnet build Backend` after every task and confirm `0 Error(s)` before committing.
- Commit after every task with a `feat:`/`fix:` prefixed message (repo has no enforced convention, but recent history uses plain imperative English messages — follow that).

---

## File Structure Overview

```
Backend/src/SmartInvest.Domain/
  Common/Roles.cs                                   [MODIFY]
  Enums/ChangeRequestStatus.cs                       [CREATE]
  Entities/SubProject.cs                             [MODIFY]
  Entities/ProjectAssignment.cs                      [MODIFY]
  Entities/ProjectAssignmentChangeRequest.cs          [CREATE]
  Entities/AuditLog.cs                               [CREATE]
  Interfaces/IProjectAssignmentRepository.cs         [CREATE]

Backend/src/SmartInvest.Infrastructure/
  Identity/ApplicationUser.cs                        [MODIFY]
  Identity/IdentityService.cs                        [MODIFY]
  Data/Configurations/ApplicationUserConfiguration.cs [CREATE]
  Data/Configurations/SubProjectConfiguration.cs      [MODIFY]
  Data/Configurations/ProjectAssignmentConfiguration.cs [CREATE]
  Repositories/ProjectAssignmentRepository.cs         [CREATE]
  Migrations/..._AddAgencyContractorAssignmentSystem  [CREATE via dotnet ef]
  DependencyInjection.cs                              [MODIFY]

Backend/src/SmartInvest.Application/
  Common/Exceptions/ForbiddenAccessException.cs       [CREATE]
  Interfaces/ICurrentUserService.cs                   [CREATE]
  Interfaces/IIdentityService.cs                      [MODIFY]
  Interfaces/IExecutiveAgencyService.cs                [CREATE]
  Interfaces/IContractorService.cs                    [CREATE]
  Interfaces/IContractTypeService.cs                  [CREATE]
  Interfaces/IProjectAssignmentService.cs              [CREATE]
  Interfaces/IChangeRequestService.cs                  [CREATE]
  Interfaces/IAuditLogService.cs                       [CREATE]
  Interfaces/ISubProjectService.cs                     [MODIFY]
  DTOs/ExecutiveAgencyDtos.cs                          [CREATE]
  DTOs/ContractorDtos.cs                               [CREATE]
  DTOs/ContractTypeDtos.cs                             [CREATE]
  DTOs/ProjectAssignmentDtos.cs                        [CREATE]
  DTOs/ChangeRequestDtos.cs                            [CREATE]
  DTOs/AuditLogDtos.cs                                 [CREATE]
  DTOs/SubProjectDtos.cs                               [MODIFY]
  Validators/CreateExecutiveAgencyDtoValidator.cs      [CREATE]
  Validators/CreateContractorDtoValidator.cs           [CREATE]
  Validators/CreateContractTypeDtoValidator.cs         [CREATE]
  Validators/CreateProjectAssignmentDtoValidator.cs    [CREATE]
  Validators/CreateChangeRequestDtoValidator.cs        [CREATE]
  Common/Mappings/ExecutiveAgencyMappingProfile.cs     [CREATE]
  Common/Mappings/ContractorMappingProfile.cs          [CREATE]
  Common/Mappings/ContractTypeMappingProfile.cs        [CREATE]
  Common/Mappings/ProjectAssignmentMappingProfile.cs   [CREATE]
  Common/Mappings/SubProjectMappingProfile.cs          [MODIFY]
  Services/ExecutiveAgencyService.cs                   [CREATE]
  Services/ContractorService.cs                        [CREATE]
  Services/ContractTypeService.cs                      [CREATE]
  Services/ProjectAssignmentService.cs                  [CREATE]
  Services/ChangeRequestService.cs                      [CREATE]
  Services/AuditLogService.cs                           [CREATE]
  Services/SubProjectService.cs                         [MODIFY]

Backend/src/SmartInvest.API/
  Common/CurrentUserService.cs                         [CREATE]
  Middleware/ExceptionHandlingMiddleware.cs             [MODIFY]
  Controllers/ExecutiveAgenciesController.cs            [CREATE]
  Controllers/ContractorsController.cs                  [CREATE]
  Controllers/ContractTypesController.cs                [CREATE]
  Controllers/ProjectAssignmentsController.cs            [CREATE]
  Controllers/AuditLogsController.cs                     [CREATE]
  Controllers/SubProjectsController.cs                   [MODIFY]
  Program.cs                                             [MODIFY]
```

---

### Task 1: Roles, exceptions, ICurrentUserService foundation

**Files:**
- Modify: `Backend/src/SmartInvest.Domain/Common/Roles.cs`
- Create: `Backend/src/SmartInvest.Application/Common/Exceptions/ForbiddenAccessException.cs`
- Modify: `Backend/src/SmartInvest.API/Middleware/ExceptionHandlingMiddleware.cs`
- Create: `Backend/src/SmartInvest.Application/Interfaces/ICurrentUserService.cs`
- Create: `Backend/src/SmartInvest.API/Common/CurrentUserService.cs`
- Modify: `Backend/src/SmartInvest.API/Program.cs`

**Interfaces:**
- Produces: `Roles.ExecutiveAgency`, `Roles.Contractor`, `Roles.PlanningStaff`, `Roles.StaffAndAgency`, `Roles.AssignmentParties` (string constants) — used by every controller in later tasks.
- Produces: `ForbiddenAccessException(string message)` — thrown by services for ownership violations, mapped to HTTP 403.
- Produces: `ICurrentUserService` with `string? UserId`, `string? Role`, `int? ExecutiveAgencyId`, `int? ContractorId` — injected into every service added in this plan that needs ownership checks.

- [ ] **Step 1: Add new role constants**

Replace the full contents of `Backend/src/SmartInvest.Domain/Common/Roles.cs`:

```csharp
namespace SmartInvest.Domain.Common;

public static class Roles
{
    public const string PlanningEmployee = "PlanningEmployee";

    public const string PlanningManager = "PlanningManager";

    public const string ExecutiveAgency = "ExecutiveAgency";

    public const string Contractor = "Contractor";

    /// <summary>مدير + موظف تخطيط.</summary>
    public const string PlanningStaff = "PlanningEmployee,PlanningManager";

    /// <summary>مدير + موظف تخطيط + الجهة التنفيذية.</summary>
    public const string StaffAndAgency = "PlanningEmployee,PlanningManager,ExecutiveAgency";

    /// <summary>كل الأطراف المشاركة في تعيين مقاول: تخطيط + جهة + مقاول.</summary>
    public const string AssignmentParties = "PlanningEmployee,PlanningManager,ExecutiveAgency,Contractor";
}
```

- [ ] **Step 2: Add `ForbiddenAccessException`**

Create `Backend/src/SmartInvest.Application/Common/Exceptions/ForbiddenAccessException.cs`:

```csharp
namespace SmartInvest.Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
```

- [ ] **Step 3: Map it to HTTP 403 in the middleware**

In `Backend/src/SmartInvest.API/Middleware/ExceptionHandlingMiddleware.cs`, insert a new case right after the `BusinessRuleException` case (before `ValidationException`):

```csharp
            case ForbiddenAccessException forbiddenException:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response = new { message = forbiddenException.Message };
                break;

```

- [ ] **Step 4: Add `ICurrentUserService` (Application interface)**

Create `Backend/src/SmartInvest.Application/Interfaces/ICurrentUserService.cs`:

```csharp
namespace SmartInvest.Application.Interfaces;

/// <summary>
/// يكشف هوية المستخدم الحالي (من الـ JWT) للطبقات الأعلى بدون أي اعتماد مباشر على ASP.NET Core.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }

    string? Role { get; }

    int? ExecutiveAgencyId { get; }

    int? ContractorId { get; }
}
```

- [ ] **Step 5: Implement it in the API project**

Create `Backend/src/SmartInvest.API/Common/CurrentUserService.cs`:

```csharp
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
```

- [ ] **Step 6: Register `IHttpContextAccessor` and `ICurrentUserService` in `Program.cs`**

In `Backend/src/SmartInvest.API/Program.cs`, find this block:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

Replace it with:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SmartInvest.Application.Interfaces.ICurrentUserService, SmartInvest.API.Common.CurrentUserService>();
```

- [ ] **Step 7: Build and verify**

Run: `dotnet build Backend`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 8: Commit**

```bash
git add Backend/src/SmartInvest.Domain/Common/Roles.cs \
        Backend/src/SmartInvest.Application/Common/Exceptions/ForbiddenAccessException.cs \
        Backend/src/SmartInvest.API/Middleware/ExceptionHandlingMiddleware.cs \
        Backend/src/SmartInvest.Application/Interfaces/ICurrentUserService.cs \
        Backend/src/SmartInvest.API/Common/CurrentUserService.cs \
        Backend/src/SmartInvest.API/Program.cs
git commit -m "feat: add agency/contractor roles, ForbiddenAccessException, and ICurrentUserService"
```

---

### Task 2: Domain entities, EF configuration, and migration

**Files:**
- Create: `Backend/src/SmartInvest.Domain/Enums/ChangeRequestStatus.cs`
- Modify: `Backend/src/SmartInvest.Domain/Entities/SubProject.cs`
- Modify: `Backend/src/SmartInvest.Domain/Entities/ProjectAssignment.cs`
- Create: `Backend/src/SmartInvest.Domain/Entities/ProjectAssignmentChangeRequest.cs`
- Create: `Backend/src/SmartInvest.Domain/Entities/AuditLog.cs`
- Modify: `Backend/src/SmartInvest.Infrastructure/Identity/ApplicationUser.cs`
- Create: `Backend/src/SmartInvest.Infrastructure/Data/Configurations/ApplicationUserConfiguration.cs`
- Modify: `Backend/src/SmartInvest.Infrastructure/Data/Configurations/SubProjectConfiguration.cs`
- Create: `Backend/src/SmartInvest.Infrastructure/Data/Configurations/ProjectAssignmentConfiguration.cs`

**Interfaces:**
- Consumes: nothing from Task 1 directly (pure data model).
- Produces: `SubProject.ExecutiveAgencyId` (`int?`), `SubProject.ExecutiveAgency` (nav). `ProjectAssignment.IsLocked` (`bool`), no more `ProjectAssignment.ExecutiveAgencyId`. `ApplicationUser.ExecutiveAgencyId`/`ContractorId` (`int?`) + navs. `ProjectAssignmentChangeRequest` entity with `ChangeRequestId, AssignmentId, RequestedContractValue, RequestedExpectedEndDate, Status, RequestedByUserId, RequestedAt, ReviewedByUserId, ReviewedAt, ReviewNote`. `AuditLog` entity with `AuditLogId, EntityName, EntityId, FieldName, OldValue, NewValue, ChangedByUserId, ChangedAt`. These exact names are used by every later task.

- [ ] **Step 1: Add the `ChangeRequestStatus` enum**

Create `Backend/src/SmartInvest.Domain/Enums/ChangeRequestStatus.cs`:

```csharp
namespace SmartInvest.Domain.Enums;

public enum ChangeRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
```

- [ ] **Step 2: Add `ExecutiveAgencyId` to `SubProject`**

In `Backend/src/SmartInvest.Domain/Entities/SubProject.cs`, add this block right after the `Status` property (after line `public virtual ProjectStatus Status { get; set; }`):

```csharp

        [ForeignKey("ExecutiveAgency")]
        public int? ExecutiveAgencyId { get; set; }
        public virtual ExecutiveAgency? ExecutiveAgency { get; set; }
```

- [ ] **Step 3: Drop `ExecutiveAgencyId` from `ProjectAssignment`, add `IsLocked`**

Replace the full contents of `Backend/src/SmartInvest.Domain/Entities/ProjectAssignment.cs`:

```csharp
namespace SmartInvest.Domain.Entities
{
    public class ProjectAssignment
    {
        [Key]
        public int AssignmentId { get; set; }
        [ForeignKey("SubProject")]
        public int SubProjectId { get; set; }
        public virtual SubProject SubProject { get; set; }
        [ForeignKey("Contractor")]
        public int? ContractorId { get; set; } // Nullable based on ERD because it may not be assigned for now to contractor 
        public virtual Contractor Contractor { get; set; }
        [ForeignKey("ContractType")]
        public int ContractTypeId { get; set; }
        public virtual ContractType ContractType { get; set; }

        public DateTime AssignmentDate { get; set; }
        public string? ContractNumber { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ContractValue { get; set; }
        public DateTime ExpectedStartDate { get; set; }
        public DateTime ExpectedEndDate { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// يُضبط تلقائيًا لـ true لكل التعيينات القائمة عند تغيير الجهة التنفيذية للمشروع الفرعي.
        /// التعيين المقفول للقراءة فقط لأي حد ما عدا مدير التخطيط.
        /// </summary>
        public bool IsLocked { get; set; }
    }
}
```

- [ ] **Step 4: Add `ProjectAssignmentChangeRequest` entity**

Create `Backend/src/SmartInvest.Domain/Entities/ProjectAssignmentChangeRequest.cs`:

```csharp
using SmartInvest.Domain.Enums;

namespace SmartInvest.Domain.Entities
{
    public class ProjectAssignmentChangeRequest
    {
        [Key]
        public int ChangeRequestId { get; set; }

        [ForeignKey("Assignment")]
        public int AssignmentId { get; set; }
        public virtual ProjectAssignment Assignment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? RequestedContractValue { get; set; }
        public DateTime? RequestedExpectedEndDate { get; set; }

        public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;

        [Required]
        public string RequestedByUserId { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public string? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }
    }
}
```

- [ ] **Step 5: Add `AuditLog` entity**

Create `Backend/src/SmartInvest.Domain/Entities/AuditLog.cs`:

```csharp
namespace SmartInvest.Domain.Entities
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        [Required, MaxLength(100)]
        public string EntityName { get; set; } = string.Empty;

        public int EntityId { get; set; }

        [Required, MaxLength(100)]
        public string FieldName { get; set; } = string.Empty;

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        [Required]
        public string ChangedByUserId { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
```

- [ ] **Step 6: Link `ApplicationUser` to at most one agency or contractor**

In `Backend/src/SmartInvest.Infrastructure/Identity/ApplicationUser.cs`, replace the full file contents:

```csharp
using Microsoft.AspNetCore.Identity;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Infrastructure.Identity;

/// <summary>
/// Application user extending ASP.NET Core Identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>مضبوطة فقط لمستخدمي دور ExecutiveAgency — حساب واحد لكل جهة.</summary>
    public int? ExecutiveAgencyId { get; set; }
    public virtual ExecutiveAgency? ExecutiveAgency { get; set; }

    /// <summary>مضبوطة فقط لمستخدمي دور Contractor — حساب واحد لكل مقاول.</summary>
    public int? ContractorId { get; set; }
    public virtual Contractor? Contractor { get; set; }
}
```

- [ ] **Step 7: Configure the new `ApplicationUser` FKs (uniqueness + no cascade)**

Create `Backend/src/SmartInvest.Infrastructure/Data/Configurations/ApplicationUserConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInvest.Infrastructure.Identity;

namespace SmartInvest.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // حساب دخول واحد بحد أقصى لكل جهة/مقاول
        builder.HasIndex(x => x.ExecutiveAgencyId)
               .IsUnique()
               .HasFilter("[ExecutiveAgencyId] IS NOT NULL");

        builder.HasIndex(x => x.ContractorId)
               .IsUnique()
               .HasFilter("[ContractorId] IS NOT NULL");

        builder.HasOne(x => x.ExecutiveAgency)
               .WithMany()
               .HasForeignKey(x => x.ExecutiveAgencyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Contractor)
               .WithMany()
               .HasForeignKey(x => x.ContractorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 8: Configure `SubProject.ExecutiveAgency` (no cascade)**

In `Backend/src/SmartInvest.Infrastructure/Data/Configurations/SubProjectConfiguration.cs`, add this block right before the final closing `}` of the `Configure` method (after the `Markaz` block):

```csharp

        builder.HasOne(x => x.ExecutiveAgency)
               .WithMany()
               .HasForeignKey(x => x.ExecutiveAgencyId)
               .OnDelete(DeleteBehavior.Restrict);
```

- [ ] **Step 9: Configure `ProjectAssignment` relations (no cascade)**

Create `Backend/src/SmartInvest.Infrastructure/Data/Configurations/ProjectAssignmentConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Infrastructure.Data.Configurations;

public class ProjectAssignmentConfiguration : IEntityTypeConfiguration<ProjectAssignment>
{
    public void Configure(EntityTypeBuilder<ProjectAssignment> builder)
    {
        builder.HasOne(x => x.SubProject)
               .WithMany(s => s.ProjectAssignments)
               .HasForeignKey(x => x.SubProjectId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Contractor)
               .WithMany(c => c.ProjectAssignments)
               .HasForeignKey(x => x.ContractorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ContractType)
               .WithMany(c => c.ProjectAssignments)
               .HasForeignKey(x => x.ContractTypeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 10: Generate the migration**

Run (from `Backend/src/SmartInvest.API`):

```bash
cd Backend/src/SmartInvest.API
dotnet ef migrations add AddAgencyContractorAssignmentSystem
```

Expected: `Done.` and a new set of files under `Backend/src/SmartInvest.Infrastructure/Migrations/` (`..._AddAgencyContractorAssignmentSystem.cs`, `.Designer.cs`, updated `AppDbContextModelSnapshot.cs`). A warning about tool version `9.0.4` vs runtime `10.0.10` is expected and harmless (pre-existing, unrelated to this change).

- [ ] **Step 11: Build and verify**

Run: `dotnet build Backend`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 12: Apply the migration (skip if no local SQL Server is reachable)**

Run (still inside `Backend/src/SmartInvest.API`):

```bash
dotnet ef database update
```

Expected: `Done.` If this fails with a connection error (no local SQL Server instance), skip this step — it is not required to continue the plan, only useful for local manual verification later.

- [ ] **Step 13: Commit**

```bash
git add Backend/src/SmartInvest.Domain/Enums/ChangeRequestStatus.cs \
        Backend/src/SmartInvest.Domain/Entities/SubProject.cs \
        Backend/src/SmartInvest.Domain/Entities/ProjectAssignment.cs \
        Backend/src/SmartInvest.Domain/Entities/ProjectAssignmentChangeRequest.cs \
        Backend/src/SmartInvest.Domain/Entities/AuditLog.cs \
        Backend/src/SmartInvest.Infrastructure/Identity/ApplicationUser.cs \
        Backend/src/SmartInvest.Infrastructure/Data/Configurations/ApplicationUserConfiguration.cs \
        Backend/src/SmartInvest.Infrastructure/Data/Configurations/SubProjectConfiguration.cs \
        Backend/src/SmartInvest.Infrastructure/Data/Configurations/ProjectAssignmentConfiguration.cs \
        Backend/src/SmartInvest.Infrastructure/Migrations/
git commit -m "feat: add agency/contractor assignment data model and migration"
```

---

### Task 3: JWT claims for agency/contractor scoping

**Files:**
- Modify: `Backend/src/SmartInvest.Infrastructure/Identity/IdentityService.cs`

**Interfaces:**
- Consumes: `ApplicationUser.ExecutiveAgencyId`/`ContractorId` (Task 2).
- Produces: JWT now carries `executiveAgencyId`/`contractorId` claims when set — consumed by `CurrentUserService` (Task 1) at request time.

- [ ] **Step 1: Include the two claims in `GenerateJwtToken`**

In `Backend/src/SmartInvest.Infrastructure/Identity/IdentityService.cs`, replace the `GenerateJwtToken` method:

```csharp
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
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Backend`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Manual verification — existing login still works**

Start the API (`dotnet run --project Backend/src/SmartInvest.API`), then `POST /api/auth/login` with the seeded admin (`admin@gmail.com` / `Admin@123`) via Swagger UI (`/swagger`) or the `.http` file. Expected: 200 OK, token returned, same shape as before (admin has no agency/contractor, so no new claims appear — decode the JWT at jwt.io-style inspection is optional, just confirm login still succeeds).

- [ ] **Step 4: Commit**

```bash
git add Backend/src/SmartInvest.Infrastructure/Identity/IdentityService.cs
git commit -m "feat: embed executiveAgencyId/contractorId claims in JWT"
```

---

### Task 4: `IIdentityService` — agency/contractor account provisioning

**Files:**
- Modify: `Backend/src/SmartInvest.Application/Interfaces/IIdentityService.cs`
- Modify: `Backend/src/SmartInvest.Infrastructure/Identity/IdentityService.cs`

**Interfaces:**
- Consumes: `Roles.ExecutiveAgency`, `Roles.Contractor` (Task 1); `ApplicationUser.ExecutiveAgencyId`/`ContractorId` (Task 2).
- Produces (used by Task 5 and Task 6 services): `Task<UserDto> CreateAgencyUserAsync(string userName, string email, string? phoneNumber, string password, int executiveAgencyId, CancellationToken)`, `Task<UserDto> CreateContractorUserAsync(string userName, string email, string? phoneNumber, string password, int contractorId, CancellationToken)`, `Task<UserDto?> GetUserByExecutiveAgencyIdAsync(int executiveAgencyId, CancellationToken)`, `Task<UserDto?> GetUserByContractorIdAsync(int contractorId, CancellationToken)`, `Task ResetPasswordForAgencyAsync(int executiveAgencyId, string newPassword, CancellationToken)`, `Task ResetPasswordForContractorAsync(int contractorId, string newPassword, CancellationToken)`, `Task DeleteUserByExecutiveAgencyIdAsync(int executiveAgencyId, CancellationToken)`, `Task DeleteUserByContractorIdAsync(int contractorId, CancellationToken)`.

- [ ] **Step 1: Extend `IIdentityService`**

In `Backend/src/SmartInvest.Application/Interfaces/IIdentityService.cs`, replace the full file contents:

```csharp
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
```

- [ ] **Step 2: Implement the new methods**

In `Backend/src/SmartInvest.Infrastructure/Identity/IdentityService.cs`, add these methods right before the final closing `}` of the class (after `GetUsersAsync`, before `GenerateJwtToken`):

```csharp
    public async Task<UserDto> CreateAgencyUserAsync(string userName, string email, string? phoneNumber, string password, int executiveAgencyId, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            PhoneNumber = phoneNumber,
            FullName = userName,
            IsActive = true,
            ExecutiveAgencyId = executiveAgencyId
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" - ", createResult.Errors.Select(e => e.Description));
            throw new BusinessRuleException(errors);
        }

        await _userManager.AddToRoleAsync(user, Roles.ExecutiveAgency);

        return MapUser(user, Roles.ExecutiveAgency);
    }

    public async Task<UserDto> CreateContractorUserAsync(string userName, string email, string? phoneNumber, string password, int contractorId, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            PhoneNumber = phoneNumber,
            FullName = userName,
            IsActive = true,
            ContractorId = contractorId
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" - ", createResult.Errors.Select(e => e.Description));
            throw new BusinessRuleException(errors);
        }

        await _userManager.AddToRoleAsync(user, Roles.Contractor);

        return MapUser(user, Roles.Contractor);
    }

    public async Task<UserDto?> GetUserByExecutiveAgencyIdAsync(int executiveAgencyId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.ExecutiveAgencyId == executiveAgencyId, cancellationToken);
        return user == null ? null : MapUser(user, Roles.ExecutiveAgency);
    }

    public async Task<UserDto?> GetUserByContractorIdAsync(int contractorId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.ContractorId == contractorId, cancellationToken);
        return user == null ? null : MapUser(user, Roles.Contractor);
    }

    public async Task ResetPasswordForAgencyAsync(int executiveAgencyId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.ExecutiveAgencyId == executiveAgencyId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("لا يوجد حساب دخول مرتبط بهذه الجهة");
        }

        await ResetPasswordAsync(user.Id, newPassword, cancellationToken);
    }

    public async Task ResetPasswordForContractorAsync(int contractorId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.ContractorId == contractorId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("لا يوجد حساب دخول مرتبط بهذا المقاول");
        }

        await ResetPasswordAsync(user.Id, newPassword, cancellationToken);
    }

    public async Task DeleteUserByExecutiveAgencyIdAsync(int executiveAgencyId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.ExecutiveAgencyId == executiveAgencyId, cancellationToken);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }
    }

    public async Task DeleteUserByContractorIdAsync(int contractorId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.ContractorId == contractorId, cancellationToken);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }
    }

    private static UserDto MapUser(ApplicationUser user, string role)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build Backend`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Backend/src/SmartInvest.Application/Interfaces/IIdentityService.cs \
        Backend/src/SmartInvest.Infrastructure/Identity/IdentityService.cs
git commit -m "feat: add agency/contractor account provisioning to IdentityService"
```

---

### Task 5: Executive Agency CRUD

**Files:**
- Create: `Backend/src/SmartInvest.Application/DTOs/ExecutiveAgencyDtos.cs`
- Create: `Backend/src/SmartInvest.Application/Validators/CreateExecutiveAgencyDtoValidator.cs`
- Create: `Backend/src/SmartInvest.Application/Common/Mappings/ExecutiveAgencyMappingProfile.cs`
- Create: `Backend/src/SmartInvest.Application/Interfaces/IExecutiveAgencyService.cs`
- Create: `Backend/src/SmartInvest.Application/Services/ExecutiveAgencyService.cs`
- Create: `Backend/src/SmartInvest.API/Controllers/ExecutiveAgenciesController.cs`
- Modify: `Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IIdentityService.CreateAgencyUserAsync/GetUserByExecutiveAgencyIdAsync/ResetPasswordForAgencyAsync/DeleteUserByExecutiveAgencyIdAsync` (Task 4); `IGenericRepository<ExecutiveAgency>`, `ISubProjectRepository.FindAsync` (existing); `Roles.PlanningStaff`, `Roles.PlanningManager` (Task 1).
- Produces: `IExecutiveAgencyService` with `GetAllAsync/GetByIdAsync/CreateAsync/UpdateAsync/DeleteAsync/ResetPasswordAsync` — consumed only by `ExecutiveAgenciesController` in this task; `ExecutiveAgencyDto.Id` (`int`) is the FK value later tasks (SubProject assignment, ProjectAssignment) use as `executiveAgencyId`.

- [ ] **Step 1: DTOs**

Create `Backend/src/SmartInvest.Application/DTOs/ExecutiveAgencyDtos.cs`:

```csharp
namespace SmartInvest.Application.DTOs;

public class ExecutiveAgencyDto
{
    public int Id { get; set; }
    public string AgencyName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateExecutiveAgencyDto
{
    public string AgencyName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateExecutiveAgencyDto
{
    public string AgencyName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Validators**

Create `Backend/src/SmartInvest.Application/Validators/CreateExecutiveAgencyDtoValidator.cs`:

```csharp
using FluentValidation;
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Validators;

public class CreateExecutiveAgencyDtoValidator : AbstractValidator<CreateExecutiveAgencyDto>
{
    public CreateExecutiveAgencyDtoValidator()
    {
        RuleFor(x => x.AgencyName).NotEmpty().WithMessage("اسم الجهة مطلوب").MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().WithMessage("رقم الهاتف مطلوب");
        RuleFor(x => x.Email).NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");
        RuleFor(x => x.UserName).NotEmpty().WithMessage("اسم المستخدم مطلوب").MinimumLength(3);
        RuleFor(x => x.Password).NotEmpty().WithMessage("كلمة المرور مطلوبة").MinimumLength(6);
    }
}

public class UpdateExecutiveAgencyDtoValidator : AbstractValidator<UpdateExecutiveAgencyDto>
{
    public UpdateExecutiveAgencyDtoValidator()
    {
        RuleFor(x => x.AgencyName).NotEmpty().WithMessage("اسم الجهة مطلوب").MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().WithMessage("رقم الهاتف مطلوب");
        RuleFor(x => x.Email).NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");
    }
}
```

- [ ] **Step 3: Mapping profile**

Create `Backend/src/SmartInvest.Application/Common/Mappings/ExecutiveAgencyMappingProfile.cs`:

```csharp
using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class ExecutiveAgencyMappingProfile : Profile
{
    public ExecutiveAgencyMappingProfile()
    {
        CreateMap<ExecutiveAgency, ExecutiveAgencyDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ExecutiveAgencyId))
            .ForMember(dest => dest.UserName, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());
    }
}
```

- [ ] **Step 4: Service interface**

Create `Backend/src/SmartInvest.Application/Interfaces/IExecutiveAgencyService.cs`:

```csharp
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IExecutiveAgencyService
{
    Task<IReadOnlyList<ExecutiveAgencyDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ExecutiveAgencyDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ExecutiveAgencyDto> CreateAsync(CreateExecutiveAgencyDto dto, CancellationToken cancellationToken = default);

    Task<ExecutiveAgencyDto> UpdateAsync(int id, UpdateExecutiveAgencyDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 5: Service implementation**

Create `Backend/src/SmartInvest.Application/Services/ExecutiveAgencyService.cs`:

```csharp
using AutoMapper;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class ExecutiveAgencyService : IExecutiveAgencyService
{
    private readonly IGenericRepository<ExecutiveAgency> _agencyRepository;
    private readonly ISubProjectRepository _subProjectRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ExecutiveAgencyService(
        IGenericRepository<ExecutiveAgency> agencyRepository,
        ISubProjectRepository subProjectRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _agencyRepository = agencyRepository;
        _subProjectRepository = subProjectRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ExecutiveAgencyDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var agencies = await _agencyRepository.GetAllAsync(cancellationToken);
        var result = new List<ExecutiveAgencyDto>();
        foreach (var agency in agencies)
        {
            result.Add(await MapWithUserAsync(agency, cancellationToken));
        }

        return result;
    }

    public async Task<ExecutiveAgencyDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var agency = await GetOrThrowAsync(id, cancellationToken);
        return await MapWithUserAsync(agency, cancellationToken);
    }

    public async Task<ExecutiveAgencyDto> CreateAsync(CreateExecutiveAgencyDto dto, CancellationToken cancellationToken = default)
    {
        var agency = new ExecutiveAgency
        {
            AgencyName = dto.AgencyName,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
        };

        await _agencyRepository.AddAsync(agency, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _identityService.CreateAgencyUserAsync(
                dto.UserName, dto.Email, dto.Phone, dto.Password, agency.ExecutiveAgencyId, cancellationToken);
        }
        catch
        {
            _agencyRepository.Remove(agency);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }

        return await MapWithUserAsync(agency, cancellationToken);
    }

    public async Task<ExecutiveAgencyDto> UpdateAsync(int id, UpdateExecutiveAgencyDto dto, CancellationToken cancellationToken = default)
    {
        var agency = await GetOrThrowAsync(id, cancellationToken);

        agency.AgencyName = dto.AgencyName;
        agency.Phone = dto.Phone;
        agency.Email = dto.Email;
        agency.Address = dto.Address;

        _agencyRepository.Update(agency);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapWithUserAsync(agency, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var agency = await GetOrThrowAsync(id, cancellationToken);

        var linkedSubProjects = await _subProjectRepository.FindAsync(x => x.ExecutiveAgencyId == id, cancellationToken);
        if (linkedSubProjects.Count > 0)
        {
            throw new BusinessRuleException("لا يمكن حذف الجهة لوجود مشروعات فرعية مسندة إليها");
        }

        await _identityService.DeleteUserByExecutiveAgencyIdAsync(id, cancellationToken);

        _agencyRepository.Remove(agency);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default)
    {
        await GetOrThrowAsync(id, cancellationToken);
        await _identityService.ResetPasswordForAgencyAsync(id, newPassword, cancellationToken);
    }

    private async Task<ExecutiveAgency> GetOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var agency = await _agencyRepository.GetByIdAsync(id, cancellationToken);
        if (agency == null)
        {
            throw new NotFoundException($"الجهة التنفيذية رقم {id} غير موجودة");
        }

        return agency;
    }

    private async Task<ExecutiveAgencyDto> MapWithUserAsync(ExecutiveAgency agency, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<ExecutiveAgencyDto>(agency);
        var user = await _identityService.GetUserByExecutiveAgencyIdAsync(agency.ExecutiveAgencyId, cancellationToken);
        dto.UserName = user?.UserName ?? string.Empty;
        dto.IsActive = user?.IsActive ?? false;
        return dto;
    }
}
```

- [ ] **Step 6: Controller**

Create `Backend/src/SmartInvest.API/Controllers/ExecutiveAgenciesController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/agencies")]
[Authorize(Roles = Roles.PlanningStaff)]
public class ExecutiveAgenciesController : ControllerBase
{
    private readonly IExecutiveAgencyService _agencyService;

    public ExecutiveAgenciesController(IExecutiveAgencyService agencyService)
    {
        _agencyService = agencyService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExecutiveAgencyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _agencyService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExecutiveAgencyDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _agencyService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<ActionResult<ExecutiveAgencyDto>> Create(CreateExecutiveAgencyDto dto, CancellationToken cancellationToken)
    {
        var result = await _agencyService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<ActionResult<ExecutiveAgencyDto>> Update(int id, UpdateExecutiveAgencyDto dto, CancellationToken cancellationToken)
    {
        var result = await _agencyService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _agencyService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}/reset-password")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        await _agencyService.ResetPasswordAsync(id, dto.NewPassword, cancellationToken);
        return NoContent();
    }
}
```

- [ ] **Step 7: Register in DI**

In `Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs`, add this line right after `services.AddScoped<IIdentityService, IdentityService>();`:

```csharp
        services.AddScoped<IExecutiveAgencyService, ExecutiveAgencyService>();
```

- [ ] **Step 8: Build and verify**

Run: `dotnet build Backend`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 9: Manual verification**

Start the API. Login as admin, then:
- `POST /api/agencies` with `{"agencyName":"شركة مياه الشرب","phone":"0482000000","email":"agency1@test.com","address":"شبين الكوم","userName":"agency1","password":"Agency@123"}` → expect 201 with `id`.
- `GET /api/agencies` → expect the new agency listed with `userName: "agency1"`, `isActive: true`.
- `POST /api/auth/login` with `usernameOrEmail: "agency1", password: "Agency@123"` → expect 200, `role: "ExecutiveAgency"`.

- [ ] **Step 10: Commit**

```bash
git add Backend/src/SmartInvest.Application/DTOs/ExecutiveAgencyDtos.cs \
        Backend/src/SmartInvest.Application/Validators/CreateExecutiveAgencyDtoValidator.cs \
        Backend/src/SmartInvest.Application/Common/Mappings/ExecutiveAgencyMappingProfile.cs \
        Backend/src/SmartInvest.Application/Interfaces/IExecutiveAgencyService.cs \
        Backend/src/SmartInvest.Application/Services/ExecutiveAgencyService.cs \
        Backend/src/SmartInvest.API/Controllers/ExecutiveAgenciesController.cs \
        Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs
git commit -m "feat: add executive agency CRUD with combined login provisioning"
```

---

### Task 6: Contractor CRUD

**Files:**
- Create: `Backend/src/SmartInvest.Application/DTOs/ContractorDtos.cs`
- Create: `Backend/src/SmartInvest.Application/Validators/CreateContractorDtoValidator.cs`
- Create: `Backend/src/SmartInvest.Application/Common/Mappings/ContractorMappingProfile.cs`
- Create: `Backend/src/SmartInvest.Application/Interfaces/IContractorService.cs`
- Create: `Backend/src/SmartInvest.Application/Services/ContractorService.cs`
- Create: `Backend/src/SmartInvest.API/Controllers/ContractorsController.cs`
- Modify: `Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IIdentityService.CreateContractorUserAsync/GetUserByContractorIdAsync/ResetPasswordForContractorAsync/DeleteUserByContractorIdAsync` (Task 4).
- Produces: `IContractorService` (`GetAllAsync/GetByIdAsync/CreateAsync/UpdateAsync/DeleteAsync/ResetPasswordAsync`) and `ContractorDto.Id` — used by Task 8 (`ProjectAssignment.ContractorId`).

- [ ] **Step 1: DTOs**

Create `Backend/src/SmartInvest.Application/DTOs/ContractorDtos.cs`:

```csharp
namespace SmartInvest.Application.DTOs;

public class ContractorDto
{
    public int Id { get; set; }
    public string ContractorName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string NationalIdOrCommercialRegister { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class CreateContractorDto
{
    public string ContractorName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string NationalIdOrCommercialRegister { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateContractorDto
{
    public string ContractorName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string NationalIdOrCommercialRegister { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
```

- [ ] **Step 2: Validators**

Create `Backend/src/SmartInvest.Application/Validators/CreateContractorDtoValidator.cs`:

```csharp
using FluentValidation;
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Validators;

public class CreateContractorDtoValidator : AbstractValidator<CreateContractorDto>
{
    public CreateContractorDtoValidator()
    {
        RuleFor(x => x.ContractorName).NotEmpty().WithMessage("اسم المقاول مطلوب").MaximumLength(200);
        RuleFor(x => x.NationalIdOrCommercialRegister).NotEmpty().WithMessage("الرقم القومي أو السجل التجاري مطلوب");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("رقم الهاتف مطلوب");
        RuleFor(x => x.Email).NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");
        RuleFor(x => x.UserName).NotEmpty().WithMessage("اسم المستخدم مطلوب").MinimumLength(3);
        RuleFor(x => x.Password).NotEmpty().WithMessage("كلمة المرور مطلوبة").MinimumLength(6);
    }
}

public class UpdateContractorDtoValidator : AbstractValidator<UpdateContractorDto>
{
    public UpdateContractorDtoValidator()
    {
        RuleFor(x => x.ContractorName).NotEmpty().WithMessage("اسم المقاول مطلوب").MaximumLength(200);
        RuleFor(x => x.NationalIdOrCommercialRegister).NotEmpty().WithMessage("الرقم القومي أو السجل التجاري مطلوب");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("رقم الهاتف مطلوب");
        RuleFor(x => x.Email).NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");
    }
}
```

- [ ] **Step 3: Mapping profile**

Create `Backend/src/SmartInvest.Application/Common/Mappings/ContractorMappingProfile.cs`:

```csharp
using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class ContractorMappingProfile : Profile
{
    public ContractorMappingProfile()
    {
        CreateMap<Contractor, ContractorDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ContractorId))
            .ForMember(dest => dest.UserName, opt => opt.Ignore());
    }
}
```

- [ ] **Step 4: Service interface**

Create `Backend/src/SmartInvest.Application/Interfaces/IContractorService.cs`:

```csharp
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IContractorService
{
    Task<IReadOnlyList<ContractorDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ContractorDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ContractorDto> CreateAsync(CreateContractorDto dto, CancellationToken cancellationToken = default);

    Task<ContractorDto> UpdateAsync(int id, UpdateContractorDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 5: Service implementation**

Create `Backend/src/SmartInvest.Application/Services/ContractorService.cs`:

```csharp
using AutoMapper;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class ContractorService : IContractorService
{
    private readonly IGenericRepository<Contractor> _contractorRepository;
    private readonly IGenericRepository<ProjectAssignment> _assignmentRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ContractorService(
        IGenericRepository<Contractor> contractorRepository,
        IGenericRepository<ProjectAssignment> assignmentRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _contractorRepository = contractorRepository;
        _assignmentRepository = assignmentRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ContractorDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var contractors = await _contractorRepository.GetAllAsync(cancellationToken);
        var result = new List<ContractorDto>();
        foreach (var contractor in contractors)
        {
            result.Add(await MapWithUserAsync(contractor, cancellationToken));
        }

        return result;
    }

    public async Task<ContractorDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var contractor = await GetOrThrowAsync(id, cancellationToken);
        return await MapWithUserAsync(contractor, cancellationToken);
    }

    public async Task<ContractorDto> CreateAsync(CreateContractorDto dto, CancellationToken cancellationToken = default)
    {
        var contractor = new Contractor
        {
            ContractorName = dto.ContractorName,
            CompanyType = dto.CompanyType,
            NationalIdOrCommercialRegister = dto.NationalIdOrCommercialRegister,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Category = dto.Category,
            IsActive = true,
        };

        await _contractorRepository.AddAsync(contractor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _identityService.CreateContractorUserAsync(
                dto.UserName, dto.Email, dto.PhoneNumber, dto.Password, contractor.ContractorId, cancellationToken);
        }
        catch
        {
            _contractorRepository.Remove(contractor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }

        return await MapWithUserAsync(contractor, cancellationToken);
    }

    public async Task<ContractorDto> UpdateAsync(int id, UpdateContractorDto dto, CancellationToken cancellationToken = default)
    {
        var contractor = await GetOrThrowAsync(id, cancellationToken);

        contractor.ContractorName = dto.ContractorName;
        contractor.CompanyType = dto.CompanyType;
        contractor.NationalIdOrCommercialRegister = dto.NationalIdOrCommercialRegister;
        contractor.PhoneNumber = dto.PhoneNumber;
        contractor.Email = dto.Email;
        contractor.Address = dto.Address;
        contractor.Category = dto.Category;
        contractor.IsActive = dto.IsActive;

        _contractorRepository.Update(contractor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapWithUserAsync(contractor, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var contractor = await GetOrThrowAsync(id, cancellationToken);

        var linkedAssignments = await _assignmentRepository.FindAsync(x => x.ContractorId == id, cancellationToken);
        if (linkedAssignments.Count > 0)
        {
            throw new BusinessRuleException("لا يمكن حذف المقاول لوجود تعيينات مرتبطة به");
        }

        await _identityService.DeleteUserByContractorIdAsync(id, cancellationToken);

        _contractorRepository.Remove(contractor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default)
    {
        await GetOrThrowAsync(id, cancellationToken);
        await _identityService.ResetPasswordForContractorAsync(id, newPassword, cancellationToken);
    }

    private async Task<Contractor> GetOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var contractor = await _contractorRepository.GetByIdAsync(id, cancellationToken);
        if (contractor == null)
        {
            throw new NotFoundException($"المقاول رقم {id} غير موجود");
        }

        return contractor;
    }

    private async Task<ContractorDto> MapWithUserAsync(Contractor contractor, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<ContractorDto>(contractor);
        var user = await _identityService.GetUserByContractorIdAsync(contractor.ContractorId, cancellationToken);
        dto.UserName = user?.UserName ?? string.Empty;
        return dto;
    }
}
```

- [ ] **Step 6: Controller**

Create `Backend/src/SmartInvest.API/Controllers/ContractorsController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/contractors")]
[Authorize(Roles = Roles.StaffAndAgency)]
public class ContractorsController : ControllerBase
{
    private readonly IContractorService _contractorService;

    public ContractorsController(IContractorService contractorService)
    {
        _contractorService = contractorService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContractorDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _contractorService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractorDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _contractorService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<ActionResult<ContractorDto>> Create(CreateContractorDto dto, CancellationToken cancellationToken)
    {
        var result = await _contractorService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<ActionResult<ContractorDto>> Update(int id, UpdateContractorDto dto, CancellationToken cancellationToken)
    {
        var result = await _contractorService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _contractorService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}/reset-password")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        await _contractorService.ResetPasswordAsync(id, dto.NewPassword, cancellationToken);
        return NoContent();
    }
}
```

Note: read access (`GetAll`/`GetById`) is open to `StaffAndAgency` (Manager+Employee+Agency) — an agency needs to browse contractors to assign one in Task 8.

- [ ] **Step 7: Register in DI**

In `Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs`, add right after the `IExecutiveAgencyService` line from Task 5:

```csharp
        services.AddScoped<IContractorService, ContractorService>();
```

- [ ] **Step 8: Build and verify**

Run: `dotnet build Backend`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 9: Manual verification**

As admin: `POST /api/contractors` with `{"contractorName":"شركة ألفا","companyType":"مقاولات عامة","nationalIdOrCommercialRegister":"12345","phoneNumber":"01000000000","email":"contractor1@test.com","address":"المنوفية","category":"إنشاءات","userName":"contractor1","password":"Contractor@123"}` → expect 201. `GET /api/contractors` → listed.

- [ ] **Step 10: Commit**

```bash
git add Backend/src/SmartInvest.Application/DTOs/ContractorDtos.cs \
        Backend/src/SmartInvest.Application/Validators/CreateContractorDtoValidator.cs \
        Backend/src/SmartInvest.Application/Common/Mappings/ContractorMappingProfile.cs \
        Backend/src/SmartInvest.Application/Interfaces/IContractorService.cs \
        Backend/src/SmartInvest.Application/Services/ContractorService.cs \
        Backend/src/SmartInvest.API/Controllers/ContractorsController.cs \
        Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs
git commit -m "feat: add contractor CRUD with combined login provisioning"
```

---

### Task 7: Contract Type CRUD

**Files:**
- Create: `Backend/src/SmartInvest.Application/DTOs/ContractTypeDtos.cs`
- Create: `Backend/src/SmartInvest.Application/Validators/CreateContractTypeDtoValidator.cs`
- Create: `Backend/src/SmartInvest.Application/Common/Mappings/ContractTypeMappingProfile.cs`
- Create: `Backend/src/SmartInvest.Application/Interfaces/IContractTypeService.cs`
- Create: `Backend/src/SmartInvest.Application/Services/ContractTypeService.cs`
- Create: `Backend/src/SmartInvest.API/Controllers/ContractTypesController.cs`
- Modify: `Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IGenericRepository<ContractType>`, `IGenericRepository<ProjectAssignment>` (existing/Task 2), `Roles.PlanningStaff/PlanningManager` (Task 1).
- Produces: `IContractTypeService` (`GetAllAsync/GetByIdAsync/CreateAsync/UpdateAsync/DeleteAsync`); `ContractTypeDto.Id` used by Task 8 (`ProjectAssignment.ContractTypeId`).

- [ ] **Step 1: DTOs**

Create `Backend/src/SmartInvest.Application/DTOs/ContractTypeDtos.cs`:

```csharp
namespace SmartInvest.Application.DTOs;

public class ContractTypeDto
{
    public int Id { get; set; }
    public string ContractName { get; set; } = string.Empty;
}

public class CreateContractTypeDto
{
    public string ContractName { get; set; } = string.Empty;
}

public class UpdateContractTypeDto
{
    public string ContractName { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Validator**

Create `Backend/src/SmartInvest.Application/Validators/CreateContractTypeDtoValidator.cs`:

```csharp
using FluentValidation;
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Validators;

public class CreateContractTypeDtoValidator : AbstractValidator<CreateContractTypeDto>
{
    public CreateContractTypeDtoValidator()
    {
        RuleFor(x => x.ContractName).NotEmpty().WithMessage("اسم نوع العقد مطلوب").MaximumLength(150);
    }
}

public class UpdateContractTypeDtoValidator : AbstractValidator<UpdateContractTypeDto>
{
    public UpdateContractTypeDtoValidator()
    {
        RuleFor(x => x.ContractName).NotEmpty().WithMessage("اسم نوع العقد مطلوب").MaximumLength(150);
    }
}
```

- [ ] **Step 3: Mapping profile**

Create `Backend/src/SmartInvest.Application/Common/Mappings/ContractTypeMappingProfile.cs`:

```csharp
using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class ContractTypeMappingProfile : Profile
{
    public ContractTypeMappingProfile()
    {
        CreateMap<ContractType, ContractTypeDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ContractTypeId));
    }
}
```

- [ ] **Step 4: Service interface**

Create `Backend/src/SmartInvest.Application/Interfaces/IContractTypeService.cs`:

```csharp
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IContractTypeService
{
    Task<IReadOnlyList<ContractTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ContractTypeDto> CreateAsync(CreateContractTypeDto dto, CancellationToken cancellationToken = default);

    Task<ContractTypeDto> UpdateAsync(int id, UpdateContractTypeDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 5: Service implementation**

Create `Backend/src/SmartInvest.Application/Services/ContractTypeService.cs`:

```csharp
using AutoMapper;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class ContractTypeService : IContractTypeService
{
    private readonly IGenericRepository<ContractType> _contractTypeRepository;
    private readonly IGenericRepository<ProjectAssignment> _assignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ContractTypeService(
        IGenericRepository<ContractType> contractTypeRepository,
        IGenericRepository<ProjectAssignment> assignmentRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _contractTypeRepository = contractTypeRepository;
        _assignmentRepository = assignmentRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ContractTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var contractTypes = await _contractTypeRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<ContractTypeDto>>(contractTypes);
    }

    public async Task<ContractTypeDto> CreateAsync(CreateContractTypeDto dto, CancellationToken cancellationToken = default)
    {
        var contractType = new ContractType { ContractName = dto.ContractName };

        await _contractTypeRepository.AddAsync(contractType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ContractTypeDto>(contractType);
    }

    public async Task<ContractTypeDto> UpdateAsync(int id, UpdateContractTypeDto dto, CancellationToken cancellationToken = default)
    {
        var contractType = await _contractTypeRepository.GetByIdAsync(id, cancellationToken);
        if (contractType == null)
        {
            throw new NotFoundException($"نوع العقد رقم {id} غير موجود");
        }

        contractType.ContractName = dto.ContractName;

        _contractTypeRepository.Update(contractType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ContractTypeDto>(contractType);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var contractType = await _contractTypeRepository.GetByIdAsync(id, cancellationToken);
        if (contractType == null)
        {
            throw new NotFoundException($"نوع العقد رقم {id} غير موجود");
        }

        var linkedAssignments = await _assignmentRepository.FindAsync(x => x.ContractTypeId == id, cancellationToken);
        if (linkedAssignments.Count > 0)
        {
            throw new BusinessRuleException("لا يمكن حذف نوع العقد لوجود تعيينات مرتبطة به");
        }

        _contractTypeRepository.Remove(contractType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Controller**

Create `Backend/src/SmartInvest.API/Controllers/ContractTypesController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/contract-types")]
[Authorize(Roles = Roles.PlanningStaff)]
public class ContractTypesController : ControllerBase
{
    private readonly IContractTypeService _contractTypeService;

    public ContractTypesController(IContractTypeService contractTypeService)
    {
        _contractTypeService = contractTypeService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ContractTypeDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _contractTypeService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ContractTypeDto>> Create(CreateContractTypeDto dto, CancellationToken cancellationToken)
    {
        var result = await _contractTypeService.CreateAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ContractTypeDto>> Update(int id, UpdateContractTypeDto dto, CancellationToken cancellationToken)
    {
        var result = await _contractTypeService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _contractTypeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
```

Note: `GetAll` is opened to any authenticated role (`[Authorize]` override) since agencies and contractors also need this list to display/select contract types; create/update stay `PlanningStaff` (Manager+Employee) per your explicit answer; delete stays Manager-only.

- [ ] **Step 7: Register in DI**

In `Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs`, add right after the `IContractorService` line:

```csharp
        services.AddScoped<IContractTypeService, ContractTypeService>();
```

- [ ] **Step 8: Build and verify**

Run: `dotnet build Backend`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 9: Manual verification**

As admin: `POST /api/contract-types` with `{"contractName":"مقاولة عامة"}` → 200 with `id`. `GET /api/contract-types` → listed.

- [ ] **Step 10: Commit**

```bash
git add Backend/src/SmartInvest.Application/DTOs/ContractTypeDtos.cs \
        Backend/src/SmartInvest.Application/Validators/CreateContractTypeDtoValidator.cs \
        Backend/src/SmartInvest.Application/Common/Mappings/ContractTypeMappingProfile.cs \
        Backend/src/SmartInvest.Application/Interfaces/IContractTypeService.cs \
        Backend/src/SmartInvest.Application/Services/ContractTypeService.cs \
        Backend/src/SmartInvest.API/Controllers/ContractTypesController.cs \
        Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs
git commit -m "feat: add contract type CRUD"
```

---

### Task 8: SubProject → Executive Agency assignment + locking rule

**Files:**
- Modify: `Backend/src/SmartInvest.Application/DTOs/SubProjectDtos.cs`
- Modify: `Backend/src/SmartInvest.Application/Common/Mappings/SubProjectMappingProfile.cs`
- Modify: `Backend/src/SmartInvest.Application/Interfaces/ISubProjectService.cs`
- Modify: `Backend/src/SmartInvest.Application/Services/SubProjectService.cs`
- Modify: `Backend/src/SmartInvest.API/Controllers/SubProjectsController.cs`

**Interfaces:**
- Consumes: `SubProject.ExecutiveAgencyId` (Task 2); `IGenericRepository<ExecutiveAgency>`, `IGenericRepository<ProjectAssignment>` (existing/Task 2); `Roles.PlanningStaff` (Task 1).
- Produces: `ISubProjectService.AssignExecutiveAgencyAsync(int subProjectId, int executiveAgencyId, CancellationToken)` — the only place that flips `ProjectAssignment.IsLocked`; `SubProjectDetailDto.ExecutiveAgencyId`/`ExecutiveAgencyName` — consumed by Task 9's ownership checks (`ProjectAssignmentService` reads the sub-project's agency via `ISubProjectRepository`, not this DTO, but the DTO shape is a contract other tasks may read).

- [ ] **Step 1: Extend `SubProjectDetailDto` and `SubProjectListItemDto`**

In `Backend/src/SmartInvest.Application/DTOs/SubProjectDtos.cs`, add these two properties to `SubProjectListItemDto` (right after `StatusName`):

```csharp
    public int? ExecutiveAgencyId { get; set; }
    public string? ExecutiveAgencyName { get; set; }
```

Add the same two properties to `SubProjectDetailDto` (right after `StatusName`):

```csharp
    public int? ExecutiveAgencyId { get; set; }
    public string? ExecutiveAgencyName { get; set; }
```

- [ ] **Step 2: Add a dedicated DTO for the assign-agency action**

At the end of `Backend/src/SmartInvest.Application/DTOs/SubProjectDtos.cs`, append:

```csharp

public class AssignExecutiveAgencyDto
{
    public int ExecutiveAgencyId { get; set; }
}
```

- [ ] **Step 3: Map the new fields**

In `Backend/src/SmartInvest.Application/Common/Mappings/SubProjectMappingProfile.cs`, the `CreateMap<SubProject, SubProjectListItemDto>()` chain currently ends with:

```csharp
            .ForMember(
                dest => dest.StatusId,
                opt => opt.MapFrom(src => src.StatusId))
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.StatusName));
```

Replace that exact block with:

```csharp
            .ForMember(
                dest => dest.StatusId,
                opt => opt.MapFrom(src => src.StatusId))
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.StatusName))
            .ForMember(
                dest => dest.ExecutiveAgencyId,
                opt => opt.MapFrom(src => src.ExecutiveAgencyId))
            .ForMember(
                dest => dest.ExecutiveAgencyName,
                opt => opt.MapFrom(src => src.ExecutiveAgency != null ? src.ExecutiveAgency.AgencyName : null));
```

The `CreateMap<SubProject, SubProjectDetailDto>()` chain currently ends with:

```csharp
            .ForMember(
                dest => dest.StatusId,
                opt => opt.MapFrom(src => src.StatusId))
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.StatusName))
            .ForMember(
                dest => dest.Specifications,
                opt => opt.MapFrom(src => src.ProjectSpecifications));
```

Replace that exact block with:

```csharp
            .ForMember(
                dest => dest.StatusId,
                opt => opt.MapFrom(src => src.StatusId))
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.StatusName))
            .ForMember(
                dest => dest.ExecutiveAgencyId,
                opt => opt.MapFrom(src => src.ExecutiveAgencyId))
            .ForMember(
                dest => dest.ExecutiveAgencyName,
                opt => opt.MapFrom(src => src.ExecutiveAgency != null ? src.ExecutiveAgency.AgencyName : null))
            .ForMember(
                dest => dest.Specifications,
                opt => opt.MapFrom(src => src.ProjectSpecifications));
```

- [ ] **Step 4: Include `ExecutiveAgency` in the repository's eager-loaded queries**

In `Backend/src/SmartInvest.Infrastructure/Repositories/SubProjectRepository.cs`, add `.Include(x => x.ExecutiveAgency)` to both `GetWithDetailsAsync` and `SearchAsync` — right after the `.Include(x => x.Status)` line in each method:

```csharp
            .Include(x => x.ExecutiveAgency)
```

- [ ] **Step 5: Add `AssignExecutiveAgencyAsync` to the service interface**

In `Backend/src/SmartInvest.Application/Interfaces/ISubProjectService.cs`, add this method to the interface (right after `DeleteAsync`):

```csharp

    Task<SubProjectDetailDto> AssignExecutiveAgencyAsync(int id, int executiveAgencyId, CancellationToken cancellationToken = default);
```

- [ ] **Step 6: Implement it with the locking rule**

In `Backend/src/SmartInvest.Application/Services/SubProjectService.cs`:

1. Add two new constructor dependencies. Replace the constructor and field declarations:

```csharp
    private readonly ISubProjectRepository _subProjectRepository;
    private readonly IMainProjectRepository _mainProjectRepository;
    private readonly IGenericRepository<Markaz> _markazRepository;
    private readonly IGenericRepository<ProjectPriority> _priorityRepository;
    private readonly IGenericRepository<ProjectStatus> _statusRepository;
    private readonly IGenericRepository<ExecutiveAgency> _agencyRepository;
    private readonly IGenericRepository<ProjectAssignment> _assignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SubProjectService(
        ISubProjectRepository subProjectRepository,
        IMainProjectRepository mainProjectRepository,
        IGenericRepository<Markaz> markazRepository,
        IGenericRepository<ProjectPriority> priorityRepository,
        IGenericRepository<ProjectStatus> statusRepository,
        IGenericRepository<ExecutiveAgency> agencyRepository,
        IGenericRepository<ProjectAssignment> assignmentRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _subProjectRepository = subProjectRepository;
        _mainProjectRepository = mainProjectRepository;
        _markazRepository = markazRepository;
        _priorityRepository = priorityRepository;
        _statusRepository = statusRepository;
        _agencyRepository = agencyRepository;
        _assignmentRepository = assignmentRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
```

2. Add the new method right before the closing `}` of the class (after `ValidateReferencesAsync`):

```csharp

    public async Task<SubProjectDetailDto> AssignExecutiveAgencyAsync(int id, int executiveAgencyId, CancellationToken cancellationToken = default)
    {
        var subProject = await _subProjectRepository.GetByIdAsync(id, cancellationToken);
        if (subProject == null)
        {
            throw new NotFoundException($"المشروع الفرعي رقم {id} غير موجود");
        }

        var agency = await _agencyRepository.GetByIdAsync(executiveAgencyId, cancellationToken);
        if (agency == null)
        {
            throw new NotFoundException("الجهة التنفيذية المحددة غير موجودة");
        }

        var isAgencyChanging = subProject.ExecutiveAgencyId.HasValue && subProject.ExecutiveAgencyId != executiveAgencyId;
        if (isAgencyChanging)
        {
            var existingAssignments = await _assignmentRepository.FindAsync(x => x.SubProjectId == id, cancellationToken);
            foreach (var assignment in existingAssignments)
            {
                assignment.IsLocked = true;
                _assignmentRepository.Update(assignment);
            }
        }

        subProject.ExecutiveAgencyId = executiveAgencyId;
        _subProjectRepository.Update(subProject);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _subProjectRepository.GetWithDetailsAsync(id, cancellationToken);
        return _mapper.Map<SubProjectDetailDto>(updated);
    }
```

3. Add the missing `using` for `ExecutiveAgency`/`ProjectAssignment` — they are already in `SmartInvest.Domain.Entities`, which is already imported (`using SmartInvest.Domain.Entities;` at the top of the file) — no new using statement needed.

- [ ] **Step 7: Expose the endpoint + agency filter**

In `Backend/src/SmartInvest.API/Controllers/SubProjectsController.cs`:

1. The controller's class-level attribute is currently plain `[Authorize]` (any authenticated user), so the new action below needs its own `[Authorize(Roles = Roles.PlanningStaff)]` override — that requires `using SmartInvest.Domain.Common;`, which is not currently imported. Add it right after `using SmartInvest.Application.Interfaces;` at the top of the file:

```csharp
using SmartInvest.Domain.Common;
```

2. Add the assignment endpoint — insert after the existing `Update` action and before `Delete`:

```csharp

    [HttpPut("{id:int}/executive-agency")]
    [Authorize(Roles = Roles.PlanningStaff)]
    public async Task<ActionResult<SubProjectDetailDto>> AssignExecutiveAgency(int id, AssignExecutiveAgencyDto dto, CancellationToken cancellationToken)
    {
        var result = await _subProjectService.AssignExecutiveAgencyAsync(id, dto.ExecutiveAgencyId, cancellationToken);
        return Ok(result);
    }
```

An `executiveAgencyId` filter on `Search` is not required by the spec, so `Search` itself is left unchanged.

- [ ] **Step 8: Build and verify**

Run: `dotnet build Backend`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 9: Manual verification**

As admin (has `PlanningManager`, which is inside `PlanningStaff`): pick an existing sub-project id and the agency id created in Task 5. `PUT /api/subprojects/{id}/executive-agency` with `{"executiveAgencyId": 1}` → 200, response includes `executiveAgencyId: 1, executiveAgencyName: "شركة مياه الشرب"`. `GET /api/subprojects/{id}` → same fields present.

- [ ] **Step 10: Commit**

```bash
git add Backend/src/SmartInvest.Application/DTOs/SubProjectDtos.cs \
        Backend/src/SmartInvest.Application/Common/Mappings/SubProjectMappingProfile.cs \
        Backend/src/SmartInvest.Application/Interfaces/ISubProjectService.cs \
        Backend/src/SmartInvest.Application/Services/SubProjectService.cs \
        Backend/src/SmartInvest.Infrastructure/Repositories/SubProjectRepository.cs \
        Backend/src/SmartInvest.API/Controllers/SubProjectsController.cs
git commit -m "feat: assign sub-projects to an executive agency, lock prior assignments on reassignment"
```

---

### Task 9: ProjectAssignment CRUD (agency assigns contractor)

**Files:**
- Create: `Backend/src/SmartInvest.Domain/Interfaces/IProjectAssignmentRepository.cs`
- Create: `Backend/src/SmartInvest.Infrastructure/Repositories/ProjectAssignmentRepository.cs`
- Create: `Backend/src/SmartInvest.Application/DTOs/ProjectAssignmentDtos.cs`
- Create: `Backend/src/SmartInvest.Application/Validators/CreateProjectAssignmentDtoValidator.cs`
- Create: `Backend/src/SmartInvest.Application/Common/Mappings/ProjectAssignmentMappingProfile.cs`
- Create: `Backend/src/SmartInvest.Application/Interfaces/IProjectAssignmentService.cs`
- Create: `Backend/src/SmartInvest.Application/Services/ProjectAssignmentService.cs`
- Create: `Backend/src/SmartInvest.API/Controllers/ProjectAssignmentsController.cs`
- Modify: `Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `ICurrentUserService` (Task 1); `SubProject.ExecutiveAgencyId` (Task 2/8); `Roles.StaffAndAgency/PlanningManager` (Task 1).
- Produces: `IProjectAssignmentService` (`GetBySubProjectAsync/CreateAsync/UpdateGeneralAsync/DeleteAsync`) — `AssignmentId` (`int`) consumed by Task 10 (`ProjectAssignmentChangeRequest.AssignmentId`).

- [ ] **Step 1: Bespoke repository (needs includes)**

Create `Backend/src/SmartInvest.Domain/Interfaces/IProjectAssignmentRepository.cs`:

```csharp
using SmartInvest.Domain.Entities;

namespace SmartInvest.Domain.Interfaces;

public interface IProjectAssignmentRepository : IGenericRepository<ProjectAssignment>
{
    Task<ProjectAssignment?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectAssignment>> GetBySubProjectAsync(int subProjectId, CancellationToken cancellationToken = default);
}
```

Create `Backend/src/SmartInvest.Infrastructure/Repositories/ProjectAssignmentRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;
using SmartInvest.Infrastructure.Data;

namespace SmartInvest.Infrastructure.Repositories;

public class ProjectAssignmentRepository : GenericRepository<ProjectAssignment>, IProjectAssignmentRepository
{
    public ProjectAssignmentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<ProjectAssignment?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.SubProject).ThenInclude(s => s.ExecutiveAgency)
            .Include(x => x.Contractor)
            .Include(x => x.ContractType)
            .FirstOrDefaultAsync(x => x.AssignmentId == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectAssignment>> GetBySubProjectAsync(int subProjectId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.SubProject).ThenInclude(s => s.ExecutiveAgency)
            .Include(x => x.Contractor)
            .Include(x => x.ContractType)
            .Where(x => x.SubProjectId == subProjectId)
            .OrderByDescending(x => x.AssignmentDate)
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 2: Register the repository in DI**

In `Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs`, add right after `services.AddScoped<ISubProjectRepository, SubProjectRepository>();`:

```csharp
        services.AddScoped<IProjectAssignmentRepository, ProjectAssignmentRepository>();
```

- [ ] **Step 3: DTOs**

Create `Backend/src/SmartInvest.Application/DTOs/ProjectAssignmentDtos.cs`:

```csharp
namespace SmartInvest.Application.DTOs;

public class ProjectAssignmentDto
{
    public int Id { get; set; }
    public int SubProjectId { get; set; }
    public int ExecutiveAgencyId { get; set; }
    public string ExecutiveAgencyName { get; set; } = string.Empty;
    public int? ContractorId { get; set; }
    public string? ContractorName { get; set; }
    public int ContractTypeId { get; set; }
    public string ContractTypeName { get; set; } = string.Empty;
    public DateTime AssignmentDate { get; set; }
    public string? ContractNumber { get; set; }
    public decimal? ContractValue { get; set; }
    public DateTime ExpectedStartDate { get; set; }
    public DateTime ExpectedEndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsLocked { get; set; }
}

public class CreateProjectAssignmentDto
{
    public int? ContractorId { get; set; }
    public int ContractTypeId { get; set; }
    public string? ContractNumber { get; set; }
    public decimal? ContractValue { get; set; }
    public DateTime ExpectedStartDate { get; set; }
    public DateTime ExpectedEndDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateProjectAssignmentDto
{
    public int? ContractorId { get; set; }
    public int ContractTypeId { get; set; }
    public string? ContractNumber { get; set; }
    public string? Notes { get; set; }
}
```

Note: `UpdateProjectAssignmentDto` intentionally excludes `ContractValue`/`ExpectedEndDate`/`ExpectedStartDate` — those are only mutable through the Task 10 change-request flow.

- [ ] **Step 4: Validator**

Create `Backend/src/SmartInvest.Application/Validators/CreateProjectAssignmentDtoValidator.cs`:

```csharp
using FluentValidation;
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Validators;

public class CreateProjectAssignmentDtoValidator : AbstractValidator<CreateProjectAssignmentDto>
{
    public CreateProjectAssignmentDtoValidator()
    {
        RuleFor(x => x.ContractTypeId).GreaterThan(0).WithMessage("يجب اختيار نوع العقد");
        RuleFor(x => x.ContractValue).GreaterThanOrEqualTo(0).When(x => x.ContractValue.HasValue)
            .WithMessage("قيمة العقد لا يمكن أن تكون سالبة");
        RuleFor(x => x.ExpectedEndDate).GreaterThan(x => x.ExpectedStartDate)
            .WithMessage("تاريخ الانتهاء المتوقع يجب أن يكون بعد تاريخ البدء المتوقع");
    }
}

public class UpdateProjectAssignmentDtoValidator : AbstractValidator<UpdateProjectAssignmentDto>
{
    public UpdateProjectAssignmentDtoValidator()
    {
        RuleFor(x => x.ContractTypeId).GreaterThan(0).WithMessage("يجب اختيار نوع العقد");
    }
}
```

- [ ] **Step 5: Mapping profile**

Create `Backend/src/SmartInvest.Application/Common/Mappings/ProjectAssignmentMappingProfile.cs`:

```csharp
using AutoMapper;
using SmartInvest.Application.DTOs;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Application.Common.Mappings;

public class ProjectAssignmentMappingProfile : Profile
{
    public ProjectAssignmentMappingProfile()
    {
        CreateMap<ProjectAssignment, ProjectAssignmentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.AssignmentId))
            .ForMember(dest => dest.ExecutiveAgencyId, opt => opt.MapFrom(src => src.SubProject.ExecutiveAgencyId ?? 0))
            .ForMember(dest => dest.ExecutiveAgencyName, opt => opt.MapFrom(src => src.SubProject.ExecutiveAgency != null ? src.SubProject.ExecutiveAgency.AgencyName : string.Empty))
            .ForMember(dest => dest.ContractorName, opt => opt.MapFrom(src => src.Contractor != null ? src.Contractor.ContractorName : null))
            .ForMember(dest => dest.ContractTypeName, opt => opt.MapFrom(src => src.ContractType.ContractName));
    }
}
```

- [ ] **Step 6: Service interface**

Create `Backend/src/SmartInvest.Application/Interfaces/IProjectAssignmentService.cs`:

```csharp
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IProjectAssignmentService
{
    Task<IReadOnlyList<ProjectAssignmentDto>> GetBySubProjectAsync(int subProjectId, CancellationToken cancellationToken = default);

    Task<ProjectAssignmentDto> CreateAsync(int subProjectId, CreateProjectAssignmentDto dto, CancellationToken cancellationToken = default);

    Task<ProjectAssignmentDto> UpdateGeneralAsync(int subProjectId, int id, UpdateProjectAssignmentDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int subProjectId, int id, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 7: Service implementation with ownership scoping**

Create `Backend/src/SmartInvest.Application/Services/ProjectAssignmentService.cs`:

```csharp
using AutoMapper;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class ProjectAssignmentService : IProjectAssignmentService
{
    private readonly IProjectAssignmentRepository _assignmentRepository;
    private readonly ISubProjectRepository _subProjectRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectAssignmentService(
        IProjectAssignmentRepository assignmentRepository,
        ISubProjectRepository subProjectRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _assignmentRepository = assignmentRepository;
        _subProjectRepository = subProjectRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ProjectAssignmentDto>> GetBySubProjectAsync(int subProjectId, CancellationToken cancellationToken = default)
    {
        var assignments = await _assignmentRepository.GetBySubProjectAsync(subProjectId, cancellationToken);
        return _mapper.Map<List<ProjectAssignmentDto>>(assignments);
    }

    public async Task<ProjectAssignmentDto> CreateAsync(int subProjectId, CreateProjectAssignmentDto dto, CancellationToken cancellationToken = default)
    {
        var subProject = await GetSubProjectOrThrowAsync(subProjectId, cancellationToken);
        EnsureAgencyOwnership(subProject);

        var assignment = new ProjectAssignment
        {
            SubProjectId = subProjectId,
            ContractorId = dto.ContractorId,
            ContractTypeId = dto.ContractTypeId,
            ContractNumber = dto.ContractNumber,
            ContractValue = dto.ContractValue,
            ExpectedStartDate = dto.ExpectedStartDate,
            ExpectedEndDate = dto.ExpectedEndDate,
            Notes = dto.Notes,
            AssignmentDate = DateTime.UtcNow,
            IsLocked = false,
        };

        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _assignmentRepository.GetWithDetailsAsync(assignment.AssignmentId, cancellationToken);
        return _mapper.Map<ProjectAssignmentDto>(created);
    }

    public async Task<ProjectAssignmentDto> UpdateGeneralAsync(int subProjectId, int id, UpdateProjectAssignmentDto dto, CancellationToken cancellationToken = default)
    {
        var subProject = await GetSubProjectOrThrowAsync(subProjectId, cancellationToken);
        EnsureAgencyOwnership(subProject);

        var assignment = await GetAssignmentOrThrowAsync(subProjectId, id, cancellationToken);
        if (assignment.IsLocked)
        {
            throw new BusinessRuleException("هذا التعيين مقفول ولا يمكن تعديله (تم تغيير الجهة التنفيذية للمشروع)");
        }

        assignment.ContractorId = dto.ContractorId;
        assignment.ContractTypeId = dto.ContractTypeId;
        assignment.ContractNumber = dto.ContractNumber;
        assignment.Notes = dto.Notes;

        _assignmentRepository.Update(assignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _assignmentRepository.GetWithDetailsAsync(id, cancellationToken);
        return _mapper.Map<ProjectAssignmentDto>(updated);
    }

    public async Task DeleteAsync(int subProjectId, int id, CancellationToken cancellationToken = default)
    {
        await GetSubProjectOrThrowAsync(subProjectId, cancellationToken);
        var assignment = await GetAssignmentOrThrowAsync(subProjectId, id, cancellationToken);

        _assignmentRepository.Remove(assignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<SubProject> GetSubProjectOrThrowAsync(int subProjectId, CancellationToken cancellationToken)
    {
        var subProject = await _subProjectRepository.GetByIdAsync(subProjectId, cancellationToken);
        if (subProject == null)
        {
            throw new NotFoundException($"المشروع الفرعي رقم {subProjectId} غير موجود");
        }

        return subProject;
    }

    private async Task<ProjectAssignment> GetAssignmentOrThrowAsync(int subProjectId, int id, CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(id, cancellationToken);
        if (assignment == null || assignment.SubProjectId != subProjectId)
        {
            throw new NotFoundException($"التعيين رقم {id} غير موجود");
        }

        return assignment;
    }

    /// <summary>
    /// مدير التخطيط وموظف التخطيط لهم تجاوز كامل. الجهة التنفيذية مقصورة على مشروعاتها فقط.
    /// </summary>
    private void EnsureAgencyOwnership(SubProject subProject)
    {
        if (_currentUser.Role != Roles.ExecutiveAgency)
        {
            return;
        }

        if (subProject.ExecutiveAgencyId == null || subProject.ExecutiveAgencyId != _currentUser.ExecutiveAgencyId)
        {
            throw new ForbiddenAccessException("لا يمكنك التعامل مع تعيينات مشروع غير مسند لجهتك");
        }
    }
}
```

- [ ] **Step 8: Controller**

Create `Backend/src/SmartInvest.API/Controllers/ProjectAssignmentsController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/subprojects/{subProjectId:int}/assignments")]
[Authorize(Roles = Roles.StaffAndAgency)]
public class ProjectAssignmentsController : ControllerBase
{
    private readonly IProjectAssignmentService _assignmentService;

    public ProjectAssignmentsController(IProjectAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectAssignmentDto>>> GetAll(int subProjectId, CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetBySubProjectAsync(subProjectId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectAssignmentDto>> Create(int subProjectId, CreateProjectAssignmentDto dto, CancellationToken cancellationToken)
    {
        var result = await _assignmentService.CreateAsync(subProjectId, dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectAssignmentDto>> Update(int subProjectId, int id, UpdateProjectAssignmentDto dto, CancellationToken cancellationToken)
    {
        var result = await _assignmentService.UpdateGeneralAsync(subProjectId, id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> Delete(int subProjectId, int id, CancellationToken cancellationToken)
    {
        await _assignmentService.DeleteAsync(subProjectId, id, cancellationToken);
        return NoContent();
    }
}
```

- [ ] **Step 9: Build and verify**

Run: `dotnet build Backend`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 10: Manual verification**

As admin: create an assignment on the sub-project from Task 8 (already has `executiveAgencyId: 1`): `POST /api/subprojects/{subProjectId}/assignments` with `{"contractorId": 1, "contractTypeId": 1, "contractNumber": "C-001", "contractValue": 5000000, "expectedStartDate": "2026-08-01", "expectedEndDate": "2027-01-01", "notes": "مرحلة أولى"}` → 200. `GET /api/subprojects/{subProjectId}/assignments` → listed with `isLocked: false`.

Then login as `agency1` (from Task 5) and repeat the `POST` on a *different* sub-project that has no `executiveAgencyId` set (or belongs to a different agency) → expect `403 Forbidden` with the Arabic ownership message.

- [ ] **Step 11: Commit**

```bash
git add Backend/src/SmartInvest.Domain/Interfaces/IProjectAssignmentRepository.cs \
        Backend/src/SmartInvest.Infrastructure/Repositories/ProjectAssignmentRepository.cs \
        Backend/src/SmartInvest.Application/DTOs/ProjectAssignmentDtos.cs \
        Backend/src/SmartInvest.Application/Validators/CreateProjectAssignmentDtoValidator.cs \
        Backend/src/SmartInvest.Application/Common/Mappings/ProjectAssignmentMappingProfile.cs \
        Backend/src/SmartInvest.Application/Interfaces/IProjectAssignmentService.cs \
        Backend/src/SmartInvest.Application/Services/ProjectAssignmentService.cs \
        Backend/src/SmartInvest.API/Controllers/ProjectAssignmentsController.cs \
        Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs
git commit -m "feat: add ProjectAssignment CRUD scoped to the owning executive agency"
```

---

### Task 10: Change-request workflow + reusable AuditLog service

**Files:**
- Create: `Backend/src/SmartInvest.Application/DTOs/ChangeRequestDtos.cs`
- Create: `Backend/src/SmartInvest.Application/DTOs/AuditLogDtos.cs`
- Create: `Backend/src/SmartInvest.Application/Validators/CreateChangeRequestDtoValidator.cs`
- Create: `Backend/src/SmartInvest.Application/Interfaces/IAuditLogService.cs`
- Create: `Backend/src/SmartInvest.Application/Services/AuditLogService.cs`
- Create: `Backend/src/SmartInvest.Application/Interfaces/IChangeRequestService.cs`
- Create: `Backend/src/SmartInvest.Application/Services/ChangeRequestService.cs`
- Create: `Backend/src/SmartInvest.API/Controllers/AuditLogsController.cs`
- Modify: `Backend/src/SmartInvest.API/Controllers/ProjectAssignmentsController.cs`
- Modify: `Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `ICurrentUserService` (Task 1), `IProjectAssignmentRepository` (Task 9), `ProjectAssignmentChangeRequest`/`AuditLog` entities (Task 2), `Roles.StaffAndAgency/AssignmentParties` (Task 1).
- Produces: `IAuditLogService.LogAsync(string entityName, int entityId, string fieldName, string? oldValue, string? newValue, string changedByUserId, CancellationToken)` — reusable, intended for Phase 2 (متابعة المشروع) too. `IChangeRequestService` (`SubmitAsync/ApproveAsync/RejectAsync/GetHistoryAsync`).

- [ ] **Step 1: DTOs**

Create `Backend/src/SmartInvest.Application/DTOs/ChangeRequestDtos.cs`:

```csharp
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
```

Create `Backend/src/SmartInvest.Application/DTOs/AuditLogDtos.cs`:

```csharp
namespace SmartInvest.Application.DTOs;

public class AuditLogDto
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
```

- [ ] **Step 2: Validator**

Create `Backend/src/SmartInvest.Application/Validators/CreateChangeRequestDtoValidator.cs`:

```csharp
using FluentValidation;
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Validators;

public class CreateChangeRequestDtoValidator : AbstractValidator<CreateChangeRequestDto>
{
    public CreateChangeRequestDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => x.RequestedContractValue.HasValue || x.RequestedExpectedEndDate.HasValue)
            .WithMessage("يجب تحديد قيمة عقد جديدة أو تاريخ انتهاء متوقع جديد على الأقل");

        RuleFor(x => x.RequestedContractValue)
            .GreaterThanOrEqualTo(0).When(x => x.RequestedContractValue.HasValue)
            .WithMessage("قيمة العقد لا يمكن أن تكون سالبة");
    }
}
```

- [ ] **Step 3: Reusable `IAuditLogService`**

Create `Backend/src/SmartInvest.Application/Interfaces/IAuditLogService.cs`:

```csharp
namespace SmartInvest.Application.Interfaces;

/// <summary>
/// آلية تسجيل تعديلات عامة، قابلة لإعادة الاستخدام عبر أي كيان (تُستخدم هنا لـ ProjectAssignment،
/// ومخطط استخدامها في مسار متابعة المشروع لاحقًا).
/// </summary>
public interface IAuditLogService
{
    Task LogAsync(string entityName, int entityId, string fieldName, string? oldValue, string? newValue, string changedByUserId, CancellationToken cancellationToken = default);
}
```

Create `Backend/src/SmartInvest.Application/Services/AuditLogService.cs`:

```csharp
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IGenericRepository<AuditLog> _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AuditLogService(IGenericRepository<AuditLog> auditLogRepository, IUnitOfWork unitOfWork)
    {
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(string entityName, int entityId, string fieldName, string? oldValue, string? newValue, string changedByUserId, CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
        };

        await _auditLogRepository.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: `IChangeRequestService`**

Create `Backend/src/SmartInvest.Application/Interfaces/IChangeRequestService.cs`:

```csharp
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Interfaces;

public interface IChangeRequestService
{
    Task<IReadOnlyList<ChangeRequestDto>> GetHistoryAsync(int assignmentId, CancellationToken cancellationToken = default);

    Task<ChangeRequestDto> SubmitAsync(int assignmentId, CreateChangeRequestDto dto, CancellationToken cancellationToken = default);

    Task<ChangeRequestDto> ApproveAsync(int assignmentId, int changeRequestId, ReviewChangeRequestDto dto, CancellationToken cancellationToken = default);

    Task<ChangeRequestDto> RejectAsync(int assignmentId, int changeRequestId, ReviewChangeRequestDto dto, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 5: `ChangeRequestService` implementation**

Create `Backend/src/SmartInvest.Application/Services/ChangeRequestService.cs`:

```csharp
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Enums;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class ChangeRequestService : IChangeRequestService
{
    private readonly IGenericRepository<ProjectAssignmentChangeRequest> _changeRequestRepository;
    private readonly IProjectAssignmentRepository _assignmentRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeRequestService(
        IGenericRepository<ProjectAssignmentChangeRequest> changeRequestRepository,
        IProjectAssignmentRepository assignmentRepository,
        ICurrentUserService currentUser,
        IAuditLogService auditLogService,
        IUnitOfWork unitOfWork)
    {
        _changeRequestRepository = changeRequestRepository;
        _assignmentRepository = assignmentRepository;
        _currentUser = currentUser;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ChangeRequestDto>> GetHistoryAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        var requests = await _changeRequestRepository.FindAsync(x => x.AssignmentId == assignmentId, cancellationToken);
        return requests
            .OrderByDescending(x => x.RequestedAt)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ChangeRequestDto> SubmitAsync(int assignmentId, CreateChangeRequestDto dto, CancellationToken cancellationToken = default)
    {
        var assignment = await GetAssignmentOrThrowAsync(assignmentId, cancellationToken);
        EnsurePartyOwnership(assignment);

        var pending = await _changeRequestRepository.FindAsync(
            x => x.AssignmentId == assignmentId && x.Status == ChangeRequestStatus.Pending, cancellationToken);
        if (pending.Count > 0)
        {
            throw new BusinessRuleException("يوجد طلب تعديل قيد الانتظار بالفعل لهذا التعيين، يجب حسمه أولاً");
        }

        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("المستخدم غير معروف");
        var isContractor = _currentUser.Role == Roles.Contractor;

        var changeRequest = new ProjectAssignmentChangeRequest
        {
            AssignmentId = assignmentId,
            RequestedContractValue = dto.RequestedContractValue,
            RequestedExpectedEndDate = dto.RequestedExpectedEndDate,
            RequestedByUserId = userId,
            RequestedAt = DateTime.UtcNow,
            Status = isContractor ? ChangeRequestStatus.Pending : ChangeRequestStatus.Approved,
        };

        if (!isContractor)
        {
            changeRequest.ReviewedByUserId = userId;
            changeRequest.ReviewedAt = DateTime.UtcNow;
        }

        await _changeRequestRepository.AddAsync(changeRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!isContractor)
        {
            await ApplyToAssignmentAsync(assignment, changeRequest, userId, cancellationToken);
        }

        return MapToDto(changeRequest);
    }

    public async Task<ChangeRequestDto> ApproveAsync(int assignmentId, int changeRequestId, ReviewChangeRequestDto dto, CancellationToken cancellationToken = default)
    {
        var assignment = await GetAssignmentOrThrowAsync(assignmentId, cancellationToken);
        EnsureReviewerOwnership(assignment);

        var changeRequest = await GetPendingOrThrowAsync(assignmentId, changeRequestId, cancellationToken);
        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("المستخدم غير معروف");

        changeRequest.Status = ChangeRequestStatus.Approved;
        changeRequest.ReviewedByUserId = userId;
        changeRequest.ReviewedAt = DateTime.UtcNow;
        changeRequest.ReviewNote = dto.ReviewNote;

        _changeRequestRepository.Update(changeRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await ApplyToAssignmentAsync(assignment, changeRequest, userId, cancellationToken);

        return MapToDto(changeRequest);
    }

    public async Task<ChangeRequestDto> RejectAsync(int assignmentId, int changeRequestId, ReviewChangeRequestDto dto, CancellationToken cancellationToken = default)
    {
        var assignment = await GetAssignmentOrThrowAsync(assignmentId, cancellationToken);
        EnsureReviewerOwnership(assignment);

        var changeRequest = await GetPendingOrThrowAsync(assignmentId, changeRequestId, cancellationToken);
        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("المستخدم غير معروف");

        changeRequest.Status = ChangeRequestStatus.Rejected;
        changeRequest.ReviewedByUserId = userId;
        changeRequest.ReviewedAt = DateTime.UtcNow;
        changeRequest.ReviewNote = dto.ReviewNote;

        _changeRequestRepository.Update(changeRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(changeRequest);
    }

    private async Task ApplyToAssignmentAsync(ProjectAssignment assignment, ProjectAssignmentChangeRequest changeRequest, string userId, CancellationToken cancellationToken)
    {
        if (changeRequest.RequestedContractValue.HasValue && changeRequest.RequestedContractValue != assignment.ContractValue)
        {
            await _auditLogService.LogAsync(
                nameof(ProjectAssignment), assignment.AssignmentId, nameof(ProjectAssignment.ContractValue),
                assignment.ContractValue?.ToString(), changeRequest.RequestedContractValue.Value.ToString(), userId, cancellationToken);
            assignment.ContractValue = changeRequest.RequestedContractValue;
        }

        if (changeRequest.RequestedExpectedEndDate.HasValue && changeRequest.RequestedExpectedEndDate != assignment.ExpectedEndDate)
        {
            await _auditLogService.LogAsync(
                nameof(ProjectAssignment), assignment.AssignmentId, nameof(ProjectAssignment.ExpectedEndDate),
                assignment.ExpectedEndDate.ToString("O"), changeRequest.RequestedExpectedEndDate.Value.ToString("O"), userId, cancellationToken);
            assignment.ExpectedEndDate = changeRequest.RequestedExpectedEndDate.Value;
        }

        _assignmentRepository.Update(assignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<ProjectAssignment> GetAssignmentOrThrowAsync(int assignmentId, CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository.GetWithDetailsAsync(assignmentId, cancellationToken);
        if (assignment == null)
        {
            throw new NotFoundException($"التعيين رقم {assignmentId} غير موجود");
        }

        if (assignment.IsLocked)
        {
            throw new BusinessRuleException("هذا التعيين مقفول ولا يمكن تعديله (تم تغيير الجهة التنفيذية للمشروع)");
        }

        return assignment;
    }

    private async Task<ProjectAssignmentChangeRequest> GetPendingOrThrowAsync(int assignmentId, int changeRequestId, CancellationToken cancellationToken)
    {
        var changeRequest = await _changeRequestRepository.GetByIdAsync(changeRequestId, cancellationToken);
        if (changeRequest == null || changeRequest.AssignmentId != assignmentId)
        {
            throw new NotFoundException($"طلب التعديل رقم {changeRequestId} غير موجود");
        }

        if (changeRequest.Status != ChangeRequestStatus.Pending)
        {
            throw new BusinessRuleException("تم حسم هذا الطلب بالفعل");
        }

        return changeRequest;
    }

    /// <summary>يسمح بتقديم طلب: تخطيط (تجاوز كامل)، الجهة المسندة، أو المقاول المسند لنفس التعيين.</summary>
    private void EnsurePartyOwnership(ProjectAssignment assignment)
    {
        switch (_currentUser.Role)
        {
            case Roles.ExecutiveAgency:
                if (assignment.SubProject.ExecutiveAgencyId != _currentUser.ExecutiveAgencyId)
                {
                    throw new ForbiddenAccessException("لا يمكنك التعامل مع تعيين مشروع غير مسند لجهتك");
                }

                break;
            case Roles.Contractor:
                if (assignment.ContractorId != _currentUser.ContractorId)
                {
                    throw new ForbiddenAccessException("لا يمكنك التعامل مع تعيين غير مسند إليك");
                }

                break;
        }
    }

    /// <summary>الموافقة/الرفض مقصورة على تخطيط (تجاوز كامل) أو الجهة المسندة — المقاول لا يراجع طلباته.</summary>
    private void EnsureReviewerOwnership(ProjectAssignment assignment)
    {
        if (_currentUser.Role == Roles.ExecutiveAgency && assignment.SubProject.ExecutiveAgencyId != _currentUser.ExecutiveAgencyId)
        {
            throw new ForbiddenAccessException("لا يمكنك مراجعة طلب تعديل لمشروع غير مسند لجهتك");
        }

        if (_currentUser.Role == Roles.Contractor)
        {
            throw new ForbiddenAccessException("لا يحق للمقاول مراجعة طلبات التعديل");
        }
    }

    private static ChangeRequestDto MapToDto(ProjectAssignmentChangeRequest entity)
    {
        return new ChangeRequestDto
        {
            Id = entity.ChangeRequestId,
            AssignmentId = entity.AssignmentId,
            RequestedContractValue = entity.RequestedContractValue,
            RequestedExpectedEndDate = entity.RequestedExpectedEndDate,
            Status = entity.Status,
            RequestedByUserId = entity.RequestedByUserId,
            RequestedAt = entity.RequestedAt,
            ReviewedByUserId = entity.ReviewedByUserId,
            ReviewedAt = entity.ReviewedAt,
            ReviewNote = entity.ReviewNote,
        };
    }
}
```

- [ ] **Step 6: Wire the change-request endpoints into `ProjectAssignmentsController`**

In `Backend/src/SmartInvest.API/Controllers/ProjectAssignmentsController.cs`, add a constructor parameter and four actions. Replace the constructor:

```csharp
    private readonly IProjectAssignmentService _assignmentService;
    private readonly IChangeRequestService _changeRequestService;

    public ProjectAssignmentsController(IProjectAssignmentService assignmentService, IChangeRequestService changeRequestService)
    {
        _assignmentService = assignmentService;
        _changeRequestService = changeRequestService;
    }
```

Add these actions right before the final closing `}` of the class (after `Delete`):

```csharp

    [HttpGet("{id:int}/change-requests")]
    public async Task<ActionResult<IReadOnlyList<ChangeRequestDto>>> GetChangeRequests(int subProjectId, int id, CancellationToken cancellationToken)
    {
        var result = await _changeRequestService.GetHistoryAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/change-requests")]
    [Authorize(Roles = Roles.AssignmentParties)]
    public async Task<ActionResult<ChangeRequestDto>> SubmitChangeRequest(int subProjectId, int id, CreateChangeRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _changeRequestService.SubmitAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}/change-requests/{changeRequestId:int}/approve")]
    public async Task<ActionResult<ChangeRequestDto>> ApproveChangeRequest(int subProjectId, int id, int changeRequestId, ReviewChangeRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _changeRequestService.ApproveAsync(id, changeRequestId, dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}/change-requests/{changeRequestId:int}/reject")]
    public async Task<ActionResult<ChangeRequestDto>> RejectChangeRequest(int subProjectId, int id, int changeRequestId, ReviewChangeRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _changeRequestService.RejectAsync(id, changeRequestId, dto, cancellationToken);
        return Ok(result);
    }
```

Note: `SubmitChangeRequest` needs `[Authorize(Roles = Roles.AssignmentParties)]` because the class-level attribute is `Roles.StaffAndAgency` (no `Contractor`) — contractors must reach this specific action. `GetChangeRequests`/`Approve`/`Reject` inherit the class-level `StaffAndAgency` (contractors cannot review, enforced again defensively inside `EnsureReviewerOwnership`).

- [ ] **Step 7: Minimal read-only `AuditLogsController`**

Create `Backend/src/SmartInvest.API/Controllers/AuditLogsController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = Roles.PlanningManager)]
public class AuditLogsController : ControllerBase
{
    private readonly IGenericRepository<SmartInvest.Domain.Entities.AuditLog> _auditLogRepository;

    public AuditLogsController(IGenericRepository<SmartInvest.Domain.Entities.AuditLog> auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAll([FromQuery] string entityName, [FromQuery] int entityId, CancellationToken cancellationToken)
    {
        var logs = await _auditLogRepository.FindAsync(x => x.EntityName == entityName && x.EntityId == entityId, cancellationToken);

        var result = logs
            .OrderByDescending(x => x.ChangedAt)
            .Select(x => new AuditLogDto
            {
                Id = x.AuditLogId,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                FieldName = x.FieldName,
                OldValue = x.OldValue,
                NewValue = x.NewValue,
                ChangedByUserId = x.ChangedByUserId,
                ChangedAt = x.ChangedAt,
            })
            .ToList();

        return Ok(result);
    }
}
```

- [ ] **Step 8: Register the two new services in DI**

In `Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs`, add right after the `IProjectAssignmentService`-equivalent registration from Task 9 (i.e. after wherever `IProjectAssignmentRepository`/service lines ended up — add these two lines at the end of the service registrations, right before `return services;`):

```csharp
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IChangeRequestService, ChangeRequestService>();
```

- [ ] **Step 9: Build and verify**

Run: `dotnet build Backend`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 10: Manual verification**

Using the assignment created in Task 9's verification (assume `id=1`, `subProjectId` from Task 8):
1. As admin: `PUT /api/subprojects/{subProjectId}/assignments/1/change-requests` — wait, submission is `POST`. `POST /api/subprojects/{subProjectId}/assignments/1/change-requests` with `{"requestedContractValue": 5500000}` → expect 200, `status: 1` (Approved), applied immediately. `GET .../assignments/1/change-requests` → one entry, `Approved`.
2. As `contractor1` (Task 6) — first make sure the assignment's `ContractorId` is 1 (matches) — `POST .../assignments/1/change-requests` with `{"requestedExpectedEndDate": "2027-03-01"}` → expect 200, `status: 0` (Pending).
3. Immediately repeat step 2 → expect `400` with "يوجد طلب تعديل قيد الانتظار بالفعل...".
4. As admin: `PUT .../assignments/1/change-requests/{changeRequestId}/approve` with `{}` → expect 200, `status: 1`, and `GET /api/subprojects/{subProjectId}/assignments` shows the updated `expectedEndDate`.
5. `GET /api/audit-logs?entityName=ProjectAssignment&entityId=1` (as admin) → shows both field changes with old/new values and the acting user id.

- [ ] **Step 11: Commit**

```bash
git add Backend/src/SmartInvest.Application/DTOs/ChangeRequestDtos.cs \
        Backend/src/SmartInvest.Application/DTOs/AuditLogDtos.cs \
        Backend/src/SmartInvest.Application/Validators/CreateChangeRequestDtoValidator.cs \
        Backend/src/SmartInvest.Application/Interfaces/IAuditLogService.cs \
        Backend/src/SmartInvest.Application/Services/AuditLogService.cs \
        Backend/src/SmartInvest.Application/Interfaces/IChangeRequestService.cs \
        Backend/src/SmartInvest.Application/Services/ChangeRequestService.cs \
        Backend/src/SmartInvest.API/Controllers/AuditLogsController.cs \
        Backend/src/SmartInvest.API/Controllers/ProjectAssignmentsController.cs \
        Backend/src/SmartInvest.Infrastructure/DependencyInjection.cs
git commit -m "feat: add contractor change-request approval workflow and reusable audit log"
```

---

### Task 11: Seed the two new roles at startup

**Files:**
- Modify: `Backend/src/SmartInvest.API/Program.cs`

**Interfaces:**
- Consumes: `Roles.ExecutiveAgency`, `Roles.Contractor` (Task 1).
- Produces: nothing consumed by later tasks — this is the last task.

- [ ] **Step 1: Add the two roles to the seeding array**

In `Backend/src/SmartInvest.API/Program.cs`, find:

```csharp
    string[] roles = { Roles.PlanningEmployee, Roles.PlanningManager };
```

Replace with:

```csharp
    string[] roles = { Roles.PlanningEmployee, Roles.PlanningManager, Roles.ExecutiveAgency, Roles.Contractor };
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Backend`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Manual verification**

Run the API once (`dotnet run --project Backend/src/SmartInvest.API`) so the seeding block in `Program.cs` executes, then check the `AspNetRoles` table (or re-run `GET /api/agencies` login flow from Task 5, which already implicitly required the `ExecutiveAgency` role to exist) confirms no `BusinessRuleException`/role-not-found error occurs on `AddToRoleAsync`.

- [ ] **Step 4: Commit**

```bash
git add Backend/src/SmartInvest.API/Program.cs
git commit -m "feat: seed ExecutiveAgency and Contractor roles at startup"
```

---

## Self-Review Notes

**Spec coverage:**
- إسناد مشروع فرعي لجهة تنفيذية واحدة → Task 8.
- دور لكل جهة (تسجيل دخول 1:1) → Task 5 (+ Task 1 role constants, Task 4 provisioning).
- تعيين مقاول واحد أو أكثر بنوع عقد محدد → Task 9 (multiple `ProjectAssignment` rows per sub-project already supported by the schema).
- مقاول 1:1 حساب دخول → Task 6.
- CRUD كاملة (جهات، مقاولين، أنواع عقود، تعيينات) → Tasks 5, 6, 7, 9.
- مدير التخطيط تجاوز كامل → enforced in every service's ownership check (`if (_currentUser.Role != Roles.ExecutiveAgency) return;` pattern skips the check entirely for Manager/Employee; controller-level attributes always include `PlanningManager`).
- قاعدة القفل عند تغيير الجهة → Task 8, `AssignExecutiveAgencyAsync`.
- مسار الموافقة لتعديلات المقاول + Audit Log عام → Task 10.
- "فين تتحط الصلاحية" → answered structurally throughout: `[Authorize(Roles=...)]` on controllers/actions (role gate), ownership checks inside services via `ICurrentUserService` (Task 1, used from Task 9 onward).

**Placeholder scan:** none found — every step has complete, concrete code.

**Type consistency check:** `ProjectAssignment.AssignmentId` (Task 2) matches `ProjectAssignmentChangeRequest.AssignmentId` (Task 2) and `IProjectAssignmentRepository`/`ChangeRequestService` usage (Tasks 9–10). `ExecutiveAgencyDto.Id`/`ContractorDto.Id`/`ContractTypeDto.Id` (Tasks 5–7) match the `int` FK fields used in `CreateProjectAssignmentDto`/`SubProject.ExecutiveAgencyId` (Tasks 8–9). `ICurrentUserService.Role/ExecutiveAgencyId/ContractorId` (Task 1) match the JWT claim names `"executiveAgencyId"`/`"contractorId"` set in Task 3 exactly.
