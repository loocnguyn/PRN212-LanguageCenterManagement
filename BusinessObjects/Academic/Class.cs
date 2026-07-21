using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessObjects;

// Class — a running instance of a course.
//
// A class SNAPSHOTS its course when created. CourseId stays only as provenance
// ("built from which course"); never read Course.TuitionFee / DurationSessions /
// Level for anything the class runs on — use the Snap* properties below, and
// GradeComponents for the grading structure. Editing a course must not restate
// what enrolled students were charged or promised.

public partial class Class
{
    public int ClassId { get; set; }

    public int SemesterId { get; set; }

    /// <summary>Provenance only — the course this class was created from. See the Snap* properties.</summary>
    public int CourseId { get; set; }

    public int ClassroomId { get; set; }

    public string Name { get; set; } = null!;

    public int MaxStudents { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    // ---- Frozen copy of the course at creation time -------------
    public string SnapCourseCode { get; set; } = null!;
    public string SnapCourseName { get; set; } = null!;
    public string SnapLanguage { get; set; } = null!;
    public string? SnapLevel { get; set; }
    public int SnapDurationSessions { get; set; }

    /// <summary>The price this class actually charges. Invoices are built from this, not Course.TuitionFee.</summary>
    public decimal SnapTuitionFee { get; set; }

    // ---- Navigation ---------------------------------------------
    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();

    public virtual Classroom Classroom { get; set; } = null!;

    public virtual Course Course { get; set; } = null!;

    public virtual Semester Semester { get; set; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    /// <summary>Teachers assigned to this class; one of them is flagged primary.</summary>
    public virtual ICollection<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();

    /// <summary>This class's frozen grading structure.</summary>
    public virtual ICollection<ClassGradeComponent> GradeComponents { get; set; } = new List<ClassGradeComponent>();

    // ---- Derived helpers ----------------------------------------
    // These walk the loaded ClassTeachers collection, so they need it Included.
    // They are not translatable to SQL — filter on ClassTeachers in queries.

    /// <summary>The teacher reports attribute this class to, or null if none is flagged.</summary>
    public Teacher? PrimaryTeacher =>
        ClassTeachers.FirstOrDefault(t => t.IsPrimary)?.Teacher
        ?? ClassTeachers.FirstOrDefault()?.Teacher;

    /// <summary>All assigned teachers, primary first.</summary>
    public IEnumerable<Teacher> Teachers =>
        ClassTeachers.OrderByDescending(t => t.IsPrimary)
                     .Select(t => t.Teacher)
                     .Where(t => t != null);

    /// <summary>"Tran Thi Binh, Le Minh Khoa" — primary first. Empty string when unassigned.</summary>
    public string TeacherNames => string.Join(", ", Teachers.Select(t => t.FullName));
}
