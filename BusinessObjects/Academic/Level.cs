using System;
using System.Collections.Generic;

namespace BusinessObjects;

// Level — a proficiency level, scoped to one language on purpose: "N5" only
// means anything for Japanese, "B1" only for the CEFR languages. Picking a
// language in the course dialog filters this list.
//
// Display order is level_id, i.e. the order they were added — so add a
// language's levels beginner-first.

public partial class Level
{
    public int LevelId { get; set; }

    public int LanguageId { get; set; }

    public string Name { get; set; } = null!;

    public virtual Language Language { get; set; } = null!;

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public override string ToString() => Name;
}
