using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class Teacher
{
    public int TeacherId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Specialization { get; set; }

    public string? Degree { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<TeacherAttendance> TeacherAttendances { get; set; } = new List<TeacherAttendance>();

    public virtual User User { get; set; } = null!;
}
