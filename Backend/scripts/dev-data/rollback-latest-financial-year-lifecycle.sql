/* Removes only rows created by seed-latest-financial-year-lifecycle.sql. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;

DECLARE @ProjectIds table (SubProjectId int PRIMARY KEY);
INSERT INTO @ProjectIds (SubProjectId)
SELECT SubProjectId FROM SubProjects WHERE SubProjectCode LIKE N'LIFE-FY16-%';

DELETE d
FROM BankAvailabilityDocuments d
JOIN BankAvailabilities b ON b.BankAvailabilityId = d.BankAvailabilityId
WHERE b.Notes LIKE N'%LIFECYCLE-SEED-FY16%';

DELETE FROM BankAvailabilities WHERE Notes LIKE N'%LIFECYCLE-SEED-FY16%';

DELETE FROM ExecutionStages WHERE SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);

DELETE v FROM ContractAwardVersions v
JOIN ContractAwards d ON d.Id = v.ContractAwardId
WHERE d.SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);
DELETE FROM ContractAwards WHERE SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);

DELETE v FROM FinancialEvaluationVersions v
JOIN FinancialEvaluations d ON d.Id = v.FinancialEvaluationId
WHERE d.SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);
DELETE FROM FinancialEvaluations WHERE SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);

DELETE v FROM TechnicalEvaluationVersions v
JOIN TechnicalEvaluations d ON d.Id = v.TechnicalEvaluationId
WHERE d.SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);
DELETE FROM TechnicalEvaluations WHERE SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);

DELETE v FROM OpeningEnvelopesVersions v
JOIN OpeningEnvelopes d ON d.Id = v.OpeningEnvelopesId
WHERE d.SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);
DELETE FROM OpeningEnvelopes WHERE SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);

DELETE v FROM AnnouncementVersions v
JOIN Announcements d ON d.Id = v.AnnouncementId
WHERE d.SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);
DELETE FROM Announcements WHERE SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);

DELETE v FROM TenderDocumentVersions v
JOIN TenderDocuments d ON d.Id = v.TenderDocumentId
WHERE d.SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);
DELETE FROM TenderDocuments WHERE SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);

DELETE FROM PresentationMemoSubProjects WHERE SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);

DELETE v FROM PresentationMemoVersions v
JOIN PresentationMemos m ON m.Id = v.PresentationMemoId
WHERE m.Title IN
(
    N'مذكرة عرض قيد المراجعة – دورة حياة 2037/2038',
    N'مذكرة عرض معتمدة – دورة حياة 2037/2038'
);
DELETE FROM PresentationMemos
WHERE Title IN
(
    N'مذكرة عرض قيد المراجعة – دورة حياة 2037/2038',
    N'مذكرة عرض معتمدة – دورة حياة 2037/2038'
);

DELETE FROM ProjectAssignment WHERE SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);
DELETE FROM PlanProjects WHERE SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);
DELETE FROM SubProjectFinancialYear WHERE SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);
DELETE FROM SubProjects WHERE SubProjectId IN (SELECT SubProjectId FROM @ProjectIds);

DELETE FROM ContractType
WHERE ContractName = N'عقد مقاولة عامة – دورة حياة'
  AND NOT EXISTS
  (
      SELECT 1 FROM ProjectAssignment pa
      WHERE pa.ContractTypeId = ContractType.ContractTypeId
  );

DELETE FROM Contractor
WHERE NationalIdOrCommercialRegister = N'LIFE-FY16-DEMO'
  AND NOT EXISTS
  (
      SELECT 1 FROM ProjectAssignment pa
      WHERE pa.ContractorId = Contractor.ContractorId
  );

COMMIT TRANSACTION;

SELECT N'تم حذف بيانات دورة الحياة التجريبية فقط.' AS Result;
