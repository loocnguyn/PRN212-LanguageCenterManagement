using System.Collections.Generic;

namespace BusinessObjects;

// Department — a staff department. Just an id and a name: which menus a
// department's staff can reach is decided in code
// (MainWindow.ApplyStaffDepartmentVisibility), not stored per row.

public partial class Department
{
    public int DepartmentId { get; set; }

    public string Name { get; set; } = null!;
}
