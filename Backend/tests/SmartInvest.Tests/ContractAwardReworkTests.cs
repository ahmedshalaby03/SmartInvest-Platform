using Microsoft.EntityFrameworkCore;
using Moq;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Enums;
using SmartInvest.Infrastructure.Data;
using SmartInvest.Infrastructure.Services;

namespace SmartInvest.Tests;

public sealed class ContractAwardReworkTests
{
    [Fact]
    public async Task Cannot_start_procurement_stage_when_memo_is_attached_but_not_completed()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 100_000m, selfFunding: 0m);
        var memo = new PresentationMemo
        {
            Title = "مذكرة اختبار",
            ContractingMethod = ContractingMethod.PublicTender,
            IsCompleted = false,
        };
        context.Set<PresentationMemo>().Add(memo);
        await context.SaveChangesAsync();
        context.Set<PresentationMemoSubProject>().Add(new PresentationMemoSubProject
        {
            PresentationMemoId = memo.Id,
            SubProjectId = project.SubProjectId,
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UploadVersionAsync(project.SubProjectId, ProcurementStage.TenderDocument, new UploadProcurementVersionDto
            {
                Files = new Dictionary<string, FileUploadDto> { ["file"] = File("f.pdf") },
            }));
    }

    [Fact]
    public async Task Can_start_procurement_stage_when_memo_is_completed()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 100_000m, selfFunding: 0m);
        var memo = new PresentationMemo
        {
            Title = "مذكرة اختبار",
            ContractingMethod = ContractingMethod.PublicTender,
            IsCompleted = true,
        };
        context.Set<PresentationMemo>().Add(memo);
        await context.SaveChangesAsync();
        context.Set<PresentationMemoSubProject>().Add(new PresentationMemoSubProject
        {
            PresentationMemoId = memo.Id,
            SubProjectId = project.SubProjectId,
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await service.UploadVersionAsync(project.SubProjectId, ProcurementStage.TenderDocument, new UploadProcurementVersionDto
        {
            Files = new Dictionary<string, FileUploadDto> { ["file"] = File("f.pdf") },
        });

        Assert.Equal(1, await context.TenderDocuments.CountAsync());
    }

    /// <summary>مذكرة مكتملة لكن من سنة مالية سابقة (لا الحالية) لا تكفي لبدء مرحلة طرح جديدة —
    /// هذا هو الحارس الفعلي خلف تعطيل زر "مراحل الطرح" في الواجهة لمشروع بمذكرة قديمة فقط.</summary>
    [Fact]
    public async Task Cannot_start_procurement_stage_when_only_a_previous_year_memo_is_completed()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 100_000m, selfFunding: 0m);
        var previousYear = new FinancialYear
        {
            Name = "سنة سابقة",
            StartDate = DateTime.UtcNow.AddYears(-3),
            EndDate = DateTime.UtcNow.AddYears(-2),
        };
        context.FinancialYears.Add(previousYear);
        await context.SaveChangesAsync();

        var memo = new PresentationMemo
        {
            Title = "مذكرة سنة سابقة",
            ContractingMethod = ContractingMethod.PublicTender,
            IsCompleted = true,
            FinancialYearId = previousYear.FinancialYearId,
        };
        context.Set<PresentationMemo>().Add(memo);
        await context.SaveChangesAsync();
        context.Set<PresentationMemoSubProject>().Add(new PresentationMemoSubProject
        {
            PresentationMemoId = memo.Id,
            SubProjectId = project.SubProjectId,
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UploadVersionAsync(project.SubProjectId, ProcurementStage.TenderDocument, new UploadProcurementVersionDto
            {
                Files = new Dictionary<string, FileUploadDto> { ["file"] = File("f.pdf") },
            }));
        Assert.Contains("سنة المالية الحالية", ex.Message);
    }

    [Fact]
    public async Task Contract_value_above_planned_plus_overrun_blocks_completion()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 100_000m, selfFunding: 0m, projectNature: "توريدات", overrunPercentage: 10m);
        var (memo, contractor, contractType) = await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        await service.SetContractAwardDetailsAsync(project.SubProjectId, new SetContractAwardDetailsDto
        {
            ContractorId = contractor.ContractorId,
            ContractDate = DateTime.UtcNow.Date,
            ContractValue = 115_000m, // planned 100,000 + 10% = 110,000 ceiling — this exceeds it
            ExecutionDurationMonths = 1,
        });

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.SetCompletionAsync(project.SubProjectId, ProcurementStage.ContractAward, true));
        Assert.Contains("تتجاوز", ex.Message);
    }

    [Fact]
    public async Task Contract_value_within_overrun_ceiling_allows_completion_and_computes_savings()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 100_000m, selfFunding: 0m, projectNature: "توريدات", overrunPercentage: 10m);
        var (memo, contractor, contractType) = await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        await service.SetContractAwardDetailsAsync(project.SubProjectId, new SetContractAwardDetailsDto
        {
            ContractorId = contractor.ContractorId,
            ContractDate = DateTime.UtcNow.Date,
            ContractValue = 90_000m, // below planned 100,000 → 10,000 savings, no supply project needs handover fields
            ExecutionDurationMonths = 1,
        });

        await service.SetCompletionAsync(project.SubProjectId, ProcurementStage.ContractAward, true);

        var details = (await service.GetOverviewAsync(project.SubProjectId))
            .Stages.Single(s => s.Stage == "contract-award").ContractAward!;
        Assert.Equal(10_000m, details.Savings);
    }

    /// <summary>قيمة عقد مُدخَلة صراحةً كصفر لا تُنتج "وفرة" مزيَّفة تساوي كامل الميزانية المخططة — بخلاف
    /// قيمة عقد فارغة (تُحفَظ null بالفعل)، صفر قيمة صريحة يمر من التحقق في SetContractAwardDetailsAsync
    /// دون رفض لأن حد إكمال الترسية (قيمة العقد > 0) لا يُفعَّل إلا عند SetCompletionAsync.</summary>
    [Fact]
    public async Task Zero_contract_value_does_not_compute_fabricated_savings()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 100_000m, selfFunding: 0m, projectNature: "توريدات");
        var (memo, contractor, contractType) = await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        await service.SetContractAwardDetailsAsync(project.SubProjectId, new SetContractAwardDetailsDto
        {
            ContractorId = contractor.ContractorId,
            ContractDate = DateTime.UtcNow.Date,
            ContractValue = 0m,
            ExecutionDurationMonths = 1,
        });

        var details = (await service.GetOverviewAsync(project.SubProjectId))
            .Stages.Single(s => s.Stage == "contract-award").ContractAward!;
        Assert.Null(details.Savings);
    }

    [Fact]
    public async Task Supply_project_completes_without_handover_fields_and_auto_sets_handover_date()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 50_000m, selfFunding: 0m, projectNature: "توريدات");
        var (memo, contractor, contractType) = await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        await service.SetContractAwardDetailsAsync(project.SubProjectId, new SetContractAwardDetailsDto
        {
            ContractorId = contractor.ContractorId,
            ContractDate = DateTime.UtcNow.Date,
            ContractValue = 40_000m,
            ExecutionDurationMonths = 1,
            // deliberately no SiteHandoverMode/Date/Proof — must not be required for توريدات
        });

        await service.SetCompletionAsync(project.SubProjectId, ProcurementStage.ContractAward, true);

        var award = await context.ContractAwards.AsNoTracking().SingleAsync(x => x.SubProjectId == project.SubProjectId);
        Assert.NotNull(award.SiteHandoverDate);
        Assert.Null(award.SiteHandoverMode);
    }

    [Fact]
    public async Task Contracting_project_still_requires_site_handover_mode()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 50_000m, selfFunding: 0m, projectNature: "مقاولات");
        var (memo, contractor, _) = await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        await service.SetContractAwardDetailsAsync(project.SubProjectId, new SetContractAwardDetailsDto
        {
            ContractorId = contractor.ContractorId,
            ContractDate = DateTime.UtcNow.Date,
            ContractValue = 40_000m,
            ExecutionDurationMonths = 1,
            AdvancePaymentDone = true,
            AdvancePaymentPercentage = 10m,
            AdvancePaymentSelfAmount = 0m,
            AdvancePaymentBankAmount = 5_000m,
            // deliberately no SiteHandoverMode — مقاولات must still be blocked without it
        });

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.SetCompletionAsync(project.SubProjectId, ProcurementStage.ContractAward, true));
        Assert.Equal("يجب تحديد ما إذا كانت أرضية المشروع مُسلَّمة للمقاول أم لا", ex.Message);
    }

    [Fact]
    public async Task Contract_type_is_derived_from_memo_contracting_method_not_chosen_independently()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 50_000m, selfFunding: 0m, projectNature: "توريدات");
        var (memo, contractor, _) = await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        await service.SetContractAwardDetailsAsync(project.SubProjectId, new SetContractAwardDetailsDto
        {
            ContractorId = contractor.ContractorId,
            ContractDate = DateTime.UtcNow.Date,
            ContractValue = 10_000m,
            ExecutionDurationMonths = 1,
        });

        var assignment = await context.Set<ProjectAssignment>().AsNoTracking().SingleAsync(a => a.SubProjectId == project.SubProjectId);
        var createdType = await context.Set<ContractType>().AsNoTracking().SingleAsync(t => t.ContractTypeId == assignment.ContractTypeId);
        Assert.Equal(ContractingMethodLabels.ToLabel(memo.ContractingMethod), createdType.ContractName);

        // رقم العقد مُولَّد من AssignmentId، لا من أي إدخال
        Assert.Equal(assignment.AssignmentId.ToString(), assignment.ContractNumber);
    }

    [Fact]
    public async Task Advance_self_amount_above_planned_self_funding_is_blocked()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, projectNature: "مقاولات", selfFunding: 10_000m, bankFunding: 90_000m);
        await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        var dto = new SetContractAwardDetailsDto
        {
            AdvancePaymentDone = false,
            AdvancePaymentPercentage = 10m,
            AdvancePaymentSelfAmount = 10_000.01m,
            AdvancePaymentBankAmount = 0m,
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.SetContractAwardDetailsAsync(project.SubProjectId, dto));

        Assert.Contains("التمويل الذاتي", ex.Message);
    }

    [Fact]
    public async Task Advance_self_amount_equal_to_planned_self_funding_is_allowed()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, projectNature: "مقاولات", selfFunding: 10_000m, bankFunding: 90_000m);
        await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        var dto = new SetContractAwardDetailsDto
        {
            AdvancePaymentDone = false,
            AdvancePaymentPercentage = 10m,
            AdvancePaymentSelfAmount = 10_000m,
            AdvancePaymentBankAmount = 0m,
        };

        await service.SetContractAwardDetailsAsync(project.SubProjectId, dto);

        var saved = await context.ContractAwards.AsNoTracking().FirstAsync(x => x.SubProjectId == project.SubProjectId);
        Assert.Equal(10_000m, saved.AdvancePaymentSelfAmount);
    }

    [Fact]
    public async Task Advance_bank_amount_above_planned_bank_funding_is_blocked()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, projectNature: "مقاولات", selfFunding: 10_000m, bankFunding: 90_000m);
        await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        var dto = new SetContractAwardDetailsDto
        {
            AdvancePaymentDone = false,
            AdvancePaymentPercentage = 10m,
            AdvancePaymentSelfAmount = 0m,
            AdvancePaymentBankAmount = 90_000.01m,
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.SetContractAwardDetailsAsync(project.SubProjectId, dto));

        Assert.Contains("التمويل البنكي", ex.Message);
    }

    /// <summary>سقف التمويل البنكي المخطط للمشروع (funding.BankFunding) لا يكفي وحده — رقم خاص بهذا
    /// المشروع، بينما "المتاح" رصيد بنكي فعلي مشترك بين كل مشروعات السنة المالية. هنا المبلغ (5,000)
    /// يمر من سقف المشروع (90,000) لكنه يتجاوز المتاح الفعلي المستلم من البنك لهذه السنة (4,000 فقط).</summary>
    [Fact]
    public async Task Advance_bank_amount_within_planned_funding_but_above_available_balance_is_blocked()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(
            context, projectNature: "مقاولات", selfFunding: 10_000m, bankFunding: 90_000m, availableBankAmount: 4_000m);
        await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        var dto = new SetContractAwardDetailsDto
        {
            AdvancePaymentDone = false,
            AdvancePaymentPercentage = 10m,
            AdvancePaymentSelfAmount = 0m,
            AdvancePaymentBankAmount = 5_000m,
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.SetContractAwardDetailsAsync(project.SubProjectId, dto));

        Assert.Contains("المتاح", ex.Message);
    }

    [Fact]
    public async Task Advance_bank_amount_within_available_balance_is_accepted()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(
            context, projectNature: "مقاولات", selfFunding: 10_000m, bankFunding: 90_000m, availableBankAmount: 5_000m);
        await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        await service.SetContractAwardDetailsAsync(project.SubProjectId, new SetContractAwardDetailsDto
        {
            AdvancePaymentDone = false,
            AdvancePaymentPercentage = 10m,
            AdvancePaymentSelfAmount = 0m,
            AdvancePaymentBankAmount = 5_000m,
        });

        var saved = await context.ContractAwards.AsNoTracking().FirstAsync(x => x.SubProjectId == project.SubProjectId);
        Assert.Equal(5_000m, saved.AdvancePaymentBankAmount);
    }

    /// <summary>يثبّت الاستبعاد الذاتي: الترسية الحالية تستهلك "المتاح" بالكامل بالفعل (5,000 من 5,000) —
    /// إعادة حفظ نموذج الترسية بنفس مبلغ الدفعة المقدمة (مع تعديل حقل آخر فقط) يجب ألا تُرفض بدعوى أنها
    /// تستهلك المتاح مرتين.</summary>
    [Fact]
    public async Task Resaving_same_advance_bank_amount_does_not_double_count_against_available()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(
            context, projectNature: "مقاولات", selfFunding: 10_000m, bankFunding: 90_000m, availableBankAmount: 5_000m);
        var (memo, contractor, _) = await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        var dto = new SetContractAwardDetailsDto
        {
            ContractorId = contractor.ContractorId,
            ContractDate = DateTime.UtcNow.Date,
            // = الإجمالي المخطط (10,000 + 90,000) بالضبط، بلا وفرة — لو أقل، ProjectFundingPolicy
            // بتخصم الفرق من التمويل الذاتي أولًا (قاعدة منفصلة تمامًا عن "المتاح" اللي الاختبار ده
            // بيثبّته)، فكانت هتُسقط adjustedSelfFunding لصفر وترفض AdvancePaymentSelfAmount بلا علاقة
            // بموضوع الاختبار.
            ContractValue = 100_000m,
            ExecutionDurationMonths = 1,
            SiteHandoverMode = (int)SiteHandoverMode.Pending,
            AdvancePaymentDone = true,
            AdvancePaymentPercentage = 10m,
            AdvancePaymentSelfAmount = 1_000m,
            AdvancePaymentBankAmount = 5_000m, // يستهلك كل "المتاح" (5,000) بالضبط
        };
        await service.SetContractAwardDetailsAsync(project.SubProjectId, dto);

        // إعادة الحفظ بنفس المبلغ بالضبط (كما لو عدّل المستخدم حقلًا آخر وأعاد الحفظ) — يجب ألا تُرفض.
        dto.PenaltyAmount = 500m;
        await service.SetContractAwardDetailsAsync(project.SubProjectId, dto);

        var saved = await context.ContractAwards.AsNoTracking().FirstAsync(x => x.SubProjectId == project.SubProjectId);
        Assert.Equal(5_000m, saved.AdvancePaymentBankAmount);
        Assert.Equal(500m, saved.PenaltyAmount);
    }

    [Fact]
    public async Task Advance_payment_proof_slot_is_hidden_from_contract_award_file_slots()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 50_000m, selfFunding: 0m, projectNature: "مقاولات");
        await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        var overview = await service.GetOverviewAsync(project.SubProjectId);
        var awardStage = overview.Stages.Single(s => s.Stage == "contract-award");

        Assert.DoesNotContain(awardStage.FileSlots, s => s.Key == "advance-payment-proof");
        Assert.Contains(awardStage.FileSlots, s => s.Key == "award-order");
    }

    /// <summary>لا يثبّت هذا الاختبار شرط المرحلة في فلترة BuildStageDto على وجه التحديد — لو استُبدلت
    /// الفلترة بشرط أبسط (key != advance-payment-proof) بلا فحص المرحلة أصلًا، يبقى هذا الاختبار ناجحًا،
    /// لأن ما يثبّته فعليًا أضيق: أن مجموعة خانات ملفات مرحلة أخرى (technical-evaluation) لا تتأثر بفلترة
    /// مرحلة الترسية.</summary>
    [Fact]
    public async Task Other_stage_file_slots_are_unaffected_by_contract_award_filter()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 50_000m, selfFunding: 0m);
        var service = CreateService(context);

        var overview = await service.GetOverviewAsync(project.SubProjectId);
        var technicalStage = overview.Stages.Single(s => s.Stage == "technical-evaluation");

        Assert.Equal(3, technicalStage.FileSlots.Count);
        Assert.Contains(technicalStage.FileSlots, s => s.Key == "first-committee-report");
        Assert.Contains(technicalStage.FileSlots, s => s.Key == "second-committee-report");
        Assert.Contains(technicalStage.FileSlots, s => s.Key == "final-technical-evaluation-report");
    }

    /// <summary>يغطي C1: المسار الوحيد الآن لرفع إثبات صرف الدفعة المقدمة — تحديث الإصدار الحالي مباشرة
    /// عبر SetAdvancePaymentProofAsync، بلا إعادة رفع أمر الإسناد والعقد (كان uploadStageVersion يفشل دائمًا
    /// لأن UploadVersionAsync يُلزم كل خانة إلزامية بكل رفعة).</summary>
    [Fact]
    public async Task Uploading_advance_payment_proof_via_dedicated_endpoint_satisfies_completion_check()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 90_000m, selfFunding: 10_000m, projectNature: "مقاولات");
        var (memo, contractor, _) = await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        await service.SetContractAwardDetailsAsync(project.SubProjectId, new SetContractAwardDetailsDto
        {
            ContractorId = contractor.ContractorId,
            ContractDate = DateTime.UtcNow.Date,
            ContractValue = 60_000m,
            ExecutionDurationMonths = 1,
            SiteHandoverMode = (int)SiteHandoverMode.Pending,
            AdvancePaymentDone = true,
            AdvancePaymentPercentage = 10m,
            // فرق العقد عن الإجمالي المخطط (100,000-60,000=40,000) يستهلك التمويل الذاتي كاملًا
            // (10,000) عبر ProjectFundingPolicy، فلا يتبقى تمويل ذاتي متاح - الدفعة بالكامل من البنكي.
            AdvancePaymentSelfAmount = 0m,
            AdvancePaymentBankAmount = 6_000m, // 10% من قيمة العقد 60,000 = 6,000
        });

        // لم يُرفَع أي شيء بعد على advance-payment-proof — لولا هذا الاستدعاء تفشل SetCompletionAsync أدناه
        await service.SetAdvancePaymentProofAsync(project.SubProjectId, File("advance-proof.pdf"));

        await service.SetCompletionAsync(project.SubProjectId, ProcurementStage.ContractAward, true);

        var completed = (await service.GetOverviewAsync(project.SubProjectId))
            .Stages.Single(s => s.Stage == "contract-award").IsCompleted;
        Assert.True(completed);
    }

    [Fact]
    public async Task SetAdvancePaymentProofAsync_rejects_empty_file()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 50_000m, selfFunding: 0m, projectNature: "مقاولات");
        await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.SetAdvancePaymentProofAsync(project.SubProjectId, new FileUploadDto
            {
                FileName = "empty.pdf",
                FileExtension = ".pdf",
                FileSize = 0,
                Content = [],
            }));
        Assert.Contains("مطلوب", ex.Message);
    }

    [Fact]
    public async Task SetAdvancePaymentProofAsync_rejects_when_no_version_exists_yet()
    {
        await using var context = CreateContext();
        // مشروع بمرحلة ترسية مفتوحة لكن بلا أي إصدار مرفوع بعد (لا أمر إسناد ولا عقد)
        var project = await SeedProjectAsync(context, bankFunding: 50_000m, selfFunding: 0m, projectNature: "مقاولات");
        var memo = new PresentationMemo
        {
            Title = "مذكرة اختبار",
            ContractingMethod = ContractingMethod.PublicTender,
            IsCompleted = true,
        };
        context.Set<PresentationMemo>().Add(memo);
        await context.SaveChangesAsync();
        context.Set<PresentationMemoSubProject>().Add(new PresentationMemoSubProject
        {
            PresentationMemoId = memo.Id,
            SubProjectId = project.SubProjectId,
        });
        context.Set<FinancialEvaluation>().Add(new FinancialEvaluation
        {
            SubProjectId = project.SubProjectId,
            IsCompleted = true,
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.SetAdvancePaymentProofAsync(project.SubProjectId, File("advance-proof.pdf")));
        Assert.Contains("أمر الإسناد والعقد", ex.Message);
    }

    /// <summary>يغطي C2: الدفعة المقدمة عند الإكمال تُقاس على قيمة العقد لا totalCost (Bank+Self المخطط) —
    /// نفس الأساس الذي تعرضه الواجهة أثناء الإدخال (advanceAmount() في procurement-workflow.ts).</summary>
    [Fact]
    public async Task Advance_payment_matching_contract_value_base_allows_completion()
    {
        await using var context = CreateContext();
        // totalCost المخطط = 100,000 لكن قيمة العقد الفعلية 60,000 — لو الخادم لا يزال يحسب على totalCost
        // (10% × 100,000 = 10,000) سيرفض هذا الاختبار المبلغ الصحيح (10% × 60,000 = 6,000)
        var project = await SeedProjectAsync(context, bankFunding: 90_000m, selfFunding: 10_000m, projectNature: "مقاولات");
        var (memo, contractor, _) = await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        await service.SetContractAwardDetailsAsync(project.SubProjectId, new SetContractAwardDetailsDto
        {
            ContractorId = contractor.ContractorId,
            ContractDate = DateTime.UtcNow.Date,
            ContractValue = 60_000m,
            ExecutionDurationMonths = 1,
            SiteHandoverMode = (int)SiteHandoverMode.Pending,
            AdvancePaymentDone = true,
            AdvancePaymentPercentage = 10m,
            // فرق العقد عن الإجمالي المخطط (100,000-60,000=40,000) يستهلك التمويل الذاتي كاملًا
            // (10,000) عبر ProjectFundingPolicy، فلا يتبقى تمويل ذاتي متاح - الدفعة بالكامل من البنكي.
            AdvancePaymentSelfAmount = 0m,
            AdvancePaymentBankAmount = 6_000m,
        });
        await service.SetAdvancePaymentProofAsync(project.SubProjectId, File("advance-proof.pdf"));

        await service.SetCompletionAsync(project.SubProjectId, ProcurementStage.ContractAward, true);

        var completed = (await service.GetOverviewAsync(project.SubProjectId))
            .Stages.Single(s => s.Stage == "contract-award").IsCompleted;
        Assert.True(completed);
    }

    [Fact]
    public async Task Advance_payment_matching_old_total_cost_base_instead_of_contract_value_is_rejected()
    {
        await using var context = CreateContext();
        var project = await SeedProjectAsync(context, bankFunding: 90_000m, selfFunding: 10_000m, projectNature: "مقاولات");
        var (memo, contractor, _) = await SeedAwardPrereqsAsync(context, project.SubProjectId);
        var service = CreateService(context);

        // قيمة العقد 60,000 والنسبة 10% → الصحيح 6,000. هنا نصرف 10,000 عمدًا (10% من totalCost القديم 100,000)
        // فيجب الرفض، والرسالة يجب أن تقتبس 6,000.00 (قيمة العقد) لا 10,000.00 (totalCost القديم).
        await service.SetContractAwardDetailsAsync(project.SubProjectId, new SetContractAwardDetailsDto
        {
            ContractorId = contractor.ContractorId,
            ContractDate = DateTime.UtcNow.Date,
            ContractValue = 60_000m,
            ExecutionDurationMonths = 1,
            SiteHandoverMode = (int)SiteHandoverMode.Pending,
            AdvancePaymentDone = true,
            AdvancePaymentPercentage = 10m,
            // فرق العقد عن الإجمالي المخطط يستهلك التمويل الذاتي كاملًا هنا (ProjectFundingPolicy) -
            // كل الـ10,000 (الخاطئة عمدًا) من البنكي، فلا يصطدم بسقف التمويل الذاتي قبل الوصول
            // لفحص المجموع المقصود اختباره أدناه.
            AdvancePaymentSelfAmount = 0m,
            AdvancePaymentBankAmount = 10_000m,
        });
        await service.SetAdvancePaymentProofAsync(project.SubProjectId, File("advance-proof.pdf"));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.SetCompletionAsync(project.SubProjectId, ProcurementStage.ContractAward, true));
        Assert.Contains("6,000.00", ex.Message);
    }

    private static async Task<(PresentationMemo memo, Contractor contractor, ContractType? contractType)> SeedAwardPrereqsAsync(
        AppDbContext context, int subProjectId)
    {
        var memo = new PresentationMemo
        {
            Title = "مذكرة اختبار",
            ContractingMethod = ContractingMethod.PublicTender,
            IsCompleted = true,
        };
        context.Set<PresentationMemo>().Add(memo);
        var contractor = new Contractor { ContractorName = "مقاول اختبار", IsActive = true };
        context.Set<Contractor>().Add(contractor);
        await context.SaveChangesAsync();
        context.Set<PresentationMemoSubProject>().Add(new PresentationMemoSubProject
        {
            PresentationMemoId = memo.Id,
            SubProjectId = subProjectId,
        });
        var award = new ContractAward
        {
            SubProjectId = subProjectId,
            CurrentVersionNumber = 1,
        };
        context.Set<ContractAward>().Add(award);
        // المرحلة السابقة (التقييم المالي) يجب أن تكون مكتملة حتى تُفتح مرحلة الترسية للتعديل —
        // EnsurePreviousStageCompletedAsync تتحقق من هذا في كل من SetContractAwardDetailsAsync وSetCompletionAsync.
        context.Set<FinancialEvaluation>().Add(new FinancialEvaluation
        {
            SubProjectId = subProjectId,
            IsCompleted = true,
        });
        await context.SaveChangesAsync();

        // نسخة أولى بالملفين الإلزاميين (أمر الإسناد + العقد) — SetCompletionAsync يتحقق من وجودهما
        // في أحدث نسخة قبل استدعاء ValidateContractAwardForCompletionAsync، بنفس نمط AnnouncementVersion
        // في ProcurementDurationAndAuthorizationTests.
        context.Set<ContractAwardVersion>().Add(new ContractAwardVersion
        {
            ContractAwardId = award.Id,
            VersionNumber = 1,
            AwardOrder = StoredTestFile("award-order.pdf"),
            Contract = StoredTestFile("contract.pdf"),
        });
        await context.SaveChangesAsync();

        return (memo, contractor, null);
    }

    private static ProcurementService CreateService(AppDbContext context) => new(
        context,
        Mock.Of<IExecutionStageService>(),
        new TestCurrentUser());

    private static AppDbContext CreateContext() => new(
        new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // UploadAsync يفتح معاملة DB حقيقية (تعمل ضد SQL Server في الإنتاج) — مزوّد InMemory
            // يرفض دعم المعاملات عمدًا ويرمي استثناءً بدل تجاهلها بصمت، فنتجاهل هذا التحذير هنا فقط.
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    /// <summary>يربط المشروع بسنة مالية ويسجّل إتاحة بنكية سخية (10 مليون) لها — لا لاختبار "المتاح" نفسه
    /// (اختبارات ذلك في BankFundingSpendGate)، بل حتى تبقى اختبارات هذا الملف (المبنية على قيم دفعة مقدمة
    /// صغيرة نسبيًا) غير محظورة بالخطأ بعد إضافة تحقق "المتاح" في SetContractAwardDetailsAsync، الذي
    /// يتطلب أصلًا أن يكون المشروع مرتبطًا بسنة مالية ليحدد إليها ينسب الدفعة.</summary>
    private static async Task<SubProject> SeedProjectAsync(
        AppDbContext context, decimal bankFunding, decimal selfFunding,
        string projectNature = "توريدات", decimal? overrunPercentage = null, decimal availableBankAmount = 10_000_000m)
    {
        var project = new SubProject
        {
            SubProjectName = "اختبار الترسية",
            IsApproved = true,
            BankFunding = bankFunding,
            SelfFunding = selfFunding,
            ProjectNature = projectNature,
            OverrunPercentage = overrunPercentage,
        };
        context.SubProjects.Add(project);
        await context.SaveChangesAsync();

        var year = new FinancialYear
        {
            Name = "2026/2027",
            StartDate = DateTime.UtcNow.Date.AddYears(-1),
            EndDate = DateTime.UtcNow.Date.AddYears(1),
        };
        context.FinancialYears.Add(year);
        await context.SaveChangesAsync();
        context.Set<SubProjectFinancialYear>().Add(new SubProjectFinancialYear
        {
            SubProjectId = project.SubProjectId,
            FinancialYearId = year.FinancialYearId,
        });
        context.BankAvailabilities.Add(new BankAvailability
        {
            FinancialYearId = year.FinancialYearId,
            Amount = availableBankAmount,
            ReceivedDate = DateTime.UtcNow.Date,
        });
        await context.SaveChangesAsync();

        return project;
    }

    private static FileUploadDto File(string name) => new()
    {
        FileName = name,
        FileExtension = ".pdf",
        FileSize = 1,
        Content = [1],
    };

    private static StoredFile StoredTestFile(string name) => new()
    {
        FileName = name,
        FileExtension = ".pdf",
        FileSize = 1,
        Content = [1],
    };

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public string? UserId => "award-rework-test-user";
        public string? Role => Roles.SuperAdmin;
    }
}
