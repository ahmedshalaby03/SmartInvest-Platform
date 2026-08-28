/*
  SmartInvest dashboard/lifecycle demo data for FinancialYearId = 16 (2037/2038).
  - Idempotent: safe to run repeatedly.
  - Additive: does not update or delete existing business rows.
  - Reversible: run rollback-latest-financial-year-lifecycle.sql.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @FinancialYearId int = 16;
DECLARE @ExpectedYearName nvarchar(50) = N'2037/2038';
DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @Pdf varbinary(max) = CONVERT(varbinary(max),
    '%PDF-1.4' + CHAR(10) +
    '1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj' + CHAR(10) +
    '2 0 obj << /Type /Pages /Count 0 >> endobj' + CHAR(10) +
    'trailer << /Root 1 0 R >>' + CHAR(10) +
    '%%EOF');

IF NOT EXISTS (
    SELECT 1 FROM FinancialYears
    WHERE FinancialYearId = @FinancialYearId AND Name = @ExpectedYearName
)
    THROW 51000, N'السنة المالية المستهدفة 2037/2038 غير موجودة.', 1;

IF @FinancialYearId <> (SELECT TOP (1) FinancialYearId FROM FinancialYears ORDER BY StartDate DESC)
    THROW 51001, N'السنة 2037/2038 لم تعد أحدث سنة مالية. راجع السكربت قبل التنفيذ.', 1;

DECLARE @NewStatusId int = (SELECT TOP (1) StatusId FROM ProjectStatus WHERE StatusName = N'جديد');
DECLARE @InProgressStatusId int = (SELECT TOP (1) StatusId FROM ProjectStatus WHERE StatusName = N'قيد التنفيذ');
DECLARE @StalledStatusId int = (SELECT TOP (1) StatusId FROM ProjectStatus WHERE StatusName = N'متعثر');
DECLARE @CompletedStatusId int = (SELECT TOP (1) StatusId FROM ProjectStatus WHERE StatusName = N'منتهي');
DECLARE @HighPriorityId int = (SELECT TOP (1) Id FROM ProjectPriority WHERE Priority = N'عالية');
DECLARE @MediumPriorityId int = (SELECT TOP (1) Id FROM ProjectPriority WHERE Priority = N'متوسطة');

IF @NewStatusId IS NULL OR @InProgressStatusId IS NULL OR @StalledStatusId IS NULL
   OR @CompletedStatusId IS NULL OR @HighPriorityId IS NULL OR @MediumPriorityId IS NULL
    THROW 51002, N'إحدى الحالات أو الأولويات الأساسية غير موجودة.', 1;

BEGIN TRANSACTION;

DECLARE @Definitions table
(
    Code nvarchar(50) NOT NULL PRIMARY KEY,
    ProjectName nvarchar(1000) NOT NULL,
    SourceSubProjectId int NOT NULL,
    IsApproved bit NOT NULL,
    StatusId int NOT NULL,
    PriorityId int NOT NULL,
    BankFunding decimal(18,2) NOT NULL,
    SelfFunding decimal(18,2) NOT NULL,
    ProjectNature nvarchar(100) NOT NULL,
    OverrunPercentage decimal(5,2) NULL,
    ContractValue decimal(18,2) NULL
);

INSERT INTO @Definitions
    (Code, ProjectName, SourceSubProjectId, IsApproved, StatusId, PriorityId,
     BankFunding, SelfFunding, ProjectNature, OverrunPercentage, ContractValue)
VALUES
 (N'LIFE-FY16-01', N'إنشاء مركز خدمات المواطنين المتكامل بمدينة شبين الكوم', 11, 0, @NewStatusId, @HighPriorityId, 18000000, 4000000, N'مقاولات', 10, NULL),
 (N'LIFE-FY16-02', N'تطوير الحديقة المركزية ورفع كفاءة المساحات الخضراء', 50, 0, @NewStatusId, @MediumPriorityId, 6000000, 2000000, N'مقاولات', 10, NULL),
 (N'LIFE-FY16-03', N'تطوير وتجهيز دار مناسبات لخدمة القرى الأكثر احتياجًا', 17, 1, @NewStatusId, @HighPriorityId, 12000000, 3000000, N'مقاولات', 15, NULL),
 (N'LIFE-FY16-04', N'توريد معدات جديدة لمنظومة النظافة بالمراكز والمدن', 8, 1, @NewStatusId, @MediumPriorityId, 22000000, 5000000, N'توريدات', 5, NULL),
 (N'LIFE-FY16-05', N'رصف ورفع كفاءة طريق المدخل الغربي لمدينة الشهداء', 51, 1, @NewStatusId, @MediumPriorityId, 14000000, 2000000, N'مقاولات', 15, NULL),
 (N'LIFE-FY16-06', N'تطوير منظومة الإنارة الذكية بالشوارع الرئيسية', 20, 1, @NewStatusId, @HighPriorityId, 9000000, 1000000, N'توريدات', 10, NULL),
 (N'LIFE-FY16-07', N'رفع كفاءة مبنى الوحدة المحلية وتحسين خدمات الجمهور', 38, 1, @NewStatusId, @MediumPriorityId, 5000000, 2000000, N'مقاولات', 10, NULL),
 (N'LIFE-FY16-08', N'إنشاء سوق حضاري للباعة وتطوير المنطقة المحيطة', 50, 1, @NewStatusId, @HighPriorityId, 10000000, 2000000, N'مقاولات', 15, NULL),
 (N'LIFE-FY16-09', N'توصيل المرافق لمدرستين بقرى مركز السادات', 47, 1, @NewStatusId, @MediumPriorityId, 3000000, 1000000, N'مقاولات', 10, NULL),
 (N'LIFE-FY16-10', N'تطوير المركز التكنولوجي لخدمة المواطنين', 38, 1, @NewStatusId, @MediumPriorityId, 7000000, 1000000, N'مقاولات', 10, 7500000),
 (N'LIFE-FY16-11', N'توريد أجهزة وتجهيزات لمركز معلومات المرافق', 45, 1, @NewStatusId, @MediumPriorityId, 2000000, 500000, N'توريدات', 5, 2300000),
 (N'LIFE-FY16-12', N'رفع كفاءة الطريق الإقليمي وربطه بالمناطق السكنية', 31, 1, @InProgressStatusId, @HighPriorityId, 20000000, 4000000, N'مقاولات', 15, 22000000),
 (N'LIFE-FY16-13', N'تطوير شبكة الإنارة واستبدال الأعمدة المتهالكة', 27, 1, @InProgressStatusId, @MediumPriorityId, 8000000, 2000000, N'مقاولات', 10, 9000000),
 (N'LIFE-FY16-14', N'إنشاء مجمع خدمي متكامل بقرى مركز الباجور', 11, 1, @StalledStatusId, @HighPriorityId, 15000000, 3000000, N'مقاولات', 20, 16000000),
 (N'LIFE-FY16-15', N'تطوير منظومة النظافة وتوريد معدات الجمع الآلي', 8, 1, @CompletedStatusId, @MediumPriorityId, 18000000, 4000000, N'مقاولات', 10, 20000000),
 (N'LIFE-FY16-16', N'رصف الطرق الداخلية وتركيب بلاط الإنترلوك', 65, 1, @InProgressStatusId, @MediumPriorityId, 12000000, 2000000, N'مقاولات', 15, 13000000);

INSERT INTO SubProjects
(
    MainProjectId, SubProjectName, ProjectNature, GreenInvestmentLink,
    ProjectDescription, ProjectGoal, SocialImpact, EconomicImpact, EnvironmentalImpact,
    MarkazId, PriorityId, Latitude, Longitude, StatusId, SubProjectCode,
    IsApproved, ApprovalCancellationReason, ApprovedAt, ApprovalCancelledAt,
    BankFunding, SelfFunding, ExecutiveAgencyId,
    ProjectLevelId, ComponentTypeId, AccountingUnitId, OverrunPercentage, ExecutionCompletedAt
)
SELECT
    source.MainProjectId,
    d.ProjectName,
    d.ProjectNature,
    source.GreenInvestmentLink,
    N'بيانات تشغيلية متكاملة لتوضيح دورة حياة المشروع ومؤشرات لوحة التحكم.',
    N'تحسين جودة الخدمات المحلية ورفع كفاءة الأصول العامة.',
    N'تحسين مستوى الخدمة المقدمة للمواطنين وتوسيع نطاق المستفيدين.',
    N'خفض تكاليف التشغيل ودعم النشاط الاقتصادي المحلي.',
    N'تقليل الأثر البيئي وتحسين كفاءة استخدام الموارد.',
    source.MarkazId,
    d.PriorityId,
    source.Latitude,
    source.Longitude,
    d.StatusId,
    d.Code,
    d.IsApproved,
    NULL,
    CASE WHEN d.IsApproved = 1 THEN DATEADD(day, -45, @Now) ELSE NULL END,
    NULL,
    d.BankFunding,
    d.SelfFunding,
    source.ExecutiveAgencyId,
    source.ProjectLevelId,
    source.ComponentTypeId,
    source.AccountingUnitId,
    d.OverrunPercentage,
    CASE WHEN d.Code = N'LIFE-FY16-15' THEN DATEADD(day, -2, @Now) ELSE NULL END
FROM @Definitions d
JOIN SubProjects source ON source.SubProjectId = d.SourceSubProjectId
WHERE NOT EXISTS (SELECT 1 FROM SubProjects existing WHERE existing.SubProjectCode = d.Code);

DECLARE @DemoProjects table
(
    Code nvarchar(50) PRIMARY KEY,
    SubProjectId int NOT NULL,
    ContractValue decimal(18,2) NULL
);

INSERT INTO @DemoProjects (Code, SubProjectId, ContractValue)
SELECT d.Code, s.SubProjectId, d.ContractValue
FROM @Definitions d
JOIN SubProjects s ON s.SubProjectCode = d.Code;

IF (SELECT COUNT(*) FROM @DemoProjects) <> 16
    THROW 51003, N'تعذر إنشاء أو استرجاع مجموعة مشروعات دورة الحياة كاملة.', 1;

INSERT INTO SubProjectFinancialYear (SubProjectId, FinancialYearId)
SELECT d.SubProjectId, @FinancialYearId
FROM @DemoProjects d
WHERE NOT EXISTS
(
    SELECT 1 FROM SubProjectFinancialYear x
    WHERE x.SubProjectId = d.SubProjectId AND x.FinancialYearId = @FinancialYearId
);

DECLARE @PlanId int =
(
    SELECT TOP (1) PlanId FROM Plans
    WHERE FinancialYearId = @FinancialYearId
    ORDER BY CASE WHEN PlanStatus = 1 THEN 0 ELSE 1 END, PlanId DESC
);

IF @PlanId IS NULL
BEGIN
    INSERT INTO Plans
        (PlanName, PlanStatus, StartDate, EndDate, IsClosed, SuggestionDate, ApprovalDate, FinancialYearId)
    SELECT N'الخطة المتكاملة – ' + Name, 1, StartDate, EndDate, 0, @Now, @Now, FinancialYearId
    FROM FinancialYears WHERE FinancialYearId = @FinancialYearId;
    SET @PlanId = SCOPE_IDENTITY();
END;

INSERT INTO PlanProjects (PlanId, SubProjectId)
SELECT @PlanId, d.SubProjectId
FROM @DemoProjects d
WHERE NOT EXISTS
(
    SELECT 1 FROM PlanProjects pp
    WHERE pp.PlanId = @PlanId AND pp.SubProjectId = d.SubProjectId
);

/* Reference contractor and contract type for the award/execution examples. */
IF NOT EXISTS (SELECT 1 FROM Contractor WHERE NationalIdOrCommercialRegister = N'LIFE-FY16-DEMO')
BEGIN
    INSERT INTO Contractor
        (ContractorName, CompanyType, NationalIdOrCommercialRegister, PhoneNumber,
         Email, Address, Category, IsActive, WillWorkAgain)
    VALUES
        (N'شركة المنوفية للمقاولات والتوريدات', N'شركة مساهمة مصرية', N'LIFE-FY16-DEMO',
         N'01000002038', N'contracts.demo@smartinvest.local', N'شبين الكوم – المنوفية',
         N'الفئة الأولى', 1, 1);
END;

IF NOT EXISTS (SELECT 1 FROM ContractType WHERE ContractName = N'عقد مقاولة عامة – دورة حياة')
    INSERT INTO ContractType (ContractName) VALUES (N'عقد مقاولة عامة – دورة حياة');

DECLARE @ContractorId int = (SELECT TOP (1) ContractorId FROM Contractor WHERE NationalIdOrCommercialRegister = N'LIFE-FY16-DEMO');
DECLARE @ContractTypeId int = (SELECT TOP (1) ContractTypeId FROM ContractType WHERE ContractName = N'عقد مقاولة عامة – دورة حياة');

/* Presentation memo: one under review and one approved. */
DECLARE @PendingMemoId int = (SELECT TOP (1) Id FROM PresentationMemos WHERE Title = N'مذكرة عرض قيد المراجعة – دورة حياة 2037/2038');
IF @PendingMemoId IS NULL
BEGIN
    INSERT INTO PresentationMemos
        (Title, CreatedAt, UpdatedAt, IsDeleted, CurrentVersionNumber, IsCompleted, ContractingMethod, FinancialYearId)
    VALUES
        (N'مذكرة عرض قيد المراجعة – دورة حياة 2037/2038', DATEADD(day,-40,@Now), NULL, 0, 1, 0, 1, @FinancialYearId);
    SET @PendingMemoId = SCOPE_IDENTITY();
END;

DECLARE @ApprovedMemoId int = (SELECT TOP (1) Id FROM PresentationMemos WHERE Title = N'مذكرة عرض معتمدة – دورة حياة 2037/2038');
IF @ApprovedMemoId IS NULL
BEGIN
    INSERT INTO PresentationMemos
        (Title, CreatedAt, UpdatedAt, IsDeleted, CurrentVersionNumber, IsCompleted, ContractingMethod, FinancialYearId)
    VALUES
        (N'مذكرة عرض معتمدة – دورة حياة 2037/2038', DATEADD(day,-38,@Now), DATEADD(day,-37,@Now), 0, 1, 1, 1, @FinancialYearId);
    SET @ApprovedMemoId = SCOPE_IDENTITY();
END;

IF NOT EXISTS (SELECT 1 FROM PresentationMemoVersions WHERE PresentationMemoId = @PendingMemoId AND VersionNumber = 1)
    INSERT INTO PresentationMemoVersions
        (PresentationMemoId, File_FileName, File_FileExtension, File_FileSize, File_Content,
         CreatedAt, UpdatedAt, IsDeleted, VersionNumber, Notes)
    VALUES
        (@PendingMemoId, N'مذكرة-قيد-المراجعة.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
         DATEADD(day,-40,@Now), NULL, 0, 1, N'بيانات دورة الحياة للعرض في بيئة التطوير');

IF NOT EXISTS (SELECT 1 FROM PresentationMemoVersions WHERE PresentationMemoId = @ApprovedMemoId AND VersionNumber = 1)
    INSERT INTO PresentationMemoVersions
        (PresentationMemoId, File_FileName, File_FileExtension, File_FileSize, File_Content,
         CreatedAt, UpdatedAt, IsDeleted, VersionNumber, Notes, LegalAffairsDecisionUploadedAt,
         LegalAffairsDecision_FileName, LegalAffairsDecision_FileExtension,
         LegalAffairsDecision_FileSize, LegalAffairsDecision_Content)
    VALUES
        (@ApprovedMemoId, N'مذكرة-معتمدة.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
         DATEADD(day,-38,@Now), NULL, 0, 1, N'بيانات دورة الحياة للعرض في بيئة التطوير',
         DATEADD(day,-37,@Now), N'قرار-الشؤون-القانونية.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf);

INSERT INTO PresentationMemoSubProjects
    (PresentationMemoId, SubProjectId, CreatedAt, UpdatedAt, IsDeleted)
SELECT @PendingMemoId, d.SubProjectId, DATEADD(day,-40,@Now), NULL, 0
FROM @DemoProjects d
WHERE d.Code = N'LIFE-FY16-04'
  AND NOT EXISTS
  (
      SELECT 1 FROM PresentationMemoSubProjects x
      WHERE x.PresentationMemoId = @PendingMemoId AND x.SubProjectId = d.SubProjectId
  );

INSERT INTO PresentationMemoSubProjects
    (PresentationMemoId, SubProjectId, CreatedAt, UpdatedAt, IsDeleted)
SELECT @ApprovedMemoId, d.SubProjectId, DATEADD(day,-38,@Now), NULL, 0
FROM @DemoProjects d
WHERE d.Code BETWEEN N'LIFE-FY16-05' AND N'LIFE-FY16-16'
  AND NOT EXISTS
  (
      SELECT 1 FROM PresentationMemoSubProjects x
      WHERE x.PresentationMemoId = @ApprovedMemoId AND x.SubProjectId = d.SubProjectId
  );

/* Tender: project 05 is current, later projects completed it. */
INSERT INTO TenderDocuments
    (CreatedAt, UpdatedAt, IsDeleted, CurrentVersionNumber, IsCompleted, SubProjectId,
     DurationDays, FailedAt, FailureReason, IsSkipped, SkipReason, SkippedAt, DurationSetAt)
SELECT DATEADD(day,-35,@Now), NULL, 0, 1,
       CASE WHEN d.Code = N'LIFE-FY16-05' THEN 0 ELSE 1 END,
       d.SubProjectId, 7, NULL, NULL, 0, NULL, NULL,
       CASE WHEN d.Code = N'LIFE-FY16-05' THEN DATEADD(day,-2,@Now) ELSE NULL END
FROM @DemoProjects d
WHERE d.Code BETWEEN N'LIFE-FY16-05' AND N'LIFE-FY16-16'
  AND NOT EXISTS (SELECT 1 FROM TenderDocuments x WHERE x.SubProjectId = d.SubProjectId);

INSERT INTO TenderDocumentVersions
    (TenderDocumentId, File_FileName, File_FileExtension, File_FileSize, File_Content,
     CreatedAt, UpdatedAt, IsDeleted, VersionNumber, Notes)
SELECT td.Id, N'كراسة-الشروط.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       td.CreatedAt, NULL, 0, 1, N'بيانات دورة الحياة للعرض في بيئة التطوير'
FROM TenderDocuments td
JOIN @DemoProjects d ON d.SubProjectId = td.SubProjectId
WHERE NOT EXISTS (SELECT 1 FROM TenderDocumentVersions v WHERE v.TenderDocumentId = td.Id AND v.VersionNumber = 1);

/* Announcement: project 06 is current, later projects completed it. */
INSERT INTO Announcements
    (CreatedAt, UpdatedAt, IsDeleted, CurrentVersionNumber, IsCompleted, SubProjectId,
     AnnouncementDate, DurationDays, FailedAt, FailureReason, IsSkipped, SkipReason, SkippedAt, DurationSetAt)
SELECT DATEADD(day,-30,@Now), NULL, 0, 1,
       CASE WHEN d.Code = N'LIFE-FY16-06' THEN 0 ELSE 1 END,
       d.SubProjectId, DATEADD(day,-30,@Now), 15, NULL, NULL, 0, NULL, NULL, DATEADD(day,-30,@Now)
FROM @DemoProjects d
WHERE d.Code BETWEEN N'LIFE-FY16-06' AND N'LIFE-FY16-16'
  AND NOT EXISTS (SELECT 1 FROM Announcements x WHERE x.SubProjectId = d.SubProjectId);

INSERT INTO AnnouncementVersions
    (AnnouncementId, NewspaperAdvertisement_FileName, NewspaperAdvertisement_FileExtension,
     NewspaperAdvertisement_FileSize, NewspaperAdvertisement_Content,
     PortalAdvertisement_FileName, PortalAdvertisement_FileExtension,
     PortalAdvertisement_FileSize, PortalAdvertisement_Content,
     CompetentAuthorityApproval_FileName, CompetentAuthorityApproval_FileExtension,
     CompetentAuthorityApproval_FileSize, CompetentAuthorityApproval_Content,
     CreatedAt, UpdatedAt, IsDeleted, VersionNumber, Notes)
SELECT a.Id,
       N'إعلان-الصحيفة.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       N'إعلان-البوابة.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       N'اعتماد-السلطة-المختصة.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       a.CreatedAt, NULL, 0, 1, N'بيانات دورة الحياة للعرض في بيئة التطوير'
FROM Announcements a
JOIN @DemoProjects d ON d.SubProjectId = a.SubProjectId
WHERE NOT EXISTS (SELECT 1 FROM AnnouncementVersions v WHERE v.AnnouncementId = a.Id AND v.VersionNumber = 1);

/* Opening envelopes: project 07 is current. */
INSERT INTO OpeningEnvelopes
    (CreatedAt, UpdatedAt, IsDeleted, CurrentVersionNumber, IsCompleted, SubProjectId,
     DurationDays, FailedAt, FailureReason, IsSkipped, SkipReason, SkippedAt, DurationSetAt)
SELECT DATEADD(day,-24,@Now), NULL, 0, 1,
       CASE WHEN d.Code = N'LIFE-FY16-07' THEN 0 ELSE 1 END,
       d.SubProjectId, 7, NULL, NULL, 0, NULL, NULL,
       CASE WHEN d.Code = N'LIFE-FY16-07' THEN DATEADD(day,-2,@Now) ELSE NULL END
FROM @DemoProjects d
WHERE d.Code BETWEEN N'LIFE-FY16-07' AND N'LIFE-FY16-16'
  AND NOT EXISTS (SELECT 1 FROM OpeningEnvelopes x WHERE x.SubProjectId = d.SubProjectId);

INSERT INTO OpeningEnvelopesVersions
    (OpeningEnvelopesId, File_FileName, File_FileExtension, File_FileSize, File_Content,
     CreatedAt, UpdatedAt, IsDeleted, VersionNumber, Notes)
SELECT o.Id, N'محضر-فتح-المظاريف.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       o.CreatedAt, NULL, 0, 1, N'بيانات دورة الحياة للعرض في بيئة التطوير'
FROM OpeningEnvelopes o
JOIN @DemoProjects d ON d.SubProjectId = o.SubProjectId
WHERE NOT EXISTS (SELECT 1 FROM OpeningEnvelopesVersions v WHERE v.OpeningEnvelopesId = o.Id AND v.VersionNumber = 1);

/* Technical evaluation: project 08 is current. */
INSERT INTO TechnicalEvaluations
    (CreatedAt, UpdatedAt, IsDeleted, CurrentVersionNumber, IsCompleted, SubProjectId,
     DurationDays, FailedAt, FailureReason, IsSkipped, SkipReason, SkippedAt, DurationSetAt)
SELECT DATEADD(day,-18,@Now), NULL, 0, 1,
       CASE WHEN d.Code = N'LIFE-FY16-08' THEN 0 ELSE 1 END,
       d.SubProjectId, 7, NULL, NULL, 0, NULL, NULL,
       CASE WHEN d.Code = N'LIFE-FY16-08' THEN DATEADD(day,-2,@Now) ELSE NULL END
FROM @DemoProjects d
WHERE d.Code BETWEEN N'LIFE-FY16-08' AND N'LIFE-FY16-16'
  AND NOT EXISTS (SELECT 1 FROM TechnicalEvaluations x WHERE x.SubProjectId = d.SubProjectId);

INSERT INTO TechnicalEvaluationVersions
    (TechnicalEvaluationId,
     FirstCommitteeReport_FileName, FirstCommitteeReport_FileExtension, FirstCommitteeReport_FileSize, FirstCommitteeReport_Content,
     SecondCommitteeReport_FileName, SecondCommitteeReport_FileExtension, SecondCommitteeReport_FileSize, SecondCommitteeReport_Content,
     FinalTechnicalEvaluationReport_FileName, FinalTechnicalEvaluationReport_FileExtension,
     FinalTechnicalEvaluationReport_FileSize, FinalTechnicalEvaluationReport_Content,
     CreatedAt, UpdatedAt, IsDeleted, VersionNumber, Notes)
SELECT t.Id,
       N'تقرير-اللجنة-الأولى.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       N'تقرير-اللجنة-الثانية.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       N'التقرير-الفني-النهائي.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       t.CreatedAt, NULL, 0, 1, N'بيانات دورة الحياة للعرض في بيئة التطوير'
FROM TechnicalEvaluations t
JOIN @DemoProjects d ON d.SubProjectId = t.SubProjectId
WHERE NOT EXISTS (SELECT 1 FROM TechnicalEvaluationVersions v WHERE v.TechnicalEvaluationId = t.Id AND v.VersionNumber = 1);

/* Financial evaluation: project 09 is current. */
INSERT INTO FinancialEvaluations
    (CreatedAt, UpdatedAt, IsDeleted, CurrentVersionNumber, IsCompleted, SubProjectId,
     DurationDays, FailedAt, FailureReason, IsSkipped, SkipReason, SkippedAt, DurationSetAt)
SELECT DATEADD(day,-12,@Now), NULL, 0, 1,
       CASE WHEN d.Code = N'LIFE-FY16-09' THEN 0 ELSE 1 END,
       d.SubProjectId, 7, NULL, NULL, 0, NULL, NULL,
       CASE WHEN d.Code = N'LIFE-FY16-09' THEN DATEADD(day,-2,@Now) ELSE NULL END
FROM @DemoProjects d
WHERE d.Code BETWEEN N'LIFE-FY16-09' AND N'LIFE-FY16-16'
  AND NOT EXISTS (SELECT 1 FROM FinancialEvaluations x WHERE x.SubProjectId = d.SubProjectId);

INSERT INTO FinancialEvaluationVersions
    (FinancialEvaluationId,
     FinancialBidOpeningMinutes_FileName, FinancialBidOpeningMinutes_FileExtension,
     FinancialBidOpeningMinutes_FileSize, FinancialBidOpeningMinutes_Content,
     FinancialEvaluationReport_FileName, FinancialEvaluationReport_FileExtension,
     FinancialEvaluationReport_FileSize, FinancialEvaluationReport_Content,
     EstimatedCostSheet_FileName, EstimatedCostSheet_FileExtension,
     EstimatedCostSheet_FileSize, EstimatedCostSheet_Content,
     CreatedAt, UpdatedAt, IsDeleted, VersionNumber, Notes)
SELECT f.Id,
       N'محضر-فتح-المظاريف-المالية.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       N'تقرير-التقييم-المالي.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       N'كشف-التكلفة-التقديرية.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       f.CreatedAt, NULL, 0, 1, N'بيانات دورة الحياة للعرض في بيئة التطوير'
FROM FinancialEvaluations f
JOIN @DemoProjects d ON d.SubProjectId = f.SubProjectId
WHERE NOT EXISTS (SELECT 1 FROM FinancialEvaluationVersions v WHERE v.FinancialEvaluationId = f.Id AND v.VersionNumber = 1);

/* Assignment and award: project 10 is pending award completion; 11-16 are awarded. */
INSERT INTO ProjectAssignment
    (SubProjectId, ContractorId, ContractTypeId, AssignmentDate, ContractNumber,
     ContractValue, ExpectedStartDate, ExpectedEndDate, Notes, IsLocked, ContractDate)
SELECT d.SubProjectId, @ContractorId, @ContractTypeId, DATEADD(day,-8,@Now), NULL,
       d.ContractValue, DATEADD(day,10,@Now), DATEADD(month,8,@Now),
       N'إسناد ضمن بيانات دورة الحياة المتكاملة', 0, DATEADD(day,-8,@Now)
FROM @DemoProjects d
WHERE d.Code BETWEEN N'LIFE-FY16-10' AND N'LIFE-FY16-16'
  AND NOT EXISTS (SELECT 1 FROM ProjectAssignment x WHERE x.SubProjectId = d.SubProjectId);

UPDATE pa
SET ContractNumber = CONVERT(nvarchar(50), pa.AssignmentId)
FROM ProjectAssignment pa
JOIN @DemoProjects d ON d.SubProjectId = pa.SubProjectId
WHERE d.Code BETWEEN N'LIFE-FY16-10' AND N'LIFE-FY16-16'
  AND pa.ContractNumber IS NULL;

INSERT INTO ContractAwards
    (AdvancePaymentDone, CreatedAt, UpdatedAt, IsDeleted, CurrentVersionNumber, IsCompleted,
     SubProjectId, AdvancePaymentBankAmount, AdvancePaymentPercentage, AdvancePaymentSelfAmount,
     ExecutionDurationDays, ExecutionDurationMonths, PenaltyAmount, ProjectAssignmentId,
     SiteHandoverDate, SiteHandoverMode,
     SiteHandoverProof_Content, SiteHandoverProof_FileExtension,
     SiteHandoverProof_FileName, SiteHandoverProof_FileSize,
     DurationDays, FailedAt, FailureReason, IsSkipped, SkipReason, SkippedAt, DurationSetAt)
SELECT
    CASE WHEN d.Code BETWEEN N'LIFE-FY16-12' AND N'LIFE-FY16-16' THEN 1 ELSE 0 END,
    DATEADD(day,-7,@Now), NULL, 0, 1,
    CASE WHEN d.Code = N'LIFE-FY16-10' THEN 0 ELSE 1 END,
    d.SubProjectId,
    CASE d.Code
        WHEN N'LIFE-FY16-12' THEN 1800000 WHEN N'LIFE-FY16-13' THEN 700000
        WHEN N'LIFE-FY16-14' THEN 1300000 WHEN N'LIFE-FY16-15' THEN 1000000
        WHEN N'LIFE-FY16-16' THEN 1100000 ELSE NULL END,
    CASE WHEN d.Code BETWEEN N'LIFE-FY16-12' AND N'LIFE-FY16-16' THEN 10 ELSE NULL END,
    CASE d.Code
        WHEN N'LIFE-FY16-12' THEN 400000 WHEN N'LIFE-FY16-13' THEN 200000
        WHEN N'LIFE-FY16-14' THEN 300000 WHEN N'LIFE-FY16-15' THEN 250000
        WHEN N'LIFE-FY16-16' THEN 200000 ELSE NULL END,
    0, 8, 250000,
    pa.AssignmentId,
    CASE WHEN d.Code = N'LIFE-FY16-10' THEN NULL ELSE DATEADD(day,-5,@Now) END,
    CASE WHEN d.Code = N'LIFE-FY16-10' THEN 1 ELSE 0 END,
    CASE WHEN d.Code = N'LIFE-FY16-10' THEN NULL ELSE @Pdf END,
    CASE WHEN d.Code = N'LIFE-FY16-10' THEN NULL ELSE N'.pdf' END,
    CASE WHEN d.Code = N'LIFE-FY16-10' THEN NULL ELSE N'إثبات-تسليم-الموقع.pdf' END,
    CASE WHEN d.Code = N'LIFE-FY16-10' THEN NULL ELSE DATALENGTH(@Pdf) END,
    NULL, NULL, NULL, 0, NULL, NULL, NULL
FROM @DemoProjects d
JOIN ProjectAssignment pa ON pa.SubProjectId = d.SubProjectId
WHERE d.Code BETWEEN N'LIFE-FY16-10' AND N'LIFE-FY16-16'
  AND NOT EXISTS (SELECT 1 FROM ContractAwards x WHERE x.SubProjectId = d.SubProjectId);

INSERT INTO ContractAwardVersions
    (ContractAwardId, AwardOrder_FileName, AwardOrder_FileExtension, AwardOrder_FileSize, AwardOrder_Content,
     Contract_FileName, Contract_FileExtension, Contract_FileSize, Contract_Content,
     CreatedAt, UpdatedAt, IsDeleted, VersionNumber, Notes,
     AdvancePaymentProof_Content, AdvancePaymentProof_FileExtension,
     AdvancePaymentProof_FileName, AdvancePaymentProof_FileSize)
SELECT a.Id,
       N'أمر-الإسناد.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       N'العقد.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
       a.CreatedAt, NULL, 0, 1, N'بيانات دورة الحياة للعرض في بيئة التطوير',
       CASE WHEN a.AdvancePaymentDone = 1 THEN @Pdf ELSE NULL END,
       CASE WHEN a.AdvancePaymentDone = 1 THEN N'.pdf' ELSE NULL END,
       CASE WHEN a.AdvancePaymentDone = 1 THEN N'إثبات-الدفعة-المقدمة.pdf' ELSE NULL END,
       CASE WHEN a.AdvancePaymentDone = 1 THEN DATALENGTH(@Pdf) ELSE NULL END
FROM ContractAwards a
JOIN @DemoProjects d ON d.SubProjectId = a.SubProjectId
WHERE NOT EXISTS (SELECT 1 FROM ContractAwardVersions v WHERE v.ContractAwardId = a.Id AND v.VersionNumber = 1);

/* System-managed initial delivery stage for awarded projects. */
INSERT INTO ExecutionStages
    (SubProjectId, Name, Deadline, SelfFundingSpent, BankFundingSpent,
     PhysicalProgressPercent, Notes, PenaltyAmount, PenaltyPaid, IsCompleted,
     CreatedAt, CompletedAt, IsFinalDelivery, SubProjectFinancialYearId)
SELECT d.SubProjectId, N'التسليم الأولي', DATEADD(month,8,@Now), 0, 0, 0,
       N'مرحلة تلقائية ضمن بيانات دورة الحياة', NULL, 0,
       CASE WHEN d.Code = N'LIFE-FY16-15' THEN 1 ELSE 0 END,
       DATEADD(day,-5,@Now),
       CASE WHEN d.Code = N'LIFE-FY16-15' THEN DATEADD(day,-2,@Now) ELSE NULL END,
       1, fy.SubProjectFinancialYearId
FROM @DemoProjects d
JOIN SubProjectFinancialYear fy
  ON fy.SubProjectId = d.SubProjectId AND fy.FinancialYearId = @FinancialYearId
WHERE d.Code BETWEEN N'LIFE-FY16-11' AND N'LIFE-FY16-16'
  AND NOT EXISTS
  (
      SELECT 1 FROM ExecutionStages x
      WHERE x.SubProjectFinancialYearId = fy.SubProjectFinancialYearId AND x.IsFinalDelivery = 1
  );

DECLARE @Stages table
(
    Code nvarchar(50), StageOrder int, StageName nvarchar(200),
    PhysicalProgress decimal(5,2), BankSpent decimal(18,2), SelfSpent decimal(18,2),
    IsCompleted bit, IsOverdue bit
);

INSERT INTO @Stages
    (Code, StageOrder, StageName, PhysicalProgress, BankSpent, SelfSpent, IsCompleted, IsOverdue)
VALUES
 (N'LIFE-FY16-12', 1, N'الأعمال التمهيدية وتجهيز الموقع', 20, 2400000, 600000, 1, 0),
 (N'LIFE-FY16-13', 1, N'توريد الأعمدة والكابلات', 25, 1800000, 450000, 1, 0),
 (N'LIFE-FY16-13', 2, N'التركيب والتشغيل التجريبي', 30, 2200000, 550000, 0, 0),
 (N'LIFE-FY16-14', 1, N'تجهيز الموقع والأساسات', 20, 2300000, 500000, 1, 0),
 (N'LIFE-FY16-14', 2, N'تنفيذ الهيكل الخرساني', 15, 1700000, 500000, 0, 1),
 (N'LIFE-FY16-15', 1, N'توريد معدات الجمع والنقل', 30, 4250000, 750000, 1, 0),
 (N'LIFE-FY16-15', 2, N'توزيع المعدات على المراكز', 35, 5000000, 1000000, 1, 0),
 (N'LIFE-FY16-15', 3, N'التشغيل والتدريب والاستلام', 35, 6750000, 1000000, 1, 0),
 (N'LIFE-FY16-16', 1, N'تجهيز التربة وطبقة الأساس', 30, 2500000, 500000, 1, 0),
 (N'LIFE-FY16-16', 2, N'تركيب الإنترلوك والأرصفة', 30, 2800000, 500000, 1, 0),
 (N'LIFE-FY16-16', 3, N'التشطيبات ومعالجة الملاحظات', 22, 2200000, 500000, 0, 0);

INSERT INTO ExecutionStages
(
    SubProjectId, Name, Deadline, SelfFundingSpent, BankFundingSpent,
    SelfFundingProof_FileName, SelfFundingProof_FileExtension, SelfFundingProof_FileSize, SelfFundingProof_Content,
    BankFundingProof_FileName, BankFundingProof_FileExtension, BankFundingProof_FileSize, BankFundingProof_Content,
    PhysicalProgressPercent,
    PhysicalProgressProof_FileName, PhysicalProgressProof_FileExtension,
    PhysicalProgressProof_FileSize, PhysicalProgressProof_Content,
    Notes, PenaltyAmount, PenaltyPaid, IsCompleted, CreatedAt, CompletedAt,
    IsFinalDelivery, SubProjectFinancialYearId
)
SELECT
    d.SubProjectId,
    s.StageName,
    CASE WHEN s.IsOverdue = 1 THEN DATEADD(day,-15,@Now) ELSE DATEADD(day,30 + (s.StageOrder * 35),@Now) END,
    s.SelfSpent,
    s.BankSpent,
    CASE WHEN s.SelfSpent > 0 THEN N'إثبات-صرف-ذاتي.pdf' END,
    CASE WHEN s.SelfSpent > 0 THEN N'.pdf' END,
    CASE WHEN s.SelfSpent > 0 THEN DATALENGTH(@Pdf) END,
    CASE WHEN s.SelfSpent > 0 THEN @Pdf END,
    CASE WHEN s.BankSpent > 0 THEN N'إثبات-صرف-بنكي.pdf' END,
    CASE WHEN s.BankSpent > 0 THEN N'.pdf' END,
    CASE WHEN s.BankSpent > 0 THEN DATALENGTH(@Pdf) END,
    CASE WHEN s.BankSpent > 0 THEN @Pdf END,
    s.PhysicalProgress,
    N'إثبات-التنفيذ-العيني.pdf', N'.pdf', DATALENGTH(@Pdf), @Pdf,
    N'بيانات دورة الحياة للعرض في بيئة التطوير',
    CASE WHEN s.IsOverdue = 1 THEN 125000 ELSE NULL END,
    0,
    s.IsCompleted,
    DATEADD(day,-10 + s.StageOrder,@Now),
    CASE WHEN s.IsCompleted = 1 THEN DATEADD(day,-3 + s.StageOrder,@Now) ELSE NULL END,
    0,
    fy.SubProjectFinancialYearId
FROM @Stages s
JOIN @DemoProjects d ON d.Code = s.Code
JOIN SubProjectFinancialYear fy
  ON fy.SubProjectId = d.SubProjectId AND fy.FinancialYearId = @FinancialYearId
WHERE NOT EXISTS
(
    SELECT 1 FROM ExecutionStages existing
    WHERE existing.SubProjectFinancialYearId = fy.SubProjectFinancialYearId
      AND existing.Name = s.StageName
);

/* Six bank availability receipts with proof documents. */
DECLARE @Availabilities table
(
    SequenceNo int PRIMARY KEY,
    Amount decimal(18,2),
    ReceivedDate datetime2
);

INSERT INTO @Availabilities (SequenceNo, Amount, ReceivedDate)
SELECT 1, 100000000, DATEADD(day, 15, StartDate) FROM FinancialYears WHERE FinancialYearId = @FinancialYearId
UNION ALL SELECT 2, 95000000, DATEADD(day, 55, StartDate) FROM FinancialYears WHERE FinancialYearId = @FinancialYearId
UNION ALL SELECT 3, 90000000, DATEADD(day, 100, StartDate) FROM FinancialYears WHERE FinancialYearId = @FinancialYearId
UNION ALL SELECT 4, 80000000, DATEADD(day, 155, StartDate) FROM FinancialYears WHERE FinancialYearId = @FinancialYearId
UNION ALL SELECT 5, 75000000, DATEADD(day, 215, StartDate) FROM FinancialYears WHERE FinancialYearId = @FinancialYearId
UNION ALL SELECT 6, 60000000, DATEADD(day, 275, StartDate) FROM FinancialYears WHERE FinancialYearId = @FinancialYearId;

DECLARE @FinancialAdminUserId nvarchar(450) =
(
    SELECT TOP (1) u.Id
    FROM AspNetUsers u
    WHERE u.NormalizedEmail = N'FINANCIAL.ADMIN@GMAIL.COM'
    ORDER BY u.CreatedAt
);

INSERT INTO BankAvailabilities
    (FinancialYearId, Amount, ReceivedDate, Notes, CreatedAt, CreatedByUserId)
SELECT @FinancialYearId, a.Amount, a.ReceivedDate,
       N'[LIFECYCLE-SEED-FY16] إتاحة بنكية مرحلية رقم ' + CONVERT(nvarchar(10),a.SequenceNo),
       DATEADD(minute,a.SequenceNo,@Now), @FinancialAdminUserId
FROM @Availabilities a
WHERE NOT EXISTS
(
    SELECT 1 FROM BankAvailabilities b
    WHERE b.FinancialYearId = @FinancialYearId
      AND b.Notes = N'[LIFECYCLE-SEED-FY16] إتاحة بنكية مرحلية رقم ' + CONVERT(nvarchar(10),a.SequenceNo)
);

INSERT INTO BankAvailabilityDocuments
    (BankAvailabilityId, File_FileName, File_FileExtension, File_FileSize, File_Content)
SELECT b.BankAvailabilityId,
       N'إثبات-الإتاحة-' + REPLACE(CONVERT(nvarchar(30), b.BankAvailabilityId), N' ', N'') + N'.pdf',
       N'.pdf', DATALENGTH(@Pdf), @Pdf
FROM BankAvailabilities b
WHERE b.FinancialYearId = @FinancialYearId
  AND b.Notes LIKE N'%LIFECYCLE-SEED-FY16%'
  AND NOT EXISTS
  (
      SELECT 1 FROM BankAvailabilityDocuments d
      WHERE d.BankAvailabilityId = b.BankAvailabilityId
  );

COMMIT TRANSACTION;

SELECT
    fy.Name AS FinancialYear,
    COUNT(DISTINCT sp.SubProjectId) AS DemoProjects,
    (SELECT COUNT(*) FROM BankAvailabilities b
      WHERE b.FinancialYearId = @FinancialYearId AND b.Notes LIKE N'%LIFECYCLE-SEED-FY16%') AS DemoAvailabilities,
    (SELECT COUNT(*) FROM ExecutionStages es
      JOIN SubProjectFinancialYear link ON link.SubProjectFinancialYearId = es.SubProjectFinancialYearId
      JOIN SubProjects p ON p.SubProjectId = link.SubProjectId
      WHERE link.FinancialYearId = @FinancialYearId AND p.SubProjectCode LIKE N'LIFE-FY16-%') AS DemoExecutionStages
FROM FinancialYears fy
JOIN SubProjectFinancialYear link ON link.FinancialYearId = fy.FinancialYearId
JOIN SubProjects sp ON sp.SubProjectId = link.SubProjectId AND sp.SubProjectCode LIKE N'LIFE-FY16-%'
WHERE fy.FinancialYearId = @FinancialYearId
GROUP BY fy.Name;
