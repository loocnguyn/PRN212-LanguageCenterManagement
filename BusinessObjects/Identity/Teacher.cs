using System;
using System.Collections.Generic;

namespace BusinessObjects;

// Teacher — domain model.

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

    /// <summary>Assignments to classes; a class may have several teachers. See ClassTeacher.</summary>
    public virtual ICollection<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();

    public virtual ICollection<TeacherAttendance> TeacherAttendances { get; set; } = new List<TeacherAttendance>();

    public virtual User User { get; set; } = null!;
}
