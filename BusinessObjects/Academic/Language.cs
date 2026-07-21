using System;
using System.Collections.Generic;

namespace BusinessObjects;

// Language — a language the centre teaches. Courses pick one of these rather
// than storing a free-text name, so the catalogue stays consistent.

public partial class Language
{
    public int LanguageId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    /// <summary>Levels defined for this language (A1/B1 for English, N5/N4 for Japanese, …).</summary>
    public virtual ICollection<Level> Levels { get; set; } = new List<Level>();

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public override string ToString() => Name;
}
