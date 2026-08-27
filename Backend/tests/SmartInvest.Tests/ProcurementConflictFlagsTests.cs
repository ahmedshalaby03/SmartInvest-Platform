using Microsoft.EntityFrameworkCore;
using Moq;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Enums;
using SmartInvest.Infrastructure.Data;
using SmartInvest.Infrastructure.Services;

namespace SmartInvest.Tests;

/// <summary>يغطي علم التعارض عند إضافة مشروع فرعي لمذكرة عرض جديدة/أخرى — الخلفية وراء التظليل
/// الأحمر/الأصفر في شاشة "مذكرة عرض جديدة"/"تعديل مذكرة عرض" بالواجهة الأمامية
/// (presentation-memos.html: .conflict-completed / .conflict-active).</summary>
public sealed class ProcurementConflictFlagsTests
{
    [Fact]
    public async Task SubProject_linked_to_completed_memo_is_flagged_with_its_title()
    {
        await using var context = CreateContext();
        var project = await SeedApprovedProjectAsync(context, "مشروع مغطى بمذكرة مكتملة");
        await LinkMemoAsync(context, project, title: "مذكرة عرض بناء", isCompleted: true, currentVersionNumber: 1);

        var items = await CreateService(context).GetSubProjectsAsync();
        var item = items.Single(i => i.SubProjectId == project.SubProjectId);

        Assert.True(item.HasCompletedMemo);
        Assert.Equal("مذكرة عرض بناء", item.CompletedMemoTitle);
        Assert.False(item.HasInProgressMemo);
    }

    /// <summary>مذكرة "جارية" = غير مكتملة ولها إصدار واحد على الأقل مرفوع بالفعل. مذكرة لم يُرفع
    /// لها أي إصدار (CurrentVersionNumber == 0) ليست جارية بعد — لا تُظهر أي تنبيه تعارض،
    /// طبقًا لما اعتمده المستخدم أثناء التصميم.</summary>
    [Fact]
    public async Task SubProject_linked_to_in_progress_memo_is_flagged_only_once_a_version_exists()
    {
        await using var context = CreateContext();
        var notStarted = await SeedApprovedProjectAsync(context, "مشروع بمذكرة لم تبدأ بعد");
        await LinkMemoAsync(context, notStarted, title: "مذكرة بلا إصدارات", isCompleted: false, currentVersionNumber: 0);

        var inProgress = await SeedApprovedProjectAsync(context, "مشروع بمذكرة جارية");
        await LinkMemoAsync(context, inProgress, title: "مذكرة رصف طرق", isCompleted: false, currentVersionNumber: 1);

        var items = await CreateService(context).GetSubProjectsAsync();

        var notStartedItem = items.Single(i => i.SubProjectId == notStarted.SubProjectId);
        Assert.False(notStartedItem.HasCompletedMemo);
        Assert.False(notStartedItem.HasInProgressMemo);

        var inProgressItem = items.Single(i => i.SubProjectId == inProgress.SubProjectId);
        Assert.False(inProgressItem.HasCompletedMemo);
        Assert.True(inProgressItem.HasInProgressMemo);
        Assert.Equal("مذكرة رصف طرق", inProgressItem.InProgressMemoTitle);
    }

    /// <summary>وضع التعديل: استبعاد المذكرة قيد التعديل نفسها من فحص التعارض حتى لا تُعتبر
    /// مشروعاتها متعارضة معها هي ذاتها — presentation-memos.ts.openEdit يمرر memo.id كـ excludeMemoId.</summary>
    [Fact]
    public async Task ExcludeMemoId_removes_the_conflict_flag_that_the_memo_itself_would_cause()
    {
        await using var context = CreateContext();
        var project = await SeedApprovedProjectAsync(context, "مشروع ضمن المذكرة قيد التعديل");
        var memo = await LinkMemoAsync(context, project, title: "مذكرة قيد التعديل", isCompleted: true, currentVersionNumber: 1);

        var withoutExclusion = await CreateService(context).GetSubProjectsAsync(excludeMemoId: null);
        Assert.True(withoutExclusion.Single(i => i.SubProjectId == project.SubProjectId).HasCompletedMemo);

        var withExclusion = await CreateService(context).GetSubProjectsAsync(excludeMemoId: memo.Id);
        var excludedItem = withExclusion.Single(i => i.SubProjectId == project.SubProjectId);
        Assert.False(excludedItem.HasCompletedMemo);
        Assert.False(excludedItem.HasInProgressMemo);
    }

    /// <summary>يغطي زر "مراحل الطرح" في شاشة الإدارة المالية: مذكرة من سنة مالية غير المعروضة حاليًا
    /// لا تُفعِّله — HasPresentationMemo يجب أن يعكس مذكرة السنة المطلوبة فقط، لا "أي مذكرة على الإطلاق".</summary>
    [Fact]
    public async Task HasPresentationMemo_reflects_only_the_requested_financial_year()
    {
        await using var context = CreateContext();
        var project = await SeedApprovedProjectAsync(context, "مشروع بمذكرة سنة سابقة فقط");
        var previousYear = new FinancialYear { Name = "سنة سابقة", StartDate = DateTime.UtcNow.AddYears(-2), EndDate = DateTime.UtcNow.AddYears(-1) };
        var currentYear = new FinancialYear { Name = "سنة حالية", StartDate = DateTime.UtcNow.AddYears(-1), EndDate = DateTime.UtcNow.AddYears(1) };
        context.FinancialYears.AddRange(previousYear, currentYear);
        await context.SaveChangesAsync();
        // المشروع نفسه (لا المذكرة فقط) يجب أن يكون مرتبطًا بالسنتين حتى تُدرجه GetSubProjectsAsync
        // عند تمرير financialYearId — هذا فلتر منفصل مسبق (SubProject.FinancialYears) قبل فحص المذكرة أصلًا.
        context.Set<SubProjectFinancialYear>().AddRange(
            new SubProjectFinancialYear { SubProjectId = project.SubProjectId, FinancialYearId = previousYear.FinancialYearId },
            new SubProjectFinancialYear { SubProjectId = project.SubProjectId, FinancialYearId = currentYear.FinancialYearId });
        await context.SaveChangesAsync();

        await LinkMemoAsync(context, project, title: "مذكرة السنة السابقة", isCompleted: true, currentVersionNumber: 1, financialYearId: previousYear.FinancialYearId);

        var itemsForPreviousYear = await CreateService(context).GetSubProjectsAsync(financialYearId: previousYear.FinancialYearId);
        var previousYearItem = itemsForPreviousYear.Single(i => i.SubProjectId == project.SubProjectId);
        Assert.True(previousYearItem.HasPresentationMemo);
        Assert.NotNull(previousYearItem.ActiveMemoId);

        var itemsForCurrentYear = await CreateService(context).GetSubProjectsAsync(financialYearId: currentYear.FinancialYearId);
        var currentYearItem = itemsForCurrentYear.Single(i => i.SubProjectId == project.SubProjectId);
        Assert.False(currentYearItem.HasPresentationMemo);
        Assert.Null(currentYearItem.ActiveMemoId);
        Assert.Null(currentYearItem.ActiveMemoTitle);
    }

    /// <summary>سجلات قديمة بلا سنة مالية مسجَّلة على المذكرة (FinancialYearId == null) تبقى تُفعِّل الزر
    /// أيًّا كانت السنة المعروضة — تفاديًا لتعطيل مشروعات حقيقية بأثر رجعي بسبب نقص بيانات تاريخي.</summary>
    [Fact]
    public async Task HasPresentationMemo_treats_legacy_memo_without_a_financial_year_as_always_matching()
    {
        await using var context = CreateContext();
        var project = await SeedApprovedProjectAsync(context, "مشروع بمذكرة قديمة بلا سنة");
        var year = new FinancialYear { Name = "سنة معروضة", StartDate = DateTime.UtcNow.AddYears(-1), EndDate = DateTime.UtcNow.AddYears(1) };
        context.FinancialYears.Add(year);
        await context.SaveChangesAsync();
        context.Set<SubProjectFinancialYear>().Add(
            new SubProjectFinancialYear { SubProjectId = project.SubProjectId, FinancialYearId = year.FinancialYearId });
        await context.SaveChangesAsync();

        await LinkMemoAsync(context, project, title: "مذكرة قديمة", isCompleted: true, currentVersionNumber: 1, financialYearId: null);

        var items = await CreateService(context).GetSubProjectsAsync(financialYearId: year.FinancialYearId);
        var item = items.Single(i => i.SubProjectId == project.SubProjectId);
        Assert.True(item.HasPresentationMemo);
        Assert.NotNull(item.ActiveMemoId);
    }

    private static ProcurementService CreateService(AppDbContext context) => new(
        context,
        Mock.Of<IExecutionStageService>(),
        new TestCurrentUser());

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<SubProject> SeedApprovedProjectAsync(AppDbContext context, string name)
    {
        var project = new SubProject
        {
            SubProjectName = name,
            IsApproved = true,
            MainProject = new MainProject { MainProjectName = "مشروع رئيسي" },
        };
        context.SubProjects.Add(project);
        await context.SaveChangesAsync();
        return project;
    }

    private static async Task<PresentationMemo> LinkMemoAsync(
        AppDbContext context,
        SubProject project,
        string title,
        bool isCompleted,
        int currentVersionNumber,
        int? financialYearId = null)
    {
        var memo = new PresentationMemo
        {
            Title = title,
            ContractingMethod = ContractingMethod.PublicTender,
            IsCompleted = isCompleted,
            CurrentVersionNumber = currentVersionNumber,
            FinancialYearId = financialYearId,
        };
        context.Set<PresentationMemo>().Add(memo);
        await context.SaveChangesAsync();

        context.Set<PresentationMemoSubProject>().Add(new PresentationMemoSubProject
        {
            PresentationMemoId = memo.Id,
            SubProjectId = project.SubProjectId,
        });
        await context.SaveChangesAsync();
        return memo;
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public string? UserId => "conflict-test-user";
        public string? Role => Roles.SuperAdmin;
    }
}
