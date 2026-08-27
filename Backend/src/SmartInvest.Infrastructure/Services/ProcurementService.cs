using Microsoft.EntityFrameworkCore;
using SmartInvest.Application.Common;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Application.Services;
using SmartInvest.Domain.Common;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Enums;
using SmartInvest.Infrastructure.Data;
using System.Linq.Expressions;

namespace SmartInvest.Infrastructure.Services;

/// <summary>
/// تنفيذ دورة التعاقدات. يستخدم AppDbContext مباشرة (نفس نمط IdentityService)
/// لأن كل مرحلة تحتاج استعلامات مخصوصة على جدولين (المستند + إصداراته).
/// </summary>
public class ProcurementService : IProcurementService
{
    private readonly AppDbContext _context;
    private readonly IExecutionStageService _executionStageService;
    private readonly ICurrentUserService _currentUser;
    private readonly Dictionary<ProcurementStage, IStageOps> _stages;

    public ProcurementService(
        AppDbContext context,
        IExecutionStageService executionStageService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _executionStageService = executionStageService;
        _currentUser = currentUser;
        _stages = BuildStages(context);
    }

    // ------------------------------------------------------------------
    // الواجهة العامة
    // ------------------------------------------------------------------

    public async Task<IReadOnlyList<ProcurementSubProjectListItemDto>> GetSubProjectsAsync(int? financialYearId = null, int? excludeMemoId = null, CancellationToken cancellationToken = default)
    {
        var items = await _context.SubProjects.AsNoTracking()
            .Where(s => s.IsApproved
                && (financialYearId == null || s.FinancialYears.Any(y => y.FinancialYearId == financialYearId)))
            .OrderBy(s => s.SubProjectId)
            .Select(s => new ProcurementSubProjectListItemDto
            {
                SubProjectId = s.SubProjectId,
                SubProjectName = s.SubProjectName,
                SubProjectCode = s.SubProjectCode,
                MainProjectName = s.MainProject.MainProjectName,
                TotalStages = 6,
                CompletedStages =
                    (_context.TenderDocuments.Any(d => d.SubProjectId == s.SubProjectId && d.IsCompleted) ? 1 : 0) +
                    (_context.Announcements.Any(d => d.SubProjectId == s.SubProjectId && d.IsCompleted) ? 1 : 0) +
                    (_context.OpeningEnvelopes.Any(d => d.SubProjectId == s.SubProjectId && d.IsCompleted) ? 1 : 0) +
                    (_context.TechnicalEvaluations.Any(d => d.SubProjectId == s.SubProjectId && d.IsCompleted) ? 1 : 0) +
                    (_context.FinancialEvaluations.Any(d => d.SubProjectId == s.SubProjectId && d.IsCompleted) ? 1 : 0) +
                    (_context.ContractAwards.Any(d => d.SubProjectId == s.SubProjectId && d.IsCompleted) ? 1 : 0),
                // فحص أوّلي عام (أي مذكرة في أي سنة) — يُستخدم فقط لتحديد أي المشروعات يستحق استعلام
                // AttachActiveMemosAsync التفصيلي؛ القيمة النهائية المعروضة للواجهة تُستبدَل هناك بواحدة
                // مقيَّدة بالسنة المالية المطلوبة (financialYearId) — هي مصدر تفعيل زر "مراحل الطرح".
                HasPresentationMemo = _context.PresentationMemoSubProjects.Any(m => m.SubProjectId == s.SubProjectId),
            })
            .ToListAsync(cancellationToken);

        await AttachActiveMemosAsync(items, financialYearId, excludeMemoId, cancellationToken);
        return items;
    }

    /// <summary>
    /// يملأ بيانات المذكرة الفعّالة لكل مشروع، وتنبيهات التعارض (مكتملة/جارية)، عبر استعلام منفصل
    /// ثم دمج في الذاكرة. ضم جدول الروابط داخل الاستعلام الرئيسي يُضاعف الصفوف (cartesian join) —
    /// وهو ما جرى تفاديه سابقًا.
    /// </summary>
    /// <param name="financialYearId">السنة المالية المطلوبة (فلتر شاشة الإدارة المالية) — الفعّالة (Active*)
    /// وHasPresentationMemo النهائية تُقيَّدان بها: مذكرة من سنة أخرى لا تُفعِّل زر "مراحل الطرح" لهذه السنة.
    /// مذكرات قديمة بلا سنة مسجَّلة (FinancialYearId == null) تُعامَل كمطابقة لأي سنة — تفاديًا لتعطيل
    /// مشروعات حقيقية قديمة بأثر رجعي بسبب نقص بيانات تاريخي لا خطأ فيها.</param>
    /// <param name="excludeMemoId">مذكرة تُستبعد من فحص التعارض فقط (المذكرة قيد التعديل نفسها) —
    /// فحص التعارض (Completed/InProgress) يبقى بصرف النظر عن السنة المالية عمدًا، فهو تنبيه عام لا يخص شاشة بعينها.</param>
    private async Task AttachActiveMemosAsync(
        List<ProcurementSubProjectListItemDto> items,
        int? financialYearId,
        int? excludeMemoId,
        CancellationToken cancellationToken)
    {
        var subProjectIds = items.Where(i => i.HasPresentationMemo).Select(i => i.SubProjectId).ToList();
        if (subProjectIds.Count == 0)
        {
            return;
        }

        var links = await _context.PresentationMemoSubProjects.AsNoTracking()
            .Where(x => subProjectIds.Contains(x.SubProjectId))
            .Select(x => new
            {
                x.SubProjectId,
                MemoId = x.PresentationMemo.Id,
                x.PresentationMemo.Title,
                x.PresentationMemo.CreatedAt,
                x.PresentationMemo.ContractingMethod,
                x.PresentationMemo.IsCompleted,
                x.PresentationMemo.CurrentVersionNumber,
                x.PresentationMemo.FinancialYearId,
            })
            .ToListAsync(cancellationToken);

        // الفعّالة = الأحدث إنشاءً ضمن السنة المالية المطلوبة (أو أي سنة لو لم تُحدَّد سنة)، وعند التساوي
        // الأعلى Id — ترتيب حتمي لا يتذبذب بين الطلبات. هذه هي المعلومة المعروضة في شاشة الإدارة المالية
        // ومصدر تفعيل زر "مراحل الطرح"، فيجب أن تُقيَّد بالسنة حتى لا تُفعِّله مذكرة من سنة سابقة.
        var activeCandidates = financialYearId == null
            ? links
            : links.Where(x => x.FinancialYearId == financialYearId || x.FinancialYearId == null).ToList();

        var activeBySubProject = activeCandidates
            .GroupBy(x => x.SubProjectId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.MemoId).First());

        // فحص التعارض: يستبعد المذكرة قيد التعديل (إن وُجدت) حتى لا يُعتبر المشروع متعارضًا مع
        // المذكرة التي هو أصلًا جزء منها. بلا قيد سنة مالية عمدًا — تعارض حقيقي بصرف النظر عن سنته.
        var conflictLinks = excludeMemoId is int excluded
            ? links.Where(x => x.MemoId != excluded).ToList()
            : links;

        var completedBySubProject = conflictLinks
            .Where(x => x.IsCompleted)
            .GroupBy(x => x.SubProjectId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.MemoId).First());

        var inProgressBySubProject = conflictLinks
            .Where(x => !x.IsCompleted && x.CurrentVersionNumber > 0)
            .GroupBy(x => x.SubProjectId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.MemoId).First());

        foreach (var item in items)
        {
            if (completedBySubProject.TryGetValue(item.SubProjectId, out var completed))
            {
                item.HasCompletedMemo = true;
                item.CompletedMemoTitle = completed.Title;
            }

            if (inProgressBySubProject.TryGetValue(item.SubProjectId, out var inProgress))
            {
                item.HasInProgressMemo = true;
                item.InProgressMemoTitle = inProgress.Title;
            }

            if (!activeBySubProject.TryGetValue(item.SubProjectId, out var active))
            {
                // مذكرة (مذكرات) المشروع كلها من سنوات مالية أخرى — لا تُفعِّل زر "مراحل الطرح" لهذه السنة.
                item.HasPresentationMemo = false;
                continue;
            }

            item.HasPresentationMemo = true;
            item.ActiveMemoId = active.MemoId;
            item.ActiveMemoTitle = active.Title;
            item.ContractingMethod = (int?)active.ContractingMethod;
            item.ContractingMethodLabel = ContractingMethodLabels.ToLabel(active.ContractingMethod);
        }
    }

    public async Task<ProcurementOverviewDto> GetOverviewAsync(int subProjectId, CancellationToken cancellationToken = default)
    {
        var sub = await _context.SubProjects.AsNoTracking()
            .Where(s => s.SubProjectId == subProjectId)
            .Select(s => new { s.SubProjectName, s.SubProjectCode, s.IsApproved })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"المشروع الفرعي رقم {subProjectId} غير موجود");

        if (!sub.IsApproved)
        {
            throw new BusinessRuleException("لا يمكن بدء مراحل الطرح قبل اعتماد المشروع الفرعي");
        }

        // الفعّالة فقط: الأحدث إنشاءً، وعند التساوي الأعلى Id.
        // نجلب نوع التعاقد كرقم خام لأن تحويله لتسمية عربية بحث في قاموس — لا يُترجَم إلى SQL.
        var activeMemoRaw = await _context.PresentationMemoSubProjects.AsNoTracking()
            .Where(x => x.SubProjectId == subProjectId)
            .OrderByDescending(x => x.PresentationMemo.CreatedAt)
            .ThenByDescending(x => x.PresentationMemo.Id)
            .Select(x => new
            {
                x.PresentationMemo.Id,
                x.PresentationMemo.Title,
                x.PresentationMemo.CurrentVersionNumber,
                x.PresentationMemo.IsCompleted,
                x.PresentationMemo.ContractingMethod,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var overview = new ProcurementOverviewDto
        {
            SubProjectId = subProjectId,
            SubProjectName = sub.SubProjectName,
            SubProjectCode = sub.SubProjectCode,
            ActivePresentationMemo = activeMemoRaw == null ? null : new PresentationMemoBriefDto
            {
                Id = activeMemoRaw.Id,
                Title = activeMemoRaw.Title,
                CurrentVersionNumber = activeMemoRaw.CurrentVersionNumber,
                IsCompleted = activeMemoRaw.IsCompleted,
                ContractingMethodLabel = ContractingMethodLabels.ToLabel(activeMemoRaw.ContractingMethod),
            },
        };

        bool? previousCompleted = true;
        foreach (var (stage, ops) in _stages.OrderBy(x => (int)x.Key))
        {
            var state = await ops.FindDocAsync(subProjectId, cancellationToken);
            var stageDto = BuildStageDto<ProcurementStageDto>(stage, ops, state);
            stageDto.IsLocked = previousCompleted != true;
            if (stage == ProcurementStage.ContractAward)
            {
                stageDto.AdvancePaymentDone = await GetAdvancePaymentDoneAsync(subProjectId, cancellationToken);
                stageDto.ContractAward = await GetContractAwardDetailsAsync(subProjectId, cancellationToken);
            }
            else if (stage == ProcurementStage.Announcement)
            {
                await ApplyAnnouncementOverridesAsync(stageDto, subProjectId, cancellationToken);
            }
            overview.Stages.Add(stageDto);
            previousCompleted = state?.IsCompleted ?? false;
        }

        return overview;
    }

    public async Task<ProcurementStageDetailDto> GetStageAsync(int subProjectId, ProcurementStage stage, CancellationToken cancellationToken = default)
    {
        await EnsureSubProjectExistsAsync(subProjectId, cancellationToken);

        var ops = _stages[stage];
        var state = await ops.FindDocAsync(subProjectId, cancellationToken);
        var dto = BuildStageDto<ProcurementStageDetailDto>(stage, ops, state);
        dto.IsLocked = !await IsPreviousStageCompletedAsync(stage, subProjectId, cancellationToken);

        if (stage == ProcurementStage.ContractAward)
        {
            dto.AdvancePaymentDone = await GetAdvancePaymentDoneAsync(subProjectId, cancellationToken);
            dto.ContractAward = await GetContractAwardDetailsAsync(subProjectId, cancellationToken);
        }
        else if (stage == ProcurementStage.Announcement)
        {
            await ApplyAnnouncementOverridesAsync(dto, subProjectId, cancellationToken);
        }

        if (state != null)
        {
            dto.Versions = await ops.GetVersionDtosAsync(state.Id, cancellationToken);
        }

        return dto;
    }

    public async Task<ProcurementVersionDto> UploadVersionAsync(int subProjectId, ProcurementStage stage, UploadProcurementVersionDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureSubProjectExistsAsync(subProjectId, cancellationToken);
        await EnsureHasPresentationMemoAsync(subProjectId, cancellationToken);
        await EnsurePreviousStageCompletedAsync(stage, subProjectId, cancellationToken);

        var ops = _stages[stage];

        if (dto.Files.Count == 0)
        {
            throw new BusinessRuleException("يجب رفع ملف واحد على الأقل");
        }

        var knownKeys = ops.Slots.Select(s => s.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = dto.Files.Keys.FirstOrDefault(k => !knownKeys.Contains(k));
        if (unknown != null)
        {
            throw new BusinessRuleException($"نوع الملف '{unknown}' غير معروف لهذه المرحلة");
        }

        foreach (var slot in ops.Slots.Where(s => s.Required))
        {
            if (!dto.Files.Keys.Contains(slot.Key, StringComparer.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException($"ملف \"{slot.Label}\" مطلوب");
            }
        }

        var files = dto.Files.ToDictionary(
            x => x.Key,
            x => new StoredFile
            {
                FileName = x.Value.FileName,
                FileExtension = x.Value.FileExtension,
                FileSize = x.Value.FileSize,
                Content = x.Value.Content,
            },
            StringComparer.OrdinalIgnoreCase);

        // أول نشاط كتابي موثوق للمرحلة الأولى (أو لسجل قديم بلا مستند) يفعّل المؤقت مرة واحدة.
        // لا يتم ذلك في GET، لذلك لا يغيّر Refresh موعد النهاية.
        if (await ops.FindDocAsync(subProjectId, cancellationToken) == null)
        {
            await ActivateStageAsync(subProjectId, stage, DateTime.UtcNow, cancellationToken);
        }

        return await ops.UploadAsync(subProjectId, files, dto.Notes, cancellationToken);
    }

    public async Task<FileDownloadDto> DownloadFileAsync(int subProjectId, ProcurementStage stage, int versionNumber, string fileKey, CancellationToken cancellationToken = default)
    {
        var ops = _stages[stage];
        var file = await ops.GetFileAsync(subProjectId, versionNumber, fileKey, cancellationToken)
            ?? throw new NotFoundException("الملف المطلوب غير موجود");

        return new FileDownloadDto
        {
            FileName = file.FileName,
            FileExtension = file.FileExtension,
            Content = file.Content,
        };
    }

    public async Task SetCompletionAsync(int subProjectId, ProcurementStage stage, bool isCompleted, CancellationToken cancellationToken = default)
    {
        await EnsureSubProjectExistsAsync(subProjectId, cancellationToken);

        if (isCompleted)
        {
            await EnsurePreviousStageCompletedAsync(stage, subProjectId, cancellationToken);
            var wasAlreadyCompleted = (await _stages[stage].FindDocAsync(subProjectId, cancellationToken))?.IsCompleted == true;
            await _stages[stage].SetCompletionAsync(subProjectId, true, cancellationToken);

            if (stage == ProcurementStage.ContractAward)
            {
                await AutoSetSupplyHandoverDateAsync(subProjectId, cancellationToken);
                await _executionStageService.SyncFinalDeliveryStageAsync(subProjectId, cancellationToken);
                await _executionStageService.SyncAdvancePaymentStageAsync(subProjectId, cancellationToken);
            }
            else if (!wasAlreadyCompleted)
            {
                var nextStage = (ProcurementStage)((int)stage + 1);
                await ActivateStageAsync(subProjectId, nextStage, DateTime.UtcNow, cancellationToken);
            }

            return;
        }

        // إعادة فتح مرحلة تُبطل كل ما بعدها فعليًا — ترجع "لم تكتمل" في القاعدة نفسها،
        // مش بس تظهر مقفولة في الواجهة، عشان لو المرحلة دي اتقفلت تاني تفضل كل المراحل التالية بانتظار إعادة اعتمادها من جديد.
        await _stages[stage].SetCompletionAsync(subProjectId, false, cancellationToken);

        foreach (var laterStage in _stages.Keys.Where(s => (int)s > (int)stage).OrderBy(s => (int)s))
        {
            await _stages[laterStage].ResetIfCompletedAsync(subProjectId, cancellationToken);
        }
    }

    /// <summary>
    /// توريدات لا تسليم أرضية لها — العدّاد يبدأ فور اكتمال الترسية. نعيد استخدام SiteHandoverDate
    /// نفسه كنقطة بداية بدل اختراع آلية موازية، حتى يستمر SyncFinalDeliveryStageAsync يعمل بلا تغيير
    /// لكِلا نوعي المشروع.
    /// </summary>
    private async Task AutoSetSupplyHandoverDateAsync(int subProjectId, CancellationToken cancellationToken)
    {
        var projectNature = await _context.SubProjects.AsNoTracking()
            .Where(s => s.SubProjectId == subProjectId)
            .Select(s => s.ProjectNature)
            .FirstOrDefaultAsync(cancellationToken);

        if (IsContractingProject(projectNature))
        {
            return;
        }

        var doc = await _context.ContractAwards
            .FirstOrDefaultAsync(x => x.SubProjectId == subProjectId, cancellationToken);

        if (doc != null && doc.SiteHandoverDate == null)
        {
            doc.SiteHandoverDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SetAdvancePaymentDoneAsync(int subProjectId, bool done, CancellationToken cancellationToken = default)
    {
        var doc = await GetEditableContractAwardAsync(subProjectId, cancellationToken);
        doc.AdvancePaymentDone = done;
        if (done)
        {
            doc.AdvancePaymentDate ??= DateTime.UtcNow.Date;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _executionStageService.SyncAdvancePaymentStageAsync(subProjectId, cancellationToken);
    }

    public async Task SetContractAwardDetailsAsync(int subProjectId, SetContractAwardDetailsDto dto, CancellationToken cancellationToken = default)
    {
        var doc = await GetEditableContractAwardAsync(subProjectId, cancellationToken);

        var funding = await _context.SubProjects.AsNoTracking()
            .Where(x => x.SubProjectId == subProjectId)
            .Select(x => new { x.SelfFunding, x.BankFunding })
            .FirstAsync(cancellationToken);

        // إن كانت قيمة العقد المُدخلة الآن أقل من الإجمالي المخطط، يُخصم الفرق أولًا من التمويل
        // الذاتي ثم البنكي (ProjectFundingPolicy) قبل التحقق من سقف الدفعة المقدمة لكل مصدر -
        // نفس القاعدة المستخدَمة لحساب "المتبقي" في متابعة المشروعات، مصدر واحد لا يتكرر.
        var (adjustedSelfFunding, adjustedBankFunding) = ProjectFundingPolicy.ApplyContractSavings(
            funding.SelfFunding, funding.BankFunding, funding.SelfFunding + funding.BankFunding, dto.ContractValue);

        if (dto.AdvancePaymentSelfAmount > adjustedSelfFunding)
        {
            throw new BusinessRuleException(
                $"الجزء المصروف من التمويل الذاتي ({dto.AdvancePaymentSelfAmount:N2} ج.م) يتجاوز التمويل الذاتي المتاح للمشروع ({adjustedSelfFunding:N2} ج.م)");
        }
        if (dto.AdvancePaymentBankAmount > adjustedBankFunding)
        {
            throw new BusinessRuleException(
                $"الجزء المصروف من التمويل البنكي ({dto.AdvancePaymentBankAmount:N2} ج.م) يتجاوز التمويل البنكي المتاح للمشروع ({adjustedBankFunding:N2} ج.م)");
        }

        doc.AdvancePaymentDone = dto.AdvancePaymentDone;
        doc.AdvancePaymentPercentage = dto.AdvancePaymentPercentage;
        doc.AdvancePaymentSelfAmount = dto.AdvancePaymentSelfAmount;
        doc.AdvancePaymentBankAmount = dto.AdvancePaymentBankAmount;
        // تاريخ الصرف يُملأ من الشاشة، وإن تُرك فارغًا مع تأكيد الصرف نسجّل تاريخ اليوم
        // حتى لا تظهر مرحلة الدفعة المقدمة في المتابعة بلا موعد.
        doc.AdvancePaymentDate = dto.AdvancePaymentDate
            ?? (dto.AdvancePaymentDone ? doc.AdvancePaymentDate ?? DateTime.UtcNow.Date : doc.AdvancePaymentDate);
        doc.ExecutionDurationMonths = dto.ExecutionDurationMonths;
        doc.ExecutionDurationDays = dto.ExecutionDurationDays;
        doc.SiteHandoverMode = dto.SiteHandoverMode is int mode ? (SiteHandoverMode)mode : null;
        doc.PenaltyAmount = dto.PenaltyAmount;

        // الإسناد نفسه يعيش في ProjectAssignment — مصدر حقيقة واحد لهوية المقاول،
        // تقرأ منه متابعة المشروعات وملف المقاول.
        if (dto.ContractorId is int contractorId)
        {
            await UpsertAssignmentAsync(doc, contractorId, dto, cancellationToken);
        }

        // الشرطان أعلاه (self/bank مقابل تمويل المشروع المخطط) لا يكفيان وحدهما — رقمان خاصان
        // بهذا المشروع، بينما "المتاح" رصيد بنكي فعلي مشترك بين كل مشروعات السنة المالية. يُتحقق منه
        // هنا (بعد استقرار doc.ProjectAssignmentId) حتى يُحسب تاريخ الترسية الصحيح لتحديد السنة
        // المالية المالكة لهذه الدفعة — نفس قاعدة BankSpendCalculator.GetAdvancePaymentsSpentAsync تمامًا.
        if (dto.AdvancePaymentBankAmount is decimal bankAmount and > 0m)
        {
            var owningYearId = await BankSpendCalculator.ResolveAdvancePaymentFinancialYearIdAsync(
                _context, subProjectId, doc.ProjectAssignmentId, cancellationToken);
            var availableBank = await BankSpendCalculator.GetTotalAvailableAsync(
                _context, owningYearId, cancellationToken, excludeContractAwardId: doc.Id);
            if (bankAmount > availableBank)
            {
                throw new BusinessRuleException(
                    $"الجزء المصروف من التمويل البنكي في الدفعة المقدمة ({bankAmount:N2} ج.م) يتجاوز المتاح الفعلي من البنك لهذه السنة المالية ({availableBank:N2} ج.م)");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _executionStageService.SyncAdvancePaymentStageAsync(subProjectId, cancellationToken);
    }

    public async Task SetSiteHandoverAsync(int subProjectId, DateTime handoverDate, FileUploadDto proofFile, CancellationToken cancellationToken = default)
    {
        await EnsureSubProjectExistsAsync(subProjectId, cancellationToken);

        var doc = await _context.ContractAwards
            .FirstOrDefaultAsync(x => x.SubProjectId == subProjectId, cancellationToken)
            ?? throw new NotFoundException("لم تبدأ مرحلة الترسية بعد");

        if (doc.SiteHandoverMode == null)
        {
            throw new BusinessRuleException("يجب تحديد حالة أرضية المشروع في بيانات الترسية أولاً");
        }

        // «لم تُسلَّم بعد» تعني أن التسليم يحدث بعد الترسية — تسجيله قبل اكتمالها بلا معنى.
        // أما «مُسلَّمة وقت الترسية» فتُسجَّل أثناء المرحلة السادسة نفسها قبل إكمالها.
        if (doc.SiteHandoverMode == SiteHandoverMode.Pending && !doc.IsCompleted)
        {
            throw new BusinessRuleException("لا يمكن تسجيل تسليم الأرضية قبل إكمال الترسية");
        }

        if (proofFile == null || proofFile.Content.Length == 0)
        {
            throw new BusinessRuleException("إثبات تسليم الأرضية مطلوب");
        }

        doc.SiteHandoverDate = handoverDate;
        doc.SiteHandoverProofFile = new StoredFile
        {
            FileName = proofFile.FileName,
            FileExtension = proofFile.FileExtension,
            FileSize = proofFile.FileSize,
            Content = proofFile.Content,
        };

        await _context.SaveChangesAsync(cancellationToken);

        // الموعد النهائي يُحسب من تاريخ التسليم، فأي تغيير هنا يجب أن ينعكس على مرحلة التسليم النهائي
        await _executionStageService.SyncFinalDeliveryStageAsync(subProjectId, cancellationToken);
    }

    /// <summary>
    /// يحدّث إثبات صرف الدفعة المقدمة على الإصدار الحالي مباشرة — لا يمر بـ UploadVersionAsync
    /// (الذي يُلزم كل خانة إلزامية بكل رفعة، أي أمر الإسناد والعقد أيضًا). يماثل SetSiteHandoverAsync
    /// في أنه يُعدِّل مستندًا موجودًا بدل تعدين إصدار جديد.
    /// </summary>
    public async Task SetAdvancePaymentProofAsync(int subProjectId, FileUploadDto proofFile, CancellationToken cancellationToken = default)
    {
        if (proofFile == null || proofFile.Content.Length == 0)
        {
            throw new BusinessRuleException("إثبات صرف الدفعة المقدمة مطلوب");
        }

        var doc = await GetEditableContractAwardAsync(subProjectId, cancellationToken);

        if (doc.CurrentVersionNumber == 0)
        {
            throw new BusinessRuleException("يجب رفع ملفات المرحلة (أمر الإسناد والعقد) أولاً قبل رفع إثبات صرف الدفعة المقدمة");
        }

        var version = await _context.ContractAwardVersions
            .FirstOrDefaultAsync(v => v.ContractAwardId == doc.Id && v.VersionNumber == doc.CurrentVersionNumber, cancellationToken)
            ?? throw new BusinessRuleException("يجب رفع ملفات المرحلة (أمر الإسناد والعقد) أولاً قبل رفع إثبات صرف الدفعة المقدمة");

        version.AdvancePaymentProof = new StoredFile
        {
            FileName = proofFile.FileName,
            FileExtension = proofFile.FileExtension,
            FileSize = proofFile.FileSize,
            Content = proofFile.Content,
        };

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FileDownloadDto> DownloadSiteHandoverProofAsync(int subProjectId, CancellationToken cancellationToken = default)
    {
        var doc = await _context.ContractAwards.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SubProjectId == subProjectId, cancellationToken)
            ?? throw new NotFoundException("لم تبدأ مرحلة الترسية بعد");

        var file = doc.SiteHandoverProofFile
            ?? throw new NotFoundException("لم يُرفع إثبات تسليم الأرضية بعد");

        return new FileDownloadDto
        {
            FileName = file.FileName,
            FileExtension = file.FileExtension,
            Content = file.Content,
        };
    }

    public async Task SetStageDurationAsync(int subProjectId, ProcurementStage stage, int? durationDays, CancellationToken cancellationToken = default)
    {
        if (stage == ProcurementStage.Announcement)
        {
            // للإعلان قاعدة ثابتة (15 يومًا من AnnouncementDate) لا تقدير فيها لمدير التخطيط —
            // راجع SetAnnouncementDateAsync بدلًا من هذه الدالة لهذه المرحلة تحديدًا.
            throw new BusinessRuleException("مرحلة الإعلان لها قاعدة موعد ثابتة (15 يومًا من تاريخ الإعلان) ولا تقبل تحديد مدة يدويًا");
        }

        if (stage == ProcurementStage.ContractAward)
        {
            throw new BusinessRuleException("مرحلة العقد والترسية لا تقبل مدة مرحلة عامة؛ استخدم مدة تنفيذ العقد بالشهور والأيام");
        }

        await EnsureSubProjectExistsAsync(subProjectId, cancellationToken);
        await EnsurePreviousStageCompletedAsync(stage, subProjectId, cancellationToken);
        var previous = await _stages[stage].FindDocAsync(subProjectId, cancellationToken);
        if (previous?.IsCompleted == true)
        {
            throw new BusinessRuleException("هذه المرحلة مكتملة — يجب إعادة فتحها قبل تعديل المدة القصوى");
        }

        var effectiveDuration = durationDays ?? DefaultStageDurationDays;
        await _stages[stage].SetDurationAsync(subProjectId, effectiveDuration, cancellationToken);

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "ProcurementStageDuration",
            EntityId = subProjectId,
            FieldName = ProcurementStageKeys.ToKey(stage),
            OldValue = previous?.DurationDays?.ToString(),
            NewValue = effectiveDuration.ToString(),
            ChangedByUserId = _currentUser.UserId
                ?? throw new UnauthorizedAccessException("تعذر تحديد المستخدم الذي عدّل مدة مرحلة الطرح"),
            ChangedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ActivateStageAsync(
        int subProjectId,
        ProcurementStage stage,
        DateTime activatedAt,
        CancellationToken cancellationToken)
    {
        if (stage == ProcurementStage.ContractAward)
        {
            return;
        }

        if (stage == ProcurementStage.Announcement)
        {
            await _stages[stage].ActivateAsync(
                subProjectId,
                AnnouncementMinimumDays,
                durationSetAt: null,
                cancellationToken);
            return;
        }

        await _stages[stage].ActivateAsync(
            subProjectId,
            DefaultStageDurationDays,
            activatedAt,
            cancellationToken);
    }

    public async Task SetAnnouncementDateAsync(int subProjectId, DateTime announcementDate, CancellationToken cancellationToken = default)
    {
        await EnsureSubProjectExistsAsync(subProjectId, cancellationToken);

        var doc = await _context.Announcements
            .FirstOrDefaultAsync(x => x.SubProjectId == subProjectId, cancellationToken);

        if (doc == null)
        {
            doc = new Announcement { SubProjectId = subProjectId };
            _context.Announcements.Add(doc);
        }
        else if (doc.IsCompleted)
        {
            throw new BusinessRuleException("هذه المرحلة مكتملة — يجب إعادة فتحها قبل تعديل تاريخ الإعلان");
        }

        doc.AnnouncementDate = announcementDate;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SkipStageAsync(int subProjectId, ProcurementStage stage, string reason, CancellationToken cancellationToken = default)
    {
        await EnsureSubProjectExistsAsync(subProjectId, cancellationToken);
        await EnsurePreviousStageCompletedAsync(stage, subProjectId, cancellationToken);
        await _stages[stage].SkipAsync(subProjectId, reason, cancellationToken);

        if (stage != ProcurementStage.ContractAward)
        {
            await ActivateStageAsync(
                subProjectId,
                (ProcurementStage)((int)stage + 1),
                DateTime.UtcNow,
                cancellationToken);
        }
    }

    public async Task FailStageAsync(int subProjectId, ProcurementStage stage, string reason, CancellationToken cancellationToken = default)
    {
        await EnsureSubProjectExistsAsync(subProjectId, cancellationToken);
        await _stages[stage].FailAsync(subProjectId, reason, cancellationToken);

        // فشل مرحلة يُبطل كل ما بعدها فعليًا — بنفس منطق إعادة الفتح تمامًا.
        foreach (var laterStage in _stages.Keys.Where(s => (int)s > (int)stage).OrderBy(s => (int)s))
        {
            await _stages[laterStage].ResetIfCompletedAsync(subProjectId, cancellationToken);
        }
    }

    private async Task<ContractAward> GetEditableContractAwardAsync(int subProjectId, CancellationToken cancellationToken)
    {
        await EnsureSubProjectExistsAsync(subProjectId, cancellationToken);
        await EnsurePreviousStageCompletedAsync(ProcurementStage.ContractAward, subProjectId, cancellationToken);

        var doc = await _context.ContractAwards
            .FirstOrDefaultAsync(x => x.SubProjectId == subProjectId, cancellationToken);

        if (doc == null)
        {
            doc = new ContractAward { SubProjectId = subProjectId };
            _context.ContractAwards.Add(doc);
        }
        else if (doc.IsCompleted)
        {
            throw new BusinessRuleException("هذه المرحلة مكتملة — يجب إعادة فتحها قبل التعديل");
        }

        return doc;
    }

    /// <summary>ينشئ الإسناد أو يحدّثه — إعادة فتح الترسية ثم إكمالها لا تُنشئ إسنادًا مكررًا.</summary>
    private async Task UpsertAssignmentAsync(ContractAward doc, int contractorId, SetContractAwardDetailsDto dto, CancellationToken cancellationToken)
    {
        var contractorExists = await _context.Set<Contractor>().AsNoTracking()
            .AnyAsync(c => c.ContractorId == contractorId, cancellationToken);
        if (!contractorExists)
        {
            throw new NotFoundException($"المقاول رقم {contractorId} غير موجود");
        }

        var contractTypeId = await ResolveContractTypeIdFromMemoAsync(doc.SubProjectId, cancellationToken);

        var assignment = doc.ProjectAssignmentId is int existingId
            ? await _context.Set<ProjectAssignment>().FirstOrDefaultAsync(a => a.AssignmentId == existingId, cancellationToken)
            : null;

        var isNew = assignment == null;
        if (assignment == null)
        {
            assignment = new ProjectAssignment
            {
                SubProjectId = doc.SubProjectId,
                AssignmentDate = DateTime.UtcNow,
                ExpectedStartDate = DateTime.UtcNow,
                ExpectedEndDate = DateTime.UtcNow,
            };
            _context.Set<ProjectAssignment>().Add(assignment);
        }

        assignment.ContractorId = contractorId;
        assignment.ContractTypeId = contractTypeId;
        assignment.ContractDate = dto.ContractDate;
        assignment.ContractValue = dto.ContractValue;

        // رقم العقد رقم تعريفي مُولَّد من AssignmentId — لا يُدخله أحد يدويًا ولا يظهر في أي واجهة.
        // يُضبط مرة واحدة فقط عند أول إنشاء، لا يُعاد توليده عند كل تعديل لاحق.
        if (isNew)
        {
            await _context.SaveChangesAsync(cancellationToken);
            assignment.ContractNumber = assignment.AssignmentId.ToString();
        }

        await _context.SaveChangesAsync(cancellationToken);
        doc.ProjectAssignmentId = assignment.AssignmentId;
    }

    /// <summary>
    /// نوع العقد يُشتق من طريقة التعاقد في مذكرة العرض الفعّالة للمشروع، لا يُختار مستقلًا —
    /// إنشاء-أو-إيجاد صف ContractType مطابق بالاسم، بنفس نمط إنشاء القوائم المرجعية الناقصة
    /// المستخدَم في مسار استيراد Excel (بدل الرجوع لقيمة "غير محدد").
    /// </summary>
    private async Task<int> ResolveContractTypeIdFromMemoAsync(int subProjectId, CancellationToken cancellationToken)
    {
        var contractingMethod = await _context.PresentationMemoSubProjects.AsNoTracking()
            .Where(x => x.SubProjectId == subProjectId)
            .OrderByDescending(x => x.PresentationMemo.CreatedAt)
            .ThenByDescending(x => x.PresentationMemo.Id)
            .Select(x => x.PresentationMemo.ContractingMethod)
            .FirstOrDefaultAsync(cancellationToken);

        var label = ContractingMethodLabels.ToLabel(contractingMethod)
            ?? throw new BusinessRuleException("لا يمكن تحديد نوع العقد قبل استكمال طريقة التعاقد في مذكرة العرض");

        var existing = await _context.Set<ContractType>()
            .FirstOrDefaultAsync(t => t.ContractName == label, cancellationToken);

        if (existing != null)
        {
            return existing.ContractTypeId;
        }

        var created = new ContractType { ContractName = label };
        _context.Set<ContractType>().Add(created);
        await _context.SaveChangesAsync(cancellationToken);
        return created.ContractTypeId;
    }

    // ------------------------------------------------------------------
    // مساعدات
    // ------------------------------------------------------------------

    private async Task EnsureSubProjectExistsAsync(int subProjectId, CancellationToken cancellationToken)
    {
        var isApproved = await _context.SubProjects.AsNoTracking()
            .Where(s => s.SubProjectId == subProjectId)
            .Select(s => (bool?)s.IsApproved)
            .FirstOrDefaultAsync(cancellationToken);

        if (isApproved == null)
        {
            throw new NotFoundException($"المشروع الفرعي رقم {subProjectId} غير موجود");
        }

        if (isApproved == false)
        {
            throw new BusinessRuleException("لا يمكن العمل على مراحل الطرح قبل اعتماد المشروع الفرعي");
        }
    }

    /// <summary>
    /// لا تبدأ أي مرحلة طرح لمشروع قبل اكتمال مذكرة العرض المرتبطة به لهذا العام — يجب أن تكون معتمَدة
    /// بقرار لجنة الشؤون القانونية، لا يكفي إرفاقها فقط. مذكرة مكتملة من سنة مالية سابقة لا تكفي لبدء
    /// عمل جديد في السنة الحالية — هذا هو الحارس الفعلي خلف زر "مراحل الطرح" (وليس تعطيله في الواجهة
    /// وحده)، فيُطابق نفس شرط تفعيله بالضبط.
    /// </summary>
    private async Task EnsureHasPresentationMemoAsync(int subProjectId, CancellationToken cancellationToken)
    {
        var currentYearId = await ResolveCurrentFinancialYearIdAsync(cancellationToken);

        var isActiveMemoCompleted = await _context.PresentationMemoSubProjects.AsNoTracking()
            .Where(x => x.SubProjectId == subProjectId
                && (x.PresentationMemo.FinancialYearId == currentYearId || x.PresentationMemo.FinancialYearId == null))
            .OrderByDescending(x => x.PresentationMemo.CreatedAt)
            .ThenByDescending(x => x.PresentationMemo.Id)
            .Select(x => (bool?)x.PresentationMemo.IsCompleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (isActiveMemoCompleted != true)
        {
            throw new BusinessRuleException("لا يمكن بدء مراحل الطرح قبل اكتمال مذكرة العرض المرتبطة بالمشروع للسنة المالية الحالية");
        }
    }

    /// <summary>
    /// السنة المالية "الحالية" = التي يقع تاريخ اليوم بين بدايتها ونهايتها؛ محسوبة من التاريخ لا من اختيار
    /// العميل، حتى لا يُخدَع الحارس بمعامل سنة مُرسَل من طلب مباشر. عند غياب سنة تطابق اليوم (فجوة بيانات)
    /// يُستخدَم أحدث سنة بدأت فعلًا كأقرب تقدير بدل ترك الحارس بلا مرجعية.
    /// </summary>
    private async Task<int?> ResolveCurrentFinancialYearIdAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var currentByRange = await _context.FinancialYears.AsNoTracking()
            .Where(y => y.StartDate.Date <= today && y.EndDate.Date >= today)
            .OrderByDescending(y => y.StartDate)
            .Select(y => (int?)y.FinancialYearId)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentByRange != null)
        {
            return currentByRange;
        }

        return await _context.FinancialYears.AsNoTracking()
            .OrderByDescending(y => y.StartDate)
            .Select(y => (int?)y.FinancialYearId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> IsPreviousStageCompletedAsync(ProcurementStage stage, int subProjectId, CancellationToken cancellationToken)
    {
        if (stage == ProcurementStage.TenderDocument)
        {
            return true;
        }

        var previousStage = (ProcurementStage)((int)stage - 1);
        var previousState = await _stages[previousStage].FindDocAsync(subProjectId, cancellationToken);
        return previousState?.IsCompleted ?? false;
    }

    private async Task EnsurePreviousStageCompletedAsync(ProcurementStage stage, int subProjectId, CancellationToken cancellationToken)
    {
        if (stage == ProcurementStage.TenderDocument)
        {
            return;
        }

        if (!await IsPreviousStageCompletedAsync(stage, subProjectId, cancellationToken))
        {
            var previousStage = (ProcurementStage)((int)stage - 1);
            throw new BusinessRuleException($"يجب إكمال مرحلة \"{_stages[previousStage].Label}\" أولاً");
        }
    }

    private async Task<ContractAwardDetailsDto?> GetContractAwardDetailsAsync(int subProjectId, CancellationToken cancellationToken)
    {
        var project = await _context.SubProjects.AsNoTracking()
            .Where(s => s.SubProjectId == subProjectId)
            .Select(s => new { s.ProjectNature, s.BankFunding, s.SelfFunding })
            .FirstOrDefaultAsync(cancellationToken);

        if (project == null)
        {
            return null;
        }

        var doc = await _context.ContractAwards.AsNoTracking()
            .Where(x => x.SubProjectId == subProjectId)
            .Select(x => new
            {
                x.AdvancePaymentDone,
                x.AdvancePaymentPercentage,
                x.AdvancePaymentSelfAmount,
                x.AdvancePaymentBankAmount,
                x.AdvancePaymentDate,
                x.ExecutionDurationMonths,
                x.ExecutionDurationDays,
                x.SiteHandoverMode,
                x.SiteHandoverDate,
                SiteHandoverProofFileName = x.SiteHandoverProofFile == null ? null : x.SiteHandoverProofFile.FileName,
                x.PenaltyAmount,
                x.ProjectAssignmentId,
                ContractorId = (int?)x.ProjectAssignment!.ContractorId,
                ContractorName = x.ProjectAssignment!.Contractor!.ContractorName,
                ContractTypeId = (int?)x.ProjectAssignment!.ContractTypeId,
                x.ProjectAssignment!.ContractDate,
                x.ProjectAssignment!.ContractValue,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalCost = project.BankFunding + project.SelfFunding;
        var details = new ContractAwardDetailsDto
        {
            ProjectNature = project.ProjectNature,
            RequiresAdvancePayment = IsContractingProject(project.ProjectNature),
            TotalCost = totalCost,
            BankFunding = project.BankFunding,
            SelfFunding = project.SelfFunding,
        };

        if (doc == null)
        {
            return details;
        }

        details.AdvancePaymentDone = doc.AdvancePaymentDone;
        details.AdvancePaymentPercentage = doc.AdvancePaymentPercentage;
        details.AdvancePaymentSelfAmount = doc.AdvancePaymentSelfAmount;
        details.AdvancePaymentBankAmount = doc.AdvancePaymentBankAmount;
        details.AdvancePaymentDate = doc.AdvancePaymentDate;
        details.ExecutionDurationMonths = doc.ExecutionDurationMonths;
        details.ExecutionDurationDays = doc.ExecutionDurationDays;
        details.SiteHandoverMode = (int?)doc.SiteHandoverMode;
        details.SiteHandoverDate = doc.SiteHandoverDate;
        details.SiteHandoverProofFileName = doc.SiteHandoverProofFileName;
        details.ContractualDeliveryDate = doc.SiteHandoverDate?
            .AddMonths(doc.ExecutionDurationMonths ?? 0)
            .AddDays(doc.ExecutionDurationDays ?? 0);
        details.PenaltyAmount = doc.PenaltyAmount;
        details.ContractorId = doc.ContractorId;
        details.ContractorName = doc.ContractorName;
        details.ContractTypeId = doc.ContractTypeId;
        details.ContractDate = doc.ContractDate;
        details.ContractValue = doc.ContractValue;
        details.Savings = doc.ContractValue is decimal cv && cv > 0 && cv < totalCost ? totalCost - cv : null;

        return details;
    }

    private async Task<bool> GetAdvancePaymentDoneAsync(int subProjectId, CancellationToken cancellationToken)
    {
        return await _context.ContractAwards.AsNoTracking()
            .Where(x => x.SubProjectId == subProjectId)
            .Select(x => x.AdvancePaymentDone)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static TDto BuildStageDto<TDto>(ProcurementStage stage, IStageOps ops, StageDocState? state)
        where TDto : ProcurementStageDto, new()
    {
        // الموعد النهائي = وقت تحديد المدة (لا وقت إنشاء المستند) + المدة التي حددها مدير التخطيط —
        // لو المستند موجود من قبل (مثلاً أُنشئ برفع إصدار سابق) وحُدِّدت المدة لاحقًا، العدّ يبدأ من التحديد نفسه.
        // مرحلة الإعلان تتجاوز هذا لاحقًا بقاعدة الـ15 يومًا الثابتة (انظر GetOverviewAsync/GetStageAsync).
        var deadline = state is { DurationDays: int days, DurationSetAt: DateTime durationSetAt }
            ? durationSetAt.AddDays(days)
            : (DateTime?)null;
        var canFail = deadline != null && DateTime.UtcNow > deadline && state?.IsCompleted != true;

        return new TDto
        {
            Stage = ProcurementStageKeys.ToKey(stage),
            StageLabel = ops.Label,
            Order = (int)stage,
            DocumentId = state?.Id,
            CurrentVersionNumber = state?.CurrentVersionNumber ?? 0,
            IsCompleted = state?.IsCompleted ?? false,
            LastUpdatedAt = state?.LastUpdatedAt,
            FileSlots = ops.Slots
                .Where(s => !(stage == ProcurementStage.ContractAward && s.Key == "advance-payment-proof"))
                .Select(s => new ProcurementFileSlotDto { Key = s.Key, Label = s.Label, Required = s.Required })
                .ToList(),
            DurationDays = state?.DurationDays,
            Deadline = deadline,
            CanFail = canFail,
            IsSkipped = state?.IsSkipped ?? false,
            SkipReason = state?.SkipReason,
            FailedAt = state?.FailedAt,
            FailureReason = state?.FailureReason,
        };
    }

    private static Dictionary<ProcurementStage, IStageOps> BuildStages(AppDbContext db) => new()
    {
        [ProcurementStage.TenderDocument] = new StageOps<TenderDocument, TenderDocumentVersion>(
            db, "كراسة الشروط",
            (v, id) => v.TenderDocumentId = id,
            docId => v => v.TenderDocumentId == docId,
            [FileSlot<TenderDocumentVersion>.Single("file", "ملف كراسة الشروط", v => v.File, (v, f) => v.File = f!)]),

        [ProcurementStage.Announcement] = new StageOps<Announcement, AnnouncementVersion>(
            db, "الإعلان",
            (v, id) => v.AnnouncementId = id,
            docId => v => v.AnnouncementId == docId,
            [
                new FileSlot<AnnouncementVersion>("newspaper-advertisement", "إعلان الجريدة", v => v.NewspaperAdvertisement, (v, f) => v.NewspaperAdvertisement = f, required: true),
                new FileSlot<AnnouncementVersion>("portal-advertisement", "إعلان البوابة", v => v.PortalAdvertisement, (v, f) => v.PortalAdvertisement = f, required: true),
                new FileSlot<AnnouncementVersion>("competent-authority-approval", "موافقة الجهة المختصة", v => v.CompetentAuthorityApproval, (v, f) => v.CompetentAuthorityApproval = f, required: true),
            ],
            extraCompletionCheck: (doc, ct) => Task.FromResult(ValidateAnnouncementForCompletion(doc))),

        [ProcurementStage.OpeningEnvelopes] = new StageOps<OpeningEnvelopes, OpeningEnvelopesVersion>(
            db, "فتح المظاريف",
            (v, id) => v.OpeningEnvelopesId = id,
            docId => v => v.OpeningEnvelopesId == docId,
            [FileSlot<OpeningEnvelopesVersion>.Single("file", "محضر فتح المظاريف", v => v.File, (v, f) => v.File = f!)]),

        [ProcurementStage.TechnicalEvaluation] = new StageOps<TechnicalEvaluation, TechnicalEvaluationVersion>(
            db, "التقييم الفني",
            (v, id) => v.TechnicalEvaluationId = id,
            docId => v => v.TechnicalEvaluationId == docId,
            [
                new FileSlot<TechnicalEvaluationVersion>("first-committee-report", "تقرير اللجنة الأول", v => v.FirstCommitteeReport, (v, f) => v.FirstCommitteeReport = f, required: true),
                new FileSlot<TechnicalEvaluationVersion>("second-committee-report", "تقرير اللجنة الثاني", v => v.SecondCommitteeReport, (v, f) => v.SecondCommitteeReport = f, required: true),
                new FileSlot<TechnicalEvaluationVersion>("final-technical-evaluation-report", "التقرير الفني النهائي", v => v.FinalTechnicalEvaluationReport, (v, f) => v.FinalTechnicalEvaluationReport = f, required: true),
            ]),

        [ProcurementStage.FinancialEvaluation] = new StageOps<FinancialEvaluation, FinancialEvaluationVersion>(
            db, "التقييم المالي",
            (v, id) => v.FinancialEvaluationId = id,
            docId => v => v.FinancialEvaluationId == docId,
            [
                new FileSlot<FinancialEvaluationVersion>("financial-bid-opening-minutes", "محضر فتح المظاريف المالية", v => v.FinancialBidOpeningMinutes, (v, f) => v.FinancialBidOpeningMinutes = f, required: true),
                new FileSlot<FinancialEvaluationVersion>("financial-evaluation-report", "تقرير التقييم المالي", v => v.FinancialEvaluationReport, (v, f) => v.FinancialEvaluationReport = f, required: true),
                new FileSlot<FinancialEvaluationVersion>("estimated-cost-sheet", "مقايسة التكلفة التقديرية", v => v.EstimatedCostSheet, (v, f) => v.EstimatedCostSheet = f, required: true),
            ]),

        [ProcurementStage.ContractAward] = new StageOps<ContractAward, ContractAwardVersion>(
            db, "العقد والترسية",
            (v, id) => v.ContractAwardId = id,
            docId => v => v.ContractAwardId == docId,
            [
                new FileSlot<ContractAwardVersion>("award-order", "أمر الإسناد", v => v.AwardOrder, (v, f) => v.AwardOrder = f, required: true),
                new FileSlot<ContractAwardVersion>("contract", "العقد", v => v.Contract, (v, f) => v.Contract = f, required: true),
                // إثبات صرف الدفعة المقدمة — إلزاميته مشروطة بنوع المشروع، فيتحقق منها extraCompletionCheck
                new FileSlot<ContractAwardVersion>("advance-payment-proof", "إثبات صرف الدفعة المقدمة", v => v.AdvancePaymentProof, (v, f) => v.AdvancePaymentProof = f, required: false),
            ],
            extraCompletionCheck: (doc, ct) => ValidateContractAwardForCompletionAsync(db, doc, ct)),
    };

    /// <summary>«مقاولات» = أعمال تنفيذ تستحق دفعة مقدمة. «توريدات» لا يُصرف لها مقدَّم.</summary>
    internal static bool IsContractingProject(string? projectNature) => projectNature == "مقاولات";

    /// <summary>
    /// الحد الأدنى الإلزامي لبقاء الإعلان قائمًا قبل فتح المظاريف — لا يخضع لتقدير مدير التخطيط،
    /// بعكس بقية المراحل التي مدتها القصوى اختيارية بالكامل.
    /// </summary>
    internal const int AnnouncementMinimumDays = 15;

    /// <summary>المدة الافتراضية للمرحلة العادية، وتبدأ فقط عند فتح المرحلة فعليًا.</summary>
    internal const int DefaultStageDurationDays = 7;

    /// <summary>لا يمكن إكمال الإعلان قبل تحديد تاريخه، ولا قبل مرور 15 يومًا كاملة منه.</summary>
    private static string? ValidateAnnouncementForCompletion(Announcement doc)
    {
        if (doc.AnnouncementDate == null)
        {
            return "يجب تحديد تاريخ الإعلان قبل إكمال هذه المرحلة";
        }

        var minimumCompletionDate = doc.AnnouncementDate.Value.AddDays(AnnouncementMinimumDays);
        if (DateTime.UtcNow < minimumCompletionDate)
        {
            var remaining = (minimumCompletionDate - DateTime.UtcNow).Days + 1;
            return $"لا يمكن إكمال الإعلان قبل مرور {AnnouncementMinimumDays} يومًا من تاريخه — متبقٍ {remaining} يوم تقريبًا";
        }

        return null;
    }

    /// <summary>
    /// يستبدل حساب الموعد النهائي العام (CreatedAt + DurationDays) بقاعدة الإعلان الثابتة
    /// (AnnouncementDate + 15 يومًا)، ويُرفق تاريخ الإعلان نفسه للعرض.
    /// </summary>
    private async Task ApplyAnnouncementOverridesAsync(ProcurementStageDto dto, int subProjectId, CancellationToken cancellationToken)
    {
        var announcementDate = await _context.Announcements.AsNoTracking()
            .Where(a => a.SubProjectId == subProjectId)
            .Select(a => a.AnnouncementDate)
            .FirstOrDefaultAsync(cancellationToken);

        dto.AnnouncementDate = announcementDate;
        dto.DurationDays = AnnouncementMinimumDays;
        dto.Deadline = announcementDate?.AddDays(AnnouncementMinimumDays);
        dto.CanFail = dto.Deadline != null && DateTime.UtcNow > dto.Deadline && !dto.IsCompleted;
    }

    /// <summary>
    /// قواعد إكمال الترسية: المقاول، المدة، تسليم الأرضية — ثم الدفعة المقدمة لمشروعات «مقاولات» فقط.
    /// </summary>
    private static async Task<string?> ValidateContractAwardForCompletionAsync(
        AppDbContext db,
        ContractAward doc,
        CancellationToken ct)
    {
        var project = await db.SubProjects.AsNoTracking()
            .Where(s => s.SubProjectId == doc.SubProjectId)
            .Select(s => new { s.ProjectNature, s.BankFunding, s.SelfFunding, s.OverrunPercentage })
            .FirstOrDefaultAsync(ct);

        if (project == null)
        {
            return "المشروع الفرعي غير موجود";
        }

        if (doc.ProjectAssignmentId == null)
        {
            return "يجب اختيار المقاول المسند إليه المشروع قبل إكمال الترسية";
        }

        if ((doc.ExecutionDurationMonths ?? 0) <= 0 && (doc.ExecutionDurationDays ?? 0) <= 0)
        {
            return "يجب تحديد المدة القصوى لتنفيذ المشروع";
        }

        var isContracting = IsContractingProject(project.ProjectNature);

        // توريدات لا تسليم أرضية لها إطلاقًا — المورّد يورّد أولًا ثم يُصرف له، لا أرض تُسلَّم.
        if (isContracting)
        {
            if (doc.SiteHandoverMode == null)
            {
                return "يجب تحديد ما إذا كانت أرضية المشروع مُسلَّمة للمقاول أم لا";
            }

            if (doc.SiteHandoverMode == SiteHandoverMode.AtAward)
            {
                if (doc.SiteHandoverDate == null)
                {
                    return "يجب تسجيل تاريخ تسليم الأرضية قبل إكمال الترسية";
                }

                if (doc.SiteHandoverProofFile == null)
                {
                    return "يجب رفع إثبات تسليم الأرضية قبل إكمال الترسية";
                }
            }
        }

        var totalCost = project.BankFunding + project.SelfFunding;

        var contractValue = await db.Set<ProjectAssignment>().AsNoTracking()
            .Where(a => a.AssignmentId == doc.ProjectAssignmentId)
            .Select(a => a.ContractValue)
            .FirstOrDefaultAsync(ct);

        if (contractValue is null or <= 0)
        {
            return "يجب تحديد قيمة العقد قبل إكمال الترسية";
        }

        var allowedCeiling = totalCost * (1 + (project.OverrunPercentage ?? 0) / 100m);
        if (contractValue.Value > allowedCeiling)
        {
            return $"قيمة العقد ({contractValue.Value:N2} ج.م) تتجاوز الإجمالي المخطط بعد نسبة التجاوز ({allowedCeiling:N2} ج.م)";
        }

        if (!isContracting)
        {
            return null;
        }

        // من هنا: مشروع «مقاولات» — الدفعة المقدمة إلزامية
        if (!doc.AdvancePaymentDone)
        {
            return "يجب تأكيد صرف الدفعة المقدمة للمقاول قبل إكمال هذه المرحلة";
        }

        var percentage = doc.AdvancePaymentPercentage ?? 0m;
        if (percentage <= 0m || percentage > 100m)
        {
            return "نسبة الدفعة المقدمة يجب أن تكون بين 1% و100%";
        }

        // القاعدة = قيمة العقد نفسها لا الإجمالي المخطط (totalCost) — تطابقًا مع الواجهة التي تحسب
        // advanceAmount() من قيمة العقد المُدخلة، وإلا يوازن المستخدم الشاشة على صفر ثم يُرفض عند الإكمال برقم لا يظهر له أبدًا.
        var expected = Math.Round(contractValue.Value * percentage / 100m, 2);
        var self = doc.AdvancePaymentSelfAmount ?? 0m;
        var bank = doc.AdvancePaymentBankAmount ?? 0m;

        if (Math.Round(self + bank, 2) != expected)
        {
            return $"مجموع المصروف ذاتيًا وبنكيًا يجب أن يساوي قيمة الدفعة المقدمة ({expected:N2} ج.م)";
        }

        // نفس توزيع فرق العقد المستخدَم في متابعة المشروعات (ProjectFundingPolicy) - إن كانت قيمة
        // العقد أقل من الإجمالي المخطط، المتاح الفعلي لكل مصدر أقل من مخططه الأصلي.
        var (adjustedSelfFunding, adjustedBankFunding) = ProjectFundingPolicy.ApplyContractSavings(
            project.SelfFunding, project.BankFunding, totalCost, contractValue);

        if (self > adjustedSelfFunding)
        {
            return $"المصروف من التمويل الذاتي يتجاوز المتاح ({adjustedSelfFunding:N2} ج.م)";
        }

        if (bank > adjustedBankFunding)
        {
            return $"المصروف من التمويل البنكي يتجاوز المتاح ({adjustedBankFunding:N2} ج.م)";
        }

        var latestHasProof = await db.ContractAwardVersions.AsNoTracking()
            .Where(v => v.ContractAwardId == doc.Id && v.VersionNumber == doc.CurrentVersionNumber)
            .Select(v => v.AdvancePaymentProof != null)
            .FirstOrDefaultAsync(ct);

        return latestHasProof ? null : "يجب رفع إثبات صرف الدفعة المقدمة قبل إكمال هذه المرحلة";
    }

    // ------------------------------------------------------------------
    // تجريد المرحلة (مستند + إصدارات) بشكل عام على أنواع الكيانات
    // ------------------------------------------------------------------

    internal sealed record StageDocState(
        int Id,
        int CurrentVersionNumber,
        bool IsCompleted,
        DateTime? LastUpdatedAt,
        DateTime CreatedAt,
        int? DurationDays,
        DateTime? DurationSetAt,
        bool IsSkipped,
        string? SkipReason,
        DateTime? FailedAt,
        string? FailureReason);

    internal sealed record SlotInfo(string Key, string Label, bool Required);

    internal interface IStageOps
    {
        string Label { get; }
        IReadOnlyList<SlotInfo> Slots { get; }
        Task<StageDocState?> FindDocAsync(int subProjectId, CancellationToken ct);
        Task<List<ProcurementVersionDto>> GetVersionDtosAsync(int documentId, CancellationToken ct);
        Task<ProcurementVersionDto> UploadAsync(int subProjectId, Dictionary<string, StoredFile> files, string? notes, CancellationToken ct);
        Task<StoredFile?> GetFileAsync(int subProjectId, int versionNumber, string fileKey, CancellationToken ct);
        Task SetCompletionAsync(int subProjectId, bool isCompleted, CancellationToken ct);

        /// <summary>لو المرحلة مكتملة يرجعها "غير مكتملة" بصمت (بدون التحقق من الشروط) — تُستخدم عند إعادة فتح مرحلة سابقة فتُلغي المراحل التالية تبعًا لها. لا تفعل شيئًا لو المرحلة لم تبدأ أصلاً.</summary>
        Task ResetIfCompletedAsync(int subProjectId, CancellationToken ct);

        /// <summary>يخزّن المدة القصوى اليدوية؛ طبقة الخدمة تحوّل null إلى القيمة الافتراضية قبل الاستدعاء.</summary>
        Task SetDurationAsync(int subProjectId, int? durationDays, CancellationToken ct);

        /// <summary>يخزّن مدة المرحلة ولحظة تفعيلها مرة واحدة عند فتحها، لا عند القراءة.</summary>
        Task ActivateAsync(int subProjectId, int durationDays, DateTime? durationSetAt, CancellationToken ct);

        /// <summary>"هذه المرحلة غير لازمة للطرح" — تُعامَل كمكتملة فتفتح ما بعدها، مع تمييزها بصريًا.</summary>
        Task SkipAsync(int subProjectId, string reason, CancellationToken ct);

        /// <summary>فشل هذه المرحلة — تبطل اكتمالها وما بعدها (بلا حذف أي إصدار) وتُسجِّل السبب أثرًا دائمًا.</summary>
        Task FailAsync(int subProjectId, string reason, CancellationToken ct);
    }

    internal sealed class FileSlot<TVer>
    {
        public FileSlot(string key, string label, Func<TVer, StoredFile?> get, Action<TVer, StoredFile?> set, bool required = false)
        {
            Key = key;
            Label = label;
            Get = get;
            Set = set;
            Required = required;
        }

        public string Key { get; }
        public string Label { get; }
        public bool Required { get; }
        public Func<TVer, StoredFile?> Get { get; }
        public Action<TVer, StoredFile?> Set { get; }

        /// <summary>مرحلة بملف واحد: الملف إجباري.</summary>
        public static FileSlot<TVer> Single(string key, string label, Func<TVer, StoredFile?> get, Action<TVer, StoredFile?> set) =>
            new(key, label, get, set, required: true);
    }

    internal sealed class StageOps<TDoc, TVer> : IStageOps
        where TDoc : SubProjectDocumentBase, new()
        where TVer : DocumentVersionBase, new()
    {
        private readonly AppDbContext _db;
        private readonly Action<TVer, int> _setDocId;
        private readonly Func<int, Expression<Func<TVer, bool>>> _versionsOfDoc;
        private readonly IReadOnlyList<FileSlot<TVer>> _slots;

        /// <summary>
        /// تحقق إضافي عند الإكمال. غير متزامن لأن بعض القواعد تحتاج قراءة من القاعدة
        /// (مثل نوع المشروع لتحديد إلزامية الدفعة المقدمة).
        /// </summary>
        private readonly Func<TDoc, CancellationToken, Task<string?>>? _extraCompletionCheck;

        public StageOps(
            AppDbContext db,
            string label,
            Action<TVer, int> setDocId,
            Func<int, Expression<Func<TVer, bool>>> versionsOfDoc,
            IReadOnlyList<FileSlot<TVer>> slots,
            Func<TDoc, CancellationToken, Task<string?>>? extraCompletionCheck = null)
        {
            _db = db;
            Label = label;
            _setDocId = setDocId;
            _versionsOfDoc = versionsOfDoc;
            _slots = slots;
            _extraCompletionCheck = extraCompletionCheck;
        }

        public string Label { get; }

        public IReadOnlyList<SlotInfo> Slots =>
            _slots.Select(s => new SlotInfo(s.Key, s.Label, s.Required)).ToList();

        public async Task<StageDocState?> FindDocAsync(int subProjectId, CancellationToken ct)
        {
            return await _db.Set<TDoc>().AsNoTracking()
                .Where(d => d.SubProjectId == subProjectId)
                .Select(d => new StageDocState(
                    d.Id,
                    d.CurrentVersionNumber,
                    d.IsCompleted,
                    d.UpdatedAt ?? d.CreatedAt,
                    d.CreatedAt,
                    d.DurationDays,
                    d.DurationSetAt,
                    d.IsSkipped,
                    d.SkipReason,
                    d.FailedAt,
                    d.FailureReason))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<ProcurementVersionDto>> GetVersionDtosAsync(int documentId, CancellationToken ct)
        {
            var versions = await _db.Set<TVer>().AsNoTracking()
                .Where(_versionsOfDoc(documentId))
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync(ct);

            return versions.Select(ToDto).ToList();
        }

        public async Task<ProcurementVersionDto> UploadAsync(int subProjectId, Dictionary<string, StoredFile> files, string? notes, CancellationToken ct)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            var doc = await _db.Set<TDoc>()
                .FirstOrDefaultAsync(d => d.SubProjectId == subProjectId, ct);

            if (doc == null)
            {
                doc = new TDoc { SubProjectId = subProjectId };
                _db.Set<TDoc>().Add(doc);
                await _db.SaveChangesAsync(ct);
            }

            if (doc.IsCompleted)
            {
                throw new BusinessRuleException("هذه المرحلة مكتملة — يجب إعادة فتحها قبل إضافة إصدار جديد");
            }

            doc.CurrentVersionNumber += 1;

            var version = new TVer
            {
                VersionNumber = doc.CurrentVersionNumber,
                Notes = notes,
            };
            _setDocId(version, doc.Id);

            foreach (var slot in _slots)
            {
                if (files.TryGetValue(slot.Key, out var file))
                {
                    slot.Set(version, file);
                }
            }

            _db.Set<TVer>().Add(version);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return ToDto(version);
        }

        public async Task<StoredFile?> GetFileAsync(int subProjectId, int versionNumber, string fileKey, CancellationToken ct)
        {
            var slot = _slots.FirstOrDefault(s => string.Equals(s.Key, fileKey, StringComparison.OrdinalIgnoreCase));
            if (slot == null)
            {
                return null;
            }

            var docId = await _db.Set<TDoc>().AsNoTracking()
                .Where(d => d.SubProjectId == subProjectId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync(ct);

            if (docId == null)
            {
                return null;
            }

            var version = await _db.Set<TVer>().AsNoTracking()
                .Where(_versionsOfDoc(docId.Value))
                .Where(v => v.VersionNumber == versionNumber)
                .FirstOrDefaultAsync(ct);

            return version == null ? null : slot.Get(version);
        }

        public async Task SetCompletionAsync(int subProjectId, bool isCompleted, CancellationToken ct)
        {
            var doc = await _db.Set<TDoc>()
                .FirstOrDefaultAsync(d => d.SubProjectId == subProjectId, ct)
                ?? throw new NotFoundException("لم تبدأ هذه المرحلة بعد — لا يوجد مستند");

            if (isCompleted)
            {
                if (doc.CurrentVersionNumber == 0)
                {
                    throw new BusinessRuleException("لا يمكن إكمال مرحلة بدون أي إصدار مرفوع");
                }

                var latestVersion = await _db.Set<TVer>().AsNoTracking()
                    .Where(_versionsOfDoc(doc.Id))
                    .FirstOrDefaultAsync(v => v.VersionNumber == doc.CurrentVersionNumber, ct);

                foreach (var slot in _slots.Where(s => s.Required))
                {
                    if (latestVersion == null || slot.Get(latestVersion) == null)
                    {
                        throw new BusinessRuleException($"يجب رفع ملف \"{slot.Label}\" قبل إكمال هذه المرحلة");
                    }
                }

                if (_extraCompletionCheck != null)
                {
                    var error = await _extraCompletionCheck(doc, ct);
                    if (error != null)
                    {
                        throw new BusinessRuleException(error);
                    }
                }
            }

            doc.IsCompleted = isCompleted;
            if (!isCompleted)
            {
                // إبطال الاكتمال يُلغي حالة "التخطي" أيضًا — تخطٍّ ثم إعادة فتح تعني
                // أن المرحلة لازمة فعليًا الآن، فيجب رفع مستنداتها بشكل طبيعي.
                doc.IsSkipped = false;
                doc.SkipReason = null;
                doc.SkippedAt = null;
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task ResetIfCompletedAsync(int subProjectId, CancellationToken ct)
        {
            var doc = await _db.Set<TDoc>()
                .FirstOrDefaultAsync(d => d.SubProjectId == subProjectId, ct);

            if (doc != null && doc.IsCompleted)
            {
                doc.IsCompleted = false;
                doc.IsSkipped = false;
                doc.SkipReason = null;
                doc.SkippedAt = null;
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task SetDurationAsync(int subProjectId, int? durationDays, CancellationToken ct)
        {
            if (durationDays is < 1)
            {
                throw new BusinessRuleException("المدة القصوى يجب أن تكون يومًا واحدًا على الأقل");
            }

            var doc = await _db.Set<TDoc>()
                .FirstOrDefaultAsync(d => d.SubProjectId == subProjectId, ct);

            if (doc == null)
            {
                // مدير التخطيط يحدد المدة وقت فتح المرحلة، قبل أي رفع فعلي — العدّ يبدأ من هنا،
                // لا من أول إصدار، حتى يعكس "من غير ما يحصل إكمال" بدقة أكبر.
                doc = new TDoc { SubProjectId = subProjectId };
                _db.Set<TDoc>().Add(doc);
            }

            doc.DurationDays = durationDays;
            doc.DurationSetAt = durationDays == null ? null : DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task ActivateAsync(int subProjectId, int durationDays, DateTime? durationSetAt, CancellationToken ct)
        {
            var doc = await _db.Set<TDoc>()
                .FirstOrDefaultAsync(d => d.SubProjectId == subProjectId, ct);

            if (doc == null)
            {
                doc = new TDoc { SubProjectId = subProjectId };
                _db.Set<TDoc>().Add(doc);
            }

            doc.DurationDays ??= durationDays;
            if (durationSetAt.HasValue && doc.DurationSetAt == null)
            {
                doc.DurationSetAt = durationSetAt.Value;
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task SkipAsync(int subProjectId, string reason, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new BusinessRuleException("سبب التخطي مطلوب");
            }

            var doc = await _db.Set<TDoc>()
                .FirstOrDefaultAsync(d => d.SubProjectId == subProjectId, ct);

            if (doc == null)
            {
                doc = new TDoc { SubProjectId = subProjectId };
                _db.Set<TDoc>().Add(doc);
            }
            else if (doc.IsCompleted && !doc.IsSkipped)
            {
                throw new BusinessRuleException("هذه المرحلة مكتملة فعليًا بمستندات — لا معنى لتخطيها");
            }

            doc.IsCompleted = true;
            doc.IsSkipped = true;
            doc.SkipReason = reason.Trim();
            doc.SkippedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task FailAsync(int subProjectId, string reason, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new BusinessRuleException("سبب الفشل مطلوب");
            }

            var doc = await _db.Set<TDoc>()
                .FirstOrDefaultAsync(d => d.SubProjectId == subProjectId, ct)
                ?? throw new NotFoundException("لم تبدأ هذه المرحلة بعد — لا يوجد مستند");

            // نفس أثر إعادة الفتح على هذه المرحلة (بلا حذف إصدارات) + توثيق أن السبب فشل لا تصحيح عادي
            doc.IsCompleted = false;
            doc.IsSkipped = false;
            doc.SkipReason = null;
            doc.SkippedAt = null;
            doc.FailedAt = DateTime.UtcNow;
            doc.FailureReason = reason.Trim();
            await _db.SaveChangesAsync(ct);
        }

        private ProcurementVersionDto ToDto(TVer version)
        {
            var dto = new ProcurementVersionDto
            {
                Id = version.Id,
                VersionNumber = version.VersionNumber,
                Notes = version.Notes,
                CreatedAt = version.CreatedAt,
            };

            foreach (var slot in _slots)
            {
                var file = slot.Get(version);
                if (file != null)
                {
                    dto.Files.Add(new ProcurementFileDto
                    {
                        Key = slot.Key,
                        Label = slot.Label,
                        FileName = file.FileName,
                        FileExtension = file.FileExtension,
                        FileSize = file.FileSize,
                    });
                }
            }

            return dto;
        }
    }
}
