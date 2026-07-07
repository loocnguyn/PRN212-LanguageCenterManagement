using System;
using System.Collections.Generic;

namespace BusinessObjects;

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
}
