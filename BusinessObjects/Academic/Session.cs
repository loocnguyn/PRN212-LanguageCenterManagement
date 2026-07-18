using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class Session
{
    public int SessionId { get; set; }

    public int ClassId { get; set; }

    public int? ScheduleId { get; set; }

    public DateOnly SessionDate { get; set; }

    public string? Topic { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Class Class { get; set; } = null!;

    public virtual ClassSchedule? Schedule { get; set; }

    public virtual ICollection<TeacherAttendance> TeacherAttendances { get; set; } = new List<TeacherAttendance>();
}
