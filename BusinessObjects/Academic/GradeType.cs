using System;
using System.Collections.Generic;

namespace BusinessObjects;

// GradeType — domain model.

public partial class GradeType
{
    public int GradeTypeId { get; set; }

    public int CourseId { get; set; }

    public string Name { get; set; } = null!;

    public decimal WeightPercent { get; set; }

    public string? Description { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
