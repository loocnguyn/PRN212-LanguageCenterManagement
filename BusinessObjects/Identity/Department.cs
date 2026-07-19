using System.Collections.Generic;

namespace BusinessObjects;

// Department — domain model.

public partial class Department
{
    public int DepartmentId { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Which functional menu group this department unlocks for its staff:
    /// "ACADEMIC" (students/enrollment/classes) or "FINANCE" (invoices/payments/reports).</summary>
    public string AccessGroup { get; set; } = "ACADEMIC";

    /// <summary>Human-friendly label for the access group, shown in pickers/grids.</summary>
    public string AccessGroupDisplay => AccessGroup == "FINANCE" ? "Finance" : "Academic Setup";
}
