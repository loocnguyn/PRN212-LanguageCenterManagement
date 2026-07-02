using System;
using System.Collections.Generic;

namespace BusinessObjects;

public partial class Semester
{
    public int SemesterId { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
