using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class TeacherAttendance
{
    public int TeacherAttendanceId { get; set; }

    public int SessionId { get; set; }

    public int TeacherId { get; set; }

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public virtual Session Session { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
