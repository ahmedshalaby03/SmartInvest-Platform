using SmartInvest.Application.Common;
using SmartInvest.Application.Common.Exceptions;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Enums;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services.Import;

public class SuggestedPlanImportService
{
    private readonly IGenericRepository<Markaz> _markazRepository;
    private readonly IGenericRepository<Governorate> _governorateRepository;
    private readonly IGenericRepository<MainProgram> _mainProgramRepository;
    private readonly IGenericRepository<SubProgram> _subProgramRepository;
    private readonly IGenericRepository<ExecutiveAgency> _agencyRepository;
    private readonly IGenericRepository<ProjectLevel> _projectLevelRepository;
    private readonly IGenericRepository<ComponentType> _componentTypeRepository;
    private readonly IGenericRepository<AccountingUnit> _accountingUnitRepository;
    private readonly IGenericRepository<ProjectPriority> _priorityRepository;
    private readonly IGenericRepository<ProjectStatus> _statusRepository;
    private readonly IGenericRepository<FinancialYear> _financialYearRepository;
    private readonly IGenericRepository<PlanProject> _planProjectRepository;
    private readonly IGenericRepository<SubProjectFinancialYear> _financialYearLinkRepository;
    private readonly ILookupService _lookupService;
    private readonly IExecutiveAgencyService _agencyService;
    private readonly IMainProjectRepository _mainProjectRepository;
    private readonly ISubProjectRepository _subProjectRepository;
    private readonly IPlanRepo _planRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMeasurementResolutionService _measurementResolutionService;
    private readonly ILookupMatchSuggestionService _lookupMatchSuggestionService;

    public SuggestedPlanImportService(
        IGenericRepository<Markaz> markazRepository,
        IGenericRepository<Governorate> governorateRepository,
        IGenericRepository<MainProgram> mainProgramRepository,
        IGenericRepository<SubProgram> subProgramRepository,
        IGenericRepository<ExecutiveAgency> agencyRepository,
        IGenericRepository<ProjectLevel> projectLevelRepository,
        IGenericRepository<ComponentType> componentTypeRepository,
        IGenericRepository<AccountingUnit> accountingUnitRepository,
        IGenericRepository<ProjectPriority> priorityRepository,
        IGenericRepository<ProjectStatus> statusRepository,
        IGenericRepository<FinancialYear> financialYearRepository,
        IGenericRepository<PlanProject> planProjectRepository,
        IGenericRepository<SubProjectFinancialYear> financialYearLinkRepository,
        ILookupService lookupService,
        IExecutiveAgencyService agencyService,
        IMainProjectRepository mainProjectRepository,
        ISubProjectRepository subProjectRepository,
        IPlanRepo planRepo,
        IUnitOfWork unitOfWork,
        IMeasurementResolutionService measurementResolutionService,
        ILookupMatchSuggestionService lookupMatchSuggestionService)
    {
        _markazRepository = markazRepository;
        _governorateRepository = governorateRepository;
        _mainProgramRepository = mainProgramRepository;
        _subProgramRepository = subProgramRepository;
        _agencyRepository = agencyRepository;
        _projectLevelRepository = projectLevelRepository;
        _componentTypeRepository = componentTypeRepository;
        _accountingUnitRepository = accountingUnitRepository;
        _priorityRepository = priorityRepository;
        _statusRepository = statusRepository;
        _financialYearRepository = financialYearRepository;
        _planProjectRepository = planProjectRepository;
        _financialYearLinkRepository = financialYearLinkRepository;
        _lookupService = lookupService;
        _agencyService = agencyService;
        _mainProjectRepository = mainProjectRepository;
        _subProjectRepository = subProjectRepository;
        _planRepo = planRepo;
        _unitOfWork = unitOfWork;
        _measurementResolutionService = measurementResolutionService;
        _lookupMatchSuggestionService = lookupMatchSuggestionService;
    }

    public async Task<SuggestedImportPreviewDto> PreviewAsync(ParsedImportFile file, CancellationToken cancellationToken)
    {
        var markazNames = (await _markazRepository.GetAllAsync(cancellationToken)).Select(x => x.MarkazName).ToHashSet();
        var mainProgramNames = (await _mainProgramRepository.GetAllAsync(cancellationToken)).Select(x => x.ProgramName).ToHashSet();
        var subProgramNames = (await _subProgramRepository.GetAllAsync(cancellationToken)).Select(x => x.SubProgramName).ToHashSet();
        var agencyNames = (await _agencyRepository.GetAllAsync(cancellationToken)).Select(x => x.AgencyName).ToHashSet();
        var projectLevelNames = (await _projectLevelRepository.GetAllAsync(cancellationToken)).Select(x => x.Name).ToHashSet();
        var componentTypeNames = (await _componentTypeRepository.GetAllAsync(cancellationToken)).Select(x => x.Name).ToHashSet();
        var accountingUnitNames = (await _accountingUnitRepository.GetAllAsync(cancellationToken)).Select(x => x.Name).ToHashSet();

        var dto = new SuggestedImportPreviewDto
        {
            UnresolvedMarkaz = Unresolved(file.Rows, r => r.MarkazName, markazNames),
            UnresolvedMainPrograms = Unresolved(file.Rows, r => r.MainProgramName, mainProgramNames),
            UnresolvedSubPrograms = Unresolved(file.Rows, r => r.SubProgramName, subProgramNames),
            UnresolvedAgencies = Unresolved(file.Rows, r => r.ExecutiveAgencyName, agencyNames),
            UnresolvedProjectLevels = Unresolved(file.Rows, r => r.ProjectLevelName, projectLevelNames),
            UnresolvedComponentTypes = Unresolved(file.Rows, r => r.ComponentTypeName, componentTypeNames),
            UnresolvedAccountingUnits = Unresolved(file.Rows, r => r.AccountingUnitName, accountingUnitNames),
            MainProjectCodeConflicts = DetectCodeConflicts(file.Rows),
        };

        var mainProjectGroups = GroupByMainProject(file.Rows, new List<MainProjectCodeResolutionDto>());
        dto.MainProjectCount = mainProjectGroups.Count;
        dto.SubProjectCount = file.Rows.Count;

        await ApplyMatchSuggestionsAsync(
            dto, markazNames, mainProgramNames, subProgramNames, agencyNames,
            projectLevelNames, componentTypeNames, accountingUnitNames, cancellationToken);

        return dto;
    }

    private async Task ApplyMatchSuggestionsAsync(
        SuggestedImportPreviewDto dto,
        HashSet<string> markazNames,
        HashSet<string> mainProgramNames,
        HashSet<string> subProgramNames,
        HashSet<string> agencyNames,
        HashSet<string> projectLevelNames,
        HashSet<string> componentTypeNames,
        HashSet<string> accountingUnitNames,
        CancellationToken cancellationToken)
    {
        var categories = new List<LookupMatchCategory>
        {
            new("markaz", dto.UnresolvedMarkaz.Select(u => u.Name).ToList(), markazNames.ToList()),
            new("mainProgram", dto.UnresolvedMainPrograms.Select(u => u.Name).ToList(), mainProgramNames.ToList()),
            new("subProgram", dto.UnresolvedSubPrograms.Select(u => u.Name).ToList(), subProgramNames.ToList()),
            new("agency", dto.UnresolvedAgencies.Select(u => u.Name).ToList(), agencyNames.ToList()),
            new("projectLevel", dto.UnresolvedProjectLevels.Select(u => u.Name).ToList(), projectLevelNames.ToList()),
            new("componentType", dto.UnresolvedComponentTypes.Select(u => u.Name).ToList(), componentTypeNames.ToList()),
            new("accountingUnit", dto.UnresolvedAccountingUnits.Select(u => u.Name).ToList(), accountingUnitNames.ToList()),
        };

        var suggestions = await _lookupMatchSuggestionService.SuggestMatchesAsync(categories, cancellationToken);

        void Apply(string categoryKey, List<UnresolvedNameDto> items)
        {
            if (!suggestions.TryGetValue(categoryKey, out var matches))
            {
                return;
            }

            foreach (var item in items)
            {
                if (matches.TryGetValue(item.Name, out var suggested))
                {
                    item.SuggestedMatch = suggested;
                }
            }
        }

        Apply("markaz", dto.UnresolvedMarkaz);
        Apply("mainProgram", dto.UnresolvedMainPrograms);
        Apply("subProgram", dto.UnresolvedSubPrograms);
        Apply("agency", dto.UnresolvedAgencies);
        Apply("projectLevel", dto.UnresolvedProjectLevels);
        Apply("componentType", dto.UnresolvedComponentTypes);
        Apply("accountingUnit", dto.UnresolvedAccountingUnits);
    }

    public async Task<ImportCommitResultDto> CommitAsync(ParsedImportFile file, ImportCommitDto dto, CancellationToken cancellationToken)
    {
        var financialYear = await _financialYearRepository.GetByIdAsync(dto.FinancialYearId, cancellationToken)
            ?? throw new NotFoundException($"السنة المالية رقم {dto.FinancialYearId} غير موجودة");

        var markazIdByName = await ResolveMarkazAsync(dto.MarkazResolutions, cancellationToken);
        var mainProgramIdByName = await ResolveNamedLookupAsync(
            dto.MainProgramResolutions, _mainProgramRepository, x => x.ProgramName, x => x.ProgramId,
            async name => (await _lookupService.CreateMainProgramAsync(new CreateNamedLookupDto { Name = name }, cancellationToken)).Id,
            cancellationToken);
        var mainProjectGroups = GroupByMainProject(file.Rows, dto.MainProjectCodeResolutions);
        var neededSubProgramPairs = mainProjectGroups
            .Where(g => mainProgramIdByName.ContainsKey(ArabicTextNormalizer.Normalize(g.MainProgramName)))
            .Select(g => (MainProgramId: mainProgramIdByName[ArabicTextNormalizer.Normalize(g.MainProgramName)], SubProgramName: g.Rows[0].SubProgramName.Trim()))
            .Distinct()
            .ToList();
        var subProgramIdByName = await ResolveSubProgramAsync(dto.SubProgramResolutions, neededSubProgramPairs, cancellationToken);
        var agencyIdByName = await ResolveNamedLookupAsync(
            dto.AgencyResolutions, _agencyRepository, x => x.AgencyName, x => x.ExecutiveAgencyId,
            async name => (await _agencyService.CreateAsync(new CreateExecutiveAgencyDto { AgencyName = name, Phone = string.Empty, Email = string.Empty, Address = string.Empty }, cancellationToken)).Id,
            cancellationToken);
        var projectLevelIdByName = await ResolveNamedLookupAsync(
            dto.ProjectLevelResolutions, _projectLevelRepository, x => x.Name, x => x.Id,
            async name => (await _lookupService.CreateProjectLevelAsync(new CreateNamedLookupDto { Name = name }, cancellationToken)).Id,
            cancellationToken);
        var componentTypeIdByName = await ResolveNamedLookupAsync(
            dto.ComponentTypeResolutions, _componentTypeRepository, x => x.Name, x => x.Id,
            async name => (await _lookupService.CreateComponentTypeAsync(new CreateNamedLookupDto { Name = name }, cancellationToken)).Id,
            cancellationToken);
        var accountingUnitIdByName = await ResolveNamedLookupAsync(
            dto.AccountingUnitResolutions, _accountingUnitRepository, x => x.Name, x => x.Id,
            async name => (await _lookupService.CreateAccountingUnitAsync(new CreateNamedLookupDto { Name = name }, cancellationToken)).Id,
            cancellationToken);

        var defaultPriorityId = (await _priorityRepository.FirstOrDefaultAsync(x => x.Priority == "منخفضة", cancellationToken))?.Id
            ?? throw new BusinessRuleException("أولوية «منخفضة» الافتراضية غير موجودة في قاعدة البيانات");
        var defaultStatusId = (await _statusRepository.FirstOrDefaultAsync(x => x.StatusName == "جديد", cancellationToken))?.StatusId
            ?? throw new BusinessRuleException("حالة «جديد» الافتراضية غير موجودة في قاعدة البيانات");

        var result = new ImportCommitResultDto { Mode = "Suggested" };
        var createdSubProjects = new List<SubProject>();

        foreach (var group in mainProjectGroups)
        {
            if (!mainProgramIdByName.TryGetValue(ArabicTextNormalizer.Normalize(group.MainProgramName), out var mainProgramId))
            {
                foreach (var row in group.Rows)
                {
                    result.Failed.Add(new ImportRowFailureDto { Name = row.SubProjectName, Reason = $"البرنامج الرئيسي «{row.MainProgramName}» غير محلول" });
                }
                continue;
            }

            var subProgramName = group.Rows[0].SubProgramName.Trim();
            var normalizedSubProgramName = ArabicTextNormalizer.Normalize(subProgramName);
            if (!subProgramIdByName.TryGetValue((mainProgramId, normalizedSubProgramName), out var subProgramId))
            {
                // The name may already exist under a different main program - sub-program names
                // aren't globally unique, but the preview's "unresolved names" check only flags a
                // name that doesn't exist ANYWHERE, so this case silently passes reconciliation
                // and only surfaces here. Create it under the correct parent instead of failing
                // rows the user already reconciled successfully.
                var createdSubProgram = await _lookupService.CreateSubProgramAsync(
                    new CreateSubProgramDto { Name = subProgramName, MainProgramId = mainProgramId }, cancellationToken);
                subProgramId = createdSubProgram.Id;
                subProgramIdByName[(mainProgramId, normalizedSubProgramName)] = subProgramId;
            }

            MainProject? mainProject = null;
            var mainProjectIsNew = false;
            var mainProjectCreatedHere = false;
            try
            {
                // A suggested-mode file re-imported (whole or in part - e.g. staff re-uploads the
                // same plan after fixing one row) must not spawn a second "الإدارة المحلية..." main
                // project identical to one already sitting in the DB from the first import. Reuse
                // it by exact name when unambiguous, same matching ApprovedPlanImportService already
                // does for its own "create new" branch.
                var existingMainProjects = await _mainProjectRepository.FindByNameAsync(group.MainProjectName.Trim(), cancellationToken);
                mainProject = existingMainProjects.Count == 1 ? existingMainProjects[0] : null;

                if (mainProject == null)
                {
                    mainProject = new MainProject
                    {
                        MainProjectCode = string.IsNullOrWhiteSpace(group.Code) ? null : group.Code,
                        MainProjectName = group.MainProjectName,
                        ExecutingAgency = string.Empty,
                        SubProgramId = subProgramId,
                        IsApproved = false,
                    };

                    mainProjectIsNew = true;
                    await _mainProjectRepository.AddAsync(mainProject, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    mainProjectCreatedHere = true;
                    result.MainProjectsCreated++;
                }
                else
                {
                    // نفس فكرة SubProjectsAlreadyLinked أدناه — بدونها MainProjectsCreated قد يكون
                    // صفرًا بلا أي تفسير في واجهة النتيجة إذا كان كل شيء في الملف موجودًا بالفعل.
                    result.MainProjectsAlreadyExisted++;
                }
            }
            catch (Exception ex)
            {
                foreach (var row in group.Rows)
                {
                    result.Failed.Add(new ImportRowFailureDto { Name = row.SubProjectName, Reason = ex.Message });
                }

                // A failed SaveChangesAsync leaves the entity tracked as Added; if we don't detach it
                // here, every later SaveChangesAsync (including the plan-level one at the end of this
                // method) retries that same doomed INSERT, so one bad group poisons the whole commit.
                // Detach only an entity THIS attempt instantiated and failed to persist: a reused
                // existing MainProject is tracked as Unchanged, and one that was actually saved is
                // real data - neither may be turned into a DELETE just because something else failed.
                if (mainProject is not null && mainProjectIsNew && !mainProjectCreatedHere)
                {
                    _mainProjectRepository.Remove(mainProject);
                }

                continue;
            }

            foreach (var row in group.Rows)
            {
                SubProject? subProject = null;
                var subProjectIsNew = false;
                var subProjectCreatedHere = false;
                try
                {
                    if (!markazIdByName.TryGetValue(ArabicTextNormalizer.Normalize(row.MarkazName), out var markazId))
                    {
                        throw new BusinessRuleException($"المركز «{row.MarkazName}» غير محلول");
                    }

                    if (!agencyIdByName.TryGetValue(ArabicTextNormalizer.Normalize(row.ExecutiveAgencyName), out var agencyId))
                    {
                        throw new BusinessRuleException($"الجهة المنفذة «{row.ExecutiveAgencyName}» غير محلولة");
                    }

                    if (!projectLevelIdByName.TryGetValue(ArabicTextNormalizer.Normalize(row.ProjectLevelName), out var projectLevelId))
                    {
                        throw new BusinessRuleException($"مستوى المشروع «{row.ProjectLevelName}» غير محلول");
                    }

                    if (!componentTypeIdByName.TryGetValue(ArabicTextNormalizer.Normalize(row.ComponentTypeName), out var componentTypeId))
                    {
                        throw new BusinessRuleException($"المكوّن العيني «{row.ComponentTypeName}» غير محلول");
                    }

                    if (!accountingUnitIdByName.TryGetValue(ArabicTextNormalizer.Normalize(row.AccountingUnitName), out var accountingUnitId))
                    {
                        throw new BusinessRuleException($"الوحدة الحسابية «{row.AccountingUnitName}» غير محلولة");
                    }

                    // Same duplicate guard as the main-project reuse above, one level down - a
                    // sub-project with this exact name already under this main project is reused
                    // instead of duplicated. Prefer the one already linked to THIS financial year
                    // (a re-upload/correction of the same year's plan), but fall back to any
                    // same-named sibling: (MainProjectId, SubProjectName) carries a unique index,
                    // so a second row under the same main project is not something the database
                    // will accept - importing the same plan into a NEW financial year has to reuse
                    // the existing row and add a year link to it, which is what the block below
                    // already does. The sub-project is then shared across both years' plans.
                    var candidatesByName = await _subProjectRepository.FindByNameWithinMainProjectAsync(row.SubProjectName.Trim(), mainProject.MainProjectId, cancellationToken);
                    subProject = null;
                    foreach (var candidate in candidatesByName)
                    {
                        var linkedToThisYear = await _financialYearLinkRepository.FirstOrDefaultAsync(
                            x => x.SubProjectId == candidate.SubProjectId && x.FinancialYearId == dto.FinancialYearId, cancellationToken);
                        if (linkedToThisYear != null)
                        {
                            subProject = candidate;
                            break;
                        }
                    }

                    if (subProject == null && candidatesByName.Count > 0)
                    {
                        subProject = candidatesByName[0];
                    }

                    if (subProject == null)
                    {
                        subProject = new SubProject
                        {
                            MainProjectId = mainProject.MainProjectId,
                            SubProjectName = row.SubProjectName.Trim(),
                            SubProjectCode = null,
                            IsApproved = false,
                            ProjectLevelId = projectLevelId,
                            ComponentTypeId = componentTypeId,
                            AccountingUnitId = accountingUnitId,
                            ProjectNature = row.ProjectNature,
                            MarkazId = markazId,
                            PriorityId = defaultPriorityId,
                            StatusId = defaultStatusId,
                            ExecutiveAgencyId = agencyId,
                            BankFunding = row.BankFunding,
                            SelfFunding = row.SelfFunding,
                        };

                        subProjectIsNew = true;
                        await _subProjectRepository.AddAsync(subProject, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        subProjectCreatedHere = true;
                        result.SubProjectsCreated++;
                    }
                    else
                    {
                        result.SubProjectsAlreadyLinked++;
                    }

                    createdSubProjects.Add(subProject);

                    try
                    {
                        var alreadyLinkedToYear = await _financialYearLinkRepository.FindAsync(
                            x => x.SubProjectId == subProject.SubProjectId && x.FinancialYearId == dto.FinancialYearId, cancellationToken);
                        if (alreadyLinkedToYear.Count == 0)
                        {
                            await _financialYearLinkRepository.AddAsync(
                                new SubProjectFinancialYear { SubProjectId = subProject.SubProjectId, FinancialYearId = dto.FinancialYearId },
                                cancellationToken);
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                        }
                    }
                    catch (Exception linkEx)
                    {
                        result.Failed.Add(new ImportRowFailureDto { Name = row.SubProjectName, Reason = $"تم حفظ المشروع الفرعي بنجاح، لكن تعذّر ربطه بالسنة المالية: {linkEx.Message}" });
                    }

                    // Only record AI-extracted measurements for a row that just created its
                    // sub-project - re-running this against a reused/already-existing sub-project
                    // would append duplicate measurement rows on top of whatever it already has.
                    if (subProjectCreatedHere)
                    {
                        var measurementResolution = dto.MeasurementResolutions.FirstOrDefault(m => m.RowIndex == row.RowIndex);
                        if (measurementResolution != null)
                        {
                            try
                            {
                                await _measurementResolutionService.RecordMeasurementsAsync(subProject.SubProjectId, subProgramId, measurementResolution.Measurements, cancellationToken);
                            }
                            catch (Exception measurementEx)
                            {
                                result.Failed.Add(new ImportRowFailureDto { Name = row.SubProjectName, Reason = $"تم حفظ المشروع الفرعي بنجاح، لكن تعذّر تسجيل القياسات: {measurementEx.Message}" });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Failed.Add(new ImportRowFailureDto { Name = row.SubProjectName, Reason = ex.Message });

                    // A failed SaveChangesAsync leaves the entity tracked as Added; if we don't detach it
                    // here, every later SaveChangesAsync (including the plan-level one at the end of this
                    // method) retries that same doomed INSERT, so one bad row poisons the whole commit.
                    // Detach only an entity THIS attempt instantiated and failed to persist: a reused
                    // existing sub-project is tracked as Unchanged, and one that was actually saved is
                    // real data - neither may be turned into a DELETE just because a later step failed.
                    if (subProject is not null && subProjectIsNew && !subProjectCreatedHere)
                    {
                        _subProjectRepository.Remove(subProject);
                    }
                }
            }
        }

        // Everything above already committed its own rows; an exception escaping this plan-level
        // step would surface as a bare 500 ("حدث خطأ غير متوقع") and hide work that really happened,
        // so report it as a failure entry instead - same as ApprovedPlanImportService does.
        try
        {
            var plan = _planRepo.GetByFinancialYearAndStatus(dto.FinancialYearId, PlanStatus.Suggested);
            if (plan == null)
            {
                plan = new Plan
                {
                    PlanName = $"الخطة المقترحة – {financialYear.Name}",
                    PlanStatus = PlanStatus.Suggested,
                    StartDate = financialYear.StartDate,
                    EndDate = financialYear.EndDate,
                    FinancialYearId = dto.FinancialYearId,
                    SuggestionDate = DateTime.UtcNow,
                };
                await _planRepo.AddAsync(plan, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // createdSubProjects now also holds reused (not just newly-created) sub-projects - a reused
            // one may already sit on this exact plan from the import that first created it, so guard
            // against a duplicate PlanProject row the same way ApprovedPlanImportService does. Two rows
            // of the same file can also land on one reused sub-project, so de-duplicate within this
            // batch too - (PlanId, SubProjectId) is a unique index.
            var alreadyLinkedToPlan = (await _planProjectRepository.FindAsync(x => x.PlanId == plan.PlanId, cancellationToken))
                .Select(x => x.SubProjectId).ToHashSet();
            foreach (var subProjectId in createdSubProjects.Select(sp => sp.SubProjectId).Distinct().Where(id => !alreadyLinkedToPlan.Contains(id)))
            {
                await _planProjectRepository.AddAsync(new PlanProject { PlanId = plan.PlanId, SubProjectId = subProjectId }, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            result.PlanId = plan.PlanId;
            result.PlanName = plan.PlanName;
            result.PlanStatus = plan.PlanStatus.ToString();
        }
        catch (Exception ex)
        {
            result.Failed.Add(new ImportRowFailureDto
            {
                Name = "-",
                Reason = $"تم حفظ المشروعات الفرعية بنجاح، لكن تعذّر تحديث الخطة: {ex.Message}",
            });
        }

        return result;
    }

    private static List<UnresolvedNameDto> Unresolved(List<ParsedImportRow> rows, Func<ParsedImportRow, string> selector, HashSet<string> existingNames)
    {
        // Diacritic/whitespace-normalized comparison - a name that already exists but differs from
        // the file's spelling by a missing harakah or an extra internal space must not be flagged
        // as unresolved (and, if left on "create new" during reconciliation, silently duplicated).
        var normalizedExisting = existingNames
            .Select(ArabicTextNormalizer.Normalize)
            .ToHashSet();

        return rows
            .Select(r => selector(r).Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name) && !normalizedExisting.Contains(ArabicTextNormalizer.Normalize(name)))
            .GroupBy(name => name)
            .Select(g => new UnresolvedNameDto { Name = g.Key, RowCount = g.Count() })
            .ToList();
    }

    private static List<MainProjectCodeConflictDto> DetectCodeConflicts(List<ParsedImportRow> rows)
    {
        return rows
            .Where(r => !string.IsNullOrWhiteSpace(r.MainProjectCode))
            .GroupBy(r => r.MainProjectCode.Trim())
            .Select(g => new
            {
                Code = g.Key,
                Pairs = g.Select(r => (Name: r.MainProjectName.Trim(), Program: r.MainProgramName.Trim())).Distinct().ToList(),
            })
            .Where(x => x.Pairs.Count > 1)
            .Select(x => new MainProjectCodeConflictDto
            {
                Code = x.Code,
                Options = x.Pairs.Select(p => new MainProjectCodeConflictOptionDto { MainProjectName = p.Name, MainProgramName = p.Program }).ToList(),
            })
            .ToList();
    }

    private class MainProjectGroup
    {
        public string Code { get; set; } = string.Empty;
        public string MainProjectName { get; set; } = string.Empty;
        public string MainProgramName { get; set; } = string.Empty;
        public List<ParsedImportRow> Rows { get; set; } = new();
    }

    private static List<MainProjectGroup> GroupByMainProject(
        List<ParsedImportRow> rows,
        List<MainProjectCodeResolutionDto> resolutions)
    {
        var resolutionByCode = resolutions.ToDictionary(r => r.Code.Trim(), r => r);

        string GroupKey(ParsedImportRow row)
        {
            var code = row.MainProjectCode.Trim();
            if (resolutionByCode.TryGetValue(code, out var resolution))
            {
                return $"{code}|{resolution.ChosenMainProjectName.Trim()}";
            }

            return $"{code}|{row.MainProjectName.Trim()}";
        }

        return rows
            .GroupBy(GroupKey)
            .Select(g =>
            {
                var first = g.First();
                var code = first.MainProjectCode.Trim();
                var name = resolutionByCode.TryGetValue(code, out var resolution)
                    ? resolution.ChosenMainProjectName.Trim()
                    : first.MainProjectName.Trim();
                var programName = resolutionByCode.TryGetValue(code, out var programResolution)
                    ? programResolution.ChosenMainProgramName.Trim()
                    : first.MainProgramName.Trim();

                return new MainProjectGroup { Code = code, MainProjectName = name, MainProgramName = programName, Rows = g.ToList() };
            })
            .ToList();
    }

    private async Task<Dictionary<string, int>> ResolveMarkazAsync(List<ImportResolutionDto> resolutions, CancellationToken cancellationToken)
    {
        var governorates = await _governorateRepository.GetAllAsync(cancellationToken);
        if (governorates.Count != 1)
        {
            throw new BusinessRuleException("تعذّر تحديد المحافظة الافتراضية — يجب أن توجد محافظة واحدة بالضبط في النظام");
        }
        var governorateId = governorates[0].GovernorateId;

        var existing = await _markazRepository.GetAllAsync(cancellationToken);
        var result = existing.ToDictionary(x => ArabicTextNormalizer.Normalize(x.MarkazName), x => x.MarkazId);

        foreach (var resolution in resolutions.Where(r => r.CreateNew))
        {
            var name = resolution.Name.Trim();
            var normalized = ArabicTextNormalizer.Normalize(name);
            if (result.ContainsKey(normalized))
            {
                continue;
            }

            var created = await _lookupService.CreateMarkazAsync(new CreateMarkazDto { Name = name, GovernorateId = governorateId }, cancellationToken);
            result[normalized] = created.Id;
        }

        foreach (var resolution in resolutions.Where(r => !r.CreateNew && r.ExistingId.HasValue))
        {
            var match = existing.FirstOrDefault(x => x.MarkazId == resolution.ExistingId!.Value);
            if (match != null)
            {
                result[ArabicTextNormalizer.Normalize(resolution.Name)] = match.MarkazId;
            }
        }

        return result;
    }

    private async Task<Dictionary<(int MainProgramId, string SubProgramName), int>> ResolveSubProgramAsync(
        List<ImportResolutionDto> resolutions, List<(int MainProgramId, string SubProgramName)> neededPairs, CancellationToken cancellationToken)
    {
        var existing = await _subProgramRepository.GetAllAsync(cancellationToken);
        var result = existing.ToDictionary(x => (x.ProgramId, ArabicTextNormalizer.Normalize(x.SubProgramName)), x => x.SubProgramId);
        // neededPairs' SubProgramName values are compared against normalized names below, so
        // normalize once here too rather than re-normalizing inside every loop iteration.
        string NormalizedPairName((int MainProgramId, string SubProgramName) p) => ArabicTextNormalizer.Normalize(p.SubProgramName);

        foreach (var resolution in resolutions.Where(r => r.CreateNew))
        {
            var name = resolution.Name.Trim();
            var normalizedName = ArabicTextNormalizer.Normalize(name);
            // Only create under the main program(s) this import file actually pairs this
            // sub-program name with - not every main program in the database (that used to
            // fan out one duplicate sub-program row per unrelated existing main program).
            foreach (var mainProgramId in neededPairs.Where(p => NormalizedPairName(p) == normalizedName).Select(p => p.MainProgramId).Distinct())
            {
                if (result.ContainsKey((mainProgramId, normalizedName)))
                {
                    continue;
                }
                var created = await _lookupService.CreateSubProgramAsync(new CreateSubProgramDto { Name = name, MainProgramId = mainProgramId }, cancellationToken);
                result[(mainProgramId, normalizedName)] = created.Id;
            }
        }

        foreach (var resolution in resolutions.Where(r => !r.CreateNew && r.ExistingId.HasValue))
        {
            var match = existing.FirstOrDefault(x => x.SubProgramId == resolution.ExistingId!.Value);
            if (match == null)
            {
                continue;
            }
            var normalizedName = ArabicTextNormalizer.Normalize(resolution.Name);
            // Staff explicitly chose to map this unresolved name to an existing sub-program
            // regardless of that sub-program's own parent main program - key the mapping by
            // every main program this file's rows actually need it under (falling back to the
            // match's own parent if the file happens not to reference any group needing it,
            // e.g. all rows for that name were rejected earlier for an unrelated reason).
            var targetProgramIds = neededPairs.Where(p => NormalizedPairName(p) == normalizedName).Select(p => p.MainProgramId).Distinct().ToList();
            if (targetProgramIds.Count == 0)
            {
                targetProgramIds.Add(match.ProgramId);
            }
            foreach (var mainProgramId in targetProgramIds)
            {
                result[(mainProgramId, normalizedName)] = match.SubProgramId;
            }
        }

        return result;
    }

    private static async Task<Dictionary<string, int>> ResolveNamedLookupAsync<T>(
        List<ImportResolutionDto> resolutions,
        IGenericRepository<T> repository,
        Func<T, string> nameSelector,
        Func<T, int> idSelector,
        Func<string, Task<int>> createNew,
        CancellationToken cancellationToken)
        where T : class
    {
        var existing = await repository.GetAllAsync(cancellationToken);
        // Keyed by normalized name so a row-application lookup by the file's own (also normalized)
        // text finds an existing record even when the two spellings differ by diacritics/whitespace.
        var result = new Dictionary<string, int>();
        foreach (var item in existing)
        {
            result[ArabicTextNormalizer.Normalize(nameSelector(item))] = idSelector(item);
        }

        foreach (var resolution in resolutions.Where(r => r.CreateNew))
        {
            var name = resolution.Name.Trim();
            var normalized = ArabicTextNormalizer.Normalize(name);
            if (result.ContainsKey(normalized))
            {
                continue;
            }
            // The newly-created record keeps the user's original spelling (with diacritics) -
            // normalization is for matching only, never for what gets persisted.
            result[normalized] = await createNew(name);
        }

        foreach (var resolution in resolutions.Where(r => !r.CreateNew && r.ExistingId.HasValue))
        {
            var match = existing.FirstOrDefault(x => idSelector(x) == resolution.ExistingId!.Value);
            if (match != null)
            {
                result[ArabicTextNormalizer.Normalize(resolution.Name)] = resolution.ExistingId!.Value;
            }
        }

        return result;
    }
}
