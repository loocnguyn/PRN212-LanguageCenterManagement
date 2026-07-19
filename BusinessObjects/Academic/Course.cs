using System;
using System.Collections.Generic;

namespace BusinessObjects;

// Course — domain model.

public partial class Course
{
    public int CourseId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Level { get; set; }

    public string Language { get; set; } = null!;

    public int DurationSessions { get; set; }

    public decimal TuitionFee { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<GradeType> GradeTypes { get; set; } = new List<GradeType>();
}
