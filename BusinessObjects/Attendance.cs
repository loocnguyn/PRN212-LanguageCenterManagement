using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int SessionId { get; set; }

    public int StudentId { get; set; }

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime RecordedAt { get; set; }

    public virtual Session Session { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
