using System;
using System.Collections.Generic;

namespace BusinessObjects;

// Semester — domain model.
//
// Three date milestones, all required:
//   StartDate    — semester opens; class setup begins
//   SetupEndDate — LAST day of setup; teaching starts the day after
//   EndDate      — semester closes
//
// There is deliberately no stored "is active" flag. Exactly one semester can
// contain today, so activeness is derived (see IsActive) — that makes it
// impossible to end up with two semesters flagged active at once, which the
// old stored flag allowed. Semesters must not overlap; SemesterService
// enforces that on save.

public partial class Semester
{
    public int SemesterId { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    /// <summary>Last day of the setup phase. Teaching (and session generation) starts the next day.</summary>
    public DateOnly SetupEndDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>
    /// True when today falls within this semester. Derived, not stored — do not use
    /// inside an EF query (it cannot be translated to SQL); query on the dates instead.
    /// </summary>
    public bool IsActive => Contains(DateOnly.FromDateTime(DateTime.Today));

    /// <summary>True when <paramref name="date"/> falls within this semester (inclusive).</summary>
    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;

    /// <summary>True when this semester's dates clash with <paramref name="other"/>'s.</summary>
    public bool Overlaps(Semester other) => StartDate <= other.EndDate && other.StartDate <= EndDate;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
