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

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>The one state the dates cannot express — set by cancelling the class.</summary>
    public bool IsCancelled { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UPCOMING / ONGOING / COMPLETED derived from today against the class's dates,
    /// or CANCELLED when the class was called off. Derived rather than stored so a
    /// class cannot claim to be ONGOING while its semester has not started —
    /// do not use inside an EF query, filter on the dates instead.
    /// </summary>
    public string Status
    {
        get
        {
            if (IsCancelled) return "CANCELLED";

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (today < StartDate) return "UPCOMING";
            if (today > EndDate) return "COMPLETED";
            return "ONGOING";
        }
    }

    /// <summary>True while the class can still take enrollments — not yet finished or cancelled.</summary>
    public bool IsOpenForEnrollment => !IsCancelled && DateOnly.FromDateTime(DateTime.Today) <= EndDate;

    /// <summary>
    /// The run is over: the last session has been and gone. Editing is closed from
    /// here — the marks are final, the invoices are settled, and the transcripts
    /// students already downloaded have to keep matching what the class says.
    ///
    /// Deliberately NOT `Status == "COMPLETED"`: a cancelled class whose dates have
    /// passed reports CANCELLED, and it is just as finished as any other.
    /// </summary>
    public bool IsFinished => DateOnly.FromDateTime(DateTime.Today) > EndDate;

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
