using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class Class
{
    public int ClassId { get; set; }

    public int SemesterId { get; set; }

    public int CourseId { get; set; }

    public int TeacherId { get; set; }

    public int ClassroomId { get; set; }

    public string Name { get; set; } = null!;

    public int MaxStudents { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();

    public virtual Classroom Classroom { get; set; } = null!;

    public virtual Course Course { get; set; } = null!;

    public virtual Semester Semester { get; set; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual Teacher Teacher { get; set; } = null!;
}
