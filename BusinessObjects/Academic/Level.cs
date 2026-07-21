using System;
using System.Collections.Generic;

namespace BusinessObjects;

// Level — a proficiency level, scoped to one language on purpose: "N5" only
// means anything for Japanese, "B1" only for the CEFR languages. Picking a
// language in the course dialog filters this list.

public partial class Level
{
    public int LevelId { get; set; }

    public int LanguageId { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Display order within the language (A1 before A2, N5 before N4).</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Language Language { get; set; } = null!;

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public override string ToString() => Name;
}
