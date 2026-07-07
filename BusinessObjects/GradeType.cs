using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class GradeType
{
    public int GradeTypeId { get; set; }

    public string Name { get; set; } = null!;

    public decimal WeightPercent { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
