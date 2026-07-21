using System;
using System.Collections.Generic;

namespace BusinessObjects;

// ClassTeacher — a teacher assigned to a class. A class may be co-taught;
// exactly one assignment carries IsPrimary, and that is the teacher reports
// and finance filters attribute the class to.

public partial class ClassTeacher
{
    public int ClassId { get; set; }

    public int TeacherId { get; set; }

    /// <summary>At most one per class — enforced by a filtered unique index in the schema.</summary>
    public bool IsPrimary { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
