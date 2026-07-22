using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartInvest.Application.Interfaces;
using SmartInvest.Application.Services;
using SmartInvest.Domain.Interfaces;
using SmartInvest.Infrastructure.Data;
using SmartInvest.Infrastructure.Identity;
using SmartInvest.Infrastructure.Repositories;

namespace SmartInvest.Infrastructure;

/// <summary>
/// Registers Infrastructure-layer services (EF Core, Identity, repositories) into the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMainProjectRepository, MainProjectRepository>();
        services.AddScoped<ISubProjectRepository, SubProjectRepository>();
        services.AddScoped<IProjectAssignmentRepository, ProjectAssignmentRepository>();
        services.AddScoped<IMainProjectService, MainProjectService>();
        services.AddScoped<ISubProjectService, SubProjectService>();
        services.AddScoped<IProjectSpecificationService, ProjectSpecificationService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IExecutiveAgencyService, ExecutiveAgencyService>();
        services.AddScoped<IContractorService, ContractorService>();
        services.AddScoped<IContractTypeService, ContractTypeService>();
        services.AddScoped<IProjectAssignmentService, ProjectAssignmentService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IChangeRequestService, ChangeRequestService>();

        return services;
    }
}
