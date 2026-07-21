using System;
using System.Collections.Generic;

namespace BusinessObjects;

// GradeType — one row of a course's grading TEMPLATE.
//
// Nothing points at these: when a class is created its components are copied
// out of here into ClassGradeComponents, and grades attach to those instead.
// Editing a template therefore only affects classes created afterwards.

public partial class GradeType
{
    public int GradeTypeId { get; set; }

    public int CourseId { get; set; }

    public string Name { get; set; } = null!;

    public decimal WeightPercent { get; set; }

    public string? Description { get; set; }

    public virtual Course Course { get; set; } = null!;
}
