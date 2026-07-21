using Microsoft.EntityFrameworkCore;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Infrastructure.Data;

/// <summary>
/// Seeds the reference (lookup) data used by the project forms:
/// governorate, markaz, villages, programs, sub-programs, priorities, statuses.
/// Idempotent — only inserts a group if its table is empty.
/// </summary>
public static class LookupSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedGeographyAsync(context);
        await SeedProgramsAsync(context);
        await SeedPrioritiesAsync(context);
        await SeedStatusesAsync(context);
    }

    private static async Task SeedGeographyAsync(AppDbContext context)
    {
        if (await context.Set<Governorate>().AnyAsync())
        {
            return;
        }

        var menoufia = new Governorate { GovernorateName = "المنوفية" };
        context.Add(menoufia);
        await context.SaveChangesAsync();

        var markazNames = new[]
        {
            "المنوفية", "حي شرق", "حي غرب", "سرس الليان", "مركز اشمون", "مركز الباجور",
            "مركز السادات", "مركز الشهداء", "مركز بركة السبع", "مركز تلا",
            "مركز شبين الكوم", "مركز قويسنا", "مركز منوف",
        };

        var markazList = markazNames
            .Select(name => new Markaz { MarkazName = name, GovernorateId = menoufia.GovernorateId })
            .ToList();
        context.AddRange(markazList);
        await context.SaveChangesAsync();

        // قرى مركز قويسنا (مبدئية — للمراجعة والتوسعة لباقي المراكز لاحقًا)
        var quwaysna = markazList.First(m => m.MarkazName == "مركز قويسنا");
        var quwaysnaVillages = new[]
        {
             "ميت برة", "عرب الرمل"
            , "الرمالي", "أشليم",
        };
        context.AddRange(quwaysnaVillages.Select(name =>
            new Village { VillageName = name, MarkazId = quwaysna.MarkazId }));
        await context.SaveChangesAsync();
    }

    private static async Task SeedProgramsAsync(AppDbContext context)
    {
        if (await context.Set<MainProgram>().AnyAsync())
        {
            return;
        }

        var mainPrograms = new[]
        {
            new MainProgram { ProgramName = "الإدارة المحلية والدعم الفني" },
            new MainProgram { ProgramName = "التنمية الحضارية والريفية" },
            new MainProgram { ProgramName = "النقل والطرق والمواصلات" },
            new MainProgram { ProgramName = "تحسين البيئة" },
            new MainProgram { ProgramName = "تدعيم الخدمات المحلية والمجتمع" },
        };
        context.AddRange(mainPrograms);
        await context.SaveChangesAsync();

        MainProgram Prog(string name) => mainPrograms.First(p => p.ProgramName == name);

        var subPrograms = new[]
        {
            new SubProgram { SubProgramName = "إدارة الأزمات والكوارث", ProgramId = Prog("الإدارة المحلية والدعم الفني").ProgramId },
            new SubProgram { SubProgramName = "دعم الوحدات المحلية", ProgramId = Prog("الإدارة المحلية والدعم الفني").ProgramId },
            new SubProgram { SubProgramName = "التنمية الحضارية", ProgramId = Prog("التنمية الحضارية والريفية").ProgramId },
            new SubProgram { SubProgramName = "الطرق المحلية", ProgramId = Prog("النقل والطرق والمواصلات").ProgramId },
            new SubProgram { SubProgramName = "إنارة الطرق والشوارع", ProgramId = Prog("النقل والطرق والمواصلات").ProgramId },
            new SubProgram { SubProgramName = "إدارة المخلفات الصلبة", ProgramId = Prog("تحسين البيئة").ProgramId },
            new SubProgram { SubProgramName = "تدعيم الخدمات المحلية والمجتمعية", ProgramId = Prog("تدعيم الخدمات المحلية والمجتمع").ProgramId },
        };
        context.AddRange(subPrograms);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPrioritiesAsync(AppDbContext context)
    {
        if (await context.Set<ProjectPriority>().AnyAsync())
        {
            return;
        }

        context.AddRange(
            new ProjectPriority { Priority = "عالية" },
            new ProjectPriority { Priority = "متوسطة" },
            new ProjectPriority { Priority = "منخفضة" });
        await context.SaveChangesAsync();
    }

    private static async Task SeedStatusesAsync(AppDbContext context)
    {
        if (await context.Set<ProjectStatus>().AnyAsync())
        {
            return;
        }

        context.AddRange(
            new ProjectStatus { StatusName = "جديد" },
            new ProjectStatus { StatusName = "قيد التنفيذ" },
            new ProjectStatus { StatusName = "متوقف" },
            new ProjectStatus { StatusName = "منتهي" });
        await context.SaveChangesAsync();
    }
}
