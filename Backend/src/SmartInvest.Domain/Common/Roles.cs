namespace SmartInvest.Domain.Common;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";

    public const string PlanningEmployee = "PlanningEmployee";

    public const string PlanningManager = "PlanningManager";

    public const string FinancialEmployee = "FinancialEmployee";

    public const string FinancialManager = "FinancialManager";

    /// <summary>المخولون بتعديل بيانات المشروعات والخطط.</summary>
    public const string PlanningStaff = "PlanningEmployee,PlanningManager,SuperAdmin";

    /// <summary>مدير التخطيط + سوبر أدمن — للعمليات الإدارية الحساسة مثل حذف الإتاحات البنكية.</summary>
    public const string ManagementStaff = "PlanningManager,SuperAdmin";

    /// <summary>كل الأدوار الوظيفية النشطة داخل النظام.</summary>
    public const string AllStaff = "PlanningEmployee,PlanningManager,FinancialEmployee,FinancialManager,SuperAdmin";

    /// <summary>مدير وموظف الإدارة المالية مع السوبر أدمن.</summary>
    public const string FinancialStaff = "FinancialEmployee,FinancialManager,SuperAdmin";

    /// <summary>الموظفون المسموح لهم بتنفيذ أعمال الإدارة المالية اليومية.</summary>
    public const string FinancialOperationsStaff = "FinancialEmployee,FinancialManager,SuperAdmin";

    /// <summary>المسموح لهم بتسجيل إتاحة بنكية جديدة — الإدارة المالية ومدير التخطيط والسوبر أدمن. التعديل والحذف يظلان على الإدارة المالية وحدها.</summary>
    public const string BankAvailabilityCreators = "FinancialEmployee,FinancialManager,PlanningManager,SuperAdmin";

    /// <summary>المديرون المسموح لهم بتنفيذ العمليات المالية الحساسة.</summary>
    public const string FinancialManagers = "FinancialManager,SuperAdmin";

    /// <summary>المخولون بإدارة بيانات المقاولين.</summary>
    public const string ContractorManagers = "FinancialManager,SuperAdmin";

    /// <summary>كل العاملين المسموح لهم بتحديث متابعة التنفيذ.</summary>
    public const string FollowUpStaff = AllStaff;

    /// <summary>المديرون المسموح لهم بعكس الإنهاء وتطبيق الغرامات.</summary>
    public const string FollowUpManagers = "PlanningManager,FinancialManager,SuperAdmin";

    /// <summary>المديرون المسموح لهم بالدخول لإدارة المستخدمين.</summary>
    public const string UserManagers = "PlanningManager,FinancialManager,SuperAdmin";
}
