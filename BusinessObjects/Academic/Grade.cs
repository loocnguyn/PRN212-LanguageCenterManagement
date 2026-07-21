using System;
using System.Collections.Generic;

namespace BusinessObjects;

// Grade — one score for one enrollment against one of the CLASS's frozen
// grading components (not the course's GradeType template — see ClassGradeComponent).

public partial class Grade
{
    public int GradeId { get; set; }

    public int EnrollmentId { get; set; }

    public int ComponentId { get; set; }

    public decimal Score { get; set; }

    public decimal MaxScore { get; set; }

    public DateTime GradedAt { get; set; }

    public string? Note { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual ClassGradeComponent Component { get; set; } = null!;
}
