using System;
using System.Collections.Generic;

namespace BusinessObjects;

// ClassGradeComponent — one row of a class's FROZEN grading structure.
//
// Copied from the course's GradeTypes when the class is created, then never
// edited: grades are recorded against these rows, so changing a weight later
// would restate results students have already been shown. To run a class on a
// different structure, change the course template and create a new class.

public partial class ClassGradeComponent
{
    public int ComponentId { get; set; }

    public int ClassId { get; set; }

    public string Name { get; set; } = null!;

    public decimal WeightPercent { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
