namespace SmartInvest.Domain.Common;

public static class Roles
{
    public const string PlanningEmployee = "PlanningEmployee";

    public const string PlanningManager = "PlanningManager";

    public const string ExecutiveAgency = "ExecutiveAgency";

    public const string Contractor = "Contractor";

    /// <summary>مدير + موظف تخطيط.</summary>
    public const string PlanningStaff = "PlanningEmployee,PlanningManager";

    /// <summary>مدير + موظف تخطيط + الجهة التنفيذية.</summary>
    public const string StaffAndAgency = "PlanningEmployee,PlanningManager,ExecutiveAgency";

    /// <summary>كل الأطراف المشاركة في تعيين مقاول: تخطيط + جهة + مقاول.</summary>
    public const string AssignmentParties = "PlanningEmployee,PlanningManager,ExecutiveAgency,Contractor";
}