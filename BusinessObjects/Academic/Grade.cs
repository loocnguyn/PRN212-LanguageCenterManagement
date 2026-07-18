using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class Grade
{
    public int GradeId { get; set; }

    public int EnrollmentId { get; set; }

    public int GradeTypeId { get; set; }

    public decimal Score { get; set; }

    public decimal MaxScore { get; set; }

    public DateTime GradedAt { get; set; }

    public string? Note { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual GradeType GradeType { get; set; } = null!;
}
