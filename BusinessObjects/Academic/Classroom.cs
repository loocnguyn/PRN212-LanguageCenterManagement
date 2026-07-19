using System;
using System.Collections.Generic;

namespace BusinessObjects;

// Classroom — domain model.

public partial class Classroom
{
    public int ClassroomId { get; set; }

    public string Name { get; set; } = null!;

    public int Capacity { get; set; }

    public string? Location { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
