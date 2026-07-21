using System;
using System.Collections.Generic;

namespace BusinessObjects;

// Course — a catalogue entry: what the centre offers, at what price.
//
// This is a TEMPLATE. Classes snapshot it at creation, so editing a course
// (price, level, grading structure) only affects classes created afterwards.

public partial class Course
{
    public int CourseId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int LanguageId { get; set; }

    /// <summary>Optional — a course need not be pinned to a level.</summary>
    public int? LevelId { get; set; }

    public int DurationSessions { get; set; }

    public decimal TuitionFee { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Language Language { get; set; } = null!;

    public virtual Level? Level { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    /// <summary>Grading template copied into each new class's GradeComponents.</summary>
    public virtual ICollection<GradeType> GradeTypes { get; set; } = new List<GradeType>();

    // ---- Display helpers (null-safe for un-Included navigations) ----
    public string LanguageName => Language?.Name ?? "";
    public string LevelName => Level?.Name ?? "";
}
