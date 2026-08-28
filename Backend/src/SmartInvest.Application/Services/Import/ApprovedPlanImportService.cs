using SmartInvest.Application.Common;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Enums;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services.Import;

public class ApprovedPlanImportService
{
    private readonly IMainProjectRepository _mainProjectRepository;
    private readonly ISubProjectRepository _subProjectRepository;
    private readonly IGenericRepository<FinancialYear> _financialYearRepository;
    private readonly IGenericRepository<PlanProject> _planProjectRepository;
    private readonly IGenericRepository<ProjectPriority> _priorityRepository;
    private readonly IGenericRepository<ProjectStatus> _statusRepository;
    private readonly IGenericRepository<Markaz> _markazRepository;
    private readonly IGenericRepository<SubProgram> _subProgramRepository;
    private readonly IGenericRepository<ProjectLevel> _projectLevelRepository;
    private readonly IGenericRepository<ComponentType> _componentTypeRepository;
    private readonly IGenericRepository<AccountingUnit> _accountingUnitRepository;
    private readonly IGenericRepository<ExecutiveAgency> _agencyRepository;
    private readonly IGenericRepository<SubProjectFinancialYear> _financialYearLinkRepository;
    private readonly ISubProjectService _subProjectService;
    private readonly IPlanRepo _planRepo;
    private readonly IPlanService _planService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMeasurementResolutionService _measurementResolutionService;

    public ApprovedPlanImportService(
        IMainProjectRepository mainProjectRepository,
        ISubProjectRepository subProjectRepository,
        IGenericRepository<FinancialYear> financialYearRepository,
        IGenericRepository<PlanProject> planProjectRepository,
        IGenericRepository<ProjectPriority> priorityRepository,
        IGenericRepository<ProjectStatus> statusRepository,
        IGenericRepository<Markaz> markazRepository,
        IGenericRepository<SubProgram> subProgramRepository,
        IGenericRepository<ProjectLevel> projectLevelRepository,
        IGenericRepository<ComponentType> componentTypeRepository,
        IGenericRepository<AccountingUnit> accountingUnitRepository,
        IGenericRepository<ExecutiveAgency> agencyRepository,
        IGenericRepository<SubProjectFinancialYear> financialYearLinkRepository,
        ISubProjectService subProjectService,
        IPlanRepo planRepo,
        IPlanService planService,
        IUnitOfWork unitOfWork,
        IMeasurementResolutionService measurementResolutionService)
    {
        _mainProjectRepository = mainProjectRepository;
        _subProjectRepository = subProjectRepository;
        _financialYearRepository = financialYearRepository;
        _planProjectRepository = planProjectRepository;
        _priorityRepository = priorityRepository;
        _statusRepository = statusRepository;
        _markazRepository = markazRepository;
        _subProgramRepository = subProgramRepository;
        _projectLevelRepository = projectLevelRepository;
        _componentTypeRepository = componentTypeRepository;
        _accountingUnitRepository = accountingUnitRepository;
        _agencyRepository = agencyRepository;
        _financialYearLinkRepository = financialYearLinkRepository;
        _subProjectService = subProjectService;
        _planRepo = planRepo;
        _planService = planService;
        _unitOfWork = unitOfWork;
        _measurementResolutionService = measurementResolutionService;
    }

    private record MatchResult(int? SubProjectId, int? MainProjectId, bool IsApproved);

    public async Task<ApprovedImportPreviewDto> PreviewAsync(ParsedImportFile file, int? financialYearId, CancellationToken cancellationToken)
    {
        var dto = new ApprovedImportPreviewDto();

        foreach (var row in file.Rows)
        {
            var match = await MatchRowAsync(row, cancellationToken);
            if (match.SubProjectId.HasValue)
            {
                dto.MatchedCount++;
            }
            else
            {
                dto.UnresolvedRows.Add(new UnresolvedImportRowDto
                {
                    RowIndex = row.RowIndex,
                    MainProjectName = row.MainProjectName,
                    SubProjectName = row.SubProjectName,
                    Code = row.SubProjectCode,
                });
            }
        }

        if (dto.UnresolvedRows.Count > 0)
        {
            await SuggestMatchesForUnresolvedRowsAsync(dto.UnresolvedRows, financialYearId, cancellationToken);
        }

        return dto;
    }

    // Confidence floor for a token-overlap match to be worth suggesting at all - picked from
    // observation, not a formal calibration: two genuinely-the-same projects re-typed between a
    // suggested-plan draft and its approved-plan export share the large majority of their words,
    // so a true match scores well above this even after minor rewording.
    private const double MinRowMatchSimilarity = 0.6;

    private async Task SuggestMatchesForUnresolvedRowsAsync(List<UnresolvedImportRowDto> unresolvedRows, int? financialYearId, CancellationToken cancellationToken)
    {
        // Exact-name matching (MatchRowAsync) misses the common real-world case of the same
        // project re-submitted from a suggested-plan file with slightly different wording (e.g.
        // extra/missing spaces, minor rewording between draft and final text). Suggest the closest
        // existing sub-project so staff don't have to manually search a list of hundreds.
        //
        // This deliberately does NOT go through the AI lookup-match service: verification against
        // a real 85-row approved-plan file showed the model's index-pair response frequently
        // pointed at the wrong entry once both the unresolved and existing lists ran into the
        // hundreds - a real risk here, since a wrong match silently approves the wrong project
        // under a real code. Matching near-identical structured text (not free-form semantics) is
        // exactly what deterministic token-overlap similarity is for, and it can't hallucinate a
        // match that isn't actually in the candidate list.
        //
        // Scoped to the target financial year: an approved-mode file is approving THAT year's own
        // suggested-mode sub-projects, not some same-named duplicate left over in a different year
        // from repeated testing - searching every year both bloated the candidate pool for no
        // reason and could point the suggestion at the wrong year's copy.
        var (allSubProjects, _) = await _subProjectRepository.SearchAsync(
            null, null, null, null, null, null, financialYearId, null, 1, 5000, cancellationToken);

        var candidates = allSubProjects
            .Select(s => (
                s.SubProjectId,
                Label: $"{s.MainProject.MainProjectName} / {s.SubProjectName}",
                s.IsApproved,
                Tokens: Tokenize(s.MainProject.MainProjectName + " " + s.SubProjectName)))
            .Where(c => c.Tokens.Count > 0)
            .ToList();

        foreach (var row in unresolvedRows)
        {
            var rowTokens = Tokenize(row.MainProjectName + " " + row.SubProjectName);
            if (rowTokens.Count == 0)
            {
                continue;
            }

            var best = candidates
                .Select(c => (c.SubProjectId, c.Label, c.IsApproved, Score: JaccardSimilarity(rowTokens, c.Tokens)))
                .OrderByDescending(c => c.Score)
                // Ties are common in a real deployment too (repeated suggested/approved cycles
                // across years reuse the exact same project text). The whole point of an
                // approved-mode import is to approve the still-pending sub-project it was
                // submitted for, so an unapproved candidate must always win a tie over an
                // already-approved one - otherwise this links to an unrelated already-approved
                // duplicate from some other year while the actual target stays pending forever
                // (verified live: this is exactly what happened before this ordering existed).
                .ThenBy(c => c.IsApproved)
                .ThenByDescending(c => c.SubProjectId)
                .FirstOrDefault();

            if (best.Score >= MinRowMatchSimilarity)
            {
                row.SuggestedSubProjectId = best.SubProjectId;
                row.SuggestedMatchLabel = best.Label;
            }
        }
    }

    private static readonly char[] TokenizeSeparators = { ' ', '\t', '\n', '\r', '(', ')', '-', '،', ',', '.', '/', '"', '\'', ':' };

    private static HashSet<string> Tokenize(string text)
    {
        return text
            .Split(TokenizeSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length > 1)
            .ToHashSet();
    }

    private static double JaccardSimilarity(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return 0;
        }

        var intersection = a.Count(b.Contains);
        var union = a.Count + b.Count - intersection;
        return union == 0 ? 0 : (double)intersection / union;
    }

    public async Task<ImportCommitResultDto> CommitAsync(ParsedImportFile file, ImportCommitDto dto, CancellationToken cancellationToken)
    {
        var financialYear = await _financialYearRepository.GetByIdAsync(dto.FinancialYearId, cancellationToken)
            ?? throw new NotFoundException($"السنة المالية رقم {dto.FinancialYearId} غير موجودة");

        var approvalDate = dto.ApprovalDate ?? throw new BusinessRuleException("تاريخ الاعتماد مطلوب");
        var rowResolutionByIndex = dto.RowResolutions.ToDictionary(r => r.RowIndex, r => r);

        var defaultPriorityId = (await _priorityRepository.FirstOrDefaultAsync(x => x.Priority == "منخفضة", cancellationToken))?.Id
            ?? throw new BusinessRuleException("أولوية «منخفضة» الافتراضية غير موجودة في قاعدة البيانات");
        var runningStatusId = (await _statusRepository.FirstOrDefaultAsync(x => x.StatusName == "قيد التنفيذ", cancellationToken))?.StatusId
            ?? throw new BusinessRuleException("حالة «قيد التنفيذ» الافتراضية غير موجودة في قاعدة البيانات");
        // These three "غير محدد" sentinels were originally seeded once by a data migration
        // (20260730215742_AddComponentTypeProjectLevelAccountingUnitTables), not by LookupSeeder -
        // unlike Governorate/MainProgram/ProjectPriority/ProjectStatus above, nothing recreates them
        // if the tables are ever emptied outside a from-scratch migration replay (e.g. a data reset
        // between test cycles). Self-heal instead of hard-failing every approved-mode import until
        // someone notices and manually re-inserts the row.
        var unspecifiedProjectLevelId = await GetOrCreateUnspecifiedProjectLevelIdAsync(cancellationToken);
        var unspecifiedComponentTypeId = await GetOrCreateUnspecifiedComponentTypeIdAsync(cancellationToken);
        var unspecifiedAccountingUnitId = await GetOrCreateUnspecifiedAccountingUnitIdAsync(cancellationToken);

        // A new SubProject needs a Markaz/SubProgram even when the row's own values don't match an
        // existing record (this mode has no per-row reconciliation for these two, unlike suggested
        // mode). Resolve by exact name from the row first; only fall back to an arbitrary existing
        // record - never a fabricated "unspecified" one, since neither table seeds such a sentinel.
        // All five dictionaries below are keyed by diacritic/whitespace-normalized name (not raw
        // .Trim()'d text) - a name that already exists but differs from the file's spelling by a
        // missing harakah or an extra internal space must still resolve to the existing record,
        // not silently fall through to the fallback/"غير محدد" sentinel or a fresh duplicate.
        var allMarkaz = await _markazRepository.GetAllAsync(cancellationToken);
        var markazIdByName = allMarkaz.ToDictionary(x => ArabicTextNormalizer.Normalize(x.MarkazName), x => x.MarkazId);
        var fallbackMarkazId = allMarkaz.FirstOrDefault()?.MarkazId
            ?? throw new BusinessRuleException("لا يوجد أي مركز في قاعدة البيانات");

        var allSubPrograms = await _subProgramRepository.GetAllAsync(cancellationToken);
        var subProgramIdByName = allSubPrograms
            .GroupBy(x => ArabicTextNormalizer.Normalize(x.SubProgramName))
            .ToDictionary(g => g.Key, g => g.First().SubProgramId);
        var fallbackSubProgramId = allSubPrograms.FirstOrDefault()?.SubProgramId
            ?? throw new BusinessRuleException("لا يوجد أي برنامج فرعي في قاعدة البيانات لإنشاء مشروع رئيسي جديد عليه");

        // Same idea for the row's own ProjectLevel/ComponentType/AccountingUnit/ExecutiveAgency text -
        // resolve by exact name so a newly-created sub-project reflects what the file actually says
        // instead of always landing on "غير محدد". Unlike Markaz/SubProgram these three lookups DO
        // seed a real "غير محدد" sentinel, so that's the fallback (ExecutiveAgency has none - stays null).
        var projectLevelIdByName = (await _projectLevelRepository.GetAllAsync(cancellationToken))
            .GroupBy(x => ArabicTextNormalizer.Normalize(x.Name)).ToDictionary(g => g.Key, g => g.First().Id);
        var componentTypeIdByName = (await _componentTypeRepository.GetAllAsync(cancellationToken))
            .GroupBy(x => ArabicTextNormalizer.Normalize(x.Name)).ToDictionary(g => g.Key, g => g.First().Id);
        var accountingUnitIdByName = (await _accountingUnitRepository.GetAllAsync(cancellationToken))
            .GroupBy(x => ArabicTextNormalizer.Normalize(x.Name)).ToDictionary(g => g.Key, g => g.First().Id);
        var agencyIdByName = (await _agencyRepository.GetAllAsync(cancellationToken))
            .GroupBy(x => ArabicTextNormalizer.Normalize(x.AgencyName)).ToDictionary(g => g.Key, g => g.First().ExecutiveAgencyId);

        var result = new ImportCommitResultDto { Mode = "Approved" };
        var approvedSubProjectIds = new List<int>();

        foreach (var row in file.Rows)
        {
            MainProject? mainProject = null;
            SubProject? subProject = null;
            bool mainProjectIsNew = false;
            bool mainProjectCreatedHere = false;
            bool subProjectCreatedHere = false;
            try
            {
                var match = await MatchRowAsync(row, cancellationToken);
                int subProjectId;

                if (match.SubProjectId.HasValue)
                {
                    // Re-importing a file whose rows are already approved (e.g. running the same
                    // approved plan a second time) should be a no-op for those rows, not a hard
                    // failure - ApproveAsync itself rejects re-approving an already-approved
                    // sub-project, and previously nothing here checked for that before calling it.
                    if (!match.IsApproved)
                    {
                        await _subProjectService.ApproveAsync(match.SubProjectId.Value, new ApproveSubProjectDto { Code = row.SubProjectCode.Trim() }, cancellationToken);
                        result.SubProjectsApproved++;
                    }
                    else
                    {
                        result.SubProjectsAlreadyLinked++;
                    }
                    subProjectId = match.SubProjectId.Value;
                    await EnsureMainProjectApprovedAsync(match.MainProjectId!.Value, row.MainProjectCode, cancellationToken);
                }
                else if (rowResolutionByIndex.TryGetValue(row.RowIndex, out var resolution) && !resolution.CreateNew && resolution.ExistingSubProjectId.HasValue)
                {
                    var existingSubProject = await _subProjectRepository.GetByIdAsync(resolution.ExistingSubProjectId.Value, cancellationToken)
                        ?? throw new NotFoundException($"المشروع الفرعي رقم {resolution.ExistingSubProjectId.Value} غير موجود");
                    if (!existingSubProject.IsApproved)
                    {
                        await _subProjectService.ApproveAsync(resolution.ExistingSubProjectId.Value, new ApproveSubProjectDto { Code = row.SubProjectCode.Trim() }, cancellationToken);
                        result.SubProjectsApproved++;
                    }
                    else
                    {
                        result.SubProjectsAlreadyLinked++;
                    }
                    subProjectId = resolution.ExistingSubProjectId.Value;
                    await EnsureMainProjectApprovedAsync(existingSubProject.MainProjectId, row.MainProjectCode, cancellationToken);
                }
                else if (rowResolutionByIndex.TryGetValue(row.RowIndex, out var createResolution) && createResolution.CreateNew)
                {
                    var mainProjects = await _mainProjectRepository.FindByNameAsync(row.MainProjectName, cancellationToken);
                    // Only reuse an existing MainProject when the name match is unambiguous.
                    // If it matched multiple (ambiguous), MatchRowAsync already correctly rejected
                    // this row as unresolved; since staff explicitly chose "create new" anyway,
                    // create a genuinely new MainProject instead of guessing which one they meant.
                    mainProject = mainProjects.Count == 1 ? mainProjects[0] : null;
                    if (mainProject == null)
                    {
                        mainProject = new MainProject
                        {
                            MainProjectName = row.MainProjectName.Trim(),
                            MainProjectCode = string.IsNullOrWhiteSpace(row.MainProjectCode) ? null : row.MainProjectCode.Trim(),
                            ExecutingAgency = string.Empty,
                            SubProgramId = subProgramIdByName.TryGetValue(ArabicTextNormalizer.Normalize(row.SubProgramName), out var resolvedSubProgramId)
                                ? resolvedSubProgramId
                                : fallbackSubProgramId,
                            IsApproved = true,
                        };
                        mainProjectIsNew = true;
                        await _mainProjectRepository.AddAsync(mainProject, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        mainProjectCreatedHere = true;
                        result.MainProjectsCreated++;
                    }
                    else
                    {
                        // Reused an existing MainProject (e.g. created earlier by a suggested-mode
                        // import) by exact name match - it still needs approving and coding here,
                        // same as the brand-new-MainProject branch above already does at creation.
                        await EnsureMainProjectApprovedAsync(mainProject.MainProjectId, row.MainProjectCode, cancellationToken);
                    }

                    var resolvedProjectLevelId = await ResolveOrCreateProjectLevelIdAsync(row.ProjectLevelName, projectLevelIdByName, unspecifiedProjectLevelId, cancellationToken);
                    var resolvedComponentTypeId = await ResolveOrCreateComponentTypeIdAsync(row.ComponentTypeName, componentTypeIdByName, unspecifiedComponentTypeId, cancellationToken);
                    var resolvedAccountingUnitId = await ResolveOrCreateAccountingUnitIdAsync(row.AccountingUnitName, accountingUnitIdByName, unspecifiedAccountingUnitId, cancellationToken);

                    subProject = new SubProject
                    {
                        MainProjectId = mainProject.MainProjectId,
                        SubProjectName = row.SubProjectName.Trim(),
                        SubProjectCode = row.SubProjectCode.Trim(),
                        IsApproved = true,
                        ApprovedAt = approvalDate,
                        ProjectLevelId = resolvedProjectLevelId,
                        ComponentTypeId = resolvedComponentTypeId,
                        AccountingUnitId = resolvedAccountingUnitId,
                        ExecutiveAgencyId = agencyIdByName.TryGetValue(ArabicTextNormalizer.Normalize(row.ExecutiveAgencyName), out var resolvedAgencyId) ? resolvedAgencyId : null,
                        ProjectNature = row.ProjectNature,
                        MarkazId = markazIdByName.TryGetValue(ArabicTextNormalizer.Normalize(row.MarkazName), out var resolvedMarkazId) ? resolvedMarkazId : fallbackMarkazId,
                        PriorityId = defaultPriorityId,
                        StatusId = runningStatusId,
                        BankFunding = row.BankFunding,
                        SelfFunding = row.SelfFunding,
                    };
                    await _subProjectRepository.AddAsync(subProject, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    subProjectCreatedHere = true;
                    subProjectId = subProject.SubProjectId;
                    result.SubProjectsCreatedAndApproved++;
                }
                else
                {
                    throw new BusinessRuleException("الصف غير محلول ولم يتم تحديد إجراء له");
                }

                // Match/reuse in this file is by name only, not scoped to the financial year being
                // imported (a real project can legitimately span multiple years' plans) - so a
                // matched or reused sub-project isn't automatically linked to THIS year. Without
                // this, an approved import into a new financial year could report success while
                // linking nothing that year's Projects page can actually find.
                try
                {
                    var alreadyLinkedToYear = await _financialYearLinkRepository.FindAsync(
                        x => x.SubProjectId == subProjectId && x.FinancialYearId == dto.FinancialYearId, cancellationToken);
                    if (alreadyLinkedToYear.Count == 0)
                    {
                        await _financialYearLinkRepository.AddAsync(
                            new SubProjectFinancialYear { SubProjectId = subProjectId, FinancialYearId = dto.FinancialYearId },
                            cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                }
                catch (Exception linkEx)
                {
                    result.Failed.Add(new ImportRowFailureDto { Name = row.SubProjectName, Reason = $"تم حفظ المشروع الفرعي بنجاح، لكن تعذّر ربطه بالسنة المالية: {linkEx.Message}" });
                }

                var measurementResolution = dto.MeasurementResolutions.FirstOrDefault(m => m.RowIndex == row.RowIndex);
                if (measurementResolution != null)
                {
                    try
                    {
                        var subProgramId = mainProject?.SubProgramId
                            ?? (await _mainProjectRepository.GetByIdAsync((await _subProjectRepository.GetByIdAsync(subProjectId, cancellationToken))!.MainProjectId, cancellationToken))!.SubProgramId;
                        await _measurementResolutionService.RecordMeasurementsAsync(subProjectId, subProgramId, measurementResolution.Measurements, cancellationToken);
                    }
                    catch (Exception measurementEx)
                    {
                        result.Failed.Add(new ImportRowFailureDto { Name = row.SubProjectName, Reason = $"تم حفظ المشروع الفرعي بنجاح، لكن تعذّر تسجيل القياسات: {measurementEx.Message}" });
                    }
                }

                approvedSubProjectIds.Add(subProjectId);
            }
            catch (Exception ex)
            {
                result.Failed.Add(new ImportRowFailureDto { Name = row.SubProjectName, Reason = ex.Message });

                // A failed SaveChangesAsync leaves the entity tracked as Added; if we don't detach it
                // here, every later SaveChangesAsync retries that same doomed INSERT and mislabels
                // the next row as failed for the same reason. Detach only what THIS attempt both
                // instantiated AND failed to persist: a MainProject reused from an existing row
                // (Count == 1 match) is tracked as Unchanged, and one that actually saved is real
                // data - turning either into a DELETE would destroy a valid, unrelated row.
                if (mainProject is not null && mainProjectIsNew && !mainProjectCreatedHere)
                {
                    _mainProjectRepository.Remove(mainProject);
                }

                if (subProject is not null && !subProjectCreatedHere)
                {
                    _subProjectRepository.Remove(subProject);
                }
            }
        }

        Plan? resultPlan;
        try
        {
            var approvedPlan = _planRepo.GetByFinancialYearAndStatus(dto.FinancialYearId, PlanStatus.Approved);
            var suggestedPlan = approvedPlan == null ? _planRepo.GetByFinancialYearAndStatus(dto.FinancialYearId, PlanStatus.Suggested) : null;

            if (approvedPlan != null)
            {
                // Already approved for this financial year - reuse it rather than creating a duplicate.
                resultPlan = approvedPlan;
            }
            else
            {
                resultPlan = suggestedPlan ?? new Plan
                {
                    PlanName = $"الخطة المقترحة – {financialYear.Name}",
                    PlanStatus = PlanStatus.Suggested,
                    StartDate = financialYear.StartDate,
                    EndDate = financialYear.EndDate,
                    FinancialYearId = dto.FinancialYearId,
                    SuggestionDate = DateTime.UtcNow,
                };

                if (suggestedPlan == null)
                {
                    await _planRepo.AddAsync(resultPlan, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            var alreadyLinked = (await _planProjectRepository.FindAsync(x => x.PlanId == resultPlan.PlanId, cancellationToken))
                .Select(x => x.SubProjectId).ToHashSet();
            foreach (var subProjectId in approvedSubProjectIds.Where(id => !alreadyLinked.Contains(id)))
            {
                await _planProjectRepository.AddAsync(new PlanProject { PlanId = resultPlan.PlanId, SubProjectId = subProjectId }, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (approvedPlan == null)
            {
                resultPlan = await _planService.ApproveAsync(resultPlan.PlanId, approvalDate);
            }
        }
        catch (Exception ex)
        {
            // The per-row loop above has already committed and approved sub-projects - that work is
            // real and must not be thrown away just because the plan-level step failed afterward.
            // Report it as a failure entry instead of letting an exception hide successful rows.
            result.Failed.Add(new ImportRowFailureDto
            {
                Name = "-",
                Reason = $"تم اعتماد المشروعات الفرعية بنجاح، لكن تعذّر تحديث حالة الخطة: {ex.Message}",
            });
            return result;
        }

        result.PlanId = resultPlan.PlanId;
        result.PlanName = resultPlan.PlanName;
        result.PlanStatus = resultPlan.PlanStatus.ToString();
        return result;
    }

    // Approving a row's sub-project (whether by exact match, resolved-to-existing, or reusing an
    // existing MainProject under "create new sub-project") never implied approving its PARENT
    // MainProject too - a suggested-mode import always creates the MainProject with IsApproved
    // false and no code, and nothing here ever updated that, so the sub-project would show its
    // real code while the main project stayed stuck on "مقترح" forever. Every branch that links
    // to an existing MainProject during an approved-mode commit must go through this.
    private async Task EnsureMainProjectApprovedAsync(int mainProjectId, string mainProjectCode, CancellationToken cancellationToken)
    {
        var mainProject = await _mainProjectRepository.GetByIdAsync(mainProjectId, cancellationToken)
            ?? throw new NotFoundException($"المشروع الرئيسي رقم {mainProjectId} غير موجود");

        var trimmedCode = mainProjectCode.Trim();
        var changed = false;

        if (!mainProject.IsApproved)
        {
            mainProject.IsApproved = true;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(trimmedCode) && mainProject.MainProjectCode != trimmedCode)
        {
            mainProject.MainProjectCode = trimmedCode;
            changed = true;
        }

        if (changed)
        {
            _mainProjectRepository.Update(mainProject);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>يضمن وجود سجل «غير محدد» في مستويات المشروع، وينشئه إن لم يوجد بدل رفض الاستيراد كله.</summary>
    private async Task<int> GetOrCreateUnspecifiedProjectLevelIdAsync(CancellationToken cancellationToken)
    {
        var existing = await _projectLevelRepository.FirstOrDefaultAsync(x => x.Name == "غير محدد", cancellationToken);
        if (existing != null)
        {
            return existing.Id;
        }

        var created = new ProjectLevel { Name = "غير محدد" };
        await _projectLevelRepository.AddAsync(created, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created.Id;
    }

    /// <summary>يضمن وجود سجل «غير محدد» في المكوّنات العينية، وينشئه إن لم يوجد بدل رفض الاستيراد كله.</summary>
    private async Task<int> GetOrCreateUnspecifiedComponentTypeIdAsync(CancellationToken cancellationToken)
    {
        var existing = await _componentTypeRepository.FirstOrDefaultAsync(x => x.Name == "غير محدد", cancellationToken);
        if (existing != null)
        {
            return existing.Id;
        }

        var created = new ComponentType { Name = "غير محدد" };
        await _componentTypeRepository.AddAsync(created, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created.Id;
    }

    /// <summary>يضمن وجود سجل «غير محدد» في الوحدات الحسابية، وينشئه إن لم يوجد بدل رفض الاستيراد كله.</summary>
    private async Task<int> GetOrCreateUnspecifiedAccountingUnitIdAsync(CancellationToken cancellationToken)
    {
        var existing = await _accountingUnitRepository.FirstOrDefaultAsync(x => x.Name == "غير محدد", cancellationToken);
        if (existing != null)
        {
            return existing.Id;
        }

        var created = new AccountingUnit { Name = "غير محدد" };
        await _accountingUnitRepository.AddAsync(created, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created.Id;
    }

    /// <summary>
    /// يبحث عن مستوى مشروع بنفس الاسم، وإن لم يوجد ينشئه بدلًا من الرجوع لـ«غير محدد» -
    /// القيمة في الملف حقيقية، فليست حالة تستحق الإسقاط الصامت.
    /// </summary>
    private async Task<int> ResolveOrCreateProjectLevelIdAsync(string rawName, Dictionary<string, int> idByName, int fallbackId, CancellationToken cancellationToken)
    {
        var name = rawName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return fallbackId;
        }
        var normalized = ArabicTextNormalizer.Normalize(name);
        if (idByName.TryGetValue(normalized, out var existingId))
        {
            return existingId;
        }

        var created = new ProjectLevel { Name = name };
        await _projectLevelRepository.AddAsync(created, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        idByName[normalized] = created.Id;
        return created.Id;
    }

    private async Task<int> ResolveOrCreateComponentTypeIdAsync(string rawName, Dictionary<string, int> idByName, int fallbackId, CancellationToken cancellationToken)
    {
        var name = rawName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return fallbackId;
        }
        var normalized = ArabicTextNormalizer.Normalize(name);
        if (idByName.TryGetValue(normalized, out var existingId))
        {
            return existingId;
        }

        var created = new ComponentType { Name = name };
        await _componentTypeRepository.AddAsync(created, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        idByName[normalized] = created.Id;
        return created.Id;
    }

    private async Task<int> ResolveOrCreateAccountingUnitIdAsync(string rawName, Dictionary<string, int> idByName, int fallbackId, CancellationToken cancellationToken)
    {
        var name = rawName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return fallbackId;
        }
        var normalized = ArabicTextNormalizer.Normalize(name);
        if (idByName.TryGetValue(normalized, out var existingId))
        {
            return existingId;
        }

        var created = new AccountingUnit { Name = name };
        await _accountingUnitRepository.AddAsync(created, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        idByName[normalized] = created.Id;
        return created.Id;
    }

    private async Task<MatchResult> MatchRowAsync(ParsedImportRow row, CancellationToken cancellationToken)
    {
        var mainProjects = await _mainProjectRepository.FindByNameAsync(row.MainProjectName, cancellationToken);
        if (mainProjects.Count != 1)
        {
            return new MatchResult(null, null, false);
        }

        var subProjects = await _subProjectRepository.FindByNameWithinMainProjectAsync(row.SubProjectName, mainProjects[0].MainProjectId, cancellationToken);
        if (subProjects.Count != 1)
        {
            return new MatchResult(null, mainProjects[0].MainProjectId, false);
        }

        return new MatchResult(subProjects[0].SubProjectId, mainProjects[0].MainProjectId, subProjects[0].IsApproved);
    }
}
