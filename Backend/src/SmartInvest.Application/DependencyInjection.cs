using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartInvest.Application.Interfaces;
using SmartInvest.Application.Services;

namespace SmartInvest.Application;

/// <summary>
/// Registers Application-layer services into the DI container.
/// Called from the API composition root.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        services.AddValidatorsFromAssembly(assembly);

        // Application services
        services.AddScoped<IInvestmentProjectService, InvestmentProjectService>();

        return services;
    }
}
